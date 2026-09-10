using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using FDK;

namespace OpenTaiko {
	// Glyph-composed text: caches textures per unique character and composes strings at draw time, instead
	// of caching one texture per string (LuaText.GetText) which grows without bound. Post-processing applies
	// per letter: the maxWidth squish scales every glyph individually. Glyph bitmaps share GetText's geometry
	// (25px left/right/bottom padding, ink top at y=0), so a glyph drawn at (x + pen, y) lands exactly where
	// the equivalent string texture drawn at (x, y) would put that character.
	//
	// Each character bakes a white fill (tinted with the fore colour at draw) and, on first use with a visible
	// outline, a white edge stroke (tinted with the outline colour). Gradient fills bake their colours in.
	// A draw call places every glyph, then draws all the edges and all the fills on top, like the string
	// renderer does for a token: a letter's outline never bites into its neighbour's ink.
	public class LuaGlyphText : IDisposable {
		private CCachedFontRenderer? _font;
		internal HashSet<LuaGlyphText>? _disposeList = null;

		private readonly record struct FillKey(int CodePoint, int GradTopArgb, int GradBottomArgb);
		private readonly Dictionary<FillKey, LuaTexture?> _fills = [];   // null = whitespace or a failed bake
		private readonly Dictionary<int, LuaTexture?> _edges = [];
		private readonly Dictionary<int, double> _advances = [];

		private struct Placement {
			public LuaTexture? Fill, Edge;
			public double X, Y;          // glyph box origin before pixel snapping
			public Color Fore, Outline;
		}
		private readonly List<Placement> _placements = [];

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

		private LuaTexture? Bake(string s, bool edgeOnly) {
			if (_font == null || string.IsNullOrWhiteSpace(s)) return null;
			// base cast: render directly, skipping CCachedFontRenderer's FIFO (it would hold a duplicate copy).
			// A failed bake (e.g. the font resource was swapped by a language change) degrades to an
			// advance-only glyph instead of aborting the caller's whole draw pass.
			try {
				using var bmp = edgeOnly
					? ((CFontRenderer)_font).DrawTextEdgeOnly(s, Color.White, 30)
					: ((CFontRenderer)_font).DrawText(s, Color.White, false);
				var tex = new LuaTexture(OpenTaiko.tTextureCreate(bmp, false));
				if (tex._texture == null) return null;
				Interlocked.Increment(ref LiveGlyphs);
				return tex;
			} catch (Exception e) {
				Trace.TraceWarning($"LuaGlyphText: glyph bake failed for '{s}': {e.Message}");
				return null;
			}
		}

		private LuaTexture? GetFill(int cp, Color? gradTop, Color? gradBottom) {
			bool grad = gradTop.HasValue && gradBottom.HasValue;
			var key = new FillKey(cp, grad ? gradTop.Value.ToArgb() : 0, grad ? gradBottom.Value.ToArgb() : 0);
			if (_fills.TryGetValue(key, out var tex)) return tex;
			string s = char.ConvertFromUtf32(cp);
			// The text renderer only paints a vertical gradient for a token wrapped in a <g.#top.#bottom> tag
			// (the DrawMode.Gradation flag alone is ignored), so a gradient glyph bakes as that tagged token.
			string bake = grad
				? $"<g.#{gradTop.Value.R:X2}{gradTop.Value.G:X2}{gradTop.Value.B:X2}.#{gradBottom.Value.R:X2}{gradBottom.Value.G:X2}{gradBottom.Value.B:X2}>{s}</g>"
				: s;
			tex = Bake(bake, false);
			_fills[key] = tex;
			return tex;
		}

		private LuaTexture? GetEdge(int cp) {
			if (_edges.TryGetValue(cp, out var tex)) return tex;
			tex = Bake(char.ConvertFromUtf32(cp), true);
			_edges[cp] = tex;
			return tex;
		}

		private void Place(int cp, double gx, double gy, Color fore, Color outline, Color? gradTop, Color? gradBottom) {
			var fill = GetFill(cp, gradTop, gradBottom);
			var edge = outline.A > 0 ? GetEdge(cp) : null;
			if (fill == null && edge == null) return;
			_placements.Add(new Placement { Fill = fill, Edge = edge, X = gx, Y = gy, Fore = fore, Outline = outline });
		}

		// ── drawing ─────────────────────────────────────────────────────────────────

		private static (double ax, double ay) AnchorFractions(string anchor) => (anchor ?? "topleft").ToLower() switch {
			"top" => (0.5, 0), "topright" => (1, 0),
			"left" => (0, 0.5), "center" => (0.5, 0.5), "right" => (1, 0.5),
			"bottomleft" => (0, 1), "bottom" => (0.5, 1), "bottomright" => (1, 1),
			_ => (0, 0),
		};

		// Vertical clip band (screen-space y) applied to subsequent Draw calls: glyphs outside are
		// culled, edge glyphs are sliced pixel-exact via a source-rect draw (upright draws; rotated
		// text ignores the band). Scrolling lists (PopUI menus) set this so text clips to the viewport
		// like the row graphics do. Pass y1 <= y0 to clear.
		private double _clipY0 = double.NegativeInfinity, _clipY1 = double.PositiveInfinity;
		public void SetClipY(double y0, double y1) {
			if (y1 <= y0) { _clipY0 = double.NegativeInfinity; _clipY1 = double.PositiveInfinity; } else { _clipY0 = y0; _clipY1 = y1; }
		}

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

