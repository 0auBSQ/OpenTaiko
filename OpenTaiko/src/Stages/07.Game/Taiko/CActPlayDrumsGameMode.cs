using System.Runtime.InteropServices;
using FDK;
using Point = System.Drawing.Point;
using Rectangle = System.Drawing.Rectangle;

namespace OpenTaiko;

internal class CActPlayDrumsGameMode : CActivity {
	/// <summary>
	/// 現時点では「完走!叩ききりまショー!」のみ。
	///
	/// </summary>
	public CActPlayDrumsGameMode() {
		this.IsDeActivated = true;
	}

	//叩ききりまショー!
	//<ルール>
	//_某DAMのやつに似てるやつ。
	//_演奏可能な残り時間が減っていく。
	//_複数の項目に対して、一定の条件をクリアしていって延命させていく。
	//_タイマーが0になったらSTAGE FAILED。
	//
	//判定要素
	//_精度
	//_ミス数
	//_ズレ時間
	//_最大コンボ数
	//その他諸々

	private long nTatakikiriShow_RemainingTimeTimer;
	public STTatakikiriShow stTatakikiriShow;
	public struct STTatakikiriShow {
		public bool bFirstChipHit;
		public bool bTimerUse;
		public bool bSuperExtreme;
		public bool bAddAnime;
		public int nHitCount_PERFECT;
		public int nHitCount_GREAT;
		public int nHitCount_GOOD;
		public int nHitCount_POOR;
		public int nHitCount_MISS;
		public int nMaxCombo;
		public int nCurrentCombo;
		public int nSectionNoteCount;
		public int nMaxGapTime;
		public int nMinGapTime;
		public int nCurrentPassedNoteCount;
		public int nTotalMaxGapTime;
		public int nBonusAddOccurCount;
		public int nExtendAnimeSpeed;
		public CCounter ctRemainingTime;
		public CCounter ctAddTimeDisplay;
		public CCounter ctAddJudging;
		public CCounter ctNeedleAnime;
	}
	private int nLastTimeExtendTime;
	private int nPlayTime;
	private int nPreviousExtendTime;
	//private CTexture tx残り時間数字;
	//private CTexture tx背景黒;
	//private CTexture tx加算時間数字;
	//private CTexture txタイマー枠;
	//private CTexture txタイマー針;

	[StructLayout(LayoutKind.Sequential)]
	private struct STBonus {
		public double ret;
		public double point;
		public STBonus(double ret, double point) {
			this.ret = ret;
			this.point = point;
		}
	}

	private STBonus[] nAccuracyBonus;
	private STBonus[] nMaxGapTimeBonus;
	private STBonus[] nMinGapTimeBonus;
	private STBonus[] nComboRateBonus;
	private STBonus[] nMissRateBonus;

	private STBonus[] nTotalAccuracyBonus;
	private STBonus[] nTotalMaxGapTimeBonus;
	private STBonus[] nTotalComboRateBonus;
	private STBonus[] nTotalMissRateBonus;
	private int nAddTime;

	public void tTatakikiriShow_Initialize() {
		this.stTatakikiriShow = new STTatakikiriShow();
		this.nPlayTime = (OpenTaiko.TJA.listChip.Count > 0) ? OpenTaiko.TJA.listChip[OpenTaiko.TJA.listChip.Count - 1].nSoundTimems : 0;
		this.stTatakikiriShow.ctRemainingTime = new CCounter(0, 25000, 1, OpenTaiko.Timer);
		this.stTatakikiriShow.ctAddTimeDisplay = new CCounter();
		this.stTatakikiriShow.ctAddJudging = new CCounter();
		this.stTatakikiriShow.bFirstChipHit = false;
		this.stTatakikiriShow.bTimerUse = false;
		this.stTatakikiriShow.bAddAnime = false;
		this.nLastTimeExtendTime = 0;

		this.stTatakikiriShow.nHitCount_PERFECT = 0;
		this.stTatakikiriShow.nHitCount_GREAT = 0;
		this.stTatakikiriShow.nHitCount_GOOD = 0;
		this.stTatakikiriShow.nHitCount_POOR = 0;
		this.stTatakikiriShow.nHitCount_MISS = 0;
		this.stTatakikiriShow.nSectionNoteCount = 0;
		this.stTatakikiriShow.nCurrentCombo = 0;
		this.stTatakikiriShow.nMinGapTime = -1;
		this.stTatakikiriShow.nMaxGapTime = -1;
		this.stTatakikiriShow.nTotalMaxGapTime = -1;
		this.stTatakikiriShow.nCurrentPassedNoteCount = 0;
		this.stTatakikiriShow.bSuperExtreme = false;
		this.stTatakikiriShow.nBonusAddOccurCount = 0;
		this.stTatakikiriShow.nExtendAnimeSpeed = 0;
		this.nAddTime = 0;
		this.nPreviousExtendTime = 0;

		this.stTatakikiriShow.ctNeedleAnime = new CCounter(0, 1000, 1, OpenTaiko.Timer);

		this.tTatakikiriShow_DecideJudgeItemAndDifficulty();
	}

