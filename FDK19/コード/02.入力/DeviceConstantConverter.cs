using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using SlimDXKeys;

using WindowsKey = System.Windows.Forms.Keys;
using SlimDXKey = SlimDXKeys.Key;
using SharpDXKey = SharpDX.DirectInput.Key;

namespace FDK
{
	public class DeviceConstantConverter
	{
		// メソッド

		public static Key DIKtoKey( int dik )
		{
			switch( dik )
			{
				case 1:
					return Key.Escape;

				case 2:
					return Key.D1;

				case 3:
					return Key.D2;

				case 4:
					return Key.D3;

				case 5:
					return Key.D4;

				case 6:
					return Key.D5;

				case 7:
					return Key.D6;

				case 8:
					return Key.D7;

				case 9:
					return Key.D8;

				case 10:
					return Key.D9;

				case 11:
					return Key.D0;

				case 12:
					return Key.Minus;

				case 13:
					return Key.Equals;

				case 14:
					return Key.Backspace;

				case 15:
					return Key.Tab;

				case 0x10:
					return Key.Q;

				case 0x11:
					return Key.W;

				case 0x12:
					return Key.E;

				case 0x13:
					return Key.R;

				case 20:
					return Key.T;

				case 0x15:
					return Key.Y;

				case 0x16:
					return Key.U;

				case 0x17:
					return Key.I;

				case 0x18:
					return Key.O;

				case 0x19:
					return Key.P;

				case 0x1a:
					return Key.LeftBracket;

				case 0x1b:
					return Key.RightBracket;

				case 0x1c:
					return Key.Return;

				case 0x1d:
					return Key.LeftControl;

				case 30:
					return Key.A;

				case 0x1f:
					return Key.S;

				case 0x20:
					return Key.D;

				case 0x21:
					return Key.F;

				case 0x22:
					return Key.G;

				case 0x23:
					return Key.H;

				case 0x24:
					return Key.J;

				case 0x25:
					return Key.K;

				case 0x26:
					return Key.L;

				case 0x27:
					return Key.Semicolon;

				case 40:
					return Key.Apostrophe;

				case 0x29:
					return Key.Grave;

				case 0x2a:
					return Key.LeftShift;

				case 0x2b:
					return Key.Backslash;

				case 0x2c:
					return Key.Z;

				case 0x2d:
					return Key.X;

				case 0x2e:
					return Key.C;

				case 0x2f:
					return Key.V;

				case 0x30:
					return Key.B;

				case 0x31:
					return Key.N;

				case 50:
					return Key.M;

				case 0x33:
					return Key.Comma;

				case 0x34:
					return Key.Period;

				case 0x35:
					return Key.Slash;

				case 0x36:
					return Key.RightShift;

				case 0x37:
					return Key.NumberPadStar;

				case 0x38:
					return Key.LeftAlt;

				case 0x39:
					return Key.Space;

				case 0x3a:
					return Key.CapsLock;

				case 0x3b:
					return Key.F1;

				case 60:
					return Key.F2;

				case 0x3d:
					return Key.F3;

				case 0x3e:
					return Key.F4;

				case 0x3f:
					return Key.F5;

				case 0x40:
					return Key.F6;

				case 0x41:
					return Key.F7;

				case 0x42:
					return Key.F8;

				case 0x43:
					return Key.F9;

				case 0x44:
					return Key.F10;

				case 0x45:
					return Key.NumberLock;

				case 70:
					return Key.ScrollLock;

				case 0x47:
					return Key.NumberPad7;

				case 0x48:
					return Key.NumberPad8;

				case 0x49:
					return Key.NumberPad9;

				case 0x4a:
					return Key.NumberPadMinus;

				case 0x4b:
					return Key.NumberPad4;

				case 0x4c:
					return Key.NumberPad5;

				case 0x4d:
					return Key.NumberPad6;

				case 0x4e:
					return Key.NumberPadPlus;

				case 0x4f:
					return Key.NumberPad1;

				case 80:
					return Key.NumberPad2;

				case 0x51:
					return Key.NumberPad3;

				case 0x52:
					return Key.NumberPad0;

				case 0x53:
					return Key.NumberPadPeriod;

				case 0x56:
					return Key.Oem102;

				case 0x57:
					return Key.F11;

				case 0x58:
					return Key.F12;

				case 100:
					return Key.F13;

				case 0x65:
					return Key.F14;

				case 0x66:
					return Key.F15;

				case 0x70:
					return Key.Kana;

				case 0x73:
					return Key.AbntC1;

				case 0x79:
					return Key.Convert;

				case 0x7b:
					return Key.NoConvert;

				case 0x7d:
					return Key.Yen;

				case 0x7e:
					return Key.AbntC2;

				case 0x8d:
					return Key.NumberPadEquals;

				case 0x90:
					return Key.PreviousTrack;

				case 0x91:
					return Key.AT;

				case 0x92:
					return Key.Colon;

				case 0x93:
					return Key.Underline;

				case 0x94:
					return Key.Kanji;

				case 0x95:
					return Key.Stop;

				case 150:
					return Key.AX;

				case 0x97:
					return Key.Unlabeled;

				case 0x99:
					return Key.NextTrack;

				case 0x9c:
					return Key.NumberPadEnter;

				case 0x9d:
					return Key.RightControl;

				case 160:
					return Key.Mute;

				case 0xa1:
					return Key.Calculator;

				case 0xa2:
					return Key.PlayPause;

				case 0xa4:
					return Key.MediaStop;

				case 0xae:
					return Key.VolumeDown;

				case 0xb0:
					return Key.VolumeUp;

				case 0xb2:
					return Key.WebHome;

				case 0xb3:
					return Key.NumberPadComma;

				case 0xb5:
					return Key.NumberPadSlash;

				case 0xb7:
					return Key.PrintScreen;

				case 0xb8:
					return Key.RightAlt;

				case 0xc5:
					return Key.Pause;

				case 0xc7:
					return Key.Home;

				case 200:
					return Key.UpArrow;

				case 0xc9:
					return Key.PageUp;

				case 0xcb:
					return Key.LeftArrow;

				case 0xcd:
					return Key.RightArrow;

				case 0xcf:
					return Key.End;

				case 0xd0:
					return Key.DownArrow;

				case 0xd1:
					return Key.PageDown;

				case 210:
					return Key.Insert;

				case 0xd3:
					return Key.Delete;

				case 0xdb:
					return Key.LeftWindowsKey;

				case 220:
					return Key.RightWindowsKey;

				case 0xdd:
					return Key.Applications;

				case 0xde:
					return Key.Power;

				case 0xdf:
					return Key.Sleep;

				case 0xe3:
					return Key.Wake;

				case 0xe5:
					return Key.WebSearch;

				case 230:
					return Key.WebFavorites;

				case 0xe7:
					return Key.WebRefresh;

				case 0xe8:
					return Key.WebStop;

				case 0xe9:
					return Key.WebForward;

				case 0xea:
					return Key.WebBack;

				case 0xeb:
					return Key.MyComputer;

				case 0xec:
					return Key.Mail;

				case 0xed:
					return Key.MediaSelect;
			}
			return Key.Unknown;
		}
		public static int KeyToDIK( Key key )
		{
			switch( key )
			{
				case Key.D0:
					return 11;

				case Key.D1:
					return 2;

				case Key.D2:
					return 3;

				case Key.D3:
					return 4;

				case Key.D4:
					return 5;

				case Key.D5:
					return 6;

				case Key.D6:
					return 7;

				case Key.D7:
					return 8;

				case Key.D8:
					return 9;

				case Key.D9:
					return 10;

				case Key.A:
					return 30;

				case Key.B:
					return 0x30;

				case Key.C:
					return 0x2e;

				case Key.D:
					return 0x20;

				case Key.E:
					return 0x12;

				case Key.F:
					return 0x21;

				case Key.G:
					return 0x22;

				case Key.H:
					return 0x23;

				case Key.I:
					return 0x17;

				case Key.J:
					return 0x24;

				case Key.K:
					return 0x25;

				case Key.L:
					return 0x26;

				case Key.M:
					return 50;

				case Key.N:
					return 0x31;

				case Key.O:
					return 0x18;

				case Key.P:
					return 0x19;

				case Key.Q:
					return 0x10;

				case Key.R:
					return 0x13;

				case Key.S:
					return 0x1f;

				case Key.T:
					return 20;

				case Key.U:
					return 0x16;

				case Key.V:
					return 0x2f;

				case Key.W:
					return 0x11;

				case Key.X:
					return 0x2d;

				case Key.Y:
					return 0x15;

				case Key.Z:
					return 0x2c;

				case Key.AbntC1:
					return 0x73;

				case Key.AbntC2:
					return 0x7e;

				case Key.Apostrophe:
					return 40;

				case Key.Applications:
					return 0xdd;

				case Key.AT:
					return 0x91;

				case Key.AX:
					return 150;

				case Key.Backspace:
					return 14;

				case Key.Backslash:
					return 0x2b;

				case Key.Calculator:
					return 0xa1;

				case Key.CapsLock:
					return 0x3a;

				case Key.Colon:
					return 0x92;

				case Key.Comma:
					return 0x33;

				case Key.Convert:
					return 0x79;

				case Key.Delete:
					return 0xd3;

				case Key.DownArrow:
					return 0xd0;

				case Key.End:
					return 0xcf;

				case Key.Equals:
					return 13;

				case Key.Escape:
					return 1;

				case Key.F1:
					return 0x3b;

				case Key.F2:
					return 60;

				case Key.F3:
					return 0x3d;

				case Key.F4:
					return 0x3e;

				case Key.F5:
					return 0x3f;

				case Key.F6:
					return 0x40;

				case Key.F7:
					return 0x41;

				case Key.F8:
					return 0x42;

				case Key.F9:
					return 0x43;

				case Key.F10:
					return 0x44;

				case Key.F11:
					return 0x57;

				case Key.F12:
					return 0x58;

				case Key.F13:
					return 100;

				case Key.F14:
					return 0x65;

				case Key.F15:
					return 0x66;

				case Key.Grave:
					return 0x29;

				case Key.Home:
					return 0xc7;

				case Key.Insert:
					return 210;

				case Key.Kana:
					return 0x70;

				case Key.Kanji:
					return 0x94;

				case Key.LeftBracket:
					return 0x1a;

				case Key.LeftControl:
					return 0x1d;

				case Key.LeftArrow:
					return 0xcb;

				case Key.LeftAlt:
					return 0x38;

				case Key.LeftShift:
					return 0x2a;

				case Key.LeftWindowsKey:
					return 0xdb;

				case Key.Mail:
					return 0xec;

				case Key.MediaSelect:
					return 0xed;

				case Key.MediaStop:
					return 0xa4;

				case Key.Minus:
					return 12;

				case Key.Mute:
					return 160;

				case Key.MyComputer:
					return 0xeb;

				case Key.NextTrack:
					return 0x99;

				case Key.NoConvert:
					return 0x7b;

				case Key.NumberLock:
					return 0x45;

				case Key.NumberPad0:
					return 0x52;

				case Key.NumberPad1:
					return 0x4f;

				case Key.NumberPad2:
					return 80;

				case Key.NumberPad3:
					return 0x51;

				case Key.NumberPad4:
					return 0x4b;

				case Key.NumberPad5:
					return 0x4c;

				case Key.NumberPad6:
					return 0x4d;

				case Key.NumberPad7:
					return 0x47;

				case Key.NumberPad8:
					return 0x48;

				case Key.NumberPad9:
					return 0x49;

				case Key.NumberPadComma:
					return 0xb3;

				case Key.NumberPadEnter:
					return 0x9c;

				case Key.NumberPadEquals:
					return 0x8d;

				case Key.NumberPadMinus:
					return 0x4a;

				case Key.NumberPadPeriod:
					return 0x53;

				case Key.NumberPadPlus:
					return 0x4e;

				case Key.NumberPadSlash:
					return 0xb5;

				case Key.NumberPadStar:
					return 0x37;

				case Key.Oem102:
					return 0x56;

				case Key.PageDown:
					return 0xd1;

				case Key.PageUp:
					return 0xc9;

				case Key.Pause:
					return 0xc5;

				case Key.Period:
					return 0x34;

				case Key.PlayPause:
					return 0xa2;

				case Key.Power:
					return 0xde;

				case Key.PreviousTrack:
					return 0x90;

				case Key.RightBracket:
					return 0x1b;

				case Key.RightControl:
					return 0x9d;

				case Key.Return:
					return 0x1c;

				case Key.RightArrow:
					return 0xcd;

				case Key.RightAlt:
					return 0xb8;

				case Key.RightShift:
					return 0x36;

				case Key.RightWindowsKey:
					return 220;

				case Key.ScrollLock:
					return 70;

				case Key.Semicolon:
					return 0x27;

				case Key.Slash:
					return 0x35;

				case Key.Sleep:
					return 0xdf;

				case Key.Space:
					return 0x39;

				case Key.Stop:
					return 0x95;

				case Key.PrintScreen:
					return 0xb7;

				case Key.Tab:
					return 15;

				case Key.Underline:
					return 0x93;

				case Key.Unlabeled:
					return 0x97;

				case Key.UpArrow:
					return 200;

				case Key.VolumeDown:
					return 0xae;

				case Key.VolumeUp:
					return 0xb0;

				case Key.Wake:
					return 0xe3;

				case Key.WebBack:
					return 0xea;

				case Key.WebFavorites:
					return 230;

				case Key.WebForward:
					return 0xe9;

				case Key.WebHome:
					return 0xb2;

				case Key.WebRefresh:
					return 0xe7;

				case Key.WebSearch:
					return 0xe5;

				case Key.WebStop:
					return 0xe8;

				case Key.Yen:
					return 0x7d;
			}
			return 0;
		}
		public static Keys KeyToKeyCode(SlimDXKey key)
		{
			switch ( key )
			{
				case Key.D0:
					return Keys.D0;

				case Key.D1:
					return Keys.D1;

				case Key.D2:
					return Keys.D2;

				case Key.D3:
					return Keys.D3;

				case Key.D4:
					return Keys.D4;

				case Key.D5:
					return Keys.D5;

				case Key.D6:
					return Keys.D6;

				case Key.D7:
					return Keys.D7;

				case Key.D8:
					return Keys.D8;

				case Key.D9:
					return Keys.D9;

				case Key.A:
					return Keys.A;

				case Key.B:
					return Keys.B;

				case Key.C:
					return Keys.C;

				case Key.D:
					return Keys.D;

				case Key.E:
					return Keys.E;

				case Key.F:
					return Keys.F;

				case Key.G:
					return Keys.G;

				case Key.H:
					return Keys.H;

				case Key.I:
					return Keys.I;

				case Key.J:
					return Keys.J;

				case Key.K:
					return Keys.K;

				case Key.L:
					return Keys.L;

				case Key.M:
					return Keys.M;

				case Key.N:
					return Keys.N;

				case Key.O:
					return Keys.O;

				case Key.P:
					return Keys.P;

				case Key.Q:
					return Keys.Q;

				case Key.R:
					return Keys.R;

				case Key.S:
					return Keys.S;

				case Key.T:
					return Keys.T;

				case Key.U:
					return Keys.U;

				case Key.V:
					return Keys.V;

				case Key.W:
					return Keys.W;

				case Key.X:
					return Keys.X;

				case Key.Y:
					return Keys.Y;

				case Key.Z:
					return Keys.Z;

//				case Key.AbntC1:
//					return Keys.A; //0x73;
					//147
//				case Key.AbntC2:
//					return Keys.A; //0x7e;

//				case Key.Apostrophe:
//					return Keys.A;			///

				case Key.Applications:
					return Keys.Apps;

				case Key.AT:
					return Keys.Oem3;

//				case Key.AX:
//					return Keys.A;			///

				case Key.Backspace:
					return Keys.Back;

				case Key.Backslash:
					return Keys.Oem5;

//				case Key.Calculator:
//					return Keys.A;			///

				case Key.CapsLock:
					return Keys.CapsLock;

				case Key.Colon:
					return Keys.Oem1;

				case Key.Comma:
					return Keys.Oemcomma;

				case Key.Convert:
					return Keys.IMEConvert;

				case Key.Delete:
					return Keys.Delete;

				case Key.DownArrow:
					return Keys.Down;

				case Key.End:
					return Keys.End;

				case Key.Equals:
					return Keys.A;			///

				case Key.Escape:
					return Keys.Escape;

				case Key.F1:
					return Keys.F1;

				case Key.F2:
					return Keys.F2;

				case Key.F3:
					return Keys.F3;

				case Key.F4:
					return Keys.F4;

				case Key.F5:
					return Keys.F5;

				case Key.F6:
					return Keys.F6;

				case Key.F7:
					return Keys.F7;

				case Key.F8:
					return Keys.F8;

				case Key.F9:
					return Keys.F9;

				case Key.F10:
					return Keys.F10;

				case Key.F11:
					return Keys.F11;

				case Key.F12:
					return Keys.F12;

				case Key.F13:
					return Keys.F13;

				case Key.F14:
					return Keys.F14;

				case Key.F15:
					return Keys.F15;

				case Key.Grave:
					return Keys.A;			///

				case Key.Home:
					return Keys.Home;

				case Key.Insert:
					return Keys.Insert;

				case Key.Kana:
					return Keys.KanaMode;

				case Key.Kanji:
					return Keys.KanjiMode;

				case Key.LeftBracket:
					return Keys.Oem4;

				case Key.LeftControl:
					return Keys.LControlKey;

				case Key.LeftArrow:
					return Keys.Left;

				case Key.LeftAlt:
					return Keys.LMenu;

				case Key.LeftShift:
					return Keys.LShiftKey;

				case Key.LeftWindowsKey:
					return Keys.LWin;

				case Key.Mail:
					return Keys.LaunchMail;

				case Key.MediaSelect:
					return Keys.SelectMedia;

				case Key.MediaStop:
					return Keys.MediaStop;

				case Key.Minus:
					return Keys.OemMinus;

				case Key.Mute:
					return Keys.VolumeMute;

				case Key.MyComputer:			///
					return Keys.A;

				case Key.NextTrack:
					return Keys.MediaNextTrack;

				case Key.NoConvert:
					return Keys.IMENonconvert;

				case Key.NumberLock:
					return Keys.NumLock;

				case Key.NumberPad0:
					return Keys.NumPad0;

				case Key.NumberPad1:
					return Keys.NumPad1;

				case Key.NumberPad2:
					return Keys.NumPad2;

				case Key.NumberPad3:
					return Keys.NumPad3;

				case Key.NumberPad4:
					return Keys.NumPad4;

				case Key.NumberPad5:
					return Keys.NumPad5;

				case Key.NumberPad6:
					return Keys.NumPad6;

				case Key.NumberPad7:
					return Keys.NumPad7;

				case Key.NumberPad8:
					return Keys.NumPad8;

				case Key.NumberPad9:
					return Keys.NumPad9;

				case Key.NumberPadComma:
					return Keys.Separator;

				case Key.NumberPadEnter:
					return Keys.A;				//

				case Key.NumberPadEquals:
					return Keys.A;				//

				case Key.NumberPadMinus:
					return Keys.Subtract;

				case Key.NumberPadPeriod:
					return Keys.Decimal;

				case Key.NumberPadPlus:
					return Keys.Add;

				case Key.NumberPadSlash:
					return Keys.Divide;

				case Key.NumberPadStar:
					return Keys.Multiply;		//

				case Key.Oem102:
					return Keys.Oem102;

				case Key.PageDown:
					return Keys.PageDown;

				case Key.PageUp:
					return Keys.PageUp;

				case Key.Pause:
					return Keys.Pause;

				case Key.Period:
					return Keys.OemPeriod;

				case Key.PlayPause:
					return Keys.MediaPlayPause;

				case Key.Power:
					return Keys.A;				///

				case Key.PreviousTrack:
					return Keys.MediaPreviousTrack;

				case Key.RightBracket:
					return Keys.Oem6;

				case Key.RightControl:
					return Keys.RControlKey;

				case Key.Return:
					return Keys.Return;

				case Key.RightArrow:
					return Keys.Right;

				case Key.RightAlt:
					return Keys.RMenu;

				case Key.RightShift:
					return Keys.A;

				case Key.RightWindowsKey:
					return Keys.RWin;

				case Key.ScrollLock:
					return Keys.Scroll;

				case Key.Semicolon:
					return Keys.Oemplus;		///??

				case Key.Slash:
					return Keys.Oem2;

				case Key.Sleep:
					return Keys.Sleep;

				case Key.Space:
					return Keys.Space;

				case Key.Stop:
					return Keys.MediaStop;

				case Key.PrintScreen:
					return Keys.PrintScreen;

				case Key.Tab:
					return Keys.Tab;

				case Key.Underline:
					return Keys.Oem102;

//				case Key.Unlabeled:				///
//					return Keys.A;

				case Key.UpArrow:
					return Keys.Up;

				case Key.VolumeDown:
					return Keys.VolumeDown;

				case Key.VolumeUp:
					return Keys.VolumeUp;

				case Key.Wake:
					return Keys.A;				///

				case Key.WebBack:
					return Keys.BrowserBack;

				case Key.WebFavorites:
					return Keys.BrowserFavorites;

				case Key.WebForward:
					return Keys.BrowserForward;

				case Key.WebHome:
					return Keys.BrowserHome;

				case Key.WebRefresh:
					return Keys.BrowserRefresh;

				case Key.WebSearch:
					return Keys.BrowserSearch;

				case Key.WebStop:
					return Keys.BrowserStop;

				case Key.Yen:
					return Keys.OemBackslash;
			}
			return 0;
		}

