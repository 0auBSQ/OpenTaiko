using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Drawing;
using FDK;

namespace TJAPlayer3
{
	internal class CActConfigKeyAssign : CActivity
	{
		// プロパティ

		public bool bキー入力待ちの最中である
		{
			get
			{
				return this.bキー入力待ち;
			}
		}


		// メソッド

		public void t開始( EKeyConfigPart part, EKeyConfigPad pad, string strパッド名 )
		{
			if( part != EKeyConfigPart.UNKNOWN )
			{
				this.part = part;
				this.pad = pad;
				this.strパッド名 = strパッド名;
				for( int i = 0; i < 0x10; i++ )
				{
					this.structReset用KeyAssign[ i ].入力デバイス = TJAPlayer3.ConfigIni.KeyAssign[ (int) part ][ (int) pad ][ i ].入力デバイス;
					this.structReset用KeyAssign[ i ].ID = TJAPlayer3.ConfigIni.KeyAssign[ (int) part ][ (int) pad ][ i ].ID;
					this.structReset用KeyAssign[ i ].コード = TJAPlayer3.ConfigIni.KeyAssign[ (int) part ][ (int) pad ][ i ].コード;
				}
			}
		}
		
		public void tEnter押下()
		{
			if( !this.bキー入力待ち )
			{
				TJAPlayer3.Skin.soundDecideSFX.tPlay();
				switch( this.n現在の選択行 )
				{
					case 0x10:
						for( int i = 0; i < 0x10; i++ )
						{
							TJAPlayer3.ConfigIni.KeyAssign[ (int) this.part ][ (int) this.pad ][ i ].入力デバイス = this.structReset用KeyAssign[ i ].入力デバイス;
							TJAPlayer3.ConfigIni.KeyAssign[ (int) this.part ][ (int) this.pad ][ i ].ID = this.structReset用KeyAssign[ i ].ID;
							TJAPlayer3.ConfigIni.KeyAssign[ (int) this.part ][ (int) this.pad ][ i ].コード = this.structReset用KeyAssign[ i ].コード;
						}
						return;

					case 0x11:
						TJAPlayer3.stageコンフィグ.tアサイン完了通知();
						return;
				}
				this.bキー入力待ち = true;
			}
		}
		public void t次に移動()
		{
			if( !this.bキー入力待ち )
			{
				TJAPlayer3.Skin.soundカーソル移動音.tPlay();
				this.n現在の選択行 = ( this.n現在の選択行 + 1 ) % 0x12;
			}
		}
		public void t前に移動()
		{
			if( !this.bキー入力待ち )
			{
				TJAPlayer3.Skin.soundカーソル移動音.tPlay();
				this.n現在の選択行 = ( ( this.n現在の選択行 - 1 ) + 0x12 ) % 0x12;
			}
		}

		
		// CActivity 実装

