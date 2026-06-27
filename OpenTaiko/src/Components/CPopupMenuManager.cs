using System;
using FDK;

namespace OpenTaiko;

/// <summary>
/// Drives the <c>popup_menu</c> ROActivity (which renders the list popup in Lua) and owns the menu input +
/// navigation — the replacement for the old C# <see cref="CActSelectPopupMenu"/> rendering. One popup shows at a
/// time (the in-game pause menu and the cutscene pause menu never overlap), so a single shared instance suffices,
/// mirroring <see cref="CModalManager"/>.
///
/// The menu MODEL (title + item labels) and the ACTION dispatch (Resume/Restart/Exit/Skip + the 1.5 s retry delay)
/// stay in C# — the caller passes them to <see cref="Open"/>; rendering + the cursor live in Lua.
/// </summary>
internal class CPopupMenuManager {
	private static LuaROActivityWrapper? Script => LuaROActivityWrapper.GetROActivity("popup_menu");

	private bool _active;
	private string[] _items = Array.Empty<string>();
	private int _selected;
	private bool _escEnabled;
	private bool _locked;            // a selection that defers (the retry) locks further input, like the old bSelected
	private Action<int>? _onDecide;
	private Action? _onCancel;
	private Action? _onUpdateSub;    // per-frame hook (the play menu's retry timer), mirrors CActSelectPopupMenu.UpdateSub

	private readonly CCounter _ctRepeatUp = new(0, 0, 0, OpenTaiko.Timer);
	private readonly CCounter _ctRepeatDown = new(0, 0, 0, OpenTaiko.Timer);

	public bool IsActive => _active;

	// ROActivities are reloaded wholesale by the skin loader, so this is a no-op (mirrors CModalManager.RefleshSkin).
	public void RefleshSkin() { }

	/// <summary>Open the popup with a title + item labels. <paramref name="onDecide"/> receives the chosen index;
	/// it should call <see cref="Close"/> (immediate) or <see cref="LockSelection"/> (defer, e.g. the retry delay).
	/// <paramref name="onUpdateSub"/> runs every frame while open (used for the deferred retry).</summary>
	public void Open(string title, string[] items, int defaultIndex,
					 Action<int> onDecide, Action? onCancel, bool escEnabled, Action? onUpdateSub = null) {
		_items = items ?? Array.Empty<string>();
		_selected = (defaultIndex >= 0 && defaultIndex < _items.Length) ? defaultIndex : 0;
		_onDecide = onDecide;
		_onCancel = onCancel;
		_onUpdateSub = onUpdateSub;
		_escEnabled = escEnabled;
		_locked = false;
		_active = true;

		// Push the model to Lua: title, "\n"-joined labels, font size, and every PopupMenu_* position. The panel +
		// cursor sprites are owned by the popup_menu module itself (Modules/ROActivities/popup_menu/Textures/).
		Script?.Activate(
			title,
			string.Join("\n", _items),
			OpenTaiko.Skin.PopupMenu_Font_Size,
			OpenTaiko.Skin.PopupMenu_Menu_Title[0], OpenTaiko.Skin.PopupMenu_Menu_Title[1],
			OpenTaiko.Skin.PopupMenu_Title[0], OpenTaiko.Skin.PopupMenu_Title[1],
			OpenTaiko.Skin.PopupMenu_Menu_Highlight[0], OpenTaiko.Skin.PopupMenu_Menu_Highlight[1],
			OpenTaiko.Skin.PopupMenu_MenuItem_Name[0], OpenTaiko.Skin.PopupMenu_MenuItem_Name[1],
			OpenTaiko.Skin.PopupMenu_Move[0], OpenTaiko.Skin.PopupMenu_Move[1]);
	}

	public void Close() {
		_active = false;
		Script?.Deactivate();
	}

	/// <summary>Stop processing input but keep the popup open (the action defers — e.g. the retry's 1.5 s wait).</summary>
	public void LockSelection() => _locked = true;

	private void Prev() {
		OpenTaiko.Skin.soundCursorMoveSound.tPlay();
		if (--_selected < 0) _selected = _items.Length - 1;
	}

	private void Next() {
		OpenTaiko.Skin.soundCursorMoveSound.tPlay();
		if (++_selected >= _items.Length) _selected = 0;
	}

	public void Update() {
		if (!_active) return;

		if (!_locked) {
			// Cancel (only when the caller enabled it; both pause menus disable it)
			if ((OpenTaiko.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.Escape)
				 || OpenTaiko.Pad.bPressed(EKeyConfigPart.Taiko, EPad.FT))
				&& _escEnabled) {
				OpenTaiko.Skin.soundCancelSFX.tPlay();
				_onCancel?.Invoke();
				Close();
				return;
			}

			// Decide
			if (OpenTaiko.Pad.bPressed(EKeyConfigPart.Taiko, EPad.Decide)
				|| OpenTaiko.Pad.bPressed(EKeyConfigPart.Taiko, EPad.RD)
				|| OpenTaiko.Pad.bPressed(EKeyConfigPart.Taiko, EPad.LC)
				|| OpenTaiko.Pad.bPressed(EKeyConfigPart.Taiko, EPad.LRed)
				|| OpenTaiko.Pad.bPressed(EKeyConfigPart.Taiko, EPad.RRed)
				|| (OpenTaiko.ConfigIni.bEnterIsNotUsedInKeyAssignments && OpenTaiko.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.Return))) {
				OpenTaiko.Skin.soundDecideSFX.tPlay();
				_onDecide?.Invoke(_selected);
			}

			// Up (arrow with key-repeat, plus pads)
			this._ctRepeatUp.KeyIntervalFunc(OpenTaiko.InputManager.Keyboard.KeyPressing((int)SlimDXKeys.Key.UpArrow), new CCounter.KeyProcess(this.Prev));
			if (OpenTaiko.Pad.bPressed(EKeyConfigPart.Taiko, EPad.SD) || OpenTaiko.Pad.bPressed(EKeyConfigPart.Taiko, EPad.LBlue))
				this.Prev();

			// Down (arrow with key-repeat, plus pads)
			this._ctRepeatDown.KeyIntervalFunc(OpenTaiko.InputManager.Keyboard.KeyPressing((int)SlimDXKeys.Key.DownArrow), new CCounter.KeyProcess(this.Next));
			if (OpenTaiko.Pad.bPressed(EKeyConfigPart.Taiko, EPad.LT) || OpenTaiko.Pad.bPressed(EKeyConfigPart.Taiko, EPad.RBlue))
				this.Next();
		}

		_onUpdateSub?.Invoke();
	}

	public void Draw() {
		if (!_active) return;
		Script?.Draw(_selected);
	}
}
