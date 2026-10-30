using System.Diagnostics;
using DiscordRPC;
using FDK;

namespace OpenTaiko;

internal class CStageResult : CStage {

	// Properties

	public int nOverallRankValue;

	public int[] nClear = { 0, 0, 0, 0, 0 };        //0:Not-cleared 1:Assisted-cleared 2:Cleared 3:Full-combo 4:Perfect
	public int[] nScoreRank = { 0, 0, 0, 0, 0 };  //0:未取得 1:白粋 2:銅粋 3:銀粋 4:金雅 5:桃雅 6:紫雅 7:虹極
	public int[] nHighScore = { 0, 0, 0, 0, 0 };
	public bool[] IsScoreValid = [false, false, false, false, false];
	public int[] ClearStatusesSaved = [0, 0, 0, 0, 0];
	public int[] ScoreRanksSaved = [0, 0, 0, 0, 0];

	public CChip[] rEmptyHitDrumsChip;
	public CScoreIni.CPlayRecord stPlayRecord;


	// Constructor

	public CStageResult() {
		this.stPlayRecord = new CScoreIni.CPlayRecord();
		this.rEmptyHitDrumsChip = new CChip[10];
		this.nOverallRankValue = -1;
		base.eStageID = CStage.EStage.Results;
		base.ePhaseID = CStage.EPhase.Common_NORMAL;
		base.IsDeActivated = true;
		base.ChildActivities.Add(this.actOption = new CActOptionPanel());
		base.ChildActivities.Add(this.actFIFromGameplay = new CActFIFOResult());
		base.ChildActivities.Add(this.actFO = new CActFIFOBlack());
	}


	public bool isAutoDisabled(int player) {
		return ((player != 1 && !OpenTaiko.ConfigIni.bAutoPlay[player])
				|| (player == 1 && !OpenTaiko.ConfigIni.bAutoPlay[player] && !OpenTaiko.ConfigIni.bAIBattleMode));
	}


	public int GetTowerScoreRank() {
		int tmpClear = 0;
		double progress = OpenTaiko.stageGameScreen.FloorManagement.LastRegisteredFloor / ((double)OpenTaiko.SongMount.rChoosenSong!.score[5]!.ChartInfo.nTotalFloor);

		// Clear badges : 10% (E), 25% (D), 50% (C), 75% (B), Clear (A), FC (S), DFC (X)
		bool[] conditions =
		{
			progress >= 0.1,
			progress >= 0.25,
			progress >= 0.5,
			progress >= 0.75,
			progress == 1 && OpenTaiko.stageGameScreen.FloorManagement.CurrentNumberOfLives > 0,
			OpenTaiko.stageGameScreen.CChartScore[0].nMiss == 0 && OpenTaiko.stageGameScreen.CChartScore[0].nMine == 0,
			OpenTaiko.stageGameScreen.CChartScore[0].nGood == 0
		};

		for (int i = 0; i < conditions.Length; i++) {
			if (conditions[i] == true)
				tmpClear++;
			else
				break;
		}

		return tmpClear;
	}

	// the player's best play on the chosen chart and difficulty in their save, null when none. Saving the play puts new
	// records in the save, so one read before keeps the bests as they were.
	private static BestPlayRecords.CBestPlayRecord? tBestBefore(int player) {
		var song = OpenTaiko.SongMount.rChoosenSong;
		if (song?.uniqueId == null) return null;
		string key = song.uniqueId.data.id + OpenTaiko.SongMount.nChoosenSongDifficulty[player].ToString();
		return OpenTaiko.SaveFileInstances[player].data.bestPlaysDistinctCharts.TryGetValue(key, out var best) ? best : null;
	}

	// the tower play for results_tower; the new bests count only for a play that went into the save
	private LuaResultInfo tTowerResultInfo(BestPlayRecords.CBestPlayRecord?[] bestBefore) {
		var song = OpenTaiko.SongMount.rChoosenSong!;
		var game = OpenTaiko.stageGameScreen;
		var chart = game.CChartScore[0];
		var info = new LuaResultInfo {
			title = song.ldTitle.GetString(""),
			subtitle = song.ldSubtitle.GetString(""),
			playerSide = OpenTaiko.P1IsBlue() ? 1 : 0,
			towerType = song.score[(int)Difficulty.Tower]?.ChartInfo.nTowerType ?? "",
			towerSide = (int)song.nSide,
			floor = game.FloorManagement.LastRegisteredFloor,
			totalFloors = song.score[(int)Difficulty.Tower]?.ChartInfo.nTotalFloor ?? 0,
			lives = game.FloorManagement.CurrentNumberOfLives,
			maxLives = game.FloorManagement.MaxNumberOfLives,
			towerRank = GetTowerScoreRank(),
			previousBestFloor = (int)(bestBefore[0]?.TowerBestFloor ?? 0),
			tired = game.FloorManagement.IsTired,
			bpm = game.actPlayInfo.dbBPM[0],
		};
		info.good[0] = chart.nGreat;
		info.ok[0] = chart.nGood;
		info.bad[0] = chart.nMiss;
		info.roll[0] = chart.nRoll;
		info.maxCombo[0] = game.actCombo.nCurrentCombo.MaxValue[0];
		info.adlib[0] = chart.nADLIB;
		info.boom[0] = chart.nMine;
		info.score[0] = (long)game.actScore.Get(0);
		info.saved[0] = this.IsScoreValid[0];
		info.previousBestScore[0] = bestBefore[0]?.HighScore ?? 0;
		info.newBestFloor = info.saved[0] && info.floor > info.previousBestFloor;
		info.newBestScore = info.saved[0] && chart.nScore > info.previousBestScore[0];
		return info;
	}

