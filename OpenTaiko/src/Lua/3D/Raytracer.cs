using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenTaiko {
	/// <summary>
	/// Progressive Monte-Carlo path tracer for <see cref="Lua3DScene"/>. Renders the same triangles
	/// the rasterizer draws (built once into a world-space list) plus raytracer-only analytic
	/// primitives (sphere / plane / box) and ray-marched SDF surfaces (torus + presets), with
	/// point lights, emissive global illumination, and diffuse / metal / glass / emissive materials.
	///
	/// Accumulates one sample per pixel each <see cref="Render"/> call into a float buffer; the
	/// image converges while the scene is still and resets (via the scene's Revision) whenever the
	/// camera or any geometry / light / material changes — so it is grainy in motion and clean when
	/// stopped. Multithreaded over horizontal row bands (disjoint pixels ⇒ no races).
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

		// ── built world triangle ─────────────────────────────────────────────────────────
		private struct RTTri {
			public V3 A, B, C;            // vertices
			public double U0, V0, U1, V1, U2, V2;
			public int Mat;              // material id, or -1
			public int[]? Tex; public int Tw, Th;   // fallback texture (Mat<0), or material-driven
			public double FR, FG, FB;    // fallback flat albedo (linear), used when Mat<0 && Tex==null
			public double Shade;
		}

		// ── hit record ────────────────────────────────────────────────────────────────────
		private struct Hit {
			public double T; public V3 P, N;
			public int Mat;
			public bool HasUV; public double U, V;
			public int[]? Tex; public int Tw, Th; public double Shade;
			public double FR, FG, FB;    // fallback albedo (linear) when Mat<0 && Tex==null
			public int Ti;               // source triangle index (for tangent-space normal maps), or -1
		}

		private Lua3DScene _s = null!;
		private int _lastRevision = -1, _w, _h;
		private float[] _accum = Array.Empty<float>();   // length w*h*3, linear radiance sum
		private int _samples;

		private RTTri[] _tri = Array.Empty<RTTri>();
		private int _triN;
		private ScenePrimitive[] _prims = Array.Empty<ScenePrimitive>();
		private SceneMaterial[] _mats = Array.Empty<SceneMaterial>();
		private SceneLight[] _lights = Array.Empty<SceneLight>();

		private const int MaxDepth = 8;
		/// <summary>When > 0, caps the per-ray bounce depth (used by the hybrid RTT insets to trade
		/// fidelity for speed; 0 = full depth).</summary>
		public int MaxDepthOverride = 0;

		/// <summary>How many samples per pixel have accumulated since the last reset (0 just after
		/// a camera/scene change). Lets the demo show convergence progress.</summary>
		public int SampleCount => _samples;

		public void Invalidate() { _lastRevision = -1; _samples = 0; }

		public void Render(Lua3DScene s) {
			_s = s;
			bool resize = _w != s._w || _h != s._h;
			if (resize) { _w = s._w; _h = s._h; _accum = new float[_w * _h * 3]; }
			if (resize || s.Revision != _lastRevision) {
				_lastRevision = s.Revision;
				Rebuild();
				Array.Clear(_accum, 0, _accum.Length);
				_samples = 0;
			}

			_samples++;
			double inv = 1.0 / _samples;
			int n = s._threads;
			if (n <= 1) RenderBand(0, _h, inv);
			else Parallel.For(0, n, s._po, bi => RenderBand(bi * _h / n, (bi + 1) * _h / n, inv));
			s._canvas.MarkDirty(0, 0, _w - 1, _h - 1);
		}

		// Snapshot scene data into flat arrays + build the world-space triangle list once per reset.
		private void Rebuild() {
			_prims = _s._primitives.ToArray();
			_mats = _s._materials.ToArray();
			_lights = _s._lights.ToArray();

			_triN = 0;
			int cap = 256;
			if (_tri.Length < cap) _tri = new RTTri[cap];
			foreach (var o in _s._objects.Values) {
				if (!o.Visible || o.N == 0) continue;
				var d = o.D; var t = o.Transform;
				if (o.Kind == 0) {                                   // textured quads
					for (int i = 0; i < o.N; i++) {
						int k = i * 16; int texId = (int)d[k + 12];
						_s._texPix.TryGetValue(texId, out var tex);
						int tw = tex != null ? _s._texW[texId] : 0, th = tex != null ? _s._texH[texId] : 0;
						double uMax = d[k + 13], vMax = d[k + 14], shade = d[k + 15];
						V3 p0 = Xform(t, d[k], d[k + 1], d[k + 2]);
						V3 p1 = Xform(t, d[k + 3], d[k + 4], d[k + 5]);
						V3 p2 = Xform(t, d[k + 6], d[k + 7], d[k + 8]);
						V3 p3 = Xform(t, d[k + 9], d[k + 10], d[k + 11]);
						PushTri(p0, p1, p2, 0, vMax, uMax, vMax, uMax, 0, o.Material, tex, tw, th, shade, o.R, o.G, o.B);
						PushTri(p0, p2, p3, 0, vMax, uMax, 0, 0, 0, o.Material, tex, tw, th, shade, o.R, o.G, o.B);
					}
				} else if (o.Kind == 1) {                            // flat quads
					for (int i = 0; i < o.N; i++) {
						int k = i * 12;
						V3 p0 = Xform(t, d[k], d[k + 1], d[k + 2]);
						V3 p1 = Xform(t, d[k + 3], d[k + 4], d[k + 5]);
						V3 p2 = Xform(t, d[k + 6], d[k + 7], d[k + 8]);
						V3 p3 = Xform(t, d[k + 9], d[k + 10], d[k + 11]);
						PushTri(p0, p1, p2, 0, 0, 0, 0, 0, 0, o.Material, null, 0, 0, 1, o.R, o.G, o.B);
						PushTri(p0, p2, p3, 0, 0, 0, 0, 0, 0, o.Material, null, 0, 0, 1, o.R, o.G, o.B);
					}
				} else if (o.Kind == 2) {                            // textured triangles
					for (int i = 0; i < o.N; i++) {
						int k = i * 17; int texId = (int)d[k + 15];
						_s._texPix.TryGetValue(texId, out var tex);
						int tw = tex != null ? _s._texW[texId] : 0, th = tex != null ? _s._texH[texId] : 0;
						double shade = d[k + 16];
						V3 p0 = Xform(t, d[k], d[k + 1], d[k + 2]);
						V3 p1 = Xform(t, d[k + 5], d[k + 6], d[k + 7]);
						V3 p2 = Xform(t, d[k + 10], d[k + 11], d[k + 12]);
						PushTri(p0, p1, p2, d[k + 3], d[k + 4], d[k + 8], d[k + 9], d[k + 13], d[k + 14],
							o.Material, tex, tw, th, shade, o.R, o.G, o.B);
					}
				}
			}
		}

		private void PushTri(V3 a, V3 b, V3 c, double u0, double v0, double u1, double v1, double u2, double v2,
			int mat, int[]? tex, int tw, int th, double shade, double fr255, double fg255, double fb255) {
			if (_triN == _tri.Length) Array.Resize(ref _tri, _tri.Length * 2);
			ref RTTri t = ref _tri[_triN++];
			t.A = a; t.B = b; t.C = c;
			t.U0 = u0; t.V0 = v0; t.U1 = u1; t.V1 = v1; t.U2 = u2; t.V2 = v2;
			t.Mat = mat; t.Tex = tex; t.Tw = tw; t.Th = th; t.Shade = shade;
			t.FR = Srgb(fr255 / 255.0); t.FG = Srgb(fg255 / 255.0); t.FB = Srgb(fb255 / 255.0);
		}

		private static V3 Xform(double[]? t, double x, double y, double z) {
			if (t == null) return new V3(x, y, z);
			return new V3(t[0] * x + t[1] * y + t[2] * z + t[3],
						  t[4] * x + t[5] * y + t[6] * z + t[7],
						  t[8] * x + t[9] * y + t[10] * z + t[11]);
		}

		// ── per-band sampling ───────────────────────────────────────────────────────────
		private void RenderBand(int y0, int y1, double inv) {
			var s = _s;
			V3 eye = new(s._camX, s._camY, s._camZ);
			V3 R = new(s._Rx, s._Ry, s._Rz), U = new(s._Ux, s._Uy, s._Uz), F = new(s._Fx, s._Fy, s._Fz);
			double hw = _w * 0.5, hh = _h * 0.5, scale = s._scale;
			byte[] buf = s._canvas._buf;
			for (int py = y0; py < y1; py++) {
				int row = py * _w;
				for (int px = 0; px < _w; px++) {
					uint seed = Hash((uint)(row + px) * 2654435761u ^ (uint)_samples * 40503u);
					double jx = NextD(ref seed), jy = NextD(ref seed);
					double sx = (px + jx - hw) / scale;
					double sy = -(py + jy - hh) / scale;
					V3 dir = (R * sx + U * sy + F).Norm();
					V3 col = Trace(eye, dir, ref seed);

					int ai = (row + px) * 3;
					float ar = _accum[ai] + (float)col.X, ag = _accum[ai + 1] + (float)col.Y, ab = _accum[ai + 2] + (float)col.Z;
					_accum[ai] = ar; _accum[ai + 1] = ag; _accum[ai + 2] = ab;
					int o = (row + px) * 4;
					buf[o] = Out(ar * inv); buf[o + 1] = Out(ag * inv); buf[o + 2] = Out(ab * inv); buf[o + 3] = 255;
				}
			}
		}

		// ── path integrator ─────────────────────────────────────────────────────────────
		private V3 Trace(V3 o, V3 d, ref uint seed) {
			V3 L = new(0, 0, 0), thr = new(1, 1, 1);
			int maxDepth = MaxDepthOverride > 0 ? MaxDepthOverride : MaxDepth;
			for (int bounce = 0; bounce < maxDepth; bounce++) {
				if (!Closest(o, d, 1e-4, double.MaxValue, out Hit h)) {
					L = L + thr * Sky(d); break;
				}
				Resolve(h, out int type, out V3 alb, out V3 emis, out double rough, out double ior, out int nmap, out int normTex);

				// facing normal (against the ray) + glass inside/outside flag
				V3 ng = h.N; double cosI = -d.Dot(ng); bool inside = cosI < 0;
				V3 n = inside ? ng * -1 : ng;

				// normal mapping: a tangent-space texture (needs UVs) wins; else a procedural preset.
				if (normTex >= 0 && h.Ti >= 0) n = TexNormal(h, n, normTex);
				else if (nmap != 0) { if (nmap == 1) alb = WoodAlbedo(h.P, alb); n = PerturbNormal(h.P, n, nmap); }

				L = L + thr * emis;
				if (type == 3) break;                                 // emissive: done

				V3 p = h.P;
				if (type == 2) {                                      // glass (dielectric)
					double eta = inside ? ior : 1.0 / ior;
					double c = Math.Abs(cosI);
					double k = 1 - eta * eta * (1 - c * c);
					V3 refl = Reflect(d, n);
					if (k < 0) { o = p + n * 1e-4; d = refl; }         // total internal reflection
					else {
						double cosT = Math.Sqrt(k);
						double r0 = (eta - 1) / (eta + 1); r0 *= r0;
						double fr = r0 + (1 - r0) * Math.Pow(1 - c, 5);
						if (NextD(ref seed) < fr) { o = p + n * 1e-4; d = refl; }
						else { V3 rt = (d * eta + n * (eta * c - cosT)).Norm(); o = p - n * 1e-4; d = rt; }
					}
					thr = thr * alb;
				} else if (type == 1) {                               // metal (tinted reflection)
					V3 refl = Reflect(d, n);
					if (rough > 0) refl = (refl + RandUnit(ref seed) * rough).Norm();
					if (refl.Dot(n) < 0) refl = Reflect(d, n);
					o = p + n * 1e-4; d = refl; thr = thr * alb;
				} else {                                              // diffuse: NEE + cosine bounce
					L = L + thr * alb * DirectLight(p, n, ref seed);
					V3 nd = CosineHemisphere(n, ref seed);
					o = p + n * 1e-4; d = nd; thr = thr * alb;
				}

				if (bounce >= 3) {                                    // Russian roulette
					double q = Math.Max(thr.X, Math.Max(thr.Y, thr.Z));
					if (q < 1) { if (NextD(ref seed) > q) break; thr = thr * (1.0 / Math.Max(q, 1e-4)); }
				}
			}
			return L;
		}

		// Next-event estimation: sum unshadowed point-light contributions for a diffuse hit.
		private V3 DirectLight(V3 p, V3 n, ref uint seed) {
			V3 sum = new(0, 0, 0);
			for (int i = 0; i < _lights.Length; i++) {
				var lt = _lights[i];
				V3 to = new V3(lt.X, lt.Y, lt.Z) - p;
				double dist = to.Len(); if (dist < 1e-6) continue;
				V3 l = to * (1.0 / dist);
				double ndl = n.Dot(l); if (ndl <= 0) continue;
				if (Occluded(p + n * 1e-4, l, dist - 2e-3)) continue;
				double inv = ndl / (Math.PI * dist * dist);
				sum = sum + new V3(lt.R, lt.G, lt.B) * inv;
			}
			return sum;
		}

		// ── intersection ────────────────────────────────────────────────────────────────
		private bool Closest(V3 o, V3 d, double tmin, double tmax, out Hit h) {
			h = default; h.T = tmax; h.Ti = -1; bool hit = false;

			for (int i = 0; i < _triN; i++) {
				ref RTTri t = ref _tri[i];
				V3 e1 = t.B - t.A, e2 = t.C - t.A;
				V3 pv = d.Cross(e2); double det = e1.Dot(pv);
				if (det > -1e-12 && det < 1e-12) continue;
				double idet = 1.0 / det;
				V3 tv = o - t.A; double u = tv.Dot(pv) * idet; if (u < 0 || u > 1) continue;
				V3 qv = tv.Cross(e1); double v = d.Dot(qv) * idet; if (v < 0 || u + v > 1) continue;
				double tt = e2.Dot(qv) * idet; if (tt <= tmin || tt >= h.T) continue;
				h.T = tt; hit = true;
				double w = 1 - u - v;
				h.P = o + d * tt;
				h.N = e1.Cross(e2).Norm();
				h.Mat = t.Mat; h.Shade = t.Shade; h.Ti = i;
				h.Tex = t.Tex; h.Tw = t.Tw; h.Th = t.Th;
				h.HasUV = t.Tex != null;
				h.U = t.U0 * w + t.U1 * u + t.U2 * v;
				h.V = t.V0 * w + t.V1 * u + t.V2 * v;
				h.FR = t.FR; h.FG = t.FG; h.FB = t.FB;
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
					h.Mat = pr.Material; h.HasUV = false; h.Tex = null; h.Ti = -1;
				} else if (pr.Kind == 1) {                            // plane
					V3 nrm = new(pr.A, pr.B, pr.C);
					double den = d.Dot(nrm); if (den > -1e-9 && den < 1e-9) continue;
					double tt = (new V3(pr.X, pr.Y, pr.Z) - o).Dot(nrm) / den;
					if (tt <= tmin || tt >= h.T) continue;
					h.T = tt; hit = true; h.P = o + d * tt; h.N = nrm;
					h.Mat = pr.Material; h.HasUV = false; h.Tex = null; h.Ti = -1;
				} else if (pr.Kind == 2) {                            // AABB box (slab)
					if (BoxHit(o, d, pr, tmin, h.T, out double tt, out V3 bn)) {
						h.T = tt; hit = true; h.P = o + d * tt; h.N = bn;
						h.Mat = pr.Material; h.HasUV = false; h.Tex = null; h.Ti = -1;
					}
				}
			}

			// ray-marched SDF surfaces (Kind 3 torus + Kind 4 presets), competing for closest t
			if (MarchHit(o, d, tmin, h.T, out double mt, out V3 mn, out int mmat)) {
				h.T = mt; hit = true; h.P = o + d * mt; h.N = mn; h.Mat = mmat; h.HasUV = false; h.Tex = null; h.Ti = -1;
			}
			return hit;
		}

		private bool Occluded(V3 o, V3 d, double tmax) {
			for (int i = 0; i < _triN; i++) {
				ref RTTri t = ref _tri[i];
				V3 e1 = t.B - t.A, e2 = t.C - t.A;
				V3 pv = d.Cross(e2); double det = e1.Dot(pv);
				if (det > -1e-12 && det < 1e-12) continue;
				double idet = 1.0 / det;
				V3 tv = o - t.A; double u = tv.Dot(pv) * idet; if (u < 0 || u > 1) continue;
				V3 qv = tv.Cross(e1); double v = d.Dot(qv) * idet; if (v < 0 || u + v > 1) continue;
				double tt = e2.Dot(qv) * idet; if (tt > 1e-4 && tt < tmax) return true;
			}
			for (int i = 0; i < _prims.Length; i++) {
				var pr = _prims[i];
				if (pr.Kind == 0) {
					V3 c = new(pr.X, pr.Y, pr.Z); V3 oc = o - c;
					double b = oc.Dot(d), cc = oc.Dot(oc) - pr.A * pr.A; double disc = b * b - cc;
					if (disc < 0) continue; double sq = Math.Sqrt(disc), tt = -b - sq;
					if (tt <= 1e-4) tt = -b + sq;
					if (tt > 1e-4 && tt < tmax) return true;
				} else if (pr.Kind == 2) {
					if (BoxHit(o, d, pr, 1e-4, tmax, out _, out _)) return true;
				}
			}
			if (MarchHit(o, d, 1e-4, tmax, out _, out _, out _)) return true;
			return false;
		}

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

		// ── SDF ray marching (Kind 3 / 4) ─────────────────────────────────────────────────
		private bool MarchHit(V3 o, V3 d, double tmin, double tmax, out double tHit, out V3 n, out int mat) {
			tHit = 0; n = default; mat = -1;
			bool any = false; for (int i = 0; i < _prims.Length; i++) if (_prims[i].Kind >= 3) { any = true; break; }
			if (!any) return false;
			double t = tmin;
			// Sign of the field at the start: +1 outside the solids, -1 if we begin inside one (e.g.
			// a refracted ray travelling through the glass diamond). Stepping by sign*dist always
			// advances toward the next boundary, so refraction through SDF solids works too.
			double s0 = SceneSDF(o + d * tmin, out _) < 0 ? -1.0 : 1.0;
			for (int i = 0; i < 160 && t < tmax; i++) {
				V3 p = o + d * t;
				double dist = SceneSDF(p, out int idx);
				if (Math.Abs(dist) < 1e-4 * (1 + t)) {
					tHit = t; mat = _prims[idx].Material;
					double e = 5e-4;
					double nx = SceneSDF(new V3(p.X + e, p.Y, p.Z), out _) - SceneSDF(new V3(p.X - e, p.Y, p.Z), out _);
					double ny = SceneSDF(new V3(p.X, p.Y + e, p.Z), out _) - SceneSDF(new V3(p.X, p.Y - e, p.Z), out _);
					double nz = SceneSDF(new V3(p.X, p.Y, p.Z + e), out _) - SceneSDF(new V3(p.X, p.Y, p.Z - e), out _);
					n = new V3(nx, ny, nz).Norm();
					return true;
				}
				t += s0 * dist;
			}
			return false;
		}

		private double SceneSDF(V3 p, out int idx) {
			double best = double.MaxValue; idx = -1;
			for (int i = 0; i < _prims.Length; i++) {
				var pr = _prims[i]; if (pr.Kind < 3) continue;
				double dd = PrimSDF(pr, p);
				if (dd < best) { best = dd; idx = i; }
			}
			return best;
		}

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
		private static readonly double[] _gemFacets = BuildGemFacets();
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
			double[] f = _gemFacets;
			double d = -1e30;
			for (int i = 0; i < f.Length; i += 4)
				d = Math.Max(d, f[i] * x + f[i + 1] * y + f[i + 2] * z - f[i + 3]);
			return d * A;                                                  // unit-space distance -> world
		}

		// ── material resolve ────────────────────────────────────────────────────────────
		private void Resolve(in Hit h, out int type, out V3 alb, out V3 emis, out double rough, out double ior, out int nmap, out int normTex) {
			type = 0; rough = 1; ior = 1.5; nmap = 0; normTex = -1; emis = new V3(0, 0, 0); alb = new V3(0.8, 0.8, 0.8);
			SceneMaterial? m = (h.Mat >= 0 && h.Mat < _mats.Length) ? _mats[h.Mat] : null;
			if (m != null) {
				type = m.Type; rough = m.Rough; ior = m.Ior; nmap = m.NormalMap; normTex = m.NormalTex;
				emis = new V3(m.ER, m.EG, m.EB);
				if (m.TexId >= 0 && h.HasUV && _s._texPix.TryGetValue(m.TexId, out var tex))
					alb = SampleTex(tex, _s._texW[m.TexId], _s._texH[m.TexId], h.U, h.V);
				else alb = new V3(m.R, m.G, m.B);
			} else if (h.Tex != null) {                              // untagged textured geometry
				alb = SampleTex(h.Tex, h.Tw, h.Th, h.U, h.V) * h.Shade;
			} else {                                                 // untagged flat geometry
				alb = new V3(h.FR, h.FG, h.FB);
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
			V3 w = n;
			V3 a = Math.Abs(w.X) > 0.9 ? new V3(0, 1, 0) : new V3(1, 0, 0);
			V3 u = a.Cross(w).Norm(); V3 v = w.Cross(u);
			return (u * x + v * y + w * z).Norm();
		}

		private static V3 RandUnit(ref uint seed) {
			double z = NextD(ref seed) * 2 - 1, a = NextD(ref seed) * 2 * Math.PI, r = Math.Sqrt(Math.Max(0, 1 - z * z));
			return new V3(r * Math.Cos(a), r * Math.Sin(a), z);
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

		// Tangent-space normal map from a texture: build a tangent frame from the hit triangle's
		// position + UV gradients, decode the RGB normal, and rotate it into world space.
		private V3 TexNormal(in Hit h, V3 n, int texId) {
			if (!_s._texPix.TryGetValue(texId, out var tex)) return n;
			ref RTTri t = ref _tri[h.Ti];
			V3 e1 = t.B - t.A, e2 = t.C - t.A;
			double du1 = t.U1 - t.U0, dv1 = t.V1 - t.V0, du2 = t.U2 - t.U0, dv2 = t.V2 - t.V0;
			double det = du1 * dv2 - du2 * dv1; if (Math.Abs(det) < 1e-12) return n;
			double r = 1.0 / det;
			V3 tan = ((e1 * dv2) - (e2 * dv1)) * r;
			tan = (tan - n * tan.Dot(n)).Norm();        // Gram-Schmidt against the (facing) normal
			V3 bit = n.Cross(tan);
			int tw = _s._texW[texId], th = _s._texH[texId];
			int x = (int)(h.U * tw) % tw; if (x < 0) x += tw;
			int y = (int)(h.V * th) % th; if (y < 0) y += th;
			int px = tex[y * tw + x];
			double nx = ((px >> 16) & 255) / 127.5 - 1.0;   // RGB → tangent-space normal (linear data)
			double ny = ((px >> 8) & 255) / 127.5 - 1.0;
			double nz = (px & 255) / 127.5 - 1.0;
			return (tan * nx + bit * ny + n * Math.Max(nz, 0.05)).Norm();
		}

		private static V3 SampleTex(int[] tex, int tw, int th, double u, double v) {
			int x = (int)(u * tw) % tw; if (x < 0) x += tw;
			int y = (int)(v * th) % th; if (y < 0) y += th;
			int p = tex[y * tw + x];
			return new V3(Srgb(((p >> 16) & 255) / 255.0), Srgb(((p >> 8) & 255) / 255.0), Srgb((p & 255) / 255.0));
		}

		private static double Srgb(double c) => Math.Pow(c < 0 ? 0 : c, 2.2);

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
