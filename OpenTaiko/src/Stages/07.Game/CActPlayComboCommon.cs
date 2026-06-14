using System.Drawing;
using FDK;

namespace OpenTaiko;

internal class CActPlayComboCommon : CActivity {
	// Properties

	public STCOMBO nCurrentCombo;
	public struct STCOMBO {
		public CActPlayComboCommon act;

		public int this[int index] {
			get {
				switch (index) {
					case 0:
						return this.P1;

					case 1:
						return this.P2;

					case 2:
						return this.P3;

					case 3:
						return this.P4;

					case 4:
						return this.P5;
				}
				throw new IndexOutOfRangeException();
			}
			set {
				switch (index) {
					case 0:
						this.P1 = value;
						return;

					case 1:
						this.P2 = value;
						return;

					case 2:
						this.P3 = value;
						return;

					case 3:
						this.P4 = value;
						return;

					case 4:
						this.P5 = value;
						return;
				}
				throw new IndexOutOfRangeException();
			}
		}
		public int P1 {
			get {
				return this.p1;
			}
			set {
				this.p1 = value;
				if (this.p1 > this.MaxValue[0]) {
					this.MaxValue[0] = this.p1;
				}
				this.act.status.P1.nCOMBOValue = this.p1;
				this.act.status.P1.nMaxCOMBOValue = this.MaxValue[0];
			}
		}
		public int P2 {
			get {
				return this.p2;
			}
			set {
				this.p2 = value;
				if (this.p2 > this.MaxValue[1]) {
					this.MaxValue[1] = this.p2;
				}
				this.act.status.P2.nCOMBOValue = this.p2;
				this.act.status.P2.nMaxCOMBOValue = this.MaxValue[1];
			}
		}
		public int P3 {
			get {
				return this.p3;
			}
			set {
				this.p3 = value;
				if (this.p3 > this.MaxValue[2]) {
					this.MaxValue[2] = this.p3;
				}
				this.act.status.P3.nCOMBOValue = this.p3;
				this.act.status.P3.nMaxCOMBOValue = this.MaxValue[2];
			}
		}
		public int P4 {
			get {
				return this.p4;
			}
			set {
				this.p4 = value;
				if (this.p4 > this.MaxValue[3]) {
					this.MaxValue[3] = this.p4;
				}
				this.act.status.P4.nCOMBOValue = this.p4;
				this.act.status.P4.nMaxCOMBOValue = this.MaxValue[3];
			}
		}
		public int P5 {
			get {
				return this.p5;
			}
			set {
				this.p5 = value;
				if (this.p5 > this.MaxValue[4]) {
					this.MaxValue[4] = this.p5;
				}
				this.act.status.P5.nCOMBOValue = this.p5;
				this.act.status.P5.nMaxCOMBOValue = this.MaxValue[4];
			}
		}
		public int[] MaxValue { get; set; }

		private int p1;
		private int p2;
		private int p3;
		private int p4;
		private int p5;
	}

	protected enum EEvent { Hide, ValueUpdate, SameValue, MissNotify }
	protected enum EMode { Hidden, ProgressDisplaying, AfterimageDisplaying }
	protected const int nDrumsComboCOMBOTextHeight = 32;
	protected const int nDrumsComboCOMBOTextWidth = 90;
	protected const int nDrumsComboHeight = 115;
	protected const int nDrumsComboWidth = 90;
	protected const int nDrumsComboTextInterval = -6;
	protected int[] nJumpDiffPartValue = new int[180];
	protected CSTATUS status;
	//protected CTexture txCOMBO太鼓;
	//protected CTexture txCOMBO太鼓_でかいやつ;
	//protected CTexture txコンボラメ;
	public CCounter[] ctComboAddCounter;
	public CCounter ctComboGlitter;

