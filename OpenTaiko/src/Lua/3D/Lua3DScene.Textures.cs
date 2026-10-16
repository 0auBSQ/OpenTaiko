using System;
using System.Collections.Generic;
using FDK;
using NLua;

namespace OpenTaiko {
	// Texture and sprite registries.
	public partial class Lua3DScene {
		// registered textures by id: pixels, size, GPU filter (CPU renderers ignore it), normal map and the revision
		// the GPU renderer compares to know when to re-upload its copy (e.g. a per-frame mirror RTT)
		internal readonly Dictionary<int, TexEntry> _tex = new();
		/// <summary>Bumped whenever any texture's pixels, filter or normal map change or a texture is unregistered.</summary>
		internal int _texRevision;
		// the entry of texture id, made on first use
		private TexEntry TexOf(int id) { if (!_tex.TryGetValue(id, out var e)) _tex[id] = e = new TexEntry(); return e; }
		/// <summary>The entry of texture <paramref name="id"/> when it has registered pixels.</summary>
		internal bool TryTex(int id, [System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)] out TexEntry e) => _tex.TryGetValue(id, out e) && e.Pix != null;
		private void SetTexPixels(int id, int[] px, int w, int h) { var e = TexOf(id); e.Pix = px; e.W = w; e.H = h; e.Rev++; _texRevision++; }

		/// <summary>Per-texture GPU sampling filter: "nearest" (default), "linear" or "mipmap"
		/// (trilinear; call once after registering the texture — mip chain is generated on upload).
		/// Photo textures (wood/plaster/brick) want mipmap; pixel-art stays nearest.</summary>
		public void SetTextureFilter(int id, string mode) {
			var e = TexOf(id);
			e.Filter = mode == "mipmap" ? 2 : (mode == "linear" ? 1 : 0);
			e.Rev++; _texRevision++;
		}

		/// <summary>Give texture <paramref name="id"/> a normal map: <paramref name="normalId"/> is another
		/// registered texture holding a tangent-space normal map in the DirectX convention (green points down
		/// the image, like the texture rows). On the GPU rasterizer every surface drawn with <paramref name="id"/>
		/// then bends its lighting along the map's relief; an unlit surface gets a gentle relief shade from a
		/// light fixed to the surface. The path tracers use it where the surface's material has no normal map; the
		/// CPU rasterizer ignores it. A negative normalId removes the map.</summary>
		public void SetTextureNormalMap(int id, int normalId) {
			if (normalId < 0) normalId = -1;
			int old = _tex.TryGetValue(id, out var e) ? e.Normal : -1;
			if (old == normalId) return;
			TexOf(id).Normal = normalId;
			_texRevision++;
			RtEdit();
		}

		public void RegisterTexture(int id, LuaTable pixels, int w, int h) {
			if (_disposedValue || pixels == null || w <= 0 || h <= 0) return;
			if ((long)w * h > 4_194_304) { System.Diagnostics.Trace.TraceWarning($"RegisterTexture {id}: {w}x{h} exceeds the 4M-pixel cap, ignored."); return; }
			var px = new int[w * h];
			for (int i = 0; i < px.Length; i++) {
				object o = pixels[i + 1];
				px[i] = o switch { long l => (int)l, double d => (int)d, int ii => ii, _ => 0 };
			}
			SetTexPixels(id, px, w, h); RtEdit();
		}

		/// <summary>Register texture <paramref name="id"/> as a recolour of the registered texture
		/// <paramref name="srcId"/>: every texel takes the colour (r, g, b 0..255) scaled by how bright it is
		/// against the source's average, so the grain stays and the average becomes the colour. Cut-out texels
		/// stay cut out. Does nothing when <paramref name="srcId"/> is not registered.</summary>
		public void RegisterTextureTinted(int id, int srcId, int r, int g, int b) {
			if (_disposedValue || !TryTex(srcId, out var se)) return;
			var src = se.Pix!;
			double sum = 0;
			for (int i = 0; i < src.Length; i++) {
				int p = src[i];
				sum += 0.299 * ((p >> 16) & 255) + 0.587 * ((p >> 8) & 255) + 0.114 * (p & 255);
			}
			double avg = Math.Max(1.0, sum / Math.Max(1, src.Length));
			var px = new int[src.Length];
			for (int i = 0; i < src.Length; i++) {
				int p = src[i];
				double k = (0.299 * ((p >> 16) & 255) + 0.587 * ((p >> 8) & 255) + 0.114 * (p & 255)) / avg;
				int nr = (int)Math.Clamp(r * k, 0, 255), ng = (int)Math.Clamp(g * k, 0, 255), nb = (int)Math.Clamp(b * k, 0, 255);
				px[i] = (p & unchecked((int)0x80000000)) | (nr << 16) | (ng << 8) | nb;
			}
			SetTexPixels(id, px, se.W, se.H); RtEdit();
		}

