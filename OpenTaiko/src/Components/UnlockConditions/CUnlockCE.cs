namespace OpenTaiko {
	internal class CUnlockCE : CUnlockCondition {
		/*
		 * "Coins earned" Unlock condition object
		 * Validates if the total earned coin count requirement is satisfied
		 */


		public CUnlockCE(CUnlockConditionFactory.UnlockConditionJsonRaw rawJson) : base(rawJson) {
			this.RequiredArgCount = 1;
			this.ConditionId = "ce";
		}

		public override (bool, string?) tConditionMet(int player, EScreen screen = EScreen.MyRoom) {
			if (this.Values.Length == this.RequiredArgCount) {
				int _totalEarned = (int)OpenTaiko.SaveFileInstances[player].data.TotalEarnedMedals;

				bool fulfiled = this.tValueRequirementMet(_totalEarned, this.Values[0]);

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

			return CLangManager.LangInstance.GetString("UNLOCK_CONDITION_EARN", this.Values[0], SaveData.TotalEarnedMedals);
		}

		protected override int tGetCountChartsPassingCondition(int player) {
			// Unused for this condition
			return -1;
		}
	}
}
