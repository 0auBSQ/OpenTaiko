namespace OpenTaiko {
	internal class CUnlockIG : CUnlockCondition {


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
