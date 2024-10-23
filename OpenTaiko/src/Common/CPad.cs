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
		public void Clear() {
			this.Keyboard = false;
			this.MIDIIN = false;
			this.Joypad = false;
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
					switch (stkeyassignArray[i].InputDevice) {
						case EInputDevice.Keyboard:
							if ((device.CurrentType == InputDeviceType.Keyboard) && (event2.nKey == stkeyassignArray[i].Code)) {
								list.Add(event2);
								this.detectedDevice.Keyboard = true;
							}
							break;

						case EInputDevice.MIDIInput:
							if (((device.CurrentType == InputDeviceType.MidiIn) && (device.ID == stkeyassignArray[i].ID)) && (event2.nKey == stkeyassignArray[i].Code)) {
								list.Add(event2);
								this.detectedDevice.MIDIIN = true;
							}
							break;

						case EInputDevice.Joypad:
							if (((device.CurrentType == InputDeviceType.Joystick) && (device.ID == stkeyassignArray[i].ID)) && (event2.nKey == stkeyassignArray[i].Code)) {
								list.Add(event2);
								this.detectedDevice.Joypad = true;
							}
							break;

						case EInputDevice.Gamepad:
							if (((device.CurrentType == InputDeviceType.Gamepad) && (device.ID == stkeyassignArray[i].ID)) && (event2.nKey == stkeyassignArray[i].Code)) {
								list.Add(event2);
								this.detectedDevice.Gamepad = true;
							}
							break;

						case EInputDevice.Mouse:
							if ((device.CurrentType == InputDeviceType.Mouse) && (event2.nKey == stkeyassignArray[i].Code)) {
								list.Add(event2);
								this.detectedDevice.Mouse = true;
							}
							break;
					}
				}
			}
		}
		return list;
	}

	public bool bPressed(EInstrumentPad part, EPad pad) {
		if (part == EInstrumentPad.Unknown) {
			return false;
		}

		CConfigIni.CKeyAssign.STKEYASSIGN[] stkeyassignArray = this.rConfigIni.KeyAssign[(int)part][(int)pad];
		for (int i = 0; i < stkeyassignArray.Length; i++) {
			switch (stkeyassignArray[i].InputDevice) {
				case EInputDevice.Keyboard:
					if (!this.inputManager.Keyboard.KeyPressed(stkeyassignArray[i].Code))
						break;

					this.detectedDevice.Keyboard = true;
					return true;

				case EInputDevice.MIDIInput: {
						IInputDevice device2 = this.inputManager.MidiIn(stkeyassignArray[i].ID);
						if (device2 == null || !device2.KeyPressed(stkeyassignArray[i].Code))
							break;

						this.detectedDevice.MIDIIN = true;
						return true;
					}
				case EInputDevice.Joypad: {
						if (!this.rConfigIni.dicJoystick.ContainsKey(stkeyassignArray[i].ID))
							break;

						IInputDevice device = this.inputManager.Joystick(stkeyassignArray[i].ID);
						if (device == null || !device.KeyPressed(stkeyassignArray[i].Code))
							break;

						this.detectedDevice.Joypad = true;
						return true;
					}
				case EInputDevice.Gamepad: {
						if (!this.rConfigIni.dicJoystick.ContainsKey(stkeyassignArray[i].ID))
							break;

						IInputDevice device = this.inputManager.Gamepad(stkeyassignArray[i].ID);
						if (device == null || !device.KeyPressed(stkeyassignArray[i].Code))
							break;

						this.detectedDevice.Gamepad = true;
						return true;
					}
				case EInputDevice.Mouse:
					if (!this.inputManager.Mouse.KeyPressed(stkeyassignArray[i].Code))
						break;

					this.detectedDevice.Mouse = true;
					return true;
			}
		}
		return false;
	}

	public bool bPressedDGB(EPad pad) {
		if (!this.bPressed(EInstrumentPad.Drums, pad) && !this.bPressed(EInstrumentPad.Guitar, pad)) {
			return this.bPressed(EInstrumentPad.Bass, pad);
		}
		return true;
	}

	public bool bPressedGB(EPad pad) {
		return this.bPressed(EInstrumentPad.Guitar, pad) || this.bPressed(EInstrumentPad.Bass, pad);
	}

	public bool IsPressing(EInstrumentPad part, EPad pad) {
		if (part == EInstrumentPad.Unknown) {
			return false;
		}

		CConfigIni.CKeyAssign.STKEYASSIGN[] stkeyassignArray = this.rConfigIni.KeyAssign[(int)part][(int)pad];
		for (int i = 0; i < stkeyassignArray.Length; i++) {
			switch (stkeyassignArray[i].InputDevice) {
				case EInputDevice.Keyboard:
					if (!this.inputManager.Keyboard.KeyPressing(stkeyassignArray[i].Code)) {
						break;
					}
					this.detectedDevice.Keyboard = true;
					return true;

				case EInputDevice.Joypad: {
						if (!this.rConfigIni.dicJoystick.ContainsKey(stkeyassignArray[i].ID)) {
							break;
						}
						IInputDevice device = this.inputManager.Joystick(stkeyassignArray[i].ID);
						if (device == null || !device.KeyPressing(stkeyassignArray[i].Code)) {
							break;
						}
						this.detectedDevice.Joypad = true;
						return true;
					}

				case EInputDevice.Gamepad: {
						if (!this.rConfigIni.dicJoystick.ContainsKey(stkeyassignArray[i].ID)) {
							break;
						}
						IInputDevice device = this.inputManager.Gamepad(stkeyassignArray[i].ID);
						if (device == null || !device.KeyPressing(stkeyassignArray[i].Code)) {
							break;
						}
						this.detectedDevice.Gamepad = true;
						return true;
					}
				case EInputDevice.Mouse:
					if (!this.inputManager.Mouse.KeyPressing(stkeyassignArray[i].Code)) {
						break;
					}
					this.detectedDevice.Mouse = true;
					return true;
			}
		}
		return false;
	}

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
