using FDK;
using NLua;

namespace OpenTaiko {
	public class LuaInputFunc {
		private Dictionary<string, LuaCounter> _repeatCounters = new Dictionary<string, LuaCounter>();
		private Dictionary<string, LuaCounter> _repeatKbCounters = new Dictionary<string, LuaCounter>();

		// no keys or mouse while a transition reveals the stage (CPad blocks the pad)
		private static bool Blocked => CStageTransition.BlocksInput;

		// General inputs
		public bool Pressed(string input)
			=> Enum.TryParse<EKeyConfigPad>(input, true, out var pad)
				&& OpenTaiko.Pad.bPressed(EKeyConfigPart.Taiko, pad);
		public bool Pressing(string input)
			=> Enum.TryParse<EKeyConfigPad>(input, true, out var pad)
				&& OpenTaiko.Pad.IsPressing(EKeyConfigPart.Taiko, pad);

		public void RepeatWhilePressing(string input, double interval_seconds, LuaFunction predicate) {
			bool isPressing = Pressing(input);

			if (isPressing) {
				if (!_repeatCounters.ContainsKey(input)) {
					LuaCounter counter = new LuaCounter(0, 1, interval_seconds, predicate);
					counter.SetLoop(true);
					counter.Start();
					_repeatCounters[input] = counter;
				}

				_repeatCounters[input].Tick();
			} else {
				if (_repeatCounters.ContainsKey(input)) {
					_repeatCounters[input].Stop();
					_repeatCounters.Remove(input);
				}
			}
		}

		public bool Released(string input)
			=> Enum.TryParse<EKeyConfigPad>(input, true, out var pad)
				&& OpenTaiko.Pad.IsReleased(EKeyConfigPart.Taiko, pad);
		public bool Releasing(string input)
			=> Enum.TryParse<EKeyConfigPad>(input, true, out var pad)
				&& OpenTaiko.Pad.IsReleasing(EKeyConfigPart.Taiko, pad);

		// Timed events

		// each key config part's inputs in pad order (name as Pressed takes it, player or -1)
		private static readonly (EKeyConfigPad pad, string name, int player)[][] _partPads = PartPads();
		private static (EKeyConfigPad, string, int)[][] PartPads() {
			var parts = new List<(EKeyConfigPad, string, int)>[(int)EKeyConfigPart.Total];
			for (int i = 0; i < parts.Length; ++i)
				parts[i] = new();
			for (var pad = EKeyConfigPad.Cancel; pad < EKeyConfigPad.Max; ++pad) {
				// drum-kit pads
				if (pad > EKeyConfigPad.Decide && pad < EKeyConfigPad.LRed)
					continue;
				// values shared with drum-kit names
				string name = pad switch {
					EKeyConfigPad.Cancel => nameof(EKeyConfigPad.Cancel),
					EKeyConfigPad.Decide => nameof(EKeyConfigPad.Decide),
					_ => pad.ToString(),
				};
				int player = NotesManager.GetPadPlayer((EPad)pad);
				parts[(int)CPad.ResolveKeyConfigPart(EKeyConfigPart.Taiko, pad)].Add((pad, name, player == int.MaxValue ? -1 : player));
			}
			return parts.Select(list => list.ToArray()).ToArray();
		}

		// a part by name, Taiko for none; -1 for an unknown name
		private static int PartIndex(string? part) {
			if (string.IsNullOrEmpty(part))
				return (int)EKeyConfigPart.Taiko;
			for (var p = (EKeyConfigPart)0; p < EKeyConfigPart.Total; ++p)
				if (string.Equals(p.ToString(), part, StringComparison.OrdinalIgnoreCase))
					return (int)p;
			return -1;
		}

		private static bool IsBound(CConfigIni.CKeyAssign.STKEYASSIGN[] binds, IInputDevice device, int key) {
			foreach (var b in binds)
				if (b.InputDevice == device.CurrentType && b.ID == device.ID && b.Code == key)
					return true;
			return false;
		}

