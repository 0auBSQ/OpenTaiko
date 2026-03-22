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
	public List<STInputEvent> GetEvents(EInstrumentPad part, EPad pad) {
		CConfigIni.CKeyAssign.STKEYASSIGN[] stkeyassignArray = this.rConfigIni.KeyAssign[(int)part][(int)pad];
		List<STInputEvent> list = new List<STInputEvent>();

		// すべての入力デバイスについて…
		foreach (IInputDevice device in this.inputManager.InputDevices) {
			if (device.InputEvents == null || device.InputEvents.Count == 0) {
				continue;
			}

			foreach (STInputEvent event2 in device.InputEvents) {
				for (int i = 0; i < stkeyassignArray.Length; i++) {
					if ((device.CurrentType == stkeyassignArray[i].InputDevice)
						&& (device.ID == stkeyassignArray[i].ID)
						&& (event2.nKey == stkeyassignArray[i].Code)
						) {
						list.Add(event2);
						this.detectedDevice[stkeyassignArray[i].InputDevice] = true;
					}
				}
			}
		}
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

	#region [ private ]
	//-----------------
	private CConfigIni rConfigIni;
	private CInputManager inputManager;
	//-----------------
	#endregion
}
