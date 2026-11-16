using System;

namespace OpenTaiko {
	// grass tile: tuft cards around a focus on a heightfield
	public partial class Lua3DScene {
		internal bool _gtOn;
		internal int _gtBufRev, _gtHeightRev;
		internal float[]? _gtHeights; internal int _gtHW, _gtHH; internal float _gtHCell = 1;
		internal int _gtSplat = -1, _gtGround = -1, _gtTint = -1, _gtMask = -1, _gtAtlas = -1;
		internal float _gtSplatW = 1, _gtSplatH = 1, _gtTexScale = 1, _gtMapSize = 1;
		internal double _gtFx, _gtFz;
		internal int _gtDrawn, _gtCount;
		// ten vec4: density, range, size, wind, wind2, kind, atlas, tip, dry, ground
		internal readonly float[] _gtP = {
			6f, 1f, 0.15f, 11f,
			30f, 75f, 0.35f, 0.8f,
			1.05f, 1.45f, 0.65f, 1.0f,
			0.8f, 0.6f, 0.30f, 24f,
			5f, 0.02f, 0.5f, 0f,
			0.06f, 26f, 40f, 0.5f,
			1f, 1f, 1f, 0f,
			1.0f, 1.0f, 0.95f, 0.85f,
			1.12f, 1.04f, 0.78f, 0.5f,
			0.45f, 0.7f, 0.137f, 0.8f,
		};
		internal readonly float[] _gtCells = new float[64];
		internal readonly float[] _gtAtlasAvg = { 0.34f, 0.5f, 0.22f };

		/// <summary>Grass tile ground: heightfield plus BuildTerrain's min/max y and cell size; nil drops it.</summary>
		public void GrassTileHeights(LuaHeightfield hf, double minY, double maxY, double cellSize) {
			if (_disposedValue) return;
			if (hf == null) { _gtHeights = null; _gtHeightRev++; return; }
			var d = hf.Data; int w = hf.W(), h = hf.H();
			if (w <= 1 || h <= 1 || d.Length < w * h) { _gtHeights = null; _gtHeightRev++; return; }
			var y = new float[w * h];
			double k = (maxY - minY) / 65535.0;
			for (int i = 0; i < y.Length; i++) y[i] = (float)(minY + d[i] * k);
			_gtHeights = y; _gtHW = w; _gtHH = h; _gtHCell = (float)(cellSize <= 0 ? 1 : cellSize);
			_gtHeightRev++;
		}

		/// <summary>Grass tile textures; negative id = none. Sizes in world units, uvScale in repeats per unit.
		/// Grass follows splat layer 1; mask R density, G flower share, B height (128 = unchanged).</summary>
		public void GrassTileMaps(int splatSprite, double splatW, double splatH, int groundTex, double uvScale,
			int tintTex, int maskTex, double mapSize, int atlasSprite) {
			_gtSplat = splatSprite; _gtSplatW = (float)(splatW <= 0 ? 1 : splatW); _gtSplatH = (float)(splatH <= 0 ? 1 : splatH);
			_gtGround = groundTex; _gtTexScale = (float)(uvScale <= 0 ? 1 : uvScale);
			_gtTint = tintTex; _gtMask = maskTex; _gtMapSize = (float)(mapSize <= 0 ? 1 : mapSize);
			_gtAtlas = atlasSprite;
		}

		/// <summary>Grass tile setting; sizes in world units, shares 0..1.
		/// density: cards per unit^2, quality, patch keep share, patch size. range: thin start, end, tilt, contrast.
		/// size: min, max width factor, min, max height factor. wind: dir x, z, strength, gust size.
		/// wind2: gust speed per second, flutter, alpha cut. kind: flower share, flower patch, dry patch, dry amount.
		/// atlas: columns, rows, grass cells, flower cells (grass first). tip: r, g, b mul, root shade.
		/// dry: r, g, b mul, own-colour share. ground: min layer 1 weight, max slope, second scale, strength.
		/// avg: r, g, b of the atlas grass cells.</summary>
		public void GrassTileParam(string name, double x, double y, double z, double w) {
			if (name == "avg") { _gtAtlasAvg[0] = (float)x; _gtAtlasAvg[1] = (float)y; _gtAtlasAvg[2] = (float)z; return; }
			int slot = name switch {
				"density" => 0, "range" => 1, "size" => 2, "wind" => 3, "wind2" => 4, "kind" => 5, "atlas" => 6, "tip" => 7, "dry" => 8, "ground" => 9, _ => -1
			};
			if (slot < 0) return;
			int k = slot * 4;
			float fx = (float)x, fy = (float)y, fz = (float)z, fw = (float)w;
			if ((slot == 0 && (_gtP[k] != fx || _gtP[k + 1] != fy)) || (slot == 1 && _gtP[k + 1] != fy)) _gtBufRev++;
			_gtP[k] = fx; _gtP[k + 1] = fy; _gtP[k + 2] = fz; _gtP[k + 3] = fw;
		}

		/// <summary>Atlas cell card size, world units.</summary>
		public void GrassTileCell(int index, double width, double height) {
			if (index < 0 || index > 15) return;
			_gtCells[index * 4] = (float)width; _gtCells[index * 4 + 1] = (float)height;
		}

		public void GrassTileFocus(double x, double z) { _gtFx = x; _gtFz = z; }

		public void GrassTileEnable(bool on) { _gtOn = on; }

		internal int _gtOwner;
		/// <summary>A hidden owner object hides the tile; 0 = none.</summary>
		public void GrassTileOwner(int objId) { _gtOwner = objId; }

		/// <summary>Cards in the buffer, cards drawn last frame.</summary>
		public (int, int) GrassTileStats() => (_gtCount, _gtDrawn);
	}
}