	public void tTatakikiriShow_DecideJudgeItemAndDifficulty() {
		//まず通常、激辛時でわける。
		if (OpenTaiko.ConfigIni.eGameMode == EGame.Survival) {
			#region[ 通常 ]
			//通常の査定
			// 精度 > 最小ズレ > コンボ > 最大ズレ > ミス
			this.nAccuracyBonus = new STBonus[]{
				new STBonus( 90, 5 ),
				new STBonus( 70, 4.5 ),
				new STBonus( 60, 3 ),
				new STBonus( 50, 1 ),
				new STBonus( 30, 0 )
			};
			this.nMinGapTimeBonus = new STBonus[]{
				new STBonus( 5, 4 ),
				new STBonus( 10, 3.5 ),
				new STBonus( 20, 3 ),
				new STBonus( 50, 1.5 ),
				new STBonus( 80, -1 )
			};
			this.nMaxGapTimeBonus = new STBonus[]{
				new STBonus( 50, 2 ),
				new STBonus( 60, 1.5 ),
				new STBonus( 80, 0.5 ),
				new STBonus( 90, 0 ),
				new STBonus( 100, -1 )
			};
			this.nComboRateBonus = new STBonus[]{
				new STBonus( 98.0, 3.5 ),
				new STBonus( 80.0, 1 ),
				new STBonus( 50.0, 0.5 ),
				new STBonus( 35.0, -1.5 )
			};
			this.nMissRateBonus = new STBonus[]{
				new STBonus( 0, 2 ),
				new STBonus( 20.0, 1 ),
				new STBonus( 50.0, -0.5 )
			};

			this.nTotalAccuracyBonus = new STBonus[]{
				new STBonus( 90, 3.5 ),
				new STBonus( 70, 2.5 ),
				new STBonus( 60, 1 ),
				new STBonus( 50, 0.5 ),
				new STBonus( 30, -0.5 )
			};
			this.nTotalMaxGapTimeBonus = new STBonus[]{
				new STBonus( 50, 4.2 ),
				new STBonus( 60, 3.6 ),
				new STBonus( 80, 2 ),
				new STBonus( 90, -0.5 ),
				new STBonus( 100, -1 )
			};
			this.nTotalComboRateBonus = new STBonus[]{
				new STBonus( 98.0, 3 ),
				new STBonus( 80.0, 2.5 ),
				new STBonus( 50.0, 0.5 )
			};
			this.nTotalMissRateBonus = new STBonus[]{
				new STBonus( 0, 2 ),
				new STBonus( 20.0, 1.5 ),
				new STBonus( 50.0, 0.5 ),
				new STBonus( 70.0, -0.5 )
			};
			#endregion
		} else if (OpenTaiko.ConfigIni.eGameMode == EGame.SurvivalHard) {
			#region[ 激辛 ]
			//激ムズの査定
			// 最大ズレ > 精度 > コンボ > 最小ズレ > ミス
			//各項目最高値合計で20秒加算になるようにすること。
			this.nAccuracyBonus = new STBonus[]{
				new STBonus( 100, 3 ),
				new STBonus( 95, 2 ),
				new STBonus( 90, 1 ),
				new STBonus( 70, -2 ),
				new STBonus( 50, -4 ),
				new STBonus( 0, -10 )
			};
			this.nMinGapTimeBonus = new STBonus[]{
				new STBonus( 0, 2 ),
				new STBonus( 3, 1 ),
				new STBonus( 5, -2 ),
				new STBonus( 10, -3 ),
				new STBonus( 30, -3 ),
				new STBonus( 108, -4 )
			};
			this.nMaxGapTimeBonus = new STBonus[]{
				new STBonus( 3, 3.5 ),
				new STBonus( 10, 2 ),
				new STBonus( 15, 1 ),
				new STBonus( 20, 0 ),
				new STBonus( 50, -2 ),
				new STBonus( 108, -5 )
			};
			this.nComboRateBonus = new STBonus[]{
				new STBonus( 100.0, 1 ),
				new STBonus( 50.0, 0.5 ),
				new STBonus( 0.0, -5 )
			};
			this.nMissRateBonus = new STBonus[]{
				new STBonus( 0, 1 ),
				new STBonus( 100.0, -5 )
			};

			this.nTotalAccuracyBonus = new STBonus[]{
				new STBonus( 100, 5 ),
				new STBonus( 99, 4 ),
				new STBonus( 90, 1.5 ),
				new STBonus( 80, 1 ),
				new STBonus( 50, -1 ),
				new STBonus( 30, -3 ),
				new STBonus( 0, -4.5 )
			};
			this.nTotalMaxGapTimeBonus = new STBonus[]{
				new STBonus( 20, 3 ),
				new STBonus( 30, 1.5 ),
				new STBonus( 50, 1 ),
				new STBonus( 80, 0 ),
				new STBonus( 108, -2.5 )
			};
			this.nTotalComboRateBonus = new STBonus[]{
				new STBonus( 100.0, 1 ),
				new STBonus( 0.0, -2 )
			};
			this.nTotalMissRateBonus = new STBonus[]{
				new STBonus( 0, 1 ),
				new STBonus( 100.0, -2 ),
			};

			//★10の場合超激辛モードになる。
			if (OpenTaiko.TJA.PlayerSideMetadata.LEVELtaiko >= 10) {
				#region[ 超激辛 ]
				this.stTatakikiriShow.bSuperExtreme = true;

				this.nAccuracyBonus = new STBonus[]{
					new STBonus( 100, 3 ),
					new STBonus( 95, 2 ),
					new STBonus( 88, 1 ),
					new STBonus( 80, -3 ),
					new STBonus( 50, -6 ),
					new STBonus( 0, -10 )
				};

				this.nMaxGapTimeBonus = new STBonus[]{
					new STBonus( 2, 4 ),
					new STBonus( 10, 1 ),
					new STBonus( 30, 0 ),
					new STBonus( 50, -1 ),
					new STBonus( 70, -3 ),
					new STBonus( 108, -5 )
				};
				this.nComboRateBonus = new STBonus[]{
					new STBonus( 100.0, 1 ),
					new STBonus( 0.0, -6 )
				};
				this.nMissRateBonus = new STBonus[]{
					new STBonus( 0, 1 ),
					new STBonus( 100.0, -6 )
				};

				this.nTotalMaxGapTimeBonus = new STBonus[]{
					new STBonus( 20, 3 ),
					new STBonus( 60, 1 ),
					new STBonus( 108, -5 )
				};
				this.nTotalComboRateBonus = new STBonus[]{
					new STBonus( 100.0, 1 ),
					new STBonus( 0.0, -5 )
				};
				this.nTotalMissRateBonus = new STBonus[]{
					new STBonus( 0, 1 ),
					new STBonus( 100.0, -5 ),
				};
				#endregion
			}

			if (OpenTaiko.ConfigIni.bSuperHard) {
				#region[ 超激辛 ]
				this.stTatakikiriShow.bSuperExtreme = true;

				this.nAccuracyBonus = new STBonus[]{
					new STBonus( 100, 3 ),
					new STBonus( 98, 2.3 ),
					new STBonus( 95, 2 ),
					new STBonus( 90, 1.5 ),
					new STBonus( 85, 0 ),
					new STBonus( 80, -2 ),
					new STBonus( 60, -3 ),
					new STBonus( 40, -6 ),
					new STBonus( 0, -7.5 )
				};

				this.nMaxGapTimeBonus = new STBonus[]{
					new STBonus( 8, 5 ),
					new STBonus( 18, 3 ),
					new STBonus( 40, 1 ),
					new STBonus( 50, -0.5 ),
					new STBonus( 70, -3 ),
					new STBonus( 108, -5 )
				};
				this.nComboRateBonus = new STBonus[]{
					new STBonus( 100.0, 1 ),
					new STBonus( 0.0, -6 )
				};
				this.nMissRateBonus = new STBonus[]{
					new STBonus( 0, 1 ),
					new STBonus( 100.0, -6 )
				};

				this.nTotalAccuracyBonus = new STBonus[]{
					new STBonus( 100, 7 ),
					new STBonus( 99, 4 ),
					new STBonus( 90, 2 ),
					new STBonus( 80, 1 ),
					new STBonus( 50, -1 ),
					new STBonus( 0, -7 )
				};
				this.nTotalMaxGapTimeBonus = new STBonus[]{
					new STBonus( 20, 3 ),
					new STBonus( 40, 1 ),
					new STBonus( 60, -3 ),
					new STBonus( 108, -5 )
				};
				this.nTotalComboRateBonus = new STBonus[]{
					new STBonus( 100.0, 1 ),
					new STBonus( 0.0, -5 )
				};
				this.nTotalMissRateBonus = new STBonus[]{
					new STBonus( 0, 0 ),
					new STBonus( 100.0, -5 ),
				};
				#endregion
			}
			#endregion
		}
	}

