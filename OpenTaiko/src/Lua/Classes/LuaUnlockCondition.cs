using static OpenTaiko.CUnlockCondition;

namespace OpenTaiko {
	internal class LuaUnlockCondition {
		private CUnlockCondition? _cUC;

		public string GetConditionMessage(EScreen screen = EScreen.MyRoom) {
			return _cUC?.tConditionMessage(screen) ?? "";
		}

		// Probably not necessary or a different API should be given?
		public (bool, string?) IsConditionMet(int player, EScreen screen = EScreen.MyRoom) {
			return _cUC?.tConditionMet(player, screen) ?? (true, "");
		}

		public LuaUnlockCondition(CUnlockCondition cUC) {
			this._cUC = cUC;
		}

		public LuaUnlockCondition() {

		}
	}
}
