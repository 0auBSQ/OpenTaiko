using System.Drawing;
using System.Runtime.InteropServices;
using FDK;

namespace OpenTaiko {
	internal class CActImplRoll : CActivity {


		public CActImplRoll() {
			base.IsDeActivated = true;
		}

		public override void Activate() {
			this.ct連打枠カウンター = new CCounter[5];
			this.ct連打アニメ = new CCounter[5];
			FadeOut = new Animations.FadeOut[5];
			for (int i = 0; i < 5; i++) {
				this.ct連打枠カウンター[i] = new CCounter();
				this.ct連打アニメ[i] = new CCounter();
				// 後から変えれるようにする。大体10フレーム分。
				FadeOut[i] = new Animations.FadeOut(167);
			}
			this.b表示 = new bool[] { false, false, false, false, false };
			this.n連打数 = new int[5];

			base.Activate();
		}

		public override void DeActivate() {
			for (int i = 0; i < 5; i++) {
				ct連打枠カウンター[i] = null;
				ct連打アニメ[i] = null;
				FadeOut[i] = null;
			}
			base.DeActivate();
		}

		public override void CreateManagedResource() {
			base.CreateManagedResource();
		}

		public override void ReleaseManagedResource() {
			base.ReleaseManagedResource();
		}

		public override int Draw() {
			return base.Draw();
		}

		public int On進行描画(int n連打数, int player) {
			if (OpenTaiko.ConfigIni.nPlayerCount > 2) return base.Draw();

			this.ct連打枠カウンター[player].Tick();
			this.ct連打アニメ[player].Tick();
			FadeOut[player].Tick();
			//1PY:-3 2PY:514
			//仮置き
			int[] nRollBalloon = new int[] { -3, 514, 0, 0 };
			int[] nRollNumber = new int[] { 48, 559, 0, 0 };
			for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
				//CDTXMania.act文字コンソール.tPrint(0, 0, C文字コンソール.Eフォント種別.白, this.ct連打枠カウンター[player].n現在の値.ToString());
				if (this.ct連打枠カウンター[player].IsUnEnded) {
					if (ct連打枠カウンター[player].CurrentValue > 66 && !FadeOut[player].Counter.IsTicked) {
						FadeOut[player].Start();
					}
					var opacity = (int)FadeOut[player].GetAnimation();

					if (ct連打枠カウンター[player].CurrentValue == 0 || ct連打枠カウンター[player].CurrentValue == 60) {
						bNowRollAnime = 0;
						OpenTaiko.Tx.Balloon_Number_Roll.Opacity = 64;
					} else if (ct連打枠カウンター[player].CurrentValue == 1 || ct連打枠カウンター[player].CurrentValue == 59) {
						bNowRollAnime = 1;
						OpenTaiko.Tx.Balloon_Number_Roll.Opacity = 128;
					} else if (ct連打枠カウンター[player].CurrentValue == 2 || ct連打枠カウンター[player].CurrentValue == 58) {
						bNowRollAnime = 2;
						OpenTaiko.Tx.Balloon_Number_Roll.Opacity = 192;
					} else if (ct連打枠カウンター[player].CurrentValue == 3 || ct連打枠カウンター[player].CurrentValue == 57) {
						bNowRollAnime = 3;
						OpenTaiko.Tx.Balloon_Number_Roll.Opacity = 255;
					} else if (ct連打枠カウンター[player].CurrentValue >= 4 || ct連打枠カウンター[player].CurrentValue <= 56) {
						bNowRollAnime = 4;
						OpenTaiko.Tx.Balloon_Number_Roll.Opacity = 255;
					}

					float width = OpenTaiko.Tx.Balloon_Roll.szTextureSize.Width / 5.0f;
					float height = OpenTaiko.Tx.Balloon_Roll.szTextureSize.Height;

					OpenTaiko.Tx.Balloon_Roll?.t2D描画(OpenTaiko.Skin.Game_Balloon_Roll_Frame_X[player], OpenTaiko.Skin.Game_Balloon_Roll_Frame_Y[player], new RectangleF(0 + bNowRollAnime * width, 0, width, height));
					this.t文字表示(OpenTaiko.Skin.Game_Balloon_Roll_Number_X[player], OpenTaiko.Skin.Game_Balloon_Roll_Number_Y[player], n連打数, player);
				}
			}

			return base.Draw();
		}

		public void t枠表示時間延長(int player, bool first) {
			if ((this.ct連打枠カウンター[player].CurrentValue >= 6 && !first) || first)
				this.ct連打枠カウンター[player] = new CCounter(0, 60, 40, OpenTaiko.Timer);

			if (!first)
				this.ct連打枠カウンター[player].CurrentValue = 5;
			else
				this.ct連打枠カウンター[player].CurrentValue = 0;
		}

		public int bNowRollAnime;
		public bool[] b表示;
		public int[] n連打数;
		public CCounter[] ct連打枠カウンター;

		public CCounter[] ct連打アニメ;
		private float[] RollScale = new float[]
		{
			0.000f,
			0.123f, // リピート
            0.164f,
			0.164f,
			0.164f,
			0.137f,
			0.110f,
			0.082f,
			0.055f,
			0.000f
		};
		private Animations.FadeOut[] FadeOut;

		[StructLayout(LayoutKind.Sequential)]
		private struct ST文字位置 {
			public char ch;
			public Point pt;
		}

		private void t文字表示(int x, int y, int num, int nPlayer) {
			OpenTaiko.Tx.Balloon_Number_Roll.vcScaleRatio.X = OpenTaiko.Skin.Game_Balloon_Roll_Number_Scale;
			OpenTaiko.Tx.Balloon_Number_Roll.vcScaleRatio.Y = OpenTaiko.Skin.Game_Balloon_Roll_Number_Scale + RollScale[this.ct連打アニメ[nPlayer].CurrentValue];

			int[] nums = CConversion.SeparateDigits(num);
			for (int j = 0; j < nums.Length; j++) {
				float offset = j - (nums.Length / 2.0f);
				float _x = x - (OpenTaiko.Skin.Game_Balloon_Number_Interval[0] * offset);
				float _y = y - (OpenTaiko.Skin.Game_Balloon_Number_Interval[1] * offset);

				float width = OpenTaiko.Tx.Balloon_Number_Roll.sz画像サイズ.Width / 10.0f;
				float height = OpenTaiko.Tx.Balloon_Number_Roll.sz画像サイズ.Height;

				OpenTaiko.Tx.Balloon_Number_Roll.t2D拡大率考慮下基準描画(_x, _y, new RectangleF(width * nums[j], 0, width, height));
			}
		}
	}
}