	public override void CreateManagedResource() {
		//this.tx残り時間数字 = CDTXMania.tテクスチャの生成( CSkin.Path( @"Graphics\7_combo taiko.png" ) );
		//this.tx加算時間数字 = CDTXMania.tテクスチャの生成( CSkin.Path( @"Graphics\7_Score_number_1P.png" ) );
		//this.txタイマー枠 = CDTXMania.tテクスチャの生成( CSkin.Path( @"Graphics\7_TimerPanel.png" ) );
		//this.txタイマー針 = CDTXMania.tテクスチャの生成( CSkin.Path( @"Graphics\7_TimerTick.png" ) );
		//this.tx背景黒 = CDTXMania.tテクスチャの生成( CSkin.Path( @"Graphics\Tile black 64x64.png" ) );
		base.CreateManagedResource();
	}

	public override void ReleaseManagedResource() {
		//CDTXMania.tテクスチャの解放( ref this.tx残り時間数字 );
		//CDTXMania.tテクスチャの解放( ref this.tx加算時間数字 );
		//CDTXMania.tテクスチャの解放( ref this.txタイマー枠 );
		//CDTXMania.tテクスチャの解放( ref this.txタイマー針 );
		//CDTXMania.tテクスチャの解放( ref this.tx背景黒 );
		base.ReleaseManagedResource();
	}

