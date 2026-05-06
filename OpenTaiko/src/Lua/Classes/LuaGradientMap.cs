using FDK;
using NLua;

namespace OpenTaiko {
	/// <summary>Lua wrapper around a <see cref="CGradientMap"/> texture.</summary>
	public class LuaGradientMap : IDisposable {
		internal CGradientMap? _gradientMap;
		internal HashSet<LuaGradientMap>? _disposeList;
		/// <summary>0 = no effect, 1 = full replacement, 0.5 = 50% blend with original colours.</summary>
		public float BlendStrength { get; internal set; } = 1.0f;

		internal LuaGradientMap(CGradientMap gm, HashSet<LuaGradientMap> list) {
			_gradientMap = gm;
			_disposeList = list;
		}

		private bool _disposed;
		public void Dispose() {
			if (!_disposed) {
				_gradientMap?.Dispose();
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
		/// Creates a gradient map from a Lua table of colour stops.<br/>
		/// Each stop is an inner table <c>{ position, r, g, b [, a] }</c>:<br/>
		/// • <c>position</c> – float 0.0–1.0<br/>
		/// • <c>r, g, b, a</c> – integers 0–255 (alpha defaults to 255)<br/>
		/// At least 2 stops are required.
		/// </summary>
		/// <param name="blend">0 = no effect, 1 = full replacement, 0.5 = 50% blend with original colours.</param>
		public LuaGradientMap Create(LuaTable stops, float blend = 1.0f) {
			var list = new List<(float Pos, float R, float G, float B, float A)>();
			foreach (var key in stops.Keys) {
				if (stops[key] is not LuaTable inner) continue;
				float pos = Convert.ToSingle(inner[1]);
				float r   = Convert.ToSingle(inner[2]) / 255f;
				float g   = Convert.ToSingle(inner[3]) / 255f;
				float b   = Convert.ToSingle(inner[4]) / 255f;
				float a   = inner[5] != null ? Convert.ToSingle(inner[5]) / 255f : 1f;
				list.Add((pos, r, g, b, a));
			}
			if (list.Count < 2)
				throw new InvalidOperationException("GRADIENT:Create requires at least 2 colour stops.");

			var gm = new CGradientMap(list);
			var lua_gm = new LuaGradientMap(gm, _gradients) { BlendStrength = Math.Clamp(blend, 0f, 1f) };
			_gradients.Add(lua_gm);
			return lua_gm;
		}

		/// <summary>
		/// Applies <paramref name="gm"/> to every texture draw until <see cref="ClearActive"/> is called.
		/// Wrap a character draw between SetActive / ClearActive to recolour it.
		/// </summary>
		public void SetActive(LuaGradientMap gm) {
			CTexture.ActiveGradientMapId = gm._gradientMap?.TextureId ?? 0;
			CTexture.ActiveGradientMapBlend = gm.BlendStrength;
		}

		/// <summary>Removes the global active gradient map.</summary>
		public void ClearActive() {
			CTexture.ActiveGradientMapId = 0;
			CTexture.ActiveGradientMapBlend = 1.0f;
		}
	}
}
