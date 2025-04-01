namespace OpenTaiko {
	internal class CUnlockAP : CUnlockCondition {
		/*
		 * "AI Battle plays" Unlock condition object
		 * Validates if the total AI Battle playcount requirement is satisfied
		 */


		public CUnlockAP(CUnlockConditionFactory.UnlockConditionJsonRaw rawJson) : base(rawJson) {
			this.RequiredArgCount = 1;
			this.ConditionId = "ap";
		}

		public override (bool, string?) tConditionMet(int player, EScreen screen = EScreen.MyRoom) {
			if (this.Values.Length == this.RequiredArgCount) {
				int _satisfactoryPlays = (int)OpenTaiko.SaveFileInstances[player].data.AIBattleModePlaycount;

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

			return CLangManager.LangInstance.GetString("UNLOCK_CONDITION_AIPLAY", this.Values[0], SaveData.AIBattleModePlaycount);
		}

		protected override int tGetCountChartsPassingCondition(int player) {
			// Unused for this condition
			return -1;
		}
	}
}
