using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FDK;
using NLua;

namespace OpenTaiko {
	/// <summary>
	/// A 3D render target for Lua stages, drawn like a <see cref="LuaTexture"/>/<see cref="LuaCanvas"/>.
	/// Owns a colour buffer (internal LuaCanvas) + a depth buffer + a camera, and a software
	/// rasterizer (flat or textured, perspective-correct, optional alpha, near-plane clipping).
	/// The hot loops use incremental edge functions and unsafe pointers. Lua keeps the scene
	/// logic and submits world-space triangles/quads; the fill runs natively here.
	///
	/// Submit triangles via FillTriangleWorld / FillTriangleWorldTex (per-vertex UV — for
	/// models), or convenience quads via FillQuadWorld / FillQuadWorldTex (corners in order
	/// bottom-left, bottom-right, top-right, top-left).
	/// </summary>
	public class Lua3DScene : IDisposable {
		private LuaCanvas _canvas;
		internal HashSet<Lua3DScene>? _disposeList = null;

		private readonly int _w;
		private readonly int _h;
		private float[] _depth;

		// Camera, owned by the scene: position + orientation (yaw/pitch → basis) +
		// lens (fov → scale, near plane). Lua feeds these and reads the basis / Project back.
		private double _camX, _camY, _camZ;
		private double _Rx = 1, _Ry, _Rz, _Ux, _Uy = 1, _Uz, _Fx, _Fy, _Fz = 1;
		private double _yaw, _pitch;
		private double _fov = 70.0;
		private double _near = 0.06;
		private double _scale = 1.0;
		private double _renderDist = 0.0;   // 0 = unlimited; else objects past this are culled
		private int _threads = 1;   // rasterizer threads (screen split into this many row bands)
		private ParallelOptions _po = new ParallelOptions { MaxDegreeOfParallelism = 1 };

		// Distance fog: pixels fade toward (_fogR,_fogG,_fogB) from _fogStart to _fogEnd
		// (camera-space depth). _fogInv = 1/(end-start). Off by default.
		private bool _fog;
		private double _fogR, _fogG, _fogB, _fogStart, _fogInv;

		private readonly Dictionary<int, int[]> _texPix = new();
		private readonly Dictionary<int, int> _texW = new();
		private readonly Dictionary<int, int> _texH = new();

		public Lua3DScene(int width, int height) {
			_canvas = new LuaCanvas(width, height);
			_w = _canvas._w;
			_h = _canvas._h;
			_depth = new float[_w * _h];
			_scale = (_h * 0.5) / Math.Tan(_fov * 0.5 * Math.PI / 180.0);
		}

		public int Width => _w;
		public int Height => _h;

		#region Frame setup / 2D / textures
		public void Clear(int r, int g, int b, int a) => _canvas.Clear(r, g, b, a);
		public void FillRect(int x, int y, int w, int h, int r, int g, int b, int a) => _canvas.FillRect(x, y, w, h, r, g, b, a);
		public void ClearDepth() => Array.Clear(_depth, 0, _depth.Length);

		// ── Scene-owned camera ────────────────────────────────────────────────────────
		// The scene holds the camera state and derives the basis + projection scale itself,
		// so Lua only feeds eye position, yaw/pitch and the lens, then reads the basis back
		// for movement / picking.

		/// <summary>Eye position in world space.</summary>
		public void SetCameraPosition(double x, double y, double z) { _camX = x; _camY = y; _camZ = z; }

		/// <summary>Look direction in degrees. Yaw turns about +Y; +pitch looks up (clamped ±89.9).
		/// Rebuilds the right/up/forward basis used by the rasterizer and Project.</summary>
		public void SetCameraAngles(double yawDeg, double pitchDeg) {
			if (pitchDeg > 89.9) pitchDeg = 89.9; else if (pitchDeg < -89.9) pitchDeg = -89.9;
			_yaw = yawDeg; _pitch = pitchDeg;
			double y = yawDeg * Math.PI / 180.0, p = pitchDeg * Math.PI / 180.0;
			double cy = Math.Cos(y), sy = Math.Sin(y), cp = Math.Cos(p), sp = Math.Sin(p);
			_Fx = sy * cp;  _Fy = sp;  _Fz = cy * cp;   // forward
			_Rx = cy;       _Ry = 0;   _Rz = -sy;       // right
			_Ux = -sy * sp; _Uy = cp;  _Uz = -cy * sp;  // up
		}

