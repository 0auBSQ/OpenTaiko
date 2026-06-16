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
			if (_loading || _draining) {
				System.Diagnostics.Trace.AutoFlush = _savedAutoFlush;
				CAsyncLoad.CancelPhase();
			}
			_loading = false;
			_draining = false;
			_skinLoad = null;

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
				var background = new ScriptBG(CSkin.Path($"{TextureLoader.BASE}{TextureLoader.STARTUP}Script.lua"));
				background.Init();
				this.Background = background;

				// Drive the module (re)load incrementally on the RENDER THREAD with the loading bar — never on
				// a worker thread (Lua is thread-affine; the old Task.Run path could corrupt the VM). The
				// modules' onStart textures stream in (async decode) + the per-texture trace flush is batched.
				CLoadingProgress.Begin();
				_skinLoad = OpenTaiko.app.LoadSkinBegin();
				_loading = true;
				CAsyncLoad.BeginPhase();
				_savedAutoFlush = System.Diagnostics.Trace.AutoFlush;
				System.Diagnostics.Trace.AutoFlush = false;

				base.IsFirstDraw = false;
				return 0;
			}

			Background?.Update();
			Background?.Draw();
			CLoadingScreen.Draw();   // engine loading bar overlay

			if (_loading) {
				long _t0 = System.Diagnostics.Stopwatch.GetTimestamp();
				while (System.Diagnostics.Stopwatch.GetElapsedTime(_t0).TotalMilliseconds < 10.0) {
					if (!_skinLoad.MoveNext()) {
						_loading = false;
						CAsyncLoad.StartDecode();   // begin decoding the queued onStart textures (off-thread)
						_draining = true;
						break;
					}
					CLoadingProgress.Report(0.55f * _skinLoad.Current);
				}
				CAsyncLoad.Pump(8.0);
			} else if (_draining) {
				CAsyncLoad.Pump(8.0);
				CLoadingProgress.Report(0.55f + 0.40f * CAsyncLoad.Fraction);
				if (CAsyncLoad.Complete) {
					_draining = false;
					CAsyncLoad.EndPhase();
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
	private ScriptBG? Background;
	private System.Collections.Generic.IEnumerator<float>? _skinLoad;   // incremental module (re)loader
	private bool _loading;
	private bool _draining;            // finishing the streamed onStart-texture uploads
	private bool _savedAutoFlush;      // Trace.AutoFlush state to restore after the batched reload
	#endregion
}
