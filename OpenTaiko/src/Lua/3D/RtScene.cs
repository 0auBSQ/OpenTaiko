using System;
using System.Collections.Generic;

namespace OpenTaiko {
	/// <summary>
	/// The path tracers' snapshot of a <see cref="Lua3DScene"/>, shared by the CPU <see cref="Raytracer"/> and the
	/// <see cref="GpuRaytracer"/>. Every visible quad, triangle or sprite object becomes a block of object-space
	/// triangles with its own bounding volume hierarchy (BVH), placed in the world by an instance (transform, material,
	/// flat colour, tint, colour override, emissive, alpha cut); a top-level BVH over the instances' world boxes finds
	/// the objects a ray can reach. Texels whose alpha is under the instance's alpha cut are holes: rays pass them.
	///
	/// <see cref="Update"/> rebuilds only what changed: an object's triangles and BVH when its geometry changes (a refit
	/// when the triangle count stays the same) and a camera-facing sprite object's when the camera turns, the instances
	/// and the top level when objects move or change material,
	/// the primitive, material and light arrays when they change, and the texture slots when textures do. The arrays are
	/// flat floats in the layout the GPU kernel reads (<see cref="Geo"/>, <see cref="Top"/>, <see cref="Mats"/>,
	/// <see cref="Lights"/>), and the CPU tracer walks the same arrays with <see cref="ClosestTri"/> and <see cref="AnyTri"/>,
	/// which follow the kernel's closestTri and anyTri line by line.
	/// </summary>
	internal sealed class RtScene {
		// float strides (the GPU kernel's consts are built from these)
		public const int TRI = 18;    // A(3) e1(3) e2(3) uv0..2(6) shade(1) texture slot(1) |e1 x e2|(1), object space
		public const int NODE = 8;    // box min(3) max(3); a leaf's first item and item count, or an inner node's left child and 0
		public const int INST = 38;   // world->object rows(12) object->world linear rows(9) node base(1) triangle base(1) material(1) flat rgb(3) normal sign(1)
		                              // tint rgb(3) colour override rgb(3, r < 0: none) emissive rgb(3) alpha cut(1)
		public const int PRM = 16;    // kind(1) c(3) abc(3) min(3) max(3) preset(1) material(1) SDF bounding radius(1)
		public const int MTL = 12;    // type(1) rgb(3) rough(1) ior(1) emis(3) texture slot(1) nmap(1) normal-map slot(1)
		public const int LGT = 6;     // pos(3) col(3)
		// node depth limits: a traversal stack of their sum never overflows (each level pushes at most one node)
		public const int TlasDepth = 16, BlasDepth = 32, Stack = TlasDepth + BlasDepth;
		/// <summary>A ray this close to parallel to a triangle (|cos| of the angle to its normal) misses it.</summary>
		public const double DetEps = 1e-7;
		/// <summary>Box exits are pushed out by this factor, past float rounding's relative error, so a box the ray
		/// touches is never culled.</summary>
		public const double BoxSlack = 1.000001;
		/// <summary>The "no hit" distance.</summary>
		public const double NoHit = 1e30;
		private const int Bins = 16, MaxLeaf = 8;

		/// <summary>[bottom-level nodes of every object][triangles]; the triangles start at float <see cref="TriOff"/>.</summary>
		public float[] Geo = new float[NODE];
		public int GeoLen, TriOff, NumTri;
		/// <summary>[primitives][instances in top-level order][top-level nodes].</summary>
		public float[] Top = new float[PRM];
		public int TopLen, NumPrim, NumInst, InstOff, TlasOff;
		public float[] Mats = new float[MTL], Lights = new float[LGT];
		public int NumMat, NumLight;
		/// <summary>A primitive is a ray-marched SDF / a material or a texture uses a normal map / a texture or sprite
		/// has holes: the GPU kernel compiles that code only then.</summary>
		public bool HasSdf, HasNormalMaps, HasCutout;

		// ── changes since the consumer last called ClearChanges ──
		/// <summary>The whole of <see cref="Geo"/> moved (layout change); otherwise <see cref="GeoRanges"/> lists the
		/// float ranges that changed in place.</summary>
		public bool GeoFull;
		public readonly List<(int off, int len)> GeoRanges = new();
		public bool TopChanged, MatsChanged, LightsChanged;
		/// <summary>The set of used textures, or the registration or size of one, changed: the atlas is packed again.</summary>
		public bool TexLayoutChanged;
		/// <summary>Slots whose pixels changed at the same size.</summary>
		public readonly List<int> TexDirty = new();
		/// <summary>A slot's cut mode (<see cref="SlotCut"/>) or normal map (<see cref="SlotNormal"/>) changed.</summary>
		public bool SlotInfoChanged;
		public bool Changed => GeoFull || GeoRanges.Count > 0 || TopChanged || MatsChanged || LightsChanged || TexLayoutChanged || TexDirty.Count > 0 || SlotInfoChanged;
		/// <summary>Bottom-level builds and refits done so far.</summary>
		public int Builds, Refits;

		public void ClearChanges() {
			GeoFull = false; GeoRanges.Clear(); TopChanged = MatsChanged = LightsChanged = TexLayoutChanged = SlotInfoChanged = false; TexDirty.Clear();
		}
		/// <summary>Flag every array as changed (the consumer lost its copies).</summary>
		public void MarkAllChanged() {
			GeoFull = TopChanged = MatsChanged = LightsChanged = TexLayoutChanged = SlotInfoChanged = true; GeoRanges.Clear(); TexDirty.Clear();
		}

		// ── texture slots: kernels see slots, never ids (ids can be anywhere in int range) ──
		// a slot is given to every texture id a triangle or material names and every sprite id a sprite names,
		// registered or not; it never moves while it exists, so the triangle blocks that store it stay valid. Unregistered
		// or unused slots have no pixels. A sprite's key is its id plus SpriteKey, so texture and sprite ids never meet.
		private const long SpriteKey = 1L << 32;
		private readonly List<long> _slotIds = new();
		private readonly Dictionary<long, int> _slotOf = new();
		private int[]?[] _slotPix = Array.Empty<int[]?>();
		private int[] _slotW = Array.Empty<int>(), _slotH = Array.Empty<int>(), _slotRev = Array.Empty<int>(), _slotNorm = Array.Empty<int>();
		private byte[] _slotCut = Array.Empty<byte>();
		private bool[] _slotUsed = Array.Empty<bool>();
		private int _usedSlots;
		public int SlotCount => _slotIds.Count;
		/// <summary>Pixels of a used, registered slot (packed RGB for a texture, whose negative texels are holes; ARGB for
		/// a sprite), or null.</summary>
		public int[]? SlotPixels(int slot, out int w, out int h) {
			if (slot < 0 || slot >= _slotIds.Count || _slotPix[slot] == null) { w = h = 0; return null; }
			w = _slotW[slot]; h = _slotH[slot]; return _slotPix[slot];
		}
		/// <summary>Whether the slot holds a sprite (ARGB pixels) rather than a texture.</summary>
		public bool SlotIsSprite(int slot) => slot >= 0 && slot < _slotIds.Count && _slotIds[slot] >= SpriteKey;
		/// <summary>How hits on a used slot's texels are tested: 0 never cut, 1 cut where the texel's alpha is under the
		/// instance's alpha cut, 2 always cut (a sprite that is not registered).</summary>
		public int SlotCut(int slot) => slot >= 0 && slot < _slotIds.Count && slot < _slotUsed.Length && _slotUsed[slot] ? _slotCut[slot] : 0;
		/// <summary>The slot of the normal map SetTextureNormalMap gave a used texture slot, or -1.</summary>
		public int SlotNormal(int slot) => slot >= 0 && slot < _slotIds.Count && slot < _slotUsed.Length && _slotUsed[slot] ? _slotNorm[slot] : -1;
		/// <summary>The slot of texture <paramref name="id"/>, or -1.</summary>
		public int SlotIndex(int id) => id >= 0 && _slotOf.TryGetValue(id, out int k) ? k : -1;
		private int SlotOf(int id) => id < 0 ? -1 : SlotOfKey(id);
		private int SpriteSlotOf(int id) => id < 0 ? -1 : SlotOfKey(SpriteKey | (long)id);
		private int SlotOfKey(long key) {
			if (_slotOf.TryGetValue(key, out int k)) return k;
			k = _slotIds.Count; _slotIds.Add(key); _slotOf[key] = k;
			return k;
		}

