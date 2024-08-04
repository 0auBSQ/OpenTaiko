using System.Diagnostics;


namespace OpenTaiko {
	/// <summary>
	/// box.defによるスキン変更時に一時的に遷移する、スキン画像の一切無いステージ。
	/// </summary>
	internal class CStageChangeSkin : CStage {
		// コンストラクタ

		public CStageChangeSkin() {
			base.eStageID = CStage.EStage.ChangeSkin;
			base.IsDeActivated = true;
		}


		// CStage 実装

		public override void Activate() {
			Trace.TraceInformation("スキン変更ステージを活性化します。");
			Trace.Indent();
			try {
				base.Activate();
				Trace.TraceInformation("スキン変更ステージの活性化を完了しました。");
			} finally {
				Trace.Unindent();
			}
		}
		public override void DeActivate() {
			Trace.TraceInformation("スキン変更ステージを非活性化します。");
			Trace.Indent();
			try {
				base.DeActivate();
				Trace.TraceInformation("スキン変更ステージの非活性化を完了しました。");
			} finally {
				Trace.Unindent();
			}
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
			if (!base.IsDeActivated) {
				if (base.IsFirstDraw) {
					base.IsFirstDraw = false;
					return 0;
				}

				//スキン変更処理
				OpenTaiko.app.RefreshSkin();

				return 1;
			}
			return 0;
		}
		//public void tChangeSkinMain()
		//{
		//	Trace.TraceInformation( "スキン変更:" + CDTXMania.Skin.GetCurrentSkinSubfolderFullName( false ) );

		//	CDTXMania.act文字コンソール.On非活性化();

		//	CDTXMania.Skin.PrepareReloadSkin();
		//	CDTXMania.Skin.ReloadSkin();


		//          CDTXMania.Tx.DisposeTexture();
		//          CDTXMania.Tx.LoadTexture();

		//	CDTXMania.act文字コンソール.On活性化();
		//}
	}
}
