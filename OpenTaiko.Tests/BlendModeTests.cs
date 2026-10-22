using System;
using System.Collections.Generic;
using System.Numerics;
using FDK;
using Silk.NET.OpenGLES;
using Xunit;

namespace OpenTaikoTests {
	// The 2D texture shader outputs colour multiplied by its alpha and CTexture blends it with BlendHelper.Premultiplied.
	// No GL here: the blend factors are evaluated the way GL applies them, on a source made by a C# copy of the
	// fragment shader's maths (and of the one before it, to compare with what the game drew until now).
	public class BlendModeTests {
		private const float Eps = 1e-5f;

		// the factor GL applies to one channel (alpha: the alpha channel)
		private static float Factor(GLEnum f, Vector4 s, Vector4 d, int ch) {
			bool alpha = ch == 3;
			float sc = s[ch], dc = d[ch];
			return f switch {
				GLEnum.Zero => 0f,
				GLEnum.One => 1f,
				GLEnum.SrcColor => alpha ? s.W : sc,
				GLEnum.OneMinusSrcColor => 1f - (alpha ? s.W : sc),
				GLEnum.DstColor => alpha ? d.W : dc,
				GLEnum.OneMinusDstColor => 1f - (alpha ? d.W : dc),
				GLEnum.SrcAlpha => s.W,
				GLEnum.OneMinusSrcAlpha => 1f - s.W,
				GLEnum.DstAlpha => d.W,
				GLEnum.OneMinusDstAlpha => 1f - d.W,
				_ => throw new ArgumentException("factor not covered: " + f),
			};
		}

		// what lands in an 8-bit-per-channel target (clamped to 0..1)
		private static Vector4 Blend(BlendFactors b, Vector4 s, Vector4 d) {
			var r = new Vector4();
			for (int ch = 0; ch < 4; ch++) {
				bool alpha = ch == 3;
				float fs = Factor(alpha ? b.SrcAlpha : b.SrcRgb, s, d, ch);
				float fd = Factor(alpha ? b.DstAlpha : b.DstRgb, s, d, ch);
				float v = b.Equation switch {
					BlendEquationModeEXT.FuncAdd => s[ch] * fs + d[ch] * fd,
					BlendEquationModeEXT.FuncSubtract => s[ch] * fs - d[ch] * fd,
					BlendEquationModeEXT.FuncReverseSubtract => d[ch] * fd - s[ch] * fs,
					_ => throw new ArgumentException("equation not covered"),
				};
				r[ch] = Math.Clamp(v, 0f, 1f);
			}
			return r;
		}

		// linear sample of a 256-texel gradient at u (clamped to the edges), channels 0..1
		private static Vector4 SampleLut(byte[] lut, float u) {
			float x = u * 256f - 0.5f;
			int i0 = (int)MathF.Floor(x);
			float f = x - i0;
			int a = Math.Clamp(i0, 0, 255) * 4, b = Math.Clamp(i0 + 1, 0, 255) * 4;
			var lo = new Vector4(lut[a], lut[a + 1], lut[a + 2], lut[a + 3]) / 255f;
			var hi = new Vector4(lut[b], lut[b + 1], lut[b + 2], lut[b + 3]) / 255f;
			return lo + (hi - lo) * f;
		}

		private static float Luma(Vector4 c) => c.X * 0.299f + c.Y * 0.587f + c.Z * 0.114f;

		// the fragment shader now: straight texel, gradient on the straight colour, then the colour times the alpha
		private static Vector4 Fragment(Vector4 texel, Vector4 color, byte[] lut = null, float gradientBlend = 0f) {
			Vector4 raw = texel;
			if (gradientBlend > 0f) {
				Vector4 mapped = SampleLut(lut, Luma(raw));
				Vector3 rgb = Vector3.Lerp(new Vector3(raw.X, raw.Y, raw.Z), new Vector3(mapped.X, mapped.Y, mapped.Z), gradientBlend);
				raw = new Vector4(rgb, raw.W * mapped.W);
			}
			Vector4 t = raw * color;
			return new Vector4(t.X * t.W, t.Y * t.W, t.Z * t.W, t.W);
		}