		// ── per-object blocks ──
		private sealed class Entry {
			public SceneObject? Obj;
			public int GeomVersion, Kind = -1, N;
			public bool Baked, Live; public int Pass;
			public readonly double[] Xf = new double[12];   // the transform a baked block was built with
			public bool XfNull;                             // ... none (a sprite block)
			public bool Billboard;                          // the block holds camera-facing sprites, built facing Cam
			public readonly double[] Cam = new double[6];
			public float[] Nodes = Array.Empty<float>(); public int NodeCount;
			public float[] Tris = Array.Empty<float>(); public int TriCount;
			public int[] Order = Array.Empty<int>();
			public double Cost0;          // node area sum of the last full build (a refit that grows it too much rebuilds)
			public long[] TexIds = Array.Empty<long>();   // slot keys
			public int NodeBase, TriBase;
			public bool Dirty;            // block rebuilt in place since it was copied into Geo
			public readonly float[] Inst = new float[INST];
			public readonly float[] Box = new float[6];   // world box
		}
		private readonly Dictionary<int, Entry> _objs = new();
		private readonly List<Entry> _live = new();
		private int _pass;
		private bool _built;
		private float[] _src = Array.Empty<float>(), _boxes = Array.Empty<float>();
		private float[] _primBuf = Array.Empty<float>(), _instBoxes = Array.Empty<float>(), _tlas = Array.Empty<float>();
		private int[] _instOrder = Array.Empty<int>();
		private readonly HashSet<long> _texSet = new();
		private readonly double[] _cam = { 1, 0, 0, 0, 1, 0 };   // camera right and up axes, for the sprites that face it

		internal bool Update(Lua3DScene s) {
			Span<double> cam = stackalloc double[6] { s._Rx, s._Ry, s._Rz, s._Ux, s._Uy, s._Uz };
			return Update(s._objects, s._primitives, s._materials, s._lights, s._tex, s._sprites, cam);
		}

		/// <summary>Bring the arrays up to date with the scene data; the change flags say what moved. Returns
		/// <see cref="Changed"/>. <paramref name="tex"/> and <paramref name="sprites"/> hold the registered textures (with
		/// their SetTextureNormalMap normal maps) and sprites, and <paramref name="cam"/> is the camera's right and up
		/// axes (Rx, Ry, Rz, Ux, Uy, Uz) that camera-facing sprites turn to.</summary>
		internal bool Update(Dictionary<int, SceneObject> objects, List<ScenePrimitive> prims, List<SceneMaterial> mats,
			List<SceneLight> lights, Dictionary<int, TexEntry> tex, Dictionary<int, TexEntry>? sprites = null,
			ReadOnlySpan<double> cam = default) {
			if (_slotIds.Count > 64 && _slotIds.Count > 4 * _usedSlots) ResetSlots();   // drop slots of textures no longer used
			bool first = !_built; _built = true;
			if (first) MarkAllChanged();
			if (cam.Length >= 6) cam.Slice(0, 6).CopyTo(_cam);
			_pass++;
			bool layout = first;
			// ── objects: build or refit the blocks whose geometry changed ──
			foreach (var kv in objects) {
				var o = kv.Value;
				int tris = TriCount(o);
				if (tris == 0) continue;
				if (!_objs.TryGetValue(kv.Key, out var e)) { e = new Entry(); _objs[kv.Key] = e; }
				e.Pass = _pass; e.Obj = o;
				// sprites are built in the world: the transform moves their centres, the camera turns them
				bool baked = o.Kind == 3 || (o.Transform != null && !Invertible(o.Transform));
				if (e.NodeCount == 0 || e.GeomVersion != o.GeomVersion || e.Kind != o.Kind || e.N != o.N || e.Baked != baked
					|| (baked && !SameXf(e, o.Transform)) || (e.Billboard && !_cam.AsSpan().SequenceEqual(e.Cam))) {
					int nn = e.NodeCount, nt = e.TriCount;
					BuildBlock(e, o, baked, tris);
					if (!e.Live || nn != e.NodeCount || nt != e.TriCount) layout = true;
					else e.Dirty = true;
				}
				if (!e.Live) { e.Live = true; layout = true; }
			}
			List<int>? gone = null;
			foreach (var kv in _objs) {
				var e = kv.Value;
				if (e.Pass == _pass) continue;
				if (e.Live) { e.Live = false; layout = true; }
				if (!objects.ContainsKey(kv.Key)) (gone ??= new List<int>()).Add(kv.Key);   // deleted: hidden ones keep their block
			}
			if (gone != null) foreach (int k in gone) _objs.Remove(k);
			_live.Clear();
			foreach (var e in _objs.Values) if (e.Live) _live.Add(e);
			if (layout) Layout();
			else foreach (var e in _live) {
				if (!e.Dirty) continue;
				Array.Copy(e.Nodes, 0, Geo, e.NodeBase * NODE, e.NodeCount * NODE);
				Array.Copy(e.Tris, 0, Geo, TriOff + e.TriBase * TRI, e.TriCount * TRI);
				if (!GeoFull) { GeoRanges.Add((e.NodeBase * NODE, e.NodeCount * NODE)); GeoRanges.Add((TriOff + e.TriBase * TRI, e.TriCount * TRI)); }
				e.Dirty = false;
			}

			// ── materials (they name texture slots too) ──
			NumMat = mats.Count;
			bool nmaps = false;
			if (FillCompare(ref Mats, mats.Count * MTL, (f) => {
				for (int i = 0; i < mats.Count; i++) {
					var m = mats[i]; int b = i * MTL;
					f[b] = m.Type; f[b + 1] = (float)m.R; f[b + 2] = (float)m.G; f[b + 3] = (float)m.B;
					f[b + 4] = (float)m.Rough; f[b + 5] = (float)m.Ior;
					f[b + 6] = (float)m.ER; f[b + 7] = (float)m.EG; f[b + 8] = (float)m.EB;
					f[b + 9] = SlotOf(m.TexId); f[b + 10] = m.NormalMap; f[b + 11] = SlotOf(m.NormalTex);
				}
			})) MatsChanged = true;
			foreach (var m in mats) if (m.NormalMap != 0 || m.NormalTex >= 0) { nmaps = true; break; }

			// ── lights ──
			NumLight = lights.Count;
			if (FillCompare(ref Lights, lights.Count * LGT, (f) => {
				for (int i = 0; i < lights.Count; i++) {
					var l = lights[i]; int b = i * LGT;
					f[b] = (float)l.X; f[b + 1] = (float)l.Y; f[b + 2] = (float)l.Z;
					f[b + 3] = (float)l.R; f[b + 4] = (float)l.G; f[b + 5] = (float)l.B;
				}
			})) LightsChanged = true;

			UpdateTextures(mats, tex, sprites, out bool texNmaps);
			HasNormalMaps = nmaps || texNmaps;

			// ── primitives, instances and the top level ──
			bool sdf = false;
			int np = prims.Count;
			if (_primBuf.Length < np * PRM) _primBuf = new float[np * PRM + PRM * 8];
			for (int i = 0; i < np; i++) {
				var pr = prims[i]; int b = i * PRM;
				_primBuf[b] = pr.Kind; _primBuf[b + 1] = (float)pr.X; _primBuf[b + 2] = (float)pr.Y; _primBuf[b + 3] = (float)pr.Z;
				_primBuf[b + 4] = (float)pr.A; _primBuf[b + 5] = (float)pr.B; _primBuf[b + 6] = (float)pr.C;
				_primBuf[b + 7] = (float)pr.MinX; _primBuf[b + 8] = (float)pr.MinY; _primBuf[b + 9] = (float)pr.MinZ;
				_primBuf[b + 10] = (float)pr.MaxX; _primBuf[b + 11] = (float)pr.MaxY; _primBuf[b + 12] = (float)pr.MaxZ;
				_primBuf[b + 13] = pr.SdfPreset; _primBuf[b + 14] = pr.Material; _primBuf[b + 15] = (float)SdfBound(pr);
				if (pr.Kind >= 3) sdf = true;
			}
			HasSdf = sdf;
			bool top = np != NumPrim || !_primBuf.AsSpan(0, np * PRM).SequenceEqual(Top.AsSpan(0, Math.Min(np * PRM, Top.Length)));
			foreach (var e in _live) if (SetInstance(e)) top = true;
			if (layout) top = true;   // another set of live objects, or blocks at new offsets
			if (top || first) BuildTop(np);
			return Changed;
		}

