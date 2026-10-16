using OpenTaiko;
using Xunit;

namespace OpenTaikoTests {
	// The layout of a scene object's primitive array, which every renderer decodes through PrimLayout.
	public class PrimLayoutTests {
		[Fact]
		public void EachKindKeepsItsCornersFirstAndItsFieldsInsideTheStride() {
			// quads and triangles: the corners first, the other fields right after them
			Assert.Equal(PrimLayout.TexQuadTex, PrimLayout.Corners(0) * PrimLayout.CornerStep(0));
			Assert.Equal(PrimLayout.FlatQuadStride, PrimLayout.Corners(1) * PrimLayout.CornerStep(1));
			Assert.Equal(PrimLayout.TexTriTex, PrimLayout.Corners(2) * PrimLayout.CornerStep(2));
			Assert.Equal(PrimLayout.SpriteQuadId, PrimLayout.Corners(4) * PrimLayout.CornerStep(4));
			// the last field of each kind ends its stride
			Assert.Equal(PrimLayout.TexQuadStride, PrimLayout.TexQuadShade + 1);
			Assert.Equal(PrimLayout.TexTriStride, PrimLayout.TexTriShade + 1);
			Assert.Equal(PrimLayout.SpriteStride, PrimLayout.SpriteCutout + 1);
			Assert.Equal(PrimLayout.SpriteQuadStride, PrimLayout.SpriteQuadCutout + 1);
			Assert.True(PrimLayout.SpriteW >= 3 && PrimLayout.SpriteW < PrimLayout.SpriteH);   // after the foot xyz
		}

		[Fact]
		public void StrideAndTextureFieldFollowTheKind() {
			int[] strides = { PrimLayout.TexQuadStride, PrimLayout.FlatQuadStride, PrimLayout.TexTriStride, PrimLayout.SpriteStride, PrimLayout.SpriteQuadStride };
			for (int kind = 0; kind < strides.Length; kind++) Assert.Equal(strides[kind], PrimLayout.Stride(kind));
			Assert.Equal(0, PrimLayout.Stride(-1));
			Assert.Equal(PrimLayout.TexQuadTex, PrimLayout.TexAt(0));
			Assert.Equal(PrimLayout.TexTriTex, PrimLayout.TexAt(2));
			Assert.Equal(-1, PrimLayout.TexAt(1));
			Assert.Equal(-1, PrimLayout.TexAt(3));
		}
	}
}
