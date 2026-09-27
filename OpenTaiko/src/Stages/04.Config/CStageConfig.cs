using System.Diagnostics;
using FDK;

namespace OpenTaiko;

internal class CStageConfig : CStage {
	// Properties

	public CActCalibrationMode actCalibrationMode;


	// Constructor

	public CStageConfig() {
		base.eStageID = CStage.EStage.Config;
		base.ePhaseID = CStage.EPhase.Common_NORMAL;
		base.ChildActivities.Add(this.actFIFO = new CActFIFOWhite());
		// The config UI is the Lua config_ui ROActivity (see Draw). The old C# config screen
		// (CActConfigList / CActConfigKeyAssign / CActDFPFont) has been removed entirely.
		base.ChildActivities.Add(this.actCalibrationMode = new CActCalibrationMode());
		base.IsDeActivated = true;
	}


	// CStage 実装

	public override void Activate() {
		Trace.TraceInformation("コンフィグステージを活性化します。");
		Trace.Indent();
		// a failed save on the last exit can leave the stage activated, so the first-draw reset may not run
		_leaving = false;
		base.ePhaseID = CStage.EPhase.Common_NORMAL;
		try {
			OpenTaiko.Skin.bgmConfigScreen.tPlay();

			// snapshot the entry values whose heavy apply is deferred to exit (skin / render scale / sound device)
			_skinOrg = OpenTaiko.Skin.GetCurrentSkinSubfolderFullName(true);
			_renderOrg = OpenTaiko.ConfigIni.fRenderScale;
			_soundTypeOrg = OpenTaiko.ConfigIni.nSoundDeviceType;
			_bassBufOrg = OpenTaiko.ConfigIni.nBassBufferSizeMs;
			_wasapiBufOrg = OpenTaiko.ConfigIni.nWASAPIBufferSizeMs;
			_asioOrg = OpenTaiko.ConfigIni.nASIODevice;
			_osTimerOrg = OpenTaiko.ConfigIni.bUseOSTimer;

			RebuildModel();
			UI?.Activate(_model);   // hand the schema to the Lua config_ui ROActivity (it draws its own calm gradient bg)
		} finally {
			Trace.TraceInformation("コンフィグステージの活性化を完了しました。");
			Trace.Unindent();
		}
		base.Activate();        // 2011.3.14 yyagi: On活性化()をtryの中から外に移動
	}

	private static LuaROActivityWrapper? UI => LuaROActivityWrapper.GetROActivity("config_ui");
	private CLuaConfigModel _model;
	private CConfigOptionBuilder.Hooks _hooks;
	private string _skinOrg;
	private float _renderOrg;
	private int _soundTypeOrg, _bassBufOrg, _wasapiBufOrg, _asioOrg;
	private bool _osTimerOrg;

	private void RebuildModel() {
		if (_hooks == null) {
			_hooks = new CConfigOptionBuilder.Hooks {
				// any other settings or texts changed → rebuild model + push reload to Lua
				Refresh = () => { _model = CConfigOptionBuilder.Build(_hooks); UI?.Call("reload", _model); },
				Calibration = (onDone) => actCalibrationMode.Start(onDone),
				ReloadSongs = () => tReloadSongs(false),
				HardReloadSongs = () => tReloadSongs(true),
				ImportScore = () => { try { new System.Threading.Thread(CScoreIni_Importer.ImportScoreInisToSavesDb3).Start(); } catch (Exception e) { Trace.TraceError(e.ToString()); } },
			};
		}
		_model = CConfigOptionBuilder.Build(_hooks);
	}

	private void tReloadSongs(bool hard) {
		if (OpenTaiko.EnumSongs.IsEnumerating) {
			OpenTaiko.EnumSongs.Abort();
			OpenTaiko.actEnumSongs.DeActivate();
		}
		OpenTaiko.EnumSongs.StartEnumFromDisk(hard);
		OpenTaiko.EnumSongs.ChangeEnumeratePriority(System.Threading.ThreadPriority.Normal);
		OpenTaiko.actEnumSongs.bCommandSongDataGet = true;
		OpenTaiko.actEnumSongs.Activate();
	}

	private bool tSoundDeviceChanged() {
		var cfg = OpenTaiko.ConfigIni;
		if (OperatingSystem.IsWindows())
			return _soundTypeOrg != cfg.nSoundDeviceType || _bassBufOrg != cfg.nBassBufferSizeMs ||
				_wasapiBufOrg != cfg.nWASAPIBufferSizeMs || _asioOrg != cfg.nASIODevice || _osTimerOrg != cfg.bUseOSTimer;
		return _bassBufOrg != cfg.nBassBufferSizeMs || _osTimerOrg != cfg.bUseOSTimer;
	}

	// the skin, render scale or sound device changed: leaving reloads them (in DeActivate)
	private bool tHeavyApplyPending()
		=> OpenTaiko.Skin.GetCurrentSkinSubfolderFullName(true) != _skinOrg
		   || OpenTaiko.ConfigIni.fRenderScale != _renderOrg || tSoundDeviceChanged();