		/// <returns>
		///		対応する値がなければ SlimDX.DirectInput.Unknown を返す。
		/// </returns>
		public static SlimDXKey DIKtoKey(SharpDXKey key)
		{
			if (_DIKtoKey.ContainsKey(key))
			{
				return _DIKtoKey[key];
			}
			else
			{
				return SlimDXKey.Unknown;
			}
		}

		/// <returns>
		///		対応する値がなければ System.Windows.Forms.Keys.None を返す。
		/// </returns>
		public static WindowsKey KeyToKeys(SlimDXKey key)
		{
			if (_KeyToKeys.ContainsKey(key))
			{
				return _KeyToKeys[key];
			}
			else
			{
				return WindowsKey.None;
			}
		}


		/// <summary>
		///		DIK (SharpDX.DirectInput.Key) から SlimDX.DirectInput.Key への変換表。
		/// </summary>
		private static readonly Dictionary<SharpDXKey, SlimDXKey> _DIKtoKey = new Dictionary<SharpDXKey, SlimDXKey>() {
			#region [ *** ]
			{ SharpDXKey.Unknown, SlimDXKey.Unknown },
			{ SharpDXKey.Escape, SlimDXKey.Escape },
			{ SharpDXKey.D1, SlimDXKey.D1 },
			{ SharpDXKey.D2, SlimDXKey.D2 },
			{ SharpDXKey.D3, SlimDXKey.D3 },
			{ SharpDXKey.D4, SlimDXKey.D4 },
			{ SharpDXKey.D5, SlimDXKey.D5 },
			{ SharpDXKey.D6, SlimDXKey.D6 },
			{ SharpDXKey.D7, SlimDXKey.D7 },
			{ SharpDXKey.D8, SlimDXKey.D8 },
			{ SharpDXKey.D9, SlimDXKey.D9 },
			{ SharpDXKey.D0, SlimDXKey.D0 },
			{ SharpDXKey.Minus, SlimDXKey.Minus },
			{ SharpDXKey.Equals, SlimDXKey.Equals },
			{ SharpDXKey.Back, SlimDXKey.Backspace },
			{ SharpDXKey.Tab, SlimDXKey.Tab },
			{ SharpDXKey.Q, SlimDXKey.Q },
			{ SharpDXKey.W, SlimDXKey.W },
			{ SharpDXKey.E, SlimDXKey.E },
			{ SharpDXKey.R, SlimDXKey.R },
			{ SharpDXKey.T, SlimDXKey.T },
			{ SharpDXKey.Y, SlimDXKey.Y },
			{ SharpDXKey.U, SlimDXKey.U },
			{ SharpDXKey.I, SlimDXKey.I },
			{ SharpDXKey.O, SlimDXKey.O },
			{ SharpDXKey.P, SlimDXKey.P },
			{ SharpDXKey.LeftBracket, SlimDXKey.LeftBracket },
			{ SharpDXKey.RightBracket, SlimDXKey.RightBracket },
			{ SharpDXKey.Return, SlimDXKey.Return },
			{ SharpDXKey.LeftControl, SlimDXKey.LeftControl },
			{ SharpDXKey.A, SlimDXKey.A },
			{ SharpDXKey.S, SlimDXKey.S },
			{ SharpDXKey.D, SlimDXKey.D },
			{ SharpDXKey.F, SlimDXKey.F },
			{ SharpDXKey.G, SlimDXKey.G },
			{ SharpDXKey.H, SlimDXKey.H },
			{ SharpDXKey.J, SlimDXKey.J },
			{ SharpDXKey.K, SlimDXKey.K },
			{ SharpDXKey.L, SlimDXKey.L },
			{ SharpDXKey.Semicolon, SlimDXKey.Semicolon },
			{ SharpDXKey.Apostrophe, SlimDXKey.Apostrophe },
			{ SharpDXKey.Grave, SlimDXKey.Grave },
			{ SharpDXKey.LeftShift, SlimDXKey.LeftShift },
			{ SharpDXKey.Backslash, SlimDXKey.Backslash },
			{ SharpDXKey.Z, SlimDXKey.Z },
			{ SharpDXKey.X, SlimDXKey.X },
			{ SharpDXKey.C, SlimDXKey.C },
			{ SharpDXKey.V, SlimDXKey.V },
			{ SharpDXKey.B, SlimDXKey.B },
			{ SharpDXKey.N, SlimDXKey.N },
			{ SharpDXKey.M, SlimDXKey.M },
			{ SharpDXKey.Comma, SlimDXKey.Comma },
			{ SharpDXKey.Period, SlimDXKey.Period },
			{ SharpDXKey.Slash, SlimDXKey.Slash },
			{ SharpDXKey.RightShift, SlimDXKey.RightShift },
			{ SharpDXKey.Multiply, SlimDXKey.NumberPadStar },
			{ SharpDXKey.LeftAlt, SlimDXKey.LeftAlt },
			{ SharpDXKey.Space, SlimDXKey.Space },
			{ SharpDXKey.Capital, SlimDXKey.CapsLock },
			{ SharpDXKey.F1, SlimDXKey.F1 },
			{ SharpDXKey.F2, SlimDXKey.F2 },
			{ SharpDXKey.F3, SlimDXKey.F3 },
			{ SharpDXKey.F4, SlimDXKey.F4 },
			{ SharpDXKey.F5, SlimDXKey.F5 },
			{ SharpDXKey.F6, SlimDXKey.F6 },
			{ SharpDXKey.F7, SlimDXKey.F7 },
			{ SharpDXKey.F8, SlimDXKey.F8 },
			{ SharpDXKey.F9, SlimDXKey.F9 },
			{ SharpDXKey.F10, SlimDXKey.F10 },
			{ SharpDXKey.NumberLock, SlimDXKey.NumberLock },
			{ SharpDXKey.ScrollLock, SlimDXKey.ScrollLock },
			{ SharpDXKey.NumberPad7, SlimDXKey.NumberPad7 },
			{ SharpDXKey.NumberPad8, SlimDXKey.NumberPad8 },
			{ SharpDXKey.NumberPad9, SlimDXKey.NumberPad9 },
			{ SharpDXKey.Subtract, SlimDXKey.NumberPadMinus },
			{ SharpDXKey.NumberPad4, SlimDXKey.NumberPad4 },
			{ SharpDXKey.NumberPad5, SlimDXKey.NumberPad5 },
			{ SharpDXKey.NumberPad6, SlimDXKey.NumberPad6 },
			{ SharpDXKey.Add, SlimDXKey.NumberPadPlus },
			{ SharpDXKey.NumberPad1, SlimDXKey.NumberPad1 },
			{ SharpDXKey.NumberPad2, SlimDXKey.NumberPad2 },
			{ SharpDXKey.NumberPad3, SlimDXKey.NumberPad3 },
			{ SharpDXKey.NumberPad0, SlimDXKey.NumberPad0 },
			{ SharpDXKey.Decimal, SlimDXKey.NumberPadPeriod },
			{ SharpDXKey.Oem102, SlimDXKey.Oem102 },
			{ SharpDXKey.F11, SlimDXKey.F11 },
			{ SharpDXKey.F12, SlimDXKey.F12 },
			{ SharpDXKey.F13, SlimDXKey.F13 },
			{ SharpDXKey.F14, SlimDXKey.F14 },
			{ SharpDXKey.F15, SlimDXKey.F15 },
			{ SharpDXKey.Kana, SlimDXKey.Kana },
			{ SharpDXKey.AbntC1, SlimDXKey.AbntC1 },
			{ SharpDXKey.Convert, SlimDXKey.Convert },
			{ SharpDXKey.NoConvert, SlimDXKey.NoConvert },
			{ SharpDXKey.Yen, SlimDXKey.Yen },
			{ SharpDXKey.AbntC2, SlimDXKey.AbntC2 },
			{ SharpDXKey.NumberPadEquals, SlimDXKey.NumberPadEquals },
			{ SharpDXKey.PreviousTrack, SlimDXKey.PreviousTrack },
			{ SharpDXKey.AT, SlimDXKey.AT },
			{ SharpDXKey.Colon, SlimDXKey.Colon },
			{ SharpDXKey.Underline, SlimDXKey.Underline },
			{ SharpDXKey.Kanji, SlimDXKey.Kanji },
			{ SharpDXKey.Stop, SlimDXKey.Stop },
			{ SharpDXKey.AX, SlimDXKey.AX },
			{ SharpDXKey.Unlabeled, SlimDXKey.Unlabeled },
			{ SharpDXKey.NextTrack, SlimDXKey.NextTrack },
			{ SharpDXKey.NumberPadEnter, SlimDXKey.NumberPadEnter },
			{ SharpDXKey.RightControl, SlimDXKey.RightControl },
			{ SharpDXKey.Mute, SlimDXKey.Mute },
			{ SharpDXKey.Calculator, SlimDXKey.Calculator },
			{ SharpDXKey.PlayPause, SlimDXKey.PlayPause },
			{ SharpDXKey.MediaStop, SlimDXKey.MediaStop },
			{ SharpDXKey.VolumeDown, SlimDXKey.VolumeDown },
			{ SharpDXKey.VolumeUp, SlimDXKey.VolumeUp },
			{ SharpDXKey.WebHome, SlimDXKey.WebHome },
			{ SharpDXKey.PrintScreen, SlimDXKey.PrintScreen },
			{ SharpDXKey.RightAlt, SlimDXKey.RightAlt },
			{ SharpDXKey.Pause, SlimDXKey.Pause },
			{ SharpDXKey.Home, SlimDXKey.Home },
			{ SharpDXKey.Up, SlimDXKey.UpArrow },
			{ SharpDXKey.PageUp, SlimDXKey.PageUp },
			{ SharpDXKey.Left, SlimDXKey.LeftArrow },
			{ SharpDXKey.Right, SlimDXKey.RightArrow },
			{ SharpDXKey.End, SlimDXKey.End },
			{ SharpDXKey.Down, SlimDXKey.DownArrow },
			{ SharpDXKey.PageDown, SlimDXKey.PageDown },
			{ SharpDXKey.Insert, SlimDXKey.Insert },
			{ SharpDXKey.Delete, SlimDXKey.Delete },
			{ SharpDXKey.LeftWindowsKey, SlimDXKey.LeftWindowsKey },
			{ SharpDXKey.RightWindowsKey, SlimDXKey.RightWindowsKey },
			{ SharpDXKey.Applications, SlimDXKey.Applications },
			{ SharpDXKey.Power, SlimDXKey.Power },
			{ SharpDXKey.Sleep, SlimDXKey.Sleep },
			{ SharpDXKey.Wake, SlimDXKey.Wake },
			{ SharpDXKey.WebSearch, SlimDXKey.WebSearch },
			{ SharpDXKey.WebFavorites, SlimDXKey.WebFavorites },
			{ SharpDXKey.WebRefresh, SlimDXKey.WebRefresh },
			{ SharpDXKey.WebStop, SlimDXKey.WebStop },
			{ SharpDXKey.WebForward, SlimDXKey.WebForward },
			{ SharpDXKey.WebBack, SlimDXKey.WebBack },
			{ SharpDXKey.MyComputer, SlimDXKey.MyComputer },
			{ SharpDXKey.Mail, SlimDXKey.Mail },
			{ SharpDXKey.MediaSelect, SlimDXKey.MediaSelect },
			#endregion
		};

