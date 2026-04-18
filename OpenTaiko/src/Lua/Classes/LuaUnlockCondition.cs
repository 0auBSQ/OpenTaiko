using static OpenTaiko.CUnlockCondition;

namespace OpenTaiko {
	public class LuaUnlockCondition {
		private CUnlockCondition? _cUC;

		public string GetConditionMessage() {
			return _cUC?.tConditionMessage(EScreen.MyRoom) ?? "";
		}

		/// <summary>Returns true when this entry has an explicit unlock condition. Items with no condition are available by default.</summary>
		public bool HasCondition => _cUC != null;

		/// <summary>Returns the coin price of this unlock condition (0 if no coin cost).</summary>
		public int GetCoinPrice() => _cUC?.CoinStack ?? 0;

		/// <summary>Returns true if the given player meets this unlock condition.</summary>
		public bool IsUnlockable(int player) =>
			(_cUC?.tConditionMet(player, EScreen.MyRoom) ?? (true, null)).Item1;

		/// <summary>Returns the reason the condition is blocked, or empty string if unlockable.</summary>
		public string GetBlockedMessage(int player) =>
			(_cUC?.tConditionMet(player, EScreen.MyRoom) ?? (true, null)).Item2 ?? "";

		internal LuaUnlockCondition(CUnlockCondition? cUC) {
			this._cUC = cUC;
		}

		public LuaUnlockCondition() {

		}
	}
}
