using static OpenTaiko.BestPlayRecords;

namespace OpenTaiko {
	internal class CUnlockLP : CUnlockCondition {


		public CUnlockLP(CUnlockConditionFactory.UnlockConditionJsonRaw rawJson) : base(rawJson) {
			this.RequiredArgCount = 3;
			this.ConditionId = "lp";
		}

		public override (bool, string?) tConditionMet(int player, EScreen screen = EScreen.MyRoom) {
			if (this.Values.Length == this.RequiredArgCount) {
				int _satisfactoryPlays = this.tGetCountChartsPassingCondition(player);

				bool fulfiled = this.tValueRequirementMet(_satisfactoryPlays, this.Values[2]);

				if (screen == EScreen.Internal) {
					return (fulfiled, "");
				} else {
					return (fulfiled, null);
				}
			} else
				return (false, CLangManager.LangInstance.GetString("UNLOCK_CONDITION_ERROR", this.ConditionId, this.RequiredArgCount.ToString()));
		}

		public override string tConditionMessage(EScreen screen = EScreen.MyRoom) {
			if (this.Values.Length < this.RequiredArgCount)
				return CLangManager.LangInstance.GetString("UNLOCK_CONDITION_ERROR", this.ConditionId, this.RequiredArgCount);

			// Only the player loaded as 1P can check unlockables in real time
			var SaveData = OpenTaiko.SaveFileInstances[OpenTaiko.SaveFile].data;
			var ChartStats = SaveData.bestPlaysStats;

			// Check distinct plays
			var _aimedDifficulty = this.Values[0];
			var _aimedStatus = this.Values[1];

			if (_aimedStatus < (int)EClearStatus.NONE || _aimedStatus >= (int)EClearStatus.TOTAL) return (CLangManager.LangInstance.GetString("UNLOCK_CONDITION_INVALID"));

			int _count = 0;
			if (_aimedStatus == (int)EClearStatus.NONE) _count = ChartStats.LevelPlays.TryGetValue(_aimedDifficulty, out var value) ? value : 0;
			else if (_aimedStatus <= (int)EClearStatus.CLEAR) _count = ChartStats.LevelClears.TryGetValue(_aimedDifficulty, out var value) ? value : 0;
			else if (_aimedStatus == (int)EClearStatus.FC) _count = ChartStats.LevelFCs.TryGetValue(_aimedDifficulty, out var value) ? value : 0;
			else _count = ChartStats.LevelPerfects.TryGetValue(_aimedDifficulty, out var value) ? value : 0;

			var statusString = GetRequiredClearStatus(_aimedStatus);
			return CLangManager.LangInstance.GetString("UNLOCK_CONDITION_PLAYLEVEL", statusString, this.Values[2], _aimedDifficulty, _count);
		}

		protected override int tGetCountChartsPassingCondition(int player) {
			var _aimedDifficulty = this.Values[0]; // Difficulty if dp, Level if lp
			var _aimedStatus = this.Values[1];

			// dp and lp only work for regular (Dan and Tower excluded) charts
			if (_aimedStatus < (int)EClearStatus.NONE || _aimedStatus >= (int)EClearStatus.TOTAL) return 0;

			var bpDistinctCharts = OpenTaiko.SaveFileInstances[player].data.bestPlaysDistinctCharts;
			var chartStats = OpenTaiko.SaveFileInstances[player].data.bestPlaysStats;

			if (_aimedStatus == (int)EClearStatus.NONE) return chartStats.LevelPlays.TryGetValue(_aimedDifficulty, out var value) ? value : 0;
			else if (_aimedStatus <= (int)EClearStatus.CLEAR) return chartStats.LevelClears.TryGetValue(_aimedDifficulty, out var value) ? value : 0;
			else if (_aimedStatus == (int)EClearStatus.FC) return chartStats.LevelFCs.TryGetValue(_aimedDifficulty, out var value) ? value : 0;
			else return chartStats.LevelPerfects.TryGetValue(_aimedDifficulty, out var value) ? value : 0;
		}
	}
}
