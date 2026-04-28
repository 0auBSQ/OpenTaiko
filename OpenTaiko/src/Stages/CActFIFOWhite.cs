using FDK;

namespace OpenTaiko;

internal class CActFIFOWhite : CActFIFOBase {
	// メソッド

	public override void tフェードアウト開始(int? start = null, int? end = null, int? interval = null)
		=> base.tフェードアウト開始(start ?? 0, end ?? 100, interval ?? 3);
	public override void tフェードイン開始(int? start = null, int? end = null, int? interval = null)
		=> base.tフェードイン開始(start ?? 0, end ?? 100, interval ?? 3);

	// CActivity 実装

	public override int DrawSub() {
		// Size clientSize = CDTXMania.app.Window.ClientSize;	// #23510 2010.10.31 yyagi: delete as of no one use this any longer.
		if (OpenTaiko.Tx.Tile_Black != null) {
			OpenTaiko.Tx.Tile_Black.Opacity = (this.mode == EFIFOMode.FadeIn) ? (((100 - this.counter.CurrentValue) * 0xff) / 100) : ((this.counter.CurrentValue * 0xff) / 100);
			for (int i = 0; i <= (GameWindowSize.Width / OpenTaiko.Tx.Tile_Black.szTextureSize.Width); i++)        // #23510 2010.10.31 yyagi: change "clientSize.Width" to "640" to fix FIFO drawing size
			{
				for (int j = 0; j <= (GameWindowSize.Height / OpenTaiko.Tx.Tile_Black.szTextureSize.Height); j++)  // #23510 2010.10.31 yyagi: change "clientSize.Height" to "480" to fix FIFO drawing size
				{
					OpenTaiko.Tx.Tile_Black.t2D描画(i * OpenTaiko.Tx.Tile_Black.szTextureSize.Width, j * OpenTaiko.Tx.Tile_Black.szTextureSize.Height);
				}
			}
		}
		return 0;
	}
}
