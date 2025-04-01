namespace OpenTaiko {
	internal class CUnlockAndComb : CUnlockCondition {
		/*
		 * "And combination" Unlock condition object
		 * Validates if all contained conditions are satisfied
		 */


		public CUnlockAndComb(CUnlockConditionFactory.UnlockConditionJsonRaw rawJson) : base(rawJson) {
			this.RequiredArgCount = 0;
			this.ConditionId = "andcomb";
		}

		public override (bool, string?) tConditionMet(int player, EScreen screen = EScreen.MyRoom) {
			foreach (CUnlockCondition child in this.ChildrenCondition) {
				var _met = child.tConditionMet(player, screen);
				if (_met.Item1 == false) return _met;
			}

			if (screen == EScreen.Internal) {
				return (true, "");
			} else {
				return (true, null);
			}
		}

		public override string tConditionMessage(EScreen screen = EScreen.MyRoom) {
			List<string> _els = new List<string>();

			foreach (CUnlockCondition child in this.ChildrenCondition) {
				var _msg = child.tConditionMessage(screen);
				_els.Add("- " + _msg.Replace("\n", "\n  "));
			}

			return String.Join("\n", _els.ToArray());
		}

		protected override int tGetCountChartsPassingCondition(int player) {
			// Unused for this condition
			return -1;
		}
	}
}
