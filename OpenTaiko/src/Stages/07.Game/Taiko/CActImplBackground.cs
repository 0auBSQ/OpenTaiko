using FDK;

namespace OpenTaiko;

internal class CActImplBackground : CActivity {
	// 本家っぽい背景を表示させるメソッド。
	//
	// 拡張性とかないんで。はい、ヨロシクゥ!
	//
	public CActImplBackground() {
		base.IsDeActivated = true;
	}

	public void tFadeIn(int player) {
		//this.ct上背景クリアインタイマー[player] = new CCounter(0, 100, 2, TJAPlayer3.Timer);
		this.eFadeMode = EFIFOMode.FadeIn;
	}

	//public void tFadeOut(int player)
	//{
	//    this.ct上背景フェードタイマー[player] = new CCounter( 0, 100, 6, CDTXMania.Timer );
	//    this.eFadeMode = EFIFOモード.フェードアウト;
	//}

	public void ClearIn(int player) {
		/*this.ct上背景クリアインタイマー[player] = new CCounter(0, 100, 2, TJAPlayer3.Timer);
        this.ct上背景クリアインタイマー[player].n現在の値 = 0;
        this.ct上背景FIFOタイマー = new CCounter(0, 100, 2, TJAPlayer3.Timer);
        this.ct上背景FIFOタイマー.n現在の値 = 0;*/
		UpScript?.Call("clearIn", player);
		DownScript?.Call("clearIn", player);
	}

	public void ClearOut(int player) {
		UpScript?.Call("clearOut", player);
		DownScript?.Call("clearOut", player);
	}

	/// <summary>The Down folder of the tower chart's look (its TOWERTYPE), or null when the chart names none or
	/// the skin has no folder of that name (a preset or a random pick then applies).</summary>
	private static string? TowerLookPath(string bgOrigindir) {
		if (OpenTaiko.SongMount.nChoosenSongDifficulty[0] != (int)Difficulty.Tower) return null;
		string? look = OpenTaiko.SongMount.rChoosenSong?.score[(int)Difficulty.Tower]?.ChartInfo.nTowerType;
		if (string.IsNullOrEmpty(look) || look.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) return null;
		string path = $@"{bgOrigindir}{Path.DirectorySeparatorChar}Down{Path.DirectorySeparatorChar}{look}";
		return Directory.Exists(path) ? path : null;
	}

