using System.Drawing;
using OpenTaiko;
using Silk.NET.Maths;
using Xunit;

namespace OpenTaikoTests {
	// The batched glyph quads give the vertices CTexture.t2DDraw gives the same quad: its matrix, built here the way
	// t2DDraw builds it for an upright draw, puts the corners exactly on the batch's box, and the batch carries
	// t2DDraw's texture rect.
	public class GlyphQuadBatchTests {
		// t2DDraw's corner (vx, vy in -1..1, CTexture's quad) through its mvp, as clip-space x, y
		private static (float x, float y) DrawCorner(float x, float y, RectangleF rc, float sx, float sy, int gw, int gh, float vx, float vy) {
			Matrix4X4<float> mvp = Matrix4X4<float>.Identity;
			float gameAspect = (float)gw / gh;
			mvp *= Matrix4X4.CreateScale(rc.Width / gw, rc.Height / gh, 1f) * Matrix4X4.CreateScale(sx, sy, 1.0f);
			mvp *= Matrix4X4.CreateScale(1.0f * gameAspect, 1.0f, 1.0f) * Matrix4X4.CreateRotationZ(0f) * Matrix4X4.CreateScale(1.0f / gameAspect, 1.0f, 1.0f);
			float offsetX = rc.Width * sx / gw;
			float offsetY = rc.Height * sy / gh;
			mvp *= Matrix4X4.CreateTranslation(offsetX, -offsetY, 0.0f);
			mvp *= Matrix4X4.CreateTranslation(-1.0f, 1.0f, 0f);
			mvp *= Matrix4X4.CreateTranslation(x / gw * 2, -y / gh * 2, 0.0f);
			return (vx * mvp.M11 + vy * mvp.M21 + mvp.M41, vx * mvp.M12 + vy * mvp.M22 + mvp.M42);
		}

		[Theory]
		[InlineData(1234.5678f, 84.25f, 30f, 2f, 22f, 31f, 1f, 1f, 256f, 256f, 1920, 1080)]
		[InlineData(17.125f, 1003.9f, 401f, 389f, 47f, 52f, 0.73f, 1f, 1024f, 1024f, 1920, 1080)]
		[InlineData(960.3333f, 540.77f, 0f, 0f, 12f, 40f, 1.5f, 0.5f, 512f, 512f, 1920, 1080)]
		[InlineData(1699.0417f, 893.5f, 233f, 61f, 29f, 33f, 0.8173f, 1f, 512f, 512f, 1280, 720)]
		[InlineData(-12.6f, 7.3f, 5f, 9f, 31f, 28f, 1f, 1f, 256f, 256f, 1920, 1200)]
		public void QuadMatchesTheSingleDraw(float x, float y, float rx, float ry, float rw, float rh, float sx, float sy, float texW, float texH, int gw, int gh) {
			var rc = new RectangleF(rx, ry, rw, rh);
			var q = new float[GlyphQuadBatch.FloatsPerQuad];
			GlyphQuadBatch.PackQuad(q, x, y, rc, sx, sy, gw, gh, texW, texH, 0.25f, 0.5f, 0.75f, 0.5f, 2);
			// the box is (left, top, right, bottom): t2DDraw's corners top-left (-1, 1) and bottom-right (1, -1)
			var (l, t) = DrawCorner(x, y, rc, sx, sy, gw, gh, -1, 1);
			var (r, b) = DrawCorner(x, y, rc, sx, sy, gw, gh, 1, -1);
			Assert.Equal(new[] { l, t, r, b }, q[0..4]);
			Assert.Equal(DrawCorner(x, y, rc, sx, sy, gw, gh, 1, 1), (q[2], q[1]));
			Assert.Equal(DrawCorner(x, y, rc, sx, sy, gw, gh, -1, -1), (q[0], q[3]));
			// t2DDraw's textureRect uniform, then the tint and the slot
			Assert.Equal(new[] { rx / texW, ry / texH, rw / texW, rh / texH }, q[4..8]);
			Assert.Equal(new[] { 0.25f, 0.5f, 0.75f, 0.5f, 2f }, q[8..13]);
		}
	}
}
