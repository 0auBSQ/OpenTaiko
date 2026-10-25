namespace OpenTaiko {
	internal class LuaStageWrapper : CStage {
		// Used to toggle stages in the OpenTaiko.cs file
		// The skin's stages and the global ones (Global/Modules/Stages) are kept apart, as a name can be in both
		public static Dictionary<string, LuaStageWrapper> _skinLuaStages = new Dictionary<string, LuaStageWrapper>();
		public static Dictionary<string, LuaStageWrapper> _globalLuaStages = new Dictionary<string, LuaStageWrapper>();

		private static IEnumerable<LuaStageWrapper> _allLuaStages => _skinLuaStages.Values.Concat(_globalLuaStages.Values);

		// the stages only a skin may have: one of them in Global/Modules/Stages stops the game from starting
		public static readonly string[] CoreStages = { "_boot", "_title" };

		// the core stage found in Global/Modules/Stages by the last module load, null when there is none
		public static string? GlobalCoreStage = null;

		private static string _nextRequestedStage = "";
		private static bool _nextRequestedFromGlobal = false;

		#region [Setters]

		// fromGlobal: a global stage asks, so a name in both folders opens the global stage
		public static void ForceSetNextRequestedStage(string name, bool fromGlobal = false) {
			_nextRequestedStage = name;
			_nextRequestedFromGlobal = fromGlobal;
		}

		public static void ResetLuaStagesDictionary() {
			foreach (LuaStageWrapper _stage in _allLuaStages) {
				_stage.DisposeStage();
			}
			_skinLuaStages.Clear();
			_globalLuaStages.Clear();
		}

		#endregion

		#region [Getters]

		// A stage by name: the skin's first, unless preferGlobal (the lookup comes from a global module)
		public static LuaStageWrapper? GetLuaStage(string name, bool preferGlobal = false) {
			var (first, second) = preferGlobal ? (_globalLuaStages, _skinLuaStages) : (_skinLuaStages, _globalLuaStages);
			if (first.TryGetValue(name, out var _stage) || second.TryGetValue(name, out _stage)) {
				return _stage;
			}
			return null;
		}

		// The stages of a namespace as a caller reaches them: one per name, the one GetLuaStage gives, kept when
		// it declared the namespace; ordered by name
		public static List<LuaStageWrapper> GetStagesInNamespace(string ns, bool preferGlobal = false) {
			var found = new List<LuaStageWrapper>();
			if (string.IsNullOrEmpty(ns)) return found;
			foreach (string name in _skinLuaStages.Keys.Union(_globalLuaStages.Keys).OrderBy(n => n, StringComparer.Ordinal)) {
				var stage = GetLuaStage(name, preferGlobal);
				if (stage != null && stage.Namespace == ns) found.Add(stage);
			}
			return found;
		}

		public static LuaStageWrapper? GetNextRequestedStage() {
			LuaStageWrapper? _stage = GetLuaStage(_nextRequestedStage, _nextRequestedFromGlobal);

			if (_stage == null) {
				LogNotification.PopError($"The requested Lua Stage {_nextRequestedStage} is not present in the Modules folder");
			}
			return _stage;
		}

		public static string GetNextRequestedStageName() {
			return _nextRequestedStage;
		}

		public static bool IsNextRequestedStageFromGlobal() {
			return _nextRequestedFromGlobal;
		}

		#endregion

		#region [Executers]

		public static void PropagateAfterSongEnumEvent() {
			foreach (LuaStageWrapper _stage in _allLuaStages) {
				_stage.AfterSongsEnum();
			}
		}

		public static void PropagateOnDestroy() {
			foreach (LuaStageWrapper _stage in _allLuaStages) {
				_stage.OnDestroy();
			}
		}

		#endregion

		private CLuaStageScript lcStageScript;

		// loaded from Global/Modules/Stages
		public readonly bool IsGlobal;

		// the namespace the stage declared with STAGES:SetNamespace ("" for none) and the info it gave
		public string Namespace => lcStageScript?.StageNamespace ?? "";
		internal Dictionary<object, object>? NamespaceInfo => lcStageScript?.StageNamespaceInfo;

		// everything the stage runs is pushed in the resolution it declared (STAGES:SetResolution); false when it
		// declared none
		private bool PushResolution() {
			int w = lcStageScript?.ResolutionWidth ?? 0, h = lcStageScript?.ResolutionHeight ?? 0;
			if (w <= 0 || h <= 0) return false;
			FDK.Game.PushLogicalResolution(w, h);
			return true;
		}

		private static void PopResolution(bool pushed) {
			if (pushed) FDK.Game.PopLogicalResolution();
		}

		public void DisposeStage() {
			lcStageScript?.Dispose();
		}

		public LuaStageWrapper(string name, bool isGlobal = false) {
			base.eStageID = EStage.CUSTOM;
			base.ePhaseID = CStage.EPhase.Common_NORMAL;
			base.customStageName = name;
			IsGlobal = isGlobal;

			if (isGlobal == false) lcStageScript = new CLuaStageScript(CSkin.Path($"Modules/Stages/{name}"), name);
			else lcStageScript = new CLuaStageScript(CSkin.GlobalModulePath("Stages", name), $"[GLOBAL]{name}");
			lcStageScript.AttachExitCallBack(RequestExitStage);

			(isGlobal ? _globalLuaStages : _skinLuaStages).Add(name, this);

			base.ChildActivities.Add(this.actFOtoTitle = new CActFIFOBlack());
		}

		// "heya" opens the "myroom" stage (a global stage looks in Global/Modules/Stages first), or the title when there is none
		private EReturnValue _MyRoomOrTitle() {
			if (GetLuaStage("myroom", IsGlobal) == null)
				return EReturnValue.BackToTitle;
			ForceSetNextRequestedStage("myroom", IsGlobal);
			return EReturnValue.JumpToLuaStage;
		}

		private EReturnValue _StringToLegacyValue(string rv) {
			return rv switch {
				"heya" => _MyRoomOrTitle(),
				"config" => EReturnValue.CONFIG,
				"exit" => EReturnValue.EXIT,
				"onlinelounge" => EReturnValue.ONLINELOUNGE,
				_ => EReturnValue.BackToTitle
			};
		}

		private EReturnValue _StringToReturnValue(string transition, string? moduleName = null) {
			EReturnValue _rv = transition switch {
				"title" => EReturnValue.BackToTitle,
				"play" => EReturnValue.SongSelected,
				"stage" => EReturnValue.JumpToLuaStage,
				_ => EReturnValue.BackToTitle
			};

			if (moduleName != null) {
				if (transition == "stage") {
					ForceSetNextRequestedStage(moduleName, IsGlobal);
				} else if (transition == "legacy") {
					return _StringToLegacyValue(moduleName);
				}
			}

			return _rv;
		}

		private bool _exitImmediate = false;

		private int RequestExitStage(string target, string? moduleName, string? transitionName) {
			this.eFadeOutReturnValue = _StringToReturnValue(target, moduleName);

			var tr = LuaTransitionWrapper.Get(transitionName, IsGlobal);
			if (tr != null) {
				// Hand off this frame so CStageTransition plays the fade-out/loading/fade-in (skip the legacy fade).
				CStageTransition.SetPendingScript(this, tr);
				_exitImmediate = true;
			} else {
				// No transition modules in this skin — fall back to the legacy black fade-out.
				this.actFOtoTitle.tFadeOutStart();
				base.ePhaseID = CStage.EPhase.Common_FADEOUT;
			}
			return 0;
		}

		#region [Override events]

		// Synchronous activate (non-transition mounts): begin + drain + finish in one call.
		public override void Activate() {
			if (base.IsActivated)
				return;
			BeginActivate();
			while (StepActivate(out _)) { }
			FinishActivate();
		}

		// Incremental activate, driven by CStageTransition so a heavy activate spreads across frames behind the
		// loading bar. BeginActivate → StepActivate each frame until false → FinishActivate (framework activate).
		internal void BeginActivate() {
			if (base.IsActivated) { _activating = false; return; }
			base.ePhaseID = CStage.EPhase.Common_NORMAL;
			this.eFadeOutReturnValue = EReturnValue.Continuation;
			_exitImmediate = false;
			bool pushed = PushResolution();
			try { lcStageScript?.BeginActivate(); } finally { PopResolution(pushed); }
			_activating = true;
		}
		internal bool StepActivate(out float progress) {
			progress = 1f;
			if (!_activating || lcStageScript == null) { _activating = false; return false; }
			bool more;
			bool pushed = PushResolution();
			try { more = lcStageScript.StepActivate(out progress); } finally { PopResolution(pushed); }
			if (!more) _activating = false;
			return more;
		}
		internal void FinishActivate() {
			if (!base.IsActivated) base.Activate();
		}
		private bool _activating = false;

		public override void DeActivate() {
			bool pushed = PushResolution();
			try { lcStageScript?.Deactivate(); } finally { PopResolution(pushed); }

			base.DeActivate();
		}

		public override int Draw() {
			bool pushed = PushResolution();
			try {
				if (this.eFadeOutReturnValue == EReturnValue.Continuation) lcStageScript?.Update(FDK.Game.TimeMs);
				lcStageScript?.Draw();
			} finally { PopResolution(pushed); }

			// A transition was requested: hand off this frame (CStageTransition then drives the fade/loading).
			if (_exitImmediate) {
				_exitImmediate = false;
				return (int)this.eFadeOutReturnValue;
			}

			// Legacy exit fade out (no transition module in this skin)
			switch (base.ePhaseID) {
				case CStage.EPhase.Common_FADEOUT:
					if (this.actFOtoTitle.Draw() == 0) {
						break;
					}
					return (int)this.eFadeOutReturnValue;

			}
			return 0;
		}

		#endregion

		#region [Events not present on CStage/CActivity]

		// Incremental onStart (run just after the skin loads + on every reload): the engine calls BeginOnStart()
		// then StepOnStart() each frame until it returns false, so a heavy onStart spreads across frames.
		internal void BeginOnStart() {
			bool pushed = PushResolution();
			try { lcStageScript?.BeginOnStart(); } finally { PopResolution(pushed); }
		}

		internal bool StepOnStart(out float progress) {
			if (lcStageScript == null) { progress = 0f; return false; }
			bool pushed = PushResolution();
			try { return lcStageScript.StepOnStart(out progress); } finally { PopResolution(pushed); }
		}


		// Executes everytime songs enum is done, including soft/hard reload and at start, **Even** if the stage is not activated
		private void AfterSongsEnum() {
			lcStageScript?.AfterSongsEnum();
		}

		// Executes before skin change, in order to deallocate any ressources carried by the skin's Lua modules
		private void OnDestroy() {
			lcStageScript?.OnDestroy();
		}


		#endregion


		#region [Private]


		public EReturnValue eFadeOutReturnValue;
		public CActFIFOBlack actFOtoTitle;

		#endregion
	}
}