	public override void Activate() {
		if (!this.IsDeActivated)
			return;

		var bgOrigindir = CSkin.Path($"{TextureLoader.BASE}{TextureLoader.GAME}{TextureLoader.BACKGROUND}");
		var preset = HScenePreset.GetBGPreset();
		if (OpenTaiko.SongMount.nChoosenSongDifficulty[0] == (int)Difficulty.Tower) {
			bgOrigindir += "Tower";
		} else if (OpenTaiko.SongMount.nChoosenSongDifficulty[0] == (int)Difficulty.Dan) {
			bgOrigindir += "Dan";
		} else if (OpenTaiko.ConfigIni.bAIBattleMode) {
			bgOrigindir += "AI";
		} else {
			bgOrigindir += "Normal";
		}

		Random random = new Random();
		_state.RefreshConst();

		if (System.IO.Directory.Exists($@"{bgOrigindir}{Path.DirectorySeparatorChar}Up")) {
			var upDirs = System.IO.Directory.GetDirectories($@"{bgOrigindir}{Path.DirectorySeparatorChar}Up");

			// If there is a preset upper background and this preset exists on the skin use it, else random upper background
			var _presetPath = (preset != null && preset.UpperBackground != null) ? $@"{bgOrigindir}{Path.DirectorySeparatorChar}Up{Path.DirectorySeparatorChar}" + preset.UpperBackground[random.Next(0, preset.UpperBackground.Length)] : "";
			var upPath = (preset != null && System.IO.Directory.Exists(_presetPath))
				? _presetPath
				: upDirs[random.Next(0, upDirs.Length)];

			UpScript = new LuaBackgroundWrapper(upPath);
			UpScript.Activate(_state);

			IsUpNotFound = !UpScript.Exists;
		} else {
			IsUpNotFound = true;
		}

		if (System.IO.Directory.Exists($@"{bgOrigindir}{Path.DirectorySeparatorChar}Down")) {
			var downDirs = System.IO.Directory.GetDirectories($@"{bgOrigindir}{Path.DirectorySeparatorChar}Down");

			// If there is a preset lower background and this preset exists on the skin use it, else random upper background
			var _presetPath = (preset != null && preset.LowerBackground != null) ? $@"{bgOrigindir}{Path.DirectorySeparatorChar}Down{Path.DirectorySeparatorChar}" + preset.LowerBackground[random.Next(0, preset.LowerBackground.Length)] : "";
			var downPath = (preset != null && System.IO.Directory.Exists(_presetPath))
				? _presetPath
				: downDirs[random.Next(0, downDirs.Length)];

			// A tower chart names its look (TOWERTYPE): the Down folder of that name holds the sky and the tower
			var towerLook = TowerLookPath(bgOrigindir);
			if (towerLook != null) downPath = towerLook;

			DownScript = new LuaBackgroundWrapper(downPath);
			DownScript.Activate(_state);

			if (DownScript.Exists) IsDownNotFound = false;
		} else {
			IsDownNotFound = true;
		}

		if (OpenTaiko.SongMount.nChoosenSongDifficulty[0] == (int)Difficulty.Tower) {
			CCharacter.AddEssentialAnimation(0, CCharacter.ANIM_GAME_TOWER_STANDING);
			CCharacter.AddEssentialAnimation(0, CCharacter.ANIM_GAME_TOWER_STANDING_TIRED);
			CCharacter.AddEssentialAnimation(0, CCharacter.ANIM_GAME_TOWER_CLIMBING);
			CCharacter.AddEssentialAnimation(0, CCharacter.ANIM_GAME_TOWER_CLIMBING_TIRED);
			CCharacter.AddEssentialAnimation(0, CCharacter.ANIM_GAME_TOWER_RUNNING);
			CCharacter.AddEssentialAnimation(0, CCharacter.ANIM_GAME_TOWER_RUNNING_TIRED);
			CCharacter.AddEssentialAnimation(0, CCharacter.ANIM_GAME_TOWER_CLEAR);
			CCharacter.AddEssentialAnimation(0, CCharacter.ANIM_GAME_TOWER_CLEAR_TIRED);
			CCharacter.AddEssentialAnimation(0, CCharacter.ANIM_GAME_TOWER_FAIL);

			CCharacter.AddEssentialVoice(0, CCharacter.VOICE_TOWER_MISS);
		}

		// the floor and lives display over the upper background is the tower_hud ROActivity
		if (OpenTaiko.SongMount.nChoosenSongDifficulty[0] == (int)Difficulty.Tower)
			TowerHud?.Activate(OpenTaiko.SongMount.rChoosenSong?.score[(int)Difficulty.Tower]?.ChartInfo.nTotalFloor ?? 1);

		this.currentCharacter = Math.Max(0, Math.Min(OpenTaiko.SaveFileInstances[0].data.Character, OpenTaiko.Tx.Characters.Length - 1));

		//float resolutionScaleX = OpenTaiko.Skin.Resolution[0] / (float)OpenTaiko.Skin.Characters_Resolution[currentCharacter][0];
		//float resolutionScaleY = OpenTaiko.Skin.Resolution[1] / (float)OpenTaiko.Skin.Characters_Resolution[currentCharacter][1];

		// Scale tower chara
		/*
		foreach (CTexture texture in OpenTaiko.Tx.Characters_Tower_Standing[currentCharacter]) {
			texture.vcScaleRatio.X = resolutionScaleX;
			texture.vcScaleRatio.Y = resolutionScaleY;
		}
		foreach (CTexture texture in OpenTaiko.Tx.Characters_Tower_Climbing[currentCharacter]) {
			texture.vcScaleRatio.X = resolutionScaleX;
			texture.vcScaleRatio.Y = resolutionScaleY;
		}
		foreach (CTexture texture in OpenTaiko.Tx.Characters_Tower_Running[currentCharacter]) {
			texture.vcScaleRatio.X = resolutionScaleX;
			texture.vcScaleRatio.Y = resolutionScaleY;
		}
		foreach (CTexture texture in OpenTaiko.Tx.Characters_Tower_Clear[currentCharacter]) {
			texture.vcScaleRatio.X = resolutionScaleX;
			texture.vcScaleRatio.Y = resolutionScaleY;
		}
		foreach (CTexture texture in OpenTaiko.Tx.Characters_Tower_Fail[currentCharacter]) {
			texture.vcScaleRatio.X = resolutionScaleX;
			texture.vcScaleRatio.Y = resolutionScaleY;
		}
		foreach (CTexture texture in OpenTaiko.Tx.Characters_Tower_Standing_Tired[currentCharacter]) {
			texture.vcScaleRatio.X = resolutionScaleX;
			texture.vcScaleRatio.Y = resolutionScaleY;
		}
		foreach (CTexture texture in OpenTaiko.Tx.Characters_Tower_Climbing_Tired[currentCharacter]) {
			texture.vcScaleRatio.X = resolutionScaleX;
			texture.vcScaleRatio.Y = resolutionScaleY;
		}
		foreach (CTexture texture in OpenTaiko.Tx.Characters_Tower_Running_Tired[currentCharacter]) {
			texture.vcScaleRatio.X = resolutionScaleX;
			texture.vcScaleRatio.Y = resolutionScaleY;
		}
		foreach (CTexture texture in OpenTaiko.Tx.Characters_Tower_Clear_Tired[currentCharacter]) {
			texture.vcScaleRatio.X = resolutionScaleX;
			texture.vcScaleRatio.Y = resolutionScaleY;
		}
		*/

		this.ctClimbDuration = new CCounter();
		//this.ctStandingAnimation = new CCounter(0, 1000, (60000f / (float)CTja.TjaBeatSpeedToGameBeatSpeed(OpenTaiko.stageGameScreen.actPlayInfo.dbBPM[0])) * OpenTaiko.Skin.Characters_Beat_Tower_Standing[currentCharacter] / OpenTaiko.Skin.Characters_Tower_Standing_Ptn[currentCharacter], OpenTaiko.Timer);
		this.ctStandingAnimation = new CCounter(0, 1000, OpenTaiko.stageGameScreen.actPlayInfo.msPerGameBeatAbs(0) * 1 / 1, OpenTaiko.Timer);
		this.ctClimbingAnimation = new CCounter();
		this.ctRunningAnimation = new CCounter();
		this.ctClearAnimation = new CCounter();
		this.ctFailAnimation = new CCounter();
		this.ctStandTiredAnimation = new CCounter();
		this.ctClimbTiredAnimation = new CCounter();
		this.ctRunTiredAnimation = new CCounter();
		this.ctClearTiredAnimation = new CCounter();

		TowerFinished = false;

		base.Activate();
	}

