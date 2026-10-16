using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Silk.NET.OpenGLES;

namespace OpenTaiko {
	// Per-object vertex buffers and the world pass: gathering what the view shows and drawing it.
	internal sealed partial class GpuRasterizer {
		private readonly List<SceneObject> _screenTex = new(8);    // ScreenTex mirror/portal surfaces

		// Pal: the batch samples the solid-colour palette (TexId is then PAL_TEX)
		internal struct OBatch { public int Mode, TexId, Start, Count; public bool Pal; }
		internal const int PAL_TEX = int.MinValue;
		private sealed class ObjCache {
			public uint Vbo; public int Version = int.MinValue; public readonly List<OBatch> Batches = new();
			public SurfProg? Surf; public string? SurfSrc;   // the custom surface program last resolved for this object
			public readonly List<Piece> Pieces = new(); public int PieceVersion = int.MinValue;   // transparent queue pieces
			public int Serial;                                // bumped by every rebuild of the buffer
			// the solid textures folded into the palette (id, revision, pixel array), and the scene's texture and
			// live-target revisions they were last checked at
			public readonly List<(int id, int rev, int[]? pix)> Folded = new();
			public int TexStamp, RttStamp;
			// a buffer rebuilt frame after frame: the frame it was last built, how many rebuilds came in a row, and the
			// buffers it takes turns with (Vbo is the current one)
			public int BuiltAt = int.MinValue / 2, Streak, RingAt; public uint[]? Ring;
			// as a shadow caster: the hash of what it casts and the frame that hash last changed (a new caster starts settled)
			public ulong ShadowSig; public int ShadowAt = int.MinValue / 2; public bool ShadowSeen;
		}
		// Rebuilt vertices go into a buffer the GPU may still be drawing from: the driver then waits for the GPU before
		// taking them. An object rebuilt frame after frame takes turns with RING buffers instead.
		private const int RING = 3;
		private void NextBuffer(ObjCache c) {
			c.Streak = _frame - c.BuiltAt <= 2 ? c.Streak + 1 : 0;
			c.BuiltAt = _frame;
			if (c.Streak < 2) return;
			if (c.Ring == null) { c.Ring = new uint[RING]; c.Ring[0] = c.Vbo; for (int i = 1; i < RING; i++) c.Ring[i] = Gl.GenBuffer(); }
			c.RingAt = (c.RingAt + 1) % RING; c.Vbo = c.Ring[c.RingAt];
		}
		private void FreeBuffers(ObjCache c) {
			if (c.Ring == null) { Gl.DeleteBuffer(c.Vbo); return; }
			foreach (uint b in c.Ring) Gl.DeleteBuffer(b);
		}
		private readonly Dictionary<SceneObject, ObjCache> _cache = new();
		private readonly List<float> _tmp = new(4096);
		private Lua3DScene? _scene;   // the scene of the pass being prepared (a renderer serves one scene)
		private readonly List<int> _foldIds = new(16);
		private readonly Dictionary<int, (float u, float v)> _fold = new();
		private readonly List<SceneObject> _opaque = new(256), _trans = new(64), _overlay = new(16);

		private static readonly float[] _ident = { 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1 };
		private static readonly float[] _ident3 = { 1, 0, 0, 0, 1, 0, 0, 0, 1 };
		private readonly float[] _mtx = new float[16], _nmtx = new float[9];

