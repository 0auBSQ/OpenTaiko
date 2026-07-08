using System.Text;

namespace OpenTaiko {
	// ── HEIGHTMAP: raw heightfield loader for OWM3d terrain maps ─────────────────────────────────────
	// Terrain maps store elevation as raw little-endian uint16, row-major ("height.r16", W×H samples,
	// editor-authored). Raw was chosen over PNG so the standalone C++ editor (stb) and this loader stay
	// trivially compatible — no 16-bit PNG codec needed on either side.
	//
	// Lua:  local hf = HEIGHTMAP:Load("maps/harbor/height.r16", 256, 256)
	//       local y01 = hf:Sample(fx, fz)      -- bilinear, grid coords (0..W-1 / 0..H-1), returns 0..1
	//       map height = minY + y01 * (maxY - minY)   (min/max live in map.json)

	public sealed class LuaHeightfield {
		private ushort[] _d;
		private int _w, _h;

		internal LuaHeightfield(ushort[] d, int w, int h) { _d = d; _w = w; _h = h; }

		public int W() => _w;
		public int H() => _h;

		/// <summary>Deterministically drop the sample buffer (map unload) — a 401² field is ~320 KB of
		/// ushorts that would otherwise wait on two GC generations across the NLua boundary. At/Sample
		/// return 0 afterwards.</summary>
		public void Release() { _d = System.Array.Empty<ushort>(); _w = 0; _h = 0; }

		// normalized sample (0..1) at integer grid coords, clamped to the edges
		public double At(int ix, int iz) {
			if (_w <= 0 || _h <= 0) return 0;
			if (ix < 0) ix = 0; else if (ix >= _w) ix = _w - 1;
			if (iz < 0) iz = 0; else if (iz >= _h) iz = _h - 1;
			return _d[iz * _w + ix] / 65535.0;
		}

		// bilinear sample (0..1) at fractional grid coords
		public double Sample(double fx, double fz) {
			if (_w <= 0 || _h <= 0) return 0;
			if (fx < 0) fx = 0; else if (fx > _w - 1) fx = _w - 1;
			if (fz < 0) fz = 0; else if (fz > _h - 1) fz = _h - 1;
			int x0 = (int)fx, z0 = (int)fz;
			int x1 = x0 + 1 < _w ? x0 + 1 : x0;
			int z1 = z0 + 1 < _h ? z0 + 1 : z0;
			double tx = fx - x0, tz = fz - z0;
			double h00 = _d[z0 * _w + x0], h10 = _d[z0 * _w + x1];
			double h01 = _d[z1 * _w + x0], h11 = _d[z1 * _w + x1];
			double top = h00 + (h10 - h00) * tx;
			double bot = h01 + (h11 - h01) * tx;
			return (top + (bot - top) * tz) / 65535.0;
		}
	}

	public sealed class LuaHeightmapFunc {
		private readonly string _dir;

		public LuaHeightmapFunc(string dir) { _dir = dir; }

		/// <summary>Load a raw little-endian uint16 row-major heightfield (w×h samples). Returns an empty
		/// (flat zero) field on any error so a broken map renders flat instead of crashing the stage.</summary>
		public LuaHeightfield Load(string relPath, int w, int h) {
			if (w <= 0 || h <= 0) return new LuaHeightfield(System.Array.Empty<ushort>(), 0, 0);
			try {
				string path = System.IO.Path.Combine(_dir, relPath);
				byte[] raw = System.IO.File.ReadAllBytes(path);
				int need = w * h * 2;
				if (raw.Length < need) {
					System.Diagnostics.Trace.TraceWarning($"HEIGHTMAP: '{relPath}' is {raw.Length} bytes, expected {need} ({w}x{h} u16) — using flat field");
					return new LuaHeightfield(new ushort[w * h], w, h);
				}
				var d = new ushort[w * h];
				for (int i = 0; i < d.Length; i++)
					d[i] = (ushort)(raw[i * 2] | (raw[i * 2 + 1] << 8));   // little-endian
				return new LuaHeightfield(d, w, h);
			} catch (System.Exception e) {
				System.Diagnostics.Trace.TraceWarning($"HEIGHTMAP: failed to load '{relPath}': {e.Message} — using flat field");
				return new LuaHeightfield(new ushort[w * h], w, h);
			}
		}