	public override void DeActivate() {
		if (this.IsDeActivated)
			return;

		if (OpenTaiko.SongMount.nChoosenSongDifficulty[0] == (int)Difficulty.Tower) {
			CCharacter.RemoveEssentialAnimation(0, CCharacter.ANIM_GAME_TOWER_STANDING);
			CCharacter.RemoveEssentialAnimation(0, CCharacter.ANIM_GAME_TOWER_STANDING_TIRED);
			CCharacter.RemoveEssentialAnimation(0, CCharacter.ANIM_GAME_TOWER_CLIMBING);
			CCharacter.RemoveEssentialAnimation(0, CCharacter.ANIM_GAME_TOWER_CLIMBING_TIRED);
			CCharacter.RemoveEssentialAnimation(0, CCharacter.ANIM_GAME_TOWER_RUNNING);
			CCharacter.RemoveEssentialAnimation(0, CCharacter.ANIM_GAME_TOWER_RUNNING_TIRED);
			CCharacter.RemoveEssentialAnimation(0, CCharacter.ANIM_GAME_TOWER_CLEAR);
			CCharacter.RemoveEssentialAnimation(0, CCharacter.ANIM_GAME_TOWER_CLEAR_TIRED);
			CCharacter.RemoveEssentialAnimation(0, CCharacter.ANIM_GAME_TOWER_FAIL);

			CCharacter.RemoveEssentialVoice(0, CCharacter.VOICE_TOWER_MISS);
		}

		OpenTaiko.tDisposeSafely(ref UpScript);
		OpenTaiko.tDisposeSafely(ref DownScript);

		if (TowerHud != null && TowerHud.IsActive) TowerHud.Deactivate();

		base.DeActivate();
	}

	public override void CreateManagedResource() {
		base.CreateManagedResource();
	}

	public override void ReleaseManagedResource() {
		base.ReleaseManagedResource();
	}

