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

	public void Start(int iPlayer, bool endOfPlay = false) {
		if (OpenTaiko.ConfigIni.bAIBattleMode && iPlayer == 1) // skip animation for AI
			return;

		var lastMode = this.Mode[iPlayer];

		/*
        this.ctEnd_ClearFailed = new CCounter(0, 69, 30, TJAPlayer3.Timer);
        this.ctEnd_FullCombo = new CCounter(0, 66, 33, TJAPlayer3.Timer);
        this.ctEnd_FullComboLoop = new CCounter(0, 2, 30, TJAPlayer3.Timer);
        this.ctEnd_DondaFullCombo = new CCounter(0, 61, 33, TJAPlayer3.Timer);
        this.ctEnd_DondaFullComboLoop = new CCounter(0, 2, 30, TJAPlayer3.Timer);
        */

		// モードの決定。クリア失敗・フルコンボも事前に作っとく。
		if (OpenTaiko.SongMount.nChoosenSongDifficulty[0] == (int)Difficulty.Tower) {
			if (!(OpenTaiko.stageGameScreen.IsStageFailed(iPlayer) || OpenTaiko.stageGameScreen.IsStageFailed_Fast()) && OpenTaiko.stageGameScreen.FloorManagement.CurrentNumberOfLives > 0) {
				if (OpenTaiko.stageGameScreen.CChartScore[0].nMiss == 0 && OpenTaiko.stageGameScreen.CChartScore[0].nMine == 0) {
					if (OpenTaiko.stageGameScreen.CChartScore[0].nGood == 0)
						this.Mode[0] = EndMode.Tower_TopReached_Perfect;
					else
						this.Mode[0] = EndMode.Tower_TopReached_FullCombo;
				} else
					this.Mode[0] = EndMode.Tower_TopReached_Pass;
			} else
				this.Mode[0] = EndMode.Tower_Dropout;
		} else if (OpenTaiko.SongMount.nChoosenSongDifficulty[0] == (int)Difficulty.Dan) {
			// 段位認定モード。
			if (!(OpenTaiko.stageGameScreen.IsStageFailed(iPlayer) || OpenTaiko.stageGameScreen.IsStageFailed_Fast())
				&& !OpenTaiko.stageGameScreen.actDan.GetFailedAllChallenges(OpenTaiko.SongMount.rChoosenSong.DanSongs)
				) {
				// 段位認定モード、クリア成功
				// this.Mode[0] = EndMode.StageCleared;
				if (!endOfPlay) {
					this.Mode[0] = EndMode.Total; // cancel mis-judged failed
				} else {
					bool bgold = OpenTaiko.stageGameScreen.actDan.GetResultExamStatus(OpenTaiko.stageGameScreen.actDan.GetExam(), OpenTaiko.SongMount.rChoosenSong.DanSongs) == Exam.Status.Better_Success;

					if (OpenTaiko.stageGameScreen.CChartScore[0].nMiss == 0 && OpenTaiko.stageGameScreen.CChartScore[0].nMine == 0) {
						if (OpenTaiko.stageGameScreen.CChartScore[0].nGood == 0)
							this.Mode[0] = bgold ? EndMode.Dan_Gold_Perfect : EndMode.Dan_Red_Perfect;
						else
							this.Mode[0] = bgold ? EndMode.Dan_Gold_FullCombo : EndMode.Dan_Red_FullCombo;
					} else
						this.Mode[0] = bgold ? EndMode.Dan_Gold_Pass : EndMode.Dan_Red_Pass;
				}
			} else {
				// 段位認定モード、クリア失敗
				this.Mode[0] = EndMode.Dan_Fail;
			}
		} else if (OpenTaiko.ConfigIni.bAIBattleMode) {
			if (!(OpenTaiko.stageGameScreen.IsStageFailed(iPlayer) || OpenTaiko.stageGameScreen.IsStageFailed_Fast()) && OpenTaiko.stageGameScreen.bIsAIBattleWin) {
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
			if (!(OpenTaiko.stageGameScreen.IsStageFailed(iPlayer) || OpenTaiko.stageGameScreen.IsStageFailed_Fast()) && HGaugeMethods.UNSAFE_FastNormaCheck(iPlayer)) {
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

		if (this.Mode[iPlayer] == lastMode) // prevent replaying animation
			return;

		if (this.Mode[iPlayer] == EndMode.Total)
			this.ctProgressMain[iPlayer] = null;
		else
			this.ctProgressMain[iPlayer] ??= new CCounter(0, 300, 22, OpenTaiko.Timer);
		bSoundPlayed[iPlayer] = false;
	}

	public override void Activate() {
		this.Mode = new EndMode[5];

		var origindir = CSkin.Path($"{TextureLoader.BASE}{TextureLoader.GAME}{TextureLoader.END}");

		// lazy load
		if (OpenTaiko.SongMount.nChoosenSongDifficulty[0] == (int)Difficulty.Tower) {
			Tower_DropoutScript ??= new LuaBackgroundWrapper($@"{origindir}Tower_Dropout", $@"{origindir}ClearFailed");
			Tower_TopReached_PassScript ??= new LuaBackgroundWrapper($@"{origindir}Tower_TopReached_Pass", $@"{origindir}Clear");
			Tower_TopReached_FullComboScript ??= new LuaBackgroundWrapper($@"{origindir}Tower_TopReached_FullCombo", $@"{origindir}FullCombo");
			Tower_TopReached_PerfectScript ??= new LuaBackgroundWrapper($@"{origindir}Tower_TopReached_Perfect", $@"{origindir}AllPerfect");

			this.soundTowerDropout ??= OpenTaiko.SoundManager.tCreateSound(CSkin.Path(@$"Sounds{Path.DirectorySeparatorChar}Tower{Path.DirectorySeparatorChar}Tower_Dropout.ogg"), ESoundGroup.SoundEffect);
			this.soundTowerTopPass ??= OpenTaiko.SoundManager.tCreateSound(CSkin.Path(@$"Sounds{Path.DirectorySeparatorChar}Tower{Path.DirectorySeparatorChar}Tower_TopReached_Pass.ogg"), ESoundGroup.SoundEffect);
			this.soundTowerTopFC ??= OpenTaiko.SoundManager.tCreateSound(CSkin.Path(@$"Sounds{Path.DirectorySeparatorChar}Tower{Path.DirectorySeparatorChar}Tower_TopReached_FullCombo.ogg"), ESoundGroup.SoundEffect);
			this.soundTowerTopPerfect ??= OpenTaiko.SoundManager.tCreateSound(CSkin.Path(@$"Sounds{Path.DirectorySeparatorChar}Tower{Path.DirectorySeparatorChar}Tower_TopReached_Perfect.ogg"), ESoundGroup.SoundEffect);
		} else if (OpenTaiko.SongMount.nChoosenSongDifficulty[0] == (int)Difficulty.Dan) {
			Dan_FailScript ??= new LuaBackgroundWrapper($@"{origindir}Dan_Fail", $@"{origindir}ClearFailed");
			Dan_Red_PassScript ??= new LuaBackgroundWrapper($@"{origindir}Dan_Red_Pass", $@"{origindir}Clear");
			Dan_Red_FullComboScript ??= new LuaBackgroundWrapper($@"{origindir}Dan_Red_FullCombo", $@"{origindir}FullCombo");
			Dan_Red_PerfectScript ??= new LuaBackgroundWrapper($@"{origindir}Dan_Red_Perfect", $@"{origindir}AllPerfect");
			Dan_Gold_PassScript ??= new LuaBackgroundWrapper($@"{origindir}Dan_Gold_Pass", $@"{origindir}Dan_Red_Pass", $@"{origindir}Clear");
			Dan_Gold_FullComboScript ??= new LuaBackgroundWrapper($@"{origindir}Dan_Gold_FullCombo", $@"{origindir}Dan_Red_FullCombo", $@"{origindir}FullCombo");
			Dan_Gold_PerfectScript ??= new LuaBackgroundWrapper($@"{origindir}Dan_Gold_Perfect", $@"{origindir}Dan_Red_Perfect", $@"{origindir}AllPerfect");

			this.soundDanFailed ??= OpenTaiko.SoundManager.tCreateSound(CSkin.Path(@$"Sounds{Path.DirectorySeparatorChar}Dan{Path.DirectorySeparatorChar}Dan_Fail.ogg"), ESoundGroup.SoundEffect);
			this.soundDanRedClear ??= OpenTaiko.SoundManager.tCreateSound(CSkin.Path(@$"Sounds{Path.DirectorySeparatorChar}Dan{Path.DirectorySeparatorChar}Dan_Red_Pass.ogg"), ESoundGroup.SoundEffect);
			this.soundDanRedFC ??= OpenTaiko.SoundManager.tCreateSound(CSkin.Path(@$"Sounds{Path.DirectorySeparatorChar}Dan{Path.DirectorySeparatorChar}Dan_Red_FullCombo.ogg"), ESoundGroup.SoundEffect);
			this.soundDanRedPerfect ??= OpenTaiko.SoundManager.tCreateSound(CSkin.Path(@$"Sounds{Path.DirectorySeparatorChar}Dan{Path.DirectorySeparatorChar}Dan_Red_Perfect.ogg"), ESoundGroup.SoundEffect);
			this.soundDanGoldClear ??= OpenTaiko.SoundManager.tCreateSound(CSkin.Path(@$"Sounds{Path.DirectorySeparatorChar}Dan{Path.DirectorySeparatorChar}Dan_Gold_Pass.ogg"), ESoundGroup.SoundEffect);
			this.soundDanGoldFC ??= OpenTaiko.SoundManager.tCreateSound(CSkin.Path(@$"Sounds{Path.DirectorySeparatorChar}Dan{Path.DirectorySeparatorChar}Dan_Gold_FullCombo.ogg"), ESoundGroup.SoundEffect);
			this.soundDanGoldPerfect ??= OpenTaiko.SoundManager.tCreateSound(CSkin.Path(@$"Sounds{Path.DirectorySeparatorChar}Dan{Path.DirectorySeparatorChar}Dan_Gold_Perfect.ogg"), ESoundGroup.SoundEffect);

		} else if (OpenTaiko.ConfigIni.bAIBattleMode) {
			AILoseScript ??= new LuaBackgroundWrapper($@"{origindir}AI_Lose", $@"{origindir}ClearFailed");
			AIWinScript ??= new LuaBackgroundWrapper($@"{origindir}AI_Win", $@"{origindir}Clear");
			AIWin_FullComboScript ??= new LuaBackgroundWrapper($@"{origindir}AI_Win_FullCombo", $@"{origindir}FullCombo");
			AIWin_PerfectScript ??= new LuaBackgroundWrapper($@"{origindir}AI_Win_Perfect", $@"{origindir}AllPerfect");

			this.soundAILose ??= OpenTaiko.SoundManager.tCreateSound(CSkin.Path(@$"Sounds{Path.DirectorySeparatorChar}AIBattle_Lose.ogg"), ESoundGroup.SoundEffect);
			this.soundAIWin ??= OpenTaiko.SoundManager.tCreateSound(CSkin.Path(@$"Sounds{Path.DirectorySeparatorChar}AIBattle_Win.ogg"), ESoundGroup.SoundEffect);
			this.soundAIWinFullCombo ??= OpenTaiko.SoundManager.tCreateSound(CSkin.Path(@$"Sounds{Path.DirectorySeparatorChar}AIBattle_Win_FullCombo.ogg"), ESoundGroup.SoundEffect);
			this.soundAIWinPerfectCombo ??= OpenTaiko.SoundManager.tCreateSound(CSkin.Path(@$"Sounds{Path.DirectorySeparatorChar}AIBattle_Win_AllPerfect.ogg"), ESoundGroup.SoundEffect);
		} else {
			FailedScript ??= new LuaBackgroundWrapper($@"{origindir}ClearFailed");//ClearFailed
			ClearScript ??= new LuaBackgroundWrapper($@"{origindir}Clear");
			FullComboScript ??= new LuaBackgroundWrapper($@"{origindir}FullCombo");
			PerfectComboScript ??= new LuaBackgroundWrapper($@"{origindir}AllPerfect");
		}

		for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
			this.soundClear[i] ??= OpenTaiko.SoundManager.tCreateSound(CSkin.Path(@$"Sounds{Path.DirectorySeparatorChar}Clear.ogg"), ESoundGroup.SoundEffect);
			this.soundFailed[i] ??= OpenTaiko.SoundManager.tCreateSound(CSkin.Path(@$"Sounds{Path.DirectorySeparatorChar}Failed.ogg"), ESoundGroup.SoundEffect);
			this.soundFullCombo[i] ??= OpenTaiko.SoundManager.tCreateSound(CSkin.Path(@$"Sounds{Path.DirectorySeparatorChar}FullCombo.ogg"), ESoundGroup.SoundEffect);
			this.soundPerfectCombo[i] ??= OpenTaiko.SoundManager.tCreateSound(CSkin.Path(@$"Sounds{Path.DirectorySeparatorChar}AllPerfect.ogg"), ESoundGroup.SoundEffect);
		}

		this.InitScripts();

		base.Activate();
	}

	public void InitScripts() {
		_state.RefreshConst();
		for (int i = 0; i < OpenTaiko.MAX_PLAYERS; ++i) {
			this.Mode[i] = EndMode.Total;
			this.bSoundPlayed[i] = false;
			this.ctProgressMain[i] = null;
		}
		for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
			this.soundFailed[i]?.tStopSound();
			this.soundClear[i]?.tStopSound();
			this.soundFullCombo[i]?.tStopSound();
			this.soundPerfectCombo[i]?.tStopSound();
		}

		if (OpenTaiko.SongMount.nChoosenSongDifficulty[0] == (int)Difficulty.Tower) {
			Tower_DropoutScript?.Activate(_state);
			Tower_TopReached_PassScript?.Activate(_state);
			Tower_TopReached_FullComboScript?.Activate(_state);
			Tower_TopReached_PerfectScript?.Activate(_state);
		} else if (OpenTaiko.SongMount.nChoosenSongDifficulty[0] == (int)Difficulty.Dan) {
			Dan_FailScript?.Activate(_state);
			Dan_Red_PassScript?.Activate(_state);
			Dan_Red_FullComboScript?.Activate(_state);
			Dan_Red_PerfectScript?.Activate(_state);
			Dan_Gold_PassScript?.Activate(_state);
			Dan_Gold_FullComboScript?.Activate(_state);
			Dan_Gold_PerfectScript?.Activate(_state);
		} else if (OpenTaiko.ConfigIni.bAIBattleMode) {
			AILoseScript?.Activate(_state);
			AIWinScript?.Activate(_state);
			AIWin_FullComboScript?.Activate(_state);
			AIWin_PerfectScript?.Activate(_state);
		} else {
			FailedScript?.Activate(_state);
			ClearScript?.Activate(_state);
			FullComboScript?.Activate(_state);
			PerfectComboScript?.Activate(_state);
		}
	}

	public override void DeActivate() {
		for (int i = 0; i < OpenTaiko.MAX_PLAYERS; ++i)
			this.ctProgressMain[i] = null;

		this.ReleaseManagedResource(true);

		for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
			CCharacter.RemoveEssentialVoice(i, CCharacter.VOICE_END_FAILED);
			CCharacter.RemoveEssentialVoice(i, CCharacter.VOICE_END_CLEAR);
			CCharacter.RemoveEssentialVoice(i, CCharacter.VOICE_END_FULLCOMBO);
			CCharacter.RemoveEssentialVoice(i, CCharacter.VOICE_END_ALLPERFECT);
		}

		base.DeActivate();
	}

	public override void ReleaseManagedResource() => ReleaseManagedResource(false);

	public void ReleaseManagedResource(bool keepForNowGameMode) {
		if (!(keepForNowGameMode && OpenTaiko.SongMount.nChoosenSongDifficulty[0] == (int)Difficulty.Tower)) {
			this.soundTowerDropout?.tDispose(); this.soundTowerDropout = null;
			this.soundTowerTopPass?.tDispose(); this.soundTowerTopPass = null;
			this.soundTowerTopFC?.tDispose(); this.soundTowerTopFC = null;
			this.soundTowerTopPerfect?.tDispose(); this.soundTowerTopPerfect = null;

			Tower_DropoutScript?.Dispose(); Tower_DropoutScript = null;
			Tower_TopReached_PassScript?.Dispose(); Tower_TopReached_PassScript = null;
			Tower_TopReached_FullComboScript?.Dispose(); Tower_TopReached_FullComboScript = null;
			Tower_TopReached_PerfectScript?.Dispose(); Tower_TopReached_PerfectScript = null;
		}

		if (!(keepForNowGameMode && OpenTaiko.SongMount.nChoosenSongDifficulty[0] == (int)Difficulty.Dan)) {
			this.soundDanFailed?.tDispose(); this.soundDanFailed = null;
			this.soundDanRedClear?.tDispose(); this.soundDanRedClear = null;
			this.soundDanRedFC?.tDispose(); this.soundDanRedFC = null;
			this.soundDanRedPerfect?.tDispose(); this.soundDanRedPerfect = null;
			this.soundDanGoldClear?.tDispose(); this.soundDanGoldClear = null;
			this.soundDanGoldFC?.tDispose(); this.soundDanGoldFC = null;
			this.soundDanGoldPerfect?.tDispose(); this.soundDanGoldPerfect = null;

			Dan_FailScript?.Dispose(); Dan_FailScript = null;
			Dan_Red_PassScript?.Dispose(); Dan_Red_PassScript = null;
			Dan_Red_FullComboScript?.Dispose(); Dan_Red_FullComboScript = null;
			Dan_Red_PerfectScript?.Dispose(); Dan_Red_PerfectScript = null;
			Dan_Gold_PassScript?.Dispose(); Dan_Gold_PassScript = null;
			Dan_Gold_FullComboScript?.Dispose(); Dan_Gold_FullComboScript = null;
			Dan_Gold_PerfectScript?.Dispose(); Dan_Gold_PerfectScript = null;
		}

		if (!(keepForNowGameMode && OpenTaiko.ConfigIni.bAIBattleMode)) {
			this.soundAILose?.tDispose(); this.soundAILose = null;
			this.soundAIWin?.tDispose(); this.soundAIWin = null;
			this.soundAIWinFullCombo?.tDispose(); this.soundAIWinFullCombo = null;
			this.soundAIWinPerfectCombo?.tDispose(); this.soundAIWinPerfectCombo = null;
			AILoseScript?.Dispose(); AILoseScript = null;
			AIWinScript?.Dispose(); AIWinScript = null;
			AIWin_FullComboScript?.Dispose(); AIWin_FullComboScript = null;
			AIWin_PerfectScript?.Dispose(); AIWin_PerfectScript = null;
		}

		if (!keepForNowGameMode) {
			for (int i = 0; i < OpenTaiko.MAX_PLAYERS; i++) {
				this.soundClear[i]?.tDispose(); this.soundClear[i] = null;
				this.soundFailed[i]?.tDispose(); this.soundFailed[i] = null;
				this.soundFullCombo[i]?.tDispose(); this.soundFullCombo[i] = null;
				this.soundPerfectCombo[i]?.tDispose(); this.soundPerfectCombo[i] = null;
			}

			FailedScript?.Dispose(); FailedScript = null;
			ClearScript?.Dispose(); ClearScript = null;
			FullComboScript?.Dispose(); FullComboScript = null;
			PerfectComboScript?.Dispose(); PerfectComboScript = null;
		}

		base.ReleaseManagedResource();
	}


	public override int Draw() {
		if (base.IsFirstDraw) {
			base.IsFirstDraw = false;
		}
		int ret = 1;
		int nDrawnPlayers = OpenTaiko.ConfigIni.bAIBattleMode ? 1 : OpenTaiko.ConfigIni.nPlayerCount;
		for (int i = 0; i < nDrawnPlayers; ++i)
			if (this.Draw(i, nDrawnPlayers) == 0)
				ret = 0;
		return ret;
	}
	protected int Draw(int iPlayer, int nDrawnPlayers) {
		if (this.ctProgressMain[iPlayer] != null) {
			bool playerStageFailed = OpenTaiko.stageGameScreen.IsStageFailed(iPlayer);
			if (!((playerStageFailed && !OpenTaiko.ConfigIni.bAIBattleMode) || OpenTaiko.stageGameScreen.IsStageFailed_Fast() || OpenTaiko.stageGameScreen.IsStageCompleted()))
				return 0;

			this.ctProgressMain[iPlayer].Tick();

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
				int pan = OpenTaiko.ConfigIni.nPanning[nDrawnPlayers - 1][iPlayer];
				script?.Call("playEndAnime", iPlayer);
				sound?.SetPanning(pan);
				sound?.PlayStart();
				voices?[iPlayer]?.SetPanning(pan);
				voices?[iPlayer]?.tPlay();

				bSoundPlayed[iPlayer] = true;
			}

			_state.player = iPlayer;
			_state.RefreshGameplay();
			if (!OpenTaiko.stageGameScreen.bPAUSE)
				script?.Update(_state);
			script?.Draw(_state);

			if (this.ctProgressMain[iPlayer].IsEnded) {
				return 1;
			}
		}

		return 0;
	}

	#region[ private ]
	//-----------------

	public LuaBackgroundWrapper FailedScript { get; private set; }
	public LuaBackgroundWrapper ClearScript { get; private set; }
	public LuaBackgroundWrapper FullComboScript { get; private set; }
	public LuaBackgroundWrapper PerfectComboScript { get; private set; }

	public LuaBackgroundWrapper AILoseScript { get; private set; }
	public LuaBackgroundWrapper AIWinScript { get; private set; }
	public LuaBackgroundWrapper AIWin_FullComboScript { get; private set; }
	public LuaBackgroundWrapper AIWin_PerfectScript { get; private set; }

	public LuaBackgroundWrapper Tower_DropoutScript { get; private set; }
	public LuaBackgroundWrapper Tower_TopReached_PassScript { get; private set; }
	public LuaBackgroundWrapper Tower_TopReached_FullComboScript { get; private set; }
	public LuaBackgroundWrapper Tower_TopReached_PerfectScript { get; private set; }

	public LuaBackgroundWrapper Dan_FailScript { get; private set; }
	public LuaBackgroundWrapper Dan_Red_PassScript { get; private set; }
	public LuaBackgroundWrapper Dan_Red_FullComboScript { get; private set; }
	public LuaBackgroundWrapper Dan_Red_PerfectScript { get; private set; }

	public LuaBackgroundWrapper Dan_Gold_PassScript { get; private set; }
	public LuaBackgroundWrapper Dan_Gold_FullComboScript { get; private set; }
	public LuaBackgroundWrapper Dan_Gold_PerfectScript { get; private set; }

	bool[] bSoundPlayed = new bool[OpenTaiko.MAX_PLAYERS];
	CCounter[] ctProgressMain = new CCounter[OpenTaiko.MAX_PLAYERS];
	private readonly LuaBackgroundState _state = new();

	/*
    CCounter ctEnd_ClearFailed;
    CCounter ctEnd_FullCombo;
    CCounter ctEnd_FullComboLoop;
    CCounter ctEnd_DondaFullCombo;
    CCounter ctEnd_DondaFullComboLoop;
    */

	CCounter ctProgressLoop;
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
		Dan_Gold_Perfect,

		Total,
	}

	//-----------------
	#endregion
}