		/// <summary>Bulk terrain emission — the WHOLE heightfield mesh + physics + grass in ONE call.
		/// The per-quad Lua loop made ~8 interop calls × 160k quads on big maps; every boxed argument
		/// became GC garbage (a ~900 MB spike on the volcano). This native loop replaces it. Texturing
		/// matches terrain.lua: splat-dominant layer when a splat is given, else slope-based rock.
		/// Grass (when grassDensity &gt; 0) is planted on layer-1 quads with stochastic rounding and a
		/// deterministic hash, so rebuilds are identical. Returns the blade count.</summary>
		public int BuildTerrain(Lua3DScene scene, int floorObj, PhysicsWorld phys,
			LuaHeightfield hf, LuaSplatmap splat, int w, int h, double minY, double maxY, double cellSize,
			int tex1, int tex2, int tex3, int tex4, double grassDensity, double grassClump) {
			int W = w - 1, H = h - 1;
			double range = maxY - minY;
			double cs = cellSize <= 0 ? 1 : cellSize;
			int[] tex = { tex1, tex2, tex3, tex4 };
			double sxk = 0, szk = 0;
			if (splat != null && splat.W() > 0) { sxk = (double)splat.W() / w; szk = (double)splat.H() / h; }
			// deterministic hash (parity with the Lua fallback's sin hash)
			static double H2(double a, double b) {
				double n = System.Math.Sin(a * 127.1 + b * 311.7) * 43758.5453;
				return n - System.Math.Floor(n);
			}
			// smooth value noise on the hash — drives grass CLUMPING (organic patches, not a uniform tile)
			static double VNoise(double x, double z) {
				double xf = System.Math.Floor(x), zf = System.Math.Floor(z);
				double fx = x - xf, fz = z - zf;
				fx = fx * fx * (3 - 2 * fx); fz = fz * fz * (3 - 2 * fz);
				double a = H2(xf, zf), b2 = H2(xf + 1, zf), c = H2(xf, zf + 1), d2 = H2(xf + 1, zf + 1);
				return a + (b2 - a) * fx + (c - a) * fz + (a - b2 - c + d2) * fx * fz;
			}
			double SampleY(int ix, int iz) => minY + hf.At(ix, iz) * range;
			int blades = 0;
			for (int iz = 0; iz < H; iz++) {
				for (int ix = 0; ix < W; ix++) {
					double x0 = ix * cs, z0 = iz * cs;
					double x1 = x0 + cs, z1 = z0 + cs;
					double h00 = SampleY(ix, iz);
					double h10 = SampleY(ix + 1, iz);
					double h11 = SampleY(ix + 1, iz + 1);
					double h01 = SampleY(ix, iz + 1);
					double slope = System.Math.Max(System.Math.Abs(h10 - h00),
						System.Math.Max(System.Math.Abs(h01 - h00), System.Math.Abs(h11 - h00))) / cs;
					int li = splat != null && sxk > 0
						? splat.At((int)(ix * sxk), (int)(iz * szk))
						: (slope > 0.9 ? 2 : 1);
					if (li < 1 || li > 4) li = 1;
					scene.ObjAddQuadTex(floorObj, x0, h00, z0, x0, h01, z1, x1, h11, z1, x1, h10, z0, tex[li - 1], 1, 1, 1.0);
					phys.AddQuad(x0, h00, z0, x0, h01, z1, x1, h11, z1, x1, h10, z0);
					// grass follows the GRASS WEIGHT smoothly (fades out along painted paths/pads
					// instead of cutting off at the dominant-layer square)
					double w1 = (splat != null && sxk > 0)
						? splat.Weight(1, (ix + 0.5) * sxk, (iz + 0.5) * szk)
						: (li == 1 ? 1.0 : 0.0);
					if (grassDensity > 0 && w1 > 0.15 && slope < 0.55 && !(ix == 0 || iz == 0 || ix == W - 1 || iz == H - 1)) {
						double n = grassDensity * (1 - slope) * (0.5 + H2(ix, iz)) * System.Math.Pow(w1, 1.5);
						if (grassClump > 0) {
							// clumped patches: smooth noise gates density to ~0 in the gaps between clumps,
							// so fields read as meadows with bare earth showing — not a uniform green tile
							double cn = VNoise(ix * 0.137 * grassClump, iz * 0.137 * grassClump);
							double cl = (cn - 0.35) / 0.40; if (cl < 0) cl = 0; else if (cl > 1) cl = 1;
							n *= cl * cl * (3 - 2 * cl) * 1.35 + 0.06;
						}
						int cnt = (int)n + (H2(ix * 3.7, iz * 9.1) < (n - System.Math.Floor(n)) ? 1 : 0);
						for (int b = 1; b <= cnt; b++) {
							double fx = H2(ix * 3 + b, iz * 7 + b);
							double fz = H2(ix * 7 - b, iz * 3 + b * 5);
							double gx = x0 + fx * cs, gz = z0 + fz * cs;
							// bilinear ground height inside this quad
							double top = h00 + (h10 - h00) * fx;
							double bot = h01 + (h11 - h01) * fx;
							double gy = top + (bot - top) * fz;
							scene.GrassAdd(gx, gy - 0.02, gz,
								0.28 + 0.3 * H2(gx * 13, gz * 17),
								H2(gx * 5, gz * 5) * 6.28318,
								0.7 + 0.3 * H2(gx, gz));
							blades++;
						}
					}
				}
			}
			// invisible border walls — the map edge is a hard boundary
			double bw = W * cs, bd = H * cs, lo = minY - 3, hi = maxY + 6;
			phys.AddQuad(0, lo, 0, bw, lo, 0, bw, hi, 0, 0, hi, 0);
			phys.AddQuad(0, lo, bd, bw, lo, bd, bw, hi, bd, 0, hi, bd);
			phys.AddQuad(0, lo, 0, 0, lo, bd, 0, hi, bd, 0, hi, 0);
			phys.AddQuad(bw, lo, 0, bw, lo, bd, bw, hi, bd, bw, hi, 0);
			return blades;
		}

