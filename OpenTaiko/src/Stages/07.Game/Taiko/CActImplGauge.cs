using System.Drawing;
using System.Runtime.InteropServices;
using FDK;

namespace OpenTaiko;

internal class CActImplGauge : CActPlayGaugeCommon {
	// コンストラクタ
	/// <summary>
	/// ゲージの描画クラス。ドラム側。
	///
	/// 課題
	/// _ゲージの実装。
	/// _Danger時にゲージの色が変わる演出の実装。
	/// _Danger、MAX時のアニメーション実装。
	/// </summary>
	public CActImplGauge() {
		base.IsDeActivated = true;
	}

	public override void Start(NotesManager.ENoteType nLane, EGameType gameType, ENoteJudge judge, int player) {
		for (int j = 0; j < 32; j++) {
			if (player == 0) {
				if (!this.stFireworkState[player][j].bUse) {
					this.stFireworkState[player][j].ctProgress = new CCounter(0, 10, 20, OpenTaiko.Timer);
					this.stFireworkState[player][j].nPlayer = player;
					this.stFireworkState[player][j].nLane = nLane;
					this.stFireworkState[player][j].gameType = gameType;
					this.stFireworkState[player][j].isBig = NotesManager.IsBigNoteTaiko(nLane, gameType) || NotesManager.IsBigRollTaiko(nLane, gameType) || NotesManager.IsBalloon(nLane);
					this.stFireworkState[player][j].bUse = true;
					break;
				}
			}
		}
	}

	// CActivity 実装