		private float[] ModelMatrix(double[] t) {
			if (t == null) return _ident;
			var m = _mtx;
			m[0] = (float)t[0]; m[1] = (float)t[4]; m[2] = (float)t[8]; m[3] = 0;
			m[4] = (float)t[1]; m[5] = (float)t[5]; m[6] = (float)t[9]; m[7] = 0;
			m[8] = (float)t[2]; m[9] = (float)t[6]; m[10] = (float)t[10]; m[11] = 0;
			m[12] = (float)t[3]; m[13] = (float)t[7]; m[14] = (float)t[11]; m[15] = 1;
			return m;
		}
		/// <summary>The normal matrix (column-major mat3) of a row-major transform: the inverse transpose of its linear
		/// part up to a positive scale (the adjugate, sign-corrected), so normals stay perpendicular under any scale.
		/// Shaders normalise the result.</summary>
		internal static float[] NormalMatrix(double[] t, float[] m) {
			double ax = t[0], ay = t[1], az = t[2], bx = t[4], by = t[5], bz = t[6], cx = t[8], cy = t[9], cz = t[10];
			// the adjugate, row-major, read as column-major, is the transpose of the inverse times the determinant
			double a0 = by * cz - bz * cy, a3 = bz * cx - bx * cz, a6 = bx * cy - by * cx;
			double a1 = cy * az - cz * ay, a4 = cz * ax - cx * az, a7 = cx * ay - cy * ax;
			double a2 = ay * bz - az * by, a5 = az * bx - ax * bz, a8 = ax * by - ay * bx;
			double sg = ax * a0 + ay * a3 + az * a6 < 0 ? -1 : 1;
			m[0] = (float)(sg * a0); m[1] = (float)(sg * a1); m[2] = (float)(sg * a2);
			m[3] = (float)(sg * a3); m[4] = (float)(sg * a4); m[5] = (float)(sg * a5);
			m[6] = (float)(sg * a6); m[7] = (float)(sg * a7); m[8] = (float)(sg * a8);
			return m;
		}

		private static void Vtx(List<float> d, double x, double y, double z, double u, double v, double sh, double nx, double ny, double nz) {
			d.Add((float)x); d.Add((float)y); d.Add((float)z); d.Add((float)u); d.Add((float)v); d.Add((float)sh);
			d.Add((float)nx); d.Add((float)ny); d.Add((float)nz);
		}
		// per-primitive normal: the object's own normal if set, else the triangle's geometric normal
		private static void Nrm(SceneObject o, double ax, double ay, double az, double bx, double by, double bz, double cx, double cy, double cz, out double nx, out double ny, out double nz) {
			if (o.HasNormal) { nx = o.Nx; ny = o.Ny; nz = o.Nz; return; }
			double e1x = bx - ax, e1y = by - ay, e1z = bz - az, e2x = cx - ax, e2y = cy - ay, e2z = cz - az;
			nx = e1y * e2z - e1z * e2y; ny = e1z * e2x - e1x * e2z; nz = e1x * e2y - e1y * e2x;
			double l = Math.Sqrt(nx * nx + ny * ny + nz * nz); if (l > 1e-9) { nx /= l; ny /= l; nz /= l; }
		}

		/// <summary>Takes an object's quads and triangles from <see cref="Walk"/>: their model-space corners in the
		/// object's primitive array (<see cref="PrimLayout"/>), uvs, shade and normal.</summary>
		internal interface IPrimSink {
			/// <summary>Whether to take the primitives that follow, drawn in this mode (0 flat, 1 textured) with this
			/// texture id (0 for flat ones).</summary>
			bool Use(int mode, int tex);
			/// <summary>A quad at d[k]: its corners take the uvs (0, vM), (uM, vM), (uM, 0), (0, 0).</summary>
			void Quad(double[] d, int k, double uM, double vM, double sh, double nx, double ny, double nz);
			/// <summary>A textured triangle at d[k], with the uvs it holds.</summary>
			void Tri(double[] d, int k, double sh, double nx, double ny, double nz);
		}