		/// <summary>Radius around an SDF primitive's centre (Kind 3 torus, Kind 4 presets) that holds all of its surface,
		/// padded so a march that starts on it starts outside; 0 for the other kinds.</summary>
		internal static double SdfBound(ScenePrimitive p) {
			double a = Math.Abs(p.A), b = Math.Abs(p.B), c = Math.Abs(p.C), r;
			if (p.Kind == 3) r = a + b;
			else if (p.Kind != 4) return 0;
			else r = p.SdfPreset switch {
				1 => Math.Sqrt(a * a + b * b + c * c),   // rounded box: inside its half-extents
				2 or 3 => a + b,                         // torus (R + r), capsule (half-height + radius)
				4 => 2.2 * a,                            // gyroid: cut by a ball of 2.2 A
				_ => a,                                  // sphere, octahedron, gem (the unit gem fits in 0.83)
			};
			return r * 1.001 + 1e-4;
		}

		// ── traversal (the GPU kernel's closestTri / anyTri, line by line) ──
		// 1/d with no zero component, so a slab test never multiplies 0 by infinity
		private static double SafeInv(double v) => 1.0 / (v >= 0 ? Math.Max(v, 1e-20) : Math.Min(v, -1e-20));
		// entry distance of the ray into the box at f[b], or NoHit when it misses it within [tmin, tmax]
		private static double Slab(float[] f, int b, double ox, double oy, double oz, double ix, double iy, double iz, double tmin, double tmax) {
			double t0x = (f[b] - ox) * ix, t1x = (f[b + 3] - ox) * ix;
			double t0y = (f[b + 1] - oy) * iy, t1y = (f[b + 4] - oy) * iy;
			double t0z = (f[b + 2] - oz) * iz, t1z = (f[b + 5] - oz) * iz;
			double a = Math.Max(Math.Max(Math.Min(t0x, t1x), Math.Min(t0y, t1y)), Math.Max(Math.Min(t0z, t1z), tmin));
			double c = Math.Min(Math.Min(Math.Max(t0x, t1x), Math.Max(t0y, t1y)), Math.Min(Math.Max(t0z, t1z), tmax));
			return a <= c * BoxSlack ? a : NoHit;
		}
		// Moller-Trumbore against triangle k (object space; d need not be unit length, dl is its length), t in (tmin, tmax)
		internal bool TriHit(int k, double ox, double oy, double oz, double dx, double dy, double dz, double dl, double tmin, double tmax,
			out double t, out double u, out double v) {
			var g = Geo; int b = TriOff + k * TRI;
			double e1x = g[b + 3], e1y = g[b + 4], e1z = g[b + 5], e2x = g[b + 6], e2y = g[b + 7], e2z = g[b + 8];
			double px = dy * e2z - dz * e2y, py = dz * e2x - dx * e2z, pz = dx * e2y - dy * e2x;
			double det = e1x * px + e1y * py + e1z * pz;
			t = u = v = 0;
			if (Math.Abs(det) <= DetEps * dl * g[b + 17]) return false;
			double idet = 1.0 / det;
			double tx = ox - g[b], ty = oy - g[b + 1], tz = oz - g[b + 2];
			u = (tx * px + ty * py + tz * pz) * idet; if (u < 0 || u > 1) return false;
			double qx = ty * e1z - tz * e1y, qy = tz * e1x - tx * e1z, qz = tx * e1y - ty * e1x;
			v = (dx * qx + dy * qy + dz * qz) * idet; if (v < 0 || u + v > 1) return false;
			t = (e2x * qx + e2y * qy + e2z * qz) * idet;
			return t > tmin && t < tmax;
		}
		// the world ray in instance ib's own space
		internal static void InstRay(float[] f, int ib, double ox, double oy, double oz, double dx, double dy, double dz,
			out double lox, out double loy, out double loz, out double ldx, out double ldy, out double ldz) {
			lox = f[ib] * ox + f[ib + 1] * oy + f[ib + 2] * oz + f[ib + 3];
			loy = f[ib + 4] * ox + f[ib + 5] * oy + f[ib + 6] * oz + f[ib + 7];
			loz = f[ib + 8] * ox + f[ib + 9] * oy + f[ib + 10] * oz + f[ib + 11];
			ldx = f[ib] * dx + f[ib + 1] * dy + f[ib + 2] * dz;
			ldy = f[ib + 4] * dx + f[ib + 5] * dy + f[ib + 6] * dz;
			ldz = f[ib + 8] * dx + f[ib + 9] * dy + f[ib + 10] * dz;
		}
		private bool EmissiveMat(int m) => m >= 0 && m < NumMat && (int)Mats[m * MTL] == 3;

		/// <summary>Whether the hit at barycentrics (u, v) of triangle k lands on a hole of its texture or sprite: a texel
		/// whose alpha is under <paramref name="cut"/> (the instance's alpha cut), or any texel of a sprite that is not
		/// registered. The GPU kernel's cutOut.</summary>
		internal bool CutOut(int k, double u, double v, double cut) {
			var g = Geo; int b = TriOff + k * TRI;
			int slot = (int)g[b + 16];
			int mode = SlotCut(slot);
			if (mode == 0) return false;
			if (mode == 2) return true;
			var px = _slotPix[slot]!; int w = _slotW[slot], h = _slotH[slot];
			double c = 1 - u - v;
			double tu = g[b + 9] * c + g[b + 11] * u + g[b + 13] * v, tv = g[b + 10] * c + g[b + 12] * u + g[b + 14] * v;
			int p = px[Raytracer.WrapTexel(tv, h) * w + Raytracer.WrapTexel(tu, w)];
			double a = _slotIds[slot] >= SpriteKey ? ((p >> 24) & 255) / 255.0 : (p < 0 ? 0.0 : 1.0);
			return a < cut;
		}

		/// <summary>The nearest triangle a world-space ray hits with t in (tmin, tBest): on a hit tBest becomes its
		/// distance, inst the instance (in top-level order), tri the triangle (index into the triangle region) and
		/// u, v the barycentrics.</summary>
		internal bool ClosestTri(double ox, double oy, double oz, double dx, double dy, double dz, double tmin, ref double tBest,
			out int hInst, out int hTri, out double hu, out double hv) {
			hInst = -1; hTri = -1; hu = 0; hv = 0;
			if (NumInst == 0) return false;
			var top = Top; var geo = Geo;
			Span<int> st = stackalloc int[Stack]; int sp = 0, node = 0;
			double ix = SafeInv(dx), iy = SafeInv(dy), iz = SafeInv(dz);
			while (true) {
				int b = TlasOff + node * NODE;
				int cnt = (int)top[b + 7];
				if (cnt > 0) {
					int first = (int)top[b + 6];
					for (int k = 0; k < cnt; k++) {
						int ib = InstOff + (first + k) * INST;
						InstRay(top, ib, ox, oy, oz, dx, dy, dz, out double lox, out double loy, out double loz, out double ldx, out double ldy, out double ldz);
						double lix = SafeInv(ldx), liy = SafeInv(ldy), liz = SafeInv(ldz);
						double dl = Math.Sqrt(ldx * ldx + ldy * ldy + ldz * ldz);
						int nb = (int)top[ib + 21], tb = (int)top[ib + 22];
						double cut = top[ib + 37];
						if (Slab(geo, nb * NODE, lox, loy, loz, lix, liy, liz, tmin, tBest) >= NoHit) continue;
						int bas = sp, bn = 0;
						while (true) {
							int c = (nb + bn) * NODE;
							int bc = (int)geo[c + 7], bf = (int)geo[c + 6];
							if (bc > 0) {
								for (int j = 0; j < bc; j++) {
									if (TriHit(tb + bf + j, lox, loy, loz, ldx, ldy, ldz, dl, tmin, tBest, out double tt, out double uu, out double vv)
										&& !CutOut(tb + bf + j, uu, vv, cut)) {
										tBest = tt; hInst = first + k; hTri = tb + bf + j; hu = uu; hv = vv;
									}
								}
							} else {
								double d1 = Slab(geo, (nb + bf) * NODE, lox, loy, loz, lix, liy, liz, tmin, tBest);
								double d2 = Slab(geo, (nb + bf + 1) * NODE, lox, loy, loz, lix, liy, liz, tmin, tBest);
								int n1 = bf, n2 = bf + 1;
								if (d2 < d1) { (d1, d2) = (d2, d1); n1 = bf + 1; n2 = bf; }
								if (d1 < NoHit) {
									if (d2 < NoHit && sp < Stack) st[sp++] = n2;
									bn = n1; continue;
								}
							}
							if (sp == bas) break;
							bn = st[--sp];
						}
					}
				} else {
					int l = (int)top[b + 6];
					double d1 = Slab(top, TlasOff + l * NODE, ox, oy, oz, ix, iy, iz, tmin, tBest);
					double d2 = Slab(top, TlasOff + (l + 1) * NODE, ox, oy, oz, ix, iy, iz, tmin, tBest);
					int n1 = l, n2 = l + 1;
					if (d2 < d1) { (d1, d2) = (d2, d1); n1 = l + 1; n2 = l; }
					if (d1 < NoHit) {
						if (d2 < NoHit && sp < Stack) st[sp++] = n2;
						node = n1; continue;
					}
				}
				if (sp == 0) break;
				node = st[--sp];
			}
			return hInst >= 0;
		}

