using System;
using System.Collections.Generic;
using System.Text;
using SlimDXKeys;

using SlimDXKey = SlimDXKeys.Key;

namespace FDK
{
	public static class DeviceConstantConverter
	{
		// メソッド

		public static Key DIKtoKey( Silk.NET.Input.Key key )
		{
			return _DIKtoKey[key];
		}


		/// <summary>
		///		DIK (SharpDX.DirectInput.Key) から SlimDX.DirectInput.Key への変換表。
		/// </summary>
		private static Dictionary<Silk.NET.Input.Key, SlimDXKey> _DIKtoKey = new Dictionary<Silk.NET.Input.Key, SlimDXKey>() {
			{ Silk.NET.Input.Key.Unknown, SlimDXKey.Unknown },
			{ Silk.NET.Input.Key.Escape, SlimDXKey.Escape },
			{ Silk.NET.Input.Key.Number1, SlimDXKey.D1 },
			{ Silk.NET.Input.Key.Number2, SlimDXKey.D2 },
			{ Silk.NET.Input.Key.Number3, SlimDXKey.D3 },
			{ Silk.NET.Input.Key.Number4, SlimDXKey.D4 },
			{ Silk.NET.Input.Key.Number5, SlimDXKey.D5 },
			{ Silk.NET.Input.Key.Number6, SlimDXKey.D6 },
			{ Silk.NET.Input.Key.Number7, SlimDXKey.D7 },
			{ Silk.NET.Input.Key.Number8, SlimDXKey.D8 },
			{ Silk.NET.Input.Key.Number9, SlimDXKey.D9 },
			{ Silk.NET.Input.Key.Number0, SlimDXKey.D0 },
			{ Silk.NET.Input.Key.Minus, SlimDXKey.Minus },
			{ Silk.NET.Input.Key.Equal, SlimDXKey.Equals },
			{ Silk.NET.Input.Key.Backspace, SlimDXKey.Backspace },
			{ Silk.NET.Input.Key.Tab, SlimDXKey.Tab },
			{ Silk.NET.Input.Key.Q, SlimDXKey.Q },
			{ Silk.NET.Input.Key.W, SlimDXKey.W },
			{ Silk.NET.Input.Key.E, SlimDXKey.E },
			{ Silk.NET.Input.Key.R, SlimDXKey.R },
			{ Silk.NET.Input.Key.T, SlimDXKey.T },
			{ Silk.NET.Input.Key.Y, SlimDXKey.Y },
			{ Silk.NET.Input.Key.U, SlimDXKey.U },
			{ Silk.NET.Input.Key.I, SlimDXKey.I },
			{ Silk.NET.Input.Key.O, SlimDXKey.O },
			{ Silk.NET.Input.Key.P, SlimDXKey.P },
			{ Silk.NET.Input.Key.LeftBracket, SlimDXKey.LeftBracket },
			{ Silk.NET.Input.Key.RightBracket, SlimDXKey.RightBracket },
			{ Silk.NET.Input.Key.Enter, SlimDXKey.Return },
			{ Silk.NET.Input.Key.ControlLeft, SlimDXKey.LeftControl },
			{ Silk.NET.Input.Key.A, SlimDXKey.A },
			{ Silk.NET.Input.Key.S, SlimDXKey.S },
			{ Silk.NET.Input.Key.D, SlimDXKey.D },
			{ Silk.NET.Input.Key.F, SlimDXKey.F },
			{ Silk.NET.Input.Key.G, SlimDXKey.G },
			{ Silk.NET.Input.Key.H, SlimDXKey.H },
			{ Silk.NET.Input.Key.J, SlimDXKey.J },
			{ Silk.NET.Input.Key.K, SlimDXKey.K },
			{ Silk.NET.Input.Key.L, SlimDXKey.L },
			{ Silk.NET.Input.Key.Semicolon, SlimDXKey.Semicolon },
			{ Silk.NET.Input.Key.Apostrophe, SlimDXKey.Apostrophe },
			{ Silk.NET.Input.Key.GraveAccent, SlimDXKey.Grave },
			{ Silk.NET.Input.Key.ShiftLeft, SlimDXKey.LeftShift },
			{ Silk.NET.Input.Key.BackSlash, SlimDXKey.Backslash },
			{ Silk.NET.Input.Key.Z, SlimDXKey.Z },
			{ Silk.NET.Input.Key.X, SlimDXKey.X },
			{ Silk.NET.Input.Key.C, SlimDXKey.C },
			{ Silk.NET.Input.Key.V, SlimDXKey.V },
			{ Silk.NET.Input.Key.B, SlimDXKey.B },
			{ Silk.NET.Input.Key.N, SlimDXKey.N },
			{ Silk.NET.Input.Key.M, SlimDXKey.M },
			{ Silk.NET.Input.Key.Comma, SlimDXKey.Comma },
			{ Silk.NET.Input.Key.Period, SlimDXKey.Period },
			{ Silk.NET.Input.Key.Slash, SlimDXKey.Slash },
			{ Silk.NET.Input.Key.ShiftRight, SlimDXKey.RightShift },
			{ Silk.NET.Input.Key.KeypadMultiply, SlimDXKey.NumberPadStar },
			{ Silk.NET.Input.Key.AltLeft, SlimDXKey.LeftAlt },
			{ Silk.NET.Input.Key.Space, SlimDXKey.Space },
			{ Silk.NET.Input.Key.CapsLock, SlimDXKey.CapsLock },
			{ Silk.NET.Input.Key.F1, SlimDXKey.F1 },
			{ Silk.NET.Input.Key.F2, SlimDXKey.F2 },
			{ Silk.NET.Input.Key.F3, SlimDXKey.F3 },
			{ Silk.NET.Input.Key.F4, SlimDXKey.F4 },
			{ Silk.NET.Input.Key.F5, SlimDXKey.F5 },
			{ Silk.NET.Input.Key.F6, SlimDXKey.F6 },
			{ Silk.NET.Input.Key.F7, SlimDXKey.F7 },
			{ Silk.NET.Input.Key.F8, SlimDXKey.F8 },
			{ Silk.NET.Input.Key.F9, SlimDXKey.F9 },
			{ Silk.NET.Input.Key.F10, SlimDXKey.F10 },
			{ Silk.NET.Input.Key.NumLock, SlimDXKey.NumberLock },
			{ Silk.NET.Input.Key.ScrollLock, SlimDXKey.ScrollLock },
			{ Silk.NET.Input.Key.Keypad7, SlimDXKey.NumberPad7 },
			{ Silk.NET.Input.Key.Keypad8, SlimDXKey.NumberPad8 },
			{ Silk.NET.Input.Key.Keypad9, SlimDXKey.NumberPad9 },
			{ Silk.NET.Input.Key.KeypadSubtract, SlimDXKey.NumberPadMinus },
			{ Silk.NET.Input.Key.Keypad4, SlimDXKey.NumberPad4 },
			{ Silk.NET.Input.Key.Keypad5, SlimDXKey.NumberPad5 },
			{ Silk.NET.Input.Key.Keypad6, SlimDXKey.NumberPad6 },
			{ Silk.NET.Input.Key.KeypadAdd, SlimDXKey.NumberPadPlus },
			{ Silk.NET.Input.Key.Keypad1, SlimDXKey.NumberPad1 },
			{ Silk.NET.Input.Key.Keypad2, SlimDXKey.NumberPad2 },
			{ Silk.NET.Input.Key.Keypad3, SlimDXKey.NumberPad3 },
			{ Silk.NET.Input.Key.Keypad0, SlimDXKey.NumberPad0 },
			{ Silk.NET.Input.Key.KeypadDecimal, SlimDXKey.NumberPadPeriod },
			{ Silk.NET.Input.Key.F11, SlimDXKey.F11 },
			{ Silk.NET.Input.Key.F12, SlimDXKey.F12 },
			{ Silk.NET.Input.Key.F13, SlimDXKey.F13 },
			{ Silk.NET.Input.Key.F14, SlimDXKey.F14 },
			{ Silk.NET.Input.Key.F15, SlimDXKey.F15 },
			/*
			{ Silk.NET.Input.Key.Unknown, SlimDXKey.Kana },
			{ Silk.NET.Input.Key.Unknown, SlimDXKey.AbntC1 },
			{ Silk.NET.Input.Key.Unknown, SlimDXKey.Convert },
			{ Silk.NET.Input.Key.Unknown, SlimDXKey.NoConvert },
			{ Silk.NET.Input.Key.Unknown, SlimDXKey.Yen },
			{ Silk.NET.Input.Key.Unknown, SlimDXKey.AbntC2 },
			*/
			{ Silk.NET.Input.Key.KeypadEqual, SlimDXKey.NumberPadEquals },
			/*
			{ Silk.NET.Input.Key.Unknown, SlimDXKey.PreviousTrack },
			{ Silk.NET.Input.Key.Unknown, SlimDXKey.AT },
			*/
			//{ Silk.NET.Input.Key.Semicolon, SlimDXKey.Colon },
			/*
			{ Silk.NET.Input.Key.Unknown, SlimDXKey.Underline },
			{ Silk.NET.Input.Key.Unknown, SlimDXKey.Kanji },
			{ Silk.NET.Input.Key.Unknown, SlimDXKey.Stop },
			{ Silk.NET.Input.Key.Unknown, SlimDXKey.AX },
			{ Silk.NET.Input.Key.Unknown, SlimDXKey.Unlabeled },
			{ Silk.NET.Input.Key.Unknown, SlimDXKey.NextTrack },
			*/
			{ Silk.NET.Input.Key.KeypadEnter, SlimDXKey.NumberPadEnter },
			{ Silk.NET.Input.Key.ControlRight, SlimDXKey.RightControl },
			/*
			{ Silk.NET.Input.Key.Unknown, SlimDXKey.Mute },
			{ Silk.NET.Input.Key.Unknown, SlimDXKey.Calculator },
			*/
			/*
			{ Silk.NET.Input.Key.Unknown, SlimDXKey.MediaStop },
			{ Silk.NET.Input.Key.Unknown, SlimDXKey.VolumeDown },
			{ Silk.NET.Input.Key.Unknown, SlimDXKey.VolumeUp },
			{ Silk.NET.Input.Key.Unknown, SlimDXKey.WebHome },
			*/
			{ Silk.NET.Input.Key.PrintScreen, SlimDXKey.PrintScreen },
			{ Silk.NET.Input.Key.AltRight, SlimDXKey.RightAlt },
			{ Silk.NET.Input.Key.Pause, SlimDXKey.Pause },
			{ Silk.NET.Input.Key.Home, SlimDXKey.Home },
			{ Silk.NET.Input.Key.Up, SlimDXKey.UpArrow },
			{ Silk.NET.Input.Key.PageUp, SlimDXKey.PageUp },
			{ Silk.NET.Input.Key.Left, SlimDXKey.LeftArrow },
			{ Silk.NET.Input.Key.Right, SlimDXKey.RightArrow },
			{ Silk.NET.Input.Key.End, SlimDXKey.End },
			{ Silk.NET.Input.Key.Down, SlimDXKey.DownArrow },
			{ Silk.NET.Input.Key.PageDown, SlimDXKey.PageDown },
			{ Silk.NET.Input.Key.Insert, SlimDXKey.Insert },
			{ Silk.NET.Input.Key.Delete, SlimDXKey.Delete },
			{ Silk.NET.Input.Key.SuperLeft, SlimDXKey.LeftWindowsKey },
			{ Silk.NET.Input.Key.SuperRight, SlimDXKey.RightWindowsKey },
			{ Silk.NET.Input.Key.Menu, SlimDXKey.Applications },
			/*
			{ Silk.NET.Input.Key.Unknown, SlimDXKey.Power },
			{ Silk.NET.Input.Key.Unknown, SlimDXKey.Sleep },
			{ Silk.NET.Input.Key.Unknown, SlimDXKey.Wake },
			{ Silk.NET.Input.Key.Unknown, SlimDXKey.WebSearch },
			{ Silk.NET.Input.Key.Unknown, SlimDXKey.WebFavorites },
			{ Silk.NET.Input.Key.Unknown, SlimDXKey.WebRefresh },
			{ Silk.NET.Input.Key.Unknown, SlimDXKey.WebStop },
			{ Silk.NET.Input.Key.Unknown, SlimDXKey.WebForward },
			{ Silk.NET.Input.Key.Unknown, SlimDXKey.WebBack },
			{ Silk.NET.Input.Key.Unknown, SlimDXKey.MyComputer },
			{ Silk.NET.Input.Key.Unknown, SlimDXKey.Mail },
			{ Silk.NET.Input.Key.Unknown, SlimDXKey.MediaSelect },
			*/
		};
	}
}
