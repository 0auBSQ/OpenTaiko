// Minimalist menu class to use for custom menus
namespace OpenTaiko {
	class CStageTemplate : CStage {
		public CStageTemplate() {
			base.eStageID = EStage.TEMPLATE;
			base.ePhaseID = CStage.EPhase.Common_NORMAL;

			// Load CActivity objects here
			// base.list子Activities.Add(this.act = new CAct());

			base.ChildActivities.Add(this.actFOtoTitle = new CActFIFOBlack());

		}

		public override void Activate() {
			// On activation

			if (base.IsActivated)
				return;

			base.ePhaseID = CStage.EPhase.Common_NORMAL;
			this.eフェードアウト完了時の戻り値 = EReturnValue.Continuation;



			base.Activate();
		}

		public override void DeActivate() {
			// On de-activation

			base.DeActivate();
		}

		public override void CreateManagedResource() {
			// Ressource allocation

			base.CreateManagedResource();
		}

		public override void ReleaseManagedResource() {
			// Ressource freeing

			base.ReleaseManagedResource();
		}

		public override int Draw() {





			// Menu exit fade out transition
			switch (base.ePhaseID) {
				case CStage.EPhase.Common_FADEOUT:
					if (this.actFOtoTitle.Draw() == 0) {
						break;
					}
					return (int)this.eフェードアウト完了時の戻り値;

			}

			return 0;
		}

		#region [Private]


		public EReturnValue eフェードアウト完了時の戻り値;
		public CActFIFOBlack actFOtoTitle;

		#endregion
	}
}
