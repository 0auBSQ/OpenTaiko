using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Diagnostics;

namespace TJAPlayer3
{
	/// <summary>
	/// 高速描画版のCPrivateFontクラス。
	/// といっても、一度レンダリングした結果をキャッシュして使いまわしているだけ。
	/// </summary>
	public class CPrivateFastFont : CPrivateFont
	{
		/// <summary>
		/// キャッシュ容量
		/// </summary>
		private const int MAXCACHESIZE = 128;

		private struct FontCache
		{
			// public Font font;
			public int edgePt;
			public string drawstr;
			public DrawMode drawmode;
			public Color fontColor;
			public Color edgeColor;
			public Color gradationTopColor;
			public Color gradationBottomColor;
			public Bitmap bmp;
			public Rectangle rectStrings;
			public Point ptOrigin;
		}
		private List<FontCache> listFontCache;


		#region [ コンストラクタ ]
		public CPrivateFastFont(FontFamily fontfamily, int pt, FontStyle style)
		{
			Initialize(null, fontfamily, pt, style);
		}
		public CPrivateFastFont(FontFamily fontfamily, int pt)
		{
			Initialize(null, fontfamily, pt, FontStyle.Regular);
		}
		public CPrivateFastFont(string fontpath, int pt, FontStyle style)
		{
			Initialize(fontpath, null, pt, style);
		}
		public CPrivateFastFont(string fontpath, int pt)
		{
			Initialize(fontpath, null, pt, FontStyle.Regular);
		}
		public CPrivateFastFont()
		{
			throw new ArgumentException("CPrivateFastFont: 引数があるコンストラクタを使用してください。");
		}
		#endregion
		#region [ コンストラクタから呼ばれる初期化処理 ]
		protected new void Initialize(string fontpath, FontFamily fontfamily, int pt, FontStyle style)
		{
			this.bDispose完了済み_CPrivateFastFont = false;
			this.listFontCache = new List<FontCache>();
			base.Initialize(fontpath, fontfamily, pt, style);
		}
		#endregion


		#region [ DrawPrivateFontのオーバーロード群 ]
		/// <summary>
		/// 文字列を描画したテクスチャを返す
		/// </summary>
		/// <param name="drawstr">描画文字列</param>
		/// <param name="fontColor">描画色</param>
		/// <returns>描画済テクスチャ</returns>
		public new Bitmap DrawPrivateFont(string drawstr, Color fontColor)
		{
			return DrawPrivateFont(drawstr, DrawMode.Normal, fontColor, Color.White, Color.White, Color.White);
		}

		public new Bitmap DrawPrivateFont(string drawstr, Color fontColor, Color edgeColor, int edgePt)
		{
			return DrawPrivateFont_E(drawstr, DrawMode.Edge, fontColor, edgeColor, Color.White, Color.White, edgePt);
		}

		/// <summary>
		/// 文字列を描画したテクスチャを返す
		/// </summary>
		/// <param name="drawstr">描画文字列</param>
		/// <param name="fontColor">描画色</param>
		/// <param name="edgeColor">縁取色</param>
		/// <returns>描画済テクスチャ</returns>
		public new Bitmap DrawPrivateFont(string drawstr, Color fontColor, Color edgeColor)
		{
			return DrawPrivateFont(drawstr, DrawMode.Edge, fontColor, edgeColor, Color.White, Color.White);
		}

		/// <summary>
		/// 文字列を描画したテクスチャを返す
		/// </summary>
		/// <param name="drawstr">描画文字列</param>
		/// <param name="fontColor">描画色</param>
		/// <param name="edgeColor">縁取色</param>
		/// <returns>描画済テクスチャ</returns>
		public Bitmap DrawPrivateFont(string drawstr, Color fontColor, Color edgeColor, DrawMode dMode)
		{
			return DrawPrivateFont(drawstr, dMode, fontColor, edgeColor, Color.White, Color.White);
		}
		/// <summary>
		/// 文字列を描画したテクスチャを返す
		/// </summary>
		/// <param name="drawstr">描画文字列</param>
		/// <param name="fontColor">描画色</param>
		/// <param name="gradationTopColor">グラデーション 上側の色</param>
		/// <param name="gradationBottomColor">グラデーション 下側の色</param>
		/// <returns>描画済テクスチャ</returns>
		//public new Bitmap DrawPrivateFont( string drawstr, Color fontColor, Color gradationTopColor, Color gradataionBottomColor )
		//{
		//    return DrawPrivateFont( drawstr, DrawMode.Gradation, fontColor, Color.White, gradationTopColor, gradataionBottomColor );
		//}

