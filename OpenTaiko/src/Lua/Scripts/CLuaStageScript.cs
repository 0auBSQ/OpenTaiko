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


		// Handle resources independently in Lua stages, ultimately nameplate script and modal script no longer needing the old hardcoded lua script methods would be a good longer term goal
		public CLuaStageScript(string dir, string name, string? texturesDir = null, string? soundsDir = null, bool loadAssets = false) : base(dir, texturesDir, soundsDir, loadAssets) {
			_stageName = name;

			lfUpdate = (LuaFunction)LuaScript["update"];
			lfDraw = (LuaFunction)LuaScript["draw"];
			lfActivate = (LuaFunction)LuaScript["activate"];
			lfDeactivate = (LuaFunction)LuaScript["deactivate"];
		}

	}
}