	public override int Draw() {
		if (OpenTaiko.ConfigIni.eGameMode == EGame.Survival || OpenTaiko.ConfigIni.eGameMode == EGame.SurvivalHard) {
			CTja tja = OpenTaiko.TJA!; // 1P-only mode (?)
			//if( this.st叩ききりまショー.b最初のチップが叩かれた == true )//&&
			//CDTXMania.stage演奏ドラム画面.r検索範囲内にチップがあるか調べる( CSound管理.rc演奏用タイマ.n現在時刻ms, 0, 3000 ) )
			//this.st叩ききりまショー.ct残り時間.t進行();
			//else
			//{
			//    this.st叩ききりまショー.ct残り時間.n現在の値 = this.st叩ききりまショー.ct残り時間.n現在の値;
			//}


			//if( !this.st叩ききりまショー.ct残り時間.b停止中 )
			if (this.stTatakikiriShow.bTimerUse) {
				if (!this.stTatakikiriShow.ctRemainingTime.IsStopped || this.stTatakikiriShow.bAddAnime == true) {
					this.stTatakikiriShow.ctRemainingTime.Tick();
					if (!OpenTaiko.stageGameScreen.rIsChipInSearchRange((long)tja.GameTimeToTjaTime(SoundManager.PlayTimer.NowTimeMs), 5000, 0) || this.stTatakikiriShow.bAddAnime == true) {
						this.stTatakikiriShow.bTimerUse = false;
						this.stTatakikiriShow.ctRemainingTime.Stop();
					}
				}
			}

			if (!this.stTatakikiriShow.bTimerUse && this.stTatakikiriShow.bAddAnime == false) {
				if ((this.stTatakikiriShow.bFirstChipHit == true && (OpenTaiko.stageGameScreen.rIsChipInSearchRange(SoundManager.PlayTimer.NowTimeMs, 2000, 0)))) {
					this.stTatakikiriShow.bTimerUse = true;
					int nCount = this.stTatakikiriShow.ctRemainingTime.CurrentValue;
					this.stTatakikiriShow.ctRemainingTime = new CCounter(0, 25000, 1, OpenTaiko.Timer);
					this.stTatakikiriShow.ctNeedleAnime = new CCounter(0, 1000, 1, OpenTaiko.Timer);
					this.stTatakikiriShow.ctRemainingTime.CurrentValue = nCount;
				}

			}


			if ((this.stTatakikiriShow.ctRemainingTime.CurrentValue >= 20000) && this.stTatakikiriShow.ctRemainingTime.CurrentValue != 25000)
				this.tTatakikiriShow_EvaluateRemainingTimeExtend();

			if (OpenTaiko.Tx.Tile_Black != null) {
				if (this.stTatakikiriShow.ctRemainingTime.CurrentValue >= 22000 && this.stTatakikiriShow.ctRemainingTime.CurrentValue < 23000)
					OpenTaiko.Tx.Tile_Black.Opacity = 64;
				else if (this.stTatakikiriShow.ctRemainingTime.CurrentValue >= 23000 && this.stTatakikiriShow.ctRemainingTime.CurrentValue < 24000)
					OpenTaiko.Tx.Tile_Black.Opacity = 128;
				else if (this.stTatakikiriShow.ctRemainingTime.CurrentValue >= 24000)
					OpenTaiko.Tx.Tile_Black.Opacity = 192;
				else
					OpenTaiko.Tx.Tile_Black.Opacity = 0;

				for (int i = 0; i <= (GameWindowSize.Width / 64); i++) {
					for (int j = 0; j <= (GameWindowSize.Height / 64); j++) {
						OpenTaiko.Tx.Tile_Black.t2DDraw(i * 64, j * 64);
					}
				}
			}

			//CDTXMania.act文字コンソール.tPrint( 100, 0, C文字コンソール.Eフォント種別.白, ( 25 - this.st叩ききりまショー.ct残り時間.n現在の値 ).ToString() );
			//CDTXMania.act文字コンソール.tPrint( 100, 16, C文字コンソール.Eフォント種別.白, this.st叩ききりまショー.n区間ノート数.ToString() );
			//CDTXMania.act文字コンソール.tPrint( 100, 16 * 2, C文字コンソール.Eフォント種別.白, this.st叩ききりまショー.n現在通過したノート数.ToString() );
			//CDTXMania.act文字コンソール.tPrint( 100, 16 * 3, C文字コンソール.Eフォント種別.白, this.st叩ききりまショー.nヒット数_MISS.ToString() );
			//CDTXMania.act文字コンソール.tPrint( 100, 16 * 4, C文字コンソール.Eフォント種別.白, this.st叩ききりまショー.n最小ズレ時間.ToString() );
			//CDTXMania.act文字コンソール.tPrint( 100, 16 * 5, C文字コンソール.Eフォント種別.白, this.st叩ききりまショー.n最大ズレ時間.ToString() );
			//CDTXMania.act文字コンソール.tPrint( 100, 16 * 6, C文字コンソール.Eフォント種別.白, this.st叩ききりまショー.n全体最大ズレ時間.ToString() );
			//CDTXMania.act文字コンソール.tPrint( 100, 16 * 7, C文字コンソール.Eフォント種別.白, this.st叩ききりまショー.n最大コンボ.ToString() );
			//CDTXMania.act文字コンソール.tPrint( 100, 16 * 7, C文字コンソール.Eフォント種別.白, this.st叩ききりまショー.ct加算審査中.n現在の値.ToString() );

			#region[ 残り時間描画 ]
			if (OpenTaiko.Tx.Taiko_Combo != null) {
				if (OpenTaiko.Tx.GameMode_Timer_Frame != null)
					OpenTaiko.Tx.GameMode_Timer_Frame.t2DDraw(230, 84);
				this.stTatakikiriShow.ctNeedleAnime.TickLoop();

				int nCenterX = 230;
				int nCerterY = 84;
				float fRotate = -CConversion.DegreeToRadian(360.0f * (this.stTatakikiriShow.ctNeedleAnime.CurrentValue / 1000.0f));
				if (this.stTatakikiriShow.bAddAnime == true)
					fRotate = CConversion.DegreeToRadian(360.0f * (this.stTatakikiriShow.ctNeedleAnime.CurrentValue / (float)this.stTatakikiriShow.nExtendAnimeSpeed));

				/*
                Matrix mat = Matrix.Identity;
                if( this.st叩ききりまショー.b最初のチップが叩かれた )
                {
                    mat *= Matrix.RotationZ( fRotate );
                    mat *= Matrix.Translation( 280 - 640, -( 134 - 360 ), 0 );
                }
                else
                {
                    mat *= Matrix.Translation( 280 - 640, -( 134 - 360 ), 0 );
                }

                TJAPlayer3.Tx.GameMode_Timer_Tick?.t3D描画( mat );
                */

				string strDisplayRemainingTime = (this.stTatakikiriShow.ctRemainingTime.CurrentValue < 1000) ? "25" : ((26000 - this.stTatakikiriShow.ctRemainingTime.CurrentValue) / 1000).ToString();

				if (OpenTaiko.Tx.GameMode_Timer_Frame != null)
					this.tSmallDisplay(230 + (strDisplayRemainingTime.Length * OpenTaiko.Skin.Game_Taiko_Combo_Size[0] / 4), 84 + OpenTaiko.Tx.GameMode_Timer_Frame.szTextureSize.Height / 2, string.Format("{0,2:#0}", strDisplayRemainingTime));
			}

			if (!this.stTatakikiriShow.ctAddJudging.IsStopped) {
				if (!this.stTatakikiriShow.ctAddJudging.IsStopped) {
					this.stTatakikiriShow.ctAddJudging.Tick();
					if (this.stTatakikiriShow.ctAddJudging.IsEnded) {
						this.stTatakikiriShow.ctAddJudging.Stop();
						this.stTatakikiriShow.bAddAnime = false;
						this.tAddTimeDraw_Start();
					}
				}
			}
			if (!this.stTatakikiriShow.ctAddTimeDisplay.IsStopped) {
				if (!this.stTatakikiriShow.ctAddTimeDisplay.IsStopped) {
					this.stTatakikiriShow.ctAddTimeDisplay.Tick();
					if (this.stTatakikiriShow.ctAddTimeDisplay.IsEnded) {
						this.stTatakikiriShow.ctAddTimeDisplay.Stop();
					}
				}
				this.tAddTimeDraw(this.nPreviousExtendTime);
			}
			#endregion
		}
		return 0;
	}

