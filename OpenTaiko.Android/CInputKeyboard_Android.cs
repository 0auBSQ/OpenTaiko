using Android.Views;
using FDK;
using SlimDXKeys;

namespace OpenTaiko.Android;

/// <summary>
/// Keyboard/touch input device for Android — the twin of the iOS port's CInputKeyboard_iOS.
/// The host feeds it USB-HID usage codes: hardware keyboards via the Keycode map below, touch
/// zones via TouchKeyDown (single-frame pulses released after each hosted frame).
/// </summary>
public class CInputKeyboard_Android : CInputButtonsBase {
	public CInputKeyboard_Android() : base(145) {
		this.CurrentType = InputDeviceType.Keyboard;
		this.Name = "Android Keyboard";
		this.GUID = "android-keyboard";
		this.ID = 0;
	}

	public new Silk.NET.Input.IInputDevice? Device => null;

	// Android delivers touches/key events on the UI thread while the game reads and mutates input
	// state on the render thread (iOS has no such split — CADisplayLink frames and touches share
	// the main thread). Pressing buttons directly from the UI thread crashed two ways in the
	// field: a HashSet enumeration race in ReleaseTouchKeys, and an NRE inside ButtonDown when a
	// touch landed before/around sound-timer initialization. So UI-thread events only ENQUEUE
	// here; the render loop applies them at the top of each frame via FlushQueuedKeys.
	private readonly System.Collections.Concurrent.ConcurrentQueue<(long hid, bool down, bool touch, long tick)> _queued = new();
	private readonly HashSet<int> _touchPressedKeys = new();   // render thread only

	public void KeyDown(long hidUsage) => _queued.Enqueue((hidUsage, true, false, Environment.TickCount64));

	public void KeyUp(long hidUsage) => _queued.Enqueue((hidUsage, false, false, Environment.TickCount64));

	/// <summary>Record a key as touch-originated and press it (a single-frame pulse).</summary>
	public void TouchKeyDown(long hidUsage) => _queued.Enqueue((hidUsage, true, true, Environment.TickCount64));

	/// <summary>Apply queued UI-thread events. Render thread, once per frame, before the game update.</summary>
	public void FlushQueuedKeys() {
		long now = Environment.TickCount64;
		while (_queued.TryDequeue(out var e)) {
			// Drop events that sat in the queue across a load/pause — replaying a stale ESC or
			// Enter half a second later would navigate menus by itself.
			if (now - e.tick > 500) continue;
			var key = HIDUsageToSlimDXKey(e.hid);
			if (key == Key.Unknown || (int)key >= this.ButtonStates.Length) continue;
			if (!e.down) {
				base.ButtonUp((int)key);
			} else {
				if (e.touch) _touchPressedKeys.Add((int)key);
				base.ButtonDown((int)key);
			}
		}
	}

	/// <summary>
	/// Release all touch-originated keys after each hosted frame. Only resets isPressed — no
	/// release events are emitted, since the touch pulse model has no real release. Hardware
	/// keyboard keys are unaffected. Render thread only.
	/// </summary>
	public void ReleaseTouchKeys() {
		foreach (int key in _touchPressedKeys) {
			this.ButtonStates[key].isPressed = 0U;
		}
		_touchPressedKeys.Clear();
	}

	/// <summary>Android Keycode → USB HID usage (page 0x07), for hardware keyboards.</summary>
	public static long KeycodeToHid(Keycode code) {
		if (code >= Keycode.A && code <= Keycode.Z) return 0x04 + (code - Keycode.A);
		if (code >= Keycode.Num1 && code <= Keycode.Num9) return 0x1E + (code - Keycode.Num1);
		return code switch {
			Keycode.Num0 => 0x27,
			Keycode.Enter => 0x28,
			Keycode.Escape => 0x29,
			Keycode.Del => 0x2A,               // backspace
			Keycode.Tab => 0x2B,
			Keycode.Space => 0x2C,
			Keycode.Minus => 0x2D,
			Keycode.Equals => 0x2E,
			Keycode.LeftBracket => 0x2F,
			Keycode.RightBracket => 0x30,
			Keycode.Backslash => 0x31,
			Keycode.Semicolon => 0x33,
			Keycode.Apostrophe => 0x34,
			Keycode.Grave => 0x35,
			Keycode.Comma => 0x36,
			Keycode.Period => 0x37,
			Keycode.Slash => 0x38,
			Keycode.CapsLock => 0x39,
			>= Keycode.F1 and <= Keycode.F12 => 0x3A + (code - Keycode.F1),
			Keycode.Insert => 0x49,
			Keycode.MoveHome => 0x4A,
			Keycode.PageUp => 0x4B,
			Keycode.ForwardDel => 0x4C,        // delete
			Keycode.MoveEnd => 0x4D,
			Keycode.PageDown => 0x4E,
			Keycode.DpadRight => 0x4F,
			Keycode.DpadLeft => 0x50,
			Keycode.DpadDown => 0x51,
			Keycode.DpadUp => 0x52,
			Keycode.CtrlLeft => 0xE0,
			Keycode.ShiftLeft => 0xE1,
			Keycode.AltLeft => 0xE2,
			Keycode.CtrlRight => 0xE4,
			Keycode.ShiftRight => 0xE5,
			Keycode.AltRight => 0xE6,
			_ => 0
		};
	}

