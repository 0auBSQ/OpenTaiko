using System.Diagnostics;
using FDK;

namespace OpenTaiko;

class CStageCutScene : CStage {
	public CStageCutScene() {
		base.eStageID = EStage.CutScene;
		base.ePhaseID = CStage.EPhase.Common_NORMAL;

		// Load CActivity objects here
		// base.list子Activities.Add(this.act = new CAct());

		base.ChildActivities.Add(this.actAVI = new());
		base.ChildActivities.Add(this.actFOIntro = new());
		base.ChildActivities.Add(this.actFOOutro = new());
		base.ChildActivities.Add(this.actPauseMenu = new());
	}

	public override void Activate() {
		// On activation

		if (base.IsActivated)
			return;

		if (this.cutScenes == null)
			this.cutScenes = [];
		//this.LoadCutScenes(OpenTaiko.rPreviousStage);

		this.iCutScene = -1;

		base.ePhaseID = CStage.EPhase.Common_NORMAL;
		this.ReturnValueAfterFadingOut = EReturnValue.Continue;

		this.isPause = false;

		base.Activate();
	}

	public void Pause() {
		this.isPause = true;

		SoundManager.PlayTimer.Pause();
		OpenTaiko.Timer.Pause();

		this.sound?.Pause();
		this.actAVI.Pause();
	}

	public void Resume() {
		this.isPause = false;

		OpenTaiko.Timer.Resume();
		SoundManager.PlayTimer.NowTimeMs = OpenTaiko.Timer.NowTimeMs;
		SoundManager.PlayTimer.Resume();

		this.actAVI.Resume();
		if (this.rVD != null) {
			this.sound?.Resume((long)this.rVD.msPlayPosition);
		}
	}

	public void Skip() {
		this.isPause = false;

		OpenTaiko.Timer.Resume();
		SoundManager.PlayTimer.NowTimeMs = OpenTaiko.Timer.NowTimeMs;
		SoundManager.PlayTimer.Resume();

		this.actAVI.Stop(); // only skip one video
	}

	public bool LoadCutScenes(CStage stageLast) {
		var selectedSong = OpenTaiko.stageSongSelect.rChoosenSong;
		if (stageLast == OpenTaiko.stageSongSelect
			|| stageLast == OpenTaiko.stageDanSongSelect
			|| stageLast == OpenTaiko.stageTowerSelect
			) {
			this.mode = ECutSceneMode.Intro;
			this.cutScenes = (selectedSong.CutSceneIntro != null) ? [selectedSong.CutSceneIntro] : [];
		} else {
			this.mode = ECutSceneMode.Outro;
			this.cutScenes = (selectedSong.CutSceneOutros != null) ? [.. selectedSong.CutSceneOutros] : [];
		}
		this.cutScenes.RemoveAll(x => !this.JudgeRequirement(x, selectedSong));
		return this.cutScenes.Count > 0;
	}

	private bool JudgeRequirement(CTja.CutSceneDef cutScene, CSongListNode? songInfo = null) {
		string fileName = Path.GetFileName(cutScene.FullPath);
		string _gTriggerName = $".regcutscene_{songInfo?.tGetUniqueId() ?? ""}_{this.mode.ToString()}_{fileName}".EscapeSingleQuotes();

		if (OpenTaiko.ConfigIni.bAutoPlay[0]) {
			return false; // no human player, no cut scene, no repeat status
		}
		if (OpenTaiko.PrimarySaveFile.tGetGlobalTrigger(_gTriggerName) == true
			&& cutScene.RepeatMode != CTja.ECutSceneRepeatMode.EverytimeMet
			) {
			return false; // disabled depending on repeat mode
		}
		if (this.mode != ECutSceneMode.Intro) {
			if (!OpenTaiko.stageResults.IsScoreValid[0]) {
				return false; // no score register, no cut scene, no repeat status
			}
			int clearstatus = (int)(OpenTaiko.stageResults.ClearStatusesSaved[0] + 1); // was -1 to 3
			int clearRequirement = (int)cutScene.ClearRequirement; // 0 to 4
			bool met = (cutScene.RequirementRange) switch {
				"l" => clearstatus < clearRequirement,
				"le" => clearstatus <= clearRequirement,
				"e" => clearstatus == clearRequirement,
				"m" => clearstatus > clearRequirement,
				"d" => clearstatus != clearRequirement,
				"me" or _ => clearstatus >= clearRequirement,
			};
			if (!met) {
				if (cutScene.RepeatMode == CTja.ECutSceneRepeatMode.UntilFirstUnmet) {
					// First Unmet => Does not play AND disable its future plays
					OpenTaiko.PrimarySaveFile.tSetGlobalTrigger(_gTriggerName, true);
				}
				return false;
			}
		}

		if (cutScene.RepeatMode == CTja.ECutSceneRepeatMode.FirstMet) {
			// First Met => Does play but disable future plays
			OpenTaiko.PrimarySaveFile.tSetGlobalTrigger(_gTriggerName, true);
		}
		return true;
	}

