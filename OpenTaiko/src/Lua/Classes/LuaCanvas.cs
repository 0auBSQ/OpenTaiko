using FDK;
using NLua;

namespace OpenTaiko {
	/// <summary>
	/// A writable pixel surface for Lua scripts: like <see cref="LuaTexture"/> but its
	/// pixels can be edited directly from Lua (SetPixel / FillRect / Clear) and pushed
	/// to the GPU in one upload (<see cref="Upload"/>). Created transparent.
	///
	/// Intended for software rendering (e.g. the raytracer stage): instead of issuing
	/// thousands of coloured quads per frame, write into the buffer and draw it as a
	/// single texture.
	/// </summary>
	public class LuaCanvas : IDisposable {
		internal CTexture? _texture = null;
		internal HashSet<LuaCanvas>? _disposeList = null;

		internal byte[] _buf;         // RGBA, top-left origin
		internal readonly int _w;     // GL pixel buffer size (may be reduced by the render-scale)
		internal readonly int _h;
		internal readonly int _logW;  // LOGICAL display size (what Lua sees + how the surface is presented)
		internal readonly int _logH;
		private bool _dirty;
		// dirty rectangle (inclusive) so Upload only sends the changed region
		private int _dx0, _dy0, _dx1, _dy1;

		public uint Pointer => _texture != null ? _texture.Pointer : 0;
		public int Width => _logW;
		public int Height => _logH;

		public LuaCanvas(int width, int height) : this(width, height, width, height) { }

		/// <summary>Create a canvas whose GL pixel buffer is <paramref name="pixelW"/>×<paramref name="pixelH"/> but that
		/// presents at <paramref name="logicalW"/>×<paramref name="logicalH"/> (used by render-scaled 3D scenes: render
		/// fewer pixels, display + report full size). When logical == pixel this is an ordinary full-res canvas.</summary>
		public LuaCanvas(int pixelW, int pixelH, int logicalW, int logicalH) {
			_w = Math.Max(1, pixelW);
			_h = Math.Max(1, pixelH);
			_logW = Math.Max(1, logicalW);
			_logH = Math.Max(1, logicalH);
			_buf = new byte[_w * _h * 4];          // all zero → fully transparent
			_texture = new CTexture(_w, _h);
			_texture.tUpdateColor4(new(1f, 1f, 1f, 1f));  // no tint
			_texture.tUpdateOpacity(255);
			MarkDirty(0, 0, _w - 1, _h - 1);
			Upload();                              // allocate the GL texture up front
			if (_logW != _w || _logH != _h) _texture.SetLogicalSize(_logW, _logH);   // present at the logical size
		}

		internal void MarkDirty(int x0, int y0, int x1, int y1) {
			if (!_dirty) {
				_dx0 = x0; _dy0 = y0; _dx1 = x1; _dy1 = y1; _dirty = true;
			} else {
				if (x0 < _dx0) _dx0 = x0;
				if (y0 < _dy0) _dy0 = y0;
				if (x1 > _dx1) _dx1 = x1;
				if (y1 > _dy1) _dy1 = y1;
			}
		}

		#region Pixel editing
		/// <summary>Set one pixel. r,g,b,a are 0-255. Out-of-range coords are ignored.</summary>
		public void SetPixel(int x, int y, int r, int g, int b, int a) {
			if ((uint)x >= (uint)_w || (uint)y >= (uint)_h) return;
			int o = (y * _w + x) * 4;
			_buf[o] = (byte)r; _buf[o + 1] = (byte)g; _buf[o + 2] = (byte)b; _buf[o + 3] = (byte)a;
			MarkDirty(x, y, x, y);
		}

