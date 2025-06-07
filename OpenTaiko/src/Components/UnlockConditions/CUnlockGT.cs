namespace OpenTaiko {
	internal class CUnlockGT : CUnlockCondition {


		public CUnlockGT(CUnlockConditionFactory.UnlockConditionJsonRaw rawJson) : base(rawJson) {
			this.RequiredArgCount = 1;
			this.ConditionId = "gt";
		}

		public override (bool, string?) tConditionMet(int player, EScreen screen = EScreen.MyRoom) {
			if (this.Values.Length == this.RequiredArgCount) {
				bool _globalTrigger = OpenTaiko.SaveFileInstances[player].tGetGlobalTrigger(this.Reference[0]);

				bool fulfiled = this.Values[0] == (_globalTrigger ? 1 : 0);

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
			string _cur = OpenTaiko.PrimarySaveFile.tGetGlobalTrigger(this.Reference[0]) ? "ON" : "OFF";
			string _state = CLangManager.LangInstance.GetString($"UNLOCK_CONDITION_TRIGGER_{this.Values[0]}");

			return CLangManager.LangInstance.GetString("UNLOCK_CONDITION_GLOBAL_TRIGGER", _state, this.Reference[0], _cur);
		}

		protected override int tGetCountChartsPassingCondition(int player) {
			// Unused for this condition
			return -1;
		}
	}
}
