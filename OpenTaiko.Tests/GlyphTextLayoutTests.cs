using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using FDK;
using OpenTaiko;
using Xunit;

namespace OpenTaikoTests {
	// Headless tests for the glyph-text layout engine (GlyphTextLayout): color-tag tokenizing, word wrap,
	// squish math — plus a CPU-only sanity check of the new FDK font measurement (SkiaSharp needs no GPU).
	public class GlyphTextLayoutTests {
		// fixed-width advance: 10 per rune (spaces included) unless overridden
		private static double Adv(int cp) => 10;

		private static (List<GlyphTextLayout.StyledRune> runes, List<GlyphTextLayout.Style> styles) Tok(string s)
			=> GlyphTextLayout.Tokenize(s);

		private static string LineText(List<GlyphTextLayout.StyledRune> runes, GlyphTextLayout.Line l)
			=> string.Concat(Enumerable.Range(l.Start, l.Count).Select(i => char.ConvertFromUtf32(runes[i].CodePoint)));

		private static List<string> WrapText(string s, double maxWidth) {
			var (runes, _) = Tok(s);
			return GlyphTextLayout.Wrap(runes, maxWidth, Adv).Select(l => LineText(runes, l)).ToList();
		}

		// ── wrap ────────────────────────────────────────────────────────────────────

		[Fact]
		public void Wrap_BreaksAtSpaces_AndDropsEdgeSpaces() {
			// each word "aaaa" = 40 wide; width 100 fits two words + space (90)
			var lines = WrapText("aaaa bbbb cccc dddd", 100);
			Assert.Equal(new[] { "aaaa bbbb", "cccc dddd" }, lines);
		}

		[Fact]
		public void Wrap_HonorsExplicitNewlines() {
			var lines = WrapText("ab\ncd\n\nef", 1000);
			Assert.Equal(new[] { "ab", "cd", "", "ef" }, lines);
		}

		[Fact]
		public void Wrap_CjkBreaksAnywhere() {
			// 8 CJK runes at 10 each; width 30 → 3 per line
			var lines = WrapText("日本語のテキスト格", 30);
			Assert.Equal(new[] { "日本語", "のテキ", "スト格" }, lines);
		}

		[Fact]
		public void Wrap_HardBreaksOverlongWord() {
			var lines = WrapText("abcdefghij", 30);   // one unbreakable 100-wide word, 3 runes per line
			Assert.Equal(4, lines.Count);
			Assert.Equal("abc", lines[0]);
			Assert.Equal("j", lines[3]);
		}

		[Fact]
		public void Wrap_TrailingSpaces_DoNotCountTowardWidth() {
			var (runes, _) = Tok("ab   ");
			var lines = GlyphTextLayout.Wrap(runes, 1000, Adv);
			Assert.Single(lines);
			Assert.Equal(20, lines[0].Width);   // trailing spaces excluded from the line width
		}

		[Fact]
		public void Wrap_EmptyText_YieldsOneEmptyLine() {
			var (runes, _) = Tok("");
			var lines = GlyphTextLayout.Wrap(runes, 100, Adv);
			Assert.Single(lines);
			Assert.Equal(0, lines[0].Count);
		}

		[Fact]
		public void Wrap_WidthSmallerThanOneGlyph_StillProgresses() {
			var lines = WrapText("abc", 5);   // every rune wider than the box
			Assert.Equal(3, lines.Count);     // one rune per line, never an infinite loop
		}

		[Fact]
		public void Wrap_NoMaxWidth_OnlyNewlinesSplit() {
			var lines = WrapText("aaaa bbbb cccc dddd eeee ffff", 0);
			Assert.Single(lines);
		}

		[Fact]
		public void Wrap_MixedLatinAndCjk() {
			// "ab " (20+space) + 4 CJK; width 45 → "ab 日" (40), then "本語の"
			var lines = WrapText("ab 日本語の", 45);
			Assert.Equal(new[] { "ab 日", "本語の" }, lines);
		}

		// ── tokenize ────────────────────────────────────────────────────────────────

		[Fact]
		public void Tokenize_PlainText_NoStyles() {
			var (runes, styles) = Tok("abc");
			Assert.Equal(3, runes.Count);
			Assert.All(runes, r => Assert.Equal(-1, r.StyleId));
			Assert.Empty(styles);
		}