			for (int li = 0; li < lines.Count; li++) {
				var line = lines[li];
				double pen = 0;
				double lineY = startY + li * LineHeight * sy;
				for (int i = line.Start; i < line.Start + line.Count; i++) {
					int cp = runes[i].CodePoint;
					int styleId = runes[i].StyleId;
					Color gFore = fore, gOutline = outline;
					Color? gGradTop = null, gGradBottom = null;
					if (styleId >= 0) {
						var st = styles[styleId];
						if (!st.Fore.IsEmpty) gFore = st.Fore;
						if (st.Outline != null) gOutline = st.Outline.Value;
						gGradTop = st.GradTop; gGradBottom = st.GradBottom;
					}
					Place(cp, startX + pen * f * scale, lineY, gFore, gOutline, gGradTop, gGradBottom);
					pen += AdvanceOf(cp);
				}
			}
			Compose(opacity, (float)(f * scale), (float)sy, clip: true, rotationDeg, x, y);
			return startX + boxW;
		}

		// Draws the placed glyphs: every edge first, then every fill on top. An upright glyph snaps to the
		// nearest pixel of its exact pen position, which is how Skia places the glyphs of a drawn string
		// (the engine's texture draw truncates, so the snap happens here). Rotated text keeps exact positions
		// and ignores the clip band.
		private void Compose(double opacity, float sx, float sy, bool clip, double rotationDeg, double ox, double oy) {
			bool rot = rotationDeg != 0;
			double rad = rotationDeg * Math.PI / 180.0;
			double cosR = Math.Cos(rad), sinR = Math.Sin(rad);
			bool anyEdge = false;
			foreach (var p in _placements) if (p.Edge != null) { anyEdge = true; break; }

			for (int pass = anyEdge ? 0 : 1; pass < 2; pass++) {
				foreach (var p in _placements) {
					var tex = pass == 0 ? p.Edge : p.Fill;
					if (tex == null) continue;
					Color tint = pass == 0 ? p.Outline : p.Fore;
					tex.SetScale(sx, sy);
					tex.SetColor(tint.R / 255f, tint.G / 255f, tint.B / 255f);
					tex.SetOpacity((float)(pass == 0 ? opacity * tint.A / 255.0 : opacity));
					if (rot) {
						// rotate this glyph's centre about the anchor, then spin the glyph to match.
						// SetRotation is CCW on screen, so the position rotation is the matching CCW form
						// (y-down screen); otherwise the glyphs and their placement disagree.
						double gw = tex.Width * sx;
						double gh = tex.Height * sy;
						double relx = p.X + gw / 2 - ox, rely = p.Y + gh / 2 - oy;
						double rcx = ox + relx * cosR + rely * sinR;
						double rcy = oy - relx * sinR + rely * cosR;
						tex.SetRotation((float)rotationDeg);
						tex.Draw(rcx - gw / 2, rcy - gh / 2);
						tex.SetRotation(0);
					} else {
						double gx = Math.Floor(p.X + 0.5);
						double top = Math.Floor(p.Y);
						double bot = top + tex.Height * sy;
						if (clip && (bot <= _clipY0 || top >= _clipY1)) {
							// fully outside the clip band: culled
						} else if (!clip || (top >= _clipY0 && bot <= _clipY1)) {
							tex.Draw(gx, top);
						} else {
							// edge glyph: slice the visible band via a source rect (exact at sy=1,
							// the menu/list case; scaled draws slice in source pixels)
							double v0 = Math.Max(top, _clipY0), v1 = Math.Min(bot, _clipY1);
							int srcY = (int)Math.Floor((v0 - top) / sy);
							int srcH = (int)Math.Ceiling((v1 - v0) / sy);
							if (srcH > 0) tex.DrawRect(gx, Math.Floor(v0), 0, srcY, tex.Width, srcH);
						}
					}
					tex.SetScale(1, 1);
					tex.SetColor(1, 1, 1);
					tex.SetOpacity(1);
				}
			}
			_placements.Clear();
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
					int styleId = runes[i].StyleId;
					Color gFore = fore, gOutline = outline;
					Color? gGradTop = null, gGradBottom = null;
					if (styleId >= 0) {
						var st = styles[styleId];
						if (!st.Fore.IsEmpty) gFore = st.Fore;
						if (st.Outline != null) gOutline = st.Outline.Value;
						gGradTop = st.GradTop; gGradBottom = st.GradBottom;
					}
					Place(cp, x + pen * scale, lineY, gFore, gOutline, gGradTop, gGradBottom);
					pen += AdvanceOf(cp);
				}
			}
			Compose(opacity, (float)scale, (float)scale, clip: false, 0, x, y);
			return (lines.Count - 1) * pitch + BoxHeight * scale;
		}

		#region Dispose
		private bool _disposedValue;
		// Live baked-glyph gauge for the [MEMTRACE] debug line (attributes CTexture growth to glyphs vs images).
		public static int LiveGlyphs;

		protected virtual void Dispose(bool disposing) {
			if (!_disposedValue) {
				foreach (var tex in _fills.Values) {
					if (tex != null) {
						tex.Dispose();
						Interlocked.Decrement(ref LiveGlyphs);
					}
				}
				foreach (var tex in _edges.Values) {
					if (tex != null) {
						tex.Dispose();
						Interlocked.Decrement(ref LiveGlyphs);
					}
				}
				_fills.Clear();
				_edges.Clear();
				_placements.Clear();
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
