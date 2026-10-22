using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Text.RegularExpressions;
using SkiaSharp;
using Color = System.Drawing.Color;

namespace FDK;

internal class CSkiaSharpTextRenderer : ITextRenderer {
	//https://monobook.org/wiki/SkiaSharp%E3%81%A7%E6%97%A5%E6%9C%AC%E8%AA%9E%E6%96%87%E5%AD%97%E5%88%97%E3%82%92%E6%8F%8F%E7%94%BB%E3%81%99%E3%82%8B
	public CSkiaSharpTextRenderer(string fontpath, int pt) {
		Initialize(fontpath, pt, CFontRenderer.FontStyle.Regular);
	}

	public CSkiaSharpTextRenderer(string fontpath, int pt, CFontRenderer.FontStyle style) {
		Initialize(fontpath, pt, style);
	}

	public CSkiaSharpTextRenderer(Stream fontstream, string fontKey, int pt, CFontRenderer.FontStyle style) {
		Initialize(fontstream, fontKey, pt, style);
	}

	protected void Initialize(Stream fontstream, string fontKey, int pt, CFontRenderer.FontStyle style) {
		paint = new SKPaint();

		//stream・filepathから生成した場合に、style設定をどうすればいいのかがわからない
		// A stream has no path, so the caller provides the source name as the cache key.
		paint.Typeface = GetCachedTypeface($"stream:{fontKey}", () => SKFontManager.Default.CreateTypeface(fontstream));

		paint.TextSize = (pt * 1.3f);
		paint.IsAntialias = true;
	}

	protected void Initialize(string fontpath, int pt, CFontRenderer.FontStyle style) {
		paint = new SKPaint();

		SKFontStyleWeight weight = SKFontStyleWeight.Normal;
		SKFontStyleWidth width = SKFontStyleWidth.Normal;
		SKFontStyleSlant slant = SKFontStyleSlant.Upright;

		if (style.HasFlag(CFontRenderer.FontStyle.Bold)) {
			weight = SKFontStyleWeight.Bold;
		}
		if (style.HasFlag(CFontRenderer.FontStyle.Italic)) {
			slant = SKFontStyleSlant.Italic;
		}
		if (style.HasFlag(CFontRenderer.FontStyle.Strikeout)) {
			paint.Style = SKPaintStyle.Stroke;
		}
		if (style.HasFlag(CFontRenderer.FontStyle.Underline)) {
			//????
			//paint.FontMetrics.UnderlinePosition;
		}

		// Typefaces are cached and shared (see _typefaceCache): loading one copies the whole
		// font file into native memory, SKPaint does not own/free its Typeface, and renderers
		// are created per stage/HUD element. Resolve into a local rather than reading
		// paint.Typeface back, so paint disposal can never dispose a cached typeface.
		SKTypeface typeface = null;

		if (SKFontManager.Default.FontFamilies.Contains(fontpath))
			typeface = GetCachedTypeface($"family:{fontpath}:{weight}:{width}:{slant}",
				() => SKTypeface.FromFamilyName(fontpath, weight, width, slant));

		//stream・filepathから生成した場合に、style設定をどうすればいいのかがわからない
		if (File.Exists(fontpath)) {
			typeface = FileTypeface(fontpath);
		}

		if (typeface == null) {
			Trace.TraceWarning($"The requested font '{fontpath}' could not be found from a file or font family name. Falling back to SkiaSharp's default typeface.");
			typeface = GetCachedTypeface("default", () => SKTypeface.CreateDefault());
		}

		// If a font couldn't be loaded despite the fallback implemented above, we're absolutely cooked.
		if (typeface == null)
			throw new FileNotFoundException("All attempts to load this font failed, including using SkiaSharp's default typeface as a fallback.", fontpath);

		paint.Typeface = typeface;

		paint.TextSize = (pt * 1.3f);
		paint.IsAntialias = true;
	}

