using System;
using System.Collections.Generic;
using System.Diagnostics;
using SkiaSharp;

using Color = System.Drawing.Color;

namespace FDK
{
	/// <summary>
	/// 高速描画版のCFontRendererクラス。
	/// といっても、一度レンダリングした結果をキャッシュして使いまわしているだけ。
	/// </summary>
	public class CCachedFontRenderer : CFontRenderer
	{
		#region [ コンストラクタ ]
		public CCachedFontRenderer( string fontpath, int pt, CFontRenderer.FontStyle style )
		{
			Initialize( fontpath, pt, style );
		}
		public CCachedFontRenderer( string fontpath, int pt )
		{
			Initialize( fontpath, pt, CFontRenderer.FontStyle.Regular );
		}
		#endregion
		#region [ コンストラクタから呼ばれる初期化処理 ]
		protected new void Initialize( string fontpath, int pt, CFontRenderer.FontStyle style )
		{
			this.bDisposed_CCachedFontRenderer = false;
			this.listFontCache = new List<FontCache>();
			base.Initialize( fontpath, pt, style );
		}
		#endregion


		#region [ DrawTextのオーバーロード群 ]
		/// <summary>
		/// 文字列を描画したテクスチャを返す
		/// </summary>
		/// <param name="drawstr">描画文字列</param>
		/// <param name="fontColor">描画色</param>
		/// <param name="edgeColor">縁取色</param>
		/// <returns>描画済テクスチャ</returns>
		public new SKBitmap DrawText( string drawstr, Color fontColor, Color edgeColor, Color? secondEdgeColor, int edge_Ratio, bool keepCenter = false)
		{
			return DrawText( drawstr, DrawMode.Edge, fontColor, edgeColor, secondEdgeColor, Color.White, Color.White, edge_Ratio, keepCenter );
		}

