namespace OpenTaiko {
	internal class LuaSaveFile {
		private SaveFile _sf;
		private int _mounted;

		#region [Player Metadata]

		public string Name {
			get {
				return _sf.data.Name;
			}
		}

		public Int64 SaveId {
			get {
				return _sf.data.SaveId;
			}
		}

		public string SaveUID {
			get {
				return _sf.data.SaveUID;
			}
		}

		public LuaNameplateInfo NameplateInfo {
			get {
				int _npId = _sf.data.TitleId;
				var _dbNp = OpenTaiko.Databases.DBNameplateUnlockables.data;
				if (_dbNp.ContainsKey(_npId)) {
					var _entry = _dbNp[_npId];
					return new LuaNameplateInfo(_entry, _npId);
				}
				return new LuaNameplateInfo();
			}
		}

		public LuaDanplateInfo DanplateInfo {
			get {
				return new LuaDanplateInfo(_sf);
			}
		}

		#endregion

		#region [General Data]

		public int TotalPlaycount {
			get {
				return _sf.data.TotalPlaycount;
			}
		}

		public int AIBattlePlaycount {
			get {
				return _sf.data.AIBattleModePlaycount;
			}
		}

		public int AIBattleWins {
			get {
				return _sf.data.AIBattleModeWins;
			}
		}

		#endregion

		#region [Coins]

		public long Coins {
			get {
				return _sf.data.Medals;
			}
		}

		public long TotalEarnedCoins {
			get {
				return _sf.data.TotalEarnedMedals;
			}
		}

		public void SpendCoins(long price) {
			_sf.data.Medals = Math.Max(0, _sf.data.Medals - price);
			DBSaves.AlterCoinsAndTotalPlayCount(_sf.data.SaveId, -price, 0);
		}

		public void EarnCoins(long amount) {
			_sf.data.Medals += amount;
			_sf.data.TotalEarnedMedals += amount;
			DBSaves.AlterCoinsAndTotalPlayCount(_sf.data.SaveId, amount, 0);
		}

		#endregion

		#region [Unlockables]

		public bool IsNameplateUnlocked(int id) {
			return _sf.data.UnlockedNameplateIds.Contains(id);
		}

		public void UnlockNameplate(int id) {
			if (!IsNameplateUnlocked(id)) {
				_sf.data.UnlockedNameplateIds.Add(id);
				DBSaves.RegisterUnlockedNameplate(_sf.data.SaveId, id);
			}
		}

		public bool IsSongUnlocked(string uniqueId) {
			return _sf.data.UnlockedSongs.Contains(uniqueId);
		}

		public void UnlockSong(string uniqueId) {
			if (!IsSongUnlocked(uniqueId)) {
				_sf.data.UnlockedSongs.Add(uniqueId);
				DBSaves.RegisterStringUnlockedAsset(_sf.data.SaveId, "unlocked_songs", uniqueId);
			}
		}

		#endregion

		#region [Hitsounds]

		/// <summary>The folder name of this player's selected hitsound set (e.g. "Taiko").</summary>
		public string SelectedHitsounds {
			get => _sf.data.SelectedHitsounds;
			set {
				if (_sf.data.SelectedHitsounds == value) return;
				_sf.data.SelectedHitsounds = value;
				DBSaves.SetSelectedHitsounds(_sf.data.SaveId, value);

				// Apply immediately if the hitsounds are loaded
				var hs = OpenTaiko.Skin.hsHitSoundsInformations;
				if (hs != null) {
					int idx = hs.GetIndexByFolderName(value);
					hs.tReloadHitSounds(idx, _mounted);
				}
			}
		}

		#endregion

		#region [Triggers and Counters]

		public bool GetGlobalTrigger(string triggerName) {
			return _sf.tGetGlobalTrigger(triggerName);
		}

		public double GetGlobalCounter(string counterName) {
			return _sf.tGetGlobalCounter(counterName);
		}

		public void SetGlobalTrigger(string triggerName, bool triggerValue) {
			_sf.tSetGlobalTrigger(triggerName, triggerValue);
		}

		public void SetGlobalCounter(string counterName, double counterValue) {
			_sf.tSetGlobalCounter(counterName, counterValue);
		}

		/// <summary>
		/// Returns the number of charts cleared at exactly <paramref name="clearStatus"/>
		/// for <paramref name="difficulty"/> (0=Easy…4=Edit).
		/// clearStatus: 0=None, 1=Assisted, 2=Clear, 3=FC, 4=Perfect.
		/// </summary>
		public int GetClearStatusCount(int difficulty, int clearStatus) {
			if (difficulty < 0 || difficulty >= (int)Difficulty.Total) return 0;
			var table = _sf.data.bestPlaysStats?.ClearStatuses?[difficulty];
			if (table == null || clearStatus < 0 || clearStatus >= table.Length) return 0;
			return table[clearStatus];
		}

		/// <summary>Returns the Dan best play record for <paramref name="node"/> for default (no-mod) plays.</summary>
		public LuaDanBestPlay GetDanBestPlay(LuaSongNode? node) {
			string? uid = node?.UniqueId;
			if (uid == null) return new LuaDanBestPlay();
			string key = uid + ((int)Difficulty.Dan).ToString() + "8925478921";
			return _sf.data.bestPlays.TryGetValue(key, out var record)
				? new LuaDanBestPlay(record)
				: new LuaDanBestPlay();
		}

		#endregion

		#region [Characters and Puchis]

		public LuaCharacter GetCharacter() {
			return OpenTaiko.Tx.PlayerCharacters[_mounted];
		}

