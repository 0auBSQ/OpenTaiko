using System.Drawing;
using FDK;

namespace OpenTaiko {
	class CActSelect段位挑戦選択画面 : CActivity {
		public override void Activate() {
			ctBarIn = new CCounter();
			ctBarOut = new CCounter();
			ctBarOut.CurrentValue = 255;
			OpenTaiko.stage段位選択.bDifficultyIn = false;
			bOption = false;

			base.Activate();
		}

		public override void DeActivate() {
			base.DeActivate();
		}

		public override void CreateManagedResource() {
			base.CreateManagedResource();
		}

		public override void ReleaseManagedResource() {
			base.ReleaseManagedResource();
		}

		public override int Draw() {
			if (OpenTaiko.stage段位選択.bDifficultyIn || ctBarOut.CurrentValue < ctBarOut.EndValue) {
				ctBarIn.Tick();
				ctBarOut.Tick();

				OpenTaiko.Tx.Challenge_Select[0].Opacity = OpenTaiko.stage段位選択.bDifficultyIn ? ctBarIn.CurrentValue : 255 - ctBarOut.CurrentValue;
				OpenTaiko.Tx.Challenge_Select[1].Opacity = OpenTaiko.stage段位選択.bDifficultyIn ? ctBarIn.CurrentValue : 255 - ctBarOut.CurrentValue;
				OpenTaiko.Tx.Challenge_Select[2].Opacity = OpenTaiko.stage段位選択.bDifficultyIn ? ctBarIn.CurrentValue : 255 - ctBarOut.CurrentValue;

				OpenTaiko.Tx.Challenge_Select[0].t2D描画(0, 0);

				int selectIndex = (2 - n現在の選択行);
				int[] challenge_select_rect = OpenTaiko.Skin.DaniSelect_Challenge_Select_Rect[selectIndex];

				OpenTaiko.Tx.Challenge_Select[2].t2D描画(OpenTaiko.Skin.DaniSelect_Challenge_Select_X[selectIndex], OpenTaiko.Skin.DaniSelect_Challenge_Select_Y[selectIndex],
					new Rectangle(challenge_select_rect[0], challenge_select_rect[1], challenge_select_rect[2], challenge_select_rect[3]));

				OpenTaiko.Tx.Challenge_Select[1].t2D描画(0, 0);


				if (OpenTaiko.stage段位選択.ct待機.IsStarted)
					return base.Draw();

				#region [Key bindings]

				if (ctBarIn.IsEnded && !OpenTaiko.stage段位選択.b選択した && bOption == false) {
					if (OpenTaiko.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.RightArrow) ||
						OpenTaiko.Pad.bPressed(EInstrumentPad.DRUMS, EPad.RBlue)) {
						if (n現在の選択行 - 1 >= 0) {
							OpenTaiko.Skin.soundChangeSFX.tPlay();
							n現在の選択行--;
						}
					}

					if (OpenTaiko.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.LeftArrow) ||
					OpenTaiko.Pad.bPressed(EInstrumentPad.DRUMS, EPad.LBlue)) {
						if (n現在の選択行 + 1 <= 2) {
							OpenTaiko.Skin.soundChangeSFX.tPlay();
							n現在の選択行++;
						}
					}

					if (OpenTaiko.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.Return) ||
						OpenTaiko.Pad.bPressed(EInstrumentPad.DRUMS, EPad.LRed) ||
						OpenTaiko.Pad.bPressed(EInstrumentPad.DRUMS, EPad.RRed)) {
						if (n現在の選択行 == 0) {
							this.ctBarOut.Start(0, 255, 0.5f, OpenTaiko.Timer);
							OpenTaiko.Skin.soundCancelSFX.tPlay();
							OpenTaiko.stage段位選択.bDifficultyIn = false;
						} else if (n現在の選択行 == 1) {
							//TJAPlayer3.Skin.soundDanSongSelect.t再生する();
							OpenTaiko.ConfigIni.bTokkunMode = false;
							OpenTaiko.Skin.soundDecideSFX.tPlay();
							OpenTaiko.Skin.voiceMenuDanSelectConfirm[OpenTaiko.SaveFile]?.tPlay();
							OpenTaiko.stage段位選択.ct待機.Start(0, 3000, 1, OpenTaiko.Timer);
						} else if (n現在の選択行 == 2) {
							bOption = true;
						}
					}
				}

				#endregion
			}

			return base.Draw();
		}

		public CCounter ctBarIn;
		public CCounter ctBarOut;

		public bool bOption;

		private int n現在の選択行;
	}
}
