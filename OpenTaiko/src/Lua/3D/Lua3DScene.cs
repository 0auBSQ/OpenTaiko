using System;
using System.Collections.Generic;
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

		public void SetCamera3D(double camx, double camy, double camz,
			double rx, double ry, double rz, double ux, double uy, double uz, double fx, double fy, double fz) {
			_camX = camx; _camY = camy; _camZ = camz;
			_Rx = rx; _Ry = ry; _Rz = rz; _Ux = ux; _Uy = uy; _Uz = uz; _Fx = fx; _Fy = fy; _Fz = fz;
		}

		public void SetProjection3D(double near, double scale) {
			_near = near < 1e-4 ? 1e-4 : near;
			_scale = scale;
		}

		// ── Scene-owned camera ────────────────────────────────────────────────────────
		// Prefer these over SetCamera3D/SetProjection3D: the scene holds the camera state
		// and derives the basis + projection scale itself, so Lua only feeds eye position,
		// yaw/pitch and the lens, then reads the basis back for movement / picking.

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
		private int _mdx0, _mdy0, _mdx1, _mdy1;
		private bool _mddirty;

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

		// Flat triangle, incremental edges, unsafe.
		private unsafe void RasterTriFlat(int i0, int i1, int i2, byte r, byte g, byte b, int alpha) {
			double x0 = _sx[i0], y0 = _sy[i0], w0 = _sw[i0];
			double x1 = _sx[i1], y1 = _sy[i1], w1 = _sw[i1];
			double x2 = _sx[i2], y2 = _sy[i2], w2 = _sw[i2];
			double area = (x1 - x0) * (y2 - y0) - (x2 - x0) * (y1 - y0);
			if (area > -1e-9 && area < 1e-9) return;
			if (area < 0) { double t; t = x1; x1 = x2; x2 = t; t = y1; y1 = y2; y2 = t; t = w1; w1 = w2; w2 = t; area = -area; }
			int minX = (int)Math.Floor(Math.Min(x0, Math.Min(x1, x2))); if (minX < 0) minX = 0;
			int maxX = (int)Math.Ceiling(Math.Max(x0, Math.Max(x1, x2))); if (maxX > _w - 1) maxX = _w - 1;
			int minY = (int)Math.Floor(Math.Min(y0, Math.Min(y1, y2))); if (minY < 0) minY = 0;
			int maxY = (int)Math.Ceiling(Math.Max(y0, Math.Max(y1, y2))); if (maxY > _h - 1) maxY = _h - 1;
			if (minX > maxX || minY > maxY) return;

			double invA = 1.0 / area;
			double d0x = y1 - y2, d0y = x2 - x1;   // d(e0)/dx, /dy
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
								if (opaque) { D[idx] = fid; B[o] = r; B[o + 1] = g; B[o + 2] = b; B[o + 3] = 255; }
								else {
									B[o] = (byte)(r * a + B[o] * ia); B[o + 1] = (byte)(g * a + B[o + 1] * ia);
									B[o + 2] = (byte)(b * a + B[o + 2] * ia); B[o + 3] = 255;
								}
							}
						}
						E0 += d0x; E1 += d1x; E2 += d2x; ID += idDx;
					}
					e0r += d0y; e1r += d1y; e2r += d2y; idr += idDy;
				}
			}
			Bump(minX, minY, maxX, maxY);
		}

		// Textured, perspective-correct, incremental edges + attribute planes, unsafe.
		private unsafe void RasterTriTex(int i0, int i1, int i2, int[] tex, int tw, int th, double shade, int alpha) {
			double x0 = _sx[i0], y0 = _sy[i0], w0 = _sw[i0], uz0 = _su[i0], vz0 = _sv[i0];
			double x1 = _sx[i1], y1 = _sy[i1], w1 = _sw[i1], uz1 = _su[i1], vz1 = _sv[i1];
			double x2 = _sx[i2], y2 = _sy[i2], w2 = _sw[i2], uz2 = _su[i2], vz2 = _sv[i2];
			double area = (x1 - x0) * (y2 - y0) - (x2 - x0) * (y1 - y0);
			if (area > -1e-9 && area < 1e-9) return;
			if (area < 0) {
				double t; t = x1; x1 = x2; x2 = t; t = y1; y1 = y2; y2 = t; t = w1; w1 = w2; w2 = t;
				t = uz1; uz1 = uz2; uz2 = t; t = vz1; vz1 = vz2; vz2 = t; area = -area;
			}
			int minX = (int)Math.Floor(Math.Min(x0, Math.Min(x1, x2))); if (minX < 0) minX = 0;
			int maxX = (int)Math.Ceiling(Math.Max(x0, Math.Max(x1, x2))); if (maxX > _w - 1) maxX = _w - 1;
			int minY = (int)Math.Floor(Math.Min(y0, Math.Min(y1, y2))); if (minY < 0) minY = 0;
			int maxY = (int)Math.Ceiling(Math.Max(y0, Math.Max(y1, y2))); if (maxY > _h - 1) maxY = _h - 1;
			if (minX > maxX || minY > maxY) return;

			double invA = 1.0 / area;
			double d0x = y1 - y2, d0y = x2 - x1;
			double d1x = y2 - y0, d1y = x0 - x2;
			double d2x = y0 - y1, d2y = x1 - x0;
			double cx = minX + 0.5, cy = minY + 0.5;
			double e0r = (x2 - x1) * (cy - y1) - (y2 - y1) * (cx - x1);
			double e1r = (x0 - x2) * (cy - y2) - (y0 - y2) * (cx - x2);
			double e2r = (x1 - x0) * (cy - y0) - (y1 - y0) * (cx - x0);
			// attribute planes (interpolated linearly in screen space): W=1/z, UZ=u/z, VZ=v/z
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
								int o = idx * 4;
								if (opaque) {
									D[idx] = fw; B[o] = CB(tr); B[o + 1] = CB(tg); B[o + 2] = CB(tb); B[o + 3] = 255;
								} else {
									B[o] = (byte)(tr * a + B[o] * ia); B[o + 1] = (byte)(tg * a + B[o + 1] * ia);
									B[o + 2] = (byte)(tb * a + B[o + 2] * ia); B[o + 3] = 255;
								}
							}
						}
						E0 += d0x; E1 += d1x; E2 += d2x; W += wDx; UZ += uDx; VZ += vDx;
					}
					e0r += d0y; e1r += d1y; e2r += d2y; wR += wDy; uR += uDy; vR += vDy;
				}
			}
			Bump(minX, minY, maxX, maxY);
		}

		private void Bump(int minX, int minY, int maxX, int maxY) {
			if (minX < _mdx0) _mdx0 = minX; if (minY < _mdy0) _mdy0 = minY;
			if (maxX > _mdx1) _mdx1 = maxX; if (maxY > _mdy1) _mdy1 = maxY;
			_mddirty = true;
		}
		private void BeginPoly() { _mddirty = false; _mdx0 = _w; _mdy0 = _h; _mdx1 = -1; _mdy1 = -1; }
		private void EndPoly() { if (_mddirty) _canvas.MarkDirty(_mdx0, _mdy0, _mdx1, _mdy1); }

		public void FillTriangleWorld(double x0, double y0, double z0, double x1, double y1, double z1,
			double x2, double y2, double z2, double r, double g, double b) {
			ToCam(x0, y0, z0, out _vx[0], out _vy[0], out _vz[0]);
			ToCam(x1, y1, z1, out _vx[1], out _vy[1], out _vz[1]);
			ToCam(x2, y2, z2, out _vx[2], out _vy[2], out _vz[2]);
			int m = ClipNear(3); if (m < 3) return;
			Project(m); BeginPoly();
			byte br = CB(r), bg = CB(g), bb = CB(b);
			for (int i = 1; i < m - 1; i++) RasterTriFlat(0, i, i + 1, br, bg, bb, 255);
			EndPoly();
		}

		/// <summary>Textured triangle with explicit per-vertex UVs (the primitive for 3D models).</summary>
		public void FillTriangleWorldTex(double x0, double y0, double z0, double u0, double v0,
			double x1, double y1, double z1, double u1, double v1,
			double x2, double y2, double z2, double u2, double v2,
			int texId, double shade, int alpha) {
			if (!_texPix.TryGetValue(texId, out var tex)) return;
			ToCam(x0, y0, z0, out _vx[0], out _vy[0], out _vz[0]); _vu[0] = u0; _vv[0] = v0;
			ToCam(x1, y1, z1, out _vx[1], out _vy[1], out _vz[1]); _vu[1] = u1; _vv[1] = v1;
			ToCam(x2, y2, z2, out _vx[2], out _vy[2], out _vz[2]); _vu[2] = u2; _vv[2] = v2;
			int m = ClipNear(3); if (m < 3) return;
			Project(m); BeginPoly();
			for (int i = 1; i < m - 1; i++) RasterTriTex(0, i, i + 1, tex, _texW[texId], _texH[texId], shade, alpha);
			EndPoly();
		}

		public void FillQuadWorld(double x0, double y0, double z0, double x1, double y1, double z1,
			double x2, double y2, double z2, double x3, double y3, double z3, double r, double g, double b, int alpha) {
			ToCam(x0, y0, z0, out _vx[0], out _vy[0], out _vz[0]);
			ToCam(x1, y1, z1, out _vx[1], out _vy[1], out _vz[1]);
			ToCam(x2, y2, z2, out _vx[2], out _vy[2], out _vz[2]);
			ToCam(x3, y3, z3, out _vx[3], out _vy[3], out _vz[3]);
			int m = ClipNear(4); if (m < 3) return;
			Project(m); BeginPoly();
			byte br = CB(r), bg = CB(g), bb = CB(b);
			for (int i = 1; i < m - 1; i++) RasterTriFlat(0, i, i + 1, br, bg, bb, alpha);
			EndPoly();
		}

		public void FillQuadWorldTex(double x0, double y0, double z0, double x1, double y1, double z1,
			double x2, double y2, double z2, double x3, double y3, double z3,
			int texId, double uMax, double vMax, double shade, int alpha) {
			if (!_texPix.TryGetValue(texId, out var tex)) return;
			int tw = _texW[texId], th = _texH[texId];
			ToCam(x0, y0, z0, out _vx[0], out _vy[0], out _vz[0]); _vu[0] = 0; _vv[0] = vMax;
			ToCam(x1, y1, z1, out _vx[1], out _vy[1], out _vz[1]); _vu[1] = uMax; _vv[1] = vMax;
			ToCam(x2, y2, z2, out _vx[2], out _vy[2], out _vz[2]); _vu[2] = uMax; _vv[2] = 0;
			ToCam(x3, y3, z3, out _vx[3], out _vy[3], out _vz[3]); _vu[3] = 0; _vv[3] = 0;
			int m = ClipNear(4); if (m < 3) return;
			Project(m); BeginPoly();
			for (int i = 1; i < m - 1; i++) RasterTriTex(0, i, i + 1, tex, tw, th, shade, alpha);
			EndPoly();
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
