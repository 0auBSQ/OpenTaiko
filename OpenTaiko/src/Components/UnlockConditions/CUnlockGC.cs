namespace OpenTaiko {
	internal class CUnlockGC : CUnlockCondition {
		/*
		 * "Global Counter" Unlock condition object
		 * Validates if the total AI Battle win count requirement is satisfied
		 */

		public CUnlockGC(CUnlockConditionFactory.UnlockConditionJsonRaw rawJson) : base(rawJson) {
			this.RequiredArgCount = 1;
			this.ConditionId = "gc";
		}

		public override (bool, string?) tConditionMet(int player, EScreen screen = EScreen.MyRoom) {
			if (this.Values.Length == this.RequiredArgCount) {
				double _globalCounter = OpenTaiko.SaveFileInstances[player].tGetGlobalCounter(this.Reference[0]);

				bool fulfiled = this.tValueRequirementMet(_globalCounter, (double)this.Values[0]);

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
			double _cur = OpenTaiko.PrimarySaveFile.tGetGlobalCounter(this.Reference[0]);
			string _type = CLangManager.LangInstance.GetString($"UNLOCK_CONDITION_TYPE_{this.Type}");

			return CLangManager.LangInstance.GetString("UNLOCK_CONDITION_GLOBAL_COUNTER", _type, this.Reference[0], this.Values[0], _cur);
		}

		protected override int tGetCountChartsPassingCondition(int player) {
			// Unused for this condition
			return -1;
		}
	}
}
