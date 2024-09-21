using System.Drawing;
using System.Runtime.InteropServices;
using FDK;

namespace OpenTaiko {
	internal class CAct演奏スコア共通 : CActivity {
		// プロパティ

		protected long[] nScoreIncrease;
		protected double[] nCurrentRealScore;
		protected long[] nCurrentlyDisplayedScore;
		//protected CTexture txScore;

		//      protected CTexture txScore_1P;
		protected CCounter ctTimer;
		public CCounter[] ct点数アニメタイマ;

		public CCounter[] ctボーナス加算タイマ;

		protected STスコア[] stScore;
		protected int nNowDisplayedAddScore;

		[StructLayout(LayoutKind.Sequential)]
		protected struct STスコア {
			public bool bAddEnd;
			public bool b使用中;
			public bool b表示中;
			public bool bBonusScore;
			public CCounter ctTimer;
			public int nAddScore;
			public int nPlayer;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct ST文字位置 {
			public char ch;
			public Point pt;
		}
		private ST文字位置[] stFont;


		public long GetScore(int player) {
			return nCurrentlyDisplayedScore[player];
		}

		// コンストラクタ

		public CAct演奏スコア共通() {
			ST文字位置[] st文字位置Array = new ST文字位置[11];
			ST文字位置 st文字位置 = new ST文字位置();
			st文字位置.ch = '0';
			st文字位置.pt = new Point(0, 0);
			st文字位置Array[0] = st文字位置;
			ST文字位置 st文字位置2 = new ST文字位置();
			st文字位置2.ch = '1';
			st文字位置2.pt = new Point(24, 0);
			st文字位置Array[1] = st文字位置2;
			ST文字位置 st文字位置3 = new ST文字位置();
			st文字位置3.ch = '2';
			st文字位置3.pt = new Point(48, 0);
			st文字位置Array[2] = st文字位置3;
			ST文字位置 st文字位置4 = new ST文字位置();
			st文字位置4.ch = '3';
			st文字位置4.pt = new Point(72, 0);
			st文字位置Array[3] = st文字位置4;
			ST文字位置 st文字位置5 = new ST文字位置();
			st文字位置5.ch = '4';
			st文字位置5.pt = new Point(96, 0);
			st文字位置Array[4] = st文字位置5;
			ST文字位置 st文字位置6 = new ST文字位置();
			st文字位置6.ch = '5';
			st文字位置6.pt = new Point(120, 0);
			st文字位置Array[5] = st文字位置6;
			ST文字位置 st文字位置7 = new ST文字位置();
			st文字位置7.ch = '6';
			st文字位置7.pt = new Point(144, 0);
			st文字位置Array[6] = st文字位置7;
			ST文字位置 st文字位置8 = new ST文字位置();
			st文字位置8.ch = '7';
			st文字位置8.pt = new Point(168, 0);
			st文字位置Array[7] = st文字位置8;
			ST文字位置 st文字位置9 = new ST文字位置();
			st文字位置9.ch = '8';
			st文字位置9.pt = new Point(192, 0);
			st文字位置Array[8] = st文字位置9;
			ST文字位置 st文字位置10 = new ST文字位置();
			st文字位置10.ch = '9';
			st文字位置10.pt = new Point(216, 0);
			st文字位置Array[9] = st文字位置10;
			this.stFont = st文字位置Array;

			this.stScore = new STスコア[256];
			base.IsDeActivated = true;
		}


		// メソッド

		private float[,] n点数アニメ拡大率_座標 = new float[,]
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
				this.nScoreIncrease[player] = (long)(((double)(this.nCurrentRealScore[player] - this.nCurrentlyDisplayedScore[player])) / 20.0);
				if (this.nScoreIncrease[player] < 1L) {
					this.nScoreIncrease[player] = 1L;
				}
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

			delta = (long)(delta * OpenTaiko.stageSongSelect.actPlayOption.tGetModMultiplier(CActPlayOption.EBalancingType.SCORE, false, player));

			this.ctTimer = new CCounter(0, 400, 1, OpenTaiko.Timer);

