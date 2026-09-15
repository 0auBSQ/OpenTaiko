using System;
using System.Drawing;
using System.IO;
using FDK;
using SkiaSharp;
using Xunit;

namespace OpenTaikoTests {
	// The single-glyph raster (CFontRenderer.DrawGlyph, what the Lua glyph atlas bakes) must put its ink exactly
	// where the string renderer (DrawText) puts the same character, only with `margin` px of room instead of 25.
	public class GlyphRasterTests {
		private static string FontPath() {
			// any TTF the repo ships works; the skin's rounded font is the one the game uses
			string p = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "OpenTaiko", "System", "Open-World Memories", "Fonts", "MPLUSRounded1c-Medium.ttf");
			return Path.GetFullPath(p);
		}

		private static (int x0, int y0, int x1, int y1) InkBox(SKBitmap b) {
			int x0 = int.MaxValue, y0 = int.MaxValue, x1 = -1, y1 = -1;
			for (int y = 0; y < b.Height; y++)
				for (int x = 0; x < b.Width; x++)
					if (b.GetPixel(x, y).Alpha > 8) { x0 = Math.Min(x0, x); y0 = Math.Min(y0, y); x1 = Math.Max(x1, x); y1 = Math.Max(y1, y); }
			return (x0, y0, x1, y1);
		}

		[Theory]
		[InlineData("設", 22, false)]
		[InlineData("g", 22, false)]
		[InlineData("W", 40, false)]
		[InlineData("設", 22, true)]
		public void DrawGlyph_MatchesStringRendererInkPlacement(string ch, int size, bool edge) {
			if (!File.Exists(FontPath())) return;   // no skin checkout: nothing to compare against
			var fr = new CFontRenderer(FontPath(), size);
			int margin = (int)Math.Ceiling(size * 1.3 * 8.0 / 30 / 2) + 3;
			using var reference = edge ? fr.DrawTextEdgeOnly(ch, Color.White, 30) : fr.DrawText(ch, Color.White, false);
			using var glyph = fr.DrawGlyph(ch, edge, 30, margin);
			var a = InkBox(reference);
			var b = InkBox(glyph);
			Assert.True(b.x1 >= 0, "the glyph raster is empty");
			int shift = CFontRenderer.TextPadding - margin;
			// same ink, shifted left by the padding the tight box drops; same vertical placement (ascender at y=0)
			Assert.InRange(b.x0 + shift - a.x0, -1, 1);
			Assert.InRange(b.x1 + shift - a.x1, -1, 1);
			Assert.InRange(b.y0 - a.y0, -1, 1);
			Assert.InRange(b.y1 - a.y1, -1, 1);
			// tight box: width = advance + 2*margin, height = line height + margin
			Assert.Equal(reference.Width - 2 * shift, glyph.Width);
			Assert.Equal(reference.Height - shift, glyph.Height);
			// straight alpha, white ink
			Assert.Equal(SKAlphaType.Unpremul, glyph.AlphaType);
			var px = glyph.GetPixel((b.x0 + b.x1) / 2, (b.y0 + b.y1) / 2);
			if (px.Alpha > 200) { Assert.Equal(255, px.Red); Assert.Equal(255, px.Green); Assert.Equal(255, px.Blue); }
		}
	}
}