		/// <summary>Whether a world-space ray hits any triangle with t in (tmin, tmax). The holes of textures and sprites
		/// let it through, and with <paramref name="passEmissive"/> so do instances with an emissive material (on a ray
		/// toward a light they stand for).</summary>
		internal bool AnyTri(double ox, double oy, double oz, double dx, double dy, double dz, double tmin, double tmax, bool passEmissive) {
			if (NumInst == 0) return false;
			var top = Top; var geo = Geo;
			Span<int> st = stackalloc int[Stack]; int sp = 0, node = 0;
			double ix = SafeInv(dx), iy = SafeInv(dy), iz = SafeInv(dz);
			while (true) {
				int b = TlasOff + node * NODE;
				int cnt = (int)top[b + 7];
				if (cnt > 0) {
					int first = (int)top[b + 6];
					for (int k = 0; k < cnt; k++) {
						int ib = InstOff + (first + k) * INST;
						if (passEmissive && EmissiveMat((int)top[ib + 23])) continue;
						InstRay(top, ib, ox, oy, oz, dx, dy, dz, out double lox, out double loy, out double loz, out double ldx, out double ldy, out double ldz);
						double lix = SafeInv(ldx), liy = SafeInv(ldy), liz = SafeInv(ldz);
						double dl = Math.Sqrt(ldx * ldx + ldy * ldy + ldz * ldz);
						int nb = (int)top[ib + 21], tb = (int)top[ib + 22];
						double cut = top[ib + 37];
						if (Slab(geo, nb * NODE, lox, loy, loz, lix, liy, liz, tmin, tmax) >= NoHit) continue;
						int bas = sp, bn = 0;
						while (true) {
							int c = (nb + bn) * NODE;
							int bc = (int)geo[c + 7], bf = (int)geo[c + 6];
							if (bc > 0) {
								for (int j = 0; j < bc; j++)
									if (TriHit(tb + bf + j, lox, loy, loz, ldx, ldy, ldz, dl, tmin, tmax, out _, out double uu, out double vv)
										&& !CutOut(tb + bf + j, uu, vv, cut)) return true;
							} else {
								bool h1 = Slab(geo, (nb + bf) * NODE, lox, loy, loz, lix, liy, liz, tmin, tmax) < NoHit;
								bool h2 = Slab(geo, (nb + bf + 1) * NODE, lox, loy, loz, lix, liy, liz, tmin, tmax) < NoHit;
								if (h1 || h2) {
									if (h1 && h2 && sp < Stack) st[sp++] = bf + 1;
									bn = h1 ? bf : bf + 1; continue;
								}
							}
							if (sp == bas) break;
							bn = st[--sp];
						}
					}
				} else {
					int l = (int)top[b + 6];
					bool h1 = Slab(top, TlasOff + l * NODE, ox, oy, oz, ix, iy, iz, tmin, tmax) < NoHit;
					bool h2 = Slab(top, TlasOff + (l + 1) * NODE, ox, oy, oz, ix, iy, iz, tmin, tmax) < NoHit;
					if (h1 || h2) {
						if (h1 && h2 && sp < Stack) st[sp++] = l + 1;
						node = h1 ? l : l + 1; continue;
					}
				}
				if (sp == 0) break;
				node = st[--sp];
			}
			return false;
		}

		// write n floats with fill into a scratch copy and swap it in when they differ from arr
		private float[] _cmp = Array.Empty<float>();
		private bool FillCompare(ref float[] arr, int n, Action<float[]> fill) {
			if (_cmp.Length < Math.Max(1, n)) _cmp = new float[Math.Max(1, n)];
			fill(_cmp);
			if (arr.Length >= n && _cmp.AsSpan(0, n).SequenceEqual(arr.AsSpan(0, n)) && arr.Length < 2 * n + 64) return false;
			if (arr.Length < Math.Max(1, n) || arr.Length >= 2 * n + 64) arr = new float[Math.Max(1, n)];
			Array.Copy(_cmp, arr, n);
			return true;
		}

		private void ResetSlots() {
			_slotIds.Clear(); _slotOf.Clear();
			Array.Clear(_slotPix); Array.Clear(_slotUsed);      // every slot is seen afresh
			foreach (var e in _objs.Values) e.NodeCount = 0;   // every block stores slots: rebuild them all
			TexLayoutChanged = true;
		}

		private void UpdateTextures(List<SceneMaterial> mats, Dictionary<int, TexEntry> tex, Dictionary<int, TexEntry>? sprites,
			out bool texNmaps) {
			int n0 = _slotIds.Count;
			var used = new bool[n0];
			foreach (var e in _live) foreach (long key in e.TexIds) used[_slotOf[key]] = true;
			foreach (var m in mats) {
				if (m.TexId >= 0) used[_slotOf[m.TexId]] = true;
				if (m.NormalTex >= 0) used[_slotOf[m.NormalTex]] = true;
			}
			// the normal maps SetTextureNormalMap gave the used textures (their slots may be new)
			var norm = new int[n0];
			texNmaps = false;
			for (int k = 0; k < n0; k++) {
				norm[k] = -1;
				long key = _slotIds[k];
				if (used[k] && key < SpriteKey && tex.TryGetValue((int)key, out var te) && te.Normal >= 0) {
					norm[k] = SlotOf(te.Normal); texNmaps = true;
				}
			}
			int n = _slotIds.Count;
			if (n > n0) Array.Resize(ref used, n);
			for (int k = 0; k < n0; k++) if (norm[k] >= 0) used[norm[k]] = true;
			if (_slotPix.Length < n) {
				int cap = Math.Max(n, _slotPix.Length * 2 + 8);
				Array.Resize(ref _slotPix, cap); Array.Resize(ref _slotW, cap); Array.Resize(ref _slotH, cap);
				Array.Resize(ref _slotRev, cap); Array.Resize(ref _slotUsed, cap); Array.Resize(ref _slotCut, cap);
				Array.Resize(ref _slotNorm, cap);
				TexLayoutChanged = true;
			}
			_usedSlots = 0;
			bool cutout = false;
			for (int k = 0; k < n; k++) {
				long key = _slotIds[k]; bool sprite = key >= SpriteKey; int id = (int)(key & 0xFFFFFFFFL);
				int[]? px = null; int w = 0, h = 0, rev = 0;
				if (used[k]) {
					_usedSlots++;
					var reg = sprite ? sprites : tex;
					if (reg != null && reg.TryGetValue(id, out var te) && te.Pix != null) { px = te.Pix; w = te.W; h = te.H; rev = te.Rev; }
				}
				bool fresh = used[k] != _slotUsed[k] || !ReferenceEquals(px, _slotPix[k]) || rev != _slotRev[k] || w != _slotW[k] || h != _slotH[k];
				if (used[k] != _slotUsed[k] || (px == null) != (_slotPix[k] == null) || w != _slotW[k] || h != _slotH[k]) TexLayoutChanged = true;
				else if (px != null && (!ReferenceEquals(px, _slotPix[k]) || rev != _slotRev[k])) TexDirty.Add(k);
				byte mode = fresh ? CutMode(used[k], sprite, px, w * h) : _slotCut[k];
				int nm = k < n0 ? norm[k] : -1;
				if (mode != _slotCut[k] || nm != _slotNorm[k]) SlotInfoChanged = true;
				if (mode != 0) cutout = true;
				_slotUsed[k] = used[k]; _slotPix[k] = px; _slotW[k] = w; _slotH[k] = h; _slotRev[k] = rev; _slotCut[k] = mode; _slotNorm[k] = nm;
			}
			for (int k = n; k < _slotPix.Length; k++) {
				if (_slotUsed[k] || _slotPix[k] != null) TexLayoutChanged = true;
				_slotUsed[k] = false; _slotPix[k] = null; _slotW[k] = _slotH[k] = _slotRev[k] = 0; _slotCut[k] = 0; _slotNorm[k] = -1;
			}
			if (TexLayoutChanged) TexDirty.Clear();
			HasCutout = cutout;
		}

