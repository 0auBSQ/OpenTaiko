using Newtonsoft.Json;

namespace TJAPlayer3 {
	internal class SaveFile {

		public void tSaveFile(string filename) {
			path = @$"Saves{Path.DirectorySeparatorChar}" + filename + @".json";
			name = filename;

			if (!File.Exists(path)) {
				this.data.Name = filename;
				tSaveFile();
			}

			tLoadFile();

			tInitSaveFile();
		}

		public void tInitSaveFile() {
			data.bestPlays = DBSaves.GetBestPlaysAsDict(data.SaveId);
			data.tFactorizeBestPlays();
		}

		public void tLoadUnlockables() {
			data.UnlockedCharacters = DBSaves.FetchStringUnlockedAsset(data.SaveId, "unlocked_characters");
			data.UnlockedPuchicharas = DBSaves.FetchStringUnlockedAsset(data.SaveId, "unlocked_puchicharas");
			data.UnlockedSongs = DBSaves.FetchStringUnlockedAsset(data.SaveId, "unlocked_songs");
			data.UnlockedNameplateIds = DBSaves.FetchUnlockedNameplateIds(data.SaveId);
			data.DanTitles = DBSaves.FetchUnlockedDanTitles(data.SaveId);
		}


		#region [Medals and PlayCount]

		public void tEarnCoins(int amount) {
			data.Medals += amount;
			data.TotalEarnedMedals += amount;

			// Small trick here, each actual play (excluding Auto, AI, etc) are worth at least 5 coins for the player, whatever which mode it is (Dan, Tower, Taiko mode, etc)
			// Earn Coins is also called once per play, so we just add 1 here to the total playcount
			data.TotalPlaycount += 1;
			DBSaves.AlterCoinsAndTotalPlayCount(data.SaveId, amount, 1);
			//tSaveFile();
		}

		// Return false if the current amount of coins is to low
		public bool tSpendCoins(int amount) {
			if (data.Medals < amount)
				return false;

			data.Medals -= amount;
			DBSaves.AlterCoinsAndTotalPlayCount(data.SaveId, -amount, 0);
			//tSaveFile();

			return true;
		}

		public void tRegisterAIBattleModePlay(bool IsWon) {
			data.AIBattleModePlaycount++;
			if (IsWon) data.AIBattleModeWins++;
			DBSaves.RegisterAIBattleModePlay(data.SaveId, IsWon);
		}

		#endregion

		#region [Dan titles]

		public bool tUpdateDanTitle(string title, bool isGold, int clearStatus) {
			bool changed = false;

			bool iG = isGold;
			int cs = clearStatus;

			if (this.data.DanTitles == null)
				this.data.DanTitles = new Dictionary<string, CDanTitle>();

			if (this.data.DanTitles.ContainsKey(title)) {
				if (this.data.DanTitles[title].clearStatus > cs)
					cs = this.data.DanTitles[title].clearStatus;
				if (this.data.DanTitles[title].isGold)
					iG = true;
			}

			// Automatically set the dan to nameplate if new
			// Add a function within the NamePlate.cs file to update the title texture 

			if (!this.data.DanTitles.ContainsKey(title) || cs != clearStatus || iG != isGold) {
				DBSaves.RegisterDanTitle(data.SaveId, title, clearStatus, isGold);
				changed = true;
				/*
                TJAPlayer3.NamePlateConfig.data.Dan[player] = title;
                TJAPlayer3.NamePlateConfig.data.DanGold[player] = iG;
                TJAPlayer3.NamePlateConfig.data.DanType[player] = cs;
                */
			}


			CDanTitle danTitle = new CDanTitle(iG, cs);

			this.data.DanTitles[title] = danTitle;

			//tSaveFile();

			return changed;
		}

		#endregion

		#region [Auxilliary classes]

		public class CDanTitle {
			public CDanTitle(bool iG, int cs) {
				isGold = iG;
				clearStatus = cs;
			}

			public CDanTitle() {
				isGold = false;
				clearStatus = 0;
			}

			[JsonProperty("isGold")]
			public bool isGold;

			[JsonProperty("clearStatus")]
			public int clearStatus;
		}

		public class CNamePlateTitle {
			public CNamePlateTitle(int type) {
				iType = type;
				cld = new CLocalizationData();
			}

			[JsonProperty("iType")]
			public int iType;

			[JsonProperty("Localization")]
			public CLocalizationData cld;
		}

		public class CPassStatus {
			public CPassStatus() {
				d = new int[5] { -1, -1, -1, -1, -1 };
			}

			public int[] d;
		}

		#endregion

		#region [Heya]

		public void tReindexCharacter(string[] characterNamesList) {
			string character = this.data.CharacterName;

			if (characterNamesList.Contains(character))
				this.data.Character = characterNamesList.ToList().IndexOf(character);

		}