		/// <summary>Fill an axis-aligned rectangle (clipped to the canvas). r,g,b,a are 0-255.</summary>
		public void FillRect(int x, int y, int w, int h, int r, int g, int b, int a) {
			if (w <= 0 || h <= 0) return;
			int x0 = Math.Max(0, x), y0 = Math.Max(0, y);
			int x1 = Math.Min(_w, x + w), y1 = Math.Min(_h, y + h);
			if (x1 <= x0 || y1 <= y0) return;
			byte br = (byte)r, bg = (byte)g, bb = (byte)b, ba = (byte)a;
			for (int yy = y0; yy < y1; yy++) {
				int o = (yy * _w + x0) * 4;
				for (int xx = x0; xx < x1; xx++) {
					_buf[o] = br; _buf[o + 1] = bg; _buf[o + 2] = bb; _buf[o + 3] = ba;
					o += 4;
				}
			}
			MarkDirty(x0, y0, x1 - 1, y1 - 1);
		}

		/// <summary>Fill a filled disc (radius in pixels). r,g,b,a are 0-255.</summary>
		public void FillCircle(int cx, int cy, int radius, int r, int g, int b, int a) {
			if (radius < 0) return;
			byte br = (byte)r, bg = (byte)g, bb = (byte)b, ba = (byte)a;
			int r2 = radius * radius;
			int y0 = Math.Max(0, cy - radius), y1 = Math.Min(_h - 1, cy + radius);
			int minx = _w, miny = _h, maxx = -1, maxy = -1;
			for (int yy = y0; yy <= y1; yy++) {
				int dy = yy - cy;
				int span = r2 - dy * dy;
				if (span < 0) continue;
				int hw = (int)Math.Sqrt(span);
				int x0 = Math.Max(0, cx - hw), x1 = Math.Min(_w - 1, cx + hw);
				if (x1 < x0) continue;
				int o = (yy * _w + x0) * 4;
				for (int xx = x0; xx <= x1; xx++) {
					_buf[o] = br; _buf[o + 1] = bg; _buf[o + 2] = bb; _buf[o + 3] = ba;
					o += 4;
				}
				if (x0 < minx) minx = x0;
				if (x1 > maxx) maxx = x1;
				if (yy < miny) miny = yy;
				if (yy > maxy) maxy = yy;
			}
			if (maxx >= 0) MarkDirty(minx, miny, maxx, maxy);
		}

		/// <summary>
		/// Stamp a thick line (a brush stroke) as overlapping discs of the given radius,
		/// entirely in C#. One Lua call paints a whole stroke instead of hundreds.
		/// </summary>
		public void StrokeLine(int x0, int y0, int x1, int y1, int radius, int r, int g, int b, int a) {
			int dx = x1 - x0, dy = y1 - y0;
			double dist = Math.Sqrt(dx * dx + dy * dy);
			int n = Math.Max(1, (int)(dist / Math.Max(1, radius * 0.5)));
			for (int i = 0; i <= n; i++) {
				double t = (double)i / n;
				FillCircle((int)Math.Round(x0 + dx * t), (int)Math.Round(y0 + dy * t), radius, r, g, b, a);
			}
		}

		/// <summary>
		/// Paste a <see cref="LuaTexture"/> onto the canvas with its top-left at (x, y),
		/// alpha-blended over the existing pixels. Pixels falling outside the canvas are
		/// ignored. Reads the texture back from the GPU (one-off cost), so avoid per-frame use.
		/// </summary>
		public void PasteTexture(LuaTexture tex, int x, int y) {
			PasteTextureTransformed(tex, x, y, 1f, 0f, "topleft");
		}