		[Fact]
		public void Tokenize_ColorTag_AppliesAndPops() {
			var (runes, styles) = Tok("a<c.#ff0000>b</c>c");
			Assert.Equal(3, runes.Count);
			Assert.Equal(-1, runes[0].StyleId);
			Assert.True(runes[1].StyleId >= 0);
			Assert.Equal(-1, runes[2].StyleId);
			Assert.Equal(Color.FromArgb(255, 0, 0).ToArgb(), styles[runes[1].StyleId].Fore.ToArgb());
			Assert.Null(styles[runes[1].StyleId].Outline);
		}

		[Fact]
		public void Tokenize_ColorTagWithOutline() {
			// two-color form is dot-separated (the engine splits the tag body on '.')
			var (runes, styles) = Tok("<c.#00ff00.#0000ff>x</c>");
			var st = styles[runes[0].StyleId];
			Assert.Equal(Color.FromArgb(0, 255, 0).ToArgb(), st.Fore.ToArgb());
			Assert.Equal(Color.FromArgb(0, 0, 255).ToArgb(), st.Outline!.Value.ToArgb());
		}

		[Fact]
		public void Tokenize_NestedTags_RestoreOuter() {
			var (runes, styles) = Tok("<c.#ff0000>a<c.#00ff00>b</c>c</c>");
			Assert.Equal(Color.FromArgb(255, 0, 0).ToArgb(), styles[runes[0].StyleId].Fore.ToArgb());
			Assert.Equal(Color.FromArgb(0, 255, 0).ToArgb(), styles[runes[1].StyleId].Fore.ToArgb());
			Assert.Equal(Color.FromArgb(255, 0, 0).ToArgb(), styles[runes[2].StyleId].Fore.ToArgb());
		}

		[Fact]
		public void Tokenize_UnclosedTag_RunsToEnd() {
			var (runes, styles) = Tok("<c.#ff0000>abc");
			Assert.All(runes, r => Assert.Equal(runes[0].StyleId, r.StyleId));
		}

		[Fact]
		public void Tokenize_GradientTag_StrippedButBalanced() {
			var (runes, styles) = Tok("<g.#ff0000.#00ff00>ab</g>c");
			Assert.Equal(3, runes.Count);
			// g keeps the current colors (empty default), and its close pops correctly
			Assert.True(styles[runes[0].StyleId].Fore.IsEmpty);
			Assert.Equal(-1, runes[2].StyleId);
		}

		[Fact]
		public void Tokenize_SurrogatePair_IsOneRune() {
			var (runes, _) = Tok("a𝄞b");   // U+1D11E musical symbol
			Assert.Equal(3, runes.Count);
			Assert.Equal(0x1D11E, runes[1].CodePoint);
		}

		[Fact]
		public void Wrap_SurrogatePair_NeverSplit() {
			var (runes, _) = Tok("𝄞𝄞𝄞");
			var lines = GlyphTextLayout.Wrap(runes, 10, Adv);
			Assert.Equal(3, lines.Count);   // hard-break between pairs is fine; through one is impossible by construction
		}

		// ── squish ──────────────────────────────────────────────────────────────────

		[Theory]
		[InlineData(100, 0, 1.0)]      // no max: never squish
		[InlineData(100, 200, 1.0)]    // fits: no squish
		[InlineData(200, 100, 0.5)]    // 2x too wide → half
		[InlineData(0, 100, 1.0)]      // degenerate
		public void SquishFactor_Clamps(double natural, double max, double expected) {
			Assert.Equal(expected, GlyphTextLayout.SquishFactor(natural, max), 6);
		}

		[Fact]
		public void Squish_PerGlyphPositions_MatchWholeStringScaling() {
			// per-letter mapping x_i = f·Σadv must equal scaling the whole string's pen positions by f
			double[] adv = { 12, 7, 22, 9 };
			double natural = adv.Sum();
			double f = GlyphTextLayout.SquishFactor(natural, 30);
			double pen = 0;
			for (int i = 0; i < adv.Length; i++) {
				double perGlyph = f * pen;
				double wholeString = pen * (30.0 / natural);
				Assert.Equal(wholeString, perGlyph, 9);
				pen += adv[i];
			}
		}

		// ── CPU-only FDK font measurement (embedded fallback font; no GPU) ─────────

		[Fact]
		public void FontRenderer_MeasureText_IsAdditive_AndSpacesCount() {
			using var font = new CFontRenderer(null, 40);
			float a = font.MeasureText("a");
			float b = font.MeasureText("b");
			float ab = font.MeasureText("ab");
			Assert.True(a > 0 && b > 0);
			Assert.True(Math.Abs((a + b) - ab) < 1.5f);   // no kerning surprises for this pair
			Assert.True(font.MeasureText(" ") > 0);        // advance includes spaces
			Assert.True(font.GetLineHeight() > 0);
		}
	}
}