		public override void Activate()
		{
			this.part = EKeyConfigPart.UNKNOWN;
			this.pad = EKeyConfigPad.UNKNOWN;
			this.strパッド名 = "";
			this.n現在の選択行 = 0;
			this.bキー入力待ち = false;
			this.structReset用KeyAssign = new CConfigIni.CKeyAssign.STKEYASSIGN[ 0x10 ];
			base.Activate();
		}
		public override void DeActivate()
		{
			if( !base.IsDeActivated )
			{
				//CDTXMania.tテクスチャの解放( ref this.txカーソル );
				//CDTXMania.tテクスチャの解放( ref this.txHitKeyダイアログ );
				base.DeActivate();
			}
		}
		public override void CreateManagedResource()
		{
			//this.txカーソル = CDTXMania.tテクスチャの生成( CSkin.Path( @"Graphics\ScreenConfig menu cursor.png" ), false );
			//this.txHitKeyダイアログ = CDTXMania.tテクスチャの生成( CSkin.Path( @"Graphics\ScreenConfig hit key to assign dialog.png" ), false );
			base.CreateManagedResource();
		}
		public override void ReleaseManagedResource()
		{
			base.ReleaseManagedResource();
		}
		public override int Draw()
		{
			if( !base.IsDeActivated )
			{
				if( this.bキー入力待ち )
				{
					if( TJAPlayer3.InputManager.Keyboard.KeyPressed( (int)SlimDXKeys.Key.Escape ) )
					{
						TJAPlayer3.Skin.soundCancelSFX.tPlay();
						this.bキー入力待ち = false;
						TJAPlayer3.InputManager.Polling( false );
					}
					else if( ( this.tキーチェックとアサイン_Keyboard() || this.tキーチェックとアサイン_MidiIn() ) || ( this.tキーチェックとアサイン_Joypad() || tキーチェックとアサイン_Gamepad() || this.tキーチェックとアサイン_Mouse() ) )
					{
						this.bキー入力待ち = false;
						TJAPlayer3.InputManager.Polling( false );
					}
				}
				else if( ( TJAPlayer3.InputManager.Keyboard.KeyPressed( (int)SlimDXKeys.Key.Delete ) && ( this.n現在の選択行 >= 0 ) ) && ( this.n現在の選択行 <= 15 ) )
				{
					TJAPlayer3.Skin.soundDecideSFX.tPlay();
					TJAPlayer3.ConfigIni.KeyAssign[ (int) this.part ][ (int) this.pad ][ this.n現在の選択行 ].入力デバイス = EInputDevice.Unknown;
					TJAPlayer3.ConfigIni.KeyAssign[ (int) this.part ][ (int) this.pad ][ this.n現在の選択行 ].ID = 0;
					TJAPlayer3.ConfigIni.KeyAssign[ (int) this.part ][ (int) this.pad ][ this.n現在の選択行 ].コード = 0;
				}
				if(TJAPlayer3.Tx.Menu_Highlight != null )
				{
					int num = TJAPlayer3.Skin.Config_KeyAssign_Move;
					int num2 = TJAPlayer3.Skin.Config_KeyAssign_Menu_Highlight[0];
					int num3 = TJAPlayer3.Skin.Config_KeyAssign_Menu_Highlight[1] + ( num * ( this.n現在の選択行 + 1 ) );
					//TJAPlayer3.Tx.Menu_Highlight.t2D描画( num2, num3, new Rectangle( 0, 0, 0x10, 0x20 ) );
					float scale = 0.55f;
					for( int j = 0; j < 14; j++ )
					{
						TJAPlayer3.Tx.Menu_Highlight.vcScaleRatio.X = scale;
						TJAPlayer3.Tx.Menu_Highlight.vcScaleRatio.Y = scale;

						TJAPlayer3.Tx.Menu_Highlight.t2D描画( num2, num3 );
						num2 += (int)(TJAPlayer3.Tx.Menu_Highlight.szTextureSize.Width * scale);

						TJAPlayer3.Tx.Menu_Highlight.vcScaleRatio.X = 1;
						TJAPlayer3.Tx.Menu_Highlight.vcScaleRatio.Y = 1;
					}
					//TJAPlayer3.Tx.Menu_Highlight.t2D描画( num2, num3, new Rectangle( 0x10, 0, 0x10, 0x20 ) );
				}
				int num5 = TJAPlayer3.Skin.Config_KeyAssign_Move;
				int x = TJAPlayer3.Skin.Config_KeyAssign_Font[0];
				int y = TJAPlayer3.Skin.Config_KeyAssign_Font[1];
				TJAPlayer3.stageコンフィグ.actFont.t文字列描画( x, y, this.strパッド名, false, 0.75f );
				y += num5;
				CConfigIni.CKeyAssign.STKEYASSIGN[] stkeyassignArray = TJAPlayer3.ConfigIni.KeyAssign[ (int) this.part ][ (int) this.pad ];
				for( int i = 0; i < 0x10; i++ )
				{
					switch( stkeyassignArray[ i ].入力デバイス )
					{
						case EInputDevice.Keyboard:
							this.tアサインコードの描画_Keyboard( i + 1, x + num5, y, stkeyassignArray[ i ].ID, stkeyassignArray[ i ].コード, this.n現在の選択行 == i );
							break;

						case EInputDevice.MIDIInput:
							this.tアサインコードの描画_MidiIn( i + 1, x + num5, y, stkeyassignArray[ i ].ID, stkeyassignArray[ i ].コード, this.n現在の選択行 == i );
							break;

						case EInputDevice.Joypad:
							this.tアサインコードの描画_Joypad( i + 1, x + num5, y, stkeyassignArray[ i ].ID, stkeyassignArray[ i ].コード, this.n現在の選択行 == i );
							break;

						case EInputDevice.Gamepad:
							this.tアサインコードの描画_Gamepad( i + 1, x + num5, y, stkeyassignArray[ i ].ID, stkeyassignArray[ i ].コード, this.n現在の選択行 == i );
							break;

						case EInputDevice.Mouse:
							this.tアサインコードの描画_Mouse( i + 1, x + num5, y, stkeyassignArray[ i ].ID, stkeyassignArray[ i ].コード, this.n現在の選択行 == i );
							break;

						default:
							TJAPlayer3.stageコンフィグ.actFont.t文字列描画( x + num5, y, string.Format( "{0,2}.", i + 1 ), this.n現在の選択行 == i, 0.75f );
							break;
					}
					y += num5;
				}
				TJAPlayer3.stageコンフィグ.actFont.t文字列描画( x + num5, y, "Reset", this.n現在の選択行 == 0x10, 0.75f );
				y += num5;
				TJAPlayer3.stageコンフィグ.actFont.t文字列描画( x + num5, y, "<< Returnto List", this.n現在の選択行 == 0x11, 0.75f );
				y += num5;
				if( this.bキー入力待ち && ( TJAPlayer3.Tx.Config_KeyAssign != null ) )
				{
                    TJAPlayer3.Tx.Config_KeyAssign.t2D描画( TJAPlayer3.Skin.Config_KeyAssign[0], TJAPlayer3.Skin.Config_KeyAssign[1]);
				}
			}
			return 0;
		}