	internal struct SStringToken {
		public string s;
		public Color TextColor;
		public bool UseGradiant;
		public Color GradiantTop;
		public Color GradiantBottom;
		public bool UseOutline;
		public Color OutlineColor;
	}

	// Purify is equivalent to RemoveTags on ObjectExtensions.cs, update both if changing TagRegex

	private const string TagRegex = @"<(/?)([gc](?:\.#[0-9a-fA-F]{6})*?)>";

	public string Purify(string input) {
		return Regex.Replace(input, TagRegex, "");
	}

	// advance width in px (includes trailing spaces) — the same measure DrawText uses to place tokens
	public float MeasureText(string s) {
		float width = 0;
		foreach (var (text, runPaint) in this.Runs(s))
			width += runPaint.MeasureText(text);
		return width;
	}

	// ink line height (ascent + descent) — the line pitch DrawText uses when stacking \n lines
	public float GetLineHeight() {
		return paint.FontMetrics.Descent - paint.FontMetrics.Ascent;
	}

	public List<SStringToken> Tokenize(string input, Color fontColor, Color edgeColor, Color? secondEdgeColor, Color gradationTopColor, Color gradationBottomColor) {
		List<SStringToken> tokens = new List<SStringToken>();
		Stack<string> tags = new Stack<string>();
		Stack<SStringToken> tokenStack = new Stack<SStringToken>();
		int lastPos = 0;

		var tagRegex = new Regex(TagRegex);
		var matches = tagRegex.Matches(input);

		foreach (Match match in matches) {
			int pos = match.Index;
			string text = input.Substring(lastPos, pos - lastPos);

			// First
			if (text.Length > 0) {
				SStringToken token = new SStringToken {
					s = text,
					UseGradiant = tokenStack.Count > 0 && tokenStack.Peek().UseGradiant,
					GradiantTop = (tokenStack.Count == 0) ? gradationTopColor : tokenStack.Peek().GradiantTop,
					GradiantBottom = (tokenStack.Count == 0) ? gradationBottomColor : tokenStack.Peek().GradiantBottom,
					TextColor = (tokenStack.Count == 0) ? fontColor : tokenStack.Peek().TextColor,
					UseOutline = tokenStack.Count > 0 && tokenStack.Peek().UseOutline,
					OutlineColor = (tokenStack.Count == 0) ? edgeColor : tokenStack.Peek().OutlineColor,
				};
				tokens.Add(token);
			}

			lastPos = pos + match.Length;

			if (match.Groups[1].Value == "/") {
				if (tags.Count > 0) tags.Pop();
				if (tokenStack.Count > 0) tokenStack.Pop();
			} else {
				tags.Push(match.Groups[2].Value);
				SStringToken newToken = new SStringToken {
					UseGradiant = tokenStack.Count > 0 ? tokenStack.Peek().UseGradiant : false,
					GradiantTop = (tokenStack.Count == 0) ? gradationTopColor : tokenStack.Peek().GradiantTop,
					GradiantBottom = (tokenStack.Count == 0) ? gradationBottomColor : tokenStack.Peek().GradiantBottom,
					TextColor = (tokenStack.Count == 0) ? fontColor : tokenStack.Peek().TextColor,
					UseOutline = tokenStack.Count > 0 ? tokenStack.Peek().UseOutline : false,
					OutlineColor = (tokenStack.Count == 0) ? edgeColor : tokenStack.Peek().OutlineColor,
				};

				string[] _varSplit = match.Groups[2].Value.Split(".");

				if (_varSplit.Length > 0) {
					switch (_varSplit[0]) {
						case "g": {
								if (_varSplit.Length > 2) {
									newToken.UseGradiant = true;
									newToken.GradiantTop = ColorTranslator.FromHtml(_varSplit[1]);
									newToken.GradiantBottom = ColorTranslator.FromHtml(_varSplit[2]);
								}
								break;
							}
						case "c": {
								if (_varSplit.Length > 1) {
									newToken.TextColor = ColorTranslator.FromHtml(_varSplit[1]);
								}
								if (_varSplit.Length > 2) {
									newToken.UseOutline = true;
									newToken.OutlineColor = ColorTranslator.FromHtml(_varSplit[2]);
								}
								break;
							}

					}
				}

				tokenStack.Push(newToken);
			}
		}

		// Last
		if (lastPos < input.Length) {
			SStringToken token = new SStringToken {
				s = input.Substring(lastPos),
				UseGradiant = tokenStack.Count > 0 && tokenStack.Peek().UseGradiant,
				GradiantTop = (tokenStack.Count == 0) ? gradationTopColor : tokenStack.Peek().GradiantTop,
				GradiantBottom = (tokenStack.Count == 0) ? gradationBottomColor : tokenStack.Peek().GradiantBottom,
				TextColor = (tokenStack.Count == 0) ? fontColor : tokenStack.Peek().TextColor,
				UseOutline = tokenStack.Count > 0 && tokenStack.Peek().UseOutline,
				OutlineColor = (tokenStack.Count == 0) ? edgeColor : tokenStack.Peek().OutlineColor,
			};
			tokens.Add(token);
		}

		return tokens;
	}

