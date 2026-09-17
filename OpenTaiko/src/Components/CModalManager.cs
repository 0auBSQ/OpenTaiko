namespace OpenTaiko;

internal class CModalManager {
	public ModalQueue rModalQueue { get; private set; }
	private Modal? displayedModals;

	/// <summary>
	/// Set to true by <see cref="Draw"/> after the last queued modal is dismissed by the
	/// Lua script. The result screen polls this every frame to trigger the fade-out.
	/// </summary>
	public bool AllModalsDone { get; set; } = false;

	/// <summary>A modal is on screen (the stage dims its music behind it).</summary>
	public bool IsShowing => displayedModals != null;

	private static LuaROActivityWrapper? Script => LuaROActivityWrapper.GetROActivity("modal");

	// Called by the skin system on skin refresh — ROActivities are managed by the skin loader.
	public void RefreshSkin() { }

	public void RegisterNewModal(int player, int rarity, Modal.EModalType modalType, params object?[] args) {
		object[] newParams = new object[] { player, rarity, (int)modalType };
		if (args != null) {
			var wrappedArgs = new object?[args.Length];
			for (int i = 0; i < args.Length; i++) {
				if (args[i] is CPuchichara p)
					wrappedArgs[i] = OpenTaiko.Tx?.LuaPuchicharaDb?.GetByName(Path.GetFileName(p._path)) ?? (object?)p;
				else
					wrappedArgs[i] = args[i];
			}
			newParams = [.. newParams, .. wrappedArgs];
		}
		Script?.Activate(newParams);
	}

	/// <summary>
	/// Called every frame. Updates and draws the active modal.
	/// When the Lua script self-deactivates (player confirmed), automatically pops the next
	/// queued modal or sets <see cref="AllModalsDone"/> when the queue is exhausted.
	/// </summary>
	public void Draw() {
		if (displayedModals == null || Script == null) return;

		Script.Update();
		Script.Draw();

		// Lua called DEACTIVATE() after detecting input — advance the queue
		if (!Script.IsActive) {
			if (!rModalQueue.tAreBothQueuesEmpty()) {
				OpenTaiko.Skin.soundDecideSFX.tPlay();
				displayedModals = rModalQueue.tPopModalInOrder();
			} else {
				// same state as if no modals were ever queued
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
