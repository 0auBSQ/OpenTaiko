using FDK;

namespace OpenTaiko;

internal class CActImplClearAnimation : CActivity {
	/// <summary>
	/// 課題
	/// _クリア失敗 →素材不足(確保はできる。切り出しと加工をしてないだけ。)
	/// _
	/// </summary>
	public CActImplClearAnimation() {
		base.IsDeActivated = true;
	}

	public void Start(int iPlayer) {
		// this.ct進行メイン = new CCounter(0, 500, 1000 / 60, TJAPlayer3.Timer);

		bSoundPlayed[iPlayer] = false;

		this.ct進行メイン[iPlayer] = new CCounter(0, 300, 22, OpenTaiko.Timer);

		/*
        this.ctEnd_ClearFailed = new CCounter(0, 69, 30, TJAPlayer3.Timer);
        this.ctEnd_FullCombo = new CCounter(0, 66, 33, TJAPlayer3.Timer);
        this.ctEnd_FullComboLoop = new CCounter(0, 2, 30, TJAPlayer3.Timer);
        this.ctEnd_DondaFullCombo = new CCounter(0, 61, 33, TJAPlayer3.Timer);
        this.ctEnd_DondaFullComboLoop = new CCounter(0, 2, 30, TJAPlayer3.Timer);
        */

		// モードの決定。クリア失敗・フルコンボも事前に作っとく。
		if (OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Tower) {
			if (!(OpenTaiko.stageGameScreen.IsStageFailed(iPlayer) || OpenTaiko.stageGameScreen.IsStageAborted()) && CFloorManagement.CurrentNumberOfLives > 0) {
				if (OpenTaiko.stageGameScreen.CChartScore[0].nMiss == 0 && OpenTaiko.stageGameScreen.CChartScore[0].nMine == 0) {
					if (OpenTaiko.stageGameScreen.CChartScore[0].nGood == 0)
						this.Mode[0] = EndMode.Tower_TopReached_Perfect;
					else
						this.Mode[0] = EndMode.Tower_TopReached_FullCombo;
				} else
					this.Mode[0] = EndMode.Tower_TopReached_Pass;
			} else
				this.Mode[0] = EndMode.Tower_Dropout;
		} else if (OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Dan) {
			// 段位認定モード。
			if (!(OpenTaiko.stageGameScreen.IsStageFailed(iPlayer) || OpenTaiko.stageGameScreen.IsStageAborted()) && !Dan_Cert.GetFailedAllChallenges(OpenTaiko.stageGameScreen.actDan.GetExam(), OpenTaiko.stageSongSelect.rChoosenSong.DanSongs)) {
				// 段位認定モード、クリア成功
				// this.Mode[0] = EndMode.StageCleared;

				bool bgold = OpenTaiko.stageGameScreen.actDan.GetResultExamStatus(OpenTaiko.stageGameScreen.actDan.GetExam(), OpenTaiko.stageSongSelect.rChoosenSong.DanSongs) == Exam.Status.Better_Success;

				if (OpenTaiko.stageGameScreen.CChartScore[0].nMiss == 0 && OpenTaiko.stageGameScreen.CChartScore[0].nMine == 0) {
					if (OpenTaiko.stageGameScreen.CChartScore[0].nGood == 0)
						this.Mode[0] = bgold ? EndMode.Dan_Gold_Perfect : EndMode.Dan_Red_Perfect;
					else
						this.Mode[0] = bgold ? EndMode.Dan_Gold_FullCombo : EndMode.Dan_Red_FullCombo;
				} else
					this.Mode[0] = bgold ? EndMode.Dan_Gold_Pass : EndMode.Dan_Red_Pass;


			} else {
				// 段位認定モード、クリア失敗
				this.Mode[0] = EndMode.Dan_Fail;
			}
		} else if (OpenTaiko.ConfigIni.bAIBattleMode) {
			if (!(OpenTaiko.stageGameScreen.IsStageFailed(iPlayer) || OpenTaiko.stageGameScreen.IsStageAborted()) && OpenTaiko.stageGameScreen.bIsAIBattleWin) {
				if (OpenTaiko.stageGameScreen.CChartScore[0].nMiss == 0 && OpenTaiko.stageGameScreen.CChartScore[0].nMine == 0) {
					if (OpenTaiko.stageGameScreen.CChartScore[0].nGood == 0)
						this.Mode[0] = EndMode.AI_Win_Perfect;
					else
						this.Mode[0] = EndMode.AI_Win_FullCombo;
				} else
					this.Mode[0] = EndMode.AI_Win;
			} else {
				this.Mode[0] = EndMode.AI_Lose;
			}
		} else {
			// 通常のモード。
			// ここでフルコンボフラグをチェックするが現時点ではない。
			// 今の段階では魂ゲージ80%以上でチェック。
			if (!(OpenTaiko.stageGameScreen.IsStageFailed(iPlayer) || OpenTaiko.stageGameScreen.IsStageAborted()) && HGaugeMethods.UNSAFE_FastNormaCheck(iPlayer)) {
				if (OpenTaiko.stageGameScreen.CChartScore[iPlayer].nMiss == 0 && OpenTaiko.stageGameScreen.CChartScore[iPlayer].nMine == 0)
				//if (TJAPlayer3.stage演奏ドラム画面.nヒット数_Auto含まない.Drums.Miss == 0)
				{
					if (OpenTaiko.stageGameScreen.CChartScore[iPlayer].nGood == 0)
					//if (TJAPlayer3.stage演奏ドラム画面.nヒット数_Auto含まない.Drums.Great == 0)
					{
						this.Mode[iPlayer] = EndMode.StagePerfectCombo;
					} else {
						this.Mode[iPlayer] = EndMode.StageFullCombo;
					}
				} else {
					this.Mode[iPlayer] = EndMode.StageCleared;
				}
			} else {
				this.Mode[iPlayer] = EndMode.StageFailed;
			}
		}
	}