	public SKBitmap DrawText(string drawstr, CFontRenderer.DrawMode drawMode, Color fontColor, Color edgeColor, Color? secondEdgeColor, Color gradationTopColor, Color gradationBottomColor, int edge_Ratio, bool keepCenter) {
		if (string.IsNullOrEmpty(drawstr)) {
			//nullか""だったら、1x1を返す
			return new SKBitmap(1, 1);
		}

		string[] strs = drawstr.Split("\n");
		List<SStringToken>[] tokens = new List<SStringToken>[strs.Length];

		for (int i = 0; i < strs.Length; i++) {
			tokens[i] = Tokenize(strs[i], fontColor, edgeColor, secondEdgeColor, gradationTopColor, gradationBottomColor);
		}

		SKBitmap[] images = new SKBitmap[strs.Length];

		for (int i = 0; i < strs.Length; i++) {
			int width = (int)Math.Ceiling(this.MeasureText(Purify(strs[i]))) + 50;
			int height = (int)Math.Ceiling(paint.FontMetrics.Descent - paint.FontMetrics.Ascent) + 50;

			//少し大きめにとる(定数じゃない方法を考えましょう)
			SKBitmap bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
			// Canvas that composites the glyph layers onto the bitmap; native, disposed via using.
			using SKCanvas canvas = new SKCanvas(bitmap);

			int x_offset = 0;

			foreach (SStringToken tok in tokens[i]) {
				// the token in runs of one typeface each (one run unless the font lacks some characters)
				var runs = this.Runs(tok.s);
				float baseline = -paint.FontMetrics.Ascent + 25;
				int token_width = (int)Math.Ceiling(this.MeasureText(tok.s));

				if (drawMode.HasFlag(CFontRenderer.DrawMode.Edge) || tok.UseOutline) {
					// Temporary paints for the outlines; native, disposed via using.
					using SKPaint secondEdgePaint = new SKPaint();
					if (secondEdgeColor != null) {
						secondEdgePaint.StrokeWidth = paint.TextSize * 8 / edge_Ratio;
						secondEdgePaint.StrokeJoin = SKStrokeJoin.Round;
						secondEdgePaint.Color = new SKColor(secondEdgeColor.Value.R, secondEdgeColor.Value.G, secondEdgeColor.Value.B, secondEdgeColor.Value.A);
						secondEdgePaint.Style = SKPaintStyle.Stroke;
						secondEdgePaint.IsAntialias = true;
					}

					using SKPaint edgePaint = new SKPaint();
					edgePaint.StrokeWidth = paint.TextSize * (secondEdgeColor == null ? 8 : 4) / edge_Ratio;
					edgePaint.StrokeJoin = SKStrokeJoin.Round;
					edgePaint.Color = new SKColor(tok.OutlineColor.R, tok.OutlineColor.G, tok.OutlineColor.B, tok.OutlineColor.A);
					edgePaint.Style = SKPaintStyle.Stroke;
					edgePaint.IsAntialias = true;

					float run_x = 25 + x_offset;
					foreach (var (text, runPaint) in runs) {
						// Glyph outline path for the edge stroke; native, disposed via using.
						using SKPath path = runPaint.GetTextPath(text, run_x, baseline);
						if (secondEdgeColor != null)
							canvas.DrawPath(path, secondEdgePaint);
						canvas.DrawPath(path, edgePaint);
						run_x += runPaint.MeasureText(text);
					}
				}

				if (!drawMode.HasFlag(CFontRenderer.DrawMode.NoFill)) {
					SKShader shader = null;
					SKColor color;
					if (tok.UseGradiant) {
						//https://docs.microsoft.com/ja-jp/xamarin/xamarin-forms/user-interface/graphics/skiasharp/effects/shaders/linear-gradient
						shader = SKShader.CreateLinearGradient(
							new SKPoint(0, 25),
							new SKPoint(0, height - 25),
							new SKColor[] {
								new SKColor(tok.GradiantTop.R, tok.GradiantTop.G, tok.GradiantTop.B, tok.GradiantTop.A),
								new SKColor(tok.GradiantBottom.R, tok.GradiantBottom.G, tok.GradiantBottom.B, tok.GradiantBottom.A) },
							new float[] { 0, 1 },
							SKShaderTileMode.Clamp);
						color = new SKColor(0xffffffff);
					} else {
						color = new SKColor(tok.TextColor.R, tok.TextColor.G, tok.TextColor.B);
					}

					float run_x = 25 + x_offset;
					foreach (var (text, runPaint) in runs) {
						runPaint.Shader = shader;
						runPaint.Color = color;
						canvas.DrawText(text, run_x, baseline, runPaint);
						runPaint.Shader = null;
						run_x += runPaint.MeasureText(text);
					}

					// the paints are persistent members; release the per-token gradient shader so its native
					// memory isn't leaked until finalization (the GC heap doesn't track it).
					shader?.Dispose();
				}

				x_offset += token_width;
			}




			canvas.Flush();

			images[i] = bitmap;
		}

		int ret_width = 0;
		int ret_height = 0;
		for (int i = 0; i < images.Length; i++) {
			ret_width = Math.Max(ret_width, images[i].Width);
			ret_height += images[i].Height - 25;
		}




		SKImageInfo skImageInfo = new SKImageInfo(ret_width, ret_height);

		using var skSurface = SKSurface.Create(skImageInfo);
		using var skCanvas = skSurface.Canvas;




		int height_i = -25;
		for (int i = 0; i < images.Length; i++) {
			if (keepCenter) {
				skCanvas.DrawBitmap(images[i], new SKPoint((ret_width / 2) - (images[i].Width / 2.0f), height_i));
			} else {
				skCanvas.DrawBitmap(images[i], new SKPoint(0, height_i));
			}
			height_i += images[i].Height - 50;
			images[i].Dispose();
		}



		// Snapshot the surface, then copy into the returned bitmap; the SKImage is native, so dispose it via using.
		using SKImage image = skSurface.Snapshot();
		//返します
		return SKBitmap.FromImage(image);
	}