	/// <summary>USB HID usage (page 0x07) → SlimDXKeys.Key — identical to the iOS port's table.</summary>
	private static Key HIDUsageToSlimDXKey(long usage) {
		return usage switch {
			0x04 => Key.A, 0x05 => Key.B, 0x06 => Key.C, 0x07 => Key.D, 0x08 => Key.E,
			0x09 => Key.F, 0x0A => Key.G, 0x0B => Key.H, 0x0C => Key.I, 0x0D => Key.J,
			0x0E => Key.K, 0x0F => Key.L, 0x10 => Key.M, 0x11 => Key.N, 0x12 => Key.O,
			0x13 => Key.P, 0x14 => Key.Q, 0x15 => Key.R, 0x16 => Key.S, 0x17 => Key.T,
			0x18 => Key.U, 0x19 => Key.V, 0x1A => Key.W, 0x1B => Key.X, 0x1C => Key.Y, 0x1D => Key.Z,
			0x1E => Key.D1, 0x1F => Key.D2, 0x20 => Key.D3, 0x21 => Key.D4, 0x22 => Key.D5,
			0x23 => Key.D6, 0x24 => Key.D7, 0x25 => Key.D8, 0x26 => Key.D9, 0x27 => Key.D0,
			0x28 => Key.Return, 0x29 => Key.Escape, 0x2A => Key.Backspace, 0x2B => Key.Tab,
			0x2C => Key.Space, 0x2D => Key.Minus, 0x2E => Key.Equals, 0x2F => Key.LeftBracket,
			0x30 => Key.RightBracket, 0x31 => Key.Backslash, 0x33 => Key.Semicolon,
			0x34 => Key.Apostrophe, 0x35 => Key.Grave, 0x36 => Key.Comma, 0x37 => Key.Period,
			0x38 => Key.Slash, 0x39 => Key.CapsLock,
			0x3A => Key.F1, 0x3B => Key.F2, 0x3C => Key.F3, 0x3D => Key.F4, 0x3E => Key.F5,
			0x3F => Key.F6, 0x40 => Key.F7, 0x41 => Key.F8, 0x42 => Key.F9, 0x43 => Key.F10,
			0x44 => Key.F11, 0x45 => Key.F12,
			0x46 => Key.PrintScreen, 0x47 => Key.ScrollLock, 0x48 => Key.Pause,
			0x49 => Key.Insert, 0x4A => Key.Home, 0x4B => Key.PageUp, 0x4C => Key.Delete,
			0x4D => Key.End, 0x4E => Key.PageDown,
			0x4F => Key.RightArrow, 0x50 => Key.LeftArrow, 0x51 => Key.DownArrow, 0x52 => Key.UpArrow,
			0x53 => Key.NumberLock, 0x54 => Key.NumberPadSlash, 0x55 => Key.NumberPadStar,
			0x56 => Key.NumberPadMinus, 0x57 => Key.NumberPadPlus, 0x58 => Key.NumberPadEnter,
			0x59 => Key.NumberPad1, 0x5A => Key.NumberPad2, 0x5B => Key.NumberPad3,
			0x5C => Key.NumberPad4, 0x5D => Key.NumberPad5, 0x5E => Key.NumberPad6,
			0x5F => Key.NumberPad7, 0x60 => Key.NumberPad8, 0x61 => Key.NumberPad9,
			0x62 => Key.NumberPad0, 0x63 => Key.NumberPadPeriod,
			0xE0 => Key.LeftControl, 0xE1 => Key.LeftShift, 0xE2 => Key.LeftAlt, 0xE3 => Key.LeftWindowsKey,
			0xE4 => Key.RightControl, 0xE5 => Key.RightShift, 0xE6 => Key.RightAlt, 0xE7 => Key.RightWindowsKey,
			_ => Key.Unknown
		};
	}

	public override string ToString() => "Android Keyboard";
}