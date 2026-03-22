using System.Runtime.InteropServices;
using FDK;

namespace OpenTaiko;

public class CPad {
	// Properties

	internal STHIT detectedDevice;
	[StructLayout(LayoutKind.Sequential)]
	internal struct STHIT {
		public bool Keyboard;
		public bool MIDIIN;
		public bool Joypad;
		public bool Gamepad;
		public bool Mouse;
		public bool this[InputDeviceType type] {
			get => type switch {
				InputDeviceType.Keyboard => Keyboard,
				InputDeviceType.Mouse => Mouse,
				InputDeviceType.Joystick => Joypad,
				InputDeviceType.Gamepad => Gamepad,
				InputDeviceType.MidiIn => MIDIIN,
				_ => throw new IndexOutOfRangeException(),
			};
			set {
				switch (type) {
					case InputDeviceType.Keyboard: Keyboard = value; break;
					case InputDeviceType.Mouse: Mouse = value; break;
					case InputDeviceType.Joystick: Joypad = value; break;
					case InputDeviceType.Gamepad: Gamepad = value; break;
					case InputDeviceType.MidiIn: MIDIIN = value; break;
					default: throw new IndexOutOfRangeException();
				}
			}
		}
		public void Clear() {
			this.Keyboard = false;
			this.MIDIIN = false;
			this.Joypad = false;
			this.Gamepad = false;
			this.Mouse = false;
		}
	}

	// Constructor
	internal CPad(CConfigIni configIni, CInputManager mgrInput) {
		this.rConfigIni = configIni;
		this.inputManager = mgrInput;
		this.detectedDevice.Clear();
	}

	// Methods
	public List<(EPad pad, STInputEvent inputEvent, int order)> GetEvents(EInstrumentPad part) {
		var stkeyassignArray = this.rConfigIni.KeyAssign[(int)part];
		List<(EPad pad, STInputEvent inputEvent, int order)> list = new();
		// すべての入力デバイスについて…
		foreach (IInputDevice device in this.inputManager.InputDevices) {
			if (device.InputEvents == null || device.InputEvents.Count == 0) {
				continue;
			}

			foreach (STInputEvent event2 in device.InputEvents) {
				var pads = this.InputToPads(device.CurrentType, device.ID, event2.nKey);
				if (pads.Count <= 0)
					continue;
				this.detectedDevice[device.CurrentType] = true;
				foreach (EPad pad in pads)
					list.Add((pad, event2, list.Count));
			}
		}
		list.Sort((lhs, rhs) => (lhs.inputEvent.nTimeStamp, lhs.order).CompareTo((rhs.inputEvent.nTimeStamp, rhs.order)));
		return list;
	}

	public bool HasInput(EInstrumentPad part, EPad pad, Func<IInputDevice?, int, bool> predicate) {
		if (part == EInstrumentPad.Unknown) {
			return false;
		}

		var device = this.rConfigIni.KeyAssign[(int)part][(int)pad].GetDevice(predicate);
		if (device != null && (device.CurrentType >= 0 && device.CurrentType < InputDeviceType.Total)) {
			this.detectedDevice[device.CurrentType] = true;
			return true;
		}
		return false;
	}

	public bool bPressed(EInstrumentPad part, EPad pad)
		=> HasInput(part, pad, (device, keyCode) => device?.KeyPressed(keyCode) ?? false);

	public bool bPressedDGB(EPad pad) {
		if (!this.bPressed(EInstrumentPad.Drums, pad) && !this.bPressed(EInstrumentPad.Guitar, pad)) {
			return this.bPressed(EInstrumentPad.Bass, pad);
		}
		return true;
	}

	public bool bPressedGB(EPad pad) {
		return this.bPressed(EInstrumentPad.Guitar, pad) || this.bPressed(EInstrumentPad.Bass, pad);
	}

	public bool IsPressing(EInstrumentPad part, EPad pad)
		=> HasInput(part, pad, (device, keyCode) => device?.KeyPressing(keyCode) ?? false);

	public bool IsPressingGB(EPad pad) {
		return this.IsPressing(EInstrumentPad.Guitar, pad) || this.IsPressing(EInstrumentPad.Bass, pad);
	}
	public void InvalidateInputToPadCache() => inputToPadCacheValid = false;

	#region [ private ]
	//-----------------
	private CConfigIni rConfigIni;
	private CInputManager inputManager;

	private readonly Dictionary<(InputDeviceType type, int id, int key), SortedSet<EPad>> inputToPadCache = new();
	private bool inputToPadCacheValid = false;

	private SortedSet<EPad> InputToPads(InputDeviceType type, int id, int key) {
		var cacheKey = (type, id, key);
		if (!this.inputToPadCacheValid)
			this.RebuildInputToPadCache();
		if (this.inputToPadCache.TryGetValue(cacheKey, out var res))
			return res;
		return [];
	}

	private void RebuildInputToPadCache() {
		this.inputToPadCache.Clear();
		for (EInstrumentPad part = 0; part < EInstrumentPad.Total; ++part) {
			for (EPad pad = 0; pad < EPad.Max; ++pad) {
				var keyAssigns = this.rConfigIni.KeyAssign[(int)part][(int)pad];
				for (int i = 0; i < keyAssigns.Length; ++i) {
					if (keyAssigns[i].InputDevice == InputDeviceType.Unknown)
						continue;
					var cacheKey = (keyAssigns[i].InputDevice, keyAssigns[i].ID, keyAssigns[i].Code);
					if (!this.inputToPadCache.TryGetValue(cacheKey, out var pads)) {
						pads = [];
						this.inputToPadCache.Add(cacheKey, pads);
					}
					pads.Add(pad);
				}
			}
		}
	}
	//-----------------
	#endregion
}