		// one object's quads and triangles into the sink, in order; a flat quad is a quad with no uv extent and full shade
		internal static void Walk<T>(SceneObject o, ref T sink) where T : struct, IPrimSink {
			var d = o.D;
			if (o.Kind == 0) {
				for (int i = 0; i < o.N; i++) {
					int k = i * PrimLayout.TexQuadStride;
					if (!sink.Use(1, (int)d[k + PrimLayout.TexQuadTex])) continue;
					Nrm(o, d[k], d[k + 1], d[k + 2], d[k + 3], d[k + 4], d[k + 5], d[k + 9], d[k + 10], d[k + 11], out double nx, out double ny, out double nz);
					sink.Quad(d, k, d[k + PrimLayout.TexQuadUMax], d[k + PrimLayout.TexQuadVMax], d[k + PrimLayout.TexQuadShade], nx, ny, nz);
				}
			} else if (o.Kind == 1) {
				if (!sink.Use(0, 0)) return;
				for (int i = 0; i < o.N; i++) {
					int k = i * PrimLayout.FlatQuadStride;
					Nrm(o, d[k], d[k + 1], d[k + 2], d[k + 3], d[k + 4], d[k + 5], d[k + 9], d[k + 10], d[k + 11], out double nx, out double ny, out double nz);
					sink.Quad(d, k, 0, 0, 1, nx, ny, nz);
				}
			} else if (o.Kind == 2) {
				for (int i = 0; i < o.N; i++) {
					int k = i * PrimLayout.TexTriStride;
					if (!sink.Use(1, (int)d[k + PrimLayout.TexTriTex])) continue;
					Nrm(o, d[k], d[k + 1], d[k + 2], d[k + 5], d[k + 6], d[k + 7], d[k + 10], d[k + 11], d[k + 12], out double nx, out double ny, out double nz);
					sink.Tri(d, k, d[k + PrimLayout.TexTriShade], nx, ny, nz);
				}
			}
		}

		// emits an object's primitives as plain triangles (a quad's two share no vertex), grouped into batches of one mode
		// and texture id. Every primitive is emitted: one whose texture is not registered yet is skipped at draw time and
		// shows once it is. With Fold, the primitives of the textures it lists take their palette uv and join palette
		// batches.
		internal struct Batcher : IPrimSink {
			public List<OBatch> Batches; public List<float> Dst; public int Mode, Tex, Start;
			public bool Pal;
			public Dictionary<int, (float u, float v)>? Fold;
			private int _raw; private bool _haveRaw, _rawPal; private float _pu, _pv;   // the last texture id seen and its fold
			public bool Use(int mode, int tex) {
				bool pal = false;
				if (Fold != null && mode == 1) {
					if (!_haveRaw || tex != _raw) { _haveRaw = true; _raw = tex; _rawPal = Fold.TryGetValue(tex, out var uv); _pu = uv.u; _pv = uv.v; }
					pal = _rawPal;
				}
				int key = pal ? PAL_TEX : tex;
				if (mode != Mode || key != Tex || pal != Pal) { Flush(); Mode = mode; Tex = key; Pal = pal; }
				return true;
			}
			public void Flush() { int end = Dst.Count / STRIDE; if (end > Start) Batches.Add(new OBatch { Mode = Mode, TexId = Tex, Start = Start, Count = end - Start, Pal = Pal }); Start = end; }
			public void Quad(double[] d, int k, double uM, double vM, double sh, double nx, double ny, double nz) {
				double u0 = 0, v0 = 0, u1 = uM, v1 = vM;
				if (Pal) { u0 = u1 = _pu; v0 = v1 = _pv; }
				var w = Grow(6);
				Put(w, 0, d[k], d[k + 1], d[k + 2], u0, v1, sh, nx, ny, nz); Put(w, 9, d[k + 3], d[k + 4], d[k + 5], u1, v1, sh, nx, ny, nz); Put(w, 18, d[k + 6], d[k + 7], d[k + 8], u1, v0, sh, nx, ny, nz);
				Put(w, 27, d[k], d[k + 1], d[k + 2], u0, v1, sh, nx, ny, nz); Put(w, 36, d[k + 6], d[k + 7], d[k + 8], u1, v0, sh, nx, ny, nz); Put(w, 45, d[k + 9], d[k + 10], d[k + 11], u0, v0, sh, nx, ny, nz);
			}
			public void Tri(double[] d, int k, double sh, double nx, double ny, double nz) {
				var w = Grow(3);
				if (Pal) {
					Put(w, 0, d[k], d[k + 1], d[k + 2], _pu, _pv, sh, nx, ny, nz);
					Put(w, 9, d[k + 5], d[k + 6], d[k + 7], _pu, _pv, sh, nx, ny, nz);
					Put(w, 18, d[k + 10], d[k + 11], d[k + 12], _pu, _pv, sh, nx, ny, nz);
					return;
				}
				Put(w, 0, d[k], d[k + 1], d[k + 2], d[k + 3], d[k + 4], sh, nx, ny, nz);
				Put(w, 9, d[k + 5], d[k + 6], d[k + 7], d[k + 8], d[k + 9], sh, nx, ny, nz);
				Put(w, 18, d[k + 10], d[k + 11], d[k + 12], d[k + 13], d[k + 14], sh, nx, ny, nz);
			}
			// room for n more vertices at the end of Dst
			private readonly Span<float> Grow(int n) {
				int at = Dst.Count;
				CollectionsMarshal.SetCount(Dst, at + n * STRIDE);
				return CollectionsMarshal.AsSpan(Dst).Slice(at);
			}
			private static void Put(Span<float> w, int i, double x, double y, double z, double u, double v, double sh, double nx, double ny, double nz) {
				w[i] = (float)x; w[i + 1] = (float)y; w[i + 2] = (float)z; w[i + 3] = (float)u; w[i + 4] = (float)v; w[i + 5] = (float)sh;
				w[i + 6] = (float)nx; w[i + 7] = (float)ny; w[i + 8] = (float)nz;
			}
		}