	// the play for results_regular, an AI battle's included
	private LuaResultInfo tRegularResultInfo(BestPlayRecords.CBestPlayRecord?[] bestBefore) {
		var song = OpenTaiko.SongMount.rChoosenSong!;
		var game = OpenTaiko.stageGameScreen;
		int players = OpenTaiko.ConfigIni.nPlayerCount;
		var info = new LuaResultInfo {
			title = song.ldTitle.GetString(""),
			subtitle = song.ldSubtitle.GetString(""),
			playerCount = players,
			playerSide = OpenTaiko.P1IsBlue() ? 1 : 0,
			aiBattle = OpenTaiko.ConfigIni.bAIBattleMode,
			aiSections = OpenTaiko.ConfigIni.bAIBattleMode ? game.AIBattleSections.Select(s => (int)s.End).ToArray() : [],
		};
		bool[] clear = this.bClear;
		for (int p = 0; p < players; p++) {
			var chart = game.CChartScore[p];
			info.good[p] = chart.nGreat;
			info.ok[p] = chart.nGood;
			info.bad[p] = chart.nMiss;
			info.roll[p] = chart.nRoll;
			info.maxCombo[p] = game.actCombo.nCurrentCombo.MaxValue[p];
			info.adlib[p] = chart.nADLIB;
			info.boom[p] = chart.nMine;
			info.score[p] = (long)game.actScore.Get(p);
			info.previousBestScore[p] = bestBefore[p]?.HighScore ?? 0;
			info.saved[p] = this.IsScoreValid[p];
			info.difficulty[p] = OpenTaiko.SongMount.nChoosenSongDifficulty[p];
			info.gauge[p] = game.actGauge.dbCurrentGaugeValue[p];
			info.gaugeType[p] = (int)HGaugeMethods.tGetGaugeTypeEnum(p);
			info.norma[p] = HGaugeMethods.UNSAFE_Norma(p);
			info.killZone[p] = HGaugeMethods.UNSAFE_KillZonePercent(p);
			info.clearType[p] = this.nClear[p];
			info.scoreRank[p] = this.nScoreRank[p];
			info.cleared[p] = p < clear.Length && clear[p];
			var speech = OpenTaiko.Tx.Characters[CVirtualSlotManager.GetCharacterIndex(p)].metadata.SpeechText;
			info.speech[p] = speech == null ? [] : speech.Select(s => s.GetString("")).ToArray();
		}
		return info;
	}

	// the dan play for results_dan, with its exams as they ended and their bests before this play
	private LuaResultInfo tDanResultInfo(BestPlayRecords.CBestPlayRecord?[] bestBefore) {
		var song = OpenTaiko.SongMount.rChoosenSong!;
		var game = OpenTaiko.stageGameScreen;
		var chart = game.CChartScore[0];
		var danSongs = song.DanSongs!;
		var chartInfo = OpenTaiko.SongMount.rChosenScore?.ChartInfo;
		var info = new LuaResultInfo {
			title = song.ldTitle.GetString(""),
			subtitle = song.ldSubtitle.GetString(""),
			playerSide = OpenTaiko.P1IsBlue() ? 1 : 0,
			danTick = chartInfo?.nDanTick ?? 0,
			danTickColor = chartInfo is { } ci ? new LuaColor(ci.cDanTickColor) : new LuaColor(255, 255, 255),
			danResult = (int)GetResultExamStatus(),
			danCombo = this.stPlayRecord.nBadCount != 0 ? 0 : this.stPlayRecord.nOkCount != 0 ? 1 : 2,
			totalHits = chart.nGreat + chart.nGood + game.GetRoll(0),
			songReached = game.actDan.NowShowingNumber,
			examSong = game.actDan.NowCymbolShowingNumber,
			danSongs = danSongs.Select((s, i) => {
				var counts = i < game.DanSongScore.Length ? game.DanSongScore[i] : null;
				return new LuaResultDanSong {
					title = s.bTitleShow ? "???" : s.Title,
					subtitle = s.bTitleShow ? "" : s.SubTitle ?? "",
					difficulty = s.Difficulty,
					level = s.Level,
					good = counts?.nGreat ?? 0,
					ok = counts?.nGood ?? 0,
					bad = counts?.nMiss ?? 0,
					roll = counts?.nRoll ?? 0,
				};
			}).ToArray(),
		};
		info.good[0] = chart.nGreat;
		info.ok[0] = chart.nGood;
		info.bad[0] = chart.nMiss;
		info.roll[0] = chart.nRoll;
		info.maxCombo[0] = game.actCombo.nCurrentCombo.MaxValue[0];
		info.adlib[0] = chart.nADLIB;
		info.boom[0] = chart.nMine;
		info.score[0] = (long)game.actScore.Get(0);
		info.previousBestScore[0] = bestBefore[0]?.HighScore ?? 0;
		info.saved[0] = this.IsScoreValid[0];
		info.danGradeChange = this.danGradeChange;
		var dans = OpenTaiko.SaveFileInstances[0].data.DanTitles;
		if (this.danGradeTitle != "" && dans != null && dans.TryGetValue(this.danGradeTitle, out var grade))
			info.danGrade = new LuaResultDanGrade { title = this.danGradeTitle, gold = grade.isGold, tier = grade.clearStatus };
		info.previousNameplateDan = new LuaResultDanGrade {
			title = this.nameplateDanBefore.dan ?? "", gold = this.nameplateDanBefore.gold, tier = this.nameplateDanBefore.type,
		};

		// the save keeps, per exam, one best amount for each song that has the exam, in the songs' order
		var best = bestBefore[0];
		var savedExams = best == null ? null
			: new[] { best.DanExam1, best.DanExam2, best.DanExam3, best.DanExam4, best.DanExam5, best.DanExam6, best.DanExam7 };
		int tSaved(int exam, int danSong) {
			if (savedExams == null) return -1;
			int at = 0;
			for (int s = 0; s < danSong; s++) {
				if (danSongs[s].Dan_C[exam] != null) at++;
			}
			return at < savedExams[exam].Count ? savedExams[exam][at] : -1;
		}

		var exams = new List<LuaResultExam>();
		for (int i = 0; i < CExamInfo.cMaxExam; i++) {
			if (this.stPlayRecord.Dan_C[i] is not Dan_C exam || !exam.ExamIsEnable) continue;
			// the exam shown is one of the songs' exams: the first song's for an exam on the whole dan
			int ownSong = Math.Max(0, danSongs.FindIndex(s => ReferenceEquals(s.Dan_C[i], exam)));
			var shown = tExamInfo(exam, tSaved(i, ownSong));
			if (game.actDan.IsPerSongExam(i))
				shown.songs = danSongs.Select((s, j) => s.Dan_C[i] is Dan_C e ? tExamInfo(e, tSaved(i, j)) : null).ToArray();
			exams.Add(shown);
		}
		info.exams = [.. exams];
		return info;
	}