		// the fragment shader before: straight output, and the gradient divided the colour by its alpha
		private static Vector4 OldFragment(Vector4 texel, Vector4 color, byte[] lut = null, float gradientBlend = 0f) {
			Vector4 raw = texel;
			if (gradientBlend > 0f) {
				Vector3 straight = raw.W > 0.001f ? new Vector3(raw.X, raw.Y, raw.Z) / raw.W : Vector3.Zero;
				Vector4 mapped = SampleLut(lut, straight.X * 0.299f + straight.Y * 0.587f + straight.Z * 0.114f);
				raw = new Vector4(Vector3.Lerp(straight, new Vector3(mapped.X, mapped.Y, mapped.Z), gradientBlend), raw.W * mapped.W);
			}
			return raw * color;
		}

		private static Vector4 Draw(BlendType mode, Vector4 texel, float opacity, Vector4 dst) =>
			Blend(BlendHelper.Premultiplied(mode), Fragment(texel, new Vector4(1, 1, 1, opacity)), dst);

		private static Vector4 OldDraw(BlendType mode, Vector4 texel, float opacity, Vector4 dst) =>
			Blend(BlendHelper.Straight(mode), OldFragment(texel, new Vector4(1, 1, 1, opacity)), dst);

		private static readonly Vector4 Red = new(1, 0, 0, 1);
		private static readonly Vector4 Grey = new(0.5f, 0.5f, 0.5f, 1);

		[Theory]
		[InlineData(0f, 0.5f, 0.5f)]
		[InlineData(0.5f, 0.75f, 0.5f)]
		[InlineData(1f, 1.0f, 0.5f)]
		public void ScreenFadesWithOpacity(float opacity, float r, float g) {
			Vector4 o = Draw(BlendType.Screen, Red, opacity, Grey);
			Assert.Equal(r, o.X, Eps);
			Assert.Equal(g, o.Y, Eps);
			Assert.Equal(1f, o.W, Eps);
		}

		[Theory]
		[InlineData(0f, 0.5f, 0.5f)]
		[InlineData(0.5f, 0.5f, 0.25f)]
		[InlineData(1f, 0.5f, 0.0f)]
		public void MultiplyFadesWithOpacity(float opacity, float r, float g) {
			Vector4 o = Draw(BlendType.Multi, Red, opacity, Grey);
			Assert.Equal(r, o.X, Eps);
			Assert.Equal(g, o.Y, Eps);
			Assert.Equal(1f, o.W, Eps);
		}

		// sample sources (straight colour + alpha) and destinations (straight colour + alpha)
		private static IEnumerable<(Vector4 s, Vector4 d)> Pairs() {
			float[] vs = { 0f, 0.2f, 0.5f, 0.9f, 1f };
			foreach (float sa in vs)
				foreach (float da in vs)
					foreach (float sc in vs)
						foreach (float dc in vs)
							yield return (new Vector4(sc, 1f - sc, 0.3f, sa), new Vector4(dc, 0.6f, 1f - dc, da));
		}

		// a destination as the target stores it: colour multiplied by its alpha
		private static Vector4 Stored(Vector4 d) => new(d.X * d.W, d.Y * d.W, d.Z * d.W, d.W);

		[Fact]
		public void MultiplyMatchesTheTargetFormulas() {
			foreach (var (s, d) in Pairs()) {
				Vector4 o = Draw(BlendType.Multi, s, 1f, Stored(d));
				// MultiD_A = D_A; MultiD_A * MultiD_i = S_A S_i D_A D_i + (1 - S_A) D_A D_i
				Assert.Equal(d.W, o.W, Eps);
				for (int i = 0; i < 3; i++)
					Assert.Equal(s.W * s[i] * d.W * d[i] + (1 - s.W) * d.W * d[i], o[i], Eps);
			}
		}

		[Fact]
		public void ScreenMatchesTheTargetFormulas() {
			foreach (var (s, d) in Pairs()) {
				Vector4 o = Draw(BlendType.Screen, s, 1f, Stored(d));
				// Screen_A = 1 - (1 - S_A)(1 - D_A); Screen_A * Screen_i = 1 - (1 - S_A S_i)(1 - D_A D_i)
				Assert.Equal(1 - (1 - s.W) * (1 - d.W), o.W, Eps);
				for (int i = 0; i < 3; i++)
					Assert.Equal(1 - (1 - s.W * s[i]) * (1 - d.W * d[i]), o[i], Eps);
				// over an opaque destination: S_A S_i + (1 - S_A S_i) D_i
				if (d.W == 1f)
					for (int i = 0; i < 3; i++)
						Assert.Equal(s.W * s[i] + (1 - s.W * s[i]) * d[i], o[i], Eps);
			}
		}

