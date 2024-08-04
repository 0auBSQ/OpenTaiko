using System.Drawing;
using FDK;

namespace OpenTaiko {
	class CActImplTrainingMode : CActivity {
		public CActImplTrainingMode() {
			base.IsDeActivated = true;
		}

		public override void Activate() {
			this.nCurrentMeasure = 0;
			this.bTrainingPAUSE = false;
			this.n最終演奏位置ms = 0;

			base.Activate();

			CDTX dTX = OpenTaiko.DTX;

			var measureCount = 1;
			var bIsInGoGo = false;

			int endtime = 1;
			int bgmlength = 1;

			for (int index = 0; index < OpenTaiko.DTX.listChip.Count; index++) {
				if (OpenTaiko.DTX.listChip[index].nチャンネル番号 == 0xff) {
					endtime = OpenTaiko.DTX.listChip[index].n発声時刻ms;
					break;
				}
			}
			for (int index = 0; index < OpenTaiko.DTX.listChip.Count; index++) {
				if (OpenTaiko.DTX.listChip[index].nチャンネル番号 == 0x01) {
					bgmlength = OpenTaiko.DTX.listChip[index].GetDuration() + OpenTaiko.DTX.listChip[index].n発声時刻ms;
					break;
				}
			}

			length = Math.Max(endtime, bgmlength);

			gogoXList = new List<int>();
			JumpPointList = new List<STJUMPP>();

			for (int i = 0; i < dTX.listChip.Count; i++) {
				CDTX.CChip pChip = dTX.listChip[i];

				if (pChip.n整数値_内部番号 > measureCount && pChip.nチャンネル番号 == 0x50) measureCount = pChip.n整数値_内部番号;

				if (pChip.nチャンネル番号 == 0x9E && !bIsInGoGo) {
					bIsInGoGo = true;

					var current = ((double)(pChip.db発声時刻ms * OpenTaiko.ConfigIni.SongPlaybackSpeed));
					var width = 0;
					if (OpenTaiko.Tx.Tokkun_ProgressBar != null) width = OpenTaiko.Tx.Tokkun_ProgressBar.szTextureSize.Width;

					this.gogoXList.Add((int)(width * (current / length)));
				}
				if (pChip.nチャンネル番号 == 0x9F && bIsInGoGo) {
					bIsInGoGo = false;
				}
			}

			this.nMeasureCount = measureCount;

			if (OpenTaiko.Tx.Tokkun_Background_Up != null) this.ctBackgroundScrollTimer = new CCounter(1, OpenTaiko.Tx.Tokkun_Background_Up.szTextureSize.Width, 16, OpenTaiko.Timer);
		}

		public override void DeActivate() {
			length = 1;
			gogoXList = null;
			JumpPointList = null;

			this.ctScrollCounter = null;
			this.ctBackgroundScrollTimer = null;
			base.DeActivate();
		}

		public override void CreateManagedResource() {
			base.CreateManagedResource();
		}

		public override void ReleaseManagedResource() {
			base.ReleaseManagedResource();
		}