			for (int sc = 0; sc < 1; sc++) {
				for (int i = 0; i < 256; i++) {
					if (this.stScore[i].b使用中 == false) {
						this.stScore[i].b使用中 = true;
						this.stScore[i].b表示中 = true;
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
					if (this.stScore[i].b使用中 == false) {
						this.stScore[i].b使用中 = true;
						this.stScore[i].b表示中 = true;
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
			this.nScoreIncrease = new long[5] { 0L, 0L, 0L, 0L, 0L };

			for (int sc = 0; sc < 256; sc++) {
				this.stScore[sc].b使用中 = false;
				this.stScore[sc].ctTimer = new CCounter();
				this.stScore[sc].nAddScore = 0;
				this.stScore[sc].bBonusScore = false;
				this.stScore[sc].bAddEnd = false;
			}

			this.nNowDisplayedAddScore = 0;

			this.ctTimer = new CCounter();

			this.ct点数アニメタイマ = new CCounter[5];
			for (int i = 0; i < 5; i++) {
				this.ct点数アニメタイマ[i] = new CCounter();
			}
			this.ctボーナス加算タイマ = new CCounter[5];
			for (int i = 0; i < 5; i++) {
				this.ctボーナス加算タイマ[i] = new CCounter();
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

		protected void t小文字表示(int x, int y, string str, int mode, int alpha, int player) {
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
										OpenTaiko.Tx.Taiko_Score[0].vcScaleRatio.Y = ScoreScale[this.ct点数アニメタイマ[player].CurrentValue];
									}
									OpenTaiko.Tx.Taiko_Score[0].t2D拡大率考慮下基準描画(x, y, rectangle);

								}
								break;
							case 1:
								if (OpenTaiko.Tx.Taiko_Score[1] != null) {
									//this.txScore.color4 = new SlimDX.Color4( 1.0f, 0.5f, 0.4f );
									//this.txScore.color4 = CDTXMania.Skin.cScoreColor1P;
									OpenTaiko.Tx.Taiko_Score[1].Opacity = alpha;
									OpenTaiko.Tx.Taiko_Score[1].vcScaleRatio.Y = 1;
									OpenTaiko.Tx.Taiko_Score[1].t2D拡大率考慮下基準描画(x, y, rectangle);
								}
								break;
							case 2:
								if (OpenTaiko.Tx.Taiko_Score[2] != null) {
									//this.txScore.color4 = new SlimDX.Color4( 0.4f, 0.5f, 1.0f );
									//this.txScore.color4 = CDTXMania.Skin.cScoreColor2P;
									OpenTaiko.Tx.Taiko_Score[2].Opacity = alpha;
									OpenTaiko.Tx.Taiko_Score[2].vcScaleRatio.Y = 1;
									OpenTaiko.Tx.Taiko_Score[2].t2D拡大率考慮下基準描画(x, y, rectangle);
								}
								break;
							case 3:
								if (OpenTaiko.Tx.Taiko_Score[3] != null) {
									//this.txScore.color4 = new SlimDX.Color4( 0.4f, 0.5f, 1.0f );
									//this.txScore.color4 = CDTXMania.Skin.cScoreColor2P;
									OpenTaiko.Tx.Taiko_Score[3].Opacity = alpha;
									OpenTaiko.Tx.Taiko_Score[3].vcScaleRatio.Y = 1;
									OpenTaiko.Tx.Taiko_Score[3].t2D拡大率考慮下基準描画(x, y, rectangle);
								}
								break;
							case 4:
								if (OpenTaiko.Tx.Taiko_Score[4] != null) {
									//this.txScore.color4 = new SlimDX.Color4( 0.4f, 0.5f, 1.0f );
									//this.txScore.color4 = CDTXMania.Skin.cScoreColor2P;
									OpenTaiko.Tx.Taiko_Score[4].Opacity = alpha;
									OpenTaiko.Tx.Taiko_Score[4].vcScaleRatio.Y = 1;
									OpenTaiko.Tx.Taiko_Score[4].t2D拡大率考慮下基準描画(x, y, rectangle);
								}
								break;
							case 5:
								if (OpenTaiko.Tx.Taiko_Score[5] != null) {
									//this.txScore.color4 = new SlimDX.Color4( 0.4f, 0.5f, 1.0f );
									//this.txScore.color4 = CDTXMania.Skin.cScoreColor2P;
									OpenTaiko.Tx.Taiko_Score[5].Opacity = alpha;
									OpenTaiko.Tx.Taiko_Score[5].vcScaleRatio.Y = 1;
									OpenTaiko.Tx.Taiko_Score[5].t2D拡大率考慮下基準描画(x, y, rectangle);
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
}
