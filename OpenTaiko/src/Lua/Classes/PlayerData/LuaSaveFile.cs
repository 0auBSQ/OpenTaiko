namespace OpenTaiko {
	internal class LuaSaveFile {
		private SaveFile _sf;

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

		#endregion

		public LuaSaveFile(SaveFile sf) {
			_sf = sf;
		}
	}
}
