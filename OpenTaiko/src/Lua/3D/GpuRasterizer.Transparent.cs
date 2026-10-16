using System;
using System.Collections.Generic;
using Silk.NET.OpenGLES;

namespace OpenTaiko {
	// The transparent queue: transparent pieces and sprites back to front, particles placed between them.
	internal sealed partial class GpuRasterizer {
		// The transparent queue: the transparent objects (in pieces) and billboard sprite batches, back to front by
		// squared distance from the eye, with each particle placed between them (TQItem; the planes of the nearest
		// planar pieces place the particles beside them)
		private struct TQItem { public double Key; public int Seq, Piece, Start, Count, Spr; public SceneObject? Obj; public ObjCache? Cache; }
		private readonly List<TQItem> _tq = new(64);
		private double[] _tqKeys = new double[64];
		private readonly List<TPlane> _tqPlanes = new(16);
		private const int MAX_PIECES = 32, MAX_PLANES = 16;

		// ── transparent queue ──────────────────────────────────────────────────────────────────────────
		/// <summary>A piece of a transparent object for the transparent queue: a run of its primitives with one texture
		/// that lie in one plane, or the whole object. Its vertex range in the object's buffer, its box and, when planar,
		/// its plane (unit normal, offset), in the object's own space.</summary>
		internal struct Piece { public int Start, Count; public double MinX, MinY, MinZ, MaxX, MaxY, MaxZ; public bool Planar; public double Nx, Ny, Nz, D; }
		/// <summary>A planar queue item in world space: its index in the queue, its plane (unit normal, offset), the eye's
		/// signed distance to that plane, and the rectangle its part beyond the near plane covers on the camera plane at
		/// depth 1 (x/z, y/z).</summary>
		internal struct TPlane { public int Index; public double Nx, Ny, Nz, D, Side, X0, Y0, X1, Y1; }

