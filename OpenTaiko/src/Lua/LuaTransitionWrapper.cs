namespace OpenTaiko {
	// Holds one transition Lua component + a static registry of those loaded with the current skin.
	// CStageTransition looks one up by name (from a stage's Exit(target, name, transition)) and drives its
	// fade/loading callbacks. Transitions onStart BEFORE the skin's Stages/Activities and onDestroy AFTER them.
	internal class LuaTransitionWrapper {
		// The skin's transitions and the global ones (Global/Modules/Transitions) are kept apart, as a name can be in both
		public static Dictionary<string, LuaTransitionWrapper> _skinTransitions = new Dictionary<string, LuaTransitionWrapper>();
		public static Dictionary<string, LuaTransitionWrapper> _globalTransitions = new Dictionary<string, LuaTransitionWrapper>();

		private static IEnumerable<LuaTransitionWrapper> _allTransitions => _skinTransitions.Values.Concat(_globalTransitions.Values);

		// Used when Exit requests a transition without naming one (or names a missing one).
		public const string DefaultName = "default";

		private CLuaTransitionScript lcTransitionScript;

		public LuaTransitionWrapper(string name, bool isGlobal = false) {
			if (isGlobal == false) lcTransitionScript = new CLuaTransitionScript(CSkin.Path($"Modules/Transitions/{name}"), name);
			else lcTransitionScript = new CLuaTransitionScript(CSkin.GlobalModulePath("Transitions", name), $"[GLOBAL]{name}");
			(isGlobal ? _globalTransitions : _skinTransitions)[name] = this;
		}

		// Named transition, else the default, else null (no Transitions/ anywhere). Each looks in the skin's
		// transitions first, unless preferGlobal (a global module asks).
		public static LuaTransitionWrapper? Get(string? name, bool preferGlobal = false) {
			var (first, second) = preferGlobal ? (_globalTransitions, _skinTransitions) : (_skinTransitions, _globalTransitions);
			if (!string.IsNullOrEmpty(name) && (first.TryGetValue(name, out var t) || second.TryGetValue(name, out t))) return t;
			if (first.TryGetValue(DefaultName, out var def) || second.TryGetValue(DefaultName, out def)) return def;
			return null;
		}

		public static void ResetTransitionsDictionary() {
			foreach (var t in _allTransitions) t.Dispose();
			_skinTransitions.Clear();
			_globalTransitions.Clear();
		}

		public static void PropagateOnDestroy() {
			foreach (var t in _allTransitions) t.lcTransitionScript?.OnDestroy();
		}

		// Incremental onStart, driven by the boot/skin-reload bar.
		public void BeginOnStart() => lcTransitionScript?.BeginOnStart();
		public bool StepOnStart(out float progress) {
			if (lcTransitionScript == null) { progress = 0f; return false; }
			return lcTransitionScript.StepOnStart(out progress);
		}

		// Per-transition fade-duration overrides (null ⇒ CStageTransition's default), declared as FADE_OUT_SECONDS
		// / FADE_IN_SECONDS globals in the transition's Script.lua.
		public double? FadeOutSeconds => lcTransitionScript?.FadeOutSeconds;
		public double? FadeInSeconds => lcTransitionScript?.FadeInSeconds;

		public void FadeOut(double t) => lcTransitionScript?.FadeOut(t);
		public void Loading(double progress, double elapsed) => lcTransitionScript?.Loading(progress, elapsed);
		public void FadeIn(double t) => lcTransitionScript?.FadeIn(t);

		public void Dispose() => lcTransitionScript?.Dispose();
	}
}
