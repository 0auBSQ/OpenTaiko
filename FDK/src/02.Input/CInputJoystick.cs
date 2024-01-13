using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Silk.NET.Input;

namespace FDK
{
	public class CInputJoystick : IInputDevice, IDisposable
	{
		// コンストラクタ

		private IJoystick Joystick {get; set;}

		public CInputJoystick(IJoystick joystick)
		{
			Joystick = joystick;
			this.CurrentType = InputDeviceType.Joystick;
			this.GUID = joystick.Index.ToString();
			this.ID = joystick.Index;

			this.InputEvents = new List<STInputEvent>(32);

			joystick.ButtonDown += Joystick_ButtonDown;
			joystick.ButtonUp += Joystick_ButtonUp;
			joystick.AxisMoved += Joystick_AxisMoved;
			joystick.HatMoved += Joystick_HatMoved;
		}
		
		
		// メソッド
		
		public void SetID( int nID )
		{
			this.ID = nID;
		}

		#region [ IInputDevice 実装 ]
		//-----------------
		public InputDeviceType CurrentType
		{ 
			get;
			private set;
		}
		public string GUID
		{
			get;
			private set;
		}
		public int ID
		{ 
			get; 
			private set;
		}
		public List<STInputEvent> InputEvents 
		{
			get;
			private set;
		}
		public string strDeviceName
		{
			get;
			set;
		}

		public void Polling(bool useBufferInput)
		{
			InputEvents.Clear();
			
			// BUG: In Silk.NET, GLFW input does not fire events, so we have to poll
			// 			them instead.
			// 			https://github.com/dotnet/Silk.NET/issues/1889
			foreach (var button in Joystick.Buttons) {
				// also, in GLFW the buttons don't have names, so the indices are the names
				ButtonStates[button.Index].Item1 = button.Pressed;
			}

			for (int i = 0; i < ButtonStates.Length; i++)
			{
				if (ButtonStates[i].Item1)
				{
					if (ButtonStates[i].Item2 >= 1)
					{
						ButtonStates[i].Item2 = 2;
					}
					else
					{
						ButtonStates[i].Item2 = 1;

						InputEvents.Add(
							new STInputEvent()
							{
								nKey = i,
								Pressed = true,
								Released = false,
								nTimeStamp = SampleFramework.Game.TimeMs,
								nVelocity = 0,
							}
						);
					}
				}
				else
				{
					if (ButtonStates[i].Item2 <= -1)
					{
						ButtonStates[i].Item2 = -2;
					}
					else
					{
						ButtonStates[i].Item2 = -1;

						InputEvents.Add(
							new STInputEvent()
							{
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

		public bool KeyPressed(int nButton)
		{
			return ButtonStates[nButton].Item2 == 1;
		}
		public bool KeyPressing(int nButton)
		{
			return ButtonStates[nButton].Item2 >= 1;
		}
		public bool KeyReleased(int nButton)
		{
			return ButtonStates[nButton].Item2 == -1;
		}
		public bool KeyReleasing(int nButton)
		{
			return ButtonStates[nButton].Item2 <= -1;
		}
		//-----------------
		#endregion

		#region [ IDisposable 実装 ]
		//-----------------
		public void Dispose()
		{
			if(!this.IsDisposed)
			{
				if (this.InputEvents != null)
				{
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
		private (bool, int)[] ButtonStates = new (bool, int)[18];
		private bool IsDisposed;

		private void Joystick_ButtonDown(IJoystick joystick, Button button)
		{
			if (button.Name != ButtonName.Unknown)
			{
				ButtonStates[(int)button.Name].Item1 = true;
			}
		}

		private void Joystick_ButtonUp(IJoystick joystick, Button button)
		{
			if (button.Name != ButtonName.Unknown)
			{
				ButtonStates[(int)button.Name].Item1 = false;
			}
		}

		private void Joystick_AxisMoved(IJoystick joystick, Axis axis)
		{

		}

		private void Joystick_HatMoved(IJoystick joystick, Hat hat)
		{

		}
		//-----------------
		#endregion
	}
}