		/// <summary>
		/// Paste a <see cref="LuaTexture"/> onto the canvas scaled by <paramref name="scale"/> and
		/// rotated by <paramref name="rotationDeg"/> (clockwise). When <paramref name="anchor"/> is
		/// "center" the texture's centre lands at (x, y); otherwise its top-left does. Alpha-blended;
		/// out-of-canvas pixels are ignored. The sampling is nearest-neighbour (keeps pixel art crisp).
		/// </summary>
		public void PasteTextureTransformed(LuaTexture tex, int x, int y, float scale, float rotationDeg, string anchor) {
			if (tex?._texture == null || scale <= 0f) return;
			byte[]? src = tex.GetCachedPixels(out int sw, out int sh);
			if (src == null || sw <= 0 || sh <= 0) return;

			bool center = (anchor ?? "").ToLower() == "center";
			// placement origin in canvas space the source is positioned around
			double ox = x, oy = y;
			// source pivot (the point that maps to (ox,oy))
			double pvx = center ? sw * 0.5 : 0.0;
			double pvy = center ? sh * 0.5 : 0.0;

			double rad = rotationDeg * Math.PI / 180.0;
			double cs = Math.Cos(rad), sn = Math.Sin(rad);

			// destination bounding box: transform the 4 source corners
			double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
			void corner(double u, double v) {
				double rx = (u - pvx) * scale, ry = (v - pvy) * scale;
				double dx = ox + (rx * cs - ry * sn);
				double dy = oy + (rx * sn + ry * cs);
				if (dx < minX) minX = dx; if (dx > maxX) maxX = dx;
				if (dy < minY) minY = dy; if (dy > maxY) maxY = dy;
			}
			corner(0, 0); corner(sw, 0); corner(0, sh); corner(sw, sh);

			int bx0 = Math.Max(0, (int)Math.Floor(minX)), by0 = Math.Max(0, (int)Math.Floor(minY));
			int bx1 = Math.Min(_w - 1, (int)Math.Ceiling(maxX)), by1 = Math.Min(_h - 1, (int)Math.Ceiling(maxY));
			if (bx1 < bx0 || by1 < by0) return;

			double inv = 1.0 / scale;
			for (int py = by0; py <= by1; py++) {
				for (int px = bx0; px <= bx1; px++) {
					// inverse transform dest → source
					double ddx = (px + 0.5) - ox, ddy = (py + 0.5) - oy;
					double ux = (ddx * cs + ddy * sn) * inv + pvx;
					double uy = (-ddx * sn + ddy * cs) * inv + pvy;
					int su = (int)Math.Floor(ux), sv = (int)Math.Floor(uy);
					if ((uint)su >= (uint)sw || (uint)sv >= (uint)sh) continue;
					int so = (sv * sw + su) * 4;
					int sa = src[so + 3];
					if (sa == 0) continue;
					int o = (py * _w + px) * 4;
					if (sa >= 255) {
						_buf[o] = src[so]; _buf[o + 1] = src[so + 1]; _buf[o + 2] = src[so + 2]; _buf[o + 3] = 255;
					} else {
						double a = sa / 255.0, ia = 1.0 - a;
						_buf[o]     = (byte)(src[so]     * a + _buf[o]     * ia);
						_buf[o + 1] = (byte)(src[so + 1] * a + _buf[o + 1] * ia);
						_buf[o + 2] = (byte)(src[so + 2] * a + _buf[o + 2] * ia);
						_buf[o + 3] = (byte)Math.Min(255, sa + _buf[o + 3] * ia);
					}
				}
			}
			MarkDirty(bx0, by0, bx1, by1);
		}

		/// <summary>Fill the whole canvas with a colour (r,g,b,a 0-255).</summary>
		public void Clear(int r, int g, int b, int a) => FillRect(0, 0, _w, _h, r, g, b, a);

		/// <summary>Copy another same-size canvas's pixels into this one (marks the whole canvas
		/// dirty; the caller uploads). Used for undo: a "base" layer of baked-in edits.</summary>
		public void CopyFrom(LuaCanvas other) {
			if (other == null || other._buf.Length != _buf.Length) return;
			Array.Copy(other._buf, _buf, _buf.Length);
			MarkDirty(0, 0, _w - 1, _h - 1);
		}

		/// <summary>Reset the whole canvas to transparent.</summary>
		public void ClearTransparent() {
			Array.Clear(_buf, 0, _buf.Length);
			MarkDirty(0, 0, _w - 1, _h - 1);
		}

		/// <summary>Push pending pixel edits to the GPU (only the changed region). No-op if clean.</summary>
		public void Upload() {
			if (_dirty && _texture != null) {
				_texture.UpdatePixelBufferRegion(_buf, _w, _h,
					_dx0, _dy0, _dx1 - _dx0 + 1, _dy1 - _dy0 + 1);
				_dirty = false;
			}
		}

