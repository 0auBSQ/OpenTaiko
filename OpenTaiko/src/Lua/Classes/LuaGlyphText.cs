using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using FDK;

namespace OpenTaiko {
	// Glyph-composed text: caches ONE texture per unique character (per color variant) and composes strings at
	// draw time, instead of caching one texture per string (LuaText.GetText) which grows without bound. Post-
	// processing applies per letter: the maxWidth squish scales every glyph individually. Glyph bitmaps share
	// GetText's geometry (25px left/right/bottom padding, ink top at y=0), so a glyph drawn at (x + pen, y)
	// lands exactly where the equivalent string texture drawn at (x, y) would put that character.
	//
	// Bake strategy: when the outline is black or fully transparent, the fill is baked WHITE and tinted at draw
	// (a black outline stays black under tinting), so any number of fore colors share one glyph. Non-black
	// opaque outlines bake the actual colors and key the cache on them.
	public class LuaGlyphText : IDisposable {
		private CCachedFontRenderer? _font;
		internal HashSet<LuaGlyphText>? _disposeList = null;

		private readonly record struct GlyphKey(int CodePoint, int ForeArgb, int OutlineArgb);
		private sealed class GlyphEntry {
			public LuaTexture? Tex;      // null for whitespace (advance only)
			public double Advance;
			public Color Tint;           // color applied at draw (White when the fore is baked in)
		}
		private readonly Dictionary<GlyphKey, GlyphEntry> _glyphs = [];
		private readonly Dictionary<int, double> _advances = [];

		private static readonly Color DefaultFore = Color.White;
		private static readonly Color DefaultOutline = Color.Black;

		public LuaGlyphText() { _font = null; }
		public LuaGlyphText(int size, params string[] style) {
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
			_font = HPrivateFastFont.tInstantiateMainFont(size, fontstyle);
		}

		// ink line height (= the line pitch of multi-line text)
		public double LineHeight => _font?.GetLineHeight() ?? 0;
		// height of a 1-line GetText texture (ink + bottom padding) — use for layout compatible with GetText
		public double BoxHeight => Math.Ceiling(LineHeight) + CFontRenderer.TextPadding;

		// ── measurement ─────────────────────────────────────────────────────────────

		private double AdvanceOf(int cp) {
			if (_advances.TryGetValue(cp, out double a)) return a;
			try { a = _font?.MeasureText(char.ConvertFromUtf32(cp)) ?? 0; } catch { a = 0; }
			_advances[cp] = a;
			return a;
		}

		// ink advance of the widest \n-line, color tags stripped (a GetText texture is this + 50 wide)
		public double Measure(string text, double scale = 1.0) {
			if (string.IsNullOrEmpty(text) || _font == null) return 0;
			var (runes, _) = GlyphTextLayout.Tokenize(text);
			double best = 0, w = 0;
			foreach (var r in runes) {
				if (r.CodePoint == '\n') { best = Math.Max(best, w); w = 0; continue; }
				w += AdvanceOf(r.CodePoint);
			}
			return Math.Max(best, w) * scale;
		}

		// ── glyph cache ─────────────────────────────────────────────────────────────

		private static bool IsTintable(Color outline)
			=> outline.A == 0 || (outline.R == 0 && outline.G == 0 && outline.B == 0);

		private GlyphEntry GetGlyph(int cp, Color fore, Color outline) {
			bool tintable = IsTintable(outline);
			var key = new GlyphKey(cp, tintable ? Color.White.ToArgb() : fore.ToArgb(), outline.ToArgb());
			if (_glyphs.TryGetValue(key, out var entry)) {
				entry.Tint = tintable ? fore : Color.White;
				return entry;
			}
			entry = new GlyphEntry { Advance = AdvanceOf(cp), Tint = tintable ? fore : Color.White };
			string s = char.ConvertFromUtf32(cp);
			if (!string.IsNullOrWhiteSpace(s) && _font != null) {
				// base cast: render directly, skipping CCachedFontRenderer's FIFO (it would hold a duplicate copy).
				// A failed bake (e.g. the font resource was swapped by a language change) degrades to an
				// advance-only glyph instead of aborting the caller's whole draw pass.
				try {
					using var bmp = ((CFontRenderer)_font).DrawText(s, tintable ? Color.White : fore, outline, null, 30, false);
					entry.Tex = new LuaTexture(OpenTaiko.tTextureCreate(bmp, false));
					if (entry.Tex != null) Interlocked.Increment(ref LiveGlyphs);
				} catch (Exception e) {
					Trace.TraceWarning($"LuaGlyphText: glyph bake failed for U+{cp:X4}: {e.Message}");
					entry.Tex = null;
				}
			}
			_glyphs[key] = entry;
			return entry;
		}

