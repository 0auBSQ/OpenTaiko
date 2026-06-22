using System.Diagnostics;
using FDK;

namespace OpenTaiko;

// Thin adapter: builds the in-game pause-menu model (Resume / Restart / Exit) + action dispatch, and drives the
// shared CPopupMenuManager (which renders via the popup_menu ROActivity). Replaces the old CActSelectPopupMenu-based
// implementation; the Pause/Resume/tPlayRetry/tPlayAbort state logic stays in CStagePlayScreenCommon.
internal class CActPlayPauseMenu : CActivity {
	private enum EOrder { Continue, Redoing, Return }
	private List<EOrder> menuActions = new();

	private bool bRetrySelected;
	private Stopwatch sw = new();

	public CActPlayPauseMenu() {
		base.IsDeActivated = true;
	}

	public bool bIsActivePopupMenu => OpenTaiko.PopupMenuManager.IsActive;

	public void tActivatePopupMenu(EKeyConfigPart einst) {
		this.bRetrySelected = false;
		this.sw.Reset();

		var items = new List<string>();
		menuActions = new List<EOrder>();

		items.Add(CLangManager.LangInstance.GetString("PAUSE_RESUME"));
		menuActions.Add(EOrder.Continue);
		// Restart is unavailable in Dan mode and after a song-jump (matches the original).
		if (OpenTaiko.SongMount.nChoosenSongDifficulty[0] != (int)Difficulty.Dan && !OpenTaiko.SongMount.bIsAfterSongJump) {
			items.Add(CLangManager.LangInstance.GetString("PAUSE_RESTART"));
			menuActions.Add(EOrder.Redoing);
		}
		items.Add(CLangManager.LangInstance.GetString("PAUSE_EXIT"));
		menuActions.Add(EOrder.Return);

		OpenTaiko.PopupMenuManager.Open(
			CLangManager.LangInstance.GetString("PAUSE_TITLE"),
			items.ToArray(), 0, OnDecide, null, escEnabled: false, onUpdateSub: OnUpdateSub);
	}

	public void tDeativatePopupMenu() => OpenTaiko.PopupMenuManager.Close();

	private void OnDecide(int index) {
		if (index < 0 || index >= menuActions.Count) return;
		switch (menuActions[index]) {
			case EOrder.Continue:
				OpenTaiko.stageGameScreen.Resume();
				OpenTaiko.PopupMenuManager.Close();
				break;
			case EOrder.Redoing:
				// Defer the retry: lock input + run the 1.5 s delay in OnUpdateSub (matches the old UpdateSub).
				this.bRetrySelected = true;
				OpenTaiko.PopupMenuManager.LockSelection();
				break;
			case EOrder.Return:
				OpenTaiko.stageGameScreen.tPlayAbort();
				OpenTaiko.PopupMenuManager.Close();
				break;
		}
	}

	private void OnUpdateSub() {
		if (this.bRetrySelected) {
			if (!sw.IsRunning)
				this.sw = Stopwatch.StartNew();
			if (sw.ElapsedMilliseconds > 1500) {
				OpenTaiko.stageGameScreen.tPlayRetry();
				OpenTaiko.PopupMenuManager.Close();
				this.sw.Reset();
				this.bRetrySelected = false;
			}
		}
	}

	public int Update() {
		OpenTaiko.PopupMenuManager.Update();
		return 0;
	}

	public override int Draw() {
		OpenTaiko.PopupMenuManager.Draw();
		return 0;
	}

	public override void DeActivate() {
		if (OpenTaiko.PopupMenuManager.IsActive) OpenTaiko.PopupMenuManager.Close();
		base.DeActivate();
	}
}
