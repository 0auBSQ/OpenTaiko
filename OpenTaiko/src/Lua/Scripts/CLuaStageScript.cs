using NLua;

namespace OpenTaiko {
	internal class CLuaStageScript : CLuaScript {

		// Used to identify the lua stage, 
		internal string _stageName;

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

		private Func<string, string?, int> StageExitCallBack;

		#region [CStage/CActivity events]

		public void Update() {
			RunLuaCode(lfUpdate);
		}

		public void Draw() {
			RunLuaCode(lfDraw);
		}

		public void Activate() {
			RunLuaCode(lfActivate);
		}

		public void Deactivate() {
			RunLuaCode(lfDeactivate);
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



		public void AttachExitCallBack(Func<string, string?, int> cb) {
			StageExitCallBack = cb;
		}


		// Handle resources independently in Lua stages, ultimately nameplate script and modal script no longer needing the old hardcoded lua script methods would be a good longer term goal
		public CLuaStageScript(string dir, string name, string? texturesDir = null, string? soundsDir = null, bool loadAssets = false) : base(dir, texturesDir, soundsDir, loadAssets) {
			_stageName = name;

			try {
				lfUpdate = (LuaFunction)LuaScript["update"];
				lfDraw = (LuaFunction)LuaScript["draw"];
				lfActivate = (LuaFunction)LuaScript["activate"];
				lfDeactivate = (LuaFunction)LuaScript["deactivate"];
				lfOnStart = (LuaFunction)LuaScript["onStart"];
				lfAfterSongEnum = (LuaFunction)LuaScript["afterSongEnum"];
				lfOnDestroy = (LuaFunction)LuaScript["onDestroy"];

				// Call "return Exit(transition)" in the Lua stage's update function to exit the stage
				LuaScript["Exit"] = StageExitCallBack;

			} catch (Exception e) {
				Crash(e);
			}

		}

	}
}