		public override int Draw() {
			if (!base.IsDeActivated) {
				if (base.IsFirstDraw) {
					base.IsFirstDraw = false;
				}

				OpenTaiko.actTextConsole.tPrint(0, 0, CTextConsole.EFontType.White, "TRAINING MODE (BETA)");

				if (OpenTaiko.ConfigIni.KeyAssign.KeyIsPressed(OpenTaiko.ConfigIni.KeyAssign.Drums.TrainingPause)) {
					if (this.bTrainingPAUSE) {
						OpenTaiko.Skin.sound特訓再生音.tPlay();
						this.tResumePlay();
					} else {
						OpenTaiko.Skin.sound特訓停止音.tPlay();
						this.tPausePlay();
					}
				}
				if (OpenTaiko.ConfigIni.KeyAssign.KeyIsPressed(OpenTaiko.ConfigIni.KeyAssign.Drums.TrainingSkipForwardMeasure)) {
					if (this.bTrainingPAUSE) {
						this.nCurrentMeasure += OpenTaiko.ConfigIni.TokkunSkipMeasures;
						if (this.nCurrentMeasure > this.nMeasureCount)
							this.nCurrentMeasure = this.nMeasureCount;

						OpenTaiko.stage演奏ドラム画面.actPlayInfo.NowMeasure[0] = this.nCurrentMeasure;

						this.tMatchWithTheChartDisplayPosition(true);
						OpenTaiko.Skin.soundTrainingModeScrollSFX.tPlay();
					}
				}
				if (OpenTaiko.ConfigIni.KeyAssign.KeyIsPressed(OpenTaiko.ConfigIni.KeyAssign.Drums.TrainingSkipBackMeasure)) {
					if (this.bTrainingPAUSE) {
						this.nCurrentMeasure -= OpenTaiko.ConfigIni.TokkunSkipMeasures;
						if (this.nCurrentMeasure <= 0)
							this.nCurrentMeasure = 1;

						OpenTaiko.stage演奏ドラム画面.actPlayInfo.NowMeasure[0] = this.nCurrentMeasure;

						this.tMatchWithTheChartDisplayPosition(true);
						OpenTaiko.Skin.soundTrainingModeScrollSFX.tPlay();
					}
				}
				if (OpenTaiko.ConfigIni.KeyAssign.KeyIsPressed(OpenTaiko.ConfigIni.KeyAssign.Drums.TrainingMoveForwardMeasure)) {
					if (this.bTrainingPAUSE) {
						if (this.nCurrentMeasure < this.nMeasureCount) {
							this.nCurrentMeasure++;
							OpenTaiko.stage演奏ドラム画面.actPlayInfo.NowMeasure[0] = this.nCurrentMeasure;

							this.tMatchWithTheChartDisplayPosition(true);
							OpenTaiko.Skin.soundTrainingModeScrollSFX.tPlay();
						}
						if (t配列の値interval以下か(ref this.RBlue, SoundManager.PlayTimer.SystemTimeMs, OpenTaiko.ConfigIni.TokkunMashInterval)) {
							for (int index = 0; index < this.JumpPointList.Count; index++) {
								if (this.JumpPointList[index].Time >= SoundManager.PlayTimer.NowTimeMs * OpenTaiko.ConfigIni.SongPlaybackSpeed) {
									this.nCurrentMeasure = this.JumpPointList[index].Measure;
									OpenTaiko.stage演奏ドラム画面.actPlayInfo.NowMeasure[0] = this.nCurrentMeasure;
									OpenTaiko.Skin.soundSkip.tPlay();
									this.tMatchWithTheChartDisplayPosition(false);
									break;
								}
							}
						}

					}
				}
				if (OpenTaiko.ConfigIni.KeyAssign.KeyIsPressed(OpenTaiko.ConfigIni.KeyAssign.Drums.TrainingMoveBackMeasure)) {
					if (this.bTrainingPAUSE) {
						if (this.nCurrentMeasure > 1) {
							this.nCurrentMeasure--;
							OpenTaiko.stage演奏ドラム画面.actPlayInfo.NowMeasure[0] = this.nCurrentMeasure;

							this.tMatchWithTheChartDisplayPosition(true);
							OpenTaiko.Skin.soundTrainingModeScrollSFX.tPlay();
						}
						if (t配列の値interval以下か(ref this.LBlue, SoundManager.PlayTimer.SystemTimeMs, OpenTaiko.ConfigIni.TokkunMashInterval)) {
							for (int index = this.JumpPointList.Count - 1; index >= 0; index--) {
								if (this.JumpPointList[index].Time <= SoundManager.PlayTimer.NowTimeMs * OpenTaiko.ConfigIni.SongPlaybackSpeed) {
									this.nCurrentMeasure = this.JumpPointList[index].Measure;
									OpenTaiko.stage演奏ドラム画面.actPlayInfo.NowMeasure[0] = this.nCurrentMeasure;
									OpenTaiko.Skin.sound特訓スキップ音.tPlay();
									this.tMatchWithTheChartDisplayPosition(false);
									break;
								}
							}
						}
					}
				}
				if (OpenTaiko.ConfigIni.KeyAssign.KeyIsPressed(OpenTaiko.ConfigIni.KeyAssign.Drums.TrainingDecreaseSongSpeed)) {
					if (this.bTrainingPAUSE) {
						if (OpenTaiko.ConfigIni.nSongSpeed > 6) {
							OpenTaiko.ConfigIni.nSongSpeed = OpenTaiko.ConfigIni.nSongSpeed - 2;
							this.tMatchWithTheChartDisplayPosition(false);
						}
					}
				}
				if (OpenTaiko.ConfigIni.KeyAssign.KeyIsPressed(OpenTaiko.ConfigIni.KeyAssign.Drums.TrainingIncreaseSongSpeed)) {
					if (this.bTrainingPAUSE) {
						if (OpenTaiko.ConfigIni.nSongSpeed < 399) {
							OpenTaiko.ConfigIni.nSongSpeed = OpenTaiko.ConfigIni.nSongSpeed + 2;
							this.tMatchWithTheChartDisplayPosition(false);
						}
					}
				}
				if (OpenTaiko.ConfigIni.KeyAssign.KeyIsPressed(OpenTaiko.ConfigIni.KeyAssign.Drums.TrainingJumpToFirstMeasure)) {
					if (this.bTrainingPAUSE) {
						if (this.nCurrentMeasure > 1) {
							this.nCurrentMeasure = 1;
							OpenTaiko.stage演奏ドラム画面.actPlayInfo.NowMeasure[0] = this.nCurrentMeasure;

							this.tMatchWithTheChartDisplayPosition(true);
							OpenTaiko.Skin.soundTrainingModeScrollSFX.tPlay();
						}
					}
				}
				if (OpenTaiko.ConfigIni.KeyAssign.KeyIsPressed(OpenTaiko.ConfigIni.KeyAssign.Drums.TrainingJumpToLastMeasure)) {
					if (this.bTrainingPAUSE) {
						if (this.nCurrentMeasure < this.nMeasureCount) {
							this.nCurrentMeasure = this.nMeasureCount;
							OpenTaiko.stage演奏ドラム画面.actPlayInfo.NowMeasure[0] = this.nCurrentMeasure;

							this.tMatchWithTheChartDisplayPosition(true);
							OpenTaiko.Skin.soundTrainingModeScrollSFX.tPlay();
						}
					}
				}
				if (OpenTaiko.ConfigIni.KeyAssign.KeyIsPressed(OpenTaiko.ConfigIni.KeyAssign.Drums.TrainingBookmark))
					this.tToggleBookmarkAtTheCurrentPosition();

				if (this.bCurrentlyScrolling) {
					SoundManager.PlayTimer.NowTimeMs = easing.EaseOut(this.ctScrollCounter, (int)this.nスクロール前ms, (int)this.nスクロール後ms, Easing.CalcType.Circular);

					this.ctScrollCounter.Tick();

					if ((int)SoundManager.PlayTimer.NowTimeMs == (int)this.nスクロール後ms) {
						this.bCurrentlyScrolling = false;
						SoundManager.PlayTimer.NowTimeMs = this.nスクロール後ms;
					}
				}
				if (!this.bTrainingPAUSE) {
					if (this.nCurrentMeasure < OpenTaiko.stage演奏ドラム画面.actPlayInfo.NowMeasure[0]) {
						this.nCurrentMeasure = OpenTaiko.stage演奏ドラム画面.actPlayInfo.NowMeasure[0];
					}

					if (SoundManager.PlayTimer.NowTimeMs * OpenTaiko.ConfigIni.SongPlaybackSpeed > this.n最終演奏位置ms) {
						this.n最終演奏位置ms = (long)(SoundManager.PlayTimer.NowTimeMs * OpenTaiko.ConfigIni.SongPlaybackSpeed);
					}
				}

			}

			var current = (double)(SoundManager.PlayTimer.NowTimeMs * OpenTaiko.ConfigIni.SongPlaybackSpeed);
			var percentage = current / length;

			var currentWhite = (double)(this.n最終演奏位置ms);
			var percentageWhite = currentWhite / length;

			if (OpenTaiko.Tx.Tokkun_ProgressBarWhite != null) OpenTaiko.Tx.Tokkun_ProgressBarWhite.t2D描画(OpenTaiko.Skin.Game_Training_ProgressBar_XY[0], OpenTaiko.Skin.Game_Training_ProgressBar_XY[1], new Rectangle(1, 1, (int)(OpenTaiko.Tx.Tokkun_ProgressBarWhite.szTextureSize.Width * percentageWhite), OpenTaiko.Tx.Tokkun_ProgressBarWhite.szTextureSize.Height));
			if (OpenTaiko.Tx.Tokkun_ProgressBar != null) OpenTaiko.Tx.Tokkun_ProgressBar.t2D描画(OpenTaiko.Skin.Game_Training_ProgressBar_XY[0], OpenTaiko.Skin.Game_Training_ProgressBar_XY[1], new Rectangle(1, 1, (int)(OpenTaiko.Tx.Tokkun_ProgressBar.szTextureSize.Width * percentage), OpenTaiko.Tx.Tokkun_ProgressBar.szTextureSize.Height));
			if (OpenTaiko.Tx.Tokkun_GoGoPoint != null) {
				foreach (int xpos in gogoXList) {
					OpenTaiko.Tx.Tokkun_GoGoPoint.t2D描画(xpos + OpenTaiko.Skin.Game_Training_ProgressBar_XY[0] - (OpenTaiko.Tx.Tokkun_GoGoPoint.szTextureSize.Width / 2), OpenTaiko.Skin.Game_Training_GoGoPoint_Y);
				}
			}

			if (OpenTaiko.Tx.Tokkun_JumpPoint != null) {
				foreach (STJUMPP xpos in JumpPointList) {
					var width = 0;
					if (OpenTaiko.Tx.Tokkun_ProgressBar != null) width = OpenTaiko.Tx.Tokkun_ProgressBar.szTextureSize.Width;

					int x = (int)((double)width * ((double)xpos.Time / (double)length));
					OpenTaiko.Tx.Tokkun_JumpPoint.t2D描画(x + OpenTaiko.Skin.Game_Training_ProgressBar_XY[0] - (OpenTaiko.Tx.Tokkun_JumpPoint.szTextureSize.Width / 2), OpenTaiko.Skin.Game_Training_JumpPoint_Y);
				}
			}

			return base.Draw();
		}