		private static double ClockMs() => System.Diagnostics.Stopwatch.GetTimestamp() * 1000.0 / System.Diagnostics.Stopwatch.Frequency;

		// oldest first (ties in device order); each stamp moved onto fps.ms's clock by its age
		private static LuaInputEvent[] Timed(List<(STInputEvent ev, string name, int player)>? found) {
			if (found == null)
				return [];
			double now = ClockMs();
			// play timer ms (whole); no stamp before the sound device: the call time
			long? stampNow = SoundManager.PlayTimer?.msGetPreciseNowSoundTimerTime();
			return found.OrderBy(f => f.ev.nTimeStamp)
				.Select(f => new LuaInputEvent(f.name, f.player, f.ev.Pressed,
					(stampNow is long s && f.ev.nTimeStamp > 0) ? now - Math.Max(0, s - f.ev.nTimeStamp) : now))
				.ToArray();
		}

		// this frame's presses and releases of a key config part's inputs (the play screen's events)
		public LuaInputEvent[] GetEvents(string? part = "Taiko") {
			int p = PartIndex(part);
			var devices = OpenTaiko.InputManager?.InputDevices;
			if (p < 0 || Blocked || devices == null)
				return [];
			var binds = OpenTaiko.ConfigIni.KeyAssign[p];
			List<(STInputEvent ev, string name, int player)>? found = null;
			foreach (var device in devices) {
				if (device.InputEvents is not { Count: > 0 } events)
					continue;
				foreach (var ev in events)
					foreach (var (pad, name, player) in _partPads[p])
						if (IsBound(binds[(int)pad], device, ev.nKey))
							(found ??= new()).Add((ev, name, player));
			}
			return Timed(found);
		}

		// What the player bound to a general input in the key config, for showing it (in the order of the key config)
		private static List<CConfigIni.CKeyAssign.STKEYASSIGN> Bindings(string input) {
			var list = new List<CConfigIni.CKeyAssign.STKEYASSIGN>();
			if (!Enum.TryParse<EKeyConfigPad>(input, true, out var pad) || pad < 0) return list;
			var part = CPad.ResolveKeyConfigPart(EKeyConfigPart.Taiko, pad);
			if (part == EKeyConfigPart.Unknown) return list;
			foreach (var k in OpenTaiko.ConfigIni.KeyAssign[(int)part][(int)pad])
				if (k.InputDevice != InputDeviceType.Unknown) list.Add(k);
			return list;
		}
		public int GetBindingCount(string input) => Bindings(input).Count;
		// "Keyboard", "Gamepad", "Joystick", "MidiIn" or "Mouse"; "" out of range
		public string GetBindingDevice(string input, int index) {
			var list = Bindings(input);
			return (index >= 0 && index < list.Count) ? list[index].InputDevice.ToString() : "";
		}
		// a keyboard key as KeyboardPressed takes it ("Return"), the device's button name, a MIDI note, or a mouse
		// button as MousePressed takes it; "" out of range
		public string GetBindingName(string input, int index) {
			var list = Bindings(input);
			if (index < 0 || index >= list.Count) return "";
			var k = list[index];
			switch (k.InputDevice) {
				case InputDeviceType.Keyboard:
					return KeyName(k.Code);
				case InputDeviceType.MidiIn:
					return CInputMIDI.GetButtonName(k.Code);
				case InputDeviceType.Mouse:
					return k.Code switch { 0 => "left", 1 => "right", 2 => "middle", 3 => "button4", 4 => "button5", _ => k.Code.ToString() };
				default:
					var device = OpenTaiko.InputManager.FindDevice(k.InputDevice, k.ID);
					return device?.GetButtonName(k.Code) ?? $"Button{k.Code}";
			}
		}

