using FDK;

namespace OpenTaiko {
	internal class CActResultRank : CActivity {
		// コンストラクタ

		public CActResultRank() {
			base.IsDeActivated = true;
		}

		// CActivity 実装

		public override void Activate() {

			base.Activate();
		}
		public override void DeActivate() {

			base.DeActivate();
		}
		public override void CreateManagedResource() {
			if (!base.IsDeActivated) {

				base.CreateManagedResource();
			}
		}
		public override void ReleaseManagedResource() {
			if (!base.IsDeActivated) {

				base.ReleaseManagedResource();
			}
		}
		public override int Draw() {
			if (base.IsDeActivated) {
				return 0;
			}
			if (base.IsFirstDraw) {
				base.IsFirstDraw = false;
			}

			return 1;
		}


		// その他

		#region [ private ]
		//-----------------
		//-----------------
		#endregion
	}
}
