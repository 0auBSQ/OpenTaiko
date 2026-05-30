using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenTaiko {
	/// <summary>
	/// Software rasterizer for <see cref="Lua3DScene"/>: builds an ordered screen-space triangle
	/// list from the scene's objects (cull + sort), then fills it with a perspective-correct,
	/// z-buffered, fog-aware, multithreaded (horizontal band) rasterizer.
	/// </summary>
	internal sealed class Rasterizer : IRenderer {
		private Lua3DScene _s;

		public void Invalidate() { }   // stateless between frames

		// projection scratch (single-threaded build phase)
		private readonly double[] _vx = new double[8], _vy = new double[8], _vz = new double[8], _vu = new double[8], _vv = new double[8];
		private readonly double[] _ox = new double[8], _oy = new double[8], _oz = new double[8], _ou = new double[8], _ov = new double[8];
		private readonly double[] _sx = new double[8], _sy = new double[8], _sw = new double[8], _su = new double[8], _sv = new double[8];

		private struct RTri {
			public double X0, Y0, W0, X1, Y1, W1, X2, Y2, W2;
			public double U0, V0, U1, V1, U2, V2;
			public int[] Tex; public int Tw, Th;
			public double Shade; public int Alpha;
			public byte R, G, B; public bool IsTex;
		}
		private RTri[] _tris = new RTri[2048];
		private int _triN;
		private readonly List<SceneObject> _drawList = new();
		private static readonly Comparison<SceneObject> _nearFirst = (a, b) => a.Dist.CompareTo(b.Dist);
		private static readonly Comparison<SceneObject> _farFirst = (a, b) => b.Dist.CompareTo(a.Dist);

		public void Render(Lua3DScene s) {
			_s = s;
			Array.Clear(s._depth, 0, s._depth.Length);
			_triN = 0;
			_drawList.Clear();
			foreach (var o in s._objects.Values) {
				if (!o.Visible || o.N == 0 || o.Pass != 0 || Culled(o)) continue;
				o.Dist = DistSq(o); _drawList.Add(o);
			}
			_drawList.Sort(_nearFirst);
			for (int i = 0; i < _drawList.Count; i++) BuildObject(_drawList[i]);
			_drawList.Clear();
			foreach (var o in s._objects.Values) {
				if (!o.Visible || o.N == 0 || o.Pass == 0 || Culled(o)) continue;
				o.Dist = DistSq(o); _drawList.Add(o);
			}
			_drawList.Sort(_farFirst);
			for (int i = 0; i < _drawList.Count; i++) BuildObject(_drawList[i]);
			int n = s._threads;
			if (n <= 1 || _triN == 0) RasterizeBand(0, s._h);
			else Parallel.For(0, n, s._po, bi => RasterizeBand(bi * s._h / n, (bi + 1) * s._h / n));
			s._canvas.MarkDirty(0, 0, s._w - 1, s._h - 1);
		}

		private void ToCam(double wx, double wy, double wz, out double cx, out double cy, out double cz) {
			var s = _s;
			double rx = wx - s._camX, ry = wy - s._camY, rz = wz - s._camZ;
			cx = rx * s._Rx + ry * s._Ry + rz * s._Rz;
			cy = rx * s._Ux + ry * s._Uy + rz * s._Uz;
			cz = rx * s._Fx + ry * s._Fy + rz * s._Fz;
		}

		private int ClipNear(int n) {
			int m = 0; double near = _s._near;
			for (int i = 0; i < n; i++) {
				int j = (i + 1) % n;
				double czi = _vz[i], czj = _vz[j];
				bool inI = czi >= near, inJ = czj >= near;
				if (inI) { _ox[m] = _vx[i]; _oy[m] = _vy[i]; _oz[m] = _vz[i]; _ou[m] = _vu[i]; _ov[m] = _vv[i]; m++; }
				if (inI != inJ) {
					double t = (near - czi) / (czj - czi);
					_ox[m] = _vx[i] + (_vx[j] - _vx[i]) * t;
					_oy[m] = _vy[i] + (_vy[j] - _vy[i]) * t;
					_oz[m] = near;
					_ou[m] = _vu[i] + (_vu[j] - _vu[i]) * t;
					_ov[m] = _vv[i] + (_vv[j] - _vv[i]) * t;
					m++;
				}
			}
			return m;
		}

		private int Project(int m) {
			double hw = _s._w * 0.5, hh = _s._h * 0.5, scale = _s._scale;
			for (int i = 0; i < m; i++) {
				double iz = 1.0 / _oz[i];
				_sx[i] = hw + _ox[i] * iz * scale;
				_sy[i] = hh - _oy[i] * iz * scale;
				_sw[i] = iz;
				_su[i] = _ou[i] * iz;
				_sv[i] = _ov[i] * iz;
			}
			return m;
		}

		private void AddTexTri(int i0, int i1, int i2, int[] tex, int tw, int th, double shade, int alpha) {
			if (_triN == _tris.Length) Array.Resize(ref _tris, _tris.Length * 2);
			ref RTri t = ref _tris[_triN++];
			t.IsTex = true; t.Tex = tex; t.Tw = tw; t.Th = th; t.Shade = shade; t.Alpha = alpha;
			t.X0 = _sx[i0]; t.Y0 = _sy[i0]; t.W0 = _sw[i0]; t.U0 = _su[i0]; t.V0 = _sv[i0];
			t.X1 = _sx[i1]; t.Y1 = _sy[i1]; t.W1 = _sw[i1]; t.U1 = _su[i1]; t.V1 = _sv[i1];
			t.X2 = _sx[i2]; t.Y2 = _sy[i2]; t.W2 = _sw[i2]; t.U2 = _su[i2]; t.V2 = _sv[i2];
		}
		private void AddFlatTri(int i0, int i1, int i2, byte r, byte g, byte b, int alpha) {
			if (_triN == _tris.Length) Array.Resize(ref _tris, _tris.Length * 2);
			ref RTri t = ref _tris[_triN++];
			t.IsTex = false; t.R = r; t.G = g; t.B = b; t.Alpha = alpha;
			t.X0 = _sx[i0]; t.Y0 = _sy[i0]; t.W0 = _sw[i0];
			t.X1 = _sx[i1]; t.Y1 = _sy[i1]; t.W1 = _sw[i1];
			t.X2 = _sx[i2]; t.Y2 = _sy[i2]; t.W2 = _sw[i2];
		}

		private void RasterizeBand(int by0, int by1) {
			for (int i = 0; i < _triN; i++) {
				if (_tris[i].IsTex) RasterTexBand(ref _tris[i], by0, by1);
				else RasterFlatBand(ref _tris[i], by0, by1);
			}
		}

		private unsafe void RasterFlatBand(ref RTri t, int by0, int by1) {
			var s = _s;
			double x0 = t.X0, y0 = t.Y0, w0 = t.W0;
			double x1 = t.X1, y1 = t.Y1, w1 = t.W1;
			double x2 = t.X2, y2 = t.Y2, w2 = t.W2;
			double area = (x1 - x0) * (y2 - y0) - (x2 - x0) * (y1 - y0);
			if (area > -1e-9 && area < 1e-9) return;
			if (area < 0) { double tt; tt=x1;x1=x2;x2=tt; tt=y1;y1=y2;y2=tt; tt=w1;w1=w2;w2=tt; area=-area; }
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
			bool opaque = alpha >= 255; double a = alpha / 255.0, ia = 1.0 - a;
			bool fog = s._fog; double fr = s._fogR, fg = s._fogG, fb = s._fogB, fstart = s._fogStart, finv = s._fogInv;
			int W = s._w;
			fixed (byte* B = s._canvas._buf) fixed (float* D = s._depth) {
				for (int py = minY; py <= maxY; py++) {
					double E0 = e0r, E1 = e1r, E2 = e2r, ID = idr;
					int row = py * W;
					for (int px = minX; px <= maxX; px++) {
						if (E0 >= 0 && E1 >= 0 && E2 >= 0) {
							int idx = row + px;
							float fid = (float)ID;
							if (fid > D[idx]) {
								int o = idx * 4;
								if (fog) {
									double f = (1.0 / ID - fstart) * finv;
									double cr = r, cg = g, cb = b;
									if (f > 0) { if (f > 1) f = 1; cr += (fr - cr) * f; cg += (fg - cg) * f; cb += (fb - cb) * f; }
									if (opaque) { D[idx] = fid; B[o] = (byte)cr; B[o + 1] = (byte)cg; B[o + 2] = (byte)cb; B[o + 3] = 255; }
									else { B[o] = (byte)(cr * a + B[o] * ia); B[o + 1] = (byte)(cg * a + B[o + 1] * ia); B[o + 2] = (byte)(cb * a + B[o + 2] * ia); B[o + 3] = 255; }
								} else if (opaque) {
									D[idx] = fid; B[o] = r; B[o + 1] = g; B[o + 2] = b; B[o + 3] = 255;
								} else {
									B[o] = (byte)(r * a + B[o] * ia); B[o + 1] = (byte)(g * a + B[o + 1] * ia); B[o + 2] = (byte)(b * a + B[o + 2] * ia); B[o + 3] = 255;
								}
							}
						}
						E0 += d0x; E1 += d1x; E2 += d2x; ID += idDx;
					}
					e0r += d0y; e1r += d1y; e2r += d2y; idr += idDy;
				}
			}
		}

		private unsafe void RasterTexBand(ref RTri t, int by0, int by1) {
			var s = _s;
			double x0 = t.X0, y0 = t.Y0, w0 = t.W0, uz0 = t.U0, vz0 = t.V0;
			double x1 = t.X1, y1 = t.Y1, w1 = t.W1, uz1 = t.U1, vz1 = t.V1;
			double x2 = t.X2, y2 = t.Y2, w2 = t.W2, uz2 = t.U2, vz2 = t.V2;
			double area = (x1 - x0) * (y2 - y0) - (x2 - x0) * (y1 - y0);
			if (area > -1e-9 && area < 1e-9) return;
			if (area < 0) { double tt; tt=x1;x1=x2;x2=tt; tt=y1;y1=y2;y2=tt; tt=w1;w1=w2;w2=tt; tt=uz1;uz1=uz2;uz2=tt; tt=vz1;vz1=vz2;vz2=tt; area=-area; }
			int minX = (int)Math.Floor(Math.Min(x0, Math.Min(x1, x2))); if (minX < 0) minX = 0;
			int maxX = (int)Math.Ceiling(Math.Max(x0, Math.Max(x1, x2))); if (maxX > s._w - 1) maxX = s._w - 1;
			int minY = (int)Math.Floor(Math.Min(y0, Math.Min(y1, y2))); if (minY < by0) minY = by0;
			int maxY = (int)Math.Ceiling(Math.Max(y0, Math.Max(y1, y2))); if (maxY > by1 - 1) maxY = by1 - 1;
			if (minX > maxX || minY > maxY) return;
			int[] tex = t.Tex; int tw = t.Tw, th = t.Th; double shade = t.Shade; int alpha = t.Alpha;
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
			bool opaque = alpha >= 255; double a = alpha / 255.0, ia = 1.0 - a;
			bool fog = s._fog; double fr = s._fogR, fg = s._fogG, fb = s._fogB, fstart = s._fogStart, finv = s._fogInv;
			int W = s._w;
			fixed (byte* B = s._canvas._buf) fixed (float* D = s._depth) fixed (int* T = tex) {
				for (int py = minY; py <= maxY; py++) {
					double E0 = e0r, E1 = e1r, E2 = e2r, WW = wR, UZ = uR, VZ = vR;
					int row = py * W;
					for (int px = minX; px <= maxX; px++) {
						if (E0 >= 0 && E1 >= 0 && E2 >= 0) {
							int idx = row + px;
							float fw = (float)WW;
							if (fw > D[idx]) {
								double iw = 1.0 / WW;
								double u = UZ * iw, v = VZ * iw;
								int txi = (int)(u * tw) % tw; if (txi < 0) txi += tw;
								int tyi = (int)(v * th) % th; if (tyi < 0) tyi += th;
								int packed = T[tyi * tw + txi];
								double tr = ((packed >> 16) & 0xFF) * shade;
								double tg = ((packed >> 8) & 0xFF) * shade;
								double tb = (packed & 0xFF) * shade;
								if (fog) {
									double f = (iw - fstart) * finv;
									if (f > 0) { if (f > 1) f = 1; tr += (fr - tr) * f; tg += (fg - tg) * f; tb += (fb - tb) * f; }
								}
								int o = idx * 4;
								if (opaque) {
									D[idx] = fw; B[o] = RenderUtil.CB(tr); B[o + 1] = RenderUtil.CB(tg); B[o + 2] = RenderUtil.CB(tb); B[o + 3] = 255;
								} else {
									B[o] = (byte)(tr * a + B[o] * ia); B[o + 1] = (byte)(tg * a + B[o + 1] * ia); B[o + 2] = (byte)(tb * a + B[o + 2] * ia); B[o + 3] = 255;
								}
							}
						}
						E0 += d0x; E1 += d1x; E2 += d2x; WW += wDx; UZ += uDx; VZ += vDx;
					}
					e0r += d0y; e1r += d1y; e2r += d2y; wR += wDy; uR += uDy; vR += vDy;
				}
			}
		}

		// model->world (row-major 4x4 . point), or identity when t is null.
		private static void Xform(double[] t, double x, double y, double z, out double ox, out double oy, out double oz) {
			if (t == null) { ox = x; oy = y; oz = z; return; }
			ox = t[0]*x + t[1]*y + t[2]*z + t[3];
			oy = t[4]*x + t[5]*y + t[6]*z + t[7];
			oz = t[8]*x + t[9]*y + t[10]*z + t[11];
		}

		private bool Culled(SceneObject o) {
			var s = _s;
			if (o.HasBounds) {
				double cx = (o.MinX + o.MaxX) * 0.5, cy = (o.MinY + o.MaxY) * 0.5, cz = (o.MinZ + o.MaxZ) * 0.5;
				double vx = cx - s._camX, vy = cy - s._camY, vz = cz - s._camZ;
				double rx = (o.MaxX - o.MinX) * 0.5, ry = (o.MaxY - o.MinY) * 0.5, rz = (o.MaxZ - o.MinZ) * 0.5;
				double radius = Math.Sqrt(rx*rx + ry*ry + rz*rz);
				if (s._renderDist > 0) {
					double dist = Math.Sqrt(vx*vx + vy*vy + vz*vz);
					if (dist - radius > s._renderDist) return true;
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
			double cx = o.HasBounds ? (o.MinX + o.MaxX) * 0.5 : 0;
			double cy = o.HasBounds ? (o.MinY + o.MaxY) * 0.5 : 0;
			double cz = o.HasBounds ? (o.MinZ + o.MaxZ) * 0.5 : 0;
			double vx = cx - s._camX, vy = cy - s._camY, vz = cz - s._camZ;
			return vx*vx + vy*vy + vz*vz;
		}

		private void BuildObject(SceneObject o) {
			var s = _s; var d = o.D; var t = o.Transform;
			if (o.Kind == 0) {
				for (int i = 0; i < o.N; i++) {
					int k = i * 16; int texId = (int)d[k+12];
					if (!s._texPix.TryGetValue(texId, out var tex)) continue;
					int tw = s._texW[texId], th = s._texH[texId];
					double uMax = d[k+13], vMax = d[k+14], shade = d[k+15];
					Xform(t, d[k],   d[k+1], d[k+2],  out double wx, out double wy, out double wz); ToCam(wx,wy,wz, out _vx[0], out _vy[0], out _vz[0]); _vu[0]=0;    _vv[0]=vMax;
					Xform(t, d[k+3], d[k+4], d[k+5],  out wx, out wy, out wz); ToCam(wx,wy,wz, out _vx[1], out _vy[1], out _vz[1]); _vu[1]=uMax; _vv[1]=vMax;
					Xform(t, d[k+6], d[k+7], d[k+8],  out wx, out wy, out wz); ToCam(wx,wy,wz, out _vx[2], out _vy[2], out _vz[2]); _vu[2]=uMax; _vv[2]=0;
					Xform(t, d[k+9], d[k+10],d[k+11], out wx, out wy, out wz); ToCam(wx,wy,wz, out _vx[3], out _vy[3], out _vz[3]); _vu[3]=0;    _vv[3]=0;
					int m = ClipNear(4); if (m < 3) continue;
					Project(m);
					for (int tri = 1; tri < m - 1; tri++) AddTexTri(0, tri, tri+1, tex, tw, th, shade, o.A);
				}
			} else if (o.Kind == 1) {
				byte br = RenderUtil.CB(o.R), bg = RenderUtil.CB(o.G), bb = RenderUtil.CB(o.B);
				for (int i = 0; i < o.N; i++) {
					int k = i * 12;
					Xform(t, d[k],   d[k+1], d[k+2],  out double wx, out double wy, out double wz); ToCam(wx,wy,wz, out _vx[0], out _vy[0], out _vz[0]);
					Xform(t, d[k+3], d[k+4], d[k+5],  out wx, out wy, out wz); ToCam(wx,wy,wz, out _vx[1], out _vy[1], out _vz[1]);
					Xform(t, d[k+6], d[k+7], d[k+8],  out wx, out wy, out wz); ToCam(wx,wy,wz, out _vx[2], out _vy[2], out _vz[2]);
					Xform(t, d[k+9], d[k+10],d[k+11], out wx, out wy, out wz); ToCam(wx,wy,wz, out _vx[3], out _vy[3], out _vz[3]);
					int m = ClipNear(4); if (m < 3) continue;
					Project(m);
					for (int tri = 1; tri < m - 1; tri++) AddFlatTri(0, tri, tri+1, br, bg, bb, o.A);
				}
			} else if (o.Kind == 2) {
				for (int i = 0; i < o.N; i++) {
					int k = i * 17; int texId = (int)d[k+15];
					if (!s._texPix.TryGetValue(texId, out var tex)) continue;
					int tw = s._texW[texId], th = s._texH[texId]; double shade = d[k+16];
					Xform(t, d[k],    d[k+1],  d[k+2],  out double wx, out double wy, out double wz); ToCam(wx,wy,wz, out _vx[0], out _vy[0], out _vz[0]); _vu[0]=d[k+3];  _vv[0]=d[k+4];
					Xform(t, d[k+5],  d[k+6],  d[k+7],  out wx, out wy, out wz); ToCam(wx,wy,wz, out _vx[1], out _vy[1], out _vz[1]); _vu[1]=d[k+8];  _vv[1]=d[k+9];
					Xform(t, d[k+10], d[k+11], d[k+12], out wx, out wy, out wz); ToCam(wx,wy,wz, out _vx[2], out _vy[2], out _vz[2]); _vu[2]=d[k+13]; _vv[2]=d[k+14];
					int m = ClipNear(3); if (m < 3) continue;
					Project(m);
					for (int tri = 1; tri < m - 1; tri++) AddTexTri(0, tri, tri+1, tex, tw, th, shade, o.A);
				}
			}
		}
	}
}
