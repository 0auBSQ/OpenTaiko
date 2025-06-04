namespace OpenTaiko {
	internal class CUnlockAndComb : CUnlockCondition {
		/*
		 * "And combination" Unlock condition object
		 * Validates if all contained conditions are satisfied
		 */

		private void ParseChildrenOperations(CUnlockConditionFactory.UnlockConditionJsonRaw rawJson) {
			this.ChildrenCondition = new List<CUnlockCondition>();

			foreach (string jsonstr in this.Reference) {
				CUnlockConditionFactory.UnlockConditionJsonRaw? _raw = ConfigManager.JsonParse<CUnlockConditionFactory.UnlockConditionJsonRaw>(jsonstr);
				if (_raw != null) {
					CUnlockCondition _cond = OpenTaiko.UnlockConditionFactory.GenerateUnlockObjectFromJsonRaw(_raw);
					this.ChildrenCondition.Add(_cond);
				}
			}
		}


		public CUnlockAndComb(CUnlockConditionFactory.UnlockConditionJsonRaw rawJson) : base(rawJson) {
			this.RequiredArgCount = 0;
			this.ConditionId = "andcomb";
			this.ParseChildrenOperations(rawJson);
		}

		public override (bool, string?) tConditionMet(int player, EScreen screen = EScreen.MyRoom) {
			int _medals = (int)OpenTaiko.SaveFileInstances[player].data.Medals;

			foreach (CUnlockCondition child in this.ChildrenCondition) {
				var _met = child.tConditionMet(player, screen);
				this.CoinStack += child.CoinStack;
				if (_met.Item1 == false) return _met;
			}

			if (_medals < this.CoinStack) return (false, CLangManager.LangInstance.GetString("UNLOCK_COIN_MORE"));

			if (screen == EScreen.Internal) {
				return (true, "");
			} else if (this.CoinStack > 0) {
				return (true, CLangManager.LangInstance.GetString("UNLOCK_COIN_BOUGHT"));
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
