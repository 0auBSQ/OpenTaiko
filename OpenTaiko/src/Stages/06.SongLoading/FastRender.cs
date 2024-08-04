using FDK;

namespace OpenTaiko {
	class FastRender {
		public FastRender() {

		}

		public void Render() {
			/*for (int i = 0; i < 5; i++)
            {
                for (int k = 0; k < TJAPlayer3.Skin.Game_Dancer_Ptn; k++)
                {
                    NullCheckAndRender(ref TJAPlayer3.Tx.Dancer[i][k]);
                }
            }*/

			NullCheckAndRender(ref OpenTaiko.Tx.Effects_GoGoSplash);

			//NullCheckAndRender(ref TJAPlayer3.Tx.PuchiChara);

		}

		private void NullCheckAndRender(ref CTexture tx) {
			if (tx == null) return;
			tx.Opacity = 0;
			tx.t2D描画(0, 0);
			tx.Opacity = 255;
		}
	}
}
