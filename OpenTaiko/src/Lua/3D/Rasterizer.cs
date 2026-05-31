using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenTaiko {
	// One screen-space triangle ready to rasterize. Produced by Builder, consumed by Rasterizer.
	internal struct RTri {
		public double X0, Y0, W0, X1, Y1, W1, X2, Y2, W2;
		public double U0, V0, U1, V1, U2, V2;
		public double Lr0, Lg0, Lb0, Lr1, Lg1, Lb1, Lr2, Lg2, Lb2;   // per-vertex light * w (Gouraud)
		public bool Flat; public double FlR, FlG, FlB;               // constant light multiplier (no per-pixel interp)
		public int[] Tex; public int Tw, Th;
		public double Shade; public int Alpha;
		public byte R, G, B; public bool IsTex; public bool Additive; public bool ScreenTex;
		public byte BaseR, BaseG, BaseB;   // base colour blended under a ScreenTex (mirror) sample
		public bool Sprite, Cutout;        // Sprite: Tex is ARGB (per-texel alpha). Cutout: alpha-test + depth write (HD2D); else blend
	}

	/// <summary>
	/// Transforms + lights a subset of the scene's objects into a screen-space <see cref="RTri"/>
	/// list. Each Builder owns all its scratch, so several run in parallel (one per worker) with no
	/// shared state — this spreads the per-frame geometry rebuild (the rasterizer's hot bottleneck)
	/// across CPU cores.
	/// </summary>
	internal sealed class Builder {
		private Lua3DScene s;
		private bool _lit, _gour, _screenT;
		private byte _baseR, _baseG, _baseB;

		private readonly double[] _vx = new double[8], _vy = new double[8], _vz = new double[8], _vu = new double[8], _vv = new double[8];
		private readonly double[] _ox = new double[8], _oy = new double[8], _oz = new double[8], _ou = new double[8], _ov = new double[8];
		private readonly double[] _sx = new double[8], _sy = new double[8], _sw = new double[8], _su = new double[8], _sv = new double[8];
		private readonly double[] _vlr = new double[8], _vlg = new double[8], _vlb = new double[8];
		private readonly double[] _olr = new double[8], _olg = new double[8], _olb = new double[8];
		private readonly double[] _slr = new double[8], _slg = new double[8], _slb = new double[8];
		private readonly List<int> _objLights = new();

		public RTri[] Tris = new RTri[2048];
		public int TriN;

		public void Reset(Lua3DScene scene, bool lit) { s = scene; _lit = lit; TriN = 0; }

		// painter's order for the transparent pass: farthest first (smaller W = 1/z = farther)
		private sealed class TriDepthCmp : System.Collections.Generic.IComparer<RTri> {
			public int Compare(RTri a, RTri b) {
				double da = a.W0 + a.W1 + a.W2, db = b.W0 + b.W1 + b.W2;
				return da < db ? -1 : (da > db ? 1 : 0);
			}
		}
		private static readonly TriDepthCmp _triCmp = new();
		public void SortByDepth() { if (TriN > 1) Array.Sort(Tris, 0, TriN, _triCmp); }

		private void ToCam(double wx, double wy, double wz, out double cx, out double cy, out double cz) {
			double rx = wx - s._camX, ry = wy - s._camY, rz = wz - s._camZ;
			cx = rx * s._Rx + ry * s._Ry + rz * s._Rz;
			cy = rx * s._Ux + ry * s._Uy + rz * s._Uz;
			cz = rx * s._Fx + ry * s._Fy + rz * s._Fz;
		}

		private int ClipNear(int n) {
			int m = 0; double near = s._near;
			for (int i = 0; i < n; i++) {
				int j = (i + 1) % n;
				double czi = _vz[i], czj = _vz[j];
				bool inI = czi >= near, inJ = czj >= near;
				if (inI) {
					_ox[m] = _vx[i]; _oy[m] = _vy[i]; _oz[m] = _vz[i]; _ou[m] = _vu[i]; _ov[m] = _vv[i];
					if (_gour) { _olr[m] = _vlr[i]; _olg[m] = _vlg[i]; _olb[m] = _vlb[i]; }
					m++;
				}
				if (inI != inJ) {
					double t = (near - czi) / (czj - czi);
					_ox[m] = _vx[i] + (_vx[j] - _vx[i]) * t;
					_oy[m] = _vy[i] + (_vy[j] - _vy[i]) * t;
					_oz[m] = near;
					_ou[m] = _vu[i] + (_vu[j] - _vu[i]) * t;
					_ov[m] = _vv[i] + (_vv[j] - _vv[i]) * t;
					if (_gour) {
						_olr[m] = _vlr[i] + (_vlr[j] - _vlr[i]) * t;
						_olg[m] = _vlg[i] + (_vlg[j] - _vlg[i]) * t;
						_olb[m] = _vlb[i] + (_vlb[j] - _vlb[i]) * t;
					}
					m++;
				}
			}
			return m;
		}

		private void Project(int m) {
			double hw = s._w * 0.5, hh = s._h * 0.5, scale = s._scale;
			for (int i = 0; i < m; i++) {
				double iz = 1.0 / _oz[i];
				_sx[i] = hw + _ox[i] * iz * scale;
				_sy[i] = hh - _oy[i] * iz * scale;
				_sw[i] = iz;
				_su[i] = _ou[i] * iz;
				_sv[i] = _ov[i] * iz;
				if (_gour) { _slr[i] = _olr[i] * iz; _slg[i] = _olg[i] * iz; _slb[i] = _olb[i] * iz; }
			}
		}

		private void AddTexTri(int i0, int i1, int i2, int[] tex, int tw, int th, double shade, int alpha,
			bool gouraud, double flR, double flG, double flB) {
			if (TriN == Tris.Length) Array.Resize(ref Tris, Tris.Length * 2);
			ref RTri t = ref Tris[TriN++];
			t.IsTex = true; t.Sprite = false; t.Tex = tex; t.Tw = tw; t.Th = th; t.Shade = shade; t.Alpha = alpha; t.ScreenTex = _screenT;
			t.BaseR = _baseR; t.BaseG = _baseG; t.BaseB = _baseB;
			t.X0 = _sx[i0]; t.Y0 = _sy[i0]; t.W0 = _sw[i0]; t.U0 = _su[i0]; t.V0 = _sv[i0];
			t.X1 = _sx[i1]; t.Y1 = _sy[i1]; t.W1 = _sw[i1]; t.U1 = _su[i1]; t.V1 = _sv[i1];
			t.X2 = _sx[i2]; t.Y2 = _sy[i2]; t.W2 = _sw[i2]; t.U2 = _su[i2]; t.V2 = _sv[i2];
			t.Flat = !gouraud; t.FlR = flR; t.FlG = flG; t.FlB = flB;
			if (gouraud) {
				t.Lr0 = _slr[i0]; t.Lg0 = _slg[i0]; t.Lb0 = _slb[i0];
				t.Lr1 = _slr[i1]; t.Lg1 = _slg[i1]; t.Lb1 = _slb[i1];
				t.Lr2 = _slr[i2]; t.Lg2 = _slg[i2]; t.Lb2 = _slb[i2];
			}
		}
		private void AddFlatTri(int i0, int i1, int i2, byte r, byte g, byte b, int alpha,
			bool gouraud, double flR, double flG, double flB, bool additive = false) {
			if (TriN == Tris.Length) Array.Resize(ref Tris, Tris.Length * 2);
			ref RTri t = ref Tris[TriN++];
			t.IsTex = false; t.Sprite = false; t.R = r; t.G = g; t.B = b; t.Alpha = alpha; t.Additive = additive;
			t.X0 = _sx[i0]; t.Y0 = _sy[i0]; t.W0 = _sw[i0];
			t.X1 = _sx[i1]; t.Y1 = _sy[i1]; t.W1 = _sw[i1];
			t.X2 = _sx[i2]; t.Y2 = _sy[i2]; t.W2 = _sw[i2];
			t.Flat = !gouraud; t.FlR = flR; t.FlG = flG; t.FlB = flB;
			if (gouraud) {
				t.Lr0 = _slr[i0]; t.Lg0 = _slg[i0]; t.Lb0 = _slb[i0];
				t.Lr1 = _slr[i1]; t.Lg1 = _slg[i1]; t.Lb1 = _slb[i1];
				t.Lr2 = _slr[i2]; t.Lg2 = _slg[i2]; t.Lb2 = _slb[i2];
			}
		}

		// alpha-textured sprite tri (for billboarded HD2D sprite objects). Tex is ARGB; tinted by
		// (tr,tg,tb); cutout = alpha-test + depth write (crisp, occludes), else alpha blend (no write).
		private void AddSpriteTri(int i0, int i1, int i2, int[] tex, int tw, int th,
			double tr, double tg, double tb, int alpha, bool additive, bool cutout) {
			if (TriN == Tris.Length) Array.Resize(ref Tris, Tris.Length * 2);
			ref RTri t = ref Tris[TriN++];
			t.IsTex = true; t.Sprite = true; t.Cutout = cutout; t.ScreenTex = false; t.Additive = additive;
			t.Tex = tex; t.Tw = tw; t.Th = th; t.Alpha = alpha;
			t.Flat = true; t.FlR = tr; t.FlG = tg; t.FlB = tb;
			t.X0 = _sx[i0]; t.Y0 = _sy[i0]; t.W0 = _sw[i0]; t.U0 = _su[i0]; t.V0 = _sv[i0];
			t.X1 = _sx[i1]; t.Y1 = _sy[i1]; t.W1 = _sw[i1]; t.U1 = _su[i1]; t.V1 = _sv[i1];
			t.X2 = _sx[i2]; t.Y2 = _sy[i2]; t.W2 = _sw[i2]; t.U2 = _su[i2]; t.V2 = _sv[i2];
		}

		private static void FaceNormal(SceneObject o, double ax, double ay, double az, double bx, double by, double bz,
			double cx, double cy, double cz, out double nx, out double ny, out double nz) {
			if (o.HasNormal) { nx = o.Nx; ny = o.Ny; nz = o.Nz; }
			else {
				double e1x = bx - ax, e1y = by - ay, e1z = bz - az;
				double e2x = cx - ax, e2y = cy - ay, e2z = cz - az;
				nx = e1y * e2z - e1z * e2y; ny = e1z * e2x - e1x * e2z; nz = e1x * e2y - e1y * e2x;
			}
			double l = Math.Sqrt(nx * nx + ny * ny + nz * nz);
			if (l > 1e-9) { nx /= l; ny /= l; nz /= l; }
		}

		// Global ambient + directional sun (both gated by the face's sky exposure, via sqrt so partial
		// cover only dims gently while full cover reaches black) + the point lights reaching this object.
		private void LightAt(double wx, double wy, double wz, double nx, double ny, double nz, double shade,
			out double lr, out double lg, out double lb) {
			double sndl = nx * s._sunX + ny * s._sunY + nz * s._sunZ; if (sndl < 0) sndl = 0;
			double e = shade <= 0 ? 0 : Math.Sqrt(shade);
			lr = e * (s._ambR + sndl * s._sunR);
			lg = e * (s._ambG + sndl * s._sunG);
			lb = e * (s._ambB + sndl * s._sunB);
			var lights = s._lights;
			for (int j = 0; j < _objLights.Count; j++) {
				var L = lights[_objLights[j]];
				double dx = L.X - wx, dy = L.Y - wy, dz = L.Z - wz;
				double d2 = dx * dx + dy * dy + dz * dz;
				double att;
				if (L.Range > 0) {
					if (d2 >= L.Range * L.Range) continue;
					double dlen = Math.Sqrt(d2);
					double inv = dlen > 1e-9 ? 1.0 / dlen : 0;
					double ndl = (nx * dx + ny * dy + nz * dz) * inv; if (ndl <= 0) continue;
					double f = 1.0 - dlen / L.Range;
					att = f * f * ndl;
				} else {
					double inv = d2 > 1e-6 ? 1.0 / Math.Sqrt(d2) : 0;
					double ndl = (nx * dx + ny * dy + nz * dz) * inv; if (ndl <= 0) continue;
					att = ndl / (d2 + 1.0);
				}
				lr += L.R * att; lg += L.G * att; lb += L.B * att;
			}
		}

		private bool Uniform(int n) {
			double r = _vlr[0], g = _vlg[0], b = _vlb[0];
			for (int i = 1; i < n; i++)
				if (Math.Abs(_vlr[i] - r) > 0.02 || Math.Abs(_vlg[i] - g) > 0.02 || Math.Abs(_vlb[i] - b) > 0.02) return false;
			return true;
		}

		private void GatherObjLights(SceneObject o) {
			_objLights.Clear();
			var lights = s._lights;
			for (int i = 0; i < lights.Count; i++) {
				var L = lights[i];
				if (L.Range > 0 && o.HasBounds) {
					double cx = L.X < o.MinX ? o.MinX : (L.X > o.MaxX ? o.MaxX : L.X);
					double cy = L.Y < o.MinY ? o.MinY : (L.Y > o.MaxY ? o.MaxY : L.Y);
					double cz = L.Z < o.MinZ ? o.MinZ : (L.Z > o.MaxZ ? o.MaxZ : L.Z);
					double dx = L.X - cx, dy = L.Y - cy, dz = L.Z - cz;
					if (dx * dx + dy * dy + dz * dz >= L.Range * L.Range) continue;
				}
				_objLights.Add(i);
			}
		}

		public void Build(SceneObject o) {
			var d = o.D; var t = o.Transform;
			_screenT = o.ScreenTex;
			if (_screenT) { _baseR = RenderUtil.CB(o.R); _baseG = RenderUtil.CB(o.G); _baseB = RenderUtil.CB(o.B); }
			bool objLit = _lit && o.Lit;
			if (objLit) GatherObjLights(o); else _objLights.Clear();
			bool perVertex = objLit && _objLights.Count > 0;   // per-vertex only where a torch reaches
			if (o.Kind == 0) {                                  // textured quads
				for (int i = 0; i < o.N; i++) {
					int k = i * 16; int texId = (int)d[k+12];
					if (!s._texPix.TryGetValue(texId, out var tex)) continue;
					int tw = s._texW[texId], th = s._texH[texId];
					double uMax = d[k+13], vMax = d[k+14], shade = d[k+15];
					Xform(t, d[k],   d[k+1], d[k+2],  out double wx0, out double wy0, out double wz0);
					Xform(t, d[k+3], d[k+4], d[k+5],  out double wx1, out double wy1, out double wz1);
					Xform(t, d[k+6], d[k+7], d[k+8],  out double wx2, out double wy2, out double wz2);
					Xform(t, d[k+9], d[k+10],d[k+11], out double wx3, out double wy3, out double wz3);
					ToCam(wx0,wy0,wz0, out _vx[0], out _vy[0], out _vz[0]); _vu[0]=0;    _vv[0]=vMax;
					ToCam(wx1,wy1,wz1, out _vx[1], out _vy[1], out _vz[1]); _vu[1]=uMax; _vv[1]=vMax;
					ToCam(wx2,wy2,wz2, out _vx[2], out _vy[2], out _vz[2]); _vu[2]=uMax; _vv[2]=0;
					ToCam(wx3,wy3,wz3, out _vx[3], out _vy[3], out _vz[3]); _vu[3]=0;    _vv[3]=0;
					double flR = shade * o.TintR, flG = shade * o.TintG, flB = shade * o.TintB; bool fg = false;
					if (objLit) {
						FaceNormal(o, wx0,wy0,wz0, wx1,wy1,wz1, wx3,wy3,wz3, out double nx, out double ny, out double nz);
						if (perVertex) {
							LightAt(wx0,wy0,wz0, nx,ny,nz, shade, out _vlr[0], out _vlg[0], out _vlb[0]);
							LightAt(wx1,wy1,wz1, nx,ny,nz, shade, out _vlr[1], out _vlg[1], out _vlb[1]);
							LightAt(wx2,wy2,wz2, nx,ny,nz, shade, out _vlr[2], out _vlg[2], out _vlb[2]);
							LightAt(wx3,wy3,wz3, nx,ny,nz, shade, out _vlr[3], out _vlg[3], out _vlb[3]);
							fg = !Uniform(4); flR = _vlr[0]; flG = _vlg[0]; flB = _vlb[0];
						} else {
							LightAt(wx0,wy0,wz0, nx,ny,nz, shade, out flR, out flG, out flB);
						}
					}
					_gour = fg;
					int m = ClipNear(4); if (m < 3) continue;
					Project(m);
					for (int tri = 1; tri < m - 1; tri++) AddTexTri(0, tri, tri+1, tex, tw, th, shade, o.A, fg, flR, flG, flB);
				}
			} else if (o.Kind == 1) {                           // flat quads (water)
				byte br = RenderUtil.CB(o.R), bg = RenderUtil.CB(o.G), bb = RenderUtil.CB(o.B);
				for (int i = 0; i < o.N; i++) {
					int k = i * 12;
					Xform(t, d[k],   d[k+1], d[k+2],  out double wx0, out double wy0, out double wz0);
					Xform(t, d[k+3], d[k+4], d[k+5],  out double wx1, out double wy1, out double wz1);
					Xform(t, d[k+6], d[k+7], d[k+8],  out double wx2, out double wy2, out double wz2);
					Xform(t, d[k+9], d[k+10],d[k+11], out double wx3, out double wy3, out double wz3);
					ToCam(wx0,wy0,wz0, out _vx[0], out _vy[0], out _vz[0]);
					ToCam(wx1,wy1,wz1, out _vx[1], out _vy[1], out _vz[1]);
					ToCam(wx2,wy2,wz2, out _vx[2], out _vy[2], out _vz[2]);
					ToCam(wx3,wy3,wz3, out _vx[3], out _vy[3], out _vz[3]);
					double flR = 1.0, flG = 1.0, flB = 1.0; bool fg = false;
					if (objLit) {
						FaceNormal(o, wx0,wy0,wz0, wx1,wy1,wz1, wx3,wy3,wz3, out double nx, out double ny, out double nz);
						if (perVertex) {
							LightAt(wx0,wy0,wz0, nx,ny,nz, 1.0, out _vlr[0], out _vlg[0], out _vlb[0]);
							LightAt(wx1,wy1,wz1, nx,ny,nz, 1.0, out _vlr[1], out _vlg[1], out _vlb[1]);
							LightAt(wx2,wy2,wz2, nx,ny,nz, 1.0, out _vlr[2], out _vlg[2], out _vlb[2]);
							LightAt(wx3,wy3,wz3, nx,ny,nz, 1.0, out _vlr[3], out _vlg[3], out _vlb[3]);
							fg = !Uniform(4); flR = _vlr[0]; flG = _vlg[0]; flB = _vlb[0];
						} else {
							LightAt(wx0,wy0,wz0, nx,ny,nz, 1.0, out flR, out flG, out flB);
						}
					}
					_gour = fg;
					int m = ClipNear(4); if (m < 3) continue;
					Project(m);
					for (int tri = 1; tri < m - 1; tri++) AddFlatTri(0, tri, tri+1, br, bg, bb, o.A, fg, flR, flG, flB);
				}
			} else if (o.Kind == 2) {                           // textured triangles (models)
				for (int i = 0; i < o.N; i++) {
					int k = i * 17; int texId = (int)d[k+15];
					if (!s._texPix.TryGetValue(texId, out var tex)) continue;
					int tw = s._texW[texId], th = s._texH[texId]; double shade = d[k+16];
					Xform(t, d[k],    d[k+1],  d[k+2],  out double wx0, out double wy0, out double wz0);
					Xform(t, d[k+5],  d[k+6],  d[k+7],  out double wx1, out double wy1, out double wz1);
					Xform(t, d[k+10], d[k+11], d[k+12], out double wx2, out double wy2, out double wz2);
					ToCam(wx0,wy0,wz0, out _vx[0], out _vy[0], out _vz[0]); _vu[0]=d[k+3];  _vv[0]=d[k+4];
					ToCam(wx1,wy1,wz1, out _vx[1], out _vy[1], out _vz[1]); _vu[1]=d[k+8];  _vv[1]=d[k+9];
					ToCam(wx2,wy2,wz2, out _vx[2], out _vy[2], out _vz[2]); _vu[2]=d[k+13]; _vv[2]=d[k+14];
					double flR = shade * o.TintR, flG = shade * o.TintG, flB = shade * o.TintB; bool fg = false;
					if (objLit) {
						FaceNormal(o, wx0,wy0,wz0, wx1,wy1,wz1, wx2,wy2,wz2, out double nx, out double ny, out double nz);
						if (perVertex) {
							LightAt(wx0,wy0,wz0, nx,ny,nz, shade, out _vlr[0], out _vlg[0], out _vlb[0]);
							LightAt(wx1,wy1,wz1, nx,ny,nz, shade, out _vlr[1], out _vlg[1], out _vlb[1]);
							LightAt(wx2,wy2,wz2, nx,ny,nz, shade, out _vlr[2], out _vlg[2], out _vlb[2]);
							fg = !Uniform(3); flR = _vlr[0]; flG = _vlg[0]; flB = _vlb[0];
						} else {
							LightAt(wx0,wy0,wz0, nx,ny,nz, shade, out flR, out flG, out flB);
						}
					}
					_gour = fg;
					int m = ClipNear(3); if (m < 3) continue;
					Project(m);
					for (int tri = 1; tri < m - 1; tri++) AddTexTri(0, tri, tri+1, tex, tw, th, shade, o.A, fg, flR, flG, flB);
				}
			} else if (o.Kind == 3) {                           // billboarded alpha sprites (HD2D 2D-in-3D)
				for (int i = 0; i < o.N; i++) {
					int k = i * 8; int spriteId = (int)d[k + 5];
					if (!s._spritePix.TryGetValue(spriteId, out var tex)) continue;
					int tw = s._spriteW[spriteId], th = s._spriteH[spriteId];
					double w = d[k + 3], hgt = d[k + 4]; int bb = (int)d[k + 6]; bool cut = d[k + 7] != 0;
					Xform(t, d[k], d[k + 1], d[k + 2], out double wcx, out double wcy, out double wcz);
					// billboard basis: 2 = full (camera right/up), 1 = Y-axis (face camera, upright; HD2D), else fixed XY
					double rx, ry, rz, ux, uy, uz;
					if (bb == 2) { rx = s._Rx; ry = s._Ry; rz = s._Rz; ux = s._Ux; uy = s._Uy; uz = s._Uz; }
					else if (bb == 1) { rx = s._Rx; ry = 0; rz = s._Rz; double rl = Math.Sqrt(rx * rx + rz * rz); if (rl > 1e-6) { rx /= rl; rz /= rl; } ux = 0; uy = 1; uz = 0; }
					else { rx = 1; ry = 0; rz = 0; ux = 0; uy = 1; uz = 0; }
					double hw2 = w * 0.5;                                  // bottom-anchored: (cx,cy,cz) is the foot
					double b0x = wcx - rx * hw2, b0y = wcy - ry * hw2, b0z = wcz - rz * hw2;
					double b1x = wcx + rx * hw2, b1y = wcy + ry * hw2, b1z = wcz + rz * hw2;
					double tux = ux * hgt, tuy = uy * hgt, tuz = uz * hgt;
					ToCam(b0x, b0y, b0z, out _vx[0], out _vy[0], out _vz[0]); _vu[0] = 0; _vv[0] = 1;
					ToCam(b1x, b1y, b1z, out _vx[1], out _vy[1], out _vz[1]); _vu[1] = 1; _vv[1] = 1;
					ToCam(b1x + tux, b1y + tuy, b1z + tuz, out _vx[2], out _vy[2], out _vz[2]); _vu[2] = 1; _vv[2] = 0;
					ToCam(b0x + tux, b0y + tuy, b0z + tuz, out _vx[3], out _vy[3], out _vz[3]); _vu[3] = 0; _vv[3] = 0;
					_gour = false;
					int m = ClipNear(4); if (m < 3) continue;
					Project(m);
					for (int tri = 1; tri < m - 1; tri++) AddSpriteTri(0, tri, tri + 1, tex, tw, th, o.TintR, o.TintG, o.TintB, o.A, false, cut);
				}
			}
		}

		// (Particles are no longer billboarded as triangles here — they render in the rasterizer's
		// dedicated point-sprite pass, which is far cheaper for the many-small-billboard case.)

		// model->world (row-major 4x4 . point), or identity when t is null.
		private static void Xform(double[] t, double x, double y, double z, out double ox, out double oy, out double oz) {
			if (t == null) { ox = x; oy = y; oz = z; return; }
			ox = t[0]*x + t[1]*y + t[2]*z + t[3];
			oy = t[4]*x + t[5]*y + t[6]*z + t[7];
			oz = t[8]*x + t[9]*y + t[10]*z + t[11];
		}
	}

	/// <summary>
	/// Software rasterizer for <see cref="Lua3DScene"/>. Splits the scene's objects across several
	/// <see cref="Builder"/> workers (parallel transform + lighting), then fills the resulting
	/// triangles with a perspective-correct, z-buffered, fog-aware rasterizer parallelised over
	/// horizontal bands. Opaque builders are kept in near→far order for early-z; the transparent
	/// pass is built serially (sorted) and drawn last.
	/// </summary>
	internal sealed class Rasterizer : IRenderer {
		private Lua3DScene _s;
		private bool _lit;
		private Builder[] _builders = Array.Empty<Builder>();
		private Builder _transBuilder;
		private Builder _overlayBuilder;
		private int _nb;
		private readonly List<SceneObject> _opaque = new();
		private readonly List<SceneObject> _transp = new();
		private readonly List<SceneObject> _overlay = new();
		private static readonly Comparison<SceneObject> _nearFirst = (a, b) => a.Dist.CompareTo(b.Dist);
		private static readonly Comparison<SceneObject> _farFirst = (a, b) => b.Dist.CompareTo(a.Dist);

		public void Invalidate() { }   // stateless between frames

		public void Render(Lua3DScene s) {
			_s = s;
			_lit = s._useLights;
			Array.Clear(s._depth, 0, s._depth.Length);

			_opaque.Clear(); _transp.Clear(); _overlay.Clear();
			foreach (var o in s._objects.Values) {
				if (!o.Visible || o.N == 0) continue;
				if (s._rttPass && (o.Overlay || o.ScreenTex)) continue;   // no viewmodels/mirrors inside RTT views
				if (o.Overlay) { _overlay.Add(o); continue; }   // viewmodel: drawn last, depth-cleared
				if (Culled(o)) continue;
				o.Dist = DistSq(o);
				if (o.Pass == 0) _opaque.Add(o); else _transp.Add(o);
			}
			_opaque.Sort(_nearFirst);
			_transp.Sort(_farFirst);

			int want = s._threads < 1 ? 1 : s._threads;
			if (_builders.Length < want) {
				var nbld = new Builder[want];
				for (int i = 0; i < want; i++) nbld[i] = i < _builders.Length ? _builders[i] : new Builder();
				_builders = nbld;
			}
			_transBuilder ??= new Builder();

			// Parallel build: partition the (near→far sorted) opaque list into contiguous slices, one
			// per builder, so iterating builders 0..nb-1 stays roughly near→far for early-z.
			_nb = _opaque.Count == 0 ? 0 : Math.Min(want, _opaque.Count);
			for (int i = 0; i < _nb; i++) _builders[i].Reset(s, _lit);
			if (_nb == 1) {
				for (int k = 0; k < _opaque.Count; k++) _builders[0].Build(_opaque[k]);
			} else if (_nb > 1) {
				Parallel.For(0, _nb, s._po, slot => {
					var bld = _builders[slot];
					int lo = slot * _opaque.Count / _nb, hi = (slot + 1) * _opaque.Count / _nb;
					for (int k = lo; k < hi; k++) bld.Build(_opaque[k]);
				});
			}
			_transBuilder.Reset(s, _lit);
			for (int k = 0; k < _transp.Count; k++) _transBuilder.Build(_transp[k]);
			_transBuilder.SortByDepth();    // back→front so glass dims what's behind it

			int n = s._threads;
			if (n <= 1) RasterizeBand(0, s._h);
			else Parallel.For(0, n, s._po, bi => RasterizeBand(bi * s._h / n, (bi + 1) * s._h / n));
			// NB: one band per thread (not n×4). RasterizeBand re-scans EVERY triangle per band, so more
			// bands = more per-triangle rescans — costly for many-triangle scenes like voxel.

			// Particles: a dedicated point-sprite pass. Billboards are camera-facing flat quads → they
			// project to axis-aligned screen rectangles at a single depth, so we skip the whole triangle
			// pipeline (no per-tri setup, no per-pixel perspective divide, half the sort).
			ProjectParticles(s);
			if (_ppN > 0) {
				SortParticles();
				if (n <= 1) RenderParticleBand(s, 0, s._h);
				else Parallel.For(0, n, s._po, bi => RenderParticleBand(s, bi * s._h / n, (bi + 1) * s._h / n));
			}

			// Overlay pass (first-person viewmodel): clear depth so it draws on top of the world, then
			// rasterize the overlay tris — multithreaded over row bands like the main fill, because the
			// weapon model fills a big close-up chunk of the screen (this was a single-threaded hot spot).
			if (_overlay.Count > 0) {
				Array.Clear(s._depth, 0, s._depth.Length);
				_overlayBuilder ??= new Builder();
				_overlayBuilder.Reset(s, _lit);
				for (int k = 0; k < _overlay.Count; k++) _overlayBuilder.Build(_overlay[k]);
				int on = s._threads;
				if (on <= 1) RasterTris(_overlayBuilder, 0, s._h);
				else {
					int ob = on * 4;
					Parallel.For(0, ob, s._po, bi => RasterTris(_overlayBuilder, bi * s._h / ob, (bi + 1) * s._h / ob));
				}
			}
			// Optional post-process antialiasing (smooths the low-res render's edges before upscale).
			// Skipped inside RTT passes (reflections don't need it).
			if (s._aa && !s._rttPass) ApplyAA(s);
			s._canvas.MarkDirty(0, 0, s._w - 1, s._h - 1);
		}

		// ── dedicated particle (point-sprite) pass ────────────────────────────
		// A billboard uses the camera right/up axes, so in camera space its 4 corners share the particle's
		// depth and project to an axis-aligned screen rectangle. We exploit that: project each particle to
		// (centre, half-size, 1/z), depth-sort once, then fill rectangles with affine sprite UVs — no
		// triangles, no invA setup, no per-pixel divide, no per-band triangle rescan.
		private struct PP {
			public double Sx, Sy, Half; public float W;
			public int[]? Spr; public int Sw, Sh;
			public double R, G, B, A; public bool Add;
		}
		private PP[] _pp = new PP[2048];
		private int _ppN;
		private int[] _ppIdx = new int[2048];
		private double[] _ppDepth = new double[2048];
		private sealed class DepthCmp : System.Collections.Generic.IComparer<int> { public double[] D = System.Array.Empty<double>(); public int Compare(int a, int b) => D[b].CompareTo(D[a]); }
		private readonly DepthCmp _ppCmp = new();

		private void ProjectParticles(Lua3DScene s) {
			_ppN = 0;
			var systems = s._particleSystems;
			if (systems == null || systems.Count == 0) return;
			double hw = s._w * 0.5, hh = s._h * 0.5, scale = s._scale, near = s._near;
			int W = s._w, H = s._h;
			double cxp = s._camX, cyp = s._camY, czp = s._camZ;
			double Rx = s._Rx, Ry = s._Ry, Rz = s._Rz, Ux = s._Ux, Uy = s._Uy, Uz = s._Uz, Fx = s._Fx, Fy = s._Fy, Fz = s._Fz;
			for (int si = 0; si < systems.Count; si++) {
				var psys = systems[si]; var arr = psys.P; int cnt = psys.Count;
				for (int i = 0; i < cnt; i++) {
					ref Particle p = ref arr[i];
					double lf = p.MaxLife > 0 ? p.Life / p.MaxLife : 0; if (lf < 0) lf = 0; else if (lf > 1) lf = 1;
					double a = p.A0 * lf; if (a <= 0) continue; if (a > 1) a = 1;
					double rxw = p.X - cxp, ryw = p.Y - cyp, rzw = p.Z - czp;
					double cz = rxw * Fx + ryw * Fy + rzw * Fz; if (cz < near) continue;
					double w = 1.0 / cz;
					double sx = hw + (rxw * Rx + ryw * Ry + rzw * Rz) * w * scale;
					double sy = hh - (rxw * Ux + ryw * Uy + rzw * Uz) * w * scale;
					double half = (p.Size0 + (p.Size1 - p.Size0) * (1.0 - lf)) * 0.5 * w * scale;
					if (half < 0.5) half = 0.5;   // keep tiny/distant particles at least ~1px (stars, sparks)
					if (sx + half < 0 || sx - half >= W || sy + half < 0 || sy - half >= H) continue;
					if (_ppN == _pp.Length) { System.Array.Resize(ref _pp, _pp.Length * 2); System.Array.Resize(ref _ppIdx, _pp.Length); System.Array.Resize(ref _ppDepth, _pp.Length); }
					int[]? spr = null; int sw = 0, sh = 0;
					if (p.Sprite >= 0 && s._spritePix.TryGetValue(p.Sprite, out var sp)) { spr = sp; sw = s._spriteW[p.Sprite]; sh = s._spriteH[p.Sprite]; }
					_ppDepth[_ppN] = cz;
					_pp[_ppN++] = new PP { Sx = sx, Sy = sy, Half = half, W = (float)w, Spr = spr, Sw = sw, Sh = sh, R = p.R, G = p.G, B = p.B, A = a, Add = p.Additive };
				}
			}
		}

		private void SortParticles() {
			for (int i = 0; i < _ppN; i++) _ppIdx[i] = i;
			_ppCmp.D = _ppDepth;
			System.Array.Sort(_ppIdx, 0, _ppN, _ppCmp);   // far -> near (back-to-front) for correct alpha
		}

		private unsafe void RenderParticleBand(Lua3DScene s, int by0, int by1) {
			int W = s._w;
			fixed (byte* B = s._canvas._buf) fixed (float* Dp = s._depth) {
				for (int k = 0; k < _ppN; k++) {
					ref PP p = ref _pp[_ppIdx[k]];
					double half = p.Half, left = p.Sx - half, top = p.Sy - half, inv = 1.0 / (2 * half);
					int x0 = (int)System.Math.Floor(p.Sx - half); if (x0 < 0) x0 = 0;
					int x1 = (int)System.Math.Ceiling(p.Sx + half); if (x1 > W - 1) x1 = W - 1;
					int y0 = (int)System.Math.Floor(p.Sy - half); if (y0 < by0) y0 = by0;
					int y1 = (int)System.Math.Ceiling(p.Sy + half); if (y1 > by1 - 1) y1 = by1 - 1;
					if (x0 > x1 || y0 > y1) continue;
					float fw = p.W; bool add = p.Add; double a = p.A;
					if (p.Spr != null) {
						int sw = p.Sw, sh = p.Sh;
						double tr = p.R * (1.0 / 255), tg = p.G * (1.0 / 255), tb = p.B * (1.0 / 255);
						double du = inv * sw, dv = inv * sh, u0 = (x0 + 0.5 - left) * du;
						fixed (int* T = p.Spr) {
							double vf = (y0 + 0.5 - top) * dv;
							for (int py = y0; py <= y1; py++, vf += dv) {
								int syi = (int)vf; if (syi < 0) syi = 0; else if (syi >= sh) syi = sh - 1;
								int rowS = syi * sw, rowB = py * W; double uf = u0;
								for (int px = x0; px <= x1; px++, uf += du) {
									int idx = rowB + px;
									if (fw > Dp[idx]) {
										int sxi = (int)uf; if (sxi < 0) sxi = 0; else if (sxi >= sw) sxi = sw - 1;
										int pk = T[rowS + sxi]; int ta = (pk >> 24) & 0xFF;
										if (ta != 0) {
											double pa = (ta * 0.003921568627) * a;
											double sr = ((pk >> 16) & 0xFF) * tr, sg = ((pk >> 8) & 0xFF) * tg, sb = (pk & 0xFF) * tb;
											int o = idx * 4;
											if (add) { B[o] = RenderUtil.CB(sr * pa + B[o]); B[o + 1] = RenderUtil.CB(sg * pa + B[o + 1]); B[o + 2] = RenderUtil.CB(sb * pa + B[o + 2]); B[o + 3] = 255; }
											else { double ia = 1 - pa; B[o] = (byte)(sr * pa + B[o] * ia); B[o + 1] = (byte)(sg * pa + B[o + 1] * ia); B[o + 2] = (byte)(sb * pa + B[o + 2] * ia); B[o + 3] = 255; }
										}
									}
								}
							}
						}
					} else {
						double sr = p.R, sg = p.G, sb = p.B;
						for (int py = y0; py <= y1; py++) {
							int rowB = py * W;
							for (int px = x0; px <= x1; px++) {
								int idx = rowB + px;
								if (fw > Dp[idx]) {
									int o = idx * 4;
									if (add) { B[o] = RenderUtil.CB(sr * a + B[o]); B[o + 1] = RenderUtil.CB(sg * a + B[o + 1]); B[o + 2] = RenderUtil.CB(sb * a + B[o + 2]); B[o + 3] = 255; }
									else { double ia = 1 - a; B[o] = (byte)(sr * a + B[o] * ia); B[o + 1] = (byte)(sg * a + B[o + 1] * ia); B[o + 2] = (byte)(sb * a + B[o + 2] * ia); B[o + 3] = 255; }
								}
							}
						}
					}
				}
			}
		}

		// ── FXAA-lite: a cheap edge-aware blur over the final colour buffer ──────────────────────────
		// Finds pixels on a luma edge and blends them across the edge with their two perpendicular
		// neighbours (centre 50%, each side 25%). Not full FXAA (no sub-pixel directional search) but it
		// noticeably softens the jaggies of a low-res software render. Multithreaded; reads from a
		// snapshot so neighbour reads stay consistent.
		private byte[]? _aaBuf;
		private void ApplyAA(Lua3DScene s) {
			int W = s._w, H = s._h, n = W * H * 4;
			if (_aaBuf == null || _aaBuf.Length < n) _aaBuf = new byte[n];
			Array.Copy(s._canvas._buf, _aaBuf, n);   // immutable source for this pass
			if (s._threads <= 1) FXAABand(s, 0, H);
			else { int b = s._threads * 4; Parallel.For(0, b, s._po, bi => FXAABand(s, bi * H / b, (bi + 1) * H / b)); }
		}
		private unsafe void FXAABand(Lua3DScene s, int y0, int y1) {
			int W = s._w, H = s._h, row = W * 4;
			if (y0 < 1) y0 = 1; if (y1 > H - 1) y1 = H - 1;
			fixed (byte* S = _aaBuf) fixed (byte* D = s._canvas._buf) {
				for (int y = y0; y < y1; y++) {
					int i = (y * W + 1) * 4;
					for (int x = 1; x < W - 1; x++, i += 4) {
						int lC = S[i] + (S[i + 1] << 1) + S[i + 2];           // relative luma 0..1020
						int lN = S[i - row] + (S[i - row + 1] << 1) + S[i - row + 2];
						int lS = S[i + row] + (S[i + row + 1] << 1) + S[i + row + 2];
						int lE = S[i + 4] + (S[i + 5] << 1) + S[i + 6];
						int lW = S[i - 4] + (S[i - 3] << 1) + S[i - 2];
						int mn = lC, mx = lC;
						if (lN < mn) mn = lN; else if (lN > mx) mx = lN;
						if (lS < mn) mn = lS; else if (lS > mx) mx = lS;
						if (lE < mn) mn = lE; else if (lE > mx) mx = lE;
						if (lW < mn) mn = lW; else if (lW > mx) mx = lW;
						int range = mx - mn;
						if (range < 64 || range * 8 < mx) continue;          // flat area → leave the original pixel
						int a, b;
						if (System.Math.Abs(lN + lS - 2 * lC) >= System.Math.Abs(lE + lW - 2 * lC)) { a = i - row; b = i + row; }
						else { a = i - 4; b = i + 4; }
						D[i]     = (byte)(((S[i]     << 1) + S[a]     + S[b])     >> 2);
						D[i + 1] = (byte)(((S[i + 1] << 1) + S[a + 1] + S[b + 1]) >> 2);
						D[i + 2] = (byte)(((S[i + 2] << 1) + S[a + 2] + S[b + 2]) >> 2);
					}
				}
			}
		}

		private void RasterTris(Builder bld, int by0, int by1) {
			var ts = bld.Tris; int tn = bld.TriN;
			for (int i = 0; i < tn; i++) {
				if (ts[i].IsTex) RasterTexBand(ref ts[i], by0, by1);
				else RasterFlatBand(ref ts[i], by0, by1);
			}
		}

		private void RasterizeBand(int by0, int by1) {
			for (int bi = 0; bi < _nb; bi++) RasterTris(_builders[bi], by0, by1);
			RasterTris(_transBuilder, by0, by1);
		}

		private bool Culled(SceneObject o) {
			var s = _s;
			if (o.HasBounds) {
				double vx = o.CenX - s._camX, vy = o.CenY - s._camY, vz = o.CenZ - s._camZ;
				double radius = o.Radius;
				if (s._renderDist > 0) {
					double rr = s._renderDist + radius;           // sqrt-free distance cull
					if (vx*vx + vy*vy + vz*vz > rr*rr) return true;
				}
				if (s._Fx*vx + s._Fy*vy + s._Fz*vz < -radius) return true;
			}
			if (o.HasNormal) {
				if (o.Nx > 0 && s._camX <= o.MinX) return true;
				if (o.Nx < 0 && s._camX >= o.MaxX) return true;
				if (o.Ny > 0 && s._camY <= o.MinY) return true;
				if (o.Ny < 0 && s._camY >= o.MaxY) return true;
				if (o.Nz > 0 && s._camZ <= o.MinZ) return true;
				if (o.Nz < 0 && s._camZ >= o.MaxZ) return true;
			}
			return false;
		}

		private double DistSq(SceneObject o) {
			var s = _s;
			double vx = o.CenX - s._camX, vy = o.CenY - s._camY, vz = o.CenZ - s._camZ;
			return vx*vx + vy*vy + vz*vz;   // CenX/Y/Z are 0 for unbounded objects (same as before)
		}

		private unsafe void RasterFlatBand(ref RTri t, int by0, int by1) {
			var s = _s;
			double x0 = t.X0, y0 = t.Y0, w0 = t.W0;
			double x1 = t.X1, y1 = t.Y1, w1 = t.W1;
			double x2 = t.X2, y2 = t.Y2, w2 = t.W2;
			double lr0 = t.Lr0, lg0 = t.Lg0, lb0 = t.Lb0, lr1 = t.Lr1, lg1 = t.Lg1, lb1 = t.Lb1, lr2 = t.Lr2, lg2 = t.Lg2, lb2 = t.Lb2;
			bool flat = t.Flat; double flR = t.FlR, flG = t.FlG, flB = t.FlB;
			bool gour = _lit && !flat;
			double area = (x1 - x0) * (y2 - y0) - (x2 - x0) * (y1 - y0);
			if (area > -1e-9 && area < 1e-9) return;
			if (area < 0) {
				double tt; tt=x1;x1=x2;x2=tt; tt=y1;y1=y2;y2=tt; tt=w1;w1=w2;w2=tt; area=-area;
				if (gour) { tt=lr1;lr1=lr2;lr2=tt; tt=lg1;lg1=lg2;lg2=tt; tt=lb1;lb1=lb2;lb2=tt; }
			}
			int minX = (int)Math.Floor(Math.Min(x0, Math.Min(x1, x2))); if (minX < 0) minX = 0;
			int maxX = (int)Math.Ceiling(Math.Max(x0, Math.Max(x1, x2))); if (maxX > s._w - 1) maxX = s._w - 1;
			int minY = (int)Math.Floor(Math.Min(y0, Math.Min(y1, y2))); if (minY < by0) minY = by0;
			int maxY = (int)Math.Ceiling(Math.Max(y0, Math.Max(y1, y2))); if (maxY > by1 - 1) maxY = by1 - 1;
			if (minX > maxX || minY > maxY) return;
			byte r = t.R, g = t.G, b = t.B; int alpha = t.Alpha;
			double invA = 1.0 / area;
			double d0x = y1 - y2, d0y = x2 - x1;
			double d1x = y2 - y0, d1y = x0 - x2;
			double d2x = y0 - y1, d2y = x1 - x0;
			double cx = minX + 0.5, cy = minY + 0.5;
			double e0r = (x2 - x1) * (cy - y1) - (y2 - y1) * (cx - x1);
			double e1r = (x0 - x2) * (cy - y2) - (y0 - y2) * (cx - x2);
			double e2r = (x1 - x0) * (cy - y0) - (y1 - y0) * (cx - x0);
			double idr = invA * (e0r * w0 + e1r * w1 + e2r * w2);
			double idDx = invA * (d0x * w0 + d1x * w1 + d2x * w2);
			double idDy = invA * (d0y * w0 + d1y * w1 + d2y * w2);
			double lrR = 0, lrDx = 0, lrDy = 0, lgR = 0, lgDx = 0, lgDy = 0, lbR = 0, lbDx = 0, lbDy = 0;
			if (gour) {
				lrR = invA * (e0r * lr0 + e1r * lr1 + e2r * lr2); lrDx = invA * (d0x * lr0 + d1x * lr1 + d2x * lr2); lrDy = invA * (d0y * lr0 + d1y * lr1 + d2y * lr2);
				lgR = invA * (e0r * lg0 + e1r * lg1 + e2r * lg2); lgDx = invA * (d0x * lg0 + d1x * lg1 + d2x * lg2); lgDy = invA * (d0y * lg0 + d1y * lg1 + d2y * lg2);
				lbR = invA * (e0r * lb0 + e1r * lb1 + e2r * lb2); lbDx = invA * (d0x * lb0 + d1x * lb1 + d2x * lb2); lbDy = invA * (d0y * lb0 + d1y * lb1 + d2y * lb2);
			}
			double fmR = _lit ? flR : 1.0, fmG = _lit ? flG : 1.0, fmB = _lit ? flB : 1.0;
			bool opaque = alpha >= 255; double a = alpha / 255.0, ia = 1.0 - a; bool add = t.Additive;
			bool fog = s._fog; double fr = s._fogR, fg = s._fogG, fb = s._fogB, fstart = s._fogStart, finv = s._fogInv;
			int W = s._w;
			fixed (byte* B = s._canvas._buf) fixed (float* D = s._depth) {
				for (int py = minY; py <= maxY; py++) {
					double E0 = e0r, E1 = e1r, E2 = e2r, ID = idr, LR = lrR, LG = lgR, LB = lbR;
					int row = py * W;
					for (int px = minX; px <= maxX; px++) {
						if (E0 >= 0 && E1 >= 0 && E2 >= 0) {
							int idx = row + px;
							float fid = (float)ID;
							if (fid > D[idx]) {
								int o = idx * 4;
								double cr, cg, cb;
								if (gour) { double iw = 1.0 / ID; cr = r * LR * iw; cg = g * LG * iw; cb = b * LB * iw; }
								else { cr = r * fmR; cg = g * fmG; cb = b * fmB; }
								if (fog && !add) {     // additive glows skip fog (no per-pixel divide)
									double f = (1.0 / ID - fstart) * finv;
									if (f > 0) { if (f > 1) f = 1; cr += (fr - cr) * f; cg += (fg - cg) * f; cb += (fb - cb) * f; }
								}
								if (opaque) { D[idx] = fid; B[o] = RenderUtil.CB(cr); B[o + 1] = RenderUtil.CB(cg); B[o + 2] = RenderUtil.CB(cb); B[o + 3] = 255; }
								else if (add) { B[o] = RenderUtil.CB(cr * a + B[o]); B[o + 1] = RenderUtil.CB(cg * a + B[o + 1]); B[o + 2] = RenderUtil.CB(cb * a + B[o + 2]); B[o + 3] = 255; }
								else { B[o] = (byte)(cr * a + B[o] * ia); B[o + 1] = (byte)(cg * a + B[o + 1] * ia); B[o + 2] = (byte)(cb * a + B[o + 2] * ia); B[o + 3] = 255; }
							}
						}
						E0 += d0x; E1 += d1x; E2 += d2x; ID += idDx;
						if (gour) { LR += lrDx; LG += lgDx; LB += lbDx; }
					}
					e0r += d0y; e1r += d1y; e2r += d2y; idr += idDy;
					if (gour) { lrR += lrDy; lgR += lgDy; lbR += lbDy; }
				}
			}
		}

		private unsafe void RasterTexBand(ref RTri t, int by0, int by1) {
			var s = _s;
			double x0 = t.X0, y0 = t.Y0, w0 = t.W0, uz0 = t.U0, vz0 = t.V0;
			double x1 = t.X1, y1 = t.Y1, w1 = t.W1, uz1 = t.U1, vz1 = t.V1;
			double x2 = t.X2, y2 = t.Y2, w2 = t.W2, uz2 = t.U2, vz2 = t.V2;
			double lr0 = t.Lr0, lg0 = t.Lg0, lb0 = t.Lb0, lr1 = t.Lr1, lg1 = t.Lg1, lb1 = t.Lb1, lr2 = t.Lr2, lg2 = t.Lg2, lb2 = t.Lb2;
			bool flat = t.Flat; double flR = t.FlR, flG = t.FlG, flB = t.FlB;
			bool gour = _lit && !flat;
			double area = (x1 - x0) * (y2 - y0) - (x2 - x0) * (y1 - y0);
			if (area > -1e-9 && area < 1e-9) return;
			if (area < 0) {
				double tt; tt=x1;x1=x2;x2=tt; tt=y1;y1=y2;y2=tt; tt=w1;w1=w2;w2=tt; tt=uz1;uz1=uz2;uz2=tt; tt=vz1;vz1=vz2;vz2=tt; area=-area;
				if (gour) { tt=lr1;lr1=lr2;lr2=tt; tt=lg1;lg1=lg2;lg2=tt; tt=lb1;lb1=lb2;lb2=tt; }
			}
			int minX = (int)Math.Floor(Math.Min(x0, Math.Min(x1, x2))); if (minX < 0) minX = 0;
			int maxX = (int)Math.Ceiling(Math.Max(x0, Math.Max(x1, x2))); if (maxX > s._w - 1) maxX = s._w - 1;
			int minY = (int)Math.Floor(Math.Min(y0, Math.Min(y1, y2))); if (minY < by0) minY = by0;
			int maxY = (int)Math.Ceiling(Math.Max(y0, Math.Max(y1, y2))); if (maxY > by1 - 1) maxY = by1 - 1;
			if (minX > maxX || minY > maxY) return;
			int[] tex = t.Tex; int tw = t.Tw, th = t.Th; int alpha = t.Alpha; bool screenT = t.ScreenTex;
			bool sprite = t.Sprite, cutout = t.Cutout, add = t.Additive;   // alpha-textured HD2D sprite quad
			int rsh = s._rttShift;   // mirror/portal screen-tex is rendered at 1/2^rsh res -> sample px>>rsh
			double bR = t.BaseR, bG = t.BaseG, bB = t.BaseB;
			double invA = 1.0 / area;
			double d0x = y1 - y2, d0y = x2 - x1;
			double d1x = y2 - y0, d1y = x0 - x2;
			double d2x = y0 - y1, d2y = x1 - x0;
			double cx = minX + 0.5, cy = minY + 0.5;
			double e0r = (x2 - x1) * (cy - y1) - (y2 - y1) * (cx - x1);
			double e1r = (x0 - x2) * (cy - y2) - (y0 - y2) * (cx - x2);
			double e2r = (x1 - x0) * (cy - y0) - (y1 - y0) * (cx - x0);
			double wR = invA * (e0r * w0 + e1r * w1 + e2r * w2), wDx = invA * (d0x * w0 + d1x * w1 + d2x * w2), wDy = invA * (d0y * w0 + d1y * w1 + d2y * w2);
			double uR = invA * (e0r * uz0 + e1r * uz1 + e2r * uz2), uDx = invA * (d0x * uz0 + d1x * uz1 + d2x * uz2), uDy = invA * (d0y * uz0 + d1y * uz1 + d2y * uz2);
			double vR = invA * (e0r * vz0 + e1r * vz1 + e2r * vz2), vDx = invA * (d0x * vz0 + d1x * vz1 + d2x * vz2), vDy = invA * (d0y * vz0 + d1y * vz1 + d2y * vz2);
			double lrR = 0, lrDx = 0, lrDy = 0, lgR = 0, lgDx = 0, lgDy = 0, lbR = 0, lbDx = 0, lbDy = 0;
			if (gour) {
				lrR = invA * (e0r * lr0 + e1r * lr1 + e2r * lr2); lrDx = invA * (d0x * lr0 + d1x * lr1 + d2x * lr2); lrDy = invA * (d0y * lr0 + d1y * lr1 + d2y * lr2);
				lgR = invA * (e0r * lg0 + e1r * lg1 + e2r * lg2); lgDx = invA * (d0x * lg0 + d1x * lg1 + d2x * lg2); lgDy = invA * (d0y * lg0 + d1y * lg1 + d2y * lg2);
				lbR = invA * (e0r * lb0 + e1r * lb1 + e2r * lb2); lbDx = invA * (d0x * lb0 + d1x * lb1 + d2x * lb2); lbDy = invA * (d0y * lb0 + d1y * lb1 + d2y * lb2);
			}
			double fmR = flR, fmG = flG, fmB = flB;   // flR/G/B already = shade*tint (unlit) or sun/ambient (lit-flat)
			bool opaque = alpha >= 255; double a = alpha / 255.0, ia = 1.0 - a;
			bool fog = s._fog; double fr = s._fogR, fg = s._fogG, fb = s._fogB, fstart = s._fogStart, finv = s._fogInv;
			int W = s._w;
			bool pow2 = (tw & (tw - 1)) == 0 && (th & (th - 1)) == 0; int mw = tw - 1, mh = th - 1;
			fixed (byte* B = s._canvas._buf) fixed (float* D = s._depth) fixed (int* T = tex) {
				for (int py = minY; py <= maxY; py++) {
					double E0 = e0r, E1 = e1r, E2 = e2r, WW = wR, UZ = uR, VZ = vR, LR = lrR, LG = lgR, LB = lbR;
					int row = py * W;
					for (int px = minX; px <= maxX; px++) {
						if (E0 >= 0 && E1 >= 0 && E2 >= 0) {
							int idx = row + px; float fw = (float)WW;
							if (fw > D[idx]) {
								double iw = 1.0 / WW;
								if (sprite) {
									double su = UZ * iw, sv = VZ * iw;
									int sxi = (int)(su * tw); if (sxi < 0) sxi = 0; else if (sxi >= tw) sxi = tw - 1;
									int syi = (int)(sv * th); if (syi < 0) syi = 0; else if (syi >= th) syi = th - 1;
									int pk = T[syi * tw + sxi]; int ta = (pk >> 24) & 0xFF;
									if (cutout) {                       // HD2D: alpha-test crisp edges, write depth (occludes)
										if (ta >= 128) {
											double sr = ((pk >> 16) & 0xFF) * fmR, sg = ((pk >> 8) & 0xFF) * fmG, sb = (pk & 0xFF) * fmB;
											if (fog) { double f = (iw - fstart) * finv; if (f > 0) { if (f > 1) f = 1; sr += (fr - sr) * f; sg += (fg - sg) * f; sb += (fb - sb) * f; } }
											int o = idx * 4; D[idx] = fw; B[o] = RenderUtil.CB(sr); B[o + 1] = RenderUtil.CB(sg); B[o + 2] = RenderUtil.CB(sb); B[o + 3] = 255;
										}
									} else if (ta != 0) {              // soft sprite: alpha blend (or additive), no depth write
										double pa = (ta * 0.003921568627) * a;
										double sr = ((pk >> 16) & 0xFF) * fmR, sg = ((pk >> 8) & 0xFF) * fmG, sb = (pk & 0xFF) * fmB;
										int o = idx * 4;
										if (add) { B[o] = RenderUtil.CB(sr * pa + B[o]); B[o + 1] = RenderUtil.CB(sg * pa + B[o + 1]); B[o + 2] = RenderUtil.CB(sb * pa + B[o + 2]); B[o + 3] = 255; }
										else { double pia = 1 - pa; B[o] = (byte)(sr * pa + B[o] * pia); B[o + 1] = (byte)(sg * pa + B[o + 1] * pia); B[o + 2] = (byte)(sb * pa + B[o + 2] * pia); B[o + 3] = 255; }
									}
								} else {
									int txi, tyi;
								if (screenT) { txi = px >> rsh; if (txi >= tw) txi = tw - 1; tyi = py >> rsh; if (tyi >= th) tyi = th - 1; }
								else if (pow2) { txi = (int)(UZ * iw * tw) & mw; tyi = (int)(VZ * iw * th) & mh; }
								else { txi = (int)(UZ * iw * tw) % tw; if (txi < 0) txi += tw; tyi = (int)(VZ * iw * th) % th; if (tyi < 0) tyi += th; }
								int packed = T[tyi * tw + txi];
								if (packed >= 0) {
									double pr = (packed >> 16) & 0xFF, pg = (packed >> 8) & 0xFF, pb = packed & 0xFF, tr, tg, tb;
									if (gour) { tr = pr * (LR * iw); tg = pg * (LG * iw); tb = pb * (LB * iw); }
									else if (screenT) { tr = pr * fmR + bR * (1 - fmR); tg = pg * fmG + bG * (1 - fmG); tb = pb * fmB + bB * (1 - fmB); }
									else { tr = pr * fmR; tg = pg * fmG; tb = pb * fmB; }
									if (fog) { double f = (iw - fstart) * finv; if (f > 0) { if (f > 1) f = 1; tr += (fr - tr) * f; tg += (fg - tg) * f; tb += (fb - tb) * f; } }
									int o = idx * 4;
									if (opaque) { D[idx] = fw; B[o] = RenderUtil.CB(tr); B[o + 1] = RenderUtil.CB(tg); B[o + 2] = RenderUtil.CB(tb); B[o + 3] = 255; }
									else { B[o] = (byte)(tr * a + B[o] * ia); B[o + 1] = (byte)(tg * a + B[o + 1] * ia); B[o + 2] = (byte)(tb * a + B[o + 2] * ia); B[o + 3] = 255; }
								}
								}
							}
						}
						E0 += d0x; E1 += d1x; E2 += d2x; WW += wDx; UZ += uDx; VZ += vDx;
						if (gour) { LR += lrDx; LG += lgDx; LB += lbDx; }
					}
					e0r += d0y; e1r += d1y; e2r += d2y; wR += wDy; uR += uDy; vR += vDy;
					if (gour) { lrR += lrDy; lgR += lgDy; lbR += lbDy; }
				}
			}
		}
	}
}
