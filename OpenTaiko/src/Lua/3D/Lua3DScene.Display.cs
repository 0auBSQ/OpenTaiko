using System;
using FDK;

namespace OpenTaiko {
	// Presentation and 2D drawing on the colour buffer.
	public partial class Lua3DScene {
		public void Clear(int r, int g, int b, int a) { if (!_disposedValue) _canvas.Clear(r, g, b, a); }
		/// <summary>Reset the colour buffer to fully transparent (Clear alpha-blends, so it cannot):
		/// the frame then composites over whatever the stage drew underneath.</summary>
		public void ClearTransparent() { if (!_disposedValue) _canvas.ClearTransparent(); }
		public void FillRect(int x, int y, int w, int h, int r, int g, int b, int a) {
			if (_disposedValue) return;
			if (_w != _logicalW) {   // render-scale: inputs are LOGICAL → map to the reduced pixel buffer
				double sc = (double)_w / _logicalW;
				int x2 = (int)Math.Round((x + w) * sc), y2 = (int)Math.Round((y + h) * sc);
				x = (int)Math.Round(x * sc); y = (int)Math.Round(y * sc); w = x2 - x; h = y2 - y;
			}
			if (_gpuOwnsCanvas && _overlay2d != null) _overlay2d.Rect2D(this, x, y, w, h, r, g, b, a);   // after GPU 3D: composite onto the canvas texture
			else _canvas.FillRect(x, y, w, h, r, g, b, a);
		}
		public void ClearDepth() => Array.Clear(_depth, 0, _depth.Length);