		// a used slot's cut mode (see SlotCut): a texture with negative texels or a sprite with texels that are not fully
		// opaque has holes; a sprite with no pixels is never hit
		private static byte CutMode(bool used, bool sprite, int[]? px, int n) {
			if (!used) return 0;
			if (px == null) return (byte)(sprite ? 2 : 0);
			if (sprite) { for (int i = 0; i < n; i++) if ((uint)px[i] < 0xFF000000u) return 1; }
			else { for (int i = 0; i < n; i++) if (px[i] < 0) return 1; }
			return 0;
		}

		// ── one object's block ──
		private static float Down(double v) { float f = (float)v; return f > v ? MathF.BitDecrement(f) : f; }
		private static float Up(double v) { float f = (float)v; return f < v ? MathF.BitIncrement(f) : f; }
		/// <summary>A colour channel (0..1) as linear light, through gamma 2.2 like the GPU kernel's pow; below 0 counts as 0.</summary>
		internal static double Srgb(double c) => Math.Pow(c < 0 ? 0 : c, 2.2);

		// triangle j of the source list: corners, uvs, shade, slot; its box (outward-rounded around what is stored) goes to _boxes
		private void Emit(int j, double ax, double ay, double az, double bx, double by, double bz, double cx, double cy, double cz,
			double u0, double v0, double u1, double v1, double u2, double v2, double shade, int slot) {
			var f = _src; int b = j * TRI, q = j * 6;
			double e1x = bx - ax, e1y = by - ay, e1z = bz - az, e2x = cx - ax, e2y = cy - ay, e2z = cz - az;
			if (!(double.IsFinite(ax + ay + az + e1x + e1y + e1z + e2x + e2y + e2z))) {
				Array.Clear(f, b, TRI); f[b + 16] = -1;   // a never-hit triangle (zero area) at the origin
				Array.Clear(_boxes, q, 6);
				return;
			}
			f[b] = (float)ax; f[b + 1] = (float)ay; f[b + 2] = (float)az;
			f[b + 3] = (float)e1x; f[b + 4] = (float)e1y; f[b + 5] = (float)e1z;
			f[b + 6] = (float)e2x; f[b + 7] = (float)e2y; f[b + 8] = (float)e2z;
			f[b + 9] = (float)u0; f[b + 10] = (float)v0; f[b + 11] = (float)u1; f[b + 12] = (float)v1; f[b + 13] = (float)u2; f[b + 14] = (float)v2;
			f[b + 15] = (float)shade; f[b + 16] = slot;
			// the triangle as the intersection tests see it: the stored corner plus the stored edges
			double sx = f[b], sy = f[b + 1], sz = f[b + 2];
			double px = sx + f[b + 3], py = sy + f[b + 4], pz = sz + f[b + 5], rx = sx + f[b + 6], ry = sy + f[b + 7], rz = sz + f[b + 8];
			double nx = (double)f[b + 4] * f[b + 8] - (double)f[b + 5] * f[b + 7];
			double ny = (double)f[b + 5] * f[b + 6] - (double)f[b + 3] * f[b + 8];
			double nz = (double)f[b + 3] * f[b + 7] - (double)f[b + 4] * f[b + 6];
			f[b + 17] = (float)Math.Sqrt(nx * nx + ny * ny + nz * nz);
			var bx2 = _boxes;
			bx2[q] = Down(Math.Min(sx, Math.Min(px, rx))); bx2[q + 1] = Down(Math.Min(sy, Math.Min(py, ry))); bx2[q + 2] = Down(Math.Min(sz, Math.Min(pz, rz)));
			bx2[q + 3] = Up(Math.Max(sx, Math.Max(px, rx))); bx2[q + 4] = Up(Math.Max(sy, Math.Max(py, ry))); bx2[q + 5] = Up(Math.Max(sz, Math.Max(pz, rz)));
		}

		private static void Xf(double[]? t, double x, double y, double z, out double ox, out double oy, out double oz) {
			if (t == null) { ox = x; oy = y; oz = z; return; }
			ox = t[0] * x + t[1] * y + t[2] * z + t[3];
			oy = t[4] * x + t[5] * y + t[6] * z + t[7];
			oz = t[8] * x + t[9] * y + t[10] * z + t[11];
		}

		// the triangles an object gives the tracers: 0 when it is hidden or empty, or a sprite object faded past the
		// alpha cut. Ground-flat sprites (billboard 3) and blended sprite quads are shadow and decal stand-ins, and
		// sprites without a sprite id draw nothing: none of them is traced (the tracers cast real shadows).
		private static int TriCount(SceneObject o) {
			if (!o.Visible || o.N <= 0 || o.Kind < 0 || o.Kind > 4) return 0;
			if (o.Kind == 2) return o.N;
			if (o.Kind < 3) return o.N * 2;
			if (AlphaCut(o) > 1) return 0;
			int n = 0; var d = o.D;
			if (o.Kind == 3) {
				for (int i = 0; i < o.N; i++) {
					int k = i * PrimLayout.SpriteStride;
					if ((int)d[k + PrimLayout.SpriteBillboard] != 3 && (int)d[k + PrimLayout.SpriteId] >= 0) n += 2;
				}
			} else {
				for (int i = 0; i < o.N; i++) {
					int k = i * PrimLayout.SpriteQuadStride;
					if ((int)d[k + PrimLayout.SpriteQuadId] >= 0 && d[k + PrimLayout.SpriteQuadCutout] != 0) n += 2;
				}
			}
			return n;
		}

		// the alpha a texel needs to be hit: the rasterizer's alpha test, which for sprites scales the texel alpha by the
		// object's alpha (ObjSetPass)
		private static double AlphaCut(SceneObject o) => o.Kind >= 3 ? 0.5 * 255.0 / Math.Max(o.A, 1) : 0.5;