	protected float[,] nComboScale_Coord = new float[,]{
		{1.11f,-7},
		{1.22f,-14},
		{1.2f,-12},
		{1.15f,-9},
		{1.13f,-8},
		{1.11f,-7},
		{1.06f,-3},
		{1.04f,-2},
		{1.0f,0},
	};
	protected float[,] nComboScale_Coord_100combo = new float[,]{
		{0.81f,-7},
		{0.92f,-14},
		{0.9f,-12},
		{0.85f,-9},
		{0.83f,-8},
		{0.81f,-7},
		{0.78f,-3},
		{0.74f,-2},
		{0.7f,0},
	};
	protected float[,] nComboScale_Coord_1000combo = new float[,]{
		{1.11f,-7},
		{1.22f,-14},
		{1.2f,-12},
		{1.15f,-9},
		{1.13f,-8},
		{1.11f,-7},
		{1.06f,-3},
		{1.04f,-2},
		{1.0f,0},
	};


	private float[] ComboScale = new float[]
	{
		0.000f,
		0.042f,
		0.120f,
		0.160f,
		0.180f,
		0.120f,
		0.110f,
		0.095f,
		0.086f,
		0.044f,
		0.032f,
		0.011f,
		0.000f
	};
	private float[,] ComboScale_Ex = new float[,]
	{
		{ 0.000f, 0},
		{ 0.042f, 0},
		{ 0.120f, 0},
		{ 0.160f, 0},
		{ 0.180f, 0},
		{ 0.120f, 0},
		{ 0.110f, 0},
		{ 0.095f, 0},
		{ 0.086f, 0},
		{ 0.044f, 0},
		{ 0.032f, 0},
		{ 0.011f, 0},
		{ 0.000f, 0},
	};
	// 内部クラス

	protected class CSTATUS {
		public CSTAT P1 = new CSTAT();
		public CSTAT P2 = new CSTAT();
		public CSTAT P3 = new CSTAT();
		public CSTAT P4 = new CSTAT();
		public CSTAT P5 = new CSTAT();
		public CSTAT this[int index] {
			get {
				switch (index) {
					case 0:
						return this.P1;

					case 1:
						return this.P2;

					case 2:
						return this.P3;

					case 3:
						return this.P4;

					case 4:
						return this.P5;
				}
				throw new IndexOutOfRangeException();
			}
			set {
				switch (index) {
					case 0:
						this.P1 = value;
						return;

					case 1:
						this.P2 = value;
						return;

					case 2:
						this.P3 = value;
						return;

					case 3:
						this.P4 = value;
						return;

					case 4:
						this.P5 = value;
						return;
				}
				throw new IndexOutOfRangeException();
			}
		}

		public class CSTAT {
			public CActPlayComboCommon.EMode eCurrentMode;
			public int nCOMBOValue;
			public long nComboCutTime;
			public int nJumpIndexValue;
			public int nCurrentDisplayingCOMBOValue;
			public int nMaxCOMBOValue;
			public int nAfterimageDisplayingCOMBOValue;
			public long nPreviousTime_Jump;
		}
	}


	// メソッド


	private void showComboEffect(int cat, int i, int rightX, int y, int nPlayer) {
		if (OpenTaiko.Tx.Taiko_Combo_Effect != null) {
			int a = rightX - OpenTaiko.Skin.Game_Taiko_Combo_Padding[cat] * i;
			float b = (OpenTaiko.Skin.Game_Taiko_Combo_Size_Ex[0] / 4) * OpenTaiko.Skin.Game_Taiko_Combo_Scale[cat];
			float c = (OpenTaiko.Skin.Game_Taiko_Combo_Size_Ex[1] / 4) * OpenTaiko.Skin.Game_Taiko_Combo_Scale[cat];
			float d = y;

			if (ctComboGlitter.CurrentValue < 13) // First
			{
				// まんなか
				OpenTaiko.Tx.Taiko_Combo_Effect.t2DScaledBottomCenterBasedDraw(a, d - c - (int)(1.05 * this.ctComboGlitter.CurrentValue) - 13);
			}
			if (ctComboGlitter.CurrentValue >= 8 && ctComboGlitter.CurrentValue < 23) {
				// みぎ
				OpenTaiko.Tx.Taiko_Combo_Effect.t2DScaledBottomCenterBasedDraw(a + b, d - c - (int)(1.05 * (this.ctComboGlitter.CurrentValue - 10)) - 3);

			}
			if (this.ctComboGlitter.CurrentValue >= 17 && this.ctComboGlitter.CurrentValue < 32) {
				// ひだり
				OpenTaiko.Tx.Taiko_Combo_Effect.t2DScaledBottomCenterBasedDraw(a - b, d - c - (int)(1.05 * this.ctComboGlitter.CurrentValue - 20) - 8);
			}
		}
	}

