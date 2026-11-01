using static OpenTaiko.CUnlockCondition;

namespace OpenTaiko {
	public class LuaUnlockCondition {
		private CUnlockCondition? _cUC;

		/// <summary>The Unlock.json's custom_unlock_text in the game language when it has one, else the condition's own message.</summary>
		public string GetConditionMessage() {
			if (_cUC == null) return "";
			if (_cUC.CustomText != null && _cUC.CustomText.Strings.Count > 0)
				return _cUC.CustomText.GetString(_cUC.tConditionMessage(EScreen.MyRoom));
			return _cUC.tConditionMessage(EScreen.MyRoom);
		}

		/// <summary>True when the Unlock.json gives its own text (custom_unlock_text) for this condition.</summary>
		public bool HasCustomMessage => _cUC?.CustomText != null && _cUC.CustomText.Strings.Count > 0;

		/// <summary>The Unlock.json's hidden_index, 0-3 as for songs (0 displayed, 1 grayed, 2 blurred, 3 hidden); 0 without one.</summary>
		public int GetHiddenIndex() => _cUC?.HiddenIndex ?? 0;

		/// <summary>The Unlock.json's rarity, or "" when it gives none (the asset's Rarity already includes it).</summary>
		public string GetRarity() => _cUC?.Rarity ?? "";

		/// <summary>Returns true when this entry has an explicit unlock condition. Items with no condition are available by default.</summary>
		public bool HasCondition => _cUC != null;

		/// <summary>The condition's type id (e.g. "ch","cs","gt","gc","ig",…) or "" if none.</summary>
		public string GetConditionType() => _cUC?.Id ?? "";

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
