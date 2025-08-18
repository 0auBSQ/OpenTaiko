using FDK;

namespace OpenTaiko {
	public class LuaVector2 {
		public double X;
		public double Y;

		public LuaVector2() {
			X = 0;
			Y = 0;
		}

		public LuaVector2(double x, double y) {
			X = x;
			Y = y;
		}
	}
	public class LuaVector2Func {
		public LuaVector2Func() { }

		public LuaVector2 CreateVector2(double x, double y) {
			return new LuaVector2(x, y);
		}
	}
}