	public override void Activate() {
		this.bリザルトボイス再生済み = false;
		this.Mode = new EndMode[5];

		var origindir = CSkin.Path($"{TextureLoader.BASE}{TextureLoader.GAME}{TextureLoader.END}");

		if (OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Tower) {
			Tower_DropoutScript = new EndAnimeScript($@"{origindir}Tower_Dropout{Path.DirectorySeparatorChar}Script.lua", $@"{origindir}ClearFailed{Path.DirectorySeparatorChar}Script.lua");
			Tower_DropoutScript.Init();

			Tower_TopReached_PassScript = new EndAnimeScript($@"{origindir}Tower_TopReached_Pass{Path.DirectorySeparatorChar}Script.lua", $@"{origindir}Clear{Path.DirectorySeparatorChar}Script.lua");
			Tower_TopReached_PassScript.Init();

			Tower_TopReached_FullComboScript = new EndAnimeScript($@"{origindir}Tower_TopReached_FullCombo{Path.DirectorySeparatorChar}Script.lua", $@"{origindir}FullCombo{Path.DirectorySeparatorChar}Script.lua");
			Tower_TopReached_FullComboScript.Init();

			Tower_TopReached_PerfectScript = new EndAnimeScript($@"{origindir}Tower_TopReached_Perfect{Path.DirectorySeparatorChar}Script.lua", $@"{origindir}AllPerfect{Path.DirectorySeparatorChar}Script.lua");
			Tower_TopReached_PerfectScript.Init();

			this.soundTowerDropout = OpenTaiko.SoundManager.tCreateSound(CSkin.Path(@$"Sounds{Path.DirectorySeparatorChar}Tower{Path.DirectorySeparatorChar}Tower_Dropout.ogg"), ESoundGroup.SoundEffect);
			this.soundTowerTopPass = OpenTaiko.SoundManager.tCreateSound(CSkin.Path(@$"Sounds{Path.DirectorySeparatorChar}Tower{Path.DirectorySeparatorChar}Tower_TopReached_Pass.ogg"), ESoundGroup.SoundEffect);
			this.soundTowerTopFC = OpenTaiko.SoundManager.tCreateSound(CSkin.Path(@$"Sounds{Path.DirectorySeparatorChar}Tower{Path.DirectorySeparatorChar}Tower_TopReached_FullCombo.ogg"), ESoundGroup.SoundEffect);
			this.soundTowerTopPerfect = OpenTaiko.SoundManager.tCreateSound(CSkin.Path(@$"Sounds{Path.DirectorySeparatorChar}Tower{Path.DirectorySeparatorChar}Tower_TopReached_Perfect.ogg"), ESoundGroup.SoundEffect);
		} else if (OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Dan) {
			Dan_FailScript = new EndAnimeScript($@"{origindir}Dan_Fail{Path.DirectorySeparatorChar}Script.lua", $@"{origindir}ClearFailed{Path.DirectorySeparatorChar}Script.lua");
			Dan_FailScript.Init();

			Dan_Red_PassScript = new EndAnimeScript($@"{origindir}Dan_Red_Pass{Path.DirectorySeparatorChar}Script.lua", $@"{origindir}Clear{Path.DirectorySeparatorChar}Script.lua");
			Dan_Red_PassScript.Init();

			Dan_Red_FullComboScript = new EndAnimeScript($@"{origindir}Dan_Red_FullCombo{Path.DirectorySeparatorChar}Script.lua", $@"{origindir}FullCombo{Path.DirectorySeparatorChar}Script.lua");
			Dan_Red_FullComboScript.Init();

			Dan_Red_PerfectScript = new EndAnimeScript($@"{origindir}Dan_Red_Perfect{Path.DirectorySeparatorChar}Script.lua", $@"{origindir}AllPerfect{Path.DirectorySeparatorChar}Script.lua");
			Dan_Red_PerfectScript.Init();

			Dan_Gold_PassScript = new EndAnimeScript($@"{origindir}Dan_Gold_Pass{Path.DirectorySeparatorChar}Script.lua", $@"{origindir}Dan_Red_Pass{Path.DirectorySeparatorChar}Script.lua", $@"{origindir}Clear{Path.DirectorySeparatorChar}Script.lua");
			Dan_Gold_PassScript.Init();

			Dan_Gold_FullComboScript = new EndAnimeScript($@"{origindir}Dan_Gold_FullCombo{Path.DirectorySeparatorChar}Script.lua", $@"{origindir}Dan_Red_FullCombo{Path.DirectorySeparatorChar}Script.lua", $@"{origindir}FullCombo{Path.DirectorySeparatorChar}Script.lua");
			Dan_Gold_FullComboScript.Init();

			Dan_Gold_PerfectScript = new EndAnimeScript($@"{origindir}Dan_Gold_Perfect{Path.DirectorySeparatorChar}Script.lua", $@"{origindir}Dan_Red_Perfect{Path.DirectorySeparatorChar}Script.lua", $@"{origindir}AllPerfect{Path.DirectorySeparatorChar}Script.lua");
			Dan_Gold_PerfectScript.Init();

			this.soundDanFailed = OpenTaiko.SoundManager.tCreateSound(CSkin.Path(@$"Sounds{Path.DirectorySeparatorChar}Dan{Path.DirectorySeparatorChar}Dan_Fail.ogg"), ESoundGroup.SoundEffect);
			this.soundDanRedClear = OpenTaiko.SoundManager.tCreateSound(CSkin.Path(@$"Sounds{Path.DirectorySeparatorChar}Dan{Path.DirectorySeparatorChar}Dan_Red_Pass.ogg"), ESoundGroup.SoundEffect);
			this.soundDanRedFC = OpenTaiko.SoundManager.tCreateSound(CSkin.Path(@$"Sounds{Path.DirectorySeparatorChar}Dan{Path.DirectorySeparatorChar}Dan_Red_FullCombo.ogg"), ESoundGroup.SoundEffect);
			this.soundDanRedPerfect = OpenTaiko.SoundManager.tCreateSound(CSkin.Path(@$"Sounds{Path.DirectorySeparatorChar}Dan{Path.DirectorySeparatorChar}Dan_Red_Perfect.ogg"), ESoundGroup.SoundEffect);
			this.soundDanGoldClear = OpenTaiko.SoundManager.tCreateSound(CSkin.Path(@$"Sounds{Path.DirectorySeparatorChar}Dan{Path.DirectorySeparatorChar}Dan_Gold_Pass.ogg"), ESoundGroup.SoundEffect);
			this.soundDanGoldFC = OpenTaiko.SoundManager.tCreateSound(CSkin.Path(@$"Sounds{Path.DirectorySeparatorChar}Dan{Path.DirectorySeparatorChar}Dan_Gold_FullCombo.ogg"), ESoundGroup.SoundEffect);
			this.soundDanGoldPerfect = OpenTaiko.SoundManager.tCreateSound(CSkin.Path(@$"Sounds{Path.DirectorySeparatorChar}Dan{Path.DirectorySeparatorChar}Dan_Gold_Perfect.ogg"), ESoundGroup.SoundEffect);

		} else if (OpenTaiko.ConfigIni.bAIBattleMode) {
			AILoseScript = new EndAnimeScript($@"{origindir}AI_Lose{Path.DirectorySeparatorChar}Script.lua", $@"{origindir}ClearFailed{Path.DirectorySeparatorChar}Script.lua");
			AILoseScript.Init();

			AIWinScript = new EndAnimeScript($@"{origindir}AI_Win{Path.DirectorySeparatorChar}Script.lua", $@"{origindir}Clear{Path.DirectorySeparatorChar}Script.lua");
			AIWinScript.Init();

			AIWin_FullComboScript = new EndAnimeScript($@"{origindir}AI_Win_FullCombo{Path.DirectorySeparatorChar}Script.lua", $@"{origindir}FullCombo{Path.DirectorySeparatorChar}Script.lua");
			AIWin_FullComboScript.Init();

			AIWin_PerfectScript = new EndAnimeScript($@"{origindir}AI_Win_Perfect{Path.DirectorySeparatorChar}Script.lua", $@"{origindir}AllPerfect{Path.DirectorySeparatorChar}Script.lua");
			AIWin_PerfectScript.Init();

			this.soundAILose = OpenTaiko.SoundManager.tCreateSound(CSkin.Path(@$"Sounds{Path.DirectorySeparatorChar}AIBattle_Lose.ogg"), ESoundGroup.SoundEffect);
			this.soundAIWin = OpenTaiko.SoundManager.tCreateSound(CSkin.Path(@$"Sounds{Path.DirectorySeparatorChar}AIBattle_Win.ogg"), ESoundGroup.SoundEffect);
			this.soundAIWinFullCombo = OpenTaiko.SoundManager.tCreateSound(CSkin.Path(@$"Sounds{Path.DirectorySeparatorChar}AIBattle_Win_FullCombo.ogg"), ESoundGroup.SoundEffect);
			this.soundAIWinPerfectCombo = OpenTaiko.SoundManager.tCreateSound(CSkin.Path(@$"Sounds{Path.DirectorySeparatorChar}AIBattle_Win_AllPerfect.ogg"), ESoundGroup.SoundEffect);
		} else {
			FailedScript = new EndAnimeScript($@"{origindir}ClearFailed{Path.DirectorySeparatorChar}Script.lua");//ClearFailed
			FailedScript.Init();

			ClearScript = new EndAnimeScript($@"{origindir}Clear{Path.DirectorySeparatorChar}Script.lua");
			ClearScript.Init();

			FullComboScript = new EndAnimeScript($@"{origindir}FullCombo{Path.DirectorySeparatorChar}Script.lua");
			FullComboScript.Init();

			PerfectComboScript = new EndAnimeScript($@"{origindir}AllPerfect{Path.DirectorySeparatorChar}Script.lua");
			PerfectComboScript.Init();
		}


		base.Activate();
	}

