namespace OpenTaiko {
	internal class LuaBestScoreInfo {
		BestPlayRecords.CSongSelectTableEntry _scoreInfo;
		int _diff;
		int _save;
		string? _uniqueId;
		bool _hasBeenPlayed;

		#region [Best play infos]

		public int ScoreRank {
			get {
				return _scoreInfo.ScoreRanks[_diff];
			}
		}

		public int ClearStatus {
			get {
				return _scoreInfo.ClearStatuses[_diff];
			}
		}

		public int HighScore {
			get {
				return _scoreInfo.HighScore[_diff];
			}
		}

		/// <summary>The highest floor reached on this song's tower chart, 0 when none was played.</summary>
		public int TowerReachedFloor => _scoreInfo.TowerReachedFloor;

		/// <summary>True if this chart has at least one registered play for this player,
		/// regardless of clear status or score rank.</summary>
		public bool HasBeenPlayed => _hasBeenPlayed;

		/// <summary>Total times this player has played this chart at this difficulty, summed across every mod-flag
		/// variant (mods are not distinguished). 0 if the save/song is invalid.</summary>
		public long PlayCount {
			get {
				if (_uniqueId == null || _save < 0 || _save >= OpenTaiko.MAX_PLAYERS) return 0;
				return BestPlayRecords.SumPlayCount(OpenTaiko.SaveFileInstances[_save].data.bestPlays.Values, _uniqueId, _diff);
			}
		}

		#endregion

		public LuaBestScoreInfo(LuaSongChart songChart, int save) {
			_diff = (int)songChart.Difficulty;
			_save = save;
			var _from = songChart?.Parent?.UniqueId ?? null;
			_uniqueId = _from;
			if (_from == null) {
				_scoreInfo = new BestPlayRecords.CSongSelectTableEntry();
				_hasBeenPlayed = false;
			} else if (save < 0 || save >= OpenTaiko.MAX_PLAYERS) {
				_scoreInfo = new BestPlayRecords.CSongSelectTableEntry();
				_hasBeenPlayed = false;
			} else {
				var entries = OpenTaiko.SaveFileInstances[save].data.songSelectTableEntries;
				_scoreInfo = entries.ContainsKey(_from) ? entries[_from] : new BestPlayRecords.CSongSelectTableEntry();
				_hasBeenPlayed = _scoreInfo.PlayedDifficulties[_diff];
			}
		}
	}
}
