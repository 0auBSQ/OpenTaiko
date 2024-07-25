using System.Drawing;
using FDK;

namespace TJAPlayer3 {
	class GoGoSplash : CActivity {
		public GoGoSplash() {
			this.IsDeActivated = true;
		}

		public override void Activate() {
			Splash = new CCounter();
			base.Activate();
		}

		public override void DeActivate() {
			base.DeActivate();
		}

		/// <summary>
		/// ゴーゴースプラッシュの描画処理です。
		/// SkinCofigで本数を変更することができます。
		/// </summary>
		/// <returns></returns>
		public override int Draw() {
			if (Splash == null || TJAPlayer3.ConfigIni.SimpleMode) return base.Draw();
			Splash.Tick();
			if (Splash.IsEnded) {
				Splash.CurrentValue = 0;
				Splash.Stop();
			}
			if (Splash.IsTicked) {
				for (int i = 0; i < TJAPlayer3.Skin.Game_Effect_GoGoSplash_X.Length; i++) {
					if (i > TJAPlayer3.Skin.Game_Effect_GoGoSplash_Y.Length) break;
					// Yの配列がiよりも小さかったらそこでキャンセルする。
					if (TJAPlayer3.Skin.Game_Effect_GoGoSplash_Rotate && TJAPlayer3.Tx.Effects_GoGoSplash != null) {
						// Switch文を使いたかったが、定数じゃないから使えねぇ!!!!
						if (i == 0) {
							TJAPlayer3.Tx.Effects_GoGoSplash.fZ軸中心回転 = -0.2792526803190927f;
						} else if (i == 1) {
							TJAPlayer3.Tx.Effects_GoGoSplash.fZ軸中心回転 = -0.13962634015954636f;
						} else if (i == TJAPlayer3.Skin.Game_Effect_GoGoSplash_X.Length - 2) {
							TJAPlayer3.Tx.Effects_GoGoSplash.fZ軸中心回転 = 0.13962634015954636f;
						} else if (i == TJAPlayer3.Skin.Game_Effect_GoGoSplash_X.Length - 1) {
							TJAPlayer3.Tx.Effects_GoGoSplash.fZ軸中心回転 = 0.2792526803190927f;
						} else {
							TJAPlayer3.Tx.Effects_GoGoSplash.fZ軸中心回転 = 0.0f;
						}
					}
					TJAPlayer3.Tx.Effects_GoGoSplash?.t2D拡大率考慮下中心基準描画(TJAPlayer3.Skin.Game_Effect_GoGoSplash_X[i], TJAPlayer3.Skin.Game_Effect_GoGoSplash_Y[i], new Rectangle(TJAPlayer3.Skin.Game_Effect_GoGoSplash[0] * Splash.CurrentValue, 0, TJAPlayer3.Skin.Game_Effect_GoGoSplash[0], TJAPlayer3.Skin.Game_Effect_GoGoSplash[1]));
				}
			}
			return base.Draw();
		}

		public void StartSplash() {
			Splash = new CCounter(0, TJAPlayer3.Skin.Game_Effect_GoGoSplash[2] - 1, TJAPlayer3.Skin.Game_Effect_GoGoSplash_Timer, TJAPlayer3.Timer);
		}

		private CCounter Splash;
	}
}