		/// <summary>Vertical field of view in degrees; updates the projection scale.</summary>
		public void SetCameraFov(double fovDeg) {
			_fov = fovDeg < 1 ? 1 : (fovDeg > 179 ? 179 : fovDeg);
			_scale = (_h * 0.5) / Math.Tan(_fov * 0.5 * Math.PI / 180.0);
		}

		/// <summary>Near clip plane (camera-space z, > 0).</summary>
		public void SetCameraNear(double near) { _near = near < 1e-4 ? 1e-4 : near; }

		/// <summary>Cull objects whose bounds lie entirely beyond this distance from the camera
		/// (0 = unlimited). Lowers the drawn geometry for big worlds.</summary>
		public void SetRenderDistance(double d) { _renderDist = d < 0 ? 0 : d; }

		/// <summary>Distance fog: pixels fade to (r,g,b) (0-255) between camera depths
		/// <paramref name="start"/> and <paramref name="end"/>. <paramref name="on"/>=false disables.</summary>
		public void SetFog(bool on, double r, double g, double b, double start, double end) {
			_fog = on;
			_fogR = r; _fogG = g; _fogB = b; _fogStart = start;
			_fogInv = end > start ? 1.0 / (end - start) : 0.0;
		}

		/// <summary>Number of CPU threads the rasterizer uses (screen split into this many
		/// horizontal row bands). 1 = single-threaded.</summary>
		public void SetThreads(int n) {
			_threads = n < 1 ? 1 : n;
			_po = new ParallelOptions { MaxDegreeOfParallelism = _threads };
		}

		public double GetCameraFov() => _fov;
		public double GetCameraNear() => _near;
		public double GetCameraYaw() => _yaw;
		public double GetCameraPitch() => _pitch;
		public (double, double, double) GetCameraPosition() => (_camX, _camY, _camZ);
		public (double, double, double) GetCameraForward() => (_Fx, _Fy, _Fz);
		public (double, double, double) GetCameraRight() => (_Rx, _Ry, _Rz);
		public (double, double, double) GetCameraUp() => (_Ux, _Uy, _Uz);

		/// <summary>Project a world point to screen pixels using the scene camera:
		/// returns (sx, sy, inFront). inFront is false when the point is behind the near plane.</summary>
		public (double, double, bool) Project(double wx, double wy, double wz) {
			double rx = wx - _camX, ry = wy - _camY, rz = wz - _camZ;
			double cz = rx * _Fx + ry * _Fy + rz * _Fz;
			if (cz < _near) return (0, 0, false);
			double cx = rx * _Rx + ry * _Ry + rz * _Rz;
			double cyv = rx * _Ux + ry * _Uy + rz * _Uz;
			return (_w * 0.5 + cx / cz * _scale, _h * 0.5 - cyv / cz * _scale, true);
		}

		public void RegisterTexture(int id, LuaTable pixels, int w, int h) {
			if (pixels == null || w <= 0 || h <= 0) return;
			var px = new int[w * h];
			for (int i = 0; i < px.Length; i++) {
				object o = pixels[i + 1];
				px[i] = o switch { long l => (int)l, double d => (int)d, int ii => ii, _ => 0 };
			}
			_texPix[id] = px; _texW[id] = w; _texH[id] = h;
		}

