using Silk.NET.Input;

namespace FDK {
	public class CInputGamepad : IInputDevice, IDisposable {
		// コンストラクタ

		public CInputGamepad(IGamepad gamepad) {
			this.CurrentType = InputDeviceType.Gamepad;
			this.GUID = gamepad.Index.ToString();
			this.ID = gamepad.Index;

			this.InputEvents = new List<STInputEvent>(32);

			gamepad.ButtonDown += Joystick_ButtonDown;
			gamepad.ButtonUp += Joystick_ButtonUp;
		}


		// メソッド

		public void SetID(int nID) {
			this.ID = nID;
		}

		#region [ IInputDevice 実装 ]
		//-----------------
		public InputDeviceType CurrentType {
			get;
			private set;
		}
		public string GUID {
			get;
			private set;
		}
		public int ID {
			get;
			private set;
		}
		public List<STInputEvent> InputEvents {
			get;
			private set;
		}
		public string strDeviceName {
			get;
			set;
		}

		public void Polling(bool useBufferInput) {
			InputEvents.Clear();

			for (int i = 0; i < ButtonStates.Length; i++) {
				if (ButtonStates[i].Item1) {
					if (ButtonStates[i].Item2 >= 1) {
						ButtonStates[i].Item2 = 2;
					} else {
						ButtonStates[i].Item2 = 1;

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
					if (ButtonStates[i].Item2 <= -1) {
						ButtonStates[i].Item2 = -2;
					} else {
						ButtonStates[i].Item2 = -1;

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

		public bool KeyPressed(int nButton) {
			return ButtonStates[nButton].Item2 == 1;
		}
		public bool KeyPressing(int nButton) {
			return ButtonStates[nButton].Item2 >= 1;
		}
		public bool KeyReleased(int nButton) {
			return ButtonStates[nButton].Item2 == -1;
		}
		public bool KeyReleasing(int nButton) {
			return ButtonStates[nButton].Item2 <= -1;
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
		private (bool, int)[] ButtonStates = new (bool, int)[15];
		private bool IsDisposed;

		private void Joystick_ButtonDown(IGamepad joystick, Button button) {
			if (button.Name != ButtonName.Unknown) {
				ButtonStates[(int)button.Name].Item1 = true;
			}
		}

		private void Joystick_ButtonUp(IGamepad joystick, Button button) {
			if (button.Name != ButtonName.Unknown) {
				ButtonStates[(int)button.Name].Item1 = false;
			}
		}
		//-----------------
		#endregion
	}
}
