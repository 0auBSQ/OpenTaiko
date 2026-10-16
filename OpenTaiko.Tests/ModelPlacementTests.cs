using System;
using OpenTaiko;
using Xunit;

namespace OpenTaikoTests {
	// A model's Pose writes model-space triangles and places them with the object's transform. These check that
	// the transform puts every vertex at its world position T(x,y,z) * Ry(yaw) * S(scale) * p, that the baked face
	// shades equal the shades of the world-space triangles, the automatic bounds and world bounding sphere the
	// culling uses, and the object-space camera and normals of the ObjSetNormal test.
	public class ModelPlacementTests {
		// reference: the world position T(x,y,z) * Ry(yaw) * S(scale) * p of a model-space vertex p
		private static void WorldReference(double yawDeg, double scale, double x, double y, double z, double px, double py, double pz,
			out double wx, out double wy, out double wz) {
			double yr = yawDeg * Math.PI / 180.0, c = Math.Cos(yr), s = Math.Sin(yr);
			double w0 = c * scale, w2 = -s * scale, w8 = s * scale, w10 = c * scale, w5 = scale;
			wx = w0 * px + w8 * pz + x; wy = w5 * py + y; wz = w2 * px + w10 * pz + z;
		}

		// the CPU renderers' row-major transform of a point
		private static void Apply(double[] t, double px, double py, double pz, out double x, out double y, out double z) {
			x = t[0] * px + t[1] * py + t[2] * pz + t[3];
			y = t[4] * px + t[5] * py + t[6] * pz + t[7];
			z = t[8] * px + t[9] * py + t[10] * pz + t[11];
		}

		[Theory]
		[InlineData(0.0, 1.0, 0.0, 0.0, 0.0)]
		[InlineData(90.0, 1.0, 4.0, 0.0, 7.0)]
		[InlineData(-37.5, 0.35, -120.0, 3.5, 88.25)]
		[InlineData(180.0, 2.5, 1000.0, -2.0, -1000.0)]
		[InlineData(271.0, -1.6, 12.0, 0.5, -3.0)]   // a negative scale mirrors the posed triangles, not the transform
		public void TransformPutsEveryVertexAtItsWorldPosition(double yaw, double scale, double x, double y, double z) {
			var rng = new Random(1234);
			var m = new double[16];
			ModelPlacement.Matrix(x, y, z, yaw, scale, m);
			Assert.True(SceneObject.MaxStretch(m) > 0);
			ModelPlacement.Light(yaw, out double lx, out double ly, out double lz);
			double sg = scale < 0 ? -1 : 1;
			for (int tri = 0; tri < 200; tri++) {
				var p = new double[9];
				for (int k = 0; k < 9; k++) p[k] = rng.NextDouble() * 4 - 2;
				var w = new double[9];
				for (int v = 0; v < 3; v++) {
					WorldReference(yaw, scale, x, y, z, p[v * 3], p[v * 3 + 1], p[v * 3 + 2], out w[v * 3], out w[v * 3 + 1], out w[v * 3 + 2]);
					Apply(m, sg * p[v * 3], sg * p[v * 3 + 1], sg * p[v * 3 + 2], out double nx, out double ny, out double nz);
					double tol = 1e-9 * (1 + Math.Abs(x) + Math.Abs(y) + Math.Abs(z));
					Assert.Equal(w[v * 3], nx, tol); Assert.Equal(w[v * 3 + 1], ny, tol); Assert.Equal(w[v * 3 + 2], nz, tol);
				}
				double worldShade = ModelPlacement.Shade(w[0], w[1], w[2], w[3], w[4], w[5], w[6], w[7], w[8], 0.35, 0.82, 0.45);
				double placedShade = ModelPlacement.Shade(sg * p[0], sg * p[1], sg * p[2], sg * p[3], sg * p[4], sg * p[5],
					sg * p[6], sg * p[7], sg * p[8], lx, ly, lz);
				Assert.Equal(worldShade, placedShade, 1e-9);
			}
		}

		[Fact]
		public void TransformNeverMirrors() {
			var m = new double[16];
			ModelPlacement.Matrix(3, 4, 5, 63, -2.0, m);
			double det = m[0] * (m[5] * m[10] - m[6] * m[9]) - m[1] * (m[4] * m[10] - m[6] * m[8]) + m[2] * (m[4] * m[9] - m[5] * m[8]);
			Assert.True(det > 0);
		}

