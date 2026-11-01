using Silk.NET.Input;

namespace FDK;

public abstract class CInputButtonsBase : IInputDevice, IDisposable {
	// Constructor

	public CInputButtonsBase(int nButtonStates) {
		this.ButtonStates = Enumerable.Range(0, nButtonStates).Select(_ => (0U, -2)).ToArray();
		this.pressedSincePoll = new bool[nButtonStates];
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
	public string GUID { get; protected set; } = null!;
	public int ID { get; set; }
	public string Name { get; protected set; } = null!;
	public List<STInputEvent> InputEvents { get; protected set; }
	public string? strDeviceName { get; set; }

	public void Polling(bool accumulate = false) {
		// clear previous input buffer
		if (!accumulate)
		InputEvents.Clear();
		// update per-frame button state
		// the input buffer has already been filled.
		for (int i = 0; i < ButtonStates.Length; i++) {
			// Use the same timer used in gameplay to prevent desyncs between BGM/chart and input.
			this.ProcessButtonState(i, this.GetVelocity(i), accumulate);
		}
		// swap input buffer
		if (!accumulate) {
			lock (this.eventLock) { // update thread and input thread
				(this.InputEvents, this.EventBuffer) = (this.EventBuffer, this.InputEvents);
			}
		}
	}

	// 1: press start, 2: press continue
	// -1: release start, -2: release continue, -3: press start & end
	// Only polling changes the state; the input thread only records that a press happened since the last poll.
	protected void ProcessButtonState(int idxBtn, int velocity = 0, bool accumulate = false) {
		lock (this.ButtonStates) { // update thread, concurrent with input thread
			bool pressedSincePoll = this.pressedSincePoll[idxBtn];
			this.pressedSincePoll[idxBtn] = false;
			var isPressed = Volatile.Read(ref ButtonStates[idxBtn].isPressed);
			var state = Volatile.Read(ref ButtonStates[idxBtn].state);
			if (isPressed != 0) {
				if (!pressedSincePoll && (state == 2 || (state == 1 && !accumulate))) {
					Volatile.Write(ref ButtonStates[idxBtn].state, 2);
				} else {
					Volatile.Write(ref ButtonStates[idxBtn].state, 1);
				}
			} else {
				if (pressedSincePoll || (accumulate && (state == 1 || state == -3))) {
					Volatile.Write(ref ButtonStates[idxBtn].state, -3);
				} else if (state == -2 || ((state == -1 || state == -3) && !accumulate)) {
					Volatile.Write(ref ButtonStates[idxBtn].state, -2);
				} else {
					Volatile.Write(ref ButtonStates[idxBtn].state, -1);
				}
			}
		}
	}

	protected void AddReleasedEvent(int idxBtn, long msTImestamp) {
		lock (this.eventLock) { // update thread and input thread
			this.EventBuffer.Add(new STInputEvent() {
				nKey = idxBtn,
				Pressed = false,
				Released = true,
				nTimeStamp = msTImestamp,
				nVelocity = 0,
			});
		}
	}

	protected void AddPressedEvent(int idxBtn, long msTimestamp, int velocity = 0, bool handledOnArrival = false) {
		lock (this.eventLock) { // update thread and input thread
			this.EventBuffer.Add(new STInputEvent() {
				nKey = idxBtn,
				Pressed = true,
				Released = false,
				nTimeStamp = msTimestamp,
				nVelocity = velocity,
				HandledOnArrival = handledOnArrival,
			});
		}
	}

	protected void ButtonDown(int idxBtn, int velocity = 0) {
		uint isPressed;
		lock (this.ButtonStates) { // update thread and input thread
			isPressed = Interlocked.Exchange(ref this.ButtonStates[idxBtn].isPressed, 1U);
			if (isPressed == 0)
				this.pressedSincePoll[idxBtn] = true;
		}
		if (isPressed == 0) {
			long msTimestamp = NowTimestamp();
			this.AddPressedEvent(idxBtn, msTimestamp, velocity, this.HandleOnArrival(idxBtn));
		}
		this.SetVelocity(idxBtn, velocity);
	}

	/// <summary>Called on the thread that delivers a new press, before its event is queued. Returns true when the press
	/// was handled there; its event then carries <see cref="STInputEvent.HandledOnArrival"/>.</summary>
	public static Func<IInputDevice, int, bool>? PressArrived;

	private bool HandleOnArrival(int idxBtn) {
		try {
			return PressArrived?.Invoke(this, idxBtn) ?? false;
		} catch (Exception e) {
			System.Diagnostics.Trace.TraceWarning($"Input press handler failed: {e}");
			return false;
		}
	}

	protected void ButtonUp(int idxBtn) {
		var isPressed = Interlocked.Exchange(ref this.ButtonStates[idxBtn].isPressed, 0U);
		if (isPressed != 0)
			this.AddReleasedEvent(idxBtn, NowTimestamp());
	}

	// the play timer's time; 0 before the sound device exists (input arrives from the start)
	private static long NowTimestamp() => SoundManager.PlayTimer?.msGetPreciseNowSoundTimerTime() ?? 0;

	public bool KeyAvailable(int nButton) {
		return Volatile.Read(ref ButtonStates[nButton].state) is not 0;
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
			lock (this.eventLock) // update thread and input thread
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
	private readonly object eventLock = new();
	public (uint isPressed, int state)[] ButtonStates { get; protected set; }
	private readonly bool[] pressedSincePoll;
	protected bool IsDisposed;
	//-----------------
	#endregion
}