		/// <summary>Split an object into the pieces the transparent queue sorts: runs of consecutive primitives with one
		/// texture whose corners lie in one plane (a pane of glass, a sheet of water). An object that would make more than
		/// <see cref="MAX_PIECES"/> runs stays one piece, planar when all its corners lie near one plane (rippled water).</summary>
		internal static void BuildPieces(SceneObject o, List<Piece> pieces) {
			pieces.Clear();
			if (o.Kind < 0 || o.Kind > 2 || o.N <= 0) return;
			var d = o.D;
			int stride = PrimLayout.Stride(o.Kind), corners = PrimLayout.Corners(o.Kind), step = PrimLayout.CornerStep(o.Kind);
			int verts = o.Kind == 2 ? 3 : 6, texAt = PrimLayout.TexAt(o.Kind);
			var cur = new Piece(); int curTex = 0;
			bool haveRef = false; double rnx = 0, rny = 0, rnz = 0, snx = 0, sny = 0, snz = 0;   // the whole object's area-weighted normal
			for (int i = 0; i < o.N; i++) {
				int k = i * stride, tex = texAt < 0 ? 0 : (int)d[k + texAt];
				// Newell's normal: twice the primitive's area along each axis
				double nx = 0, ny = 0, nz = 0;
				for (int a = 0; a < corners; a++) {
					int p = k + a * step, q = k + ((a + 1) % corners) * step;
					nx += (d[p + 1] - d[q + 1]) * (d[p + 2] + d[q + 2]);
					ny += (d[p + 2] - d[q + 2]) * (d[p] + d[q]);
					nz += (d[p] - d[q]) * (d[p + 1] + d[q + 1]);
				}
				double len = Math.Sqrt(nx * nx + ny * ny + nz * nz);
				bool flat = len > 1e-12;   // not degenerate
				if (flat) {
					if (!haveRef) { haveRef = true; rnx = nx; rny = ny; rnz = nz; }
					double sg = nx * rnx + ny * rny + nz * rnz < 0 ? -1 : 1;   // every normal on one side, so a surface sums up
					snx += sg * nx; sny += sg * ny; snz += sg * nz;
					nx /= len; ny /= len; nz /= len;
				}
				bool join = i > 0 && tex == curTex;
				if (join && flat) {
					if (!cur.Planar) SetPlane(ref cur, nx, ny, nz, d, k);
					else if (Math.Abs(nx * cur.Nx + ny * cur.Ny + nz * cur.Nz) < 0.999 || !OnPlane(cur, d, k, corners, step)) join = false;
				}
				if (!join) {
					if (i > 0) pieces.Add(cur);
					cur = new Piece { Start = i * verts, MinX = double.MaxValue, MinY = double.MaxValue, MinZ = double.MaxValue, MaxX = double.MinValue, MaxY = double.MinValue, MaxZ = double.MinValue };
					curTex = tex;
					if (flat) SetPlane(ref cur, nx, ny, nz, d, k);
				}
				for (int a = 0; a < corners; a++) Grow(ref cur, d, k + a * step);
				cur.Count += verts;
			}
			pieces.Add(cur);
			if (pieces.Count <= MAX_PIECES) return;
			// too many runs: one piece, planar when every corner lies within a small share of its size from one plane
			var all = new Piece { Start = 0, Count = o.N * verts, MinX = double.MaxValue, MinY = double.MaxValue, MinZ = double.MaxValue, MaxX = double.MinValue, MaxY = double.MinValue, MaxZ = double.MinValue };
			foreach (var p in pieces) {
				all.MinX = Math.Min(all.MinX, p.MinX); all.MinY = Math.Min(all.MinY, p.MinY); all.MinZ = Math.Min(all.MinZ, p.MinZ);
				all.MaxX = Math.Max(all.MaxX, p.MaxX); all.MaxY = Math.Max(all.MaxY, p.MaxY); all.MaxZ = Math.Max(all.MaxZ, p.MaxZ);
			}
			pieces.Clear();
			double sl = Math.Sqrt(snx * snx + sny * sny + snz * snz);
			if (sl > 1e-12) {
				double ux = snx / sl, uy = sny / sl, uz = snz / sl, lo = double.MaxValue, hi = double.MinValue;
				for (int i = 0; i < o.N; i++)
					for (int a = 0; a < corners; a++) {
						int q = i * stride + a * step;
						double t = ux * d[q] + uy * d[q + 1] + uz * d[q + 2];
						if (t < lo) lo = t; if (t > hi) hi = t;
					}
				double size = Math.Max(all.MaxX - all.MinX, Math.Max(all.MaxY - all.MinY, all.MaxZ - all.MinZ));
				if (hi - lo <= 0.03 * size + 0.02) { all.Planar = true; all.Nx = ux; all.Ny = uy; all.Nz = uz; all.D = (lo + hi) * 0.5; }
			}
			pieces.Add(all);
		}
		// the plane through a primitive's first corner with the primitive's unit normal
		private static void SetPlane(ref Piece p, double nx, double ny, double nz, double[] d, int k) {
			p.Planar = true; p.Nx = nx; p.Ny = ny; p.Nz = nz; p.D = nx * d[k] + ny * d[k + 1] + nz * d[k + 2];
		}
		// whether a primitive's corners lie in the piece's plane, within a tolerance that grows with the coordinates
		private static bool OnPlane(in Piece p, double[] d, int k, int corners, int step) {
			for (int a = 0; a < corners; a++) {
				int q = k + a * step;
				double x = d[q], y = d[q + 1], z = d[q + 2];
				if (!(Math.Abs(p.Nx * x + p.Ny * y + p.Nz * z - p.D) <= 1e-3 + 1e-4 * (Math.Abs(x) + Math.Abs(y) + Math.Abs(z)))) return false;
			}
			return true;
		}
		private static void Grow(ref Piece p, double[] d, int q) {
			double x = d[q], y = d[q + 1], z = d[q + 2];
			if (x < p.MinX) p.MinX = x; if (x > p.MaxX) p.MaxX = x;
			if (y < p.MinY) p.MinY = y; if (y > p.MaxY) p.MaxY = y;
			if (z < p.MinZ) p.MinZ = z; if (z > p.MaxZ) p.MaxZ = z;
		}
		private static List<Piece> PiecesOf(SceneObject o, ObjCache c) {
			if (c.PieceVersion != o.GeomVersion) { c.PieceVersion = o.GeomVersion; BuildPieces(o, c.Pieces); }
			return c.Pieces;
		}

