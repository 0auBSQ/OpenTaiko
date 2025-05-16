using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenTaiko {
	public static class HBlackBackdrop {
		public static void Draw(int opacity = 255) {
			if (OpenTaiko.Tx.Tile_Black != null) {
				OpenTaiko.Tx.Tile_Black.Opacity = opacity;
				for (int i = 0; i <= FDK.GameWindowSize.Width; i += OpenTaiko.Tx.Tile_Black.szTextureSize.Width) {
					for (int j = 0; j <= FDK.GameWindowSize.Height; j += OpenTaiko.Tx.Tile_Black.szTextureSize.Height) {
						OpenTaiko.Tx.Tile_Black.t2D描画(i, j);
					}
				}
				OpenTaiko.Tx.Tile_Black.Opacity = 255;
			}
		}
	}
}
