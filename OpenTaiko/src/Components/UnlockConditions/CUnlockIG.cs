namespace OpenTaiko {
	internal class CUnlockIG : CUnlockCondition {
		/*
		 * "Impossible to get" Unlock condition object
		 * For assets that are meant to be obtained only through database manipulation, should always be of rarity Mythical
		 */


		public CUnlockIG(CUnlockConditionFactory.UnlockConditionJsonRaw rawJson) : base(rawJson) {
			this.RequiredArgCount = 0;
			this.ConditionId = "ig";
		}

		public override (bool, string?) tConditionMet(int player, EScreen screen = EScreen.MyRoom) {
			return (false, "");
		}

		public override string tConditionMessage(EScreen screen = EScreen.MyRoom) {
			return (CLangManager.LangInstance.GetString("UNLOCK_CONDITION_INVALID"));
		}

		protected override int tGetCountChartsPassingCondition(int player) {
			// Unused for this condition
			return -1;
		}
	}
}