		// その他

		#region [ private ]
		//-----------------
		[StructLayout( LayoutKind.Sequential )]
		private struct STKEYLABEL
		{
			public int nCode;
			public string strLabel;
			public STKEYLABEL( int nCode, string strLabel )
			{
				this.nCode = nCode;
				this.strLabel = strLabel;
			}
		}

		private bool bキー入力待ち;
		private STKEYLABEL[] KeyLabel = new STKEYLABEL[] { 
			new STKEYLABEL(0x35, "[ESC]"), new STKEYLABEL(1, "[ 1 ]"), new STKEYLABEL(2, "[ 2 ]"), new STKEYLABEL(3, "[ 3 ]"), new STKEYLABEL(4, "[ 4 ]"), new STKEYLABEL(5, "[ 5 ]"), new STKEYLABEL(6, "[ 6 ]"), new STKEYLABEL(7, "[ 7 ]"), new STKEYLABEL(8, "[ 8 ]"), new STKEYLABEL(9, "[ 9 ]"), new STKEYLABEL(0, "[ 0 ]"), new STKEYLABEL(0x53, "[ - ]"), new STKEYLABEL(0x34, "[ = ]"), new STKEYLABEL(0x2a, "[BSC]"), new STKEYLABEL(0x81, "[TAB]"), new STKEYLABEL(0x1a, "[ Q ]"), 
			new STKEYLABEL(0x20, "[ W ]"), new STKEYLABEL(14, "[ E ]"), new STKEYLABEL(0x1b, "[ R ]"), new STKEYLABEL(0x1d, "[ T ]"), new STKEYLABEL(0x22, "[ Y ]"), new STKEYLABEL(30, "[ U ]"), new STKEYLABEL(0x12, "[ I ]"), new STKEYLABEL(0x18, "[ O ]"), new STKEYLABEL(0x19, "[ P ]"), new STKEYLABEL(0x4a, "[ [ ]"), new STKEYLABEL(0x73, "[ ] ]"), new STKEYLABEL(0x75, "[Enter]"), new STKEYLABEL(0x4b, "[L-Ctrl]"), new STKEYLABEL(10, "[ A ]"), new STKEYLABEL(0x1c, "[ S ]"), new STKEYLABEL(13, "[ D ]"), 
			new STKEYLABEL(15, "[ F ]"), new STKEYLABEL(0x10, "[ G ]"), new STKEYLABEL(0x11, "[ H ]"), new STKEYLABEL(0x13, "[ J ]"), new STKEYLABEL(20, "[ K ]"), new STKEYLABEL(0x15, "[ L ]"), new STKEYLABEL(0x7b, "[ ; ]"), new STKEYLABEL(0x26, "[ ' ]"), new STKEYLABEL(0x45, "[ ` ]"), new STKEYLABEL(0x4e, "[L-Shift]"), new STKEYLABEL(0x2b, @"[ \]"), new STKEYLABEL(0x23, "[ Z ]"), new STKEYLABEL(0x21, "[ X ]"), new STKEYLABEL(12, "[ C ]"), new STKEYLABEL(0x1f, "[ V ]"), new STKEYLABEL(11, "[ B ]"), 
			new STKEYLABEL(0x17, "[ N ]"), new STKEYLABEL(0x16, "[ M ]"), new STKEYLABEL(0x2f, "[ , ]"), new STKEYLABEL(0x6f, "[ . ]"), new STKEYLABEL(0x7c, "[ / ]"), new STKEYLABEL(120, "[R-Shift]"), new STKEYLABEL(0x6a, "[ * ]"), new STKEYLABEL(0x4d, "[L-Alt]"), new STKEYLABEL(0x7e, "[Space]"), new STKEYLABEL(0x2d, "[CAPS]"), new STKEYLABEL(0x36, "[F1]"), new STKEYLABEL(0x37, "[F2]"), new STKEYLABEL(0x38, "[F3]"), new STKEYLABEL(0x39, "[F4]"), new STKEYLABEL(0x3a, "[F5]"), new STKEYLABEL(0x3b, "[F6]"), 
			new STKEYLABEL(60, "[F7]"), new STKEYLABEL(0x3d, "[F8]"), new STKEYLABEL(0x3e, "[F9]"), new STKEYLABEL(0x3f, "[F10]"), new STKEYLABEL(0x58, "[NumLock]"), new STKEYLABEL(0x7a, "[Scroll]"), new STKEYLABEL(0x60, "[NPad7]"), new STKEYLABEL(0x61, "[NPad8]"), new STKEYLABEL(0x62, "[NPad9]"), new STKEYLABEL(0x66, "[NPad-]"), new STKEYLABEL(0x5d, "[NPad4]"), new STKEYLABEL(0x5e, "[NPad5]"), new STKEYLABEL(0x5f, "[NPad6]"), new STKEYLABEL(0x68, "[NPad+]"), new STKEYLABEL(90, "[NPad1]"), new STKEYLABEL(0x5b, "[NPad2]"), 
			new STKEYLABEL(0x5c, "[NPad3]"), new STKEYLABEL(0x59, "[NPad0]"), new STKEYLABEL(0x67, "[NPad.]"), new STKEYLABEL(0x40, "[F11]"), new STKEYLABEL(0x41, "[F12]"), new STKEYLABEL(0x42, "[F13]"), new STKEYLABEL(0x43, "[F14]"), new STKEYLABEL(0x44, "[F15]"), new STKEYLABEL(0x48, "[Kana]"), new STKEYLABEL(0x24, "[ ? ]"), new STKEYLABEL(0x30, "[Henkan]"), new STKEYLABEL(0x57, "[MuHenkan]"), new STKEYLABEL(0x8f, @"[ \ ]"), new STKEYLABEL(0x25, "[NPad.]"), new STKEYLABEL(0x65, "[NPad=]"), new STKEYLABEL(0x72, "[ ^ ]"), 
			new STKEYLABEL(40, "[ @ ]"), new STKEYLABEL(0x2e, "[ : ]"), new STKEYLABEL(130, "[ _ ]"), new STKEYLABEL(0x49, "[Kanji]"), new STKEYLABEL(0x7f, "[Stop]"), new STKEYLABEL(0x29, "[AX]"), new STKEYLABEL(100, "[NPEnter]"), new STKEYLABEL(0x74, "[R-Ctrl]"), new STKEYLABEL(0x54, "[Mute]"), new STKEYLABEL(0x2c, "[Calc]"), new STKEYLABEL(0x70, "[PlayPause]"), new STKEYLABEL(0x52, "[MediaStop]"), new STKEYLABEL(0x85, "[Volume-]"), new STKEYLABEL(0x86, "[Volume+]"), new STKEYLABEL(0x8b, "[WebHome]"), new STKEYLABEL(0x63, "[NPad,]"), 
			new STKEYLABEL(0x69, "[ / ]"), new STKEYLABEL(0x80, "[PrtScn]"), new STKEYLABEL(0x77, "[R-Alt]"), new STKEYLABEL(110, "[Pause]"), new STKEYLABEL(70, "[Home]"), new STKEYLABEL(0x84, "[Up]"), new STKEYLABEL(0x6d, "[PageUp]"), new STKEYLABEL(0x4c, "[Left]"), new STKEYLABEL(0x76, "[Right]"), new STKEYLABEL(0x33, "[End]"), new STKEYLABEL(50, "[Down]"), new STKEYLABEL(0x6c, "[PageDown]"), new STKEYLABEL(0x47, "[Insert]"), new STKEYLABEL(0x31, "[Delete]"), new STKEYLABEL(0x4f, "[L-Win]"), new STKEYLABEL(0x79, "[R-Win]"), 
			new STKEYLABEL(0x27, "[APP]"), new STKEYLABEL(0x71, "[Power]"), new STKEYLABEL(0x7d, "[Sleep]"), new STKEYLABEL(0x87, "[Wake]")
		};
		private int n現在の選択行;
		private EKeyConfigPad pad;
		private EKeyConfigPart part;
		private CConfigIni.CKeyAssign.STKEYASSIGN[] structReset用KeyAssign;
		private string strパッド名;
		//private CTexture txHitKeyダイアログ;
		//private CTexture txカーソル;