		/// <summary>
		/// 文字列を描画したテクスチャを返す
		/// </summary>
		/// <param name="drawstr">描画文字列</param>
		/// <param name="fontColor">描画色</param>
		/// <param name="edgeColor">縁取色</param>
		/// <returns>描画済テクスチャ</returns>
		public SKBitmap DrawText( string drawstr, Color fontColor, Color edgeColor, Color? secondEdgeColor, DrawMode dMode, int edge_Ratio, bool keepCenter = false)
		{
			return DrawText( drawstr, dMode, fontColor, edgeColor, secondEdgeColor, Color.White, Color.White, edge_Ratio, keepCenter );
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
		public new SKBitmap DrawText( string drawstr, Color fontColor, Color edgeColor, Color? secondEdgeColor, Color gradationTopColor, Color gradataionBottomColor, int edge_Ratio, bool keepCenter = false )
		{
			return DrawText( drawstr, DrawMode.Edge | DrawMode.Gradation, fontColor, edgeColor, secondEdgeColor, gradationTopColor, gradataionBottomColor, edge_Ratio, keepCenter );
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
		public new SKBitmap DrawText_V( string drawstr, Color fontColor, Color edgeColor, Color? secondEdgeColor, int edge_Ratio, bool keepCenter = false )
		{
			return DrawText_V(drawstr, DrawMode.Edge, fontColor, edgeColor, secondEdgeColor, Color.Black, Color.Black, edge_Ratio, keepCenter);
		}

		#endregion

		protected new SKBitmap DrawText( string drawstr, DrawMode drawmode, Color fontColor, Color edgeColor, Color? secondEdgeColor, Color gradationTopColor, Color gradationBottomColor, int edge_Ratio, bool keepCenter = false )
		{
			#region [ 以前レンダリングしたことのある文字列/フォントか? (キャッシュにヒットするか?) ]
			int index = listFontCache.FindIndex(
				delegate( FontCache fontcache )
				{
					return (
						drawstr == fontcache.drawstr &&
						drawmode == fontcache.drawmode &&
						fontColor == fontcache.fontColor &&
						edgeColor == fontcache.edgeColor &&
						secondEdgeColor == fontcache.secondEdgeColor &&
						gradationTopColor == fontcache.gradationTopColor &&
						gradationBottomColor == fontcache.gradationBottomColor &&
						fontcache.Vertical == false &&
						fontcache.KeepCenter == keepCenter
					);
				}
			);
			#endregion
			if ( index < 0 )
			{
				// キャッシュにヒットせず。
				#region [ レンダリングして、キャッシュに登録 ]
				FontCache fc = new FontCache();
				fc.bmp = base.DrawText( drawstr, drawmode, fontColor, edgeColor, secondEdgeColor, gradationTopColor, gradationBottomColor, edge_Ratio, keepCenter);
				fc.drawstr = drawstr;
				fc.drawmode = drawmode;
				fc.fontColor = fontColor;
				fc.edgeColor = edgeColor;
				fc.secondEdgeColor = secondEdgeColor;
				fc.gradationTopColor = gradationTopColor;
				fc.gradationBottomColor = gradationBottomColor;
				fc.Vertical = false;
				fc.KeepCenter = keepCenter;
				listFontCache.Add( fc );
				Debug.WriteLine( drawstr + ": Cacheにヒットせず。(cachesize=" + listFontCache.Count + ")" );
				#endregion
				#region [ もしキャッシュがあふれたら、最も古いキャッシュを破棄する ]
				if ( listFontCache.Count > MAXCACHESIZE )
				{
					Debug.WriteLine( "Cache溢れ。" + listFontCache[ 0 ].drawstr + " を解放します。" );
					if ( listFontCache[ 0 ].bmp != null )
					{
						listFontCache[ 0 ].bmp.Dispose();
					}
					listFontCache.RemoveAt( 0 );
				}
				#endregion

				// 呼び出し元のDispose()でキャッシュもDispose()されないように、Clone()で返す。
				return listFontCache[ listFontCache.Count - 1 ].bmp.Copy();
			}
			else
			{
				Debug.WriteLine( drawstr + ": Cacheにヒット!! index=" + index );
				#region [ キャッシュにヒット。レンダリングは行わず、キャッシュ内のデータを返して終了。]
				// 呼び出し元のDispose()でキャッシュもDispose()されないように、Clone()で返す。
				return listFontCache[ index ].bmp.Copy();
				#endregion
			}
		}

		protected new SKBitmap DrawText_V(string drawstr, DrawMode drawmode, Color fontColor, Color edgeColor, Color? secondEdgeColor, Color gradationTopColor, Color gradationBottomColor, int edge_Ratio, bool keepCenter = false)
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
						secondEdgeColor == fontcache.secondEdgeColor &&
						gradationTopColor == fontcache.gradationTopColor &&
						gradationBottomColor == fontcache.gradationBottomColor &&
						fontcache.Vertical == true &&
						fontcache.KeepCenter == keepCenter
					);
				}
			);
			#endregion
			if ( index < 0 )
			{
				// キャッシュにヒットせず。
				#region [ レンダリングして、キャッシュに登録 ]
				FontCache fc = new FontCache();
				fc.bmp = base.DrawText_V(drawstr, drawmode, fontColor, edgeColor, secondEdgeColor, gradationTopColor, gradationBottomColor, edge_Ratio, keepCenter);
				fc.drawstr = drawstr;
				fc.fontColor = fontColor;
				fc.edgeColor = edgeColor;
				fc.secondEdgeColor = secondEdgeColor;
				fc.gradationTopColor = gradationTopColor;
				fc.gradationBottomColor = gradationBottomColor;
				fc.Vertical = true;
				fc.KeepCenter = keepCenter;
				listFontCache.Add( fc );
				Debug.WriteLine( drawstr + ": Cacheにヒットせず。(cachesize=" + listFontCache.Count + ")" );
				#endregion
				#region [ もしキャッシュがあふれたら、最も古いキャッシュを破棄する ]
				if ( listFontCache.Count > MAXCACHESIZE )
				{
					Debug.WriteLine( "Cache溢れ。" + listFontCache[ 0 ].drawstr + " を解放します。" );
					if ( listFontCache[ 0 ].bmp != null )
					{
						listFontCache[ 0 ].bmp.Dispose();
					}
					listFontCache.RemoveAt( 0 );
				}
				#endregion

				// 呼び出し元のDispose()でキャッシュもDispose()されないように、Clone()で返す。
				return listFontCache[ listFontCache.Count - 1 ].bmp.Copy();
			}
			else
			{
				Debug.WriteLine( drawstr + ": Cacheにヒット!! index=" + index );
				#region [ キャッシュにヒット。レンダリングは行わず、キャッシュ内のデータを返して終了。]
				// 呼び出し元のDispose()でキャッシュもDispose()されないように、Clone()で返す。
				return listFontCache[ index ].bmp.Copy();
				#endregion
			}
		}

		#region [ IDisposable 実装 ]
		//-----------------
		public new void Dispose()
		{
			if (!this.bDisposed_CCachedFontRenderer)
			{
				if (listFontCache != null)
				{
					//Debug.WriteLine( "Disposing CCachedFontRenderer()" );
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
				this.bDisposed_CCachedFontRenderer = true;
			}
			base.Dispose();
		}
		//-----------------
		#endregion

		#region [ private ]
		//-----------------
		/// <summary>
		/// キャッシュ容量
		/// </summary>
		private const int MAXCACHESIZE = 256;

		private struct FontCache
		{
			// public Font font;
			public string drawstr;
			public DrawMode drawmode;
			public Color fontColor;
			public Color edgeColor;
			public Color? secondEdgeColor;
			public Color gradationTopColor;
			public Color gradationBottomColor;
			public SKBitmap bmp;
			public bool Vertical;
			public bool KeepCenter;
		}
		private List<FontCache> listFontCache;

		protected bool bDisposed_CCachedFontRenderer;
		//-----------------
		#endregion
	}
}