	private void tTatakikiriShow_EvaluateRemainingTimeExtend() {
		double nExtendTime = 0;

		CTja tja = OpenTaiko.TJA!; // 1P-only mode (?)

		//最後に延長した時刻から11秒経過していなければ延長を行わない。
		if (this.nLastTimeExtendTime + 11000 <= tja.GameTimeToTjaTime(SoundManager.PlayTimer.NowTimeMs)) {
			//1項目につき5秒
			//-精度
			if (this.stTatakikiriShow.nHitCount_PERFECT != 0 || this.stTatakikiriShow.nHitCount_GREAT != 0) {
				double dbSectionInAccuracy = ((double)(this.stTatakikiriShow.nHitCount_PERFECT + this.stTatakikiriShow.nHitCount_GREAT) / this.stTatakikiriShow.nSectionNoteCount) * 100.0;
				for (int i = 0; i < this.nAccuracyBonus.Length; i++) {
					if (dbSectionInAccuracy >= this.nAccuracyBonus[i].ret) {
						nExtendTime += this.nAccuracyBonus[i].point;
						break;
					}
				}
			}

			//-ラグ時間
			#region[ ラグ時間による判定 ]
			if (this.stTatakikiriShow.nMinGapTime != -1) {
				for (int i = 0; i < this.nMinGapTimeBonus.Length; i++) {
					if (this.stTatakikiriShow.nMinGapTime >= this.nMinGapTimeBonus[i].ret) {
						nExtendTime += this.nMinGapTimeBonus[i].point;
						break;
					}
				}
			}

			if (this.stTatakikiriShow.nMaxGapTime != -1) {
				for (int i = 0; i < this.nMaxGapTimeBonus.Length; i++) {
					if (this.stTatakikiriShow.nMaxGapTime <= this.nMaxGapTimeBonus[i].ret) {
						nExtendTime += this.nMaxGapTimeBonus[i].point;
						break;
					}
				}
			}
			#endregion
			if (this.stTatakikiriShow.nMaxCombo != 0) {
				double dbSectionInComboAccuracy = ((double)this.stTatakikiriShow.nMaxCombo / this.stTatakikiriShow.nSectionNoteCount) * 100.0;
				for (int i = 0; i < this.nComboRateBonus.Length; i++) {
					if (dbSectionInComboAccuracy >= this.nComboRateBonus[i].ret) {
						nExtendTime += this.nComboRateBonus[i].point;
						break;
					}
				}
			}

			double dbSectionInMissRate = (((double)this.stTatakikiriShow.nHitCount_POOR + this.stTatakikiriShow.nHitCount_MISS) / this.stTatakikiriShow.nSectionNoteCount) * 100.0;
			for (int i = 0; i < this.nMissRateBonus.Length; i++) {
				if (dbSectionInMissRate >= this.nMissRateBonus[i].ret) {
					nExtendTime += this.nMissRateBonus[i].point;
					break;
				}
			}
			#region[ 全体 ]
			if (OpenTaiko.stageGameScreen.nHitCount_ExclAuto.Perfect != 0 || OpenTaiko.stageGameScreen.nHitCount_ExclAuto.Great != 0) {
				double dbTotalAccuracy = ((double)(OpenTaiko.stageGameScreen.nHitCount_ExclAuto.Perfect + OpenTaiko.stageGameScreen.nHitCount_ExclAuto.Great) / this.stTatakikiriShow.nSectionNoteCount) * 100.0;
				for (int i = 0; i < this.nTotalAccuracyBonus.Length; i++) {
					if (dbTotalAccuracy >= this.nTotalAccuracyBonus[i].ret) {
						nExtendTime += this.nTotalAccuracyBonus[i].point;
						break;
					}
				}
			}

			//-ラグ時間
			#region[ ラグ時間による判定 ]
			if (this.stTatakikiriShow.nTotalMaxGapTime != -1) {
				for (int i = 0; i < this.nTotalMaxGapTimeBonus.Length; i++) {
					if (this.stTatakikiriShow.nTotalMaxGapTime <= this.nTotalMaxGapTimeBonus[i].ret) {
						nExtendTime += this.nTotalMaxGapTimeBonus[i].point;
						break;
					}
				}
			}
			#endregion
			if (OpenTaiko.stageGameScreen.actCombo.nCurrentCombo.MaxValue[0] != 0) {
				double dbTotalComboRate = ((double)OpenTaiko.stageGameScreen.actCombo.nCurrentCombo.MaxValue[0] / this.stTatakikiriShow.nCurrentPassedNoteCount) * 100.0;
				for (int i = 0; i < this.nTotalComboRateBonus.Length; i++) {
					if (dbTotalComboRate >= this.nTotalComboRateBonus[i].ret) {
						nExtendTime += this.nTotalComboRateBonus[i].point;
						break;
					}
				}
			}

			double dbTotalMissRate = (((double)OpenTaiko.stageGameScreen.nHitCount_ExclAuto.Poor + OpenTaiko.stageGameScreen.nHitCount_ExclAuto.Miss) / this.stTatakikiriShow.nCurrentPassedNoteCount) * 100.0;
			for (int i = 0; i < this.nTotalMissRateBonus.Length; i++) {
				if (dbTotalMissRate >= this.nTotalMissRateBonus[i].ret) {
					nExtendTime += this.nTotalMissRateBonus[i].point;
					break;
				}
			}
			#endregion


			this.nLastTimeExtendTime = (int)tja.GameTimeToTjaTime(SoundManager.PlayTimer.NowTimeMs);
			if (nExtendTime < 0)
				nExtendTime = 0;
			if (this.stTatakikiriShow.nSectionNoteCount == 0)
				nExtendTime = 15;

			//各数値を初期化
			this.stTatakikiriShow.nHitCount_PERFECT = 0;
			this.stTatakikiriShow.nHitCount_GREAT = 0;
			this.stTatakikiriShow.nHitCount_GOOD = 0;
			this.stTatakikiriShow.nHitCount_POOR = 0;
			this.stTatakikiriShow.nHitCount_MISS = 0;
			this.stTatakikiriShow.nSectionNoteCount = 0;
			this.stTatakikiriShow.nCurrentCombo = 0;
			this.stTatakikiriShow.nMinGapTime = -1;
			this.stTatakikiriShow.nMaxGapTime = -1;

			this.nPreviousExtendTime = (int)nExtendTime;
			nExtendTime = nExtendTime * 1000;
			if (nExtendTime > 0) {
				this.tAddJudgingAnime_Start();
				if (this.stTatakikiriShow.bAddAnime == false)
					this.tAddTimeDraw_Start();
			}
			this.stTatakikiriShow.ctRemainingTime.CurrentValue -= (int)nExtendTime;
		} else if (this.stTatakikiriShow.ctRemainingTime.CurrentValue >= 24000) {
			if (this.stTatakikiriShow.nBonusAddOccurCount > 3)
				return;
			if (this.stTatakikiriShow.bSuperExtreme && (((double)this.stTatakikiriShow.nHitCount_POOR + this.stTatakikiriShow.nHitCount_MISS) > 0))
				return; //ミスが出るようでは上達しませんよ。お兄様。
			if (OpenTaiko.ConfigIni.bSuperHard)
				return; //スーパーハード時はボーナス加点無し。


			this.stTatakikiriShow.nBonusAddOccurCount++;

			if (this.stTatakikiriShow.nHitCount_PERFECT != 0 || this.stTatakikiriShow.nHitCount_GREAT != 0) {
				double dbSectionInAccuracy = ((double)(this.stTatakikiriShow.nHitCount_PERFECT + this.stTatakikiriShow.nHitCount_GREAT) / this.stTatakikiriShow.nSectionNoteCount) * 100.0;
				if (this.stTatakikiriShow.bSuperExtreme ? (dbSectionInAccuracy >= 95.0) : (dbSectionInAccuracy >= 98.0)) {
					nExtendTime += 6;
				}
			}
			#region[ ラグ時間による判定 ]
			if (this.stTatakikiriShow.nMinGapTime != -1) {
				if (this.stTatakikiriShow.nMinGapTime >= 0) {
					nExtendTime += 6;
				}
			}

			if (this.stTatakikiriShow.nMaxGapTime != -1) {
				if (this.stTatakikiriShow.nMaxGapTime <= 30) {
					nExtendTime += 6;
				}
			}
			#endregion
			double dbSectionInMissRate = (((double)this.stTatakikiriShow.nHitCount_POOR + this.stTatakikiriShow.nHitCount_MISS) / this.stTatakikiriShow.nSectionNoteCount) * 100.0;
			if (dbSectionInMissRate >= 5.0) {
				nExtendTime -= 2;
			}


			this.nLastTimeExtendTime = (int)tja.GameTimeToTjaTime(SoundManager.PlayTimer.NowTimeMs);
			if (nExtendTime < 0)
				nExtendTime = 0;

			//各数値を初期化
			this.stTatakikiriShow.nHitCount_PERFECT = 0;
			this.stTatakikiriShow.nHitCount_GREAT = 0;
			this.stTatakikiriShow.nHitCount_GOOD = 0;
			this.stTatakikiriShow.nHitCount_POOR = 0;
			this.stTatakikiriShow.nHitCount_MISS = 0;
			this.stTatakikiriShow.nSectionNoteCount = 0;
			this.stTatakikiriShow.nCurrentCombo = 0;
			this.stTatakikiriShow.nMinGapTime = -1;
			this.stTatakikiriShow.nMaxGapTime = -1;

			this.nPreviousExtendTime = (int)nExtendTime;
			nExtendTime = nExtendTime * 1000;
			if (nExtendTime > 0) {
				this.tAddJudgingAnime_Start();
				if (this.stTatakikiriShow.bAddAnime == false)
					this.tAddTimeDraw_Start();
			}
			if (nExtendTime > 5000)
				this.stTatakikiriShow.ctRemainingTime.CurrentValue -= (int)nExtendTime;
		}

		if (nExtendTime >= 12000)
			this.stTatakikiriShow.nExtendAnimeSpeed = 100;
		else if (nExtendTime < 12000 && nExtendTime >= 5000)
			this.stTatakikiriShow.nExtendAnimeSpeed = 250;
		else
			this.stTatakikiriShow.nExtendAnimeSpeed = 500;
	}

