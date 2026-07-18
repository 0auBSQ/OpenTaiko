namespace OpenTaiko {
	// A transition Lua component (Modules/Transitions/<name>/Script.lua). CStageTransition drives its visuals
	// via fadeOut(t) / loading(progress, elapsed) / fadeIn(t); onStart/onDestroy run respectively before/after
	// the skin's Stages/Activities. Inherits the CLuaScript drawing/asset API.
	internal class CLuaTransitionScript : CLuaScript {
		internal string _transitionName;

		// Optional per-transition fade durations: a transition's Script.lua may set the top-level globals
		// FADE_OUT_SECONDS / FADE_IN_SECONDS to override CStageTransition's default. null ⇒ use the default.
		internal double? FadeOutSeconds;
		internal double? FadeInSeconds;

		private NamedLuaFunction lfFadeOut = new("fadeOut");
		private NamedLuaFunction lfLoading = new("loading");
		private NamedLuaFunction lfFadeIn = new("fadeIn");
		private NamedLuaFunction lfOnStart = new("onStart");
		private NamedLuaFunction lfOnDestroy = new("onDestroy");

		public void FadeOut(double t) => RunLuaCode(lfFadeOut, t);
		public void Loading(double progress, double elapsed) => RunLuaCode(lfLoading, progress, elapsed);
		public void FadeIn(double t) => RunLuaCode(lfFadeIn, t);

		// onStart runs as a coroutine so its asset loads stream in behind the boot/skin-reload bar.
		public void BeginOnStart() => tBeginYieldable(lfOnStart);
		public bool StepOnStart(out float progress) => tStepYieldable(out progress);

		public void OnDestroy() => RunLuaCode(lfOnDestroy);

		public CLuaTransitionScript(string dir, string name, string? texturesDir = null, string? soundsDir = null, bool loadAssets = false) : base(dir, texturesDir, soundsDir, loadAssets) {
			_transitionName = name;

			try {
				lfFadeOut.Load(LuaScript);
				lfLoading.Load(LuaScript);
				lfFadeIn.Load(LuaScript);
				lfOnStart.Load(LuaScript);
				lfOnDestroy.Load(LuaScript);
				FadeOutSeconds = ReadSeconds("FADE_OUT_SECONDS");
				FadeInSeconds = ReadSeconds("FADE_IN_SECONDS");
			} catch (Exception e) {
				Crash(e, $"initializing {nameof(CLuaTransitionScript)}");
			}
		}

		// Read a top-level numeric global (the transition's fade duration override), or null if unset/invalid.
		private double? ReadSeconds(string name) {
			try {
				var v = LuaScript?[name];
				if (v == null) return null;
				double s = Convert.ToDouble(v);
				return s > 0 ? s : null;
			} catch { return null; }
		}
	}
}
