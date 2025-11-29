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

internal class CStage演奏ドラム画面 : CStage演奏画面共通 {
	// コンストラクタ

	public CStage演奏ドラム画面() {
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
		base.ChildActivities.Add(this.actAVI = new CAct演奏AVI());
		base.ChildActivities.Add(this.actPanel = new CAct演奏パネル文字列());
		base.ChildActivities.Add(this.actStageFailed = new CAct演奏ステージ失敗());
		base.ChildActivities.Add(this.actPlayInfo = new CAct演奏演奏情報());
		//base.list子Activities.Add( this.actFI = new CActFIFOBlack() );
		base.ChildActivities.Add(this.actFI = new CActFIFOStart());
		base.ChildActivities.Add(this.actFO = new CActFIFOBlack());
		base.ChildActivities.Add(this.actFOClear = new CActFIFOResult());
		base.ChildActivities.Add(this.actEnd = new CActImplClearAnimation());
		base.ChildActivities.Add(this.actDancer = new CActImplDancer());
		base.ChildActivities.Add(this.actMtaiko = new CActImplMtaiko());
		base.ChildActivities.Add(this.actLaneTaiko = new CActImplLaneTaiko());
		base.ChildActivities.Add(this.actRoll = new CActImplRoll());
		base.ChildActivities.Add(this.actBalloon = new CActImplBalloon());
		base.ChildActivities.Add(this.actChara = new CActImplCharacter());
		base.ChildActivities.Add(this.actGame = new CAct演奏Drumsゲームモード());
		base.ChildActivities.Add(this.actBackground = new CActImplBackground());
		base.ChildActivities.Add(this.actRollChara = new CActImplRollEffect());
		base.ChildActivities.Add(this.actComboBalloon = new CActImplComboBalloon());
		base.ChildActivities.Add(this.actComboVoice = new CAct演奏Combo音声());
		base.ChildActivities.Add(this.actPauseMenu = new CAct演奏PauseMenu());
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
		ST文字位置[] st文字位置Array = new ST文字位置[12];
		ST文字位置 st文字位置 = new ST文字位置();
		st文字位置.ch = '0';
		st文字位置.pt = new Point(0, 0);
		st文字位置Array[0] = st文字位置;
		ST文字位置 st文字位置2 = new ST文字位置();
		st文字位置2.ch = '1';
		st文字位置2.pt = new Point(32, 0);
		st文字位置Array[1] = st文字位置2;
		ST文字位置 st文字位置3 = new ST文字位置();
		st文字位置3.ch = '2';
		st文字位置3.pt = new Point(64, 0);
		st文字位置Array[2] = st文字位置3;
		ST文字位置 st文字位置4 = new ST文字位置();
		st文字位置4.ch = '3';
		st文字位置4.pt = new Point(96, 0);
		st文字位置Array[3] = st文字位置4;
		ST文字位置 st文字位置5 = new ST文字位置();
		st文字位置5.ch = '4';
		st文字位置5.pt = new Point(128, 0);
		st文字位置Array[4] = st文字位置5;
		ST文字位置 st文字位置6 = new ST文字位置();
		st文字位置6.ch = '5';
		st文字位置6.pt = new Point(160, 0);
		st文字位置Array[5] = st文字位置6;
		ST文字位置 st文字位置7 = new ST文字位置();
		st文字位置7.ch = '6';
		st文字位置7.pt = new Point(192, 0);
		st文字位置Array[6] = st文字位置7;
		ST文字位置 st文字位置8 = new ST文字位置();
		st文字位置8.ch = '7';
		st文字位置8.pt = new Point(224, 0);
		st文字位置Array[7] = st文字位置8;
		ST文字位置 st文字位置9 = new ST文字位置();
		st文字位置9.ch = '8';
		st文字位置9.pt = new Point(256, 0);
		st文字位置Array[8] = st文字位置9;
		ST文字位置 st文字位置10 = new ST文字位置();
		st文字位置10.ch = '9';
		st文字位置10.pt = new Point(288, 0);
		st文字位置Array[9] = st文字位置10;
		ST文字位置 st文字位置11 = new ST文字位置();
		st文字位置11.ch = '%';
		st文字位置11.pt = new Point(320, 0);
		st文字位置Array[10] = st文字位置11;
		ST文字位置 st文字位置12 = new ST文字位置();
		st文字位置12.ch = ' ';
		st文字位置12.pt = new Point(0, 0);
		st文字位置Array[11] = st文字位置12;
		this.st小文字位置 = st文字位置Array;

		st文字位置Array = new ST文字位置[12];
		st文字位置 = new ST文字位置();
		st文字位置.ch = '0';
		st文字位置.pt = new Point(0, 0);
		st文字位置Array[0] = st文字位置;
		st文字位置2 = new ST文字位置();
		st文字位置2.ch = '1';
		st文字位置2.pt = new Point(32, 0);
		st文字位置Array[1] = st文字位置2;
		st文字位置3 = new ST文字位置();
		st文字位置3.ch = '2';
		st文字位置3.pt = new Point(64, 0);
		st文字位置Array[2] = st文字位置3;
		st文字位置4 = new ST文字位置();
		st文字位置4.ch = '3';
		st文字位置4.pt = new Point(96, 0);
		st文字位置Array[3] = st文字位置4;
		st文字位置5 = new ST文字位置();
		st文字位置5.ch = '4';
		st文字位置5.pt = new Point(128, 0);
		st文字位置Array[4] = st文字位置5;
		st文字位置6 = new ST文字位置();
		st文字位置6.ch = '5';
		st文字位置6.pt = new Point(160, 0);
		st文字位置Array[5] = st文字位置6;
		st文字位置7 = new ST文字位置();
		st文字位置7.ch = '6';
		st文字位置7.pt = new Point(192, 0);
		st文字位置Array[6] = st文字位置7;
		st文字位置8 = new ST文字位置();
		st文字位置8.ch = '7';
		st文字位置8.pt = new Point(224, 0);
		st文字位置Array[7] = st文字位置8;
		st文字位置9 = new ST文字位置();
		st文字位置9.ch = '8';
		st文字位置9.pt = new Point(256, 0);
		st文字位置Array[8] = st文字位置9;
		st文字位置10 = new ST文字位置();
		st文字位置10.ch = '9';
		st文字位置10.pt = new Point(288, 0);
		st文字位置Array[9] = st文字位置10;
		st文字位置11 = new ST文字位置();
		st文字位置11.ch = '%';
		st文字位置11.pt = new Point(320, 0);
		st文字位置Array[10] = st文字位置11;
		st文字位置12 = new ST文字位置();
		st文字位置12.ch = ' ';
		st文字位置12.pt = new Point(0, 0);
		st文字位置Array[11] = st文字位置12;
		this.st小文字位置 = st文字位置Array;
		#endregion
	}


	// メソッド

	public void t演奏結果を格納する(out CScoreIni.C演奏記録 Drums) {
		base.t演奏結果を格納する_ドラム(out Drums);
	}


	// CStage 実装