		/// <summary>Load a splat image (RGBA PNG) as per-texel DOMINANT layer indices: R→1, G→2, B→3,
		/// A→4 (terrain.lua's layer order). Returns a flat all-layer-1 map on any error.</summary>
		public LuaSplatmap LoadSplat(string relPath) {
			try {
				string path = System.IO.Path.Combine(_dir, relPath);
				using var bmp = SkiaSharp.SKBitmap.Decode(path);
				if (bmp == null || bmp.Width <= 0 || bmp.Height <= 0) throw new System.Exception("decode failed");
				int w = bmp.Width, h = bmp.Height;
				var d = new byte[w * h];
				var ch = new byte[w * h * 4];
				for (int z = 0; z < h; z++) {
					for (int x = 0; x < w; x++) {
						var c = bmp.GetPixel(x, z);
						byte a = c.Alpha == 255 ? (byte)0 : c.Alpha;      // fully-opaque alpha = "no alpha layer"
						byte best = c.Red; byte li = 1;
						if (c.Green > best) { best = c.Green; li = 2; }
						if (c.Blue > best) { best = c.Blue; li = 3; }
						if (a > best) { li = 4; }
						d[z * w + x] = li;
						int o = (z * w + x) * 4;
						ch[o] = c.Red; ch[o + 1] = c.Green; ch[o + 2] = c.Blue; ch[o + 3] = a;
					}
				}
				return new LuaSplatmap(d, ch, w, h);
			} catch (System.Exception e) {
				System.Diagnostics.Trace.TraceWarning($"HEIGHTMAP: failed to load splat '{relPath}': {e.Message} — using layer 1");
				return new LuaSplatmap(System.Array.Empty<byte>(), System.Array.Empty<byte>(), 0, 0);
			}
		}
	}

