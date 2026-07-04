using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;

namespace OpenTaiko;

// Pure layout helpers for glyph-composed text rendering (LuaGlyphText): color-tag tokenizing, word wrap and
// squish math. No font or GPU dependencies — advances are supplied by the caller — so all of it is
// unit-testable headless.
public static class GlyphTextLayout {
	// keep in sync with CSkiaSharpTextRenderer.TagRegex and StringExtensions.TagRegex
	private static readonly Regex TagRegex = new(@"<(/?)([gc](?:\.#[0-9a-fA-F]{6})*?)>", RegexOptions.Compiled);

	// a color override introduced by a <c.#rrggbb> or <c.#rrggbb#rrggbb> tag (fore / fore+outline)
	public readonly record struct Style(Color Fore, Color? Outline);

	// one Unicode code point with the style active at its position; StyleId -1 = the caller's default colors
	public readonly record struct StyledRune(int CodePoint, int StyleId);

	// Split text into code points + a style table. Mirrors CSkiaSharpTextRenderer.Tokenize semantics:
	// <c.#fore> / <c.#fore#outline> push a color override, </c> pops, tags nest, unclosed tags run to the end.
	// <g.…> gradient tags are structural only here (pushed so their close pops correctly, colors unchanged).
	public static (List<StyledRune> Runes, List<Style> Styles) Tokenize(string text) {
		var runes = new List<StyledRune>();
		var styles = new List<Style>();
		if (string.IsNullOrEmpty(text)) return (runes, styles);

		var stack = new Stack<int>();   // style ids; empty = default (-1)
		int lastPos = 0;
		foreach (Match match in TagRegex.Matches(text)) {
			AppendRunes(runes, text, lastPos, match.Index, stack.Count > 0 ? stack.Peek() : -1);
			lastPos = match.Index + match.Length;

			if (match.Groups[1].Value == "/") {
				if (stack.Count > 0) stack.Pop();
				continue;
			}
			// inherit the current style, then apply the tag's colors on top (c only; g keeps colors)
			Style current = stack.Count > 0 ? styles[stack.Peek()] : new Style(Color.Empty, null);
			string[] parts = match.Groups[2].Value.Split('.');
			if (parts[0] == "c") {
				if (parts.Length > 1) current = current with { Fore = ColorTranslator.FromHtml(parts[1]) };
				if (parts.Length > 2) current = current with { Outline = ColorTranslator.FromHtml(parts[2]) };
			}
			styles.Add(current);
			stack.Push(styles.Count - 1);
		}
		AppendRunes(runes, text, lastPos, text.Length, stack.Count > 0 ? stack.Peek() : -1);
		return (runes, styles);
	}

	private static void AppendRunes(List<StyledRune> runes, string text, int start, int end, int styleId) {
		int i = start;
		while (i < end) {
			// surrogate-safe code-point iteration
			int cp = char.ConvertToUtf32(text, i);
			runes.Add(new StyledRune(cp, styleId));
			i += char.IsSurrogatePair(text, i) ? 2 : 1;
		}
	}

	// a wrapped line: runes[Start .. Start+Count), Width = advance sum incl. inner spaces (edge spaces dropped)
	public readonly record struct Line(int Start, int Count, double Width);