		// the palette uvs of the object's solid textures, recorded in c.Folded; null when none fold (screen-texture and
		// splat surfaces draw their textures as they are)
		private Dictionary<int, (float u, float v)>? FoldFor(SceneObject o, ObjCache c) {
			c.Folded.Clear();
			var s = _scene;
			if (s == null || (o.Kind != 0 && o.Kind != 2) || o.ScreenTex || o.SplatSprite >= 0) return null;
			c.TexStamp = s._texRevision; c.RttStamp = _rttRev;
			TexIds(o, _foldIds); _fold.Clear();
			foreach (int id in _foldIds)
				if (SolidUv(s, id, out float u, out float v, out int rev, out int[]? pix)) { _fold[id] = (u, v); c.Folded.Add((id, rev, pix)); }
			return _fold.Count > 0 ? _fold : null;
		}
		// whether the object's folds still hold: every folded texture unchanged, and the object still draws by uv
		private bool FoldsHold(SceneObject o, ObjCache c) {
			if (c.Folded.Count == 0) return true;
			if (o.ScreenTex || o.SplatSprite >= 0) return false;
			var s = _scene;
			if (s == null || (c.TexStamp == s._texRevision && c.RttStamp == _rttRev)) return true;
			foreach (var (id, rev, pix) in c.Folded) if (!StillSolid(s, id, rev, pix)) return false;
			c.TexStamp = s._texRevision; c.RttStamp = _rttRev;
			return true;
		}

		private ObjCache EnsureObj(SceneObject o) {
			if (!_cache.TryGetValue(o, out var c)) { c = new ObjCache { Vbo = Gl.GenBuffer() }; _cache[o] = c; }
			if (c.Version == o.GeomVersion && FoldsHold(o, c)) return c;
			c.Version = o.GeomVersion; c.Batches.Clear(); _tmp.Clear(); c.Serial++;
			NextBuffer(c);
			var b = new Batcher { Batches = c.Batches, Dst = _tmp, Mode = -1, Tex = int.MinValue, Fold = FoldFor(o, c) };
			Walk(o, ref b); b.Flush();
			Gl.BindBuffer(BufferTargetARB.ArrayBuffer, c.Vbo);
			Gl.BufferData<float>(BufferTargetARB.ArrayBuffer, (ReadOnlySpan<float>)CollectionsMarshal.AsSpan(_tmp), BufferUsageARB.StaticDraw);
			return c;
		}

		private static void XfPt(double[] t, double px, double py, double pz, out double x, out double y, out double z) {
			if (t == null) { x = px; y = py; z = pz; return; }
			x = t[0] * px + t[1] * py + t[2] * pz + t[3];
			y = t[4] * px + t[5] * py + t[6] * pz + t[7];
			z = t[8] * px + t[9] * py + t[10] * pz + t[11];
		}

