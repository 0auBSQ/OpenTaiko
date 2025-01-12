﻿using System.Diagnostics;
using Silk.NET.Input;
using Silk.NET.Windowing;

namespace FDK;

public class CInputManager : IDisposable {
	// 定数

	public static int DefaultVolume = 110;


	// Properties

	public List<IInputDevice> InputDevices {
		get;
		private set;
	}
	public IInputDevice Keyboard {
		get {
			if (this._Keyboard != null) {
				return this._Keyboard;
			}
			foreach (IInputDevice device in this.InputDevices) {
				if (device.CurrentType == InputDeviceType.Keyboard) {
					this._Keyboard = device;
					return device;
				}
			}
			return null;
		}
	}
	public IInputDevice Mouse {
		get {
			if (this._Mouse != null) {
				return this._Mouse;
			}
			foreach (IInputDevice device in this.InputDevices) {
				if (device.CurrentType == InputDeviceType.Mouse) {
					this._Mouse = device;
					return device;
				}
			}
			return null;
		}
	}
	public float Deadzone = 0.5f;


	// Constructor
	public CInputManager(IWindow window, bool useBufferedInput, bool bUseMidiIn = true, float gamepad_deadzone = 0.5f) {
		Initialize(window, useBufferedInput, bUseMidiIn, gamepad_deadzone);
	}

	public void Initialize(IWindow window, bool useBufferedInput, bool bUseMidiIn, float controller_deadzone) {
		Context = window.CreateInput();
		Context.ConnectionChanged += this.ConnectionChanged;
		Deadzone = controller_deadzone;

		this.InputDevices = new List<IInputDevice>(10);
		#region [ Enumerate keyboard/mouse: exception is masked if keyboard/mouse is not connected ]
		CInputKeyboard cinputkeyboard = null;
		CInputMouse cinputmouse = null;
		try {
			cinputkeyboard = new CInputKeyboard(Context.Keyboards[0]);
			cinputmouse = new CInputMouse(Context.Mice[0]);
		} catch {
		}
		if (cinputkeyboard != null) {
			this.InputDevices.Add(cinputkeyboard);
		}
		if (cinputmouse != null) {
			this.InputDevices.Add(cinputmouse);
		}
		#endregion
		#region [ Enumerate joypad ]
		foreach (var joysticks in Context.Joysticks) {
			this.InputDevices.Add(new CInputJoystick(joysticks));
		}
		foreach (var gamepad in Context.Gamepads) {
			this.InputDevices.Add(new CInputGamepad(gamepad, Deadzone));
		}
		#endregion
		Trace.TraceInformation("Found {0} Input Device{1}", InputDevices.Count, InputDevices.Count != 1 ? "s:" : ":");
		for (int i = 0; i < InputDevices.Count; i++) {
			try {
				Trace.TraceInformation("Input Device #" + i + " (" + InputDevices[i].CurrentType.ToString() + " - " + InputDevices[i].Name + ")");
			} catch { }
		}

		Game.InitImGuiController(window, Context);
	}

	private void ConnectionChanged(Silk.NET.Input.IInputDevice device, bool connected) {
		if (connected) {
			if (device is IKeyboard) {
				if (Keyboard == null) {
					this.InputDevices.Add(new CInputKeyboard((IKeyboard)device));
					Trace.TraceInformation($"A keyboard was connected. Device name: {device.Name}");
				}
				else {
					Trace.TraceWarning($"A keyboard was connected, but there is another keyboard already loaded. This keyboard will not be used. Device name: {device.Name}");
				}
			}
			else if (device is IMouse) {
				if (Mouse == null) {
					this.InputDevices.Add(new CInputMouse((IMouse)device));
					Trace.TraceInformation($"A mouse was connected. Device name: {device.Name}");
				} else {
					Trace.TraceWarning($"A mouse was connected, but there is another mouse already loaded. This mouse will not be used. Device name: {device.Name}");
				}
			}
			else if (device is IGamepad) {
				this.InputDevices.Add(new CInputGamepad((IGamepad)device, Deadzone));
				Trace.TraceInformation($"A gamepad was connected. Device name: {device.Name} / Index: {device.Index}");
			}
			else if (device is IJoystick) {
				this.InputDevices.Add(new CInputJoystick((IJoystick)device));
				Trace.TraceInformation($"A joystick was connected. Device name: {device.Name} / Index: {device.Index}");
			}
			else {
				Trace.TraceWarning($"An input device was connected, but Silk.NET could not recognize what type of device this is. It will not be used. Device name: {device.Name}");
			}
		}
		else {
			for (int i = InputDevices.Count; i-- > 0;) {
				var inputdevice = InputDevices[i];
				if (!inputdevice.Device.IsConnected) {
					Trace.TraceInformation($"An input device was disconnected. Device name: {inputdevice.Name} / Index: {inputdevice.ID} / Device Type: {inputdevice.CurrentType}");
					inputdevice.Dispose();
					this.InputDevices.Remove(inputdevice);
				}
			}
		}
	}