		/// <summary>
		/// Register a texture for the rasterizer from an existing <see cref="LuaTexture"/> by
		/// reading its pixels back from the GPU (alpha is dropped — the rasterizer treats textures
		/// as opaque). One-off cost; call at load time, not per frame.
		/// </summary>
		public void RegisterTextureFromImage(int id, LuaTexture tex) {
			if (_disposedValue || tex?._texture == null) return;
			byte[]? rgba = tex.GetCachedPixels(out int w, out int h);
			if (rgba == null || w <= 0 || h <= 0) return;
			var px = new int[w * h];
			for (int i = 0; i < px.Length; i++) {
				int o = i * 4;
				px[i] = (rgba[o] << 16) | (rgba[o + 1] << 8) | rgba[o + 2];
			}
			SetTexPixels(id, px, w, h); RtEdit();
		}

		/// <summary>Register a texture from a raw packed-RGB int[] (length w*h). For engine-internal
		/// use (e.g. the glTF model loader registering solid material-colour textures).</summary>
		internal void RegisterTexturePixels(int id, int[] px, int w, int h) {
			if (_disposedValue || px == null || w <= 0 || h <= 0 || px.Length < w * h) return;
			SetTexPixels(id, px, w, h); RtEdit();
		}

		// Model material textures use ids from a range kept for models (far above the ids stages pick):
		// one block per model file in each scene, so models never overwrite each other's or the stage's
		// textures, and a reloaded file takes its old block again.
		internal const int ModelTextureFirst = 1 << 24;
		private readonly Dictionary<string, (int first, int count)> _modelTexBlocks = new(StringComparer.OrdinalIgnoreCase);
		private int _nextModelTex = ModelTextureFirst;
		/// <summary>First id of the <paramref name="count"/> texture ids that model file
		/// <paramref name="key"/> registers its materials under in this scene.</summary>
		internal int ModelTextureBase(string key, int count) {
			count = Math.Max(1, count);
			if (_modelTexBlocks.TryGetValue(key, out var b) && b.count >= count) return b.first;
			int first = _nextModelTex; _nextModelTex += count;
			_modelTexBlocks[key] = (first, count);
			return first;
		}

		/// <summary>Forget a registered texture: frees the CPU pixel copy, its filter and normal map, the GPU
		/// upload and any reflection RTT target bound to this id. Sampling it afterwards draws nothing until it
		/// is re-registered.</summary>
		public void UnregisterTexture(int id) {
			if (_disposedValue) return;
			_tex.Remove(id); _texRevision++;
			_reflectTrue.Remove(id);
			if (_rttCanvases.TryGetValue(id, out var cv)) { cv.Dispose(); _rttCanvases.Remove(id); }
			_rttDepths.Remove(id); _rttBufs.Remove(id);
			foreach (var r in _made) r.DropTexture(id);
			RtEdit();
		}

		/// <summary>Forget a registered particle/billboard sprite (CPU pixels + GPU upload).</summary>
		public void UnregisterSprite(int id) {
			if (_disposedValue) return;
			_sprites.Remove(id);
			foreach (var r in _made) r.DropSprite(id);
			RtEdit();
		}

