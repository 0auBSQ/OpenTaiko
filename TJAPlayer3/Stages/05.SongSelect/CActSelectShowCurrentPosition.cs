using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Diagnostics;
using System.IO;
using FDK;

namespace TJAPlayer3
{
	internal class CActSelectShowCurrentPosition : CActivity
	{
		// メソッド

		public CActSelectShowCurrentPosition()
		{
			base.b活性化してない = true;
		}

		// CActivity 実装

		public override void On活性化()
		{
			if ( this.b活性化してる )
				return;

			base.On活性化();
		}
		public override void On非活性化()
		{
			base.On非活性化();
		}
		public override void OnManagedリソースの作成()
		{
			if ( !base.b活性化してない )
			{
				string pathScrollBar = CSkin.Path( @"Graphics\5_scrollbar.png" );
				string pathScrollPosition = CSkin.Path( @"Graphics\5_scrollbar.png" );
				if ( File.Exists( pathScrollBar ) )
				{
					this.txScrollBar = TJAPlayer3.tテクスチャの生成( pathScrollBar, false );
				}
				if ( File.Exists( pathScrollPosition ) )
				{
					this.txScrollPosition = TJAPlayer3.tテクスチャの生成( pathScrollPosition, false );
				}
				base.OnManagedリソースの作成();
			}
		}
		public override void OnManagedリソースの解放()
		{
			if ( !base.b活性化してない )
			{
				TJAPlayer3.t安全にDisposeする( ref this.txScrollBar );
				TJAPlayer3.t安全にDisposeする( ref this.txScrollPosition );

				base.OnManagedリソースの解放();
			}
		}
		public override int On進行描画()
		{
			if ( this.txScrollBar != null )
			{
			    #region [ スクロールバーの描画 #27648 ]
                //this.txScrollBar.t2D描画( CDTXMania.app.Device, (int)(1280 - ((429.0f / 100.0f ) * CDTXMania.stage選曲.ct登場時アニメ用共通.n現在の値)), 164, new Rectangle( 0, 0, 352, 26 ) ); //移動後のxは851
			    #endregion
			    #region [ スクロール地点の描画 (計算はCActSelect曲リストで行う。スクロール位置と選曲項目の同期のため。)#27648 ]
				int py = TJAPlayer3.stage選曲.nスクロールバー相対y座標;
				if( py <= 336 && py >= 0 )
				{
					//this.txScrollBar.t2D描画( CDTXMania.app.Device, (int)( 1280 - 4 - (( 424.0f / 100.0f ) * CDTXMania.stage選曲.ct登場時アニメ用共通.n現在の値 ) ) + py, 164, new Rectangle( 352, 0, 26, 26 ) );//856
				}
			    #endregion
            }

			return 0;
		}


		// その他

		#region [ private ]
		//-----------------
		private CTexture txScrollPosition;
		private CTexture txScrollBar;
		//-----------------
		#endregion
	}
}
