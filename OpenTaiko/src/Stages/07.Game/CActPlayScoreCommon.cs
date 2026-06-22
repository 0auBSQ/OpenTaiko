using System.Drawing;
using System.Runtime.InteropServices;
using FDK;

namespace OpenTaiko;

internal class CActPlayScoreCommon : CActivity {
	// Properties

	protected double[] nCurrentRealScore;
	protected long[] nCurrentlyDisplayedScore;
	//protected CTexture txScore;

	//      protected CTexture txScore_1P;
	protected CCounter ctTimer;
	public CCounter[] ctPointsAnimeTimer;

	public CCounter[] ctBonusAddTimer;

	protected STScore[] stScore;
	protected int nNowDisplayedAddScore;

	[StructLayout(LayoutKind.Sequential)]
	protected struct STScore {
		public bool bAddEnd;
		public bool bUse;
		public bool bDisplaying;
		public bool bBonusScore;
		public CCounter ctTimer;
		public int nAddScore;
		public int nPlayer;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct STTextPosition {
		public char ch;
		public Point pt;
	}
	private STTextPosition[] stFont;


	public long GetDisplayedScore(int player) {
		return nCurrentlyDisplayedScore[player];
	}

	// コンストラクタ

	public CActPlayScoreCommon() {
		STTextPosition[] stTextPositionArray = new STTextPosition[11];
		STTextPosition stTextPosition = new STTextPosition();
		stTextPosition.ch = '0';
		stTextPosition.pt = new Point(0, 0);
		stTextPositionArray[0] = stTextPosition;
		STTextPosition stTextPosition2 = new STTextPosition();
		stTextPosition2.ch = '1';
		stTextPosition2.pt = new Point(24, 0);
		stTextPositionArray[1] = stTextPosition2;
		STTextPosition stTextPosition3 = new STTextPosition();
		stTextPosition3.ch = '2';
		stTextPosition3.pt = new Point(48, 0);
		stTextPositionArray[2] = stTextPosition3;
		STTextPosition stTextPosition4 = new STTextPosition();
		stTextPosition4.ch = '3';
		stTextPosition4.pt = new Point(72, 0);
		stTextPositionArray[3] = stTextPosition4;
		STTextPosition stTextPosition5 = new STTextPosition();
		stTextPosition5.ch = '4';
		stTextPosition5.pt = new Point(96, 0);
		stTextPositionArray[4] = stTextPosition5;
		STTextPosition stTextPosition6 = new STTextPosition();
		stTextPosition6.ch = '5';
		stTextPosition6.pt = new Point(120, 0);
		stTextPositionArray[5] = stTextPosition6;
		STTextPosition stTextPosition7 = new STTextPosition();
		stTextPosition7.ch = '6';
		stTextPosition7.pt = new Point(144, 0);
		stTextPositionArray[6] = stTextPosition7;
		STTextPosition stTextPosition8 = new STTextPosition();
		stTextPosition8.ch = '7';
		stTextPosition8.pt = new Point(168, 0);
		stTextPositionArray[7] = stTextPosition8;
		STTextPosition stTextPosition9 = new STTextPosition();
		stTextPosition9.ch = '8';
		stTextPosition9.pt = new Point(192, 0);
		stTextPositionArray[8] = stTextPosition9;
		STTextPosition stTextPosition10 = new STTextPosition();
		stTextPosition10.ch = '9';
		stTextPosition10.pt = new Point(216, 0);
		stTextPositionArray[9] = stTextPosition10;
		this.stFont = stTextPositionArray;

		this.stScore = new STScore[256];
		base.IsDeActivated = true;
	}


	// メソッド

	private float[,] nPointsAnimeScale_Coord = new float[,]
	{
		{
			1.14f,
			-5f
		},
		{
			1.2f,
			-6f
		},
		{
			1.23f,
			-8f
		},
		{
			1.25f,
			-9f
		},
		{
			1.23f,
			-8f
		},
		{
			1.2f,
			-6f
		},
		{
			1.14f,
			-5f
		},
		{
			1.08f,
			-4f
		},
		{
			1.04f,
			-2f
		},
		{
			1.02f,
			-1f
		},
		{
			1.01f,
			-1f
		},
		{
			1f,
			0f
		}
	};