	// Greedy word wrap. '\n' always breaks; spaces are break points and are dropped at line edges; CJK runes
	// may break between any two runes; an unbreakable run wider than maxWidth hard-breaks mid-run.
	// maxWidth <= 0 disables width wrapping (only '\n' splits). advance() supplies each rune's width in the
	// same unit as maxWidth.
	public static List<Line> Wrap(IReadOnlyList<StyledRune> runes, double maxWidth, Func<int, double> advance) {
		var lines = new List<Line>();
		int n = runes.Count;
		int lineStart = -1, lineEnd = -1;   // current line rune range (-1 = empty line so far)
		double lineWidth = 0;
		int pendingSpaces = 0;              // spaces seen since the last word, not yet committed to the line
		double pendingWidth = 0;

		void Flush() {
			lines.Add(lineStart < 0 ? new Line(0, 0, 0) : new Line(lineStart, lineEnd - lineStart, lineWidth));
			lineStart = lineEnd = -1; lineWidth = 0; pendingSpaces = 0; pendingWidth = 0;
		}
		void Append(int start, int endEx, double width) {
			if (lineStart < 0) { lineStart = start; lineWidth = 0; } else { lineWidth += pendingWidth; }
			lineEnd = endEx; lineWidth += width;
			pendingSpaces = 0; pendingWidth = 0;
		}

		int i = 0;
		while (i < n) {
			int cp = runes[i].CodePoint;
			if (cp == '\n') { Flush(); i++; continue; }
			if (cp == ' ') { pendingSpaces++; pendingWidth += advance(cp); i++; continue; }

			// collect one unbreakable word: a single CJK rune, or a run of non-space non-CJK runes
			int wordStart = i;
			double wordWidth = 0;
			if (IsCjk(cp)) {
				wordWidth = advance(cp);
				i++;
			} else {
				while (i < n && runes[i].CodePoint != ' ' && runes[i].CodePoint != '\n' && !IsCjk(runes[i].CodePoint)) {
					wordWidth += advance(runes[i].CodePoint);
					i++;
				}
			}

			if (maxWidth > 0 && lineStart >= 0 && lineWidth + pendingWidth + wordWidth > maxWidth) {
				Flush();   // drops the pending spaces at the break
			}
			if (maxWidth > 0 && lineStart < 0 && wordWidth > maxWidth && (i - wordStart) > 1) {
				// hard-break an overlong word: emit maxWidth-sized chunks, leave the tail as the current line
				int j = wordStart;
				double w = 0;
				int chunkStart = wordStart;
				while (j < i) {
					double a = advance(runes[j].CodePoint);
					if (w > 0 && w + a > maxWidth) {
						lines.Add(new Line(chunkStart, j - chunkStart, w));
						chunkStart = j; w = 0;
					}
					w += a; j++;
				}
				Append(chunkStart, i, w);
				continue;
			}
			Append(wordStart, i, wordWidth);
		}
		Flush();   // final line (also emits one empty line for empty/blank-tail input)
		return lines;
	}

	// scripts written without spaces, where a line may break between any two characters
	public static bool IsCjk(int cp) {
		return (cp >= 0x1100 && cp <= 0x11FF)      // Hangul Jamo
			|| (cp >= 0x2E80 && cp <= 0x303F)      // CJK radicals, Kangxi, CJK symbols/punctuation (、。)
			|| (cp >= 0x3040 && cp <= 0x30FF)      // hiragana + katakana
			|| (cp >= 0x3130 && cp <= 0x318F)      // Hangul compatibility Jamo
			|| (cp >= 0x31F0 && cp <= 0x31FF)      // katakana phonetic extensions
			|| (cp >= 0x3400 && cp <= 0x4DBF)      // CJK ext A
			|| (cp >= 0x4E00 && cp <= 0x9FFF)      // CJK unified
			|| (cp >= 0xAC00 && cp <= 0xD7AF)      // Hangul syllables
			|| (cp >= 0xF900 && cp <= 0xFAFF)      // CJK compatibility
			|| (cp >= 0xFF00 && cp <= 0xFFEF)      // fullwidth forms
			|| (cp >= 0x20000 && cp <= 0x2FA1F);   // CJK ext B+ (astral)
	}

	// per-letter squish: 1.0 while the text fits, else the shrink factor that makes it exactly maxWidth wide
	public static double SquishFactor(double naturalWidth, double maxWidth) {
		if (maxWidth <= 0 || naturalWidth <= 0 || naturalWidth <= maxWidth) return 1.0;
		return maxWidth / naturalWidth;
	}
}
