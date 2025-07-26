namespace OpenTaiko.src.Lua {
	internal class LuaStageWrapper : CStage {
		// Used to toggle stages in the OpenTaiko.cs file
		public static Dictionary<string, LuaStageWrapper> _allLuaStages = new Dictionary<string, LuaStageWrapper>();

		private CLuaStageScript lcStageScript;

		public LuaStageWrapper(string name) {
			base.eStageID = EStage.CUSTOM;
			base.ePhaseID = CStage.EPhase.Common_NORMAL;

			lcStageScript = new CLuaStageScript(CSkin.Path($"Modules/{name}"), name);
			lcStageScript.AttachExitCallBack(RequestExitStage);

			_allLuaStages.Add(name, this);

			base.ChildActivities.Add(this.actFOtoTitle = new CActFIFOBlack());
		}

		private CStageSongSelect.EReturnValue _StringToReturnValue(string transition) {
			CStageSongSelect.EReturnValue _rv = transition switch {
				"title" => CStageSongSelect.EReturnValue.BackToTitle,
				"play" => CStageSongSelect.EReturnValue.SongSelected,
				_ => CStageSongSelect.EReturnValue.BackToTitle
			};

			return _rv;
		}

		private int RequestExitStage(string transition) {
			this.eFadeOutReturnValue = _StringToReturnValue(transition);
			this.actFOtoTitle.tフェードアウト開始();
			base.ePhaseID = CStage.EPhase.Common_FADEOUT;
			return 0;
		}

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
			lcStageScript?.Update();
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


		#region [Private]


		public CStageSongSelect.EReturnValue eFadeOutReturnValue;
		public CActFIFOBlack actFOtoTitle;

		#endregion
	}
}
