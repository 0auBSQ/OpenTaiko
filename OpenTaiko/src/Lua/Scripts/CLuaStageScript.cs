using NLua;

namespace OpenTaiko {
	internal class CLuaStageScript : CLuaScript {

		// Used to identify the lua stage, 
		internal string _stageName;

		// Activation / Deactivation (when met/unmet)
		private NamedLuaFunction lfActivate = new("activate");
		private NamedLuaFunction lfDeactivate = new("deactivate");
		// Main loops 
		private NamedLuaFunction lfUpdate = new("update");
		private NamedLuaFunction lfDraw = new("draw");
		// Extra events
		private NamedLuaFunction lfOnStart = new("onStart");
		private NamedLuaFunction lfAfterSongEnum = new("afterSongEnum");
		private NamedLuaFunction lfOnDestroy = new("onDestroy");

		private Func<string, string?, int> StageExitCallBack;

		#region [CStage/CActivity events]

		public void Update(long timestamp) {
			RunLuaCode(lfUpdate, timestamp);
		}

		public void Draw() {
			RunLuaCode(lfDraw);
		}

		public void Activate() {
			// Refresh globals that are populated by TextureLoader after script construction.
			LuaScript["CHARACTERLIST"] = OpenTaiko.Tx?.LuaCharacterDb;
			LuaScript["PUCHICHARALIST"] = OpenTaiko.Tx?.LuaPuchicharaDb;
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

		public int ExitStage(string transition, string? name) {
			return StageExitCallBack(transition, name);
		}


		// Handle resources independently in Lua stages, ultimately nameplate script and modal script no longer needing the old hardcoded lua script methods would be a good longer term goal
		public CLuaStageScript(string dir, string name, string? texturesDir = null, string? soundsDir = null, bool loadAssets = false) : base(dir, texturesDir, soundsDir, loadAssets) {
			_stageName = name;

			try {
				lfUpdate.Load(LuaScript);
				lfDraw.Load(LuaScript);
				lfActivate.Load(LuaScript);
				lfDeactivate.Load(LuaScript);
				lfOnStart.Load(LuaScript);
				lfAfterSongEnum.Load(LuaScript);
				lfOnDestroy.Load(LuaScript);

				// Call "return Exit(transition)" in the Lua stage's update function to exit the stage
				LuaScript["Exit"] = ExitStage;

			} catch (Exception e) {
				Crash(e);
			}

		}

	}
}