	/// <summary>Splat lookup for terrain texturing: dominant layer (At, 1..4 — physics/CPU path)
	/// plus the raw 4-channel WEIGHTS (bilinear Weight + an ARGB image for the GPU splat shader,
	/// which blends the four layer textures smoothly instead of the per-quad dominant blocks).</summary>
	public sealed class LuaSplatmap {
		private byte[] _dom;      // dominant layer per texel (1..4)
		private byte[] _ch;       // raw channel weights, 4 bytes per texel (r,g,b,a = layers 1..4)
		private int _w, _h;
		internal LuaSplatmap(byte[] dom, byte[] channels, int w, int h) { _dom = dom; _ch = channels; _w = w; _h = h; }
		public int W() => _w;
		public int H() => _h;
		/// <summary>Deterministically drop the buffers (map unload). At returns 1 afterwards.</summary>
		public void Release() { _dom = System.Array.Empty<byte>(); _ch = System.Array.Empty<byte>(); _w = 0; _h = 0; }
		public int At(int ix, int iz) {
			if (_w <= 0 || _h <= 0) return 1;
			if (ix < 0) ix = 0; else if (ix >= _w) ix = _w - 1;
			if (iz < 0) iz = 0; else if (iz >= _h) iz = _h - 1;
			return _dom[iz * _w + ix];
		}
		/// <summary>Bilinear layer weight (0..1, normalized against the texel's channel sum) at
		/// fractional texel coords. layer 1..4.</summary>
		public double Weight(int layer, double fx, double fz) {
			if (_w <= 0 || _h <= 0 || _ch.Length == 0) return layer == 1 ? 1 : 0;
			if (layer < 1) layer = 1; else if (layer > 4) layer = 4;
			if (fx < 0) fx = 0; else if (fx > _w - 1) fx = _w - 1;
			if (fz < 0) fz = 0; else if (fz > _h - 1) fz = _h - 1;
			int x0 = (int)fx, z0 = (int)fz;
			int x1 = x0 + 1 < _w ? x0 + 1 : x0, z1 = z0 + 1 < _h ? z0 + 1 : z0;
			double tx = fx - x0, tz = fz - z0;
			double Wn(int x, int z) {
				int o = (z * _w + x) * 4;
				double sum = _ch[o] + _ch[o + 1] + _ch[o + 2] + _ch[o + 3];
				return sum <= 0 ? (layer == 1 ? 1 : 0) : _ch[o + layer - 1] / sum;
			}
			double top = Wn(x0, z0) + (Wn(x1, z0) - Wn(x0, z0)) * tx;
			double bot = Wn(x0, z1) + (Wn(x1, z1) - Wn(x0, z1)) * tx;
			return top + (bot - top) * tz;
		}
		/// <summary>Register the weights as an ARGB SPRITE (linear-filtered) so the GPU terrain
		/// shader can blend layers per pixel. R,G,B,A = layer 1..4 weight bytes.</summary>
		public void RegisterSprite(Lua3DScene scene, int spriteId) {
			if (scene == null || _w <= 0 || _ch.Length == 0) return;
			var px = new int[_w * _h];
			for (int i = 0; i < px.Length; i++) {
				int o = i * 4;
				px[i] = (_ch[o + 3] << 24) | (_ch[o] << 16) | (_ch[o + 1] << 8) | _ch[o + 2];
			}
			scene.RegisterSprite(spriteId, px, _w, _h);
			scene.SetSpriteFilter(spriteId, "linear");
		}
	}
}
