using Silk.NET.OpenGLES;

namespace FDK;

/// <summary>
/// A 256×1 RGBA texture that maps luminance (0→1) to a colour.
/// Set <see cref="CTexture.ActiveGradientMapId"/> or call
/// <see cref="CTexture.SetGradientMap"/> to apply it at draw time.
/// </summary>
public class CGradientMap : IDisposable {
	public uint TextureId { get; private set; }
	private bool _disposed;

	/// <param name="stops">
	/// At least 2 stops. Each: (position 0–1, R 0–1, G 0–1, B 0–1, A 0–1).
	/// Stops are sorted by position automatically.
	/// </param>
	public CGradientMap(IReadOnlyList<(float Pos, float R, float G, float B, float A)> stops) {
		if (stops.Count < 2)
			throw new ArgumentException("A gradient needs at least 2 stops.", nameof(stops));
		var sorted = stops.OrderBy(s => s.Pos).ToList();
		TextureId = Upload(BuildPixels(sorted));
	}

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
		Game.Gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureMinFilter, (int)TextureMinFilter.Linear);
		Game.Gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureMagFilter, (int)TextureMagFilter.Linear);
		Game.Gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
		Game.Gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
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
