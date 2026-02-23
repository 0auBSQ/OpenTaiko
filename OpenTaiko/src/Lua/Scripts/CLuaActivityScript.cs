using NLua;

namespace OpenTaiko {
	internal class CLuaActivityScript : CLuaScript {
		// Very similar to a LuaStage, but with more limited features, meant to be used in a lua stage

		// Used to identify the lua activity, 
		internal string _activityName;

		// Activation / Deactivation (when met/unmet)
		private LuaFunction lfActivate;
		private LuaFunction lfDeactivate;
		// Main loops 
		private LuaFunction lfUpdate;
		private LuaFunction lfDraw;
		// Extra events
		private LuaFunction lfOnStart;
		private LuaFunction lfAfterSongEnum;
		private LuaFunction lfOnDestroy;

		private bool _active = false;

		#region [CStage/CActivity events]

		public bool IsActive() {
			return _active;
		}

		public object[]? Update(long timestamp, params object[] args) {
			return RunLuaCode(lfUpdate, timestamp, args);
		}

		public object[]? Draw(params object[] args) {
			return RunLuaCode(lfDraw, args);
		}

		public object[]? Activate(params object[] args) {
			_active = true;
			return RunLuaCode(lfActivate, args);
		}

		public object[]? Deactivate(params object[] args) {
			_active = false;
			return RunLuaCode(lfDeactivate, args);
		}

		#endregion

		#region [Extra events]

		public void OnStart() {
			RunLuaCode(lfOnStart);
		}

		public void AfterSongsEnum() {
			RunLuaCode(lfAfterSongEnum);
		}

		public void OnDestroy() {
			RunLuaCode(lfOnDestroy);
		}

		#endregion

		// Handle resources independently in Lua stages, ultimately nameplate script and modal script no longer needing the old hardcoded lua script methods would be a good longer term goal
		public CLuaActivityScript(string dir, string name, string? texturesDir = null, string? soundsDir = null, bool loadAssets = false) : base(dir, texturesDir, soundsDir, loadAssets) {
			_activityName = name;

			try {
				lfUpdate = (LuaFunction)LuaScript["update"];
				lfDraw = (LuaFunction)LuaScript["draw"];
				lfActivate = (LuaFunction)LuaScript["activate"];
				lfDeactivate = (LuaFunction)LuaScript["deactivate"];
				lfOnStart = (LuaFunction)LuaScript["onStart"];
				lfAfterSongEnum = (LuaFunction)LuaScript["afterSongEnum"];
				lfOnDestroy = (LuaFunction)LuaScript["onDestroy"];

				LuaScript["DEACTIVATE"] = Deactivate;

			} catch (Exception e) {
				Crash(e);
			}

		}

	}
}
