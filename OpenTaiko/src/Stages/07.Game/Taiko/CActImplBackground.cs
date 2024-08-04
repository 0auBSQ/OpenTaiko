using FDK;
using static OpenTaiko.CActSelect曲リスト;
using Color = System.Drawing.Color;
using Rectangle = System.Drawing.Rectangle;

namespace OpenTaiko {
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
			this.eFadeMode = EFIFOモード.フェードイン;
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
			UpScript?.ClearIn(player);
			DownScript?.ClearIn(player);
		}

		public void ClearOut(int player) {
			UpScript?.ClearOut(player);
			DownScript?.ClearOut(player);
		}

		public override void Activate() {
			if (!this.IsDeActivated)
				return;

			var bgOrigindir = CSkin.Path($"{TextureLoader.BASE}{TextureLoader.GAME}{TextureLoader.BACKGROUND}");
			var preset = HScenePreset.GetBGPreset();
			if (OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Tower) {
				bgOrigindir += "Tower";
			} else if (OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Dan) {
				bgOrigindir += "Dan";
			} else if (OpenTaiko.ConfigIni.bAIBattleMode) {
				bgOrigindir += "AI";
			} else {
				bgOrigindir += "Normal";
			}

			Random random = new Random();

			if (System.IO.Directory.Exists($@"{bgOrigindir}{Path.DirectorySeparatorChar}Up")) {
				var upDirs = System.IO.Directory.GetDirectories($@"{bgOrigindir}{Path.DirectorySeparatorChar}Up");

				// If there is a preset upper background and this preset exists on the skin use it, else random upper background
				var _presetPath = (preset != null && preset.UpperBackground != null) ? $@"{bgOrigindir}{Path.DirectorySeparatorChar}Up{Path.DirectorySeparatorChar}" + preset.UpperBackground[random.Next(0, preset.UpperBackground.Length)] : "";
				var upPath = (preset != null && System.IO.Directory.Exists(_presetPath))
					? _presetPath
					: upDirs[random.Next(0, upDirs.Length)];

				UpScript = new ScriptBG($@"{upPath}{Path.DirectorySeparatorChar}Script.lua");
				UpScript.Init();

				IsUpNotFound = false;
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

				DownScript = new ScriptBG($@"{downPath}{Path.DirectorySeparatorChar}Script.lua");
				DownScript?.Init();

				if (DownScript.Exists()) IsDownNotFound = false;
			} else {
				IsDownNotFound = true;
			}

			this.pfTowerText = HPrivateFastFont.tInstantiateMainFont(OpenTaiko.Skin.Game_Tower_Font_TowerText);

			/*
            if (!string.IsNullOrEmpty(TJAPlayer3.ConfigIni.FontName))
            {
                this.pfTowerText = new CPrivateFastFont(new FontFamily(TJAPlayer3.ConfigIni.FontName), TJAPlayer3.Skin.Game_Tower_Font_TowerText);
            }
            else
            { 
                this.pfTowerText = new CPrivateFastFont(new FontFamily("MS UI Gothic"), TJAPlayer3.Skin.Game_Tower_Font_TowerText);
            }
            */

			this.ttkTouTatsuKaiSuu = new TitleTextureKey(CLangManager.LangInstance.GetString("TOWER_FLOOR_REACHED"), pfTowerText, Color.White, Color.Black, 700);
			this.ttkKai = new TitleTextureKey(CLangManager.LangInstance.GetString("TOWER_FLOOR_INITIAL"), pfTowerText, Color.White, Color.Black, 700);

			this.ct炎 = new CCounter(0, 6, 50, OpenTaiko.Timer);

			this.currentCharacter = Math.Max(0, Math.Min(OpenTaiko.SaveFileInstances[0].data.Character, OpenTaiko.Skin.Characters_Ptn - 1));

			float resolutionScaleX = OpenTaiko.Skin.Resolution[0] / (float)OpenTaiko.Skin.Characters_Resolution[currentCharacter][0];
			float resolutionScaleY = OpenTaiko.Skin.Resolution[1] / (float)OpenTaiko.Skin.Characters_Resolution[currentCharacter][1];

			// Scale tower chara
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

			this.ctSlideAnimation = new CCounter();
			this.ctClimbDuration = new CCounter();
			this.ctStandingAnimation = new CCounter(0, 1000, (60000f / (float)(OpenTaiko.stage演奏ドラム画面.actPlayInfo.dbBPM[0] * OpenTaiko.ConfigIni.SongPlaybackSpeed)) * OpenTaiko.Skin.Characters_Beat_Tower_Standing[currentCharacter] / OpenTaiko.Skin.Characters_Tower_Standing_Ptn[currentCharacter], OpenTaiko.Timer);
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

			OpenTaiko.tDisposeSafely(ref UpScript);
			OpenTaiko.tDisposeSafely(ref DownScript);

			OpenTaiko.tDisposeSafely(ref pfTowerText);

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


			//this.ct上背景FIFOタイマー?.t進行();


			#region [Tower specific variables declaration]

			float currentFloorPositionMax140 = 0;

			#endregion

			// fNow_Measure_s (/ m)

			#region [Upper background]

			if (!IsUpNotFound) {
				if (!OpenTaiko.stage演奏ドラム画面.bPAUSE) UpScript?.Update();
				UpScript?.Draw();
				if (OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Tower) {
					#region [Tower animations variables]

					this.bFloorChanged = CFloorManagement.LastRegisteredFloor > 0 && (CFloorManagement.LastRegisteredFloor < OpenTaiko.stage演奏ドラム画面.actPlayInfo.NowMeasure[0] + 1);

					int maxFloor = OpenTaiko.stageSongSelect.rChoosenSong.arスコア[5].譜面情報.nTotalFloor;
					int nightTime = Math.Max(140, maxFloor / 2);

					currentFloorPositionMax140 = Math.Min(OpenTaiko.stage演奏ドラム画面.actPlayInfo.NowMeasure[0] / (float)nightTime, 1f);

					#endregion

					#region [Tower background informations]

					if (OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Tower) {
						TitleTextureKey.ResolveTitleTexture(ttkTouTatsuKaiSuu).t2D描画(OpenTaiko.Skin.Game_Tower_Font_TouTatsuKaiSuu[0], OpenTaiko.Skin.Game_Tower_Font_TouTatsuKaiSuu[1]);
						TitleTextureKey.ResolveTitleTexture(ttkKai).t2D描画(OpenTaiko.Skin.Game_Tower_Font_Kai[0], OpenTaiko.Skin.Game_Tower_Font_Kai[1]);

						this.ct炎.TickLoop();
						CFloorManagement.loopFrames();

						#region [Floor number]

						if (CFloorManagement.CurrentNumberOfLives > 0)
							CFloorManagement.LastRegisteredFloor = OpenTaiko.stage演奏ドラム画面.actPlayInfo.NowMeasure[0] + 1;

						string floorStr = CFloorManagement.LastRegisteredFloor.ToString();

						int len = floorStr.Length;

						int digitLength = OpenTaiko.Tx.Taiko_Combo[0].szTextureSize.Width / 10;

						OpenTaiko.Tx.Taiko_Combo[0].color4 = new Color4(1f, 0.6f, 0.2f, 1f);
						OpenTaiko.Tx.Taiko_Combo[0].vcScaleRatio.X = 1.4f;
						OpenTaiko.Tx.Taiko_Combo[0].vcScaleRatio.Y = 1.4f;

						for (int idx = len - 1; idx >= 0; idx--) {
							int currentNum = int.Parse(floorStr[idx].ToString());

							OpenTaiko.Tx.Taiko_Combo[0].t2D描画(OpenTaiko.Skin.Game_Tower_Floor_Number[0] - ((digitLength - 8) * (len - idx) * 1.4f),
								OpenTaiko.Skin.Game_Tower_Floor_Number[1],
								new Rectangle(digitLength * currentNum, 0,
									digitLength, OpenTaiko.Tx.Taiko_Combo[0].szTextureSize.Height));
						}

						#endregion

						#region [Life Tamashii icon]

						int soulfire_width = OpenTaiko.Tx.Gauge_Soul_Fire.szTextureSize.Width / 8;
						int soulfire_height = OpenTaiko.Tx.Gauge_Soul_Fire.szTextureSize.Height;

						int soul_height = OpenTaiko.Tx.Gauge_Soul.szTextureSize.Height / 2;

						OpenTaiko.Tx.Gauge_Soul_Fire?.t2D描画(OpenTaiko.Skin.Gauge_Soul_Fire_X_Tower, OpenTaiko.Skin.Gauge_Soul_Fire_Y_Tower, new Rectangle(soulfire_width * (this.ct炎.CurrentValue), 0, soulfire_width, soulfire_height));
						OpenTaiko.Tx.Gauge_Soul?.t2D描画(OpenTaiko.Skin.Gauge_Soul_X_Tower, OpenTaiko.Skin.Gauge_Soul_Y_Tower, new Rectangle(0, soul_height, OpenTaiko.Tx.Gauge_Soul.szTextureSize.Width, soul_height));

						#endregion

						#region [Life number]

						if (CFloorManagement.MaxNumberOfLives <= 0) {
							CFloorManagement.MaxNumberOfLives = 5;
							CFloorManagement.CurrentNumberOfLives = 5;
						}

						string lifeStr = CFloorManagement.CurrentNumberOfLives.ToString();

						len = lifeStr.Length;

						bool lifeSpecialCase = CFloorManagement.CurrentNumberOfLives == 1 && CFloorManagement.MaxNumberOfLives != 1;
						float lifeRatio = CFloorManagement.CurrentNumberOfLives / (float)CFloorManagement.MaxNumberOfLives;

						Color4 lifeColor = (lifeRatio > 0.5f && !lifeSpecialCase) ? new Color4(0.2f, 1f, 0.2f, 1f)
								: ((lifeRatio >= 0.2f && !lifeSpecialCase) ? new Color4(1f, 1f, 0.2f, 1f)
								: new Color4(1f, 0.2f, 0.2f, 1f));

						OpenTaiko.Tx.Taiko_Combo[0].color4 = lifeColor;
						OpenTaiko.Tx.Taiko_Combo[0].vcScaleRatio.X = 1.1f;
						OpenTaiko.Tx.Taiko_Combo[0].vcScaleRatio.Y = 1.1f;

						for (int idx = 0; idx < len; idx++) {
							int currentNum = int.Parse(lifeStr[len - idx - 1].ToString());

							OpenTaiko.Tx.Taiko_Combo[0].t2D描画(OpenTaiko.Skin.Game_Tower_Life_Number[0] + ((digitLength - 8) * (len - idx) * 1.1f),
								OpenTaiko.Skin.Game_Tower_Life_Number[1],
								new Rectangle(digitLength * currentNum, 0,
									digitLength, OpenTaiko.Tx.Taiko_Combo[0].szTextureSize.Height));
						}

						OpenTaiko.Tx.Taiko_Combo[0].color4 = new Color4(1f, 1f, 1f, 1f);

						#endregion

					}

					#endregion
				}
			}

			#endregion

			#region [Lower background]


			if (OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Tower) {
				int maxFloor = OpenTaiko.stageSongSelect.rChoosenSong.arスコア[5].譜面情報.nTotalFloor;

				OpenTaiko.actTextConsole.tPrint(0, 0, CTextConsole.EFontType.White, maxFloor.ToString());

				int nightTime = Math.Max(140, maxFloor / 2);

				int currentTowerType = Array.IndexOf(OpenTaiko.Skin.Game_Tower_Names, OpenTaiko.stageSongSelect.rChoosenSong.arスコア[5].譜面情報.nTowerType);

				if (currentTowerType < 0 || currentTowerType >= OpenTaiko.Skin.Game_Tower_Ptn)
					currentTowerType = 0;

				#region [Tower lower background]

				float nextPositionMax140 = Math.Min((OpenTaiko.stage演奏ドラム画面.actPlayInfo.NowMeasure[0] + 1) / (float)nightTime, 1f);

				if (bFloorChanged == true)
					ctSlideAnimation.Start(0, 1000, 120f / ((float)OpenTaiko.stage演奏ドラム画面.actPlayInfo.dbBPM[0] * OpenTaiko.ConfigIni.SongPlaybackSpeed), OpenTaiko.Timer);

				float progressFactor = (nextPositionMax140 - currentFloorPositionMax140) * (ctSlideAnimation.CurrentValue / 1000f);



				#region [Skybox]

				//int skyboxYPosition = (int)((TJAPlayer3.Tx.Tower_Sky_Gradient.szテクスチャサイズ.Height - TJAPlayer3.Skin.Game_Tower_Sky_Gradient_Size[1]) * (1f - (currentFloorPositionMax140 + progressFactor)));

				//TJAPlayer3.Tx.Tower_Sky_Gradient?.t2D描画(TJAPlayer3.Skin.Game_Tower_Sky_Gradient[0], TJAPlayer3.Skin.Game_Tower_Sky_Gradient[1], 
				//new Rectangle(0, skyboxYPosition, TJAPlayer3.Skin.Game_Tower_Sky_Gradient_Size[0], TJAPlayer3.Skin.Game_Tower_Sky_Gradient_Size[1]));

				if (!OpenTaiko.stage演奏ドラム画面.bPAUSE) DownScript.Update();
				DownScript.Draw();

				#endregion


				#region [Tower body]

				progressFactor = ctSlideAnimation.CurrentValue / 1000f;

				int currentTower = currentTowerType;

				// Will implement the roof later, need the beforehand total floor count calculation before
				int nextTowerBase = ((OpenTaiko.stage演奏ドラム画面.actPlayInfo.NowMeasure[0] + 1) / 10) % OpenTaiko.Skin.Game_Tower_Ptn_Base[currentTower];
				int towerBase = (OpenTaiko.stage演奏ドラム画面.actPlayInfo.NowMeasure[0] / 10) % OpenTaiko.Skin.Game_Tower_Ptn_Base[currentTower];

				int currentDeco = OpenTaiko.stage演奏ドラム画面.actPlayInfo.NowMeasure[0] % OpenTaiko.Skin.Game_Tower_Ptn_Deco[currentTower];
				int nextDeco = (OpenTaiko.stage演奏ドラム画面.actPlayInfo.NowMeasure[0] + 1) % OpenTaiko.Skin.Game_Tower_Ptn_Deco[currentTower];

				// Microfix for the first floor suddenly changing texture
				if (OpenTaiko.stage演奏ドラム画面.actPlayInfo.NowMeasure[0] == 0 && OpenTaiko.Skin.Game_Tower_Ptn_Deco[currentTower] > 1)
					currentDeco++;
				if (OpenTaiko.stage演奏ドラム画面.actPlayInfo.NowMeasure[0] == 0 && OpenTaiko.Skin.Game_Tower_Ptn_Base[currentTower] > 1)
					towerBase++;

				int widthChange = (int)(progressFactor * OpenTaiko.Skin.Game_Tower_Floors_Move[0]);
				int heightChange = (int)(progressFactor * OpenTaiko.Skin.Game_Tower_Floors_Move[1]);

				// Current trunk
				if (OpenTaiko.stage演奏ドラム画面.actPlayInfo.NowMeasure[0] < maxFloor)
					OpenTaiko.Tx.Tower_Base[currentTower][towerBase]?.t2D下中央基準描画(
						OpenTaiko.Skin.Game_Tower_Floors_Body[0] + widthChange,
						OpenTaiko.Skin.Game_Tower_Floors_Body[1] + heightChange); // 316 + 360
				else
					OpenTaiko.Tx.Tower_Top[currentTower]?.t2D下中央基準描画(
						OpenTaiko.Skin.Game_Tower_Floors_Body[0] + widthChange,
						OpenTaiko.Skin.Game_Tower_Floors_Body[1] + heightChange);

				// Current deco
				OpenTaiko.Tx.Tower_Deco[currentTower][currentDeco]?.t2D下中央基準描画(
					OpenTaiko.Skin.Game_Tower_Floors_Deco[0] + widthChange,
					OpenTaiko.Skin.Game_Tower_Floors_Deco[1] + heightChange);

				int originY = OpenTaiko.Skin.Game_Tower_Floors_Move[1] - heightChange;

				// Next trunk
				if (OpenTaiko.stage演奏ドラム画面.actPlayInfo.NowMeasure[0] + 1 < maxFloor)
					OpenTaiko.Tx.Tower_Base[currentTower][nextTowerBase]?.t2D下中央基準描画(
						OpenTaiko.Skin.Game_Tower_Floors_Body[0] - OpenTaiko.Skin.Game_Tower_Floors_Move[0] + widthChange,
						OpenTaiko.Skin.Game_Tower_Floors_Body[1] - OpenTaiko.Skin.Game_Tower_Floors_Move[1] + heightChange,
						new Rectangle(0, originY, OpenTaiko.Tx.Tower_Base[currentTower][nextTowerBase].szTextureSize.Width, OpenTaiko.Tx.Tower_Base[currentTower][nextTowerBase].szTextureSize.Height - originY));
				else if (OpenTaiko.stage演奏ドラム画面.actPlayInfo.NowMeasure[0] + 1 == maxFloor) {
					OpenTaiko.Tx.Tower_Top[currentTower]?.t2D下中央基準描画(
						OpenTaiko.Skin.Game_Tower_Floors_Body[0] - OpenTaiko.Skin.Game_Tower_Floors_Move[0] + widthChange,
						OpenTaiko.Skin.Game_Tower_Floors_Body[1] - OpenTaiko.Skin.Game_Tower_Floors_Move[1] + heightChange,
						new Rectangle(0, originY, OpenTaiko.Tx.Tower_Top[currentTower].szTextureSize.Width, OpenTaiko.Tx.Tower_Top[currentTower].szTextureSize.Height - originY));
				}

				// Next deco
				if (OpenTaiko.stage演奏ドラム画面.actPlayInfo.NowMeasure[0] + 1 <= maxFloor)
					OpenTaiko.Tx.Tower_Deco[currentTower][nextDeco]?.t2D下中央基準描画(
						OpenTaiko.Skin.Game_Tower_Floors_Deco[0] - OpenTaiko.Skin.Game_Tower_Floors_Move[0] + widthChange,
						OpenTaiko.Skin.Game_Tower_Floors_Deco[1] - OpenTaiko.Skin.Game_Tower_Floors_Move[1] + heightChange);


				#endregion

				#region [Climbing don]

				bool ctIsTired = !((CFloorManagement.CurrentNumberOfLives / (float)CFloorManagement.MaxNumberOfLives) >= 0.2f && !(CFloorManagement.CurrentNumberOfLives == 1 && CFloorManagement.MaxNumberOfLives != 1));

				bool stageEnded = OpenTaiko.stage演奏ドラム画面.ePhaseID == CStage.EPhase.Game_EndStage || OpenTaiko.stage演奏ドラム画面.ePhaseID == CStage.EPhase.Game_STAGE_CLEAR_FadeOut || CFloorManagement.CurrentNumberOfLives == 0;

				if (bFloorChanged == true) {
					float floorBPM = (float)(OpenTaiko.stage演奏ドラム画面.actPlayInfo.dbBPM[0] * OpenTaiko.ConfigIni.SongPlaybackSpeed);
					ctClimbDuration.Start(0, 1500, 120f / floorBPM, OpenTaiko.Timer);
					ctStandingAnimation.Start(0, 1000, (60000f / floorBPM) * OpenTaiko.Skin.Characters_Beat_Tower_Standing[currentCharacter] / OpenTaiko.Skin.Characters_Tower_Standing_Ptn[currentCharacter], OpenTaiko.Timer);
					ctClimbingAnimation.Start(0, 1000, (120000f / floorBPM) / OpenTaiko.Skin.Characters_Tower_Climbing_Ptn[currentCharacter], OpenTaiko.Timer);
					ctRunningAnimation.Start(0, 1000, (60000f / floorBPM) / OpenTaiko.Skin.Characters_Tower_Running_Ptn[currentCharacter], OpenTaiko.Timer);
					ctStandTiredAnimation.Start(0, 1000, (60000f / floorBPM) * OpenTaiko.Skin.Characters_Beat_Tower_Standing_Tired[currentCharacter] / OpenTaiko.Skin.Characters_Tower_Standing_Tired_Ptn[currentCharacter], OpenTaiko.Timer);
					ctClimbTiredAnimation.Start(0, 1000, (120000f / floorBPM) / OpenTaiko.Skin.Characters_Tower_Climbing_Tired_Ptn[currentCharacter], OpenTaiko.Timer);
					ctRunTiredAnimation.Start(0, 1000, (60000f / floorBPM) / OpenTaiko.Skin.Characters_Tower_Running_Tired_Ptn[currentCharacter], OpenTaiko.Timer);
				}

				bool isClimbing = ctClimbDuration.CurrentValue > 0 && ctClimbDuration.CurrentValue < 1500;

				if (stageEnded && !TowerFinished && !isClimbing) {
					float floorBPM = (float)(OpenTaiko.stage演奏ドラム画面.actPlayInfo.dbBPM[0] * OpenTaiko.ConfigIni.SongPlaybackSpeed);
					ctClearAnimation.Start(0, 20000, (60000f / floorBPM) * OpenTaiko.Skin.Characters_Beat_Tower_Clear[currentCharacter] / OpenTaiko.Skin.Characters_Tower_Clear_Ptn[currentCharacter], OpenTaiko.Timer);
					ctClearTiredAnimation.Start(0, 20000, (60000f / floorBPM) * OpenTaiko.Skin.Characters_Beat_Tower_Clear_Tired[currentCharacter] / OpenTaiko.Skin.Characters_Tower_Clear_Tired_Ptn[currentCharacter], OpenTaiko.Timer);
					ctFailAnimation.Start(0, 20000, (60000f / floorBPM) * OpenTaiko.Skin.Characters_Beat_Tower_Fail[currentCharacter] / OpenTaiko.Skin.Characters_Tower_Fail_Ptn[currentCharacter], OpenTaiko.Timer);
					TowerFinished = true;
				}

				if (isClimbing) {
					// Tired Climb
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
				} else {
					// Fail
					if (OpenTaiko.Skin.Characters_Tower_Fail_Ptn[currentCharacter] > 0 && CFloorManagement.CurrentNumberOfLives == 0) {
						int animChar = OpenTaiko.Skin.Characters_Tower_Fail_IsLooping[currentCharacter] ?
							ctFailAnimation.CurrentValue % OpenTaiko.Skin.Characters_Tower_Fail_Ptn[currentCharacter] :
							Math.Min(ctFailAnimation.CurrentValue, OpenTaiko.Skin.Characters_Tower_Fail_Ptn[currentCharacter] - 1);
						OpenTaiko.Tx.Characters_Tower_Fail[currentCharacter][animChar]?.t2D拡大率考慮下中心基準描画(OpenTaiko.Skin.Game_Tower_Don[0], OpenTaiko.Skin.Game_Tower_Don[1]);
					}
					// Tired Clear
					else if (ctIsTired && stageEnded && OpenTaiko.Skin.Characters_Tower_Clear_Tired_Ptn[currentCharacter] > 0 && CFloorManagement.CurrentNumberOfLives > 0) {
						int animChar = OpenTaiko.Skin.Characters_Tower_Clear_Tired_IsLooping[currentCharacter] ?
							ctClearTiredAnimation.CurrentValue % OpenTaiko.Skin.Characters_Tower_Clear_Tired_Ptn[currentCharacter] :
							Math.Min(ctClearTiredAnimation.CurrentValue, OpenTaiko.Skin.Characters_Tower_Clear_Tired_Ptn[currentCharacter] - 1);
						OpenTaiko.Tx.Characters_Tower_Clear_Tired[currentCharacter][animChar]?.t2D拡大率考慮下中心基準描画(OpenTaiko.Skin.Game_Tower_Don[0], OpenTaiko.Skin.Game_Tower_Don[1]);
					}
					// Clear
					else if (stageEnded && OpenTaiko.Skin.Characters_Tower_Clear_Ptn[currentCharacter] > 0 && CFloorManagement.CurrentNumberOfLives > 0) {
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
				}

				#endregion

				#region [Miss icon]

				if (CFloorManagement.InvincibilityFrames != null && CFloorManagement.InvincibilityFrames.CurrentValue < CFloorManagement.InvincibilityDurationSpeedDependent) {
					if (OpenTaiko.Tx.Tower_Miss != null)
						OpenTaiko.Tx.Tower_Miss.Opacity = Math.Min(255, 1000 - CFloorManagement.InvincibilityFrames.CurrentValue);
					OpenTaiko.Tx.Tower_Miss?.t2D下中央基準描画(OpenTaiko.Skin.Game_Tower_Miss[0], OpenTaiko.Skin.Game_Tower_Miss[1]);
				}

				#endregion

				ctSlideAnimation?.Tick();
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
			} else if (!OpenTaiko.stage演奏ドラム画面.bDoublePlay && OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0] != (int)Difficulty.Dan) {
				if (!IsDownNotFound) {
					if (!OpenTaiko.stage演奏ドラム画面.bPAUSE) DownScript?.Update();
					DownScript?.Draw();
				}
			}


			#endregion

			return base.Draw();
		}

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

		private ScriptBG UpScript;
		private ScriptBG DownScript;

		private TitleTextureKey ttkTouTatsuKaiSuu;
		private TitleTextureKey ttkKai;
		private CCachedFontRenderer pfTowerText;

		private bool bFloorChanged = false;
		private int currentCharacter;
		private CCounter ctSlideAnimation;
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

		private CCounter ct炎;

		private bool IsUpNotFound;
		private bool IsDownNotFound;

		private EFIFOモード eFadeMode;
		//-----------------
		#endregion
	}
}
