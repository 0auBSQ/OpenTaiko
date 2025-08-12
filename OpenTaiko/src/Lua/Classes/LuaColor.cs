using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Globalization;

namespace OpenTaiko {
	public class LuaColor {
		public byte R = 0xFF;
		public byte G = 0xFF;
		public byte B = 0xFF;
		public byte A = 0xFF;

		public LuaColor(byte r, byte g, byte b, byte a = 0xFF) { R = r; G = g; B = b; A = a; }
	}

	public class LuaColorFunc {
		public LuaColorFunc() { }

		public LuaColor CreateColorFromRGBA(byte r, byte g, byte b, byte a = 0xFF) {
			return new LuaColor(r, g, b, a);
		}

		public LuaColor CreateColorFromARGB(byte a, byte r, byte g, byte b) {
			return new LuaColor(r, g, b, a);
		}

		public LuaColor CreateColorFromHex(string value) {
			Color color = int.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int result) ? Color.FromArgb(result) : Color.White;
			return new LuaColor(color.R, color.G, color.B, color.A);
		}
	}
}
