using FDK;


namespace OpenTaiko {
	class HGenreBar {
		public static CTexture tGetGenreBar(string value, Dictionary<string, CTexture> textures) {
			if (textures.TryGetValue($"{value}", out CTexture tex)) {
				return tex;
			} else {
				if (textures.TryGetValue("0", out CTexture tex2)) {
					return tex2;
				} else {
					return null;
				}
			}
		}
	}
}
