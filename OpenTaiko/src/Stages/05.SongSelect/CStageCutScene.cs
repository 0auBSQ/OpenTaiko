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
			this.LoadCutScenes(OpenTaiko.rPreviousStage);

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

		this.actAVI.Pause();
	}

	public void Resume() {
		this.isPause = false;

		OpenTaiko.Timer.Resume();
		SoundManager.PlayTimer.NowTimeMs = OpenTaiko.Timer.NowTimeMs;
		SoundManager.PlayTimer.Resume();

		this.actAVI.Resume();
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
			this.cutScenes = [..selectedSong.CutSceneOutros];
		}
		this.cutScenes.RemoveAll(x => !this.JudgeRequirement(x));
		return this.cutScenes.Count > 0;
	}

	private bool JudgeRequirement(CTja.CutSceneDef cutScene) {
		if (OpenTaiko.ConfigIni.bAutoPlay[0]) {
			return false; // no human player, no cut scene, no repeat status
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
				// TODO: Update repeat status
				return false;
			}
		}

		// TODO: Judge by repeat status and update repeat status
		return true;
	}

	public override void DeActivate() {
		// On de-activation

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
			this.rVD?.Dispose();
			while (++this.iCutScene < this.cutScenes!.Count) {
				var cutScene = this.cutScenes[this.iCutScene];
				if (this.LoadCutSceneAVI(cutScene)) {
					this.actAVI.Start(this.rVD!, true);
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
					this.actFOIntro.tフェードアウト開始();
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
			this.rVD = new CVideoDecoder(cutScene.FullPath);
			this.rVD.InitRead();
			this.rVD.dbPlaySpeed = 1;
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

	private bool isPause;
	private CActCutScenePauseMenu actPauseMenu;

	#endregion
}
