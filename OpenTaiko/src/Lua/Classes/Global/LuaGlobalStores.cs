namespace OpenTaiko {
	public class LuaGlobalStores {
		public Dictionary<string, LuaSharedResource<LuaTexture>> SharedTextures = new();
		public Dictionary<string, LuaSharedResource<LuaSound>> SharedSounds = new();
		public Dictionary<string, string> SharedStrings = new();
		public Dictionary<string, Lua3DScene> SharedScenes = new();

		public LuaGlobalStores() {

		}
	}
}