	public override void DeActivate() {
		// On de-activation
		this.StopSound();
		this.rVD?.Dispose();
		this.rVD = null;

		this.cutScenes?.Clear();
		this.cutScenes = null;

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
		if (!base.IsActivated)
			return 0;

		#region [ First draw (unused) ]
		if (base.IsFirstDraw) {
			base.IsFirstDraw = false;
		}
		#endregion

		this.KeyInput();

		if ((this.rVD == null || this.rVD.bFinishPlaying) && this.iCutScene < this.cutScenes!.Count) {
			while (++this.iCutScene < this.cutScenes!.Count) {
				var cutScene = this.cutScenes[this.iCutScene];
				if (this.LoadCutSceneAVI(cutScene)) {
					this.actAVI.Start(this.rVD!, true);
					this.sound?.PlayStart();
					break;
				}
			}
		}

		this.actAVI.Draw();
		this.actPauseMenu.Draw();

		if (base.ePhaseID == EPhase.Common_NORMAL && !(this.iCutScene < this.cutScenes!.Count)) {
			base.ePhaseID = EPhase.Common_FADEOUT;
			switch (this.mode) {
				case ECutSceneMode.Intro:
					this.actFOIntro.tフェードアウト開始(true);
					this.ReturnValueAfterFadingOut = EReturnValue.IntroFinished;
					break;

				case ECutSceneMode.Outro:
					this.actFOOutro.tフェードアウト開始();
					this.ReturnValueAfterFadingOut = EReturnValue.OutroFinished;
					break;
			}
		}

		#region [ Fading in/out transition ]
		switch (base.ePhaseID) {
			case CStage.EPhase.Common_FADEOUT:
				int fadeOutDrawResult = this.mode switch {
					ECutSceneMode.Intro => this.actFOIntro.Draw(),
					ECutSceneMode.Outro or _ => this.actFOOutro.Draw(),
				};
				if (fadeOutDrawResult == 0) {
					break;
				}
				return (int)this.ReturnValueAfterFadingOut;
		}
		#endregion

		return 0;
	}

	private bool LoadCutSceneAVI(CTja.CutSceneDef cutScene) {
		try {
			this.StopSound();
			this.rVD?.Dispose();
			this.rVD = new CVideoDecoder(cutScene.FullPath);
			this.rVD.Pause();
			this.rVD.InitRead();
			this.rVD.dbPlaySpeed = 1;
			this.sound = CreateSound(cutScene.FullPath);
			return true;
		} catch (Exception e) {
			Trace.TraceWarning(e.ToString() + "\n"
				+ $"Failed to load cutscene video: {cutScene.FullPath}; skipped.");
			return false;
		}
	}

	private void KeyInput() {
		IInputDevice keyboard = OpenTaiko.InputManager.Keyboard;
		if ((base.ePhaseID == CStage.EPhase.Common_NORMAL) && (
			keyboard.KeyPressed((int)SlimDXKeys.Key.Escape) || keyboard.KeyPressed((int)SlimDXKeys.Key.F1) || OpenTaiko.Pad.bPressedGB(EPad.FT)
			)) {
			if (!this.actPauseMenu.bIsActivePopupMenu && !this.isPause) {
				OpenTaiko.Skin.soundChangeSFX.tPlay();
				this.Pause();
				this.actPauseMenu.tActivatePopupMenu(0);
			}
		}
	}

	public enum EReturnValue : int {
		Continue,
		IntroFinished,
		OutroFinished,
	}

	private static CSound? CreateSound(string? filepathAVI) {
		if (string.IsNullOrEmpty(filepathAVI) || !File.Exists(filepathAVI)) {
			return null;
		}
		CSound? sound = null;
		try {
			// load video as audio
			sound = OpenTaiko.SoundManager.tCreateSound(filepathAVI, ESoundGroup.SongPlayback);
			if (sound == null)
				return null;

			// 2018-08-27 twopointzero - DO attempt to load (or queue scanning) loudness metadata here.
			//                           Initialization, song enumeration, and/or interactions may have
			//                           caused background scanning and the metadata may now be available.
			//                           If is not yet available then we wish to queue scanning.
			var loudnessMetadata = LoudnessMetadataScanner.LoadForAudioPath(filepathAVI);
			OpenTaiko.SongGainController.Set(CSound.DefaultSongVol, loudnessMetadata, sound);

			Trace.TraceInformation($"Loaded sound ({filepathAVI}) for video ({filepathAVI})");

			return sound;
		} catch (Exception e) {
			Trace.TraceError(e.ToString());
			Trace.TraceError($"Failed to load sound ({filepathAVI}) for video ({filepathAVI})");
			sound?.Dispose();
			return null;
		}
	}

	public void StopSound() {
		if (this.sound != null) {
			this.sound.Stop();
			OpenTaiko.SoundManager.tDisposeSound(this.sound);
			this.sound = null;
		}
	}


	#region [Private]

	private enum ECutSceneMode {
		Intro,
		Outro,
	}

	private ECutSceneMode mode;
	private EReturnValue ReturnValueAfterFadingOut;

	private CAct演奏AVI actAVI;
	private CActFIFOStart actFOIntro;
	private CActFIFOBlack actFOOutro;

	private List<CTja.CutSceneDef>? cutScenes;
	private int iCutScene;
	private CVideoDecoder? rVD;
	private CSound? sound;

	private bool isPause;
	private CActCutScenePauseMenu actPauseMenu;

	#endregion
}
