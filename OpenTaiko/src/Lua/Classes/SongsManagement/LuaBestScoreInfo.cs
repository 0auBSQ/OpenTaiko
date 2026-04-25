namespace OpenTaiko {
	internal class LuaBestScoreInfo {
		BestPlayRecords.CSongSelectTableEntry _scoreInfo;
		int _diff;
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

		/// <summary>True if this chart has at least one registered play for this player,
		/// regardless of clear status or score rank.</summary>
		public bool HasBeenPlayed => _hasBeenPlayed;

		#endregion

		public LuaBestScoreInfo(LuaSongChart songChart, int save) {
			_diff = (int)songChart.Difficulty;
			var _from = songChart?.Parent?.UniqueId ?? null;
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
