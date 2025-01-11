﻿using FDK;

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

	public void Start() {
		// this.ct進行メイン = new CCounter(0, 500, 1000 / 60, TJAPlayer3.Timer);

		bSongsPlayed = false;

		this.ct進行メイン = new CCounter(0, 300, 22, OpenTaiko.Timer);

		/*
        this.ctEnd_ClearFailed = new CCounter(0, 69, 30, TJAPlayer3.Timer);
        this.ctEnd_FullCombo = new CCounter(0, 66, 33, TJAPlayer3.Timer);
        this.ctEnd_FullComboLoop = new CCounter(0, 2, 30, TJAPlayer3.Timer);
        this.ctEnd_DondaFullCombo = new CCounter(0, 61, 33, TJAPlayer3.Timer);
        this.ctEnd_DondaFullComboLoop = new CCounter(0, 2, 30, TJAPlayer3.Timer);
        */

		// モードの決定。クリア失敗・フルコンボも事前に作っとく。
		if (OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Tower) {
			if (CFloorManagement.CurrentNumberOfLives > 0) {
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
			if (!OpenTaiko.stageGameScreen.actDan.GetFailedAllChallenges()) {
				// 段位認定モード、クリア成功
				// this.Mode[0] = EndMode.StageCleared;

				bool bgold = OpenTaiko.stageGameScreen.actDan.GetExamStatus(OpenTaiko.stageResults.st演奏記録.Drums.Dan_C) == Exam.Status.Better_Success;

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
			if (OpenTaiko.stageGameScreen.bIsAIBattleWin) {
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
			for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
				if (HGaugeMethods.UNSAFE_FastNormaCheck(i)) {
					if (OpenTaiko.stageGameScreen.CChartScore[i].nMiss == 0 && OpenTaiko.stageGameScreen.CChartScore[i].nMine == 0)
					//if (TJAPlayer3.stage演奏ドラム画面.nヒット数_Auto含まない.Drums.Miss == 0)
					{
						if (OpenTaiko.stageGameScreen.CChartScore[i].nGood == 0)
						//if (TJAPlayer3.stage演奏ドラム画面.nヒット数_Auto含まない.Drums.Great == 0)
						{
							this.Mode[i] = EndMode.StagePerfectCombo;
						} else {
							this.Mode[i] = EndMode.StageFullCombo;
						}
					} else {
						this.Mode[i] = EndMode.StageCleared;
					}
				} else {
					this.Mode[i] = EndMode.StageFailed;
				}
			}
		}
	}

	public override void Activate() {
		this.bリザルトボイス再生済み = false;
		this.Mode = new EndMode[5];

		var origindir = CSkin.Path($"{TextureLoader.BASE}{TextureLoader.GAME}{TextureLoader.END}");


		if (OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Tower) {
			Tower_DropoutScript = new EndAnimeScript($@"{origindir}Tower_Dropout{Path.DirectorySeparatorChar}Script.lua");
			Tower_DropoutScript.Init();

			Tower_TopReached_PassScript = new EndAnimeScript($@"{origindir}Tower_TopReached_Pass{Path.DirectorySeparatorChar}Script.lua");
			Tower_TopReached_PassScript.Init();

			Tower_TopReached_FullComboScript = new EndAnimeScript($@"{origindir}Tower_TopReached_FullCombo{Path.DirectorySeparatorChar}Script.lua");
			Tower_TopReached_FullComboScript.Init();

			Tower_TopReached_PerfectScript = new EndAnimeScript($@"{origindir}Tower_TopReached_Perfect{Path.DirectorySeparatorChar}Script.lua");
			Tower_TopReached_PerfectScript.Init();

			this.soundTowerDropout = OpenTaiko.SoundManager.tCreateSound(CSkin.Path(@$"Sounds{Path.DirectorySeparatorChar}Tower{Path.DirectorySeparatorChar}Tower_Dropout.ogg"), ESoundGroup.SoundEffect);
			this.soundTowerTopPass = OpenTaiko.SoundManager.tCreateSound(CSkin.Path(@$"Sounds{Path.DirectorySeparatorChar}Tower{Path.DirectorySeparatorChar}Tower_TopReached_Pass.ogg"), ESoundGroup.SoundEffect);
			this.soundTowerTopFC = OpenTaiko.SoundManager.tCreateSound(CSkin.Path(@$"Sounds{Path.DirectorySeparatorChar}Tower{Path.DirectorySeparatorChar}Tower_TopReached_FullCombo.ogg"), ESoundGroup.SoundEffect);
			this.soundTowerTopPerfect = OpenTaiko.SoundManager.tCreateSound(CSkin.Path(@$"Sounds{Path.DirectorySeparatorChar}Tower{Path.DirectorySeparatorChar}Tower_TopReached_Perfect.ogg"), ESoundGroup.SoundEffect);
		} else if (OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Dan) {
			Dan_FailScript = new EndAnimeScript($@"{origindir}Dan_Fail{Path.DirectorySeparatorChar}Script.lua");
			Dan_FailScript.Init();

			Dan_Red_PassScript = new EndAnimeScript($@"{origindir}Dan_Red_Pass{Path.DirectorySeparatorChar}Script.lua");
			Dan_Red_PassScript.Init();

			Dan_Red_FullComboScript = new EndAnimeScript($@"{origindir}Dan_Red_FullCombo{Path.DirectorySeparatorChar}Script.lua");
			Dan_Red_FullComboScript.Init();

			Dan_Red_PerfectScript = new EndAnimeScript($@"{origindir}Dan_Red_Perfect{Path.DirectorySeparatorChar}Script.lua");
			Dan_Red_PerfectScript.Init();

			Dan_Gold_PassScript = new EndAnimeScript($@"{origindir}Dan_Gold_Pass{Path.DirectorySeparatorChar}Script.lua");
			Dan_Gold_PassScript.Init();

			Dan_Gold_FullComboScript = new EndAnimeScript($@"{origindir}Dan_Gold_FullCombo{Path.DirectorySeparatorChar}Script.lua");
			Dan_Gold_FullComboScript.Init();

			Dan_Gold_PerfectScript = new EndAnimeScript($@"{origindir}Dan_Gold_Perfect{Path.DirectorySeparatorChar}Script.lua");
			Dan_Gold_PerfectScript.Init();

			this.soundDanFailed = OpenTaiko.SoundManager.tCreateSound(CSkin.Path(@$"Sounds{Path.DirectorySeparatorChar}Dan{Path.DirectorySeparatorChar}Dan_Fail.ogg"), ESoundGroup.SoundEffect);
			this.soundDanRedClear = OpenTaiko.SoundManager.tCreateSound(CSkin.Path(@$"Sounds{Path.DirectorySeparatorChar}Dan{Path.DirectorySeparatorChar}Dan_Red_Pass.ogg"), ESoundGroup.SoundEffect);
			this.soundDanRedFC = OpenTaiko.SoundManager.tCreateSound(CSkin.Path(@$"Sounds{Path.DirectorySeparatorChar}Dan{Path.DirectorySeparatorChar}Dan_Red_FullCombo.ogg"), ESoundGroup.SoundEffect);
			this.soundDanRedPerfect = OpenTaiko.SoundManager.tCreateSound(CSkin.Path(@$"Sounds{Path.DirectorySeparatorChar}Dan{Path.DirectorySeparatorChar}Dan_Red_Perfect.ogg"), ESoundGroup.SoundEffect);
			this.soundDanGoldClear = OpenTaiko.SoundManager.tCreateSound(CSkin.Path(@$"Sounds{Path.DirectorySeparatorChar}Dan{Path.DirectorySeparatorChar}Dan_Gold_Pass.ogg"), ESoundGroup.SoundEffect);
			this.soundDanGoldFC = OpenTaiko.SoundManager.tCreateSound(CSkin.Path(@$"Sounds{Path.DirectorySeparatorChar}Dan{Path.DirectorySeparatorChar}Dan_Gold_FullCombo.ogg"), ESoundGroup.SoundEffect);
			this.soundDanGoldPerfect = OpenTaiko.SoundManager.tCreateSound(CSkin.Path(@$"Sounds{Path.DirectorySeparatorChar}Dan{Path.DirectorySeparatorChar}Dan_Gold_Perfect.ogg"), ESoundGroup.SoundEffect);

		} else if (OpenTaiko.ConfigIni.bAIBattleMode) {
			AILoseScript = new EndAnimeScript($@"{origindir}AI_Lose{Path.DirectorySeparatorChar}Script.lua");
			AILoseScript.Init();

			AIWinScript = new EndAnimeScript($@"{origindir}AI_Win{Path.DirectorySeparatorChar}Script.lua");
			AIWinScript.Init();

			AIWin_FullComboScript = new EndAnimeScript($@"{origindir}AI_Win_FullCombo{Path.DirectorySeparatorChar}Script.lua");
			AIWin_FullComboScript.Init();

			AIWin_PerfectScript = new EndAnimeScript($@"{origindir}AI_Win_Perfect{Path.DirectorySeparatorChar}Script.lua");
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
		this.ct進行メイン = null;

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

	#region [effects]
	// ------------------------------------
	private void showEndEffect_Failed(int i) {
		if (!OpenTaiko.stageGameScreen.bPAUSE) FailedScript.Update(i);
		FailedScript.Draw(i);

		int[] y = new int[] { 0, 176 };

		/*

        this.ctEnd_ClearFailed.t進行();
        if (this.ctEnd_ClearFailed.n現在の値 <= 20 || TJAPlayer3.Tx.ClearFailed == null)
        {
            TJAPlayer3.Tx.End_ClearFailed[Math.Min(this.ctEnd_ClearFailed.n現在の値, TJAPlayer3.Tx.End_ClearFailed.Length - 1)]?.t2D描画(505, y[i] + 145);
        }
        else if (this.ctEnd_ClearFailed.n現在の値 >= 20 && this.ctEnd_ClearFailed.n現在の値 <= 67)
        {
            TJAPlayer3.Tx.ClearFailed?.t2D描画(502, y[i] + 192);
        }
        else if (this.ctEnd_ClearFailed.n現在の値 == 68)
        {
            TJAPlayer3.Tx.ClearFailed1?.t2D描画(502, y[i] + 192);
        }
        else if (this.ctEnd_ClearFailed.n現在の値 >= 69)
        {
            TJAPlayer3.Tx.ClearFailed2?.t2D描画(502, y[i] + 192);
        }
        */
	}
	private void showEndEffect_Clear(int i) {
		if (!OpenTaiko.stageGameScreen.bPAUSE) ClearScript.Update(i);
		ClearScript.Draw(i);

		/*
        int[] y = new int[] { 210, 386 };
        #region[ 文字 ]
        //登場アニメは20フレーム。うち最初の5フレームは半透過状態。
        float[] f文字拡大率 = new float[] { 1.04f, 1.11f, 1.15f, 1.19f, 1.23f, 1.26f, 1.30f, 1.31f, 1.32f, 1.32f, 1.32f, 1.30f, 1.30f, 1.26f, 1.25f, 1.19f, 1.15f, 1.11f, 1.05f, 1.0f };
        int[] n透明度 = new int[] { 43, 85, 128, 170, 213, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255 };

        if (TJAPlayer3.Tx.End_Clear_Text_ != null)
        {
            if (this.ct進行メイン.n現在の値 >= 17)
            {
                if (this.ct進行メイン.n現在の値 <= 36)
                {
                    TJAPlayer3.Tx.End_Clear_Text_.vc拡大縮小倍率.Y = f文字拡大率[this.ct進行メイン.n現在の値 - 17];
                    TJAPlayer3.Tx.End_Clear_Text_.Opacity = n透明度[this.ct進行メイン.n現在の値 - 17];
                    TJAPlayer3.Tx.End_Clear_Text_.t2D描画(634, (int)(y[i] - ((90 * f文字拡大率[this.ct進行メイン.n現在の値 - 17]) - 90)), new Rectangle(0, 0, 90, 90));
                }
                else
                {
                    TJAPlayer3.Tx.End_Clear_Text_.vc拡大縮小倍率.Y = 1.0f;
                    TJAPlayer3.Tx.End_Clear_Text_.t2D描画(634, y[i], new Rectangle(0, 0, 90, 90));
                }
            }
            if (this.ct進行メイン.n現在の値 >= 19)
            {
                if (this.ct進行メイン.n現在の値 <= 38)
                {
                    TJAPlayer3.Tx.End_Clear_Text_.vc拡大縮小倍率.Y = f文字拡大率[this.ct進行メイン.n現在の値 - 19];
                    TJAPlayer3.Tx.End_Clear_Text_.Opacity = n透明度[this.ct進行メイン.n現在の値 - 19];
                    TJAPlayer3.Tx.End_Clear_Text_.t2D描画(692, (int)(y[i] - ((90 * f文字拡大率[this.ct進行メイン.n現在の値 - 19]) - 90)), new Rectangle(90, 0, 90, 90));
                }
                else
                {
                    TJAPlayer3.Tx.End_Clear_Text_.vc拡大縮小倍率.Y = 1.0f;
                    TJAPlayer3.Tx.End_Clear_Text_.t2D描画(692, y[i], new Rectangle(90, 0, 90, 90));
                }
            }
            TJAPlayer3.Tx.End_Clear_Text_.vc拡大縮小倍率.Y = 1.0f;
            if (this.ct進行メイン.n現在の値 >= 21)
            {
                if (this.ct進行メイン.n現在の値 <= 40)
                {
                    TJAPlayer3.Tx.End_Clear_Text_.vc拡大縮小倍率.Y = f文字拡大率[this.ct進行メイン.n現在の値 - 21];
                    TJAPlayer3.Tx.End_Clear_Text_.Opacity = n透明度[this.ct進行メイン.n現在の値 - 21];
                    TJAPlayer3.Tx.End_Clear_Text_.t2D描画(750, y[i] - (int)((90 * f文字拡大率[this.ct進行メイン.n現在の値 - 21]) - 90), new Rectangle(180, 0, 90, 90));
                }
                else
                {
                    TJAPlayer3.Tx.End_Clear_Text_.vc拡大縮小倍率.Y = 1.0f;
                    TJAPlayer3.Tx.End_Clear_Text_.t2D描画(750, y[i], new Rectangle(180, 0, 90, 90));
                }
            }
            if (this.ct進行メイン.n現在の値 >= 23)
            {
                if (this.ct進行メイン.n現在の値 <= 42)
                {
                    TJAPlayer3.Tx.End_Clear_Text_.vc拡大縮小倍率.Y = f文字拡大率[this.ct進行メイン.n現在の値 - 23];
                    TJAPlayer3.Tx.End_Clear_Text_.Opacity = n透明度[this.ct進行メイン.n現在の値 - 23];
                    TJAPlayer3.Tx.End_Clear_Text_.t2D描画(819, y[i] - (int)((90 * f文字拡大率[this.ct進行メイン.n現在の値 - 23]) - 90), new Rectangle(270, 0, 90, 90));
                }
                else
                {
                    TJAPlayer3.Tx.End_Clear_Text_.vc拡大縮小倍率.Y = 1.0f;
                    TJAPlayer3.Tx.End_Clear_Text_.t2D描画(819, y[i], new Rectangle(270, 0, 90, 90));
                }
            }
            if (this.ct進行メイン.n現在の値 >= 25)
            {
                if (this.ct進行メイン.n現在の値 <= 44)
                {
                    TJAPlayer3.Tx.End_Clear_Text_.vc拡大縮小倍率.Y = f文字拡大率[this.ct進行メイン.n現在の値 - 25];
                    TJAPlayer3.Tx.End_Clear_Text_.Opacity = n透明度[this.ct進行メイン.n現在の値 - 25];
                    TJAPlayer3.Tx.End_Clear_Text_.t2D描画(890, (y[i] + 2) - (int)((90 * f文字拡大率[this.ct進行メイン.n現在の値 - 25]) - 90), new Rectangle(360, 0, 90, 90));
                }
                else
                {
                    TJAPlayer3.Tx.End_Clear_Text_.vc拡大縮小倍率.Y = 1.0f;
                    TJAPlayer3.Tx.End_Clear_Text_.t2D描画(890, y[i] + 2, new Rectangle(360, 0, 90, 90));
                }
            }
            if (this.ct進行メイン.n現在の値 >= 50 && this.ct進行メイン.n現在の値 < 90)
            {
                if (TJAPlayer3.Tx.End_Clear_Text_Effect != null)
                {
                    if (this.ct進行メイン.n現在の値 < 70)
                    {
                        TJAPlayer3.Tx.End_Clear_Text_Effect.Opacity = (this.ct進行メイン.n現在の値 - 50) * (255 / 20);
                        TJAPlayer3.Tx.End_Clear_Text_Effect.t2D描画(634, y[i] - 2);
                    }
                    else
                    {
                        TJAPlayer3.Tx.End_Clear_Text_Effect.Opacity = 255 - ((this.ct進行メイン.n現在の値 - 70) * (255 / 20));
                        TJAPlayer3.Tx.End_Clear_Text_Effect.t2D描画(634, y[i] - 2);
                    }
                }

            }
        }



        #endregion

        #region[ バチお ]

        if (this.ct進行メイン.n現在の値 <= 11)
        {
            if (TJAPlayer3.Tx.End_Clear_L[1] != null)
            {
                TJAPlayer3.Tx.End_Clear_L[1].t2D描画(697, y[i] - 30);
                TJAPlayer3.Tx.End_Clear_L[1].Opacity = (int)(11.0 / this.ct進行メイン.n現在の値) * 255;
            }
            if (TJAPlayer3.Tx.End_Clear_R[1] != null)
            {
                TJAPlayer3.Tx.End_Clear_R[1].t2D描画(738, y[i] - 30);
                TJAPlayer3.Tx.End_Clear_R[1].Opacity = (int)(11.0 / this.ct進行メイン.n現在の値) * 255;
            }
        }
        else if (this.ct進行メイン.n現在の値 <= 35)
        {
            if (TJAPlayer3.Tx.End_Clear_L[0] != null)
                TJAPlayer3.Tx.End_Clear_L[0].t2D描画(697 - (int)((this.ct進行メイン.n現在の値 - 12) * 10), y[i] - 30);
            if (TJAPlayer3.Tx.End_Clear_R[0] != null)
                TJAPlayer3.Tx.End_Clear_R[0].t2D描画(738 + (int)((this.ct進行メイン.n現在の値 - 12) * 10), y[i] - 30);
        }
        else if (this.ct進行メイン.n現在の値 <= 46)
        {
            if (TJAPlayer3.Tx.End_Clear_L[0] != null)
            {
                //2016.07.16 kairera0467 またも原始的...
                float[] fRet = new float[] { 1.0f, 0.99f, 0.98f, 0.97f, 0.96f, 0.95f, 0.96f, 0.97f, 0.98f, 0.99f, 1.0f };
                TJAPlayer3.Tx.End_Clear_L[0].t2D描画(466, y[i] - 30);
                TJAPlayer3.Tx.End_Clear_L[0].vc拡大縮小倍率 = new Vector3(fRet[this.ct進行メイン.n現在の値 - 36], 1.0f, 1.0f);
                //CDTXMania.Tx.End_Clear_R[ 0 ].t2D描画( CDTXMania.app.Device, 956 + (( this.ct進行メイン.n現在の値 - 36 ) / 2), 180 );
                TJAPlayer3.Tx.End_Clear_R[0].t2D描画(1136 - 180 * fRet[this.ct進行メイン.n現在の値 - 36], y[i] - 30);
                TJAPlayer3.Tx.End_Clear_R[0].vc拡大縮小倍率 = new Vector3(fRet[this.ct進行メイン.n現在の値 - 36], 1.0f, 1.0f);
            }
        }
        else if (this.ct進行メイン.n現在の値 <= 49)
        {
            if (TJAPlayer3.Tx.End_Clear_L[1] != null)
                TJAPlayer3.Tx.End_Clear_L[1].t2D描画(466, y[i] - 30);
            if (TJAPlayer3.Tx.End_Clear_R[1] != null)
                TJAPlayer3.Tx.End_Clear_R[1].t2D描画(956, y[i] - 30);
        }
        else if (this.ct進行メイン.n現在の値 <= 54)
        {
            if (TJAPlayer3.Tx.End_Clear_L[2] != null)
                TJAPlayer3.Tx.End_Clear_L[2].t2D描画(466, y[i] - 30);
            if (TJAPlayer3.Tx.End_Clear_R[2] != null)
                TJAPlayer3.Tx.End_Clear_R[2].t2D描画(956, y[i] - 30);
        }
        else if (this.ct進行メイン.n現在の値 <= 58)
        {
            if (TJAPlayer3.Tx.End_Clear_L[3] != null)
                TJAPlayer3.Tx.End_Clear_L[3].t2D描画(466, y[i] - 30);
            if (TJAPlayer3.Tx.End_Clear_R[3] != null)
                TJAPlayer3.Tx.End_Clear_R[3].t2D描画(956, y[i] - 30);
        }
        else
        {
            if (TJAPlayer3.Tx.End_Clear_L[4] != null)
                TJAPlayer3.Tx.End_Clear_L[4].t2D描画(466, y[i] - 30);
            if (TJAPlayer3.Tx.End_Clear_R[4] != null)
                TJAPlayer3.Tx.End_Clear_R[4].t2D描画(956, y[i] - 30);
        }

        #endregion

        */
	}

	private void showEndEffect_FullCombo(int i) {
		if (!OpenTaiko.stageGameScreen.bPAUSE) FullComboScript.Update(i);
		FullComboScript.Draw(i);

		/*
        int[] y = new int[] { 0, 176 };

        this.ctEnd_FullCombo.t進行();
        TJAPlayer3.Tx.End_FullCombo[this.ctEnd_FullCombo.n現在の値]?.t2D描画(330, y[i] + 50);

        if (this.ctEnd_FullCombo.b終了値に達した && TJAPlayer3.Tx.End_FullComboLoop[0] != null)
        {
            this.ctEnd_FullComboLoop.t進行Loop();
            TJAPlayer3.Tx.End_FullComboLoop[this.ctEnd_FullComboLoop.n現在の値]?.t2D描画(330, y[i] + 196);
        }
        */
	}

	private void showEndEffect_PerfectCombo(int i) {
		if (!OpenTaiko.stageGameScreen.bPAUSE) PerfectComboScript.Update(i);
		PerfectComboScript.Draw(i);

		/*
        int[] y = new int[] { 0, 176 };

        this.ctEnd_DondaFullCombo.t進行();
        if (this.ctEnd_DondaFullCombo.n現在の値 >= 34) TJAPlayer3.Tx.End_DondaFullComboBg?.t2D描画(332, y[i] + 192);
        TJAPlayer3.Tx.End_DondaFullCombo[this.ctEnd_DondaFullCombo.n現在の値]?.t2D描画(330, y[i] + 50);

        /*
        if (this.ctEnd_DondaFullCombo.b終了値に達した)
        {
            this.ctEnd_DondaFullComboLoop.t進行Loop();
            TJAPlayer3.Tx.End_DondaFullComboLoop[this.ctEnd_DondaFullComboLoop.n現在の値].t2D描画(330, 196);
        }
        */
	}
	// ------------------------------------
	#endregion

	public override int Draw() {
		if (base.IsFirstDraw) {
			base.IsFirstDraw = false;
		}
		if (this.ct進行メイン != null && (OpenTaiko.stageGameScreen.ePhaseID == CStage.EPhase.Game_EndStage || OpenTaiko.stageGameScreen.ePhaseID == CStage.EPhase.Game_STAGE_FAILED || OpenTaiko.stageGameScreen.ePhaseID == CStage.EPhase.Game_STAGE_CLEAR_FadeOut)) {
			this.ct進行メイン.Tick();

			//CDTXMania.act文字コンソール.tPrint( 0, 0, C文字コンソール.Eフォント種別.灰, this.ct進行メイン.n現在の値.ToString() );
			//仮置き

			if (!bSongsPlayed) {
				for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
					if (i == 1 && OpenTaiko.ConfigIni.bAIBattleMode) break;

					int pan = OpenTaiko.ConfigIni.nPanning[OpenTaiko.ConfigIni.nPlayerCount - 1][i];

					switch (this.Mode[i]) {
						case EndMode.StageFailed:
							FailedScript.PlayEndAnime(i);
							this.soundFailed[i]?.SetPanning(pan);
							this.soundFailed[i]?.PlayStart();
							OpenTaiko.Skin.voiceClearFailed[OpenTaiko.GetActualPlayer(i)]?.SetPanning(pan);
							OpenTaiko.Skin.voiceClearFailed[OpenTaiko.GetActualPlayer(i)]?.tPlay();
							break;
						case EndMode.StageCleared:
							ClearScript.PlayEndAnime(i);
							this.soundClear[i]?.SetPanning(pan);
							this.soundClear[i]?.PlayStart();
							OpenTaiko.Skin.voiceClearClear[OpenTaiko.GetActualPlayer(i)]?.SetPanning(pan);
							OpenTaiko.Skin.voiceClearClear[OpenTaiko.GetActualPlayer(i)]?.tPlay();
							break;
						case EndMode.StageFullCombo:
							FullComboScript.PlayEndAnime(i);
							this.soundFullCombo[i]?.SetPanning(pan);
							this.soundFullCombo[i]?.PlayStart();
							OpenTaiko.Skin.voiceClearFullCombo[OpenTaiko.GetActualPlayer(i)]?.SetPanning(pan);
							OpenTaiko.Skin.voiceClearFullCombo[OpenTaiko.GetActualPlayer(i)]?.tPlay();
							break;
						case EndMode.StagePerfectCombo:
							PerfectComboScript.PlayEndAnime(i);
							this.soundPerfectCombo[i]?.SetPanning(pan);
							this.soundPerfectCombo[i]?.PlayStart();
							OpenTaiko.Skin.voiceClearAllPerfect[OpenTaiko.GetActualPlayer(i)]?.SetPanning(pan);
							OpenTaiko.Skin.voiceClearAllPerfect[OpenTaiko.GetActualPlayer(i)]?.tPlay();
							break;

						case EndMode.AI_Lose:
							AILoseScript.PlayEndAnime(i);
							this.soundAILose?.PlayStart();
							OpenTaiko.Skin.voiceAILose[OpenTaiko.GetActualPlayer(1)]?.tPlay();
							break;
						case EndMode.AI_Win:
							AIWinScript.PlayEndAnime(i);
							this.soundAIWin?.PlayStart();
							OpenTaiko.Skin.voiceAIWin[OpenTaiko.GetActualPlayer(i)]?.tPlay();
							break;
						case EndMode.AI_Win_FullCombo:
							AIWin_FullComboScript.PlayEndAnime(i);
							this.soundAIWinFullCombo?.PlayStart();
							OpenTaiko.Skin.voiceAIWin[OpenTaiko.GetActualPlayer(i)]?.tPlay();
							break;
						case EndMode.AI_Win_Perfect:
							AIWin_PerfectScript.PlayEndAnime(i);
							this.soundAIWinPerfectCombo?.PlayStart();
							OpenTaiko.Skin.voiceAIWin[OpenTaiko.GetActualPlayer(i)]?.tPlay();
							break;

						case EndMode.Tower_Dropout:
							Tower_DropoutScript.PlayEndAnime(i);
							this.soundTowerDropout?.PlayStart();
							OpenTaiko.Skin.voiceClearFailed[OpenTaiko.GetActualPlayer(i)]?.tPlay();
							break;
						case EndMode.Tower_TopReached_Pass:
							Tower_TopReached_PassScript.PlayEndAnime(i);
							this.soundTowerTopPass?.PlayStart();
							OpenTaiko.Skin.voiceClearClear[OpenTaiko.GetActualPlayer(i)]?.tPlay();
							break;
						case EndMode.Tower_TopReached_FullCombo:
							Tower_TopReached_FullComboScript.PlayEndAnime(i);
							this.soundTowerTopFC?.PlayStart();
							OpenTaiko.Skin.voiceClearFullCombo[OpenTaiko.GetActualPlayer(i)]?.tPlay();
							break;
						case EndMode.Tower_TopReached_Perfect:
							Tower_TopReached_PerfectScript.PlayEndAnime(i);
							this.soundTowerTopPerfect?.PlayStart();
							OpenTaiko.Skin.voiceClearAllPerfect[OpenTaiko.GetActualPlayer(i)]?.tPlay();
							break;

						case EndMode.Dan_Fail:
							Dan_FailScript.PlayEndAnime(i);
							this.soundDanFailed?.PlayStart();
							OpenTaiko.Skin.voiceClearFailed[OpenTaiko.GetActualPlayer(i)]?.tPlay();
							break;
						case EndMode.Dan_Red_Pass:
							Dan_Red_PassScript.PlayEndAnime(i);
							this.soundDanRedClear?.PlayStart();
							OpenTaiko.Skin.voiceClearClear[OpenTaiko.GetActualPlayer(i)]?.tPlay();
							break;
						case EndMode.Dan_Red_FullCombo:
							Dan_Red_FullComboScript.PlayEndAnime(i);
							this.soundDanRedFC?.PlayStart();
							OpenTaiko.Skin.voiceClearFullCombo[OpenTaiko.GetActualPlayer(i)]?.tPlay();
							break;
						case EndMode.Dan_Red_Perfect:
							Dan_Red_PerfectScript.PlayEndAnime(i);
							this.soundDanRedPerfect?.PlayStart();
							OpenTaiko.Skin.voiceClearAllPerfect[OpenTaiko.GetActualPlayer(i)]?.tPlay();
							break;
						case EndMode.Dan_Gold_Pass:
							Dan_Gold_PassScript.PlayEndAnime(i);
							this.soundDanGoldClear?.PlayStart();
							OpenTaiko.Skin.voiceClearClear[OpenTaiko.GetActualPlayer(i)]?.tPlay();
							break;
						case EndMode.Dan_Gold_FullCombo:
							Dan_Gold_FullComboScript.PlayEndAnime(i);
							this.soundDanGoldFC?.PlayStart();
							OpenTaiko.Skin.voiceClearFullCombo[OpenTaiko.GetActualPlayer(i)]?.tPlay();
							break;
						case EndMode.Dan_Gold_Perfect:
							Dan_Gold_PerfectScript.PlayEndAnime(i);
							this.soundDanGoldPerfect?.PlayStart();
							OpenTaiko.Skin.voiceClearAllPerfect[OpenTaiko.GetActualPlayer(i)]?.tPlay();
							break;

						default:
							break;
					}
				}

				bSongsPlayed = true;
			}


			for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
				if (i == 1 && OpenTaiko.ConfigIni.bAIBattleMode) break;

				switch (this.Mode[i]) {
					case EndMode.StageFailed:
						this.showEndEffect_Failed(i);
						break;
					case EndMode.StageCleared:
						this.showEndEffect_Clear(i);
						break;
					case EndMode.StageFullCombo:
						this.showEndEffect_FullCombo(i);
						break;
					case EndMode.StagePerfectCombo:
						this.showEndEffect_PerfectCombo(i);
						break;

					case EndMode.AI_Win:
						if (!OpenTaiko.stageGameScreen.bPAUSE) AIWinScript.Update(i);
						AIWinScript.Draw(i);
						break;
					case EndMode.AI_Lose:
						if (!OpenTaiko.stageGameScreen.bPAUSE) AILoseScript.Update(i);
						AILoseScript.Draw(i);
						break;
					case EndMode.AI_Win_FullCombo:
						if (!OpenTaiko.stageGameScreen.bPAUSE) AIWin_FullComboScript.Update(i);
						AIWin_FullComboScript.Draw(i);
						break;
					case EndMode.AI_Win_Perfect:
						if (!OpenTaiko.stageGameScreen.bPAUSE) AIWin_PerfectScript.Update(i);
						AIWin_PerfectScript.Draw(i);
						break;

					case EndMode.Tower_Dropout:
						if (!OpenTaiko.stageGameScreen.bPAUSE) Tower_DropoutScript.Update(i);
						Tower_DropoutScript.Draw(i);
						break;
					case EndMode.Tower_TopReached_Pass:
						if (!OpenTaiko.stageGameScreen.bPAUSE) Tower_TopReached_PassScript.Update(i);
						Tower_TopReached_PassScript.Draw(i);
						break;
					case EndMode.Tower_TopReached_FullCombo:
						if (!OpenTaiko.stageGameScreen.bPAUSE) Tower_TopReached_FullComboScript.Update(i);
						Tower_TopReached_FullComboScript.Draw(i);
						break;
					case EndMode.Tower_TopReached_Perfect:
						if (!OpenTaiko.stageGameScreen.bPAUSE) Tower_TopReached_PerfectScript.Update(i);
						Tower_TopReached_PerfectScript.Draw(i);
						break;

					case EndMode.Dan_Fail:
						if (!OpenTaiko.stageGameScreen.bPAUSE) Dan_FailScript.Update(i);
						Dan_FailScript.Draw(i);
						break;
					case EndMode.Dan_Red_Pass:
						if (!OpenTaiko.stageGameScreen.bPAUSE) Dan_Red_PassScript.Update(i);
						Dan_Red_PassScript.Draw(i);
						break;
					case EndMode.Dan_Red_FullCombo:
						if (!OpenTaiko.stageGameScreen.bPAUSE) Dan_Red_FullComboScript.Update(i);
						Dan_Red_FullComboScript.Draw(i);
						break;
					case EndMode.Dan_Red_Perfect:
						if (!OpenTaiko.stageGameScreen.bPAUSE) Dan_Red_PerfectScript.Update(i);
						Dan_Red_PerfectScript.Draw(i);
						break;
					case EndMode.Dan_Gold_Pass:
						if (!OpenTaiko.stageGameScreen.bPAUSE) Dan_Gold_PassScript.Update(i);
						Dan_Gold_PassScript.Draw(i);
						break;
					case EndMode.Dan_Gold_FullCombo:
						if (!OpenTaiko.stageGameScreen.bPAUSE) Dan_Gold_FullComboScript.Update(i);
						Dan_Gold_FullComboScript.Draw(i);
						break;
					case EndMode.Dan_Gold_Perfect:
						if (!OpenTaiko.stageGameScreen.bPAUSE) Dan_Gold_PerfectScript.Update(i);
						Dan_Gold_PerfectScript.Draw(i);
						break;
					default:
						break;
				}

			}



			if (this.ct進行メイン.IsEnded) {
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
	bool bSongsPlayed = false;
	CCounter ct進行メイン;

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
