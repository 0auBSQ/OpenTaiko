using System.Diagnostics;
using FDK;

namespace OpenTaiko {
	public class LuaTexture : IDisposable {
		internal CTexture? _texture = null;
		internal HashSet<LuaTexture>? _disposeList = null;
		public uint Pointer => _texture != null ? _texture.Pointer : 0;

		// One-time GPU→CPU readback cache (a texture's pixels rarely change). Consumers
		// that composite this texture on the CPU (LuaCanvas paste, Lua3DScene register)
		// use this so a repeated paste/stamp does not hammer glReadPixels every call.
		private byte[]? _pixCache;
		private int _pcW, _pcH;
		internal byte[]? GetCachedPixels(out int w, out int h) {
			if (_pixCache != null) { w = _pcW; h = _pcH; return _pixCache; }
			_pixCache = _texture?.ReadPixelsRGBA(out _pcW, out _pcH);
			w = _pcW; h = _pcH;
			return _pixCache;
		}

		public LuaTexture() {
			_texture = null;
		}
		public LuaTexture(CTexture texture) {
			_texture = texture;
			SetWrapMode("Repeat");
		}


		#region Drawing
		public void Draw(int x, int y) {
			_texture?.t2DDraw(x, y);
		}
		public void DrawRect(int x, int y, int rect_x, int rect_y, int rect_width, int rect_height) {
			_texture?.t2DDraw(x, y, new System.Drawing.RectangleF(rect_x, rect_y, rect_width, rect_height));
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

			_texture?.t2DScaledDraw(ref_anchor, x, y, new(rect_x, rect_y, rect_width, rect_height));
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
			if (_texture != null) _texture.fZAxisCenterRotate = (float)(angle * Math.PI / 180);
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
		public void SetUseNoiseEffect(bool useNoiseEffect) {
			if (_texture != null) _texture.bUseNoiseEffect = useNoiseEffect;
		}
		/// <summary>Permanently assigns a gradient map to this texture instance.</summary>
		public void SetGradientMap(LuaGradientMap gm) {
			if (_texture != null) _texture.SetGradientMap(gm._gradientMap?.TextureId ?? 0, gm.BlendStrength);
		}
		/// <summary>Removes the per-instance gradient map.</summary>
		public void ClearGradientMap() => _texture?.ClearGradientMap();
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

		public LuaTexture CreateTexture() => new();
		internal LuaTexture CreateTexture(string path, bool autoDispose)
			=> CreateTextureFromAbsolutePath($@"{DirPath}{Path.DirectorySeparatorChar}{path}", autoDispose);

		// Default: load ASYNCHRONOUSLY — the texture is blank (draws no-op) until the background decode + GL
		// upload finish, so a runtime load never freezes the render thread (it pops in when ready).
		public LuaTexture CreateTexture(string path) {
			bool prev = CTexture.AsyncLoad;
			CTexture.AsyncLoad = true;
			try { return CreateTexture(path, autoDispose: true); }
			finally { CTexture.AsyncLoad = prev; }
		}

		// Load synchronously (decode + GL upload inline), even during a load phase. Use when the pixels/size are
		// needed immediately — e.g. SCENE3D:RegisterSpriteFromTexture (GPU readback) — since an async texture has
		// no pixels yet (and would register an empty sprite).
		public LuaTexture CreateTextureSync(string path) {
			bool prev = CTexture.SyncForce;
			CTexture.SyncForce = true;
			try { return CreateTexture(path, autoDispose: true); }
			finally { CTexture.SyncForce = prev; }
		}

		internal LuaTexture CreateTextureFromAbsolutePath(string path, bool autoDispose) {
			string full_path = $@"{path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar)}";

			LuaTexture luatex = new();
			if (File.Exists(full_path)) {
				try {
					var tex = OpenTaiko.tTextureCreate(full_path);
					luatex = new LuaTexture(tex);
					Textures.Add(luatex);
					if (autoDispose)
						luatex._disposeList = this.Textures;
				} catch (Exception e) {
					LogNotification.PopWarning($"Lua Texture failed to load: {e}");
					luatex?.Dispose();
					luatex = new();
				}
			} else if (Path.Exists(full_path)) {
				Trace.TraceWarning($"Lua Texture: '{full_path}' exists but is not a file.");
			} else {
				Trace.TraceWarning($"Lua Texture: '{full_path}' not found, returning empty texture.");
			}
			return luatex;
		}
		public LuaTexture CreateTextureFromAbsolutePath(string path) => CreateTextureFromAbsolutePath(path, autoDispose: true);

		public bool Exists(string path) {
			return File.Exists($@"{DirPath}{Path.DirectorySeparatorChar}{path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar)}");
		}
	}
}
