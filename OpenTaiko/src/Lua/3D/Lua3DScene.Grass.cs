using System.Collections.Generic;

namespace OpenTaiko {
	// Grass fields (GPU rasterizer).
	public partial class Lua3DScene {
		// ── GRASS fields (GPU rasterizer) ────────────────────────────────────────────────────────
		// Static camera-facing blades with wind sway, batched into one VBO. CPU renderers skip them.
		internal readonly List<float> _grass = new();
		internal int _grassRev; internal int _grassSprite = -1;
		internal double _grassFadeStart = 28, _grassFadeEnd = 46;
		/// <summary>Remove every grass blade (map unload). Trims the list's capacity too — a big
		/// terrain's million-blade buffer (~30 MB) must not ride along into the next map.</summary>
		public void GrassClear() { _grass.Clear(); _grass.TrimExcess(); _grassRev++; }

		/// <summary>Remove the blades whose FOOT lies inside the XZ rectangle (water pools, roads,
		/// building pads). In-place filter; one call per exclusion zone at build time.</summary>
		public void GrassRemoveRect(double x0, double z0, double x1, double z1) {
			if (x1 < x0) (x0, x1) = (x1, x0);
			if (z1 < z0) (z0, z1) = (z1, z0);
			int w = 0;
			for (int r = 0; r < _grass.Count; r += 6) {
				float bx = _grass[r], bz = _grass[r + 2];
				if (bx >= x0 && bx <= x1 && bz >= z0 && bz <= z1) continue;   // culled
				if (w != r) for (int k = 0; k < 6; k++) _grass[w + k] = _grass[r + k];
				w += 6;
			}
			if (w == _grass.Count) return;
			_grass.RemoveRange(w, _grass.Count - w);
			_grassRev++;
		}
		/// <summary>The sprite (RegisterSprite/SetSpriteRGBA id) used as the blade texture (cutout).</summary>
		public void GrassSprite(int spriteId) { _grassSprite = spriteId; }
		/// <summary>Blades taller than this fade/shrink between the two camera distances (world units).</summary>
		public void GrassFade(double start, double end) { _grassFadeStart = start; _grassFadeEnd = end > start ? end : start + 1; }
		/// <summary>Plant one blade: foot at (x,y,z), world height h, sway phase (radians), shade 0..1.</summary>
		public void GrassAdd(double x, double y, double z, double h, double phase, double shade) {
			if (_disposedValue) return;
			_grass.Add((float)x); _grass.Add((float)y); _grass.Add((float)z);
			_grass.Add((float)h); _grass.Add((float)phase); _grass.Add((float)shade);
			_grassRev++;
		}
		/// <summary>Blade count (for HUD/debug).</summary>
		public int GrassCount() => _grass.Count / 6;
	}
}