		private void BuildBlock(Entry e, SceneObject o, bool baked, int nt) {
			if (_src.Length < nt * TRI) _src = new float[nt * TRI];
			if (_boxes.Length < nt * 6) _boxes = new float[nt * 6];
			_texSet.Clear();
			e.Billboard = false;
			var d = o.D; var t = baked ? o.Transform : null;
			if (o.Kind == 3) {
				// a sprite stands with its foot at its centre (moved by the transform) and turns as the rasterizer turns it:
				// fixed in the XY plane (billboard 0), upright facing the camera (1), or facing it fully (2)
				var cam = _cam; int j = 0;
				for (int i = 0; i < o.N; i++) {
					int k = i * PrimLayout.SpriteStride, sid = (int)d[k + PrimLayout.SpriteId], bb = (int)d[k + PrimLayout.SpriteBillboard];
					if (bb == 3 || sid < 0) continue;
					_texSet.Add(SpriteKey | (long)sid);
					int slot = SpriteSlotOf(sid);
					Xf(o.Transform, d[k], d[k + 1], d[k + 2], out double cx, out double cy, out double cz);
					double rx = 1, ry = 0, rz = 0, ux = 0, uy = 1, uz = 0;
					if (bb == 2) { rx = cam[0]; ry = cam[1]; rz = cam[2]; ux = cam[3]; uy = cam[4]; uz = cam[5]; e.Billboard = true; }
					else if (bb == 1) {
						rx = cam[0]; rz = cam[2]; double rl = Math.Sqrt(rx * rx + rz * rz);
						if (rl > 1e-6) { rx /= rl; rz /= rl; }
						e.Billboard = true;
					}
					double hw = d[k + PrimLayout.SpriteW] * 0.5, hh = d[k + PrimLayout.SpriteH];
					double ax = cx - rx * hw, ay = cy - ry * hw, az = cz - rz * hw, bx = cx + rx * hw, by = cy + ry * hw, bz = cz + rz * hw;
					double tx = ux * hh, ty = uy * hh, tz = uz * hh;
					Emit(j++, ax, ay, az, bx, by, bz, bx + tx, by + ty, bz + tz, 0, 1, 1, 1, 1, 0, 1, slot);
					Emit(j++, ax, ay, az, bx + tx, by + ty, bz + tz, ax + tx, ay + ty, az + tz, 0, 1, 1, 0, 0, 0, 1, slot);
				}
				if (e.Billboard) Array.Copy(cam, e.Cam, 6);
			} else if (o.Kind == 4) {
				// a sprite on four given corners, mapped like a textured quad over the whole sprite (cut-out ones only)
				int j = 0;
				for (int i = 0; i < o.N; i++) {
					int k = i * PrimLayout.SpriteQuadStride, sid = (int)d[k + PrimLayout.SpriteQuadId];
					if (sid < 0 || d[k + PrimLayout.SpriteQuadCutout] == 0) continue;
					_texSet.Add(SpriteKey | (long)sid);
					int slot = SpriteSlotOf(sid);
					Xf(t, d[k], d[k + 1], d[k + 2], out double x0, out double y0, out double z0);
					Xf(t, d[k + 3], d[k + 4], d[k + 5], out double x1, out double y1, out double z1);
					Xf(t, d[k + 6], d[k + 7], d[k + 8], out double x2, out double y2, out double z2);
					Xf(t, d[k + 9], d[k + 10], d[k + 11], out double x3, out double y3, out double z3);
					Emit(j++, x0, y0, z0, x1, y1, z1, x2, y2, z2, 0, 1, 1, 1, 1, 0, 1, slot);
					Emit(j++, x0, y0, z0, x2, y2, z2, x3, y3, z3, 0, 1, 1, 0, 0, 0, 1, slot);
				}
			} else if (o.Kind == 0) {
				for (int i = 0; i < o.N; i++) {
					int k = i * PrimLayout.TexQuadStride; int texId = (int)d[k + PrimLayout.TexQuadTex];
					if (texId >= 0) _texSet.Add(texId);
					int slot = SlotOf(texId);
					double uMax = d[k + PrimLayout.TexQuadUMax], vMax = d[k + PrimLayout.TexQuadVMax], shade = d[k + PrimLayout.TexQuadShade];
					Xf(t, d[k], d[k + 1], d[k + 2], out double x0, out double y0, out double z0);
					Xf(t, d[k + 3], d[k + 4], d[k + 5], out double x1, out double y1, out double z1);
					Xf(t, d[k + 6], d[k + 7], d[k + 8], out double x2, out double y2, out double z2);
					Xf(t, d[k + 9], d[k + 10], d[k + 11], out double x3, out double y3, out double z3);
					Emit(2 * i, x0, y0, z0, x1, y1, z1, x2, y2, z2, 0, vMax, uMax, vMax, uMax, 0, shade, slot);
					Emit(2 * i + 1, x0, y0, z0, x2, y2, z2, x3, y3, z3, 0, vMax, uMax, 0, 0, 0, shade, slot);
				}
			} else if (o.Kind == 1) {
				for (int i = 0; i < o.N; i++) {
					int k = i * PrimLayout.FlatQuadStride;
					Xf(t, d[k], d[k + 1], d[k + 2], out double x0, out double y0, out double z0);
					Xf(t, d[k + 3], d[k + 4], d[k + 5], out double x1, out double y1, out double z1);
					Xf(t, d[k + 6], d[k + 7], d[k + 8], out double x2, out double y2, out double z2);
					Xf(t, d[k + 9], d[k + 10], d[k + 11], out double x3, out double y3, out double z3);
					Emit(2 * i, x0, y0, z0, x1, y1, z1, x2, y2, z2, 0, 0, 0, 0, 0, 0, 1, -1);
					Emit(2 * i + 1, x0, y0, z0, x2, y2, z2, x3, y3, z3, 0, 0, 0, 0, 0, 0, 1, -1);
				}
			} else {
				for (int i = 0; i < o.N; i++) {
					int k = i * PrimLayout.TexTriStride; int texId = (int)d[k + PrimLayout.TexTriTex];
					if (texId >= 0) _texSet.Add(texId);
					Xf(t, d[k], d[k + 1], d[k + 2], out double x0, out double y0, out double z0);
					Xf(t, d[k + 5], d[k + 6], d[k + 7], out double x1, out double y1, out double z1);
					Xf(t, d[k + 10], d[k + 11], d[k + 12], out double x2, out double y2, out double z2);
					Emit(i, x0, y0, z0, x1, y1, z1, x2, y2, z2, d[k + 3], d[k + 4], d[k + 8], d[k + 9], d[k + 13], d[k + 14], d[k + PrimLayout.TexTriShade], SlotOf(texId));
				}
			}
			// same triangles in the same order as last time: refit the tree, unless that made it much worse
			bool refit = e.NodeCount > 0 && e.TriCount == nt && e.Kind == o.Kind && e.Baked == baked;
			if (refit) {
				Refit(e.Nodes, e.NodeCount, _boxes, e.Order);
				if (NodeAreaSum(e.Nodes, e.NodeCount) > 1.5 * e.Cost0) refit = false;
				else Refits++;
			}
			if (!refit) {
				if (e.Order.Length < nt) e.Order = new int[nt];
				e.NodeCount = Build(_boxes, nt, e.Order, ref e.Nodes, BlasDepth);
				e.Cost0 = NodeAreaSum(e.Nodes, e.NodeCount);
				Builds++;
			}
			if (e.Tris.Length < nt * TRI) e.Tris = new float[nt * TRI];
			for (int j = 0; j < nt; j++) Array.Copy(_src, e.Order[j] * TRI, e.Tris, j * TRI, TRI);
			e.TriCount = nt; e.Kind = o.Kind; e.N = o.N; e.GeomVersion = o.GeomVersion; e.Baked = baked;
			e.XfNull = o.Transform == null;
			if (baked && !e.XfNull) Array.Copy(o.Transform!, e.Xf, 12);
			e.TexIds = _texSet.Count == 0 ? Array.Empty<long>() : new long[_texSet.Count];
			_texSet.CopyTo(e.TexIds);
		}

		private void Layout() {
			int nodes = 0, tris = 0;
			foreach (var e in _live) { e.NodeBase = nodes; e.TriBase = tris; nodes += e.NodeCount; tris += e.TriCount; }
			TriOff = nodes * NODE; GeoLen = TriOff + tris * TRI; NumTri = tris;
			if (Geo.Length < Math.Max(1, GeoLen) || Geo.Length > 2 * GeoLen + 1024) Geo = new float[Math.Max(1, GeoLen + GeoLen / 4)];
			foreach (var e in _live) {
				Array.Copy(e.Nodes, 0, Geo, e.NodeBase * NODE, e.NodeCount * NODE);
				Array.Copy(e.Tris, 0, Geo, TriOff + e.TriBase * TRI, e.TriCount * TRI);
				e.Dirty = false;
			}
			GeoFull = true; GeoRanges.Clear();
		}