	private float[] ScoreScale = new float[]
	{
		1f,
		1.050f,
		1.100f,
		1.110f,
		1.120f,
		1.125f,
		1.120f,
		1.080f,
		1.065f,
		1.030f,
		1.015f,
		1f
	};

	public double Get(int player) {
		return this.nCurrentRealScore[player];
	}
	public void Set(double nScore, int player) {
		if (this.nCurrentRealScore[player] != nScore) {
			this.nCurrentRealScore[player] = nScore;
			this.nCurrentlyDisplayedScore[player] = (long)this.nCurrentRealScore[player];
		}

	}
	/// <summary>
	/// 点数を加える(各種AUTO補正つき)
	/// </summary>
	/// <param name="part"></param>
	/// <param name="bAutoPlay"></param>
	/// <param name="delta"></param>
	public void Add(long delta, int player) {
		if (OpenTaiko.ConfigIni.bAIBattleMode && player == 1) return;

		double rev = 1.0;

		delta = (long)(delta * CModBalancing.tGetModMultiplier(CModBalancing.EBalancingType.SCORE, player));

		this.ctTimer = new CCounter(0, 400, 1, OpenTaiko.Timer);

		for (int sc = 0; sc < 1; sc++) {
			for (int i = 0; i < 256; i++) {
				if (this.stScore[i].bUse == false) {
					this.stScore[i].bUse = true;
					this.stScore[i].bDisplaying = true;
					this.stScore[i].nAddScore = (int)delta;
					this.stScore[i].ctTimer = new CCounter(0, 465, 2, OpenTaiko.Timer);
					this.stScore[i].bBonusScore = false;
					this.stScore[i].bAddEnd = false;
					this.stScore[i].nPlayer = player;
					this.nNowDisplayedAddScore++;
					break;
				}
			}
		}

		this.Set(this.Get(player) + delta * rev, player);
	}

	public void BonusAdd(int player) {
		if (OpenTaiko.ConfigIni.bAIBattleMode && player == 1) return;

		for (int sc = 0; sc < 1; sc++) {
			for (int i = 0; i < 256; i++) {
				if (this.stScore[i].bUse == false) {
					this.stScore[i].bUse = true;
					this.stScore[i].bDisplaying = true;
					this.stScore[i].nAddScore = 10000;
					this.stScore[i].ctTimer = new CCounter(0, 100, 4, OpenTaiko.Timer);
					this.stScore[i].bBonusScore = true;
					this.stScore[i].bAddEnd = false;
					this.stScore[i].nPlayer = player;
					this.nNowDisplayedAddScore++;
					break;
				}
			}
		}

		this.Set(this.Get(player) + 10000, player);
	}

	// CActivity 実装

	public override void Activate() {
		this.nCurrentlyDisplayedScore = new long[5] { 0L, 0L, 0L, 0L, 0L };
		this.nCurrentRealScore = new double[5] { 0L, 0L, 0L, 0L, 0L };

		for (int sc = 0; sc < 256; sc++) {
			this.stScore[sc].bUse = false;
			this.stScore[sc].ctTimer = new CCounter();
			this.stScore[sc].nAddScore = 0;
			this.stScore[sc].bBonusScore = false;
			this.stScore[sc].bAddEnd = false;
		}

		this.nNowDisplayedAddScore = 0;

		this.ctTimer = new CCounter();

		this.ctPointsAnimeTimer = new CCounter[5];
		for (int i = 0; i < 5; i++) {
			this.ctPointsAnimeTimer[i] = new CCounter();
		}
		this.ctBonusAddTimer = new CCounter[5];
		for (int i = 0; i < 5; i++) {
			this.ctBonusAddTimer[i] = new CCounter();
		}
		base.Activate();
	}
	public override void CreateManagedResource() {
		//this.txScore = CDTXMania.tテクスチャの生成( CSkin.Path( @"Graphics\7_Score_number.png" ) );
		//      this.txScore_1P = CDTXMania.tテクスチャの生成(CSkin.Path(@"Graphics\7_Score_number_1P.png"));
		base.CreateManagedResource();
	}
	public override void ReleaseManagedResource() {
		//CDTXMania.tテクスチャの解放( ref this.txScore );
		//      CDTXMania.tテクスチャの解放(ref this.txScore_1P);
		base.ReleaseManagedResource();
	}