		private void tアサインコードの描画_Joypad( int line, int x, int y, int nID, int nCode, bool b強調 )
		{
			string str = "";
			switch( nCode )
			{
				case 0:
					str = "Left";
					break;

				case 1:
					str = "Right";
					break;

				case 2:
					str = "Up";
					break;

				case 3:
					str = "Down";
					break;

				case 4:
					str = "Forward";
					break;

				case 5:
					str = "Back";
					break;

				case 6:
					str = "CCW";
					break;

				case 7:
					str = "CW";
					break;

				default:
					if ((8 <= nCode) && (nCode < 8 + 128))              // other buttons (128 types)
					{
						str = string.Format("Button{0}", nCode - 7);
					}
					else if ((8 + 128 <= nCode) && (nCode < 8 + 128 + 8))       // POV HAT ( 8 types; 45 degrees per HATs)
					{
						str = string.Format("POV {0}", (nCode - 8 - 128) * 45);
					}
					else
					{
						str = string.Format( "Code{0}", nCode );
					}
					break;
			}
			TJAPlayer3.stageコンフィグ.actFont.t文字列描画( x, y, string.Format( "{0,2}. Joypad #{1} ", line, nID ) + str, b強調, 0.75f );
		}
		private void tアサインコードの描画_Gamepad( int line, int x, int y, int nID, int nCode, bool b強調 )
		{
			string str = "";
					if ((8 <= nCode) && (nCode < 8 + 128))              // other buttons (128 types)
					{
						str = string.Format("Button{0}", nCode - 7);
					}
					else if ((8 + 128 <= nCode) && (nCode < 8 + 128 + 8))       // POV HAT ( 8 types; 45 degrees per HATs)
					{
						str = string.Format("POV {0}", (nCode - 8 - 128) * 45);
					}
					else
					{
						str = string.Format( "Code{0}", nCode );
					}
			TJAPlayer3.stageコンフィグ.actFont.t文字列描画( x, y, string.Format( "{0,2}. Gamepad #{1} ", line, nID ) + str, b強調, 0.75f );
		}
		private void tアサインコードの描画_Keyboard( int line, int x, int y, int nID, int nCode, bool b強調 )
		{
			string str = null;
			foreach( STKEYLABEL stkeylabel in this.KeyLabel )
			{
				if( stkeylabel.nCode == nCode )
				{
					str = string.Format( "{0,2}. Key {1}", line, stkeylabel.strLabel );
					break;
				}
			}
			if( str == null )
			{
				str = string.Format( "{0,2}. Key 0x{1:X2}", line, nCode );
			}
			TJAPlayer3.stageコンフィグ.actFont.t文字列描画( x, y, str, b強調, 0.75f );
		}
		private void tアサインコードの描画_MidiIn( int line, int x, int y, int nID, int nCode, bool b強調 )
		{
			TJAPlayer3.stageコンフィグ.actFont.t文字列描画( x, y, string.Format( "{0,2}. MidiIn #{1} code.{2}", line, nID, nCode ), b強調, 0.75f );
		}
		private void tアサインコードの描画_Mouse( int line, int x, int y, int nID, int nCode, bool b強調 )
		{
			TJAPlayer3.stageコンフィグ.actFont.t文字列描画( x, y, string.Format( "{0,2}. Mouse Button{1}", line, nCode ), b強調, 0.75f );
		}
		