		// Keyboard inputs
		public bool KeyboardPressed(string key) {
			if (Blocked) return false;
			if (Enum.TryParse(typeof(SlimDXKeys.Key), key, true, out var result)) {
				return OpenTaiko.InputManager.Keyboard.KeyPressed((int)result);
			}
			return false;
		}
		public bool KeyboardPressing(string key) {
			if (Blocked) return false;
			if (Enum.TryParse(typeof(SlimDXKeys.Key), key, true, out var result)) {
				return OpenTaiko.InputManager.Keyboard.KeyPressing((int)result);
			}
			return false;
		}

		public void KeyboardRepeatWhilePressing(string input, double interval_seconds, LuaFunction predicate) {
			bool isPressing = KeyboardPressing(input);

			if (isPressing) {
				if (!_repeatKbCounters.ContainsKey(input)) {
					LuaCounter counter = new LuaCounter(0, 1, interval_seconds, predicate);
					counter.SetLoop(true);
					counter.Start();
					_repeatKbCounters[input] = counter;
				}

				_repeatKbCounters[input].Tick();
			} else {
				if (_repeatKbCounters.ContainsKey(input)) {
					_repeatKbCounters[input].Stop();
					_repeatKbCounters.Remove(input);
				}
			}
		}

		public bool KeyboardReleasing(string key) {
			if (Enum.TryParse(typeof(SlimDXKeys.Key), key, true, out var result)) {
				return OpenTaiko.InputManager.Keyboard.KeyReleasing((int)result);
			}
			return false;
		}
		public bool KeyboardReleased(string key) {
			if (Blocked) return false;
			if (Enum.TryParse(typeof(SlimDXKeys.Key), key, true, out var result)) {
				return OpenTaiko.InputManager.Keyboard.KeyReleased((int)result);
			}
			return false;
		}

		// a key as KeyboardPressed takes it
		private static string KeyName(int code)
			=> Enum.IsDefined(typeof(SlimDXKeys.Key), code) ? ((SlimDXKeys.Key)code).ToString() : code.ToString();

		// this frame's key presses and releases
		public LuaInputEvent[] GetKeyboardEvents() {
			var events = OpenTaiko.InputManager?.Keyboard.InputEvents;
			if (Blocked || events is not { Count: > 0 })
				return [];
			return Timed(events.Select(ev => (ev, KeyName(ev.nKey), -1)).ToList());
		}

		// Text input
		/// <summary>
		/// Creates a text-input widget pre-filled with <paramref name="initialText"/>.
		/// Call <see cref="LuaTextInput.Update"/> each frame inside <c>update()</c>; it returns
		/// <c>true</c> when the user presses Enter to confirm.
		/// Draw <see cref="LuaTextInput.DisplayText"/> in your Lua UI to show the live text.
		/// </summary>
		public LuaTextInput CreateTextInput(string initialText, int maxLength = 64) {
			return new LuaTextInput(initialText ?? "", maxLength);
		}

		// Mouse inputs
		//
		// The rendered surface is aspect-fit inside the window (letterbox borders when
		// the window and game resolutions differ). These helpers convert the raw window
		// mouse position into game-surface coordinates (the same space scripts draw in,
		// i.e. 0..GameWindowSize), so they are correct at any window size/resolution.

		private CInputMouse? MouseDevice => OpenTaiko.InputManager?.Mouse as CInputMouse;

		private (double x, double y) MouseSurfacePosition() {
			var m = MouseDevice;
			if (m == null || Blocked) return (-1, -1);
			int vw = Game.ViewPortSize.X, vh = Game.ViewPortSize.Y;
			if (vw <= 0 || vh <= 0) return (-1, -1);
			double sx = (m.Position.x - Game.ViewPortOffset.X) / (double)vw * GameWindowSize.Width;
			double sy = (m.Position.y - Game.ViewPortOffset.Y) / (double)vh * GameWindowSize.Height;
			return (sx, sy);
		}

