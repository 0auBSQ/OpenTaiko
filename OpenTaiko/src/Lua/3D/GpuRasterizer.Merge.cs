using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Silk.NET.OpenGLES;

namespace OpenTaiko {
	// Static opaque geometry merged per spatial bucket and texture.
	internal sealed partial class GpuRasterizer {
		// Static opaque geometry is merged per (spatial bucket × texture) so a block edit only re-uploads
		// the small affected bucket(s), synchronously (no whole-world spike, and no stale-frame "x-ray").
		// Cheap separate-format binding keeps the extra draw calls (one per non-empty bucket·texture) free.
		// Quads share their corner vertices through an index buffer.
		private const double BS = 32.0;   // bucket size in world units (2×2 voxel chunks)
		private struct MergeBuf { public uint Vbo, Ebo; public int Count, Rev; public double Cx, Cy, Cz, Hx, Hy, Hz; }   // index count, upload number, the box around its vertices (centre, half extents)
		private readonly Dictionary<(int bx, int bz, int tex), MergeBuf> _merge = new();
		private int _mergeUploads;
		// per-object merge state: the geometry version and bucket it was merged with, and its texture ids
		private sealed class MergeEmit { public int Version = int.MinValue, Bx, Bz, Scan; public readonly List<int> Texs = new(2); }
		private readonly Dictionary<SceneObject, MergeEmit> _mergeCache = new();
		private readonly Dictionary<(int bx, int bz), List<SceneObject>> _buckets = new();   // merged objects per bucket
		private readonly HashSet<(int bx, int bz, int tex)> _dirtyKeys = new();
		private readonly List<(int bx, int bz, int tex)> _dirtyList = new();
		private readonly List<SceneObject> _gone = new();
		private readonly HashSet<SceneObject> _live = new();   // reused by Prune (no per-call alloc)
		/// <summary>One merge buffer's vertices and indices while it is rebuilt.</summary>
		internal sealed class MergeScratch { public readonly List<float> V = new(1024); public readonly List<uint> I = new(256); }
		private readonly Dictionary<int, MergeScratch> _scratch = new();     // per texture id, reused
		private readonly Dictionary<int, MergeScratch> _emitInto = new();    // the dirty textures of the bucket being rebuilt
		private int _mergeRevSeen = int.MinValue, _settleAt = int.MaxValue, _scan;
		private readonly List<(int tex, MergeBuf mb)> _mergeVis = new(256);  // this pass's buckets in view