		// This pass's transparent queue sorted back to front: the transparent objects by piece (the pieces in view) and the
		// transparent billboard sprite batches, each at the squared distance from the eye to its centre; the pieces of an
		// object with ObjSetBounds bounds all at the bounds' centre. Then the planes of the nearest planar pieces, which
		// place the particles beside them (GapOf).
		private void BuildQueue(Lua3DScene s) {
			_tq.Clear(); _tqPlanes.Clear();
			foreach (var o in _trans) {
				var c = EnsureObj(o);
				if (c.Batches.Count == 0) continue;
				o.WorldSphere(out double sx, out double sy, out double sz, out _);
				double whole = _view.DistSq(sx, sy, sz);
				if (o.SplatSprite >= 0) {   // terrain splat: drawn whole
					_tq.Add(new TQItem { Key = whole, Seq = _tq.Count, Obj = o, Cache = c, Piece = -1, Start = 0, Count = int.MaxValue, Spr = -1 });
					continue;
				}
				var pcs = PiecesOf(o, c);
				for (int i = 0; i < pcs.Count; i++) {
					var p = pcs[i];
					if (!PieceInView(o, p, out double cx, out double cy, out double cz)) continue;
					_tq.Add(new TQItem { Key = o.BoundsSet ? whole : _view.DistSq(cx, cy, cz), Seq = _tq.Count, Obj = o, Cache = c, Piece = i, Start = p.Start, Count = p.Count, Spr = -1 });
				}
			}
			for (int i = 0; i < _sprBatches.Count; i++) {
				var bt = _sprBatches[i];
				if (bt.Pass == 1 && bt.Obj.Kind == 3) _tq.Add(new TQItem { Key = bt.Key, Seq = _tq.Count, Piece = -1, Spr = i });
			}
			int n = _tq.Count;
			if (n == 0) return;
			_tq.Sort(_backToFront);
			if (!AnyParticles(s)) return;   // the keys and planes only place particles
			if (_tqKeys.Length < n) _tqKeys = new double[Math.Max(n, _tqKeys.Length * 2)];
			for (int i = 0; i < n; i++) _tqKeys[i] = _tq[i].Key;
			for (int i = n - 1; i >= 0 && _tqPlanes.Count < MAX_PLANES; i--) {
				var it = _tq[i];
				if (it.Piece < 0) continue;
				var p = it.Cache!.Pieces[it.Piece];
				if (p.Planar && PlaneOf(s, it.Obj!, p, i, out var pl)) _tqPlanes.Add(pl);
			}
		}
		private static readonly Comparison<TQItem> _backToFront = (a, b) => { int c = b.Key.CompareTo(a.Key); return c != 0 ? c : a.Seq.CompareTo(b.Seq); };
		private static bool AnyParticles(Lua3DScene s) {
			var systems = s._particleSystems;
			if (systems != null) foreach (var ps in systems) if (ps.Count > 0) return true;
			return false;
		}