	public void tTatakikiriShow_IncreaseValuesFromJudge(ENoteJudge eJudge, int nLagTime) {
		this.stTatakikiriShow.bFirstChipHit = true;
		this.stTatakikiriShow.nSectionNoteCount++;
		this.stTatakikiriShow.nCurrentPassedNoteCount++;
		switch (eJudge) {
			case ENoteJudge.Perfect:
				this.stTatakikiriShow.nHitCount_PERFECT++;
				break;
			case ENoteJudge.Great:
				this.stTatakikiriShow.nHitCount_GREAT++;
				break;
			case ENoteJudge.Good:
				this.stTatakikiriShow.nHitCount_GOOD++;
				break;
			case ENoteJudge.Poor:
				this.stTatakikiriShow.nHitCount_POOR++;
				break;
			case ENoteJudge.Miss:
				this.stTatakikiriShow.nHitCount_MISS++;
				break;
		}
		switch (eJudge) {
			case ENoteJudge.Perfect:
			case ENoteJudge.Great:
			case ENoteJudge.Good:
				this.stTatakikiriShow.nCurrentCombo++;
				if (this.stTatakikiriShow.nCurrentCombo >= this.stTatakikiriShow.nMaxCombo)
					this.stTatakikiriShow.nMaxCombo = this.stTatakikiriShow.nCurrentCombo;
				if (Math.Abs(nLagTime) > this.stTatakikiriShow.nMaxGapTime) {
					this.stTatakikiriShow.nMaxGapTime = Math.Abs(nLagTime);
				}
				if (Math.Abs(nLagTime) > this.stTatakikiriShow.nTotalMaxGapTime) {
					this.stTatakikiriShow.nTotalMaxGapTime = Math.Abs(nLagTime);
				}
				if (this.stTatakikiriShow.nMinGapTime == -1)
					this.stTatakikiriShow.nMinGapTime = Math.Abs(nLagTime);
				if (Math.Abs(nLagTime) < this.stTatakikiriShow.nMinGapTime) {
					this.stTatakikiriShow.nMinGapTime = Math.Abs(nLagTime);
				}
				break;
			default:
				this.stTatakikiriShow.nCurrentCombo = 0;
				break;
		}
	}