		/// <summary>
		/// 文字列を描画したテクスチャを返す
		/// </summary>
		/// <param name="drawstr">描画文字列</param>
		/// <param name="fontColor">描画色</param>
		/// <param name="edgeColor">縁取色</param>
		/// <param name="gradationTopColor">グラデーション 上側の色</param>
		/// <param name="gradationBottomColor">グラデーション 下側の色</param>
		/// <returns>描画済テクスチャ</returns>
		public new Bitmap DrawPrivateFont(string drawstr, Color fontColor, Color edgeColor, Color gradationTopColor, Color gradataionBottomColor)
		{
			return DrawPrivateFont(drawstr, DrawMode.Edge | DrawMode.Gradation, fontColor, edgeColor, gradationTopColor, gradataionBottomColor);
		}

		/// <summary>
		/// 文字列を描画したテクスチャを返す
		/// </summary>
		/// <param name="drawstr">描画文字列</param>
		/// <param name="fontColor">描画色</param>
		/// <param name="edgeColor">縁取色</param>
		/// <param name="gradationTopColor">グラデーション 上側の色</param>
		/// <param name="gradationBottomColor">グラデーション 下側の色</param>
		/// <returns>描画済テクスチャ</returns>
		public Bitmap DrawPrivateFont(string drawstr, Color fontColor, Color edgeColor, bool bVertical)
		{
			return DrawPrivateFont_V(drawstr, fontColor, edgeColor, bVertical);
		}

#if こちらは使わない // (Bitmapではなく、CTextureを返す版)
		/// <summary>
		/// 文字列を描画したテクスチャを返す
		/// </summary>
		/// <param name="drawstr">描画文字列</param>
		/// <param name="fontColor">描画色</param>
		/// <returns>描画済テクスチャ</returns>
		public CTexture DrawPrivateFont( string drawstr, Color fontColor )
		{
			Bitmap bmp = DrawPrivateFont( drawstr, DrawMode.Normal, fontColor, Color.White, Color.White, Color.White );
			return CDTXMania.tテクスチャの生成( bmp, false );
		}

		/// <summary>
		/// 文字列を描画したテクスチャを返す
		/// </summary>
		/// <param name="drawstr">描画文字列</param>
		/// <param name="fontColor">描画色</param>
		/// <param name="edgeColor">縁取色</param>
		/// <returns>描画済テクスチャ</returns>
		public CTexture DrawPrivateFont( string drawstr, Color fontColor, Color edgeColor )
		{
			Bitmap bmp = DrawPrivateFont( drawstr, DrawMode.Edge, fontColor, edgeColor, Color.White, Color.White );
			return CDTXMania.tテクスチャの生成( bmp, false );
		}

		/// <summary>
		/// 文字列を描画したテクスチャを返す
		/// </summary>
		/// <param name="drawstr">描画文字列</param>
		/// <param name="fontColor">描画色</param>
		/// <param name="gradationTopColor">グラデーション 上側の色</param>
		/// <param name="gradationBottomColor">グラデーション 下側の色</param>
		/// <returns>描画済テクスチャ</returns>
		//public CTexture DrawPrivateFont( string drawstr, Color fontColor, Color gradationTopColor, Color gradataionBottomColor )
		//{
		//    Bitmap bmp = DrawPrivateFont( drawstr, DrawMode.Gradation, fontColor, Color.White, gradationTopColor, gradataionBottomColor );
		//	  return CDTXMania.tテクスチャの生成( bmp, false );
		//}

		/// <summary>
		/// 文字列を描画したテクスチャを返す
		/// </summary>
		/// <param name="drawstr">描画文字列</param>
		/// <param name="fontColor">描画色</param>
		/// <param name="edgeColor">縁取色</param>
		/// <param name="gradationTopColor">グラデーション 上側の色</param>
		/// <param name="gradationBottomColor">グラデーション 下側の色</param>
		/// <returns>描画済テクスチャ</returns>
		public CTexture DrawPrivateFont( string drawstr, Color fontColor, Color edgeColor,  Color gradationTopColor, Color gradataionBottomColor )
		{
			Bitmap bmp = DrawPrivateFont( drawstr, DrawMode.Edge | DrawMode.Gradation, fontColor, edgeColor, gradationTopColor, gradataionBottomColor );
			return CDTXMania.tテクスチャの生成( bmp, false );
		}
#endif
		#endregion