		// ── drawing ─────────────────────────────────────────────────────────────────

		private static (double ax, double ay) AnchorFractions(string anchor) => (anchor ?? "topleft").ToLower() switch {
			"top" => (0.5, 0), "topright" => (1, 0),
			"left" => (0, 0.5), "center" => (0.5, 0.5), "right" => (1, 0.5),
			"bottomleft" => (0, 1), "bottom" => (0.5, 1), "bottomright" => (1, 1),
			_ => (0, 0),
		};

		private static Color FromLua(LuaColor? c, Color fallback)
			=> c == null ? fallback : Color.FromArgb(c.A, c.R, c.G, c.B);

		/// <summary>Draws text composed from cached glyphs. maxWidth squishes each letter horizontally so the
		/// full box (ink + 50px padding, like a GetText texture) fits, matching GetText's squish. anchor uses
		/// the same 9 points as DrawAtAnchor and anchors that box. '\n' stacks lines (left-aligned).
		/// scaleY (when > 0) scales the vertical axis independently — e.g. shrink a tall block into a fixed
		/// box while the squish still makes the width fill it. rotationDeg (when != 0) rotates the whole
		/// composed block rigidly about the anchor point (x,y) — each glyph is repositioned along the rotated
		/// baseline and spun to match. Returns the box's right-edge x.</summary>
		public double Draw(string text, double x, double y, LuaColor? forecolor = null, LuaColor? backcolor = null,
			double opacity = 1.0, double scale = 1.0, double maxWidth = 0, string anchor = "topleft", double scaleY = 0,
			double rotationDeg = 0) {
			if (string.IsNullOrEmpty(text) || _font == null) return x;
			Color fore = FromLua(forecolor, DefaultFore);
			Color outline = FromLua(backcolor, DefaultOutline);
			double sy = scaleY > 0 ? scaleY : scale;

			var (runes, styles) = GlyphTextLayout.Tokenize(text);
			var lines = GlyphTextLayout.Wrap(runes, 0, AdvanceOf);   // '\n' splits only

			double inkW = 0;
			foreach (var line in lines) inkW = Math.Max(inkW, line.Width);
			double pad = CFontRenderer.TextPadding;
			double naturalBoxW = (inkW + 2 * pad) * scale;
			double f = GlyphTextLayout.SquishFactor(naturalBoxW, maxWidth);
			double boxW = naturalBoxW * f;
			double boxH = (lines.Count - 1) * LineHeight * sy + BoxHeight * sy;

			var (ax, ay) = AnchorFractions(anchor);
			double startX = x - ax * boxW;
			double startY = y - ay * boxH;

			bool rot = rotationDeg != 0;
			double rad = rotationDeg * Math.PI / 180.0;
			double cosR = Math.Cos(rad), sinR = Math.Sin(rad);

			for (int li = 0; li < lines.Count; li++) {
				var line = lines[li];
				double pen = 0;
				double lineY = startY + li * LineHeight * sy;
				for (int i = line.Start; i < line.Start + line.Count; i++) {
					int cp = runes[i].CodePoint;
					double adv = AdvanceOf(cp);
					int styleId = runes[i].StyleId;
					Color gFore = fore, gOutline = outline;
					if (styleId >= 0) {
						var st = styles[styleId];
						if (!st.Fore.IsEmpty) gFore = st.Fore;
						if (st.Outline != null) gOutline = st.Outline.Value;
					}
					var glyph = GetGlyph(cp, gFore, gOutline);
					if (glyph.Tex != null) {
						glyph.Tex.SetScale((float)(f * scale), (float)sy);
						glyph.Tex.SetColor(glyph.Tint.R / 255f, glyph.Tint.G / 255f, glyph.Tint.B / 255f);
						glyph.Tex.SetOpacity((float)opacity);
						double gx = startX + pen * f * scale;
						double gy = lineY;
						if (rot) {
							// rotate this glyph's centre about the anchor (x,y), then spin the glyph to match.
							// SetRotation is CCW on screen, so the position rotation is the matching CCW form
							// (y-down screen) — otherwise the glyphs and their placement disagree.
							double gw = glyph.Tex.Width * f * scale;
							double gh = glyph.Tex.Height * sy;
							double relx = gx + gw / 2 - x, rely = gy + gh / 2 - y;
							double rcx = x + relx * cosR + rely * sinR;
							double rcy = y - relx * sinR + rely * cosR;
							glyph.Tex.SetRotation((float)rotationDeg);
							glyph.Tex.Draw(rcx - gw / 2, rcy - gh / 2);   // sub-pixel: rotated text must not snap
							glyph.Tex.SetRotation(0);
						} else {
							glyph.Tex.Draw(Math.Floor(gx), Math.Floor(gy));   // upright: integer-crisp
						}
						glyph.Tex.SetScale(1, 1);
						glyph.Tex.SetColor(1, 1, 1);
						glyph.Tex.SetOpacity(1);
					}
					pen += adv;
				}
			}
			return startX + boxW;
		}

