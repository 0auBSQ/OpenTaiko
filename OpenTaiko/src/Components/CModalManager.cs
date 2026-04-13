namespace OpenTaiko;

internal class CModalManager {
	public ModalQueue rModalQueue { get; private set; }
	private Modal? displayedModals;

	private static LuaROActivityWrapper? Script => LuaROActivityWrapper.GetROActivity("modal");

	/// <summary>Returns true when the modal animation has finished (or no modal script exists).</summary>
	private bool AnimationFinished() {
		var result = Script?.Call("isAnimationFinished");
		if (result == null || result.Length == 0) return true;
		return (bool)result[0];
	}

	// Called by the skin system on skin refresh — ROActivities are managed by the skin loader.
	public void RefleshSkin() { }

	public void RegisterNewModal(int player, int rarity, Modal.EModalType modalType, params object?[] args) {
		object[] newParams = new object[] { player, rarity, (int)modalType };
		if (args != null) newParams = newParams.Concat((object[])args).ToArray();
		Script?.Call("registerNewModal", newParams);
	}

	public void Draw() {
		if (displayedModals != null) {
			Script?.Update();
			Script?.Draw();
		}
	}

	public bool Input() {
		if (OpenTaiko.Pad.bPressedDGB(EPad.Decide)
			|| OpenTaiko.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.Return)) {
			return InputManagement();
		}
		return false;
	}

	public bool InputManagement() {
		if (AnimationFinished()) {
			OpenTaiko.Skin.soundDecideSFX.tPlay();

			if (!rModalQueue.tAreBothQueuesEmpty()
				&& (OpenTaiko.Pad.bPressedDGB(EPad.Decide)
					|| OpenTaiko.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.Return))) {
				displayedModals = rModalQueue.tPopModalInOrder();
			} else if (OpenTaiko.ConfigIni.nPlayerCount == 1 || rModalQueue.tAreBothQueuesEmpty()) {
				if (!rModalQueue.tAreBothQueuesEmpty())
					LogNotification.PopError("Unexpected Error: Exited results screen with remaining modals, this is likely due to a Lua script issue.");
				displayedModals = null;
				return true;
			}
		}
		return false;
	}

	public CModalManager() {
		rModalQueue = new ModalQueue();
		displayedModals = null;
	}
}