		/// <summary>Mouse X in game-surface coordinates (0..surface width). -1 if no mouse.</summary>
		public double GetMouseX() => MouseSurfacePosition().x;
		/// <summary>Mouse Y in game-surface coordinates (0..surface height). -1 if no mouse.</summary>
		public double GetMouseY() => MouseSurfacePosition().y;
		/// <summary>Mouse position as two values: local x, y = INPUT:GetMouseXY()</summary>
		public (double, double) GetMouseXY() => MouseSurfacePosition();

		// ── Free-look helpers ────────────────────────────────────────────────────────
		private int _pmx, _pmy;
		private bool _havePrev;

		/// <summary>Lock+hide the cursor for free-look (or restore it). Resets the delta baseline.</summary>
		public void SetMouseLocked(bool locked) {
			var m = MouseDevice; if (m == null) return;
			m.SetCursorLocked(locked);
			_pmx = m.Position.x; _pmy = m.Position.y; _havePrev = true;
		}

		/// <summary>Raw mouse movement since the last call (window pixels): local dx, dy = INPUT:GetMouseDelta()</summary>
		public (double, double) GetMouseDelta() {
			var m = MouseDevice; if (m == null) return (0, 0);
			int cx = m.Position.x, cy = m.Position.y;
			if (!_havePrev) { _pmx = cx; _pmy = cy; _havePrev = true; return (0, 0); }
			double dx = cx - _pmx, dy = cy - _pmy;
			_pmx = cx; _pmy = cy;
			return Blocked ? (0, 0) : (dx, dy);
		}

		/// <summary>Wheel movement since the last call: local dx, dy = INPUT:GetScrollDelta().
		/// dy is positive when scrolling up/away. Reading resets the accumulator.</summary>
		public (double, double) GetScrollDelta() {
			var m = MouseDevice; if (m == null) return (0, 0);
			var (x, y) = m.TakeScroll();
			return Blocked ? (0, 0) : (x, y);
		}

		/// <summary>True when the mouse is within the rendered surface (not on a letterbox border).</summary>
		public bool IsMouseInside() {
			var (x, y) = MouseSurfacePosition();
			return x >= 0 && y >= 0 && x < GameWindowSize.Width && y < GameWindowSize.Height;
		}

		/// <summary>Size of the game surface (the coordinate space scripts draw in).</summary>
		public int GetSurfaceWidth() => GameWindowSize.Width;
		public int GetSurfaceHeight() => GameWindowSize.Height;

		// Button names: "left", "right", "middle", "button4", "button5", or a numeric index.
		private static int MouseButtonIndex(string button) {
			switch ((button ?? "").ToLower()) {
				case "left": return 0;
				case "right": return 1;
				case "middle": return 2;
				case "button4": return 3;
				case "button5": return 4;
				default: return int.TryParse(button, out var i) ? i : -1;
			}
		}

		public bool MousePressing(string button) {
			var m = MouseDevice; if (m == null || Blocked) return false;
			int idx = MouseButtonIndex(button);
			return idx >= 0 && idx < m.ButtonStates.Length && m.KeyPressing(idx);
		}
		public bool MousePressed(string button) {
			var m = MouseDevice; if (m == null || Blocked) return false;
			int idx = MouseButtonIndex(button);
			return idx >= 0 && idx < m.ButtonStates.Length && m.KeyPressed(idx);
		}
		public bool MouseReleased(string button) {
			var m = MouseDevice; if (m == null || Blocked) return false;
			int idx = MouseButtonIndex(button);
			return idx >= 0 && idx < m.ButtonStates.Length && m.KeyReleased(idx);
		}
	}

	// a press or release; Ms on fps.ms's clock
	public sealed class LuaInputEvent {
		public string Name { get; }
		public int Player { get; }
		public bool Pressed { get; }
		public double Ms { get; }

		internal LuaInputEvent(string name, int player, bool pressed, double ms) {
			Name = name;
			Player = player;
			Pressed = pressed;
			Ms = ms;
		}
	}
}