	// メソッド

	public IInputDevice Joystick(int ID) {
		foreach (IInputDevice device in this.InputDevices) {
			if ((device.CurrentType == InputDeviceType.Joystick) && (device.ID == ID)) {
				return device;
			}
		}
		return null;
	}
	public IInputDevice Joystick(string GUID) {
		foreach (IInputDevice device in this.InputDevices) {
			if ((device.CurrentType == InputDeviceType.Joystick) && device.GUID.Equals(GUID)) {
				return device;
			}
		}
		return null;
	}
	public IInputDevice Gamepad(int ID) {
		foreach (IInputDevice device in this.InputDevices) {
			if ((device.CurrentType == InputDeviceType.Gamepad) && (device.ID == ID)) {
				return device;
			}
		}
		return null;
	}
	public IInputDevice Gamepad(string GUID) {
		foreach (IInputDevice device in this.InputDevices) {
			if ((device.CurrentType == InputDeviceType.Gamepad) && device.GUID.Equals(GUID)) {
				return device;
			}
		}
		return null;
	}
	public IInputDevice MidiIn(int ID) {
		foreach (IInputDevice device in this.InputDevices) {
			if ((device.CurrentType == InputDeviceType.MidiIn) && (device.ID == ID)) {
				return device;
			}
		}
		return null;
	}
	public void SetUseBufferInput(bool useBufferInput) {
		lock (this.objMidiIn排他用) {
			for (int i = this.InputDevices.Count - 1; i >= 0; i--)
			{
				IInputDevice device = this.InputDevices[i];
				device.useBufferInput = useBufferInput;
			}
		}
	}
	public void Polling() {
		lock (this.objMidiIn排他用) {
			//				foreach( IInputDevice device in this.list入力デバイス )
			for (int i = this.InputDevices.Count - 1; i >= 0; i--)    // #24016 2011.1.6 yyagi: change not to use "foreach" to avoid InvalidOperation exception by Remove().
			{
				try {
					IInputDevice device = this.InputDevices[i];
					device.Polling();
				} catch (Exception e)                                      // #24016 2011.1.6 yyagi: catch exception for unplugging USB joystick, and remove the device object from the polling items.
				{
					//this.InputDevices.Remove(device);
					//device.Dispose();
					//Trace.TraceError("tポーリング時に対象deviceが抜かれており例外発生。同deviceをポーリング対象からRemoveしました。");
				}
			}
		}
	}

	#region [ IDisposable＋α ]
	//-----------------
	public void Dispose() {
		this.Dispose(true);
	}
	public void Dispose(bool disposeManagedObjects) {
		if (!this.bDisposed済み) {
			if (disposeManagedObjects) {
				foreach (IInputDevice device in this.InputDevices) {
					CInputMIDI tmidi = device as CInputMIDI;
					if (tmidi != null) {
						Trace.TraceInformation("MIDI In: [{0}] を停止しました。", new object[] { tmidi.ID });
					}
				}
				foreach (IInputDevice device2 in this.InputDevices) {
					device2.Dispose();
				}
				lock (this.objMidiIn排他用) {
					this.InputDevices.Clear();
				}

				Context.Dispose();
			}
			this.bDisposed済み = true;
		}
	}
	~CInputManager() {
		this.Dispose(false);
		GC.KeepAlive(this);
	}
	//-----------------
	#endregion


	// その他

	#region [ private ]
	//-----------------
	private IInputContext Context;
	private IInputDevice _Keyboard;
	private IInputDevice _Mouse;
	private bool bDisposed済み;
	private List<uint> listHMIDIIN = new List<uint>(8);
	private object objMidiIn排他用 = new object();
	//private CTimer timer;

	//-----------------
	#endregion
}