		[Fact]
		public void AutomaticBoundsGrowUntilExplicitOnesAreSet() {
			var o = new SceneObject { D = new double[] { 1, 2, 3, -4, 0, 8, 2, 9, -1, 0, 0, 0 } };
			o.GrowBounds(0, 4, 3);
			Assert.True(o.HasBounds);
			Assert.Equal((-4.0, 0.0, -1.0, 2.0, 9.0, 8.0), (o.MinX, o.MinY, o.MinZ, o.MaxX, o.MaxY, o.MaxZ));
			Assert.Equal((-1.0, 4.5, 3.5), (o.CenX, o.CenY, o.CenZ));
			Assert.Equal(Math.Sqrt(3 * 3 + 4.5 * 4.5 + 4.5 * 4.5), o.Radius, 12);

			o.BoundsSet = true;
			o.D = new double[] { 100, 100, 100 };
			o.GrowBounds(0, 1, 3);
			Assert.Equal(2.0, o.MaxX);
		}

		[Fact]
		public void WorldSphereHoldsEveryTransformedCorner() {
			var o = new SceneObject { D = new double[] { -1, 0, -2, 3, 1.5, 2, 0.5, 4, -0.5 } };
			o.GrowBounds(0, 3, 3);
			var m = new double[16];
			ModelPlacement.Matrix(40, -3, 12, 33, 1.75, m);
			o.Transform = m;
			Assert.True(o.WorldSphere(out double cx, out double cy, out double cz, out double r));
			Assert.Equal(1.75 * o.Radius, r, 9);   // rotation times a uniform scale stretches by exactly the scale
			for (int i = 0; i < 8; i++) {
				Apply(m, (i & 1) == 0 ? o.MinX : o.MaxX, (i & 2) == 0 ? o.MinY : o.MaxY, (i & 4) == 0 ? o.MinZ : o.MaxZ,
					out double x, out double y, out double z);
				double d = Math.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy) + (z - cz) * (z - cz));
				Assert.True(d <= r + 1e-9);
			}
			Assert.False(new SceneObject().WorldSphere(out _, out _, out _, out _));
		}

		[Fact]
		public void MaxStretchBoundsAShear() {
			// columns (1,0,0), (1.5,1,0), (0,0,0.5): a shear stretches some vector by more than any column's length
			double[] t = { 1, 1.5, 0, 0, 0, 1, 0, 0, 0, 0, 0.5, 0, 0, 0, 0, 1 };
			double bound = SceneObject.MaxStretch(t), most = 0;
			for (int i = 0; i < 2000; i++) {
				double a = i * 0.0031415, b = i * 0.0271828;
				double vx = Math.Cos(a) * Math.Cos(b), vy = Math.Sin(a) * Math.Cos(b), vz = Math.Sin(b);
				Apply(t, vx, vy, vz, out double x, out double y, out double z);
				most = Math.Max(most, Math.Sqrt(x * x + y * y + z * z));
			}
			Assert.True(most > Math.Sqrt(1.5 * 1.5 + 1));   // longer than the longest column
			Assert.True(bound >= most);
		}

		[Fact]
		public void ObjectSpaceCameraAndNormalFollowTheTransform() {
			// sheared, mirrored (negative determinant) and translated
			double[] t = { 1.2, 0.4, 0, 30, 0, -0.8, 0.3, -5, 0.5, 0, 1.5, 12, 0, 0, 0, 1 };
			var o = new SceneObject { Transform = t };
			var rng = new Random(77);
			for (int i = 0; i < 100; i++) {
				double px = rng.NextDouble() * 20 - 10, py = rng.NextDouble() * 20 - 10, pz = rng.NextDouble() * 20 - 10;
				Apply(t, px, py, pz, out double wx, out double wy, out double wz);
				Assert.True(o.ToLocal(wx, wy, wz, out double lx, out double ly, out double lz));
				Assert.Equal(px, lx, 1e-9); Assert.Equal(py, ly, 1e-9); Assert.Equal(pz, lz, 1e-9);
			}

			// the world normal stays perpendicular to the moved surface and keeps its front side
			double nx = 0.3, ny = -0.5, nz = 0.8;
			double ax = 0, ay = nz, az = -ny;                                          // n x (1, 0, 0)
			double bx = ny * az - nz * ay, by = nz * ax - nx * az, bz = nx * ay - ny * ax;   // n x a
			double gx = nx, gy = ny, gz = nz;
			o.NormalToWorld(ref gx, ref gy, ref gz);
			double Moved(double vx, double vy, double vz) =>
				(t[0] * vx + t[1] * vy + t[2] * vz) * gx + (t[4] * vx + t[5] * vy + t[6] * vz) * gy + (t[8] * vx + t[9] * vy + t[10] * vz) * gz;
			Assert.Equal(0.0, Moved(ax, ay, az), 1e-9);
			Assert.Equal(0.0, Moved(bx, by, bz), 1e-9);
			Assert.True(Moved(nx, ny, nz) > 0);

			var flat = new SceneObject { Transform = new double[] { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1 } };
			Assert.False(flat.ToLocal(1, 2, 3, out _, out _, out _));
			Assert.False(new SceneObject().ToLocal(1, 2, 3, out _, out _, out _));
		}
	}
}