	// an exam as the play ended; saved is the save's best raw amount (-1 for none), shown in the terms of Amount
	private static LuaResultExam tExamInfo(Dan_C exam, int saved) => new() {
		type = (int)exam.ExamType,
		range = (int)exam.ExamRange,
		value = exam.GetValue()[0],
		amount = exam.GetDisplayedAmount(),
		previousBestAmount = saved < 0 ? -1
			: exam.ExamRange == Exam.Range.Less ? Math.Max(0, exam.GetValue()[0] - saved) : saved,
		percent = exam.GetAmountToPercent(),
		status = (int)exam.GetExamStatus(),
	};

	// CStage 実装

	public override void Activate() {

		Trace.TraceInformation("結果ステージを活性化します。");
		Trace.Indent();
		bAddedToRecentlyPlayedSongs = false;
		this.modalDuck = this.modalDuckFrom = this.modalDuckTo = 100;
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
				(CModBalancing.tGetModMultiplier(CModBalancing.EBalancingType.SCORE, 0) < 1f),
				(CModBalancing.tGetModMultiplier(CModBalancing.EBalancingType.SCORE, 1) < 1f),
				(CModBalancing.tGetModMultiplier(CModBalancing.EBalancingType.SCORE, 2) < 1f),
				(CModBalancing.tGetModMultiplier(CModBalancing.EBalancingType.SCORE, 3) < 1f),
				(CModBalancing.tGetModMultiplier(CModBalancing.EBalancingType.SCORE, 4) < 1f)
			};

			this.IsScoreValid = [false, false, false, false, false];
			this.ClearStatusesSaved = [0, 0, 0, 0, 0];
			this.ScoreRanksSaved = [0, 0, 0, 0, 0];
			// each player's bests before this play is saved, for the result's previous bests
			var bestBefore = Enumerable.Range(0, 5).Select(p => p < OpenTaiko.ConfigIni.nPlayerCount ? tBestBefore(p) : null).ToArray();

			{
				#region [ 初期化 ]
				//---------------------
				this.eFadeOutCompleteWhenReturnValue = EReturnValue.Continue;
				this.bAnimeComplete = false;
				this.bIsCheckedWhetherResultScreenShouldSaveOrNot = false;              // #24609 2011.3.14 yyagi

				//---------------------
				#endregion

				#region [ Results calculus ]
				//---------------------

				if (OpenTaiko.SongMount.nChoosenSongDifficulty[0] != (int)Difficulty.Dan && OpenTaiko.SongMount.nChoosenSongDifficulty[0] != (int)Difficulty.Tower) {
					for (int p = 0; p < OpenTaiko.ConfigIni.nPlayerCount; p++) {
						var ccf = OpenTaiko.stageGameScreen.CChartScore[p];

						this.nClear[p] = 0;
						if (!OpenTaiko.stageGameScreen.IsStageFailed(p) && HGaugeMethods.UNSAFE_FastNormaCheck(p)) {
							this.nClear[p] = 2;
							if (ccf.nMiss == 0 && ccf.nMine == 0) {
								this.nClear[p] = 3;
								if (ccf.nGood == 0) this.nClear[p] = 4;
							}

							if (assistedClear[p]) this.nClear[p] = 1;

							clearStatuses[p] = this.nClear[p] - 1;

						}

						if ((int)OpenTaiko.stageGameScreen.actScore.Get(p) < 500000) {
							this.nScoreRank[p] = 0;
						} else {
							var sr = OpenTaiko.stageGameScreen.ScoreRank.ScoreRank[p];

							for (int i = 0; i < 7; i++) {
								if ((int)OpenTaiko.stageGameScreen.actScore.Get(p) >= sr[i]) {
									this.nScoreRank[p] = i + 1;
								}
							}
						}
						scoreRanks[p] = this.nScoreRank[p] - 1;

					}


				}

				//---------------------
				#endregion

				#region [ Saves calculus ]
				//---------------------

				// Clear and score ranks



				if (OpenTaiko.SongMount.nChoosenSongDifficulty[0] != (int)Difficulty.Dan && OpenTaiko.SongMount.nChoosenSongDifficulty[0] != (int)Difficulty.Tower) {
					// Regular (Ensou game) Score and Score Rank saves

					#region [Regular saves]

					for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
						int diff = OpenTaiko.SongMount.nChoosenSongDifficulty[i];

						ClearStatus_Replay[i] = this.nClear[i];
						ScoreRank_Replay[i] = this.nScoreRank[i];
					}

					#endregion

				} else if (OpenTaiko.SongMount.nChoosenSongDifficulty[0] == (int)Difficulty.Dan) {
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

					Exam.Status examStatus = GetResultExamStatus();

					int clearValue = 0;

					if (examStatus != Exam.Status.Failure) {
						// Red Goukaku
						clearValue += 1;

						// Gold Goukaku
						if (examStatus == Exam.Status.Better_Success)
							clearValue += 1;

						// Gold Iki
						if (this.stPlayRecord.nBadCount == 0) {
							clearValue += 2;

							// Rainbow Iki
							if (this.stPlayRecord.nOkCount == 0)
								clearValue += 2;
						}

						if (assistedClear[0]) clearStatuses[0] = (examStatus == Exam.Status.Better_Success) ? 1 : 0;
						else clearStatuses[0] = clearValue + 1;

					}

					if (isAutoDisabled(0)) {
						ClearStatus_Replay[0] = clearValue;
					}

					// this.st演奏記録[0].nクリア[0] = Math.Max(ini[0].stセクション[0].nクリア[0], clearValue);

					// Unlock dan grade (a watched replay earns nothing); a new or better grade goes onto the nameplate,
					// which results_dan shows going on
					var danSave = OpenTaiko.SaveFileInstances[0];
					this.nameplateDanBefore = (danSave.data.Dan, danSave.data.DanGold, danSave.data.DanType);
					this.danGradeTitle = "";
					this.danGradeChange = 0;
					if (clearValue > 0 && !OpenTaiko.ConfigIni.bAutoPlay[0] && !OpenTaiko.bReplayMode[0]) {
						string dan_title = OpenTaiko.SongMount.rChoosenSong!.ldTitle.GetString("").RemoveTags();
						if (dan_title.Length > 2) dan_title = dan_title.Substring(0, 2);

						bool known = danSave.data.DanTitles?.ContainsKey(dan_title) ?? false;
						this.danGradeTitle = dan_title;
						if (danSave.tUpdateDanTitle(dan_title, clearValue % 2 == 0, (clearValue - 1) / 2)) {
							this.danGradeChange = known ? 2 : 1;
							danSave.tSetNameplateDan(dan_title);
							OpenTaiko.NamePlate?.tNamePlateRefreshTitles(0);
						}
					}

					#endregion

				} else if (OpenTaiko.SongMount.nChoosenSongDifficulty[0] == (int)Difficulty.Tower) {
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

				int level = OpenTaiko.SongMount.rChoosenSong!.nLevel[diff];
				CTja.ELevelIcon levelIcon = OpenTaiko.SongMount.rChoosenSong.nLevelIcon[diff];

				return (diffArr[Math.Min(diff, 6)] + "Lv." + level + diffArrIcon[(int)levelIcon]);
			}

