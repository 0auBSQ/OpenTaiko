using System.Diagnostics;
using System.Drawing;
using System.Text;
using DiscordRPC;
using FDK;
using static OpenTaiko.CActSelect曲リスト;

namespace OpenTaiko {
	internal class CStage結果 : CStage {
		// Modals Lua management

		public CLuaModalScript lcModal { get; private set; }

		public void RefreshSkin() {
			lcModal?.Dispose();
			lcModal = new CLuaModalScript(CSkin.Path("Modules/Modal"));

		}

		// プロパティ

		public STDGBVALUE<bool> b新記録スキル;
		public STDGBVALUE<bool> b新記録スコア;
		public STDGBVALUE<bool> b新記録ランク;
		public STDGBVALUE<float> fPerfect率;
		public STDGBVALUE<float> fGreat率;
		public STDGBVALUE<float> fGood率;
		public STDGBVALUE<float> fPoor率;
		public STDGBVALUE<float> fMiss率;
		public STDGBVALUE<bool> bオート;        // #23596 10.11.16 add ikanick
											 //        10.11.17 change (int to bool) ikanick

		public STDGBVALUE<int> nランク値;
		public STDGBVALUE<int> n演奏回数;
		public STDGBVALUE<int> nScoreRank;
		public int n総合ランク値;

		public int[] nクリア = { 0, 0, 0, 0, 0 };        //0:未クリア 1:クリア 2:フルコンボ 3:ドンダフルコンボ
		public int[] nスコアランク = { 0, 0, 0, 0, 0 };  //0:未取得 1:白粋 2:銅粋 3:銀粋 4:金雅 5:桃雅 6:紫雅 7:虹極
		public int[] nHighScore = { 0, 0, 0, 0, 0 };

		public CDTX.CChip[] r空うちドラムチップ;
		public STDGBVALUE<CScoreIni.C演奏記録> st演奏記録;


		// コンストラクタ

		public CStage結果() {
			this.st演奏記録.Drums = new CScoreIni.C演奏記録();
			this.st演奏記録.Guitar = new CScoreIni.C演奏記録();
			this.st演奏記録.Bass = new CScoreIni.C演奏記録();
			this.st演奏記録.Taiko = new CScoreIni.C演奏記録();
			this.r空うちドラムチップ = new CDTX.CChip[10];
			this.n総合ランク値 = -1;
			this.nチャンネル0Atoレーン07 = new int[] { 1, 2, 3, 4, 5, 7, 6, 1, 7, 0 };
			base.eStageID = CStage.EStage.Results;
			base.ePhaseID = CStage.EPhase.Common_NORMAL;
			base.IsDeActivated = true;
			base.ChildActivities.Add(this.actParameterPanel = new CActResultParameterPanel());
			base.ChildActivities.Add(this.actSongBar = new CActResultSongBar());
			base.ChildActivities.Add(this.actOption = new CActオプションパネル());
			base.ChildActivities.Add(this.actFI = new CActFIFOResult());
			base.ChildActivities.Add(this.actFO = new CActFIFOBlack());
		}


		public bool isAutoDisabled(int player) {
			return ((player != 1 && !OpenTaiko.ConfigIni.bAutoPlay[player])
					|| (player == 1 && !OpenTaiko.ConfigIni.bAutoPlay[player] && !OpenTaiko.ConfigIni.bAIBattleMode));
		}


		public int GetTowerScoreRank() {
			int tmpClear = 0;
			double progress = CFloorManagement.LastRegisteredFloor / ((double)OpenTaiko.stageSongSelect.rChoosenSong.arスコア[5].譜面情報.nTotalFloor);

			// Clear badges : 10% (E), 25% (D), 50% (C), 75% (B), Clear (A), FC (S), DFC (X)
			bool[] conditions =
			{
				progress >= 0.1,
				progress >= 0.25,
				progress >= 0.5,
				progress >= 0.75,
				progress == 1 && CFloorManagement.CurrentNumberOfLives > 0,
				OpenTaiko.stage演奏ドラム画面.CChartScore[0].nMiss == 0 && OpenTaiko.stage演奏ドラム画面.CChartScore[0].nMine == 0,
				OpenTaiko.stage演奏ドラム画面.CChartScore[0].nGood == 0
				/*
				progress == 1 && CFloorManagement.CurrentNumberOfLives > 0,
				this.st演奏記録.Drums.nMiss数 == 0,
				this.st演奏記録.Drums.nGreat数 == 0
				*/
			};

			for (int i = 0; i < conditions.Length; i++) {
				if (conditions[i] == true)
					tmpClear++;
				else
					break;
			}

			return tmpClear;
		}

		// CStage 実装

		public override void Activate() {

			Trace.TraceInformation("結果ステージを活性化します。");
			Trace.Indent();
			bAddedToRecentlyPlayedSongs = false;
			try {
				/*
				 * Notes about the difference between Replay - Save statuses and the "Assisted clear" clear status
				 * 
				 * - The values for replay files are 0 if no status, while for save files they start by -1
				 * - The "Assisted clear" status is used on the save files, but NOT on the replay files
				 * - The "Assisted clear" status is also not used in the coins evaluations
				*/
				int[] ClearStatus_Replay = new int[5] { 0, 0, 0, 0, 0 };
				int[] ScoreRank_Replay = new int[5] { 0, 0, 0, 0, 0 };

				int[] clearStatuses =
				{
					-1,
					-1,
					-1,
					-1,
					-1
				};

				int[] scoreRanks =
				{
					-1,
					-1,
					-1,
					-1,
					-1
				};

				bool[] assistedClear =
				{
					(OpenTaiko.stageSongSelect.actPlayOption.tGetModMultiplier(CActPlayOption.EBalancingType.SCORE, false, 0) < 1f),
					(OpenTaiko.stageSongSelect.actPlayOption.tGetModMultiplier(CActPlayOption.EBalancingType.SCORE, false, 1) < 1f),
					(OpenTaiko.stageSongSelect.actPlayOption.tGetModMultiplier(CActPlayOption.EBalancingType.SCORE, false, 2) < 1f),
					(OpenTaiko.stageSongSelect.actPlayOption.tGetModMultiplier(CActPlayOption.EBalancingType.SCORE, false, 3) < 1f),
					(OpenTaiko.stageSongSelect.actPlayOption.tGetModMultiplier(CActPlayOption.EBalancingType.SCORE, false, 4) < 1f)
				};

				{
					#region [ 初期化 ]
					//---------------------
					this.eフェードアウト完了時の戻り値 = E戻り値.継続;
					this.bアニメが完了 = false;
					this.bIsCheckedWhetherResultScreenShouldSaveOrNot = false;              // #24609 2011.3.14 yyagi
					this.n最後に再生したHHのWAV番号 = -1;
					this.n最後に再生したHHのチャンネル番号 = 0;

					for (int i = 0; i < 3; i++) {
						this.b新記録スキル[i] = false;
						this.b新記録スコア[i] = false;
						this.b新記録ランク[i] = false;
					}
					//---------------------
					#endregion

					#region [ Results calculus ]
					//---------------------

					if (OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0] != (int)Difficulty.Dan && OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0] != (int)Difficulty.Tower) {
						for (int p = 0; p < OpenTaiko.ConfigIni.nPlayerCount; p++) {
							var ccf = OpenTaiko.stage演奏ドラム画面.CChartScore[p];

							this.nクリア[p] = 0;
							if (HGaugeMethods.UNSAFE_FastNormaCheck(p)) {
								this.nクリア[p] = 2;
								if (ccf.nMiss == 0 && ccf.nMine == 0) {
									this.nクリア[p] = 3;
									if (ccf.nGood == 0) this.nクリア[p] = 4;
								}

								if (assistedClear[p]) this.nクリア[p] = 1;

								clearStatuses[p] = this.nクリア[p] - 1;

							}

							if ((int)OpenTaiko.stage演奏ドラム画面.actScore.Get(p) < 500000) {
								this.nスコアランク[p] = 0;
							} else {
								var sr = OpenTaiko.stage演奏ドラム画面.ScoreRank.ScoreRank[p];

								for (int i = 0; i < 7; i++) {
									if ((int)OpenTaiko.stage演奏ドラム画面.actScore.Get(p) >= sr[i]) {
										this.nスコアランク[p] = i + 1;
									}
								}
							}
							scoreRanks[p] = this.nスコアランク[p] - 1;

						}


					}

					//---------------------
					#endregion

					#region [ Saves calculus ]
					//---------------------

					// Clear and score ranks



					if (OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0] != (int)Difficulty.Dan && OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0] != (int)Difficulty.Tower) {
						// Regular (Ensou game) Score and Score Rank saves

						#region [Regular saves]

						for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
							int diff = OpenTaiko.stageSongSelect.nChoosenSongDifficulty[i];

							ClearStatus_Replay[i] = this.nクリア[i];
							ScoreRank_Replay[i] = this.nスコアランク[i];
						}