		/// <summary>
		///		SlimDX.DirectInput.Key から System.Windows.Form.Keys への変換表。
		/// </summary>
		private static readonly Dictionary<SlimDXKey, WindowsKey> _KeyToKeys = new Dictionary<SlimDXKey, WindowsKey>() {
			#region [ *** ]
			{ SlimDXKey.D0, WindowsKey.D0 },
			{ SlimDXKey.D1, WindowsKey.D1 },
			{ SlimDXKey.D2, WindowsKey.D2 },
			{ SlimDXKey.D3, WindowsKey.D3 },
			{ SlimDXKey.D4, WindowsKey.D4 },
			{ SlimDXKey.D5, WindowsKey.D5 },
			{ SlimDXKey.D6, WindowsKey.D6 },
			{ SlimDXKey.D7, WindowsKey.D7 },
			{ SlimDXKey.D8, WindowsKey.D8 },
			{ SlimDXKey.D9, WindowsKey.D9 },
			{ SlimDXKey.A, WindowsKey.A },
			{ SlimDXKey.B, WindowsKey.B },
			{ SlimDXKey.C, WindowsKey.C },
			{ SlimDXKey.D, WindowsKey.D },
			{ SlimDXKey.E, WindowsKey.E },
			{ SlimDXKey.F, WindowsKey.F },
			{ SlimDXKey.G, WindowsKey.G },
			{ SlimDXKey.H, WindowsKey.H },
			{ SlimDXKey.I, WindowsKey.I },
			{ SlimDXKey.J, WindowsKey.J },
			{ SlimDXKey.K, WindowsKey.K },
			{ SlimDXKey.L, WindowsKey.L },
			{ SlimDXKey.M, WindowsKey.M },
			{ SlimDXKey.N, WindowsKey.N },
			{ SlimDXKey.O, WindowsKey.O },
			{ SlimDXKey.P, WindowsKey.P },
			{ SlimDXKey.Q, WindowsKey.Q },
			{ SlimDXKey.R, WindowsKey.R },
			{ SlimDXKey.S, WindowsKey.S },
			{ SlimDXKey.T, WindowsKey.T },
			{ SlimDXKey.U, WindowsKey.U },
			{ SlimDXKey.V, WindowsKey.V },
			{ SlimDXKey.W, WindowsKey.W },
			{ SlimDXKey.X, WindowsKey.X },
			{ SlimDXKey.Y, WindowsKey.Y },
			{ SlimDXKey.Z, WindowsKey.Z },
			//{ SlimDXKey.AbntC1, WindowsKey.A },
			//{ SlimDXKey.AbntC2, WindowsKey.A },
			{ SlimDXKey.Apostrophe, WindowsKey.OemQuotes },
			{ SlimDXKey.Applications, WindowsKey.Apps },
			{ SlimDXKey.AT, WindowsKey.Oem3 },	// OemTilde と同値
			//{ SlimDXKey.AX, WindowsKey.A },	// OemAX (225) は未定義
			{ SlimDXKey.Backspace, WindowsKey.Back },
			{ SlimDXKey.Backslash, WindowsKey.OemBackslash },
			//{ SlimDXKey.Calculator, WindowsKey.A },
			{ SlimDXKey.CapsLock, WindowsKey.CapsLock },
			{ SlimDXKey.Colon, WindowsKey.Oem1 },
			{ SlimDXKey.Comma, WindowsKey.Oemcomma },
			{ SlimDXKey.Convert, WindowsKey.IMEConvert },
			{ SlimDXKey.Delete, WindowsKey.Delete },
			{ SlimDXKey.DownArrow, WindowsKey.Down },
			{ SlimDXKey.End, WindowsKey.End },
			{ SlimDXKey.Equals, WindowsKey.A },		// ?
			{ SlimDXKey.Escape, WindowsKey.Escape },
			{ SlimDXKey.F1, WindowsKey.F1 },
			{ SlimDXKey.F2, WindowsKey.F2 },
			{ SlimDXKey.F3, WindowsKey.F3 },
			{ SlimDXKey.F4, WindowsKey.F4 },
			{ SlimDXKey.F5, WindowsKey.F5 },
			{ SlimDXKey.F6, WindowsKey.F6 },
			{ SlimDXKey.F7, WindowsKey.F7 },
			{ SlimDXKey.F8, WindowsKey.F8 },
			{ SlimDXKey.F9, WindowsKey.F9 },
			{ SlimDXKey.F10, WindowsKey.F10 },
			{ SlimDXKey.F11, WindowsKey.F11 },
			{ SlimDXKey.F12, WindowsKey.F12 },
			{ SlimDXKey.F13, WindowsKey.F13 },
			{ SlimDXKey.F14, WindowsKey.F14 },
			{ SlimDXKey.F15, WindowsKey.F15 },
			{ SlimDXKey.Grave, WindowsKey.A },		// ?
			{ SlimDXKey.Home, WindowsKey.Home },
			{ SlimDXKey.Insert, WindowsKey.Insert },
			{ SlimDXKey.Kana, WindowsKey.KanaMode },
			{ SlimDXKey.Kanji, WindowsKey.KanjiMode },
			{ SlimDXKey.LeftBracket, WindowsKey.Oem4 },
			{ SlimDXKey.LeftControl, WindowsKey.LControlKey },
			{ SlimDXKey.LeftArrow, WindowsKey.Left },
			{ SlimDXKey.LeftAlt, WindowsKey.LMenu },
			{ SlimDXKey.LeftShift, WindowsKey.LShiftKey },
			{ SlimDXKey.LeftWindowsKey, WindowsKey.LWin },
			{ SlimDXKey.Mail, WindowsKey.LaunchMail },
			{ SlimDXKey.MediaSelect, WindowsKey.SelectMedia },
			{ SlimDXKey.MediaStop, WindowsKey.MediaStop },
			{ SlimDXKey.Minus, WindowsKey.OemMinus },
			{ SlimDXKey.Mute, WindowsKey.VolumeMute },
			{ SlimDXKey.MyComputer, WindowsKey.A },		// ?
			{ SlimDXKey.NextTrack, WindowsKey.MediaNextTrack },
			{ SlimDXKey.NoConvert, WindowsKey.IMENonconvert },
			{ SlimDXKey.NumberLock, WindowsKey.NumLock },
			{ SlimDXKey.NumberPad0, WindowsKey.NumPad0 },
			{ SlimDXKey.NumberPad1, WindowsKey.NumPad1 },
			{ SlimDXKey.NumberPad2, WindowsKey.NumPad2 },
			{ SlimDXKey.NumberPad3, WindowsKey.NumPad3 },
			{ SlimDXKey.NumberPad4, WindowsKey.NumPad4 },
			{ SlimDXKey.NumberPad5, WindowsKey.NumPad5 },
			{ SlimDXKey.NumberPad6, WindowsKey.NumPad6 },
			{ SlimDXKey.NumberPad7, WindowsKey.NumPad7 },
			{ SlimDXKey.NumberPad8, WindowsKey.NumPad8 },
			{ SlimDXKey.NumberPad9, WindowsKey.NumPad9 },
			{ SlimDXKey.NumberPadComma, WindowsKey.Separator },
			{ SlimDXKey.NumberPadEnter, WindowsKey.A },		// ?
			{ SlimDXKey.NumberPadEquals, WindowsKey.A },		// ?
			{ SlimDXKey.NumberPadMinus, WindowsKey.Subtract },
			{ SlimDXKey.NumberPadPeriod, WindowsKey.Decimal },
			{ SlimDXKey.NumberPadPlus, WindowsKey.Add },
			{ SlimDXKey.NumberPadSlash, WindowsKey.Divide },
			{ SlimDXKey.NumberPadStar, WindowsKey.Multiply },
			{ SlimDXKey.Oem102, WindowsKey.Oem102 },
			{ SlimDXKey.PageDown, WindowsKey.PageDown },
			{ SlimDXKey.PageUp, WindowsKey.PageUp },
			{ SlimDXKey.Pause, WindowsKey.Pause },
			{ SlimDXKey.Period, WindowsKey.OemPeriod },
			{ SlimDXKey.PlayPause, WindowsKey.MediaPlayPause },
			{ SlimDXKey.Power, WindowsKey.A },		// ?
			{ SlimDXKey.PreviousTrack, WindowsKey.MediaPreviousTrack },
			{ SlimDXKey.RightBracket, WindowsKey.Oem6 },
			{ SlimDXKey.RightControl, WindowsKey.RControlKey },
			{ SlimDXKey.Return, WindowsKey.Return },
			{ SlimDXKey.RightArrow, WindowsKey.Right },
			{ SlimDXKey.RightAlt, WindowsKey.RMenu },
			{ SlimDXKey.RightShift, WindowsKey.A },		// ?
			{ SlimDXKey.RightWindowsKey, WindowsKey.RWin },
			{ SlimDXKey.ScrollLock, WindowsKey.Scroll },
			{ SlimDXKey.Semicolon, WindowsKey.Oemplus    },	// OemSemicolon じゃなくて？
			{ SlimDXKey.Slash, WindowsKey.Oem2 },
			{ SlimDXKey.Sleep, WindowsKey.Sleep },
			{ SlimDXKey.Space, WindowsKey.Space },
			{ SlimDXKey.Stop, WindowsKey.MediaStop },
			{ SlimDXKey.PrintScreen, WindowsKey.PrintScreen },
			{ SlimDXKey.Tab, WindowsKey.Tab },
			{ SlimDXKey.Underline, WindowsKey.Oem102 },
			//{ SlimDXKey.Unlabeled, WindowsKey.A },		// ?
			{ SlimDXKey.UpArrow, WindowsKey.Up },
			{ SlimDXKey.VolumeDown, WindowsKey.VolumeDown },
			{ SlimDXKey.VolumeUp, WindowsKey.VolumeUp },
			{ SlimDXKey.Wake, WindowsKey.A },		// ?
			{ SlimDXKey.WebBack, WindowsKey.BrowserBack },
			{ SlimDXKey.WebFavorites, WindowsKey.BrowserFavorites },
			{ SlimDXKey.WebForward, WindowsKey.BrowserForward },
			{ SlimDXKey.WebHome, WindowsKey.BrowserHome },
			{ SlimDXKey.WebRefresh, WindowsKey.BrowserRefresh },
			{ SlimDXKey.WebSearch, WindowsKey.BrowserSearch },
			{ SlimDXKey.WebStop, WindowsKey.BrowserStop },
			{ SlimDXKey.Yen, WindowsKey.OemBackslash },
			#endregion
		};
	}
}