		/// <summary>
		/// Fast full-surface blit from a Lua array of packed 0xRRGGBB integers
		/// (1-indexed, length = count, row-major; pixels are made fully opaque), then
		/// uploads in one call. This avoids per-pixel Lua↔C# calls, making it practical
		/// to repaint an entire software-rendered frame every frame.
		/// </summary>
		public void BlitPacked(LuaTable data, int count) {
			if (data == null) return;
			int n = Math.Min(count, _w * _h);
			for (int i = 0; i < n; i++) {
				object o = data[i + 1];
				long p = o switch {
					long l   => l,
					double d => (long)d,
					int ii   => ii,
					_        => 0L,
				};
				int b4 = i * 4;
				if (p < 0) { _buf[b4] = 0; _buf[b4 + 1] = 0; _buf[b4 + 2] = 0; _buf[b4 + 3] = 0; continue; }  // cutout texel
				_buf[b4]     = (byte)((p >> 16) & 0xFF);
				_buf[b4 + 1] = (byte)((p >> 8) & 0xFF);
				_buf[b4 + 2] = (byte)(p & 0xFF);
				_buf[b4 + 3] = 255;
			}
			MarkDirty(0, 0, _w - 1, _h - 1);
			Upload();
		}
		#endregion

		#region Drawing (mirrors LuaTexture)
		public void Draw(int x, int y) {
			_texture?.t2DDraw(x, y);
		}
		/// <summary>Draw only a sub-rectangle of the canvas (source rect in canvas pixels) at x,y —
		/// scrolling lists slice partially-visible rows with this so they clip to their viewport.</summary>
		public void DrawRect(int x, int y, int rect_x, int rect_y, int rect_width, int rect_height) {
			_texture?.t2DDraw(x, y, new System.Drawing.RectangleF(rect_x, rect_y, rect_width, rect_height));
		}
		public void DrawAtAnchor(int x, int y, string anchor) {
			CTexture.RefPnt ref_anchor = anchor.ToLower() switch {
				"topleft" => CTexture.RefPnt.UpLeft,
				"top" => CTexture.RefPnt.Up,
				"topright" => CTexture.RefPnt.UpRight,
				"left" => CTexture.RefPnt.Left,
				"center" => CTexture.RefPnt.Center,
				"right" => CTexture.RefPnt.Right,
				"bottomleft" => CTexture.RefPnt.DownLeft,
				"bottom" => CTexture.RefPnt.Down,
				"bottomright" => CTexture.RefPnt.DownRight,
				_ => CTexture.RefPnt.UpLeft
			};
			_texture?.t2DScaledDraw(ref_anchor, x, y, new(0, 0, _logW, _logH));
		}
		public void SetScale(float scale_x, float scale_y) {
			_texture?.tSetScale(scale_x, scale_y);
		}
		public void SetOpacity(float opacity) {
			_texture?.tUpdateOpacity((int)(opacity * 255));
		}
		public void SetColor(float red, float green, float blue) {
			_texture?.tUpdateColor4(new(red, green, blue, 1f));
		}
		#endregion

		#region Dispose
		private bool _disposedValue;
		protected virtual void Dispose(bool disposing) {
			if (!_disposedValue) {
				OpenTaiko.tDisposeSafely(ref _texture);
				_disposeList?.Remove(this);
				_buf = Array.Empty<byte>();
				_disposedValue = true;
			}
		}
		public void Dispose() {
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
		#endregion
	}

	public class LuaCanvasFunc {
		private HashSet<LuaCanvas> Canvases;

		public LuaCanvasFunc(HashSet<LuaCanvas> canvases) {
			Canvases = canvases;
		}

		/// <summary>Create a transparent writable canvas of the given size.</summary>
		public LuaCanvas CreateCanvas(int width, int height) {
			LuaCanvas canvas = new(width, height);
			canvas._disposeList = Canvases;
			Canvases.Add(canvas);
			return canvas;
		}
	}
}