		[Theory]
		[InlineData(BlendType.Normal)]
		[InlineData(BlendType.Add)]
		[InlineData(BlendType.Sub)]
		public void NormalAddAndSubKeepTheirColoursOverAnOpaqueDestination(BlendType mode) {
			foreach (var (s, d) in Pairs()) {
				var dst = new Vector4(d.X, d.Y, d.Z, 1f);
				foreach (float opacity in new[] { 0f, 0.3f, 1f }) {
					Vector4 now = Draw(mode, s, opacity, dst), before = OldDraw(mode, s, opacity, dst);
					for (int i = 0; i < 3; i++) Assert.Equal(before[i], now[i], Eps);
				}
			}
		}

		[Fact]
		public void NormalStoresTheCoverageInTheDestinationAlpha() {
			foreach (var (s, d) in Pairs()) {
				Vector4 o = Draw(BlendType.Normal, s, 1f, Stored(d));
				Assert.Equal(s.W + (1 - s.W) * d.W, o.W, Eps);
				// over a transparent target the colour is kept multiplied by its alpha (drawn again unchanged)
				if (d.W == 0f)
					for (int i = 0; i < 3; i++) Assert.Equal(s[i] * s.W, o[i], Eps);
			}
		}

		[Theory]
		[InlineData(BlendType.Normal)]
		[InlineData(BlendType.Add)]
		[InlineData(BlendType.Sub)]
		[InlineData(BlendType.Screen)]
		[InlineData(BlendType.Multi)]
		public void OpacityZeroLeavesAnOpaqueDestinationAlone(BlendType mode) {
			foreach (var (s, d) in Pairs()) {
				var dst = new Vector4(d.X, d.Y, d.Z, 1f);
				Vector4 o = Draw(mode, s, 0f, dst);
				for (int i = 0; i < 4; i++) Assert.Equal(dst[i], o[i], Eps);
			}
		}

		[Fact]
		public void OpaqueDestinationStaysOpaque() {
			foreach (BlendType mode in Enum.GetValues<BlendType>())
				foreach (var (s, d) in Pairs())
					Assert.Equal(1f, Draw(mode, s, 0.7f, new Vector4(d.X, d.Y, d.Z, 1f)).W, Eps);
		}

		private static readonly byte[] RedToBlue = CGradientMap.BuildLut(new List<(float, float, float, float, float)> { (0f, 1f, 0f, 0f, 1f), (1f, 0f, 0f, 1f, 1f) });

		[Fact]
		public void GradientKeepsTheLookOfOpaquePixels() {
			var tint = new Vector4(0.9f, 0.8f, 1f, 0.6f);
			foreach (float blend in new[] { 0.5f, 1f })
				foreach (float c in new[] { 0f, 0.25f, 0.5f, 0.8f, 1f }) {
					var texel = new Vector4(c, 1f - c, c * 0.5f, 1f);
					Vector4 now = Fragment(texel, tint, RedToBlue, blend), before = OldFragment(texel, tint, RedToBlue, blend);
					// the same colour once the old straight output is multiplied by its alpha
					for (int i = 0; i < 3; i++) Assert.Equal(before[i] * before.W, now[i], Eps);
					Assert.Equal(before.W, now.W, Eps);
				}
		}

		[Fact]
		public void GradientOnASeeThroughPixelMatchesTheCpuMapAndIsNoLongerBrightened() {
			// a dark grey at 25% alpha: the old shader divided it by its alpha and mapped it as a light grey
			var texel = new Vector4(64 / 255f, 64 / 255f, 64 / 255f, 64 / 255f);
			var white = new Vector4(1, 1, 1, 1);
			Vector4 now = Fragment(texel, white, RedToBlue, 1f), before = OldFragment(texel, white, RedToBlue, 1f);
			byte[] cpu = { 64, 64, 64, 64 };
			CGradientMap.MapRgba(RedToBlue, 1f, cpu);
			for (int i = 0; i < 3; i++) Assert.Equal(cpu[i] / 255f, now[i] / now.W, 1.5f / 255f);
			Assert.Equal(cpu[3] / 255f, now.W, 1.5f / 255f);
			Assert.True(before.Z > now.Z / now.W + 0.2f);   // the old mapping sat much nearer the blue (light) end
		}
	}
}