		// ── word-wrapped block ──────────────────────────────────────────────────────

		/// <summary>The wrapped lines as plain strings (color tags stripped) — for typewriter-style callers.</summary>
		public string[] WrapToLines(string text, double wrapWidth, double scale = 1.0) {
			if (string.IsNullOrEmpty(text) || _font == null) return [];
			var (runes, _) = GlyphTextLayout.Tokenize(text);
			var lines = GlyphTextLayout.Wrap(runes, scale > 0 ? wrapWidth / scale : wrapWidth, AdvanceOf);
			var result = new string[lines.Count];
			var sb = new System.Text.StringBuilder();
			for (int li = 0; li < lines.Count; li++) {
				sb.Clear();
				for (int i = lines[li].Start; i < lines[li].Start + lines[li].Count; i++)
					sb.Append(char.ConvertFromUtf32(runes[i].CodePoint));
				result[li] = sb.ToString();
			}
			return result;
		}

		/// <summary>Height DrawWrapped would use, without drawing (for layout).</summary>
		public double MeasureWrapped(string text, double wrapWidth, double scale = 1.0, double lineSpacing = 1.0) {
			if (string.IsNullOrEmpty(text) || _font == null) return 0;
			var (runes, _) = GlyphTextLayout.Tokenize(text);
			var lines = GlyphTextLayout.Wrap(runes, scale > 0 ? wrapWidth / scale : wrapWidth, AdvanceOf);
			return (lines.Count - 1) * LineHeight * scale * lineSpacing + BoxHeight * scale;
		}

		/// <summary>Word-wraps text to wrapWidth (ink width) and draws it left-aligned at (x, y).
		/// Returns the drawn height (same value as MeasureWrapped).</summary>
		public double DrawWrapped(string text, double x, double y, double wrapWidth,
			LuaColor? forecolor = null, LuaColor? backcolor = null,
			double opacity = 1.0, double scale = 1.0, double lineSpacing = 1.0) {
			if (string.IsNullOrEmpty(text) || _font == null) return 0;
			Color fore = FromLua(forecolor, DefaultFore);
			Color outline = FromLua(backcolor, DefaultOutline);

			var (runes, styles) = GlyphTextLayout.Tokenize(text);
			var lines = GlyphTextLayout.Wrap(runes, scale > 0 ? wrapWidth / scale : wrapWidth, AdvanceOf);
			double pitch = LineHeight * scale * lineSpacing;

			for (int li = 0; li < lines.Count; li++) {
				var line = lines[li];
				double pen = 0;
				double lineY = y + li * pitch;
				for (int i = line.Start; i < line.Start + line.Count; i++) {
					int cp = runes[i].CodePoint;
					double adv = AdvanceOf(cp);
					int styleId = runes[i].StyleId;
					Color gFore = fore, gOutline = outline;
					if (styleId >= 0) {
						var st = styles[styleId];
						if (!st.Fore.IsEmpty) gFore = st.Fore;
						if (st.Outline != null) gOutline = st.Outline.Value;
					}
					var glyph = GetGlyph(cp, gFore, gOutline);
					if (glyph.Tex != null) {
						glyph.Tex.SetScale((float)scale, (float)scale);
						glyph.Tex.SetColor(glyph.Tint.R / 255f, glyph.Tint.G / 255f, glyph.Tint.B / 255f);
						glyph.Tex.SetOpacity((float)opacity);
						glyph.Tex.Draw((int)Math.Floor(x + pen * scale), (int)Math.Floor(lineY));
						glyph.Tex.SetScale(1, 1);
						glyph.Tex.SetColor(1, 1, 1);
						glyph.Tex.SetOpacity(1);
					}
					pen += adv;
				}
			}
			return (lines.Count - 1) * pitch + BoxHeight * scale;
		}

		#region Dispose
		private bool _disposedValue;
		// Live baked-glyph gauge for the [MEMTRACE] debug line (attributes CTexture growth to glyphs vs images).
		public static int LiveGlyphs;

		protected virtual void Dispose(bool disposing) {
			if (!_disposedValue) {
				foreach (var g in _glyphs.Values) {
					if (g.Tex != null) {
						g.Tex.Dispose();
						Interlocked.Decrement(ref LiveGlyphs);
					}
				}
				_glyphs.Clear();
				_advances.Clear();
				_font?.Dispose();
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
}
