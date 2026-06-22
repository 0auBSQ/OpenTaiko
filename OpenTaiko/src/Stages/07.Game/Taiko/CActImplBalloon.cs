using System.Drawing;
using System.Runtime.InteropServices;
using FDK;

namespace OpenTaiko;

internal class CActImplBalloon : CActivity {


	public CActImplBalloon() {
		base.IsDeActivated = true;

	}

	public override void Activate() {
		this.ctBalloonEnd = new CCounter();
		this.ctBalloonBubbleAnime = new CCounter();
		this.ctBalloonAnime = new CCounter[5];
		for (int i = 0; i < 5; i++) {
			this.ctBalloonAnime[i] = new CCounter();
		}

		this.ctBalloonBubbleAnime = new CCounter(0, 1, 100, OpenTaiko.Timer);

		_state.RefreshConst();
		KusudamaScript = new LuaBackgroundWrapper(CSkin.Path($"{TextureLoader.BASE}{TextureLoader.GAME}{TextureLoader.BALLOON}{TextureLoader.KUSUDAMA}"));
		KusudamaScript.Activate(_state);

		base.Activate();
	}

	public override void DeActivate() {
		KusudamaScript.Dispose();

		this.ctBalloonEnd = null;
		this.ctBalloonBubbleAnime = null;

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

	public void KusuIn() {
		KusudamaScript.Call("kusuIn");
		KusudamaIsActive = true;
	}
	public void KusuBroke() {
		KusudamaScript.Call("kusuBroke");
		KusudamaIsActive = false;
	}
	public void KusuMiss() {
		KusudamaScript.Call("kusuMiss");
		KusudamaIsActive = false;
	}

	public bool KusudamaIsActive { get; private set; } = false;

	public void tDrawKusudama(bool isTrainingPaused) {
		_state.RefreshGameplay();
		if (!OpenTaiko.stageGameScreen.bPAUSE) {
			KusudamaScript.Update(_state);
		}
		if (!(OpenTaiko.ConfigIni.bTokkunMode && isTrainingPaused)) {
			KusudamaScript.Draw(_state);
		}
	}

	public int OnProgressDraw(int nRollNorma, int nConsecutiveHitCount, int player, CChip chip, bool isTrainingPaused) {
		this.ctBalloonBubbleAnime.TickLoop();
		this.ctBalloonAnime[player].Tick();

		//CDTXMania.act文字コンソール.tPrint( 0, 16, C文字コンソール.Eフォント種別.赤, this.ct風船終了.n現在の値.ToString() );
		int[] nRemainingHitCount = new int[] { 0, 0, 0, 0, 0 };
		#region[  ]
		if (nRollNorma > 0) {
			if (nRollNorma < 5) {
				nRemainingHitCount = new int[] { 4, 3, 2, 1, 0 };
			} else {
				nRemainingHitCount[0] = (nRollNorma / 5) * 4;
				nRemainingHitCount[1] = (nRollNorma / 5) * 3;
				nRemainingHitCount[2] = (nRollNorma / 5) * 2;
				nRemainingHitCount[3] = (nRollNorma / 5) * 1;
			}
		}
		#endregion

		if (nConsecutiveHitCount != 0) {
			int x;
			int y;
			int frame_x;
			int frame_y;
			int num_x;
			int num_y;
			if (OpenTaiko.ConfigIni.nPlayerCount == 5) {
				x = OpenTaiko.Skin.Game_Balloon_Balloon_5P[0] + (OpenTaiko.Skin.Game_UIMove_5P[0] * player);
				y = OpenTaiko.Skin.Game_Balloon_Balloon_5P[1] + (OpenTaiko.Skin.Game_UIMove_5P[1] * player);
				frame_x = OpenTaiko.Skin.Game_Balloon_Balloon_Frame_5P[0] + (OpenTaiko.Skin.Game_UIMove_5P[0] * player);
				frame_y = OpenTaiko.Skin.Game_Balloon_Balloon_Frame_5P[1] + (OpenTaiko.Skin.Game_UIMove_5P[1] * player);
				num_x = OpenTaiko.Skin.Game_Balloon_Balloon_Number_5P[0] + (OpenTaiko.Skin.Game_UIMove_5P[0] * player);
				num_y = OpenTaiko.Skin.Game_Balloon_Balloon_Number_5P[1] + (OpenTaiko.Skin.Game_UIMove_5P[1] * player);
			} else if (OpenTaiko.ConfigIni.nPlayerCount == 4 || OpenTaiko.ConfigIni.nPlayerCount == 3) {
				x = OpenTaiko.Skin.Game_Balloon_Balloon_4P[0] + (OpenTaiko.Skin.Game_UIMove_4P[0] * player);
				y = OpenTaiko.Skin.Game_Balloon_Balloon_4P[1] + (OpenTaiko.Skin.Game_UIMove_4P[1] * player);
				frame_x = OpenTaiko.Skin.Game_Balloon_Balloon_Frame_4P[0] + (OpenTaiko.Skin.Game_UIMove_4P[0] * player);
				frame_y = OpenTaiko.Skin.Game_Balloon_Balloon_Frame_4P[1] + (OpenTaiko.Skin.Game_UIMove_4P[1] * player);
				num_x = OpenTaiko.Skin.Game_Balloon_Balloon_Number_4P[0] + (OpenTaiko.Skin.Game_UIMove_4P[0] * player);
				num_y = OpenTaiko.Skin.Game_Balloon_Balloon_Number_4P[1] + (OpenTaiko.Skin.Game_UIMove_4P[1] * player);
			} else {
				x = OpenTaiko.Skin.Game_Balloon_Balloon_X[player];
				y = OpenTaiko.Skin.Game_Balloon_Balloon_Y[player];
				frame_x = OpenTaiko.Skin.Game_Balloon_Balloon_Frame_X[player];
				frame_y = OpenTaiko.Skin.Game_Balloon_Balloon_Frame_Y[player];
				num_x = OpenTaiko.Skin.Game_Balloon_Balloon_Number_X[player];
				num_y = OpenTaiko.Skin.Game_Balloon_Balloon_Number_Y[player];
			}

			x += OpenTaiko.stageGameScreen.GetJPOSCROLLX(player);
			y += OpenTaiko.stageGameScreen.GetJPOSCROLLY(player);
			frame_x += OpenTaiko.stageGameScreen.GetJPOSCROLLX(player);
			frame_y += OpenTaiko.stageGameScreen.GetJPOSCROLLY(player);
			num_x += OpenTaiko.stageGameScreen.GetJPOSCROLLX(player);
			num_y += OpenTaiko.stageGameScreen.GetJPOSCROLLY(player);

			for (int j = 0; j < 5; j++) {

				if (nRemainingHitCount[j] < nConsecutiveHitCount && NotesManager.GetNoteType(chip) is NotesManager.ENoteType.Balloon) {
					if (OpenTaiko.Tx.Balloon_Breaking[j] != null)
						OpenTaiko.Tx.Balloon_Breaking[j].t2DDraw(x + (this.ctBalloonBubbleAnime.CurrentValue == 1 ? 3 : 0), y);
					break;
				}
			}
			//1P:31 2P:329

			if (NotesManager.GetNoteType(chip) is NotesManager.ENoteType.Balloon) {
				if (OpenTaiko.Tx.Balloon_Balloon != null)
					OpenTaiko.Tx.Balloon_Balloon.t2DDraw(frame_x, frame_y);
				this.tTextDisplay(num_x, num_y, nConsecutiveHitCount, player);
			} else if (NotesManager.GetNoteType(chip) is NotesManager.ENoteType.BalloonFuze) {
				if (OpenTaiko.Tx.Fuse_Balloon != null)
					OpenTaiko.Tx.Fuse_Balloon.t2DDraw(frame_x, frame_y);
				this.tFuseNumber(num_x, num_y, nConsecutiveHitCount, player);
			} else if (NotesManager.GetNoteType(chip) is NotesManager.ENoteType.BalloonEx && player == 0) {
				/*
                if (TJAPlayer3.Tx.Kusudama_Back != null)
                    TJAPlayer3.Tx.Kusudama_Back.t2D描画(0, 0);
                if (TJAPlayer3.Tx.Kusudama != null)
                    TJAPlayer3.Tx.Kusudama.t2D描画(0, 0);
                    */
				if (!(OpenTaiko.ConfigIni.bTokkunMode && isTrainingPaused))
					this.tKusudamaNumber(nConsecutiveHitCount);
			}

			//CDTXMania.act文字コンソール.tPrint( 0, 0, C文字コンソール.Eフォント種別.白, n連打数.ToString() );
		}

		return base.Draw();
	}

	public LuaBackgroundWrapper KusudamaScript { get; private set; }
	private readonly LuaBackgroundState _state = new();


	private CCounter ctBalloonEnd;
	private CCounter ctBalloonBubbleAnime;

	public CCounter[] ctBalloonAnime;
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

	[StructLayout(LayoutKind.Sequential)]
	private struct STTextPosition {
		public char ch;
		public Point pt;
	}

	private void _nbDisplay(CTexture tx, int num, int x, int y) {
		int[] nums = CConversion.SeparateDigits(num);
		for (int j = 0; j < nums.Length; j++) {
			float offset = j - (nums.Length / 2.0f);
			float _x = x - (OpenTaiko.Skin.Game_Balloon_Number_Interval[0] * offset);
			float _y = y - (OpenTaiko.Skin.Game_Balloon_Number_Interval[1] * offset);

			float width = tx.szImageSize.Width / 10.0f;
			float height = tx.szImageSize.Height;

			tx.t2DScaledBottomBasedDraw(_x, _y, new RectangleF(width * nums[j], 0, width, height));
		}
	}

	private void tKusudamaNumber(int num) {
		if (OpenTaiko.Tx.Kusudama_Number == null) return;
		OpenTaiko.Tx.Kusudama_Number.vcScaleRatio.X = OpenTaiko.Skin.Game_Balloon_Balloon_Number_Scale;
		OpenTaiko.Tx.Kusudama_Number.vcScaleRatio.Y = OpenTaiko.Skin.Game_Balloon_Balloon_Number_Scale;
		int x = OpenTaiko.Skin.Game_Kusudama_Number_X;
		int y = OpenTaiko.Skin.Game_Kusudama_Number_Y;

		int[] nums = CConversion.SeparateDigits(num);
		for (int j = 0; j < nums.Length; j++) {
			float offset = j - ((nums.Length - 2) / 2.0f);
			float width = OpenTaiko.Tx.Kusudama_Number.szImageSize.Width / 10.0f;
			float height = OpenTaiko.Tx.Kusudama_Number.szImageSize.Height;
			float _x = x - (width * offset);
			float _y = y;

			OpenTaiko.Tx.Kusudama_Number.t2DScaledBottomBasedDraw(_x, _y, new RectangleF(width * nums[j], 0, width, height));
		}
	}

	private void tFuseNumber(int x, int y, int num, int nPlayer) {
		if (OpenTaiko.Tx.Fuse_Number == null) return;
		OpenTaiko.Tx.Fuse_Number.vcScaleRatio.X = OpenTaiko.Skin.Game_Balloon_Balloon_Number_Scale;
		OpenTaiko.Tx.Fuse_Number.vcScaleRatio.Y = OpenTaiko.Skin.Game_Balloon_Balloon_Number_Scale + RollScale[this.ctBalloonAnime[nPlayer].CurrentValue];

		_nbDisplay(OpenTaiko.Tx.Fuse_Number, num, x, y);
	}

	private void tTextDisplay(int x, int y, int num, int nPlayer) {
		if (OpenTaiko.Tx.Balloon_Number_Roll == null) return;
		OpenTaiko.Tx.Balloon_Number_Roll.vcScaleRatio.X = OpenTaiko.Skin.Game_Balloon_Balloon_Number_Scale;
		OpenTaiko.Tx.Balloon_Number_Roll.vcScaleRatio.Y = OpenTaiko.Skin.Game_Balloon_Balloon_Number_Scale + RollScale[this.ctBalloonAnime[nPlayer].CurrentValue];

		_nbDisplay(OpenTaiko.Tx.Balloon_Number_Roll, num, x, y);
	}

	public void tEnd() {
		this.ctBalloonEnd = new CCounter(0, 80, 10, SoundManager.PlayTimer);
	}
}