	protected virtual void tComboDisplay_Taiko(int nComboValue, int nJumpIndex, int nPlayer) {
		// Combo display here

		//テスト用コンボ数
		//nCombo値 = 72;
		#region [ 事前チェック。]
		//-----------------
		//if( CDTXMania.ConfigIni.bドラムコンボ表示 == false )
		//	return;		// 表示OFF。

		if (nComboValue == 0)
			return;     // コンボゼロは表示しない。
						//-----------------
		#endregion

		int[] nPlaceCount = new int[10];   // 表示は10桁もあれば足りるだろう

		this.ctComboGlitter.TickLoop();
		this.ctComboAddCounter[nPlayer].Tick();

		#region [ nCombo値を桁数ごとに nPlaceCount[] に格納する。（例：nComboValue=125 のとき nPlaceCount = { 5,2,1,0,0,0,0,0,0,0 } ） ]
		//-----------------
		int n = nComboValue;
		int nDigitCount = 0;
		while ((n > 0) && (nDigitCount < 10)) {
			nPlaceCount[nDigitCount] = n % 10;     // 1の位を格納
			n = (n - (n % 10)) / 10;    // 右へシフト（例: 12345 → 1234 ）
			nDigitCount++;
		}
		//-----------------
		#endregion

		#region [ nPlaceCount[] を、"COMBO" → 1の位 → 10の位 … の順に、右から左へ向かって順番に表示する。]
		//-----------------
		const int n1DigitPerJumpDelay = 30;   // 1桁につき 50 インデックス遅れる


		//X右座標を元にして、右座標 - ( コンボの幅 * 桁数 ) でX座標を求めていく?

		int combo_text_x;
		int combo_text_y;
		int combo_x;
		int combo_y;
		int combo_ex_x;
		int combo_ex_y;
		int combo_ex4_x;
		int combo_ex4_y;
		if (OpenTaiko.ConfigIni.nPlayerCount == 5) {
			combo_text_x = OpenTaiko.Skin.Game_Taiko_Combo_Text_5P[0] + (OpenTaiko.Skin.Game_UIMove_5P[0] * nPlayer);
			combo_text_y = OpenTaiko.Skin.Game_Taiko_Combo_Text_5P[1] + (OpenTaiko.Skin.Game_UIMove_5P[1] * nPlayer);

			combo_x = OpenTaiko.Skin.Game_Taiko_Combo_5P[0] + (OpenTaiko.Skin.Game_UIMove_5P[0] * nPlayer);
			combo_y = OpenTaiko.Skin.Game_Taiko_Combo_5P[1] + (OpenTaiko.Skin.Game_UIMove_5P[1] * nPlayer);
			combo_ex_x = OpenTaiko.Skin.Game_Taiko_Combo_Ex_5P[0] + (OpenTaiko.Skin.Game_UIMove_5P[0] * nPlayer);
			combo_ex_y = OpenTaiko.Skin.Game_Taiko_Combo_Ex_5P[1] + (OpenTaiko.Skin.Game_UIMove_5P[1] * nPlayer);
			combo_ex4_x = OpenTaiko.Skin.Game_Taiko_Combo_Ex4_5P[0] + (OpenTaiko.Skin.Game_UIMove_5P[0] * nPlayer);
			combo_ex4_y = OpenTaiko.Skin.Game_Taiko_Combo_Ex4_5P[1] + (OpenTaiko.Skin.Game_UIMove_5P[1] * nPlayer);
		} else if (OpenTaiko.ConfigIni.nPlayerCount == 4 || OpenTaiko.ConfigIni.nPlayerCount == 3) {
			combo_text_x = OpenTaiko.Skin.Game_Taiko_Combo_Text_4P[0] + (OpenTaiko.Skin.Game_UIMove_4P[0] * nPlayer);
			combo_text_y = OpenTaiko.Skin.Game_Taiko_Combo_Text_4P[1] + (OpenTaiko.Skin.Game_UIMove_4P[1] * nPlayer);

			combo_x = OpenTaiko.Skin.Game_Taiko_Combo_4P[0] + (OpenTaiko.Skin.Game_UIMove_4P[0] * nPlayer);
			combo_y = OpenTaiko.Skin.Game_Taiko_Combo_4P[1] + (OpenTaiko.Skin.Game_UIMove_4P[1] * nPlayer);
			combo_ex_x = OpenTaiko.Skin.Game_Taiko_Combo_Ex_4P[0] + (OpenTaiko.Skin.Game_UIMove_4P[0] * nPlayer);
			combo_ex_y = OpenTaiko.Skin.Game_Taiko_Combo_Ex_4P[1] + (OpenTaiko.Skin.Game_UIMove_4P[1] * nPlayer);
			combo_ex4_x = OpenTaiko.Skin.Game_Taiko_Combo_Ex4_4P[0] + (OpenTaiko.Skin.Game_UIMove_4P[0] * nPlayer);
			combo_ex4_y = OpenTaiko.Skin.Game_Taiko_Combo_Ex4_4P[1] + (OpenTaiko.Skin.Game_UIMove_4P[1] * nPlayer);
		} else {
			combo_text_x = OpenTaiko.Skin.Game_Taiko_Combo_Text_X[nPlayer];
			combo_text_y = OpenTaiko.Skin.Game_Taiko_Combo_Text_Y[nPlayer];

			combo_x = OpenTaiko.Skin.Game_Taiko_Combo_X[nPlayer];
			combo_y = OpenTaiko.Skin.Game_Taiko_Combo_Y[nPlayer];
			combo_ex_x = OpenTaiko.Skin.Game_Taiko_Combo_Ex_X[nPlayer];
			combo_ex_y = OpenTaiko.Skin.Game_Taiko_Combo_Ex_Y[nPlayer];
			combo_ex4_x = OpenTaiko.Skin.Game_Taiko_Combo_Ex4_X[nPlayer];
			combo_ex4_y = OpenTaiko.Skin.Game_Taiko_Combo_Ex4_Y[nPlayer];
		}

		int nYTopEdgePositionpx = OpenTaiko.ConfigIni.bReverse.Drums ? 350 : 10;
		int nNumberComboCombinedImageFullLengthPx = ((44) * nDigitCount);
		int x = 245 + (nNumberComboCombinedImageFullLengthPx / 2);
		//int y = 212;
		//int y = CDTXMania.Skin.nComboNumberY[ nPlayer ];

		#region[ Combo text & Combo guides ]

		if (!OpenTaiko.ConfigIni.SimpleMode) {
			OpenTaiko.Tx.Taiko_Combo_Text?.t2DScaledBottomCenterBasedDraw(combo_text_x, combo_text_y);

			int guide = 2;
			var ccf = OpenTaiko.stageGameScreen.CChartScore[nPlayer];

			if (ccf.nGood > 0)
				guide = 1;
			if (ccf.nMiss > 0 || ccf.nMine > 0)
				guide = 0;

			OpenTaiko.Tx.Taiko_Combo_Guide[guide]?.t2DScaledBottomCenterBasedDraw(combo_text_x, combo_text_y);
		}

		#endregion

		int rightX = 0;
		#region 一番右の数字の座標の決定
		if (nDigitCount == 1) {
			// 一桁ならそのままSkinConfigの座標を使用する。
			rightX = combo_x;
		} else if (nDigitCount == 2) {
			// 二桁ならSkinConfigの座標+パディング/2を使用する
			rightX = combo_x + OpenTaiko.Skin.Game_Taiko_Combo_Padding[0] / 2;
		} else if (nDigitCount == 3) {
			// 三桁ならSkinConfigの座標+パディングを使用する
			rightX = combo_ex_x + OpenTaiko.Skin.Game_Taiko_Combo_Padding[1];
		} else if (nDigitCount == 4) {
			// 四桁ならSkinconfigの座標+パディング/2 + パディングを使用する
			rightX = combo_ex4_x + OpenTaiko.Skin.Game_Taiko_Combo_Padding[2] / 2 + OpenTaiko.Skin.Game_Taiko_Combo_Padding[2];
		} else {
			// 五桁以上の場合
			int rightDigit = 0;
			switch (nDigitCount % 2) {
				case 0:
					// 2で割り切れる
					// パディング/2を足す必要がある
					// 右に表示される桁数を求め、-1する
					rightDigit = nDigitCount / 2 - 1;
					rightX = combo_ex4_x + OpenTaiko.Skin.Game_Taiko_Combo_Padding[2] / 2 + OpenTaiko.Skin.Game_Taiko_Combo_Padding[2] * rightDigit;
					break;
				case 1:
					// 2で割るとあまりが出る
					// そのままパディングを足していく
					// 右に表示される桁数を求める(中央除く -1)
					rightDigit = (nDigitCount - 1) / 2;
					rightX = combo_ex4_x + OpenTaiko.Skin.Game_Taiko_Combo_Padding[2] * rightDigit;
					break;
				default:
					break;
			}
		}
		#endregion


		for (int i = 0; i < nDigitCount; i++) {

			OpenTaiko.Tx.Taiko_Combo[0].Opacity = 255;
			OpenTaiko.Tx.Taiko_Combo[1].Opacity = 255;

			if (nDigitCount <= 1) {
				if (OpenTaiko.Tx.Taiko_Combo[0] != null) {
					var yScalling = OpenTaiko.ConfigIni.SimpleMode ? 0 : ComboScale[this.ctComboAddCounter[nPlayer].CurrentValue];
					OpenTaiko.Tx.Taiko_Combo[0].vcScaleRatio.Y = OpenTaiko.Skin.Game_Taiko_Combo_Scale[0] + yScalling;
					OpenTaiko.Tx.Taiko_Combo[0].vcScaleRatio.X = OpenTaiko.Skin.Game_Taiko_Combo_Scale[0];
					OpenTaiko.Tx.Taiko_Combo[0].t2DScaledBottomCenterBasedDraw(rightX, combo_y, new Rectangle(nPlaceCount[i] * OpenTaiko.Skin.Game_Taiko_Combo_Size[0], 0, OpenTaiko.Skin.Game_Taiko_Combo_Size[0], OpenTaiko.Skin.Game_Taiko_Combo_Size[1]));
				}
			} else if (nDigitCount <= 2) {
				//int[] arComboX = { CDTXMania.Skin.Game_Taiko_Combo_X[nPlayer] + CDTXMania.Skin.Game_Taiko_Combo_Padding[0], CDTXMania.Skin.Game_Taiko_Combo_X[nPlayer] - CDTXMania.Skin.Game_Taiko_Combo_Padding[0] };
				if (nComboValue < 50) {
					if (OpenTaiko.Tx.Taiko_Combo[0] != null) {
						var yScalling = OpenTaiko.ConfigIni.SimpleMode ? 0 : ComboScale[this.ctComboAddCounter[nPlayer].CurrentValue];
						OpenTaiko.Tx.Taiko_Combo[0].vcScaleRatio.Y = OpenTaiko.Skin.Game_Taiko_Combo_Scale[0] + yScalling;
						OpenTaiko.Tx.Taiko_Combo[0].vcScaleRatio.X = OpenTaiko.Skin.Game_Taiko_Combo_Scale[0];
						OpenTaiko.Tx.Taiko_Combo[0].t2DScaledBottomCenterBasedDraw(rightX - OpenTaiko.Skin.Game_Taiko_Combo_Padding[0] * i, combo_y, new Rectangle(nPlaceCount[i] * OpenTaiko.Skin.Game_Taiko_Combo_Size[0], 0, OpenTaiko.Skin.Game_Taiko_Combo_Size[0], OpenTaiko.Skin.Game_Taiko_Combo_Size[1]));
					}
				} else {
					if (OpenTaiko.Tx.Taiko_Combo[2] != null) {
						var yScalling = OpenTaiko.ConfigIni.SimpleMode ? 0 : ComboScale[this.ctComboAddCounter[nPlayer].CurrentValue];
						OpenTaiko.Tx.Taiko_Combo[2].vcScaleRatio.Y = OpenTaiko.Skin.Game_Taiko_Combo_Scale[0] + yScalling;
						OpenTaiko.Tx.Taiko_Combo[2].vcScaleRatio.X = OpenTaiko.Skin.Game_Taiko_Combo_Scale[0];
						OpenTaiko.Tx.Taiko_Combo[2].t2DScaledBottomCenterBasedDraw(rightX - OpenTaiko.Skin.Game_Taiko_Combo_Padding[0] * i, combo_y, new Rectangle(nPlaceCount[i] * OpenTaiko.Skin.Game_Taiko_Combo_Size[0], 0, OpenTaiko.Skin.Game_Taiko_Combo_Size[0], OpenTaiko.Skin.Game_Taiko_Combo_Size[1]));
					}

					if (!OpenTaiko.ConfigIni.SimpleMode) showComboEffect(0, i, rightX, combo_ex_y, nPlayer);
				}
			} else if (nDigitCount == 3) {
				if (nComboValue >= 300 && OpenTaiko.Tx.Taiko_Combo[3] != null) {
					var yScalling = OpenTaiko.ConfigIni.SimpleMode ? 0 : ComboScale_Ex[this.ctComboAddCounter[nPlayer].CurrentValue, 0];
					OpenTaiko.Tx.Taiko_Combo[3].vcScaleRatio.Y = OpenTaiko.Skin.Game_Taiko_Combo_Scale[1] + yScalling;
					OpenTaiko.Tx.Taiko_Combo[3].vcScaleRatio.X = OpenTaiko.Skin.Game_Taiko_Combo_Scale[1];
					var yJumping = OpenTaiko.Skin.Game_Taiko_Combo_Ex_IsJumping ? (int)ComboScale_Ex[this.ctComboAddCounter[nPlayer].CurrentValue, 1] : 0;
					OpenTaiko.Tx.Taiko_Combo[3].t2DScaledBottomCenterBasedDraw(rightX - OpenTaiko.Skin.Game_Taiko_Combo_Padding[1] * i, combo_ex_y + yJumping, new Rectangle(nPlaceCount[i] * OpenTaiko.Skin.Game_Taiko_Combo_Size_Ex[0], 0, OpenTaiko.Skin.Game_Taiko_Combo_Size_Ex[0], OpenTaiko.Skin.Game_Taiko_Combo_Size_Ex[1]));
				} else if (OpenTaiko.Tx.Taiko_Combo[1] != null) {
					var yScalling = OpenTaiko.ConfigIni.SimpleMode ? 0 : ComboScale_Ex[this.ctComboAddCounter[nPlayer].CurrentValue, 0];
					OpenTaiko.Tx.Taiko_Combo[1].vcScaleRatio.Y = OpenTaiko.Skin.Game_Taiko_Combo_Scale[1] + yScalling;
					OpenTaiko.Tx.Taiko_Combo[1].vcScaleRatio.X = OpenTaiko.Skin.Game_Taiko_Combo_Scale[1];
					var yJumping = OpenTaiko.Skin.Game_Taiko_Combo_Ex_IsJumping ? (int)ComboScale_Ex[this.ctComboAddCounter[nPlayer].CurrentValue, 1] : 0;
					OpenTaiko.Tx.Taiko_Combo[1].t2DScaledBottomCenterBasedDraw(rightX - OpenTaiko.Skin.Game_Taiko_Combo_Padding[1] * i, combo_ex_y + yJumping, new Rectangle(nPlaceCount[i] * OpenTaiko.Skin.Game_Taiko_Combo_Size_Ex[0], 0, OpenTaiko.Skin.Game_Taiko_Combo_Size_Ex[0], OpenTaiko.Skin.Game_Taiko_Combo_Size_Ex[1]));
				}

				if (!OpenTaiko.ConfigIni.SimpleMode) showComboEffect(1, i, rightX, combo_ex_y, nPlayer);
			} else {
				if (nComboValue >= 300 && OpenTaiko.Tx.Taiko_Combo[3] != null) {
					var yScalling = OpenTaiko.ConfigIni.SimpleMode ? 0 : ComboScale_Ex[this.ctComboAddCounter[nPlayer].CurrentValue, 0];
					OpenTaiko.Tx.Taiko_Combo[3].vcScaleRatio.Y = 1.0f + yScalling;
					OpenTaiko.Tx.Taiko_Combo[3].vcScaleRatio.X = OpenTaiko.Skin.Game_Taiko_Combo_Scale[2];
					var yJumping = OpenTaiko.Skin.Game_Taiko_Combo_Ex_IsJumping ? (int)ComboScale_Ex[this.ctComboAddCounter[nPlayer].CurrentValue, 1] : 0;
					OpenTaiko.Tx.Taiko_Combo[3].t2DScaledBottomCenterBasedDraw(rightX - OpenTaiko.Skin.Game_Taiko_Combo_Padding[2] * i, combo_ex_y + yJumping, new Rectangle(nPlaceCount[i] * OpenTaiko.Skin.Game_Taiko_Combo_Size_Ex[0], 0, OpenTaiko.Skin.Game_Taiko_Combo_Size_Ex[0], OpenTaiko.Skin.Game_Taiko_Combo_Size_Ex[1]));
				} else if (OpenTaiko.Tx.Taiko_Combo[1] != null) {
					var yScalling = OpenTaiko.ConfigIni.SimpleMode ? 0 : ComboScale_Ex[this.ctComboAddCounter[nPlayer].CurrentValue, 0];
					OpenTaiko.Tx.Taiko_Combo[1].vcScaleRatio.Y = 1.0f + yScalling;
					OpenTaiko.Tx.Taiko_Combo[1].vcScaleRatio.X = OpenTaiko.Skin.Game_Taiko_Combo_Scale[2];
					var yJumping = OpenTaiko.Skin.Game_Taiko_Combo_Ex_IsJumping ? (int)ComboScale_Ex[this.ctComboAddCounter[nPlayer].CurrentValue, 1] : 0;
					OpenTaiko.Tx.Taiko_Combo[1].t2DScaledBottomCenterBasedDraw(rightX - OpenTaiko.Skin.Game_Taiko_Combo_Padding[2] * i, combo_ex_y + yJumping, new Rectangle(nPlaceCount[i] * OpenTaiko.Skin.Game_Taiko_Combo_Size_Ex[0], 0, OpenTaiko.Skin.Game_Taiko_Combo_Size_Ex[0], OpenTaiko.Skin.Game_Taiko_Combo_Size_Ex[1]));
				}

				if (!OpenTaiko.ConfigIni.SimpleMode) showComboEffect(2, i, rightX, combo_ex_y, nPlayer);

			}
		}

		//-----------------
		#endregion
	}

