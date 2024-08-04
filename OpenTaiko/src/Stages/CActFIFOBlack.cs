using FDK;

namespace OpenTaiko {
	internal class CActFIFOBlack : CActivity {
		// メソッド

		public void tフェードアウト開始(int start = 0, int end = 100, int interval = 5) {
			this.mode = EFIFOモード.フェードアウト;
			this.counter = new CCounter(start, end, interval, OpenTaiko.Timer);
		}
		public void tフェードイン開始(int start = 0, int end = 100, int interval = 5) {
			this.mode = EFIFOモード.フェードイン;
			this.counter = new CCounter(start, end, interval, OpenTaiko.Timer);
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
			if (OpenTaiko.Tx.Tile_Black != null) {
				OpenTaiko.Tx.Tile_Black.Opacity = (this.mode == EFIFOモード.フェードイン) ? (((100 - this.counter.CurrentValue) * 0xff) / 100) : ((this.counter.CurrentValue * 0xff) / 100);
				for (int i = 0; i <= (SampleFramework.GameWindowSize.Width / OpenTaiko.Tx.Tile_Black.szTextureSize.Width); i++)        // #23510 2010.10.31 yyagi: change "clientSize.Width" to "640" to fix FIFO drawing size
				{
					for (int j = 0; j <= (SampleFramework.GameWindowSize.Height / OpenTaiko.Tx.Tile_Black.szTextureSize.Height); j++)  // #23510 2010.10.31 yyagi: change "clientSize.Height" to "480" to fix FIFO drawing size
					{
						OpenTaiko.Tx.Tile_Black.t2D描画(i * OpenTaiko.Tx.Tile_Black.szTextureSize.Width, j * OpenTaiko.Tx.Tile_Black.szTextureSize.Height);
					}
				}
			}
			if (this.counter.CurrentValue != this.counter.EndValue) {
				return 0;
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
