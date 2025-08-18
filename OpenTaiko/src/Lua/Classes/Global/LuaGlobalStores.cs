namespace OpenTaiko {
	public class LuaGlobalStores {
		public Dictionary<string, LuaSharedResource<LuaTexture>> SharedTextures = new();
		public Dictionary<string, LuaSharedResource<LuaSound>> SharedSounds = new();

		public LuaGlobalStores() {

		}
	}
}
