namespace OpenTaiko.src.Lua {
	internal class LuaStageWrapper : CStage {
		private CLuaStageScript lcStageScript;

		public LuaStageWrapper(string name) {
			base.eStageID = EStage.CUSTOM;
			base.ePhaseID = CStage.EPhase.Common_NORMAL;

			lcStageScript = new CLuaStageScript(CSkin.Path($"Modules/{name}"), name);

			base.ChildActivities.Add(this.actFOtoTitle = new CActFIFOBlack());
		}

		public override void Activate() {
			if (base.IsActivated)
				return;

			base.ePhaseID = CStage.EPhase.Common_NORMAL;
			this.eFadeOutReturnValue = EReturnValue.Continuation;

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


		public EReturnValue eFadeOutReturnValue;
		public CActFIFOBlack actFOtoTitle;

		#endregion
	}
}
