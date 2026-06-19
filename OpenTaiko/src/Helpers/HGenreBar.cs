using FDK;


namespace OpenTaiko;

class HGenreBar {
	public static CTexture? tGetGenreBar(string value, Dictionary<string, CTexture> textures) {
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

	// Lazily-loaded background sets (per-genre / per-difficulty) load on demand and keep the
	// fallback-to-"0" semantics inside CLazyTextureMap.Get. May be null if neither the key nor the
	// "0" default is registered.
	public static CTexture? tGetGenreBar(string value, CLazyTextureMap textures) {
		return textures?.Get(value);
	}
}
