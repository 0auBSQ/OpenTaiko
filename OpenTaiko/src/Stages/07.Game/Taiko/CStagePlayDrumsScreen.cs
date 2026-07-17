using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using DiscordRPC;
using FDK;
using Point = System.Drawing.Point;
using Rectangle = System.Drawing.Rectangle;
namespace OpenTaiko;

internal partial class CStagePlayDrumsScreen : CStagePlayScreenCommon {
	// コンストラクタ

	public CStagePlayDrumsScreen() {
		base.eStageID = CStage.EStage.Game;
		base.ePhaseID = CStage.EPhase.Common_NORMAL;
		base.IsDeActivated = true;
		base.ChildActivities.Add(this.actPad = new CActImplPad());
		base.ChildActivities.Add(this.actCombo = new CActImplCombo());
		base.ChildActivities.Add(this.actChipFireD = new CActImplFireworks());
		base.ChildActivities.Add(this.Rainbow = new Rainbow());
		base.ChildActivities.Add(this.actGauge = new CActImplGauge());
		base.ChildActivities.Add(this.actJudgeString = new CActImplJudgeText());
		base.ChildActivities.Add(this.actTaikoLaneFlash = new TaikoLaneFlash());
		base.ChildActivities.Add(this.actScore = new CActImplScore());
		base.ChildActivities.Add(this.actScrollSpeed = new CActTaikoScrollSpeed());
		base.ChildActivities.Add(this.actAVI = new CActPlayAVI());
		base.ChildActivities.Add(this.actPanel = new CActPlayPanelString());
		base.ChildActivities.Add(this.actPlayInfo = new CActPlayPlayInfo());
		//base.list子Activities.Add( this.actFI = new CActFIFOBlack() );
		base.ChildActivities.Add(this.actFI = new CActFIFOStart());
		base.ChildActivities.Add(this.actFOBlack = new CActFIFOBlack());
		base.ChildActivities.Add(this.actFOClear = new CActFIFOResult());
		base.ChildActivities.Add(this.actEnd = new CActImplClearAnimation());
		base.ChildActivities.Add(this.actDancer = new CActImplDancer());
		base.ChildActivities.Add(this.actMtaiko = new CActImplMtaiko());
		base.ChildActivities.Add(this.actLaneTaiko = new CActImplLaneTaiko());
		base.ChildActivities.Add(this.actRoll = new CActImplRoll());
		base.ChildActivities.Add(this.actBalloon = new CActImplBalloon());
		base.ChildActivities.Add(this.actChara = new CActImplCharacter());
		base.ChildActivities.Add(this.actGame = new CActPlayDrumsGameMode());
		base.ChildActivities.Add(this.actBackground = new CActImplBackground());
		base.ChildActivities.Add(this.actRollChara = new CActImplRollEffect());
		base.ChildActivities.Add(this.actComboBalloon = new CActImplComboBalloon());
		base.ChildActivities.Add(this.actComboVoice = new CActPlayComboSound());
		base.ChildActivities.Add(this.actPauseMenu = new CActPlayPauseMenu());
		base.ChildActivities.Add(this.actChipEffects = new CActImplChipEffects());
		base.ChildActivities.Add(this.actFooter = new CActImplFooter());
		base.ChildActivities.Add(this.actRunner = new CActImplRunner());
		base.ChildActivities.Add(this.actMob = new CActImplMob());
		base.ChildActivities.Add(this.GoGoSplash = new GoGoSplash());
		base.ChildActivities.Add(this.FlyingNotes = new FlyingNotes());
		base.ChildActivities.Add(this.FireWorks = new FireWorks());
		base.ChildActivities.Add(this.PuchiChara = new PuchiChara());
		base.ChildActivities.Add(this.ScoreRank = new CActImplScoreRank());

		base.ChildActivities.Add(this.actDan = new Dan_Cert());
		base.ChildActivities.Add(this.actTokkun = new CActImplTrainingMode());
		base.ChildActivities.Add(this.actAIBattle = new AIBattle());
		#region[ 文字初期化 ]
		STTextPosition[] stTextPositionArray = new STTextPosition[12];
		STTextPosition stTextPosition = new STTextPosition();
		stTextPosition.ch = '0';
		stTextPosition.pt = new Point(0, 0);
		stTextPositionArray[0] = stTextPosition;
		STTextPosition stTextPosition2 = new STTextPosition();
		stTextPosition2.ch = '1';
		stTextPosition2.pt = new Point(32, 0);
		stTextPositionArray[1] = stTextPosition2;
		STTextPosition stTextPosition3 = new STTextPosition();
		stTextPosition3.ch = '2';
		stTextPosition3.pt = new Point(64, 0);
		stTextPositionArray[2] = stTextPosition3;
		STTextPosition stTextPosition4 = new STTextPosition();
		stTextPosition4.ch = '3';
		stTextPosition4.pt = new Point(96, 0);
		stTextPositionArray[3] = stTextPosition4;
		STTextPosition stTextPosition5 = new STTextPosition();
		stTextPosition5.ch = '4';
		stTextPosition5.pt = new Point(128, 0);
		stTextPositionArray[4] = stTextPosition5;
		STTextPosition stTextPosition6 = new STTextPosition();
		stTextPosition6.ch = '5';
		stTextPosition6.pt = new Point(160, 0);
		stTextPositionArray[5] = stTextPosition6;
		STTextPosition stTextPosition7 = new STTextPosition();
		stTextPosition7.ch = '6';
		stTextPosition7.pt = new Point(192, 0);
		stTextPositionArray[6] = stTextPosition7;
		STTextPosition stTextPosition8 = new STTextPosition();
		stTextPosition8.ch = '7';
		stTextPosition8.pt = new Point(224, 0);
		stTextPositionArray[7] = stTextPosition8;
		STTextPosition stTextPosition9 = new STTextPosition();
		stTextPosition9.ch = '8';
		stTextPosition9.pt = new Point(256, 0);
		stTextPositionArray[8] = stTextPosition9;
		STTextPosition stTextPosition10 = new STTextPosition();
		stTextPosition10.ch = '9';
		stTextPosition10.pt = new Point(288, 0);
		stTextPositionArray[9] = stTextPosition10;
		STTextPosition stTextPosition11 = new STTextPosition();
		stTextPosition11.ch = '%';
		stTextPosition11.pt = new Point(320, 0);
		stTextPositionArray[10] = stTextPosition11;
		STTextPosition stTextPosition12 = new STTextPosition();
		stTextPosition12.ch = ' ';
		stTextPosition12.pt = new Point(0, 0);
		stTextPositionArray[11] = stTextPosition12;
		this.stSmallPosition = stTextPositionArray;

		stTextPositionArray = new STTextPosition[12];
		stTextPosition = new STTextPosition();
		stTextPosition.ch = '0';
		stTextPosition.pt = new Point(0, 0);
		stTextPositionArray[0] = stTextPosition;
		stTextPosition2 = new STTextPosition();
		stTextPosition2.ch = '1';
		stTextPosition2.pt = new Point(32, 0);
		stTextPositionArray[1] = stTextPosition2;
		stTextPosition3 = new STTextPosition();
		stTextPosition3.ch = '2';
		stTextPosition3.pt = new Point(64, 0);
		stTextPositionArray[2] = stTextPosition3;
		stTextPosition4 = new STTextPosition();
		stTextPosition4.ch = '3';
		stTextPosition4.pt = new Point(96, 0);
		stTextPositionArray[3] = stTextPosition4;
		stTextPosition5 = new STTextPosition();
		stTextPosition5.ch = '4';
		stTextPosition5.pt = new Point(128, 0);
		stTextPositionArray[4] = stTextPosition5;
		stTextPosition6 = new STTextPosition();
		stTextPosition6.ch = '5';
		stTextPosition6.pt = new Point(160, 0);
		stTextPositionArray[5] = stTextPosition6;
		stTextPosition7 = new STTextPosition();
		stTextPosition7.ch = '6';
		stTextPosition7.pt = new Point(192, 0);
		stTextPositionArray[6] = stTextPosition7;
		stTextPosition8 = new STTextPosition();
		stTextPosition8.ch = '7';
		stTextPosition8.pt = new Point(224, 0);
		stTextPositionArray[7] = stTextPosition8;
		stTextPosition9 = new STTextPosition();
		stTextPosition9.ch = '8';
		stTextPosition9.pt = new Point(256, 0);
		stTextPositionArray[8] = stTextPosition9;
		stTextPosition10 = new STTextPosition();
		stTextPosition10.ch = '9';
		stTextPosition10.pt = new Point(288, 0);
		stTextPositionArray[9] = stTextPosition10;
		stTextPosition11 = new STTextPosition();
		stTextPosition11.ch = '%';
		stTextPosition11.pt = new Point(320, 0);
		stTextPositionArray[10] = stTextPosition11;
		stTextPosition12 = new STTextPosition();
		stTextPosition12.ch = ' ';
		stTextPosition12.pt = new Point(0, 0);
		stTextPositionArray[11] = stTextPosition12;
		this.stSmallPosition = stTextPositionArray;
		#endregion
	}


	// メソッド

	public void tPlayResultStore(out CScoreIni.CPlayRecord Drums) {
		base.tPlayResultStore_Drums(out Drums);
	}


	// CStage 実装

	public override System.Collections.Generic.IEnumerator<float> ActivateSteps() {
		LoudnessMetadataScanner.StopBackgroundScanning(joinImmediately: false);

		this.ctHandHold = new CCounter(0, 60, 20, OpenTaiko.Timer);

		// Hit sounds: ~20 BASS streams across players. Create them NON-BLOCKING on the budgeted finalize queue
		// (BASS stays render-thread, just spread across frames) so they don't freeze the load — they finalize
		// during the song lead-in and the play path already null-checks them.
		// (Calibration note: the drum hit sounds are intentionally NOT played during input calibration, so the
		// player focuses on their controller + the calibration audio rather than lining up their own SFX.)
		for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
			int actual = i;
			var hs = OpenTaiko.Skin.hsHitSoundsInformations;
			Game.AsyncActions.Enqueue(() => {
				this.soundRed[actual] = OpenTaiko.SoundManager.tCreateSound(hs.don[actual], ESoundGroup.SoundEffect);
				this.soundBlue[actual] = OpenTaiko.SoundManager.tCreateSound(hs.ka[actual], ESoundGroup.SoundEffect);
				this.soundAdlib[actual] = OpenTaiko.SoundManager.tCreateSound(hs.adlib[actual], ESoundGroup.SoundEffect);
				this.soundClap[actual] = OpenTaiko.SoundManager.tCreateSound(hs.clap[actual], ESoundGroup.SoundEffect);

				int _panning = OpenTaiko.ConfigIni.nPanning[OpenTaiko.ConfigIni.nPlayerCount - 1][actual];
				if (this.soundRed[actual] != null) this.soundRed[actual].SoundPosition = _panning;
				if (this.soundBlue[actual] != null) this.soundBlue[actual].SoundPosition = _panning;
				if (this.soundAdlib[actual] != null) this.soundAdlib[actual].SoundPosition = _panning;
				if (this.soundClap[actual] != null) this.soundClap[actual].SoundPosition = _panning;
			});
		}