	private void tAddJudgingAnime_Start() {
		this.stTatakikiriShow.ctAddJudging = new CCounter(0, 2000, 1, OpenTaiko.Timer);
		this.stTatakikiriShow.bAddAnime = true;
	}
	private void tAddTimeDraw_Start() {
		this.stTatakikiriShow.ctAddTimeDisplay = new CCounter(0, 1, 1000, OpenTaiko.Timer);
	}

	private void tAddTimeDraw(int addtime) {
		this.tAddTextDisplay(258, 150, string.Format("{0,2:#0}", addtime.ToString()));
		//CDTXMania.act文字コンソール.tPrint( 236, 80, C文字コンソール.Eフォント種別.赤, "+" + string.Format( "{0,2:#0}", addtime.ToString() ) );
	}

	private struct STTextPosition {
		public char ch;
		public Point pt;
		public STTextPosition(char ch, Point pt) {
			this.ch = ch;
			this.pt = pt;
		}
	}

	private STTextPosition[] stSmallPosition = new STTextPosition[]{
		new STTextPosition( '0', new Point( 0, 0 ) ),
		new STTextPosition( '1', new Point( 44, 0 ) ),
		new STTextPosition( '2', new Point( 88, 0 ) ),
		new STTextPosition( '3', new Point( 132, 0 ) ),
		new STTextPosition( '4', new Point( 176, 0 ) ),
		new STTextPosition( '5', new Point( 220, 0 ) ),
		new STTextPosition( '6', new Point( 264, 0 ) ),
		new STTextPosition( '7', new Point( 308, 0 ) ),
		new STTextPosition( '8', new Point( 352, 0 ) ),
		new STTextPosition( '9', new Point( 396, 0 ) )
	};