		// Split the per-frame object scan + the world draw out of Render so the off-screen reflection
		// pass (RenderToTexture) can reuse them with a different camera/target. Per-object draws and merge buckets
		// are culled to this pass's view: behind the camera, off screen, past SetRenderDistance, or facing away
		// (ObjSetNormal, like the CPU rasterizer).
		private void GatherObjects(Lua3DScene s) {
			_opaque.Clear(); _trans.Clear(); _overlay.Clear(); _screenTex.Clear(); _casters.Clear(); _mergeVis.Clear();
			double rd = s._renderDist;
			foreach (var o in s._objects.Values) {
				if (o.Kind < 0 || o.Kind == 3 || o.Kind == 4 || o.N <= 0) continue;   // 3/4 = sprites, drawn in the sprite pass
				if (StaticMerged(o)) continue;
				if (!o.Visible) continue;
				bool bounded = o.WorldSphere(out double cx, out double cy, out double cz, out double r);
				if (o.ScreenTex) {   // mirror/portal surfaces: own draw (never inside an RTT)
					if (!s._rttPass && InView(o, bounded, cx, cy, cz, r)) _screenTex.Add(o);
					continue;
				}
				if (o.Overlay) {   // viewmodels: own pass, never culled
					_overlay.Add(o); Reach(bounded ? _view.Depth(cx, cy, cz) + r : FAR);
					continue;
				}
				double dx = cx - s._camX, dy = cy - s._camY, dz = cz - s._camZ;
				o.Dist = dx * dx + dy * dy + dz * dz;   // an unbounded object sorts by its origin
				if (bounded && rd > 0) { double rr = rd + r; if (o.Dist > rr * rr) continue; }
				if (o.Pass == 0 && o.Lit && o.CastShadow && o.SplatSprite < 0) _casters.Add(o);   // off-screen casters still shadow the view
				if (!InView(o, bounded, cx, cy, cz, r)) continue;
				if (o.Pass == 0) _opaque.Add(o); else _trans.Add(o);
			}
			_opaque.Sort(_nearFirst);   // front to back: the depth test rejects hidden fragments before they shade
			// merge buckets: their vertex boxes against the view, and against the render distance by camera depth like
			// the fog, so a bucket left out at the screen sides lies past the fog end too
			foreach (var kv in _merge) {
				var mb = kv.Value;
				if (mb.Count == 0) continue;
				if (rd > 0 && _view.NearDepth(mb.Cx, mb.Cy, mb.Cz, mb.Hx, mb.Hy, mb.Hz) > rd) continue;
				if (!_view.BoxInView(mb.Cx, mb.Cy, mb.Cz, mb.Hx, mb.Hy, mb.Hz)) continue;
				Reach(_view.FarDepth(mb.Cx, mb.Cy, mb.Cz, mb.Hx, mb.Hy, mb.Hz));
				_mergeVis.Add((kv.Key.tex, mb));
			}
		}
		// whether a per-object draw can show: its world sphere (when it has bounds) against the view, its facing plane
		// against the eye; a drawn object pushes the far plane out to its sphere (to the cap without bounds)
		private bool InView(SceneObject o, bool bounded, double cx, double cy, double cz, double r) {
			if (bounded && !_view.SphereInView(cx, cy, cz, r)) return false;
			if (o.FacesAway(_view.Ex, _view.Ey, _view.Ez)) return false;
			Reach(bounded ? _view.Depth(cx, cy, cz) + r : FAR);
			return true;
		}
		private static readonly Comparison<SceneObject> _nearFirst = (a, b) => a.Dist.CompareTo(b.Dist);

