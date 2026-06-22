namespace OpenTaiko {
	// Holds one transition Lua component + a static registry of those loaded with the current skin.
	// CStageTransition looks one up by name (from a stage's Exit(target, name, transition)) and drives its
	// fade/loading callbacks. Transitions onStart BEFORE the skin's Stages/Activities and onDestroy AFTER them.
	internal class LuaTransitionWrapper {
		public static Dictionary<string, LuaTransitionWrapper> _allTransitions = new Dictionary<string, LuaTransitionWrapper>();

		// Used when Exit requests a transition without naming one (or names a missing one).
		public const string DefaultName = "default";

		private CLuaTransitionScript lcTransitionScript;

		public LuaTransitionWrapper(string name) {
			lcTransitionScript = new CLuaTransitionScript(CSkin.Path($"Modules/Transitions/{name}"), name);
			_allTransitions[name] = this;
		}

		// Named transition, else the default, else null (skin has no Transitions/).
		public static LuaTransitionWrapper? Get(string? name) {
			if (!string.IsNullOrEmpty(name) && _allTransitions.TryGetValue(name, out var t)) return t;
			if (_allTransitions.TryGetValue(DefaultName, out var def)) return def;
			return null;
		}

		public static void ResetTransitionsDictionary() {
			foreach (var t in _allTransitions.Values) t.Dispose();
			_allTransitions.Clear();
		}

		public static void PropagateOnDestroy() {
			foreach (var t in _allTransitions.Values) t.lcTransitionScript?.OnDestroy();
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