		// a transform the tracers can invert: finite, with no direction squashed flat
		private static bool Invertible(double[] t) {
			double det = t[0] * (t[5] * t[10] - t[6] * t[9]) - t[1] * (t[4] * t[10] - t[6] * t[8]) + t[2] * (t[4] * t[9] - t[5] * t[8]);
			double r0 = Math.Sqrt(t[0] * t[0] + t[1] * t[1] + t[2] * t[2]), r1 = Math.Sqrt(t[4] * t[4] + t[5] * t[5] + t[6] * t[6]);
			double r2 = Math.Sqrt(t[8] * t[8] + t[9] * t[9] + t[10] * t[10]);
			for (int i = 0; i < 12; i++) if (!double.IsFinite(t[i])) return false;
			return Math.Abs(det) > 1e-6 * r0 * r1 * r2;
		}
		// whether a baked block was built with this transform (null: none)
		private static bool SameXf(Entry e, double[]? t) {
			if (t == null || e.XfNull) return t == null && e.XfNull;
			for (int i = 0; i < 12; i++) if (e.Xf[i] != t[i]) return false;
			return true;
		}

		// the instance of a live block: its transform both ways, block offsets, material, the object's colours (flat
		// colour, tint, colour override, emissive; the colours linear like every tracer colour), alpha cut and world box;
		// true when any of it changed
		private bool SetInstance(Entry e) {
			var o = e.Obj!;
			Span<float> f = stackalloc float[INST];
			Span<float> box = stackalloc float[6];
			var t = e.Baked ? null : o.Transform;
			double a = 1, b = 0, c = 0, tx = 0, d = 0, g = 1, h = 0, ty = 0, p = 0, q = 0, r = 1, tz = 0;
			if (t != null) { a = t[0]; b = t[1]; c = t[2]; tx = t[3]; d = t[4]; g = t[5]; h = t[6]; ty = t[7]; p = t[8]; q = t[9]; r = t[10]; tz = t[11]; }
			// inverse of the linear part: adjugate over determinant
			double i00 = g * r - h * q, i01 = -(b * r - c * q), i02 = b * h - c * g;
			double i10 = -(d * r - h * p), i11 = a * r - c * p, i12 = -(a * h - c * d);
			double i20 = d * q - g * p, i21 = -(a * q - b * p), i22 = a * g - b * d;
			double det = a * i00 + b * i10 + c * i20, inv = 1.0 / det;
			i00 *= inv; i01 *= inv; i02 *= inv; i10 *= inv; i11 *= inv; i12 *= inv; i20 *= inv; i21 *= inv; i22 *= inv;
			f[0] = (float)i00; f[1] = (float)i01; f[2] = (float)i02; f[3] = (float)-(i00 * tx + i01 * ty + i02 * tz);
			f[4] = (float)i10; f[5] = (float)i11; f[6] = (float)i12; f[7] = (float)-(i10 * tx + i11 * ty + i12 * tz);
			f[8] = (float)i20; f[9] = (float)i21; f[10] = (float)i22; f[11] = (float)-(i20 * tx + i21 * ty + i22 * tz);
			f[12] = (float)a; f[13] = (float)b; f[14] = (float)c; f[15] = (float)d; f[16] = (float)g; f[17] = (float)h;
			f[18] = (float)p; f[19] = (float)q; f[20] = (float)r;
			f[21] = e.NodeBase; f[22] = e.TriBase; f[23] = o.Material;
			f[24] = (float)Srgb(o.R / 255.0); f[25] = (float)Srgb(o.G / 255.0); f[26] = (float)Srgb(o.B / 255.0);
			f[27] = det < 0 ? -1 : 1;   // a mirroring transform flips the winding normal
			f[28] = (float)Srgb(o.TintR); f[29] = (float)Srgb(o.TintG); f[30] = (float)Srgb(o.TintB);
			if (o.FlatR >= 0) { f[31] = (float)Srgb(o.FlatR); f[32] = (float)Srgb(o.FlatG); f[33] = (float)Srgb(o.FlatB); }
			else { f[31] = -1; f[32] = f[33] = 0; }
			f[34] = (float)Math.Max(0, o.EmR); f[35] = (float)Math.Max(0, o.EmG); f[36] = (float)Math.Max(0, o.EmB);
			f[37] = (float)AlphaCut(o);
			// world box: the root box's centre moved, its half-extents through the absolute linear part
			var nb = e.Nodes;
			double cx = (nb[0] + (double)nb[3]) * 0.5, cy = (nb[1] + (double)nb[4]) * 0.5, cz = (nb[2] + (double)nb[5]) * 0.5;
			double ex = (nb[3] - (double)nb[0]) * 0.5, ey = (nb[4] - (double)nb[1]) * 0.5, ez = (nb[5] - (double)nb[2]) * 0.5;
			double wx = a * cx + b * cy + c * cz + tx, wy = d * cx + g * cy + h * cz + ty, wz = p * cx + q * cy + r * cz + tz;
			double hx = Math.Abs(a) * ex + Math.Abs(b) * ey + Math.Abs(c) * ez;
			double hy = Math.Abs(d) * ex + Math.Abs(g) * ey + Math.Abs(h) * ez;
			double hz = Math.Abs(p) * ex + Math.Abs(q) * ey + Math.Abs(r) * ez;
			box[0] = Down(wx - hx); box[1] = Down(wy - hy); box[2] = Down(wz - hz);
			box[3] = Up(wx + hx); box[4] = Up(wy + hy); box[5] = Up(wz + hz);
			if (f.SequenceEqual(e.Inst) && box.SequenceEqual(e.Box)) return false;
			f.CopyTo(e.Inst); box.CopyTo(e.Box);
			return true;
		}

		private void BuildTop(int np) {
			int ni = _live.Count;
			NumPrim = np; NumInst = ni; InstOff = np * PRM;
			if (_instBoxes.Length < ni * 6) _instBoxes = new float[ni * 6 + 48];
			if (_instOrder.Length < ni) _instOrder = new int[ni + 8];
			for (int i = 0; i < ni; i++) Array.Copy(_live[i].Box, 0, _instBoxes, i * 6, 6);
			int nn = ni > 0 ? Build(_instBoxes, ni, _instOrder, ref _tlas, TlasDepth) : 0;
			TlasOff = InstOff + ni * INST;
			TopLen = TlasOff + nn * NODE;
			if (Top.Length < Math.Max(1, TopLen) || Top.Length > 2 * TopLen + 1024) Top = new float[Math.Max(1, TopLen + TopLen / 4)];
			Array.Copy(_primBuf, 0, Top, 0, np * PRM);
			for (int i = 0; i < ni; i++) Array.Copy(_live[_instOrder[i]].Inst, 0, Top, InstOff + i * INST, INST);
			Array.Copy(_tlas, 0, Top, TlasOff, nn * NODE);
			TopChanged = true;
		}

