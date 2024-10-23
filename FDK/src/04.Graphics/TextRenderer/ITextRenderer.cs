using SkiaSharp;
using static FDK.CSkiaSharpTextRenderer;
using Color = System.Drawing.Color;

namespace FDK;

internal interface ITextRenderer : IDisposable {
	SKBitmap DrawText(string drawstr, CFontRenderer.DrawMode drawmode, Color fontColor, Color edgeColor, Color? secondEdgeColor, Color gradationTopColor, Color gradationBottomColor, int edge_Ratio, bool keepCenter);

	string Purify(string input);

	List<SStringToken> Tokenize(string input, Color fontColor, Color edgeColor, Color? secondEdgeColor, Color gradationTopColor, Color gradationBottomColor);
}
