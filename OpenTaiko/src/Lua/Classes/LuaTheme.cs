namespace OpenTaiko {
	public class LuaThemeFunc {
		public LuaVector2 GetResolution() {
			return new LuaVector2(OpenTaiko.Skin.Resolution[0], OpenTaiko.Skin.Resolution[1]);
		}
	}
}
