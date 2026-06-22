using FDK;

namespace OpenTaiko {
	public class LuaSize {
		public int Width;
		public int Height;

		public LuaSize() {
			Width = 0;
			Height = 0;
		}

		public LuaSize(int width, int height) {
			Width = width;
			Height = height;
		}
	}
	public class LuaSizeFunc {
		public LuaSizeFunc() { }

		public LuaSize CreateSize(int width, int height) {
			return new LuaSize(width, height);
		}
	}
}
