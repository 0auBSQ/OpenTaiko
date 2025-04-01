using static OpenTaiko.BestPlayRecords;

namespace OpenTaiko {
	internal class CUnlockDP : CUnlockCondition {


		public CUnlockDP(CUnlockConditionFactory.UnlockConditionJsonRaw rawJson) : base(rawJson) {
			this.RequiredArgCount = 3;
			this.ConditionId = "dp";
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
			if (_aimedDifficulty < (int)Difficulty.Easy || _aimedDifficulty > (int)Difficulty.Edit) return (CLangManager.LangInstance.GetString("UNLOCK_CONDITION_INVALID"));

			var _table = ChartStats.ClearStatuses[_aimedDifficulty];
			var _ura = ChartStats.ClearStatuses[(int)Difficulty.Edit];
			int _count = 0;
			for (int i = _aimedStatus; i < (int)EClearStatus.TOTAL; i++) {
				_count += _table[i];
				if (_aimedDifficulty == (int)Difficulty.Oni) _count += _ura[i];
			}

			var diffString = (_aimedDifficulty == (int)Difficulty.Oni) ? CLangManager.LangInstance.GetString("DIFF_EXEXTRA") : CLangManager.LangInstance.GetDifficulty(_aimedDifficulty);
			var statusString = GetRequiredClearStatus(_aimedStatus);
			return CLangManager.LangInstance.GetString("UNLOCK_CONDITION_PLAYDIFF", statusString, this.Values[2], diffString, _count);
		}

		protected override int tGetCountChartsPassingCondition(int player) {
			var _aimedDifficulty = this.Values[0]; // Difficulty if dp, Level if lp
			var _aimedStatus = this.Values[1];

			// dp and lp only work for regular (Dan and Tower excluded) charts
			if (_aimedStatus < (int)EClearStatus.NONE || _aimedStatus >= (int)EClearStatus.TOTAL) return 0;
			if (_aimedDifficulty < (int)Difficulty.Easy || _aimedDifficulty > (int)Difficulty.Edit) return 0;


			var bpDistinctCharts = OpenTaiko.SaveFileInstances[player].data.bestPlaysDistinctCharts;
			var chartStats = OpenTaiko.SaveFileInstances[player].data.bestPlaysStats;

			var _table = chartStats.ClearStatuses[_aimedDifficulty];
			var _ura = chartStats.ClearStatuses[(int)Difficulty.Edit];
			int _count = 0;
			for (int i = _aimedStatus; i < (int)EClearStatus.TOTAL; i++) {
				_count += _table[i];
				if (_aimedDifficulty == (int)Difficulty.Oni) _count += _ura[i];
			}
			return _count;
		}
	}
}