		// the centre of a piece's box moved by the object's transform; false when that box lies out of view
		private bool PieceInView(SceneObject o, in Piece p, out double cx, out double cy, out double cz) {
			double x0 = p.MinX, y0 = p.MinY, z0 = p.MinZ, x1 = p.MaxX, y1 = p.MaxY, z1 = p.MaxZ;
			if (o.Transform != null) {
				x0 = y0 = z0 = double.MaxValue; x1 = y1 = z1 = double.MinValue;
				for (int k = 0; k < 8; k++) {
					XfPt(o.Transform, (k & 1) == 0 ? p.MinX : p.MaxX, (k & 2) == 0 ? p.MinY : p.MaxY, (k & 4) == 0 ? p.MinZ : p.MaxZ, out double x, out double y, out double z);
					if (x < x0) x0 = x; if (x > x1) x1 = x;
					if (y < y0) y0 = y; if (y > y1) y1 = y;
					if (z < z0) z0 = z; if (z > z1) z1 = z;
				}
			}
			cx = (x0 + x1) * 0.5; cy = (y0 + y1) * 0.5; cz = (z0 + z1) * 0.5;
			return _view.BoxInView(cx, cy, cz, (x1 - x0) * 0.5, (y1 - y0) * 0.5, (z1 - z0) * 0.5);
		}

		// a planar piece in world space (TPlane); false when the eye lies in its plane or the piece lies wholly at or
		// behind the near plane
		private bool PlaneOf(Lua3DScene s, SceneObject o, in Piece p, int index, out TPlane pl) {
			pl = default;
			double nx = p.Nx, ny = p.Ny, nz = p.Nz;
			XfPt(o.Transform, nx * p.D, ny * p.D, nz * p.D, out double px, out double py, out double pz);   // a point of the plane
			o.NormalToWorld(ref nx, ref ny, ref nz);
			double l = Math.Sqrt(nx * nx + ny * ny + nz * nz);
			if (!(l > 1e-12)) return false;
			nx /= l; ny /= l; nz /= l;
			double dd = nx * px + ny * py + nz * pz, side = nx * _view.Ex + ny * _view.Ey + nz * _view.Ez - dd;
			if (!(Math.Abs(side) > 1e-6)) return false;
			Span<double> cs = stackalloc double[24];   // the box corners in camera space
			for (int k = 0; k < 8; k++) {
				XfPt(o.Transform, (k & 1) == 0 ? p.MinX : p.MaxX, (k & 2) == 0 ? p.MinY : p.MaxY, (k & 4) == 0 ? p.MinZ : p.MaxZ, out double x, out double y, out double z);
				double rx = x - _view.Ex, ry = y - _view.Ey, rz = z - _view.Ez;
				cs[k * 3] = rx * s._Rx + ry * s._Ry + rz * s._Rz;
				cs[k * 3 + 1] = rx * s._Ux + ry * s._Uy + rz * s._Uz;
				cs[k * 3 + 2] = rx * _view.Fx + ry * _view.Fy + rz * _view.Fz;
			}
			if (!CameraRect(cs, s._near, out double x0, out double y0, out double x1, out double y1)) return false;
			pl = new TPlane { Index = index, Nx = nx, Ny = ny, Nz = nz, D = dd, Side = side, X0 = x0, Y0 = y0, X1 = x1, Y1 = y1 };
			return true;
		}
		/// <summary>The rectangle a box's part beyond the near plane covers on the camera plane at depth 1 (x/depth,
		/// y/depth). <paramref name="cs"/> holds the box's 8 corners in camera space (x, y, depth); bits 1, 2 and 4 of a
		/// corner's index put it at the far end of the box's first, second and third axis. False when no part of the box
		/// lies beyond the near plane.</summary>
		internal static bool CameraRect(ReadOnlySpan<double> cs, double near, out double x0, out double y0, out double x1, out double y1) {
			x0 = y0 = double.MaxValue; x1 = y1 = double.MinValue;
			bool any = false;
			for (int k = 0; k < 8; k++) {
				double x = cs[k * 3], y = cs[k * 3 + 1], d = cs[k * 3 + 2];
				bool front = d > near;
				if (front) { any = true; Fit(x / d, y / d, ref x0, ref y0, ref x1, ref y1); }
				for (int b = 1; b < 8; b <<= 1) {   // the edges to the corners at the far end of each axis, where they cross the near plane
					if ((k & b) != 0) continue;
					int j = (k | b) * 3;
					if (front == (cs[j + 2] > near)) continue;
					double t = (near - d) / (cs[j + 2] - d);
					Fit((x + t * (cs[j] - x)) / near, (y + t * (cs[j + 1] - y)) / near, ref x0, ref y0, ref x1, ref y1);
				}
			}
			return any;
		}
		private static void Fit(double u, double v, ref double x0, ref double y0, ref double x1, ref double y1) {
			if (u < x0) x0 = u; if (u > x1) x1 = u;
			if (v < y0) y0 = v; if (v > y1) y1 = v;
		}
		// a world point on the camera plane at depth 1 (x/z and y/z along the camera's right and up axes); false when it
		// lies at or behind the near plane
		private bool OnCameraPlane(Lua3DScene s, double x, double y, double z, out double u, out double v) {
			double rx = x - _view.Ex, ry = y - _view.Ey, rz = z - _view.Ez;
			double dz = rx * _view.Fx + ry * _view.Fy + rz * _view.Fz;
			u = v = 0;
			if (!(dz > s._near)) return false;
			u = (rx * s._Rx + ry * s._Ry + rz * s._Rz) / dz; v = (rx * s._Ux + ry * s._Uy + rz * s._Uz) / dz;
			return true;
		}

