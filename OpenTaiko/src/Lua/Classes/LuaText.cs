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
		internal Dictionary<TitleTextureKey, LuaTexture> _titles = [];
		internal HashSet<LuaText>? _disposeList = null;

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

		// ── DIRECT text drawing (the real fix for per-frame-changing strings) ────────────────────────
		// DrawDirect renders strings from a PER-CHARACTER glyph cache: each distinct character is
		// rasterized once (white on a black edge) and then drawn as a tinted quad per use. A chronometer
		// that changes every frame costs ~12 cached glyphs TOTAL instead of one new texture per frame —
		// bounded memory, no re-rasterization, no upload churn.
		private readonly Dictionary<char, LuaTexture> _glyphs = [];

		private LuaTexture? Glyph(char ch) {
			if (_fontRenderer == null) return null;
			if (_glyphs.TryGetValue(ch, out var g)) return g;
			TitleTextureKey key = new(ch.ToString(), _fontRenderer, Color.White, Color.Black, 9999);
			g = new(TitleTextureKey.ResolveTitleTexture(key, false, false));
			_glyphs[ch] = g;
			return g;
		}

		/// <summary>Draws <paramref name="text"/> at (x, y) tinted (r,g,b 0-255), glyph by glyph from a
		/// bounded per-character cache. Use this for text that CHANGES every frame (timers, counters,
		/// scores) — unlike GetText it never caches whole strings. <paramref name="spacing"/> tightens or
		/// widens tracking (px, default -6 ≈ compensates the glyph edge padding). Returns the end x.</summary>
		public double DrawDirect(string text, double x, double y, int r = 255, int g = 255, int b = 255, double opacity = 1.0, double spacing = -6) {
			if (string.IsNullOrEmpty(text) || _fontRenderer == null) return x;
			foreach (char ch in text) {
				if (ch == ' ') { var zero = Glyph('0'); x += (zero != null ? zero.Width * 0.55 : 12); continue; }
				var gl = Glyph(ch);
				if (gl == null) continue;
				gl.SetColor(r / 255f, g / 255f, b / 255f);
				gl.SetOpacity((float)opacity);
				gl.Draw((int)x, (int)y);
				gl.SetColor(1f, 1f, 1f); gl.SetOpacity(1f);
				x += gl.Width + spacing;
			}
			return x;
		}

		public LuaTexture GetText(string text, bool centered = false, int max_width = 99999, LuaColor? forecolor = null, LuaColor? backcolor = null) {
			if (_fontRenderer == null) return new();
			TitleTextureKey key = new(text, _fontRenderer,
				Color.FromArgb(forecolor?.A ?? 0xFF, forecolor?.R ?? 0xFF, forecolor?.G ?? 0xFF, forecolor?.B ?? 0xFF),
				Color.FromArgb(backcolor?.A ?? 0xFF, backcolor?.R ?? 0x00, backcolor?.G ?? 0x00, backcolor?.B ?? 0x00), max_width);

			if (!_titles.TryGetValue(key, out var tex)) {
				tex = new(TitleTextureKey.ResolveTitleTexture(key, false, centered));
				_titles.Add(key, tex);
			}
			return tex;
		}
		public LuaTexture GetVerticalText(string text, bool centered = false, int max_height = 99999, LuaColor? forecolor = null, LuaColor? backcolor = null) {
			if (_fontRenderer == null) return new();
			TitleTextureKey key = new(text, _fontRenderer,
				Color.FromArgb(forecolor?.A ?? 0xFF, forecolor?.R ?? 0xFF, forecolor?.G ?? 0xFF, forecolor?.B ?? 0xFF),
				Color.FromArgb(backcolor?.A ?? 0xFF, backcolor?.R ?? 0x00, backcolor?.G ?? 0x00, backcolor?.B ?? 0x00), max_height);

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
				foreach (var gl in _glyphs.Values) gl.Dispose();
				_glyphs.Clear();

				_titles.Clear();
				_fontRenderer?.Dispose();
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
	public class LuaTextFunc {
		public HashSet<LuaText> Texts;
		public HashSet<LuaGlyphText> GlyphTexts;
		public string DirPath;

		public LuaTextFunc(HashSet<LuaText> texts, HashSet<LuaGlyphText> glyphTexts, string dirPath) {
			Texts = texts;
			GlyphTexts = glyphTexts;
			DirPath = dirPath;
		}

		internal LuaText Create(int size, string[] style, bool autoDispose) {
			LuaText text = new();

			try {
				text = new(true, size, style);
				Texts.Add(text);
				if (autoDispose)
					text._disposeList = this.Texts;
			}
			catch (Exception e) {
				LogNotification.PopError($"Lua Text failed to load: {e}");
				OpenTaiko.tDisposeSafely(ref text);
				text = new();
			}

			return text;
		}


		public LuaText Create(int size, params string[] style) => Create(size, style, autoDispose: true);

		// glyph-composed text (bounded per-character cache); see LuaGlyphText
		public LuaGlyphText CreateGlyphCached(int size, params string[] style) {
			LuaGlyphText text = new();
			try {
				text = new(size, style);
				GlyphTexts.Add(text);
				text._disposeList = this.GlyphTexts;
			} catch (Exception e) {
				LogNotification.PopError($"Lua GlyphText failed to load: {e}");
				OpenTaiko.tDisposeSafely(ref text);
				text = new();
			}
			return text;
		}
	}
}