		/// <summary>2D line in the colour buffer (screen pixels), drawn on top (no depth).</summary>
		public void DrawLine(int x0, int y0, int x1, int y1, int r, int g, int b) {
			if (_disposedValue) return;
			if (_w != _logicalW) {   // render-scale: inputs are LOGICAL → map to the reduced pixel buffer
				double sc = (double)_w / _logicalW;
				x0 = (int)Math.Round(x0 * sc); y0 = (int)Math.Round(y0 * sc);
				x1 = (int)Math.Round(x1 * sc); y1 = (int)Math.Round(y1 * sc);
			}
			if (_gpuOwnsCanvas && _overlay2d != null) { _overlay2d.Line2D(this, x0, y0, x1, y1, r, g, b); return; }   // after GPU 3D: composite onto the canvas texture
			if (!ClipLine(ref x0, ref y0, ref x1, ref y1, _w, _h)) return;
			byte br = RenderUtil.CB(r), bg = RenderUtil.CB(g), bb = RenderUtil.CB(b);
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

		/// <summary>Clip the segment to the pixel rectangle [0, w-1] x [0, h-1] (Liang-Barsky), so a line drawn
		/// from far off-screen loops only over the pixels it covers. False when no part of it is inside.</summary>
		internal static bool ClipLine(ref int x0, ref int y0, ref int x1, ref int y1, int w, int h) {
			if (w <= 0 || h <= 0) return false;
			double ax = x0, ay = y0, dx = (double)x1 - x0, dy = (double)y1 - y0, xmax = w - 1, ymax = h - 1;
			double t0 = 0, t1 = 1;
			bool Edge(double p, double q) {   // p * t <= q must hold along the kept part
				if (p == 0) return q >= 0;
				double t = q / p;
				if (p < 0) { if (t > t1) return false; if (t > t0) t0 = t; }
				else { if (t < t0) return false; if (t < t1) t1 = t; }
				return true;
			}
			if (!Edge(-dx, ax) || !Edge(dx, xmax - ax) || !Edge(-dy, ay) || !Edge(dy, ymax - ay)) return false;
			x0 = (int)Math.Clamp(Math.Round(ax + t0 * dx), 0, xmax); y0 = (int)Math.Clamp(Math.Round(ay + t0 * dy), 0, ymax);
			x1 = (int)Math.Clamp(Math.Round(ax + t1 * dx), 0, xmax); y1 = (int)Math.Clamp(Math.Round(ay + t1 * dy), 0, ymax);
			return true;
		}

		public void Upload() {
			if (_disposedValue) return;
			if (_gpuOwnsCanvas) { _overlay2d?.End2D(); _gpuOwnsCanvas = false; return; }
			_canvas.Upload();
		}

		/// <summary>Copy this scene's CURRENT CPU-rendered colour buffer into an independent, standalone
		/// texture registered in the global shared-texture store under <paramref name="key"/>, so another
		/// stage can draw it later (e.g. the coin shop reusing My Room's furniture previews without
		/// re-loading the models). Only valid after a CPU render — SetMode("raster_cpu") + Render + Upload;
		/// a GPU-owned canvas exposes no readable CPU buffer, so this is a safe no-op there. The copy is
		/// independent, so it survives the source scene/stage being disposed.</summary>
		public void ShareCanvasAs(string key) {
			if (_disposedValue || string.IsNullOrEmpty(key) || _gpuOwnsCanvas || _canvas == null) return;
			int w = _w, h = _h;
			byte[] src = _canvas._buf;
			if (src == null || w <= 0 || h <= 0 || src.Length < w * h * 4) return;
			byte[] copy = new byte[w * h * 4];
			Array.Copy(src, copy, copy.Length);
			var ctex = new CTexture();
			ctex.UpdatePixelBuffer(copy, w, h);
			var tex = new LuaTexture(ctex);
			var store = OpenTaiko.GlobalStores.SharedTextures;
			if (store.TryGetValue(key, out var res)) res.Adopt(tex);
			else { var r = new LuaSharedResource<LuaTexture>(); r.Adopt(tex); store[key] = r; }
		}
		/// <summary>Silhouette OUTLINE post-pass on the CPU canvas: inks every transparent pixel within
		/// <paramref name="thickness"/> px of the rendered shape, with a ~1px ANTI-ALIASED falloff at the
		/// rim. Implemented as a 3-4 chamfer distance transform (two scans, cost independent of the
		/// thickness — the naive per-ring dilation was a frame-rate killer).</summary>
		public void ApplyOutline(int r, int g, int b, int thickness) {
			if (_disposedValue) return;
			byte[] buf = _canvas._buf;
			int w = _w, h = _h, n = w * h;
			if (_outlineDist == null || _outlineDist.Length < n) _outlineDist = new int[n];
			var dist = _outlineDist;
			const int INF = 1 << 28;
			for (int i = 0; i < n; i++) dist[i] = buf[i * 4 + 3] == 255 ? 0 : INF;
			// forward chamfer pass (3 orthogonal / 4 diagonal)
			for (int y = 0; y < h; y++) {
				int row = y * w;
				for (int x = 0; x < w; x++) {
					int i = row + x; int d = dist[i]; if (d == 0) continue;
					if (x > 0 && dist[i - 1] + 3 < d) d = dist[i - 1] + 3;
					if (y > 0) {
						if (dist[i - w] + 3 < d) d = dist[i - w] + 3;
						if (x > 0 && dist[i - w - 1] + 4 < d) d = dist[i - w - 1] + 4;
						if (x < w - 1 && dist[i - w + 1] + 4 < d) d = dist[i - w + 1] + 4;
					}
					dist[i] = d;
				}
			}
			// backward pass
			for (int y = h - 1; y >= 0; y--) {
				int row = y * w;
				for (int x = w - 1; x >= 0; x--) {
					int i = row + x; int d = dist[i]; if (d == 0) continue;
					if (x < w - 1 && dist[i + 1] + 3 < d) d = dist[i + 1] + 3;
					if (y < h - 1) {
						if (dist[i + w] + 3 < d) d = dist[i + w] + 3;
						if (x < w - 1 && dist[i + w + 1] + 4 < d) d = dist[i + w + 1] + 4;
						if (x > 0 && dist[i + w - 1] + 4 < d) d = dist[i + w - 1] + 4;
					}
					dist[i] = d;
				}
			}
			byte br = RenderUtil.CB(r), bg = RenderUtil.CB(g), bb = RenderUtil.CB(b);
			double th3 = thickness * 3.0;                       // chamfer units (3 per pixel)
			for (int i = 0; i < n; i++) {
				int o = i * 4;
				if (buf[o + 3] != 0) continue;
				double d = dist[i];
				if (d > th3 + 3.0) continue;
				double a2 = d <= th3 ? 1.0 : 1.0 - (d - th3) / 3.0;   // soft 1px rim = anti-aliased edge
				if (a2 <= 0) continue;
				buf[o] = br; buf[o + 1] = bg; buf[o + 2] = bb; buf[o + 3] = (byte)(a2 * 255.0);
			}
		}
		private int[]? _outlineDist;

		public void Draw(int x, int y) { if (!_disposedValue) _canvas.Draw(x, y); }
		public void DrawAtAnchor(int x, int y, string anchor) { if (!_disposedValue) _canvas.DrawAtAnchor(x, y, anchor); }
		/// <summary>Blit the rendered scene scaled/tinted/faded at an anchor — used by 3D CHARACTERS,
		/// whose draw contract (scale/opacity/colour) comes from the 2D chara pipeline.</summary>
		public void DrawScaled(int x, int y, double scaleX, double scaleY, double opacity, double r, double g, double b, string anchor) {
			if (_disposedValue) return;
			_canvas.SetScale((float)scaleX, (float)scaleY);
			_canvas.SetOpacity((float)opacity);
			_canvas.SetColor((float)r, (float)g, (float)b);
			_canvas.DrawAtAnchor(x, y, anchor ?? "bottom");
			_canvas.SetScale(1f, 1f); _canvas.SetOpacity(1f); _canvas.SetColor(1f, 1f, 1f);
		}
		public void SetScale(float sx, float sy) { if (!_disposedValue) _canvas.SetScale(sx, sy); }
		public void SetOpacity(float o) { if (!_disposedValue) _canvas.SetOpacity(o); }
		public void SetColor(float r, float g, float b) { if (!_disposedValue) _canvas.SetColor(r, g, b); }
		public uint Pointer => _disposedValue ? 0 : _canvas.Pointer;
	}
}
