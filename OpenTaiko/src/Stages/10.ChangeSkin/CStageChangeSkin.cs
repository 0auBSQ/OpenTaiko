using System.Diagnostics;
using FDK;


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

			// Safety: if torn down mid-load, undo the batched-trace + async-load state so it can't leak.
			if (_session != null) {
				System.Diagnostics.Trace.AutoFlush = _savedAutoFlush;
				_session.Cancel();
				_session = null;
			}

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
				// Non-Lua skin swap (dispose old skin, new CSkin, resolution, console) + the _boot background.
				OpenTaiko.app.ChangeSkin();
				var background = new LuaBackgroundWrapper(CSkin.Path($"{TextureLoader.BASE}{TextureLoader.STARTUP}"));
				background.Activate(_state.Refreshed());
				this.Background = background;

				// Drive the module (re)load incrementally on the RENDER THREAD with the loading bar via the shared
				// CLoadSession — never on a worker thread (Lua is thread-affine; the old Task.Run path could
				// corrupt the VM). The session streams the modules' onStart textures (async decode); the
				// per-texture trace flush is batched here.
				CLoadingProgress.Begin();
				_session = new CLoadSession(new EnumeratorStep(OpenTaiko.app.LoadSkinBegin()));
				_session.Begin();
				_savedAutoFlush = System.Diagnostics.Trace.AutoFlush;
				System.Diagnostics.Trace.AutoFlush = false;

				base.IsFirstDraw = false;
				return 0;
			}

			Background?.Update(_state);
			Background?.Draw(_state);
			CLoadingScreen.Draw();   // engine loading bar overlay

			if (_session != null) {
				bool more = _session.Step();
				// 0-55%: module onStart;  55-95%: streamed onStart-texture upload drain.
				CLoadingProgress.Report(_session.SourceDone ? 0.55f + 0.40f * _session.AssetFraction
				                                            : 0.55f * _session.SourceProgress);
				if (!more) {
					_session.End();
					_session = null;
					System.Diagnostics.Trace.AutoFlush = _savedAutoFlush;
					System.Diagnostics.Trace.Flush();
					OpenTaiko.app.LoadSkinFinish();   // Tx textures + AfterSongEnum + Resume + RefleshSkin(s)
					CLoadingProgress.End();
					this.ePhaseID = EPhase.Common_FADEOUT;
				}
			}

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
	private LuaBackgroundWrapper? Background;
	private readonly LuaBackgroundState _state = new();
	private CLoadSession? _session;    // drives the incremental module (re)load + onStart-texture stream
	private bool _savedAutoFlush;      // Trace.AutoFlush state to restore after the batched reload
	#endregion
}
