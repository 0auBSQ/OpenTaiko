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

		private Func<string, string?, string?, int> StageExitCallBack;

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



		public void AttachExitCallBack(Func<string, string?, string?, int> cb) {
			StageExitCallBack = cb;
		}

		// Exit is a raw Lua C function (registered in the ctor), not an NLua delegate: NLua can't pad a
		// fixed-arity delegate, but scripts call Exit() / Exit("play") / Exit("title", nil) / Exit(t,n,"fade")
		// with 0-3 args. Reads the stack directly so any arity + nil is fine.
		//   target — "title"/"play"/"stage"/"legacy" (missing ⇒ "title"); name — module/legacy key;
		//   transition — Transitions/ module name (omitted ⇒ default).
		private KeraLua.LuaFunction? _exitCFunction;   // kept alive while registered with the unmanaged state
		private int ExitFromLua(IntPtr statePtr) {
			try {
				var L = KeraLua.Lua.FromIntPtr(statePtr);
				int n = L.GetTop();
				string target = (n >= 1 && !L.IsNoneOrNil(1)) ? (L.ToString(1) ?? "title") : "title";
				string? name = (n >= 2 && !L.IsNoneOrNil(2)) ? L.ToString(2) : null;
				string? transition = (n >= 3 && !L.IsNoneOrNil(3)) ? L.ToString(3) : null;
				int rv = StageExitCallBack != null ? StageExitCallBack(target, name, transition) : 0;
				L.PushInteger(rv);
				return 1;
			} catch (Exception e) {
				System.Diagnostics.Trace.TraceError("Exit() failed: " + e);
				return 0;
			}
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

				// Call "return Exit(target, name, transition)" in the Lua stage's update function to exit the
				// stage. Registered as a raw C function (see ExitFromLua) so any arg count / nils work.
				_exitCFunction = ExitFromLua;
				LuaScript.State.Register("Exit", _exitCFunction);

			} catch (Exception e) {
				Crash(e);
			}

		}

	}
}