		public int On進行描画_背景() {
			if (this.ctBackgroundScrollTimer != null) {
				this.ctBackgroundScrollTimer.TickLoop();

				double TexSize = OpenTaiko.Skin.Resolution[0] / OpenTaiko.Tx.Tokkun_Background_Up.szTextureSize.Width;
				// 1280をテクスチャサイズで割ったものを切り上げて、プラス+1足す。
				int ForLoop = (int)Math.Ceiling(TexSize) + 1;
				OpenTaiko.Tx.Tokkun_Background_Up.t2D描画(0 - this.ctBackgroundScrollTimer.CurrentValue, OpenTaiko.Skin.Background_Scroll_Y[0]);
				for (int l = 1; l < ForLoop + 1; l++) {
					OpenTaiko.Tx.Tokkun_Background_Up.t2D描画(+(l * OpenTaiko.Tx.Tokkun_Background_Up.szTextureSize.Width) - this.ctBackgroundScrollTimer.CurrentValue, OpenTaiko.Skin.Background_Scroll_Y[0]);
				}
			}

			if (OpenTaiko.Tx.Tokkun_DownBG != null) OpenTaiko.Tx.Tokkun_DownBG.t2D描画(OpenTaiko.Skin.Game_Training_DownBG[0], OpenTaiko.Skin.Game_Training_DownBG[1]);
			if (OpenTaiko.Tx.Tokkun_BigTaiko != null) OpenTaiko.Tx.Tokkun_BigTaiko.t2D描画(OpenTaiko.Skin.Game_Training_BigTaiko[0], OpenTaiko.Skin.Game_Training_BigTaiko[1]);

			return base.Draw();
		}

