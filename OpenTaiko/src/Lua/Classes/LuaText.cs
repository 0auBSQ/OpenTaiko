using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FDK;
using System.Drawing;
using System.IO;

namespace OpenTaiko {
	public class LuaText : IDisposable {
		private CCachedFontRenderer? _fontRenderer;
		private Dictionary<TitleTextureKey, LuaTexture> _titles = [];

		public LuaText() {
			_fontRenderer = null;
		}
		public LuaText(bool isMainFont, int size, params string[] style) {
			CFontRenderer.FontStyle fontstyle = CFontRenderer.FontStyle.Regular;
			foreach (string input in style) {
				fontstyle |= input.ToLower() switch {
					"bold" => CFontRenderer.FontStyle.Bold,
					"italic" => CFontRenderer.FontStyle.Italic,
					"underline" => CFontRenderer.FontStyle.Underline,
					"strikeout" => CFontRenderer.FontStyle.Strikeout,
					_ => CFontRenderer.FontStyle.Regular
				};
			}
			if (isMainFont) {
				_fontRenderer = HPrivateFastFont.tInstantiateMainFont(size, fontstyle);
			}
			else {
				_fontRenderer = HPrivateFastFont.tInstantiateBoxFont(size, fontstyle);
			}
		}
		public LuaText(string name, int size, params string[] style) {
			CFontRenderer.FontStyle fontstyle = CFontRenderer.FontStyle.Regular;
			foreach (string input in style) {
				fontstyle |= input.ToLower() switch {
					"bold" => CFontRenderer.FontStyle.Bold,
					"italic" => CFontRenderer.FontStyle.Italic,
					"underline" => CFontRenderer.FontStyle.Underline,
					"strikeout" => CFontRenderer.FontStyle.Strikeout,
					_ => CFontRenderer.FontStyle.Regular
				};
			}
			_fontRenderer = HPrivateFastFont.tInstantiateFont(name, size, fontstyle);
		}

		public LuaTexture GetText(string text, bool centered = false, int max_width = 99999, System.Drawing.Color? forecolor = null, System.Drawing.Color? backcolor = null) {
			if (_fontRenderer == null) return new();
			TitleTextureKey key = new(text, _fontRenderer, forecolor ?? Color.White, backcolor ?? Color.Black, max_width);

			if (!_titles.TryGetValue(key, out var tex)) {
				tex = new(TitleTextureKey.ResolveTitleTexture(key, false, centered));
				_titles.Add(key, tex);
			}
			return tex;
		}
		public LuaTexture GetVerticalText(string text, bool centered = false, int max_height = 99999, System.Drawing.Color? forecolor = null, System.Drawing.Color? backcolor = null) {
			if (_fontRenderer == null) return new();
			TitleTextureKey key = new(text, _fontRenderer, forecolor ?? Color.White, backcolor ?? Color.Black, max_height);

			if (!_titles.TryGetValue(key, out var tex)) {
				tex = new(TitleTextureKey.ResolveTitleTexture(key, true, centered));
				_titles.Add(key, tex);
			}
			return tex;
		}

		#region Dispose
		private bool _disposedValue;

		protected virtual void Dispose(bool disposing) {
			if (!_disposedValue) {

				foreach (var tex in _titles.Values) {
					tex.Dispose();
				}

				_titles.Clear();
				_fontRenderer?.Dispose();
				_disposedValue = true;
			}
		}

		public void Dispose() {
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
		#endregion
	}
	public class LuaTextFunc {
		public List<LuaText> Texts;
		public string DirPath;

		public LuaTextFunc(List<LuaText> texts, string dirPath) {
			Texts = texts;
			DirPath = dirPath;
		}

		public LuaText Create(int size, params string[] style) {
			LuaText text = new();

			try {
				text = new(true, size, style);
				Texts.Add(text);
			}
			catch (Exception e) {
				LogNotification.PopError($"Lua Text failed to load: {e}");
				OpenTaiko.tDisposeSafely(ref text);
				text = new();
			}

			return text;
		}
	}
}