	public SKBitmap DrawGlyph(string glyph, bool edgeOnly, int edge_Ratio, int margin) {
		// the glyph's own typeface when the font lacks it; the line metrics stay the font's, so every glyph shares a baseline
		SKPaint glyphPaint = this.Runs(glyph)[0].paint;
		var fm = paint.FontMetrics;
		int width = (int)Math.Ceiling(glyphPaint.MeasureText(glyph)) + 2 * margin;
		int height = (int)Math.Ceiling(fm.Descent - fm.Ascent) + margin;
		// Skia rasterises premultiplied; the straight-alpha copy below is what the GL upload wants
		using var premul = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
		using (var canvas = new SKCanvas(premul)) {
			float baseline = -fm.Ascent;
			if (edgeOnly) {
				// stroking the text through a stroke-style paint is the same outline stroke as extracting the
				// glyph path and stroking that, at a fraction of the cost (measured 16x)
				if (strokePaint == null) {
					strokePaint = new SKPaint();
					strokePaint.Typeface = paint.Typeface;
					strokePaint.TextSize = paint.TextSize;
					strokePaint.IsAntialias = true;
					strokePaint.StrokeJoin = SKStrokeJoin.Round;
					strokePaint.Style = SKPaintStyle.Stroke;
					strokePaint.Color = SKColors.White;
				}
				strokePaint.StrokeWidth = paint.TextSize * 8 / edge_Ratio;
				strokePaint.Typeface = glyphPaint.Typeface;
				canvas.DrawText(glyph, margin, baseline, strokePaint);
			} else {
				glyphPaint.Shader = null;
				glyphPaint.Color = SKColors.White;
				canvas.DrawText(glyph, margin, baseline, glyphPaint);
			}
			canvas.Flush();
		}
		var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
		var straight = new SKBitmap(info);
		using (var pm = premul.PeekPixels()) {
			if (pm == null || !pm.ReadPixels(info, straight.GetPixels(), straight.RowBytes)) {
				straight.Dispose();
				return premul.Copy();
			}
		}
		return straight;
	}