		public string CharacterName {
			get {
				int idx = Math.Max(0, Math.Min(_sf.data.Character, OpenTaiko.Tx.Characters.Length - 1));
				return OpenTaiko.Tx.Characters[idx].dirName;
			}
		}

		/// <summary>
		/// Changes the player's character to the one with the given directory name.
		/// Returns false without making any change if the character name is not found.
		/// Returns true immediately (no-op) if the character is already set.
		/// </summary>
		public bool ChangeCharacter(string name) {
			int newIdx = Array.FindIndex(OpenTaiko.Tx.Characters, c => c.dirName == name);
			if (newIdx < 0) return false;
			int oldIdx = _sf.data.Character;
			if (oldIdx == newIdx) return true;
			OpenTaiko.Tx.ReloadCharacter(oldIdx, newIdx, _mounted);
			_sf.data.Character = newIdx;
			_sf.tUpdateCharacterName(OpenTaiko.Tx.Characters[newIdx].dirName);
			_sf.tApplyHeyaChanges();
			return true;
		}

		public LuaPuchichara? GetPuchichara() =>
			OpenTaiko.Tx?.LuaPuchicharaDb?.GetPlayerPuchichara(_mounted);

		public bool IsPuchicharaUnlocked(string folderName) =>
			_sf.data.UnlockedPuchicharas.Contains(folderName);

		public void UnlockPuchichara(string folderName) {
			if (!IsPuchicharaUnlocked(folderName)) {
				_sf.data.UnlockedPuchicharas.Add(folderName);
				DBSaves.RegisterStringUnlockedAsset(_sf.data.SaveId, "unlocked_puchicharas", folderName);
				_sf.tApplyHeyaChanges();
			}
		}

		public void ChangePuchichara(string folderName) {
			if (_sf.data.PuchiChara == folderName) return;
			_sf.data.PuchiChara = folderName;
			_sf.tApplyHeyaChanges();
		}

		public bool IsCharacterUnlocked(string dirName) {
			if (_sf.data.UnlockedCharacters.Contains(dirName)) return true;
			// The currently equipped character is always accessible even if not in the unlocked list.
			var chars = OpenTaiko.Tx?.Characters;
			if (chars != null) {
				int idx = _sf.data.Character;
				if (idx >= 0 && idx < chars.Length && chars[idx]?.dirName == dirName) return true;
			}
			return false;
		}

		public void UnlockCharacter(string dirName) {
			if (!IsCharacterUnlocked(dirName)) {
				_sf.data.UnlockedCharacters.Add(dirName);
				DBSaves.RegisterStringUnlockedAsset(_sf.data.SaveId, "unlocked_characters", dirName);
				_sf.tApplyHeyaChanges();
			}
		}

		// ── Dan title ──────────────────────────────────────────────────────────────

		/// <summary>Number of available dan titles (always ≥ 1 for the default "新人").</summary>
		public int DanTitleCount => 1 + (_sf.data.DanTitles?.Count ?? 0);

		/// <summary>Returns the dan-title entry at the given 0-based index, or <c>null</c> if out of range.
		/// Index 0 is always the default "新人" entry.</summary>
		public LuaDanTitleEntry? GetDanTitleByIndex(int index) {
			if (index == 0) return new LuaDanTitleEntry("新人", false, 0);
			var titles = _sf.data.DanTitles;
			if (titles == null) return null;
			int i = 1;
			foreach (var (k, v) in titles) {
				if (i == index) return new LuaDanTitleEntry(k, v.isGold, v.clearStatus);
				i++;
			}
			return null;
		}

		/// <summary>The currently active dan-title string.</summary>
		public string SelectedDan => _sf.data.Dan;

		/// <summary>Sets the player's active dan title and persists the change.</summary>
		public void ChangeDan(string title) {
			bool isGold = false;
			int  cs     = 0;
			if (_sf.data.DanTitles != null && _sf.data.DanTitles.TryGetValue(title, out var dt)) {
				isGold = dt.isGold;
				cs     = dt.clearStatus;
			}
			_sf.data.Dan     = title;
			_sf.data.DanGold = isGold;
			_sf.data.DanType = cs;
			OpenTaiko.NamePlate?.tNamePlateRefreshTitles(_mounted);
			_sf.tApplyHeyaChanges();
		}

		// ── Player name ────────────────────────────────────────────────────────────

		/// <summary>Changes the player's displayed name and persists the change.</summary>
		public void ChangeName(string name) {
			if (string.IsNullOrEmpty(name) || _sf.data.Name == name) return;
			_sf.data.Name = name;
			OpenTaiko.NamePlate?.tNamePlateRefreshTitles(_mounted);
			_sf.tApplyHeyaChanges();
		}

		public void ChangeNameplate(int id) {
			if (_sf.data.TitleId == id) return;
			_sf.data.TitleId = id;
			if (OpenTaiko.Databases.DBNameplateUnlockables.data.TryGetValue((Int64)id, out var nameplate)) {
				_sf.data.Title = nameplate.nameplateInfo.cld.GetString("");
				_sf.data.TitleType = nameplate.nameplateInfo.iType;
				_sf.data.TitleRarityInt = HRarity.tRarityToLangInt(nameplate.rarity);
			} else {
				_sf.data.Title = "";
				_sf.data.TitleType = 0;
				_sf.data.TitleRarityInt = 1;
			}
			OpenTaiko.NamePlate?.tNamePlateRefreshTitles(_mounted);
			_sf.tApplyHeyaChanges();
		}

		#endregion

		public LuaSaveFile(SaveFile sf, int mountedPlayer) {
			_sf = sf;
			_mounted = mountedPlayer;
		}
	}
}
