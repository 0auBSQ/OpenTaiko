using SkiaSharp;
using static FDK.CSkiaSharpTextRenderer;
using Color = System.Drawing.Color;

namespace FDK;

internal interface ITextRenderer : IDisposable {
	SKBitmap DrawText(string drawstr, CFontRenderer.DrawMode drawmode, Color fontColor, Color edgeColor, Color? secondEdgeColor, Color gradationTopColor, Color gradationBottomColor, int edge_Ratio, bool keepCenter);

	// one glyph, white, on a tight bitmap: the ink (or its edge stroke alone) with `margin` px of room on the
	// left, right and bottom and the ascender line at y = 0 — the geometry DrawText gives a single character
	// once its 25 px paddings shrink to `margin`. Straight (unpremultiplied) RGBA, ready to upload as is.
	SKBitmap DrawGlyph(string glyph, bool edgeOnly, int edge_Ratio, int margin);

	string Purify(string input);

	List<SStringToken> Tokenize(string input, Color fontColor, Color edgeColor, Color? secondEdgeColor, Color gradationTopColor, Color gradationBottomColor);

	float MeasureText(string s);

	float GetLineHeight();
}
