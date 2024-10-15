﻿using FDK;

namespace OpenTaiko {
	internal class CActFIFOWhite : CActivity {
		// メソッド

		public void tフェードアウト開始() {
			this.mode = EFIFOMode.FadeOut;
			this.counter = new CCounter(0, 100, 3, OpenTaiko.Timer);
		}
		public void tフェードイン開始() {
			this.mode = EFIFOMode.FadeIn;
			this.counter = new CCounter(0, 100, 3, OpenTaiko.Timer);
		}
		public void tフェードイン完了()     // #25406 2011.6.9 yyagi
		{
			this.counter.CurrentValue = (int)this.counter.EndValue;
		}

		// CActivity 実装

		public override void DeActivate() {
			if (!base.IsDeActivated) {
				//CDTXMania.tテクスチャの解放( ref this.tx白タイル64x64 );
				base.DeActivate();
			}
		}
		public override void CreateManagedResource() {
			//this.tx白タイル64x64 = CDTXMania.tテクスチャの生成( CSkin.Path( @"Graphics\Tile white 64x64.png" ), false );
			base.CreateManagedResource();
		}

		public override void ReleaseUnmanagedResource() {
			base.ReleaseUnmanagedResource();
		}
		public override int Draw() {
			if (base.IsDeActivated || (this.counter == null)) {
				return 0;
			}
			this.counter.Tick();

			// Size clientSize = CDTXMania.app.Window.ClientSize;	// #23510 2010.10.31 yyagi: delete as of no one use this any longer.
			if (OpenTaiko.Tx.Tile_Black != null) {
				OpenTaiko.Tx.Tile_Black.Opacity = (this.mode == EFIFOMode.FadeIn) ? (((100 - this.counter.CurrentValue) * 0xff) / 100) : ((this.counter.CurrentValue * 0xff) / 100);
				for (int i = 0; i <= (SampleFramework.GameWindowSize.Width / OpenTaiko.Tx.Tile_Black.szTextureSize.Width); i++)        // #23510 2010.10.31 yyagi: change "clientSize.Width" to "640" to fix FIFO drawing size
				{
					for (int j = 0; j <= (SampleFramework.GameWindowSize.Height / OpenTaiko.Tx.Tile_Black.szTextureSize.Height); j++)  // #23510 2010.10.31 yyagi: change "clientSize.Height" to "480" to fix FIFO drawing size
					{
						OpenTaiko.Tx.Tile_Black.t2D描画(i * OpenTaiko.Tx.Tile_Black.szTextureSize.Width, j * OpenTaiko.Tx.Tile_Black.szTextureSize.Height);
					}
				}
			}
			if (this.counter.CurrentValue != 100) {
				return 0;
			}
			return 1;
		}


		// その他

		#region [ private ]
		//-----------------
		private CCounter counter;
		private EFIFOMode mode;
		//private CTexture tx白タイル64x64;
		//-----------------
		#endregion
	}
}
