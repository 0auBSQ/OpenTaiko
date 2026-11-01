using SlimDXKeys;
using FDK;

namespace OpenTaiko.iOS;

/// <summary>
/// Keyboard input device for iOS.
/// Receives key events from UIKit's PressesBegan/PressesEnded (hardware keyboard
/// or macOS simulator keyboard passthrough) via KeyDown/KeyUp methods.
/// Maps UIKeyboardHIDUsage codes to SlimDXKeys.Key for the game's input system.
/// </summary>
public class CInputKeyboard_iOS : CInputButtonsBase {
	public CInputKeyboard_iOS() : base(145) {
		this.CurrentType = InputDeviceType.Keyboard;
		this.Name = "iOS Keyboard";
		this.GUID = "ios-keyboard";
		this.ID = 0;
	}

	public new Silk.NET.Input.IInputDevice? Device => null;

	/// <summary>
	/// Called by the iOS host when a key is pressed.
	/// keyCode is a UIKeyboardHIDUsage value (USB HID usage code).
	/// </summary>
	public void KeyDown(long keyCode) {
		var key = HIDUsageToSlimDXKey(keyCode);
		if (key == Key.Unknown || (int)key >= this.ButtonStates.Length) return;
		base.ButtonDown((int)key);
	}

	/// <summary>
	/// Called by the iOS host when a key is released.
	/// </summary>
	public void KeyUp(long keyCode) {
		var key = HIDUsageToSlimDXKey(keyCode);
		if (key == Key.Unknown || (int)key >= this.ButtonStates.Length) return;
		base.ButtonUp((int)key);
	}

	private readonly HashSet<int> _touchPressedKeys = new();

	/// <summary>
	/// Record a key as touch-originated and press it.
	/// </summary>
	public void TouchKeyDown(long keyCode) {
		var key = HIDUsageToSlimDXKey(keyCode);
		if (key == Key.Unknown || (int)key >= this.ButtonStates.Length) return;
		_touchPressedKeys.Add((int)key);
		base.ButtonDown((int)key);
	}

	/// <summary>
	/// Release all touch-originated keys. Called after each game frame
	/// so that touch inputs behave as single-frame pulses.
	/// Hardware keyboard keys are not affected.
	/// Only resets isPressed — does not emit release events, since
	/// the "release" is synthetic (the touch pulse model has no real release).
	/// </summary>
	public void ReleaseTouchKeys() {
		foreach (int key in _touchPressedKeys) {
			this.ButtonStates[key].isPressed = 0U;
		}
		_touchPressedKeys.Clear();
	}

	/// <summary>
	/// Maps UIKeyboardHIDUsage (USB HID Usage Table 0x07) to SlimDXKeys.Key.
	/// </summary>
	private static Key HIDUsageToSlimDXKey(long usage) {
		return usage switch {
			// Letters (HID 0x04-0x1D → A-Z)
			0x04 => Key.A,
			0x05 => Key.B,
			0x06 => Key.C,
			0x07 => Key.D,
			0x08 => Key.E,
			0x09 => Key.F,
			0x0A => Key.G,
			0x0B => Key.H,
			0x0C => Key.I,
			0x0D => Key.J,
			0x0E => Key.K,
			0x0F => Key.L,
			0x10 => Key.M,
			0x11 => Key.N,
			0x12 => Key.O,
			0x13 => Key.P,
			0x14 => Key.Q,
			0x15 => Key.R,
			0x16 => Key.S,
			0x17 => Key.T,
			0x18 => Key.U,
			0x19 => Key.V,
			0x1A => Key.W,
			0x1B => Key.X,
			0x1C => Key.Y,
			0x1D => Key.Z,

			// Numbers (HID 0x1E-0x27 → 1-9, 0)
			0x1E => Key.D1,
			0x1F => Key.D2,
			0x20 => Key.D3,
			0x21 => Key.D4,
			0x22 => Key.D5,
			0x23 => Key.D6,
			0x24 => Key.D7,
			0x25 => Key.D8,
			0x26 => Key.D9,
			0x27 => Key.D0,

			// Control keys
			0x28 => Key.Return,      // Return/Enter
			0x29 => Key.Escape,
			0x2A => Key.Backspace,
			0x2B => Key.Tab,
			0x2C => Key.Space,
			0x2D => Key.Minus,
			0x2E => Key.Equals,
			0x2F => Key.LeftBracket,
			0x30 => Key.RightBracket,
			0x31 => Key.Backslash,
			0x33 => Key.Semicolon,
			0x34 => Key.Apostrophe,
			0x35 => Key.Grave,
			0x36 => Key.Comma,
			0x37 => Key.Period,
			0x38 => Key.Slash,
			0x39 => Key.CapsLock,

			// Function keys (HID 0x3A-0x45 → F1-F12)
			0x3A => Key.F1,
			0x3B => Key.F2,
			0x3C => Key.F3,
			0x3D => Key.F4,
			0x3E => Key.F5,
			0x3F => Key.F6,
			0x40 => Key.F7,
			0x41 => Key.F8,
			0x42 => Key.F9,
			0x43 => Key.F10,
			0x44 => Key.F11,
			0x45 => Key.F12,

			// Navigation
			0x46 => Key.PrintScreen,
			0x47 => Key.ScrollLock,
			0x48 => Key.Pause,
			0x49 => Key.Insert,
			0x4A => Key.Home,
			0x4B => Key.PageUp,
			0x4C => Key.Delete,
			0x4D => Key.End,
			0x4E => Key.PageDown,
			0x4F => Key.RightArrow,
			0x50 => Key.LeftArrow,
			0x51 => Key.DownArrow,
			0x52 => Key.UpArrow,

			// Numpad
			0x53 => Key.NumberLock,
			0x54 => Key.NumberPadSlash,
			0x55 => Key.NumberPadStar,
			0x56 => Key.NumberPadMinus,
			0x57 => Key.NumberPadPlus,
			0x58 => Key.NumberPadEnter,
			0x59 => Key.NumberPad1,
			0x5A => Key.NumberPad2,
			0x5B => Key.NumberPad3,
			0x5C => Key.NumberPad4,
			0x5D => Key.NumberPad5,
			0x5E => Key.NumberPad6,
			0x5F => Key.NumberPad7,
			0x60 => Key.NumberPad8,
			0x61 => Key.NumberPad9,
			0x62 => Key.NumberPad0,
			0x63 => Key.NumberPadPeriod,

			// Modifiers (HID 0xE0-0xE7)
			0xE0 => Key.LeftControl,
			0xE1 => Key.LeftShift,
			0xE2 => Key.LeftAlt,
			0xE3 => Key.LeftWindowsKey,
			0xE4 => Key.RightControl,
			0xE5 => Key.RightShift,
			0xE6 => Key.RightAlt,
			0xE7 => Key.RightWindowsKey,

			_ => Key.Unknown
		};
	}

	public override string ToString() => "iOS Keyboard";
}
