using System;
using System.Collections.Generic;
using Silk.NET.OpenGLES;

namespace OpenTaiko {
	// GL copies of the scene's textures and sprites.
	internal sealed partial class GpuRasterizer {
		// one GL texture per scene texture/sprite id, kept for the id's life: a changed revision re-uploads into the
		// same handle, so batches hold ids and resolve the handle at draw time
		private struct GlTex { public uint Tex; public int Rev, W, H; public bool Opaque; }   // Opaque: no cut-out texel
		private readonly Dictionary<int, GlTex> _glTex = new();
		private readonly Dictionary<int, GlTex> _glSprite = new();
		private static byte[]? _uploadBuf;   // RGBA staging for uploads, shared by every renderer (all GL work runs on one thread)
		private bool _uploadOpaque;          // cleared by an upload with a cut-out texel (TexFor sets it before uploading)

		// upload into `tex` (a new texture when 0); an upload at the texture's current size replaces the pixels in place
		private unsafe uint UploadTexture(uint tex, bool sameSize, int[] px, int w, int h, bool sprite, int filter = 0) {
			int n = w * h;
			var b = _uploadBuf;
			if (b == null || b.Length < n * 4) _uploadBuf = b = new byte[n * 4];
			for (int i = 0; i < n; i++) {
				int p = px[i];
				b[i * 4] = (byte)(p >> 16); b[i * 4 + 1] = (byte)(p >> 8); b[i * 4 + 2] = (byte)p;
				b[i * 4 + 3] = sprite ? (byte)(p >> 24) : (p < 0 ? (byte)0 : (byte)255);
				if (p < 0) _uploadOpaque = false;
			}
			if (tex == 0) { tex = Gl.GenTexture(); sameSize = false; }
			Gl.BindTexture(TextureTarget.Texture2D, tex);
			fixed (byte* dp = b) {
				if (sameSize) Gl.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, (uint)w, (uint)h, PixelFormat.Rgba, PixelType.UnsignedByte, dp);
				else Gl.TexImage2D(TextureTarget.Texture2D, 0, (int)InternalFormat.Rgba8, (uint)w, (uint)h, 0, PixelFormat.Rgba, PixelType.UnsignedByte, dp);
			}
			// sprites are whole images (billboards, grass blades, splat weights): clamp so a linear edge never
			// samples the opposite border; textures tile
			int wrap = sprite ? (int)GLEnum.ClampToEdge : (int)GLEnum.Repeat;
			Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, wrap);
			Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, wrap);
			// SetTextureFilter opt-in: 1 linear, 2 trilinear mipmap (photo textures); default stays nearest (pixel art)
			if (filter == 2) {
				Gl.GenerateMipmap(TextureTarget.Texture2D);
				Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.LinearMipmapLinear);
				Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
			} else if (filter == 1) {
				Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
				Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
			} else {
				Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Nearest);
				Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Nearest);
			}
			return tex;
		}
		// the GL texture for a scene texture id, (re)uploaded when its revision changed (new pixels, a live mirror
		// capture, a filter change); 0 while the id has no pixels. An upload binds on the active texture unit.
		private uint TexFor(Lua3DScene s, int id) {
			if (_rttColor.TryGetValue(id, out var rttTex)) return rttTex;   // live GPU reflection/portal target (RenderToTexture)
			s._tex.TryGetValue(id, out var te);
			int rev = te?.Rev ?? 0;
			bool had = _glTex.TryGetValue(id, out var e);
			if (had && e.Rev == rev) return e.Tex;
			if (te?.Pix == null) return 0;
			int w = te.W, h = te.H;
			_uploadOpaque = true;
			e.Tex = UploadTexture(had ? e.Tex : 0, had && e.W == w && e.H == h, te.Pix, w, h, false, te.Filter);
			e.Rev = rev; e.W = w; e.H = h; e.Opaque = _uploadOpaque; _glTex[id] = e;
			return e.Tex;
		}
		// whether a draw with this texture id shows anything (registered pixels or a live GPU target)
		private bool HasTex(Lua3DScene s, int id) => _rttColor.ContainsKey(id) || s.TryTex(id, out _);
		// whether texture id shows and has no cut-out texel, so an alpha test with it discards nothing (uploads it when needed)
		private bool OpaqueTex(Lua3DScene s, int id) {
			if (_rttColor.ContainsKey(id) || TexFor(s, id) == 0) return false;
			return _glTex.TryGetValue(id, out var e) && e.Opaque;
		}

		// ── solid-colour palette ──────────────────────────────────────────────────────────────
		// A texture whose texels are all one opaque colour (a model material's colour) samples to that colour at any
		// uv. Its primitives take the uv of that colour's texel in one shared palette texture instead, so an object's
		// runs of such materials draw as one batch, and its shadow as one draw.
		/// <summary>The colours of the palette texture: one texel per colour, never moved once given.</summary>
		internal sealed class ColorPalette {
			public const int Side = 64;
			private readonly Dictionary<int, int> _slot = new();
			public readonly byte[] Rgba = new byte[Side * Side * 4];
			public bool Dirty;
			public int Count => _slot.Count;
			/// <summary>The uv of the centre of colour <paramref name="rgb"/>'s texel, given one when new; false when the
			/// palette is full.</summary>
			public bool UvOf(int rgb, out float u, out float v) {
				if (!_slot.TryGetValue(rgb, out int i)) {
					u = v = 0;
					if (_slot.Count >= Side * Side) return false;
					i = _slot.Count; _slot[rgb] = i;
					Rgba[i * 4] = (byte)(rgb >> 16); Rgba[i * 4 + 1] = (byte)(rgb >> 8); Rgba[i * 4 + 2] = (byte)rgb; Rgba[i * 4 + 3] = 255;
					Dirty = true;
				}
				u = (i % Side + 0.5f) / Side; v = (i / Side + 0.5f) / Side;
				return true;
			}
			public void Clear() { _slot.Clear(); Array.Clear(Rgba); Dirty = true; }
			/// <summary>The colour (0xRRGGBB) of a texture whose <paramref name="n"/> texels are one opaque colour, else -1.</summary>
			public static int SolidColour(int[] px, int n) {
				if (n <= 0 || px.Length < n) return -1;
				int p0 = px[0];
				if (p0 < 0) return -1;
				for (int i = 1; i < n; i++) if (px[i] != p0) return -1;
				return p0 & 0xFFFFFF;
			}
		}
		private readonly ColorPalette _palette = new();
		private uint _palTex;
		private readonly Dictionary<int, (int rev, int rgb)> _solid = new();   // texture id -> the revision checked and its solid colour (-1: none)
		private int _rttRev;   // bumped whenever the set of live GPU targets (_rttColor) changes

		// the palette uv of texture id when it is solid: registered, one opaque colour, no normal map, not a live GPU target.
		// rev and pix identify the pixels folded (a re-registered id counts its revisions from the start again, with a new
		// pixel array).
		private bool SolidUv(Lua3DScene s, int id, out float u, out float v, out int rev, out int[]? pix) {
			u = v = 0; rev = 0; pix = null;
			if (_rttColor.ContainsKey(id) || !s._tex.TryGetValue(id, out var te) || te.Pix == null || te.Normal >= 0) return false;
			rev = te.Rev; pix = te.Pix;
			if (!_solid.TryGetValue(id, out var so) || so.rev != te.Rev) { so = (te.Rev, ColorPalette.SolidColour(te.Pix, te.W * te.H)); _solid[id] = so; }
			return so.rgb >= 0 && _palette.UvOf(so.rgb, out u, out v);
		}
		// whether texture id still holds what SolidUv folded: the same pixel array at the same revision
		private bool StillSolid(Lua3DScene s, int id, int rev, int[]? pix) =>
			!_rttColor.ContainsKey(id) && s._tex.TryGetValue(id, out var te) && te.Pix != null && ReferenceEquals(te.Pix, pix) && te.Rev == rev && te.Normal < 0;
		// the palette's GL texture, uploaded when colours were added (binds on the active unit then)
		private unsafe uint PaletteTex() {
			if (_palTex != 0 && !_palette.Dirty) return _palTex;
			bool make = _palTex == 0;
			if (make) _palTex = Gl.GenTexture();
			Gl.BindTexture(TextureTarget.Texture2D, _palTex);
			int n = ColorPalette.Side;
			fixed (byte* p = _palette.Rgba) {
				if (make) Gl.TexImage2D(TextureTarget.Texture2D, 0, (int)InternalFormat.Rgba8, (uint)n, (uint)n, 0, PixelFormat.Rgba, PixelType.UnsignedByte, p);
				else Gl.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, (uint)n, (uint)n, PixelFormat.Rgba, PixelType.UnsignedByte, p);
			}
			if (make) {
				Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
				Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);
				Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Nearest);
				Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Nearest);
			}
			_palette.Dirty = false;
			return _palTex;
		}
		// forget the palette and every fold made from it (the caller drops the object caches with it)
		private void ClearPalette() {
			if (_palTex != 0) { Gl.DeleteTexture(_palTex); _palTex = 0; }
			_palette.Clear(); _solid.Clear();
		}
		// a textured draw's normal map (SetTextureNormalMap) on unit 6, or none. Call it BEFORE binding the
		// colour texture on unit 0: a first use uploads the map, which binds it on the active unit.
		private void BindNormalFor(Lua3DScene s, int texId) {
			uint n = 0;
			if (s._tex.TryGetValue(texId, out var te) && te.Normal >= 0) {
				n = TexFor(s, te.Normal);
				if (n != 0) { Gl.ActiveTexture(TextureUnit.Texture6); Gl.BindTexture(TextureTarget.Texture2D, n); }
			}
			UNormOn(n != 0 ? 1 : 0);
		}
		private uint SpriteFor(Lua3DScene s, int id) {
			s._sprites.TryGetValue(id, out var se);
			int rev = se?.Rev ?? 0;
			bool had = _glSprite.TryGetValue(id, out var e);
			if (had && e.Rev == rev) return e.Tex;
			if (se?.Pix == null) return 0;
			int w = se.W, h = se.H;
			// per-sprite filter opt-in (SetSpriteFilter): particles/foam/grass want LINEAR soft edges,
			// while the pixel-art character billboards stay crisp NEAREST (the diorama look)
			e.Tex = UploadTexture(had ? e.Tex : 0, had && e.W == w && e.H == h, se.Pix, w, h, true, se.Filter);
			e.Rev = rev; e.W = w; e.H = h; _glSprite[id] = e;
			return e.Tex;
		}

		// ── per-map cache release (Lua3DScene.ClearRenderCaches / UnregisterTexture) ──────────────
		/// <summary>Delete this texId's GL upload and any reflection RTT target bound to it. The next
		/// TexFor for a (re-registered) id re-uploads lazily.</summary>
		public void DropTexture(int id) {
			if (_glTex.Remove(id, out var e)) Gl.DeleteTexture(e.Tex);
			_solid.Remove(id);
			DropRenderTarget(id);
		}

		/// <summary>Delete this sprite id's GL upload (re-uploads lazily if re-registered).</summary>
		public void DropSprite(int id) {
			if (_glSprite.Remove(id, out var e)) Gl.DeleteTexture(e.Tex);
		}
	}
}
