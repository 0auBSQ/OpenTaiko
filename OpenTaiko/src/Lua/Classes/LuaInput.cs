using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FDK;

namespace OpenTaiko {
	public class LuaInputFunc {
		// General inputs
		public bool Pressed(string input) {
			if (Enum.TryParse(typeof(EKeyConfigPad), input, true, out var pad)) {
				if ((EKeyConfigPad)pad >= EKeyConfigPad.Max) return false;
				return OpenTaiko.Pad.bPressed(EInstrumentPad.Drums, (EKeyConfigPad)pad);
			}
			return false;
		}
		public bool Pressing(string input) {
			if (Enum.TryParse(typeof(EKeyConfigPad), input, true, out var pad)) {
				if ((EKeyConfigPad)pad >= EKeyConfigPad.Max) return false;
				return OpenTaiko.Pad.IsPressing(EInstrumentPad.Drums, (EKeyConfigPad)pad);
			}
			return false;
		}
		public bool Released(string input) {
			if (Enum.TryParse(typeof(EKeyConfigPad), input, true, out var pad)) {
				if ((EKeyConfigPad)pad >= EKeyConfigPad.Max) return false;
				return OpenTaiko.Pad.IsReleased(EInstrumentPad.Drums, (EKeyConfigPad)pad);
			}
			return false;
		}
		public bool Releasing(string input) {
			if (Enum.TryParse(typeof(EKeyConfigPad), input, true, out var pad)) {
				if ((EKeyConfigPad)pad >= EKeyConfigPad.Max) return false;
				return OpenTaiko.Pad.IsReleasing(EInstrumentPad.Drums, (EKeyConfigPad)pad);
			}
			return false;
		}
		// Keyboard inputs
		public bool KeyboardPressed(string key) {
			if (Enum.TryParse(typeof(SlimDXKeys.Key), key, true, out var result)) {
				return OpenTaiko.InputManager.Keyboard.KeyPressed((int)result);
			}
			return false;
		}
		public bool KeyboardPressing(string key) {
			if (Enum.TryParse(typeof(SlimDXKeys.Key), key, true, out var result)) {
				return OpenTaiko.InputManager.Keyboard.KeyPressing((int)result);
			}
			return false;
		}
		public bool KeyboardReleasing(string key) {
			if (Enum.TryParse(typeof(SlimDXKeys.Key), key, true, out var result)) {
				return OpenTaiko.InputManager.Keyboard.KeyReleasing((int)result);
			}
			return false;
		}
		public bool KeyboardReleased(string key) {
			if (Enum.TryParse(typeof(SlimDXKeys.Key), key, true, out var result)) {
				return OpenTaiko.InputManager.Keyboard.KeyReleased((int)result);
			}
			return false;
		}
		// Mouse inputs

		// Due to the way the surface scales in non-native resolutions,
		// the position of the mouse will only match the window, and not the surface.
		// For now, we'll not be doing any mouse-specific methods.

		//public (int x, int y) GetMousePos() {
		//	return ((CInputMouse)OpenTaiko.InputManager.Mouse).Position;
		//}
	}
}
