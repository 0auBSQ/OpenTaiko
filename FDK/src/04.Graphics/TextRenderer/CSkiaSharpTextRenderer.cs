using System.Drawing;
using System.Text.RegularExpressions;
using SkiaSharp;
using Color = System.Drawing.Color;

namespace FDK {
	internal class CSkiaSharpTextRenderer : ITextRenderer {
		//https://monobook.org/wiki/SkiaSharp%E3%81%A7%E6%97%A5%E6%9C%AC%E8%AA%9E%E6%96%87%E5%AD%97%E5%88%97%E3%82%92%E6%8F%8F%E7%94%BB%E3%81%99%E3%82%8B
		public CSkiaSharpTextRenderer(string fontpath, int pt) {
			Initialize(fontpath, pt, CFontRenderer.FontStyle.Regular);
		}

		public CSkiaSharpTextRenderer(string fontpath, int pt, CFontRenderer.FontStyle style) {
			Initialize(fontpath, pt, style);
		}

		public CSkiaSharpTextRenderer(Stream fontstream, int pt, CFontRenderer.FontStyle style) {
			Initialize(fontstream, pt, style);
		}

		protected void Initialize(Stream fontstream, int pt, CFontRenderer.FontStyle style) {
			paint = new SKPaint();

			//stream・filepathから生成した場合に、style設定をどうすればいいのかがわからない
			paint.Typeface = SKFontManager.Default.CreateTypeface(fontstream);

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

			if (SKFontManager.Default.FontFamilies.Contains(fontpath))
				paint.Typeface = SKTypeface.FromFamilyName(fontpath, weight, width, slant);

			//stream・filepathから生成した場合に、style設定をどうすればいいのかがわからない
			if (File.Exists(fontpath))
				paint.Typeface = SKTypeface.FromFile(fontpath, 0);

			if (paint.Typeface == null)
				throw new FileNotFoundException(fontpath);

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

		private string Purify(string input) {
			return Regex.Replace(input, TagRegex, "");
		}

		private List<SStringToken> Tokenize(string input, Color fontColor, Color edgeColor, Color? secondEdgeColor, Color gradationTopColor, Color gradationBottomColor) {
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
				SKRect bounds = new SKRect();

				int width = (int)Math.Ceiling(paint.MeasureText(Purify(strs[i]), ref bounds)) + 50;
				int height = (int)Math.Ceiling(paint.FontMetrics.Descent - paint.FontMetrics.Ascent) + 50;

				//少し大きめにとる(定数じゃない方法を考えましょう)
				SKBitmap bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
				SKCanvas canvas = new SKCanvas(bitmap);

				int x_offset = 0;

				foreach (SStringToken tok in tokens[i]) {
					int token_width = (int)Math.Ceiling(paint.MeasureText(tok.s, ref bounds));

					if (drawMode.HasFlag(CFontRenderer.DrawMode.Edge) || tok.UseOutline) {

						SKPath path = paint.GetTextPath(tok.s, 25 + x_offset, -paint.FontMetrics.Ascent + 25);

						if (secondEdgeColor != null) {
							SKPaint secondEdgePaint = new SKPaint();
							secondEdgePaint.StrokeWidth = paint.TextSize * 8 / edge_Ratio;
							secondEdgePaint.StrokeJoin = SKStrokeJoin.Round;
							secondEdgePaint.Color = new SKColor(secondEdgeColor.Value.R, secondEdgeColor.Value.G, secondEdgeColor.Value.B, secondEdgeColor.Value.A);
							secondEdgePaint.Style = SKPaintStyle.Stroke;
							secondEdgePaint.IsAntialias = true;
							canvas.DrawPath(path, secondEdgePaint);
						}

						SKPaint edgePaint = new SKPaint();
						edgePaint.StrokeWidth = paint.TextSize * (secondEdgeColor == null ? 8 : 4) / edge_Ratio;
						edgePaint.StrokeJoin = SKStrokeJoin.Round;
						edgePaint.Color = new SKColor(tok.OutlineColor.R, tok.OutlineColor.G, tok.OutlineColor.B, tok.OutlineColor.A);
						edgePaint.Style = SKPaintStyle.Stroke;
						edgePaint.IsAntialias = true;
						canvas.DrawPath(path, edgePaint);
					}

					if (tok.UseGradiant) {
						//https://docs.microsoft.com/ja-jp/xamarin/xamarin-forms/user-interface/graphics/skiasharp/effects/shaders/linear-gradient
						paint.Shader = SKShader.CreateLinearGradient(
							new SKPoint(0, 25),
							new SKPoint(0, height - 25),
							new SKColor[] {
						new SKColor(tok.GradiantTop.R, tok.GradiantTop.G, tok.GradiantTop.B, tok.GradiantTop.A),
						new SKColor(tok.GradiantBottom.R, tok.GradiantBottom.G, tok.GradiantBottom.B, tok.GradiantBottom.A) },
							new float[] { 0, 1 },
							SKShaderTileMode.Clamp);
						paint.Color = new SKColor(0xffffffff);
					} else {
						paint.Shader = null;
						paint.Color = new SKColor(tok.TextColor.R, tok.TextColor.G, tok.TextColor.B);
					}

					canvas.DrawText(tok.s, 25 + x_offset, -paint.FontMetrics.Ascent + 25, paint);

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



			SKImage image = skSurface.Snapshot();
			//返します
			return SKBitmap.FromImage(image);
		}

		public void Dispose() {
			paint.Dispose();
		}

		private SKPaint paint = null;
	}
}
