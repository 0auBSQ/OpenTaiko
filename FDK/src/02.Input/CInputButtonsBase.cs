namespace FDK;

public abstract class CInputButtonsBase : IInputDevice, IDisposable {
	// Constructor

	public CInputButtonsBase() {
		this.ButtonStates = [];
		this.InputEvents = new List<STInputEvent>(32);
	}


	// メソッド

	public void SetID(int nID) => this.ID = nID;

	#region [ IInputDevice 実装 ]
	//-----------------
	public InputDeviceType CurrentType { get; protected set; }
	public string GUID { get; protected set; }
	public int ID { get; protected set; }
	public string Name { get; protected set; }
	public List<STInputEvent> InputEvents { get; protected set; }
	public string strDeviceName { get; set; }

	public virtual void Polling(bool useBufferInput) {
		InputEvents.Clear();

		for (int i = 0; i < ButtonStates.Length; i++) {
			if (ButtonStates[i].isPressed) {
				if (ButtonStates[i].state >= 1) {
					ButtonStates[i].state = 2;
				} else {
					ButtonStates[i].state = 1;

					InputEvents.Add(
						new STInputEvent() {
							nKey = i,
							Pressed = true,
							Released = false,
							nTimeStamp = SoundManager.PlayTimer.SystemTimeMs, // Use the same timer used in gameplay to prevent desyncs between BGM/chart and input.
							nVelocity = 0,
						}
					);
				}
			} else {
				if (ButtonStates[i].state <= -1) {
					ButtonStates[i].state = -2;
				} else {
					ButtonStates[i].state = -1;

					InputEvents.Add(
						new STInputEvent() {
							nKey = i,
							Pressed = false,
							Released = true,
							nTimeStamp = SoundManager.PlayTimer.SystemTimeMs, // Use the same timer used in gameplay to prevent desyncs between BGM/chart and input.
							nVelocity = 0,
						}
					);
				}
			}
		}
	}

	public bool KeyPressed(int nButton) {
		return ButtonStates[nButton].state == 1;
	}
	public bool KeyPressing(int nButton) {
		return ButtonStates[nButton].state >= 1;
	}
	public bool KeyReleased(int nButton) {
		return ButtonStates[nButton].state == -1;
	}
	public bool KeyReleasing(int nButton) {
		return ButtonStates[nButton].state <= -1;
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
	public (bool isPressed, int state)[] ButtonStates { get; protected set; }
	private bool IsDisposed;
	//-----------------
	#endregion
}
