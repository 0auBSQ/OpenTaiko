namespace OpenTaiko {
	internal class CUnlockOrComb : CUnlockCondition {
		/*
		 * "Or combination" Unlock condition object
		 * Validates if at least one of the contained conditions is satisfied
		 */


		public CUnlockOrComb(CUnlockConditionFactory.UnlockConditionJsonRaw rawJson) : base(rawJson) {
			this.RequiredArgCount = 0;
			this.ConditionId = "orcomb";
		}

		public override (bool, string?) tConditionMet(int player, EScreen screen = EScreen.MyRoom) {
			foreach (CUnlockCondition child in this.ChildrenCondition) {
				var _met = child.tConditionMet(player, screen);
				if (_met.Item1 == true) return _met;
			}

			if (screen == EScreen.Internal) {
				return (false, "");
			} else {
				return (false, null);
			}
		}

		public override string tConditionMessage(EScreen screen = EScreen.MyRoom) {
			List<string> _els = new List<string>();

			foreach (CUnlockCondition child in this.ChildrenCondition) {
				var _msg = child.tConditionMessage(screen);
				_els.Add(_msg);
			}

			return String.Join("\nOR ", _els.ToArray());
		}

		protected override int tGetCountChartsPassingCondition(int player) {
			// Unused for this condition
			return -1;
		}
	}
}
