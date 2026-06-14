using System.Drawing;
using System.Runtime.InteropServices;
using FDK;

namespace OpenTaiko;

internal class CActImplRoll : CActivity {


	public CActImplRoll() {
		base.IsDeActivated = true;
	}

	public override void Activate() {
		this.ctRollFrameCounter = new CCounter[5];
		this.ctRollAnime = new CCounter[5];
		FadeOut = new Animations.FadeOut[5];
		for (int i = 0; i < 5; i++) {
			this.ctRollFrameCounter[i] = new CCounter();
			this.ctRollAnime[i] = new CCounter();
			// 後から変えれるようにする。大体10フレーム分。
			FadeOut[i] = new Animations.FadeOut(167);
		}
		this.bDisplay = new bool[] { false, false, false, false, false };
		this.nConsecutiveHitCount = new int[5];

		base.Activate();
	}

	public override void DeActivate() {
		for (int i = 0; i < 5; i++) {
			ctRollFrameCounter[i] = null;
			ctRollAnime[i] = null;
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

	public int OnProgressDraw(int nConsecutiveHitCount, int player) {
		if (OpenTaiko.ConfigIni.nPlayerCount > 2) return base.Draw();

		this.ctRollFrameCounter[player].Tick();
		this.ctRollAnime[player].Tick();
		FadeOut[player].Tick();
		for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
			//CDTXMania.act文字コンソール.tPrint(0, 0, C文字コンソール.Eフォント種別.白, this.ct連打枠カウンター[player].n現在の値.ToString());
			if (this.ctRollFrameCounter[player].IsUnEnded) {
				if (ctRollFrameCounter[player].CurrentValue > 66 && !FadeOut[player].Counter.IsTicked) {
					FadeOut[player].Start();
				}
				var opacity = (int)FadeOut[player].GetAnimation();

				if (ctRollFrameCounter[player].CurrentValue == 0 || ctRollFrameCounter[player].CurrentValue == 60) {
					bNowRollAnime = 0;
					OpenTaiko.Tx.Balloon_Number_Roll.Opacity = 64;
				} else if (ctRollFrameCounter[player].CurrentValue == 1 || ctRollFrameCounter[player].CurrentValue == 59) {
					bNowRollAnime = 1;
					OpenTaiko.Tx.Balloon_Number_Roll.Opacity = 128;
				} else if (ctRollFrameCounter[player].CurrentValue == 2 || ctRollFrameCounter[player].CurrentValue == 58) {
					bNowRollAnime = 2;
					OpenTaiko.Tx.Balloon_Number_Roll.Opacity = 192;
				} else if (ctRollFrameCounter[player].CurrentValue == 3 || ctRollFrameCounter[player].CurrentValue == 57) {
					bNowRollAnime = 3;
					OpenTaiko.Tx.Balloon_Number_Roll.Opacity = 255;
				} else if (ctRollFrameCounter[player].CurrentValue >= 4 || ctRollFrameCounter[player].CurrentValue <= 56) {
					bNowRollAnime = 4;
					OpenTaiko.Tx.Balloon_Number_Roll.Opacity = 255;
				}

				float width = OpenTaiko.Tx.Balloon_Roll.szTextureSize.Width / 5.0f;
				float height = OpenTaiko.Tx.Balloon_Roll.szTextureSize.Height;

				OpenTaiko.Tx.Balloon_Roll?.t2DDraw(OpenTaiko.Skin.Game_Balloon_Roll_Frame_X[player], OpenTaiko.Skin.Game_Balloon_Roll_Frame_Y[player], new RectangleF(0 + bNowRollAnime * width, 0, width, height));
				this.tTextDisplay(OpenTaiko.Skin.Game_Balloon_Roll_Number_X[player], OpenTaiko.Skin.Game_Balloon_Roll_Number_Y[player], nConsecutiveHitCount, player);

				// reset opacity for balloon's and fuze roll's pop count
				OpenTaiko.Tx.Balloon_Number_Roll.Opacity = 255;
			}
		}

		return base.Draw();
	}

	public void tFrameDisplayTimeExtend(int player, bool first) {
		if ((this.ctRollFrameCounter[player].CurrentValue >= 6 && !first) || first)
			this.ctRollFrameCounter[player] = new CCounter(0, 60, 40, OpenTaiko.Timer);

		if (!first)
			this.ctRollFrameCounter[player].CurrentValue = 5;
		else
			this.ctRollFrameCounter[player].CurrentValue = 0;
	}

	public int bNowRollAnime;
	public bool[] bDisplay;
	public int[] nConsecutiveHitCount;
	public CCounter[] ctRollFrameCounter;

	public CCounter[] ctRollAnime;
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
	private struct STTextPosition {
		public char ch;
		public Point pt;
	}

	private void tTextDisplay(int x, int y, int num, int nPlayer) {
		OpenTaiko.Tx.Balloon_Number_Roll.vcScaleRatio.X = OpenTaiko.Skin.Game_Balloon_Roll_Number_Scale;
		OpenTaiko.Tx.Balloon_Number_Roll.vcScaleRatio.Y = OpenTaiko.Skin.Game_Balloon_Roll_Number_Scale + RollScale[this.ctRollAnime[nPlayer].CurrentValue];

		int[] nums = CConversion.SeparateDigits(num);
		for (int j = 0; j < nums.Length; j++) {
			float offset = j - (nums.Length / 2.0f);
			float _x = x - (OpenTaiko.Skin.Game_Balloon_Number_Interval[0] * offset);
			float _y = y - (OpenTaiko.Skin.Game_Balloon_Number_Interval[1] * offset);

			float width = OpenTaiko.Tx.Balloon_Number_Roll.szImageSize.Width / 10.0f;
			float height = OpenTaiko.Tx.Balloon_Number_Roll.szImageSize.Height;

			OpenTaiko.Tx.Balloon_Number_Roll.t2DScaledBottomBasedDraw(_x, _y, new RectangleF(width * nums[j], 0, width, height));
		}
	}
}