		/// <summary>
		/// Register a texture for the rasterizer from an existing <see cref="LuaTexture"/> by
		/// reading its pixels back from the GPU (alpha is dropped — the rasterizer treats textures
		/// as opaque). One-off cost; call at load time, not per frame.
		/// </summary>
		public void RegisterTextureFromImage(int id, LuaTexture tex) {
			if (tex?._texture == null) return;
			byte[]? rgba = tex.GetCachedPixels(out int w, out int h);
			if (rgba == null || w <= 0 || h <= 0) return;
			var px = new int[w * h];
			for (int i = 0; i < px.Length; i++) {
				int o = i * 4;
				px[i] = (rgba[o] << 16) | (rgba[o + 1] << 8) | rgba[o + 2];
			}
			_texPix[id] = px; _texW[id] = w; _texH[id] = h;
		}

		/// <summary>2D line in the colour buffer (screen pixels), drawn on top (no depth).</summary>
		public void DrawLine(int x0, int y0, int x1, int y1, int r, int g, int b) {
			byte br = CB(r), bg = CB(g), bb = CB(b);
			int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
			int dy = -Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
			int err = dx + dy;
			int minx = _w, miny = _h, maxx = -1, maxy = -1;
			byte[] buf = _canvas._buf;
			while (true) {
				if ((uint)x0 < (uint)_w && (uint)y0 < (uint)_h) {
					int o = (y0 * _w + x0) * 4;
					buf[o] = br; buf[o + 1] = bg; buf[o + 2] = bb; buf[o + 3] = 255;
					if (x0 < minx) minx = x0; if (x0 > maxx) maxx = x0;
					if (y0 < miny) miny = y0; if (y0 > maxy) maxy = y0;
				}
				if (x0 == x1 && y0 == y1) break;
				int e2 = 2 * err;
				if (e2 >= dy) { err += dy; x0 += sx; }
				if (e2 <= dx) { err += dx; y0 += sy; }
			}
			if (maxx >= 0) _canvas.MarkDirty(minx, miny, maxx, maxy);
		}
		#endregion

		#region Rasterizer
		private void ToCam(double wx, double wy, double wz, out double cx, out double cy, out double cz) {
			double rx = wx - _camX, ry = wy - _camY, rz = wz - _camZ;
			cx = rx * _Rx + ry * _Ry + rz * _Rz;
			cy = rx * _Ux + ry * _Uy + rz * _Uz;
			cz = rx * _Fx + ry * _Fy + rz * _Fz;
		}

		private readonly double[] _vx = new double[8], _vy = new double[8], _vz = new double[8], _vu = new double[8], _vv = new double[8];
		private readonly double[] _ox = new double[8], _oy = new double[8], _oz = new double[8], _ou = new double[8], _ov = new double[8];
		private readonly double[] _sx = new double[8], _sy = new double[8], _sw = new double[8], _su = new double[8], _sv = new double[8];

