using Silk.NET.Input;

namespace FDK;

public class CInputKeyboard : IInputDevice, IDisposable {
	// Constructor

	public CInputKeyboard(IReadOnlyList<IKeyboard> keyboards) {
		this.CurrentType = InputDeviceType.Keyboard;
		this.GUID = "";
		this.ID = 0;
		this.Name = keyboards.Count > 0 ? keyboards[0].Name : "";

		foreach (var keyboard in keyboards) {
			keyboard.KeyDown += KeyDown;
			keyboard.KeyUp += KeyUp;
			keyboard.KeyChar += KeyChar;
		}

		//this.timer = new CTimer( CTimer.E種別.MultiMedia );
		this.InputEvents = new List<STInputEvent>(32);
		// this.ct = new CTimer( CTimer.E種別.PerformanceCounter );
	}


	// メソッド

	#region [ IInputDevice 実装 ]
	//-----------------
	public InputDeviceType CurrentType { get; private set; }
	public string GUID { get; private set; }
	public int ID { get; private set; }
	public string Name { get; private set; }
	public List<STInputEvent> InputEvents { get; private set; }
	public string strDeviceName { get; set; }

	public void Polling(bool useBufferInput) {
		InputEvents.Clear();

		for (int i = 0; i < KeyStates.Length; i++) {
			if (KeyStates[i].isPressed) {
				if (KeyStates[i].state >= 1) {
					KeyStates[i].state = 2;
				} else {
					KeyStates[i].state = 1;
					InputEvents.Add(
						new STInputEvent() {
							nKey = i,
							Pressed = true,
							Released = false,
							nTimeStamp = SampleFramework.Game.TimeMs,
							nVelocity = 0,
						}
					);
				}
			} else {
				if (KeyStates[i].state <= -1) {
					KeyStates[i].state = -2;
				} else {
					KeyStates[i].state = -1;
					InputEvents.Add(
						new STInputEvent() {
							nKey = i,
							Pressed = false,
							Released = true,
							nTimeStamp = SampleFramework.Game.TimeMs,
							nVelocity = 0,
						}
					);
				}
			}
		}
	}
	/// <param name="nKey">
	///		調べる SlimDX.DirectInput.Key を int にキャストした値。（SharpDX.DirectInput.Key ではないので注意。）
	/// </param>
	public bool KeyPressed(int nKey) {
		return KeyStates[nKey].state == 1;
	}
	/// <param name="nKey">
	///		調べる SlimDX.DirectInput.Key を int にキャストした値。（SharpDX.DirectInput.Key ではないので注意。）
	/// </param>	
	public bool KeyPressing(int nKey) {
		return KeyStates[nKey].state >= 1;
	}
	/// <param name="nKey">
	///		調べる SlimDX.DirectInput.Key を int にキャストした値。（SharpDX.DirectInput.Key ではないので注意。）
	/// </param>
	public bool KeyReleased(int nKey) {
		return KeyStates[nKey].state == -1;
	}
	/// <param name="nKey">
	///		調べる SlimDX.DirectInput.Key を int にキャストした値。（SharpDX.DirectInput.Key ではないので注意。）
	/// </param>
	public bool KeyReleasing(int nKey) {
		return KeyStates[nKey].state <= -1;
	}
	//-----------------
	#endregion

	#region [ IDisposable 実装 ]
	//-----------------
	public void Dispose() {
		if (!this.IsDisposed) {
			if (this.InputEvents != null) {
				this.InputEvents = null;
			}
			this.IsDisposed = true;
		}
	}
	//-----------------
	#endregion


	// その他

	#region [ private ]
	//-----------------
	public (bool isPressed, int state)[] KeyStates { get; private set; } = new (bool, int)[144];
	private bool IsDisposed;
	//private CTimer timer;
	//private CTimer ct;


	private void KeyDown(IKeyboard keyboard, Key key, int keyCode) {
		if (key != Key.Unknown) {
			var keyNum = DeviceConstantConverter.DIKtoKey(key);
			KeyStates[(int)keyNum].isPressed = true;
		}
	}

	private void KeyUp(IKeyboard keyboard, Key key, int keyCode) {
		if (key != Key.Unknown) {
			var keyNum = DeviceConstantConverter.DIKtoKey(key);
			KeyStates[(int)keyNum].isPressed = false;
		}
	}

	private void KeyChar(IKeyboard keyboard, char ch) {

	}
	//-----------------
	#endregion
}
