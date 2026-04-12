using System.Diagnostics;
using Silk.NET.Input;
using Silk.NET.Windowing;
using Commons.Music.Midi;

namespace FDK;

public class CInputManager : IDisposable {
	// 定数

	public static int DefaultVolume = 110;


	// Properties

	public List<IInputDevice> InputDevices {
		get;
		private set;
	}
	public IInputDevice? Keyboard => this._Keyboard ??= this.InputDevices.FirstOrDefault(device => device.CurrentType == InputDeviceType.Keyboard);
	public IInputDevice? Mouse => this._Mouse ??= this.InputDevices.FirstOrDefault(device => device.CurrentType == InputDeviceType.Mouse);
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
		} catch (Exception ex) {
			Trace.TraceWarning(ex.ToString());
			Trace.TraceWarning("Error adding keyboard and mouse.");
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
		#region [ Enumerate MIDI device ]
		try {
			foreach (var (v, i) in MidiAccessManager.Default.Inputs.Select((v, i) => (v, i))) {
				var midiIn = MidiAccessManager.Default.OpenInputAsync(v.Id).Result;
				this.InputDevices.Add(new CInputMIDI(midiIn, i));
			}
		} catch (Exception e) {
			Trace.TraceWarning(e.ToString());
			Trace.TraceWarning("Error adding MIDI input devices.");
		}
		#endregion
		Trace.TraceInformation("Found {0} Input Device{1}", InputDevices.Count, InputDevices.Count != 1 ? "s:" : ":");
		for (int i = 0; i < InputDevices.Count; i++) {
			try {
				Trace.TraceInformation("Input Device #" + i + " (" + InputDevices[i].CurrentType.ToString() + " - " + InputDevices[i].Name + ")");
			} catch (Exception ex) {
				Trace.TraceWarning(ex.ToString());
				Trace.TraceWarning("Error logging input devices.");
			}
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
				var isClosed = inputdevice.DeviceGeneric switch {
					Silk.NET.Input.IInputDevice deviceSilk => !deviceSilk.IsConnected,
					IMidiInput deviceMidi => deviceMidi.Connection is MidiPortConnectionState.Closed,
					_ => true,
				};
				if (isClosed) {
					Trace.TraceInformation($"An input device was disconnected. Device name: {inputdevice.Name} / Index: {inputdevice.ID} / Device Type: {inputdevice.CurrentType}");
					inputdevice.Dispose();
					this.InputDevices.Remove(inputdevice);
				}
			}
		}
	}


	// メソッド

	public IInputDevice? FindDevice(InputDeviceType type, int ID)
		=> InputDevices.FirstOrDefault(device => (device.CurrentType == type) && (device.ID == ID));
	public IInputDevice? FindDevice(InputDeviceType type, string GUID)
		=> InputDevices.FirstOrDefault(device => (device.CurrentType == type) && device.GUID.Equals(GUID));
	public IInputDevice? Joystick(int ID) => FindDevice(InputDeviceType.Joystick, ID);
	public IInputDevice? Joystick(string GUID) => FindDevice(InputDeviceType.Joystick, GUID);
	public IInputDevice? Gamepad(int ID) => FindDevice(InputDeviceType.Gamepad, ID);
	public IInputDevice? Gamepad(string GUID) => FindDevice(InputDeviceType.Gamepad, GUID);
	public IInputDevice? MidiIn(int ID) => FindDevice(InputDeviceType.MidiIn, ID);
	public IInputDevice? MidiIn(string GUID) => FindDevice(InputDeviceType.MidiIn, GUID);

	public void SetUseBufferInput(bool useBufferInput) {
		lock (this.lockInputDevices) {
			for (int i = this.InputDevices.Count - 1; i >= 0; i--)
			{
				IInputDevice device = this.InputDevices[i];
				device.useBufferInput = useBufferInput;
			}
		}
	}
	public void Polling() {
		lock (this.lockInputDevices) {
			//				foreach( IInputDevice device in this.list入力デバイス )
			for (int i = this.InputDevices.Count - 1; i >= 0; i--)    // #24016 2011.1.6 yyagi: change not to use "foreach" to avoid InvalidOperation exception by Remove().
			{
				try {
					IInputDevice device = this.InputDevices[i];
					device.Polling();
				} catch (Exception e)                                      // #24016 2011.1.6 yyagi: catch exception for unplugging USB joystick, and remove the device object from the polling items.
				{
					Trace.TraceWarning($"Error polling input device {i}.");
					Trace.TraceWarning(e.ToString());
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
					if (device is CInputMIDI tmidi)
						Trace.TraceInformation($"MIDI In: [{tmidi.ID}] has been stopped.");
					device.Dispose();
				}
				lock (this.lockInputDevices) {
					this.InputDevices.Clear();
				}

				Context.Dispose();
			}
			this.bDisposed済み = true;
		}
	}

	// stablize device index
	public void SetID(Dictionary<int, string>[] stableIdToGuid) {
		foreach (IInputDevice device in this.InputDevices) {
			if (device.CurrentType is InputDeviceType.Keyboard or InputDeviceType.Mouse)
				continue; // only support one device
			var idToGuid = stableIdToGuid[(int)device.CurrentType];
			if (!idToGuid.ContainsValue(device.GUID)) {
				int key = 0;
				while (idToGuid.ContainsKey(key)) {
					key++;
				}
				idToGuid.Add(key, device.GUID);
			}
		}
		foreach (IInputDevice device in this.InputDevices) {
			if (device.CurrentType is InputDeviceType.Keyboard or InputDeviceType.Mouse)
				continue; // only support one device
			var idToGuid = stableIdToGuid[(int)device.CurrentType];
			foreach (var (id, guid) in idToGuid) {
				if (device.GUID.Equals(guid)) {
					device.ID = id;
					break;
				}
			}
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
	private IInputDevice? _Keyboard;
	private IInputDevice? _Mouse;
	private bool bDisposed済み;
	private object lockInputDevices = new object();
	//private CTimer timer;

	//-----------------
	#endregion
}