		// Run the common build (children + note state), yielding through its steps. The base sets IsActivated /
		// IsFirstDraw (the old "base.Activate() // to unpause at end").
		var it = base.ActivateSteps();
		while (it.MoveNext()) yield return it.Current;
	}

	public override void tValueInitialize(bool bPlayRecord, bool bPlayState) {
		int iPrevTopChipMax = this.nCurrentTopChip.Max();
		base.tValueInitialize(bPlayRecord, bPlayState);
		for (int i = 0; i < 5; i++) { replayCursor[i] = 0; msReplayTjaTime[i] = double.NegativeInfinity; }

		if (bPlayState) {
			this.actGame.tTatakikiriShow_Initialize();

			for (int i = 0; i < 5; i++) {
				if (bIsAlreadyCleared[i]) {
					actBackground.ClearIn(i);
				}
			}

			// reset failure status if recorded
			this.actEnd.InitScripts();
		}

		if (bPlayState) {
			string diffToString(int diff) {
				string[] diffArr =
				{
					" Easy ",
					" Normal ",
					" Hard ",
					" Extreme ",
					" Extra ",
					" Tower ",
					" Dan "
				};
				string[] diffArrIcon =
				{
					"-",
					"",
					"+"
				};

				int level = OpenTaiko.SongMount.rChoosenSong.nLevel[diff];
				CTja.ELevelIcon levelIcon = OpenTaiko.SongMount.rChoosenSong.nLevelIcon[diff];

				return (diffArr[Math.Min(diff, 6)] + "Lv." + level + diffArrIcon[(int)levelIcon]);
			}

			// Discord Presence の更新
			string details = OpenTaiko.ConfigIni.SendDiscordPlayingInformation ? OpenTaiko.SongMount.rChoosenSong.ldTitle.GetString("")
																				 + diffToString(OpenTaiko.SongMount.nChoosenSongDifficulty[0]) : "";

			// Byte count must be used instead of String.Length.
			// The byte count is what Discord is concerned with. Some chars are greater than one byte.
			if (Encoding.UTF8.GetBytes(details).Length > 128) {
				byte[] details_byte = Encoding.UTF8.GetBytes(details);
				Array.Resize(ref details_byte, 128);
				details = Encoding.UTF8.GetString(details_byte);
			}

			var difficultyName = OpenTaiko.DifficultyNumberToEnum(OpenTaiko.SongMount.nChoosenSongDifficulty[0]).ToString();

			OpenTaiko.DiscordClient?.SetPresence(new RichPresence() {
				Details = details,
				State = "Playing" + (OpenTaiko.ConfigIni.bAutoPlay[0] == true ? " (Auto)" : ""),
				Timestamps = new Timestamps(DateTime.UtcNow, DateTime.UtcNow.AddMilliseconds(OpenTaiko.TJA.TjaTimeToGameTime(OpenTaiko.TJA.listChip[OpenTaiko.TJA.listChip.Count - 1].nSoundTimems))),
				Assets = new Assets() {
					SmallImageKey = OpenTaiko.ConfigIni.SendDiscordPlayingInformation ? difficultyName.ToLower() : "",
					SmallImageText = OpenTaiko.ConfigIni.SendDiscordPlayingInformation ? String.Format("COURSE:{0} ({1})", difficultyName, OpenTaiko.SongMount.nChoosenSongDifficulty[0]) : "",
					LargeImageKey = OpenTaiko.LargeImageKey,
					LargeImageText = OpenTaiko.LargeImageText,
				}
			});
		}

		if (!bPlayState && iPrevTopChipMax <= 0)
			return; // no needs to reset

		#region [reset accumulated chip state]
		this.bFillIn = false;
		this.nWaitBigNoteCoord = 0;

		this.actLaneTaiko.ResetPlayStates();

		for (int i = 0; i < 5; i++)
			PuchiChara.ChangeBPM(CTja.TjaDurationToGameDuration(60.0 / OpenTaiko.stageGameScreen.actPlayInfo.dbBPM[i]));

		//dbUnit = Math.Ceiling( dbUnit * 1000.0 );
		//dbUnit = dbUnit / 1000.0;

		//if (this.actChara.ctキャラクターアクションタイマ != null) this.actChara.ctキャラクターアクションタイマ = new CCounter();

		//this.actDancer.ct通常モーション = new CCounter( 0, this.actDancer.arモーション番号_通常.Length - 1, ( dbUnit * 4.0) / this.actDancer.arモーション番号_通常.Length, CSound管理.rc演奏用タイマ );
		//this.actDancer.ctモブ = new CCounter( 1.0, 16.0, ((60.0 / CDTXMania.stage演奏ドラム画面.actPlayInfo.dbBPM / 16.0 )), CSound管理.rc演奏用タイマ );


		this.ShownLyric2 = 0;
		#endregion
	}

	public override void DeActivate() {
		this.ctHandHold = null;

		this.pfReplayModeText?.Dispose(); this.pfReplayModeText = null;
		this.pfReplayModeTextSmall?.Dispose(); this.pfReplayModeTextSmall = null;
		this.ttkReplayMode = null; this.ttkReplayInvalid = null;

		// leaving a replay anywhere except to the result screen (quit / retry) → drop replay mode + restore the
		// real mods now, so the next play isn't hijacked by the recording. (the cleared→result path restores in the
		// result screen instead, so the auto modicon + persistence-skip survive through results.)
		if (OpenTaiko.bReplayMode[0]
			&& this.eFadeOutCompleteWhenReturnValue != EGameplayScreenReturnValue.StageCleared
			&& this.eFadeOutCompleteWhenReturnValue != EGameplayScreenReturnValue.StageFailed) {
			CSongReplay.tRestoreVirtualMods();
			for (int i = 0; i < 5; i++) { OpenTaiko.bReplayMode[i] = false; OpenTaiko.ReplayPlayback[i] = null; }
		}

		for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
			if (this.soundRed[i] != null)
				this.soundRed[i].tDispose();
			if (this.soundBlue[i] != null)
				this.soundBlue[i].tDispose();
			if (this.soundAdlib[i] != null)
				this.soundAdlib[i].tDispose();
			if (this.soundClap[i] != null)
				this.soundClap[i].tDispose();
		}

		base.DeActivate();

		LoudnessMetadataScanner.StartBackgroundScanning();
	}
	public override void CreateManagedResource() {
		base.CreateManagedResource();
	}
	public override void ReleaseManagedResource() {
		base.ReleaseManagedResource();
	}

	public override void SetStageFailed(int iPlayer, EStageAbort failType = EStageAbort.FailedFlow) {
		if (OpenTaiko.ConfigIni.bTokkunMode)
			return;
		var becomeStageFailed = (this.stageAbortType[iPlayer] == EStageAbort.None);
		base.SetStageFailed(iPlayer, failType);
		if (becomeStageFailed) {
			this.actChara.CharacterControllers[iPlayer].PlayAction(CCharacter.ANIM_GAME_CLEAR_OUT);
			this.actGauge.dbCurrentGaugeValue[iPlayer] = 0; // for indicate life failure in AI mode
			this.UpdateGauge(null, EKeyConfigPart.Taiko, iPlayer, ENoteJudge.Auto); // update gauge
			OpenTaiko.stageGameScreen.FloorManagement.CurrentNumberOfLives = 0; // prevent clear
			if (!OpenTaiko.ConfigIni.bAIBattleMode)
				this.actEnd.Start(iPlayer);
		}
	}
	public override int Draw() {
		base.sw.Start();
		if (!base.IsDeActivated) {
			// Online VS: hold at the very START of the gameplay screen until EVERY player has reached it too (the
			// real sync point). Report once, then wait for all peers or a 30s timeout (host then drops laggards so
			// they vanish). The song clock hasn't started yet (IsFirstDraw still pending). No-op when offline.
			if (base.IsFirstDraw && LuaNetworking.Active?.PlaySyncActive == true) {
				LuaNetworking.Active.ReportLoaded();
				if (!LuaNetworking.Active.LoadBarrierReady(30000)) {
					OnlinePlaySync.DrawWaiting("Waiting for all players...");
					base.sw.Stop();
					return 0;
				}
			}
			#region [ 初めての進行描画 ]
			if (base.IsFirstDraw) {
				SoundManager.PlayTimer.Reset();
				OpenTaiko.Timer.Reset();
				this.ctChipPatternAnime = new CCounter(0, 1, 500, OpenTaiko.Timer);

				// this.actChipFireD.Start( Eレーン.HH );	// #31554 2013.6.12 yyagi
				// 初チップヒット時のもたつき回避。最初にactChipFireD.Start()するときにJITが掛かって？
				// ものすごく待たされる(2回目以降と比べると2,3桁tick違う)。そこで最初の画面フェードインの間に
				// 一発Start()を掛けてJITの結果を生成させておく。

				base.ePhaseID = CStage.EPhase.Common_FADEIN;

				this.actFI.tFadeInStart();
				for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; ++i) {
					this.actLaneTaiko.BranchText_FadeIn(null, i);
				}

				// TJAPlayer3.Sound管理.tDisableUpdateBufferAutomatically();
				this._onlEndAnimsDone = false;   // online VS: reset the deferred-clear-anim guard each play
				base.IsFirstDraw = false;
			}
			#endregion
			// stage-fail check
			bool isTower = (OpenTaiko.SongMount.nChoosenSongDifficulty[0] == (int)Difficulty.Tower);

			if (!OpenTaiko.ConfigIni.bTokkunMode) {
				for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; ++i) {
					if (this.stageAbortType[i] == EStageAbort.Max)
						continue;
					EStageAbort failType = this.actGauge.IsRiskyFailed(i) ? EStageAbort.FailedStopSkipResult
						: (this.actGame.stTatakikiriShow.ctRemainingTime.IsEnded
							|| (isTower && OpenTaiko.stageGameScreen.FloorManagement.CurrentNumberOfLives <= 0)) ? EStageAbort.FailedStop
						: this.actGauge.IsRiskyMineFailed(i) ? EStageAbort.FailedFlow
						: EStageAbort.None;
					if (failType > this.stageAbortType[i])
						this.SetStageFailed(i, failType);
				}
			}

			bool BGA_Shown = OpenTaiko.ConfigIni.bEnableAVI && OpenTaiko.TJA.listVD.Count > 0 && ShowVideo;

			// Layer: background

			if (BGA_Shown && !OpenTaiko.ConfigIni.bTokkunMode && this.tProgressDraw_AVI()) {
				// BGMOVIE & #BGAON
			} else if (!OpenTaiko.ConfigIni.bTokkunMode && this.tProgressDraw_Background()) {
				// BGIMAGE
			} else if (OpenTaiko.ConfigIni.bEnableBGA) {
				if (OpenTaiko.ConfigIni.bTokkunMode)
					actTokkun.OnProgressDraw_Background();
				else
					actBackground.Draw();
			}

			// Layer: below-character background elements
			if (!BGA_Shown && !OpenTaiko.ConfigIni.bTokkunMode) {
				actRollChara.Draw();
				if (!this.isMultiPlay) {
					if (OpenTaiko.ConfigIni.ShowDancer)
						actDancer.Draw();
					if (OpenTaiko.ConfigIni.ShowFooter)
						this.actFooter.Draw();
				}
			}

			//this.t進行描画_グラフ();   // #24074 2011.01.23 add ikanick


			//this.t進行描画_DANGER();
			//this.t進行描画_判定ライン();

			// 1/2-player mode character
			if (OpenTaiko.ConfigIni.ShowChara && OpenTaiko.ConfigIni.nPlayerCount <= 2) {
				this.actChara.Draw();
			}

			// Layer: above-character background elements

			if (!BGA_Shown && !OpenTaiko.ConfigIni.bTokkunMode && OpenTaiko.ConfigIni.ShowMob)
				this.actMob.Draw();

			if (OpenTaiko.ConfigIni.eGameMode != EGame.Off)
				this.actGame.Draw();

			this.tProgressDraw_ChartScrollSpeed();
			this.tProgressDraw_ChipAnime();

			// Layer: note lane + below-note foreground elements

			this.actLaneTaiko.Draw();

			if (OpenTaiko.ConfigIni.ShowRunner && !OpenTaiko.ConfigIni.bAIBattleMode && !this.isMultiPlay)
				this.actRunner.Draw();

			//this.t進行描画_レーン();
			//this.t進行描画_レーンフラッシュD();

			if (BGA_Shown && !OpenTaiko.ConfigIni.bTokkunMode && !this.isMultiPlay)
				this.actAVI.tWindowDisplay();

			if (!OpenTaiko.ConfigIni.bNoInfo && !OpenTaiko.ConfigIni.bTokkunMode)
				this.tProgressDraw_Gauge();

			this.actLaneTaiko.GoGoFlame();

			// bIsFinishedPlaying was dependent on 2P in this case

			this.actDan.Draw();

			// Layer: notes & bar lines
			this.ctHandHold.TickLoop();

			for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
				// bIsFinishedPlaying = this.t進行描画_チップ(E楽器パート.DRUMS, i);
				bool btmp = this.tProgressDraw_Chip(EKeyConfigPart.Taiko, i);
				if (btmp == true)
					isFinishedPlaying[i] = true;