		public void On進行描画_小節_速度() {
			if (OpenTaiko.Tx.Tokkun_Speed_Measure != null)
				OpenTaiko.Tx.Tokkun_Speed_Measure.t2D描画(OpenTaiko.Skin.Game_Training_Speed_Measure[0], OpenTaiko.Skin.Game_Training_Speed_Measure[1]);
			var maxMeasureStr = this.nMeasureCount.ToString();
			var measureStr = OpenTaiko.stage演奏ドラム画面.actPlayInfo.NowMeasure[0].ToString();
			if (OpenTaiko.Tx.Tokkun_SmallNumber != null) {
				var x = OpenTaiko.Skin.Game_Training_MaxMeasureCount_XY[0];
				foreach (char c in maxMeasureStr) {
					var currentNum = int.Parse(c.ToString());
					OpenTaiko.Tx.Tokkun_SmallNumber.t2D描画(x, OpenTaiko.Skin.Game_Training_MaxMeasureCount_XY[1], new Rectangle(OpenTaiko.Skin.Game_Training_SmallNumber_Width * currentNum, 0, OpenTaiko.Skin.Game_Training_SmallNumber_Width, OpenTaiko.Tx.Tokkun_SmallNumber.szTextureSize.Height));
					x += OpenTaiko.Skin.Game_Training_SmallNumber_Width - 2;
				}
			}

			var subtractVal = (OpenTaiko.Skin.Game_Training_BigNumber_Width - 2) * (measureStr.Length - 1);

			if (OpenTaiko.Tx.Tokkun_BigNumber != null) {
				var x = OpenTaiko.Skin.Game_Training_CurrentMeasureCount_XY[0];
				foreach (char c in measureStr) {
					var currentNum = int.Parse(c.ToString());
					OpenTaiko.Tx.Tokkun_BigNumber.t2D描画(x - subtractVal, OpenTaiko.Skin.Game_Training_CurrentMeasureCount_XY[1], new Rectangle(OpenTaiko.Skin.Game_Training_BigNumber_Width * currentNum, 0, OpenTaiko.Skin.Game_Training_BigNumber_Width, OpenTaiko.Tx.Tokkun_BigNumber.szTextureSize.Height));
					x += OpenTaiko.Skin.Game_Training_BigNumber_Width - 2;
				}

				var PlaySpdtmp = OpenTaiko.ConfigIni.SongPlaybackSpeed * 10.0d;
				PlaySpdtmp = Math.Round(PlaySpdtmp, MidpointRounding.AwayFromZero);

				var playSpd = PlaySpdtmp / 10.0d;
				var playSpdI = playSpd - (int)playSpd;
				var playSpdStr = Decimal.Round((decimal)playSpdI, 1, MidpointRounding.AwayFromZero).ToString();
				var decimalStr = (playSpdStr == "0") ? "0" : playSpdStr[2].ToString();

				OpenTaiko.Tx.Tokkun_BigNumber.t2D描画(OpenTaiko.Skin.Game_Training_SpeedDisplay_XY[0], OpenTaiko.Skin.Game_Training_SpeedDisplay_XY[1], new Rectangle(OpenTaiko.Skin.Game_Training_BigNumber_Width * int.Parse(decimalStr), 0, OpenTaiko.Skin.Game_Training_BigNumber_Width, OpenTaiko.Tx.Tokkun_BigNumber.szTextureSize.Height));

				x = OpenTaiko.Skin.Game_Training_SpeedDisplay_XY[0] - 25;

				subtractVal = OpenTaiko.Skin.Game_Training_BigNumber_Width * (((int)playSpd).ToString().Length - 1);

				foreach (char c in ((int)playSpd).ToString()) {
					var currentNum = int.Parse(c.ToString());
					OpenTaiko.Tx.Tokkun_BigNumber.t2D描画(x - subtractVal, OpenTaiko.Skin.Game_Training_SpeedDisplay_XY[1], new Rectangle(OpenTaiko.Skin.Game_Training_BigNumber_Width * currentNum, 0, OpenTaiko.Skin.Game_Training_BigNumber_Width, OpenTaiko.Tx.Tokkun_BigNumber.szTextureSize.Height));
					x += OpenTaiko.Skin.Game_Training_BigNumber_Width - 2;
				}
			}
		}