	public void Dispose() {
		paint.Dispose();
		strokePaint?.Dispose();
		foreach (SKPaint fallbackPaint in fallbackPaints.Values)
			fallbackPaint.Dispose();
	}

	// ── fallback fonts ───────────────────────────────────────────────────────────────────────────
	// A character a font lacks is drawn with the first font that has it in the renderer's fallback list (set by the
	// game from its skin, CFontRenderer.SetFallbackFonts), then with the font the system offers for that character.

	private static readonly SKTypeface[][] _fallbacks = [[], []];   // indexed by CFontRenderer.FallbackList
	private static readonly System.Collections.Concurrent.ConcurrentDictionary<(int codepoint, int weight, int width, SKFontStyleSlant slant), SKTypeface> _systemFallbacks = new();

	internal static void SetFallbackFonts(CFontRenderer.FallbackList list, IEnumerable<string> fonts) {
		var typefaces = new List<SKTypeface>();
		foreach (string font in fonts) {
			SKTypeface typeface = null;
			if (File.Exists(font))
				typeface = FileTypeface(font);
			else if (SKFontManager.Default.FontFamilies.Contains(font))
				typeface = GetCachedTypeface($"family:{font}", () => SKTypeface.FromFamilyName(font));
			if (typeface != null)
				typefaces.Add(typeface);
			else
				Trace.TraceWarning($"The fallback font '{font}' could not be found from a file or font family name.");
		}
		Volatile.Write(ref _fallbacks[(int)list], typefaces.ToArray());
	}

	// the typeface that draws this character for a text in `typeface`: itself when it has the character
	internal static SKTypeface TypefaceFor(SKTypeface typeface, int codepoint, CFontRenderer.FallbackList list) {
		if (typeface.ContainsGlyph(codepoint))
			return typeface;
		foreach (SKTypeface fallback in Volatile.Read(ref _fallbacks[(int)list])) {
			if (fallback.ContainsGlyph(codepoint))
				return fallback;
		}
		return SystemTypefaceFor(typeface, codepoint) ?? typeface;
	}

	// the system's font for this character in the style of `typeface` (null when it has none), one shared instance
	// per family and style
	private static SKTypeface SystemTypefaceFor(SKTypeface typeface, int codepoint)
		=> _systemFallbacks.GetOrAdd((codepoint, typeface.FontWeight, typeface.FontWidth, typeface.FontSlant), key => {
			SKTypeface found = SKFontManager.Default.MatchCharacter(null, (SKFontStyleWeight)key.weight, (SKFontStyleWidth)key.width, key.slant, null, key.codepoint);
			if (found == null)
				return null;
			SKTypeface shared = _typefaceCache.GetOrAdd($"system:{found.FamilyName}:{found.FontWeight}:{found.FontWidth}:{found.FontSlant}", found);
			if (!ReferenceEquals(shared, found))
				found.Dispose();
			return shared;
		});