		protected new Bitmap DrawPrivateFont_E(string drawstr, DrawMode drawmode, Color fontColor, Color edgeColor, Color gradationTopColor, Color gradationBottomColor, int edgePt)
		{
			#region [ 以前レンダリングしたことのある文字列/フォントか? (キャッシュにヒットするか?) ]
			int index = listFontCache.FindIndex(
				delegate (FontCache fontcache)
				{
					return (
						drawstr == fontcache.drawstr &&
						drawmode == fontcache.drawmode &&
						fontColor == fontcache.fontColor &&
						edgeColor == fontcache.edgeColor &&
						gradationTopColor == fontcache.gradationTopColor &&
						gradationBottomColor == fontcache.gradationBottomColor &&
						edgePt == fontcache.edgePt
					// _font == fontcache.font
					);
				}
			);
			#endregion
			if (index < 0)
			{
				// キャッシュにヒットせず。
				#region [ レンダリングして、キャッシュに登録 ]
				FontCache fc = new FontCache();
				fc.bmp = base.DrawPrivateFont_E(drawstr, drawmode, fontColor, edgeColor, gradationTopColor, gradationBottomColor, edgePt);
				fc.drawstr = drawstr;
				fc.drawmode = drawmode;
				fc.fontColor = fontColor;
				fc.edgeColor = edgeColor;
				fc.gradationTopColor = gradationTopColor;
				fc.gradationBottomColor = gradationBottomColor;
				fc.edgePt = edgePt;
				fc.rectStrings = RectStrings;
				fc.ptOrigin = PtOrigin;
				listFontCache.Add(fc);
				Debug.WriteLine(drawstr + ": Cacheにヒットせず。(cachesize=" + listFontCache.Count + ")");
				#endregion
				#region [ もしキャッシュがあふれたら、最も古いキャッシュを破棄する ]
				if (listFontCache.Count > MAXCACHESIZE)
				{
					Debug.WriteLine("Cache溢れ。" + listFontCache[0].drawstr + " を解放します。");
					if (listFontCache[0].bmp != null)
					{
						listFontCache[0].bmp.Dispose();
					}
					listFontCache.RemoveAt(0);
				}
				#endregion

				// 呼び出し元のDispose()でキャッシュもDispose()されないように、Clone()で返す。
				return (Bitmap)listFontCache[listFontCache.Count - 1].bmp.Clone();
			}
			else
			{
				Debug.WriteLine(drawstr + ": Cacheにヒット!! index=" + index);
				#region [ キャッシュにヒット。レンダリングは行わず、キャッシュ内のデータを返して終了。]
				RectStrings = listFontCache[index].rectStrings;
				PtOrigin = listFontCache[index].ptOrigin;
				// 呼び出し元のDispose()でキャッシュもDispose()されないように、Clone()で返す。
				return (Bitmap)listFontCache[index].bmp.Clone();
				#endregion
			}
		}

		protected new Bitmap DrawPrivateFont(string drawstr, DrawMode drawmode, Color fontColor, Color edgeColor, Color gradationTopColor, Color gradationBottomColor)
		{
			#region [ 以前レンダリングしたことのある文字列/フォントか? (キャッシュにヒットするか?) ]
			int index = listFontCache.FindIndex(
				delegate (FontCache fontcache)
				{
					return (
						drawstr == fontcache.drawstr &&
						drawmode == fontcache.drawmode &&
						fontColor == fontcache.fontColor &&
						edgeColor == fontcache.edgeColor &&
						gradationTopColor == fontcache.gradationTopColor &&
						gradationBottomColor == fontcache.gradationBottomColor
					// _font == fontcache.font
					);
				}
			);
			#endregion
			if (index < 0)
			{
				// キャッシュにヒットせず。
				#region [ レンダリングして、キャッシュに登録 ]
				FontCache fc = new FontCache();
				fc.bmp = base.DrawPrivateFont(drawstr, drawmode, fontColor, edgeColor, gradationTopColor, gradationBottomColor);
				fc.drawstr = drawstr;
				fc.drawmode = drawmode;
				fc.fontColor = fontColor;
				fc.edgeColor = edgeColor;
				fc.gradationTopColor = gradationTopColor;
				fc.gradationBottomColor = gradationBottomColor;
				fc.rectStrings = RectStrings;
				fc.ptOrigin = PtOrigin;
				listFontCache.Add(fc);
				Debug.WriteLine(drawstr + ": Cacheにヒットせず。(cachesize=" + listFontCache.Count + ")");
				#endregion
				#region [ もしキャッシュがあふれたら、最も古いキャッシュを破棄する ]
				if (listFontCache.Count > MAXCACHESIZE)
				{
					Debug.WriteLine("Cache溢れ。" + listFontCache[0].drawstr + " を解放します。");
					if (listFontCache[0].bmp != null)
					{
						listFontCache[0].bmp.Dispose();
					}
					listFontCache.RemoveAt(0);
				}
				#endregion

				// 呼び出し元のDispose()でキャッシュもDispose()されないように、Clone()で返す。
				return (Bitmap)listFontCache[listFontCache.Count - 1].bmp.Clone();
			}
			else
			{
				Debug.WriteLine(drawstr + ": Cacheにヒット!! index=" + index);
				#region [ キャッシュにヒット。レンダリングは行わず、キャッシュ内のデータを返して終了。]
				RectStrings = listFontCache[index].rectStrings;
				PtOrigin = listFontCache[index].ptOrigin;
				// 呼び出し元のDispose()でキャッシュもDispose()されないように、Clone()で返す。
				return (Bitmap)listFontCache[index].bmp.Clone();
				#endregion
			}
		}

