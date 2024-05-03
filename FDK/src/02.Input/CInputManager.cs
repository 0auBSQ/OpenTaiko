using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Silk.NET.Windowing;
using Silk.NET.Input;

namespace FDK
{
	public class CInputManager : IDisposable
	{
		// 定数

		public static int DefaultVolume = 110;


		// プロパティ

		public List<IInputDevice> InputDevices
		{
			get;
			private set;
		}
		public IInputDevice Keyboard
		{
			get
			{
				if (this._Keyboard != null)
				{
					return this._Keyboard;
				}
				foreach (IInputDevice device in this.InputDevices)
				{
					if (device.CurrentType == InputDeviceType.Keyboard)
					{
						this._Keyboard = device;
						return device;
					}
				}
				return null;
			}
		}
		public IInputDevice Mouse
		{
			get
			{
				if (this._Mouse != null)
				{
					return this._Mouse;
				}
				foreach (IInputDevice device in this.InputDevices)
				{
					if (device.CurrentType == InputDeviceType.Mouse)
					{
						this._Mouse = device;
						return device;
					}
				}
				return null;
			}
		}


		// コンストラクタ
		public CInputManager(IWindow window, bool bUseMidiIn = true)
		{
			Initialize(window, bUseMidiIn);
		}

		public void Initialize(IWindow window, bool bUseMidiIn)
		{
			Context = window.CreateInput();

			this.InputDevices = new List<IInputDevice>(10);
			#region [ Enumerate keyboard/mouse: exception is masked if keyboard/mouse is not connected ]
			CInputKeyboard cinputkeyboard = null;
			CInputMouse cinputmouse = null;
			try
			{
				cinputkeyboard = new CInputKeyboard(Context.Keyboards);
				cinputmouse = new CInputMouse(Context.Mice[0]);
			}

			catch
			{
			}
			if (cinputkeyboard != null)
			{
				this.InputDevices.Add(cinputkeyboard);
			}
			if (cinputmouse != null)
			{
				this.InputDevices.Add(cinputmouse);
			}
			#endregion
			#region [ Enumerate joypad ]
			foreach (var joysticks in Context.Joysticks)
			{
				this.InputDevices.Add(new CInputJoystick(joysticks));
			}
			foreach (var gamepad in Context.Gamepads)
			{
				this.InputDevices.Add(new CInputGamepad(gamepad));
			}
			#endregion
			Trace.TraceInformation("Found {0} Input Device{1}", InputDevices.Count, InputDevices.Count != 1 ? "s:" : ":");
			for (int i = 0; i < InputDevices.Count; i++)
			{
				try
				{
					Trace.TraceInformation("Input Device #" + i + " (" + InputDevices[i].CurrentType.ToString() + ")");
				}
				catch { }
			}
		}


		// メソッド

		public IInputDevice Joystick(int ID)
		{
			foreach (IInputDevice device in this.InputDevices)
			{
				if ((device.CurrentType == InputDeviceType.Joystick) && (device.ID == ID))
				{
					return device;
				}
			}
			return null;
		}
		public IInputDevice Joystick(string GUID)
		{
			foreach (IInputDevice device in this.InputDevices)
			{
				if ((device.CurrentType == InputDeviceType.Joystick) && device.GUID.Equals(GUID))
				{
					return device;
				}
			}
			return null;
		}
		public IInputDevice Gamepad(int ID)
		{
			foreach (IInputDevice device in this.InputDevices)
			{
				if ((device.CurrentType == InputDeviceType.Gamepad) && (device.ID == ID))
				{
					return device;
				}
			}
			return null;
		}
		public IInputDevice Gamepad(string GUID)
		{
			foreach (IInputDevice device in this.InputDevices)
			{
				if ((device.CurrentType == InputDeviceType.Gamepad) && device.GUID.Equals(GUID))
				{
					return device;
				}
			}
			return null;
		}
		public IInputDevice MidiIn(int ID)
		{
			foreach (IInputDevice device in this.InputDevices)
			{
				if ((device.CurrentType == InputDeviceType.MidiIn) && (device.ID == ID))
				{
					return device;
				}
			}
			return null;
		}
		public void Polling(bool useBufferInput)
		{
			lock (this.objMidiIn排他用)
			{
				//				foreach( IInputDevice device in this.list入力デバイス )
				for (int i = this.InputDevices.Count - 1; i >= 0; i--)    // #24016 2011.1.6 yyagi: change not to use "foreach" to avoid InvalidOperation exception by Remove().
				{
					IInputDevice device = this.InputDevices[i];
					try
					{
						device.Polling(useBufferInput);
					}
					catch (Exception e)                                      // #24016 2011.1.6 yyagi: catch exception for unplugging USB joystick, and remove the device object from the polling items.
					{
						this.InputDevices.Remove(device);
						device.Dispose();
						Trace.TraceError("tポーリング時に対象deviceが抜かれており例外発生。同deviceをポーリング対象からRemoveしました。");
					}
				}
			}
		}

		#region [ IDisposable＋α ]
		//-----------------
		public void Dispose()
		{
			this.Dispose(true);
		}
		public void Dispose(bool disposeManagedObjects)
		{
			if (!this.bDisposed済み)
			{
				if (disposeManagedObjects)
				{
					foreach (IInputDevice device in this.InputDevices)
					{
						CInputMIDI tmidi = device as CInputMIDI;
						if (tmidi != null)
						{
							Trace.TraceInformation("MIDI In: [{0}] を停止しました。", new object[] { tmidi.ID });
						}
					}
					foreach (IInputDevice device2 in this.InputDevices)
					{
						device2.Dispose();
					}
					lock (this.objMidiIn排他用)
					{
						this.InputDevices.Clear();
					}

					Context.Dispose();
				}
				this.bDisposed済み = true;
			}
		}
		~CInputManager()
		{
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
}