	public override void Activate() {
		LoudnessMetadataScanner.StopBackgroundScanning(joinImmediately: false);

		base.Activate();

		this.ct手つなぎ = new CCounter(0, 60, 20, OpenTaiko.Timer);

		// When performing calibration, reduce audio distraction from user input.
		// For users who play primarily by listening to the music,
		// you might think that we want them to hear drum sound effects during
		// calibration, but we do not. Humans are remarkably good at adjusting
		// the timing of their own physical movement, even without realizing it.
		// We are calibrating their input timing for the purposes of judgment.
		// We do not want them subconsciously playing early so as to line up
		// their drum sound effects with the sounds of the input calibration file.
		// Instead, we want them focused on the sounds of their keyboard, tatacon,
		// other controller, etc. and the sounds of the input calibration audio file.
		for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
			int actual = OpenTaiko.GetActualPlayer(i);

			var hs = OpenTaiko.Skin.hsHitSoundsInformations;

			this.soundRed[i] = OpenTaiko.SoundManager.tCreateSound(hs.don[actual], ESoundGroup.SoundEffect);
			this.soundBlue[i] = OpenTaiko.SoundManager.tCreateSound(hs.ka[actual], ESoundGroup.SoundEffect);
			this.soundAdlib[i] = OpenTaiko.SoundManager.tCreateSound(hs.adlib[actual], ESoundGroup.SoundEffect);
			this.soundClap[i] = OpenTaiko.SoundManager.tCreateSound(hs.clap[actual], ESoundGroup.SoundEffect);

			int _panning = OpenTaiko.ConfigIni.nPanning[OpenTaiko.ConfigIni.nPlayerCount - 1][i];
			if (this.soundRed[i] != null) this.soundRed[i].SoundPosition = _panning;
			if (this.soundBlue[i] != null) this.soundBlue[i].SoundPosition = _panning;
			if (this.soundAdlib[i] != null) this.soundAdlib[i].SoundPosition = _panning;
			if (this.soundClap[i] != null) this.soundClap[i].SoundPosition = _panning;
		}
	}

	public override void t数値の初期化(bool b演奏記録, bool b演奏状態) {
		int iPrevTopChipMax = this.nCurrentTopChip.Max();
		base.t数値の初期化(b演奏記録, b演奏状態);

		if (b演奏状態) {
			this.actGame.t叩ききりまショー_初期化();

			for (int i = 0; i < 5; i++) {
				if (bIsAlreadyCleared[i]) {
					actBackground.ClearIn(i);
				}
			}
		}

		if (b演奏状態) {
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

				int level = OpenTaiko.stageSongSelect.rChoosenSong.nLevel[diff];
				CTja.ELevelIcon levelIcon = OpenTaiko.stageSongSelect.rChoosenSong.nLevelIcon[diff];

				return (diffArr[Math.Min(diff, 6)] + "Lv." + level + diffArrIcon[(int)levelIcon]);
			}

			// Discord Presence の更新
			string details = OpenTaiko.ConfigIni.SendDiscordPlayingInformation ? OpenTaiko.stageSongSelect.rChoosenSong.ldTitle.GetString("")
																				 + diffToString(OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0]) : "";

			// Byte count must be used instead of String.Length.
			// The byte count is what Discord is concerned with. Some chars are greater than one byte.
			if (Encoding.UTF8.GetBytes(details).Length > 128) {
				byte[] details_byte = Encoding.UTF8.GetBytes(details);
				Array.Resize(ref details_byte, 128);
				details = Encoding.UTF8.GetString(details_byte);
			}

			var difficultyName = OpenTaiko.DifficultyNumberToEnum(OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0]).ToString();

			OpenTaiko.DiscordClient?.SetPresence(new RichPresence() {
				Details = details,
				State = "Playing" + (OpenTaiko.ConfigIni.bAutoPlay[0] == true ? " (Auto)" : ""),
				Timestamps = new Timestamps(DateTime.UtcNow, DateTime.UtcNow.AddMilliseconds(OpenTaiko.TJA.TjaTimeToGameTime(OpenTaiko.TJA.listChip[OpenTaiko.TJA.listChip.Count - 1].n発声時刻ms))),
				Assets = new Assets() {
					SmallImageKey = OpenTaiko.ConfigIni.SendDiscordPlayingInformation ? difficultyName.ToLower() : "",
					SmallImageText = OpenTaiko.ConfigIni.SendDiscordPlayingInformation ? String.Format("COURSE:{0} ({1})", difficultyName, OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0]) : "",
					LargeImageKey = OpenTaiko.LargeImageKey,
					LargeImageText = OpenTaiko.LargeImageText,
				}
			});
		}

		if (!b演奏状態 && iPrevTopChipMax <= 0)
			return; // no needs to reset

		#region [reset accumulated chip state]
		this.bフィルイン中 = false;
		this.n待機中の大音符の座標 = 0;

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
		this.ct手つなぎ = null;

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
	public override int Draw() {
		base.sw.Start();
		if (!base.IsDeActivated) {
			#region [ 初めての進行描画 ]
			if (base.IsFirstDraw) {
				SoundManager.PlayTimer.Reset();
				OpenTaiko.Timer.Reset();
				this.ctチップ模様アニメ.Drums = new CCounter(0, 1, 500, OpenTaiko.Timer);

				// this.actChipFireD.Start( Eレーン.HH );	// #31554 2013.6.12 yyagi
				// 初チップヒット時のもたつき回避。最初にactChipFireD.Start()するときにJITが掛かって？
				// ものすごく待たされる(2回目以降と比べると2,3桁tick違う)。そこで最初の画面フェードインの間に
				// 一発Start()を掛けてJITの結果を生成させておく。

				base.ePhaseID = CStage.EPhase.Common_FADEIN;

				this.actFI.tフェードイン開始();
				for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; ++i) {
					this.actLaneTaiko.BranchText_FadeIn(null, i);
				}

				// TJAPlayer3.Sound管理.tDisableUpdateBufferAutomatically();
				base.IsFirstDraw = false;
			}
			#endregion
			if (((OpenTaiko.ConfigIni.nRisky != 0 && this.actGauge.IsFailed(EInstrumentPad.Taiko))
				 || this.actGame.st叩ききりまショー.ct残り時間.IsEnded
				 || (OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Tower && CFloorManagement.CurrentNumberOfLives <= 0))
				&& (base.ePhaseID == CStage.EPhase.Common_NORMAL)) {
				this.actStageFailed.Start();
				this.actEnd.Start();
				OpenTaiko.TJA.tStopAllChips();
				base.ePhaseID = CStage.EPhase.Game_STAGE_FAILED;
			}

			bool BGA_Shown = OpenTaiko.ConfigIni.bEnableAVI && OpenTaiko.TJA.listVD.Count > 0 && ShowVideo;

			// Layer: background

			if (BGA_Shown && !OpenTaiko.ConfigIni.bTokkunMode && this.t進行描画_AVI()) {
				// BGMOVIE & #BGAON
			} else if (!OpenTaiko.ConfigIni.bTokkunMode && this.t進行描画_背景()) {
				// BGIMAGE
			} else if (OpenTaiko.ConfigIni.bEnableBGA) {
				if (OpenTaiko.ConfigIni.bTokkunMode)
					actTokkun.On進行描画_背景();
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

			this.t進行描画_譜面スクロール速度();
			this.t進行描画_チップアニメ();

			// Layer: note lane + below-note foreground elements

			this.actLaneTaiko.Draw();

			if (OpenTaiko.ConfigIni.ShowRunner && !OpenTaiko.ConfigIni.bAIBattleMode && !this.isMultiPlay)
				this.actRunner.Draw();

			//this.t進行描画_レーン();
			//this.t進行描画_レーンフラッシュD();

			if (BGA_Shown && !OpenTaiko.ConfigIni.bTokkunMode && !this.isMultiPlay)
				this.actAVI.t窓表示();

			if (!OpenTaiko.ConfigIni.bNoInfo && !OpenTaiko.ConfigIni.bTokkunMode)
				this.t進行描画_ゲージ();

			this.actLaneTaiko.ゴーゴー炎();

			// bIsFinishedPlaying was dependent on 2P in this case

			this.actDan.Draw();

			// Layer: notes & bar lines
			for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
				// bIsFinishedPlaying = this.t進行描画_チップ(E楽器パート.DRUMS, i);
				bool btmp = this.t進行描画_チップ(EInstrumentPad.Drums, i);
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

			if (OpenTaiko.ConfigIni.bAIBattleMode) {
				this.actAIBattle.Draw();
			}

			this.GoGoSplash.Draw();
			this.t進行描画_リアルタイム判定数表示();
			if (OpenTaiko.ConfigIni.bTokkunMode)
				this.actTokkun.On進行描画_小節_速度();

			if (!OpenTaiko.ConfigIni.bNoInfo)
				this.t進行描画_コンボ();
			if (!OpenTaiko.ConfigIni.bNoInfo && !OpenTaiko.ConfigIni.bTokkunMode)
				this.t進行描画_スコア();

			// 3+-player mode character
			if (OpenTaiko.ConfigIni.ShowChara && OpenTaiko.ConfigIni.nPlayerCount > 2) {
				this.actChara.Draw();
			}

			// note effects
			this.Rainbow.Draw();
			this.FireWorks.Draw();
			this.actChipEffects.Draw();
			this.FlyingNotes.Draw();
			this.t進行描画_チップファイアD();

			if (!OpenTaiko.ConfigIni.bNoInfo)
				this.t進行描画_パネル文字列();

			// dialogue balloons

			this.actComboBalloon.Draw();

			for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
				this.actRoll.On進行描画(this.nCurrentRollCount[i], i);
			}

			// infos

			if (!OpenTaiko.ConfigIni.bNoInfo)
				this.t進行描画_判定文字列1_通常位置指定の場合();

			this.t進行描画_演奏情報();

			// LYRIC[S/FILE]: & #LYRIC

			if (OpenTaiko.TJA.listLyric2.Count > ShownLyric2 && OpenTaiko.TJA.listLyric2[ShownLyric2].Time < (long)OpenTaiko.TJA.GameTimeToTjaTime(SoundManager.PlayTimer.NowTimeMs)) {
				this.actPanel.t歌詞テクスチャを生成する(OpenTaiko.TJA.listLyric2[ShownLyric2++].TextTex);
			}

			this.actPanel.t歌詞テクスチャを描画する();

			actChara.OnDraw_Balloon();

			// Floor voice
			if (OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Tower)
				this.actComboVoice.tPlayFloorSound();

			this.t全体制御メソッド();

			//this.actEnd.On進行描画();
			this.t進行描画_STAGEFAILED();

			this.ScoreRank.Draw();

			// Layer: Interactive elements

			if (OpenTaiko.ConfigIni.bTokkunMode) {
				actTokkun.Draw();
			}

			// handle retry states here
			this.actPauseMenu.Draw();

			// Layer: Gameplay complete animation and fading out

			bool bIsFinishedEndAnime = this.actEnd.Draw() == 1 ? true : false;
			bool bIsFinishedFadeout = this.t進行描画_フェードイン_アウト();

			bool bIsFinishedPlaying = true;
			bool bIsChartEnded = true;
			for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
				if (!isFinishedPlaying[i]) bIsFinishedPlaying = false;
				if (!isChartEnded[i]) bIsChartEnded = false;
			}

			//演奏終了→演出表示→フェードアウト
			if ((bIsChartEnded || bIsFinishedPlaying) && base.ePhaseID == CStage.EPhase.Common_NORMAL) {
				if (!OpenTaiko.ConfigIni.bTokkunMode) {
					for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
						int Character = this.actChara.iCurrentCharacter[i];

						if (HGaugeMethods.UNSAFE_IsRainbow(i)) {
							if (OpenTaiko.Skin.Characters_10Combo_Maxed_Ptn[Character] != 0) {
								if (HGaugeMethods.UNSAFE_IsRainbow(i)) {
									this.actChara.ChangeAnime(i, CActImplCharacter.Anime.Combo10_Max, true);
								}
							}
						} else if (HGaugeMethods.UNSAFE_FastNormaCheck(i)) {
							if (OpenTaiko.Skin.Characters_Become_Cleared_Ptn[Character] != 0) {
								this.actChara.ChangeAnime(i, CActImplCharacter.Anime.Cleared, true); ;
							}
						} else {
							if (OpenTaiko.Skin.Characters_ClearOut_Ptn[Character] != 0) {
								this.actChara.ChangeAnime(i, CActImplCharacter.Anime.ClearOut, true);
							}
						}
					}
					this.actEnd.Start();
				}
				base.ePhaseID = CStage.EPhase.Game_EndChart;
			} else if (bIsFinishedPlaying && base.ePhaseID == CStage.EPhase.Game_EndChart) {
				if (OpenTaiko.ConfigIni.bTokkunMode) {
					bIsFinishedPlaying = false;
					OpenTaiko.Skin.sound特訓停止音.tPlay();
					actTokkun.tPausePlay();

					actTokkun.tMatchWithTheChartDisplayPosition(true);
				} else {
					base.ePhaseID = CStage.EPhase.Game_EndStage;
				}
			} else if (bIsFinishedEndAnime && base.ePhaseID == EPhase.Game_EndStage) {
				this.eフェードアウト完了時の戻り値 = EGameplayScreenReturnValue.StageCleared;
				base.ePhaseID = CStage.EPhase.Game_STAGE_CLEAR_FadeOut;
				this.actFOClear.tフェードアウト開始();
			}

			if (bIsFinishedFadeout) {
				Debug.WriteLine("Total On進行描画=" + sw.ElapsedMilliseconds + "ms");
				return (int)this.eフェードアウト完了時の戻り値;
			}

			ManageMixerQueue();

			// キー入力

			this.tキー入力();


		}
		base.sw.Stop();
		return 0;
	}

	// その他

	#region [ private ]
	//-----------------
	[StructLayout(LayoutKind.Sequential)]
	private struct ST文字位置 {
		public char ch;
		public Point pt;
	}
	public CActImplFireworks actChipFireD;

	private CActImplPad actPad;
	public CActImplMtaiko actMtaiko;
	public CActImplLaneTaiko actLaneTaiko;
	public CActImplClearAnimation actEnd;
	private CAct演奏Drumsゲームモード actGame;
	public CActImplTrainingMode actTokkun;
	public CActImplBackground actBackground;
	public GoGoSplash GoGoSplash;
	public FlyingNotes FlyingNotes;
	public FireWorks FireWorks;
	public PuchiChara PuchiChara;
	public CActImplScoreRank ScoreRank;
	private bool bフィルイン中;
	private readonly EPad[] eチャンネルtoパッド = new EPad[]
	{
		EPad.HH, EPad.SD, EPad.BD, EPad.HT,
		EPad.LT, EPad.CY, EPad.FT, EPad.HHO,
		EPad.RD, EPad.Unknown, EPad.Unknown, EPad.LC,
		EPad.LP, EPad.LBD
	};
	private int[] nチャンネルtoX座標 = new int[] { 370, 470, 582, 527, 645, 748, 694, 373, 815, 298, 419, 419 };
	private CCounter ct手つなぎ;
	private CTexture txヒットバーGB;
	private CTexture txレーンフレームGB;
	//private CTexture tx太鼓ノーツ;
	//private CTexture txHand;
	//private CTexture txSenotes;
	//private CTexture tx小節線;
	//private CTexture tx小節線_branch;

	private CTexture tx判定数表示パネル;
	private CTexture tx判定数小文字;
	//private CTexture txNamePlate; //ちょっと描画順で都合が悪くなるので移動。
	//private CTexture txNamePlate2P; //ちょっと描画順で都合が悪くなるので移動。
	//private CTexture txPlayerNumber;
	private CTexture txMovie; //2016.08.30 kairera0467 ウィンドウ表示

	public float nGauge = 0.0f;
	private int ShownLyric2 = 0;

	private StreamWriter stream;

	private int n待機中の大音符の座標;
	private readonly ST文字位置[] st小文字位置;
	private readonly ST文字位置[] st大文字位置;
	//-----------------

	private ENoteJudge tドラムヒット処理(long nHitTime, EPad type, CChip pChip, bool b両手入力, int nPlayer) {
		var nInput = NotesManager.PadToInputType(type, b両手入力);
		if (!(pChip != null && NotesManager.IsHittableNote(pChip) && !NotesManager.IsRollEnd(pChip)))
			return ENoteJudge.Miss;

		var e判定 = this.tチップのヒット処理(nHitTime, pChip, EInstrumentPad.Taiko, true, nInput, nPlayer);
		if (NotesManager.IsGenericRoll(pChip))
			return e判定;
		if (e判定 == ENoteJudge.Miss)
			return ENoteJudge.Miss;

		this.actGame.t叩ききりまショー_判定から各数値を増加させる(e判定, (int)(nHitTime - pChip.n発声時刻ms));
		return e判定;
	}

	protected override void ドラムスクロール速度アップ() {
		OpenTaiko.ConfigIni.nScrollSpeed[OpenTaiko.SaveFile] = Math.Min(OpenTaiko.ConfigIni.nScrollSpeed[OpenTaiko.SaveFile] + 1, 1999);
	}
	protected override void ドラムスクロール速度ダウン() {
		OpenTaiko.ConfigIni.nScrollSpeed[OpenTaiko.SaveFile] = Math.Max(OpenTaiko.ConfigIni.nScrollSpeed[OpenTaiko.SaveFile] - 1, 0);
	}

	private void t進行描画_チップファイアD() {
		this.actChipFireD.Draw();
	}

	protected override void t紙吹雪_開始() {
		//if( this.actCombo.n現在のコンボ数.Drums % 10 == 0 && this.actCombo.n現在のコンボ数.Drums > 0 )
		{
			//this.actChipFireD.Start紙吹雪();
		}
	}

	protected override void t入力処理_ドラム() {
		// Input adjust deprecated
		var nInputAdjustTimeMs = 0; // OpenTaiko.ConfigIni.nInputAdjustTimeMs;

		for (EPad nPad = 0; nPad < EPad.Max; nPad++) {      // #27029 2012.1.4 from: <10 to <=10; Eパッドの要素が１つ（HP）増えたため。
																//		  2012.1.5 yyagi: (int)Eパッド.MAX に変更。Eパッドの要素数への依存を無くすため。
			int nUsePlayer = NotesManager.GetPadPlayer(nPad);
			if (nUsePlayer >= OpenTaiko.ConfigIni.nPlayerCount
				|| OpenTaiko.stageGameScreen.isDeniedPlaying[nUsePlayer]
				|| ((!OpenTaiko.ConfigIni.bTokkunMode || nUsePlayer > 0) && OpenTaiko.ConfigIni.bAutoPlay[nUsePlayer]) //2020.05.18 Mr-Ojii オート時の入力キャンセル
				|| (nUsePlayer == 1 && OpenTaiko.ConfigIni.bAIBattleMode)
				) {
				continue; // skip input
			}

			List<STInputEvent> listInputEvent = OpenTaiko.Pad.GetEvents(EInstrumentPad.Drums, nPad);
			if ((listInputEvent == null) || (listInputEvent.Count == 0))
				continue;

			this.t入力メソッド記憶(EInstrumentPad.Drums);

			foreach (STInputEvent inputEvent in listInputEvent) {
				if (!inputEvent.Pressed)
					continue;

				// convert input time (mixer space) to note time
				CTja tja = OpenTaiko.GetTJA(nUsePlayer)!;
				long msInputMixer = SoundManager.PlayTimer.SystemTimeToGameTime(inputEvent.nTimeStamp);
				long msHitTjaTime = (long)tja.GameTimeToTjaTime(msInputMixer + nInputAdjustTimeMs);

				EPad nPadAs1P = NotesManager.PadTo1P(nPad);
				// Register to replay file
				OpenTaiko.ReplayInstances[nUsePlayer]?.tRegisterInput(msHitTjaTime, (byte)nPadAs1P);
				this.ProcessPadInput(nUsePlayer, nPadAs1P, msHitTjaTime);
			}
		}
	}

	protected override void ProcessPadInput(int nUsePlayer, EPad nPad, long msHitTjaTime) {
		// test judgement
		var (chipNoHit, e判定) = GetChipToJudge(msHitTjaTime, nUsePlayer, nPad);
		var gameType = this.eGameType[OpenTaiko.GetActualPlayer(nUsePlayer)];
		if (e判定 != ENoteJudge.Miss) {
			e判定 = this.JudgePadInput(nUsePlayer, chipNoHit, nPad, msHitTjaTime, e判定);
			if (e判定 is not (ENoteJudge.Miss or ENoteJudge.Auto or ENoteJudge.ADLIB)) // ADLIB here for "empty hit but not a miss"
				gameType = NotesManager.GetChipGameType(chipNoHit, nUsePlayer);
		}

		// Visual and sound effects
		PlayerLane.FlashType nLane = NotesManager.PadToLane(nPad, gameType);
		int nHand = NotesManager.PadToHand(nPad);
		OpenTaiko.stageGameScreen.actMtaiko.tMtaikoEvent(NotesManager.PadToInputType(nPad), nHand, nUsePlayer);

		#region [ ヒットしてなかった場合は、レーンフラッシュ、パッドアニメ、空打ち音再生を実行 ]
		if (nLane is not PlayerLane.FlashType.Total && e判定 is ENoteJudge.Miss or ENoteJudge.Auto or ENoteJudge.ADLIB) { // ADLIB here for "empty hit but not a miss"
			this.PlayHitNoteSound(nUsePlayer, NotesManager.PadToInputType(nPad));
			this.StartHitNoteLaneFlash(nUsePlayer, NotesManager.PadToInputType(nPad), gameType);

			// BAD or TIGHT 時の処理。
			if (chipNoHit != null && OpenTaiko.ConfigIni.bTight)
				this.tチップのヒット処理_BadならびにTight時のMiss(EInstrumentPad.Drums, e判定, nUsePlayer, null);
		}
		#endregion
	}

	protected override ENoteJudge JudgePadInput(int nUsePlayer, CChip? chipNoHit, EPad nPad, long msHitTjaTime, ENoteJudge rawJudge, bool skipHit = false) {
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
					return skipHit ? rawJudge : this.tドラムヒット処理(msHitTjaTime, nPad, chipNoHit, false, nUsePlayer);
				if (!skipHit) {
					chipNoHit.eNoteState = ENoteState.Wait;
					chipNoHit.msStoredHit = msHitTjaTime;
					chipNoHit.padStoredHit = nPad;
				}
				this.chipNowProcessingMultiHitNotes[nUsePlayer].Add(chipNoHit);
				return ENoteJudge.ADLIB; // here for "empty hit but not a miss"
			} else if (chipNoHit.eNoteState == ENoteState.Wait) {
				bool _isExpected = NotesManager.IsExpectedPadMultiHit(chipNoHit.padStoredHit, nPad, chipNoHit, gameType);
				var msWaitedTime = msHitTjaTime - chipNoHit.msStoredHit;
				if (_isExpected && msWaitedTime < OpenTaiko.ConfigIni.nBigNoteWaitTimems) {
					if (skipHit)
						return ENoteJudge.Perfect;
					chipNoHit.eNoteState = ENoteState.None;
					chipNoHit.padStoredHit = EPad.Unknown;
					return this.tドラムヒット処理((long)chipNoHit.msStoredHit, nPad, chipNoHit, true, nUsePlayer);
				}
			}
			return ENoteJudge.Miss;
		}
		if (skipHit)
			return rawJudge;
		chipNoHit.eNoteState = ENoteState.None;
		chipNoHit.padStoredHit = EPad.Unknown;
		return this.tドラムヒット処理(msHitTjaTime, nPad, chipNoHit, _isBigNoteTaiko, nUsePlayer);
	}

	protected override void t背景テクスチャの生成() {
		Rectangle bgrect = new Rectangle(0, 0, 1280, 720);
		string DefaultBgFilename = @$"Graphics{Path.DirectorySeparatorChar}5_Game{Path.DirectorySeparatorChar}5_Background{Path.DirectorySeparatorChar}0{Path.DirectorySeparatorChar}Background.png";
		string BgFilename = "";
		if (!String.IsNullOrEmpty(OpenTaiko.TJA.strBGIMAGE_PATH))
			BgFilename = OpenTaiko.TJA.strBGIMAGE_PATH;
		base.t背景テクスチャの生成(DefaultBgFilename, bgrect, BgFilename);
	}
	protected override void t進行描画_チップ_Taiko(CConfigIni configIni, ref CTja dTX, ref CChip pChip, int nPlayer) {
		EGameType _gt = NotesManager.GetChipGameType(pChip, nPlayer);
		CTja tja = OpenTaiko.GetTJA(nPlayer)!;

		#region[ 作り直したもの ]

		if (pChip.bVisible) {
			if (!pChip.bHit) {
				long nPlayTime = (long)tja.GameTimeToTjaTime(SoundManager.PlayTimer.NowTimeMs);

				if (pChip.nノーツ出現時刻ms != 0 && (nPlayTime < pChip.n発声時刻ms - pChip.nノーツ出現時刻ms))
					pChip.bShow = false;
				else
					pChip.bShow = true;

				int x = pChip.nHorizontalChipDistance;
				int y = GetNoteOriginY(nPlayer) + pChip.nVerticalChipDistance; // either untouched (0/5) or unused (other) for #DIRECTION

				int xTemp = 0;
				int yTemp = 0;

				#region[ スクロール方向変更 ]
				if (pChip.nScrollDirection != 0) {
					xTemp = x;
					yTemp = y;
				}
				switch (pChip.nScrollDirection) {
					case 0:
						x += (GetNoteOriginX(nPlayer));
						break;
					case 1:
						x = (GetNoteOriginX(nPlayer));
						y = GetNoteOriginY(nPlayer) - xTemp;
						break;
					case 2:
						x = (GetNoteOriginX(nPlayer) + 3);
						y = GetNoteOriginY(nPlayer) + xTemp;
						break;
					case 3:
						x += (GetNoteOriginX(nPlayer));
						y = GetNoteOriginY(nPlayer) - xTemp;
						break;
					case 4:
						x += (GetNoteOriginX(nPlayer));
						y = GetNoteOriginY(nPlayer) + xTemp;
						break;
					case 5:
						x = (GetNoteOriginX(nPlayer) + 10) - xTemp;
						break;
					case 6:
						x = (GetNoteOriginX(nPlayer)) - xTemp;
						y = GetNoteOriginY(nPlayer) - xTemp;
						break;
					case 7:
						x = (GetNoteOriginX(nPlayer)) - xTemp;
						y = GetNoteOriginY(nPlayer) + xTemp;
						break;
				}
				#endregion

				#region[ 両手待ち時 ]
				if (pChip.eNoteState == ENoteState.Wait) {
					x = (GetNoteOriginX(nPlayer));
					y = (GetNoteOriginY(nPlayer));
				}
				#endregion

				#region[ HIDSUD & STEALTH ]
				if (OpenTaiko.ConfigIni.eSTEALTH[OpenTaiko.GetActualPlayer(nPlayer)] == EStealthMode.Stealth || OpenTaiko.stageGameScreen.bCustomDoron) {
					pChip.bShow = false;
				}
				#endregion

				long __dbt = (long)tja.GameTimeToTjaTime(SoundManager.PlayTimer.NowTimeMs);
				long time = pChip.n発声時刻ms - __dbt;

				if (bSplitLane[nPlayer] || OpenTaiko.Tx.Puchichara[PuchiChara.tGetPuchiCharaIndexByName(OpenTaiko.GetActualPlayer(nPlayer))].effect.SplitLane) {
					if (NotesManager.IsAcceptRed(pChip, _gt) && !NotesManager.IsAcceptBlue(pChip, _gt)) {
						y -= NotesManager.PxSplitLaneDistance;
					} else if (NotesManager.IsAcceptBlue(pChip, _gt) && !NotesManager.IsAcceptRed(pChip, _gt)) {
						y += NotesManager.PxSplitLaneDistance;
					}
				}

				if (time < 0) {
					this.actGame.st叩ききりまショー.b最初のチップが叩かれた = true;
				}

				if (x > 0 - OpenTaiko.Skin.Game_Notes_Size[0] && x < OpenTaiko.Skin.Resolution[0]) {
					if (OpenTaiko.Tx.Notes[(int)_gt] != null) {
						//int num9 = this.actCombo.n現在のコンボ数.Drums >= 50 ? this.ctチップ模様アニメ.Drums.n現在の値 * 130 : 0;
						int num9 = 0;
						if (OpenTaiko.Skin.Game_Notes_Anime && !OpenTaiko.ConfigIni.SimpleMode) {
							if (this.actCombo.nCurrentCombo[nPlayer] >= 300 && ctChipAnimeLag[nPlayer].IsEnded) {
								//num9 = ctChipAnime[nPlayer].n現在の値 != 0 ? 260 : 0;
								if ((int)ctChipAnime[nPlayer].CurrentValue == 1 || (int)ctChipAnime[nPlayer].CurrentValue == 3) {
									num9 = OpenTaiko.Skin.Game_Notes_Size[1] * 2;
								} else {
									num9 = 0;
								}
							} else if (this.actCombo.nCurrentCombo[nPlayer] >= 300 && !ctChipAnimeLag[nPlayer].IsEnded) {
								//num9 = base.n現在の音符の顔番号 != 0 ? base.n現在の音符の顔番号 * 130 : 0;
								if ((int)ctChipAnime[nPlayer].CurrentValue == 1 || (int)ctChipAnime[nPlayer].CurrentValue == 3) {
									num9 = OpenTaiko.Skin.Game_Notes_Size[1];
								} else {
									num9 = 0;
								}
							} else if (this.actCombo.nCurrentCombo[nPlayer] >= 150) {
								//num9 = base.n現在の音符の顔番号 != 0 ? base.n現在の音符の顔番号 * 130 : 0;
								if ((int)ctChipAnime[nPlayer].CurrentValue == 1 || (int)ctChipAnime[nPlayer].CurrentValue == 3) {
									num9 = OpenTaiko.Skin.Game_Notes_Size[1];
								} else {
									num9 = 0;
								}
							} else if (this.actCombo.nCurrentCombo[nPlayer] >= 50 && ctChipAnimeLag[nPlayer].IsEnded) {
								//num9 = base.n現在の音符の顔番号 != 0 ? base.n現在の音符の顔番号 * 130 : 0;
								if ((int)ctChipAnime[nPlayer].CurrentValue <= 1) {
									num9 = OpenTaiko.Skin.Game_Notes_Size[1];
								} else {
									num9 = 0;
								}
							} else if (this.actCombo.nCurrentCombo[nPlayer] >= 50 && !ctChipAnimeLag[nPlayer].IsEnded) {
								//num9 = base.n現在の音符の顔番号 != 0 ? base.n現在の音符の顔番号 * 130 : 0;
								num9 = 0;
							} else {
								num9 = 0;
							}
						}



						int nSenotesX = 0;
						int nSenotesY = 0;

						switch (OpenTaiko.ConfigIni.nPlayerCount) {
							case 1:
							case 2:
								nSenotesX = OpenTaiko.Skin.nSENotesX[nPlayer];
								nSenotesY = OpenTaiko.Skin.nSENotesY[nPlayer];
								break;
							case 3:
							case 4:
								nSenotesX = OpenTaiko.Skin.nSENotes_4P[0];
								nSenotesY = OpenTaiko.Skin.nSENotes_4P[1];
								break;
							case 5:
								nSenotesX = OpenTaiko.Skin.nSENotes_5P[0];
								nSenotesY = OpenTaiko.Skin.nSENotes_5P[1];
								break;
						}

						this.ct手つなぎ.TickLoop();
						float fHand = (this.ct手つなぎ.CurrentValue < 30 ? this.ct手つなぎ.CurrentValue : 60 - this.ct手つなぎ.CurrentValue) / 30.0f;


						//x = ( x ) - ( ( int ) ( (TJAPlayer3.Skin.Game_Note_Size[0] * pChip.dbチップサイズ倍率 ) / 2.0 ) );

						//TJAPlayer3.Tx.Notes[(int)_gt].b加算合成 = false;
						//TJAPlayer3.Tx.SENotes.b加算合成 = false;

						switch (pChip.nChannelNo) {
							case 0x11:
							case 0x12:
							case 0x13:
							case 0x14:
							case 0x1C:
							case 0x101: {
									NotesManager.DisplayNote(nPlayer, x, y, pChip, num9);
									NotesManager.DisplaySENotes(nPlayer, x + nSenotesX, y + nSenotesY, pChip);

									//TJAPlayer3.Tx.SENotes[(int)_gt]?.t2D描画(device, x - 2, y + nSenotesY, new Rectangle(0, 30 * pChip.nSenote, 136, 30));
									break;
								}

							case 0x1A:
							case 0x1B: {
									int moveX = (int)(fHand * OpenTaiko.Skin.Game_Notes_Arm_Move[0]);
									int moveY = (int)(fHand * OpenTaiko.Skin.Game_Notes_Arm_Move[1]);
									if (OpenTaiko.ConfigIni.eSTEALTH[OpenTaiko.GetActualPlayer(nPlayer)] == EStealthMode.Off && pChip.bShow) {
										if (nPlayer != OpenTaiko.ConfigIni.nPlayerCount - 1) {
											//上から下
											OpenTaiko.Tx.Notes_Arm?.t2D上下反転描画(
												x + OpenTaiko.Skin.Game_Notes_Arm_Offset_Left_X[0] + moveX,
												y + OpenTaiko.Skin.Game_Notes_Arm_Offset_Left_Y[0] + moveY);
											OpenTaiko.Tx.Notes_Arm?.t2D上下反転描画(
												x + OpenTaiko.Skin.Game_Notes_Arm_Offset_Right_X[0] - moveX,
												y + OpenTaiko.Skin.Game_Notes_Arm_Offset_Right_Y[0] - moveY);
										}
										if (nPlayer != 0) {
											//下から上
											OpenTaiko.Tx.Notes_Arm?.t2D描画(
												x + OpenTaiko.Skin.Game_Notes_Arm_Offset_Left_X[1] + moveX,
												y + OpenTaiko.Skin.Game_Notes_Arm_Offset_Left_Y[1] + moveY);
											OpenTaiko.Tx.Notes_Arm?.t2D描画(
												x + OpenTaiko.Skin.Game_Notes_Arm_Offset_Right_X[1] - moveX,
												y + OpenTaiko.Skin.Game_Notes_Arm_Offset_Right_Y[1] - moveY);
										}
										NotesManager.DisplayNote(nPlayer, x, y, pChip, num9);
										NotesManager.DisplaySENotes(nPlayer, x + nSenotesX, y + nSenotesY, pChip);
									}
									break;
								}

							case 0x1F: {
									NotesManager.DisplayNote(nPlayer, x, y, pChip, num9);
								}
								break;
							default: {
								}
								break;

						}
						//CDTXMania.act文字コンソール.tPrint( x + 60, y + 160, C文字コンソール.Eフォント種別.白, pChip.nPlayerSide.ToString() );
					}
				}
			}
		} else {
			return;
		}
		#endregion
	}
	protected override void t進行描画_チップ_Taiko連打(CConfigIni configIni, ref CTja dTX, ref CChip pChip, int nPlayer) {
		CTja tja = OpenTaiko.GetTJA(nPlayer)!;
		int nSenotesX = 0;
		int nSenotesY = 0;
		long nowTime = (long)tja.GameTimeToTjaTime(SoundManager.PlayTimer.NowTimeMs);

		switch (OpenTaiko.ConfigIni.nPlayerCount) {
			case 1:
			case 2:
				nSenotesX = OpenTaiko.Skin.nSENotesX[nPlayer];
				nSenotesY = OpenTaiko.Skin.nSENotesY[nPlayer];
				break;
			case 3:
			case 4:
				nSenotesX = OpenTaiko.Skin.nSENotes_4P[0];
				nSenotesY = OpenTaiko.Skin.nSENotes_4P[1];
				break;
			case 5:
				nSenotesX = OpenTaiko.Skin.nSENotes_5P[0];
				nSenotesY = OpenTaiko.Skin.nSENotes_5P[1];
				break;
		}

		int n先頭発声位置 = 0;

		EGameType _gt = NotesManager.GetChipGameType(pChip, nPlayer);

		// 2016.11.2 kairera0467
		// 黄連打音符を赤くするやつの実装方法メモ
		//前面を黄色、背面を変色後にしたものを重ねて、打数に応じて前面の透明度を操作すれば、色を操作できるはず。
		//ただしテクスチャのαチャンネル部分が太くなるなどのデメリットが出る。備えよう。

		#region[ 作り直したもの ]
		if (pChip.bVisible) {
			bool pHasBar = (NotesManager.IsRoll(pChip) || NotesManager.IsFuzeRoll(pChip));

			if (NotesManager.IsGenericRoll(pChip)) {
				if (pChip.nノーツ出現時刻ms != 0 && (nowTime < pChip.n発声時刻ms - pChip.nノーツ出現時刻ms))
					pChip.bShow = false;
				else if (pChip.nノーツ出現時刻ms != 0 && pChip.nノーツ移動開始時刻ms != 0)
					pChip.bShow = true;
			}
			if (NotesManager.IsRollEnd(pChip)) {
				if (pChip.nノーツ出現時刻ms != 0 && (nowTime < n先頭発声位置 - pChip.nノーツ出現時刻ms))
					pChip.bShow = false;
				else
					pChip.bShow = true;

				if (pChip.nノーツ移動開始時刻ms != 0)
					n先頭発声位置 = pChip.start.n発声時刻ms;
			}

			int x = GetNoteOriginX(nPlayer) + pChip.nHorizontalChipDistance;
			int y = GetNoteOriginY(nPlayer) + pChip.nVerticalChipDistance;
			int x末端 = GetNoteOriginX(nPlayer) + pChip.end.nHorizontalChipDistance;
			int y末端 = GetNoteOriginY(nPlayer) + pChip.end.nVerticalChipDistance;

			if (NotesManager.IsGenericBalloon(pChip)) {
				if (nowTime >= pChip.n発声時刻ms && nowTime < pChip.end.n発声時刻ms) {
					x = GetNoteOriginX(nPlayer);
					y = GetNoteOriginY(nPlayer);
				} else if (nowTime >= pChip.end.n発声時刻ms) {
					x = x末端;
					y = y末端;
				}
			}

			if (bSplitLane[nPlayer] || OpenTaiko.Tx.Puchichara[PuchiChara.tGetPuchiCharaIndexByName(OpenTaiko.GetActualPlayer(nPlayer))].effect.SplitLane) {
				if (NotesManager.IsAcceptRed(pChip, _gt) && !NotesManager.IsAcceptBlue(pChip, _gt) && !NotesManager.IsGenericBalloon(pChip)) {
					y -= NotesManager.PxSplitLaneDistance;
					y末端 -= NotesManager.PxSplitLaneDistance;
				} else if (NotesManager.IsAcceptBlue(pChip, _gt) && !NotesManager.IsAcceptRed(pChip, _gt) && !NotesManager.IsGenericBalloon(pChip)) {
					y += NotesManager.PxSplitLaneDistance;
					y末端 += NotesManager.PxSplitLaneDistance;
				}
			}

			bool isBodyXInScreen = (Math.Min(x, x末端) < OpenTaiko.Skin.Resolution[0] && Math.Max(x, x末端) > 0 - OpenTaiko.Skin.Game_Notes_Size[0]);
			if (pHasBar) {
				this.HideObscuringRoll(nPlayer, pChip, x, y, x末端, y末端, isBodyXInScreen, nowTime);
			}

			#region[ HIDSUD & STEALTH ]

			if (OpenTaiko.ConfigIni.eSTEALTH[OpenTaiko.GetActualPlayer(nPlayer)] == EStealthMode.Stealth || OpenTaiko.stageGameScreen.bCustomDoron) {
				pChip.bShow = false;
			}

			#endregion

			//if( CDTXMania.ConfigIni.eScrollMode != EScrollMode.Normal )
			//x -= 10;

			if (isBodyXInScreen) {
				if (OpenTaiko.Tx.Notes[(int)_gt] != null) {
					//int num9 = this.actCombo.n現在のコンボ数.Drums >= 50 ? this.ctチップ模様アニメ.Drums.n現在の値 * 130 : 0;
					//int num9 = this.actCombo.n現在のコンボ数.Drums >= 50 ? base.n現在の音符の顔番号 * 130 : 0;
					int num9 = 0;
					//if( this.actCombo.n現在のコンボ数[ nPlayer ] >= 300 )
					//{
					//    num9 = base.n現在の音符の顔番号 != 0 ? 260 : 0;
					//}
					//else if( this.actCombo.n現在のコンボ数[ nPlayer ] >= 50 )
					//{
					//    num9 = base.n現在の音符の顔番号 != 0 ? base.n現在の音符の顔番号 * 130 : 0;
					//}
					if (OpenTaiko.Skin.Game_Notes_Anime && !OpenTaiko.ConfigIni.SimpleMode) {
						if (this.actCombo.nCurrentCombo[nPlayer] >= 300 && ctChipAnimeLag[nPlayer].IsEnded) {
							//num9 = ctChipAnime[nPlayer].db現在の値 != 0 ? 260 : 0;
							if ((int)ctChipAnime[nPlayer].CurrentValue == 1 || (int)ctChipAnime[nPlayer].CurrentValue == 3) {
								num9 = OpenTaiko.Skin.Game_Notes_Size[1] * 2;
							} else {
								num9 = 0;
							}
						} else if (this.actCombo.nCurrentCombo[nPlayer] >= 300 && !ctChipAnimeLag[nPlayer].IsEnded) {
							//num9 = base.n現在の音符の顔番号 != 0 ? base.n現在の音符の顔番号 * 130 : 0;
							if ((int)ctChipAnime[nPlayer].CurrentValue == 1 || (int)ctChipAnime[nPlayer].CurrentValue == 3) {
								num9 = OpenTaiko.Skin.Game_Notes_Size[1];
							} else {
								num9 = 0;
							}
						} else if (this.actCombo.nCurrentCombo[nPlayer] >= 150) {
							//num9 = base.n現在の音符の顔番号 != 0 ? base.n現在の音符の顔番号 * 130 : 0;
							if ((int)ctChipAnime[nPlayer].CurrentValue == 1 || (int)ctChipAnime[nPlayer].CurrentValue == 3) {
								num9 = OpenTaiko.Skin.Game_Notes_Size[1];
							} else {
								num9 = 0;
							}
						} else if (this.actCombo.nCurrentCombo[nPlayer] >= 50 && ctChipAnimeLag[nPlayer].IsEnded) {
							//num9 = base.n現在の音符の顔番号 != 0 ? base.n現在の音符の顔番号 * 130 : 0;
							if ((int)ctChipAnime[nPlayer].CurrentValue <= 1) {
								num9 = OpenTaiko.Skin.Game_Notes_Size[1];
							} else {
								num9 = 0;
							}
						} else if (this.actCombo.nCurrentCombo[nPlayer] >= 50 && !ctChipAnimeLag[nPlayer].IsEnded) {
							//num9 = base.n現在の音符の顔番号 != 0 ? base.n現在の音符の顔番号 * 130 : 0;
							num9 = 0;
						} else {
							num9 = 0;
						}
					}

					//kairera0467氏 の TJAPlayer2forPC のコードを参考にし、打数に応じて色を変える(打数の変更以外はほとんどそのまんま) ろみゅ～？ 2018/8/20
					pChip.RollInputTime?.Tick();
					pChip.RollDelay?.Tick();

					if (pChip.RollInputTime != null && pChip.RollInputTime.IsEnded) {
						pChip.RollInputTime.Stop();
						pChip.RollInputTime.CurrentValue = 0;
						pChip.RollDelay = new CCounter(0, 1, 1, OpenTaiko.Timer);
					}

					if (pChip.RollDelay != null && pChip.RollDelay.IsEnded && pChip.RollEffectLevel > 0) {
						pChip.RollEffectLevel--;
						pChip.RollDelay = new CCounter(0, 1, 1, OpenTaiko.Timer);
						pChip.RollDelay.CurrentValue = 0;
					}

					float f減少するカラー = 1.0f - ((0.95f / 100) * pChip.RollEffectLevel);
					var effectedColor = new Color4(1.0f, f減少するカラー, f減少するカラー, 1f);
					var normalColor = new Color4(1.0f, 1.0f, 1.0f, 1f);
					//float f末端ノーツのテクスチャ位置調整 = 65f;

					//136, 30
					var _size = OpenTaiko.Skin.Game_SENote_Size;
					int _60_cut = 60 * _size[0] / 136;
					int _58_cut = 58 * _size[0] / 136;
					int _78_cut = 78 * _size[0] / 136;

					if (NotesManager.IsRoll(pChip) || NotesManager.IsFuzeRoll(pChip)) {
						NotesManager.DisplayRoll(nPlayer, x, y, pChip, num9, normalColor, effectedColor, x末端, y末端);

						if (OpenTaiko.Tx.SENotes[(int)_gt] != null) {

							if (!NotesManager.IsFuzeRoll(pChip)) {
								int _shift = NotesManager.IsBigRollTaiko(pChip, _gt) ? 26 : 0;
								int senote = pChip.nSenote;
								if (senote == 0xA && _gt is EGameType.Konga) // DRUMROLL
									senote = 7; // drumroll

								if (pChip.bShowRoll) {
									OpenTaiko.Tx.SENotes[(int)_gt].vcScaleRatio.X = x末端 - x - 44 - _shift;
									OpenTaiko.Tx.SENotes[(int)_gt].t2D描画(x + 90 + _shift, y + nSenotesY, new Rectangle(_60_cut, 8 * _size[1], 1, _size[1]));
									OpenTaiko.Tx.SENotes[(int)_gt].vcScaleRatio.X = 1.0f;
									OpenTaiko.Tx.SENotes[(int)_gt].t2D描画(x + 30 + _shift, y + nSenotesY, new Rectangle(0, 8 * _size[1], _60_cut, _size[1]));
								}
								OpenTaiko.Tx.SENotes[(int)_gt].t2D描画(x - (_shift / 13), y + nSenotesY, new Rectangle(0, _size[1] * senote, _size[0], _size[1]));
							} else {
								NotesManager.DisplaySENotes(nPlayer, x + nSenotesX, y + nSenotesY, pChip);
							}

						}

					}

					if (NotesManager.IsBalloon(pChip) || NotesManager.IsKusudama(pChip)) {
						if (pChip.bShow) {
							NotesManager.DisplayNote(nPlayer, x, y, pChip, num9, OpenTaiko.Skin.Game_Notes_Size[0] * 2);
							NotesManager.DisplaySENotes(nPlayer, x + nSenotesX, y + nSenotesY, pChip);

							/*
                            if (TJAPlayer3.ConfigIni.eSTEALTH != Eステルスモード.DORON)
                                TJAPlayer3.Tx.Notes[(int)_gt].t2D描画(x, y, new Rectangle(1430, num9, 260, 130));
                            */

							//TJAPlayer3.Tx.SENotes.t2D描画(x - 2, y + nSenotesY, new Rectangle(0, 30 * pChip.nSenote, 136, 30));
						}
					}
					if (NotesManager.IsRollEnd(pChip)) {
						//大きい連打か小さい連打かの区別方法を考えてなかったよちくしょう
						if (OpenTaiko.Tx.Notes[(int)_gt] != null)
							OpenTaiko.Tx.Notes[(int)_gt].vcScaleRatio.X = 1.0f;
						int n = 0;
						switch (pChip.start.nChannelNo) {
							case 0x15:
								n = 910;
								break;
							case 0x16:
								n = 1300;
								break;
							default:
								n = 910;
								break;
						}
						if (!NotesManager.IsGenericBalloon(pChip.start)) {
							//if( CDTXMania.ConfigIni.eSTEALTH != Eステルスモード.DORON )
							//    CDTXMania.Tx.Notes.t2D描画( CDTXMania.app.Device, x, y, new Rectangle( n, num9, 130, 130 ) );//大音符:1170
							OpenTaiko.Tx.SENotes[(int)_gt]?.t2D描画(x + 56, y + nSenotesY, new Rectangle(_58_cut, 9 * _size[1], _78_cut, _size[1]));
						}

					}
				}
			}
		}
		#endregion
	}

	/// Detect and hide screen-obscuring rolls when any tips are out of screen
	private void HideObscuringRoll(int iPlayer, CChip pChip, int xHead, int yHead, int xEnd, int yEnd, bool isBodyXInScreen, long nowTime) {
		// display judging rolls
		if (nowTime >= pChip.n発声時刻ms && nowTime <= pChip.end.n発声時刻ms) {
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
		int dxHead = NotesManager.GetNoteX(-1000, th16DBeat, pChip.dbBPM, pChip.dbSCROLL, pChip.eScrollMode);
		int dyHead = NotesManager.GetNoteY(-1000, th16DBeat, pChip.dbBPM, pChip.dbSCROLL_Y, pChip.eScrollMode);
		int dxEnd = NotesManager.GetNoteX(-1000, th16DBeat, pChip.end.dbBPM, pChip.end.dbSCROLL, pChip.end.eScrollMode);
		int dyEnd = NotesManager.GetNoteY(-1000, th16DBeat, pChip.end.dbBPM, pChip.end.dbSCROLL_Y, pChip.end.eScrollMode);

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

	protected override void t進行描画_チップ_ドラムス(CConfigIni configIni, ref CTja dTX, ref CChip pChip) {
	}
	protected override void t進行描画_チップ本体_ドラムス(CConfigIni configIni, ref CTja dTX, ref CChip pChip) {
	}
	protected override void t進行描画_チップ_フィルイン(CConfigIni configIni, ref CTja dTX, ref CChip pChip) {

	}
	protected override void t進行描画_チップ_小節線(CConfigIni configIni, ref CTja dTX, ref CChip pChip, int nPlayer) {
		CTja tja = OpenTaiko.GetTJA(nPlayer)!;
		//int n小節番号plus1 = pChip.n発声位置 / 384;
		//int n小節番号plus1 = this.actPlayInfo.NowMeasure[nPlayer];
		int x = GetNoteOriginX(nPlayer) + pChip.nHorizontalChipDistance;
		int y = GetNoteOriginY(nPlayer) + pChip.nVerticalChipDistance;

		if ((pChip.bVisible && !pChip.bHideBarLine) && (OpenTaiko.Tx.Bar != null)) {
			if (x >= 0 && x <= GameWindowSize.Width) {
				if (pChip.bBranch) {
					//this.tx小節線_branch.t2D描画( CDTXMania.app.Device, x - 3, y, new Rectangle( 0, 0, 3, 130 ) );
					OpenTaiko.Tx.Bar_Branch?.t2D描画(x + ((OpenTaiko.Skin.Game_Notes_Size[0] - OpenTaiko.Tx.Bar_Branch.szTextureSize.Width) / 2), y, new Rectangle(0, 0, OpenTaiko.Tx.Bar_Branch.szTextureSize.Width, OpenTaiko.Skin.Game_Notes_Size[1]));
				} else {
					//this.tx小節線.t2D描画( CDTXMania.app.Device, x - 3, y, new Rectangle( 0, 0, 3, 130 ) );
					OpenTaiko.Tx.Bar?.t2D描画(x + ((OpenTaiko.Skin.Game_Notes_Size[0] - OpenTaiko.Tx.Bar.szTextureSize.Width) / 2), y, new Rectangle(0, 0, OpenTaiko.Tx.Bar.szTextureSize.Width, OpenTaiko.Skin.Game_Notes_Size[1]));
				}
			}
		}
	}

	/// <summary>
	/// 全体にわたる制御をする。
	/// </summary>
	public void t全体制御メソッド() {
		int t = (int)SoundManager.PlayTimer.NowTimeMs;
		//CDTXMania.act文字コンソール.tPrint( 0, 16, C文字コンソール.Eフォント種別.白, t.ToString() );

		this.actBalloon.tDrawKusudama();

		for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
			CTja tja = OpenTaiko.GetTJA(i)!;
			double msBarRollProgress = 0;
			for (int iChip = this.chip現在処理中の連打チップ[i].Count; iChip-- > 0;) {
				var chkChip = this.chip現在処理中の連打チップ[i][iChip];
				if (!chkChip.bVisible)
					continue;
				long nowTime = (long)tja.GameTimeToTjaTime(SoundManager.PlayTimer.NowTimeMs);
				//int n = this.chip現在処理中の連打チップ[i].nチャンネル番号;
				if (!this.bPAUSE && !this.isRewinding && !chkChip.bProcessed) {
					this.ProcessRollHeadEffects(i, chkChip);
				}
				if (!NotesManager.IsGenericBalloon(chkChip)) {
					if (chkChip.end.bVisible && chkChip.end.n発声時刻ms >= (int)nowTime)
						msBarRollProgress += (int)nowTime - chkChip.n発声時刻ms;
					continue;
				}
				if (!(chkChip.nRollCount > 0 || NotesManager.IsKusudama(chkChip))) {
					continue;
				}
				//if (this.chip現在処理中の連打チップ.n発声時刻ms <= (int)CSound管理.rc演奏用タイマ.n現在時刻ms && this.chip現在処理中の連打チップ.nノーツ終了時刻ms >= (int)CSound管理.rc演奏用タイマ.n現在時刻ms)
				if (chkChip.n発声時刻ms <= (int)nowTime
					&& chkChip.end.n発声時刻ms + 500 >= (int)nowTime
					) {
					var balloon = NotesManager.IsKusudama(chkChip) ? nCurrentKusudamaCount : chkChip.nBalloon;
					var rollCount = NotesManager.IsKusudama(chkChip) ? nCurrentKusudamaRollCount : chkChip.nRollCount;
					if (!this.bPAUSE && !this.isRewinding && !NotesManager.IsFuzeRoll(chkChip))
						chkChip.bShow = false;
					this.actBalloon.On進行描画(
						balloon,
						balloon - rollCount,
						i,
						NotesManager.IsFuzeRoll(chkChip) ? CActImplBalloon.EBalloonType.FUSEROLL
							: NotesManager.IsKusudama(chkChip) ? CActImplBalloon.EBalloonType.KUSUDAMA
							: CActImplBalloon.EBalloonType.BALLOON
					);
				}
			}
			if (msBarRollProgress != this.msCurrentBarRollProgress[i]) {
				this.msCurrentBarRollProgress[i] = msBarRollProgress;
				if (i == 0)
					this.actDan.Update();
			}
		}

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

				var branchCondType = branchJudgePoint.eBranchCondition;
				double nowBranchCondScore = this.GetBranchConditionScore(0, branchCondType);
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
				if (this.b強制的に分岐させた[0])
					(x, _) = OpenTaiko.actTextConsole.Print(x, y, 0, CTextConsole.EFontType.White, "(FORCED)");

				int nMeasuresToNextBranch = branchJudgePoint.n発声位置 / 384 - (this.actPlayInfo.NowMeasure[0] + 1); // round down
				OpenTaiko.actTextConsole.Print(0, y += dy, CTextConsole.EFontType.White, $"NEXT BRANCH:{nMeasuresToNextBranch,3} BARS");

				y = (int)(362 * OpenTaiko.Skin.ScaleY);
				if (branchCondType == CTja.EBranchConditionType.None) {
					OpenTaiko.actTextConsole.Print(0, y, CTextConsole.EFontType.White, "NEXT BRANCH INFO:(KEEP)");
				} else {
					OpenTaiko.actTextConsole.Print(0, y, CTextConsole.EFontType.White,
						string.Create(CultureInfo.InvariantCulture,
							$"NEXT BRANCH INFO:{CTja.EnumToTjaString(branchCondType)},{branchJudgePoint.nBranchCondition1_Professional},{branchJudgePoint.nBranchCondition2_Master}"));
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
				CChip chipNoHit = this.chipNowProcessingMultiHitNotes[i][iChip];
				EGameType _gt = NotesManager.GetChipGameType(chipNoHit, i);
				bool _isSwapNote = NotesManager.IsSwapNote(chipNoHit, _gt);

				int msMaxWaitTime = OpenTaiko.ConfigIni.nBigNoteWaitTimems;
				var msWaitedTime = timeNow - (float)chipNoHit.msStoredHit;
				if (chipNoHit.eNoteState == ENoteState.Wait && msWaitedTime >= msMaxWaitTime) {
					if (!_isSwapNote) {
						this.tドラムヒット処理((long)chipNoHit.msStoredHit, EPad.Unknown, chipNoHit, false, i);
						chipNoHit.padStoredHit = EPad.Unknown;
						chipNoHit.bHit = true;
						chipNoHit.IsHitted = true;
					}
					chipNoHit.eNoteState = ENoteState.None;
				}
				if (chipNoHit.eNoteState != ENoteState.Wait)
					this.chipNowProcessingMultiHitNotes[i].RemoveAt(iChip--);
			}

		}

		#endregion

		//string strNull = "Found";

		if (OpenTaiko.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.F1)) {
			if (!this.actPauseMenu.bIsActivePopupMenu && this.bPAUSE == false) {
				OpenTaiko.Skin.soundChangeSFX.tPlay();

				SoundManager.PlayTimer.Pause();
				OpenTaiko.Timer.Pause();
				OpenTaiko.TJA.t全チップの再生一時停止();
				this.actAVI.Pause();

				this.bPAUSE = true;
				this.actPauseMenu.tActivatePopupMenu(0);
			}

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

	private void t進行描画_リアルタイム判定数表示() {
		var showJudgeInfo = false;

		if (OpenTaiko.ConfigIni.nPlayerCount == 1 ? (OpenTaiko.ConfigIni.bJudgeCountDisplay && !OpenTaiko.ConfigIni.bAutoPlay[0]) : false) showJudgeInfo = true;
		if (OpenTaiko.ConfigIni.bTokkunMode) showJudgeInfo = true;

		if (showJudgeInfo) {
			//ボードの横幅は333px
			//数字フォントの小さいほうはリザルトのものと同じ。
			if (OpenTaiko.Tx.Judge_Meter != null)
				OpenTaiko.Tx.Judge_Meter.t2D描画(OpenTaiko.Skin.Game_Judge_Meter[0], OpenTaiko.Skin.Game_Judge_Meter[1]);

			this.t小文字表示(OpenTaiko.Skin.Game_Judge_Meter_Perfect[0], OpenTaiko.Skin.Game_Judge_Meter_Perfect[1], this.nHitCount_ExclAuto.Drums.Perfect, false, false);
			this.t小文字表示(OpenTaiko.Skin.Game_Judge_Meter_Good[0], OpenTaiko.Skin.Game_Judge_Meter_Good[1], this.nHitCount_ExclAuto.Drums.Great, false, false);
			this.t小文字表示(OpenTaiko.Skin.Game_Judge_Meter_Miss[0], OpenTaiko.Skin.Game_Judge_Meter_Miss[1], this.nHitCount_ExclAuto.Drums.Miss, false, false);
			this.t小文字表示(OpenTaiko.Skin.Game_Judge_Meter_Roll[0], OpenTaiko.Skin.Game_Judge_Meter_Roll[1], GetRoll(0), false, false);

			int nNowTotal = this.nHitCount_ExclAuto.Drums.Perfect + this.nHitCount_ExclAuto.Drums.Great + this.nHitCount_ExclAuto.Drums.Miss;
			double dbたたけた率 = Math.Round((100.0 * (OpenTaiko.stageGameScreen.nHitCount_ExclAuto.Drums.Perfect + OpenTaiko.stageGameScreen.nHitCount_ExclAuto.Drums.Great)) / (double)nNowTotal);
			double dbPERFECT率 = Math.Round((100.0 * OpenTaiko.stageGameScreen.nHitCount_ExclAuto.Drums.Perfect) / (double)nNowTotal);
			double dbGREAT率 = Math.Round((100.0 * OpenTaiko.stageGameScreen.nHitCount_ExclAuto.Drums.Great / (double)nNowTotal));
			double dbMISS率 = Math.Round((100.0 * OpenTaiko.stageGameScreen.nHitCount_ExclAuto.Drums.Miss / (double)nNowTotal));

			if (double.IsNaN(dbたたけた率))
				dbたたけた率 = 0;
			if (double.IsNaN(dbPERFECT率))
				dbPERFECT率 = 0;
			if (double.IsNaN(dbGREAT率))
				dbGREAT率 = 0;
			if (double.IsNaN(dbMISS率))
				dbMISS率 = 0;

			this.t大文字表示(OpenTaiko.Skin.Game_Judge_Meter_HitRate[0], OpenTaiko.Skin.Game_Judge_Meter_HitRate[1], (int)dbたたけた率);
			this.t小文字表示(OpenTaiko.Skin.Game_Judge_Meter_PerfectRate[0], OpenTaiko.Skin.Game_Judge_Meter_PerfectRate[1], (int)dbPERFECT率, false, true);
			this.t小文字表示(OpenTaiko.Skin.Game_Judge_Meter_GoodRate[0], OpenTaiko.Skin.Game_Judge_Meter_GoodRate[1], (int)dbGREAT率, false, true);
			this.t小文字表示(OpenTaiko.Skin.Game_Judge_Meter_MissRate[0], OpenTaiko.Skin.Game_Judge_Meter_MissRate[1], (int)dbMISS率, false, true);
		}
	}

	private void t小文字表示(int x, int y, int num, bool bOrange, bool drawPercent) {
		float width = OpenTaiko.Tx.Result_Number.sz画像サイズ.Width / 11.0f;
		float height = OpenTaiko.Tx.Result_Number.sz画像サイズ.Height / 2.0f;

		int[] nums = CConversion.SeparateDigits(num);

		if (drawPercent) {
			OpenTaiko.Tx.Result_Number.t2D拡大率考慮中央基準描画(x + (OpenTaiko.Skin.Result_Number_Interval[0] * 3.0f) + (width / 2),
				y + (OpenTaiko.Skin.Result_Number_Interval[1] * 3.0f) + (height / 2),
				new System.Drawing.RectangleF(width * 10, 0, width, height));
		}

		for (int j = 0; j < nums.Length; j++) {
			float offset = j - 1.5f;
			float _x = x - (OpenTaiko.Skin.Result_Number_Interval[0] * offset);
			float _y = y - (OpenTaiko.Skin.Result_Number_Interval[1] * offset);

			OpenTaiko.Tx.Result_Number.t2D拡大率考慮中央基準描画(_x + (width / 2), _y + (height / 2),
				new System.Drawing.RectangleF(width * nums[j], 0, width, height));
		}
	}

	private void t大文字表示(int x, int y, int num) {
		int[] nums = CConversion.SeparateDigits(num);
		for (int j = 0; j < nums.Length; j++) {
			float offset = j - 1.5f;
			float _x = x - ((OpenTaiko.Skin.Result_Number_Interval[0] * 1.27f) * offset);
			float _y = y - ((OpenTaiko.Skin.Result_Number_Interval[1] * 1.27f) * offset);

			float width = OpenTaiko.Tx.Result_Number.sz画像サイズ.Width / 11.0f;
			float height = OpenTaiko.Tx.Result_Number.sz画像サイズ.Height / 2.0f;

			OpenTaiko.Tx.Result_Number.t2D拡大率考慮中央基準描画(_x + (width / 2), _y + (height / 2),
				new System.Drawing.RectangleF(width * nums[j], height, width, height));
		}
	}
	#endregion
}