		public void tPausePlay() {
			CDTX dTX = OpenTaiko.DTX;

			this.nスクロール後ms = SoundManager.PlayTimer.NowTimeMs;

			OpenTaiko.stage演奏ドラム画面.Activate();
			SoundManager.PlayTimer.Pause();

			for (int i = 0; i < dTX.listChip.Count; i++) {
				CDTX.CChip pChip = dTX.listChip[i];
				pChip.bHit = false;
				if (dTX.listChip[i].nチャンネル番号 != 0x50) {
					pChip.bShow = true;
					pChip.b可視 = true;
				}
			}

			OpenTaiko.DTX.t全チップの再生一時停止();
			OpenTaiko.stage演奏ドラム画面.bPAUSE = true;
			OpenTaiko.stage演奏ドラム画面.actPlayInfo.NowMeasure[0] = this.nCurrentMeasure;
			this.bTrainingPAUSE = true;
			if (OpenTaiko.ConfigIni.bTokkunMode && OpenTaiko.stage演奏ドラム画面.actBalloon.KusudamaIsActive) OpenTaiko.stage演奏ドラム画面.actBalloon.KusuMiss();

			this.tMatchWithTheChartDisplayPosition(false);
		}

		public void tResumePlay() {
			CDTX dTX = OpenTaiko.DTX;

			this.bCurrentlyScrolling = false;
			SoundManager.PlayTimer.NowTimeMs = this.nスクロール後ms;

			int n演奏開始Chip = OpenTaiko.stage演奏ドラム画面.n現在のトップChip;
			int finalStartBar;

			finalStartBar = this.nCurrentMeasure - 2;
			if (finalStartBar < 0) finalStartBar = 0;

			OpenTaiko.stage演奏ドラム画面.t演奏位置の変更(finalStartBar, 0);


			int n少し戻ってから演奏開始Chip = OpenTaiko.stage演奏ドラム画面.n現在のトップChip;
      
			OpenTaiko.stage演奏ドラム画面.actPlayInfo.NowMeasure[0] = 0;
			OpenTaiko.stage演奏ドラム画面.t数値の初期化(true, true);
			OpenTaiko.stage演奏ドラム画面.Activate();
			if (OpenTaiko.ConfigIni.bTokkunMode && OpenTaiko.stage演奏ドラム画面.actBalloon.KusudamaIsActive) OpenTaiko.stage演奏ドラム画面.actBalloon.KusuMiss();

			for (int i = 0; i < dTX.listChip.Count; i++) {

				//if (i < n演奏開始Chip && (dTX.listChip[i].nチャンネル番号 > 0x10 && dTX.listChip[i].nチャンネル番号 < 0x20)) //2020.07.08 ノーツだけ消す。 null参照回避のために順番変更
				if (i < n演奏開始Chip && NotesManager.IsHittableNote(dTX.listChip[i])) {
					dTX.listChip[i].bHit = true;
					dTX.listChip[i].IsHitted = true;
					dTX.listChip[i].b可視 = false;
					dTX.listChip[i].bShow = false;
				}
				if (i < n少し戻ってから演奏開始Chip && dTX.listChip[i].nチャンネル番号 == 0x01) {
					dTX.listChip[i].bHit = true;
					dTX.listChip[i].IsHitted = true;
					dTX.listChip[i].b可視 = false;
					dTX.listChip[i].bShow = false;
				}
				if (dTX.listChip[i].nチャンネル番号 == 0x50 && dTX.listChip[i].n整数値_内部番号 < finalStartBar) {
					dTX.listChip[i].bHit = true;
					dTX.listChip[i].IsHitted = true;
				}

			}

			for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
				OpenTaiko.stage演奏ドラム画面.chip現在処理中の連打チップ[i] = null;
			}

