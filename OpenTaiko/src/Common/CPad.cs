using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using FDK;

namespace TJAPlayer3
{
    public class CPad
	{
		// プロパティ

		internal STHIT st検知したデバイス;
		[StructLayout( LayoutKind.Sequential )]
		internal struct STHIT
		{
			public bool Keyboard;
			public bool MIDIIN;
			public bool Joypad;
			public bool Gamepad;
			public bool Mouse;
			public void Clear()
			{
				this.Keyboard = false;
				this.MIDIIN = false;
				this.Joypad = false;
				this.Mouse = false;
			}
		}


		// コンストラクタ

		internal CPad( CConfigIni configIni, CInputManager mgrInput )
		{
			this.rConfigIni = configIni;
			this.rInput管理 = mgrInput;
			this.st検知したデバイス.Clear();
		}


		// メソッド

		public List<STInputEvent> GetEvents( E楽器パート part, Eパッド pad )
		{
			CConfigIni.CKeyAssign.STKEYASSIGN[] stkeyassignArray = this.rConfigIni.KeyAssign[ (int) part ][ (int) pad ];
			List<STInputEvent> list = new List<STInputEvent>();

			// すべての入力デバイスについて…
			foreach( IInputDevice device in this.rInput管理.InputDevices )
			{
				if( ( device.InputEvents != null ) && ( device.InputEvents.Count != 0 ) )
				{
					foreach( STInputEvent event2 in device.InputEvents )
					{
						for( int i = 0; i < stkeyassignArray.Length; i++ )
						{
							switch( stkeyassignArray[ i ].入力デバイス )
							{
								case EInputDevice.Keyboard:
									if( ( device.CurrentType == InputDeviceType.Keyboard ) && ( event2.nKey == stkeyassignArray[ i ].コード ) )
									{
										list.Add( event2 );
										this.st検知したデバイス.Keyboard = true;
									}
									break;

								case EInputDevice.MIDIInput:
									if( ( ( device.CurrentType == InputDeviceType.MidiIn ) && ( device.ID == stkeyassignArray[ i ].ID ) ) && ( event2.nKey == stkeyassignArray[ i ].コード ) )
									{
										list.Add( event2 );
										this.st検知したデバイス.MIDIIN = true;
									}
									break;

								case EInputDevice.Joypad:
									if( ( ( device.CurrentType == InputDeviceType.Joystick ) && ( device.ID == stkeyassignArray[ i ].ID ) ) && ( event2.nKey == stkeyassignArray[ i ].コード ) )
									{
										list.Add( event2 );
										this.st検知したデバイス.Joypad = true;
									}
									break;

								case EInputDevice.Gamepad:
									if( ( ( device.CurrentType == InputDeviceType.Gamepad ) && ( device.ID == stkeyassignArray[ i ].ID ) ) && ( event2.nKey == stkeyassignArray[ i ].コード ) )
									{
										list.Add( event2 );
										this.st検知したデバイス.Gamepad = true;
									}
									break;

								case EInputDevice.Mouse:
									if( ( device.CurrentType == InputDeviceType.Mouse ) && ( event2.nKey == stkeyassignArray[ i ].コード ) )
									{
										list.Add( event2 );
										this.st検知したデバイス.Mouse = true;
									}
									break;
							}
						}
					}
					continue;
				}
			}
			return list;
		}
		public bool b押された( E楽器パート part, Eパッド pad )
		{
			if( part != E楽器パート.UNKNOWN )
			{
				
				CConfigIni.CKeyAssign.STKEYASSIGN[] stkeyassignArray = this.rConfigIni.KeyAssign[ (int) part ][ (int) pad ];
				for( int i = 0; i < stkeyassignArray.Length; i++ )
				{
					switch( stkeyassignArray[ i ].入力デバイス )
					{
						case EInputDevice.Keyboard:
							if( !this.rInput管理.Keyboard.KeyPressed( stkeyassignArray[ i ].コード ) )
								break;

							this.st検知したデバイス.Keyboard = true;
							return true;

						case EInputDevice.MIDIInput:
							{
								IInputDevice device2 = this.rInput管理.MidiIn( stkeyassignArray[ i ].ID );
								if( ( device2 == null ) || !device2.KeyPressed( stkeyassignArray[ i ].コード ) )
									break;

								this.st検知したデバイス.MIDIIN = true;
								return true;
							}
						case EInputDevice.Joypad:
							{
								if( !this.rConfigIni.dicJoystick.ContainsKey( stkeyassignArray[ i ].ID ) )
									break;

								IInputDevice device = this.rInput管理.Joystick( stkeyassignArray[ i ].ID );
								if( ( device == null ) || !device.KeyPressed( stkeyassignArray[ i ].コード ) )
									break;

								this.st検知したデバイス.Joypad = true;
								return true;
							}
						case EInputDevice.Gamepad:
							{
								if( !this.rConfigIni.dicJoystick.ContainsKey( stkeyassignArray[ i ].ID ) )
									break;

								IInputDevice device = this.rInput管理.Gamepad( stkeyassignArray[ i ].ID );
								if( ( device == null ) || !device.KeyPressed( stkeyassignArray[ i ].コード ) )
									break;

								this.st検知したデバイス.Gamepad = true;
								return true;
							}
						case EInputDevice.Mouse:
							if( !this.rInput管理.Mouse.KeyPressed( stkeyassignArray[ i ].コード ) )
								break;

							this.st検知したデバイス.Mouse = true;
							return true;
					}
				}
			}
			return false;
		}
		public bool b押されたDGB( Eパッド pad )
		{
			if( !this.b押された( E楽器パート.DRUMS, pad ) && !this.b押された( E楽器パート.GUITAR, pad ) )
			{
				return this.b押された( E楽器パート.BASS, pad );
			}
			return true;
		}
		public bool b押されたGB( Eパッド pad )
		{
			if( !this.b押された( E楽器パート.GUITAR, pad ) )
			{
				return this.b押された( E楽器パート.BASS, pad );
			}
			return true;
		}
		public bool b押されている( E楽器パート part, Eパッド pad )
		{
			if( part != E楽器パート.UNKNOWN )
			{
				CConfigIni.CKeyAssign.STKEYASSIGN[] stkeyassignArray = this.rConfigIni.KeyAssign[ (int) part ][ (int) pad ];
				for( int i = 0; i < stkeyassignArray.Length; i++ )
				{
					switch( stkeyassignArray[ i ].入力デバイス )
					{
						case EInputDevice.Keyboard:
							if( !this.rInput管理.Keyboard.KeyPressing( stkeyassignArray[ i ].コード ) )
							{
								break;
							}
							this.st検知したデバイス.Keyboard = true;
							return true;

						case EInputDevice.Joypad:
							{
								if( !this.rConfigIni.dicJoystick.ContainsKey( stkeyassignArray[ i ].ID ) )
								{
									break;
								}
								IInputDevice device = this.rInput管理.Joystick( stkeyassignArray[ i ].ID );
								if( ( device == null ) || !device.KeyPressing( stkeyassignArray[ i ].コード ) )
								{
									break;
								}
								this.st検知したデバイス.Joypad = true;
								return true;
							}

						case EInputDevice.Gamepad:
							{
								if( !this.rConfigIni.dicJoystick.ContainsKey( stkeyassignArray[ i ].ID ) )
								{
									break;
								}
								IInputDevice device = this.rInput管理.Gamepad( stkeyassignArray[ i ].ID );
								if( ( device == null ) || !device.KeyPressing( stkeyassignArray[ i ].コード ) )
								{
									break;
								}
								this.st検知したデバイス.Gamepad = true;
								return true;
							}
						case EInputDevice.Mouse:
							if( !this.rInput管理.Mouse.KeyPressing( stkeyassignArray[ i ].コード ) )
							{
								break;
							}
							this.st検知したデバイス.Mouse = true;
							return true;
					}
				}
			}
			return false;
		}
		public bool b押されているGB( Eパッド pad )
		{
			if( !this.b押されている( E楽器パート.GUITAR, pad ) )
			{
				return this.b押されている( E楽器パート.BASS, pad );
			}
			return true;
		}


		// その他

		#region [ private ]
		//-----------------
		private CConfigIni rConfigIni;
		private CInputManager rInput管理;
		//-----------------
		#endregion
	}
}
