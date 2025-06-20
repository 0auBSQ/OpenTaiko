using static OpenTaiko.BestPlayRecords;

namespace OpenTaiko {
	internal class CUnlockSD : CUnlockCondition {


		public CUnlockSD(CUnlockConditionFactory.UnlockConditionJsonRaw rawJson) : base(rawJson) {
			this.RequiredArgCount = 2;
			this.ConditionId = "sd";
		}

		public override (bool, string?) tConditionMet(int player, EScreen screen = EScreen.MyRoom) {
			if (this.Values.Length == this.RequiredArgCount) {
				int _satisfactoryPlays = this.tGetCountChartsPassingCondition(player);

				bool fulfiled = this.tValueRequirementMet(_satisfactoryPlays, this.Values[0]);

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
			var _aimedStatus = this.Values[1];

			if (_aimedStatus < (int)EClearStatus.NONE || _aimedStatus >= (int)EClearStatus.TOTAL) return (CLangManager.LangInstance.GetString("UNLOCK_CONDITION_INVALID"));

			int[] _values = {
							SaveData.bestPlaysStats.DistinctPlays,
							SaveData.bestPlaysStats.DistinctClears,
							SaveData.bestPlaysStats.DistinctClears,
							SaveData.bestPlaysStats.DistinctFCs,
							SaveData.bestPlaysStats.DistinctPerfects
						};

			var _count = _values[_aimedStatus];
			var statusString = GetRequiredClearStatus(_aimedStatus);

			return CLangManager.LangInstance.GetString("UNLOCK_CONDITION_PLAYDISTINCT", statusString, this.Values[0], _count);
		}

		protected override int tGetCountChartsPassingCondition(int player) {
			var bpDistinctCharts = OpenTaiko.SaveFileInstances[player].data.bestPlaysDistinctCharts;
			var chartStats = OpenTaiko.SaveFileInstances[player].data.bestPlaysStats;

			var _aimedStatus = this.Values[1];

			int[] _values = {
					chartStats.DistinctPlays,
					chartStats.DistinctClears,
					chartStats.DistinctClears,
					chartStats.DistinctFCs,
					chartStats.DistinctPerfects
				};

			if (_aimedStatus < (int)EClearStatus.NONE || _aimedStatus >= (int)EClearStatus.TOTAL) return 0;

			return (_values[_aimedStatus]);
		}
	}
}