			string details = OpenTaiko.ConfigIni.SendDiscordPlayingInformation ? OpenTaiko.SongMount.rChoosenSong!.ldTitle.GetString("")
				+ diffToString(OpenTaiko.SongMount.nChoosenSongDifficulty[0]) : "";

			// Discord takes at most 128 UTF-8 bytes; cutting inside a character would make it throw
			details = details.TruncateUtf8(128);

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

			int nTotalHits = this.stPlayRecord.nOkCount + this.stPlayRecord.nBadCount + this.stPlayRecord.nGoodCount;

			double dAccuracyRate = Math.Pow((50 * this.stPlayRecord.nOkCount + 100 * this.stPlayRecord.nGoodCount) / (double)(100 * nTotalHits), 3);

			double diffModifier;
			float starRate;
			float redStarRate;


			float[] modMultipliers =
			{
				CModBalancing.tGetModMultiplier(CModBalancing.EBalancingType.COINS, 0),
				CModBalancing.tGetModMultiplier(CModBalancing.EBalancingType.COINS, 1),
				CModBalancing.tGetModMultiplier(CModBalancing.EBalancingType.COINS, 2),
				CModBalancing.tGetModMultiplier(CModBalancing.EBalancingType.COINS, 3),
				CModBalancing.tGetModMultiplier(CModBalancing.EBalancingType.COINS, 4)
			};

			float getCoinMul(int player) {
				var chara = OpenTaiko.Tx.Characters[OpenTaiko.SaveFileInstances[player].data.Character];
				var puchichara = OpenTaiko.Tx.Puchichara[PuchiChara.tGetPuchiCharaIndexByName(player)];

				return HGaugeMethods.GetCoinMultiplier(HGaugeMethods.tGetGaugeTypeEnum(player))
					* chara.GetEffectCoinMultiplier(gaugeEnabled: false)
					* puchichara.GetEffectCoinMultiplier();
			}