	public override void DeActivate() {
		for (int i = 0; i < OpenTaiko.MAX_PLAYERS; ++i)
			this.ct進行メイン[i] = null;

		if (OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Tower) {
			Tower_DropoutScript.Dispose();
			Tower_TopReached_PassScript.Dispose();
			Tower_TopReached_FullComboScript.Dispose();
			Tower_TopReached_PerfectScript.Dispose();
		} else if (OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Dan) {
			Dan_FailScript.Dispose();
			Dan_Red_PassScript.Dispose();
			Dan_Red_FullComboScript.Dispose();
			Dan_Red_PerfectScript.Dispose();
			Dan_Gold_PassScript.Dispose();
			Dan_Gold_FullComboScript.Dispose();
			Dan_Gold_PerfectScript.Dispose();
		} else if (OpenTaiko.ConfigIni.bAIBattleMode) {
			AILoseScript.Dispose();
			AIWinScript.Dispose();
			AIWin_FullComboScript.Dispose();
			AIWin_PerfectScript.Dispose();
		} else {
			FailedScript.Dispose();
			ClearScript.Dispose();
			FullComboScript.Dispose();
			PerfectComboScript.Dispose();
		}

		base.DeActivate();
	}

	public override void CreateManagedResource() {
		this.b再生済み = false;

		this.soundTowerDropout = OpenTaiko.SoundManager.tCreateSound(CSkin.Path(@$"Sounds{Path.DirectorySeparatorChar}Tower{Path.DirectorySeparatorChar}Tower_Dropout.ogg"), ESoundGroup.SoundEffect);
		this.soundTowerTopPass = OpenTaiko.SoundManager.tCreateSound(CSkin.Path(@$"Sounds{Path.DirectorySeparatorChar}Tower{Path.DirectorySeparatorChar}Tower_TopReached_Pass.ogg"), ESoundGroup.SoundEffect);
		this.soundTowerTopFC = OpenTaiko.SoundManager.tCreateSound(CSkin.Path(@$"Sounds{Path.DirectorySeparatorChar}Tower{Path.DirectorySeparatorChar}Tower_TopReached_FullCombo.ogg"), ESoundGroup.SoundEffect);
		this.soundTowerTopPerfect = OpenTaiko.SoundManager.tCreateSound(CSkin.Path(@$"Sounds{Path.DirectorySeparatorChar}Tower{Path.DirectorySeparatorChar}Tower_TopReached_Perfect.ogg"), ESoundGroup.SoundEffect);

		this.soundDanFailed = OpenTaiko.SoundManager.tCreateSound(CSkin.Path(@$"Sounds{Path.DirectorySeparatorChar}Dan{Path.DirectorySeparatorChar}Dan_Fail.ogg"), ESoundGroup.SoundEffect);
		this.soundDanRedClear = OpenTaiko.SoundManager.tCreateSound(CSkin.Path(@$"Sounds{Path.DirectorySeparatorChar}Dan{Path.DirectorySeparatorChar}Dan_Red_Pass.ogg"), ESoundGroup.SoundEffect);
		this.soundDanRedFC = OpenTaiko.SoundManager.tCreateSound(CSkin.Path(@$"Sounds{Path.DirectorySeparatorChar}Dan{Path.DirectorySeparatorChar}Dan_Red_FullCombo.ogg"), ESoundGroup.SoundEffect);
		this.soundDanRedPerfect = OpenTaiko.SoundManager.tCreateSound(CSkin.Path(@$"Sounds{Path.DirectorySeparatorChar}Dan{Path.DirectorySeparatorChar}Dan_Red_Perfect.ogg"), ESoundGroup.SoundEffect);
		this.soundDanGoldClear = OpenTaiko.SoundManager.tCreateSound(CSkin.Path(@$"Sounds{Path.DirectorySeparatorChar}Dan{Path.DirectorySeparatorChar}Dan_Gold_Pass.ogg"), ESoundGroup.SoundEffect);
		this.soundDanGoldFC = OpenTaiko.SoundManager.tCreateSound(CSkin.Path(@$"Sounds{Path.DirectorySeparatorChar}Dan{Path.DirectorySeparatorChar}Dan_Gold_FullCombo.ogg"), ESoundGroup.SoundEffect);
		this.soundDanGoldPerfect = OpenTaiko.SoundManager.tCreateSound(CSkin.Path(@$"Sounds{Path.DirectorySeparatorChar}Dan{Path.DirectorySeparatorChar}Dan_Gold_Perfect.ogg"), ESoundGroup.SoundEffect);

		this.soundAILose = OpenTaiko.SoundManager.tCreateSound(CSkin.Path(@$"Sounds{Path.DirectorySeparatorChar}AIBattle_Lose.ogg"), ESoundGroup.SoundEffect);
		this.soundAIWin = OpenTaiko.SoundManager.tCreateSound(CSkin.Path(@$"Sounds{Path.DirectorySeparatorChar}AIBattle_Win.ogg"), ESoundGroup.SoundEffect);
		this.soundAIWinFullCombo = OpenTaiko.SoundManager.tCreateSound(CSkin.Path(@$"Sounds{Path.DirectorySeparatorChar}AIBattle_Win_FullCombo.ogg"), ESoundGroup.SoundEffect);
		this.soundAIWinPerfectCombo = OpenTaiko.SoundManager.tCreateSound(CSkin.Path(@$"Sounds{Path.DirectorySeparatorChar}AIBattle_Win_AllPerfect.ogg"), ESoundGroup.SoundEffect);
		for (int i = 0; i < OpenTaiko.MAX_PLAYERS; i++) {
			this.soundClear[i] = OpenTaiko.SoundManager.tCreateSound(CSkin.Path(@$"Sounds{Path.DirectorySeparatorChar}Clear.ogg"), ESoundGroup.SoundEffect);
			this.soundFailed[i] = OpenTaiko.SoundManager.tCreateSound(CSkin.Path(@$"Sounds{Path.DirectorySeparatorChar}Failed.ogg"), ESoundGroup.SoundEffect);
			this.soundFullCombo[i] = OpenTaiko.SoundManager.tCreateSound(CSkin.Path(@$"Sounds{Path.DirectorySeparatorChar}FullCombo.ogg"), ESoundGroup.SoundEffect);
			this.soundPerfectCombo[i] = OpenTaiko.SoundManager.tCreateSound(CSkin.Path(@$"Sounds{Path.DirectorySeparatorChar}AllPerfect.ogg"), ESoundGroup.SoundEffect);
		}

		base.CreateManagedResource();
	}

