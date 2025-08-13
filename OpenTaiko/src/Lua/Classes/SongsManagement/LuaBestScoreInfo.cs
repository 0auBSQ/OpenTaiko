namespace OpenTaiko {
	internal class LuaBestScoreInfo {
		BestPlayRecords.CSongSelectTableEntry _scoreInfo;
		int _diff;

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

		#endregion

		public LuaBestScoreInfo(LuaSongChart songChart, int save) {
			_diff = (int)songChart.Difficulty;
			var _from = songChart?.Parent?.UniqueId ?? null;
			if (_from == null) _scoreInfo = new BestPlayRecords.CSongSelectTableEntry();
			else if (save < 0 || save >= OpenTaiko.MAX_PLAYERS) _scoreInfo = new BestPlayRecords.CSongSelectTableEntry();
			else _scoreInfo = OpenTaiko.SaveFileInstances[save].data.tGetSongSelectTableEntry(_from);
		}
	}
}
