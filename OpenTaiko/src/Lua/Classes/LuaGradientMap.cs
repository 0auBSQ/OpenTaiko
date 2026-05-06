using FDK;
using NLua;

namespace OpenTaiko {
	/// <summary>Lua wrapper around a cached <see cref="CGradientMap"/> texture.</summary>
	public class LuaGradientMap : IDisposable {
		internal CGradientMap? _gradientMap;
		internal HashSet<LuaGradientMap>? _disposeList;
		/// <summary>0 = no effect, 1 = full replacement, 0.5 = 50% blend with original colours.</summary>
		public float BlendStrength { get; internal set; } = 1.0f;

		internal LuaGradientMap(CGradientMap? gm, HashSet<LuaGradientMap>? list) {
			_gradientMap = gm;
			_disposeList = list;
		}

		private bool _disposed;
		public void Dispose() {
			if (!_disposed) {
				// The CGradientMap belongs to PaletteManager's global cache — do NOT dispose it.
				_gradientMap = null;
				_disposeList?.Remove(this);
				_disposed = true;
			}
		}
	}

	/// <summary>
	/// Exposed to Lua as the <c>GRADIENT</c> global.
	/// </summary>
	public class LuaGradientMapFunc {
		private readonly HashSet<LuaGradientMap> _gradients;

		public LuaGradientMapFunc(HashSet<LuaGradientMap> gradients) {
			_gradients = gradients;
		}

		/// <summary>
		/// Returns a gradient for the given stops and blend, reusing a cached GPU texture if
		/// the same stops+blend were used before anywhere in the program.
		/// Each stop: <c>{ position 0–1, r 0–255, g 0–255, b 0–255 [, a 0–255] }</c>.
		/// At least 2 stops are required.
		/// </summary>
		/// <param name="blend">0 = no effect, 1 = full replacement, 0.5 = 50% blend.</param>
		public LuaGradientMap Create(LuaTable stops, float blend = 1.0f) {
			var list = PaletteManager.ParseLuaStops(stops);
			if (list.Count < 2)
				throw new InvalidOperationException("GRADIENT:Create requires at least 2 colour stops.");
			string key   = PaletteManager.BuildCacheKey(list, blend);
			var   entry  = PaletteManager.GetOrCreate(key, list, blend);
			var   lua_gm = new LuaGradientMap(entry?.Map, _gradients) { BlendStrength = Math.Clamp(blend, 0f, 1f) };
			_gradients.Add(lua_gm);
			return lua_gm;
		}

		/// <summary>
		/// Applies <paramref name="gm"/> to every texture draw until <see cref="ClearActive"/> is called.
		/// </summary>
		public void SetActive(LuaGradientMap gm) {
			CTexture.ActiveGradientMapId    = gm._gradientMap?.TextureId ?? 0;
			CTexture.ActiveGradientMapBlend = gm.BlendStrength;
		}

		/// <summary>Removes the global active gradient map.</summary>
		public void ClearActive() {
			CTexture.ActiveGradientMapId    = 0;
			CTexture.ActiveGradientMapBlend = 1.0f;
		}
	}
}
