using NLua;

namespace OpenTaiko {
	/// <summary>
	/// Like <see cref="CLuaActivityScript"/> but replaces the writable <c>CONFIG</c> and
	/// <c>GetSaveFile</c> globals with read-only counterparts.
	/// The replacement happens after the base constructor so that the Lua script file
	/// is already parsed; all subsequently called Lua functions see the RO versions.
	/// </summary>
	internal class CLuaROActivityScript : CLuaActivityScript {
		public CLuaROActivityScript(string dir, string name) : base(dir, name) {
			try {
				LuaScript["CONFIG"] = new LuaROConfigIniFunc();
				LuaScript["GetSaveFile"] = (Func<int, LuaSaveFile?>)GetROSaveFile;
				// ROActivity scripts may not use ACTIVITY (use ROACTIVITY instead) or write to DATABASE
				LuaScript["ACTIVITY"] = null;
				LuaScript["DATABASE"] = new LuaRODataStorageFunc(dir);
			} catch (Exception e) {
				Crash(e);
			}
		}

		private LuaSaveFile? GetROSaveFile(int player) {
			if (player < 0 || player > OpenTaiko.MAX_PLAYERS) {
				LogNotification.PopError($"Invalid player index in lua module, expected [0,{OpenTaiko.MAX_PLAYERS}]");
				return null;
			}
			return new LuaROSaveFile(OpenTaiko.SaveFileInstances[player], player);
		}

		/// <summary>
		/// Calls a named Lua function in this script with the given arguments.
		/// Returns null if the function does not exist or the script has crashed.
		/// </summary>
		public object[]? CallFunction(string name, params object[] args) {
			var func = LuaScript[name] as LuaFunction;
			return RunLuaCode(func, args);
		}
	}
}