	// marks, joiners, variation selectors, invisible formatting characters and spaces stay in the typeface of the
	// character before them when it has them
	private static bool StaysWithPrevious(int codepoint) {
		if ((codepoint >= 0xFE00 && codepoint <= 0xFE0F) || (codepoint >= 0xE0100 && codepoint <= 0xE01EF))
			return true;
		return CharUnicodeInfo.GetUnicodeCategory(codepoint) is UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark
			or UnicodeCategory.EnclosingMark or UnicodeCategory.SpaceSeparator or UnicodeCategory.Format or UnicodeCategory.Control;
	}

	// the text in runs that one typeface draws, each with its paint; one run when the font has every character
	private List<(string text, SKPaint paint)> Runs(string s) {
		var runs = new List<(string text, SKPaint paint)>();
		if (s.Length == 0 || paint.ContainsGlyphs(s)) {
			runs.Add((s, paint));
			return runs;
		}
		SKTypeface current = null;
		int start = 0;
		for (int i = 0; i < s.Length;) {
			int length = char.IsSurrogatePair(s, i) ? 2 : 1;
			int codepoint = length == 2 ? char.ConvertToUtf32(s, i) : s[i];
			SKTypeface typeface = (current != null && StaysWithPrevious(codepoint) && current.ContainsGlyph(codepoint))
				? current : TypefaceFor(paint.Typeface, codepoint, this.Fallbacks);
			if (current != null && !ReferenceEquals(typeface, current)) {
				runs.Add((s.Substring(start, i - start), this.PaintFor(current)));
				start = i;
			}
			current = typeface;
			i += length;
		}
		runs.Add((s.Substring(start), this.PaintFor(current)));
		return runs;
	}

	public CFontRenderer.FallbackList Fallbacks { get; set; }

	// `paint` drawing with another typeface: one per typeface this renderer has needed
	private readonly Dictionary<SKTypeface, SKPaint> fallbackPaints = new();

	private SKPaint PaintFor(SKTypeface typeface) {
		if (ReferenceEquals(typeface, paint.Typeface))
			return paint;
		if (!fallbackPaints.TryGetValue(typeface, out SKPaint fallbackPaint)) {
			fallbackPaint = paint.Clone();
			fallbackPaint.Typeface = typeface;
			fallbackPaint.Shader = null;
			fallbackPaints[typeface] = fallbackPaint;
		}
		return fallbackPaint;
	}

	// Typefaces are immutable and safe to share across SKPaint instances. Loading one copies
	// the entire font file (~16 MB for CJK fonts) into native memory and SKPaint does not free
	// its Typeface, so without this cache every per-stage/per-HUD renderer leaked a full font
	// copy. Cached for the application's lifetime (a small, bounded set of font/style combos).
	private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, SKTypeface> _typefaceCache = new();

	private static SKTypeface FileTypeface(string fontpath)
		=> GetCachedTypeface($"file:{fontpath}", () => {
			if (OperatingSystem.IsIOS() || OperatingSystem.IsAndroid()) {
				// SKTypeface.FromFile can silently return a broken typeface on iOS (stream
				// loading is equally safe on Android); load via stream on mobile.
				using var fs = File.OpenRead(fontpath);
				return SKTypeface.FromStream(fs);
			} else {
				return SKTypeface.FromFile(fontpath, 0);
			}
		});

	private static SKTypeface GetCachedTypeface(string key, Func<SKTypeface> factory) {
		if (_typefaceCache.TryGetValue(key, out var cached))
			return cached;
		var created = factory();
		if (created != null)
			_typefaceCache[key] = created;
		return created;
	}

	private SKPaint paint = null;

	private SKPaint strokePaint;   // the edge stroke's paint, built on first use from `paint`
}
