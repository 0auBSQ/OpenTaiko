namespace OpenTaiko {
	internal class CUnlockError : CUnlockCondition {
		/*
		 * Error unlock condition object
		 * Used when Unlock condition fetching failed in order to have a non-null object displaying an error message in-game
		 */

		public CUnlockError(CUnlockConditionFactory.UnlockConditionJsonRaw? rawJson) : base(rawJson) {
			this.RequiredArgCount = 0;
			this.ConditionId = "error";
		}

		public override (bool, string?) tConditionMet(int player, EScreen screen = EScreen.MyRoom) {
			return (false, "");
		}

		public override string tConditionMessage(EScreen screen = EScreen.MyRoom) {
			return "Unlock condition fetching failed";
		}

		protected override int tGetCountChartsPassingCondition(int player) {
			// Unused for this condition
			return -1;
		}
	}
}