		private int ClipNear(int n) {
			int m = 0; double near = _near;
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

		private static byte CB(double v) => v <= 0 ? (byte)0 : (v >= 255 ? (byte)255 : (byte)v);

		private int Project(int m) {
			double hw = _w * 0.5, hh = _h * 0.5;
			for (int i = 0; i < m; i++) {
				double iz = 1.0 / _oz[i];
				_sx[i] = hw + _ox[i] * iz * _scale;
				_sy[i] = hh - _oy[i] * iz * _scale;
				_sw[i] = iz;
				_su[i] = _ou[i] * iz;
				_sv[i] = _ov[i] * iz;
			}
			return m;
		}

		// ── Render-triangle list: built single-threaded by BuildObject, then rasterized either
		// on one thread or split across _threads horizontal row bands (disjoint rows => no races).
		private struct RTri {
			public double X0, Y0, W0, X1, Y1, W1, X2, Y2, W2;
			public double U0, V0, U1, V1, U2, V2;
			public int[] Tex; public int Tw, Th;
			public double Shade; public int Alpha;
			public byte R, G, B; public bool IsTex;
		}
		private RTri[] _tris = new RTri[2048];
		private int _triN;

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
			double x0 = t.X0, y0 = t.Y0, w0 = t.W0;
			double x1 = t.X1, y1 = t.Y1, w1 = t.W1;
			double x2 = t.X2, y2 = t.Y2, w2 = t.W2;
			double area = (x1 - x0) * (y2 - y0) - (x2 - x0) * (y1 - y0);
			if (area > -1e-9 && area < 1e-9) return;
			if (area < 0) { double tt; tt=x1;x1=x2;x2=tt; tt=y1;y1=y2;y2=tt; tt=w1;w1=w2;w2=tt; area=-area; }
			int minX = (int)Math.Floor(Math.Min(x0, Math.Min(x1, x2))); if (minX < 0) minX = 0;
			int maxX = (int)Math.Ceiling(Math.Max(x0, Math.Max(x1, x2))); if (maxX > _w - 1) maxX = _w - 1;
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
			fixed (byte* B = _canvas._buf) fixed (float* D = _depth) {
				for (int py = minY; py <= maxY; py++) {
					double E0 = e0r, E1 = e1r, E2 = e2r, ID = idr;
					int row = py * _w;
					for (int px = minX; px <= maxX; px++) {
						if (E0 >= 0 && E1 >= 0 && E2 >= 0) {
							int idx = row + px;
							float fid = (float)ID;
							if (fid > D[idx]) {
								int o = idx * 4;
								if (_fog) {
									double f = (1.0 / ID - _fogStart) * _fogInv;
									double cr = r, cg = g, cb = b;
									if (f > 0) { if (f > 1) f = 1; cr += (_fogR - cr) * f; cg += (_fogG - cg) * f; cb += (_fogB - cb) * f; }
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
			double x0 = t.X0, y0 = t.Y0, w0 = t.W0, uz0 = t.U0, vz0 = t.V0;
			double x1 = t.X1, y1 = t.Y1, w1 = t.W1, uz1 = t.U1, vz1 = t.V1;
			double x2 = t.X2, y2 = t.Y2, w2 = t.W2, uz2 = t.U2, vz2 = t.V2;
			double area = (x1 - x0) * (y2 - y0) - (x2 - x0) * (y1 - y0);
			if (area > -1e-9 && area < 1e-9) return;
			if (area < 0) { double tt; tt=x1;x1=x2;x2=tt; tt=y1;y1=y2;y2=tt; tt=w1;w1=w2;w2=tt; tt=uz1;uz1=uz2;uz2=tt; tt=vz1;vz1=vz2;vz2=tt; area=-area; }
			int minX = (int)Math.Floor(Math.Min(x0, Math.Min(x1, x2))); if (minX < 0) minX = 0;
			int maxX = (int)Math.Ceiling(Math.Max(x0, Math.Max(x1, x2))); if (maxX > _w - 1) maxX = _w - 1;
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
			fixed (byte* B = _canvas._buf) fixed (float* D = _depth) fixed (int* T = tex) {
				for (int py = minY; py <= maxY; py++) {
					double E0 = e0r, E1 = e1r, E2 = e2r, W = wR, UZ = uR, VZ = vR;
					int row = py * _w;
					for (int px = minX; px <= maxX; px++) {
						if (E0 >= 0 && E1 >= 0 && E2 >= 0) {
							int idx = row + px;
							float fw = (float)W;
							if (fw > D[idx]) {
								double iw = 1.0 / W;
								double u = UZ * iw, v = VZ * iw;
								int txi = (int)(u * tw) % tw; if (txi < 0) txi += tw;
								int tyi = (int)(v * th) % th; if (tyi < 0) tyi += th;
								int packed = T[tyi * tw + txi];
								double tr = ((packed >> 16) & 0xFF) * shade;
								double tg = ((packed >> 8) & 0xFF) * shade;
								double tb = (packed & 0xFF) * shade;
								if (_fog) {
									double f = (iw - _fogStart) * _fogInv;
									if (f > 0) { if (f > 1) f = 1; tr += (_fogR - tr) * f; tg += (_fogG - tg) * f; tb += (_fogB - tb) * f; }
								}
								int o = idx * 4;
								if (opaque) {
									D[idx] = fw; B[o] = CB(tr); B[o + 1] = CB(tg); B[o + 2] = CB(tb); B[o + 3] = 255;
								} else {
									B[o] = (byte)(tr * a + B[o] * ia); B[o + 1] = (byte)(tg * a + B[o + 1] * ia); B[o + 2] = (byte)(tb * a + B[o + 2] * ia); B[o + 3] = 255;
								}
							}
						}
						E0 += d0x; E1 += d1x; E2 += d2x; W += wDx; UZ += uDx; VZ += vDx;
					}
					e0r += d0y; e1r += d1y; e2r += d2y; wR += wDy; uR += uDy; vR += vDy;
				}
			}
		}
		#endregion

		#region Scene objects (retained mode)
		// The scene retains a set of objects (groups of primitives). Lua creates/edits/removes
		// them and supplies bounds + an optional facing-normal; Render() then culls, sorts and
		// rasterizes everything natively — so the per-frame Lua↔C# traffic is just the camera
		// update + one Render() call, and all the "calculation" lives here.
		//
		// Primitive kinds (one per object): 0 = textured quad (stride 16: 12 coords + texId,
		// uMax, vMax, shade), 1 = flat quad (stride 12: 12 coords; colour from the object),
		// 2 = textured triangle (stride 17: 3×(xyz+uv) + texId + shade) — for models.
		private sealed class SceneObject {
			public double[] D = Array.Empty<double>();
			public int N;                 // primitive count
			public int Kind = -1;         // 0 tex-quad, 1 flat-quad, 2 tex-tri
			public bool Visible = true;
			public int Pass;              // 0 opaque (front→back), 1 transparent (back→front)
			public double R, G, B; public int A = 255;       // flat-quad colour
			public bool HasBounds;
			public double MinX, MinY, MinZ, MaxX, MaxY, MaxZ;
			public bool HasNormal; public double Nx, Ny, Nz; // axis-aligned planar back-face cull
			public double[] Transform;    // null = identity (row-major 4×4 for models)
			public double Dist;           // scratch: squared distance to camera (set each Render)
		}

		private readonly Dictionary<int, SceneObject> _objects = new();
		private int _nextObjId = 1;
		private readonly List<SceneObject> _drawList = new();

		private SceneObject Obj(int id) => _objects.TryGetValue(id, out var o) ? o : null;
		private static void EnsureCap(SceneObject o, int stride) {
			if ((o.N + 1) * stride > o.D.Length)
				Array.Resize(ref o.D, o.D.Length == 0 ? stride * 64 : o.D.Length * 2);
		}

		/// <summary>Create an empty object; returns its id. Visible by default, opaque pass.</summary>
		public int NewObject() { int id = _nextObjId++; _objects[id] = new SceneObject(); return id; }
		/// <summary>Remove an object and its geometry.</summary>
		public void DeleteObject(int id) => _objects.Remove(id);
		/// <summary>Clear an object's primitives so it can be refilled (kind resets).</summary>
		public void ObjBegin(int id) { var o = Obj(id); if (o != null) { o.N = 0; o.Kind = -1; } }
		public void ObjSetVisible(int id, bool v) { var o = Obj(id); if (o != null) o.Visible = v; }
		/// <summary>Axis-aligned bounds used for frustum (behind-camera) culling.</summary>
		public void ObjSetBounds(int id, double minX, double minY, double minZ, double maxX, double maxY, double maxZ) {
			var o = Obj(id); if (o == null) return;
			o.MinX = minX; o.MinY = minY; o.MinZ = minZ; o.MaxX = maxX; o.MaxY = maxY; o.MaxZ = maxZ; o.HasBounds = true;
		}
		/// <summary>Mark this object as a planar group facing (nx,ny,nz); it's skipped when the
		/// camera is on its back side (axis-aligned back-face culling). Pass 0,0,0 to disable.</summary>
		public void ObjSetNormal(int id, double nx, double ny, double nz) {
			var o = Obj(id); if (o == null) return;
			o.Nx = nx; o.Ny = ny; o.Nz = nz; o.HasNormal = (nx != 0 || ny != 0 || nz != 0);
		}
		/// <summary>pass 0 = opaque (drawn front→back), 1 = transparent (back→front). r,g,b,a is the
		/// colour used by flat-quad objects.</summary>
		public void ObjSetPass(int id, int pass, double r, double g, double b, int a) {
			var o = Obj(id); if (o == null) return;
			o.Pass = pass; o.R = r; o.G = g; o.B = b; o.A = a;
		}
		/// <summary>Optional model matrix (Lua table of 16 numbers, row-major). nil/empty = identity.</summary>
		public void ObjSetTransform(int id, LuaTable m) {
			var o = Obj(id); if (o == null) return;
			if (m == null) { o.Transform = null; return; }
			var t = new double[16];
			for (int i = 0; i < 16; i++) {
				object v = m[i + 1];
				t[i] = v switch { double d => d, long l => l, int ii => ii, _ => (i % 5 == 0 ? 1.0 : 0.0) };
			}
			o.Transform = t;
		}
		public void ObjClearTransform(int id) { var o = Obj(id); if (o != null) o.Transform = null; }

		public void ObjAddQuadTex(int id,
			double x0, double y0, double z0, double x1, double y1, double z1,
			double x2, double y2, double z2, double x3, double y3, double z3,
			int texId, double uMax, double vMax, double shade) {
			var o = Obj(id); if (o == null) return;
			o.Kind = 0; EnsureCap(o, 16);
			int k = o.N * 16; var d = o.D;
			d[k] = x0; d[k+1] = y0; d[k+2] = z0; d[k+3] = x1; d[k+4] = y1; d[k+5] = z1;
			d[k+6] = x2; d[k+7] = y2; d[k+8] = z2; d[k+9] = x3; d[k+10] = y3; d[k+11] = z3;
			d[k+12] = texId; d[k+13] = uMax; d[k+14] = vMax; d[k+15] = shade;
			o.N++;
		}
		public void ObjAddQuadFlat(int id,
			double x0, double y0, double z0, double x1, double y1, double z1,
			double x2, double y2, double z2, double x3, double y3, double z3) {
			var o = Obj(id); if (o == null) return;
			o.Kind = 1; EnsureCap(o, 12);
			int k = o.N * 12; var d = o.D;
			d[k] = x0; d[k+1] = y0; d[k+2] = z0; d[k+3] = x1; d[k+4] = y1; d[k+5] = z1;
			d[k+6] = x2; d[k+7] = y2; d[k+8] = z2; d[k+9] = x3; d[k+10] = y3; d[k+11] = z3;
			o.N++;
		}
		public void ObjAddTriTex(int id,
			double x0, double y0, double z0, double u0, double v0,
			double x1, double y1, double z1, double u1, double v1,
			double x2, double y2, double z2, double u2, double v2,
			int texId, double shade) {
			var o = Obj(id); if (o == null) return;
			o.Kind = 2; EnsureCap(o, 17);
			int k = o.N * 17; var d = o.D;
			d[k]=x0; d[k+1]=y0; d[k+2]=z0; d[k+3]=u0; d[k+4]=v0;
			d[k+5]=x1; d[k+6]=y1; d[k+7]=z1; d[k+8]=u1; d[k+9]=v1;
			d[k+10]=x2; d[k+11]=y2; d[k+12]=z2; d[k+13]=u2; d[k+14]=v2;
			d[k+15]=texId; d[k+16]=shade;
			o.N++;
		}

		// model→world (row-major 4×4 · point), or identity when t is null.
		private static void Xform(double[] t, double x, double y, double z, out double ox, out double oy, out double oz) {
			if (t == null) { ox = x; oy = y; oz = z; return; }
			ox = t[0]*x + t[1]*y + t[2]*z + t[3];
			oy = t[4]*x + t[5]*y + t[6]*z + t[7];
			oz = t[8]*x + t[9]*y + t[10]*z + t[11];
		}

		private bool Culled(SceneObject o) {
			if (o.HasBounds) {
				double cx = (o.MinX + o.MaxX) * 0.5, cy = (o.MinY + o.MaxY) * 0.5, cz = (o.MinZ + o.MaxZ) * 0.5;
				double vx = cx - _camX, vy = cy - _camY, vz = cz - _camZ;
				double rx = (o.MaxX - o.MinX) * 0.5, ry = (o.MaxY - o.MinY) * 0.5, rz = (o.MaxZ - o.MinZ) * 0.5;
				double radius = Math.Sqrt(rx*rx + ry*ry + rz*rz);
				if (_renderDist > 0) {                                   // beyond the render distance
					double dist = Math.Sqrt(vx*vx + vy*vy + vz*vz);
					if (dist - radius > _renderDist) return true;
				}
				if (_Fx*vx + _Fy*vy + _Fz*vz < -radius) return true;     // wholly behind the camera
			}
			if (o.HasNormal) {                                           // axis-aligned back-face cull
				if (o.Nx > 0 && _camX <= o.MinX) return true;
				if (o.Nx < 0 && _camX >= o.MaxX) return true;
				if (o.Ny > 0 && _camY <= o.MinY) return true;
				if (o.Ny < 0 && _camY >= o.MaxY) return true;
				if (o.Nz > 0 && _camZ <= o.MinZ) return true;
				if (o.Nz < 0 && _camZ >= o.MaxZ) return true;
			}
			return false;
		}

		private double DistSq(SceneObject o) {
			double cx = o.HasBounds ? (o.MinX + o.MaxX) * 0.5 : 0;
			double cy = o.HasBounds ? (o.MinY + o.MaxY) * 0.5 : 0;
			double cz = o.HasBounds ? (o.MinZ + o.MaxZ) * 0.5 : 0;
			double vx = cx - _camX, vy = cy - _camY, vz = cz - _camZ;
			return vx*vx + vy*vy + vz*vz;
		}

		private void BuildObject(SceneObject o) {
			var d = o.D; var t = o.Transform;
			if (o.Kind == 0) {                                           // textured quads
				for (int i = 0; i < o.N; i++) {
					int k = i * 16; int texId = (int)d[k+12];
					if (!_texPix.TryGetValue(texId, out var tex)) continue;
					int tw = _texW[texId], th = _texH[texId];
					double uMax = d[k+13], vMax = d[k+14], shade = d[k+15];
					Xform(t, d[k],   d[k+1], d[k+2],  out double wx, out double wy, out double wz); ToCam(wx,wy,wz, out _vx[0], out _vy[0], out _vz[0]); _vu[0]=0;    _vv[0]=vMax;
					Xform(t, d[k+3], d[k+4], d[k+5],  out wx, out wy, out wz); ToCam(wx,wy,wz, out _vx[1], out _vy[1], out _vz[1]); _vu[1]=uMax; _vv[1]=vMax;
					Xform(t, d[k+6], d[k+7], d[k+8],  out wx, out wy, out wz); ToCam(wx,wy,wz, out _vx[2], out _vy[2], out _vz[2]); _vu[2]=uMax; _vv[2]=0;
					Xform(t, d[k+9], d[k+10],d[k+11], out wx, out wy, out wz); ToCam(wx,wy,wz, out _vx[3], out _vy[3], out _vz[3]); _vu[3]=0;    _vv[3]=0;
					int m = ClipNear(4); if (m < 3) continue;
					Project(m);
					for (int tri = 1; tri < m - 1; tri++) AddTexTri(0, tri, tri+1, tex, tw, th, shade, o.A);
				}
			} else if (o.Kind == 1) {                                    // flat quads
				byte br = CB(o.R), bg = CB(o.G), bb = CB(o.B);
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
			} else if (o.Kind == 2) {                                    // textured triangles (models)
				for (int i = 0; i < o.N; i++) {
					int k = i * 17; int texId = (int)d[k+15];
					if (!_texPix.TryGetValue(texId, out var tex)) continue;
					int tw = _texW[texId], th = _texH[texId]; double shade = d[k+16];
					Xform(t, d[k],    d[k+1],  d[k+2],  out double wx, out double wy, out double wz); ToCam(wx,wy,wz, out _vx[0], out _vy[0], out _vz[0]); _vu[0]=d[k+3];  _vv[0]=d[k+4];
					Xform(t, d[k+5],  d[k+6],  d[k+7],  out wx, out wy, out wz); ToCam(wx,wy,wz, out _vx[1], out _vy[1], out _vz[1]); _vu[1]=d[k+8];  _vv[1]=d[k+9];
					Xform(t, d[k+10], d[k+11], d[k+12], out wx, out wy, out wz); ToCam(wx,wy,wz, out _vx[2], out _vy[2], out _vz[2]); _vu[2]=d[k+13]; _vv[2]=d[k+14];
					int m = ClipNear(3); if (m < 3) continue;
					Project(m);
					for (int tri = 1; tri < m - 1; tri++) AddTexTri(0, tri, tri+1, tex, tw, th, shade, o.A);
				}
			}
		}

		private static readonly Comparison<SceneObject> _nearFirst = (a, b) => a.Dist.CompareTo(b.Dist);
		private static readonly Comparison<SceneObject> _farFirst = (a, b) => b.Dist.CompareTo(a.Dist);

		/// <summary>Clear depth, cull + sort every visible object, and rasterize them: opaque
		/// front-to-back, then transparent back-to-front. Does not touch the colour buffer's
		/// background (draw your sky/clear first) and does not Upload (call Upload after).</summary>
		public void Render() {
			Array.Clear(_depth, 0, _depth.Length);
			_triN = 0;
			_drawList.Clear();
			foreach (var o in _objects.Values) {
				if (!o.Visible || o.N == 0 || o.Pass != 0 || Culled(o)) continue;
				o.Dist = DistSq(o); _drawList.Add(o);
			}
			_drawList.Sort(_nearFirst);
			for (int i = 0; i < _drawList.Count; i++) BuildObject(_drawList[i]);
			_drawList.Clear();
			foreach (var o in _objects.Values) {
				if (!o.Visible || o.N == 0 || o.Pass == 0 || Culled(o)) continue;
				o.Dist = DistSq(o); _drawList.Add(o);
			}
			_drawList.Sort(_farFirst);
			for (int i = 0; i < _drawList.Count; i++) BuildObject(_drawList[i]);
			int n = _threads;
			if (n <= 1 || _triN == 0) RasterizeBand(0, _h);
			else Parallel.For(0, n, _po, bi => RasterizeBand(bi * _h / n, (bi + 1) * _h / n));
			_canvas.MarkDirty(0, 0, _w - 1, _h - 1);
		}
		#endregion

		#region Display
		public void Upload() => _canvas.Upload();
		public void Draw(int x, int y) => _canvas.Draw(x, y);
		public void DrawAtAnchor(int x, int y, string anchor) => _canvas.DrawAtAnchor(x, y, anchor);
		public void SetScale(float sx, float sy) => _canvas.SetScale(sx, sy);
		public void SetOpacity(float o) => _canvas.SetOpacity(o);
		public void SetColor(float r, float g, float b) => _canvas.SetColor(r, g, b);
		public uint Pointer => _canvas.Pointer;
		#endregion

		#region Dispose
		private bool _disposedValue;
		protected virtual void Dispose(bool disposing) {
			if (!_disposedValue) {
				_canvas?.Dispose();
				_disposeList?.Remove(this);
				_depth = Array.Empty<float>();
				_texPix.Clear(); _texW.Clear(); _texH.Clear();
				_objects.Clear();
				_disposedValue = true;
			}
		}
		public void Dispose() { Dispose(disposing: true); GC.SuppressFinalize(this); }
		#endregion
	}

	public class Lua3DSceneFunc {
		private HashSet<Lua3DScene> Scenes;
		public Lua3DSceneFunc(HashSet<Lua3DScene> scenes) { Scenes = scenes; }
		public Lua3DScene CreateScene(int width, int height) {
			Lua3DScene scene = new(width, height);
			scene._disposeList = Scenes;
			Scenes.Add(scene);
			return scene;
		}
	}
}
