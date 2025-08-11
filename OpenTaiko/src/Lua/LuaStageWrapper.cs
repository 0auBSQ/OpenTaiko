namespace OpenTaiko {
	internal class LuaStageWrapper : CStage {
		// Used to toggle stages in the OpenTaiko.cs file
		public static Dictionary<string, LuaStageWrapper> _allLuaStages = new Dictionary<string, LuaStageWrapper>();

		private static string _nextRequestedStage = "";

		#region [Setters]

		// Necessary as long as the title screen is not Lua-ified, to deprecate as soon as possible
		public static void TEMPORARY_ForceSetNextRequestedStage(string name) {
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

		public static void PropagateOnStart() {
			foreach (KeyValuePair<string, LuaStageWrapper> _stage in _allLuaStages) {
				_stage.Value.OnStart();
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

		public LuaStageWrapper(string name) {
			base.eStageID = EStage.CUSTOM;
			base.ePhaseID = CStage.EPhase.Common_NORMAL;

			lcStageScript = new CLuaStageScript(CSkin.Path($"Modules/Stages/{name}"), name);
			lcStageScript.AttachExitCallBack(RequestExitStage);

			_allLuaStages.Add(name, this);

			base.ChildActivities.Add(this.actFOtoTitle = new CActFIFOBlack());
		}

		private CStageSongSelect.EReturnValue _StringToReturnValue(string transition, string? moduleName = null) {
			CStageSongSelect.EReturnValue _rv = transition switch {
				"title" => CStageSongSelect.EReturnValue.BackToTitle,
				"play" => CStageSongSelect.EReturnValue.SongSelected,
				"stage" => CStageSongSelect.EReturnValue.JumpToLuaStage,
				_ => CStageSongSelect.EReturnValue.BackToTitle
			};

			if (moduleName != null) _nextRequestedStage = moduleName;

			return _rv;
		}

		private int RequestExitStage(string transition, string? moduleName) {
			this.eFadeOutReturnValue = _StringToReturnValue(transition, moduleName);
			this.actFOtoTitle.tフェードアウト開始();
			base.ePhaseID = CStage.EPhase.Common_FADEOUT;
			return 0;
		}

		#region [Override events]

		public override void Activate() {
			if (base.IsActivated)
				return;

			base.ePhaseID = CStage.EPhase.Common_NORMAL;
			this.eFadeOutReturnValue = CStageSongSelect.EReturnValue.Continuation;

			lcStageScript?.Activate();

			base.Activate();
		}

		public override void DeActivate() {
			lcStageScript?.Deactivate();

			base.DeActivate();
		}

		public override int Draw() {
			if (this.eFadeOutReturnValue == CStageSongSelect.EReturnValue.Continuation) lcStageScript?.Update();
			lcStageScript?.Draw();

			// Menu exit fade out transition
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

		// Executes **Just after** loading the skin, once the readme notice appears, also executes everytime the skin is reloaded
		public void OnStart() {
			lcStageScript?.OnStart();
		}


		// Executes everytime songs enum is done, including soft/hard reload and at start, **Even** if the stage is not activated
		public void AfterSongsEnum() {
			lcStageScript?.AfterSongsEnum();
		}

		// Executes before skin change, in order to deallocate any ressources carried by the skin's Lua modules
		public void OnDestroy() {
			lcStageScript?.OnDestroy();
		}


		#endregion


		#region [Private]


		public CStageSongSelect.EReturnValue eFadeOutReturnValue;
		public CActFIFOBlack actFOtoTitle;

		#endregion
	}
}