						#endregion

					} else if (OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Dan) {
						/* == Specific format for DaniDoujou charts ==
						**
						** Higher is better, takes the Clear0 spot (Usually the spot allocated for Kantan Clear crowns)
						**
						** 0 (Fugoukaku, no insign)
						** Silver Iki (Clear) : 1 (Red Goukaku) / 2 (Gold Goukaku)
						** Gold Iki (Full Combo) : 3 (Red Goukaku) / 4 (Gold Goukaku)
						** Rainbow Iki (Donda Full Combo) : 5 (Red Goukaku) / 6 (Gold Goukaku)
						**
						*/

						#region [Dan scores]

						Exam.Status examStatus = OpenTaiko.stage演奏ドラム画面.actDan.GetExamStatus(OpenTaiko.stage結果.st演奏記録.Drums.Dan_C);

						int clearValue = 0;

						if (examStatus != Exam.Status.Failure) {
							// Red Goukaku
							clearValue += 1;

							// Gold Goukaku
							if (examStatus == Exam.Status.Better_Success)
								clearValue += 1;

							// Gold Iki
							if (this.st演奏記録.Drums.nBadCount == 0) {
								clearValue += 2;

								// Rainbow Iki
								if (this.st演奏記録.Drums.nOkCount == 0)
									clearValue += 2;
							}

							if (assistedClear[0]) clearStatuses[0] = (examStatus == Exam.Status.Better_Success) ? 1 : 0;
							else clearStatuses[0] = clearValue + 1;

						}

						if (isAutoDisabled(0)) {
							ClearStatus_Replay[0] = clearValue;
						}

						// this.st演奏記録[0].nクリア[0] = Math.Max(ini[0].stセクション[0].nクリア[0], clearValue);

						// Unlock dan grade
						if (clearValue > 0 && !OpenTaiko.ConfigIni.bAutoPlay[0]) {
							/*
							this.newGradeGranted = TJAPlayer3.NamePlateConfig.tUpdateDanTitle(TJAPlayer3.stage選曲.r確定された曲.strタイトル.Substring(0, 2),
								clearValue % 2 == 0,
								(clearValue - 1) / 2,
								TJAPlayer3.SaveFile);
							*/

							this.newGradeGranted = OpenTaiko.SaveFileInstances[OpenTaiko.SaveFile].tUpdateDanTitle(OpenTaiko.stageSongSelect.rChoosenSong.ldTitle.GetString("").Substring(0, 2),
								clearValue % 2 == 0,
								(clearValue - 1) / 2);
						}

						#endregion

					} else if (OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Tower) {
						// Clear if top reached, then FC or DFC like any regular chart
						// Score Rank cointains highest reached floor

						#region [Tower scores]

						int tmpClear = GetTowerScoreRank();

						if (tmpClear != 0) clearStatuses[0] = assistedClear[0] ? 0 : tmpClear;

						if (isAutoDisabled(0)) {
							ClearStatus_Replay[0] = tmpClear;
						}

						#endregion

					}

					//---------------------
					#endregion

				}

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
					CDTX.ELevelIcon levelIcon = OpenTaiko.stageSongSelect.rChoosenSong.nLevelIcon[diff];

					return (diffArr[Math.Min(diff, 6)] + "Lv." + level + diffArrIcon[(int)levelIcon]);
				}

				string details = OpenTaiko.ConfigIni.SendDiscordPlayingInformation ? OpenTaiko.stageSongSelect.rChoosenSong.ldTitle.GetString("")
				+ diffToString(OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0]) : "";

				// Byte count must be used instead of String.Length.
				// The byte count is what Discord is concerned with. Some chars are greater than one byte.
				if (Encoding.UTF8.GetBytes(details).Length > 128) {
					byte[] details_byte = Encoding.UTF8.GetBytes(details);
					Array.Resize(ref details_byte, 128);
					details = Encoding.UTF8.GetString(details_byte);
				}

				// Discord Presenseの更新
				OpenTaiko.DiscordClient?.SetPresence(new RichPresence() {
					Details = details,
					State = "Result" + (OpenTaiko.ConfigIni.bAutoPlay[0] == true ? " (Auto)" : ""),
					Timestamps = new Timestamps(OpenTaiko.StartupTime),
					Assets = new Assets() {
						LargeImageKey = OpenTaiko.LargeImageKey,
						LargeImageText = OpenTaiko.LargeImageText,
					}
				});


				#region [Earned medals]

				this.nEarnedMedalsCount[0] = 0;
				this.nEarnedMedalsCount[1] = 0;
				this.nEarnedMedalsCount[2] = 0;
				this.nEarnedMedalsCount[3] = 0;
				this.nEarnedMedalsCount[4] = 0;



				// Medals

				int nTotalHits = this.st演奏記録.Drums.nOkCount + this.st演奏記録.Drums.nBadCount + this.st演奏記録.Drums.nGoodCount;

				double dAccuracyRate = Math.Pow((50 * this.st演奏記録.Drums.nOkCount + 100 * this.st演奏記録.Drums.nGoodCount) / (double)(100 * nTotalHits), 3);

				int diffModifier;
				float starRate;
				float redStarRate;


				float[] modMultipliers =
				{
					OpenTaiko.stageSongSelect.actPlayOption.tGetModMultiplier(CActPlayOption.EBalancingType.COINS, false, 0),
					OpenTaiko.stageSongSelect.actPlayOption.tGetModMultiplier(CActPlayOption.EBalancingType.COINS, false, 1),
					OpenTaiko.stageSongSelect.actPlayOption.tGetModMultiplier(CActPlayOption.EBalancingType.COINS, false, 2),
					OpenTaiko.stageSongSelect.actPlayOption.tGetModMultiplier(CActPlayOption.EBalancingType.COINS, false, 3),
					OpenTaiko.stageSongSelect.actPlayOption.tGetModMultiplier(CActPlayOption.EBalancingType.COINS, false, 4)
				};

				float getCoinMul(int player) {
					var chara = OpenTaiko.Tx.Characters[OpenTaiko.SaveFileInstances[OpenTaiko.GetActualPlayer(player)].data.Character];
					var puchichara = OpenTaiko.Tx.Puchichara[PuchiChara.tGetPuchiCharaIndexByName(OpenTaiko.GetActualPlayer(player))];


					return chara.GetEffectCoinMultiplier() * puchichara.GetEffectCoinMultiplier();
				}

				if (OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Tower) {
					diffModifier = 3;

					int stars = OpenTaiko.stageSongSelect.rChoosenSong.arスコア[(int)Difficulty.Tower].譜面情報.nレベル[(int)Difficulty.Tower];

					starRate = Math.Min(10, stars) / 2;
					redStarRate = Math.Max(0, stars - 10) * 4;

					int maxFloors = OpenTaiko.stageSongSelect.rChoosenSong.arスコア[(int)Difficulty.Tower].譜面情報.nTotalFloor;

					double floorRate = Math.Pow(CFloorManagement.LastRegisteredFloor / (double)maxFloors, 2);
					double lengthBonus = Math.Max(1, maxFloors / 140.0);

					#region [Clear modifier]

					int clearModifier = 0;

					if (this.st演奏記録.Drums.nBadCount == 0) {
						clearModifier = (int)(5 * lengthBonus);
						if (this.st演奏記録.Drums.nOkCount == 0) {
							clearModifier = (int)(12 * lengthBonus);
						}
					}

					#endregion

					// this.nEarnedMedalsCount[0] = stars;
					this.nEarnedMedalsCount[0] = 5 + (int)((diffModifier * (starRate + redStarRate)) * (floorRate * lengthBonus)) + clearModifier;
					this.nEarnedMedalsCount[0] = Math.Max(5, (int)(this.nEarnedMedalsCount[0] * modMultipliers[0] * getCoinMul(0)));
				} else if (OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Dan) {
					int partialScore = 0;

					#region [Clear and Goukaku modifier]

					Exam.Status examStatus = OpenTaiko.stage演奏ドラム画面.actDan.GetExamStatus(OpenTaiko.stage結果.st演奏記録.Drums.Dan_C);

					int clearModifier = -1;
					int goukakuModifier = 0;

					if (examStatus != Exam.Status.Failure) {
						clearModifier = 0;
						if (this.st演奏記録.Drums.nBadCount == 0) {
							clearModifier = 4;
							if (this.st演奏記録.Drums.nOkCount == 0)
								clearModifier = 6;
						}

						if (examStatus == Exam.Status.Better_Success)
							goukakuModifier = 20;
					}

					#endregion

					#region [Partial scores]

					for (int i = 0; i < OpenTaiko.stageSongSelect.rChoosenSong.DanSongs.Count; i++) {
						if (OpenTaiko.stageSongSelect.rChoosenSong.DanSongs[i] != null) {
							int diff = OpenTaiko.stageSongSelect.rChoosenSong.DanSongs[i].Difficulty;
							int stars = OpenTaiko.stageSongSelect.rChoosenSong.DanSongs[i].Level;

							diffModifier = Math.Max(1, Math.Min(3, diff));

							starRate = Math.Min(10, stars) / 2;
							redStarRate = Math.Max(0, stars - 10) * 4;

							partialScore += (int)(diffModifier * (starRate + redStarRate));
						}

					}

					#endregion

					if (clearModifier < 0)
						this.nEarnedMedalsCount[0] = 10;
					else {
						this.nEarnedMedalsCount[0] = 10 + goukakuModifier + clearModifier + (int)(partialScore * dAccuracyRate);
						this.nEarnedMedalsCount[0] = Math.Max(10, (int)(this.nEarnedMedalsCount[0] * modMultipliers[0] * getCoinMul(0)));
					}
				} else {
					for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
						int diff = OpenTaiko.stageSongSelect.nChoosenSongDifficulty[i];
						int stars = OpenTaiko.stageSongSelect.rChoosenSong.arスコア[diff].譜面情報.nレベル[diff];

						diffModifier = Math.Max(1, Math.Min(3, diff));

						starRate = Math.Min(10, stars) / 2;
						redStarRate = Math.Max(0, stars - 10) * 4;

						#region [Clear modifier]

						int[] modifiers = { -1, 0, 2, 3 };

						int clearModifier = modifiers[0];

						if (HGaugeMethods.UNSAFE_FastNormaCheck(i)) {
							clearModifier = modifiers[1] * diffModifier;
							if (OpenTaiko.stage演奏ドラム画面.CChartScore[i].nMiss == 0) {
								clearModifier = modifiers[2] * diffModifier;
								if (OpenTaiko.stage演奏ドラム画面.CChartScore[i].nGood == 0)
									clearModifier = modifiers[3] * diffModifier;
							}
						}

						#endregion

						#region [Score rank modifier]

						int[] srModifiers = { 0, 0, 0, 0, 1, 1, 2, 3 };

						// int s = TJAPlayer3.stage演奏ドラム画面.ScoreRank.ScoreRank[1];

						int scoreRankModifier = srModifiers[0] * diffModifier;

						for (int j = 1; j < 8; j++) {
							if (OpenTaiko.stage演奏ドラム画面.actScore.GetScore(i) >= OpenTaiko.stage演奏ドラム画面.ScoreRank.ScoreRank[i][j - 1])
								scoreRankModifier = srModifiers[j] * diffModifier;
						}

						#endregion

						nTotalHits = OpenTaiko.stage演奏ドラム画面.CChartScore[i].nGood + OpenTaiko.stage演奏ドラム画面.CChartScore[i].nMiss + OpenTaiko.stage演奏ドラム画面.CChartScore[i].nGreat;

						dAccuracyRate = Math.Pow((50 * OpenTaiko.stage演奏ドラム画面.CChartScore[i].nGood + 100 * OpenTaiko.stage演奏ドラム画面.CChartScore[i].nGreat) / (double)(100 * nTotalHits), 3);

						if (clearModifier < 0)
							this.nEarnedMedalsCount[i] = 5;
						else {
							this.nEarnedMedalsCount[i] = 5 + (int)((diffModifier * (starRate + redStarRate)) * dAccuracyRate) + clearModifier + scoreRankModifier;
							this.nEarnedMedalsCount[i] = Math.Max(5, (int)(this.nEarnedMedalsCount[i] * modMultipliers[i] * getCoinMul(i)));
						}
					}
				}

				// ADLIB bonuses : 1 coin per ADLIB
				for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
					// Too broken on some charts, ADLibs should get either no bonus or just extra stats
					//this.nEarnedMedalsCount[i] += Math.Min(10, TJAPlayer3.stage演奏ドラム画面.CChartScore[i].nADLIB);

					if (OpenTaiko.ConfigIni.bAutoPlay[i])
						this.nEarnedMedalsCount[i] = 0;
					if (OpenTaiko.ConfigIni.bAIBattleMode && i == 1)
						this.nEarnedMedalsCount[i] = 0;

					var _sf = OpenTaiko.SaveFileInstances[OpenTaiko.GetActualPlayer(i)];

					if (OpenTaiko.ConfigIni.bAIBattleMode && i == 0) {
						_sf.tRegisterAIBattleModePlay(bClear[0]);
					}

					if (this.nEarnedMedalsCount[i] > 0)
						_sf.tEarnCoins(this.nEarnedMedalsCount[i]);

					if (!OpenTaiko.ConfigIni.bAutoPlay[i]
						&& !(OpenTaiko.ConfigIni.bAIBattleMode && i == 1)) {
						int _cs = -1;
						if (HGaugeMethods.UNSAFE_FastNormaCheck(i)) {
							_cs = 0;
							if (OpenTaiko.stage演奏ドラム画面.CChartScore[i].nMiss == 0) {
								_cs = 1;
								if (OpenTaiko.stage演奏ドラム画面.CChartScore[i].nGood == 0)
									_cs = 2;
							}
						}

						// Unsafe function, it is the only appropriate place to call it
						DBSaves.RegisterPlay(i, clearStatuses[i], scoreRanks[i]);
					}
				}


				//TJAPlayer3.NamePlateConfig.tEarnCoins(this.nEarnedMedalsCount);

				#endregion

				#region [Replay files generation]

				for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
					if (OpenTaiko.ConfigIni.bAutoPlay[i])
						continue;
					if (OpenTaiko.ConfigIni.bAIBattleMode && i == 1)
						continue;
					OpenTaiko.ReplayInstances[i].tResultsRegisterReplayInformations(this.nEarnedMedalsCount[i], ClearStatus_Replay[i], ScoreRank_Replay[i]);
					OpenTaiko.ReplayInstances[i].tSaveReplayFile();
				}

				#endregion

				#region [Modals preprocessing]

				if (OpenTaiko.ConfigIni.nPlayerCount == 1 || OpenTaiko.ConfigIni.bAIBattleMode) {
					mqModals = new ModalQueue(Modal.EModalFormat.Full);
				} else if (OpenTaiko.ConfigIni.nPlayerCount == 2) {
					mqModals = new ModalQueue(Modal.EModalFormat.Half);
				} else if (OpenTaiko.ConfigIni.nPlayerCount == 3 || OpenTaiko.ConfigIni.nPlayerCount == 4) {
					mqModals = new ModalQueue(Modal.EModalFormat.Half_4P);
				} else if (OpenTaiko.ConfigIni.nPlayerCount == 5) {
					mqModals = new ModalQueue(Modal.EModalFormat.Half_5P);
				}

				for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
					if (OpenTaiko.ConfigIni.bAutoPlay[i] || OpenTaiko.ConfigIni.bAIBattleMode && i == 1) continue;

					if (this.nEarnedMedalsCount[i] > 0)
						mqModals.tAddModal(
							new Modal(
								Modal.EModalType.Coin,
								0,
								(long)this.nEarnedMedalsCount[i],
								OpenTaiko.SaveFileInstances[OpenTaiko.GetActualPlayer(i)].data.Medals
								),
							i);

					// Check unlockables
					{
						OpenTaiko.Databases.DBNameplateUnlockables.tGetUnlockedItems(i, mqModals);
						OpenTaiko.Databases.DBSongUnlockables.tGetUnlockedItems(i, mqModals);

						foreach (var puchi in OpenTaiko.Tx.Puchichara) {
							puchi.tGetUnlockedItems(i, mqModals);
						}

						foreach (var chara in OpenTaiko.Tx.Characters) {
							chara.tGetUnlockedItems(i, mqModals);
						}
					}

				}

				displayedModals = null;

				#endregion

				OpenTaiko.stageSongSelect.actSongList.bFirstCrownLoad = false;

				this.ctPhase1 = null;
				this.ctPhase2 = null;
				this.ctPhase3 = null;
				examsShift = 0;

				Dan_Plate = OpenTaiko.tテクスチャの生成(Path.GetDirectoryName(OpenTaiko.DTX.strファイル名の絶対パス) + @$"{Path.DirectorySeparatorChar}Dan_Plate.png");

				base.Activate();


				ctShine_Plate = new CCounter(0, 1000, 1, OpenTaiko.Timer);
				ctWork_Plate = new CCounter(0, 4000, 1, OpenTaiko.Timer);

				if (OpenTaiko.Tx.TowerResult_Background != null)
					ctTower_Animation = new CCounter(0, OpenTaiko.Tx.TowerResult_Background.szTextureSize.Height - OpenTaiko.Skin.Resolution[1], 25, OpenTaiko.Timer);
				else
					ctTower_Animation = new CCounter();


				ctDanSongInfoChange = new CCounter(0, 3000, 1, OpenTaiko.Timer);
				ctDanSongInfoChange.CurrentValue = 255;

				b音声再生 = false;
				this.EndAnime = false;

				if (OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Tower) {
					this.ttkMaxFloors = new TitleTextureKey("/" + OpenTaiko.stageSongSelect.rChoosenSong.arスコア[5].譜面情報.nTotalFloor.ToString() + CLangManager.LangInstance.GetString("TOWER_FLOOR_INITIAL"), pfTowerText48, Color.Black, Color.Transparent, 700);
					this.ttkToutatsu = new TitleTextureKey(CLangManager.LangInstance.GetString("TOWER_FLOOR_REACHED"), pfTowerText48, Color.White, Color.Black, 700);
					this.ttkTen = new TitleTextureKey(CLangManager.LangInstance.GetString("TOWER_SCORE_INITIAL"), pfTowerText, Color.Black, Color.Transparent, 700);
					this.ttkReachedFloor = new TitleTextureKey(CFloorManagement.LastRegisteredFloor.ToString(), pfTowerText72, Color.Orange, Color.Black, 700);
					this.ttkScore = new TitleTextureKey(CLangManager.LangInstance.GetString("TOWER_SCORE"), pfTowerText, Color.Black, Color.Transparent, 700);
					this.ttkRemaningLifes = new TitleTextureKey(CFloorManagement.CurrentNumberOfLives.ToString() + " / " + CFloorManagement.MaxNumberOfLives.ToString(), pfTowerText, Color.Black, Color.Transparent, 700);
					this.ttkScoreCount = new TitleTextureKey(OpenTaiko.stage演奏ドラム画面.actScore.GetScore(0).ToString(), pfTowerText, Color.Black, Color.Transparent, 700);
				} else if (OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Dan) {
					Background = new ResultBG(CSkin.Path($@"{TextureLoader.BASE}{TextureLoader.DANRESULT}Script.lua"));
					Background.Init();
				} else if (OpenTaiko.ConfigIni.bAIBattleMode) {
					Background = new ResultBG(CSkin.Path($@"{TextureLoader.BASE}{TextureLoader.RESULT}AIBattle{Path.DirectorySeparatorChar}Script.lua"));
					Background.Init();
				} else {
					//Luaに移植する時にコメントアウトを解除
					Background = new ResultBG(CSkin.Path($@"{TextureLoader.BASE}{TextureLoader.RESULT}{Path.DirectorySeparatorChar}Script.lua"));
					Background.Init();
				}

				this.ttkDanTitles = new TitleTextureKey[OpenTaiko.stageSongSelect.rChoosenSong.DanSongs.Count];

				for (int i = 0; i < OpenTaiko.stageSongSelect.rChoosenSong.DanSongs.Count; i++) {
					this.ttkDanTitles[i] = new TitleTextureKey(OpenTaiko.stageSongSelect.rChoosenSong.DanSongs[i].bTitleShow
						? "???"
						: OpenTaiko.stageSongSelect.rChoosenSong.DanSongs[i].Title,
						pfDanTitles,
						Color.White,
						Color.Black,
						700);
				}
			} finally {
				Trace.TraceInformation("結果ステージの活性化を完了しました。");
				Trace.Unindent();
			}

			if (OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0] != (int)Difficulty.Dan && OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0] != (int)Difficulty.Tower)
				bgmResultIn.tPlay();
		}
		public override void DeActivate() {
			OpenTaiko.tDisposeSafely(ref Background);

			if (this.rResultSound != null) {
				OpenTaiko.SoundManager.tDisposeSound(this.rResultSound);
				this.rResultSound = null;
			}

			if (this.ct登場用 != null) {
				this.ct登場用 = null;
			}
			Dan_Plate?.Dispose();

			base.DeActivate();
		}
		public override void CreateManagedResource() {
			this.pfTowerText = HPrivateFastFont.tInstantiateMainFont(OpenTaiko.Skin.TowerResult_Font_TowerText);
			this.pfTowerText48 = HPrivateFastFont.tInstantiateMainFont(OpenTaiko.Skin.TowerResult_Font_TowerText48);
			this.pfTowerText72 = HPrivateFastFont.tInstantiateMainFont(OpenTaiko.Skin.TowerResult_Font_TowerText72);

			this.pfDanTitles = HPrivateFastFont.tInstantiateMainFont(OpenTaiko.Skin.DanResult_Font_DanTitles_Size);

			base.CreateManagedResource();
		}
		public override void ReleaseManagedResource() {

			if (OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Tower) {
				OpenTaiko.tDisposeSafely(ref pfTowerText);
				OpenTaiko.tDisposeSafely(ref pfTowerText48);
				OpenTaiko.tDisposeSafely(ref pfTowerText72);
			} else if (OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Dan) {
				OpenTaiko.tDisposeSafely(ref pfDanTitles);
			}

			base.ReleaseManagedResource();
		}
		public override int Draw() {
			if (!base.IsDeActivated) {

				ctShine_Plate.TickLoop();

				// int num;

				if (base.IsFirstDraw) {
					this.ct登場用 = new CCounter(0, 100, 5, OpenTaiko.Timer);
					this.actFI.tフェードイン開始();
					base.ePhaseID = CStage.EPhase.Common_FADEIN;

					if (this.rResultSound != null) {
						this.rResultSound.PlayStart();
					}

					base.IsFirstDraw = false;
				}
				this.bアニメが完了 = true;
				if (this.ct登場用.IsTicked) {
					this.ct登場用.Tick();
					if (this.ct登場用.IsEnded) {
						this.ct登場用.Stop();
					} else {
						this.bアニメが完了 = false;
					}
				}

				// 描画

				Background?.Update();
				Background?.Draw();

				if (OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0] != (int)Difficulty.Dan && OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0] != (int)Difficulty.Tower) {
					#region [Ensou game result screen]

					if (!b音声再生 && !bgmResultIn.bIsPlaying) {
						bgmResultLoop.tPlay();
						b音声再生 = true;
					}
					if (!OpenTaiko.ConfigIni.bAIBattleMode) {
						/*
						if (TJAPlayer3.Tx.Result_Background != null)
						{
							bool is1P = (TJAPlayer3.ConfigIni.nPlayerCount == 1);
							bool is2PSide = TJAPlayer3.P1IsBlue();
							int mountainTexId = (is2PSide && is1P) ? 2 : 0;

							int CloudType = 0;
							float MountainAppearValue = this.actParameterPanel.MountainAppearValue;

							//2000 + (this.actParameterPanel.ctゲージアニメ.n終了値 * 66) + 8360 - 85;
							int gaugeAnimFactors = 0;

							if (this.actParameterPanel.ct全体進行.n現在の値 >= MountainAppearValue && is1P)
							{
								#region [Mountain Bump (1P only)]

								if (bClear[0])
								{
									//int gaugeAnimationFactor = (this.actParameterPanel.ct全体進行.n現在の値 - (10275 + ((int)this.actParameterPanel.ctゲージアニメ[0].n終了値 * 66))) * 3;

									gaugeAnimFactors = (this.actParameterPanel.ct全体進行.n現在の値 - (int)MountainAppearValue) * 3;

									TJAPlayer3.Tx.Result_Background[1].Opacity = gaugeAnimFactors;

									TJAPlayer3.Tx.Result_Mountain[mountainTexId + 1].Opacity = gaugeAnimFactors;
									TJAPlayer3.Tx.Result_Mountain[mountainTexId + 0].Opacity = 255 - gaugeAnimFactors;

									if (this.actParameterPanel.ctMountain_ClearIn.n現在の値 <= 90)
									{
										TJAPlayer3.Tx.Result_Mountain[mountainTexId + 1].vc拡大縮小倍率.Y = 1.0f - (float)Math.Sin((float)this.actParameterPanel.ctMountain_ClearIn.n現在の値 * (Math.PI / 180)) * 0.18f;
									}
									else if (this.actParameterPanel.ctMountain_ClearIn.n現在の値 <= 225)
									{
										TJAPlayer3.Tx.Result_Mountain[mountainTexId + 1].vc拡大縮小倍率.Y = 0.82f + (float)Math.Sin((float)(this.actParameterPanel.ctMountain_ClearIn.n現在の値 - 90) / 1.5f * (Math.PI / 180)) * 0.58f;
									}
									else if (this.actParameterPanel.ctMountain_ClearIn.n現在の値 <= 245)
									{
										TJAPlayer3.Tx.Result_Mountain[mountainTexId + 1].vc拡大縮小倍率.Y = 1.4f;
									}
									else if (this.actParameterPanel.ctMountain_ClearIn.n現在の値 <= 335)
									{
										TJAPlayer3.Tx.Result_Mountain[mountainTexId + 1].vc拡大縮小倍率.Y = 0.9f + (float)Math.Sin((float)(this.actParameterPanel.ctMountain_ClearIn.n現在の値 - 155) * (Math.PI / 180)) * 0.5f;
									}
									else if (this.actParameterPanel.ctMountain_ClearIn.n現在の値 <= 515)
									{
										TJAPlayer3.Tx.Result_Mountain[mountainTexId + 1].vc拡大縮小倍率.Y = 0.9f + (float)Math.Sin((float)(this.actParameterPanel.ctMountain_ClearIn.n現在の値 - 335) * (Math.PI / 180)) * 0.4f;
									}
								}

								#endregion
							}
							else if (is1P)
							{

								TJAPlayer3.Tx.Result_Background[1].Opacity = 0;
								TJAPlayer3.Tx.Result_Mountain[mountainTexId + 0].Opacity = 255;
								TJAPlayer3.Tx.Result_Mountain[mountainTexId + 1].Opacity = 0;
							}

							#region [Display background]

							if (is1P)
							{
								if (is2PSide)
									TJAPlayer3.Tx.Result_Background[2].t2D描画(0, 0);
								else
									TJAPlayer3.Tx.Result_Background[0].t2D描画(0, 0);
								TJAPlayer3.Tx.Result_Background[1].t2D描画(0, 0);
							}
							else
							{
								if (TJAPlayer3.ConfigIni.nPlayerCount <= 2)
								{
									gaugeAnimFactors = (this.actParameterPanel.ct全体進行.n現在の値 - (int)MountainAppearValue) * 3;

									int width1 = TJAPlayer3.Tx.Result_Background[1].szテクスチャサイズ.Width / 2;
									int height1 = TJAPlayer3.Tx.Result_Background[1].szテクスチャサイズ.Height;

									for (int i = 0; i < 2; i++)
									{
										int width2 = TJAPlayer3.Tx.Result_Background[2 * i].szテクスチャサイズ.Width / 2;
										int height2 = TJAPlayer3.Tx.Result_Background[2 * i].szテクスチャサイズ.Height;
										TJAPlayer3.Tx.Result_Background[2 * i].t2D描画(width2 * i, 0, new Rectangle(width2 * i, 0, width2, height2));

										if (bClear[i])
										{
											TJAPlayer3.Tx.Result_Background[1].Opacity = gaugeAnimFactors;
											TJAPlayer3.Tx.Result_Background[1].t2D描画(width1 * i, 0, new Rectangle(width1 * i, 0, width1, height1));
										}
									}
								}
								else
								{
									CTexture[] texs = new CTexture[5] {
									TJAPlayer3.Tx.Result_Background[0],
									TJAPlayer3.Tx.Result_Background[2],
									TJAPlayer3.Tx.Result_Background[3],
									TJAPlayer3.Tx.Result_Background[4],
									TJAPlayer3.Tx.Result_Background[5]
								};
									int count = Math.Max(TJAPlayer3.ConfigIni.nPlayerCount, 4);
									for (int i = 0; i < count; i++)
									{
										if (bClear[i])
										{
											gaugeAnimFactors = (this.actParameterPanel.ct全体進行.n現在の値 - (int)MountainAppearValue) * 3;
											TJAPlayer3.Tx.Result_Background[1].Opacity = gaugeAnimFactors;
										}
										int width = texs[i].szテクスチャサイズ.Width / count;
										texs[i].t2D描画(width * i, 0, new RectangleF(width * i, 0, width, texs[i].szテクスチャサイズ.Height));
										if (bClear[i])
											TJAPlayer3.Tx.Result_Background[1].t2D描画(width * i, 0, new RectangleF(width * i, 0, width, texs[i].szテクスチャサイズ.Height));
									}
								}

							}

							#endregion

							if (is1P)
							{
								TJAPlayer3.Tx.Result_Mountain[mountainTexId + 0].t2D描画(0, 0);
								TJAPlayer3.Tx.Result_Mountain[mountainTexId + 1].t2D拡大率考慮下基準描画(0, TJAPlayer3.Tx.Result_Mountain[mountainTexId + 1].szテクスチャサイズ.Height);

								// TJAPlayer3.act文字コンソール.tPrint(0, 0, C文字コンソール.Eフォント種別.白, ctShine_Plate.n現在の値.ToString());
								// TJAPlayer3.act文字コンソール.tPrint(10, 10, C文字コンソール.Eフォント種別.白, this.actParameterPanel.ct全体進行.n現在の値.ToString());

								#region [Background Clouds]

								if (bClear[0] && this.actParameterPanel.ct全体進行.n現在の値 >= MountainAppearValue)
								{
									CloudType = Math.Min(255, Math.Max(0, (int)this.actParameterPanel.ct全体進行.n現在の値 - (int)MountainAppearValue));
								}

								int cloud_width = TJAPlayer3.Tx.Result_Cloud.szテクスチャサイズ.Width / TJAPlayer3.Skin.Result_Cloud_Count;
								int cloud_height = TJAPlayer3.Tx.Result_Cloud.szテクスチャサイズ.Height / 3;

								for (int i = TJAPlayer3.Skin.Result_Cloud_Count - 1; i >= 0; i--)
								{
									int CurMoveRed = (int)((double)TJAPlayer3.Skin.Result_Cloud_MaxMove[i] * Math.Tanh((double)this.actParameterPanel.ct全体進行.n現在の値 / 10000));
									int CurMoveGold = (int)((double)TJAPlayer3.Skin.Result_Cloud_MaxMove[i] * Math.Tanh(Math.Max(0, (double)this.actParameterPanel.ct全体進行.n現在の値 - (double)MountainAppearValue) / 10000));

									int cloudOffset = (is2PSide) ? cloud_height * 2 : 0;

									TJAPlayer3.Tx.Result_Cloud.vc拡大縮小倍率.X = 0.65f;
									TJAPlayer3.Tx.Result_Cloud.vc拡大縮小倍率.Y = 0.65f;
									TJAPlayer3.Tx.Result_Cloud.Opacity = CloudType;

									TJAPlayer3.Tx.Result_Cloud.t2D拡大率考慮中央基準描画(TJAPlayer3.Skin.Result_Cloud_X[i] - CurMoveGold, TJAPlayer3.Skin.Result_Cloud_Y[i],
										new Rectangle(i * cloud_width, cloud_height, cloud_width, cloud_height));

									TJAPlayer3.Tx.Result_Cloud.Opacity = 255 - CloudType;

									TJAPlayer3.Tx.Result_Cloud.t2D拡大率考慮中央基準描画(TJAPlayer3.Skin.Result_Cloud_X[i] - CurMoveRed, TJAPlayer3.Skin.Result_Cloud_Y[i],
										new Rectangle(i * cloud_width, cloudOffset, cloud_width, cloud_height));
								}

								#endregion

								if (bClear[0] && this.actParameterPanel.ct全体進行.n現在の値 >= MountainAppearValue)
								{

									#region [Background shines]

									int ShineTime = (int)ctShine_Plate.n現在の値;
									int Quadrant500 = ShineTime % 500;

									for (int i = 0; i < TJAPlayer3.Skin.Result_Shine_Count; i++)
									{
										if (i < 2 && ShineTime >= 500 || i >= 2 && ShineTime < 500)
											TJAPlayer3.Tx.Result_Shine.Opacity = 0;
										else if (Quadrant500 >= ShinePFade && Quadrant500 <= 500 - ShinePFade)
											TJAPlayer3.Tx.Result_Shine.Opacity = 255;
										else
											TJAPlayer3.Tx.Result_Shine.Opacity = (255 * Math.Min(Quadrant500, 500 - Quadrant500)) / ShinePFade;

										TJAPlayer3.Tx.Result_Shine.vc拡大縮小倍率.X = TJAPlayer3.Skin.Result_Shine_Size[i];
										TJAPlayer3.Tx.Result_Shine.vc拡大縮小倍率.Y = TJAPlayer3.Skin.Result_Shine_Size[i];

										TJAPlayer3.Tx.Result_Shine.t2D中心基準描画(TJAPlayer3.Skin.Result_Shine_X[is2PSide ? 1 : 0][i], TJAPlayer3.Skin.Result_Shine_Y[is2PSide ? 1 : 0][i]);
									}

									#endregion

									#region [Fireworks]

									// Primary pop
									if (this.actParameterPanel.ct全体進行.n現在の値 <= MountainAppearValue + 1000)
									{
										for (int i = 0; i < 3; i++)
										{
											if (this.actParameterPanel.ct全体進行.n現在の値 <= MountainAppearValue + 255)
											{
												int TmpTimer = (int)(this.actParameterPanel.ct全体進行.n現在の値 - MountainAppearValue);

												TJAPlayer3.Tx.Result_Work[i].Opacity = TmpTimer;
												TJAPlayer3.Tx.Result_Work[i].vc拡大縮小倍率.X = 0.6f * ((float)TmpTimer / 225f);
												TJAPlayer3.Tx.Result_Work[i].vc拡大縮小倍率.Y = 0.6f * ((float)TmpTimer / 225f);
											}
											else
											{
												int TmpTimer = Math.Max(0, (2 * 255) - (int)(this.actParameterPanel.ct全体進行.n現在の値 - MountainAppearValue - 255));

												TJAPlayer3.Tx.Result_Work[i].Opacity = TmpTimer / 2;
												TJAPlayer3.Tx.Result_Work[i].vc拡大縮小倍率.X = 0.6f;
												TJAPlayer3.Tx.Result_Work[i].vc拡大縮小倍率.Y = 0.6f;
											}

											TJAPlayer3.Tx.Result_Work[i].t2D拡大率考慮中央基準描画(TJAPlayer3.Skin.Result_Work_X[is2PSide ? 1 : 0][i], TJAPlayer3.Skin.Result_Work_Y[is2PSide ? 1 : 0][i]);
										}
									}
									else
									{
										ctWork_Plate.t進行Loop();

										for (int i = 0; i < 3; i++)
										{
											int TmpStamp = WorksTimeStamp[i];

											if (ctWork_Plate.n現在の値 <= TmpStamp + 255)
											{
												int TmpTimer = (int)(ctWork_Plate.n現在の値 - TmpStamp);

												TJAPlayer3.Tx.Result_Work[i].Opacity = TmpTimer;
												TJAPlayer3.Tx.Result_Work[i].vc拡大縮小倍率.X = 0.6f * ((float)TmpTimer / 225f);
												TJAPlayer3.Tx.Result_Work[i].vc拡大縮小倍率.Y = 0.6f * ((float)TmpTimer / 225f);
											}
											else
											{
												int TmpTimer = Math.Max(0, (2 * 255) - (int)(ctWork_Plate.n現在の値 - TmpStamp - 255));

												TJAPlayer3.Tx.Result_Work[i].Opacity = TmpTimer / 2;
												TJAPlayer3.Tx.Result_Work[i].vc拡大縮小倍率.X = 0.6f;
												TJAPlayer3.Tx.Result_Work[i].vc拡大縮小倍率.Y = 0.6f;
											}

											TJAPlayer3.Tx.Result_Work[i].t2D拡大率考慮中央基準描画(TJAPlayer3.Skin.Result_Work_X[is2PSide ? 1 : 0][i], TJAPlayer3.Skin.Result_Work_Y[is2PSide ? 1 : 0][i]);
										}
									}

									#endregion

								}
							}
						}
						*/
						if (OpenTaiko.Tx.Result_Header != null) {
							OpenTaiko.Tx.Result_Header.t2D描画(0, 0);
						}
					}

					if (this.ct登場用.IsTicked && (OpenTaiko.Tx.Result_Header != null)) {
						double num2 = ((double)this.ct登場用.CurrentValue) / 100.0;
						double num3 = Math.Sin(Math.PI / 2 * num2);

						// num = ((int)(TJAPlayer3.Tx.Result_Header.sz画像サイズ.Height * num3)) - TJAPlayer3.Tx.Result_Header.sz画像サイズ.Height;
					}
					/*
					else
					{
						num = 0;
					}
					*/

					if (!b音声再生 && !bgmResultIn.bIsPlaying) {
						bgmResultLoop.tPlay();
						b音声再生 = true;
					}


					#endregion

				} else {
					if (OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Dan) {
						double screen_ratio_x = OpenTaiko.Skin.Resolution[0] / 1280.0;

						#region [Counter processings]

						int songCount = OpenTaiko.stageSongSelect.rChoosenSong.DanSongs.Count;

						/*
						**	1600 => Dan plate 
						**  3200 + 300 * count => Songs display
						**  5500 + 300 * count => Exams plate display
						**	8200 + 300 * count => Goukaku/Fugoukaku display => Step 2 (Prompt the user to tap enter and let them swaping between informations hitting kas)
						**  ??? => Success/Fail animation
						*/
						if (ctPhase1 == null) {
							ctPhase1 = new CCounter(0, 8200 + songCount * 300, 0.5f, OpenTaiko.Timer);
							ctPhase1.CurrentValue = 0;
						}

						ctPhase1.Tick();

						if (ctPhase2 != null)
							ctPhase2.Tick();

						#endregion


						#region [DaniDoujou result screen]

						if (!b音声再生 && !OpenTaiko.Skin.bgmDanResult.bIsPlaying) {
							OpenTaiko.Skin.bgmDanResult.tPlay();
							b音声再生 = true;
						}

						//DanResult_Background.t2D描画(0, 0);
						OpenTaiko.Tx.DanResult_SongPanel_Base.t2D描画(0, 0);

						#region [DanPlate]

						// To add : Animation at 1 sec

						Dan_Plate?.t2D中心基準描画(138, 220);

						int plateOffset = Math.Max(0, 1600 - ctPhase1.CurrentValue) * 2;

						CActSelect段位リスト.tDisplayDanPlate(Dan_Plate,
							null,
							138,
							220 - plateOffset);

						#endregion

						#region [Charts Individual Results]

						ctDanSongInfoChange.Tick();

						if (ctDanSongInfoChange.CurrentValue == ctDanSongInfoChange.EndValue && songCount > 3) {
							NextDanSongInfo();
						} else if (nNowDanSongInfo > 0 && songCount <= 3) {
							nNowDanSongInfo = 0;
						}

						for (int i = 0; i < songCount; i++) {
							int songOffset = (int)(Math.Max(0, 3200 + 300 * i - ctPhase1.CurrentValue) * screen_ratio_x);

							int quadrant = i / 3;
							if (quadrant == nNowDanSongInfo)
								ftDanDisplaySongInfo(i, songOffset);
						}

						#endregion

						#region [Exam informations]

						int examsOffset = 0;

						if (ctPhase2 != null && examsShift != 0)
							examsOffset = (examsShift < 0) ? 1280 - ctPhase2.CurrentValue : ctPhase2.CurrentValue;
						else if (ctPhase1.IsUnEnded)
							examsOffset = Math.Max(0, 5500 + 300 * songCount - ctPhase1.CurrentValue) * 2;

						examsOffset = (int)(examsOffset * screen_ratio_x);

						ftDanDisplayExamInfo(examsOffset);

						#endregion

						#region [PassLogo]

						Exam.Status examStatus = OpenTaiko.stage演奏ドラム画面.actDan.GetExamStatus(OpenTaiko.stage結果.st演奏記録.Drums.Dan_C);

						int unitsBeforeAppearance = Math.Max(0, 8200 + 300 * songCount - ctPhase1.CurrentValue);

						if (unitsBeforeAppearance <= 270) {
							OpenTaiko.Tx.DanResult_Rank.Opacity = 255;

							int rank_width = OpenTaiko.Tx.DanResult_Rank.szTextureSize.Width / 7;
							int rank_height = OpenTaiko.Tx.DanResult_Rank.szTextureSize.Height;

							if (examStatus != Exam.Status.Failure) {
								#region [Goukaku]

								#region [ Appear animation ]

								if (unitsBeforeAppearance >= 90) {
									OpenTaiko.Tx.DanResult_Rank.Opacity = (int)((270 - unitsBeforeAppearance) / 180.0f * 255.0f);
									OpenTaiko.Tx.DanResult_Rank.vcScaleRatio.X = 1.0f + (float)Math.Sin((360 - unitsBeforeAppearance) / 1.5f * (Math.PI / 180)) * 1.4f;
									OpenTaiko.Tx.DanResult_Rank.vcScaleRatio.Y = 1.0f + (float)Math.Sin((360 - unitsBeforeAppearance) / 1.5f * (Math.PI / 180)) * 1.4f;
								} else if (unitsBeforeAppearance > 0) {
									OpenTaiko.Tx.Result_ScoreRankEffect.vcScaleRatio.X = 0.5f + (float)Math.Sin((float)(90 - unitsBeforeAppearance) * (Math.PI / 180)) * 0.5f;
									OpenTaiko.Tx.Result_ScoreRankEffect.vcScaleRatio.Y = 0.5f + (float)Math.Sin((float)(90 - unitsBeforeAppearance) * (Math.PI / 180)) * 0.5f;
								} else {
									OpenTaiko.Tx.DanResult_Rank.vcScaleRatio.X = 1f;
									OpenTaiko.Tx.DanResult_Rank.vcScaleRatio.Y = 1f;
								}

								#endregion

								#region [ Goukaku plate type calculus ]

								int successType = 0;

								if (examStatus == Exam.Status.Better_Success)
									successType += 1;

								int comboType = 0;
								if (this.st演奏記録.Drums.nBadCount == 0) {
									comboType += 1;

									if (this.st演奏記録.Drums.nOkCount == 0)
										comboType += 1;
								}

								#endregion

								OpenTaiko.Tx.DanResult_Rank.t2D拡大率考慮中央基準描画(OpenTaiko.Skin.DanResult_Rank[0], OpenTaiko.Skin.DanResult_Rank[1],
									new Rectangle(rank_width * (2 * comboType + successType + 1), 0, rank_width, rank_height));

								#endregion
							} else {
								#region [Fugoukaku]

								#region [ Appear animation ]

								if (unitsBeforeAppearance >= 90) {
									OpenTaiko.Tx.DanResult_Rank.Opacity = (int)((270 - unitsBeforeAppearance) / 180.0f * 255.0f);
								}

								OpenTaiko.Tx.DanResult_Rank.vcScaleRatio.X = 1f;
								OpenTaiko.Tx.DanResult_Rank.vcScaleRatio.Y = 1f;

								#endregion

								OpenTaiko.Tx.DanResult_Rank.t2D拡大率考慮中央基準描画(OpenTaiko.Skin.DanResult_Rank[0], OpenTaiko.Skin.DanResult_Rank[1] - (unitsBeforeAppearance / 10f),
									new Rectangle(0, 0, rank_width, rank_height));

								#endregion
							}
						}

						#endregion

						if (!b音声再生 && !OpenTaiko.Skin.bgmDanResult.bIsPlaying) {
							OpenTaiko.Skin.bgmDanResult.tPlay();
							b音声再生 = true;
						}

						#endregion

					} else {
						#region [Tower result screen]

						if (!b音声再生 && !OpenTaiko.Skin.bgmTowerResult.bIsPlaying) {
							OpenTaiko.Skin.bgmTowerResult.tPlay();
							b音声再生 = true;
						}

						// Pictures here

						this.ctTower_Animation.Tick();

						#region [Tower background]

						if (OpenTaiko.Skin.Game_Tower_Ptn_Result > 0) {
							int xFactor = 0;
							float yFactor = 1f;

							int currentTowerType = Array.IndexOf(OpenTaiko.Skin.Game_Tower_Names, OpenTaiko.stageSongSelect.rChoosenSong.arスコア[5].譜面情報.nTowerType);

							if (currentTowerType < 0 || currentTowerType >= OpenTaiko.Skin.Game_Tower_Ptn_Result)
								currentTowerType = 0;

							if (OpenTaiko.Tx.TowerResult_Background != null && OpenTaiko.Tx.TowerResult_Tower[currentTowerType] != null) {
								xFactor = (OpenTaiko.Tx.TowerResult_Background.szTextureSize.Width - OpenTaiko.Tx.TowerResult_Tower[currentTowerType].szTextureSize.Width) / 2;
								yFactor = OpenTaiko.Tx.TowerResult_Tower[currentTowerType].szTextureSize.Height / (float)OpenTaiko.Tx.TowerResult_Background.szTextureSize.Height;
							}

							OpenTaiko.Tx.TowerResult_Background?.t2D描画(0, -1 * this.ctTower_Animation.CurrentValue);
							OpenTaiko.Tx.TowerResult_Tower[currentTowerType]?.t2D描画(xFactor, -1 * yFactor * this.ctTower_Animation.CurrentValue);
						}

						#endregion

						OpenTaiko.Tx.TowerResult_Panel?.t2D描画(0, 0);

						#region [Score Rank]

						int sc = GetTowerScoreRank() - 1;

						OpenTaiko.actTextConsole.tPrint(0, 40, CTextConsole.EFontType.White, sc.ToString());

						if (sc >= 0 && OpenTaiko.Tx.TowerResult_ScoreRankEffect != null) {
							int scoreRankEffect_width = OpenTaiko.Tx.TowerResult_ScoreRankEffect.szTextureSize.Width / 7;
							int scoreRankEffect_height = OpenTaiko.Tx.TowerResult_ScoreRankEffect.szTextureSize.Height;

							OpenTaiko.Tx.TowerResult_ScoreRankEffect.Opacity = 255;
							OpenTaiko.Tx.TowerResult_ScoreRankEffect.vcScaleRatio.X = 1f;
							OpenTaiko.Tx.TowerResult_ScoreRankEffect.vcScaleRatio.Y = 1f;
							OpenTaiko.Tx.TowerResult_ScoreRankEffect.t2D拡大率考慮中央基準描画(
								OpenTaiko.Skin.TowerResult_ScoreRankEffect[0],
								OpenTaiko.Skin.TowerResult_ScoreRankEffect[1],
								new Rectangle(sc * scoreRankEffect_width, 0, scoreRankEffect_width, scoreRankEffect_height));
						}


						#endregion


						#region [Text elements]

						TitleTextureKey.ResolveTitleTexture(this.ttkToutatsu)?.t2D描画(OpenTaiko.Skin.TowerResult_Toutatsu[0], OpenTaiko.Skin.TowerResult_Toutatsu[1]);
						TitleTextureKey.ResolveTitleTexture(this.ttkMaxFloors)?.t2D描画(OpenTaiko.Skin.TowerResult_MaxFloors[0], OpenTaiko.Skin.TowerResult_MaxFloors[1]);
						TitleTextureKey.ResolveTitleTexture(this.ttkTen)?.t2D描画(OpenTaiko.Skin.TowerResult_Ten[0], OpenTaiko.Skin.TowerResult_Ten[1]);
						TitleTextureKey.ResolveTitleTexture(this.ttkScore)?.t2D描画(OpenTaiko.Skin.TowerResult_Score[0], OpenTaiko.Skin.TowerResult_Score[1]);

						CTexture tmpScoreCount = TitleTextureKey.ResolveTitleTexture(this.ttkScoreCount);
						CTexture tmpCurrentFloor = TitleTextureKey.ResolveTitleTexture(this.ttkReachedFloor);
						CTexture tmpRemainingLifes = TitleTextureKey.ResolveTitleTexture(this.ttkRemaningLifes);

						tmpCurrentFloor?.t2D描画(OpenTaiko.Skin.TowerResult_CurrentFloor[0] - tmpCurrentFloor.szTextureSize.Width, OpenTaiko.Skin.TowerResult_CurrentFloor[1]);
						tmpScoreCount?.t2D描画(OpenTaiko.Skin.TowerResult_ScoreCount[0] - tmpScoreCount.szTextureSize.Width, OpenTaiko.Skin.TowerResult_ScoreCount[1]);
						tmpRemainingLifes?.t2D描画(OpenTaiko.Skin.TowerResult_RemainingLifes[0] - tmpRemainingLifes.szTextureSize.Width, OpenTaiko.Skin.TowerResult_RemainingLifes[1]);

						int soul_width = OpenTaiko.Tx.Gauge_Soul.szTextureSize.Width;
						int soul_height = OpenTaiko.Tx.Gauge_Soul.szTextureSize.Height / 2;

						OpenTaiko.Tx.Gauge_Soul?.t2D描画(OpenTaiko.Skin.TowerResult_Gauge_Soul[0], OpenTaiko.Skin.TowerResult_Gauge_Soul[1], new Rectangle(0, 0, soul_width, soul_height));

						#endregion

						if (!b音声再生 && !OpenTaiko.Skin.bgmTowerResult.bIsPlaying) {
							OpenTaiko.Skin.bgmTowerResult.tPlay();
							b音声再生 = true;
						}


						#endregion
					}


				}

				// Display medals debug

				// TJAPlayer3.act文字コンソール.tPrint(0, 12, C文字コンソール.Eフォント種別.白, this.nEarnedMedalsCount[0].ToString());
				//TJAPlayer3.act文字コンソール.tPrint(0, 25, C文字コンソール.Eフォント種別.白, this.nEarnedMedalsCount[1].ToString());



				if (this.actParameterPanel.Draw() == 0) {
					this.bアニメが完了 = false;
				}

				if (this.actSongBar.Draw() == 0) {
					this.bアニメが完了 = false;
				}

				#region Nameplate

				for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
					if (OpenTaiko.ConfigIni.bAIBattleMode && i == 1) break;

					int pos = i;
					if (OpenTaiko.P1IsBlue() && OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0] < (int)Difficulty.Tower)
						pos = 1;

					int namePlate_x;
					int namePlate_y;
					int modIcons_x;
					int modIcons_y;

					if (OpenTaiko.ConfigIni.nPlayerCount == 5) {
						namePlate_x = OpenTaiko.Skin.Result_NamePlate_5P[0] + OpenTaiko.Skin.Result_UIMove_5P_X[pos];
						namePlate_y = OpenTaiko.Skin.Result_NamePlate_5P[1] + OpenTaiko.Skin.Result_UIMove_5P_Y[pos];
						modIcons_x = OpenTaiko.Skin.Result_ModIcons_5P[0] + OpenTaiko.Skin.Result_UIMove_5P_X[pos];
						modIcons_y = OpenTaiko.Skin.Result_ModIcons_5P[1] + OpenTaiko.Skin.Result_UIMove_5P_Y[pos];
					} else if (OpenTaiko.ConfigIni.nPlayerCount == 4 || OpenTaiko.ConfigIni.nPlayerCount == 3) {
						namePlate_x = OpenTaiko.Skin.Result_NamePlate_4P[0] + OpenTaiko.Skin.Result_UIMove_4P_X[pos];
						namePlate_y = OpenTaiko.Skin.Result_NamePlate_4P[1] + OpenTaiko.Skin.Result_UIMove_4P_Y[pos];
						modIcons_x = OpenTaiko.Skin.Result_ModIcons_4P[0] + OpenTaiko.Skin.Result_UIMove_4P_X[pos];
						modIcons_y = OpenTaiko.Skin.Result_ModIcons_4P[1] + OpenTaiko.Skin.Result_UIMove_4P_Y[pos];
					} else {
						namePlate_x = OpenTaiko.Skin.Result_NamePlate_X[pos];
						namePlate_y = OpenTaiko.Skin.Result_NamePlate_Y[pos];
						modIcons_x = OpenTaiko.Skin.Result_ModIcons_X[pos];
						modIcons_y = OpenTaiko.Skin.Result_ModIcons_Y[pos];
					}

					OpenTaiko.NamePlate.tNamePlateDraw(namePlate_x, namePlate_y, i);

					#region Mods

					ModIcons.tDisplayModsMenu(modIcons_x, modIcons_y, i);

					#endregion
				}

				#endregion




				#region [Display modals]

				if (displayedModals != null) {
					lcModal?.Update();
					lcModal?.Draw();
				}

				#endregion

				if (base.ePhaseID == CStage.EPhase.Common_FADEIN) {
					if (this.actFI.Draw() != 0) {
						base.ePhaseID = CStage.EPhase.Common_NORMAL;
					}
				} else if ((base.ePhaseID == CStage.EPhase.Common_FADEOUT))         //&& ( this.actFO.On進行描画() != 0 ) )
				  {
					return (int)this.eフェードアウト完了時の戻り値;
				}

				#region [ #24609 2011.3.14 yyagi ランク更新or演奏型スキル更新時、リザルト画像をpngで保存する ]
				if (this.bアニメが完了 == true && this.bIsCheckedWhetherResultScreenShouldSaveOrNot == false  // #24609 2011.3.14 yyagi; to save result screen in case BestRank or HiSkill.
					&& OpenTaiko.ConfigIni.bIsAutoResultCapture)                                               // #25399 2011.6.9 yyagi
				{
					string strFullPath =
							   Path.Combine(OpenTaiko.strEXEのあるフォルダ, "Capture_img");
					strFullPath = Path.Combine(strFullPath, DateTime.Now.ToString("yyyyMMddHHmmss") + ".png");
					OpenTaiko.app.SaveResultScreen(strFullPath);

					this.bIsCheckedWhetherResultScreenShouldSaveOrNot = true;
				}
				#endregion

				// キー入力

				if (OpenTaiko.act現在入力を占有中のプラグイン == null) {
					if (base.ePhaseID == CStage.EPhase.Common_NORMAL) {
						if (OpenTaiko.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.Escape)) {
							#region [ Return to song select screen (Faster method) ]

							bgmResultLoop.tStop();
							OpenTaiko.Skin.bgmDanResult.tStop();
							OpenTaiko.Skin.bgmTowerResult.tStop();
							OpenTaiko.Skin.soundDecideSFX.tPlay();
							actFI.tフェードアウト開始();

							if (OpenTaiko.latestSongSelect == OpenTaiko.stageSongSelect)// TJAPlayer3.stage選曲.n確定された曲の難易度[0] != (int)Difficulty.Dan)
								if (OpenTaiko.stageSongSelect.rNowSelectedSong.rParentNode != null)
									OpenTaiko.stageSongSelect.actSongList.tCloseBOX();

							tPostprocessing();
							base.ePhaseID = CStage.EPhase.Common_FADEOUT;
							this.eフェードアウト完了時の戻り値 = E戻り値.完了;

							#endregion
						}
						if (((OpenTaiko.Pad.bPressedDGB(EPad.CY)
							|| OpenTaiko.Pad.bPressed(EInstrumentPad.DRUMS, EPad.RD))
							|| (OpenTaiko.Pad.bPressed(EInstrumentPad.DRUMS, EPad.LC)
							|| (OpenTaiko.Pad.bPressedDGB(EPad.Decide)
							|| OpenTaiko.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.Return))))) {


							#region [ Skip animations ]

							if (OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0] < (int)Difficulty.Tower
								&& this.actParameterPanel.ctMainCounter.CurrentValue < this.actParameterPanel.MountainAppearValue) {
								OpenTaiko.Skin.soundDecideSFX.tPlay();
								this.actParameterPanel.tSkipResultAnimations();
							} else if (OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Dan
								  && (ctPhase1 != null && ctPhase1.IsUnEnded)) {
								OpenTaiko.Skin.soundDecideSFX.tPlay();
								ctPhase1.CurrentValue = (int)ctPhase1.EndValue;
							}

							#endregion

							  else {
								if ((lcModal?.AnimationFinished() ?? true)) {
									OpenTaiko.Skin.soundDecideSFX.tPlay();

									if (!mqModals.tAreBothQueuesEmpty()
									&& (OpenTaiko.Pad.bPressedDGB(EPad.Decide)
										|| OpenTaiko.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.Return))) {
										displayedModals = mqModals.tPopModalInOrder();


									} else if (OpenTaiko.ConfigIni.nPlayerCount == 1 || mqModals.tAreBothQueuesEmpty()) {

										if (!mqModals.tAreBothQueuesEmpty())
											LogNotification.PopError("Unexpected Error: Exited results screen with remaining modals, this is likely due to a Lua script issue.");

										#region [ Return to song select screen ]

										actFI.tフェードアウト開始();

										if (OpenTaiko.latestSongSelect == OpenTaiko.stageSongSelect)
											if (OpenTaiko.stageSongSelect.rNowSelectedSong.rParentNode != null)
												OpenTaiko.stageSongSelect.actSongList.tCloseBOX();

										tPostprocessing();

										{
											base.ePhaseID = CStage.EPhase.Common_FADEOUT;
											this.eフェードアウト完了時の戻り値 = E戻り値.完了;
											bgmResultLoop.tStop();
											OpenTaiko.Skin.bgmDanResult.tStop();
											OpenTaiko.Skin.bgmTowerResult.tStop();
										}

										#endregion
									}
								}

							}
						}


						if (OpenTaiko.InputManager.Keyboard.KeyPressing((int)SlimDXKeys.Key.LeftArrow) ||
								OpenTaiko.Pad.bPressed(EInstrumentPad.DRUMS, EPad.LeftChange) ||
							OpenTaiko.InputManager.Keyboard.KeyPressing((int)SlimDXKeys.Key.RightArrow) ||
								OpenTaiko.Pad.bPressed(EInstrumentPad.DRUMS, EPad.RightChange)) {
							if (OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Dan) {
								#region [ Phase 2 (Swap freely between Exams and Songs) ]

								if (ctPhase1 != null && ctPhase1.IsEnded && (ctPhase2 == null || ctPhase2.IsEnded)) {
									ctPhase2 = new CCounter(0, 1280, 0.5f, OpenTaiko.Timer);
									ctPhase2.CurrentValue = 0;

									if (examsShift == 0)
										examsShift = 1;
									else
										examsShift = -examsShift;

									OpenTaiko.Skin.soundChangeSFX.tPlay();
								}

								#endregion
							}
						}
					}
				}
			}
			return 0;
		}

		#region [Dan result exam information]

		private void ftDanDisplayExamInfo(int offset = 0) {
			int baseX = OpenTaiko.Skin.DanResult_StatePanel[0] + offset;
			int baseY = OpenTaiko.Skin.DanResult_StatePanel[1];

			OpenTaiko.Tx.DanResult_StatePanel_Base.t2D描画(baseX, baseY);
			OpenTaiko.Tx.DanResult_StatePanel_Main.t2D描画(baseX, baseY);

			#region [ Global scores ]

			int totalHit = OpenTaiko.stage演奏ドラム画面.CChartScore[0].nGreat
				+ OpenTaiko.stage演奏ドラム画面.CChartScore[0].nGood
				+ OpenTaiko.stage演奏ドラム画面.GetRoll(0);

			// Small digits
			this.actParameterPanel.t小文字表示(OpenTaiko.Skin.DanResult_Perfect[0] + offset, OpenTaiko.Skin.DanResult_Perfect[1],
				OpenTaiko.stage演奏ドラム画面.CChartScore[0].nGreat, 1.0f);

			this.actParameterPanel.t小文字表示(OpenTaiko.Skin.DanResult_Good[0] + offset, OpenTaiko.Skin.DanResult_Good[1],
				OpenTaiko.stage演奏ドラム画面.CChartScore[0].nGood, 1.0f);

			this.actParameterPanel.t小文字表示(OpenTaiko.Skin.DanResult_Miss[0] + offset, OpenTaiko.Skin.DanResult_Miss[1],
				OpenTaiko.stage演奏ドラム画面.CChartScore[0].nMiss, 1.0f);

			this.actParameterPanel.t小文字表示(OpenTaiko.Skin.DanResult_Roll[0] + offset, OpenTaiko.Skin.DanResult_Roll[1],
				OpenTaiko.stage演奏ドラム画面.GetRoll(0), 1.0f);

			this.actParameterPanel.t小文字表示(OpenTaiko.Skin.DanResult_MaxCombo[0] + offset, OpenTaiko.Skin.DanResult_MaxCombo[1],
				OpenTaiko.stage演奏ドラム画面.actCombo.n現在のコンボ数.最高値[0], 1.0f);

			this.actParameterPanel.t小文字表示(OpenTaiko.Skin.DanResult_TotalHit[0] + offset, OpenTaiko.Skin.DanResult_TotalHit[1],
				totalHit, 1.0f);

			// Large digits
			this.actParameterPanel.tスコア文字表示(OpenTaiko.Skin.DanResult_Score[0] + offset, OpenTaiko.Skin.DanResult_Score[1], (int)OpenTaiko.stage演奏ドラム画面.actScore.Get(0), 1.0f);

			#endregion

			#region [ Display exams ]

			OpenTaiko.stage演奏ドラム画面.actDan.DrawExam(OpenTaiko.stage結果.st演奏記録.Drums.Dan_C, true, offset);

			#endregion
		}

		#endregion


		#region [Dan result individual song information]

		private void ftDanDisplaySongInfo(int i, int offset = 0) {
			int drawPos = i % 3;
			int nowIndex = (i / 3);

			int opacityCounter = Math.Min(ctDanSongInfoChange.CurrentValue, 255);
			int opacity;

			if (nowIndex == nNowDanSongInfo) {
				opacity = opacityCounter;
			} else {
				opacity = 255 - opacityCounter;
			}

			/*
			int baseX = 255 + offset;
			int baseY = 100 + 183 * i;
			*/

			var song = OpenTaiko.stageSongSelect.rChoosenSong.DanSongs[i];

			// TJAPlayer3.Tx.Dani_Difficulty_Cymbol.t2D中心基準描画(scroll + 377, 180 + i * 73, new Rectangle(song.Difficulty * 53, 0, 53, 53));

			int songPanel_main_width = OpenTaiko.Tx.DanResult_SongPanel_Main.szTextureSize.Width;
			int songPanel_main_height = OpenTaiko.Tx.DanResult_SongPanel_Main.szTextureSize.Height / 3;

			OpenTaiko.Tx.DanResult_SongPanel_Main.Opacity = opacity;
			OpenTaiko.Tx.DanResult_SongPanel_Main.t2D描画(OpenTaiko.Skin.DanResult_SongPanel_Main_X[drawPos] + offset, OpenTaiko.Skin.DanResult_SongPanel_Main_Y[drawPos], new Rectangle(0, songPanel_main_height * Math.Min(i, 2), songPanel_main_width, songPanel_main_height));

			int difficulty_cymbol_width = OpenTaiko.Tx.Dani_Difficulty_Cymbol.szTextureSize.Width / 5;
			int difficulty_cymbol_height = OpenTaiko.Tx.Dani_Difficulty_Cymbol.szTextureSize.Height;

			OpenTaiko.Tx.Dani_Difficulty_Cymbol.Opacity = opacity;
			OpenTaiko.Tx.Dani_Difficulty_Cymbol.t2D中心基準描画(OpenTaiko.Skin.DanResult_Difficulty_Cymbol_X[drawPos] + offset, OpenTaiko.Skin.DanResult_Difficulty_Cymbol_Y[drawPos], new Rectangle(song.Difficulty * difficulty_cymbol_width, 0, difficulty_cymbol_width, difficulty_cymbol_height));
			OpenTaiko.Tx.Dani_Difficulty_Cymbol.Opacity = 255;

			OpenTaiko.Tx.Dani_Level_Number.Opacity = opacity;
			OpenTaiko.stage段位選択.段位リスト.tLevelNumberDraw(OpenTaiko.Skin.DanResult_Level_Number_X[drawPos] + offset, OpenTaiko.Skin.DanResult_Level_Number_Y[drawPos], song.Level);
			OpenTaiko.Tx.Dani_Level_Number.Opacity = 255;

			int[] scoresArr =
			{
				OpenTaiko.stage演奏ドラム画面.n良[i],
				OpenTaiko.stage演奏ドラム画面.n可[i],
				OpenTaiko.stage演奏ドラム画面.n不可[i],
				OpenTaiko.stage演奏ドラム画面.n連打[i]
			};

			int[] num_x = {
				OpenTaiko.Skin.DanResult_Sections_Perfect_X[drawPos],
				OpenTaiko.Skin.DanResult_Sections_Good_X[drawPos],
				OpenTaiko.Skin.DanResult_Sections_Miss_X[drawPos],
				OpenTaiko.Skin.DanResult_Sections_Roll_X[drawPos],
			};

			int[] num_y = {
				OpenTaiko.Skin.DanResult_Sections_Perfect_Y[drawPos],
				OpenTaiko.Skin.DanResult_Sections_Good_Y[drawPos],
				OpenTaiko.Skin.DanResult_Sections_Miss_Y[drawPos],
				OpenTaiko.Skin.DanResult_Sections_Roll_Y[drawPos],
			};

			OpenTaiko.Tx.Result_Number.Opacity = opacity;
			for (int j = 0; j < 4; j++)
				this.actParameterPanel.t小文字表示(num_x[j] + offset, num_y[j], scoresArr[j], 1.0f);
			OpenTaiko.Tx.Result_Number.Opacity = 255;

			TitleTextureKey.ResolveTitleTexture(this.ttkDanTitles[i]).Opacity = opacity;
			TitleTextureKey.ResolveTitleTexture(this.ttkDanTitles[i]).t2D描画(OpenTaiko.Skin.DanResult_DanTitles_X[drawPos] + offset, OpenTaiko.Skin.DanResult_DanTitles_Y[drawPos]);

			CActSelect段位リスト.tDisplayDanIcon(i + 1, OpenTaiko.Skin.DanResult_DanIcon_X[drawPos] + offset, OpenTaiko.Skin.DanResult_DanIcon_Y[drawPos], opacity, 1.0f);

		}

		#endregion


		public void tPostprocessing() {

			if (!bAddedToRecentlyPlayedSongs) {
				// Song added to recently added songs here

				OpenTaiko.RecentlyPlayedSongs.tAddChart(OpenTaiko.stageSongSelect.rChoosenSong.uniqueId.data.id);

				bAddedToRecentlyPlayedSongs = true;
			}

		}

		public enum E戻り値 : int {
			継続,
			完了
		}

		// その他

		#region [ private ]
		//-----------------

		public bool bAddedToRecentlyPlayedSongs;
		public bool b音声再生;
		public bool EndAnime;

		private CCounter ct登場用;
		private E戻り値 eフェードアウト完了時の戻り値;
		private CActFIFOResult actFI;
		private CActFIFOBlack actFO;
		private CActオプションパネル actOption;
		private CActResultParameterPanel actParameterPanel;

		private CActResultSongBar actSongBar;
		private bool bアニメが完了;
		private bool bIsCheckedWhetherResultScreenShouldSaveOrNot;              // #24509 2011.3.14 yyagi
		private readonly int[] nチャンネル0Atoレーン07;
		private int n最後に再生したHHのWAV番号;
		private int n最後に再生したHHのチャンネル番号;
		private CSound rResultSound;
		public ResultBG Background;

		public bool[] bClear {
			get {
				if (OpenTaiko.ConfigIni.bAIBattleMode) {
					int clearCount = 0;
					for (int i = 0; i < OpenTaiko.stage演奏ドラム画面.AIBattleSections.Count; i++) {
						if (OpenTaiko.stage演奏ドラム画面.AIBattleSections[i].End == CStage演奏画面共通.AIBattleSection.EndType.Clear) {
							clearCount++;
						}
					}
					return new bool[] { clearCount >= OpenTaiko.stage演奏ドラム画面.AIBattleSections.Count / 2.0, false };
				} else {
					return new bool[] { OpenTaiko.stage演奏ドラム画面.bIsAlreadyCleared[0], OpenTaiko.stage演奏ドラム画面.bIsAlreadyCleared[1], OpenTaiko.stage演奏ドラム画面.bIsAlreadyCleared[2], OpenTaiko.stage演奏ドラム画面.bIsAlreadyCleared[3], OpenTaiko.stage演奏ドラム画面.bIsAlreadyCleared[4] };
				}
			}
		}

		private CCounter ctDanSongInfoChange;

		private int nNowDanSongInfo;

		private void NextDanSongInfo() {
			ctDanSongInfoChange = new CCounter(0, 2000, 1, OpenTaiko.Timer);
			ctDanSongInfoChange.CurrentValue = 0;

			nNowDanSongInfo++;
			if (nNowDanSongInfo >= Math.Ceiling(OpenTaiko.stageSongSelect.rChoosenSong.DanSongs.Count / 3.0)) {
				nNowDanSongInfo = 0;
			}
		}

		// Cloud informations
		/*
		private int[] CloudXPos = { 642, 612, 652, 1148, 1180, 112, 8, 1088, 1100, 32, 412 };
		private int[] CloudYPos = { 202, 424, 636, 530, 636, 636, 102, 52, 108, 326, 644 };
		private int[] CloudMaxMove = { 150, 120, 180, 60, 90, 150, 120, 50, 45, 120, 180 };
		*/

		// Shines informations
		private CCounter ctShine_Plate;

		/*
		private int[] ShinePXPos = { 805, 1175, 645, 810, 1078, 1060 };
		private int[] ShinePYPos = { 650, 405, 645, 420, 202, 585 };

		private float[] ShinePSize = { 0.44f, 0.6f, 0.4f, 0.15f, 0.35f, 0.6f };
		*/

		private int ShinePFade = 100;

		// Fireworks informations
		private CCounter ctWork_Plate;

		/*
		private int[] WorksPosX = { 800, 900, 1160 };
		private int[] WorksPosY = { 435, 185, 260 };
		*/

		private int[] WorksTimeStamp = { 1000, 2000, 3000 };

		// Dan informations
		private CTexture Dan_Plate;
		private TitleTextureKey[] ttkDanTitles;
		private CCachedFontRenderer pfDanTitles;
		private CCounter ctPhase1; // Info display
		private CCounter ctPhase2; // Free swipe
		private CCounter ctPhase3; // Background & grade granted if changes
		private int examsShift = 0;
		private bool newGradeGranted = false;

		// Tower informations
		private CCounter ctTower_Animation;
		private TitleTextureKey ttkMaxFloors;
		private TitleTextureKey ttkToutatsu;
		private TitleTextureKey ttkTen;
		private TitleTextureKey ttkReachedFloor;
		private TitleTextureKey ttkScore;
		private TitleTextureKey ttkRemaningLifes;
		private TitleTextureKey ttkScoreCount;
		private CCachedFontRenderer pfTowerText;
		private CCachedFontRenderer pfTowerText48;
		private CCachedFontRenderer pfTowerText72;

		private CSkin.CSystemSound bgmResultIn {
			get {
				if (OpenTaiko.ConfigIni.bAIBattleMode) {
					return OpenTaiko.Skin.bgmResultIn_AI;
				} else {
					return OpenTaiko.Skin.bgmリザルトイン音;
				}
			}
		}

		private CSkin.CSystemSound bgmResultLoop {
			get {
				if (OpenTaiko.ConfigIni.bAIBattleMode) {
					return OpenTaiko.Skin.bgmResult_AI;
				} else {
					return OpenTaiko.Skin.bgmリザルト音;
				}
			}
		}

		// Modal queues
		private ModalQueue mqModals;
		private Modal? displayedModals;

		// Coins information 
		private int[] nEarnedMedalsCount = { 0, 0, 0, 0, 0 };


		//-----------------
		#endregion
	}
}