		/// <summary>How many of the queue's items draw before a particle: the items farther from the eye than its
		/// <paramref name="key"/> (its squared distance; <paramref name="keys"/> holds the queue's, far to near), then
		/// moved to the right side of every planar item whose rectangle its circle (<paramref name="u"/>,
		/// <paramref name="v"/>, radius <paramref name="r"/> on the camera plane) overlaps: after it when the particle is on
		/// the eye's side of its plane, before it otherwise. A negative radius skips the planes.</summary>
		internal static int GapOf(double[] keys, int n, ReadOnlySpan<TPlane> planes, double key, double x, double y, double z, double u, double v, double r) {
			int lo = 0, hi = n;
			while (lo < hi) { int m = (lo + hi) >> 1; if (keys[m] > key) lo = m + 1; else hi = m; }
			if (r < 0) return lo;
			int after = 0, before = n;
			foreach (ref readonly var pl in planes) {
				if (u + r < pl.X0 || u - r > pl.X1 || v + r < pl.Y0 || v - r > pl.Y1) continue;
				double sd = pl.Nx * x + pl.Ny * y + pl.Nz * z - pl.D;
				if (sd * pl.Side >= 0) { if (pl.Index + 1 > after) after = pl.Index + 1; }
				else if (pl.Index < before) before = pl.Index;
			}
			if (after > before) return before;   // the planes disagree (sorted out of order for this particle): draw before them
			return Math.Clamp(lo, after, before);
		}

		// The transparent queue back to front, each item after the particle runs placed before it. Expects the world
		// program and VAO bound, blending on and no depth write, and leaves them so.
		private void DrawQueue(Lua3DScene s, int litScene) {
			bool parts = _partRuns.Count > 0;   // whether the particle program is bound
			if (parts) BeginParticles(s);
			int r = 0, n = _tq.Count;
			for (int g = 0; ; g++) {
				for (; r < _partRuns.Count && _partRuns[r].Gap == g; r++) {
					if (!parts) { Gl.UseProgram(_progP); Gl.BindVertexArray(_vaoP); parts = true; }
					DrawParticleRun(s, _partRuns[r]);
				}
				if (g >= n) break;
				if (parts) { EndParticles(); parts = false; }
				var it = _tq[g];
				if (it.Obj != null) DrawObj(s, it.Obj, litScene, it.Start, it.Start + it.Count);
				else { Bind3D(_spriteVbo); DrawSprite(_sprBatches[it.Spr]); }
			}
			if (parts) EndParticles();
		}
	}
}
