namespace OpenTaiko {
	internal class CUnlockOrComb : CUnlockCondition {
		/*
		 * "Or combination" Unlock condition object
		 * Validates if at least one of the contained conditions is satisfied
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


		public CUnlockOrComb(CUnlockConditionFactory.UnlockConditionJsonRaw rawJson) : base(rawJson) {
			this.RequiredArgCount = 0;
			this.ConditionId = "orcomb";
			this.ParseChildrenOperations(rawJson);
		}

		public override (bool, string?) tConditionMet(int player, EScreen screen = EScreen.MyRoom) {
			int _medals = (int)OpenTaiko.SaveFileInstances[player].data.Medals;
			int _buyable_for = int.MaxValue;

			foreach (CUnlockCondition child in this.ChildrenCondition) {
				var _met = child.tConditionMet(player, screen);
				int _price = child.CoinStack;
				if (_met.Item1 == true) {
					if (_price == 0) {
						this.CoinStack = 0;
						return _met;
					}
					_buyable_for = Math.Min(_buyable_for, _price);
					this.CoinStack = _buyable_for;
				}

			}

			if (this.CoinStack > 0) {
				if (_medals < this.CoinStack) return (false, CLangManager.LangInstance.GetString("UNLOCK_COIN_MORE"));
				else return (true, CLangManager.LangInstance.GetString("UNLOCK_COIN_BOUGHT"));
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
