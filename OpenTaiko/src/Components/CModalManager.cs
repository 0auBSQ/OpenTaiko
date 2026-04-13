namespace OpenTaiko;

internal class CModalManager {
	public ModalQueue rModalQueue { get; private set; }
	private Modal? displayedModals;

	/// <summary>
	/// Set to true by <see cref="Draw"/> after the last queued modal is dismissed by the
	/// Lua script. The result screen polls this every frame to trigger the fade-out.
	/// </summary>
	public bool AllModalsDone { get; set; } = false;

	private static LuaROActivityWrapper? Script => LuaROActivityWrapper.GetROActivity("modal");

	// Called by the skin system on skin refresh — ROActivities are managed by the skin loader.
	public void RefleshSkin() { }

	public void RegisterNewModal(int player, int rarity, Modal.EModalType modalType, params object?[] args) {
		object[] newParams = new object[] { player, rarity, (int)modalType };
		if (args != null) newParams = newParams.Concat((object[])args).ToArray();
		Script?.Activate(newParams);
	}

	/// <summary>
	/// Called every frame. Updates and draws the active modal.
	/// When the Lua script self-deactivates (player confirmed), automatically pops the next
	/// queued modal or sets <see cref="AllModalsDone"/> when the queue is exhausted.
	/// </summary>
	public void Draw() {
		if (displayedModals == null) return;

		Script?.Update();
		Script?.Draw();

		// Lua called DEACTIVATE() after detecting input — advance the queue
		if (!(Script?.IsActive ?? true)) {
			OpenTaiko.Skin.soundDecideSFX.tPlay();
			if (!rModalQueue.tAreBothQueuesEmpty()) {
				displayedModals = rModalQueue.tPopModalInOrder();
			} else {
				displayedModals = null;
				AllModalsDone = true;
			}
		}
	}

	/// <summary>
	/// Called from the result screen inside its input gate.
	/// Pops the first modal on the initial input press, or returns true immediately
	/// when no modals were ever queued.
	/// Subsequent modal advancement is handled by Lua via <see cref="Draw"/>.
	/// </summary>
	public bool InputManagement() {
		if (displayedModals != null) return false; // Lua is handling the current modal

		if (rModalQueue.tAreBothQueuesEmpty()) return true; // Nothing queued — exit results

		// First modal: triggered by the result screen's existing input gate
		OpenTaiko.Skin.soundDecideSFX.tPlay();
		displayedModals = rModalQueue.tPopModalInOrder();
		return false;
	}

	public CModalManager() {
		rModalQueue = new ModalQueue();
		displayedModals = null;
	}
}