#if DEBUG
				if (OpenTaiko.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.D0)) {
					isFinishedPlaying[i] = true;
				}
#endif
			}

			// Layer: above-note elements

			this.actMtaiko.Draw();
			this.tDrawReplayModeIndicator();

			if (OpenTaiko.ConfigIni.bAIBattleMode) {
				this.actAIBattle.Draw();
			}

			this.GoGoSplash.Draw();
			this.tProgressDraw_RealTimeJudgeCountDisplay();

			if (!OpenTaiko.ConfigIni.bNoInfo)
				this.tProgressDraw_Combo();
			if (!OpenTaiko.ConfigIni.bNoInfo && !OpenTaiko.ConfigIni.bTokkunMode)
				this.tProgressDraw_Score();

			// note effects
			this.Rainbow.Draw();
			this.FireWorks.Draw();
			this.actChipEffects.Draw();
			this.FlyingNotes.Draw();
			this.tProgressDraw_ChipFireD();

			if (!OpenTaiko.ConfigIni.bNoInfo)
				this.tProgressDraw_PanelString();

			// dialogue balloons

			this.actComboBalloon.Draw();

			for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
				this.actRoll.OnProgressDraw(this.nCurrentRollCount[i], i);
			}

			// infos

			if (!OpenTaiko.ConfigIni.bNoInfo)
				this.tProgressDraw_JudgeString1_NormalPositionSpecifiedCase();

			actChara.OnDraw_Balloon();

			// Floor voice
			if (OpenTaiko.SongMount.nChoosenSongDifficulty[0] == (int)Difficulty.Tower)
				this.actComboVoice.tPlayFloorSound();

			this.tTotalControlMethod();

			this.ScoreRank.Draw();

			// object rendering
			if (!OpenTaiko.ConfigIni.bTokkunMode) {
				foreach (var (key, obj) in OpenTaiko.TJA.listObj)
					obj.tDraw();
			}

			// Layer: Interactive elements

			if (OpenTaiko.ConfigIni.bTokkunMode) {
				this.actTokkun.OnProgressDraw_Measure_Speed();
				actTokkun.Draw();
			}

			// LYRIC[S/FILE]: & #LYRIC

			if (!this.IsFailStopped()
				&& OpenTaiko.TJA.listLyric2.Count > ShownLyric2 && OpenTaiko.TJA.listLyric2[ShownLyric2].Time < (long)OpenTaiko.TJA.GameTimeToTjaTime(SoundManager.PlayTimer.NowTimeMs)
				) {
				this.actPanel.tLyricsTextureCreate(OpenTaiko.TJA.listLyric2[ShownLyric2++].TextTex);
			}

			this.actPanel.tLyricsTextureDraw();

			// Online VS: drive remote player spots — broadcast my score + feed peers' score/gauge (no-op offline)
			OnlinePlaySync.Tick(this);

			// handle retry states here
			this.actPauseMenu.Update();

			// Layer: Gameplay complete animation and fading out

			if (OpenTaiko.SongMount.bSongJumpPending)
				this.actEnd.InitScripts();
			bool bIsFinishedEndAnime = this.actEnd.Draw() == 1 ? true : false;
			bool bIsFinishedFadeout = this.tProgressDraw_FadeInOut();

			bool bIsFinishedPlaying = this.IsFinishedPlaying();
			bool bIsChartEnded = this.IsChartEnded();
			EStageAbort minAbortType = this.MinStageAbortType;

			// Song jump takes priority over all phase transitions
			if (OpenTaiko.SongMount.bSongJumpPending &&
				base.ePhaseID is not (CStage.EPhase.Game_EndStage_FadeOut or CStage.EPhase.Game_EndStage_Quit_FadeOut)) {
				this.eFadeOutCompleteWhenReturnValue = EGameplayScreenReturnValue.SongJump;
				base.ePhaseID = CStage.EPhase.Game_EndStage_FadeOut;
				this.actFO = this.actFOBlack;
				this.actFO.tFadeOutStart();
			}
			// Transition for failed games
			else if ((!OpenTaiko.ConfigIni.bAIBattleMode && minAbortType >= EStageAbort.FailedFlow) || this.IsStageFailed_Fast()) {
				if (base.ePhaseID is CStage.EPhase.Game_EndStage_FadeOut or CStage.EPhase.Game_EndStage_Quit_FadeOut) {
					// do nothing
				} else if (base.ePhaseID == EPhase.Game_STAGE_FAILED) {
					if (bIsFinishedEndAnime) {
						if (minAbortType >= EStageAbort.FailedStopSkipResult) {
							this.eFadeOutCompleteWhenReturnValue = EGameplayScreenReturnValue.StageFailed;
						} else {
							this.eFadeOutCompleteWhenReturnValue = EGameplayScreenReturnValue.StageCleared;
						}
						base.ePhaseID = CStage.EPhase.Game_EndStage_FadeOut;
						this.actFO = this.actFOBlack;
						this.actFO.tFadeOutStart();
					}
				} else {
					base.ePhaseID = CStage.EPhase.Game_STAGE_FAILED;
					OpenTaiko.TJA.tStopAllChips();
				}
			}
			// Transition for completed games:
			// 演奏終了→演出表示→フェードアウト
			else if (base.ePhaseID is CStage.EPhase.Game_EndStage_FadeOut or CStage.EPhase.Game_EndStage_Quit_FadeOut) {
				// do nothing
			} else if (base.ePhaseID == EPhase.Game_EndStage) {
				if (bIsFinishedEndAnime) {
					this.eFadeOutCompleteWhenReturnValue = EGameplayScreenReturnValue.StageCleared;
					base.ePhaseID = CStage.EPhase.Game_EndStage_FadeOut;
					this.actFO = this.actFOClear;
					this.actFO.tFadeOutStart();
				}
			} else if (base.ePhaseID == CStage.EPhase.Game_EndChart) {
				if (bIsFinishedPlaying) {
					if (OpenTaiko.ConfigIni.bTokkunMode) {
						bIsFinishedPlaying = false;
						OpenTaiko.Skin.soundTrainingStopSound.tPlay();
						actTokkun.tPausePlay();

						actTokkun.tMatchWithTheChartDisplayPosition(true);
					} else if (LuaNetworking.Active?.PlaySyncActive == true) {
						// Online VS: wait for every still-active player to finish, THEN play all clear anims with the
						// gathered results (correct clear/fail/full-combo), before moving to the result screen.
						if (LuaNetworking.Active.FinishBarrierReady(20000)) {
							if (!this._onlEndAnimsDone) {
								this.ApplyOnlineRemoteResults();   // sync remote spots' judges to the result screen
								for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) this.tEndClearAnim(i, isTower);
								this._onlEndAnimsDone = true;
							}
							base.ePhaseID = CStage.EPhase.Game_EndStage;
						}
					} else {
						base.ePhaseID = CStage.EPhase.Game_EndStage;
					}
				}
			} else if (this.IsEndOfPlay(bIsChartEnded, bIsFinishedPlaying)) {
				if (!OpenTaiko.ConfigIni.bTokkunMode) {
					if (LuaNetworking.Active?.PlaySyncActive == true) {
						// Online VS: gather my final result now; DEFER all clear anims until everyone finishes (played
						// in the Game_EndChart finish barrier above, so they reflect the gathered results).
						LuaNetworking.Active.ReportFinished(this.BuildOnlineFinishJson());
					} else {
						for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) this.tEndClearAnim(i, isTower);
					}
				}
				base.ePhaseID = CStage.EPhase.Game_EndChart;
			}

			this.tProgressDraw_PlayInfo();

			// draw above anything
			this.actPauseMenu.Draw();

			if (bIsFinishedFadeout) {
				Debug.WriteLine("Total On進行描画=" + sw.ElapsedMilliseconds + "ms");
				return (int)this.eFadeOutCompleteWhenReturnValue;
			}

			ManageMixerQueue();

			// キー入力

			this.tKeyInput();


		}
		base.sw.Stop();
		return 0;
	}

	// ── Online VS end-of-play ────────────────────────────────────────────────────────────────────────
	// The online result/finish plumbing (BuildOnlineFinishJson, ApplyOnlineRemoteResults, _onlEndAnimsDone)
	// lives in the partial file CStage演奏ドラム画面.Online.cs to keep the online concern out of this file.

	// Play spot i's end clear animation. Online-aware: a REMOTE spot uses the gathered broadcast result (so the
	// clear/fail/full-combo shown is correct); local/normal spots use the live gauge as before.
	private void tEndClearAnim(int i, bool isTower) {
		int onl = (LuaNetworking.Active?.IsRemoteSpot(i) == true) ? LuaNetworking.Active.GetSpotClearLevel(i) : -2;
		string anim;
		if (onl >= 0)
			anim = onl == 2 ? CCharacter.ANIM_GAME_10COMBO_MAX : onl == 1 ? CCharacter.ANIM_GAME_CLEARED : CCharacter.ANIM_GAME_FAILED;
		else if (isTower ? (OpenTaiko.stageGameScreen.FloorManagement.CurrentNumberOfLives >= OpenTaiko.stageGameScreen.FloorManagement.MaxNumberOfLives) : HGaugeMethods.UNSAFE_IsRainbow(i))
			anim = CCharacter.ANIM_GAME_10COMBO_MAX;
		else if (isTower ? (OpenTaiko.stageGameScreen.FloorManagement.CurrentNumberOfLives > 0) : HGaugeMethods.UNSAFE_FastNormaCheck(i))
			anim = CCharacter.ANIM_GAME_CLEARED;
		else
			anim = CCharacter.ANIM_GAME_FAILED;
		this.actChara.CharacterControllers[i].PlayAction(anim);
		this.actEnd.Start(i, endOfPlay: true);
	}

	protected override void UpdateClearAnimation(int iPlayer) {
		base.UpdateClearAnimation(iPlayer);
		this.actEnd.Start(iPlayer, endOfPlay: true);
	}

	public override bool IsEndOfPlay(bool? isChartEnded = null, bool? isFinishedPlaying = null)
		=> base.IsEndOfPlay(isChartEnded, isFinishedPlaying)
			&& (!(OpenTaiko.SongMount.nChoosenSongDifficulty[0] == (int)Difficulty.Tower) || this.actBackground.IsFinishedTowerClimbing());

	// その他

	#region [ private ]
	//-----------------
	[StructLayout(LayoutKind.Sequential)]
	private struct STTextPosition {
		public char ch;
		public Point pt;
	}
	public CActImplFireworks actChipFireD;

	private CActImplPad actPad;
	public CActImplMtaiko actMtaiko;
	public CActImplLaneTaiko actLaneTaiko;
	public CActImplClearAnimation actEnd;
	private CActPlayDrumsGameMode actGame;
	public CActImplTrainingMode actTokkun;
	public CActImplBackground actBackground;
	public GoGoSplash GoGoSplash;
	public FlyingNotes FlyingNotes;
	public FireWorks FireWorks;
	public PuchiChara PuchiChara;
	public CActImplScoreRank ScoreRank;
	private bool bFillIn;
	private readonly EPad[] eChanneltoPad = new EPad[]
	{
		EPad.HH, EPad.SD, EPad.BD, EPad.HT,
		EPad.LT, EPad.CY, EPad.FT, EPad.HHO,
		EPad.RD, EPad.Unknown, EPad.Unknown, EPad.LC,
		EPad.LP, EPad.LBD
	};
	private int[] nChanneltoXCoord = new int[] { 370, 470, 582, 527, 645, 748, 694, 373, 815, 298, 419, 419 };
	private CCounter ctHandHold;
	private CTexture txHitBarGB;
	private CTexture txLaneFrameGB;
	//private CTexture tx太鼓ノーツ;
	//private CTexture txHand;
	//private CTexture txSenotes;
	//private CTexture tx小節線;
	//private CTexture tx小節線_branch;

	private CTexture txJudgeCountDisplayPanel;
	private CTexture txJudgeCountSmall;
	//private CTexture txNamePlate; //ちょっと描画順で都合が悪くなるので移動。
	//private CTexture txNamePlate2P; //ちょっと描画順で都合が悪くなるので移動。
	//private CTexture txPlayerNumber;
	private CTexture txMovie; //2016.08.30 kairera0467 ウィンドウ表示

	public float nGauge = 0.0f;
	private int ShownLyric2 = 0;

	private StreamWriter stream;

	private int nWaitBigNoteCoord;
	private readonly STTextPosition[] stSmallPosition;
	private readonly STTextPosition[] stLargePosition;
	//-----------------

	private ENoteJudge tDrumsHitProcess(long nHitTime, EPad type, CChip pChip, bool bBothHandsInput, int nPlayer) {
		var nInput = NotesManager.PadToInputType(type, bBothHandsInput);
		if (!(pChip != null && NotesManager.IsHittableNote(pChip) && !NotesManager.IsRollEnd(pChip)))
			return ENoteJudge.Miss;

		var eJudge = this.tChipHitProcess(nHitTime, pChip, EKeyConfigPart.Taiko, true, nInput, nPlayer);
		if (NotesManager.IsGenericRoll(pChip))
			return eJudge;
		if (eJudge == ENoteJudge.Miss)
			return ENoteJudge.Miss;

		this.actGame.tTatakikiriShow_IncreaseValuesFromJudge(eJudge, (int)(nHitTime - pChip.nSoundTimems));
		return eJudge;
	}

	protected override void DrumsScrollSpeedUp() {
		OpenTaiko.ConfigIni.nScrollSpeed[0] = Math.Min(OpenTaiko.ConfigIni.nScrollSpeed[0] + 1, CConfigIni.MaximumScrollSpeed);
	}
	protected override void DrumsScrollSpeedDown() {
		OpenTaiko.ConfigIni.nScrollSpeed[0] = Math.Max(OpenTaiko.ConfigIni.nScrollSpeed[0] - 1, CConfigIni.MinimumScrollSpeed);
	}

	private void tProgressDraw_ChipFireD() {
		this.actChipFireD.Draw();
	}

	protected override void tConfetti_Start() {
		//if( this.actCombo.n現在のコンボ数.Drums % 10 == 0 && this.actCombo.n現在のコンボ数.Drums > 0 )
		{
			//this.actChipFireD.Start紙吹雪();
		}
	}

	// cursor into each replay's recorded input list, advanced as the play clock passes each input's time
	private int[] replayCursor = new int[5];

	// "> Replay Mode <" indicator under the 1P lane while watching a replay (fonts disposed in DeActivate)
	private CCachedFontRenderer pfReplayModeText;
	private CCachedFontRenderer pfReplayModeTextSmall;
	private TitleTextureKey ttkReplayMode;
	private TitleTextureKey ttkReplayInvalid;

	private void tDrawReplayModeIndicator() {
		if (!OpenTaiko.bReplayMode[0]) return;
		if (this.pfReplayModeText == null) {
			this.pfReplayModeText = HPrivateFastFont.tInstantiateMainFont(30);
			this.pfReplayModeTextSmall = HPrivateFastFont.tInstantiateMainFont(20);
			this.ttkReplayMode = new TitleTextureKey("> Replay Mode <", this.pfReplayModeText,
				System.Drawing.Color.White, System.Drawing.Color.Black, 600);
			this.ttkReplayInvalid = new TitleTextureKey("(Invalid replay file)", this.pfReplayModeTextSmall,
				System.Drawing.Color.OrangeRed, System.Drawing.Color.Black, 600);
		}
		int laneW = OpenTaiko.Tx.Lane_Background_Main?.szTextureSize.Width ?? 1000;
		int laneH = OpenTaiko.Tx.Lane_Background_Main?.szTextureSize.Height ?? 195;
		int cx = OpenTaiko.Skin.Game_Lane_X[0] + laneW / 2;
		int cy = OpenTaiko.Skin.Game_Lane_Y[0] + laneH + 26;

		// slow blink (1s cycle) so it reads as a status, not an alert
		double phase = (OpenTaiko.Timer.NowTimeMs % 1000) / 1000.0;
		var tx = TitleTextureKey.ResolveTitleTexture(this.ttkReplayMode);
		if (tx != null) {
			tx.Opacity = (int)(255 * (0.35 + 0.65 * Math.Abs(Math.Sin(Math.PI * phase))));
			tx.t2DScaledCenterBasedDraw(cx, cy);
		}

		var rep = OpenTaiko.ReplayPlayback[0];
		if (!this.IsReplayValid(0, rep)) {
			var tx2 = TitleTextureKey.ResolveTitleTexture(this.ttkReplayInvalid);
			if (tx2 != null) {
				tx2.Opacity = 255;
				tx2.t2DScaledCenterBasedDraw(cx, cy + 32);
			}
		}
	}

	private bool IsReplayValid(int iPlayer, CSongReplay rep) {
		if (rep == null)
			return false;
		if (rep.WarnChecksumMismatch)
			return false;

		// Performance informations
		bool judgeCountValid = rep.GoodCount >= this.CChartScore[iPlayer].nGreat
			&& rep.OkCount >= this.CChartScore[iPlayer].nGood
			&& rep.BadCount >= this.CChartScore[iPlayer].nMiss
			&& rep.RollCount >= this.CChartScore[iPlayer].nRoll
			&& rep.MaxCombo >= this.actCombo.nCurrentCombo.MaxValue[iPlayer]
			&& rep.BoomCount >= this.CChartScore[iPlayer].nMine
			&& rep.ADLibCount >= this.CChartScore[iPlayer].nADLIB;
		if (!judgeCountValid)
			return false;

		// Tower parameters
		if ((Difficulty)OpenTaiko.SongMount.nChoosenSongDifficulty[0] == Difficulty.Tower) {
			bool towerValid = rep.ReachedFloor >= this.FloorManagement.LastRegisteredFloor
				&& rep.RemainingLives <= this.FloorManagement.CurrentNumberOfLives;
			if (!towerValid)
				return false;
		}

		for (int songNo = 0; songNo < this.DanSongScore.Length; ++songNo) {
			bool judgeCountValidI = rep.IndividualGoodCount[songNo] >= this.DanSongScore[songNo].nGreat
				&& rep.IndividualOkCount[songNo] >= this.DanSongScore[songNo].nGood
				&& rep.IndividualBadCount[songNo] >= this.DanSongScore[songNo].nMiss
				&& rep.IndividualRollCount[songNo] >= this.DanSongScore[songNo].nRoll
				&& rep.IndividualMaxCombo[songNo] >= this.DanSongScore[songNo].nHighestCombo
				&& rep.IndividualBoomCount[songNo] >= this.DanSongScore[songNo].nMine
				&& rep.IndividualADLibCount[songNo] >= this.DanSongScore[songNo].nADLIB;
			if (!judgeCountValidI)
				return false;
		}

		return true;
	}

	// feed a replay's recorded (tjaTime, pad) inputs through the judge as the play clock reaches them
	private void tPumpReplayInputs() {
		for (int p = 0; p < OpenTaiko.ConfigIni.nPlayerCount; p++) {
			if (!OpenTaiko.bReplayMode[p]) continue;
			var rep = OpenTaiko.ReplayPlayback[p];
			CTja tja = OpenTaiko.GetTJA(p);
			if (rep == null || tja == null) continue;
			var inputs = rep.Inputs;
			// warp-aware chart time: recorded timestamps are in WARPED tja time (see tInputProcess_Drums), so
			// releasing them against the raw clock desynced Dynamic Beat replays once the factor left 1.0
			long nowTja = this.GetChartTimeNow(p);
			this.msReplayTjaTime[p] = nowTja;
			while (replayCursor[p] < inputs.Count && inputs[replayCursor[p]].Item1 <= nowTja) {
				long tHit = (long)inputs[replayCursor[p]].Item1;
				this.ProcessPadInput(p, (EPad)inputs[replayCursor[p]].Item2, tHit);
				replayCursor[p]++;
			}
		}
	}

	protected override void tInputProcess_Drums() {
		// Input adjust deprecated
		var nInputAdjustTimeMs = 0; // OpenTaiko.ConfigIni.nInputAdjustTimeMs;

		tPumpReplayInputs();

		foreach (var (nPad, inputEvent, order) in OpenTaiko.Pad.GetEvents(EKeyConfigPart.Taiko)) {      // #27029 2012.1.4 from: <10 to <=10; Eパッドの要素が１つ（HP）増えたため。
																//		  2012.1.5 yyagi: (int)Eパッド.MAX に変更。Eパッドの要素数への依存を無くすため。
			int nUsePlayer = NotesManager.GetPadPlayer(nPad);
			if (nUsePlayer >= OpenTaiko.ConfigIni.nPlayerCount
				|| OpenTaiko.stageGameScreen.isDeniedPlaying[nUsePlayer] || OpenTaiko.stageGameScreen.IsStageFailed_Fast()
				|| ((!OpenTaiko.ConfigIni.bTokkunMode || nUsePlayer > 0) && OpenTaiko.ConfigIni.bAutoPlay[nUsePlayer]) //2020.05.18 Mr-Ojii オート時の入力キャンセル
				|| OpenTaiko.bReplayMode[nUsePlayer]   // replay playback: ignore live input, inputs come from the recording
				|| (nUsePlayer == 1 && OpenTaiko.ConfigIni.bAIBattleMode)
				|| (LuaNetworking.Active != null && LuaNetworking.Active.IsRemoteSpot(nUsePlayer))   // online VS: spots 2-5 are remote players — ignore any local input mapped to them
				) {
				continue; // skip input
			}

			this.tInputMethodStore(EKeyConfigPart.Taiko);

			if (!inputEvent.Pressed)
				continue;

			// convert input time (mixer space) to note time
			CTja tja = OpenTaiko.GetTJA(nUsePlayer)!;
			long msInputMixer = SoundManager.PlayTimer.SystemTimeToGameTime(inputEvent.nTimeStamp);
			long rawInputGameTime = msInputMixer + nInputAdjustTimeMs;
			long msHitTjaTime = OpenTaiko.ConfigIni.nFunMods[nUsePlayer] == EFunMods.DynamicBeat
				? (long)(tja.GameTimeToTjaTime(rawInputGameTime) * dbDynamicBeatFactor + dbDynBeatTjaOffset)
				: (long)tja.GameTimeToTjaTime(rawInputGameTime);

			EPad nPadAs1P = NotesManager.PadTo1P(nPad);
			// Register to replay file
			OpenTaiko.ReplayInstances[nUsePlayer]?.tRegisterInput(msHitTjaTime, (byte)nPadAs1P);
			this.ProcessPadInput(nUsePlayer, nPadAs1P, msHitTjaTime);
		}
	}

	protected override void ProcessPadInput(int nUsePlayer, EPad nPad, long msHitTjaTime) {
		// test judgement
		var (chipNoHit, eJudge) = GetChipToJudge(msHitTjaTime, nUsePlayer, nPad);
		var gameType = this.eGameType[nUsePlayer];
		if (eJudge != ENoteJudge.Miss) {
			eJudge = this.JudgePadInput(nUsePlayer, chipNoHit, nPad, msHitTjaTime, eJudge);
			if (eJudge is not (ENoteJudge.Miss or ENoteJudge.Auto or ENoteJudge.ADLIB)) // ADLIB here for "empty hit but not a miss"
				gameType = NotesManager.GetChipGameType(chipNoHit, nUsePlayer);
		}

		// Visual and sound effects
		PlayerLane.FlashType nLane = NotesManager.PadToLane(nPad, gameType);
		int nHand = NotesManager.PadToHand(nPad);
		OpenTaiko.stageGameScreen.actMtaiko.tMtaikoEvent(NotesManager.PadToInputType(nPad), nHand, nUsePlayer);

		#region [ ヒットしてなかった場合は、レーンフラッシュ、パッドアニメ、空打ち音再生を実行 ]
		if (nLane is not PlayerLane.FlashType.Total && eJudge is ENoteJudge.Miss or ENoteJudge.Auto or ENoteJudge.ADLIB) { // ADLIB here for "empty hit but not a miss"
			this.PlayHitNoteSound(nUsePlayer, NotesManager.PadToInputType(nPad));
			this.StartHitNoteLaneFlash(nUsePlayer, NotesManager.PadToInputType(nPad), gameType);

			// BAD or TIGHT 時の処理。
			if (eJudge is ENoteJudge.Miss && OpenTaiko.ConfigIni.bTight)
				this.tChipHitProcess_BadAndTightWhenMiss(EKeyConfigPart.Taiko, eJudge, nUsePlayer, null);
		}
		#endregion
	}

	protected override ENoteJudge JudgePadInput(int nUsePlayer, CChip? chipNoHit, EPad nPad, long msHitTjaTime, ENoteJudge rawJudge, bool skipHit = false) {
		if (this.IsStageFailed_Fast()) // deny judgement
			return ENoteJudge.Miss;

		if (chipNoHit == null || rawJudge is ENoteJudge.Miss)
			return ENoteJudge.Miss;

		EGameType gameType = NotesManager.GetChipGameType(chipNoHit, nUsePlayer);
		PlayerLane.FlashType nLane = NotesManager.PadToLane(nPad, gameType);
		if (nLane == PlayerLane.FlashType.Total || !NotesManager.IsExpectedPadAnyHit(nPad, chipNoHit, gameType))
			return ENoteJudge.Miss;

		// Process big notes (judge big notes on)
		bool _isBigNoteTaiko = NotesManager.IsBigNoteTaiko(chipNoHit, gameType);
		if ((_isBigNoteTaiko && OpenTaiko.ConfigIni.bJudgeBigNotes) || NotesManager.IsSwapNote(chipNoHit, gameType)) {
			if (chipNoHit.eNoteState == ENoteState.None) {
				if (rawJudge is ENoteJudge.Poor)
					return skipHit ? rawJudge : this.tDrumsHitProcess(msHitTjaTime, nPad, chipNoHit, false, nUsePlayer);
				if (!skipHit) {
					chipNoHit.eNoteState = ENoteState.Wait;
					chipNoHit.msFirstMultiHit = msHitTjaTime;
					chipNoHit.padStoredHit = nPad;
				}
				this.chipNowProcessingMultiHitNotes[nUsePlayer].Add(chipNoHit);
				return ENoteJudge.ADLIB; // here for "empty hit but not a miss"
			} else if (chipNoHit.eNoteState == ENoteState.Wait) {
				bool _isExpected = NotesManager.IsExpectedPadMultiHit(chipNoHit.padStoredHit, nPad, chipNoHit, gameType);
				var msWaitedTime = msHitTjaTime - chipNoHit.msFirstMultiHit;
				if (_isExpected && msWaitedTime < OpenTaiko.ConfigIni.nBigNoteWaitTimems) {
					if (skipHit)
						return ENoteJudge.Perfect;
					chipNoHit.eNoteState = ENoteState.None;
					chipNoHit.padStoredHit = EPad.Unknown;
					return this.tDrumsHitProcess((long)chipNoHit.msFirstMultiHit, nPad, chipNoHit, true, nUsePlayer);
				}
			}
			return ENoteJudge.Miss;
		}
		if (skipHit)
			return rawJudge;
		chipNoHit.eNoteState = ENoteState.None;
		chipNoHit.padStoredHit = EPad.Unknown;
		return this.tDrumsHitProcess(msHitTjaTime, nPad, chipNoHit, _isBigNoteTaiko, nUsePlayer);
	}

	protected override void tBackgroundTextureCreate() {
		Rectangle bgrect = new Rectangle(0, 0, 1280, 720);
		string DefaultBgFilename = @$"Graphics{Path.DirectorySeparatorChar}5_Game{Path.DirectorySeparatorChar}5_Background{Path.DirectorySeparatorChar}0{Path.DirectorySeparatorChar}Background.png";
		string BgFilename = "";
		if (!String.IsNullOrEmpty(OpenTaiko.TJA.strBGIMAGE_PATH))
			BgFilename = OpenTaiko.TJA.strBGIMAGE_PATH;
		base.tBackgroundTextureCreate(DefaultBgFilename, bgrect, BgFilename);
	}
	protected override void tProgressDraw_Chip_Taiko(CConfigIni configIni, ref CTja tja, ref CChip pChip, int nPlayer, long nPlayTime) {
		NotesManager.ENoteType nt = NotesManager.GetNoteType(pChip);
		EGameType _gt = NotesManager.GetChipGameType(pChip, nPlayer);

		if (NotesManager.IsGenericRoll(nt)) {
			this.tProgressDraw_Chip_TaikoRoll(configIni, ref tja, ref pChip, nPlayer, nPlayTime, nt, _gt);
			return;
		}

		#region[ 作り直したもの ]

		if (pChip.bVisible) {
			if (!pChip.bHit) {
				int dx = pChip.nHorizontalChipDistance;
				int dy = pChip.nVerticalChipDistance;
				(dx, var dy_) = pChip.nScrollDirection switch {
					1 => (0, -dx), // ↓
					2 => (0, dx), // ↑
					3 => (dx, -dx), // ↙
					4 => (dx, +dx), // ↖
					5 => (-dx, 0), // →
					6 => (-dx, -dx), // ↘
					7 => (-dx, dx), // ↗
					0 or _ => (dx, dy), // ←
				};
				if (dy == 0) // TJAP3 behavior: vertical scrolling of non-real `#SCROLL` is kept
					dy = dy_;

				int x = GetNoteOriginX(nPlayer) + dx;
				int y = GetNoteOriginY(nPlayer) + dy;

				#region[ 両手待ち時 ]
				if (pChip.eNoteState == ENoteState.Wait) {
					x = (GetNoteOriginX(nPlayer));
					y = (GetNoteOriginY(nPlayer));
				}
				#endregion

				#region[ HIDSUD & STEALTH ]
				EStealthMode hiddenMode = OpenTaiko.ConfigIni.eSTEALTH[nPlayer];
				if (hiddenMode < EStealthMode.Stealth && !(pChip.bShow && pChip.bShowSudden))
					hiddenMode = EStealthMode.Stealth;
				if (hiddenMode < EStealthMode.Doron && this.bCustomDoron[nPlayer])
					hiddenMode = EStealthMode.Doron;
				#endregion

				if (bSplitLane[nPlayer] || OpenTaiko.Tx.Puchichara[PuchiChara.tGetPuchiCharaIndexByName(nPlayer)].effect.SplitLane) {
					if (NotesManager.IsAcceptRed(nt, _gt) && !NotesManager.IsAcceptBlue(nt, _gt)) {
						y -= NotesManager.PxSplitLaneDistance;
					} else if (NotesManager.IsAcceptBlue(nt, _gt) && !NotesManager.IsAcceptRed(nt, _gt)) {
						y += NotesManager.PxSplitLaneDistance;
					}
				}

				if (pChip.nSoundTimems < nPlayTime) {
					this.actGame.stTatakikiriShow.bFirstChipHit = true;
				}

				if (x > 0 - OpenTaiko.Skin.Game_Notes_Size[0] && x < OpenTaiko.Skin.Resolution[0]) {
					if (OpenTaiko.Tx.Notes[(int)_gt] != null) {
						int pxFaceTxOffset = this.GetPxFaceTextureOffset(nPlayer);
						var (nSenotesX, nSenotesY) = NotesManager.GetSENotesPos(nPlayer);
						if (NotesManager.IsHittableNote(nt)) {
							NotesManager.DisplayNoteArm(nPlayer, x, y, pChip, this.ctHandHold.CurrentValue, hiddenMode: hiddenMode);
							NotesManager.DisplayNote(nPlayer, x, y, pChip, pxFaceTxOffset, hiddenMode: hiddenMode);
							if (!NotesManager.IsADLIB(nt))
										NotesManager.DisplaySENotes(nPlayer, x + nSenotesX, y + nSenotesY, pChip, hiddenMode);
									}
					}
				}
			}
		}
		#endregion
	}
	protected override void tProgressDraw_Chip_TaikoRoll(CConfigIni configIni, ref CTja tja, ref CChip pChip, int nPlayer, long nowTime, NotesManager.ENoteType nt, EGameType _gt) {
		// 2016.11.2 kairera0467
		// 黄連打音符を赤くするやつの実装方法メモ
		//前面を黄色、背面を変色後にしたものを重ねて、打数に応じて前面の透明度を操作すれば、色を操作できるはず。
		//ただしテクスチャのαチャンネル部分が太くなるなどのデメリットが出る。備えよう。

		#region[ 作り直したもの ]
		if (pChip.bVisible) {
			bool pHasBar = (NotesManager.IsRoll(nt) || NotesManager.IsFuzeRoll(nt));

			int x = GetNoteOriginX(nPlayer) + pChip.nHorizontalChipDistance;
			int y = GetNoteOriginY(nPlayer) + pChip.nVerticalChipDistance;
			int xEnd = GetNoteOriginX(nPlayer) + pChip.end.nHorizontalChipDistance;
			int yEnd = GetNoteOriginY(nPlayer) + pChip.end.nVerticalChipDistance;

			if (NotesManager.IsGenericBalloon(nt)) {
				if (nowTime >= pChip.nSoundTimems && nowTime < pChip.end.nSoundTimems) {
					x = GetNoteOriginX(nPlayer);
					y = GetNoteOriginY(nPlayer);
				} else if (nowTime >= pChip.end.nSoundTimems) {
					x = xEnd;
					y = yEnd;
				}
			}

			if (bSplitLane[nPlayer] || OpenTaiko.Tx.Puchichara[PuchiChara.tGetPuchiCharaIndexByName(nPlayer)].effect.SplitLane) {
				if (NotesManager.IsAcceptRed(nt, _gt) && !NotesManager.IsAcceptBlue(nt, _gt) && !NotesManager.IsGenericBalloon(nt)) {
					y -= NotesManager.PxSplitLaneDistance;
					yEnd -= NotesManager.PxSplitLaneDistance;
				} else if (NotesManager.IsAcceptBlue(nt, _gt) && !NotesManager.IsAcceptRed(nt, _gt) && !NotesManager.IsGenericBalloon(nt)) {
					y += NotesManager.PxSplitLaneDistance;
					yEnd += NotesManager.PxSplitLaneDistance;
				}
			}

			bool isBodyXInScreen = (Math.Min(x, xEnd) < OpenTaiko.Skin.Resolution[0] && Math.Max(x, xEnd) > 0 - OpenTaiko.Skin.Game_Notes_Size[0]);
			if (pHasBar) {
				this.HideObscuringRoll(nPlayer, pChip, x, y, xEnd, yEnd, isBodyXInScreen, nowTime);
			}

			#region[ HIDSUD & STEALTH ]
			EStealthMode hiddenMode = OpenTaiko.ConfigIni.eSTEALTH[nPlayer];
			if (hiddenMode < EStealthMode.Stealth && !(pChip.bShow && pChip.bShowSudden))
				hiddenMode = EStealthMode.Stealth;
			if (hiddenMode < EStealthMode.Doron && this.bCustomDoron[nPlayer])
				hiddenMode = EStealthMode.Doron;
			#endregion

			if (isBodyXInScreen) {
				if (OpenTaiko.Tx.Notes[(int)_gt] != null) {
					int pxFaceTxOffset = this.GetPxFaceTextureOffset(nPlayer);

					//136, 30
					var _size = OpenTaiko.Skin.Game_SENote_Size;
					int _60_cut = 60 * _size[0] / 136;
					int _58_cut = 58 * _size[0] / 136;
					int _78_cut = 78 * _size[0] / 136;

					var (nSenotesX, nSenotesY) = NotesManager.GetSENotesPos(nPlayer);

					if (NotesManager.IsRoll(nt) || NotesManager.IsFuzeRoll(nt)) {
						if (NotesManager.IsRoll(nt)) {
					//kairera0467氏 の TJAPlayer2forPC のコードを参考にし、打数に応じて色を変える(打数の変更以外はほとんどそのまんま) ろみゅ～？ 2018/8/20
					pChip.RollInputTime?.Tick();
					pChip.RollDelay?.Tick();

					if (pChip.RollInputTime != null && pChip.RollInputTime.IsEnded) {
						pChip.RollInputTime.Stop();
						pChip.RollInputTime.CurrentValue = 0;
						pChip.RollDelay = new CCounter(-pChip.RollEffectLevel, 0, 1000 / 60.0, OpenTaiko.Timer);
					} else if (pChip.RollDelay != null && pChip.RollDelay.IsTicked && pChip.RollEffectLevel > 0) {
						pChip.RollEffectLevel = -pChip.RollDelay.CurrentValue;
					}
						}
					float fDecreaseColor = 1.0f - ((0.95f / 100) * pChip.RollEffectLevel);
					var effectedColor = new Color4(1.0f, fDecreaseColor, fDecreaseColor, 1f);
					var normalColor = new Color4(1.0f, 1.0f, 1.0f, 1f);

						NotesManager.DisplayNoteArm(nPlayer, x, y, pChip, this.ctHandHold.CurrentValue, hiddenMode: hiddenMode);
						NotesManager.DisplayRoll(nPlayer, x, y, pChip, pxFaceTxOffset, normalColor, effectedColor, xEnd, yEnd, hiddenMode);

						if (hiddenMode < EStealthMode.Stealth && OpenTaiko.Tx.SENotes[(int)_gt] != null) {
							if (!NotesManager.IsFuzeRoll(nt)) {
								int _shift = NotesManager.IsBigRollTaiko(nt, _gt) ? 26 : 0;
								int senote = pChip.nSenote;
								if (senote == 0xA && _gt is EGameType.Konga) // DRUMROLL
									senote = 7; // drumroll

								if (pChip.bShowRoll) {
									OpenTaiko.Tx.SENotes[(int)_gt].vcScaleRatio.X = xEnd - x - 44 - _shift;
									OpenTaiko.Tx.SENotes[(int)_gt].t2DDraw(x + 90 + _shift, y + nSenotesY, new Rectangle(_60_cut, 8 * _size[1], 1, _size[1]));
									OpenTaiko.Tx.SENotes[(int)_gt].vcScaleRatio.X = 1.0f;
									OpenTaiko.Tx.SENotes[(int)_gt].t2DDraw(x + 30 + _shift, y + nSenotesY, new Rectangle(0, 8 * _size[1], _60_cut, _size[1]));
								}
								OpenTaiko.Tx.SENotes[(int)_gt].t2DDraw(x - (_shift / 13), y + nSenotesY, new Rectangle(0, _size[1] * senote, _size[0], _size[1]));
							} else {
								NotesManager.DisplaySENotes(nPlayer, x + nSenotesX, y + nSenotesY, pChip);
							}

						}

					} else if (NotesManager.IsBalloon(nt) || NotesManager.IsKusudama(nt)) {
						NotesManager.DisplayNoteArm(nPlayer, x, y, pChip, this.ctHandHold.CurrentValue, hiddenMode: hiddenMode);
						NotesManager.DisplayNote(nPlayer, x, y, pChip, pxFaceTxOffset, OpenTaiko.Skin.Game_Notes_Size[0] * 2, hiddenMode);
						NotesManager.DisplaySENotes(nPlayer, x + nSenotesX, y + nSenotesY, pChip, hiddenMode);
					} else if (hiddenMode < EStealthMode.Stealth && NotesManager.IsRollEnd(nt)) {
						//大きい連打か小さい連打かの区別方法を考えてなかったよちくしょう
						if (OpenTaiko.Tx.Notes[(int)_gt] != null)
							OpenTaiko.Tx.Notes[(int)_gt].vcScaleRatio.X = 1.0f;
						if (!NotesManager.IsGenericBalloon(pChip.start)) {
							OpenTaiko.Tx.SENotes[(int)_gt]?.t2DDraw(x + 56, y + nSenotesY, new Rectangle(_58_cut, 9 * _size[1], _78_cut, _size[1]));
						}

					}
				}
			}
		}
		#endregion
	}

	public int GetPxFaceTextureOffset(int nPlayer) {
		int pxFaceTxOffset = 0;
		if (OpenTaiko.Skin.Game_Notes_Anime && !OpenTaiko.ConfigIni.SimpleMode) {
			if (this.actCombo.nCurrentCombo[nPlayer] >= 300) {
				if ((int)ctChipAnime[nPlayer].CurrentValue % 2 == 1) {
					pxFaceTxOffset = ctChipAnimeLag[nPlayer].IsEnded ? (OpenTaiko.Skin.Game_Notes_Size[1] * 2) : OpenTaiko.Skin.Game_Notes_Size[1];
				} else {
					pxFaceTxOffset = 0;
				}
			} else if (this.actCombo.nCurrentCombo[nPlayer] >= 150) {
				if ((int)ctChipAnime[nPlayer].CurrentValue % 2 == 1) {
					pxFaceTxOffset = OpenTaiko.Skin.Game_Notes_Size[1];
				} else {
					pxFaceTxOffset = 0;
				}
			} else if (this.actCombo.nCurrentCombo[nPlayer] >= 50) {
				if ((int)ctChipAnime[nPlayer].CurrentValue <= 1) {
					pxFaceTxOffset = ctChipAnimeLag[nPlayer].IsEnded ? OpenTaiko.Skin.Game_Notes_Size[1] : 0;
				} else {
					pxFaceTxOffset = 0;
				}
			} else {
				pxFaceTxOffset = 0;
			}
		}

		return pxFaceTxOffset;
	}

	/// Detect and hide screen-obscuring rolls when any tips are out of screen
	private void HideObscuringRoll(int iPlayer, CChip pChip, int xHead, int yHead, int xEnd, int yEnd, bool isBodyXInScreen, long nowTime) {
		// display judging rolls
		if (nowTime >= pChip.nSoundTimems && nowTime <= pChip.end.nSoundTimems) {
			pChip.bShowRoll = true;
			return;
		}

		// ignore already out-of-screen rolls
		bool isBodyYInScreen = (Math.Min(yHead, yEnd) < OpenTaiko.Skin.Resolution[1] && Math.Max(yHead, yEnd) > 0 - OpenTaiko.Skin.Game_Notes_Size[1]);
		if (!(isBodyXInScreen && isBodyYInScreen)) {
			return;
		}

		// display completely in-screen rolls
		bool headInScreen = (xHead > 0 - OpenTaiko.Skin.Game_Notes_Size[0] && xHead < OpenTaiko.Skin.Resolution[0])
			&& (yHead > 0 - OpenTaiko.Skin.Game_Notes_Size[1] && yHead < OpenTaiko.Skin.Resolution[1]);
		bool endInScreen = (xEnd > 0 - OpenTaiko.Skin.Game_Notes_Size[0] && xEnd < OpenTaiko.Skin.Resolution[0])
			&& (yEnd > 0 - OpenTaiko.Skin.Game_Notes_Size[1] && yEnd < OpenTaiko.Skin.Resolution[1]);
		if (headInScreen && endInScreen) {
			pChip.bShowRoll = true;
			return;
		}

		// displacement per sec
		double th16DBeat = -4 * pChip.dbBPM / 60;
		int dxHead = (int)NotesManager.GetNoteX(-1000, th16DBeat, pChip.dbBPM, pChip.dbSCROLL, pChip.eScrollMode);
		int dyHead = (int)NotesManager.GetNoteY(-1000, th16DBeat, pChip.dbBPM, pChip.dbSCROLL_Y, pChip.eScrollMode);
		int dxEnd = (int)NotesManager.GetNoteX(-1000, th16DBeat, pChip.end.dbBPM, pChip.end.dbSCROLL, pChip.end.eScrollMode);
		int dyEnd = (int)NotesManager.GetNoteY(-1000, th16DBeat, pChip.end.dbBPM, pChip.end.dbSCROLL_Y, pChip.end.eScrollMode);

		// get move speed near the judgement mark

		var head = new Vector2(xHead, yHead);
		var end = new Vector2(xEnd, yEnd);
		var origin = new Vector2(this.GetNoteOriginX(iPlayer), this.GetNoteOriginY(iPlayer));
		float pos = NearestLineSegRelPos(head, end, origin);

		Vector2 dr = Vector2.Lerp(new(dxHead, dyHead), new(dxEnd, dyEnd), pos);
		Vector2 rollNorm = Vector2.Normalize(new(yEnd - yHead, -(xEnd - xHead)));

		int drCanMoveAwayMin = (OpenTaiko.Skin.Game_Notes_Size.Max() + 1) / 2;
		// If the nearest point is roll tip, all moves may prevent obscuring.
		// If the nearest point is roll body, only orthogonal moves may prevent obscuring.
		float drAway = (pos > 0 && pos < 1) ? Math.Abs(Vector2.Dot(dr, rollNorm)) : dr.Length();
		bool canMoveAway = drAway >= drCanMoveAwayMin;
		pChip.bShowRoll = canMoveAway;
	}

	private static float NearestLineSegRelPos(Vector2 head, Vector2 end, Vector2 target) {
		Vector2 body = end - head;
		float len = body.Length();
		Vector2 bodyUnit = Vector2.Normalize(body);

		Vector2 dHead = target - head;
		float dHeadProj = Vector2.Dot(dHead, bodyUnit);
		return Math.Clamp(dHeadProj, 0f, len) / len;
	}

	protected override void tProgressDraw_Chip_Drums(CConfigIni configIni, ref CTja dTX, ref CChip pChip, long nowTime) {
	}
	protected override void tProgressDraw_ChipBody_Drums(CConfigIni configIni, ref CTja dTX, ref CChip pChip, long nowTime) {
	}
	protected override void tProgressDraw_Chip_FillIn(CConfigIni configIni, ref CTja dTX, ref CChip pChip, long nowTime) {

	}
	protected override void tProgressDraw_Chip_MeasureLine(CConfigIni configIni, ref CTja tja, ref CChip pChip, int nPlayer, long nowTime) {
		//int n小節番号plus1 = pChip.n発声位置 / 384;
		//int n小節番号plus1 = this.actPlayInfo.NowMeasure[nPlayer];
		int x = GetNoteOriginX(nPlayer) + pChip.nHorizontalChipDistance;
		int y = GetNoteOriginY(nPlayer) + pChip.nVerticalChipDistance;

		if ((pChip.bVisible && !pChip.bHideBarLine) && (OpenTaiko.Tx.Bar != null)) {
			var width = OpenTaiko.Tx.Bar.szTextureSize.Width;
			var height = OpenTaiko.Skin.Game_Notes_Size[1];
			var maxRadius = width + height; // upper limit of Math.Hypot(width, height) and a close approximant because width is small
			if (x >= -maxRadius / 2 && x <= GameWindowSize.Width + maxRadius / 2) {
				double theta = (pChip.dbSCROLL_Y == 0.0) ? 0 : -Math.Atan2(pChip.nVerticalChipDistance, pChip.nHorizontalChipDistance);
				CTexture tex = (pChip.bBranch) ? OpenTaiko.Tx.Bar_Branch : OpenTaiko.Tx.Bar;
				tex.fZAxisCenterRotate = (float)theta;
				tex.t2DDraw(x + ((OpenTaiko.Skin.Game_Notes_Size[0] - tex.szTextureSize.Width) / 2), y, new Rectangle(0, 0, tex.szTextureSize.Width, OpenTaiko.Skin.Game_Notes_Size[1]));
				tex.fZAxisCenterRotate = 0;
			}
		}
	}

	/// <summary>
	/// 全体にわたる制御をする。
	/// </summary>
	public void tTotalControlMethod() {
		int t = (int)SoundManager.PlayTimer.NowTimeMs;
		//CDTXMania.act文字コンソール.tPrint( 0, 16, C文字コンソール.Eフォント種別.白, t.ToString() );

		this.actBalloon.tDrawKusudama(this.actTokkun.bTrainingPAUSE);

		for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
			CTja tja = OpenTaiko.GetTJA(i)!;
			double msBarRollProgress = 0;
			// Chart time including the Dynamic Beat warp — the raw clock diverges from chip times once the
			// warp factor moves off 1.0, which made this window miss and hid the roll/kusudama/fuse numbers.
			long nowTime = this.GetChartTimeNow(i);
			for (int iChip = this.chipCurrentProcessingRollChip[i].Count; iChip-- > 0;) {
				var chkChip = this.chipCurrentProcessingRollChip[i][iChip];
				if (!chkChip.bVisible)
					continue;
				//int n = this.chip現在処理中の連打チップ[i].nチャンネル番号;
				if (!this.bPAUSE && !this.isRewinding && !chkChip.bProcessed) {
					this.ProcessRollHeadEffects(i, chkChip);
				}
				if (!NotesManager.IsGenericBalloon(chkChip)) {
					if (chkChip.end.bVisible && chkChip.end.nSoundTimems >= (int)nowTime)
						msBarRollProgress += (int)nowTime - chkChip.nSoundTimems;
					continue;
				}
				if (!(chkChip.nRollCount > 0 || NotesManager.IsKusudama(chkChip))) {
					continue;
				}
				//if (this.chip現在処理中の連打チップ.n発声時刻ms <= (int)CSound管理.rc演奏用タイマ.n現在時刻ms && this.chip現在処理中の連打チップ.nノーツ終了時刻ms >= (int)CSound管理.rc演奏用タイマ.n現在時刻ms)
				if (chkChip.nSoundTimems <= (int)nowTime
					&& chkChip.end.nSoundTimems + 500 >= (int)nowTime
					) {
					var balloon = NotesManager.IsKusudama(chkChip) ? chkChip.KusudamaCount : chkChip.nBalloon;
					var rollCount = NotesManager.IsKusudama(chkChip) ? chkChip.KusudamaRollCount : chkChip.nRollCount;
					if (!this.bPAUSE && !this.isRewinding && !NotesManager.IsFuzeRoll(chkChip))
						chkChip.bShow = false;
					this.actBalloon.OnProgressDraw(balloon, balloon - rollCount, i, chkChip, this.actTokkun.bTrainingPAUSE);
				}
			}
			if (msBarRollProgress != this.msCurrentBarRollProgress[i]) {
				this.msCurrentBarRollProgress[i] = msBarRollProgress;
				if (i == 0)
					this.actDan.Update();
				if (this.IsChartEnded())
					this.UpdateClearAnimation(i);
			}
		}

		if (!this.actTokkun.bTrainingPAUSE)
			this.tCheckBgmDrift();

		#region [ Branch guide for P1 ]
		//現在実験状態です。
		//画像などが完成したらメソッドorクラスとして分離します。

		if (OpenTaiko.ConfigIni.bBranchGuide && !OpenTaiko.ConfigIni.bAutoPlay[0]) {
			string strNext = "BRANCH END";
			CTja tjaP1 = OpenTaiko.TJA!;
			int dy = OpenTaiko.actTextConsole.fontHeight;
			int y = (int)(176 * OpenTaiko.Skin.ScaleY - 3 * dy);

			if (!(this.idxLastBranchSection[0] < tjaP1.listBRANCH.Count)) {
				y += dy;
				OpenTaiko.actTextConsole.Print(0, y, CTextConsole.EFontType.White, strNext);
			} else {
				CChip branchJudgePoint = tjaP1.listBRANCH[this.idxLastBranchSection[0]];

				var branchCond = branchJudgePoint.eBranchCondition;
				double nowBranchCondScore = this.GetBranchConditionScore(0, branchCond);
				OpenTaiko.actTextConsole.Print(0, y, CTextConsole.EFontType.White, nowBranchCondScore.ToString("##0.##"));

				var nowTargetBranch = this.tBranchJudge(0, branchJudgePoint);
				strNext = nowTargetBranch switch {
					CTja.ECourse.eMaster => "MASTER",
					CTja.ECourse.eExpert => "EXPERT",
					CTja.ECourse.eNormal or _ => "NORMAL",
				};
				var (x, _) = OpenTaiko.actTextConsole.Print(0, y += dy, CTextConsole.EFontType.White, strNext);
				if (this.bLEVELHOLD[0])
					(x, _) = OpenTaiko.actTextConsole.Print(x, y, 0, CTextConsole.EFontType.White, "(LEVELHELD)");
				if (this.bForcedBranch[0])
					(x, _) = OpenTaiko.actTextConsole.Print(x, y, 0, CTextConsole.EFontType.White, "(FORCED)");

				int nMeasuresToNextBranch = branchJudgePoint.nSoundPos / 384 - (this.actPlayInfo.NowMeasure[0] + 1); // round down
				OpenTaiko.actTextConsole.Print(0, y += dy, CTextConsole.EFontType.White, $"NEXT BRANCH:{nMeasuresToNextBranch,3} BARS");

				y = (int)(362 * OpenTaiko.Skin.ScaleY);
				if (branchCond.type == Exam.Type.None) {
					OpenTaiko.actTextConsole.Print(0, y, CTextConsole.EFontType.White, "NEXT BRANCH INFO:(KEEP)");
				} else {
					OpenTaiko.actTextConsole.Print(0, y, CTextConsole.EFontType.White,
						string.Create(CultureInfo.InvariantCulture,
							$"NEXT BRANCH INFO:{CTja.EnumToTjaString(branchCond.type, branchCond.big)},{branchJudgePoint.nBranchCondition1_Professional},{branchJudgePoint.nBranchCondition2_Master}"));
				}
			}
		}
		#endregion

		#region [ Big notes waiting time out ]
		//常時イベントが発生しているメソッドのほうがいいんじゃないかという予想。
		//CDTX.CChip chipNoHit = this.r指定時刻に一番近い未ヒットChip((int)CSound管理.rc演奏用タイマ.n現在時刻ms, 0);
		for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
			CTja tja = OpenTaiko.GetTJA(i)!;
			var timeNow = tja.GameTimeToTjaTime(SoundManager.PlayTimer.NowTimeMs);
			for (int iChip = 0; iChip < this.chipNowProcessingMultiHitNotes[i].Count; ++iChip) {
				this.MultiHitNoteTimeout(i, this.chipNowProcessingMultiHitNotes[i][iChip], timeNow, msMaxPlayedTjaTime: this.msMaxPlayedTjaTime(i));
			}
			this.chipNowProcessingMultiHitNotes[i].RemoveAll(chip => chip.eNoteState != ENoteState.Wait);
		}

		#endregion

		//string strNull = "Found";
	}

	protected override void MultiHitNoteTimeout(int iPlayer, CChip chip, double msTjaNowTime, double msMaxPlayedTjaTime = double.PositiveInfinity) {
		EGameType _gt = NotesManager.GetChipGameType(chip, iPlayer);
		bool _isSwapNote = NotesManager.IsSwapNote(chip, _gt);

		int msMaxWaitTime = OpenTaiko.ConfigIni.nBigNoteWaitTimems;
		var msJudgeTjaTime = Math.Min(msTjaNowTime, msMaxPlayedTjaTime);
		var msWaitedTime = msJudgeTjaTime - (float)chip.msFirstMultiHit;
		if (chip.eNoteState == ENoteState.Wait && msWaitedTime >= msMaxWaitTime) {
			if (!_isSwapNote) {
				this.tDrumsHitProcess((long)chip.msFirstMultiHit, EPad.Unknown, chip, false, iPlayer);
				chip.padStoredHit = EPad.Unknown;
				chip.bHit = true;
				chip.IsHitted = true;
			}
			chip.eNoteState = ENoteState.None;
		}
	}

	public void ChangeBranch(CTja.ECourse nAfter, int iPlayer, double msBranchPoint = double.MaxValue, bool stopAnime = false) {
		this.nTargetBranch[iPlayer] = nAfter;
		this.msTargetBranchTime[iPlayer] = msBranchPoint;
		if (stopAnime)
			this.nCurrentBranch[iPlayer] = nAfter;
		var shownBranch = this.bUseBranch[iPlayer] ? nAfter : CTja.ECourse.eNormal;
		this.actMtaiko.tBranchEvent(shownBranch, iPlayer, stopAnime: stopAnime);
		this.actLaneTaiko.ChangeBranch(shownBranch, iPlayer, stopAnime: stopAnime);
	}

	private void tProgressDraw_RealTimeJudgeCountDisplay() {
		var showJudgeInfo = false;

		if (OpenTaiko.ConfigIni.nPlayerCount == 1 ? (OpenTaiko.ConfigIni.bJudgeCountDisplay && !OpenTaiko.ConfigIni.bAutoPlay[0]) : false) showJudgeInfo = true;
		if (OpenTaiko.ConfigIni.bTokkunMode) showJudgeInfo = true;

		if (showJudgeInfo) {
			//ボードの横幅は333px
			//数字フォントの小さいほうはリザルトのものと同じ。
			if (OpenTaiko.Tx.Judge_Meter != null)
				OpenTaiko.Tx.Judge_Meter.t2DDraw(OpenTaiko.Skin.Game_Judge_Meter[0], OpenTaiko.Skin.Game_Judge_Meter[1]);

			this.tSmallDisplay(OpenTaiko.Skin.Game_Judge_Meter_Perfect[0], OpenTaiko.Skin.Game_Judge_Meter_Perfect[1], this.nHitCount_ExclAuto.Perfect, false, false);
			this.tSmallDisplay(OpenTaiko.Skin.Game_Judge_Meter_Good[0], OpenTaiko.Skin.Game_Judge_Meter_Good[1], this.nHitCount_ExclAuto.Great, false, false);
			this.tSmallDisplay(OpenTaiko.Skin.Game_Judge_Meter_Miss[0], OpenTaiko.Skin.Game_Judge_Meter_Miss[1], this.nHitCount_ExclAuto.Miss, false, false);
			this.tSmallDisplay(OpenTaiko.Skin.Game_Judge_Meter_Roll[0], OpenTaiko.Skin.Game_Judge_Meter_Roll[1], GetRoll(0), false, false);

			int nNowTotal = this.nHitCount_ExclAuto.Perfect + this.nHitCount_ExclAuto.Great + this.nHitCount_ExclAuto.Miss;
			double dbHitRate = Math.Round((100.0 * (OpenTaiko.stageGameScreen.nHitCount_ExclAuto.Perfect + OpenTaiko.stageGameScreen.nHitCount_ExclAuto.Great)) / (double)nNowTotal);
			double dbPERFECTRate = Math.Round((100.0 * OpenTaiko.stageGameScreen.nHitCount_ExclAuto.Perfect) / (double)nNowTotal);
			double dbGREATRate = Math.Round((100.0 * OpenTaiko.stageGameScreen.nHitCount_ExclAuto.Great / (double)nNowTotal));
			double dbMISSRate = Math.Round((100.0 * OpenTaiko.stageGameScreen.nHitCount_ExclAuto.Miss / (double)nNowTotal));

			if (double.IsNaN(dbHitRate))
				dbHitRate = 0;
			if (double.IsNaN(dbPERFECTRate))
				dbPERFECTRate = 0;
			if (double.IsNaN(dbGREATRate))
				dbGREATRate = 0;
			if (double.IsNaN(dbMISSRate))
				dbMISSRate = 0;

			this.tLargeDisplay(OpenTaiko.Skin.Game_Judge_Meter_HitRate[0], OpenTaiko.Skin.Game_Judge_Meter_HitRate[1], (int)dbHitRate);
			this.tSmallDisplay(OpenTaiko.Skin.Game_Judge_Meter_PerfectRate[0], OpenTaiko.Skin.Game_Judge_Meter_PerfectRate[1], (int)dbPERFECTRate, false, true);
			this.tSmallDisplay(OpenTaiko.Skin.Game_Judge_Meter_GoodRate[0], OpenTaiko.Skin.Game_Judge_Meter_GoodRate[1], (int)dbGREATRate, false, true);
			this.tSmallDisplay(OpenTaiko.Skin.Game_Judge_Meter_MissRate[0], OpenTaiko.Skin.Game_Judge_Meter_MissRate[1], (int)dbMISSRate, false, true);
		}
	}

	private void tSmallDisplay(int x, int y, int num, bool bOrange, bool drawPercent) {
		float width = OpenTaiko.Tx.Result_Number.szImageSize.Width / 11.0f;
		float height = OpenTaiko.Tx.Result_Number.szImageSize.Height / 2.0f;

		int[] nums = CConversion.SeparateDigits(num);

		if (drawPercent) {
			OpenTaiko.Tx.Result_Number.t2DScaledCenterBasedDraw(x + (OpenTaiko.Skin.Result_Number_Interval[0] * 3.0f) + (width / 2),
				y + (OpenTaiko.Skin.Result_Number_Interval[1] * 3.0f) + (height / 2),
				new System.Drawing.RectangleF(width * 10, 0, width, height));
		}

		for (int j = 0; j < nums.Length; j++) {
			float offset = j - 1.5f;
			float _x = x - (OpenTaiko.Skin.Result_Number_Interval[0] * offset);
			float _y = y - (OpenTaiko.Skin.Result_Number_Interval[1] * offset);

			OpenTaiko.Tx.Result_Number.t2DScaledCenterBasedDraw(_x + (width / 2), _y + (height / 2),
				new System.Drawing.RectangleF(width * nums[j], 0, width, height));
		}
	}

	private void tLargeDisplay(int x, int y, int num) {
		int[] nums = CConversion.SeparateDigits(num);
		for (int j = 0; j < nums.Length; j++) {
			float offset = j - 1.5f;
			float _x = x - ((OpenTaiko.Skin.Result_Number_Interval[0] * 1.27f) * offset);
			float _y = y - ((OpenTaiko.Skin.Result_Number_Interval[1] * 1.27f) * offset);

			float width = OpenTaiko.Tx.Result_Number.szImageSize.Width / 11.0f;
			float height = OpenTaiko.Tx.Result_Number.szImageSize.Height / 2.0f;

			OpenTaiko.Tx.Result_Number.t2DScaledCenterBasedDraw(_x + (width / 2), _y + (height / 2),
				new System.Drawing.RectangleF(width * nums[j], height, width, height));
		}
	}
	#endregion
}