	public override int Draw() {
		if (base.IsDeActivated)
			return 0;

		// One per-frame state refresh, shared by both the Up (here) and Down (Tower / non-Tower below) draws.
		_state.RefreshGameplay();

		//this.ct上背景FIFOタイマー?.t進行();

		// fNow_Measure_s (/ m)

		#region [Upper background]

		if (!IsUpNotFound) {
			if (!OpenTaiko.stageGameScreen.bPAUSE) UpScript?.Update(_state);
			UpScript?.Draw(_state);
			if (OpenTaiko.SongMount.nChoosenSongDifficulty[0] == (int)Difficulty.Tower) {
				#region [Tower animations variables]

				this.bFloorChanged = OpenTaiko.stageGameScreen.FloorManagement.LastRegisteredFloor > 0 && (OpenTaiko.stageGameScreen.FloorManagement.LastRegisteredFloor < OpenTaiko.stageGameScreen.actPlayInfo.NowMeasure[0] + 1);

				int maxFloor = OpenTaiko.SongMount.rChoosenSong.score[5].ChartInfo.nTotalFloor;

				#endregion

				#region [Tower background informations]

				if (OpenTaiko.SongMount.nChoosenSongDifficulty[0] == (int)Difficulty.Tower) {
					OpenTaiko.stageGameScreen.FloorManagement.loopFrames();

					if (OpenTaiko.stageGameScreen.FloorManagement.CurrentNumberOfLives > 0) {
						OpenTaiko.stageGameScreen.FloorManagement.LastRegisteredFloor = OpenTaiko.stageGameScreen.actPlayInfo.NowMeasure[0] + 1;
						if (!(OpenTaiko.stageGameScreen.IsChartEnded(0) || OpenTaiko.stageGameScreen.IsFinishedPlaying(0))) {
							if (OpenTaiko.stageGameScreen.FloorManagement.LastRegisteredFloor >= maxFloor)
								OpenTaiko.stageGameScreen.FloorManagement.LastRegisteredFloor = maxFloor - 1;
						}
					}

					// the floor and lives planks (tower_hud ROActivity) read the floor and the lives from PLAYSTATE
					if (TowerHud != null && TowerHud.IsActive) {
						if (!OpenTaiko.stageGameScreen.bPAUSE) TowerHud.Update();
						TowerHud.Draw();
					}
				}

				#endregion
			}
		}

		#endregion

		#region [Lower background]


		if (OpenTaiko.SongMount.nChoosenSongDifficulty[0] == (int)Difficulty.Tower) {
			int maxFloor = OpenTaiko.SongMount.rChoosenSong.score[5].ChartInfo.nTotalFloor;

			OpenTaiko.actTextConsole.Print(0, 0, CTextConsole.EFontType.White, maxFloor.ToString());

			#region [Tower lower background]

			// the tower look's Down script draws the sky and the tower body (the floors sliding under the player)
			if (!OpenTaiko.stageGameScreen.bPAUSE) DownScript?.Update(_state);
			DownScript?.Draw(_state);

			#region [Climbing don]

			CCharacter character = CCharacter.GetCharacter(0);
			float liveState = (OpenTaiko.stageGameScreen.FloorManagement.CurrentNumberOfLives / (float)OpenTaiko.stageGameScreen.FloorManagement.MaxNumberOfLives);
			bool ctIsTired = !(liveState >= 0.2f && !(OpenTaiko.stageGameScreen.FloorManagement.CurrentNumberOfLives == 1 && OpenTaiko.stageGameScreen.FloorManagement.MaxNumberOfLives != 1));

			bool stageEnded = OpenTaiko.stageGameScreen.IsStageCompleted() || OpenTaiko.stageGameScreen.FloorManagement.CurrentNumberOfLives == 0;

			if (bFloorChanged == true) {
				// float floorBPM = (float)CTja.TjaBeatSpeedToGameBeatSpeed(OpenTaiko.stageGameScreen.actPlayInfo.dbBPM[0]);
				ctClimbDuration.Start(0, 1500, OpenTaiko.stageGameScreen.actPlayInfo.msPerGameBeatAbs(0) / 500, OpenTaiko.Timer);
				//character.TowerNextFloor();
				/*
				ctStandingAnimation.Start(0, 1000, (60000f / floorBPM) * OpenTaiko.Skin.Characters_Beat_Tower_Standing[currentCharacter] / OpenTaiko.Skin.Characters_Tower_Standing_Ptn[currentCharacter], OpenTaiko.Timer);
				ctClimbingAnimation.Start(0, 1000, (120000f / floorBPM) / OpenTaiko.Skin.Characters_Tower_Climbing_Ptn[currentCharacter], OpenTaiko.Timer);
				ctRunningAnimation.Start(0, 1000, (60000f / floorBPM) / OpenTaiko.Skin.Characters_Tower_Running_Ptn[currentCharacter], OpenTaiko.Timer);
				ctStandTiredAnimation.Start(0, 1000, (60000f / floorBPM) * OpenTaiko.Skin.Characters_Beat_Tower_Standing_Tired[currentCharacter] / OpenTaiko.Skin.Characters_Tower_Standing_Tired_Ptn[currentCharacter], OpenTaiko.Timer);
				ctClimbTiredAnimation.Start(0, 1000, (120000f / floorBPM) / OpenTaiko.Skin.Characters_Tower_Climbing_Tired_Ptn[currentCharacter], OpenTaiko.Timer);
				ctRunTiredAnimation.Start(0, 1000, (60000f / floorBPM) / OpenTaiko.Skin.Characters_Tower_Running_Tired_Ptn[currentCharacter], OpenTaiko.Timer);
				*/
			}

			bool isClimbing = ctClimbDuration.CurrentValue > 0 && ctClimbDuration.CurrentValue < 1500;

			if (stageEnded && !TowerFinished && !isClimbing) {
				//float floorBPM = (float)CTja.TjaBeatSpeedToGameBeatSpeed(OpenTaiko.stageGameScreen.actPlayInfo.dbBPM[0]);
				/*
				ctClearAnimation.Start(0, 20000, (60000f / floorBPM) * OpenTaiko.Skin.Characters_Beat_Tower_Clear[currentCharacter] / OpenTaiko.Skin.Characters_Tower_Clear_Ptn[currentCharacter], OpenTaiko.Timer);
				ctClearTiredAnimation.Start(0, 20000, (60000f / floorBPM) * OpenTaiko.Skin.Characters_Beat_Tower_Clear_Tired[currentCharacter] / OpenTaiko.Skin.Characters_Tower_Clear_Tired_Ptn[currentCharacter], OpenTaiko.Timer);
				ctFailAnimation.Start(0, 20000, (60000f / floorBPM) * OpenTaiko.Skin.Characters_Beat_Tower_Fail[currentCharacter] / OpenTaiko.Skin.Characters_Tower_Fail_Ptn[currentCharacter], OpenTaiko.Timer);
				*/
				//character.TowerFinish();
				TowerFinished = true;
			}

			float x = OpenTaiko.Skin.Game_Tower_Don[0];
			float y = OpenTaiko.Skin.Game_Tower_Don[1];

			string animation = CCharacter.ANIM_GAME_TOWER_STANDING;
			if (isClimbing) {
				if (ctClimbDuration.CurrentValue <= 1000) {
					animation = ctIsTired ? CCharacter.ANIM_GAME_TOWER_CLIMBING_TIRED : CCharacter.ANIM_GAME_TOWER_CLIMBING;

					float value = ctClimbDuration.CurrentValue / 1000f;
					x += value * OpenTaiko.Skin.Game_Tower_Don_Move[0];
					y += value * OpenTaiko.Skin.Game_Tower_Don_Move[1];
				} else if (ctClimbDuration.CurrentValue < 1500) {
					animation = ctIsTired ? CCharacter.ANIM_GAME_TOWER_RUNNING_TIRED : CCharacter.ANIM_GAME_TOWER_RUNNING;

					float value = (ctClimbDuration.CurrentValue - 1000) / 500f;
					float returnValue = 1.0f - value;
					x += returnValue * OpenTaiko.Skin.Game_Tower_Don_Move[0];
					y += returnValue * OpenTaiko.Skin.Game_Tower_Don_Move[1];
				}
			} else if (stageEnded && OpenTaiko.stageGameScreen.FloorManagement.CurrentNumberOfLives == 0) {
				animation = CCharacter.ANIM_GAME_TOWER_FAIL;
			} else if (stageEnded && OpenTaiko.stageGameScreen.FloorManagement.CurrentNumberOfLives > 0) {
				animation = ctIsTired ? CCharacter.ANIM_GAME_TOWER_CLEAR_TIRED : CCharacter.ANIM_GAME_TOWER_CLEAR;
			} else if (!stageEnded) {
				animation = ctIsTired ? CCharacter.ANIM_GAME_TOWER_STANDING_TIRED : CCharacter.ANIM_GAME_TOWER_STANDING;
			}

			character.SetAnimationCyclesFromBPM(animation, OpenTaiko.stageGameScreen.actPlayInfo.dbBPM[0]);

			if (!OpenTaiko.stageGameScreen.bPAUSE) {
				character.Update(animation);
			}
			character.Draw(animation, x, y);

			if (isClimbing) {
				// Tired Climb
				/*
				if (ctIsTired && (ctClimbDuration.CurrentValue <= 1000) && OpenTaiko.Skin.Characters_Tower_Climbing_Tired_Ptn[currentCharacter] > 0) {
					int animChar = ctClimbTiredAnimation.CurrentValue % OpenTaiko.Skin.Characters_Tower_Climbing_Ptn[currentCharacter];
					int distDonX = (int)(ctClimbDuration.CurrentValue * (OpenTaiko.Skin.Game_Tower_Don_Move[0] / 1000f));
					int distDonY = (int)(ctClimbDuration.CurrentValue * (OpenTaiko.Skin.Game_Tower_Don_Move[1] / 1000f));
					OpenTaiko.Tx.Characters_Tower_Climbing_Tired[currentCharacter][animChar]?.t2D拡大率考慮下中心基準描画(OpenTaiko.Skin.Game_Tower_Don[0] + distDonX, OpenTaiko.Skin.Game_Tower_Don[1] + distDonY);
				}
				// Tired Run
				else if (ctIsTired && (ctClimbDuration.CurrentValue > 1000 && ctClimbDuration.CurrentValue < 1500) && OpenTaiko.Skin.Characters_Tower_Running_Tired_Ptn[currentCharacter] > 0) {
					int animChar = ctRunTiredAnimation.CurrentValue % OpenTaiko.Skin.Characters_Tower_Running_Ptn[currentCharacter];
					int distDonX = (int)((1500 - ctClimbDuration.CurrentValue) * (OpenTaiko.Skin.Game_Tower_Don_Move[0] / 500f));
					int distDonY = (int)((1500 - ctClimbDuration.CurrentValue) * (OpenTaiko.Skin.Game_Tower_Don_Move[1] / 500f));
					OpenTaiko.Tx.Characters_Tower_Running_Tired[currentCharacter][animChar]?.t2D拡大率考慮下中心基準描画(OpenTaiko.Skin.Game_Tower_Don[0] + distDonX, OpenTaiko.Skin.Game_Tower_Don[1] + distDonY);
				}
				// Climb
				else if ((ctClimbDuration.CurrentValue <= 1000) && OpenTaiko.Skin.Characters_Tower_Climbing_Ptn[currentCharacter] > 0) {
					int animChar = ctClimbingAnimation.CurrentValue % OpenTaiko.Skin.Characters_Tower_Climbing_Ptn[currentCharacter];
					int distDonX = (int)(ctClimbDuration.CurrentValue * (OpenTaiko.Skin.Game_Tower_Don_Move[0] / 1000f));
					int distDonY = (int)(ctClimbDuration.CurrentValue * (OpenTaiko.Skin.Game_Tower_Don_Move[1] / 1000f));
					OpenTaiko.Tx.Characters_Tower_Climbing[currentCharacter][animChar]?.t2D拡大率考慮下中心基準描画(OpenTaiko.Skin.Game_Tower_Don[0] + distDonX, OpenTaiko.Skin.Game_Tower_Don[1] + distDonY);
				}
				// Run
				else if ((ctClimbDuration.CurrentValue > 1000 && ctClimbDuration.CurrentValue < 1500) && OpenTaiko.Skin.Characters_Tower_Running_Ptn[currentCharacter] > 0) {
					int animChar = ctRunningAnimation.CurrentValue % OpenTaiko.Skin.Characters_Tower_Running_Ptn[currentCharacter];
					int distDonX = (int)((1500 - ctClimbDuration.CurrentValue) * (OpenTaiko.Skin.Game_Tower_Don_Move[0] / 500f));
					int distDonY = (int)((1500 - ctClimbDuration.CurrentValue) * (OpenTaiko.Skin.Game_Tower_Don_Move[1] / 500f));
					OpenTaiko.Tx.Characters_Tower_Running[currentCharacter][animChar]?.t2D拡大率考慮下中心基準描画(OpenTaiko.Skin.Game_Tower_Don[0] + distDonX, OpenTaiko.Skin.Game_Tower_Don[1] + distDonY);
				}
				*/
			} else {
				/*
				// Fail
				if (OpenTaiko.Skin.Characters_Tower_Fail_Ptn[currentCharacter] > 0 && OpenTaiko.stageGameScreen.FloorManagement.CurrentNumberOfLives == 0) {
					int animChar = OpenTaiko.Skin.Characters_Tower_Fail_IsLooping[currentCharacter] ?
						ctFailAnimation.CurrentValue % OpenTaiko.Skin.Characters_Tower_Fail_Ptn[currentCharacter] :
						Math.Min(ctFailAnimation.CurrentValue, OpenTaiko.Skin.Characters_Tower_Fail_Ptn[currentCharacter] - 1);
					OpenTaiko.Tx.Characters_Tower_Fail[currentCharacter][animChar]?.t2D拡大率考慮下中心基準描画(OpenTaiko.Skin.Game_Tower_Don[0], OpenTaiko.Skin.Game_Tower_Don[1]);
				}
				// Tired Clear
				else if (ctIsTired && stageEnded && OpenTaiko.Skin.Characters_Tower_Clear_Tired_Ptn[currentCharacter] > 0 && OpenTaiko.stageGameScreen.FloorManagement.CurrentNumberOfLives > 0) {
					int animChar = OpenTaiko.Skin.Characters_Tower_Clear_Tired_IsLooping[currentCharacter] ?
						ctClearTiredAnimation.CurrentValue % OpenTaiko.Skin.Characters_Tower_Clear_Tired_Ptn[currentCharacter] :
						Math.Min(ctClearTiredAnimation.CurrentValue, OpenTaiko.Skin.Characters_Tower_Clear_Tired_Ptn[currentCharacter] - 1);
					OpenTaiko.Tx.Characters_Tower_Clear_Tired[currentCharacter][animChar]?.t2D拡大率考慮下中心基準描画(OpenTaiko.Skin.Game_Tower_Don[0], OpenTaiko.Skin.Game_Tower_Don[1]);
				}
				// Clear
				else if (stageEnded && OpenTaiko.Skin.Characters_Tower_Clear_Ptn[currentCharacter] > 0 && OpenTaiko.stageGameScreen.FloorManagement.CurrentNumberOfLives > 0) {
					int animChar = OpenTaiko.Skin.Characters_Tower_Clear_IsLooping[currentCharacter] ?
						ctClearAnimation.CurrentValue % OpenTaiko.Skin.Characters_Tower_Clear_Ptn[currentCharacter] :
						Math.Min(ctClearAnimation.CurrentValue, OpenTaiko.Skin.Characters_Tower_Clear_Ptn[currentCharacter] - 1);
					OpenTaiko.Tx.Characters_Tower_Clear[currentCharacter][animChar]?.t2D拡大率考慮下中心基準描画(OpenTaiko.Skin.Game_Tower_Don[0], OpenTaiko.Skin.Game_Tower_Don[1]);
				}

				// Tired Stand
				else if (ctIsTired && OpenTaiko.Skin.Characters_Tower_Standing_Tired_Ptn[currentCharacter] > 0) {
					int animChar = ctStandTiredAnimation.CurrentValue % OpenTaiko.Skin.Characters_Tower_Standing_Tired_Ptn[currentCharacter];
					OpenTaiko.Tx.Characters_Tower_Standing_Tired[currentCharacter][animChar]?.t2D拡大率考慮下中心基準描画(OpenTaiko.Skin.Game_Tower_Don[0], OpenTaiko.Skin.Game_Tower_Don[1]); // Center X - 50
				}
				// Stand
				else if (OpenTaiko.Skin.Characters_Tower_Standing_Ptn[currentCharacter] > 0) {
					int animChar = ctStandingAnimation.CurrentValue % OpenTaiko.Skin.Characters_Tower_Standing_Ptn[currentCharacter];
					OpenTaiko.Tx.Characters_Tower_Standing[currentCharacter][animChar]?.t2D拡大率考慮下中心基準描画(OpenTaiko.Skin.Game_Tower_Don[0], OpenTaiko.Skin.Game_Tower_Don[1]); // Center X - 50
				}
				*/
			}

			#endregion

			#region [Miss icon]

			if (OpenTaiko.stageGameScreen.FloorManagement.InvincibilityFrames != null && OpenTaiko.stageGameScreen.FloorManagement.InvincibilityFrames.CurrentValue < OpenTaiko.stageGameScreen.FloorManagement.InvincibilityDurationSpeedDependent) {
				if (OpenTaiko.Tx.Tower_Miss != null)
					OpenTaiko.Tx.Tower_Miss.Opacity = Math.Min(255, 1000 - OpenTaiko.stageGameScreen.FloorManagement.InvincibilityFrames.CurrentValue);
				OpenTaiko.Tx.Tower_Miss?.t2DBottomCenterBasedDraw(OpenTaiko.Skin.Game_Tower_Miss[0], OpenTaiko.Skin.Game_Tower_Miss[1]);
			}

			#endregion

			ctClimbDuration?.Tick();
			ctStandingAnimation?.TickLoop();
			ctClimbingAnimation?.TickLoop();
			ctRunningAnimation?.TickLoop();
			ctStandTiredAnimation?.TickLoop();
			ctClimbTiredAnimation?.TickLoop();
			ctRunTiredAnimation?.TickLoop();
			ctClearAnimation?.Tick();
			ctClearTiredAnimation?.Tick();
			ctFailAnimation?.Tick();

			#endregion
		} else if (!OpenTaiko.stageGameScreen.isMultiPlay && OpenTaiko.SongMount.nChoosenSongDifficulty[0] != (int)Difficulty.Dan) {
			if (!IsDownNotFound) {
				if (!OpenTaiko.stageGameScreen.bPAUSE) DownScript?.Update(_state);
				DownScript?.Draw(_state);
			}
		}


		#endregion

		return base.Draw();
	}

