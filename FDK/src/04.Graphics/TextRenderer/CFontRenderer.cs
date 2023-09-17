using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using SkiaSharp;

using Color = System.Drawing.Color;

namespace FDK
{

	public class CFontRenderer : IDisposable
	{
		#region[static系]
		public static void SetTextCorrectionX_Chara_List_Vertical(string[] list)
		{
			if (list != null)
				CorrectionX_Chara_List_Vertical = list.Where(c => c != null).ToArray();
		}
		public static void SetTextCorrectionX_Chara_List_Value_Vertical(int[] list)
		{
			if (list != null)
				CorrectionX_Chara_List_Value_Vertical = list;
		}
		public static void SetTextCorrectionY_Chara_List_Vertical(string[] list)
		{
			if (list != null)
				CorrectionY_Chara_List_Vertical = list.Where(c => c != null).ToArray();
		}
		public static void SetTextCorrectionY_Chara_List_Value_Vertical(int[] list)
		{
			if (list != null)
				CorrectionY_Chara_List_Value_Vertical = list;
		}
		public static void SetRotate_Chara_List_Vertical(string[] list)
		{
			if (list != null)
				Rotate_Chara_List_Vertical = list.Where(c => c != null).ToArray();
		}

		private static string[] CorrectionX_Chara_List_Vertical = new string[0];
		private static int[] CorrectionX_Chara_List_Value_Vertical = new int[0];
		private static string[] CorrectionY_Chara_List_Vertical = new string[0];
		private static int[] CorrectionY_Chara_List_Value_Vertical = new int[0];
		private static string[] Rotate_Chara_List_Vertical = new string[0];
		#endregion



		[Flags]
		public enum DrawMode
		{
			Normal = 0,
			Edge,
			Gradation
		}

		[Flags]
		public enum FontStyle
		{
			Regular = 0,
			Bold,
			Italic,
			Underline,
			Strikeout
		}

		public static string DefaultFontName
		{
			get
			{
				if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
					return "MS UI Gothic";
				else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
					return "ヒラギノ角ゴ Std W8";//OSX搭載PC未所持のため暫定
				else
					return "Droid Sans Fallback";
			}
		}

		#region [ コンストラクタ ]
		public CFontRenderer(string fontpath, int pt, FontStyle style)
		{
			Initialize(fontpath, pt, style);
		}
		public CFontRenderer(string fontpath, int pt)
		{
			Initialize(fontpath, pt, FontStyle.Regular);
		}
		public CFontRenderer()
		{
			//throw new ArgumentException("CFontRenderer: 引数があるコンストラクタを使用してください。");
		}
		#endregion

		protected void Initialize(string fontpath, int pt, FontStyle style)
		{
			try
			{
				this.textRenderer = new CSkiaSharpTextRenderer(fontpath, pt, style);
				return;
			}
			catch(Exception e)
            {
                Trace.TraceWarning("SkiaSharpでのフォント生成に失敗しました。" + e.ToString());
                this.textRenderer?.Dispose();
            }

			try 
			{
				this.textRenderer = new CSkiaSharpTextRenderer(Assembly.GetExecutingAssembly().GetManifestResourceStream(@"FDK.mplus-1p-medium.ttf"), pt, style);
			}
			catch (Exception e)
			{
				Trace.TraceWarning("ビルトインフォントを使用してのフォント生成に失敗しました。" + e.ToString());
				this.textRenderer?.Dispose();
				throw;
			}
		}

		public SKBitmap DrawText(string drawstr, Color fontColor, bool keepCenter = false)
		{
			return DrawText(drawstr, CFontRenderer.DrawMode.Normal, fontColor, Color.White, null, Color.White, Color.White, 0, keepCenter);
		}

		public SKBitmap DrawText(string drawstr, Color fontColor, Color edgeColor, Color? secondEdgeColor, int edge_Ratio, bool keepCenter = false)
		{
			return DrawText(drawstr, CFontRenderer.DrawMode.Edge, fontColor, edgeColor, secondEdgeColor, Color.White, Color.White, edge_Ratio, keepCenter);
		}

		public SKBitmap DrawText(string drawstr, Color fontColor, Color gradationTopColor, Color gradataionBottomColor, int edge_Ratio, bool keepCenter = false)
		{
			return DrawText(drawstr, CFontRenderer.DrawMode.Gradation, fontColor, Color.White, null, gradationTopColor, gradataionBottomColor, edge_Ratio, keepCenter);
		}

		public SKBitmap DrawText(string drawstr, Color fontColor, Color edgeColor, Color? secondEdgeColor, Color gradationTopColor, Color gradataionBottomColor, int edge_Ratio, bool keepCenter = false)
		{
			return DrawText(drawstr, CFontRenderer.DrawMode.Edge | CFontRenderer.DrawMode.Gradation, fontColor, edgeColor, secondEdgeColor, gradationTopColor, gradataionBottomColor, edge_Ratio, keepCenter);
		}
		protected SKBitmap DrawText(string drawstr, CFontRenderer.DrawMode drawmode, Color fontColor, Color edgeColor, Color? secondEdgeColor, Color gradationTopColor, Color gradationBottomColor, int edge_Ratio, bool keepCenter = false)
		{
			//横書きに対してのCorrectionは廃止
			return this.textRenderer.DrawText(drawstr, drawmode, fontColor, edgeColor, secondEdgeColor, gradationTopColor, gradationBottomColor, edge_Ratio, keepCenter);
		}


		public SKBitmap DrawText_V(string drawstr, Color fontColor, bool keepCenter = false)
		{
			return DrawText_V(drawstr, CFontRenderer.DrawMode.Normal, fontColor, Color.White, null, Color.White, Color.White, 0, keepCenter);
		}

