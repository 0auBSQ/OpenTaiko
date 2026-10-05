using Silk.NET.OpenGLES;

namespace FDK;

/// <summary>
/// A 256×1 RGBA texture that maps luminance (0→1) to a colour.
/// Set <see cref="CTexture.ActiveGradientMapId"/> or call
/// <see cref="CTexture.SetGradientMap"/> to apply it at draw time.
/// </summary>
public class CGradientMap : IDisposable {
	public uint TextureId { get; private set; }
	/// <summary>The 256 RGBA texels of the texture, for recolouring pixels on the CPU (<see cref="MapRgba"/>).</summary>
	public byte[] Lut { get; }
	private bool _disposed;

	/// <param name="stops">
	/// At least 2 stops. Each: (position 0–1, R 0–1, G 0–1, B 0–1, A 0–1).
	/// Stops are sorted by position automatically.
	/// </param>
	public CGradientMap(IReadOnlyList<(float Pos, float R, float G, float B, float A)> stops) {
		if (stops.Count < 2)
			throw new ArgumentException("A gradient needs at least 2 stops.", nameof(stops));
		Lut = BuildLut(stops);
		TextureId = Upload(Lut);
	}

	/// <summary>The 256×1 RGBA texels for the given stops (sorted by position here).</summary>
	public static byte[] BuildLut(IReadOnlyList<(float Pos, float R, float G, float B, float A)> stops) {
		return BuildPixels(stops.OrderBy(s => s.Pos).ToList());
	}

	/// <summary>
	/// Recolours straight-alpha RGBA pixels in place the way the 2D texture shader applies a gradient map:
	/// the pixel's luminance samples <paramref name="lut"/> (linear filtering, clamped to the edges), the
	/// result is mixed over the pixel's colour by <paramref name="blend"/>, and the alpha is multiplied by the
	/// sampled alpha. A blend of 0 or less leaves the pixels untouched.
	/// </summary>
	public static void MapRgba(byte[] lut, float blend, byte[] rgba) {
		if (lut == null || lut.Length < 256 * 4 || rgba == null) return;
		if (!(blend > 0f)) return;
		if (blend > 1f) blend = 1f;
		for (int o = 0; o + 3 < rgba.Length; o += 4) {
			int a = rgba[o + 3];
			if (a == 0) continue;
			float r = rgba[o], g = rgba[o + 1], b = rgba[o + 2];
			float luma = (0.299f * r + 0.587f * g + 0.114f * b) / 255f;
			float x = luma * 256f - 0.5f;
			int i0 = (int)MathF.Floor(x);
			float f = x - i0;
			int i1 = i0 + 1;
			i0 = Math.Clamp(i0, 0, 255) * 4;
			i1 = Math.Clamp(i1, 0, 255) * 4;
			float mr = lut[i0] + (lut[i1] - lut[i0]) * f;
			float mg = lut[i0 + 1] + (lut[i1 + 1] - lut[i0 + 1]) * f;
			float mb = lut[i0 + 2] + (lut[i1 + 2] - lut[i0 + 2]) * f;
			float ma = lut[i0 + 3] + (lut[i1 + 3] - lut[i0 + 3]) * f;
			rgba[o] = ToByte(r + (mr - r) * blend);
			rgba[o + 1] = ToByte(g + (mg - g) * blend);
			rgba[o + 2] = ToByte(b + (mb - b) * blend);
			rgba[o + 3] = ToByte(a * ma / 255f);
		}
	}

	private static byte ToByte(float v) => (byte)Math.Clamp((int)(v + 0.5f), 0, 255);

	private static byte[] BuildPixels(List<(float Pos, float R, float G, float B, float A)> stops) {
		const int W = 256;
		byte[] px = new byte[W * 4];
		for (int i = 0; i < W; i++) {
			float t = i / (float)(W - 1);
			int lo = 0;
			for (int s = 0; s < stops.Count - 1; s++) {
				if (t >= stops[s].Pos) lo = s;
				else break;
			}
			int hi = Math.Min(lo + 1, stops.Count - 1);
			float span = stops[hi].Pos - stops[lo].Pos;
			float f = span < 1e-7f ? 0f : (t - stops[lo].Pos) / span;
			px[i * 4 + 0] = Clamp01((stops[lo].R + (stops[hi].R - stops[lo].R) * f));
			px[i * 4 + 1] = Clamp01((stops[lo].G + (stops[hi].G - stops[lo].G) * f));
			px[i * 4 + 2] = Clamp01((stops[lo].B + (stops[hi].B - stops[lo].B) * f));
			px[i * 4 + 3] = Clamp01((stops[lo].A + (stops[hi].A - stops[lo].A) * f));
		}
		return px;
	}

	private static byte Clamp01(float v) => (byte)Math.Clamp((int)(v * 255f + 0.5f), 0, 255);

	private static unsafe uint Upload(byte[] pixels) {
		uint handle = Game.Gl.GenTexture();
		Game.Gl.BindTexture(TextureTarget.Texture2D, handle);
		fixed (byte* ptr = pixels) {
			Game.Gl.TexImage2D(TextureTarget.Texture2D, 0,
				(int)PixelFormat.Rgba, 256, 1, 0,
				PixelFormat.Rgba, GLEnum.UnsignedByte, ptr);
		}
		Game.Gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMinFilter, (int)TextureMinFilter.Linear);
		Game.Gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMagFilter, (int)TextureMagFilter.Linear);
		Game.Gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
		Game.Gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
		Game.Gl.BindTexture(TextureTarget.Texture2D, 0);
		return handle;
	}

	public void Dispose() {
		if (!_disposed) {
			if (TextureId != 0) { Game.Gl.DeleteTexture(TextureId); TextureId = 0; }
			_disposed = true;
		}
	}
}