		// Opaque (merged + per-object + screen-tex + sprites), the sky shader behind them, then transparent (surfaces, then
		// the queue back to front). Assumes the program/VAO are bound and SetFrameUniforms already ran. w x h is the bound
		// target's size; skyOwnBasis: the sky takes this pass's camera (DrawSky).
		private void DrawWorld(Lua3DScene s, int litScene, int w, int h, bool skyOwnBasis) {
			GlPass.Opaque(Gl);
			// merged static opaque (one draw per bucket·texture in view)
			UMode(1); UCutout(0); UTint(1f, 1f, 1f); UAlpha(1f);
			ULit(litScene); UModel(null);
			foreach (var (tex, mb) in _mergeVis) {
				Gl.ActiveTexture(TextureUnit.Texture0);
				uint gt = TexFor(s, tex); if (gt == 0) continue;
				BindNormalFor(s, tex);
				Gl.ActiveTexture(TextureUnit.Texture0); Gl.BindTexture(TextureTarget.Texture2D, gt);
				DrawMerged(mb);
			}
			foreach (var o in _opaque) DrawObj(s, o, litScene);
			DrawGrass(s);                                        // wind-swayed blade field (cutout, depth-written)
			foreach (var o in _screenTex) if (o.Pass == 0) DrawScreenTex(s, o);   // OPAQUE mirrors/portals (depth-written)
			// pass boundary: assert opaque state (never assume); soft sprite edges must not lower the canvas alpha
			GlPass.Covered(Gl);
			Bind3D(_spriteVbo);
			foreach (var bt in _sprBatches) if (bt.Pass == 0) DrawSprite(bt);
			// the sky on the pixels the opaque world left uncovered (the depth test skips the rest, before they shade); the
			// transparent pass blends over it
			if (_skyOk) {
				DrawSky(s, w, h, skyOwnBasis);
				Gl.UseProgram(_worldShadow ? _progShadow : _prog); Gl.BindVertexArray(_vao);
			}

			// Transparent pass, no depth write. The surfaces come first: translucent screen-tex surfaces (water, whose
			// reflection blends over the bottom drawn beneath), then the sprite quads laid on the world (foam, ripples,
			// ground shadows), so whatever stands over them layers on top. Then the queue back to front: the transparent
			// objects by piece and the billboard sprites, with every particle between them (a pane of glass blends over
			// the smoke behind it, and the smoke in front of it over the pane).
			GlPass.Transparent(Gl);
			foreach (var o in _screenTex) if (o.Pass == 1) DrawScreenTex(s, o);
			Bind3D(_spriteVbo);
			foreach (var bt in _sprBatches) if (bt.Pass == 1 && bt.Obj.Kind == 4) DrawSprite(bt);
			DrawQueue(s, litScene);

			// X-RAY silhouettes: sprites flagged ObjSetXray re-draw ONLY where they lost the depth test
			// (DepthFunc GREATER, no depth write) as a faint ghost — the player reads through walls/hills.
			bool anyXray = false;
			foreach (var bt in _sprBatches) if (bt.Obj.XrayAlpha > 0) { anyXray = true; break; }
			if (anyXray && !s._rttPass) {
				Gl.DepthFunc(DepthFunction.Greater);
				Bind3D(_spriteVbo);
				UModel(null);
				UMode(2); UCutout(0); ULit(0);
				foreach (var bt in _sprBatches) {
					var o = bt.Obj;
					if (o.XrayAlpha <= 0) continue;
					UTint((float)(o.TintR * 0.75), (float)(o.TintG * 0.85), (float)o.TintB);
					UAlpha((float)(o.A / 255.0 * o.XrayAlpha));
					Gl.ActiveTexture(TextureUnit.Texture0); Gl.BindTexture(TextureTarget.Texture2D, bt.Tex);
					Gl.DrawArrays(PrimitiveType.Triangles, bt.Start, (uint)bt.Count);
				}
				Gl.DepthFunc(DepthFunction.Lequal);
			}
		}

