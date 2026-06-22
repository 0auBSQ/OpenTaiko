using FDK;

namespace OpenTaiko;

internal class CActFIFOStart : CActFIFOBase {
	// メソッド

	public override void tFadeOutStart(int? start = null, int? end = null, int? interval = null)
		=> tFadeOutStart(false, start, end, interval);
	public void tFadeOutStart(bool skipDelay, int? start = null, int? end = null, int? interval = null) {
		OpenTaiko.Skin.soundDanSelectBGM.tStop();

		if (OpenTaiko.SongMount.nChoosenSongDifficulty[0] == (int)Difficulty.Dan)
			base.StartFadeOutCounter(start ?? (skipDelay ? 1000 : 0), end ?? 1255, interval ?? 1);
		else if (OpenTaiko.ConfigIni.bAIBattleMode) {
			base.StartFadeOutCounter(start ?? (skipDelay ? 2000 : 0), end ?? 5500, interval ?? 1);
		} else if (OpenTaiko.SongMount.nChoosenSongDifficulty[0] >= (int)Difficulty.Tower) {
			base.StartFadeOutCounter(start ?? (skipDelay ? 1000 : 0), end ?? 3580, interval ?? 1);
		} else {
			base.StartFadeOutCounter(start ?? (skipDelay ? 2580 : 0), end ?? 3580, interval ?? 1);
		}
	}

	public override void tFadeInStart(int? start = null, int? end = null, int? interval = null) {
		if (OpenTaiko.SongMount.nChoosenSongDifficulty[0] == (int)Difficulty.Dan) {
			base.StartFadeInCounter(start ?? 0, end ?? 255, interval ?? 1);

			OpenTaiko.stageGameScreen.actDan.Start(OpenTaiko.stageGameScreen.ListDan_Number);
			return;
		}
		if (OpenTaiko.ConfigIni.bAIBattleMode) {
			base.StartFadeInCounter(start ?? 0, end ?? 3580, interval ?? 1);
		} else {
			base.StartFadeInCounter(start ?? 0, end ?? 3580, interval ?? 1);
		}
		for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; ++i) {
			OpenTaiko.stageGameScreen.actLaneTaiko.BranchText_FadeIn(1000, i);
		}
	}

	// CActivity 実装
	public override int DrawSub() {
		// The song-start curtain (which used the deprecated Graphics/4_SongLoading assets) is now drawn by the
		// song_loading transition's fade-in. This actor is kept only for its counter + the gameplay side-effects
		// in tFadeIn/OutStart (dan plate start, lane branch-text fade-in), so DrawSub draws nothing.
		return 0;
	}
}