			if (OpenTaiko.SongMount.nChoosenSongDifficulty[0] == (int)Difficulty.Tower) {
				diffModifier = 3;

				int stars = Math.Min(13, OpenTaiko.SongMount.rChoosenSong!.score[(int)Difficulty.Tower]!.ChartInfo.nLevel[(int)Difficulty.Tower]);

				starRate = Math.Min(10, stars) / 2;
				redStarRate = Math.Max(0, stars - 10) * 4;

				int maxFloors = OpenTaiko.SongMount.rChoosenSong.score[(int)Difficulty.Tower]!.ChartInfo.nTotalFloor;

				double floorRate = Math.Pow(OpenTaiko.stageGameScreen.FloorManagement.LastRegisteredFloor / (double)maxFloors, 2);
				double lengthBonus = Math.Max(1, maxFloors / 140.0);

				#region [Clear modifier]

				int clearModifier = 0;

				if (this.stPlayRecord.nBadCount == 0) {
					clearModifier = (int)(5 * lengthBonus);
					if (this.stPlayRecord.nOkCount == 0) {
						clearModifier = (int)(12 * lengthBonus);
					}
				}

				#endregion

				// this.nEarnedMedalsCount[0] = stars;
				this.nEarnedMedalsCount[0] = 5 + (int)((diffModifier * (starRate + redStarRate)) * (floorRate * lengthBonus)) + clearModifier;
				this.nEarnedMedalsCount[0] = Math.Max(5, (int)(this.nEarnedMedalsCount[0] * modMultipliers[0] * getCoinMul(0)));
			} else if (OpenTaiko.SongMount.nChoosenSongDifficulty[0] == (int)Difficulty.Dan) {
				int partialScore = 0;

				#region [Clear and Goukaku modifier]

				Exam.Status examStatus = GetResultExamStatus();

				int clearModifier = -1;
				int goukakuModifier = 0;

				if (examStatus != Exam.Status.Failure) {
					clearModifier = 0;
					if (this.stPlayRecord.nBadCount == 0) {
						clearModifier = 4;
						if (this.stPlayRecord.nOkCount == 0)
							clearModifier = 6;
					}

					if (examStatus == Exam.Status.Better_Success)
						goukakuModifier = 20;
				}

				#endregion

				#region [Partial scores]

				for (int i = 0; i < OpenTaiko.SongMount.rChoosenSong!.DanSongs!.Count; i++) {
					if (OpenTaiko.SongMount.rChoosenSong.DanSongs[i] != null) {
						int diff = OpenTaiko.SongMount.rChoosenSong.DanSongs[i].Difficulty;
						int stars = Math.Min(13, OpenTaiko.SongMount.rChoosenSong.DanSongs[i].Level);

						//diffModifier = Math.Max(1, Math.Min(3, diff));
						diffModifier = Math.Max(2.0, Math.Min(2.0 + (diff * 0.25), 3.0));

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
					int diff = OpenTaiko.SongMount.nChoosenSongDifficulty[i];
					int stars = Math.Min(13, OpenTaiko.SongMount.rChoosenSong!.score[diff]?.ChartInfo.nLevel[diff] ?? -1);

					//diffModifier = Math.Max(1, Math.Min(3, diff));
					diffModifier = Math.Max(2.0, Math.Min(2.0 + (diff * 0.25), 3.0));

					starRate = Math.Min(10, stars) / 2;
					redStarRate = Math.Max(0, stars - 10) * 4;

					#region [Clear modifier]

					double[] modifiers = { -1, 0, 2, 3 };

					double clearModifier = modifiers[0];

					if (HGaugeMethods.UNSAFE_FastNormaCheck(i)) {
						clearModifier = modifiers[1] * diffModifier;
						if (OpenTaiko.stageGameScreen.CChartScore[i].nMiss == 0) {
							clearModifier = modifiers[2] * diffModifier;
							if (OpenTaiko.stageGameScreen.CChartScore[i].nGood == 0)
								clearModifier = modifiers[3] * diffModifier;
						}
					}

					#endregion

					#region [Score rank modifier]

					double[] srModifiers = { 0, 0, 0, 0, 1, 1, 2, 3 };

					// int s = TJAPlayer3.stage演奏ドラム画面.ScoreRank.ScoreRank[1];

					double scoreRankModifier = srModifiers[0] * diffModifier;

					for (int j = 1; j < 8; j++) {
						if (OpenTaiko.stageGameScreen.actScore.Get(i) >= OpenTaiko.stageGameScreen.ScoreRank.ScoreRank[i][j - 1])
							scoreRankModifier = srModifiers[j] * diffModifier;
					}

					#endregion

					nTotalHits = OpenTaiko.stageGameScreen.CChartScore[i].nGood + OpenTaiko.stageGameScreen.CChartScore[i].nMiss + OpenTaiko.stageGameScreen.CChartScore[i].nGreat;

					dAccuracyRate = Math.Pow((50 * OpenTaiko.stageGameScreen.CChartScore[i].nGood + 100 * OpenTaiko.stageGameScreen.CChartScore[i].nGreat) / (double)(100 * nTotalHits), 3);

					if (clearModifier < 0)
						this.nEarnedMedalsCount[i] = 5;
					else {
						this.nEarnedMedalsCount[i] = (int)(5 + (int)((diffModifier * (starRate + redStarRate)) * dAccuracyRate) + clearModifier + scoreRankModifier);
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

				var _sf = OpenTaiko.SaveFileInstances[i];

				if (OpenTaiko.ConfigIni.bAIBattleMode && i == 0) {
					_sf.tRegisterAIBattleModePlay(bClear[0]);
				}

				if (this.nEarnedMedalsCount[i] > 0 && !OpenTaiko.bReplayMode[i])
					_sf.tEarnCoins(this.nEarnedMedalsCount[i]);

				if (!OpenTaiko.ConfigIni.bAutoPlay[i]
					&& !OpenTaiko.bReplayMode[i]   // watching a replay must not write score/coins/playcount
					&& !(OpenTaiko.ConfigIni.bAIBattleMode && i == 1)) {
					// Unsafe function, it is the only appropriate place to call it
					if (DBSaves.RegisterPlay(i, clearStatuses[i], scoreRanks[i])) {
						this.IsScoreValid[i] = true;
						this.ClearStatusesSaved[i] = clearStatuses[i];
						this.ScoreRanksSaved[i] = scoreRanks[i];
					}

					// Track per-character play count for palette unlock gating.
					string charFolder = OpenTaiko.Tx.Characters[_sf.data.Character]?.dirName ?? "";
					if (!string.IsNullOrEmpty(charFolder)) {
						string key = ".character_playcount_" + charFolder;
						_sf.tSetGlobalCounter(key, _sf.tGetGlobalCounter(key) + 1);
					}
				}
			}


			//TJAPlayer3.NamePlateConfig.tEarnCoins(this.nEarnedMedalsCount);

			#endregion

			#region [Set triggers for the cutscenes that will be met]

			OpenTaiko.stageCutScene.RegisterMetOutros();

			#endregion

			#region [Replay files generation]

			for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
				if (OpenTaiko.ConfigIni.bAutoPlay[i] || OpenTaiko.bReplayMode[i])
					continue;
				if (OpenTaiko.ConfigIni.bAIBattleMode && i == 1)
					continue;
				if (OpenTaiko.ReplayInstances[i].tResultsRegisterReplayInformations(this.nEarnedMedalsCount[i], ClearStatus_Replay[i], ScoreRank_Replay[i]))
					OpenTaiko.ReplayInstances[i].tSaveReplayFile();
			}

			#endregion

			#region [Modals preprocessing]

			for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
				if (OpenTaiko.ConfigIni.bAutoPlay[i] || OpenTaiko.bReplayMode[i] || OpenTaiko.ConfigIni.bAIBattleMode && i == 1) continue;   // replays award nothing

				if (this.nEarnedMedalsCount[i] > 0)
					OpenTaiko.ModalManager.rModalQueue.tAddModal(
						new Modal(
							Modal.EModalType.Coin,
							0,
							(long)this.nEarnedMedalsCount[i],
							OpenTaiko.SaveFileInstances[i].data.Medals
						),
						i);

				// Check unlockables
				{
					OpenTaiko.Databases.DBNameplateUnlockables.tGetUnlockedItems(i, OpenTaiko.ModalManager.rModalQueue);
					OpenTaiko.Databases.DBSongUnlockables.tGetUnlockedItems(i, OpenTaiko.ModalManager.rModalQueue);

					foreach (var puchi in OpenTaiko.Tx.Puchichara) {
						puchi.tGetUnlockedItems(i, OpenTaiko.ModalManager.rModalQueue);
					}

					foreach (var chara in OpenTaiko.Tx.Characters) {
						chara.tGetUnlockedItems(i, OpenTaiko.ModalManager.rModalQueue);
					}
				}

			}

			#endregion

			base.Activate();

			this.EndAnime = false;

			if (OpenTaiko.SongMount.nChoosenSongDifficulty[0] == (int)Difficulty.Tower) {
				// the skin's results_tower draws the whole tower result
				ResultsTower?.Activate(tTowerResultInfo(bestBefore));
			} else if (OpenTaiko.SongMount.nChoosenSongDifficulty[0] == (int)Difficulty.Dan) {
				Background = new LuaBackgroundWrapper(CSkin.Path($@"{TextureLoader.BASE}{TextureLoader.DANRESULT}"));
				Background.Activate(_state.Refreshed());
				// the skin's results_dan draws the rest of the dan result
				ResultsDan?.Activate(tDanResultInfo(bestBefore));
			} else {
				Background = new LuaBackgroundWrapper(CSkin.Path(OpenTaiko.ConfigIni.bAIBattleMode
					? $@"{TextureLoader.BASE}{TextureLoader.RESULT}AIBattle"
					: $@"{TextureLoader.BASE}{TextureLoader.RESULT}"));
				Background.Activate(_state.Refreshed());
				// the skin's results_regular draws the rest of the result, an AI battle's too
				ResultsRegular?.Activate(tRegularResultInfo(bestBefore));
			}
		} finally {
			Trace.TraceInformation("結果ステージの活性化を完了しました。");
			Trace.Unindent();
		}
	}
	public override void DeActivate() {
		// the fun mods forced for Dynamic Beat, then player 2's own modes in AI battle, go back after the replay was saved,
		// before a watched replay's mods
		CStagePlayScreenCommon.RestoreSharedDynamicBeat(OpenTaiko.ConfigIni);
		AIBattleModMirror.Restore(OpenTaiko.ConfigIni);
		// leaving the result screen after watching a replay: drop replay mode + restore the real mods (kept this
		// long so the auto modicon + persistence-skip held through results)
		if (OpenTaiko.bReplayMode[0]) {
			CSongReplay.tRestoreVirtualMods();
			for (int i = 0; i < 5; i++) { OpenTaiko.bReplayMode[i] = false; OpenTaiko.ReplayPlayback[i] = null; }
		}

		OpenTaiko.tDisposeSafely(ref Background);
		tDeactivateResults();

		if (this.rResultSound != null) {
			OpenTaiko.SoundManager.tDisposeSound(this.rResultSound);
			this.rResultSound = null;
		}

		if (this.ctAppear != null) {
			this.ctAppear = null;
		}
		base.DeActivate();
	}
	public override int Draw() {
		if (!base.IsDeActivated) {


			// int num;

			if (base.IsFirstDraw) {
				this.ctAppear = new CCounter(0, 100, 5, OpenTaiko.Timer);
				this.actFIFromGameplay.tFadeInStart();
				base.ePhaseID = CStage.EPhase.Common_FADEIN;

				if (this.rResultSound != null) {
					this.rResultSound.PlayStart();
				}

				base.IsFirstDraw = false;
			}
			this.bAnimeComplete = true;
			if (this.ctAppear!.IsTicked) {
				this.ctAppear.Tick();
				if (this.ctAppear.IsEnded) {
					this.ctAppear.Stop();
				} else {
					this.bAnimeComplete = false;
				}
			}

			// 描画

			_state.RefreshGameplay();   // result reflects the just-finished game (clear/fail, gauge, etc.)
			Background?.Update(_state);
			Background?.Draw(_state);

			// the skin's results ROActivity draws the result and plays its music
			if (ActiveResults is { IsActive: true } resultsUI) {
				resultsUI.Update();
				resultsUI.Draw();
			}

			// Display medals debug

			// TJAPlayer3.act文字コンソール.tPrint(0, 12, C文字コンソール.Eフォント種別.白, this.nEarnedMedalsCount[0].ToString());
			//TJAPlayer3.act文字コンソール.tPrint(0, 25, C文字コンソール.Eフォント種別.白, this.nEarnedMedalsCount[1].ToString());







			#region [Display modals]

			OpenTaiko.ModalManager.Draw();
			this.tDuckMusicForModal(OpenTaiko.ModalManager.IsShowing);

			#endregion

			if (base.ePhaseID == CStage.EPhase.Common_FADEIN) {
				if (this.actFIFromGameplay.Draw() != 0) {
					base.ePhaseID = CStage.EPhase.Common_NORMAL;
				}
			} else if ((base.ePhaseID == CStage.EPhase.Common_FADEOUT))         //&& ( this.actFO.On進行描画() != 0 ) )
			{
				if (this.actFO.Draw() != 0) {
					// the screen is black: the results module goes, its music with it
					tDeactivateResults();
					return (int)this.eFadeOutCompleteWhenReturnValue;
				}
			}

			#region [ #24609 2011.3.14 yyagi ランク更新or演奏型スキル更新時、リザルト画像をpngで保存する ]
			if (this.bAnimeComplete == true && this.bIsCheckedWhetherResultScreenShouldSaveOrNot == false  // #24609 2011.3.14 yyagi; to save result screen in case BestRank or HiSkill.
									 && OpenTaiko.ConfigIni.bIsAutoResultCapture)                                               // #25399 2011.6.9 yyagi
			{
				string strFullPath =
					Path.Combine(OpenTaiko.strEXEFolder, "Capture_img");
				strFullPath = Path.Combine(strFullPath, DateTime.Now.ToString("yyyyMMddHHmmss") + ".png");
				OpenTaiko.app.SaveResultScreen(strFullPath);

				this.bIsCheckedWhetherResultScreenShouldSaveOrNot = true;
			}
			#endregion

			// キー入力

			if (base.ePhaseID == CStage.EPhase.Common_NORMAL) {
				if (OpenTaiko.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.Escape)) {
					#region [ Return to song select screen (Faster method) ]

					OpenTaiko.Skin.soundDecideSFX.tPlay();
					actFO.tFadeOutStart();

					tPostprocessing();
					base.ePhaseID = CStage.EPhase.Common_FADEOUT;
					this.eFadeOutCompleteWhenReturnValue = EReturnValue.Complete;

					#endregion
				}
				// When Lua finishes the last modal it self-deactivates; advance to song select.
				if (OpenTaiko.ModalManager.AllModalsDone) {
					OpenTaiko.ModalManager.AllModalsDone = false;
					actFO.tFadeOutStart();
					tPostprocessing();
					base.ePhaseID = CStage.EPhase.Common_FADEOUT;
					this.eFadeOutCompleteWhenReturnValue = EReturnValue.Complete;
				}

				if (((OpenTaiko.Pad.bPressed(EKeyConfigPart.Taiko, EPad.CY)
					  || OpenTaiko.Pad.bPressed(EKeyConfigPart.Taiko, EPad.RD))
					 || (OpenTaiko.Pad.bPressed(EKeyConfigPart.Taiko, EPad.LC)
						 || (OpenTaiko.Pad.bPressed(EKeyConfigPart.Taiko, EPad.Decide)
							 || OpenTaiko.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.Return))))) {

					#region [ Skip animations ]

					if (OpenTaiko.SongMount.nChoosenSongDifficulty[0] < (int)Difficulty.Tower
						&& ResultsRegular is { IsActive: true } regularUI && regularUI.Call("skip") is [true, ..]) {
						// results_regular was still animating: it jumped to the clear, and the background with it
						OpenTaiko.Skin.soundDecideSFX.tPlay();
						Background?.Call("skipAnime");
					} else if (OpenTaiko.SongMount.nChoosenSongDifficulty[0] == (int)Difficulty.Dan
							   && ResultsDan is { IsActive: true } danUI && danUI.Call("skip") is [true, ..]) {
						// results_dan was still animating: it jumped to its end
						OpenTaiko.Skin.soundDecideSFX.tPlay();
					} else if (OpenTaiko.SongMount.nChoosenSongDifficulty[0] == (int)Difficulty.Dan
							   && ResultsDan is { IsActive: true } danNext && danNext.Call("advance") is [true, ..]) {
						// results_dan went on to another of its screens before the reward pop-ups
						OpenTaiko.Skin.soundDecideSFX.tPlay();
					} else if (OpenTaiko.SongMount.nChoosenSongDifficulty[0] == (int)Difficulty.Tower
							   && ResultsTower is { IsActive: true } towerUI && towerUI.Call("skip") is [true, ..]) {
						// results_tower was still animating: it jumped to its end
						OpenTaiko.Skin.soundDecideSFX.tPlay();
					}

					#endregion

					else {
						// Animations done — trigger first modal pop or exit when nothing queued.
						// Subsequent modals are advanced automatically by the Lua script (see CModalManager.Draw).
						bool _modalsProcessed = OpenTaiko.ModalManager.InputManagement();
						if (_modalsProcessed) {
							#region [ Return to song select screen ]
							OpenTaiko.Skin.soundDecideSFX.tPlay();
							actFO.tFadeOutStart();
							tPostprocessing();
							base.ePhaseID = CStage.EPhase.Common_FADEOUT;
							this.eFadeOutCompleteWhenReturnValue = EReturnValue.Complete;
							#endregion
						}
					}
				}


				if (OpenTaiko.InputManager.Keyboard.KeyPressing((int)SlimDXKeys.Key.LeftArrow) ||
					OpenTaiko.Pad.bPressed(EKeyConfigPart.Taiko, EPad.LeftChange) ||
					OpenTaiko.InputManager.Keyboard.KeyPressing((int)SlimDXKeys.Key.RightArrow) ||
					OpenTaiko.Pad.bPressed(EKeyConfigPart.Taiko, EPad.RightChange)) {
					// results_dan swaps between the exams and the songs once it has shown everything
					if (OpenTaiko.SongMount.nChoosenSongDifficulty[0] == (int)Difficulty.Dan
						&& ResultsDan is { IsActive: true } danUI && danUI.Call("swap") is [true, ..]) {
						OpenTaiko.Skin.soundChangeSFX.tPlay();
					}
				}
			}
		}
		return 0;
	}

	public void tPostprocessing() {

		if (!bAddedToRecentlyPlayedSongs) {
			// DanBuilder charts have no persistent uniqueId; skip recently-played tracking.
			if (OpenTaiko.SongMount.rChoosenSong!.uniqueId == null) {
				bAddedToRecentlyPlayedSongs = true;
				return;
			}

			OpenTaiko.RecentlyPlayedSongs.tAddChart(OpenTaiko.SongMount.rChoosenSong.uniqueId.data.id);

			bAddedToRecentlyPlayedSongs = true;
		}

	}

	public enum EReturnValue : int {
		Continue,
		Complete
	}

	// その他

	#region [ private ]
	//-----------------

	public bool bAddedToRecentlyPlayedSongs;
	public bool EndAnime;

	private CCounter? ctAppear;
	private EReturnValue eFadeOutCompleteWhenReturnValue;
	private CActFIFOResult actFIFromGameplay;
	private CActFIFOBlack actFO;
	private CActOptionPanel actOption;

	private bool bAnimeComplete;
	private bool bIsCheckedWhetherResultScreenShouldSaveOrNot;              // #24509 2011.3.14 yyagi
	private CSound? rResultSound;
	public LuaBackgroundWrapper? Background;
	private readonly LuaBackgroundState _state = new();

	public bool[] bClear {
		get {
			if (OpenTaiko.ConfigIni.bAIBattleMode) {
				int clearCount = 0;
				for (int i = 0; i < OpenTaiko.stageGameScreen.AIBattleSections.Count; i++) {
					if (OpenTaiko.stageGameScreen.AIBattleSections[i].End == CStagePlayScreenCommon.AIBattleSection.EndType.Clear) {
						clearCount++;
					}
				}
				return new bool[] { !OpenTaiko.stageGameScreen.IsStageFailed(0) && clearCount >= OpenTaiko.stageGameScreen.AIBattleSections.Count / 2.0, false };
			} else {
				return OpenTaiko.stageGameScreen.bIsAlreadyCleared.Select((x, i) => !OpenTaiko.stageGameScreen.IsStageFailed(i) && x).ToArray();
			}
		}
	}

	public Exam.Status GetResultExamStatus() {
		Exam.Status examStatus = OpenTaiko.stageGameScreen.actDan.GetResultExamStatus(this.stPlayRecord.Dan_C, OpenTaiko.SongMount.rChoosenSong!.DanSongs!, forceFinalJudge: true);
		if (OpenTaiko.stageGameScreen.IsStageFailed(0))
			examStatus = Exam.Status.Failure;
		return examStatus;
	}

	// the dan grade this play earned (its title "" when none), 0 / 1 new / 2 better, and the nameplate's before
	private string danGradeTitle = "";
	private int danGradeChange;
	private (string dan, bool gold, int type) nameplateDanBefore = ("", false, 0);

	private static LuaROActivityWrapper? ResultsTower => LuaROActivityWrapper.GetROActivity("results_tower");
	private static LuaROActivityWrapper? ResultsRegular => LuaROActivityWrapper.GetROActivity("results_regular");
	private static LuaROActivityWrapper? ResultsDan => LuaROActivityWrapper.GetROActivity("results_dan");

	// the results ROActivity of the play: results_tower, results_dan or results_regular
	private static LuaROActivityWrapper? ActiveResults => OpenTaiko.SongMount.nChoosenSongDifficulty[0] switch {
		(int)Difficulty.Tower => ResultsTower,
		(int)Difficulty.Dan => ResultsDan,
		_ => ResultsRegular,
	};

	private static void tDeactivateResults() {
		foreach (var results in new[] { ResultsTower, ResultsRegular, ResultsDan }) {
			if (results is { IsActive: true }) results.Deactivate();
		}
	}

	// Coins information
	private int[] nEarnedMedalsCount = { 0, 0, 0, 0, 0 };

	// The result music dims while a reward modal plays so the modal's own sounds are heard: its level (percent)
	// eases with a smoothstep down to MODAL_DUCK_LEVEL and back up, so neither edge is rough.
	private const int MODAL_DUCK_LEVEL = 30;
	private const double MODAL_DUCK_IN_MS = 350;
	private const double MODAL_DUCK_OUT_MS = 800;
	private double modalDuck = 100, modalDuckFrom = 100, modalDuckTo = 100, modalDuckMs = 1;
	private long modalDuckStartMs;

	private void tDuckMusicForModal(bool dimmed) {
		double target = dimmed ? MODAL_DUCK_LEVEL : 100;
		if (target != this.modalDuckTo) {
			this.modalDuckFrom = this.modalDuck;
			this.modalDuckTo = target;
			this.modalDuckMs = dimmed ? MODAL_DUCK_IN_MS : MODAL_DUCK_OUT_MS;
			this.modalDuckStartMs = OpenTaiko.Timer.NowTimeMs;
		}
		if (this.modalDuck == this.modalDuckTo) return;
		double k = Math.Clamp((OpenTaiko.Timer.NowTimeMs - this.modalDuckStartMs) / this.modalDuckMs, 0.0, 1.0);
		k = k * k * (3.0 - 2.0 * k);
		this.modalDuck = this.modalDuckFrom + (this.modalDuckTo - this.modalDuckFrom) * k;
		this.tSetMusicLevel((int)Math.Round(this.modalDuck));
	}

	// the results module plays the music, at this level
	private void tSetMusicLevel(int level) {
		if (ActiveResults is { IsActive: true } results) results.Call("setMusicLevel", level);
	}


	//-----------------
	#endregion
}
