using System;
using OpenTaiko;
using Xunit;

namespace OpenTaikoTests {
	// Data-safety helpers of the 3D scene that run without GL: primitive-array growth, 2D line clipping,
	// particle ageing and the renderer failure classification. A Lua3DScene itself needs a GL texture for
	// its canvas, so the scene-level rules (mixed kinds, id counting) are not exercised here.
	public class Scene3DSafetyTests {
		[Fact]
		public void EnsureCapFitsAStrideJump() {
			// 63 sprites (stride 8) leave a 512-double array; a triangle (stride 17) at index 63 needs 1088
			var o = new SceneObject { N = 63, D = new double[512] };
			Lua3DScene.EnsureCap(o, 17);
			Assert.True(o.D.Length >= 64 * 17);
		}

		[Fact]
		public void EnsureCapStartsAt64PrimitivesThenDoubles() {
			var o = new SceneObject();
			Lua3DScene.EnsureCap(o, 16);
			Assert.Equal(64 * 16, o.D.Length);
			o.N = 64;
			Lua3DScene.EnsureCap(o, 16);
			Assert.Equal(128 * 16, o.D.Length);
			o.N = 10;
			Lua3DScene.EnsureCap(o, 16);
			Assert.Equal(128 * 16, o.D.Length);   // room left: no growth
		}

		[Fact]
		public void ClipLineCutsFarEndpointsToTheCanvas() {
			int x0 = -1_000_000_000, y0 = 50, x1 = 1_000_000_000, y1 = 50;
			Assert.True(Lua3DScene.ClipLine(ref x0, ref y0, ref x1, ref y1, 100, 80));
			Assert.Equal((0, 50, 99, 50), (x0, y0, x1, y1));

			x0 = -50; y0 = -50; x1 = 150; y1 = 150;   // the diagonal through the square canvas
			Assert.True(Lua3DScene.ClipLine(ref x0, ref y0, ref x1, ref y1, 100, 100));
			Assert.Equal((0, 0, 99, 99), (x0, y0, x1, y1));
		}

		[Fact]
		public void ClipLineKeepsInsideSegmentsAndDropsOutsideOnes() {
			int x0 = 3, y0 = 4, x1 = 20, y1 = 7;
			Assert.True(Lua3DScene.ClipLine(ref x0, ref y0, ref x1, ref y1, 32, 32));
			Assert.Equal((3, 4, 20, 7), (x0, y0, x1, y1));

			x0 = -10; y0 = 5; x1 = -1; y1 = 40;   // left of the canvas
			Assert.False(Lua3DScene.ClipLine(ref x0, ref y0, ref x1, ref y1, 32, 32));
			x0 = 40; y0 = -5; x1 = 60; y1 = 10;    // past the right edge
			Assert.False(Lua3DScene.ClipLine(ref x0, ref y0, ref x1, ref y1, 32, 32));
		}

		private static Particle P(double life, double vx = 0, double drag = 0) =>
			new() { Life = life, MaxLife = life, VX = vx, Drag = drag, Sprite = -1 };

		[Fact]
		public void NonFiniteLifeOrTimeStepLeavesNoImmortalParticles() {
			var ps = new ParticleSystem();
			ps.Add(P(double.NaN));
			ps.Add(P(double.PositiveInfinity));
			Assert.Equal(0, ps.Count);

			ps.Add(P(1.0));
			ps.Update(double.NaN);
			Assert.Equal(1.0, ps.P[0].Life);
			ps.Update(1.5);
			Assert.Equal(0, ps.Count);
		}

		[Fact]
		public void DragDampsTheSameAtAnyFrameRate() {
			var one = new ParticleSystem(); one.Add(P(10, vx: 4, drag: 3));
			var many = new ParticleSystem(); many.Add(P(10, vx: 4, drag: 3));
			one.Update(0.5);
			for (int i = 0; i < 8; i++) many.Update(0.0625);
			Assert.Equal(4 * Math.Exp(-1.5), one.P[0].VX, 9);
			Assert.Equal(one.P[0].VX, many.P[0].VX, 9);
		}

		[Fact]
		public void DataErrorsAreToldApartFromDriverFailures() {
			Assert.True(Lua3DScene.IsDataError(new IndexOutOfRangeException()));
			Assert.True(Lua3DScene.IsDataError(new ArgumentOutOfRangeException()));
			Assert.False(Lua3DScene.IsDataError(new Exception("Program failed to link")));
			// a Parallel.For band wraps its exception
			Assert.True(Lua3DScene.IsDataError(new AggregateException(new IndexOutOfRangeException(),
				new AggregateException(new NullReferenceException()))));
			Assert.False(Lua3DScene.IsDataError(new AggregateException(new IndexOutOfRangeException(), new Exception("GL error"))));
		}
	}
}
