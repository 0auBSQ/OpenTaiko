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

		internal CPad( CConfigIni configIni, CInput管理 mgrInput )
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
			foreach( IInputDevice device in this.rInput管理.list入力デバイス )
			{
				if( ( device.list入力イベント != null ) && ( device.list入力イベント.Count != 0 ) )
				{
					foreach( STInputEvent event2 in device.list入力イベント )
					{
						for( int i = 0; i < stkeyassignArray.Length; i++ )
						{
							switch( stkeyassignArray[ i ].入力デバイス )
							{
								case E入力デバイス.キーボード:
									if( ( device.e入力デバイス種別 == E入力デバイス種別.Keyboard ) && ( event2.nKey == stkeyassignArray[ i ].コード ) )
									{
										list.Add( event2 );
										this.st検知したデバイス.Keyboard = true;
									}
									break;

								case E入力デバイス.MIDI入力:
									if( ( ( device.e入力デバイス種別 == E入力デバイス種別.MidiIn ) && ( device.ID == stkeyassignArray[ i ].ID ) ) && ( event2.nKey == stkeyassignArray[ i ].コード ) )
									{
										list.Add( event2 );
										this.st検知したデバイス.MIDIIN = true;
									}
									break;

								case E入力デバイス.ジョイパッド:
									if( ( ( device.e入力デバイス種別 == E入力デバイス種別.Joystick ) && ( device.ID == stkeyassignArray[ i ].ID ) ) && ( event2.nKey == stkeyassignArray[ i ].コード ) )
									{
										list.Add( event2 );
										this.st検知したデバイス.Joypad = true;
									}
									break;

								case E入力デバイス.マウス:
									if( ( device.e入力デバイス種別 == E入力デバイス種別.Mouse ) && ( event2.nKey == stkeyassignArray[ i ].コード ) )
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
						case E入力デバイス.キーボード:
							if( !this.rInput管理.Keyboard.bキーが押された( stkeyassignArray[ i ].コード ) )
								break;

							this.st検知したデバイス.Keyboard = true;
							return true;

						case E入力デバイス.MIDI入力:
							{
								IInputDevice device2 = this.rInput管理.MidiIn( stkeyassignArray[ i ].ID );
								if( ( device2 == null ) || !device2.bキーが押された( stkeyassignArray[ i ].コード ) )
									break;

								this.st検知したデバイス.MIDIIN = true;
								return true;
							}
						case E入力デバイス.ジョイパッド:
							{
								if( !this.rConfigIni.dicJoystick.ContainsKey( stkeyassignArray[ i ].ID ) )
									break;

								IInputDevice device = this.rInput管理.Joystick( stkeyassignArray[ i ].ID );
								if( ( device == null ) || !device.bキーが押された( stkeyassignArray[ i ].コード ) )
									break;

								this.st検知したデバイス.Joypad = true;
								return true;
							}
						case E入力デバイス.マウス:
							if( !this.rInput管理.Mouse.bキーが押された( stkeyassignArray[ i ].コード ) )
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
						case E入力デバイス.キーボード:
							if( !this.rInput管理.Keyboard.bキーが押されている( stkeyassignArray[ i ].コード ) )
							{
								break;
							}
							this.st検知したデバイス.Keyboard = true;
							return true;

						case E入力デバイス.ジョイパッド:
							{
								if( !this.rConfigIni.dicJoystick.ContainsKey( stkeyassignArray[ i ].ID ) )
								{
									break;
								}
								IInputDevice device = this.rInput管理.Joystick( stkeyassignArray[ i ].ID );
								if( ( device == null ) || !device.bキーが押されている( stkeyassignArray[ i ].コード ) )
								{
									break;
								}
								this.st検知したデバイス.Joypad = true;
								return true;
							}
						case E入力デバイス.マウス:
							if( !this.rInput管理.Mouse.bキーが押されている( stkeyassignArray[ i ].コード ) )
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
		private CInput管理 rInput管理;
		//-----------------
		#endregion
	}
}