		protected new Bitmap DrawPrivateFont_V(string drawstr, Color fontColor, Color edgeColor, bool bVertical)
		{
			#region [ 以前レンダリングしたことのある文字列/フォントか? (キャッシュにヒットするか?) ]
			int index = listFontCache.FindIndex(
				delegate (FontCache fontcache)
				{
					return (
						drawstr == fontcache.drawstr &&
						fontColor == fontcache.fontColor &&
						edgeColor == fontcache.edgeColor &&
						bVertical == true
					// _font == fontcache.font
					);
				}
			);
			#endregion
			if (index < 0)
			{
				// キャッシュにヒットせず。
				#region [ レンダリングして、キャッシュに登録 ]
				FontCache fc = new FontCache();
				fc.bmp = base.DrawPrivateFont_V(drawstr, fontColor, edgeColor, true);
				fc.drawstr = drawstr;
				fc.fontColor = fontColor;
				fc.edgeColor = edgeColor;
				fc.rectStrings = RectStrings;
				fc.ptOrigin = PtOrigin;
				listFontCache.Add(fc);
				Debug.WriteLine(drawstr + ": Cacheにヒットせず。(cachesize=" + listFontCache.Count + ")");
				#endregion
				#region [ もしキャッシュがあふれたら、最も古いキャッシュを破棄する ]
				if (listFontCache.Count > MAXCACHESIZE)
				{
					Debug.WriteLine("Cache溢れ。" + listFontCache[0].drawstr + " を解放します。");
					if (listFontCache[0].bmp != null)
					{
						listFontCache[0].bmp.Dispose();
					}
					listFontCache.RemoveAt(0);
				}
				#endregion

				// 呼び出し元のDispose()でキャッシュもDispose()されないように、Clone()で返す。
				return (Bitmap)listFontCache[listFontCache.Count - 1].bmp.Clone();
			}
			else
			{
				Debug.WriteLine(drawstr + ": Cacheにヒット!! index=" + index);
				#region [ キャッシュにヒット。レンダリングは行わず、キャッシュ内のデータを返して終了。]
				RectStrings = listFontCache[index].rectStrings;
				PtOrigin = listFontCache[index].ptOrigin;
				// 呼び出し元のDispose()でキャッシュもDispose()されないように、Clone()で返す。
				return (Bitmap)listFontCache[index].bmp.Clone();
				#endregion
			}
		}

		#region [ IDisposable 実装 ]
		//-----------------
		public new void Dispose()
		{
			if (!this.bDispose完了済み_CPrivateFastFont)
			{
				if (listFontCache != null)
				{
					//Debug.WriteLine( "Disposing CPrivateFastFont()" );
					#region [ キャッシュしている画像を破棄する ]
					foreach (FontCache bc in listFontCache)

					{
						if (bc.bmp != null)
						{
							bc.bmp.Dispose();
						}
					}
					#endregion
					listFontCache.Clear();
					listFontCache = null;
				}
				this.bDispose完了済み_CPrivateFastFont = true;
			}
			base.Dispose();
		}
		//-----------------
		#endregion

		#region [ private ]
		//-----------------
		protected bool bDispose完了済み_CPrivateFastFont;
		//-----------------
		#endregion
	}
}