			this.bTrainingPAUSE = false;
		}

		public void tMatchWithTheChartDisplayPosition(bool doScroll) {
			this.nスクロール前ms = SoundManager.PlayTimer.NowTimeMs;

			CDTX dTX = OpenTaiko.DTX;

			bool bSuccessSeek = false;
			for (int i = 0; i < dTX.listChip.Count; i++) {
				CDTX.CChip pChip = dTX.listChip[i];

				if (pChip.nチャンネル番号 == 0x50 && pChip.n整数値_内部番号 > nCurrentMeasure - 1) {
					bSuccessSeek = true;
					OpenTaiko.stage演奏ドラム画面.n現在のトップChip = i;
					break;
				}
			}
			if (!bSuccessSeek) {
				OpenTaiko.stage演奏ドラム画面.n現在のトップChip = 0;
			} else {
				while (dTX.listChip[OpenTaiko.stage演奏ドラム画面.n現在のトップChip].n発声時刻ms == dTX.listChip[OpenTaiko.stage演奏ドラム画面.n現在のトップChip - 1].n発声時刻ms && OpenTaiko.stage演奏ドラム画面.n現在のトップChip != 0)
					OpenTaiko.stage演奏ドラム画面.n現在のトップChip--;
			}

			if (doScroll) {
				this.nスクロール後ms = (long)(dTX.listChip[OpenTaiko.stage演奏ドラム画面.n現在のトップChip].n発声時刻ms / OpenTaiko.ConfigIni.SongPlaybackSpeed);
				this.bCurrentlyScrolling = true;

				this.ctScrollCounter = new CCounter(0, OpenTaiko.Skin.Game_Training_ScrollTime, 1, OpenTaiko.Timer);
			} else {
				SoundManager.PlayTimer.NowTimeMs = (long)(dTX.listChip[OpenTaiko.stage演奏ドラム画面.n現在のトップChip].n発声時刻ms / OpenTaiko.ConfigIni.SongPlaybackSpeed);
				this.nスクロール後ms = SoundManager.PlayTimer.NowTimeMs;
			}
		}

