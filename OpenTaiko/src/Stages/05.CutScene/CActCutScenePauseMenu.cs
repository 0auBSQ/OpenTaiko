using FDK;

namespace OpenTaiko;

// Thin adapter: cutscene pause menu (Resume / Skip) driving the shared CPopupMenuManager (which renders via the
// popup_menu ROActivity). Replaces the old CActSelectPopupMenu-based implementation.
internal class CActCutScenePauseMenu : CActivity {
	private enum EOrder { Continue, Skip }
	private List<EOrder> menuActions = new();

	public CActCutScenePauseMenu() {
		base.IsDeActivated = true;
	}

	public bool bIsActivePopupMenu => OpenTaiko.PopupMenuManager.IsActive;

	public void tActivatePopupMenu(EKeyConfigPart einst) {
		var items = new List<string> {
			CLangManager.LangInstance.GetString("PAUSE_RESUME"),
			CLangManager.LangInstance.GetString("PAUSE_SKIP"),
		};
		menuActions = new List<EOrder> { EOrder.Continue, EOrder.Skip };

		OpenTaiko.PopupMenuManager.Open(
			CLangManager.LangInstance.GetString("PAUSE_TITLE"),
			items.ToArray(), 0, OnDecide, null, escEnabled: false);
	}

	public void tDeativatePopupMenu() => OpenTaiko.PopupMenuManager.Close();

	private void OnDecide(int index) {
		if (index < 0 || index >= menuActions.Count) return;
		switch (menuActions[index]) {
			case EOrder.Continue:
				OpenTaiko.stageCutScene.Resume();
				OpenTaiko.PopupMenuManager.Close();
				break;
			case EOrder.Skip:
				OpenTaiko.stageCutScene.Skip();
				OpenTaiko.PopupMenuManager.Close();
				break;
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
