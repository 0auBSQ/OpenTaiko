using Newtonsoft.Json;

namespace OpenTaiko;

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
		lock (this) {
		// Best plays
		data.bestPlays = DBSaves.GetBestPlaysAsDict(data.SaveId);
		data.tFactorizeBestPlays();

		// Global triggers
		data.ActiveTriggers = DBSaves.GetActiveTriggersSet(data.SaveId);

		// Global counters
		data.GlobalCounters = DBSaves.GetGlobalCountersDict(data.SaveId);

		// Global strings
		data.GlobalStrings = DBSaves.GetGlobalStringsDict(data.SaveId);
	}
	}

	public void tLoadUnlockables() {
		data.UnlockedCharacters = DBSaves.FetchStringUnlockedAsset(data.SaveId, "unlocked_characters");
		data.UnlockedPuchicharas = DBSaves.FetchStringUnlockedAsset(data.SaveId, "unlocked_puchicharas");
		data.UnlockedSongs = DBSaves.FetchStringUnlockedAsset(data.SaveId, "unlocked_songs");
		data.UnlockedNameplateIds = DBSaves.FetchUnlockedNameplateIds(data.SaveId);
		data.DanTitles = DBSaves.FetchUnlockedDanTitles(data.SaveId);
	}

	#region [Triggers and Counters]

	public void tSetGlobalTrigger(string triggerName, bool triggerValue) {
		if (triggerValue) data.ActiveTriggers.Add(triggerName);
		else data.ActiveTriggers.Remove(triggerName);

		DBSaves.DBToggleGlobalTrigger(data.SaveId, triggerName, triggerValue);
	}

	public void tSetGlobalCounter(string counterName, double counterValue) {
		if (data.GlobalCounters.ContainsKey(counterName)) data.GlobalCounters[counterName] = counterValue;
		else data.GlobalCounters.Add(counterName, counterValue);

		DBSaves.DBSetGlobalCounter(data.SaveId, counterName, counterValue);
	}

	public bool tGetGlobalTrigger(string triggerName) {
		return data.ActiveTriggers.Contains(triggerName);
	}

	public double tGetGlobalCounter(string counterName) {
		data.GlobalCounters.TryGetValue(counterName, out double counterValue);
		return counterValue;
	}

	public void tSetGlobalString(string stringName, string stringValue) {
		data.GlobalStrings[stringName] = stringValue;
		DBSaves.DBSetGlobalString(data.SaveId, stringName, stringValue);
	}

	// "" when the save has no value of that name
	public string tGetGlobalString(string stringName) {
		return data.GlobalStrings.TryGetValue(stringName, out string? stringValue) ? stringValue : "";
	}

	#endregion


	#region [Medals and PlayCount]

	public void tEarnCoins(int amount) {
		data.Medals += amount;
		data.TotalEarnedMedals += amount;

		// Small trick here, each actual play (excluding Auto, AI, etc) are worth at least 5 coins for the player, whatever which mode it is (Dan, Tower, Taiko mode, etc)
		// Earn Coins is also called once per play, so we just add 1 here to the total playcount
		data.TotalPlaycount += 1;
		DBSaves.AlterCoinsAndTotalPlayCount(data.SaveId, amount, 1);
	}

	public void tRegisterAIBattleModePlay(bool IsWon) {
		data.AIBattleModePlaycount++;
		if (IsWon) data.AIBattleModeWins++;
		DBSaves.RegisterAIBattleModePlay(data.SaveId, IsWon);
	}

	#endregion

	#region [Dan titles]

	// returns true when the grade is new, or better than the one saved (gold, or a better clear)
	public bool tUpdateDanTitle(string title, bool isGold, int clearStatus) {
		if (this.data.DanTitles == null)
			this.data.DanTitles = new Dictionary<string, CDanTitle>();

		bool known = this.data.DanTitles.TryGetValue(title, out var old);
		bool iG = isGold || (known && old!.isGold);
		int cs = known ? Math.Max(old!.clearStatus, clearStatus) : clearStatus;
		bool changed = !known || iG != old!.isGold || cs != old.clearStatus;

		if (changed)
			DBSaves.RegisterDanTitle(data.SaveId, title, clearStatus, isGold);
		this.data.DanTitles[title] = new CDanTitle(iG, cs);
		return changed;
	}

	// shows the dan grade saved under title on the player's nameplate, as the nameplate menu does
	public void tSetNameplateDan(string title) {
		if (this.data.DanTitles == null || !this.data.DanTitles.TryGetValue(title, out var grade)) return;
		this.data.Dan = title;
		this.data.DanGold = grade.isGold;
		this.data.DanType = grade.clearStatus;
		this.tApplyHeyaChanges();
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

		if (characterNamesList.Contains(character)) {
			this.data.Character = characterNamesList.ToList().IndexOf(character);
		} else {
			// Character folder no longer exists on disk — fall back to index 0 (None).
			this.data.Character = 0;
		}
	}

	public void tUpdateCharacterName(string newChara) {
		this.data.CharacterName = newChara;
	}

	public void tApplyHeyaChanges() {
		DBSaves.ApplyChangesFromMyRoom(this);
	}

	#endregion

	public class Data {
		[JsonProperty("saveId")]
		public Int64 SaveId = 0;

		[JsonProperty("saveUID")]
		public string SaveUID = "";   // uuid4, unique per save; keys per-save data such as the My Room save

		[JsonProperty("name")]
		public string Name = "プレイヤー1";

		// Title, TitleType and TitleRarityInt are filled from the nameplate catalogue by TitleId (DBNameplateUnlockables.tResolveNameplate)
		[JsonProperty("title")]
		public string Title = "";

		[JsonProperty("dan")]
		public string Dan = "新人";

		[JsonProperty("danGold")]
		public bool DanGold = false;

		[JsonProperty("danType")]
		public int DanType = 0;

		[JsonProperty("titleType")]
		public int TitleType = 0;

		[JsonIgnore]
		public int TitleRarityInt = 1;

		// the equipped nameplate's id, "" = none
		[JsonIgnore]
		public string TitleId = "";

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

		/// <summary>Folder name of the selected hitsound set (e.g. "Taiko").
		/// Stored per save-file so every player can have their own hitsounds.</summary>
		[JsonIgnore]
		public string SelectedHitsounds = "Taiko";

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
		public HashSet<string> UnlockedNameplateIds = new HashSet<string>();

		// Global Triggers
		[JsonProperty("activeTriggers")]
		public HashSet<string> ActiveTriggers = new HashSet<string>();

		[JsonIgnore]
		public Dictionary<string, double> GlobalCounters = new Dictionary<string, double>();

		[JsonIgnore]
		public Dictionary<string, string> GlobalStrings = new Dictionary<string, string>();

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

		// Never null once the _boot stage is active
		[JsonIgnore]
		public CLuaCharacterScript? mountedCharacter = null;

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
				songSelectTableEntries[key].PlayedDifficulties[(int)bestPlay.ChartDifficulty] = true;
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
