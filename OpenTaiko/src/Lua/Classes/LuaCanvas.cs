using FDK;

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

		private byte[] _buf;          // RGBA, top-left origin
		private readonly int _w;
		private readonly int _h;
		private bool _dirty;

		public uint Pointer => _texture != null ? _texture.Pointer : 0;
		public int Width => _w;
		public int Height => _h;

		public LuaCanvas(int width, int height) {
			_w = Math.Max(1, width);
			_h = Math.Max(1, height);
			_buf = new byte[_w * _h * 4];          // all zero → fully transparent
			_texture = new CTexture(_w, _h);
			_texture.tUpdateColor4(new(1f, 1f, 1f, 1f));  // no tint
			_texture.tUpdateOpacity(255);
			_dirty = true;
			Upload();                              // allocate the GL texture up front
		}

		#region Pixel editing
		/// <summary>Set one pixel. r,g,b,a are 0-255. Out-of-range coords are ignored.</summary>
		public void SetPixel(int x, int y, int r, int g, int b, int a) {
			if ((uint)x >= (uint)_w || (uint)y >= (uint)_h) return;
			int o = (y * _w + x) * 4;
			_buf[o] = (byte)r; _buf[o + 1] = (byte)g; _buf[o + 2] = (byte)b; _buf[o + 3] = (byte)a;
			_dirty = true;
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
			_dirty = true;
		}

		/// <summary>Fill the whole canvas with a colour (r,g,b,a 0-255).</summary>
		public void Clear(int r, int g, int b, int a) => FillRect(0, 0, _w, _h, r, g, b, a);

		/// <summary>Reset the whole canvas to transparent.</summary>
		public void ClearTransparent() {
			Array.Clear(_buf, 0, _buf.Length);
			_dirty = true;
		}

		/// <summary>Push pending pixel edits to the GPU. No-op if nothing changed.</summary>
		public void Upload() {
			if (_dirty && _texture != null) {
				_texture.UpdatePixelBuffer(_buf, _w, _h);
				_dirty = false;
			}
		}
		#endregion

		#region Drawing (mirrors LuaTexture)
		public void Draw(int x, int y) {
			_texture?.t2D描画(x, y);
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
			_texture?.t2D拡大率考慮描画(ref_anchor, x, y, new(0, 0, _w, _h));
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