	public override void ReleaseManagedResource() {
		this.soundTowerDropout?.tDispose();
		this.soundTowerTopPass?.tDispose();
		this.soundTowerTopFC?.tDispose();
		this.soundTowerTopPerfect?.tDispose();

		this.soundDanFailed?.tDispose();
		this.soundDanRedClear?.tDispose();
		this.soundDanRedFC?.tDispose();
		this.soundDanRedPerfect?.tDispose();
		this.soundDanGoldClear?.tDispose();
		this.soundDanGoldFC?.tDispose();
		this.soundDanGoldPerfect?.tDispose();

		this.soundAILose?.tDispose();
		this.soundAIWin?.tDispose();
		this.soundAIWinFullCombo?.tDispose();
		this.soundAIWinPerfectCombo?.tDispose();

		for (int i = 0; i < OpenTaiko.MAX_PLAYERS; i++) {
			this.soundClear[i]?.tDispose();
			this.soundFailed[i]?.tDispose();
			this.soundFullCombo[i]?.tDispose();
			this.soundPerfectCombo[i]?.tDispose();
		}

		base.ReleaseManagedResource();
	}

	public override int Draw() {
		if (base.IsFirstDraw) {
			base.IsFirstDraw = false;
		}
		int ret = 1;
		for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; ++i)
			if (this.Draw(i) == 0)
				ret = 0;
		return ret;
	}
	protected int Draw(int iPlayer) {
		if (this.ct進行メイン[iPlayer] != null) {
			if (!(OpenTaiko.stageGameScreen.IsStageFailed(iPlayer) || OpenTaiko.stageGameScreen.IsStageAborted() || OpenTaiko.stageGameScreen.IsStageCompleted()))
				return 0;

			this.ct進行メイン[iPlayer].Tick();

			var (script, sound, voices) = this.Mode[iPlayer] switch {
				EndMode.StageFailed => (FailedScript, this.soundFailed[iPlayer], OpenTaiko.Skin.voiceClearFailed),
				EndMode.StageCleared => (ClearScript, this.soundClear[iPlayer], OpenTaiko.Skin.voiceClearClear),
				EndMode.StageFullCombo => (FullComboScript, this.soundFullCombo[iPlayer], OpenTaiko.Skin.voiceClearFullCombo),
				EndMode.StagePerfectCombo => (PerfectComboScript, this.soundPerfectCombo[iPlayer], OpenTaiko.Skin.voiceClearAllPerfect),

				EndMode.AI_Lose => (AILoseScript, this.soundAILose ?? this.soundFailed[iPlayer], OpenTaiko.Skin.voiceAILose),
				EndMode.AI_Win => (AIWinScript, this.soundAIWin ?? this.soundClear[iPlayer], OpenTaiko.Skin.voiceAIWin),
				EndMode.AI_Win_FullCombo => (AIWin_FullComboScript, this.soundAIWinFullCombo ?? this.soundFullCombo[iPlayer], OpenTaiko.Skin.voiceAIWin),
				EndMode.AI_Win_Perfect => (AIWin_PerfectScript, this.soundAIWinPerfectCombo ?? this.soundPerfectCombo[iPlayer], OpenTaiko.Skin.voiceAIWin),

				EndMode.Tower_Dropout => (Tower_DropoutScript, this.soundTowerDropout ?? this.soundFailed[iPlayer], OpenTaiko.Skin.voiceClearFailed),
				EndMode.Tower_TopReached_Pass => (Tower_TopReached_PassScript, this.soundTowerTopPass ?? this.soundClear[iPlayer], OpenTaiko.Skin.voiceClearClear),
				EndMode.Tower_TopReached_FullCombo => (Tower_TopReached_FullComboScript, this.soundTowerTopFC ?? this.soundFullCombo[iPlayer], OpenTaiko.Skin.voiceClearFullCombo),
				EndMode.Tower_TopReached_Perfect => (Tower_TopReached_PerfectScript, this.soundTowerTopPerfect ?? this.soundPerfectCombo[iPlayer], OpenTaiko.Skin.voiceClearAllPerfect),

				EndMode.Dan_Fail => (Dan_FailScript, this.soundDanFailed ?? this.soundFailed[iPlayer], OpenTaiko.Skin.voiceClearFailed),
				EndMode.Dan_Red_Pass => (Dan_Red_PassScript, this.soundDanRedClear ?? this.soundClear[iPlayer], OpenTaiko.Skin.voiceClearClear),
				EndMode.Dan_Red_FullCombo => (Dan_Red_FullComboScript, this.soundDanRedFC ?? this.soundFullCombo[iPlayer], OpenTaiko.Skin.voiceClearFullCombo),
				EndMode.Dan_Red_Perfect => (Dan_Red_PerfectScript, this.soundDanRedPerfect ?? this.soundPerfectCombo[iPlayer], OpenTaiko.Skin.voiceClearAllPerfect),
				EndMode.Dan_Gold_Pass => (Dan_Gold_PassScript, this.soundDanGoldClear ?? this.soundDanRedClear ?? this.soundClear[iPlayer], OpenTaiko.Skin.voiceClearClear),
				EndMode.Dan_Gold_FullCombo => (Dan_Gold_FullComboScript, this.soundDanGoldFC ?? this.soundDanRedFC ?? this.soundFullCombo[iPlayer], OpenTaiko.Skin.voiceClearFullCombo),
				EndMode.Dan_Gold_Perfect => (Dan_Gold_PerfectScript, this.soundDanGoldPerfect ?? this.soundDanRedPerfect ?? this.soundPerfectCombo[iPlayer], OpenTaiko.Skin.voiceClearAllPerfect),

				_ => (null, null, null),
			};

			if (!bSoundPlayed[iPlayer]) {
				int pan = OpenTaiko.ConfigIni.nPanning[OpenTaiko.ConfigIni.nPlayerCount - 1][iPlayer];
				script?.PlayEndAnime(iPlayer);
				sound?.SetPanning(pan);
				sound?.PlayStart();
				voices?[OpenTaiko.GetActualPlayer(iPlayer)]?.SetPanning(pan);
				voices?[OpenTaiko.GetActualPlayer(iPlayer)]?.tPlay();

				bSoundPlayed[iPlayer] = true;
			}

			if (!OpenTaiko.stageGameScreen.bPAUSE)
				script?.Update(iPlayer);
			script?.Draw(iPlayer);

			if (this.ct進行メイン[iPlayer].IsEnded) {
				return 1;
			}
		}

		return 0;
	}

	#region[ private ]
	//-----------------

	public EndAnimeScript FailedScript { get; private set; }
	public EndAnimeScript ClearScript { get; private set; }
	public EndAnimeScript FullComboScript { get; private set; }
	public EndAnimeScript PerfectComboScript { get; private set; }

	public EndAnimeScript AILoseScript { get; private set; }
	public EndAnimeScript AIWinScript { get; private set; }
	public EndAnimeScript AIWin_FullComboScript { get; private set; }
	public EndAnimeScript AIWin_PerfectScript { get; private set; }

	public EndAnimeScript Tower_DropoutScript { get; private set; }
	public EndAnimeScript Tower_TopReached_PassScript { get; private set; }
	public EndAnimeScript Tower_TopReached_FullComboScript { get; private set; }
	public EndAnimeScript Tower_TopReached_PerfectScript { get; private set; }

	public EndAnimeScript Dan_FailScript { get; private set; }
	public EndAnimeScript Dan_Red_PassScript { get; private set; }
	public EndAnimeScript Dan_Red_FullComboScript { get; private set; }
	public EndAnimeScript Dan_Red_PerfectScript { get; private set; }

	public EndAnimeScript Dan_Gold_PassScript { get; private set; }
	public EndAnimeScript Dan_Gold_FullComboScript { get; private set; }
	public EndAnimeScript Dan_Gold_PerfectScript { get; private set; }



	bool b再生済み;
	bool bリザルトボイス再生済み;
	bool[] bSoundPlayed = new bool[OpenTaiko.MAX_PLAYERS];
	CCounter[] ct進行メイン = new CCounter[OpenTaiko.MAX_PLAYERS];

	/*
    CCounter ctEnd_ClearFailed;
    CCounter ctEnd_FullCombo;
    CCounter ctEnd_FullComboLoop;
    CCounter ctEnd_DondaFullCombo;
    CCounter ctEnd_DondaFullComboLoop;
    */

	CCounter ct進行Loop;
	CSound[] soundClear = new CSound[5];
	CSound[] soundFailed = new CSound[5];
	CSound[] soundFullCombo = new CSound[5];
	CSound[] soundPerfectCombo = new CSound[5];

	CSound soundDanFailed;
	CSound soundDanRedClear;
	CSound soundDanRedFC;
	CSound soundDanRedPerfect;
	CSound soundDanGoldClear;
	CSound soundDanGoldFC;
	CSound soundDanGoldPerfect;
	CSound soundTowerDropout;
	CSound soundTowerTopPass;
	CSound soundTowerTopFC;
	CSound soundTowerTopPerfect;

	CSound soundAILose;
	CSound soundAIWin;
	CSound soundAIWinFullCombo;
	CSound soundAIWinPerfectCombo;

	public EndMode[] Mode { get; private set; }
	public enum EndMode {
		StageFailed,
		StageCleared,
		StageFullCombo,
		StagePerfectCombo,

		AI_Lose,
		AI_Win,
		AI_Win_FullCombo,
		AI_Win_Perfect,

		Tower_Dropout,
		Tower_TopReached_Pass,
		Tower_TopReached_FullCombo,
		Tower_TopReached_Perfect,

		Dan_Fail,
		Dan_Red_Pass,
		Dan_Red_FullCombo,
		Dan_Red_Perfect,
		Dan_Gold_Pass,
		Dan_Gold_FullCombo,
		Dan_Gold_Perfect
	}

	//-----------------
	#endregion
}
