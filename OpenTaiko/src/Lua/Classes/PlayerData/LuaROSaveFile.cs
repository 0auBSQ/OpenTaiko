namespace OpenTaiko {
	/// <summary>
	/// Read-only base of <see cref="LuaSaveFile"/> for use in <see cref="LuaROActivityWrapper"/> scripts.
	/// Any attempt to call a write method logs an error and does nothing.
	/// </summary>
	internal class LuaROSaveFile {
		private static void BlockWrite(string method) {
			LogNotification.PopError($"[ROActivity] 'GetSaveFile(player).{method}' is a write operation and is not allowed in a read-only module.");
		}

		protected SaveFile _sf;
		protected int _mounted;

		public LuaROSaveFile AsReadOnly() => new(this._sf, this._mounted);

		#region [Player Metadata]

		public string Name {
			get {
				return _sf.data.Name;
			}
		}

		public long SaveId {
			get {
				return _sf.data.SaveId;
			}
		}

		public string SaveUID {
			get {
				return _sf.data.SaveUID;
			}
		}

		/// <summary>The equipped nameplate; one with an empty <c>Id</c> and no title when none is equipped or its id is unknown.</summary>
		public LuaNameplateInfo NameplateInfo {
			get {
				string _npId = _sf.data.TitleId ?? "";
				if (OpenTaiko.Databases.DBNameplateUnlockables.data.TryGetValue(_npId, out var _entry))
					return new LuaNameplateInfo(_entry, _npId);
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

		public virtual void SpendCoins(long price) => BlockWrite(nameof(SpendCoins));
		public virtual void EarnCoins(long amount) => BlockWrite(nameof(EarnCoins));

		#endregion

		#region [Unlockables]

		/// <summary>Whether the nameplate is unlocked; the id is a string, or a whole number for the shipped ones.</summary>
		public bool IsNameplateUnlocked(object id) {
			return _sf.data.UnlockedNameplateIds.Contains(DBNameplateUnlockables.tKey(id));
		}

		public virtual void UnlockNameplate(object id) => BlockWrite(nameof(UnlockNameplate));

		public bool IsSongUnlocked(string uniqueId) {
			return _sf.data.UnlockedSongs.Contains(uniqueId);
		}

		public virtual void UnlockSong(string uniqueId) => BlockWrite(nameof(UnlockSong));

		#endregion

		#region [Hitsounds]

		/// <summary>The folder name of this player's selected hitsound set (e.g. "Taiko").</summary>
		public virtual string SelectedHitsounds {
			get => _sf.data.SelectedHitsounds;
			set => BlockWrite(nameof(SelectedHitsounds));
		}

		#endregion

		#region [Triggers and Counters]

		public bool GetGlobalTrigger(string triggerName) {
			return _sf.tGetGlobalTrigger(triggerName);
		}

		public double GetGlobalCounter(string counterName) {
			return _sf.tGetGlobalCounter(counterName);
		}

		/// <summary>The save's text value of that name, "" when it has none (a module's data as JSON, for example).</summary>
		public string GetGlobalString(string stringName) {
			return _sf.tGetGlobalString(stringName);
		}

		public virtual void SetGlobalTrigger(string triggerName, bool triggerValue) => BlockWrite(nameof(SetGlobalTrigger));
		public virtual void SetGlobalCounter(string counterName, double counterValue) => BlockWrite(nameof(SetGlobalCounter));
		public virtual void SetGlobalString(string stringName, string stringValue) => BlockWrite(nameof(SetGlobalString));

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

		public virtual bool ChangeCharacter(string name) { BlockWrite(nameof(ChangeCharacter)); return false; }

		public LuaPuchichara? GetPuchichara() =>
			OpenTaiko.Tx?.LuaPuchicharaDb?.GetPlayerPuchichara(_mounted);

		public bool IsPuchicharaUnlocked(string folderName) =>
			_sf.data.UnlockedPuchicharas.Contains(folderName);

		public virtual void UnlockPuchichara(string folderName) => BlockWrite(nameof(UnlockPuchichara));

		public virtual void ChangePuchichara(string folderName) => BlockWrite(nameof(ChangePuchichara));

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

		public virtual void UnlockCharacter(string dirName) => BlockWrite(nameof(UnlockCharacter));

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

		public virtual void ChangeDan(string title) => BlockWrite(nameof(ChangeDan));

		// ── Player name ────────────────────────────────────────────────────────────

		/// <summary>Changes the player's displayed name and persists the change.</summary>
		public virtual void ChangeName(string name) => BlockWrite(nameof(ChangeName));

		public virtual void ChangeNameplate(object id) => BlockWrite(nameof(ChangeNameplate));

		#endregion

		public LuaROSaveFile(SaveFile sf, int mountedPlayer) {
			_sf = sf;
			_mounted = mountedPlayer;
		}
	}
}
