using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using FDK;
using OpenTaiko;
using SkiaSharp;
using Xunit;

namespace OpenTaikoTests {
	// Text shaping (HarfBuzz) in the string renderer and the glyph text: kerning and palt follow the skin's switches,
	// measuring agrees with drawing, fallback fonts and vertical text keep working, marks and complex scripts lay out.
	// The switches and fallback lists are static, so these tests run alone.
	[Collection("text shaping")]
	public class TextShapingTests : IDisposable {
		private static string Fonts(string file) => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..",
			"OpenTaiko", "System", "Open-World Memories", "Fonts", file));
		private static readonly string Rounded = Fonts("MPLUSRounded1c-Medium.ttf");   // has kerning (GPOS), no palt
		private static readonly string HanCN = Fonts("ResourceHanRoundedCN-Medium.ttf"); // has kerning and palt
		private static readonly string HanKR = Fonts("ResourceHanRoundedKR-Medium.ttf");

		public TextShapingTests() {
			CFontRenderer.SetShapingFeatures(true, false);
		}

		public void Dispose() {
			CFontRenderer.SetShapingFeatures(true, false);
			CFontRenderer.SetFallbackFonts(CFontRenderer.FallbackList.Main, []);
		}

		private static bool HaveFonts => File.Exists(Rounded) && File.Exists(HanCN) && File.Exists(HanKR);

		private static (int x0, int y0, int x1, int y1) InkBox(SKBitmap b, int fromX = 0, int toX = int.MaxValue) {
			int x0 = int.MaxValue, y0 = int.MaxValue, x1 = -1, y1 = -1;
			for (int y = 0; y < b.Height; y++)
				for (int x = Math.Max(0, fromX); x < Math.Min(b.Width, toX); x++)
					if (b.GetPixel(x, y).Alpha > 8) { x0 = Math.Min(x0, x); y0 = Math.Min(y0, y); x1 = Math.Max(x1, x); y1 = Math.Max(y1, y); }
			return (x0, y0, x1, y1);
		}

		private static bool SamePixels(SKBitmap a, SKBitmap b) {
			if (a.Width != b.Width || a.Height != b.Height) return false;
			for (int y = 0; y < a.Height; y++)
				for (int x = 0; x < a.Width; x++)
					if (a.GetPixel(x, y) != b.GetPixel(x, y)) return false;
			return true;
		}

		// ── kerning ─────────────────────────────────────────────────────────────────

		[Theory]
		[InlineData("AV")]
		[InlineData("Ta")]
		public void Kerning_TucksPairs_WhenOn_AndNotWhenOff(string pair) {
			if (!HaveFonts) return;
			using var font = new CFontRenderer(Rounded, 40);
			string a = pair.Substring(0, 1), b = pair.Substring(1, 1);
			float singles = font.MeasureText(a) + font.MeasureText(b);
			float kerned = font.MeasureText(pair);
			Assert.True(kerned < singles - 1, $"{pair}: kerned {kerned} should be well under {singles}");
			Assert.True(font.MeasureKerning(a, b) < -1);

			CFontRenderer.SetShapingFeatures(false, false);
			Assert.Equal(singles, font.MeasureText(pair), 3);
			Assert.Equal(0, font.MeasureKerning(a, b));
		}

		[Fact]
		public void Kerning_ShowsInTheDrawnInk() {
			if (!HaveFonts) return;
			using var font = new CFontRenderer(Rounded, 40);
			using var on = font.DrawText("AV", Color.White, false);
			CFontRenderer.SetShapingFeatures(false, false);
			using var off = font.DrawText("AV", Color.White, false);
			int tucked = InkBox(off).x1 - InkBox(on).x1;
			Assert.InRange(tucked, 3, 8);   // 52 px text: the pair kerns by 5.2 px
			Assert.True(off.Width > on.Width);
		}

		// strings without kerning pairs draw exactly as plain Skia text did before shaping
		[Theory]
		[InlineData("ab cd", false)]
		[InlineData("ab cd", true)]
		[InlineData("日本語のテキスト", false)]
		[InlineData("Final", false)]   // no fi ligature: the letters stay as they were
		public void PlainStrings_DrawPixelIdenticalToPlainSkia(string text, bool edge) {
			if (!HaveFonts) return;
			using var font = new CFontRenderer(Rounded, 30);
			using var shaped = edge ? font.DrawText(text, Color.White, Color.Black, null, 30, false) : font.DrawText(text, Color.White, false);
			using var reference = PlainSkia(Rounded, 30, text, edge);
			Assert.True(SamePixels(shaped, reference), "the shaped string differs from plain Skia text");
		}

		// what the string renderer did before shaping: Skia's text drawing, char by char advances
		private static SKBitmap PlainSkia(string path, int pt, string text, bool edge) {
			using var tf = SKTypeface.FromFile(path);
			using var paint = new SKPaint { Typeface = tf, TextSize = pt * 1.3f, IsAntialias = true };
			int width = (int)Math.Ceiling(paint.MeasureText(text)) + 50;
			int height = (int)Math.Ceiling(paint.FontMetrics.Descent - paint.FontMetrics.Ascent) + 50;
			using var line = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
			using (var canvas = new SKCanvas(line)) {
				float baseline = -paint.FontMetrics.Ascent + 25;
				if (edge) {
					using var edgePaint = new SKPaint { StrokeWidth = paint.TextSize * 8 / 30, StrokeJoin = SKStrokeJoin.Round, Color = SKColors.Black, Style = SKPaintStyle.Stroke, IsAntialias = true };
					using var p = paint.GetTextPath(text, 25, baseline);
					canvas.DrawPath(p, edgePaint);
				}
				paint.Color = SKColors.White;
				canvas.DrawText(text, 25, baseline, paint);
				canvas.Flush();
			}
			using var surface = SKSurface.Create(new SKImageInfo(width, height - 25));
			surface.Canvas.DrawBitmap(line, new SKPoint(0, -25));
			using var image = surface.Snapshot();
			return SKBitmap.FromImage(image);
		}

		// ── measure == draw ─────────────────────────────────────────────────────────

		[Theory]
		[InlineData("AV AV")]
		[InlineData("Ta Ta")]
		[InlineData("WAVE Today")]
		[InlineData("A<c.#ff0000>V</c>To")]
		public void DrawnWidth_IsTheMeasuredWidth(string text) {
			if (!HaveFonts) return;
			using var font = new CFontRenderer(Rounded, 40);
			string plain = text.Replace("<c.#ff0000>", "").Replace("</c>", "");
			using var bmp = font.DrawText(text, Color.White, false);
			// a colour tag splits the shaping: no kerning across it, like the glyph text
			float measured = text == plain ? font.MeasureText(plain)
				: font.MeasureText("A") + font.MeasureText("V") + font.MeasureText("To");
			Assert.Equal((int)Math.Ceiling(measured) + 2 * CFontRenderer.TextPadding, bmp.Width);
			// the ink ends inside the measured advance (the last letter's right side bearing is small)
			var ink = InkBox(bmp);
			Assert.InRange(ink.x1, CFontRenderer.TextPadding + measured - 12, CFontRenderer.TextPadding + measured + 3);
		}

		[Theory]
		[InlineData("AV AV Ta WAVE Today")]
		[InlineData("To <c.#ff0000>AV</c> Ta")]
		public void GlyphTextPairCache_AgreesWithTheRenderer(string text) {
			if (!HaveFonts) return;
			using var font = new CFontRenderer(Rounded, 40);
			var (runes, _) = GlyphTextLayout.Tokenize(text);
			double Adv(int cp) => font.MeasureText(char.ConvertFromUtf32(cp));
			double Kern(int a, int b) => font.MeasureKerning(char.ConvertFromUtf32(a), char.ConvertFromUtf32(b));
			var lines = GlyphTextLayout.Wrap(runes, 0, Adv, Kern);
			Assert.Single(lines);
			// the string renderer shapes the text between tags on its own
			double expected = 0;
			foreach (string part in text.Replace("</c>", "<c.#ff0000>").Split("<c.#ff0000>"))
				expected += font.MeasureText(part);
			Assert.Equal(expected, lines[0].Width, 2);
		}

		[Fact]
		public void Wrap_WithKerning_BreaksWhereTheKernedWidthOverflows() {
			if (!HaveFonts) return;
			using var font = new CFontRenderer(Rounded, 40);
			double Adv(int cp) => font.MeasureText(char.ConvertFromUtf32(cp));
			double Kern(int a, int b) => font.MeasureKerning(char.ConvertFromUtf32(a), char.ConvertFromUtf32(b));
			var (runes, _) = GlyphTextLayout.Tokenize("WAVE WAVE");
			double kerned = font.MeasureText("WAVE WAVE");
			double unkerned = runes.Sum(r => Adv(r.CodePoint));
			// room for the kerned line but not the unkerned one: fits only with kerning
			double room = (kerned + unkerned) / 2;
			Assert.True(unkerned - kerned > 10);
			Assert.Single(GlyphTextLayout.Wrap(runes, room, Adv, Kern));
			Assert.Equal(2, GlyphTextLayout.Wrap(runes, room, Adv).Count);
		}

		// ── palt ────────────────────────────────────────────────────────────────────

		[Fact]
		public void Palt_NarrowsJapanese_OnlyWhenOn() {
			if (!HaveFonts) return;
			using var font = new CFontRenderer(HanCN, 40);
			const string s = "「テスト」です。";
			float full = font.MeasureText(s);
			CFontRenderer.SetShapingFeatures(true, true);
			float proportional = font.MeasureText(s);
			Assert.True(proportional < full - 20, $"palt should narrow the text ({proportional} vs {full})");
			// and the drawing follows
			using var bmp = font.DrawText(s, Color.White, false);
			Assert.Equal((int)Math.Ceiling(proportional) + 50, bmp.Width);
		}

		// ── vertical text ───────────────────────────────────────────────────────────

		[Theory]
		[InlineData("AVTa")]
		[InlineData("「テスト」")]
		public void VerticalText_IsPerCharacter_WhateverTheSwitches(string text) {
			if (!HaveFonts) return;
			using var font = new CFontRenderer(HanCN, 30);
			CFontRenderer.SetShapingFeatures(false, false);
			using var plain = font.DrawText_V(text, Color.White, Color.Black, null, 30);
			CFontRenderer.SetShapingFeatures(true, true);
			using var shaped = font.DrawText_V(text, Color.White, Color.Black, null, 30);
			Assert.True(SamePixels(plain, shaped), "vertical text changed with kerning/palt");
		}

		// ── fallback fonts ──────────────────────────────────────────────────────────

		[Fact]
		public void FallbackRuns_AreShapedInTheirOwnTypeface() {
			if (!HaveFonts) return;
			CFontRenderer.SetFallbackFonts(CFontRenderer.FallbackList.Main, [HanKR]);
			using var font = new CFontRenderer(Rounded, 40);   // the rounded font has no Hangul
			float latin = font.MeasureText("AV");
			float hangul = font.MeasureText("한글");
			Assert.True(hangul > 60, "the Hangul should come from the fallback font");
			// runs of different typefaces join without kerning across them, each run keeps its own
			Assert.Equal(latin + hangul, font.MeasureText("AV한글"), 2);
			Assert.True(latin < font.MeasureText("A") + font.MeasureText("V") - 1);
			using var bmp = font.DrawText("AV한글", Color.White, false);
			Assert.Equal((int)Math.Ceiling(latin + hangul) + 50, bmp.Width);
			var hangulInk = InkBox(bmp, 25 + (int)Math.Ceiling(latin) + 2);
			Assert.True(hangulInk.x1 >= 0, "the Hangul was not drawn");
		}

		// ── marks and complex scripts ───────────────────────────────────────────────

		[Fact]
		public void CombiningMark_SitsOverItsBase() {
			if (!HaveFonts) return;
			using var font = new CFontRenderer(HanCN, 40);
			using var baseOnly = font.DrawText("e", Color.White, false);
			using var marked = font.DrawText("é", Color.White, false);
			Assert.Equal(font.MeasureText("e"), font.MeasureText("é"), 2);   // the mark takes no room
			var e = InkBox(baseOnly);
			var m = InkBox(marked);
			Assert.True(m.y0 < e.y0 - 3, "the acute should rise above the e");
			// the ink above the e lies over the e, not after it
			var above = InkBox(Crop(marked, 0, 0, marked.Width, e.y0 - 1));
			Assert.True(above.x1 >= 0);
			Assert.True(above.x0 >= e.x0 - 2 && above.x1 <= e.x1 + 2, $"acute at {above.x0}..{above.x1}, e at {e.x0}..{e.x1}");
		}

		private static SKBitmap Crop(SKBitmap b, int x, int y, int w, int h) {
			var r = new SKBitmap(Math.Max(1, w), Math.Max(1, h));
			b.ExtractSubset(r, new SKRectI(x, y, x + w, y + h));
			return r;
		}

		// complex scripts come from the system's fonts here, so a machine without them skips
		[Theory]
		[InlineData("ببب", "ب")]                 // Arabic: joined forms are narrower than three isolated ones
		[InlineData("क्ष", null)]                     // Devanagari: the conjunct is one glyph, narrower than its parts
		[InlineData("ที่", "ท")]                 // Thai: the vowel and tone marks stack on the consonant
		public void ComplexScripts_Shape(string text, string? unit) {
			if (!HaveFonts) return;
			using var font = new CFontRenderer(Rounded, 40);
			int first = char.ConvertToUtf32(text, 0);
			var system = CFontRenderer.TypefaceFor(SKTypeface.FromFile(Rounded), first);
			if (system == null || !system.ContainsGlyph(first) || system.FamilyName.Contains("Rounded")) return;
			float whole = font.MeasureText(text);
			float parts = text.Sum(c => font.MeasureText(c.ToString()));
			if (unit == "ท")
				Assert.Equal(font.MeasureText(unit), whole, 1);
			else
				Assert.True(whole < parts - 1, $"{text}: shaped {whole} vs {parts} for the separate characters");
			using var bmp = font.DrawText(text, Color.White, false);
			Assert.True(InkBox(bmp).x1 >= 0);
		}
	}

	[CollectionDefinition("text shaping", DisableParallelization = true)]
	public class TextShapingCollection { }
}
