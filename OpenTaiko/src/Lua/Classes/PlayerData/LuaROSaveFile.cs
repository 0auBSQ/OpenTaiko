namespace OpenTaiko {
	/// <summary>
	/// Read-only view of <see cref="LuaSaveFile"/> for use in <see cref="LuaROActivityWrapper"/> scripts.
	/// Any attempt to call a write method logs an error and does nothing.
	/// </summary>
	internal class LuaROSaveFile : LuaSaveFile {
		private static void BlockWrite(string method) {
			LogNotification.PopError($"[ROActivity] '{method}' is a write operation and is not allowed in a read-only module.");
		}

		// Hide all mutating members with no-ops
		public new void SpendCoins(long price) => BlockWrite("SpendCoins");
		public new void EarnCoins(long amount) => BlockWrite("EarnCoins");
		public new void UnlockNameplate(int id) => BlockWrite("UnlockNameplate");
		public new void SetGlobalTrigger(string triggerName, bool triggerValue) => BlockWrite("SetGlobalTrigger");
		public new void SetGlobalCounter(string counterName, double counterValue) => BlockWrite("SetGlobalCounter");
		public new bool ChangeCharacter(string name) { BlockWrite("ChangeCharacter"); return false; }

		public LuaROSaveFile(SaveFile sf, int mountedPlayer) : base(sf, mountedPlayer) { }
	}
}