		// ── BVH build ──
		/// <summary>Build a binned-SAH BVH over <paramref name="n"/> items whose boxes are boxes[i*6 .. i*6+5] (min xyz,
		/// max xyz). The nodes go to <paramref name="nodes"/> (grown as needed) in the <see cref="NODE"/> layout, root at 0,
		/// every inner node's children side by side and after it; <paramref name="order"/> lists the items in leaf order
		/// (a leaf's first item is a position in it). No node is deeper than <paramref name="maxDepth"/>. Returns the node count.</summary>
		internal static int Build(float[] boxes, int n, int[] order, ref float[] nodes, int maxDepth) {
			if (n <= 0) return 0;
			int cap = (2 * n - 1) * NODE;
			if (nodes.Length < cap) nodes = new float[cap];
			for (int i = 0; i < n; i++) order[i] = i;
			var cen = new float[n * 3];
			for (int i = 0; i < n; i++) {
				int q = i * 6;
				cen[i * 3] = (boxes[q] + boxes[q + 3]) * 0.5f; cen[i * 3 + 1] = (boxes[q + 1] + boxes[q + 4]) * 0.5f; cen[i * 3 + 2] = (boxes[q + 2] + boxes[q + 5]) * 0.5f;
			}
			Span<int> binN = stackalloc int[Bins];
			Span<float> binB = stackalloc float[Bins * 6];
			Span<double> rightA = stackalloc double[Bins];
			Span<int> rightN = stackalloc int[Bins];
			var work = new Stack<(int node, int start, int count, int depth)>();
			work.Push((0, 0, n, 0));
			int used = 1;
			while (work.Count > 0) {
				var (node, start, count, depth) = work.Pop();
				int b = node * NODE;
				float x0 = float.MaxValue, y0 = float.MaxValue, z0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue, z1 = float.MinValue;
				float cx0 = float.MaxValue, cy0 = float.MaxValue, cz0 = float.MaxValue, cx1 = float.MinValue, cy1 = float.MinValue, cz1 = float.MinValue;
				for (int i = start; i < start + count; i++) {
					int it = order[i], q = it * 6, c = it * 3;
					x0 = Math.Min(x0, boxes[q]); y0 = Math.Min(y0, boxes[q + 1]); z0 = Math.Min(z0, boxes[q + 2]);
					x1 = Math.Max(x1, boxes[q + 3]); y1 = Math.Max(y1, boxes[q + 4]); z1 = Math.Max(z1, boxes[q + 5]);
					cx0 = Math.Min(cx0, cen[c]); cy0 = Math.Min(cy0, cen[c + 1]); cz0 = Math.Min(cz0, cen[c + 2]);
					cx1 = Math.Max(cx1, cen[c]); cy1 = Math.Max(cy1, cen[c + 1]); cz1 = Math.Max(cz1, cen[c + 2]);
				}
				nodes[b] = x0; nodes[b + 1] = y0; nodes[b + 2] = z0; nodes[b + 3] = x1; nodes[b + 4] = y1; nodes[b + 5] = z1;
				int split = -1, axis = -1; double best = double.MaxValue;
				if (count > 2 && depth < maxDepth) {
					for (int a = 0; a < 3; a++) {
						float lo = a == 0 ? cx0 : a == 1 ? cy0 : cz0, hi = a == 0 ? cx1 : a == 1 ? cy1 : cz1;
						if (!(hi > lo)) continue;
						float scale = Bins / (hi - lo);
						binN.Clear();
						for (int k = 0; k < Bins; k++) { int p = k * 6; binB[p] = binB[p + 1] = binB[p + 2] = float.MaxValue; binB[p + 3] = binB[p + 4] = binB[p + 5] = float.MinValue; }
						for (int i = start; i < start + count; i++) {
							int it = order[i], k = BinOf(cen[it * 3 + a], lo, scale), p = k * 6, q = it * 6;
							binN[k]++;
							binB[p] = Math.Min(binB[p], boxes[q]); binB[p + 1] = Math.Min(binB[p + 1], boxes[q + 1]); binB[p + 2] = Math.Min(binB[p + 2], boxes[q + 2]);
							binB[p + 3] = Math.Max(binB[p + 3], boxes[q + 3]); binB[p + 4] = Math.Max(binB[p + 4], boxes[q + 4]); binB[p + 5] = Math.Max(binB[p + 5], boxes[q + 5]);
						}
						// sweep from the right: area and count of bins k..Bins-1
						float rx0 = float.MaxValue, ry0 = float.MaxValue, rz0 = float.MaxValue, rx1 = float.MinValue, ry1 = float.MinValue, rz1 = float.MinValue;
						int rn = 0;
						for (int k = Bins - 1; k > 0; k--) {
							int p = k * 6;
							if (binN[k] > 0) {
								rx0 = Math.Min(rx0, binB[p]); ry0 = Math.Min(ry0, binB[p + 1]); rz0 = Math.Min(rz0, binB[p + 2]);
								rx1 = Math.Max(rx1, binB[p + 3]); ry1 = Math.Max(ry1, binB[p + 4]); rz1 = Math.Max(rz1, binB[p + 5]);
							}
							rn += binN[k]; rightN[k] = rn; rightA[k] = rn > 0 ? HalfArea(rx0, ry0, rz0, rx1, ry1, rz1) : 0;
						}
						float lx0 = float.MaxValue, ly0 = float.MaxValue, lz0 = float.MaxValue, lx1 = float.MinValue, ly1 = float.MinValue, lz1 = float.MinValue;
						int ln = 0;
						for (int k = 0; k < Bins - 1; k++) {
							int p = k * 6;
							if (binN[k] > 0) {
								lx0 = Math.Min(lx0, binB[p]); ly0 = Math.Min(ly0, binB[p + 1]); lz0 = Math.Min(lz0, binB[p + 2]);
								lx1 = Math.Max(lx1, binB[p + 3]); ly1 = Math.Max(ly1, binB[p + 4]); lz1 = Math.Max(lz1, binB[p + 5]);
							}
							ln += binN[k];
							if (ln == 0 || rightN[k + 1] == 0) continue;
							double cost = ln * HalfArea(lx0, ly0, lz0, lx1, ly1, lz1) + rightN[k + 1] * rightA[k + 1];
							if (cost < best) { best = cost; axis = a; split = k + 1; }
						}
					}
				}
				int left = -1;
				if (axis >= 0 && (best < count * HalfArea(x0, y0, z0, x1, y1, z1) || count > MaxLeaf)) {
					float lo = axis == 0 ? cx0 : axis == 1 ? cy0 : cz0, hi = axis == 0 ? cx1 : axis == 1 ? cy1 : cz1;
					float scale = Bins / (hi - lo);
					int i = start, j = start + count - 1;
					while (i <= j) {
						if (BinOf(cen[order[i] * 3 + axis], lo, scale) < split) i++;
						else { (order[i], order[j]) = (order[j], order[i]); j--; }
					}
					left = i - start;
				} else if (axis < 0 && count > MaxLeaf && depth < maxDepth) {
					left = count / 2;   // the centroids coincide: halve by position to keep leaves small
				}
				if (left <= 0 || left >= count) {
					nodes[b + 6] = start; nodes[b + 7] = count;   // leaf
					continue;
				}
				int l = used; used += 2;
				nodes[b + 6] = l; nodes[b + 7] = 0;
				work.Push((l + 1, start + left, count - left, depth + 1));
				work.Push((l, start, left, depth + 1));
			}
			return used;
		}

		private static int BinOf(float c, float lo, float scale) {
			int k = (int)((c - lo) * scale);
			return k < 0 ? 0 : (k >= Bins ? Bins - 1 : k);
		}
		private static double HalfArea(double x0, double y0, double z0, double x1, double y1, double z1) {
			double dx = x1 - x0, dy = y1 - y0, dz = z1 - z0;
			return dx * dy + dy * dz + dz * dx;
		}
		// node box areas over the root's: how many boxes a ray through the root crosses on average (lower is better)
		private static double NodeAreaSum(float[] nodes, int count) {
			double s = 0;
			for (int i = 0; i < count; i++) { int b = i * NODE; s += HalfArea(nodes[b], nodes[b + 1], nodes[b + 2], nodes[b + 3], nodes[b + 4], nodes[b + 5]); }
			return s / Math.Max(HalfArea(nodes[0], nodes[1], nodes[2], nodes[3], nodes[4], nodes[5]), 1e-30);
		}

		/// <summary>Recompute the boxes of a tree built by <see cref="Build"/> for items that moved: leaves from their items'
		/// new boxes (boxes indexed by item, <paramref name="order"/> as Build left it), inner nodes from their children.</summary>
		internal static void Refit(float[] nodes, int count, float[] boxes, int[] order) {
			for (int i = count - 1; i >= 0; i--) {   // children always come after their parent
				int b = i * NODE, f = (int)nodes[b + 6], cnt = (int)nodes[b + 7];
				float x0 = float.MaxValue, y0 = float.MaxValue, z0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue, z1 = float.MinValue;
				if (cnt > 0) {
					for (int p = f; p < f + cnt; p++) {
						int q = order[p] * 6;
						x0 = Math.Min(x0, boxes[q]); y0 = Math.Min(y0, boxes[q + 1]); z0 = Math.Min(z0, boxes[q + 2]);
						x1 = Math.Max(x1, boxes[q + 3]); y1 = Math.Max(y1, boxes[q + 4]); z1 = Math.Max(z1, boxes[q + 5]);
					}
				} else {
					for (int c = f; c < f + 2; c++) {
						int q = c * NODE;
						x0 = Math.Min(x0, nodes[q]); y0 = Math.Min(y0, nodes[q + 1]); z0 = Math.Min(z0, nodes[q + 2]);
						x1 = Math.Max(x1, nodes[q + 3]); y1 = Math.Max(y1, nodes[q + 4]); z1 = Math.Max(z1, nodes[q + 5]);
					}
				}
				nodes[b] = x0; nodes[b + 1] = y0; nodes[b + 2] = z0; nodes[b + 3] = x1; nodes[b + 4] = y1; nodes[b + 5] = z1;
			}
		}
	}
}