	public override void Activate() {
		this.ctFlame = new CCounter(0, 6, 50, OpenTaiko.Timer);

		for (int player = 0; player < 5; player++) {
			for (int i = 0; i < 32; i++) {
				this.stFireworkState[player][i].ctProgress = new CCounter();
			}
		}

		if (OpenTaiko.Skin.Game_Gauge_Rainbow_Timer <= 1) {
			throw new DivideByZeroException("SkinConfigの設定\"Game_Gauge_Rainbow_Timer\"を1以下にすることは出来ません。");
		}
		this.ctRainbowAnime = new CCounter(0, OpenTaiko.Skin.Game_Gauge_Rainbow_Ptn - 1, OpenTaiko.Skin.Game_Gauge_Rainbow_Timer, OpenTaiko.Timer);
		this.ctRainbowOpacity = new CCounter(0, OpenTaiko.Skin.Game_Gauge_Rainbow_Timer - 1, 1, OpenTaiko.Timer);
		this.ctGaugeFlash = new CCounter(0, 532, 1, OpenTaiko.Timer);

		base.Activate();
	}
	public override void DeActivate() {
		for (int player = 0; player < 5; player++) {
			for (int i = 0; i < 32; i++) {
				this.stFireworkState[player][i].ctProgress = null;
			}
		}
		this.ctFlame = null;

		this.ctRainbowAnime = null;
	}
	public override void CreateManagedResource() {
		//this.tx音符 = CDTXMania.tテクスチャの生成(CSkin.Path(@"Graphics\7_taiko_notes.png"));
		base.CreateManagedResource();
	}
	public override void ReleaseManagedResource() {
		base.ReleaseManagedResource();
	}
	public override int Draw() {
		if (!base.IsDeActivated) {
			//CDTXMania.act文字コンソール.tPrint( 20, 150, C文字コンソール.Eフォント種別.白, this.db現在のゲージ値.Taiko.ToString() );

			#region [ 初めての進行描画 ]
			if (base.IsFirstDraw) {
				base.IsFirstDraw = false;
			}
			#endregion

			this.ctGaugeFlash.TickLoop();

			int nWidth = (OpenTaiko.Skin.Game_Gauge_Rect[2] / 50);
			for (int g = 0; g < 5; g++)
				nRectX[g] = (int)(this.dbCurrentGaugeValue[g] / 2) * nWidth;
			int RainbowBase = ctRainbowAnime.CurrentValue + 1;
			if (RainbowBase == ctRainbowAnime.EndValue + 1) RainbowBase = 0;
			/*

            新虹ゲージの仕様  2018/08/10 ろみゅ～？

             フェードで動く虹ゲージが、ある程度強化できたので放出。
             透明度255の虹ベースを描画し、その上から透明度可変式の虹ゲージを描画する。
             ゲージのパターン枚数は、読み込み枚数によって決定する。
             ゲージ描画の切り替え速度は、タイマーの値をSkinConfigで指定して行う(初期値50,1にするとエラーを吐く模様)。進行速度は1ms、高フレームレートでの滑らかさを重視。
             虹ゲージの透明度調整値は、「255/パターン数」で算出する。
             こんな簡単なことを考えるのに30分(60f/s換算で108000f)を費やす。

            */

			// No gauge if tower
			if (OpenTaiko.SongMount.nChoosenSongDifficulty[0] == (int)Difficulty.Tower || OpenTaiko.ConfigIni.bTokkunMode)
				return 0;


			if (OpenTaiko.SongMount.nChoosenSongDifficulty[0] != (int)Difficulty.Dan) {
				#region [Regular gauges]

				// Flash opacity
				int Opacity = 0;
				if (this.ctGaugeFlash.CurrentValue <= 365) Opacity = 0;
				else if (this.ctGaugeFlash.CurrentValue <= 448) Opacity = (int)((this.ctGaugeFlash.CurrentValue - 365) / 83f * 255f);
				else if (this.ctGaugeFlash.CurrentValue <= 531) Opacity = 255 - (int)((this.ctGaugeFlash.CurrentValue - 448) / 83f * 255f);

				// Rainbow gauge
				this.ctRainbowAnime.TickLoop();
				this.ctRainbowOpacity.TickLoop();
				int rainbowFrame = this.ctRainbowAnime.CurrentValue;

				// Soul fire frame
				this.ctFlame.TickLoop();
				int soulFireFrame = this.ctFlame.CurrentValue;

				for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
					if (OpenTaiko.ConfigIni.bAIBattleMode && i == 1) continue;
					HGaugeMethods.UNSAFE_DrawGaugeFast(i, Opacity, rainbowFrame, soulFireFrame);
				}

				#endregion
			} else {
				float scale = 1.0f;
				if (OpenTaiko.ConfigIni.bAIBattleMode) {
					scale = 0.8f;
				}

				for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
					if (OpenTaiko.ConfigIni.nPlayerCount == 5) {
						gauge_x[i] = OpenTaiko.Skin.Game_Gauge_5P[0] + (OpenTaiko.Skin.Game_UIMove_5P[0] * i);
						gauge_y[i] = OpenTaiko.Skin.Game_Gauge_5P[1] + (OpenTaiko.Skin.Game_UIMove_5P[1] * i);
					} else if (OpenTaiko.ConfigIni.nPlayerCount == 4 || OpenTaiko.ConfigIni.nPlayerCount == 3) {
						gauge_x[i] = OpenTaiko.Skin.Game_Gauge_4P[0] + (OpenTaiko.Skin.Game_UIMove_4P[0] * i);
						gauge_y[i] = OpenTaiko.Skin.Game_Gauge_4P[1] + (OpenTaiko.Skin.Game_UIMove_4P[1] * i);
					} else {
						gauge_x[i] = OpenTaiko.Skin.Game_Gauge_X[i];
						gauge_y[i] = OpenTaiko.Skin.Game_Gauge_Y[i];
					}
				}

				#region [Gauge base]

				if (OpenTaiko.SongMount.nChoosenSongDifficulty[0] == (int)Difficulty.Dan) {
					if (OpenTaiko.P1IsBlue()) {
						OpenTaiko.Tx.Gauge_Dan[4]?.t2DDraw(gauge_x[0], gauge_y[0],
							new Rectangle(OpenTaiko.Skin.Game_Gauge_Rect[0], OpenTaiko.Skin.Game_Gauge_Rect[1], OpenTaiko.Skin.Game_Gauge_Rect[2], OpenTaiko.Skin.Game_Gauge_Rect[3]));
					} else {
						OpenTaiko.Tx.Gauge_Dan[0]?.t2DDraw(gauge_x[0], gauge_y[0],
							new Rectangle(OpenTaiko.Skin.Game_Gauge_Rect[0], OpenTaiko.Skin.Game_Gauge_Rect[1], OpenTaiko.Skin.Game_Gauge_Rect[2], OpenTaiko.Skin.Game_Gauge_Rect[3]));
					}


					if (OpenTaiko.Tx.Gauge_Dan[2] != null) {
						for (int i = 0; i < OpenTaiko.TJA.Dan_C.Length; i++) {
							if (OpenTaiko.TJA.Dan_C[i] != null) {
								Dan_C dan_c = OpenTaiko.TJA.List_DanSongs[OpenTaiko.stageGameScreen.actDan.NowCymbolShowingNumber].Dan_C[i] != null ?
									OpenTaiko.TJA.List_DanSongs[OpenTaiko.stageGameScreen.actDan.NowCymbolShowingNumber].Dan_C[i] :
									OpenTaiko.TJA.Dan_C[i];

								if (dan_c.ExamType == Exam.Type.Gauge) {
									OpenTaiko.Tx.Gauge_Dan[2].t2DDraw(gauge_x[0] + (dan_c.GetValue()[0] / 2 * nWidth), gauge_y[0],
										new Rectangle((dan_c.GetValue()[0] / 2 * nWidth), 0, OpenTaiko.Skin.Game_Gauge_Rect[2] - (dan_c.GetValue()[0] / 2 * nWidth), OpenTaiko.Skin.Game_Gauge_Rect[3]));
								}
							}
						}
					}
				}

				#endregion

				#region [ Gauge 1P ]

				if (OpenTaiko.Tx.Gauge[0] != null) {
					int x;
					int y;
					if (OpenTaiko.ConfigIni.bAIBattleMode) {
						x = OpenTaiko.Skin.Game_Gauge_X_AI;
						y = OpenTaiko.Skin.Game_Gauge_Y_AI;
					} else {
						x = gauge_x[0];
						y = gauge_y[0];
					}

					if (OpenTaiko.SongMount.nChoosenSongDifficulty[0] == (int)Difficulty.Dan) {
						if (OpenTaiko.P1IsBlue())
							OpenTaiko.Tx.Gauge_Dan[5]?.t2DDraw(x, y, new Rectangle(0, 0, nRectX[0], OpenTaiko.Skin.Game_Gauge_Rect[3]));
						else
							OpenTaiko.Tx.Gauge_Dan[1]?.t2DDraw(x, y, new Rectangle(0, 0, nRectX[0], OpenTaiko.Skin.Game_Gauge_Rect[3]));

						for (int i = 0; i < OpenTaiko.TJA.Dan_C.Length; i++) {
							Dan_C dan_c = OpenTaiko.TJA.List_DanSongs[OpenTaiko.stageGameScreen.actDan.NowCymbolShowingNumber].Dan_C[i] != null ?
								OpenTaiko.TJA.List_DanSongs[OpenTaiko.stageGameScreen.actDan.NowCymbolShowingNumber].Dan_C[i] :
								OpenTaiko.TJA.Dan_C[i];

							if (dan_c != null && dan_c.ExamType == Exam.Type.Gauge && dbCurrentGaugeValue[0] >= dan_c.GetValue()[0]) {
								OpenTaiko.Tx.Gauge_Dan[3].Opacity = 255;
								OpenTaiko.Tx.Gauge_Dan[3]?.t2DDraw(x + (dan_c.GetValue()[0] / 2 * nWidth), y, new Rectangle(dan_c.GetValue()[0] / 2 * nWidth, 0, nRectX[0] - (dan_c.GetValue()[0] / 2 * nWidth), OpenTaiko.Skin.Game_Gauge_Rect[3]));

								int Opacity = 0;
								if (this.ctGaugeFlash.CurrentValue <= 365) Opacity = 0;
								else if (this.ctGaugeFlash.CurrentValue <= 448) Opacity = (int)((this.ctGaugeFlash.CurrentValue - 365) / 83f * 255f);
								else if (this.ctGaugeFlash.CurrentValue <= 531) Opacity = 255 - (int)((this.ctGaugeFlash.CurrentValue - 448) / 83f * 255f);
								OpenTaiko.Tx.Gauge_Dan[3].Opacity = Opacity;
								OpenTaiko.Tx.Gauge_Dan[3]?.t2DDraw(x, y, new Rectangle(0, 0, dan_c.GetValue()[0] / 2 * nWidth, OpenTaiko.Skin.Game_Gauge_Rect[3]));

								break;
							}
						}

					}

					#region [Rainbow]

					if (this.dbCurrentGaugeValue[0] >= 100.0) {
						this.ctRainbowAnime.TickLoop();
						this.ctRainbowOpacity.TickLoop();
						if (OpenTaiko.Tx.Gauge_Rainbow[this.ctRainbowAnime.CurrentValue] != null) {
							OpenTaiko.Tx.Gauge_Rainbow[this.ctRainbowAnime.CurrentValue].vcScaleRatio.X = scale;
							OpenTaiko.Tx.Gauge_Rainbow[this.ctRainbowAnime.CurrentValue].vcScaleRatio.Y = scale;

							OpenTaiko.Tx.Gauge_Rainbow[RainbowBase].vcScaleRatio.X = scale;
							OpenTaiko.Tx.Gauge_Rainbow[RainbowBase].vcScaleRatio.Y = scale;

							bool smart = OpenTaiko.ConfigIni.nPlayerCount > 2 || OpenTaiko.SongMount.nChoosenSongDifficulty[0] == (int)Difficulty.Dan;


							OpenTaiko.Tx.Gauge_Rainbow[this.ctRainbowAnime.CurrentValue].Opacity = 255;
							OpenTaiko.Tx.Gauge_Rainbow[this.ctRainbowAnime.CurrentValue].t2DDraw(x, y + (smart ? (OpenTaiko.Skin.Game_Gauge_Rect[3] / 2) : 0),
								new RectangleF(0,
									smart ? (OpenTaiko.Skin.Game_Gauge_Rect[3] / 2) : 0,
									OpenTaiko.Tx.Gauge_Rainbow[this.ctRainbowAnime.CurrentValue].szTextureSize.Width,
									smart ? OpenTaiko.Tx.Gauge_Rainbow[this.ctRainbowAnime.CurrentValue].szTextureSize.Height - (OpenTaiko.Skin.Game_Gauge_Rect[3] / 2) : OpenTaiko.Tx.Gauge_Rainbow[this.ctRainbowAnime.CurrentValue].szTextureSize.Height));


							OpenTaiko.Tx.Gauge_Rainbow[RainbowBase].Opacity = (ctRainbowOpacity.CurrentValue * 255 / (int)ctRainbowOpacity.EndValue) / 1;
							OpenTaiko.Tx.Gauge_Rainbow[RainbowBase].t2DDraw(x, y + (smart ? (OpenTaiko.Skin.Game_Gauge_Rect[3] / 2) : 0),
								new RectangleF(0,
									smart ? (OpenTaiko.Skin.Game_Gauge_Rect[3] / 2) : 0,
									OpenTaiko.Tx.Gauge_Rainbow[RainbowBase].szTextureSize.Width,
									smart ? OpenTaiko.Tx.Gauge_Rainbow[RainbowBase].szTextureSize.Height - (OpenTaiko.Skin.Game_Gauge_Rect[3] / 2) : OpenTaiko.Tx.Gauge_Rainbow[RainbowBase].szTextureSize.Height));
						}
					}

					#endregion

					if (OpenTaiko.Tx.Gauge_Line[0] != null) {

						OpenTaiko.Tx.Gauge_Line[0].vcScaleRatio.X = scale;
						OpenTaiko.Tx.Gauge_Line[0].vcScaleRatio.Y = scale;

						OpenTaiko.Tx.Gauge_Line[0].t2DDraw(x, y);
					}

				}

				#endregion

				// Soul fire here
				if (OpenTaiko.Tx.Gauge_Soul_Fire != null) {
					//仮置き
					int soulfire_width = OpenTaiko.Tx.Gauge_Soul_Fire.szTextureSize.Width / 8;
					int soulfire_height = OpenTaiko.Tx.Gauge_Soul_Fire.szTextureSize.Height;
					for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
						if (OpenTaiko.ConfigIni.bAIBattleMode && i == 1) break;

						int x;
						int y;
						if (OpenTaiko.ConfigIni.bAIBattleMode) {
							x = OpenTaiko.Skin.Gauge_Soul_Fire_X_AI;
							y = OpenTaiko.Skin.Gauge_Soul_Fire_Y_AI;
						} else {
							if (OpenTaiko.ConfigIni.nPlayerCount == 5) {
								x = OpenTaiko.Skin.Gauge_Soul_Fire_5P[0] + (OpenTaiko.Skin.Game_UIMove_5P[0] * i);
								y = OpenTaiko.Skin.Gauge_Soul_Fire_5P[1] + (OpenTaiko.Skin.Game_UIMove_5P[1] * i);
							} else if (OpenTaiko.ConfigIni.nPlayerCount == 4 || OpenTaiko.ConfigIni.nPlayerCount == 3) {
								x = OpenTaiko.Skin.Gauge_Soul_Fire_4P[0] + (OpenTaiko.Skin.Game_UIMove_4P[0] * i);
								y = OpenTaiko.Skin.Gauge_Soul_Fire_4P[1] + (OpenTaiko.Skin.Game_UIMove_4P[1] * i);
							} else {
								x = OpenTaiko.Skin.Gauge_Soul_Fire_X[i];
								y = OpenTaiko.Skin.Gauge_Soul_Fire_Y[i];
							}
						}

						if (this.dbCurrentGaugeValue[i] >= 100.0) {
							this.ctFlame.TickLoop();

							OpenTaiko.Tx.Gauge_Soul_Fire.vcScaleRatio.X = scale;
							OpenTaiko.Tx.Gauge_Soul_Fire.vcScaleRatio.Y = scale;

							OpenTaiko.Tx.Gauge_Soul_Fire.t2DDraw(x, y, new Rectangle(soulfire_width * (this.ctFlame.CurrentValue), 0, soulfire_width, soulfire_height));
						}
					}
				}
				if (OpenTaiko.Tx.Gauge_Soul != null) {
					//仮置き
					int soul_height = OpenTaiko.Tx.Gauge_Soul.szTextureSize.Height / 2;
					for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
						if (OpenTaiko.ConfigIni.bAIBattleMode && i == 1) break;

						int x;
						int y;
						if (OpenTaiko.ConfigIni.bAIBattleMode) {
							x = OpenTaiko.Skin.Gauge_Soul_X_AI;
							y = OpenTaiko.Skin.Gauge_Soul_Y_AI;
						} else {
							if (OpenTaiko.ConfigIni.nPlayerCount == 5) {
								x = OpenTaiko.Skin.Gauge_Soul_5P[0] + (OpenTaiko.Skin.Game_UIMove_5P[0] * i);
								y = OpenTaiko.Skin.Gauge_Soul_5P[1] + (OpenTaiko.Skin.Game_UIMove_5P[1] * i);
							} else if (OpenTaiko.ConfigIni.nPlayerCount == 4 || OpenTaiko.ConfigIni.nPlayerCount == 3) {
								x = OpenTaiko.Skin.Gauge_Soul_4P[0] + (OpenTaiko.Skin.Game_UIMove_4P[0] * i);
								y = OpenTaiko.Skin.Gauge_Soul_4P[1] + (OpenTaiko.Skin.Game_UIMove_4P[1] * i);
							} else {
								x = OpenTaiko.Skin.Gauge_Soul_X[i];
								y = OpenTaiko.Skin.Gauge_Soul_Y[i];
							}
						}

						OpenTaiko.Tx.Gauge_Soul.vcScaleRatio.X = scale;
						OpenTaiko.Tx.Gauge_Soul.vcScaleRatio.Y = scale;

						if (this.dbCurrentGaugeValue[i] >= 80.0) {
							OpenTaiko.Tx.Gauge_Soul.t2DDraw(x, y, new Rectangle(0, 0, OpenTaiko.Tx.Gauge_Soul.szTextureSize.Width, soul_height));
						} else {
							OpenTaiko.Tx.Gauge_Soul.t2DDraw(x, y, new Rectangle(0, soul_height, OpenTaiko.Tx.Gauge_Soul.szTextureSize.Width, soul_height));
						}
					}
				}
			}






			for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
				for (int d = 0; d < 32; d++) {
					if (this.stFireworkState[i][d].bUse) {
						this.stFireworkState[i][d].ctProgress.Tick();
						if (this.stFireworkState[i][d].ctProgress.IsEnded) {
							this.stFireworkState[i][d].ctProgress.Stop();
							this.stFireworkState[i][d].bUse = false;
						}


						break;
					}
				}
			}
		}
		return 0;
	}


	// その他

	#region [ private ]
	//-----------------
	private CCounter ctGaugeFlash;

	// Reused per-frame scratch buffers (filled fresh every Draw) so the draw loop allocates no arrays.
	private readonly int[] nRectX = new int[5];
	private readonly int[] gauge_x = new int[5];
	private readonly int[] gauge_y = new int[5];

	protected STSTATUS[][] stFireworkState = new STSTATUS[5][] {
		new STSTATUS[ 32 ],
		new STSTATUS[ 32 ],
		new STSTATUS[ 32 ],
		new STSTATUS[ 32 ],
		new STSTATUS[ 32 ]
	};
	[StructLayout(LayoutKind.Sequential)]
	protected struct STSTATUS {
		public CCounter ctProgress;
		public bool isBig;
		public bool bUse;
		public int nPlayer;
		public NotesManager.ENoteType nLane;
		public EGameType gameType;
	}
	//-----------------
	#endregion
}
