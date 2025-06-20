using static OpenTaiko.BestPlayRecords;

namespace OpenTaiko {
	abstract class CUnlockCondition {
		public CUnlockCondition(CUnlockConditionFactory.UnlockConditionJsonRaw? rawJson) {
			// rawJson is nullable only for failed unlock conditions
			if (rawJson != null) {
				Values = rawJson.Values.Copy();
				Type = rawJson.Type;
				Reference = rawJson.Reference.Copy();
				CoinStack = 0;
			} else {
				Values = [];
				Type = "me";
				Reference = [];
				CoinStack = 0;
			}
		}

		// Condition values
		public int[] Values;

		// Condition type
		public string Type;

		// Referenced charts
		public string[] Reference;

		// Used to get recursively coin counts on OR/AND comb conditions
		public int CoinStack;

		protected int RequiredArgCount = -1;
		protected string ConditionId = "";

		// For (future?) combined unlock conditions
		protected List<CUnlockCondition> ChildrenCondition = new List<CUnlockCondition>();

		/*
		 * (Note: Currently only me is relevant, the other types might be used in the future)
		 * == Types of conditions ==
		 * l : "Less than"
		 * le : "Less or equal"
		 * e : "Equal"
		 * me : "More or equal"  (Default)
		 * m : "More than"
		 * d : "Different"
		 */
		public bool tValueRequirementMet(int val1, int val2) {
			switch (this.Type) {
				case "l":
					return (val1 < val2);
				case "le":
					return (val1 <= val2);
				case "e":
					return (val1 == val2);
				case "me":
					return (val1 >= val2);
				case "m":
					return (val1 > val2);
				case "d":
					return (val1 != val2);
				default:
					return (val1 >= val2);
			}
		}

		public bool tValueRequirementMet(double val1, double val2) {
			switch (this.Type) {
				case "l":
					return (val1 < val2);
				case "le":
					return (val1 <= val2);
				case "e":
					return (val1 == val2);
				case "me":
					return (val1 >= val2);
				case "m":
					return (val1 > val2);
				case "d":
					return (val1 != val2);
				default:
					return (val1 >= val2);
			}
		}

		public string GetRequiredClearStatus(int status, bool exact = false) {
			switch (status) {
				case (int)EClearStatus.PERFECT:
					return CLangManager.LangInstance.GetString(exact ? "UNLOCK_CONDITION_REQUIRE_PERFECT" : "UNLOCK_CONDITION_REQUIRE_PERFECT_MORE");
				case (int)EClearStatus.FC:
					return CLangManager.LangInstance.GetString(exact ? "UNLOCK_CONDITION_REQUIRE_FC" : "UNLOCK_CONDITION_REQUIRE_FC_MORE");
				case (int)EClearStatus.CLEAR:
					return CLangManager.LangInstance.GetString(exact ? "UNLOCK_CONDITION_REQUIRE_CLEAR" : "UNLOCK_CONDITION_REQUIRE_CLEAR_MORE");
				case (int)EClearStatus.ASSISTED_CLEAR:
					return CLangManager.LangInstance.GetString(exact ? "UNLOCK_CONDITION_REQUIRE_ASSIST" : "UNLOCK_CONDITION_REQUIRE_ASSIST_MORE");
				default:
					return CLangManager.LangInstance.GetString(exact ? "UNLOCK_CONDITION_REQUIRE_PLAY" : "UNLOCK_CONDITION_REQUIRE_PLAY_MORE");
			}
		}

		public abstract (bool, string?) tConditionMet(int player, EScreen screen = EScreen.MyRoom);

		public abstract string tConditionMessage(EScreen screen = EScreen.MyRoom);

		public enum EScreen {
			MyRoom,
			Shop,
			SongSelect,
			Internal
		}

		protected abstract int tGetCountChartsPassingCondition(int player);

	}
}
