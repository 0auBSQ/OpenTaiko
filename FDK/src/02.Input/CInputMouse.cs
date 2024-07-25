using System.Diagnostics;
using System.Numerics;
using Silk.NET.Input;

namespace FDK {
	public class CInputMouse : IInputDevice, IDisposable {
		// 定数

		public const int MouseButtonCount = 8;


		// コンストラクタ

		public CInputMouse(IMouse mouse) {
			this.CurrentType = InputDeviceType.Mouse;
			this.GUID = "";
			this.ID = 0;
			try {
				Trace.TraceInformation(mouse.Name + " を生成しました。");  // なぜか0x00のゴミが出るので削除
				this.strDeviceName = mouse.Name;
			} catch {
				Trace.TraceWarning("Mouse デバイスの生成に失敗しました。");
				throw;
			}

			mouse.Click += Mouse_Click;
			mouse.DoubleClick += Mouse_DoubleClick;
			mouse.MouseDown += Mouse_MouseDown;
			mouse.MouseUp += Mouse_MouseUp;
			mouse.MouseMove += Mouse_MouseMove;

			this.InputEvents = new List<STInputEvent>(32);
		}


		// メソッド

		#region [ IInputDevice 実装 ]
		//-----------------
		public InputDeviceType CurrentType { get; private set; }
		public string GUID { get; private set; }
		public int ID { get; private set; }
		public List<STInputEvent> InputEvents { get; private set; }
		public string strDeviceName { get; set; }

		public void Polling(bool useBufferInput) {
			InputEvents.Clear();

			for (int i = 0; i < MouseStates.Length; i++) {
				if (MouseStates[i].Item1) {
					if (MouseStates[i].Item2 >= 1) {
						MouseStates[i].Item2 = 2;
					} else {
						MouseStates[i].Item2 = 1;
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
					if (MouseStates[i].Item2 <= -1) {
						MouseStates[i].Item2 = -2;
					} else {
						MouseStates[i].Item2 = -1;
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
			return MouseStates[nButton].Item2 == 1;
		}
		public bool KeyPressing(int nButton) {
			return MouseStates[nButton].Item2 >= 1;
		}
		public bool KeyReleased(int nButton) {
			return MouseStates[nButton].Item2 == -1;
		}
		public bool KeyReleasing(int nButton) {
			return MouseStates[nButton].Item2 <= -1;
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
		private (bool, int)[] MouseStates = new (bool, int)[12];
		private bool IsDisposed;

		private void Mouse_Click(IMouse mouse, MouseButton mouseButton, Vector2 vector2) {

		}

		private void Mouse_DoubleClick(IMouse mouse, MouseButton mouseButton, Vector2 vector2) {

		}

		private void Mouse_MouseDown(IMouse mouse, MouseButton mouseButton) {
			if (mouseButton != MouseButton.Unknown) {
				MouseStates[(int)mouseButton].Item1 = true;
			}
		}

		private void Mouse_MouseUp(IMouse mouse, MouseButton mouseButton) {
			if (mouseButton != MouseButton.Unknown) {
				MouseStates[(int)mouseButton].Item1 = false;
			}
		}

		private void Mouse_MouseMove(IMouse mouse, Vector2 vector2) {

		}
		//-----------------
		#endregion
	}
}