	public bool IsFinishedTowerClimbing() => ctClimbDuration?.IsEnded ?? true;

	#region[ private ]
	//-----------------

	#region 背景
	/*private CTexture Background,
        Background_Down,
        Background_Down_Clear,
        Background_Down_Scroll;
    private CTexture[] Background_Up_1st,
                      Background_Up_2nd,
                      Background_Up_3rd,
                      Background_Up_Dan = new CTexture[6],
                      Background_Up_Tower = new CTexture[8];*/
	#endregion

	/*private CCounter[] ct上背景スクロール用タイマー1st; //上背景のX方向スクロール用
    private CCounter[] ct上背景スクロール用タイマー2nd; //上背景のY方向スクロール用
    private CCounter[] ct上背景スクロール用タイマー3rd; //上背景のY方向スクロール用
    private CCounter ct下背景スクロール用タイマー1; //下背景パーツ1のX方向スクロール用
    private CCounter ct上背景FIFOタイマー;
    private CCounter[] ct上背景クリアインタイマー;
    private CCounter[] ct上背景スクロール用タイマー1stDan;   //上背景のX方向スクロール用
    private CCounter ct上背景スクロール用タイマー2stDan;   //上背景のY方向スクロール用

    private CCounter[] ct上背景スクロール用タイマー1stTower;   //上背景のX方向スクロール用
    private CCounter ct上背景スクロール用タイマー2stTower;   //上背景のX方向スクロール用
    */
	//private CTexture tx上背景メイン;
	//private CTexture tx上背景クリアメイン;
	//private CTexture tx下背景メイン;
	//private CTexture tx下背景クリアメイン;
	//private CTexture tx下背景クリアサブ1;

	public LuaBackgroundWrapper UpScript;
	public LuaBackgroundWrapper DownScript;
	private readonly LuaBackgroundState _state = new();

	private static LuaROActivityWrapper? TowerHud => LuaROActivityWrapper.GetROActivity("tower_hud");

	private bool bFloorChanged = false;
	private int currentCharacter;
	private CCounter ctStandingAnimation;
	private CCounter ctClimbingAnimation;
	private CCounter ctRunningAnimation;
	private CCounter ctClearAnimation;
	private CCounter ctFailAnimation;
	private CCounter ctStandTiredAnimation;
	private CCounter ctClimbTiredAnimation;
	private CCounter ctRunTiredAnimation;
	private CCounter ctClearTiredAnimation;
	private CCounter ctClimbDuration;
	private bool TowerFinished;


	private bool IsUpNotFound;
	private bool IsDownNotFound;

	private EFIFOMode eFadeMode;
	//-----------------
	#endregion
}
