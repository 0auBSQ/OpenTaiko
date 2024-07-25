using FDK;

namespace TJAPlayer3 {
	internal class CActFIFOResult : CActivity {
		// メソッド

		public void tフェードアウト開始() {
			this.mode = EFIFOモード.フェードアウト;
			this.counter = new CCounter(0, 100, 30, TJAPlayer3.Timer);
		}
		public void tフェードイン開始() {
			this.mode = EFIFOモード.フェードイン;
			this.counter = new CCounter(0, 300, 2, TJAPlayer3.Timer);
		}
		public void tフェードイン完了() {
			this.counter.CurrentValue = (int)counter.BeginValue;
		}


		// CActivity 実装

		public override void DeActivate() {
			if (!base.IsDeActivated) {
				//CDTXMania.tテクスチャの解放( ref this.tx黒タイル64x64 );
				base.DeActivate();
			}
		}
		public override void CreateManagedResource() {
			//this.tx黒タイル64x64 = CDTXMania.tテクスチャの生成( CSkin.Path( @"Graphics\Tile black 64x64.png" ), false );
			base.CreateManagedResource();
		}
		public override int Draw() {
			if (base.IsDeActivated || (this.counter == null)) {
				return 0;
			}
			this.counter.Tick();
			// Size clientSize = CDTXMania.app.Window.ClientSize;	// #23510 2010.10.31 yyagi: delete as of no one use this any longer.
			if (TJAPlayer3.Tx.Tile_Black != null) {
				if (this.mode == EFIFOモード.フェードイン) {
					if (counter.CurrentValue >= 200) {
						TJAPlayer3.Tx.Tile_Black.Opacity = (((100 - (this.counter.CurrentValue - 200)) * 0xff) / 100);
					} else {
						TJAPlayer3.Tx.Tile_Black.Opacity = 255;
					}
				} else {
					TJAPlayer3.Tx.Tile_Black.Opacity = (((this.counter.CurrentValue) * 0xff) / 100);
				}

				for (int i = 0; i <= (SampleFramework.GameWindowSize.Width / TJAPlayer3.Tx.Tile_Black.szTextureSize.Width); i++)      // #23510 2010.10.31 yyagi: change "clientSize.Width" to "640" to fix FIFO drawing size
				{
					for (int j = 0; j <= (SampleFramework.GameWindowSize.Height / TJAPlayer3.Tx.Tile_Black.szTextureSize.Height); j++) // #23510 2010.10.31 yyagi: change "clientSize.Height" to "480" to fix FIFO drawing size
					{
						TJAPlayer3.Tx.Tile_Black.t2D描画(i * TJAPlayer3.Tx.Tile_Black.szTextureSize.Width, j * TJAPlayer3.Tx.Tile_Black.szTextureSize.Height);
					}
				}
			}
			if (this.mode == EFIFOモード.フェードアウト) {
				if (this.counter.CurrentValue != 100) {
					return 0;
				}
			} else {
				if (this.counter.CurrentValue != 300) {
					return 0;
				}
			}
			return 1;
		}


		// その他

		#region [ private ]
		//-----------------
		private CCounter counter;
		private EFIFOモード mode;
		//private CTexture tx黒タイル64x64;
		//-----------------
		#endregion
	}
}
