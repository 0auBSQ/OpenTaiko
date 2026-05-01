using Silk.NET.Input;

namespace FDK;

public abstract class CInputButtonsBase : IInputDevice, IDisposable {
	// Constructor

	public CInputButtonsBase(int nButtonStates) {
		this.ButtonStates = Enumerable.Range(0, nButtonStates).Select(_ => (0U, -2)).ToArray();
		this.EventBuffer = new List<STInputEvent>(nButtonStates);
		this.InputEvents = [];
	}


	// メソッド

	public virtual int GetVelocity(int index) => 0;
	protected virtual void SetVelocity(int index, int velocity) { }

	#region [ IInputDevice 実装 ]
	//-----------------
	public Silk.NET.Input.IInputDevice? Device { get; protected set; }
	public InputDeviceType CurrentType { get; protected set; }
	public string GUID { get; protected set; }
	public int ID { get; set; }
	public string Name { get; protected set; }
	public List<STInputEvent> InputEvents { get; protected set; }
	public string strDeviceName { get; set; }

	public void Polling() {
		// clear previous input buffer
		InputEvents.Clear();
		// update per-frame button state
		// the input buffer has already been filled.
		for (int i = 0; i < ButtonStates.Length; i++) {
			// Use the same timer used in gameplay to prevent desyncs between BGM/chart and input.
			this.ProcessButtonState(i, this.GetVelocity(i));
		}
		// swap input buffer
		this.InputEvents = Interlocked.Exchange(ref this.EventBuffer, this.InputEvents);
	}

	// 0 (temporary): press start this frame, 1: press start, 2: press continue
	// -1: release start, -2: release continue, -3: press start & end
	protected void ProcessButtonState(int idxBtn, int velocity = 0) {
		lock (this.ButtonStates) { // update thread, concurrent with input thread
			var isPressed = Volatile.Read(ref ButtonStates[idxBtn].isPressed);
			var state = Volatile.Read(ref ButtonStates[idxBtn].state);
			if (isPressed != 0) {
				if (state >= 1) {
					Volatile.Write(ref ButtonStates[idxBtn].state, 2);
				} else {
					Volatile.Write(ref ButtonStates[idxBtn].state, 1);
				}
			} else {
				if (state <= -1) {
					Volatile.Write(ref ButtonStates[idxBtn].state, -2);
				} else if (state == 0) {
					Volatile.Write(ref ButtonStates[idxBtn].state, -3);
				} else {
					Volatile.Write(ref ButtonStates[idxBtn].state, -1);
				}
			}
		}
	}

	protected void AddReleasedEvent(int idxBtn, long msTImestamp) {
		lock (this.EventBuffer) { // update thread and input thread
			this.EventBuffer.Add(new STInputEvent() {
				nKey = idxBtn,
				Pressed = false,
				Released = true,
				nTimeStamp = msTImestamp,
				nVelocity = 0,
			});
		}
	}

	protected void AddPressedEvent(int idxBtn, long msTimestamp, int velocity = 0) {
		lock (this.EventBuffer) { // update thread and input thread
			this.EventBuffer.Add(new STInputEvent() {
				nKey = idxBtn,
				Pressed = true,
				Released = false,
				nTimeStamp = msTimestamp,
				nVelocity = velocity,
			});
		}
	}

	protected void ButtonDown(int idxBtn, int velocity = 0) {
		uint isPressed;
		lock (this.ButtonStates) { // update thread and input thread
			isPressed = Interlocked.Exchange(ref this.ButtonStates[idxBtn].isPressed, 1U);
			if (isPressed == 0)
				Volatile.Write(ref ButtonStates[idxBtn].state, 0);
		}
		if (isPressed == 0)
			this.AddPressedEvent(idxBtn, SoundManager.PlayTimer.msGetPreciseNowSoundTimerTime(), velocity);
		this.SetVelocity(idxBtn, velocity);
	}

	protected void ButtonUp(int idxBtn) {
		var isPressed = Interlocked.Exchange(ref this.ButtonStates[idxBtn].isPressed, 0U);
		if (isPressed != 0)
			this.AddReleasedEvent(idxBtn, SoundManager.PlayTimer.msGetPreciseNowSoundTimerTime());
	}

	public bool KeyPressed(int nButton) {
		return Volatile.Read(ref ButtonStates[nButton].state) is 1 or -3;
	}
	public bool KeyPressing(int nButton) {
		return Volatile.Read(ref ButtonStates[nButton].state) >= 1;
	}
	public bool KeyReleased(int nButton) {
		return Volatile.Read(ref ButtonStates[nButton].state) is -1 or -3;
	}
	public bool KeyReleasing(int nButton) {
		return Volatile.Read(ref ButtonStates[nButton].state) <= -1;
	}
	//-----------------
	#endregion

	#region [ IDisposable 実装 ]
	//-----------------
	public virtual void Dispose() {
		if (!this.IsDisposed) {
			this.InputEvents.Clear();
			lock (this.EventBuffer) // update thread and input thread
				this.EventBuffer.Clear();
			this.IsDisposed = true;
		}
	}
	//-----------------
	#endregion


	// その他

	#region [ private ]
	//-----------------
	public List<STInputEvent> EventBuffer;
	public (uint isPressed, int state)[] ButtonStates { get; protected set; }
	protected bool IsDisposed;
	//-----------------
	#endregion
}
