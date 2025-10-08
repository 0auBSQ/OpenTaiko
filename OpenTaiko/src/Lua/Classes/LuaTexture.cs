using FDK;

namespace OpenTaiko {
	public class LuaTexture : IDisposable {
		internal CTexture? _texture = null;
		internal HashSet<LuaTexture>? _disposeList = null;
		public uint Pointer => _texture != null ? _texture.Pointer : 0;

		public LuaTexture() {
			_texture = null;
		}
		public LuaTexture(CTexture texture) {
			_texture = texture;
			SetWrapMode("Repeat");
		}


		#region Drawing
		public void Draw(int x, int y) {
			_texture?.t2D描画(x, y);
		}
		public void DrawRect(int x, int y, int rect_x, int rect_y, int rect_width, int rect_height) {
			_texture?.t2D描画(x, y, new System.Drawing.RectangleF(rect_x, rect_y, rect_width, rect_height));
		}
		public void DrawAtAnchor(int x, int y, string anchor) {
			DrawRectAtAnchor(x, y, 0, 0, Width, Height, anchor);
		}
		public void DrawRectAtAnchor(int x, int y, int rect_x, int rect_y, int rect_width, int rect_height, string anchor) {
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

			_texture?.t2D拡大率考慮描画(ref_anchor, x, y, new(rect_x, rect_y, rect_width, rect_height));
		}
		#endregion
		#region Gets
		public bool Loaded => _texture != null;
		public int Height => _texture?.szTextureSize.Height ?? -1;
		public int Width => _texture?.szTextureSize.Width ?? -1;
		public LuaVector2 GetScale() {
			return _texture != null ? new LuaVector2(_texture.vcScaleRatio.X, _texture.vcScaleRatio.Y) : new LuaVector2(0, 0);
		}
		public float GetOpacity() {
			return _texture?.Opacity / (float)255 ?? -1;
		}
		public (float Red, float Green, float Blue) GetColor() {
			return _texture != null ? new(_texture.color4.Red, _texture.color4.Green, _texture.color4.Blue) : new(0, 0, 0);
		}
		public float GetRotation() {
			return (float)((_texture?.fZRotation * 180 / Math.PI) ?? 0);
		}
		public string GetBlendMode() {
			return (_texture?.blendType ?? null) switch {
				BlendType.Normal => "Normal",
				BlendType.Add => "Add",
				BlendType.Multi => "Multi",
				BlendType.Sub => "Sub",
				BlendType.Screen => "Screen",
				_ => "???"
			};
		}
		public string GetWrapMode() {
			return (_texture?.WrapMode ?? null) switch {
				Silk.NET.OpenGLES.TextureWrapMode.ClampToEdge => "Edge",
				Silk.NET.OpenGLES.TextureWrapMode.ClampToBorder => "Border",
				Silk.NET.OpenGLES.TextureWrapMode.Repeat => "Repeat",
				Silk.NET.OpenGLES.TextureWrapMode.MirroredRepeat => "Mirror",
				_ => "???"
			};
		}
		#endregion
		#region Sets
		public void SetScale(float scale_x, float scale_y) {
			_texture?.tSetScale(scale_x, scale_y);
		}
		public void SetOpacity(float opacity) {
			_texture?.tUpdateOpacity((int)(opacity * 255));
		}
		public void SetColor(LuaColor color) {
			float toFloat(byte i) { return i / 255.0f; }
			_texture?.tUpdateColor4(new(toFloat(color.R), toFloat(color.G), toFloat(color.B), 1f));
		}
		public void SetColor(float red, float green, float blue) {
			_texture?.tUpdateColor4(new(red, green, blue, 1f));
		}
		public void SetRotation(float angle) {
			if (_texture != null) _texture.fZ軸中心回転 = (float)(angle * Math.PI / 180);
		}
		public void SetBlendMode(string mode) {
			if (_texture != null) {
				switch (mode.ToLower()) {
					case "add": _texture.blendType = BlendType.Add; break;
					case "multi": _texture.blendType = BlendType.Multi; break;
					case "sub": _texture.blendType = BlendType.Sub; break;
					case "screen": _texture.blendType = BlendType.Screen; break;
					case "normal": _texture.blendType = BlendType.Normal; break;
				}
			}
		}
		public void SetWrapMode(string mode) {
			if (_texture != null) {
				switch (mode.ToLower()) {
					case "edge": _texture.WrapMode = Silk.NET.OpenGLES.TextureWrapMode.ClampToEdge; break;
					case "border": _texture.WrapMode = Silk.NET.OpenGLES.TextureWrapMode.ClampToBorder; break;
					case "repeat": _texture.WrapMode = Silk.NET.OpenGLES.TextureWrapMode.Repeat; break;
					case "mirror": _texture.WrapMode = Silk.NET.OpenGLES.TextureWrapMode.MirroredRepeat; break;
				};
			}
		}
		#endregion
		#region Dispose
		private bool _disposedValue;
		protected virtual void Dispose(bool disposing) {
			if (!_disposedValue) {
				OpenTaiko.tDisposeSafely(ref _texture);
				_disposeList?.Remove(this);
				_disposedValue = true;
			}
		}
		public void Dispose() {
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
		#endregion
	}
	public class LuaTextureFunc {
		private HashSet<LuaTexture> Textures;
		private string DirPath;

		public LuaTextureFunc(HashSet<LuaTexture> textures, string dirPath) {
			Textures = textures;
			DirPath = dirPath;
		}

		internal LuaTexture CreateTexture(string path, bool autoDispose)
			=> CreateTextureFromAbsolutePath($@"{DirPath}{Path.DirectorySeparatorChar}{path}", autoDispose);
		public LuaTexture CreateTexture(string path) => CreateTexture(path, autoDispose: true);

		internal LuaTexture CreateTextureFromAbsolutePath(string path, bool autoDispose) {
			string full_path = $@"{path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar)}";

			LuaTexture luatex = new();
			if (File.Exists(full_path)) {
				try {
					var tex = OpenTaiko.tテクスチャの生成(full_path);
					luatex = new LuaTexture(tex);
					Textures.Add(luatex);
					if (autoDispose)
						luatex._disposeList = this.Textures;
				} catch (Exception e) {
					LogNotification.PopWarning($"Lua Texture failed to load: {e}");
					luatex?.Dispose();
					luatex = new();
				}
			} else {
				LogNotification.PopWarning($"Lua Texture failed to load because the file located at '{full_path}' does not exist.");
			}
			return luatex;
		}
		public LuaTexture CreateTextureFromAbsolutePath(string path) => CreateTextureFromAbsolutePath(path, autoDispose: true);

		public bool Exists(string path) {
			return File.Exists($@"{DirPath}{Path.DirectorySeparatorChar}{path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar)}");
		}
	}
}
