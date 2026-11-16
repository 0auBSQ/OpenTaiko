using System;

namespace OpenTaiko {
	// GPU rasterizer opt-in looks
	public partial class Lua3DScene {
		// six vec4: detail, maps, triplanar, skyamb, groundamb, cloud
		internal readonly float[] _terrP = {
			0.137f, 0.8f, 0f, 0.5f,
			1f, 1f, 0f, 0f,
			0f, 0f, 0f, 0f,
			1f, 1f, 1f, 0f,
			1f, 1f, 1f, 0f,
			0f, 60f, 1.5f, 0.6f,
		};

		/// <summary>Terrain program maps for a splat object; negative id = none, both = plain splat shading.
		/// Normal RGB = n * 0.5 + 0.5 (y up), tint 128 = unchanged; uv = world xz / mapSize.</summary>
		public void ObjSetTerrainMaps(int id, int normalTex, int tintTex, double mapSize) {
			var o = Obj(id); if (o == null) return;
			o.TerrNormalTex = normalTex < 0 ? -1 : normalTex;
			o.TerrTintTex = tintTex < 0 ? -1 : tintTex;
			o.TerrainMaps = o.TerrNormalTex >= 0 || o.TerrTintTex >= 0;
			o.TerrMapSize = (float)(mapSize <= 0 ? 1 : mapSize);
		}

		/// <summary>Terrain program setting, shared by all terrain objects; shares 0..1.
		/// detail: second scale, its variation, relief from brightness, border sharpness. maps: normal, tint, sky rim.
		/// triplanar: per layer, 1 = side projection. skyamb, groundamb: r, g, b for up / down faces.
		/// cloud: shadow strength, size, drift x, z per second.</summary>
		public void SetTerrainParam(string name, double x, double y, double z, double w) {
			int slot = name switch { "detail" => 0, "maps" => 1, "triplanar" => 2, "skyamb" => 3, "groundamb" => 4, "cloud" => 5, _ => -1 };
			if (slot < 0) return;
			int k = slot * 4;
			_terrP[k] = (float)x; _terrP[k + 1] = (float)y; _terrP[k + 2] = (float)z; _terrP[k + 3] = (float)w;
		}

		internal float _shadowSoft, _shadowFade;
		/// <summary>soft: radius in shadow texels (0 = four-tap); fade: fading share of the half extent (0 = hard).
		/// Either above 0 uses the EXT world program.</summary>
		public void SetShadowFilter(double soft, double fade) {
			_shadowSoft = (float)Math.Clamp(soft, 0, 4);
			_shadowFade = (float)Math.Clamp(fade, 0, 0.9);
		}

		internal float _cel;
		/// <summary>Sun ramp toward three steps, 0..1; 0 (default) = smooth. Above 0 uses the EXT world program.</summary>
		public void SetCelShade(double strength) { _cel = (float)Math.Clamp(strength, 0, 1); }

		internal bool _spriteLitUsed;
		/// <summary>Scene light share on the object's sprites, 0..1; 0 (default) = own colours.</summary>
		public void ObjSetSpriteLight(int id, double amount) {
			var o = Obj(id); if (o == null) return;
			o.SpriteLit = Math.Clamp(amount, 0, 1);
			if (o.SpriteLit > 0) _spriteLitUsed = true;
		}

		internal readonly float[] _fogSky = new float[24];
		/// <summary>Fog colour by view direction, slot 0..5; slot 0 w above 0 = on.</summary>
		public void SetFogSky(int slot, double x, double y, double z, double w) {
			if (slot < 0 || slot > 5) return;
			int k = slot * 4;
			_fogSky[k] = (float)x; _fogSky[k + 1] = (float)y; _fogSky[k + 2] = (float)z; _fogSky[k + 3] = (float)w;
		}

		/// <summary>Draws the object's sprites after the post passes: unlit, still depth-tested.</summary>
		public void ObjSetAfterPost(int id, bool on) { var o = Obj(id); if (o != null) o.AfterPost = on; }

		internal float _spriteNear;
		/// <summary>Sprites nearer than dist dissolve; 0 (default) = off. Above 0 uses the EXT world program.</summary>
		public void SetSpriteNearFade(double dist) { _spriteNear = (float)Math.Max(0, dist); }

		internal readonly float[] _skyCol = { 0.6f, 0.75f, 0.95f, 0.3f, 0.5f, 0.9f };
		/// <summary>Horizon and zenith colours, 0..1, for surface shaders (uSkyHor, uSkyZen).</summary>
		public void SetSkyColors(double hr, double hg, double hb, double zr, double zg, double zb) {
			_skyCol[0] = (float)hr; _skyCol[1] = (float)hg; _skyCol[2] = (float)hb;
			_skyCol[3] = (float)zr; _skyCol[4] = (float)zg; _skyCol[5] = (float)zb;
		}
	}
}