	protected void tSmallDisplay(int x, int y, string str, int mode, int alpha, int player) {
		foreach (char ch in str) {
			for (int i = 0; i < this.stFont.Length; i++) {
				if (this.stFont[i].ch == ch) {
					Rectangle rectangle = new Rectangle(OpenTaiko.Skin.Game_Score_Size[0] * i, 0, OpenTaiko.Skin.Game_Score_Size[0], OpenTaiko.Skin.Game_Score_Size[1]);
					switch (mode) {
						case 0:
							if (OpenTaiko.Tx.Taiko_Score[0] != null) {
								//this.txScore.color4 = new SlimDX.Color4( 1.0f, 1.0f, 1.0f );
								OpenTaiko.Tx.Taiko_Score[0].Opacity = alpha;
								if (OpenTaiko.ConfigIni.SimpleMode) {
									OpenTaiko.Tx.Taiko_Score[0].vcScaleRatio.Y = 1;
								} else {
									OpenTaiko.Tx.Taiko_Score[0].vcScaleRatio.Y = ScoreScale[this.ctPointsAnimeTimer[player].CurrentValue];
								}
								OpenTaiko.Tx.Taiko_Score[0].t2DScaledBottomBasedDraw(x, y, rectangle);

							}
							break;
						case 1:
							if (OpenTaiko.Tx.Taiko_Score[1] != null) {
								//this.txScore.color4 = new SlimDX.Color4( 1.0f, 0.5f, 0.4f );
								//this.txScore.color4 = CDTXMania.Skin.cScoreColor1P;
								OpenTaiko.Tx.Taiko_Score[1].Opacity = alpha;
								OpenTaiko.Tx.Taiko_Score[1].vcScaleRatio.Y = 1;
								OpenTaiko.Tx.Taiko_Score[1].t2DScaledBottomBasedDraw(x, y, rectangle);
							}
							break;
						case 2:
							if (OpenTaiko.Tx.Taiko_Score[2] != null) {
								//this.txScore.color4 = new SlimDX.Color4( 0.4f, 0.5f, 1.0f );
								//this.txScore.color4 = CDTXMania.Skin.cScoreColor2P;
								OpenTaiko.Tx.Taiko_Score[2].Opacity = alpha;
								OpenTaiko.Tx.Taiko_Score[2].vcScaleRatio.Y = 1;
								OpenTaiko.Tx.Taiko_Score[2].t2DScaledBottomBasedDraw(x, y, rectangle);
							}
							break;
						case 3:
							if (OpenTaiko.Tx.Taiko_Score[3] != null) {
								//this.txScore.color4 = new SlimDX.Color4( 0.4f, 0.5f, 1.0f );
								//this.txScore.color4 = CDTXMania.Skin.cScoreColor2P;
								OpenTaiko.Tx.Taiko_Score[3].Opacity = alpha;
								OpenTaiko.Tx.Taiko_Score[3].vcScaleRatio.Y = 1;
								OpenTaiko.Tx.Taiko_Score[3].t2DScaledBottomBasedDraw(x, y, rectangle);
							}
							break;
						case 4:
							if (OpenTaiko.Tx.Taiko_Score[4] != null) {
								//this.txScore.color4 = new SlimDX.Color4( 0.4f, 0.5f, 1.0f );
								//this.txScore.color4 = CDTXMania.Skin.cScoreColor2P;
								OpenTaiko.Tx.Taiko_Score[4].Opacity = alpha;
								OpenTaiko.Tx.Taiko_Score[4].vcScaleRatio.Y = 1;
								OpenTaiko.Tx.Taiko_Score[4].t2DScaledBottomBasedDraw(x, y, rectangle);
							}
							break;
						case 5:
							if (OpenTaiko.Tx.Taiko_Score[5] != null) {
								//this.txScore.color4 = new SlimDX.Color4( 0.4f, 0.5f, 1.0f );
								//this.txScore.color4 = CDTXMania.Skin.cScoreColor2P;
								OpenTaiko.Tx.Taiko_Score[5].Opacity = alpha;
								OpenTaiko.Tx.Taiko_Score[5].vcScaleRatio.Y = 1;
								OpenTaiko.Tx.Taiko_Score[5].t2DScaledBottomBasedDraw(x, y, rectangle);
							}
							break;
					}
					break;
				}
			}
			x += OpenTaiko.Skin.Game_Score_Padding;
		}
	}
}