		public void tUpdateCharacterName(string newChara) {
			this.data.CharacterName = newChara;
		}

		public void tApplyHeyaChanges() {
			DBSaves.ApplyChangesFromMyRoom(this);
			//this.tSaveFile();
		}

		#endregion

		public class Data {
			[JsonProperty("saveId")]
			public Int64 SaveId = 0;

			[JsonProperty("name")]
			public string Name = "プレイヤー1";

			[JsonProperty("title")]
			public string Title = "初心者";

			[JsonProperty("dan")]
			public string Dan = "新人";

			[JsonProperty("danGold")]
			public bool DanGold = false;

			[JsonProperty("danType")]
			public int DanType = 0;

			[JsonProperty("titleType")]
			public int TitleType = 0;

			[JsonProperty("puchiChara")]
			public string PuchiChara = "0";

			[JsonProperty("medals")]
			public Int64 Medals = 0;

			[JsonIgnore]
			public Int64 TotalEarnedMedals = 0;

			[JsonIgnore]
			public int TotalPlaycount = 0;

			[JsonIgnore]
			public int AIBattleModePlaycount = 0;

			[JsonIgnore]
			public int AIBattleModeWins = 0;

			[JsonProperty("character")]
			public int Character = 0;

			[JsonProperty("characterName")]
			public string CharacterName = "0";

			[JsonProperty("danTitles")]
			public Dictionary<string, CDanTitle> DanTitles = new Dictionary<string, CDanTitle>();

			// Deprecated
			[JsonProperty("namePlateTitles")]
			public Dictionary<string, CNamePlateTitle> NamePlateTitles = new Dictionary<string, CNamePlateTitle>();

			[JsonProperty("unlockedCharacters")]
			public List<string> UnlockedCharacters = new List<string>();

			[JsonProperty("unlockedPuchicharas")]
			public List<string> UnlockedPuchicharas = new List<string>();

			[JsonIgnore]
			public List<string> UnlockedSongs = new List<string>();

			[JsonIgnore]
			public List<int> UnlockedNameplateIds = new List<int>();

			[JsonProperty("activeTriggers")]
			public HashSet<string> ActiveTriggers = new HashSet<string>();

			[JsonIgnore]
			public Dictionary<string, BestPlayRecords.CBestPlayRecord> bestPlays = new Dictionary<string, BestPlayRecords.CBestPlayRecord>();

			[JsonIgnore]
			public Dictionary<string, BestPlayRecords.CBestPlayRecord> bestPlaysDistinctCharts = new Dictionary<string, BestPlayRecords.CBestPlayRecord>();

			[JsonIgnore]
			public Dictionary<string, BestPlayRecords.CBestPlayRecord> bestPlaysDistinctSongs = new Dictionary<string, BestPlayRecords.CBestPlayRecord>();

			[JsonIgnore]
			public Dictionary<string, BestPlayRecords.CBestPlayRecord> bestPlaysSongSelectTables = new Dictionary<string, BestPlayRecords.CBestPlayRecord>();

			[JsonIgnore]
			public Dictionary<string, BestPlayRecords.CSongSelectTableEntry> songSelectTableEntries = new Dictionary<string, BestPlayRecords.CSongSelectTableEntry>();

			[JsonIgnore]
			public BestPlayRecords.CBestPlayStats bestPlaysStats = new BestPlayRecords.CBestPlayStats();

			public BestPlayRecords.CSongSelectTableEntry tGetSongSelectTableEntry(string uniqueId) {
				if (songSelectTableEntries.ContainsKey(uniqueId)) return songSelectTableEntries[uniqueId];
				return new BestPlayRecords.CSongSelectTableEntry();
			}

			#region [Factorize best plays]

