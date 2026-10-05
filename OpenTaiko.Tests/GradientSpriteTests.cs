using System.Collections.Generic;
using FDK;
using OpenTaiko;
using Xunit;

namespace OpenTaikoTests {
	// The CPU gradient map used for 3D sprites (Lua3DScene.RegisterSpriteFromTextureGradient) must match the 2D
	// texture shader: luminance → linear sample of the 256-texel LUT (clamped), colour mixed by the blend, alpha
	// multiplied by the sampled alpha. No GL here: only the LUT and the pixel maths are exercised.
	public class GradientSpriteTests {
		private static List<(float Pos, float R, float G, float B, float A)> Stops(params (float, float, float, float, float)[] s) => new(s);

		private static readonly byte[] RedToBlue = CGradientMap.BuildLut(Stops((0f, 1f, 0f, 0f, 1f), (1f, 0f, 0f, 1f, 1f)));

		private static byte[] Px(params byte[] rgba) => rgba;

		[Fact]
		public void LutRunsFromTheFirstStopToTheLast() {
			byte[] lut = CGradientMap.BuildLut(Stops((1f, 1f, 1f, 1f, 1f), (0f, 0f, 0f, 0f, 1f)));   // unsorted on purpose
			Assert.Equal(256 * 4, lut.Length);
			Assert.Equal(new byte[] { 0, 0, 0, 255 }, lut[0..4]);
			Assert.Equal(new byte[] { 255, 255, 255, 255 }, lut[1020..1024]);
			Assert.Equal(128, lut[128 * 4]);
		}

		[Fact]
		public void FullBlendReplacesTheColourByLuminance() {
			byte[] px = Px(255, 255, 255, 255, 0, 0, 0, 255);
			CGradientMap.MapRgba(RedToBlue, 1f, px);
			Assert.Equal(new byte[] { 0, 0, 255, 255 }, px[0..4]);    // white → the last stop
			Assert.Equal(new byte[] { 255, 0, 0, 255 }, px[4..8]);    // black → the first stop
		}

		[Fact]
		public void LuminanceSamplesBetweenTwoTexelsLikeLinearFiltering() {
			// a hard step: texels 0..63 black, 64..255 white
			byte[] lut = new byte[256 * 4];
			for (int i = 0; i < 256; i++) { byte v = (byte)(i >= 64 ? 255 : 0); lut[i * 4] = lut[i * 4 + 1] = lut[i * 4 + 2] = v; lut[i * 4 + 3] = 255; }
			byte[] px = Px(64, 64, 64, 255, 128, 128, 128, 255);
			CGradientMap.MapRgba(lut, 1f, px);
			// grey 64: texel coordinate 64/255 × 256 − 0.5 = 63.75, a quarter of texel 63 and three quarters of 64
			Assert.InRange(px[0], 190, 193);
			Assert.Equal(255, px[4]);
		}

		[Fact]
		public void PartialBlendMixesWithTheOriginal() {
			byte[] px = Px(255, 255, 255, 255);
			CGradientMap.MapRgba(RedToBlue, 0.5f, px);
			Assert.Equal(new byte[] { 128, 128, 255, 255 }, px);
		}

		[Fact]
		public void NoBlendAndTransparentPixelsStayUntouched() {
			byte[] px = Px(10, 20, 30, 255, 200, 100, 50, 0);
			CGradientMap.MapRgba(RedToBlue, 0f, px);
			Assert.Equal(new byte[] { 10, 20, 30, 255, 200, 100, 50, 0 }, px);
			CGradientMap.MapRgba(RedToBlue, 1f, px);
			Assert.Equal(new byte[] { 200, 100, 50, 0 }, px[4..8]);
		}

		[Fact]
		public void StopAlphaScalesThePixelAlpha() {
			byte[] lut = CGradientMap.BuildLut(Stops((0f, 1f, 1f, 1f, 0.5f), (1f, 1f, 1f, 1f, 0.5f)));
			byte[] px = Px(90, 90, 90, 200);
			CGradientMap.MapRgba(lut, 1f, px);
			Assert.Equal(100, px[3]);   // 200 × 128/255
		}

		[Fact]
		public void BadInputsAreIgnored() {
			byte[] px = Px(1, 2, 3, 4);
			CGradientMap.MapRgba(null, 1f, px);
			CGradientMap.MapRgba(new byte[8], 1f, px);
			CGradientMap.MapRgba(RedToBlue, float.NaN, px);
			CGradientMap.MapRgba(RedToBlue, 1f, null);
			Assert.Equal(new byte[] { 1, 2, 3, 4 }, px);
		}

		[Fact]
		public void SpritePixelsPackAsArgbAndKeepTheSource() {
			byte[] src = Px(255, 255, 255, 255, 0x12, 0x34, 0x56, 0x78);
			int[] plain = Lua3DScene.ToSpriteArgb(src, 2, 1);
			Assert.Equal(unchecked((int)0xFFFFFFFF), plain[0]);
			Assert.Equal(0x78123456, plain[1]);

			int[] mapped = Lua3DScene.ToSpriteArgb(src, 2, 1, RedToBlue, 1f);
			Assert.Equal(unchecked((int)0xFF0000FF), mapped[0]);
			Assert.Equal(new byte[] { 255, 255, 255, 255, 0x12, 0x34, 0x56, 0x78 }, src);   // the cached pixels stay as loaded
		}
	}
}
