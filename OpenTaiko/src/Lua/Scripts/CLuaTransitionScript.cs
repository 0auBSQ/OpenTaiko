namespace OpenTaiko {
	// A transition Lua component (Modules/Transitions/<name>/Script.lua). CStageTransition drives its visuals
	// via fadeOut(t) / loading(progress, elapsed) / fadeIn(t); onStart/onDestroy run respectively before/after
	// the skin's Stages/Activities. Inherits the CLuaScript drawing/asset API.
	internal class CLuaTransitionScript : CLuaScript {
		internal string _transitionName;

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
			} catch (Exception e) {
				Crash(e);
			}
		}
	}
}
