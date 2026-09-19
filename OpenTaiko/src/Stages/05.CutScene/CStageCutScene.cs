using System.Diagnostics;
using FDK;

namespace OpenTaiko;

// The intro/outro cutscene stage. The videos, their audio, the pause popup and the closing fade all live in
// the Lua "cutscene" ROActivity, which knows nothing about intros or outros; this stage decides which
// cutscenes qualify (save-file triggers, clear requirements), hands their files and the fade it wants to the
// script, and turns its "finished" into the stage flow's return value.
class CStageCutScene : CStage {
	public CStageCutScene() {
		base.eStageID = EStage.CutScene;
		base.ePhaseID = CStage.EPhase.Common_NORMAL;
	}

	private static LuaROActivityWrapper? UI => LuaROActivityWrapper.GetROActivity("cutscene");

	// the outro fades to black before song select; the intro hands over at once, the loading transition covers it
	private const double OUTRO_FADE_SECONDS = 0.6;

	public override void Activate() {
		if (base.IsActivated)
			return;

		this.cutScenes ??= [];
		this.returned = false;
		base.ePhaseID = CStage.EPhase.Common_NORMAL;

		// whatever plays now can be watched again (My Room's TV reads this trigger, named like the
		// reg/met ones: .seencutscene_{uid}_{Intro|Outro}_{file})
		string uid = OpenTaiko.SongMount.rChoosenSong?.tGetUniqueId() ?? "";
		foreach (var c in this.cutScenes) {
			string seen = $".seencutscene_{uid}_{this.mode.ToString()}_{Path.GetFileName(c.FullPath)}".EscapeSingleQuotes();
			OpenTaiko.PrimarySaveFile.tSetGlobalTrigger(seen, true);
		}

		var ui = UI;
		if (ui == null) {
			Trace.TraceWarning("No cutscene ROActivity in this skin; the cutscene is skipped.");
		} else {
			ui.Activate(this.cutScenes.Select(c => c.FullPath).ToArray(),
				this.mode == ECutSceneMode.Outro ? OUTRO_FADE_SECONDS : 0.0);
		}

		base.Activate();
	}

	public bool LoadCutScenes(CStage stageLast, bool isLuaStageIntro = false) {
		var selectedSong = OpenTaiko.SongMount.rChoosenSong;
		if (isLuaStageIntro) {
			this.mode = ECutSceneMode.Intro;
			this.cutScenes = (selectedSong.CutSceneIntro != null) ? [selectedSong.CutSceneIntro] : [];
		} else {
			this.mode = ECutSceneMode.Outro;
			this.cutScenes = (selectedSong.CutSceneOutros != null) ? [.. selectedSong.CutSceneOutros] : [];
		}
		this.cutScenes.RemoveAll(x => !this.JudgeRequirement(x, selectedSong, true));
		return this.cutScenes.Count > 0;
	}

	public void RegisterMetOutros() {
		var selectedSong = OpenTaiko.SongMount.rChoosenSong;
		var oldMode = this.mode;
		this.mode = ECutSceneMode.Outro;
		if (selectedSong.CutSceneOutros != null) {
			foreach (var cutscene in selectedSong.CutSceneOutros) {
				string fileName = Path.GetFileName(cutscene.FullPath);
				string _gTriggerName = $".regcutscene_{selectedSong?.tGetUniqueId() ?? ""}_{this.mode.ToString()}_{fileName}".EscapeSingleQuotes();
				string _gTriggerMetName = $".metcutscene_{selectedSong?.tGetUniqueId() ?? ""}_{this.mode.ToString()}_{fileName}".EscapeSingleQuotes();

				if (OpenTaiko.PrimarySaveFile.tGetGlobalTrigger(_gTriggerName) == true
					|| this.JudgeRequirement(cutscene, selectedSong, false) == true) {
					OpenTaiko.PrimarySaveFile.tSetGlobalTrigger(_gTriggerMetName, true);
				}
			}
		}
		this.mode = oldMode;
	}

	private bool JudgeRequirement(CTja.CutSceneDef cutScene, CSongListNode? songInfo = null, bool sideEffect = true) {
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
					if (sideEffect == true) OpenTaiko.PrimarySaveFile.tSetGlobalTrigger(_gTriggerName, true);
				}
				return false;
			}
		}

		if (cutScene.RepeatMode == CTja.ECutSceneRepeatMode.FirstMet) {
			// First Met => Does play but disable future plays
			if (sideEffect == true) OpenTaiko.PrimarySaveFile.tSetGlobalTrigger(_gTriggerName, true);
		}
		return true;
	}

	public override void DeActivate() {
		UI?.Deactivate();
		this.cutScenes?.Clear();
		this.cutScenes = null;
		base.DeActivate();
	}

	public override int Draw() {
		if (!base.IsActivated)
			return 0;

		var ui = UI;
		bool finished;
		if (ui == null) {
			finished = true;
		} else {
			var r = ui.Update();
			ui.Draw();
			finished = r != null && r.Length > 0 && (r[0] as string) == "finished";
		}

		// the stage flow acts on the value once; the script keeps drawing its last picture while the next
		// stage's transition covers it
		if (finished && !this.returned) {
			this.returned = true;
			return (int)(this.mode == ECutSceneMode.Intro ? EReturnValue.IntroFinished : EReturnValue.OutroFinishedFadeOut);
		}
		return (int)EReturnValue.Continue;
	}

	public enum EReturnValue : int {
		Continue,
		IntroFinished,
		IntroFinishedFadeOut,
		OutroFinished,
		OutroFinishedFadeOut,
	}

	#region [Private]

	private enum ECutSceneMode {
		Intro,
		Outro,
	}

	private ECutSceneMode mode;
	private List<CTja.CutSceneDef>? cutScenes;
	private bool returned;

	#endregion
}
