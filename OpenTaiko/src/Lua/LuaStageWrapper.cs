namespace OpenTaiko {
	internal class LuaStageWrapper : CStage {
		// Used to toggle stages in the OpenTaiko.cs file
		public static Dictionary<string, LuaStageWrapper> _allLuaStages = new Dictionary<string, LuaStageWrapper>();

		private static string _nextRequestedStage = "";

		#region [Setters]

		public static void ForceSetNextRequestedStage(string name) {
			_nextRequestedStage = name;
		}

		public static void ResetLuaStagesDictionary() {
			foreach (KeyValuePair<string, LuaStageWrapper> _stage in _allLuaStages) {
				_stage.Value.DisposeStage();
			}
			_allLuaStages.Clear();
		}

		#endregion

		#region [Getters]

		public static LuaStageWrapper? GetLuaStage(string name) {
			if (_allLuaStages.TryGetValue(name, out var _stage)) {
				return _stage;
			}
			return null;
		}

		public static LuaStageWrapper? GetNextRequestedStage() {
			LuaStageWrapper? _stage = GetLuaStage(_nextRequestedStage);

			if (_stage == null) {
				LogNotification.PopError($"The requested Lua Stage {_nextRequestedStage} is not present in the Modules folder");
			}
			return _stage;
		}

		public static string GetNextRequestedStageName() {
			return _nextRequestedStage;
		}

		#endregion

		#region [Executers]

		public static void PropagateAfterSongEnumEvent() {
			foreach (KeyValuePair<string, LuaStageWrapper> _stage in _allLuaStages) {
				_stage.Value.AfterSongsEnum();
			}
		}

		public static void PropagateOnDestroy() {
			foreach (KeyValuePair<string, LuaStageWrapper> _stage in _allLuaStages) {
				_stage.Value.OnDestroy();
			}
		}

		#endregion

		private CLuaStageScript lcStageScript;

		public void DisposeStage() {
			lcStageScript?.Dispose();
		}

		public LuaStageWrapper(string name, bool isGlobal = false) {
			base.eStageID = EStage.CUSTOM;
			base.ePhaseID = CStage.EPhase.Common_NORMAL;
			base.customStageName = name;

			if (isGlobal == false) lcStageScript = new CLuaStageScript(CSkin.Path($"Modules/Stages/{name}"), name);
			else lcStageScript = new CLuaStageScript(CSkin.Path($"Global/Stages/{name}"), $"[GLOBAL]{name}");
			lcStageScript.AttachExitCallBack(RequestExitStage);

			_allLuaStages.Add(name, this);

			base.ChildActivities.Add(this.actFOtoTitle = new CActFIFOBlack());
		}

		private EReturnValue _StringToLegacyValue(string rv) {
			return rv switch {
				"heya" => EReturnValue.HEYA,
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
					_nextRequestedStage = moduleName;
				} else if (transition == "legacy") {
					return _StringToLegacyValue(moduleName);
				}
			}

			return _rv;
		}

		private bool _exitImmediate = false;

		private int RequestExitStage(string target, string? moduleName, string? transitionName) {
			this.eFadeOutReturnValue = _StringToReturnValue(target, moduleName);

			var tr = LuaTransitionWrapper.Get(transitionName);
			if (tr != null) {
				// Hand off this frame so CStageTransition plays the fade-out/loading/fade-in (skip the legacy fade).
				CStageTransition.SetPendingScript(tr);
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
			lcStageScript?.BeginActivate();
			_activating = true;
		}
		internal bool StepActivate(out float progress) {
			progress = 1f;
			if (!_activating || lcStageScript == null) { _activating = false; return false; }
			bool more = lcStageScript.StepActivate(out progress);
			if (!more) _activating = false;
			return more;
		}
		internal void FinishActivate() {
			if (!base.IsActivated) base.Activate();
		}
		private bool _activating = false;

		public override void DeActivate() {
			lcStageScript?.Deactivate();

			base.DeActivate();
		}

		public override int Draw() {
			if (this.eFadeOutReturnValue == EReturnValue.Continuation) lcStageScript?.Update(DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond);
			lcStageScript?.Draw();

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
			lcStageScript?.BeginOnStart();
		}

		internal bool StepOnStart(out float progress) {
			if (lcStageScript == null) { progress = 0f; return false; }
			return lcStageScript.StepOnStart(out progress);
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