		public void tToggleBookmarkAtTheCurrentPosition() {
			if (!this.bCurrentlyScrolling && this.bTrainingPAUSE) {
				STJUMPP _JumpPoint = new STJUMPP() { Time = (long)(SoundManager.PlayTimer.NowTimeMs * OpenTaiko.ConfigIni.SongPlaybackSpeed), Measure = this.nCurrentMeasure };

				if (!JumpPointList.Contains(_JumpPoint))
					JumpPointList.Add(_JumpPoint);
				else
					JumpPointList.Remove(_JumpPoint);
				OpenTaiko.Skin.soundTrainingToggleBookmarkSFX.tPlay();
				JumpPointList.Sort((a, b) => a.Time.CompareTo(b.Time));
			}
		}

		private bool t配列の値interval以下か(ref long[] array, long num, int interval) {
			long[] arraytmp = array;
			for (int index = 0; index < (array.Length - 1); index++) {
				array[index] = array[index + 1];
			}
			array[array.Length - 1] = num;
			return Math.Abs(num - arraytmp[0]) <= interval;
		}

		public int nCurrentMeasure;
		public int nMeasureCount;

		#region [private]
		private long nスクロール前ms;
		private long nスクロール後ms;
		private long n最終演奏位置ms;

		public bool bTrainingPAUSE { get; private set; }
		private bool bCurrentlyScrolling;

		private CCounter ctScrollCounter;
		private CCounter ctBackgroundScrollTimer;
		private Easing easing = new Easing();
		private long length = 1;

		private List<int> gogoXList;
		private List<STJUMPP> JumpPointList;
		private long[] LBlue = new long[] { 0, 0, 0, 0, 0 };
		private long[] RBlue = new long[] { 0, 0, 0, 0, 0 };

		private struct STJUMPP {
			public long Time;
			public int Measure;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="time">今の時間</param>
		/// <param name="begin">最初の値</param>
		/// <param name="change">最終の値-最初の値</param>
		/// <param name="duration">全体の時間</param>
		/// <returns></returns>
		private int EasingCircular(int time, int begin, int change, int duration) {
			double t = time, b = begin, c = change, d = duration;

			t = t / d * 2;
			if (t < 1)
				return (int)(-c / 2 * (Math.Sqrt(1 - t * t) - 1) + b);
			else {
				t = t - 2;
				return (int)(c / 2 * (Math.Sqrt(1 - t * t) + 1) + b);
			}
		}

		#endregion
	}
}

