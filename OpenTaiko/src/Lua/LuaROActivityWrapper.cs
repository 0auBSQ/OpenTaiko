namespace OpenTaiko {
	/// <summary>
	/// Lua-facing accessor for looking up ROActivities by name.
	/// Exposed as the <c>ROACTIVITY</c> global in all Lua scripts.
	/// These methods are also accessible in the <c>ACTIVITY</c> global in writable Lua scripts.
	/// </summary>
	public class LuaROActivityFunc {
		// the script is a global module: a name in both folders gives the global module
		protected readonly bool _fromGlobal;

		public LuaROActivityFunc(bool fromGlobal = false) {
			_fromGlobal = fromGlobal;
		}

		public LuaROActivityWrapper? GetROActivity(string name) =>
			LuaROActivityWrapper.GetROActivity(name, _fromGlobal);
	}

	/// <summary>
	/// The public constructor wraps a <see cref="CLuaROActivityScript"/> loaded from <c>Modules/ROActivities/{name}/Script.lua</c>
	/// (or <c>Global/Modules/ROActivities/{name}/Script.lua</c> in the game folder for a global one).
	/// Scripts in ROActivities receive read-only views of CONFIG and GetSaveFile — any attempt
	/// to write through those globals produces an error rather than modifying game state.
	/// </summary>
	public class LuaROActivityWrapper {
		// The skin's ROActivities and the global ones (Global/Modules/ROActivities) are kept apart, as a name can be in both
		public static Dictionary<string, LuaROActivityWrapper> _skinROActivities = new Dictionary<string, LuaROActivityWrapper>();
		public static Dictionary<string, LuaROActivityWrapper> _globalROActivities = new Dictionary<string, LuaROActivityWrapper>();

		private static IEnumerable<LuaROActivityWrapper> _allROActivities => _skinROActivities.Values.Concat(_globalROActivities.Values);

		#region [Static management]

		public static void ResetROActivityDictionary() {
			foreach (var act in _allROActivities)
				act.DisposeActivity();
			_skinROActivities.Clear();
			_globalROActivities.Clear();
		}

		// A ROActivity by name: the skin's first, unless preferGlobal (the lookup comes from a global module)
		public static LuaROActivityWrapper? GetROActivity(string name, bool preferGlobal = false) {
			var (first, second) = preferGlobal ? (_globalROActivities, _skinROActivities) : (_skinROActivities, _globalROActivities);
			if (first.TryGetValue(name, out var act) || second.TryGetValue(name, out act))
				return act;
			return null;
		}

		public static void PropagateAfterSongEnumEvent() {
			foreach (var act in _allROActivities)
				act.AfterSongsEnum();
		}


		public static void PropagateOnDestroy() {
			foreach (var act in _allROActivities)
				act.OnDestroy();
		}

		#endregion

		protected CLuaActivityScriptBase lcActScript;

		// loaded from the game's Global/Modules folder
		internal bool IsGlobal { get; private protected set; }

		public void DisposeActivity() {
			lcActScript?.Dispose();
		}

		public LuaROActivityWrapper(string name, bool isGlobal = false) {
			IsGlobal = isGlobal;
			if (isGlobal == false) lcActScript = new CLuaROActivityScript(CSkin.Path($"Modules/ROActivities/{name}"), name);
			else lcActScript = new CLuaROActivityScript(CSkin.GlobalModulePath("ROActivities", name), $"[GLOBAL]{name}");
			(isGlobal ? _globalROActivities : _skinROActivities)[name] = this;
		}

		protected LuaROActivityWrapper() { } // initialized by derived class

		#region [Standard lifecycle events]

		public bool IsActive => lcActScript?.IsActive() ?? false;

		public object[]? Activate(params object[] args) => lcActScript?.Activate(args);
		public object[]? Deactivate(params object[] args) => lcActScript?.Deactivate(args);
		public object[]? Draw(params object[] args) => lcActScript?.Draw(args);
		public object[]? Update(params object[] args) => lcActScript?.Update(FDK.Game.TimeMs, args);

		#endregion

		#region [Generic Lua function call]

		/// <summary>
		/// Calls a named Lua function defined in this (RO)Activity's script.
		/// </summary>
		public object[]? Call(string functionName, params object[] args) => lcActScript?.CallFunction(functionName, args);

		#endregion

		#region [Events not present on CStage/CActivity]

		// Incremental onStart (see LuaStageWrapper) — drives a yielding onStart across frames with the bar.
		internal void BeginOnStart() => lcActScript?.BeginOnStart();
		internal bool StepOnStart(out float progress) {
			if (lcActScript == null) { progress = 0f; return false; }
			return lcActScript.StepOnStart(out progress);
		}

		// Executes everytime songs enum is done, including soft/hard reload and at start
		protected void AfterSongsEnum() => lcActScript?.AfterSongsEnum();

		// Executes before skin change, in order to deallocate any ressources carried by the skin's Lua modules
		protected void OnDestroy() => lcActScript?.OnDestroy();

		#endregion

	}
}
