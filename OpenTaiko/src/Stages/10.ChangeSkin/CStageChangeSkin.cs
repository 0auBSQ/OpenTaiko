using System.Diagnostics;


namespace OpenTaiko;

/// <summary>
/// box.defによるスキン変更時に一時的に遷移する、スキン画像の一切無いステージ。
/// </summary>
internal class CStageChangeSkin : CStage {
	// Constructor

	public CStageChangeSkin() {
		base.eStageID = CStage.EStage.ChangeSkin;
		base.IsDeActivated = true;
	}


	// CStage 実装

	public override void Activate() {
		Trace.TraceInformation("スキン変更ステージを活性化します。");
		Trace.Indent();
		try {
			base.ePhaseID = CStage.EPhase.Common_NORMAL;
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
			this.SavedPreviousStage = null;
			this.IsPreviousStageSaved = false;
			OpenTaiko.tDisposeSafely(ref Background);

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
				//スキン変更処理
				void task() {
					OpenTaiko.app.ChangeSkin();
					var background = new ScriptBG(CSkin.Path($"{TextureLoader.BASE}{TextureLoader.STARTUP}Script.lua"));
					background.Init();
					this.Background = background;
					OpenTaiko.app.LoadSkin();

					this.ePhaseID = EPhase.Common_FADEOUT;
				}

				if (OpenTaiko.ConfigIni.ASyncTextureLoad) {
					Task.Run(task);
				} else {
					task();
				}

				base.IsFirstDraw = false;
				return 0;
			}

			Background?.Update();
			Background?.Draw();

			if (ePhaseID == EPhase.Common_FADEOUT) { // reload completed
				return 1;
			}
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

	public void SavePreviousStage(CStage? previousStage) {
		this.SavedPreviousStage = previousStage;
		this.IsPreviousStageSaved = true;
	}

	public CStage? SavedPreviousStage { get; private set; }
	public bool IsPreviousStageSaved { get; private set; }

	#region [ private ]
	private ScriptBG? Background;
	#endregion
}