	private void tSmallDisplay(int x, int y, string str) {
		foreach (char ch in str) {
			for (int i = 0; i < this.stSmallPosition.Length; i++) {
				if (this.stSmallPosition[i].ch == ch) {
					Rectangle rectangle = new Rectangle(OpenTaiko.Skin.Game_Taiko_Combo_Size[0] * i, 0, OpenTaiko.Skin.Game_Taiko_Combo_Size[0], OpenTaiko.Skin.Game_Taiko_Combo_Size[1]);
					if (OpenTaiko.Tx.Taiko_Combo[0] != null) {
						if (this.stTatakikiriShow.bTimerUse)
							OpenTaiko.Tx.Taiko_Combo[0].Opacity = 255;
						else if (this.stTatakikiriShow.bFirstChipHit && !this.stTatakikiriShow.bTimerUse)
							OpenTaiko.Tx.Taiko_Combo[0].Opacity = 128;
						if (this.stTatakikiriShow.bAddAnime)
							OpenTaiko.Tx.Taiko_Combo[0].Opacity = 0;
						OpenTaiko.Tx.Taiko_Combo[0].vcScaleRatio.Y = 1f;
						OpenTaiko.Tx.Taiko_Combo[0].vcScaleRatio.X = 1f;
						OpenTaiko.Tx.Taiko_Combo[0].t2DCenterBasedDraw(x, y, rectangle);
					}
					break;
				}
			}
			x += OpenTaiko.Skin.Game_Taiko_Combo_Padding[0] * 2;
		}
	}
	protected void tAddTextDisplay(int x, int y, string str) {
		char[] cFont = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
		foreach (char ch in str) {
			for (int i = 0; i < cFont.Length; i++) {
				if (cFont[i] == ch) {
					Rectangle rectangle = new Rectangle(OpenTaiko.Skin.Game_Score_Size[0] * i, 0, OpenTaiko.Skin.Game_Score_Size[0], OpenTaiko.Skin.Game_Score_Size[1]);
					if (OpenTaiko.Tx.Taiko_Score[0] != null) {
						OpenTaiko.Tx.Taiko_Score[0].vcScaleRatio.Y = 1f;
						OpenTaiko.Tx.Taiko_Score[0].t2DDraw(x, y, rectangle);
					}
				}
			}
			x += 20;
		}
	}
}