		// an object, or the part of its batches within the vertex range [from, to) (a piece of the transparent queue)
		private void DrawObj(Lua3DScene s, SceneObject o, int litScene, int from = 0, int to = int.MaxValue) {
			var c = EnsureObj(o);
			if (c.Batches.Count == 0) return;
			Bind3D(c.Vbo);
			UModel(o.Transform);
			UTint((float)o.TintR, (float)o.TintG, (float)o.TintB);
			UAlpha(o.A / 255.0f); UCutout(1);   // alpha-test: free for opaque texels, makes VRM hair/lashes cut out
			ULit((litScene == 1 && o.Lit) ? 1 : 0);
			UEmissive((float)o.EmR, (float)o.EmG, (float)o.EmB);
			if (o.FlatR >= 0) UFlatCol((float)o.FlatR, (float)o.FlatG, (float)o.FlatB, 1f);
			else UFlatCol(0f, 0f, 0f, 0f);
			if (o.SplatSprite >= 0) { DrawSplatObj(s, o, c); return; }   // terrain splat: its own shader mode
			// per-object depth bias (polygon offset): lets a coplanar decal/marking sit cleanly on its surface
			// instead of z-fighting it (road stripes on the road, ground shadows, etc.).
			if (o.DepthBias != 0f) { Gl.Enable(EnableCap.PolygonOffsetFill); Gl.PolygonOffset(o.DepthBias, o.DepthBias); }
			float fr = (float)(o.R / 255.0), fg = (float)(o.G / 255.0), fb = (float)(o.B / 255.0);
			foreach (var bt in c.Batches) {
				int a = Math.Max(bt.Start, from), b = Math.Min(bt.Start + bt.Count, to);
				if (a >= b) continue;
				uint gt = 0;
				if (bt.Mode != 0) { Gl.ActiveTexture(TextureUnit.Texture0); gt = bt.Pal ? PaletteTex() : TexFor(s, bt.TexId); if (gt == 0) continue; }
				UMode(bt.Mode);
				if (bt.Mode == 0) UFlat(fr, fg, fb);
				else { BindNormalFor(s, bt.TexId); Gl.ActiveTexture(TextureUnit.Texture0); Gl.BindTexture(TextureTarget.Texture2D, gt); }
				Gl.DrawArrays(PrimitiveType.Triangles, a, (uint)(b - a));
			}
			if (o.DepthBias != 0f) Gl.Disable(EnableCap.PolygonOffsetFill);
		}
		// terrain floor with splat shading: bind the four layer textures + the weight sprite and draw
		// the whole object in ONE call (per-quad texture batching is irrelevant — the shader blends)
		private void DrawSplatObj(Lua3DScene s, SceneObject o, ObjCache c) {
			uint t1 = TexFor(s, o.LayerTex1), t2 = TexFor(s, o.LayerTex2), t3 = TexFor(s, o.LayerTex3), t4 = TexFor(s, o.LayerTex4);
			uint sp = SpriteFor(s, o.SplatSprite);
			if (t1 == 0 || sp == 0) return;
			UMode(5);
			Gl.Uniform1(_uTex2, 2); Gl.Uniform1(_uTex3, 3); Gl.Uniform1(_uTex4, 4); Gl.Uniform1(_uSplat, 5);
			Gl.Uniform2(_uSplatSize, o.SplatW, o.SplatH);
			Gl.Uniform1(_uTexScale, o.LayerUvScale);
			Gl.ActiveTexture(TextureUnit.Texture2); Gl.BindTexture(TextureTarget.Texture2D, t2 != 0 ? t2 : t1);
			Gl.ActiveTexture(TextureUnit.Texture3); Gl.BindTexture(TextureTarget.Texture2D, t3 != 0 ? t3 : t1);
			Gl.ActiveTexture(TextureUnit.Texture4); Gl.BindTexture(TextureTarget.Texture2D, t4 != 0 ? t4 : t1);
			Gl.ActiveTexture(TextureUnit.Texture5); Gl.BindTexture(TextureTarget.Texture2D, sp);
			Gl.ActiveTexture(TextureUnit.Texture0); Gl.BindTexture(TextureTarget.Texture2D, t1);
			int total = 0; foreach (var bt in c.Batches) total += bt.Count;
			Gl.DrawArrays(PrimitiveType.Triangles, 0, (uint)total);
		}

		private void Prune(Lua3DScene s) {
			if (_cache.Count <= s._objects.Count) return;
			_live.Clear(); foreach (var v in s._objects.Values) _live.Add(v);
			List<SceneObject> dead = null;
			foreach (var kv in _cache) if (!_live.Contains(kv.Key)) { (dead ??= new()).Add(kv.Key); FreeBuffers(kv.Value); }
			if (dead != null) foreach (var o in dead) _cache.Remove(o);
		}
	}
}
