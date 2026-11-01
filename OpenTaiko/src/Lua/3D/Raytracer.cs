using System;
using System.Threading.Tasks;

namespace OpenTaiko {
	/// <summary>
	/// Progressive Monte-Carlo path tracer for <see cref="Lua3DScene"/>. Renders the same triangles and sprites
	/// the rasterizer draws (the shared <see cref="RtScene"/> snapshot, walked through its two-level BVH, with the
	/// alpha-tested holes of textures and sprites) plus raytracer-only analytic primitives (sphere / plane / box) and
	/// ray-marched SDF surfaces (torus + presets), with point lights, the sun, the ambient light, emissive global
	/// illumination (emissive spheres sampled directly), and diffuse / GGX metal / glass / emissive materials.
	///
	/// Accumulates one sample per pixel each <see cref="Render"/> call into a float buffer; the
	/// image converges while the scene is still and resets (via the scene's _rtRevision) whenever the
	/// camera or any geometry / light / material changes — so it is grainy in motion and clean when
	/// stopped, and it stops tracing after <see cref="MaxSamples"/> samples. A camera move rebuilds only the camera-facing sprites.
	/// Rows are traced in 16-row tiles on every processor core (disjoint pixels ⇒ no races).
	/// </summary>
	internal sealed class Raytracer : IRenderer {
		// ── small double vector ─────────────────────────────────────────────────────────
		private readonly struct V3 {
			public readonly double X, Y, Z;
			public V3(double x, double y, double z) { X = x; Y = y; Z = z; }
			public static V3 operator +(V3 a, V3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
			public static V3 operator -(V3 a, V3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
			public static V3 operator *(V3 a, double s) => new(a.X * s, a.Y * s, a.Z * s);
			public static V3 operator *(V3 a, V3 b) => new(a.X * b.X, a.Y * b.Y, a.Z * b.Z);
			public double Dot(V3 b) => X * b.X + Y * b.Y + Z * b.Z;
			public V3 Cross(V3 b) => new(Y * b.Z - Z * b.Y, Z * b.X - X * b.Z, X * b.Y - Y * b.X);
			public double Len() => Math.Sqrt(X * X + Y * Y + Z * Z);
			public V3 Norm() { double l = Math.Sqrt(X * X + Y * Y + Z * Z); return l > 1e-18 ? new(X / l, Y / l, Z / l) : this; }
		}

		// ── hit record ────────────────────────────────────────────────────────────────────
		private struct Hit {
			public double T; public V3 P, N;
			public int Mat;
			public bool HasUV; public double U, V;
			public int Tex; public double Shade;   // texture slot of the triangle (untagged textured geometry), or -1
			public double FR, FG, FB;    // fallback albedo (linear) when Mat<0 && no texture
			public int Ti, Inst;         // triangle and instance hit (for tangent-space normal maps), or -1
			public int Prim;             // analytic primitive hit, or -1
		}

		private Lua3DScene _s = null!;
		private int _lastRevision = -1, _w, _h;
		private float[] _accum = Array.Empty<float>();   // length w*h*3, linear radiance sum
		private int _samples;

		private readonly RtScene _rt = new();
		private ScenePrimitive[] _prims = Array.Empty<ScenePrimitive>();
		private double[] _primR = Array.Empty<double>();   // SDF bounding radius per primitive
		private SceneMaterial[] _mats = Array.Empty<SceneMaterial>();
		private int[] _matTex = Array.Empty<int>(), _matNorm = Array.Empty<int>();   // texture slots per material
		private SceneLight[] _lights = Array.Empty<SceneLight>();
		private V3 _sunDir, _sunCol, _amb;

		internal const int MaxDepth = 8;
		// light samples nearer than 1 cm count as 1 cm away (the inverse square has no upper bound)
		private const double MinLightDist2 = 1e-4;
		/// <summary>Samples per pixel after which a still view stops tracing (the GPU tracer stops there too).</summary>
		internal const int MaxSamples = 4096;
		private const int Tile = 16;   // rows per parallel work item
		private readonly ParallelOptions _po = new() { MaxDegreeOfParallelism = Environment.ProcessorCount };
		// the canvas and its write count when this tracer last wrote it: a converged image is written again only
		// when something else drew over it
		private LuaCanvas? _shownCanvas; private int _shownWrites;

		/// <summary>How many samples per pixel have accumulated since the last reset (0 just after
		/// a camera/scene change). Lets the demo show convergence progress.</summary>
		public int SampleCount => _samples;

		public void Invalidate() { _lastRevision = -1; _samples = 0; _shownCanvas = null; }
		public void Dispose() { }
		public string Name => "CPU path tracer";
		public string Mode => "raytrace";
		public string TracerStatus(string fallbackNote) => fallbackNote.Length > 0 ? "CPU fallback: " + fallbackNote : "CPU path tracer";

		public void Render(Lua3DScene s) {
			_s = s;
			bool resize = _w != s._w || _h != s._h;
			if (resize) { _w = s._w; _h = s._h; _accum = new float[_w * _h * 3]; }
			if (resize || s._rtRevision != _lastRevision) {
				_lastRevision = s._rtRevision;
				_rt.Update(s); _rt.ClearChanges();   // rebuilds only what changed: the camera-facing sprites for a camera move
				Snapshot();
				Array.Clear(_accum, 0, _accum.Length);
				_samples = 0;
			}
			var cv = s._canvas;
			if (_samples < MaxSamples) { _samples++; Tiles(TraceRows); }
			else if (cv == _shownCanvas && cv._writes == _shownWrites) return;   // converged, and the canvas still shows it
			else Tiles(PresentRows);
			cv.MarkDirty(0, 0, _w - 1, _h - 1);
			_shownCanvas = cv; _shownWrites = cv._writes;
		}

		private void Tiles(Action<int, int, double> rows) {
			double inv = 1.0 / _samples;
			int n = (_h + Tile - 1) / Tile;
			Parallel.For(0, n, _po, ti => rows(ti * Tile, Math.Min(_h, ti * Tile + Tile), inv));
		}

		// Snapshot the primitives, materials, lights, sun and ambient once per reset (the triangles live in _rt).
		private void Snapshot() {
			_prims = _s._primitives.ToArray();
			_primR = new double[_prims.Length];
			for (int i = 0; i < _prims.Length; i++) _primR[i] = RtScene.SdfBound(_prims[i]);
			_mats = _s._materials.ToArray();
			_matTex = new int[_mats.Length]; _matNorm = new int[_mats.Length];
			for (int i = 0; i < _mats.Length; i++) { _matTex[i] = _rt.SlotIndex(_mats[i].TexId); _matNorm[i] = _rt.SlotIndex(_mats[i].NormalTex); }
			_lights = _s._lights.ToArray();
			var s = _s;
			_sunDir = new V3(s._sunX, s._sunY, s._sunZ); _sunCol = new V3(s._sunR, s._sunG, s._sunB); _amb = new V3(s._ambR, s._ambG, s._ambB);
		}

		// ── per-tile sampling ───────────────────────────────────────────────────────────
		private void TraceRows(int y0, int y1, double inv) {
			var s = _s;
			V3 eye = new(s._camX, s._camY, s._camZ);
			V3 R = new(s._Rx, s._Ry, s._Rz), U = new(s._Ux, s._Uy, s._Uz), F = new(s._Fx, s._Fy, s._Fz);
			double hw = _w * 0.5, hh = _h * 0.5, scale = s._scale;
			byte[] buf = s._canvas._buf;
			for (int py = y0; py < y1; py++) {
				int row = py * _w;
				for (int px = 0; px < _w; px++) {
					uint seed = Hash((uint)(row + px) * 2654435761u ^ (uint)_samples * 40503u) | 1u;   // xorshift stays at 0 from a 0 seed
					double jx = NextD(ref seed), jy = NextD(ref seed);
					double sx = (px + jx - hw) / scale;
					double sy = -(py + jy - hh) / scale;
					V3 dir = (R * sx + U * sy + F).Norm();
					V3 col = Trace(eye, dir, ref seed);
					if (!double.IsFinite(col.X + col.Y + col.Z)) col = new V3(0, 0, 0);   // a NaN would stay in the sum until the next reset

					int ai = (row + px) * 3;
					float ar = _accum[ai] + (float)col.X, ag = _accum[ai + 1] + (float)col.Y, ab = _accum[ai + 2] + (float)col.Z;
					_accum[ai] = ar; _accum[ai + 1] = ag; _accum[ai + 2] = ab;
					int o = (row + px) * 4;
					buf[o] = Out(ar * inv); buf[o + 1] = Out(ag * inv); buf[o + 2] = Out(ab * inv); buf[o + 3] = 255;
				}
			}
		}

		// the accumulated image into the canvas, without tracing
		private void PresentRows(int y0, int y1, double inv) {
			byte[] buf = _s._canvas._buf;
			for (int i = y0 * _w; i < y1 * _w; i++) {
				int ai = i * 3, o = i * 4;
				buf[o] = Out(_accum[ai] * inv); buf[o + 1] = Out(_accum[ai + 1] * inv); buf[o + 2] = Out(_accum[ai + 2] * inv); buf[o + 3] = 255;
			}
		}

		// ── path integrator ─────────────────────────────────────────────────────────────
		private V3 Trace(V3 o, V3 d, ref uint seed) {
			V3 L = new(0, 0, 0), thr = new(1, 1, 1);
			double tmin = RayEps(o);
			// the last diffuse vertex, whose DirectLight sampled the emissive spheres: its point and normals
			bool pd = false; V3 pp = default, pn = default, pnf = default;
			bool lit = false;                                         // a diffuse vertex on the path: rays escaping after it bring the ambient light
			for (int bounce = 0; bounce < MaxDepth; bounce++) {
				if (!Closest(o, d, tmin, out Hit h)) {
					L = L + thr * (lit ? Sky(d) + _amb : Sky(d)); break;   // the camera and its mirrors see the plain sky
				}
				Resolve(h, out int type, out V3 alb, out V3 emis, out double rough, out double ior, out int nmap, out int normTex, out bool dx);

				// the surface normal on the ray's side (ray offsets and the side checks use it) + glass inside/outside flag
				V3 ng = h.N; double cosI = -d.Dot(ng); bool inside = cosI < 0;
				V3 nf = inside ? ng * -1 : ng;
				V3 n = nf;                                            // the shading normal

				// normal mapping: a tangent-space texture (needs UVs) wins, made on the triangle's front; else a procedural preset.
				if (normTex >= 0 && h.Ti >= 0) { n = TexNormal(h, ng, normTex, dx); if (inside) n = n * -1; }
				else if (nmap != 0) { if (nmap == 1) alb = WoodAlbedo(h.P, alb); n = PerturbNormal(h.P, nf, nmap); }

				L = L + thr * emis * (pd && type == 3 && h.Prim >= 0 ? BounceWeight(h.Prim, pp, pn, pnf, d) : 1.0);
				if (type == 3) break;                                 // emissive: done

				V3 p = h.P; double e = RayEps(p); tmin = e;
				pd = false;
				if (type == 2) {                                      // glass (dielectric)
					double eta = inside ? ior : 1.0 / ior;
					double c = -d.Dot(n);
					if (c <= 0) { n = nf; c = Math.Abs(cosI); }       // a bent normal facing away from the ray: the surface's own
					double k = 1 - eta * eta * (1 - c * c);
					V3 refl = Above(Reflect(d, n), nf);
					if (k < 0) { o = p + nf * e; d = refl; }          // total internal reflection
					else {
						double cosT = Math.Sqrt(k);
						double r0 = (eta - 1) / (eta + 1); r0 *= r0;
						double cs = eta > 1 ? cosT : c;                   // Schlick takes the cosine on the thinner side
						double fr = r0 + (1 - r0) * Math.Pow(1 - cs, 5);
						if (NextD(ref seed) < fr) { o = p + nf * e; d = refl; }
						else { o = p - nf * e; d = Above((d * eta + n * (eta * c - cosT)).Norm() * -1, nf) * -1; }
					}
					thr = thr * alb;
				} else if (type == 1) {                               // metal (tinted reflection)
					if (d.Dot(n) >= 0) n = nf;                        // a bent normal facing away from the ray: the surface's own
					if (rough > 0) {                                  // GGX
						d = GgxReflect(d * -1, n, Math.Max(rough * rough, 1e-4), ref seed, out double w);
						if (w <= 0) break;
						thr = thr * alb * w;
					} else { d = Reflect(d, n); thr = thr * alb; }
					d = Above(d, nf); o = p + nf * e;
				} else {                                              // diffuse: NEE + cosine bounce
					L = L + thr * alb * DirectLight(p, n, nf, e, ref seed);
					d = Above(CosineHemisphere(n, ref seed), nf);
					pd = true; lit = true; pp = p; pn = n; pnf = nf;
					o = p + nf * e; thr = thr * alb;
				}

				if (bounce >= 3) {                                    // Russian roulette
					double q = Math.Max(thr.X, Math.Max(thr.Y, thr.Z));
					if (q < 1) { if (NextD(ref seed) > q) break; thr = thr * (1.0 / Math.Max(q, 1e-4)); }
				}
			}
			return L;
		}

		// Self-intersection offset at p: 1e-4 near the origin, growing with the coordinates as the float spacing does
		// (the geometry is stored in float). The GPU tracer's rayEps.
		private static double RayEps(V3 p) => Math.Max(1e-4, 1e-5 * Math.Max(Math.Abs(p.X), Math.Max(Math.Abs(p.Y), Math.Abs(p.Z))));

		// Next-event estimation for a diffuse hit at p with shading normal n and surface normal nf on the ray's side (e: the
		// offset at p): the point lights, the sun, and one direction toward each emissive sphere, drawn uniformly in the
		// cone the sphere fills and weighted against the cosine bounce that can reach it too (power heuristic, see
		// BounceWeight). The GPU tracer's directLight.
		private V3 DirectLight(V3 p, V3 n, V3 nf, double e, ref uint seed) {
			V3 sum = new(0, 0, 0), po = p + nf * e;
			for (int i = 0; i < _lights.Length; i++) {
				var lt = _lights[i];
				V3 to = new V3(lt.X, lt.Y, lt.Z) - p;
				double dist = to.Len(); if (dist < 1e-6) continue;
				V3 l = to * (1.0 / dist);
				double ndl = n.Dot(l); if (ndl <= 0 || nf.Dot(l) <= 0) continue;
				if (Occluded(po, l, e, dist - 20 * e, true)) continue;
				double inv = ndl / (Math.PI * Math.Max(dist * dist, MinLightDist2));
				sum = sum + new V3(lt.R, lt.G, lt.B) * inv;
			}
			if (_sunCol.X + _sunCol.Y + _sunCol.Z > 0) {          // the sun: a direction, its shadow ray runs to infinity
				double ndl = n.Dot(_sunDir);
				if (ndl > 0 && nf.Dot(_sunDir) > 0 && !Occluded(po, _sunDir, e, RtScene.NoHit, false)) sum = sum + _sunCol * ndl;
			}
			for (int i = 0; i < _prims.Length; i++) {
				var pr = _prims[i];
				if (pr.Kind != 0 || !Emissive(pr.Material)) continue;
				V3 c = new(pr.X, pr.Y, pr.Z);
				double omc = ConeOmc(p, c, pr.A); if (omc <= 0) continue;
				V3 w = (c - p).Norm(); Frame(w, out V3 tu, out V3 tv);
				double om = NextD(ref seed) * omc, ph = 2 * Math.PI * NextD(ref seed), st = Math.Sqrt(Math.Max(0, om * (2 - om)));
				V3 l = (tu * (st * Math.Cos(ph)) + tv * (st * Math.Sin(ph)) + w * (1 - om)).Norm();
				double ndl = n.Dot(l); if (ndl <= 0 || nf.Dot(l) <= 0) continue;
				V3 oc = p - c; double bq = oc.Dot(l), tl = -bq - Math.Sqrt(Math.Max(bq * bq - (oc.Dot(oc) - pr.A * pr.A), 0));
				if (Occluded(po, l, e, tl - e, true)) continue;
				// emission * (cos/pi) / pdf, times the light's weight pl^2/(pl^2+pb^2), with q = pb/pl
				double q = 2 * ndl * omc;
				var m = _mats[pr.Material];
				sum = sum + new V3(m.ER, m.EG, m.EB) * (q / (1 + q * q));
			}
			return sum;
		}

		// The share of the emission that a cosine bounce from the diffuse vertex (pp, normals pn, pnf) in direction d finds
		// on primitive i, when DirectLight there sampled it too (an emissive sphere): pb^2/(pb^2+pl^2). 1 for a direction
		// DirectLight cannot draw.
		private double BounceWeight(int i, V3 pp, V3 pn, V3 pnf, V3 d) {
			var pr = _prims[i];
			if (pr.Kind != 0) return 1;
			double omc = ConeOmc(pp, new V3(pr.X, pr.Y, pr.Z), pr.A), ndl = pn.Dot(d);
			if (omc <= 0 || ndl <= 0 || pnf.Dot(d) <= 0) return 1;
			double q = 2 * ndl * omc;
			return q * q / (1 + q * q);
		}

		/// <summary>1 - cos of the half angle of the cone that the sphere (c, r) fills seen from p; 0 from inside it.</summary>
		private static double ConeOmc(V3 p, V3 c, double r) {
			V3 v = c - p; double d2 = v.Dot(v), r2 = r * r;
			if (d2 <= r2) return 0;
			double s2 = r2 / d2; return s2 / (1 + Math.Sqrt(1 - s2));
		}

		// an orthonormal pair perpendicular to the unit vector w
		private static void Frame(V3 w, out V3 u, out V3 v) {
			V3 a = Math.Abs(w.X) > 0.9 ? new V3(0, 1, 0) : new V3(1, 0, 0);
			u = a.Cross(w).Norm(); v = w.Cross(u);
		}

		// v turned to the side of nf: unchanged above the surface, mirrored across it below
		private static V3 Above(V3 v, V3 nf) { double k = v.Dot(nf); return k > 0 ? v : v - nf * (2 * k); }

		// Smith's Lambda for GGX roughness a and a direction at cosine c to the normal
		private static double SmithLambda(double c, double a) { double c2 = Math.Max(c * c, 1e-12); return 0.5 * (Math.Sqrt(1 + a * a * (1 - c2) / c2) - 1); }

		// A rough-metal bounce: a GGX microfacet normal drawn from those the view v (toward the eye, above n) sees
		// (Heitz 2018), and v mirrored about it. w is the bounce's weight G2/G1; 0 when the bounce leaves below n.
		private static V3 GgxReflect(V3 v, V3 n, double a, ref uint seed, out double w) {
			Frame(n, out V3 t1, out V3 t2);
			V3 vl = new(v.Dot(t1), v.Dot(t2), v.Dot(n));
			V3 vh = new V3(a * vl.X, a * vl.Y, vl.Z).Norm();
			double l2 = vh.X * vh.X + vh.Y * vh.Y;
			V3 b1 = l2 > 0 ? new V3(-vh.Y, vh.X, 0) * (1 / Math.Sqrt(l2)) : new V3(1, 0, 0), b2 = vh.Cross(b1);
			double r = Math.Sqrt(NextD(ref seed)), ph = 2 * Math.PI * NextD(ref seed);
			double p1 = r * Math.Cos(ph), p2 = r * Math.Sin(ph), s = 0.5 * (1 + vh.Z);
			p2 = (1 - s) * Math.Sqrt(Math.Max(0, 1 - p1 * p1)) + s * p2;
			V3 nh = b1 * p1 + b2 * p2 + vh * Math.Sqrt(Math.Max(0, 1 - p1 * p1 - p2 * p2));
			V3 m = new V3(a * nh.X, a * nh.Y, Math.Max(nh.Z, 0)).Norm();
			V3 ll = Reflect(vl * -1, m);
			if (ll.Z <= 0) { w = 0; return n; }
			double lv = SmithLambda(vl.Z, a), lw = SmithLambda(ll.Z, a);
			w = (1 + lv) / (1 + lv + lw);
			return (t1 * ll.X + t2 * ll.Y + n * ll.Z).Norm();
		}

		// ── intersection ────────────────────────────────────────────────────────────────
		private bool Closest(V3 o, V3 d, double tmin, out Hit h) {
			h = default; h.T = RtScene.NoHit; h.Ti = -1; h.Inst = -1; h.Tex = -1; h.Prim = -1; bool hit = false;

			double tb = h.T;
			if (_rt.ClosestTri(o.X, o.Y, o.Z, d.X, d.Y, d.Z, tmin, ref tb, out int hi, out int ht, out double hu, out double hv)) {
				var top = _rt.Top; var geo = _rt.Geo;
				int ib = _rt.InstOff + hi * RtScene.INST, b = _rt.TriOff + ht * RtScene.TRI;
				h.T = tb; hit = true; h.P = o + d * tb;
				V3 n = new V3(geo[b + 3], geo[b + 4], geo[b + 5]).Cross(new V3(geo[b + 6], geo[b + 7], geo[b + 8]));
				// the object-space normal through the inverse transpose; the sign keeps the world winding of a mirroring transform
				h.N = ((new V3(top[ib], top[ib + 1], top[ib + 2]) * n.X + new V3(top[ib + 4], top[ib + 5], top[ib + 6]) * n.Y
					+ new V3(top[ib + 8], top[ib + 9], top[ib + 10]) * n.Z) * top[ib + 27]).Norm();
				h.Mat = (int)top[ib + 23]; h.FR = top[ib + 24]; h.FG = top[ib + 25]; h.FB = top[ib + 26];
				h.Shade = geo[b + 15]; h.Tex = (int)geo[b + 16]; h.HasUV = _rt.SlotPixels(h.Tex, out _, out _) != null;
				double w = 1 - hu - hv;
				h.U = geo[b + 9] * w + geo[b + 11] * hu + geo[b + 13] * hv;
				h.V = geo[b + 10] * w + geo[b + 12] * hu + geo[b + 14] * hv;
				h.Ti = ht; h.Inst = hi;
			}

			for (int i = 0; i < _prims.Length; i++) {
				var pr = _prims[i];
				if (pr.Kind == 0) {                                   // sphere
					V3 c = new(pr.X, pr.Y, pr.Z);
					V3 oc = o - c; double b = oc.Dot(d), cc = oc.Dot(oc) - pr.A * pr.A;
					double disc = b * b - cc; if (disc < 0) continue;
					double sq = Math.Sqrt(disc), tt = -b - sq;
					if (tt <= tmin) tt = -b + sq;
					if (tt <= tmin || tt >= h.T) continue;
					h.T = tt; hit = true; h.P = o + d * tt; h.N = ((h.P - c) * (1.0 / pr.A)).Norm();
					h.Mat = pr.Material; h.HasUV = false; h.Tex = -1; h.Ti = -1; h.Inst = -1; h.Prim = i;
				} else if (pr.Kind == 1) {                            // plane
					V3 nrm = new(pr.A, pr.B, pr.C);
					double den = d.Dot(nrm); if (den > -1e-9 && den < 1e-9) continue;
					double tt = (new V3(pr.X, pr.Y, pr.Z) - o).Dot(nrm) / den;
					if (tt <= tmin || tt >= h.T) continue;
					h.T = tt; hit = true; h.P = o + d * tt; h.N = nrm;
					h.Mat = pr.Material; h.HasUV = false; h.Tex = -1; h.Ti = -1; h.Inst = -1; h.Prim = i;
				} else if (pr.Kind == 2) {                            // AABB box (slab)
					if (BoxHit(o, d, pr, tmin, h.T, out double tt, out V3 bn)) {
						h.T = tt; hit = true; h.P = o + d * tt; h.N = bn;
						h.Mat = pr.Material; h.HasUV = false; h.Tex = -1; h.Ti = -1; h.Inst = -1; h.Prim = i;
					}
				}
			}

			// ray-marched SDF surfaces (Kind 3 torus + Kind 4 presets), competing for closest t
			if (MarchHit(o, d, tmin, h.T, out double mt, out V3 mn, out int mmat)) {
				h.T = mt; hit = true; h.P = o + d * mt; h.N = mn; h.Mat = mmat; h.HasUV = false; h.Tex = -1; h.Ti = -1; h.Inst = -1; h.Prim = -1;
			}
			return hit;
		}

		// Shadow-ray test toward a light, t in (tmin, tmax). With passEmissive, emissive surfaces let it through: toward a
		// point light or an emissive sphere they stand for the light itself, so a lamp can sit inside its bulb. The sun's
		// ray passes false: emissive surfaces cast sun shadows.
		private bool Occluded(V3 o, V3 d, double tmin, double tmax, bool passEmissive) {
			if (_rt.AnyTri(o.X, o.Y, o.Z, d.X, d.Y, d.Z, tmin, tmax, passEmissive)) return true;
			for (int i = 0; i < _prims.Length; i++) {
				var pr = _prims[i];
				if (passEmissive && Emissive(pr.Material)) continue;
				if (pr.Kind == 0) {
					V3 c = new(pr.X, pr.Y, pr.Z); V3 oc = o - c;
					double b = oc.Dot(d), cc = oc.Dot(oc) - pr.A * pr.A; double disc = b * b - cc;
					if (disc < 0) continue; double sq = Math.Sqrt(disc), tt = -b - sq;
					if (tt <= tmin) tt = -b + sq;
					if (tt > tmin && tt < tmax) return true;
				} else if (pr.Kind == 1) {
					V3 nrm = new(pr.A, pr.B, pr.C);
					double den = d.Dot(nrm); if (den > -1e-9 && den < 1e-9) continue;
					double tt = (new V3(pr.X, pr.Y, pr.Z) - o).Dot(nrm) / den;
					if (tt > tmin && tt < tmax) return true;
				} else if (pr.Kind == 2) {
					if (BoxHit(o, d, pr, tmin, tmax, out _, out _)) return true;
				}
			}
			return MarchAny(o, d, tmin, tmax, passEmissive);
		}

		private bool Emissive(int mat) => mat >= 0 && mat < _mats.Length && _mats[mat].Type == 3;

		private static bool BoxHit(V3 o, V3 d, ScenePrimitive pr, double tmin, double tmax, out double t, out V3 n) {
			t = 0; n = default;
			double tx1 = (pr.MinX - o.X) / d.X, tx2 = (pr.MaxX - o.X) / d.X;
			double tlo = Math.Min(tx1, tx2), thi = Math.Max(tx1, tx2); int axis = 0; double sgn = tx1 > tx2 ? 1 : -1;
			double ty1 = (pr.MinY - o.Y) / d.Y, ty2 = (pr.MaxY - o.Y) / d.Y;
			double tylo = Math.Min(ty1, ty2), tyhi = Math.Max(ty1, ty2);
			if (tylo > tlo) { tlo = tylo; axis = 1; sgn = ty1 > ty2 ? 1 : -1; }
			thi = Math.Min(thi, tyhi);
			double tz1 = (pr.MinZ - o.Z) / d.Z, tz2 = (pr.MaxZ - o.Z) / d.Z;
			double tzlo = Math.Min(tz1, tz2), tzhi = Math.Max(tz1, tz2);
			if (tzlo > tlo) { tlo = tzlo; axis = 2; sgn = tz1 > tz2 ? 1 : -1; }
			thi = Math.Min(thi, tzhi);
			if (thi < tlo || thi < tmin) return false;
			double tt = tlo > tmin ? tlo : thi;   // tlo if outside, else exit face (inside)
			bool ent = tlo > tmin;
			if (tt <= tmin || tt >= tmax) return false;
			t = tt;
			double s = ent ? sgn : -sgn;
			n = axis == 0 ? new V3(s, 0, 0) : axis == 1 ? new V3(0, s, 0) : new V3(0, 0, s);
			return true;
		}

		// ── SDF ray marching (Kind 3 / 4), each shape inside its own bounding sphere ──────
		// Sphere-trace primitive i between where the ray enters and leaves its bounding sphere, clipped to [tmin, tmax).
		private bool MarchPrim(int i, V3 o, V3 d, double tmin, double tmax, out double tHit) {
			tHit = 0;
			var pr = _prims[i]; double r = _primR[i];
			V3 oc = o - new V3(pr.X, pr.Y, pr.Z);
			double b = oc.Dot(d), disc = b * b - (oc.Dot(oc) - r * r);
			if (disc < 0) return false;
			double sq = Math.Sqrt(disc);
			double t = Math.Max(tmin, -b - sq), t1 = Math.Min(tmax, -b + sq);
			if (t >= t1) return false;
			// Sign of the field at the start: +1 outside the solid, -1 if we begin inside it (e.g. a refracted ray
			// travelling through the glass diamond). Stepping by sign*dist always advances toward the next boundary,
			// so refraction through SDF solids works too.
			double s0 = PrimSDF(pr, o + d * t) < 0 ? -1.0 : 1.0;
			for (int k = 0; k < 160 && t < t1; k++) {
				double dist = PrimSDF(pr, o + d * t);
				if (Math.Abs(dist) < 1e-4 * (1 + t)) { tHit = t; return true; }
				t += s0 * dist;
			}
			return false;
		}

		// the nearest SDF surface in [tmin, tmax), its normal and material
		private bool MarchHit(V3 o, V3 d, double tmin, double tmax, out double tHit, out V3 n, out int mat) {
			tHit = 0; n = default; mat = -1; int hp = -1;
			for (int i = 0; i < _prims.Length; i++) {
				if (_prims[i].Kind < 3) continue;
				if (MarchPrim(i, o, d, tmin, tmax, out double t)) { tmax = t; tHit = t; hp = i; }
			}
			if (hp < 0) return false;
			n = PrimNormal(_prims[hp], o + d * tHit); mat = _prims[hp].Material;
			return true;
		}

		// shadow rays: with passEmissive, SDF shapes with an emissive material let them through
		private bool MarchAny(V3 o, V3 d, double tmin, double tmax, bool passEmissive) {
			for (int i = 0; i < _prims.Length; i++) {
				if (_prims[i].Kind < 3 || (passEmissive && Emissive(_prims[i].Material))) continue;
				if (MarchPrim(i, o, d, tmin, tmax, out _)) return true;
			}
			return false;
		}

		// tetrahedral gradient of the primitive's SDF: four samples
		private static V3 PrimNormal(ScenePrimitive pr, V3 p) {
			const double e = 5e-4;
			V3 k0 = new(1, -1, -1), k1 = new(-1, -1, 1), k2 = new(-1, 1, -1), k3 = new(1, 1, 1);
			return (k0 * PrimSDF(pr, p + k0 * e) + k1 * PrimSDF(pr, p + k1 * e) + k2 * PrimSDF(pr, p + k2 * e) + k3 * PrimSDF(pr, p + k3 * e)).Norm();
		}

		/// <summary>The signed distance from (x, y, z) to an SDF primitive's surface.</summary>
		internal static double Sdf(ScenePrimitive pr, double x, double y, double z) => PrimSDF(pr, new V3(x, y, z));

		private static double PrimSDF(ScenePrimitive pr, V3 p) {
			V3 q = new(p.X - pr.X, p.Y - pr.Y, p.Z - pr.Z);
			if (pr.Kind == 3) return TorusSDF(q, pr.A, pr.B, (int)pr.C);     // analytic torus → marched
			switch (pr.SdfPreset) {                                          // Kind 4 presets
				case 1: {                                                    // rounded box (half-extents A,B,C)
					double r = 0.15 * pr.A;
					double dx = Math.Abs(q.X) - (pr.A - r), dy = Math.Abs(q.Y) - (pr.B - r), dz = Math.Abs(q.Z) - (pr.C - r);
					double ox = Math.Max(dx, 0), oy = Math.Max(dy, 0), oz = Math.Max(dz, 0);
					return Math.Sqrt(ox * ox + oy * oy + oz * oz) + Math.Min(Math.Max(dx, Math.Max(dy, dz)), 0) - r;
				}
				case 2: return TorusSDF(q, pr.A, pr.B, 1);                    // torus
				case 3: {                                                    // capsule along Y, half-height A, radius B
					double yy = q.Y; if (yy > pr.A) yy -= pr.A; else if (yy < -pr.A) yy += pr.A; else yy = 0;
					return Math.Sqrt(q.X * q.X + yy * yy + q.Z * q.Z) - pr.B;
				}
				case 4: {                                                    // gyroid shell bounded by a sphere
					double f = 1.0 / Math.Max(pr.A, 1e-3);
					double g = Math.Sin(q.X * f) * Math.Cos(q.Y * f) + Math.Sin(q.Y * f) * Math.Cos(q.Z * f) + Math.Sin(q.Z * f) * Math.Cos(q.X * f);
					double shell = (Math.Abs(g) - 0.25) * pr.A * 0.5;
					double bound = q.Len() - pr.A * 2.2;
					return Math.Max(shell, bound);
				}
				case 5: {                                                    // octahedron, size A
					double m = Math.Abs(q.X) + Math.Abs(q.Y) + Math.Abs(q.Z) - pr.A;
					return m * 0.57735026;                                   // /sqrt(3) to keep it a metric SDF
				}
				case 6: return GemSDF(q, pr.A);                              // round brilliant-cut gem
				default: return q.Len() - pr.A;                              // sphere
			}
		}

		private static double TorusSDF(V3 q, double R, double r, int axis) {
			double a, b;
			if (axis == 0) { a = Math.Sqrt(q.Y * q.Y + q.Z * q.Z) - R; b = q.X; }
			else if (axis == 2) { a = Math.Sqrt(q.X * q.X + q.Y * q.Y) - R; b = q.Z; }
			else { a = Math.Sqrt(q.X * q.X + q.Z * q.Z) - R; b = q.Y; }
			return Math.Sqrt(a * a + b * b) - r;
		}

		// Round brilliant-cut gem (the "diamond") as a convex polytope: the intersection of a
		// table + 8 crown facets + 16 girdle facets + 8 pavilion facets meeting at a culet
		// (~33 facets, like the vista scene's gem). The SDF is the max of the half-space
		// distances - a valid, slightly-conservative distance for sphere tracing. Axis = +z.
		// Facet directions are precomputed (called millions of times; no per-eval trig).
		internal static readonly double[] GemFacets = BuildGemFacets();
		private static double[] BuildGemFacets() {
			// packed as (nx, ny, nz, d) per facet
			var list = new System.Collections.Generic.List<double>();
			list.AddRange(new[] { 0.0, 0.0, 1.0, 0.42 });                  // table
			for (int k = 0; k < 8; k++) {                                  // crown
				double az = k * (Math.PI / 4);
				list.AddRange(new[] { Math.Cos(az) * 0.545, Math.Sin(az) * 0.545, 0.838, 0.62 });
			}
			for (int k = 0; k < 16; k++) {                                 // girdle
				double az = k * (Math.PI / 8);
				list.AddRange(new[] { Math.Cos(az), Math.Sin(az), 0.0, 0.5 });
			}
			for (int k = 0; k < 8; k++) {                                  // pavilion -> culet
				double az = k * (Math.PI / 4) + (Math.PI / 8);
				list.AddRange(new[] { Math.Cos(az) * 0.643, Math.Sin(az) * 0.643, -0.766, 0.5 });
			}
			return list.ToArray();
		}
		private static double GemSDF(V3 q, double A) {
			double s = 1.0 / Math.Max(A, 1e-3);
			double x = q.X * s, y = q.Y * s, z = q.Z * s;
			double[] f = GemFacets;
			double d = -1e30;
			for (int i = 0; i < f.Length; i += 4)
				d = Math.Max(d, f[i] * x + f[i + 1] * y + f[i + 2] * z - f[i + 3]);
			return d * A;                                                  // unit-space distance -> world
		}

		// ── material resolve ────────────────────────────────────────────────────────────
		// The hit's material: type, albedo (with the object's colour override or tint), emission (the material's plus the
		// object's emissive), roughness, IOR and normal map (normTex: a texture slot; dx: its green runs down the image).
		private void Resolve(in Hit h, out int type, out V3 alb, out V3 emis, out double rough, out double ior, out int nmap, out int normTex, out bool dx) {
			type = 0; rough = 1; ior = 1.5; nmap = 0; normTex = -1; dx = false; emis = new V3(0, 0, 0); alb = new V3(0.8, 0.8, 0.8);
			SceneMaterial? m = (h.Mat >= 0 && h.Mat < _mats.Length) ? _mats[h.Mat] : null;
			int tw = 0, th = 0; bool texd = false; double sh = 1;
			if (m != null) {
				type = m.Type; rough = m.Rough; ior = m.Ior; nmap = m.NormalMap; normTex = _matNorm[h.Mat];
				emis = new V3(m.ER, m.EG, m.EB);
				int[]? mtex = h.HasUV ? _rt.SlotPixels(_matTex[h.Mat], out tw, out th) : null;
				texd = mtex != null;
				alb = mtex != null ? SampleTex(mtex, tw, th, h.U, h.V) : new V3(m.R, m.G, m.B);   // no texture, or one that isn't registered: the material colour
			} else if (_rt.SlotPixels(h.Tex, out tw, out th) is int[] tex) {   // untagged textured geometry
				sh = h.Shade; alb = SampleTex(tex, tw, th, h.U, h.V) * sh; texd = true;
			} else {                                                 // untagged flat geometry
				alb = new V3(h.FR, h.FG, h.FB);
			}
			if (h.Inst >= 0) {                                       // the object's colours
				var top = _rt.Top; int ib = _rt.InstOff + h.Inst * RtScene.INST;
				if (top[ib + 31] >= 0) alb = new V3(top[ib + 31], top[ib + 32], top[ib + 33]) * sh;
				else if (texd) alb = alb * new V3(top[ib + 28], top[ib + 29], top[ib + 30]);
				emis = emis + alb * new V3(top[ib + 34], top[ib + 35], top[ib + 36]);
				// the normal map SetTextureNormalMap gave the triangle's texture, when the material has none
				if (normTex < 0 && nmap == 0 && h.HasUV) { int ns = _rt.SlotNormal(h.Tex); if (ns >= 0) { normTex = ns; dx = true; } }
			}
		}

		// ── helpers ──────────────────────────────────────────────────────────────────────
		private V3 Sky(V3 d) {
			double t = 0.5 * (d.Y + 1.0); if (t < 0) t = 0; else if (t > 1) t = 1;
			var s = _s;
			return new V3(s._skyBR + (s._skyTR - s._skyBR) * t,
						  s._skyBG + (s._skyTG - s._skyBG) * t,
						  s._skyBB + (s._skyTB - s._skyBB) * t) * s._skyStrength;
		}

		private static V3 Reflect(V3 d, V3 n) => d - n * (2 * d.Dot(n));

		private static V3 CosineHemisphere(V3 n, ref uint seed) {
			double r1 = NextD(ref seed), r2 = NextD(ref seed);
			double r = Math.Sqrt(r1), phi = 2 * Math.PI * r2;
			double x = r * Math.Cos(phi), y = r * Math.Sin(phi), z = Math.Sqrt(1 - r1);
			Frame(n, out V3 u, out V3 v);
			return (u * x + v * y + n * z).Norm();
		}

		// Procedural wood: concentric rings perturb normal + darken latewood.
		private static V3 WoodAlbedo(V3 p, V3 baseAlb) {
			double rad = Math.Sqrt(p.X * p.X + p.Z * p.Z);
			double rings = Math.Sin(rad * 14.0 + Math.Sin(p.Y * 3.0) * 1.5);
			double f = 0.55 + 0.45 * (rings * 0.5 + 0.5);
			return baseAlb * f;
		}
		// ── procedural normal maps (world-space; work on any surface) ────────────────────
		// Perturb the normal by the tangential gradient of a scalar height field selected by
		// `kind` (1 wood, 2 perlin, 3 waves). Orientation-free: builds its own tangent frame.
		private static V3 PerturbNormal(V3 p, V3 n, int kind) {
			const double e = 0.04, amp = 0.6;
			double f0 = HeightField(p, kind);
			V3 g = new((HeightField(new V3(p.X + e, p.Y, p.Z), kind) - f0) / e,
					   (HeightField(new V3(p.X, p.Y + e, p.Z), kind) - f0) / e,
					   (HeightField(new V3(p.X, p.Y, p.Z + e), kind) - f0) / e);
			V3 gt = g - n * g.Dot(n);                  // tangential part of the gradient
			return (n - gt * amp).Norm();
		}
		private static double HeightField(V3 p, int kind) {
			if (kind == 1) {                            // wood: ring grain
				double rad = Math.Sqrt(p.X * p.X + p.Z * p.Z);
				return Math.Sin(rad * 14.0 + Math.Sin(p.Y * 3.0) * 1.5) * 0.05;
			}
			if (kind == 3) {                            // waves: crossed ripples
				return (Math.Sin(p.X * 5.0) + Math.Sin(p.Z * 5.0 + p.X * 1.3) + Math.Sin((p.X + p.Z) * 3.5)) * 0.05;
			}
			return Fbm(p * 2.2) * 0.5;                  // perlin-ish value-noise fBm
		}

		// value-noise fBm (cheap stand-in for Perlin; smooth enough for bump gradients)
		private static double Vhash(int x, int y, int z) {
			uint h = (uint)(x * 374761393 + y * 668265263 + z * 1274126177);
			h = (h ^ (h >> 13)) * 1274126177u;
			return ((h ^ (h >> 16)) & 0xFFFFFF) / 16777216.0;
		}
		private static double Noise3(V3 p) {
			int xi = (int)Math.Floor(p.X), yi = (int)Math.Floor(p.Y), zi = (int)Math.Floor(p.Z);
			double fx = p.X - xi, fy = p.Y - yi, fz = p.Z - zi;
			double sx = fx * fx * (3 - 2 * fx), sy = fy * fy * (3 - 2 * fy), sz = fz * fz * (3 - 2 * fz);
			double c000 = Vhash(xi, yi, zi), c100 = Vhash(xi + 1, yi, zi);
			double c010 = Vhash(xi, yi + 1, zi), c110 = Vhash(xi + 1, yi + 1, zi);
			double c001 = Vhash(xi, yi, zi + 1), c101 = Vhash(xi + 1, yi, zi + 1);
			double c011 = Vhash(xi, yi + 1, zi + 1), c111 = Vhash(xi + 1, yi + 1, zi + 1);
			double x00 = c000 + (c100 - c000) * sx, x10 = c010 + (c110 - c010) * sx;
			double x01 = c001 + (c101 - c001) * sx, x11 = c011 + (c111 - c011) * sx;
			double y0 = x00 + (x10 - x00) * sy, y1 = x01 + (x11 - x01) * sy;
			return (y0 + (y1 - y0) * sz) * 2 - 1;
		}
		private static double Fbm(V3 p) => Noise3(p) * 0.5 + Noise3(p * 2.03) * 0.25 + Noise3(p * 4.01) * 0.125;

		// Tangent-space normal map from a texture, on the front of the hit triangle (n: its winding normal): build a
		// tangent frame from the triangle's position + UV gradients, decode the RGB normal, and rotate it into world space.
		// Green runs along n x tangent, or down the image (the texture's v) for a map in the DirectX convention (dx).
		private V3 TexNormal(in Hit h, V3 n, int slot, bool dx) {
			var tex = _rt.SlotPixels(slot, out int tw, out int th);
			if (tex == null) return n;
			var geo = _rt.Geo; var top = _rt.Top;
			int b = _rt.TriOff + h.Ti * RtScene.TRI, ib = _rt.InstOff + h.Inst * RtScene.INST;
			V3 e1 = new(geo[b + 3], geo[b + 4], geo[b + 5]), e2 = new(geo[b + 6], geo[b + 7], geo[b + 8]);
			double du1 = geo[b + 11] - geo[b + 9], dv1 = geo[b + 12] - geo[b + 10], du2 = geo[b + 13] - geo[b + 9], dv2 = geo[b + 14] - geo[b + 10];
			double det = du1 * dv2 - du2 * dv1; if (Math.Abs(det) < 1e-12) return n;
			double r = 1.0 / det;
			// the object-space tangent into the world through the object's transform
			V3 tan = InstToWorld(top, ib, ((e1 * dv2) - (e2 * dv1)) * r);
			tan = (tan - n * tan.Dot(n)).Norm();        // Gram-Schmidt against the normal
			V3 bit = n.Cross(tan);
			if (dx) { V3 bw = InstToWorld(top, ib, ((e2 * du1) - (e1 * du2)) * r); bit = (bw - n * bw.Dot(n) - tan * bw.Dot(tan)).Norm(); }
			int px = tex[WrapTexel(h.V, th) * tw + WrapTexel(h.U, tw)];
			double nx = ((px >> 16) & 255) / 127.5 - 1.0;   // RGB → tangent-space normal (linear data)
			double ny = ((px >> 8) & 255) / 127.5 - 1.0;
			double nz = (px & 255) / 127.5 - 1.0;
			return (tan * nx + bit * ny + n * Math.Max(nz, 0.05)).Norm();
		}
		// the object-space vector v into the world through the transform of the instance at top[ib]
		private static V3 InstToWorld(float[] top, int ib, V3 v) =>
			new(top[ib + 12] * v.X + top[ib + 13] * v.Y + top[ib + 14] * v.Z,
				top[ib + 15] * v.X + top[ib + 16] * v.Y + top[ib + 17] * v.Z,
				top[ib + 18] * v.X + top[ib + 19] * v.Y + top[ib + 20] * v.Z);

		private static V3 SampleTex(int[] tex, int tw, int th, double u, double v) {
			int p = tex[WrapTexel(v, th) * tw + WrapTexel(u, tw)];
			return new V3(RtScene.Srgb(((p >> 16) & 255) / 255.0), RtScene.Srgb(((p >> 8) & 255) / 255.0), RtScene.Srgb((p & 255) / 255.0));
		}

		/// <summary>Texel column (or row) 0..n-1 for texture coordinate u with repeat wrap: the fractional part of u
		/// picks it, so u just below 0 lands on the last texel. The GPU tracer's wrapTexel does the same.</summary>
		internal static int WrapTexel(double u, int n) {
			int x = (int)((u - Math.Floor(u)) * n);
			return x < 0 ? 0 : (x >= n ? n - 1 : x);
		}

		private static byte Out(double c) {
			c = c / (1.0 + c);                 // Reinhard tonemap
			c = Math.Pow(c < 0 ? 0 : c, 1.0 / 2.2);
			return RenderUtil.CB(c * 255.0);
		}

		// xorshift-ish per-pixel RNG
		private static uint Hash(uint x) { x ^= x >> 16; x *= 0x7feb352du; x ^= x >> 15; x *= 0x846ca68bu; x ^= x >> 16; return x; }
		private static double NextD(ref uint s) { s ^= s << 13; s ^= s >> 17; s ^= s << 5; return (s & 0xFFFFFF) / 16777216.0; }
	}
}