		public SKBitmap DrawText_V(string drawstr, Color fontColor, Color edgeColor, Color? secondEdgeColor, int edge_Ratio, bool keepCenter = false)
		{
			return DrawText_V(drawstr, CFontRenderer.DrawMode.Edge, fontColor, edgeColor, secondEdgeColor, Color.White, Color.White, edge_Ratio, keepCenter);
		}

		public SKBitmap DrawText_V(string drawstr, Color fontColor, Color gradationTopColor, Color gradataionBottomColor, int edge_Ratio, bool keepCenter = false)
		{
			return DrawText_V(drawstr, CFontRenderer.DrawMode.Gradation, fontColor, Color.White, null, gradationTopColor, gradataionBottomColor, edge_Ratio, keepCenter);
		}

		public SKBitmap DrawText_V(string drawstr, Color fontColor, Color edgeColor, Color? secondEdgeColor, Color gradationTopColor, Color gradataionBottomColor, int edge_Ratio, bool keepCenter = false)
		{
			return DrawText_V(drawstr, CFontRenderer.DrawMode.Edge | CFontRenderer.DrawMode.Gradation, fontColor, edgeColor, secondEdgeColor, gradationTopColor, gradataionBottomColor, edge_Ratio, keepCenter);
		}
		protected SKBitmap DrawText_V(string drawstr, CFontRenderer.DrawMode drawmode, Color fontColor, Color edgeColor, Color? secondEdgeColor, Color gradationTopColor, Color gradationBottomColor, int edge_Ratio, bool keepCenter = false)
		{
			if (string.IsNullOrEmpty(drawstr))
			{
				//nullか""だったら、1x1を返す
				return new SKBitmap(1, 1);
			}

			//グラデ(全体)にも対応したいですね？

			string[] strList = new string[drawstr.Length];
			for (int i = 0; i < drawstr.Length; i++)
				strList[i] = drawstr.Substring(i, 1);
			SKBitmap[] strImageList = new SKBitmap[drawstr.Length];

			//レンダリング,大きさ計測
			int nWidth = 0;
			int nHeight = 0;
			for (int i = 0; i < strImageList.Length; i++)
			{
				strImageList[i] = this.textRenderer.DrawText(strList[i], drawmode, fontColor, edgeColor, secondEdgeColor, gradationTopColor, gradationBottomColor, edge_Ratio, false);

				//回転する文字
				if(Rotate_Chara_List_Vertical.Contains(strList[i]))
				{
					using (var surface = new SKCanvas(strImageList[i]))
					{
						surface.RotateDegrees(90, strImageList[i].Width / 2, strImageList[i].Height / 2);
						surface.DrawBitmap(strImageList[i], 0, 0);
					}
				}

				nWidth = Math.Max(nWidth, strImageList[i].Width);
				nHeight += strImageList[i].Height - 25;
			}

			SKImageInfo skImageInfo = new SKImageInfo(nWidth, nHeight);

			using var skSurface = SKSurface.Create(skImageInfo);
			using var skCanvas = skSurface.Canvas;

			//1文字ずつ描画したやつを全体キャンバスに描画していく
			int nowHeightPos = 0;
			for (int i = 0; i < strImageList.Length; i++)
			{
				int Correction_X = 0, Correction_Y = 0;
				if (CorrectionX_Chara_List_Vertical != null && CorrectionX_Chara_List_Value_Vertical != null)
				{
					int Xindex = Array.IndexOf(CorrectionX_Chara_List_Vertical, strList[i]);
					if (-1 < Xindex && Xindex < CorrectionX_Chara_List_Value_Vertical.Length && CorrectionX_Chara_List_Vertical.Contains(strList[i]))
					{
						Correction_X = CorrectionX_Chara_List_Value_Vertical[Xindex];
					}
					else
					{
						if (-1 < Xindex && CorrectionX_Chara_List_Value_Vertical.Length <= Xindex && CorrectionX_Chara_List_Vertical.Contains(strList[i]))
						{
							Correction_X = CorrectionX_Chara_List_Value_Vertical[0];
						}
						else
						{
							Correction_X = 0;
						}
					}
				}

				if (CorrectionY_Chara_List_Vertical != null && CorrectionY_Chara_List_Value_Vertical != null)
				{
					int Yindex = Array.IndexOf(CorrectionY_Chara_List_Vertical, strList[i]);
					if (-1 < Yindex && Yindex < CorrectionY_Chara_List_Value_Vertical.Length && CorrectionY_Chara_List_Vertical.Contains(strList[i]))
					{
						Correction_Y = CorrectionY_Chara_List_Value_Vertical[Yindex];
					}
					else
					{
						if (-1 < Yindex && CorrectionY_Chara_List_Value_Vertical.Length <= Yindex && CorrectionY_Chara_List_Vertical.Contains(strList[i]))
						{
							Correction_Y = CorrectionY_Chara_List_Value_Vertical[0];
						}
						else
						{
							Correction_Y = 0;
						}
					}
				}
				skCanvas.DrawBitmap(strImageList[i], new SKPoint((nWidth - strImageList[i].Width) / 2 + Correction_X, nowHeightPos + Correction_Y));
				nowHeightPos += strImageList[i].Height - 25;
			}

			//1文字ずつ描画したやつの解放
			for (int i = 0; i < strImageList.Length; i++)
			{
				strImageList[i].Dispose();
			}

			
			SKImage image = skSurface.Snapshot();
			//返します
			return SKBitmap.FromImage(image);
		}

		public void Dispose()
		{
			this.textRenderer.Dispose();
		}

		private ITextRenderer textRenderer;
	}
}