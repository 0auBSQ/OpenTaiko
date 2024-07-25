using FDK;

namespace TJAPlayer3 {
	internal class CActSelectステータスパネル : CActivity {
		// メソッド

		public CActSelectステータスパネル() {
			base.IsDeActivated = true;
		}
		public void t選択曲が変更された() {

		}


		// CActivity 実装

		public override void Activate() {

			base.Activate();
		}
		public override void DeActivate() {

			base.DeActivate();
		}
		public override void CreateManagedResource() {
			base.CreateManagedResource();
		}
		public override void ReleaseManagedResource() {
			base.ReleaseManagedResource();
		}
		public override int Draw() {
			if (!base.IsDeActivated) {

			}
			return 0;
		}


		// その他

		#region [ private ]
		//-----------------
		//-----------------
		#endregion
	}
}