	private void tApplySoundDeviceIfChanged() {
		if (!tSoundDeviceChanged()) return;
		var cfg = OpenTaiko.ConfigIni;
		if (OperatingSystem.IsWindows()) {
			ESoundDeviceType t = cfg.nSoundDeviceType switch {
				0 => ESoundDeviceType.Bass, 1 => ESoundDeviceType.ASIO,
				2 => ESoundDeviceType.ExclusiveWASAPI, 3 => ESoundDeviceType.SharedWASAPI, _ => ESoundDeviceType.Unknown,
			};
			OpenTaiko.SoundManager.tInitialize(t, cfg.nBassBufferSizeMs, cfg.nWASAPIBufferSizeMs, 0, cfg.nASIODevice, cfg.bUseOSTimer);
			OpenTaiko.app.ShowWindowTitle();
			OpenTaiko.Skin.ReloadSystemSounds();
			OpenTaiko.Skin.PreloadSystemSounds();
		} else {
			OpenTaiko.SoundManager.tInitialize(ESoundDeviceType.Bass, cfg.nBassBufferSizeMs, 0, 0, 0, cfg.bUseOSTimer);
		}
	}
	public override void DeActivate() {
		Trace.TraceInformation("コンフィグステージを非活性化します。");
		Trace.Indent();
		try {
			OpenTaiko.Skin.bgmConfigScreen.tStop();

			UI?.Deactivate();

			OpenTaiko.ConfigIni.tExport(OpenTaiko.strEXEFolder + "Config.ini");    // save on exit

			// deferred heavy apply (the live writes already landed in ConfigIni; here we reload what's expensive)
			if (OpenTaiko.Skin.GetCurrentSkinSubfolderFullName(true) != _skinOrg
				|| OpenTaiko.ConfigIni.fRenderScale != _renderOrg) {
				OpenTaiko.app.EnterRefreshSkinStage(isSavedBeforeUpdate: true);   // resolution change reloads the skin too
			}
			tApplySoundDeviceIfChanged();
			FDK.SoundManager.bIsTimeStretch = OpenTaiko.ConfigIni.bTimeStretch;

			for (int i = 0; i < OpenTaiko.MAX_PLAYERS; i++) {
				int id = OpenTaiko.SaveFileInstances[i].data.TitleId;
				if (id > 0) {
					var title = OpenTaiko.Databases.DBNameplateUnlockables.data[id];
					OpenTaiko.SaveFileInstances[i].data.Title = title.nameplateInfo.cld.GetString("");
				}
				OpenTaiko.NamePlate.tNamePlateRefreshTitles(i);
			}

			base.DeActivate();
		} catch (UnauthorizedAccessException e) {
			Trace.TraceError(e.ToString());
			Trace.TraceError("ファイルが読み取り専用になっていないか、管理者権限がないと書き込めなくなっていないか等を確認して下さい");
			Trace.TraceError("例外が発生しましたが処理を継続します。 (7a61f01b-1703-4aad-8d7d-08bd88ae8760)");
		} catch (Exception e) {
			Trace.TraceError(e.ToString());
			Trace.TraceError("例外が発生しましたが処理を継続します。 (83f0d93c-bb04-4a19-a596-bc32de39f496)");
		} finally {
			Trace.TraceInformation("コンフィグステージの非活性化を完了しました。");
			Trace.Unindent();
		}
	}

	public override int Draw() {
		if (base.IsDeActivated)
			return 0;

		if (base.IsFirstDraw) {
			_leaving = false;
			if (OpenTaiko.rCurrentStage is CStageTransition) {
				base.ePhaseID = CStage.EPhase.Common_NORMAL;   // a transition is revealing the settings: no fade of its own
			} else {
				base.ePhaseID = CStage.EPhase.Common_FADEIN;
				this.actFIFO.tFadeInStart();
			}
			base.IsFirstDraw = false;
		}

		if (actCalibrationMode.IsStarted) {
			// the calibration tap-test owns the screen + input until it finishes
			if (OpenTaiko.Skin.bgmConfigScreen.bIsPlaying)
				OpenTaiko.Skin.bgmConfigScreen.tStop();
			actCalibrationMode.Update();
			actCalibrationMode.Draw();
		} else {
			if (!OpenTaiko.Skin.bgmConfigScreen.bIsPlaying)
				OpenTaiko.Skin.bgmConfigScreen.tPlay();

			if (base.ePhaseID == CStage.EPhase.Common_NORMAL && !_leaving) {
				if (_model != null && _model.Keys.IsCapturing) {
					// C# owns input this frame: poll the device sweep for the key being bound
					var (done, needRefresh) = _model.Keys.PollCaptureFrame();
					if (needRefresh)
						_hooks.Refresh();
				} else {
					// Lua handles nav/edit/cancel; returns "exit" at the top level, optionally with a transition name
					// (like a Lua stage's Exit). A pending skin/sound reload keeps the plain fade.
					var r = UI?.Update();
					if (r != null && r.Length > 0 && (r[0] as string) == "exit") {
						string? name = r.Length > 1 ? r[1] as string : null;
						var tr = !string.IsNullOrEmpty(name) && !tHeavyApplyPending() ? LuaTransitionWrapper.Get(name) : null;
						if (tr != null) {
							CStageTransition.SetPendingScript(this, tr);
							_leaving = true;   // drawn (not updated) while the transition covers it
						} else {
							this.actFIFO.tFadeOutStart();
							base.ePhaseID = CStage.EPhase.Common_FADEOUT;
						}
					}
				}
			}
			UI?.Draw();
			if (_leaving && OpenTaiko.rCurrentStage == this) return 1;   // hand over to the transition this frame
		}

		switch (base.ePhaseID) {
			case CStage.EPhase.Common_FADEIN:
				if (this.actFIFO.Draw() != 0)
					base.ePhaseID = CStage.EPhase.Common_NORMAL;
				break;

			case CStage.EPhase.Common_FADEOUT:
				if (this.actFIFO.Draw() == 0)
					break;
				return 1;
		}
		return 0;
	}


	// その他

	#region [ private ]
	//-----------------
	private CActFIFOWhite actFIFO;
	private bool _leaving;   // handed over to a transition: draw only, no input
	//-----------------
	#endregion
}
