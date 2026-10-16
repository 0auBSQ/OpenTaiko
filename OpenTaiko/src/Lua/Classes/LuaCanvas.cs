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
	public class LuaCanvas : LuaTexture, IDisposable {
		internal byte[] _buf;         // RGBA, top-left origin
		internal readonly int _w;     // GL pixel buffer size (may be reduced by the render-scale)
		internal readonly int _h;
		internal readonly int _logW;  // LOGICAL display size (what Lua sees + how the surface is presented)
		internal readonly int _logH;
		private bool _dirty;
		// dirty rectangle (inclusive) so Upload only sends the changed region
		private int _dx0, _dy0, _dx1, _dy1;
		// counts MarkDirty calls: a writer can tell whether anything drew into the buffer since it last did
		internal int _writes;
		// the CPU copy was let go after an upload (Upload with release = true); the next edit starts a new one
		private bool _released;

		public override int Width => Loaded ? _logW : -1;
		public override int Height => Loaded ? _logH : -1;

		public LuaCanvas(int width, int height) : this(width, height, width, height) { }

		/// <summary>Create a canvas whose GL pixel buffer is <paramref name="pixelW"/>×<paramref name="pixelH"/> but that
		/// presents at <paramref name="logicalW"/>×<paramref name="logicalH"/> (used by render-scaled 3D scenes: render
		/// fewer pixels, display + report full size). When logical == pixel this is an ordinary full-res canvas.</summary>
		public LuaCanvas(int pixelW, int pixelH, int logicalW, int logicalH) : base() {
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
			_writes++;
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
		// a released canvas (see Upload) edits a new transparent buffer, uploaded whole on the next Upload
		private void EnsureBuf() {
			if (!_released) return;
			_buf = new byte[_w * _h * 4];
			_released = false;
			MarkDirty(0, 0, _w - 1, _h - 1);
		}

		/// <summary>Set one pixel. r,g,b,a are 0-255. Out-of-range coords are ignored.</summary>
		public void SetPixel(int x, int y, int r, int g, int b, int a) {
			if ((uint)x >= (uint)_w || (uint)y >= (uint)_h) return;
			EnsureBuf();
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
			EnsureBuf();
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
			EnsureBuf();
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
			EnsureBuf();

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
			if (other == null || other._released || other._w != _w || other._h != _h) return;
			EnsureBuf();
			Array.Copy(other._buf, _buf, _buf.Length);
			MarkDirty(0, 0, _w - 1, _h - 1);
		}

		/// <summary>Reset the whole canvas to transparent.</summary>
		public void ClearTransparent() {
			if (_released) { EnsureBuf(); return; }
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

		/// <summary>Upload with options (all optional): extendEdges = true runs <see cref="ExtendEdges"/> first; release = true
		/// then lets go of the CPU copy of the pixels, for a canvas that is drawn but no longer edited (halves its
		/// memory). Editing a released canvas starts again from a transparent canvas.</summary>
		public void Upload(LuaTable? options) {
			if (options != null && options["extendEdges"] is true) ExtendEdges();
			Upload();
			if (options != null && options["release"] is true && !_released && !_dirty && _texture != null) {
				_buf = Array.Empty<byte>();
				_released = true;
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
			EnsureBuf();
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

		#region Anti-aliased shapes
		// The shape calls draw with smooth edges and blend into what is already there (SetPixel, FillRect and
		// FillCircle overwrite). Each takes an optional options table:
		//   mode   "over" (default): blends over the pixels it covers, by its alpha and how much of each pixel it covers
		//          "replace": takes the place of what it covers, like FillRect, with a smooth edge
		//          "cut": erases what it covers (each pixel's alpha drops by the shape's alpha times its coverage)
		//   soft   the width of the edge in pixels (default 1, a crisp edge); a wider edge fades out, e.g. a shadow
		//   bottom {r, g, b, a}: a vertical gradient from the call's colour to this one
		//   fromY, toY: where that gradient starts and ends (default: the top and bottom of the shape)
		private enum ShapeMode { Over, Replace, Cut }

		private struct ShapeStyle {
			public ShapeMode Mode;
			public double Soft;
			public double R0, G0, B0, A0, R1, G1, B1, A1;
			public bool Gradient;
			public double FromY, ToY;
		}

		private static double Num(object? v, double fallback) {
			if (v == null) return fallback;
			try { return Convert.ToDouble(v); } catch { return fallback; }
		}

		private static ShapeStyle StyleOf(int r, int g, int b, int a, LuaTable? options, double top, double bottom) {
			var st = new ShapeStyle {
				Mode = ShapeMode.Over, Soft = 1,
				R0 = r, G0 = g, B0 = b, A0 = a, R1 = r, G1 = g, B1 = b, A1 = a,
				FromY = top, ToY = bottom,
			};
			if (options == null) return st;
			switch ((options["mode"] as string)?.ToLowerInvariant()) {
				case "replace": st.Mode = ShapeMode.Replace; break;
				case "cut": st.Mode = ShapeMode.Cut; break;
			}
			st.Soft = Math.Max(1, Num(options["soft"], 1));
			if (options["bottom"] is LuaTable c) {
				st.Gradient = true;
				st.R1 = Num(c[1], r); st.G1 = Num(c[2], g); st.B1 = Num(c[3], b); st.A1 = Num(c[4], 255);
			}
			st.FromY = Num(options["fromY"], top);
			st.ToY = Num(options["toY"], bottom);
			return st;
		}

		// how much of a pixel a shape covers, from the signed distance of the pixel's centre to its edge
		private static double Coverage(double d, double soft) {
			if (soft <= 1) return Math.Clamp(0.5 - d, 0, 1);
			double t = Math.Clamp(0.5 - d / soft, 0, 1);
			return t * t * (3 - 2 * t);
		}

		// blends colour (0..255 channels) into pixel o, covering `cov` of it
		private void BlendPixel(int o, double r, double g, double b, double a, double cov, ShapeMode mode) {
			double sa = a / 255.0 * cov;
			double da = _buf[o + 3] / 255.0;
			if (mode == ShapeMode.Cut) {
				if (sa <= 0) return;
				_buf[o + 3] = (byte)Math.Round(Math.Clamp(da * (1 - sa), 0, 1) * 255);
				return;
			}
			double oa, or, og, ob;
			if (mode == ShapeMode.Replace) {
				// a coverage-weighted swap, in premultiplied colour so the edge keeps the shape's colour
				double fa = a / 255.0;
				oa = da + (fa - da) * cov;
				if (oa <= 0) { _buf[o + 3] = 0; return; }
				or = (_buf[o] * da + (r * fa - _buf[o] * da) * cov) / oa;
				og = (_buf[o + 1] * da + (g * fa - _buf[o + 1] * da) * cov) / oa;
				ob = (_buf[o + 2] * da + (b * fa - _buf[o + 2] * da) * cov) / oa;
			} else {
				if (sa <= 0) return;
				if (sa >= 0.999) { _buf[o] = (byte)Math.Round(r); _buf[o + 1] = (byte)Math.Round(g); _buf[o + 2] = (byte)Math.Round(b); _buf[o + 3] = 255; return; }
				double k = da * (1 - sa);
				oa = sa + k;
				or = (r * sa + _buf[o] * k) / oa;
				og = (g * sa + _buf[o + 1] * k) / oa;
				ob = (b * sa + _buf[o + 2] * k) / oa;
			}
			_buf[o] = (byte)Math.Round(Math.Clamp(or, 0, 255));
			_buf[o + 1] = (byte)Math.Round(Math.Clamp(og, 0, 255));
			_buf[o + 2] = (byte)Math.Round(Math.Clamp(ob, 0, 255));
			_buf[o + 3] = (byte)Math.Round(Math.Clamp(oa, 0, 1) * 255);
		}

		// fills every pixel near the box (x0,y0)-(x1,y1) by the coverage of `sdf` (signed distance, negative inside)
		private void FillDistance(double x0, double y0, double x1, double y1, in ShapeStyle st, Func<double, double, double> sdf) {
			double grow = st.Soft * 0.5 + 1;
			int px0 = Math.Max(0, (int)Math.Floor(x0 - grow)), py0 = Math.Max(0, (int)Math.Floor(y0 - grow));
			int px1 = Math.Min(_w - 1, (int)Math.Ceiling(x1 + grow)), py1 = Math.Min(_h - 1, (int)Math.Ceiling(y1 + grow));
			if (px1 < px0 || py1 < py0) return;
			EnsureBuf();
			for (int py = py0; py <= py1; py++) {
				double cy = py + 0.5;
				RowColour(st, cy, out double r, out double g, out double b, out double a);
				int o = (py * _w + px0) * 4;
				for (int px = px0; px <= px1; px++, o += 4) {
					double cov = Coverage(sdf(px + 0.5, cy), st.Soft);
					if (cov > 0) BlendPixel(o, r, g, b, a, cov, st.Mode);
				}
			}
			MarkDirty(px0, py0, px1, py1);
		}

		private static void RowColour(in ShapeStyle st, double y, out double r, out double g, out double b, out double a) {
			if (!st.Gradient) { r = st.R0; g = st.G0; b = st.B0; a = st.A0; return; }
			double span = st.ToY - st.FromY;
			double t = span > 0 ? Math.Clamp((y - st.FromY) / span, 0, 1) : 0;
			r = st.R0 + (st.R1 - st.R0) * t; g = st.G0 + (st.G1 - st.G0) * t;
			b = st.B0 + (st.B1 - st.B0) * t; a = st.A0 + (st.A1 - st.A0) * t;
		}

		/// <summary>Fill a rounded rectangle with a smooth edge, blended into the canvas (see the options above).
		/// The options can also give radii = {topLeft, topRight, bottomRight, bottomLeft} in place of one radius.</summary>
		public void FillRoundRect(double x, double y, double w, double h, double radius, int r, int g, int b, int a) =>
			FillRoundRect(x, y, w, h, radius, r, g, b, a, null);
		public void FillRoundRect(double x, double y, double w, double h, double radius, int r, int g, int b, int a, LuaTable? options) {
			if (w <= 0 || h <= 0) return;
			var st = StyleOf(r, g, b, a, options, y, y + h);
			double hx = w * 0.5, hy = h * 0.5, cx = x + hx, cy = y + hy;
			double lim = Math.Min(hx, hy);
			double tl = radius, tr = radius, br = radius, bl = radius;
			if (options?["radii"] is LuaTable rad) {
				tl = Num(rad[1], radius); tr = Num(rad[2], radius); br = Num(rad[3], radius); bl = Num(rad[4], radius);
			}
			tl = Math.Clamp(tl, 0, lim); tr = Math.Clamp(tr, 0, lim); br = Math.Clamp(br, 0, lim); bl = Math.Clamp(bl, 0, lim);
			FillDistance(x, y, x + w, y + h, st, (px, py) => {
				double dx = px - cx, dy = py - cy;
				double rr = dx > 0 ? (dy > 0 ? br : tr) : (dy > 0 ? bl : tl);
				double qx = Math.Abs(dx) - hx + rr, qy = Math.Abs(dy) - hy + rr;
				double ox = Math.Max(qx, 0), oy = Math.Max(qy, 0);
				return Math.Sqrt(ox * ox + oy * oy) + Math.Min(Math.Max(qx, qy), 0) - rr;
			});
		}

		/// <summary>Fill the ellipse inside the box (x, y, w, h) with a smooth edge, blended into the canvas.</summary>
		public void FillEllipse(double x, double y, double w, double h, int r, int g, int b, int a) =>
			FillEllipse(x, y, w, h, r, g, b, a, null);
		public void FillEllipse(double x, double y, double w, double h, int r, int g, int b, int a, LuaTable? options) {
			if (w <= 0 || h <= 0) return;
			var st = StyleOf(r, g, b, a, options, y, y + h);
			double ax = w * 0.5, ay = h * 0.5, cx = x + ax, cy = y + ay;
			FillDistance(x, y, x + w, y + h, st, (px, py) => {
				double dx = Math.Abs(px - cx), dy = Math.Abs(py - cy);
				double k0 = Math.Sqrt(dx * dx / (ax * ax) + dy * dy / (ay * ay));
				double k1 = Math.Sqrt(dx * dx / (ax * ax * ax * ax) + dy * dy / (ay * ay * ay * ay));
				return k1 > 0 ? k0 * (k0 - 1) / k1 : -Math.Min(ax, ay);
			});
		}

		/// <summary>Fill a triangle with smooth edges, blended into the canvas.</summary>
		public void FillTriangle(double x1, double y1, double x2, double y2, double x3, double y3, int r, int g, int b, int a) =>
			FillTriangle(x1, y1, x2, y2, x3, y3, r, g, b, a, null);
		public void FillTriangle(double x1, double y1, double x2, double y2, double x3, double y3, int r, int g, int b, int a, LuaTable? options) {
			double top = Math.Min(y1, Math.Min(y2, y3)), bot = Math.Max(y1, Math.Max(y2, y3));
			var st = StyleOf(r, g, b, a, options, top, bot);
			double e0x = x2 - x1, e0y = y2 - y1, e1x = x3 - x2, e1y = y3 - y2, e2x = x1 - x3, e2y = y1 - y3;
			double l0 = e0x * e0x + e0y * e0y, l1 = e1x * e1x + e1y * e1y, l2 = e2x * e2x + e2y * e2y;
			double area2 = e0x * e2y - e0y * e2x;
			if (l0 <= 0 || l1 <= 0 || l2 <= 0 || Math.Abs(area2) < 1e-9) return;   // a flat triangle covers nothing
			double sgn = Math.Sign(area2);
			FillDistance(Math.Min(x1, Math.Min(x2, x3)), top, Math.Max(x1, Math.Max(x2, x3)), bot, st, (px, py) => {
				double v0x = px - x1, v0y = py - y1, v1x = px - x2, v1y = py - y2, v2x = px - x3, v2y = py - y3;
				double t0 = Math.Clamp((v0x * e0x + v0y * e0y) / l0, 0, 1);
				double t1 = Math.Clamp((v1x * e1x + v1y * e1y) / l1, 0, 1);
				double t2 = Math.Clamp((v2x * e2x + v2y * e2y) / l2, 0, 1);
				double q0x = v0x - e0x * t0, q0y = v0y - e0y * t0;
				double q1x = v1x - e1x * t1, q1y = v1y - e1y * t1;
				double q2x = v2x - e2x * t2, q2y = v2y - e2y * t2;
				double d = Math.Min(q0x * q0x + q0y * q0y, Math.Min(q1x * q1x + q1y * q1y, q2x * q2x + q2y * q2y));
				double s0 = sgn * (v0x * e0y - v0y * e0x), s1 = sgn * (v1x * e1y - v1y * e1x), s2 = sgn * (v2x * e2y - v2y * e2x);
				bool inside = s0 >= 0 && s1 >= 0 && s2 >= 0;
				return inside ? -Math.Sqrt(d) : Math.Sqrt(d);
			});
		}

		/// <summary>Fill a capsule (a line from (x0, y0) to (x1, y1) with round ends, `radius` thick on each side)
		/// with a smooth edge, blended into the canvas.</summary>
		public void FillCapsule(double x0, double y0, double x1, double y1, double radius, int r, int g, int b, int a) =>
			FillCapsule(x0, y0, x1, y1, radius, r, g, b, a, null);
		public void FillCapsule(double x0, double y0, double x1, double y1, double radius, int r, int g, int b, int a, LuaTable? options) {
			if (radius <= 0) return;
			double top = Math.Min(y0, y1) - radius, bot = Math.Max(y0, y1) + radius;
			var st = StyleOf(r, g, b, a, options, top, bot);
			double bx = x1 - x0, by = y1 - y0, bb = bx * bx + by * by;
			FillDistance(Math.Min(x0, x1) - radius, top, Math.Max(x0, x1) + radius, bot, st, (px, py) => {
				double ax = px - x0, ay = py - y0;
				double t = bb > 0 ? Math.Clamp((ax * bx + ay * by) / bb, 0, 1) : 0;
				double dx = ax - bx * t, dy = ay - by * t;
				return Math.Sqrt(dx * dx + dy * dy) - radius;
			});
		}

		/// <summary>Fill a polygon (a flat list {x1, y1, x2, y2, ...}, even-odd rule) with smooth edges, blended into
		/// the canvas. Takes the same options as the other shapes except soft.</summary>
		public void FillPolygon(LuaTable points, int r, int g, int b, int a) => FillPolygon(points, r, g, b, a, null);
		public void FillPolygon(LuaTable points, int r, int g, int b, int a, LuaTable? options) {
			if (points == null) return;
			var xs = new List<double>();
			var ys = new List<double>();
			for (int i = 1; ; i += 2) {
				object? vx = points[i], vy = points[i + 1];
				if (vx == null || vy == null) break;
				xs.Add(Num(vx, 0)); ys.Add(Num(vy, 0));
			}
			int n = xs.Count;
			if (n < 3) return;
			double minX = xs.Min(), maxX = xs.Max(), minY = ys.Min(), maxY = ys.Max();
			var st = StyleOf(r, g, b, a, options, minY, maxY);
			int px0 = Math.Max(0, (int)Math.Floor(minX)), px1 = Math.Min(_w - 1, (int)Math.Ceiling(maxX));
			int py0 = Math.Max(0, (int)Math.Floor(minY)), py1 = Math.Min(_h - 1, (int)Math.Ceiling(maxY));
			if (px1 < px0 || py1 < py0) return;
			EnsureBuf();
			const int SUB = 5;   // rows sampled inside each pixel row; across a row the coverage is exact
			var acc = new double[px1 - px0 + 2];
			var cross = new List<double>();
			for (int py = py0; py <= py1; py++) {
				Array.Clear(acc);
				for (int sIdx = 0; sIdx < SUB; sIdx++) {
					double yc = py + (sIdx + 0.5) / SUB;
					cross.Clear();
					for (int i = 0; i < n; i++) {
						int j = (i + 1) % n;
						double ya = ys[i], yb = ys[j];
						if ((ya <= yc && yb > yc) || (yb <= yc && ya > yc))
							cross.Add(xs[i] + (xs[j] - xs[i]) * (yc - ya) / (yb - ya));
					}
					cross.Sort();
					for (int k = 0; k + 1 < cross.Count; k += 2) {
						double xl = Math.Max(cross[k], px0), xr = Math.Min(cross[k + 1], px1 + 1);
						if (xr <= xl) continue;
						int il = (int)Math.Floor(xl), ir = (int)Math.Floor(xr);
						if (il == ir) { acc[il - px0] += (xr - xl) / SUB; continue; }
						acc[il - px0] += (il + 1 - xl) / SUB;
						for (int xx = il + 1; xx < ir; xx++) acc[xx - px0] += 1.0 / SUB;
						if (ir <= px1) acc[ir - px0] += (xr - ir) / SUB;
					}
				}
				RowColour(st, py + 0.5, out double cr, out double cg, out double cb, out double ca);
				int o = (py * _w + px0) * 4;
				for (int px = px0; px <= px1; px++, o += 4) {
					double cov = Math.Min(1, acc[px - px0]);
					if (cov > 0) BlendPixel(o, cr, cg, cb, ca, cov, st.Mode);
				}
			}
			MarkDirty(px0, py0, px1, py1);
		}

		/// <summary>Give every fully transparent pixel next to a visible one the colour of its visible neighbours
		/// (it stays transparent). A canvas drawn scaled or between pixels then blends its edges into the right
		/// colour instead of a dark fringe.</summary>
		public void ExtendEdges() {
			EnsureBuf();
			int x0 = 0, y0 = 0, x1 = _w - 1, y1 = _h - 1;
			if (_dirty) { x0 = Math.Max(0, _dx0 - 1); y0 = Math.Max(0, _dy0 - 1); x1 = Math.Min(_w - 1, _dx1 + 1); y1 = Math.Min(_h - 1, _dy1 + 1); }
			int bx0 = _w, by0 = _h, bx1 = -1, by1 = -1;
			for (int y = y0; y <= y1; y++) {
				for (int x = x0; x <= x1; x++) {
					int o = (y * _w + x) * 4;
					if (_buf[o + 3] != 0) continue;
					double sr = 0, sg = 0, sb = 0, sw = 0;
					for (int ny = Math.Max(0, y - 1); ny <= Math.Min(_h - 1, y + 1); ny++) {
						for (int nx = Math.Max(0, x - 1); nx <= Math.Min(_w - 1, x + 1); nx++) {
							int no = (ny * _w + nx) * 4;
							int na = _buf[no + 3];
							if (na == 0) continue;
							sr += _buf[no] * na; sg += _buf[no + 1] * na; sb += _buf[no + 2] * na; sw += na;
						}
					}
					if (sw <= 0) continue;
					_buf[o] = (byte)Math.Round(sr / sw); _buf[o + 1] = (byte)Math.Round(sg / sw); _buf[o + 2] = (byte)Math.Round(sb / sw);
					if (x < bx0) bx0 = x; if (x > bx1) bx1 = x; if (y < by0) by0 = y; if (y > by1) by1 = y;
				}
			}
			if (bx1 >= 0) MarkDirty(bx0, by0, bx1, by1);
		}
		#endregion

		#region Dispose
		protected override void Dispose(bool disposing) {
			if (!_disposedValue) {
				_buf = Array.Empty<byte>();
				_released = false;
			}
			base.Dispose(disposing);
		}
		#endregion
	}

	public class LuaCanvasFunc {
		private HashSet<LuaTexture> Canvases;

		public LuaCanvasFunc(HashSet<LuaTexture> canvases) {
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
