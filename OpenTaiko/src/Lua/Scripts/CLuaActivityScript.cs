using NLua;

namespace OpenTaiko {
	public abstract class CLuaActivityScriptBase : CLuaScript {

		// Very similar to a LuaStage, but with more limited features, meant to be used in a lua stage

		// Used to identify the lua activity, 
		internal string _activityName;

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

		public object[]? Activate(params object?[] args) {
			_active = true;
			// Refresh globals populated by TextureLoader after script construction (mirrors CLuaStageScript).
			LuaScript["CHARACTERLIST"] = OpenTaiko.Tx?.LuaCharacterDb;
			LuaScript["PUCHICHARALIST"] = OpenTaiko.Tx?.LuaPuchicharaDb;
			return RunLuaCode(lfActivate, args);
		}

		public object[]? Deactivate(params object[] args) {
			_active = false;
			return RunLuaCode(lfDeactivate, args);
		}

		#endregion

		#region [Extra events]

		// onStart runs as a coroutine (BeginOnStart + StepOnStart per frame) so heavy loading spreads across
		// frames instead of freezing the render thread.
		public void BeginOnStart() => tBeginYieldable(lfOnStart);
		public bool StepOnStart(out float progress) => tStepYieldable(out progress);

		public void AfterSongsEnum() {
			RunLuaCode(lfAfterSongEnum);
		}

		public void OnDestroy() {
			RunLuaCode(lfOnDestroy);
		}

		#endregion

		// Handle resources independently in Lua stages, ultimately nameplate script and modal script no longer needing the old hardcoded lua script methods would be a good longer term goal
		public CLuaActivityScriptBase(string dir, string name, string? texturesDir = null, string? soundsDir = null, bool loadAssets = false, bool writable = true)
			: base(dir, texturesDir, soundsDir, loadAssets, writable: writable) {
			_activityName = name;

			try {
				lfUpdate.Load(LuaScript);
				lfDraw.Load(LuaScript);
				lfActivate.Load(LuaScript);
				lfDeactivate.Load(LuaScript);
				lfOnStart.Load(LuaScript);
				lfAfterSongEnum.Load(LuaScript);
				lfOnDestroy.Load(LuaScript);

				LuaScript["DEACTIVATE"] = (Delegate)Deactivate;

			} catch (Exception e) {
				Crash(e, $"initializing {nameof(CLuaActivityScript)}");
			}

		}

		/// <summary>
		/// Calls a named Lua function in this script with the given arguments.
		/// Returns null if the function does not exist or the script has crashed.
		/// </summary>
		public object[]? CallFunction(string name, params object[] args) {
			NamedLuaFunction func = new(name);
			func.Load(LuaScript);
			return RunLuaCode(func, args);
		}
	}

	/// <summary>
	/// Like <see cref="CLuaROActivityScript"/> but loads the writable <c>CONFIG</c> and <c>GetSaveFile</c> globals.
	/// The loading happens in the base constructor so that all called Lua functions see the writable versions,
	/// so it cannot be accessed as a CLuaROActivityScript.
	/// </summary>
	public class CLuaActivityScript : CLuaActivityScriptBase {
		public CLuaActivityScript(string dir, string name) : base(dir, name, writable: true) { }
	}
}