		// ── merged static opaque geometry ────────────────────────────────────────────────────────
		// The scene decides which objects may merge (SceneObject.MergeCandidate); geometry rebuilt every frame draws
		// solo until it settles (GeomBusy).
		private static bool StaticMerged(SceneObject o) => o.MergeCandidate && !o.GeomBusy;
		// An object whose geometry keeps changing (three changes, each at most two frames after the one before) is
		// busy: it leaves the merge, so its per-frame rebuild re-uploads only its own buffer instead of its merge
		// buckets, and it rejoins once its geometry has been still for a second. A one-off edit stays merged.
		private const int BUSY_STREAK = 3, BUSY_GAP = 2, BUSY_SETTLE = 60;
		private void TrackGeom(SceneObject o) {
			if (o.GeomVersion != o.GeomSeen) {
				if (o.GeomSeen != int.MinValue) {
					o.GeomStreak = _frame - o.GeomChangedAt <= BUSY_GAP ? o.GeomStreak + 1 : 1;
					if (o.GeomStreak >= BUSY_STREAK) o.GeomBusy = true;
				}
				o.GeomSeen = o.GeomVersion; o.GeomChangedAt = _frame;
			} else if (o.GeomBusy && _frame - o.GeomChangedAt > BUSY_SETTLE) {
				o.GeomBusy = false; o.GeomStreak = 0;
			}
		}
		// Rebuild the merge when the scene's merge revision moved, or when a busy object may have settled.
		private void UpdateMerged(Lua3DScene s) {
			if (s._geomRevision == _mergeRevSeen && _frame < _settleAt) return;
			_mergeRevSeen = s._geomRevision; _settleAt = int.MaxValue;
			RebuildMerged(s);
		}
		// Find the objects that joined, changed bucket or geometry, or left the merge, then synchronously rebuild only
		// the affected (bucket × texture) buffers from the members of those buckets. Each is small (one bucket's worth),
		// so there's no whole-world upload spike and the geometry is always current (no stale-frame x-ray).
		private void RebuildMerged(Lua3DScene s) {
			_dirtyKeys.Clear();
			int scan = ++_scan;
			foreach (var o in s._objects.Values) {
				if (!o.MergeCandidate) continue;
				TrackGeom(o);
				if (o.GeomBusy) { int at = o.GeomChangedAt + BUSY_SETTLE + 1; if (at < _settleAt) _settleAt = at; continue; }
				int bx = (int)Math.Floor(o.CenX / BS), bz = (int)Math.Floor(o.CenZ / BS);
				if (!_mergeCache.TryGetValue(o, out var em)) {
					em = new MergeEmit { Bx = bx, Bz = bz }; _mergeCache[o] = em;
					Members(bx, bz).Add(o);
					if (_cache.Remove(o, out var oc)) FreeBuffers(oc);   // its own buffer from drawing solo: the merge holds the geometry now
				} else if (em.Version == o.GeomVersion && em.Bx == bx && em.Bz == bz) {
					em.Scan = scan; continue;
				} else {
					foreach (int t in em.Texs) _dirtyKeys.Add((em.Bx, em.Bz, t));
					if (em.Bx != bx || em.Bz != bz) { LeaveBucket(o, em); em.Bx = bx; em.Bz = bz; Members(bx, bz).Add(o); }
				}
				em.Scan = scan; em.Version = o.GeomVersion;
				TexIds(o, em.Texs);
				foreach (int t in em.Texs) _dirtyKeys.Add((bx, bz, t));
			}
			// objects that left the merge (deleted, changed or busy)
			_gone.Clear();
			foreach (var kv in _mergeCache) if (kv.Value.Scan != scan) _gone.Add(kv.Key);
			foreach (var o in _gone) {
				var em = _mergeCache[o];
				foreach (int t in em.Texs) _dirtyKeys.Add((em.Bx, em.Bz, t));
				LeaveBucket(o, em); _mergeCache.Remove(o);
			}
			_gone.Clear();
			if (_dirtyKeys.Count == 0) return;
			// one pass over each dirty bucket's members fills all its dirty textures
			_dirtyList.Clear(); _dirtyList.AddRange(_dirtyKeys); _dirtyList.Sort();
			Gl.BindVertexArray(_vao);   // the index buffers bind into our VAO, never into one the caller left bound
			for (int i = 0; i < _dirtyList.Count;) {
				var (bx, bz, _) = _dirtyList[i];
				int j = i;
				_emitInto.Clear();
				for (; j < _dirtyList.Count && _dirtyList[j].bx == bx && _dirtyList[j].bz == bz; j++) {
					int tex = _dirtyList[j].tex;
					if (!_scratch.TryGetValue(tex, out var sc)) { sc = new MergeScratch(); _scratch[tex] = sc; }
					sc.V.Clear(); sc.I.Clear(); _emitInto[tex] = sc;
				}
				if (_buckets.TryGetValue((bx, bz), out var members)) foreach (var o in members) EmitMerged(o, _emitInto);
				for (int k = i; k < j; k++) UploadMerged(_dirtyList[k], _emitInto[_dirtyList[k].tex]);
				i = j;
			}
			_emitInto.Clear();
		}
		private List<SceneObject> Members(int bx, int bz) {
			if (!_buckets.TryGetValue((bx, bz), out var l)) { l = new List<SceneObject>(8); _buckets[(bx, bz)] = l; }
			return l;
		}
		private void LeaveBucket(SceneObject o, MergeEmit em) {
			if (!_buckets.TryGetValue((em.Bx, em.Bz), out var l)) return;
			l.Remove(o);
			if (l.Count == 0) _buckets.Remove((em.Bx, em.Bz));
		}
		// the distinct texture ids of an object's textured primitives
		internal static void TexIds(SceneObject o, List<int> ids) {
			ids.Clear();
			var d = o.D; int stride = PrimLayout.Stride(o.Kind), at = PrimLayout.TexAt(o.Kind), last = int.MinValue;
			for (int i = 0; i < o.N; i++) {
				int t = (int)d[i * stride + at];
				if (t == last) continue;
				last = t;
				if (!ids.Contains(t)) ids.Add(t);
			}
		}
		// upload one rebuilt (bucket × texture) buffer with the box around its vertices; an empty one is freed
		private unsafe void UploadMerged((int bx, int bz, int tex) key, MergeScratch sc) {
			if (sc.I.Count == 0) {
				if (_merge.Remove(key, out var old)) { Gl.DeleteBuffer(old.Vbo); Gl.DeleteBuffer(old.Ebo); }
				return;
			}
			if (!_merge.TryGetValue(key, out var mb)) mb = new MergeBuf { Vbo = Gl.GenBuffer(), Ebo = Gl.GenBuffer() };
			mb.Count = sc.I.Count; mb.Rev = ++_mergeUploads;
			var v = sc.V;
			float mnx = float.MaxValue, mny = float.MaxValue, mnz = float.MaxValue, mxx = float.MinValue, mxy = float.MinValue, mxz = float.MinValue;
			for (int i = 0; i < v.Count; i += STRIDE) {
				float x = v[i], y = v[i + 1], z = v[i + 2];
				if (x < mnx) mnx = x; if (x > mxx) mxx = x;
				if (y < mny) mny = y; if (y > mxy) mxy = y;
				if (z < mnz) mnz = z; if (z > mxz) mxz = z;
			}
			mb.Cx = ((double)mnx + mxx) * 0.5; mb.Cy = ((double)mny + mxy) * 0.5; mb.Cz = ((double)mnz + mxz) * 0.5;
			mb.Hx = ((double)mxx - mnx) * 0.5; mb.Hy = ((double)mxy - mny) * 0.5; mb.Hz = ((double)mxz - mnz) * 0.5;
			Gl.BindBuffer(BufferTargetARB.ArrayBuffer, mb.Vbo);
			Gl.BufferData<float>(BufferTargetARB.ArrayBuffer, (ReadOnlySpan<float>)CollectionsMarshal.AsSpan(v), BufferUsageARB.DynamicDraw);
			Gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, mb.Ebo);
			Gl.BufferData<uint>(BufferTargetARB.ElementArrayBuffer, (ReadOnlySpan<uint>)CollectionsMarshal.AsSpan(sc.I), BufferUsageARB.DynamicDraw);
			_merge[key] = mb;
		}
		private unsafe void DrawMerged(in MergeBuf mb) {
			Bind3D(mb.Vbo);
			Gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, mb.Ebo);
			Gl.DrawElements(PrimitiveType.Triangles, (uint)mb.Count, DrawElementsType.UnsignedInt, (void*)0);
		}
		/// <summary>Append an object's textured primitives whose texture id is a key of <paramref name="into"/> to that
		/// texture's lists: the same vertices as the per-object buffer, with a quad's two shared corners stored once.</summary>
		internal static void EmitMerged(SceneObject o, Dictionary<int, MergeScratch> into) {
			var sink = new MergeSink { Into = into, LastTex = int.MinValue };
			Walk(o, ref sink);
		}
		// emits into the merge lists of the textures it is given, as indexed vertices
		private struct MergeSink : IPrimSink {
			public Dictionary<int, MergeScratch> Into; public int LastTex; public MergeScratch? Sc;
			public bool Use(int mode, int tex) {
				if (mode == 0) return false;   // flat quads never merge
				if (tex != LastTex) { LastTex = tex; Into.TryGetValue(tex, out Sc); }
				return Sc != null;
			}
			public void Quad(double[] d, int k, double uM, double vM, double sh, double nx, double ny, double nz) {
				var L = Sc!.V; uint b = (uint)(L.Count / STRIDE);
				Vtx(L, d[k], d[k + 1], d[k + 2], 0, vM, sh, nx, ny, nz); Vtx(L, d[k + 3], d[k + 4], d[k + 5], uM, vM, sh, nx, ny, nz);
				Vtx(L, d[k + 6], d[k + 7], d[k + 8], uM, 0, sh, nx, ny, nz); Vtx(L, d[k + 9], d[k + 10], d[k + 11], 0, 0, sh, nx, ny, nz);
				var I = Sc.I; I.Add(b); I.Add(b + 1); I.Add(b + 2); I.Add(b); I.Add(b + 2); I.Add(b + 3);
			}
			public void Tri(double[] d, int k, double sh, double nx, double ny, double nz) {
				var L = Sc!.V; uint b = (uint)(L.Count / STRIDE);
				Vtx(L, d[k], d[k + 1], d[k + 2], d[k + 3], d[k + 4], sh, nx, ny, nz);
				Vtx(L, d[k + 5], d[k + 6], d[k + 7], d[k + 8], d[k + 9], sh, nx, ny, nz);
				Vtx(L, d[k + 10], d[k + 11], d[k + 12], d[k + 13], d[k + 14], sh, nx, ny, nz);
				var I = Sc.I; I.Add(b); I.Add(b + 1); I.Add(b + 2);
			}
		}

		// drop the merged buffers and everything that refers to objects (the next frame rebuilds them)
		private void ClearMerge() {
			foreach (var mb in _merge.Values) { Gl.DeleteBuffer(mb.Vbo); Gl.DeleteBuffer(mb.Ebo); }
			_merge.Clear(); _mergeCache.Clear(); _buckets.Clear(); _dirtyKeys.Clear(); _dirtyList.Clear(); _mergeVis.Clear();
			_scratch.Clear(); _emitInto.Clear();
			_mergeRevSeen = int.MinValue; _settleAt = int.MaxValue;
			_opaque.Clear(); _trans.Clear(); _overlay.Clear(); _screenTex.Clear(); _casters.Clear(); _tq.Clear(); _tqPlanes.Clear();
			_shadowBuckets.Clear(); _shadowObjs.Clear(); _shadowValid = false;
			_shadowDyn.Clear(); _shadowStaticValid = false;
		}
	}
}