			public void tFactorizeBestPlays() {
				bestPlaysDistinctCharts = new Dictionary<string, BestPlayRecords.CBestPlayRecord>();

				foreach (BestPlayRecords.CBestPlayRecord bestPlay in bestPlays.Values) {
					string key = bestPlay.ChartUniqueId + bestPlay.ChartDifficulty.ToString();
					if (!bestPlaysDistinctCharts.ContainsKey(key)) {
						bestPlaysDistinctCharts[key] = bestPlay.Copy();
					} else {
						if (bestPlay.HighScore > bestPlaysDistinctCharts[key].HighScore) {
							bestPlaysDistinctCharts[key].HighScore = bestPlay.HighScore;
							bestPlaysDistinctCharts[key].HighScoreGoodCount = bestPlay.HighScoreGoodCount;
							bestPlaysDistinctCharts[key].HighScoreOkCount = bestPlay.HighScoreOkCount;
							bestPlaysDistinctCharts[key].HighScoreBadCount = bestPlay.HighScoreBadCount;
							bestPlaysDistinctCharts[key].HighScoreRollCount = bestPlay.HighScoreRollCount;
							bestPlaysDistinctCharts[key].HighScoreBoomCount = bestPlay.HighScoreBoomCount;
							bestPlaysDistinctCharts[key].HighScoreMaxCombo = bestPlay.HighScoreMaxCombo;
							bestPlaysDistinctCharts[key].HighScoreADLibCount = bestPlay.HighScoreADLibCount;
						}
						bestPlaysDistinctCharts[key].ScoreRank = Math.Max(bestPlaysDistinctCharts[key].ScoreRank, bestPlay.ScoreRank);
						bestPlaysDistinctCharts[key].ClearStatus = Math.Max(bestPlaysDistinctCharts[key].ClearStatus, bestPlay.ClearStatus);
					}
				}

				bestPlaysDistinctSongs = new Dictionary<string, BestPlayRecords.CBestPlayRecord>();
				songSelectTableEntries = new Dictionary<string, BestPlayRecords.CSongSelectTableEntry>();

				foreach (BestPlayRecords.CBestPlayRecord bestPlay in bestPlaysDistinctCharts.Values) {
					string key = bestPlay.ChartUniqueId;
					if (!bestPlaysDistinctSongs.ContainsKey(key)) {
						bestPlaysDistinctSongs[key] = bestPlay.Copy();
					} else {
						if (bestPlay.HighScore > bestPlaysDistinctSongs[key].HighScore) {
							bestPlaysDistinctSongs[key].HighScore = bestPlay.HighScore;
							bestPlaysDistinctSongs[key].HighScoreGoodCount = bestPlay.HighScoreGoodCount;
							bestPlaysDistinctSongs[key].HighScoreOkCount = bestPlay.HighScoreOkCount;
							bestPlaysDistinctSongs[key].HighScoreBadCount = bestPlay.HighScoreBadCount;
							bestPlaysDistinctSongs[key].HighScoreRollCount = bestPlay.HighScoreRollCount;
							bestPlaysDistinctSongs[key].HighScoreBoomCount = bestPlay.HighScoreBoomCount;
							bestPlaysDistinctSongs[key].HighScoreMaxCombo = bestPlay.HighScoreMaxCombo;
							bestPlaysDistinctSongs[key].HighScoreADLibCount = bestPlay.HighScoreADLibCount;
						}
						bestPlaysDistinctSongs[key].ScoreRank = Math.Max(bestPlaysDistinctSongs[key].ScoreRank, bestPlay.ScoreRank);
						bestPlaysDistinctSongs[key].ClearStatus = Math.Max(bestPlaysDistinctSongs[key].ClearStatus, bestPlay.ClearStatus);
					}

					// Entries to replace score.GPInfo on the song select menus
					if (!songSelectTableEntries.ContainsKey(key)) {
						songSelectTableEntries[key] = new BestPlayRecords.CSongSelectTableEntry();
					}
					if (bestPlay.ChartDifficulty > songSelectTableEntries[key].ScoreRankDifficulty && bestPlay.ScoreRank >= 0) {
						songSelectTableEntries[key].ScoreRankDifficulty = (int)bestPlay.ChartDifficulty;
						songSelectTableEntries[key].ScoreRank = (int)bestPlay.ScoreRank;
					}
					if (bestPlay.ChartDifficulty > songSelectTableEntries[key].ClearStatusDifficulty && bestPlay.ClearStatus >= 0) {
						songSelectTableEntries[key].ClearStatusDifficulty = (int)bestPlay.ChartDifficulty;
						songSelectTableEntries[key].ClearStatus = (int)bestPlay.ClearStatus;
					}
					if ((int)bestPlay.ChartDifficulty == (int)Difficulty.Tower) songSelectTableEntries[key].TowerReachedFloor = (int)bestPlay.TowerBestFloor;
					songSelectTableEntries[key].HighScore[(int)bestPlay.ChartDifficulty] = (int)bestPlay.HighScore;
					songSelectTableEntries[key].ScoreRanks[(int)bestPlay.ChartDifficulty] = (int)bestPlay.ScoreRank + 1; // 0 start
					songSelectTableEntries[key].ClearStatuses[(int)bestPlay.ChartDifficulty] = (int)bestPlay.ClearStatus + 1; // 0 start
				}

				bestPlaysStats = BestPlayRecords.tGenerateBestPlayStats(bestPlaysDistinctCharts.Values, bestPlaysDistinctSongs.Values);
			}

			#endregion
		}

		public Data data = new Data();
		public string path = "Save.json";
		public string name = "Save";

		#region [private]

		private void tSaveFile() {
			ConfigManager.SaveConfig(data, path);
		}

		private void tLoadFile() {
			data = ConfigManager.GetConfig<Data>(path);
		}

		#endregion
	}
}
