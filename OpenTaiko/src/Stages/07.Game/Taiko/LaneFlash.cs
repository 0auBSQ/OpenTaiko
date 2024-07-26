using FDK;

namespace TJAPlayer3 {
	/// <summary>
	/// レーンフラッシュのクラス。
	/// </summary>
	public class LaneFlash : CActivity {

		public LaneFlash(ref CTexture texture, int player) {
			Texture = texture;
			Player = player;
			base.IsDeActivated = true;
		}

		public void Start() {
			Counter = new CCounter(0, 100, 1, TJAPlayer3.Timer);
		}

		public override void Activate() {
			Counter = new CCounter();
			base.Activate();
		}

		public override void DeActivate() {
			Counter = null;
			base.DeActivate();
		}

		public override int Draw() {
			if (Texture == null || Counter == null) return base.Draw();
			if (!Counter.IsStoped) {
				int x;
				int y;

				if (TJAPlayer3.ConfigIni.nPlayerCount == 5) {
					x = TJAPlayer3.Skin.Game_Lane_5P[0] + (TJAPlayer3.Skin.Game_UIMove_5P[0] * Player);
					y = TJAPlayer3.Skin.Game_Lane_5P[1] + (TJAPlayer3.Skin.Game_UIMove_5P[1] * Player);
				} else if (TJAPlayer3.ConfigIni.nPlayerCount == 4 || TJAPlayer3.ConfigIni.nPlayerCount == 3) {
					x = TJAPlayer3.Skin.Game_Lane_4P[0] + (TJAPlayer3.Skin.Game_UIMove_4P[0] * Player);
					y = TJAPlayer3.Skin.Game_Lane_4P[1] + (TJAPlayer3.Skin.Game_UIMove_4P[1] * Player);
				} else {
					x = TJAPlayer3.Skin.Game_Lane_X[Player];
					y = TJAPlayer3.Skin.Game_Lane_Y[Player];
				}

				Counter.Tick();
				if (Counter.IsEnded) Counter.Stop();
				int opacity = (((150 - Counter.CurrentValue) * 255) / 100);
				Texture.Opacity = opacity;
				Texture.t2D描画(x, y);
			}
			return base.Draw();
		}

		private CTexture Texture;
		private CCounter Counter;
		private int Player;
	}
}
