﻿using System.Runtime.InteropServices;
using FDK;

namespace OpenTaiko {
	public class CPad {
		// Properties

		internal STHIT st検知したデバイス;
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
			this.rInput管理 = mgrInput;
			this.st検知したデバイス.Clear();
		}


		// メソッド

		public List<STInputEvent> GetEvents(EInstrumentPad part, EPad pad) {
			CConfigIni.CKeyAssign.STKEYASSIGN[] stkeyassignArray = this.rConfigIni.KeyAssign[(int)part][(int)pad];
			List<STInputEvent> list = new List<STInputEvent>();

			// すべての入力デバイスについて…
			foreach (IInputDevice device in this.rInput管理.InputDevices) {
				if ((device.InputEvents != null) && (device.InputEvents.Count != 0)) {
					foreach (STInputEvent event2 in device.InputEvents) {
						for (int i = 0; i < stkeyassignArray.Length; i++) {
							switch (stkeyassignArray[i].InputDevice) {
								case EInputDevice.Keyboard:
									if ((device.CurrentType == InputDeviceType.Keyboard) && (event2.nKey == stkeyassignArray[i].Code)) {
										list.Add(event2);
										this.st検知したデバイス.Keyboard = true;
									}
									break;

								case EInputDevice.MIDIInput:
									if (((device.CurrentType == InputDeviceType.MidiIn) && (device.ID == stkeyassignArray[i].ID)) && (event2.nKey == stkeyassignArray[i].Code)) {
										list.Add(event2);
										this.st検知したデバイス.MIDIIN = true;
									}
									break;

								case EInputDevice.Joypad:
									if (((device.CurrentType == InputDeviceType.Joystick) && (device.ID == stkeyassignArray[i].ID)) && (event2.nKey == stkeyassignArray[i].Code)) {
										list.Add(event2);
										this.st検知したデバイス.Joypad = true;
									}
									break;

								case EInputDevice.Gamepad:
									if (((device.CurrentType == InputDeviceType.Gamepad) && (device.ID == stkeyassignArray[i].ID)) && (event2.nKey == stkeyassignArray[i].Code)) {
										list.Add(event2);
										this.st検知したデバイス.Gamepad = true;
									}
									break;

								case EInputDevice.Mouse:
									if ((device.CurrentType == InputDeviceType.Mouse) && (event2.nKey == stkeyassignArray[i].Code)) {
										list.Add(event2);
										this.st検知したデバイス.Mouse = true;
									}
									break;
							}
						}
					}
					continue;
				}
			}
			return list;
		}
		public bool bPressed(EInstrumentPad part, EPad pad) {
			if (part != EInstrumentPad.Unknown) {

				CConfigIni.CKeyAssign.STKEYASSIGN[] stkeyassignArray = this.rConfigIni.KeyAssign[(int)part][(int)pad];
				for (int i = 0; i < stkeyassignArray.Length; i++) {
					switch (stkeyassignArray[i].InputDevice) {
						case EInputDevice.Keyboard:
							if (!this.rInput管理.Keyboard.KeyPressed(stkeyassignArray[i].Code))
								break;

							this.st検知したデバイス.Keyboard = true;
							return true;

						case EInputDevice.MIDIInput: {
								IInputDevice device2 = this.rInput管理.MidiIn(stkeyassignArray[i].ID);
								if ((device2 == null) || !device2.KeyPressed(stkeyassignArray[i].Code))
									break;

								this.st検知したデバイス.MIDIIN = true;
								return true;
							}
						case EInputDevice.Joypad: {
								if (!this.rConfigIni.dicJoystick.ContainsKey(stkeyassignArray[i].ID))
									break;

								IInputDevice device = this.rInput管理.Joystick(stkeyassignArray[i].ID);
								if ((device == null) || !device.KeyPressed(stkeyassignArray[i].Code))
									break;

								this.st検知したデバイス.Joypad = true;
								return true;
							}
						case EInputDevice.Gamepad: {
								if (!this.rConfigIni.dicJoystick.ContainsKey(stkeyassignArray[i].ID))
									break;

								IInputDevice device = this.rInput管理.Gamepad(stkeyassignArray[i].ID);
								if ((device == null) || !device.KeyPressed(stkeyassignArray[i].Code))
									break;

								this.st検知したデバイス.Gamepad = true;
								return true;
							}
						case EInputDevice.Mouse:
							if (!this.rInput管理.Mouse.KeyPressed(stkeyassignArray[i].Code))
								break;

							this.st検知したデバイス.Mouse = true;
							return true;
					}
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
			if (!this.bPressed(EInstrumentPad.Guitar, pad)) {
				return this.bPressed(EInstrumentPad.Bass, pad);
			}
			return true;
		}
		public bool b押されている(EInstrumentPad part, EPad pad) {
			if (part != EInstrumentPad.Unknown) {
				CConfigIni.CKeyAssign.STKEYASSIGN[] stkeyassignArray = this.rConfigIni.KeyAssign[(int)part][(int)pad];
				for (int i = 0; i < stkeyassignArray.Length; i++) {
					switch (stkeyassignArray[i].InputDevice) {
						case EInputDevice.Keyboard:
							if (!this.rInput管理.Keyboard.KeyPressing(stkeyassignArray[i].Code)) {
								break;
							}
							this.st検知したデバイス.Keyboard = true;
							return true;

						case EInputDevice.Joypad: {
								if (!this.rConfigIni.dicJoystick.ContainsKey(stkeyassignArray[i].ID)) {
									break;
								}
								IInputDevice device = this.rInput管理.Joystick(stkeyassignArray[i].ID);
								if ((device == null) || !device.KeyPressing(stkeyassignArray[i].Code)) {
									break;
								}
								this.st検知したデバイス.Joypad = true;
								return true;
							}

						case EInputDevice.Gamepad: {
								if (!this.rConfigIni.dicJoystick.ContainsKey(stkeyassignArray[i].ID)) {
									break;
								}
								IInputDevice device = this.rInput管理.Gamepad(stkeyassignArray[i].ID);
								if ((device == null) || !device.KeyPressing(stkeyassignArray[i].Code)) {
									break;
								}
								this.st検知したデバイス.Gamepad = true;
								return true;
							}
						case EInputDevice.Mouse:
							if (!this.rInput管理.Mouse.KeyPressing(stkeyassignArray[i].Code)) {
								break;
							}
							this.st検知したデバイス.Mouse = true;
							return true;
					}
				}
			}
			return false;
		}
		public bool b押されているGB(EPad pad) {
			if (!this.b押されている(EInstrumentPad.Guitar, pad)) {
				return this.b押されている(EInstrumentPad.Bass, pad);
			}
			return true;
		}


		// その他

		#region [ private ]
		//-----------------
		private CConfigIni rConfigIni;
		private CInputManager rInput管理;
		//-----------------
		#endregion
	}
}
