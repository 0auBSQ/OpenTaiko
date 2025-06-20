namespace OpenTaiko {
	internal class CUnlockCM : CUnlockCondition {


		public CUnlockCM(CUnlockConditionFactory.UnlockConditionJsonRaw rawJson) : base(rawJson) {
			this.RequiredArgCount = 1;
			this.ConditionId = "cm";
			this.CoinStack = this.Values[0];
		}

		public override (bool, string?) tConditionMet(int player, EScreen screen = EScreen.MyRoom) {
			if (this.Values.Length == this.RequiredArgCount) {
				int _medals = (int)OpenTaiko.SaveFileInstances[player].data.Medals;

				if (screen == EScreen.SongSelect) {
					// Coins are strictly more or equal
					this.Type = "me";
					bool fulfiled = this.tValueRequirementMet(_medals, this.Values[0]);
					return (fulfiled, CLangManager.LangInstance.GetString(fulfiled ? "UNLOCK_COIN_BOUGHT" : "UNLOCK_COIN_MORE"));
				} else if (screen == EScreen.Internal) {
					return (false, CLangManager.LangInstance.GetString("UNLOCK_CONDITION_INVALID"));
				} else {
					return (false, null);
				}

			} else
				return (false, CLangManager.LangInstance.GetString("UNLOCK_CONDITION_ERROR", this.ConditionId, this.RequiredArgCount.ToString()));
		}

		public override string tConditionMessage(EScreen screen = EScreen.MyRoom) {
			if (this.Values.Length < this.RequiredArgCount)
				return CLangManager.LangInstance.GetString("UNLOCK_CONDITION_ERROR", this.ConditionId, this.RequiredArgCount);

			// Only the player loaded as 1P can check unlockables in real time
			var SaveData = OpenTaiko.SaveFileInstances[OpenTaiko.SaveFile].data;
			var ChartStats = SaveData.bestPlaysStats;

			if (screen == EScreen.SongSelect)
				return CLangManager.LangInstance.GetString("UNLOCK_CONDITION_COST", this.Values[0]);
			return (CLangManager.LangInstance.GetString("UNLOCK_CONDITION_INVALID"));
		}

		protected override int tGetCountChartsPassingCondition(int player) {
			// Unused for this condition
			return -1;
		}
	}
}
