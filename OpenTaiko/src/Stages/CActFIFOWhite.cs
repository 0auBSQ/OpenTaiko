using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using FDK;

namespace TJAPlayer3
{
	internal class CActFIFOWhite : CActivity
	{
		// メソッド

		public void tフェードアウト開始()
		{
			this.mode = EFIFOモード.フェードアウト;
			this.counter = new CCounter( 0, 100, 3, TJAPlayer3.Timer );
		}
		public void tフェードイン開始()
		{
			this.mode = EFIFOモード.フェードイン;
			this.counter = new CCounter( 0, 100, 3, TJAPlayer3.Timer );
		}
		public void tフェードイン完了()		// #25406 2011.6.9 yyagi
		{
			this.counter.CurrentValue = (int)this.counter.EndValue;
		}

		// CActivity 実装

		public override void DeActivate()
		{
			if( !base.IsDeActivated )
			{
				//CDTXMania.tテクスチャの解放( ref this.tx白タイル64x64 );
				base.DeActivate();
			}
		}
		public override void CreateManagedResource()
		{
			//this.tx白タイル64x64 = CDTXMania.tテクスチャの生成( CSkin.Path( @"Graphics\Tile white 64x64.png" ), false );
			base.CreateManagedResource();
		}

		public override void ReleaseUnmanagedResource()
		{
			base.ReleaseUnmanagedResource();
		}
		public override int Draw()
		{
			if( base.IsDeActivated || ( this.counter == null ) )
			{
				return 0;
			}
			this.counter.Tick();

			// Size clientSize = CDTXMania.app.Window.ClientSize;	// #23510 2010.10.31 yyagi: delete as of no one use this any longer.
			if (TJAPlayer3.Tx.Tile_Black != null)
			{
                TJAPlayer3.Tx.Tile_Black.Opacity = ( this.mode == EFIFOモード.フェードイン ) ? ( ( ( 100 - this.counter.CurrentValue ) * 0xff ) / 100 ) : ( ( this.counter.CurrentValue * 0xff ) / 100 );
				for (int i = 0; i <= (SampleFramework.GameWindowSize.Width / TJAPlayer3.Tx.Tile_Black.szTextureSize.Width); i++)		// #23510 2010.10.31 yyagi: change "clientSize.Width" to "640" to fix FIFO drawing size
				{
					for (int j = 0; j <= (SampleFramework.GameWindowSize.Height / TJAPlayer3.Tx.Tile_Black.szTextureSize.Height); j++)	// #23510 2010.10.31 yyagi: change "clientSize.Height" to "480" to fix FIFO drawing size
					{
                        TJAPlayer3.Tx.Tile_Black.t2D描画( i * TJAPlayer3.Tx.Tile_Black.szTextureSize.Width, j * TJAPlayer3.Tx.Tile_Black.szTextureSize.Height);
					}
				}
			}
			if( this.counter.CurrentValue != 100 )
			{
				return 0;
			}
			return 1;
		}


		// その他

		#region [ private ]
		//-----------------
		private CCounter counter;
		private EFIFOモード mode;
		//private CTexture tx白タイル64x64;
		//-----------------
		#endregion
	}
}