	// CActivity 実装

	public override void Activate() {
		this.nCurrentCombo = new STCOMBO() { act = this };
		this.nCurrentCombo.MaxValue = new int[5];
		this.status = new CSTATUS();
		this.ctComboAddCounter = new CCounter[5];
		for (int i = 0; i < 5; i++) {
			this.status[i].eCurrentMode = EMode.Hidden;
			this.status[i].nCOMBOValue = 0;
			this.status[i].nMaxCOMBOValue = 0;
			this.status[i].nCurrentDisplayingCOMBOValue = 0;
			this.status[i].nAfterimageDisplayingCOMBOValue = 0;
			this.status[i].nJumpIndexValue = 99999;
			this.status[i].nPreviousTime_Jump = -1;
			this.status[i].nComboCutTime = -1;
			this.ctComboAddCounter[i] = new CCounter(0, 12, 12, OpenTaiko.Timer);
		}
		this.ctComboGlitter = new CCounter(0, 35, 16, OpenTaiko.Timer);
		base.Activate();
	}
	public override void DeActivate() {
		if (this.status != null)
			this.status = null;

		base.DeActivate();
	}
	public override void CreateManagedResource() {
		base.CreateManagedResource();
	}
	public override void ReleaseManagedResource() {
		base.ReleaseManagedResource();
	}
	public override int Draw() {
		if (this.IsDeActivated)
			return 0;

		for (int i = 0; i < 5; i++) {
			EEvent eThisTimeStateTransitionEvent;

			#region [ 前回と今回の COMBO 値から、e今回の状態遷移イベントを決定する。]
			//-----------------
			if (this.status[i].nCurrentDisplayingCOMBOValue == this.status[i].nCOMBOValue) {
				eThisTimeStateTransitionEvent = EEvent.SameValue;
			} else if (this.status[i].nCurrentDisplayingCOMBOValue > this.status[i].nCOMBOValue) {
				eThisTimeStateTransitionEvent = EEvent.MissNotify;
			} else if ((this.status[i].nCurrentDisplayingCOMBOValue < OpenTaiko.ConfigIni.nMinDisplayedCombo.Drums) && (this.status[i].nCOMBOValue < OpenTaiko.ConfigIni.nMinDisplayedCombo.Drums)) {
				eThisTimeStateTransitionEvent = EEvent.Hide;
			} else {
				eThisTimeStateTransitionEvent = EEvent.ValueUpdate;
			}
			//-----------------
			#endregion

			#region [ nJumpIndexValue の進行。]
			//-----------------
			if (this.status[i].nJumpIndexValue < 360) {
				if ((this.status[i].nPreviousTime_Jump == -1) || (OpenTaiko.Timer.NowTimeMs < this.status[i].nPreviousTime_Jump))
					this.status[i].nPreviousTime_Jump = OpenTaiko.Timer.NowTimeMs;

				const long INTERVAL = 2;
				while ((OpenTaiko.Timer.NowTimeMs - this.status[i].nPreviousTime_Jump) >= INTERVAL) {
					if (this.status[i].nJumpIndexValue < 2000)
						this.status[i].nJumpIndexValue += 3;

					this.status[i].nPreviousTime_Jump += INTERVAL;
				}
			}
		//-----------------
		#endregion

		Retry:  // モードが変化した場合はここからリトライする。

			switch (this.status[i].eCurrentMode) {
				case EMode.Hidden:
					#region [ *** ]
					//-----------------

					if (eThisTimeStateTransitionEvent == EEvent.ValueUpdate) {
						// モード変更
						this.status[i].eCurrentMode = EMode.ProgressDisplaying;
						this.status[i].nJumpIndexValue = 0;
						this.status[i].nPreviousTime_Jump = OpenTaiko.Timer.NowTimeMs;
						goto Retry;
					}

					this.status[i].nCurrentDisplayingCOMBOValue = this.status[i].nCOMBOValue;
					break;
				//-----------------
				#endregion

				case EMode.ProgressDisplaying:
					#region [ *** ]
					//-----------------

					if ((eThisTimeStateTransitionEvent == EEvent.Hide) || (eThisTimeStateTransitionEvent == EEvent.MissNotify)) {
						// モード変更
						this.status[i].eCurrentMode = EMode.AfterimageDisplaying;
						this.status[i].nAfterimageDisplayingCOMBOValue = this.status[i].nCurrentDisplayingCOMBOValue;
						this.status[i].nComboCutTime = OpenTaiko.Timer.NowTimeMs;
						goto Retry;
					}

					if (eThisTimeStateTransitionEvent == EEvent.ValueUpdate) {
						this.status[i].nJumpIndexValue = 0;
						this.status[i].nPreviousTime_Jump = OpenTaiko.Timer.NowTimeMs;
					}

					this.status[i].nCurrentDisplayingCOMBOValue = this.status[i].nCOMBOValue;
					switch (i) {
						case 0:
							this.tComboDisplay_Taiko(this.status[i].nCOMBOValue, this.status[i].nJumpIndexValue, 0);
							break;

						case 1:
							this.tComboDisplay_Taiko(this.status[i].nCOMBOValue, this.status[i].nJumpIndexValue, 1);
							break;

						case 2:
							this.tComboDisplay_Taiko(this.status[i].nCOMBOValue, this.status[i].nJumpIndexValue, 2);
							break;

						case 3:
							this.tComboDisplay_Taiko(this.status[i].nCOMBOValue, this.status[i].nJumpIndexValue, 3);
							break;

						case 4:
							this.tComboDisplay_Taiko(this.status[i].nCOMBOValue, this.status[i].nJumpIndexValue, 4);
							break;
					}
					break;
				//-----------------
				#endregion

				case EMode.AfterimageDisplaying:
					#region [ *** ]
					//-----------------
					if (eThisTimeStateTransitionEvent == EEvent.ValueUpdate) {
						// モード変更１
						this.status[i].eCurrentMode = EMode.ProgressDisplaying;
						goto Retry;
					}
					if ((OpenTaiko.Timer.NowTimeMs - this.status[i].nComboCutTime) > 1000) {
						// モード変更２
						this.status[i].eCurrentMode = EMode.Hidden;
						goto Retry;
					}
					this.status[i].nCurrentDisplayingCOMBOValue = this.status[i].nCOMBOValue;
					break;
					//-----------------
					#endregion
			}
		}

		return 0;
	}
}
