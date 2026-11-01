using FDK;

namespace OpenTaiko.iOS;

/// <summary>
/// The on-screen touch drum to be registered as a gamepad type device. Triggers (left/right Don,
/// left/right Ka) buttons according to the touch areas. Not affected by keyboard bindings.
/// </summary>
internal sealed class CInputTouch_iOS : CInputButtonsBase, IInputDevice {
	public const int DonLeft = 0;
	public const int DonRight = 1;
	public const int KaLeft = 2;
	public const int KaRight = 3;

	public CInputTouch_iOS() : base(4) {
		this.CurrentType = InputDeviceType.Gamepad;
		this.GUID = "ios-touch";
		this.ID = 0;
		this.Name = "Touch Drum";
	}

	public new Silk.NET.Input.IInputDevice? Device => null;

	public string GetButtonName(int index) => index switch {
		DonLeft => "Touch Don L",
		DonRight => "Touch Don R",
		KaLeft => "Touch Ka L",
		KaRight => "Touch Ka R",
		_ => $"Button{index}",
	};

	private readonly HashSet<int> _touchPressed = new();

	public void TouchButtonDown(int button) {
		_touchPressed.Add(button);
		ButtonDown(button);
	}

	/// <summary>
	/// Reset touch pulses after each game frame. Only resets isPressed without emitting
	/// release events, similar to CInputKeyboard_iOS.ReleaseTouchKeys.
	/// </summary>
	public void ReleaseTouchButtons() {
		foreach (int button in _touchPressed)
			this.ButtonStates[button].isPressed = 0U;
		_touchPressed.Clear();
	}

	public void EnsureBindings(CConfigIni cfg) {
		// The bindings reference this device's stable ID, which is only final once
		// CInputManager.SetID .
		if (!cfg.dicGamepad.TryGetValue(this.ID, out string? guid) || guid != this.GUID) {
			System.Diagnostics.Trace.TraceError(
				$"Touch drum bindings skipped: device ID {this.ID} is not registered " +
				$"to {this.GUID} in the GUID map. EnsureBindings must run after " +
				"CInputManager.SetID.");
			return;
		}
		Ensure(cfg, EKeyConfigPad.LRed, DonLeft);
		Ensure(cfg, EKeyConfigPad.RRed, DonRight);
		Ensure(cfg, EKeyConfigPad.LBlue, KaLeft);
		Ensure(cfg, EKeyConfigPad.RBlue, KaRight);
		Ensure(cfg, EKeyConfigPad.Decide, DonLeft);
		Ensure(cfg, EKeyConfigPad.Decide, DonRight);
		Ensure(cfg, EKeyConfigPad.LeftChange, KaLeft);
		Ensure(cfg, EKeyConfigPad.RightChange, KaRight);
	}

	private void Ensure(CConfigIni cfg, EKeyConfigPad pad, int code) {
		var slots = cfg.KeyAssign[(int)EKeyConfigPart.Taiko][(int)pad];
		int free = -1;
		for (int i = 0; i < slots.Length; i++) {
			if (
				slots[i].InputDevice == InputDeviceType.Gamepad &&
				slots[i].ID == this.ID && slots[i].Code == code
			)
				return;
			if (free < 0 && slots[i].InputDevice == InputDeviceType.Unknown)
				free = i;
		}
		if (free >= 0)
			slots[free] = new CConfigIni.CKeyAssign.STKEYASSIGN(
				InputDeviceType.Gamepad, this.ID, code);
	}
}