		// ── particle sprites (alpha-textured billboards) ─────────────────────────────────
		// A sprite is an ARGB image (a<<24 | r<<16 | g<<8 | b). Particles whose Sprite>=0 billboard the
		// sprite — its alpha (× the particle's opacity) shapes a soft circle / crystal / glow / any
		// Lua-supplied image, instead of a flat square. The particle's colour tints the sprite.
		// registered sprites by id: pixels, size, GPU filter (0 nearest, the default for pixel-art characters; 1 linear
		// for soft particles/foam/grass) and the revision, bumped whenever the pixels or filter change, at which the
		// GPU renderer re-uploads its copy
		internal readonly Dictionary<int, TexEntry> _sprites = new();
		private TexEntry SpriteOf(int id) { if (!_sprites.TryGetValue(id, out var e)) _sprites[id] = e = new TexEntry(); return e; }
		/// <summary>The entry of sprite <paramref name="id"/> when it has registered pixels.</summary>
		internal bool TrySprite(int id, [System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)] out TexEntry e) => _sprites.TryGetValue(id, out e) && e.Pix != null;
		/// <summary>Sampling filter for a particle/billboard sprite: "nearest" (default) or "linear"
		/// (soft edges — particles, foam, grass blades).</summary>
		public void SetSpriteFilter(int id, string mode) {
			var e = SpriteOf(id);
			e.Filter = mode == "linear" ? 1 : 0;
			e.Rev++;
		}

		/// <summary>Register a particle sprite from a flat ARGB array (length w*h, top-left origin). The scene
		/// keeps the array itself, so callers pass one they no longer write to.</summary>
		internal void RegisterSprite(int spriteId, int[] argb, int w, int h) {
			if (_disposedValue || argb == null || w <= 0 || h <= 0 || argb.Length < (long)w * h) return;
			var e = SpriteOf(spriteId); e.Pix = argb; e.W = w; e.H = h; e.Rev++; RtEdit();
		}

		/// <summary>Register a particle sprite from a Lua array of ARGB ints (1-based, length w*h).</summary>
		public void SetSpriteRGBA(int spriteId, LuaTable px, int w, int h) {
			if (_disposedValue || px == null || w <= 0 || h <= 0) return;
			var a = new int[w * h];
			for (int i = 0; i < a.Length; i++) { object v = px[i + 1]; a[i] = v is long l ? (int)l : (v is double d ? (int)d : 0); }
			RegisterSprite(spriteId, a, w, h);
		}

		/// <summary>Capture a LuaTexture's pixels as a particle sprite, so any drawn/loaded image can be
		/// used as a particle (main-thread; one-off at load).</summary>
		public void RegisterSpriteFromTexture(int spriteId, LuaTexture tex) {
			if (_disposedValue || tex == null) return;
			var rgba = tex.GetCachedPixels(out int w, out int h);
			if (rgba == null || w <= 0 || h <= 0) return;
			RegisterSprite(spriteId, ToSpriteArgb(rgba, w, h), w, h);
		}

		/// <summary>Like <see cref="RegisterSpriteFromTexture"/>, with the pixels recoloured through a gradient
		/// map (<c>GRADIENT:Create</c>) the way a 2D draw under that map shows them: each pixel's luminance picks
		/// the map's colour, mixed in by the map's blend strength. A nil map registers the plain pixels.</summary>
		public void RegisterSpriteFromTextureGradient(int spriteId, LuaTexture tex, LuaGradientMap? gradient) {
			if (_disposedValue || tex == null) return;
			var rgba = tex.GetCachedPixels(out int w, out int h);
			if (rgba == null || w <= 0 || h <= 0) return;
			RegisterSprite(spriteId, ToSpriteArgb(rgba, w, h, gradient?._gradientMap?.Lut, gradient?.BlendStrength ?? 0f), w, h);
		}

		/// <summary>Straight RGBA pixels as the ARGB ints the sprite registry keeps; with a gradient LUT
		/// (<see cref="CGradientMap.Lut"/>) a copy is recoloured first, so the cached source pixels stay intact.</summary>
		internal static int[] ToSpriteArgb(byte[] rgba, int w, int h, byte[]? lut = null, float blend = 0f) {
			byte[] src = rgba;
			if (lut != null && blend > 0f) {
				src = (byte[])rgba.Clone();
				CGradientMap.MapRgba(lut, blend, src);
			}
			var a = new int[w * h];
			for (int i = 0; i < a.Length; i++) { int o = i * 4; a[i] = (src[o + 3] << 24) | (src[o] << 16) | (src[o + 1] << 8) | src[o + 2]; }
			return a;
		}

		/// <summary>Generate a soft round particle: white, opaque at the centre fading to 0 at the edge.
		/// <paramref name="edge"/> shapes the falloff (1≈linear, 2-3 = soft glow). res ~32-64 is plenty.</summary>
		public void MakeSoftCircle(int spriteId, int res, double edge) {
			if (_disposedValue) return;
			if (res < 2) res = 2; if (edge <= 0) edge = 1;
			var a = new int[res * res]; double c = (res - 1) * 0.5;
			for (int y = 0; y < res; y++) for (int x = 0; x < res; x++) {
				double dx = (x - c) / c, dy = (y - c) / c; double d = Math.Sqrt(dx * dx + dy * dy);
				double f = d >= 1 ? 0 : Math.Pow(1 - d, edge);
				int al = (int)(f * 255 + 0.5); if (al < 0) al = 0; else if (al > 255) al = 255;
				a[y * res + x] = (al << 24) | 0xFFFFFF;
			}
			RegisterSprite(spriteId, a, res, res);
			SpriteOf(spriteId).Filter = 1;   // procedural soft art — always sample linear
		}

		/// <summary>Generate a six-armed ice-crystal/snowflake sprite (white-blue) for snow/frost.</summary>
		public void MakeCrystal(int spriteId, int res) {
			if (_disposedValue) return;
			if (res < 4) res = 4;
			var a = new int[res * res]; double c = (res - 1) * 0.5;
			for (int y = 0; y < res; y++) for (int x = 0; x < res; x++) {
				double dx = (x - c) / c, dy = (y - c) / c; double r = Math.Sqrt(dx * dx + dy * dy);
				double ang = Math.Atan2(dy, dx);
				double arm = Math.Pow(Math.Abs(Math.Cos(3 * ang)), 8);   // 6 sharp arms
				double core = Math.Exp(-(r * r) / 0.02);                 // small bright centre
				double val = Math.Max(core, arm * (r < 1 ? 1 - r : 0));
				int al = (int)(val * 255 + 0.5); if (al < 0) al = 0; else if (al > 255) al = 255;
				a[y * res + x] = (al << 24) | 0x00E6F0FF;                // tint white-blue (230,240,255)
			}
			RegisterSprite(spriteId, a, res, res);
			SpriteOf(spriteId).Filter = 1;   // procedural soft art — always sample linear
		}
	}
}
