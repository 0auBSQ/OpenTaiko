using static OpenTaiko.CUnlockCondition;

namespace OpenTaiko {
	internal class CUnlockCS : CUnlockCondition {

		public CUnlockCS(CUnlockConditionFactory.UnlockConditionJsonRaw rawJson) : base(rawJson) {
			this.RequiredArgCount = 1;
			this.ConditionId = "cs";
			this.CoinStack = this.Values[0];
		}

		public override (bool, string?) tConditionMet(int player, EScreen screen = EScreen.MyRoom) {
			if (screen == EScreen.Internal) return (false, "");

			if (this.Values.Length == this.RequiredArgCount) {
				int _medals = (int)OpenTaiko.SaveFileInstances[player].data.Medals;

				this.Type = "me";
				bool fulfiled = this.tValueRequirementMet(_medals, this.Values[0]);
				return (fulfiled, CLangManager.LangInstance.GetString(fulfiled ? "UNLOCK_COIN_BOUGHT" : "UNLOCK_COIN_MORE"));
			} else
				return (false, CLangManager.LangInstance.GetString("UNLOCK_CONDITION_ERROR", this.ConditionId, this.RequiredArgCount.ToString()));
		}

		public override string tConditionMessage(EScreen screen = EScreen.MyRoom) {
			if (this.Values.Length < this.RequiredArgCount)
				return CLangManager.LangInstance.GetString("UNLOCK_CONDITION_ERROR", this.ConditionId, this.RequiredArgCount);

			return CLangManager.LangInstance.GetString("UNLOCK_CONDITION_COST", this.Values[0]);
		}

		protected override int tGetCountChartsPassingCondition(int player) {
			// Unused for this condition
			return -1;
		}
	}
}