		private bool tキーチェックとアサイン_Gamepad()
		{
			foreach( IInputDevice device in TJAPlayer3.InputManager.InputDevices )
			{
				if( device.CurrentType == InputDeviceType.Gamepad )
				{
					for (int i = 0; i < 15; i++)      // +8 for Axis, +8 for HAT
					{
						if (device.KeyPressed(i))
						{
							TJAPlayer3.Skin.soundDecideSFX.tPlay();
							TJAPlayer3.ConfigIni.t指定した入力が既にアサイン済みである場合はそれを全削除する( EInputDevice.Gamepad, device.ID, i, this.pad);
							TJAPlayer3.ConfigIni.KeyAssign[ (int) this.part ][ (int) this.pad ][ this.n現在の選択行 ].入力デバイス = EInputDevice.Gamepad;
							TJAPlayer3.ConfigIni.KeyAssign[ (int) this.part ][ (int) this.pad ][ this.n現在の選択行 ].ID = device.ID;
							TJAPlayer3.ConfigIni.KeyAssign[ (int) this.part ][ (int) this.pad ][ this.n現在の選択行 ].コード = i;
							return true;
						}
					}
				}
			}
			return false;
		}
		private bool tキーチェックとアサイン_Joypad()
		{
			foreach( IInputDevice device in TJAPlayer3.InputManager.InputDevices )
			{
				if( device.CurrentType == InputDeviceType.Joystick )
				{
					for (int i = 0; i < 15; i++)      // +8 for Axis, +8 for HAT
					{
						if (device.KeyPressed(i))
						{
							TJAPlayer3.Skin.soundDecideSFX.tPlay();
							TJAPlayer3.ConfigIni.t指定した入力が既にアサイン済みである場合はそれを全削除する( EInputDevice.Joypad, device.ID, i, this.pad);
							TJAPlayer3.ConfigIni.KeyAssign[ (int) this.part ][ (int) this.pad ][ this.n現在の選択行 ].入力デバイス = EInputDevice.Joypad;
							TJAPlayer3.ConfigIni.KeyAssign[ (int) this.part ][ (int) this.pad ][ this.n現在の選択行 ].ID = device.ID;
							TJAPlayer3.ConfigIni.KeyAssign[ (int) this.part ][ (int) this.pad ][ this.n現在の選択行 ].コード = i;
							return true;
						}
					}
				}
			}
			return false;
		}
		private bool tキーチェックとアサイン_Keyboard()
		{
			for( int i = 0; i < 144; i++ )
			{
                //if (i != (int)SlimDXKeys.Key.Escape &&
                //	i != (int)SlimDXKeys.Key.Return &&
                //	i != (int)SlimDXKeys.Key.UpArrow &&
                //	i != (int)SlimDXKeys.Key.DownArrow &&
                //	i != (int)SlimDXKeys.Key.LeftArrow &&
                //	i != (int)SlimDXKeys.Key.RightArrow &&
                //	 TJAPlayer3.InputManager.Keyboard.KeyPressed( i ) )
                //{
                if (i != (int)SlimDXKeys.Key.Escape &&
                    i != (int)SlimDXKeys.Key.Return &&
                     TJAPlayer3.InputManager.Keyboard.KeyPressed(i))
                {
                    TJAPlayer3.Skin.soundDecideSFX.tPlay();
					if (pad < EKeyConfigPad.Capture)
						TJAPlayer3.ConfigIni.t指定した入力が既にアサイン済みである場合はそれを全削除する( EInputDevice.Keyboard, 0, i, this.pad);
					TJAPlayer3.ConfigIni.KeyAssign[ (int) this.part ][ (int) this.pad ][ this.n現在の選択行 ].入力デバイス = EInputDevice.Keyboard;
					TJAPlayer3.ConfigIni.KeyAssign[ (int) this.part ][ (int) this.pad ][ this.n現在の選択行 ].ID = 0;
					TJAPlayer3.ConfigIni.KeyAssign[ (int) this.part ][ (int) this.pad ][ this.n現在の選択行 ].コード = i;
					return true;
				}
				else if (i == (int)SlimDXKeys.Key.Return && TJAPlayer3.InputManager.Keyboard.KeyPressed(i)) // Remove keybind
                {
					TJAPlayer3.Skin.soundCancelSFX.tPlay();
                    TJAPlayer3.ConfigIni.KeyAssign[(int)this.part][(int)this.pad][this.n現在の選択行].入力デバイス = EInputDevice.Unknown;
                    TJAPlayer3.ConfigIni.KeyAssign[(int)this.part][(int)this.pad][this.n現在の選択行].ID = 0;
                    TJAPlayer3.ConfigIni.KeyAssign[(int)this.part][(int)this.pad][this.n現在の選択行].コード = 0;
					return true;
                }
			}
			return false;
		}
		private bool tキーチェックとアサイン_MidiIn()
		{
			foreach( IInputDevice device in TJAPlayer3.InputManager.InputDevices )
			{
				if( device.CurrentType == InputDeviceType.MidiIn )
				{
					for( int i = 0; i < 0x100; i++ )
					{
						if( device.KeyPressed( i ) )
						{
							TJAPlayer3.Skin.soundDecideSFX.tPlay();
							TJAPlayer3.ConfigIni.t指定した入力が既にアサイン済みである場合はそれを全削除する( EInputDevice.MIDIInput, device.ID, i, this.pad);
							TJAPlayer3.ConfigIni.KeyAssign[ (int) this.part ][ (int) this.pad ][ this.n現在の選択行 ].入力デバイス = EInputDevice.MIDIInput;
							TJAPlayer3.ConfigIni.KeyAssign[ (int) this.part ][ (int) this.pad ][ this.n現在の選択行 ].ID = device.ID;
							TJAPlayer3.ConfigIni.KeyAssign[ (int) this.part ][ (int) this.pad ][ this.n現在の選択行 ].コード = i;
							return true;
						}
					}
				}
			}
			return false;
		}
		private bool tキーチェックとアサイン_Mouse()
		{
			for( int i = 0; i < 8; i++ )
			{
				if( TJAPlayer3.InputManager.Mouse.KeyPressed( i ) )
				{
					TJAPlayer3.ConfigIni.t指定した入力が既にアサイン済みである場合はそれを全削除する( EInputDevice.Mouse, 0, i, this.pad);
					TJAPlayer3.ConfigIni.KeyAssign[ (int) this.part ][ (int) this.pad ][ this.n現在の選択行 ].入力デバイス = EInputDevice.Mouse;
					TJAPlayer3.ConfigIni.KeyAssign[ (int) this.part ][ (int) this.pad ][ this.n現在の選択行 ].ID = 0;
					TJAPlayer3.ConfigIni.KeyAssign[ (int) this.part ][ (int) this.pad ][ this.n現在の選択行 ].コード = i;
				}
			}
			return false;
		}
		//-----------------
		#endregion
	}
}
