using System;
using System.Collections.Generic;
using OpenTaiko;
using Xunit;

namespace OpenTaikoTests {
	// GL-free pieces of the GPU rasterizer: the view culling against the vertex shader's projection, the indexed
	// merge emit, the point-light pick and the normal matrix.
	public class GpuRasterizerTests {
		// the camera basis Lua3DScene.SetCameraAngles builds
		private static (double[] r, double[] u, double[] f) Basis(double yawDeg, double pitchDeg) {
			double y = yawDeg * Math.PI / 180, p = pitchDeg * Math.PI / 180;
			double cy = Math.Cos(y), sy = Math.Sin(y), cp = Math.Cos(p), sp = Math.Sin(p);
			return (new[] { cy, 0, -sy }, new[] { -sy * sp, cp, -cy * sp }, new[] { sy * cp, sp, cy * cp });
		}

		// whether the point lands on the target in front of the eye, by the world vertex shader's projection
		private static bool OnScreen(double[] e, (double[] r, double[] u, double[] f) b, double scale, int w, int h, double x, double y, double z) {
			double vx = x - e[0], vy = y - e[1], vz = z - e[2];
			double cx = vx * b.r[0] + vy * b.r[1] + vz * b.r[2], cy = vx * b.u[0] + vy * b.u[1] + vz * b.u[2], cz = vx * b.f[0] + vy * b.f[1] + vz * b.f[2];
			if (cz <= 0.01) return false;
			return Math.Abs(cx * 2 * scale / w / cz) <= 1 && Math.Abs(cy * 2 * scale / h / cz) <= 1;
		}

		[Fact]
		public void ViewCullingNeverDropsWhatReachesTheScreen() {
			var rng = new Random(7);
			int culled = 0;
			for (int n = 0; n < 4000; n++) {
				var e = new[] { rng.NextDouble() * 40 - 20, rng.NextDouble() * 10, rng.NextDouble() * 40 - 20 };
				var b = Basis(rng.NextDouble() * 360, rng.NextDouble() * 160 - 80);
				int w = 320 + rng.Next(1000), h = 180 + rng.Next(600);
				double scale = h * 0.5 / Math.Tan((30 + rng.NextDouble() * 80) * 0.5 * Math.PI / 180);
				var v = ViewFrustum.Of(e[0], e[1], e[2], b.r[0], b.r[1], b.r[2], b.u[0], b.u[1], b.u[2], b.f[0], b.f[1], b.f[2], scale, w, h);
				double cx = rng.NextDouble() * 120 - 60, cy = rng.NextDouble() * 40 - 20, cz = rng.NextDouble() * 120 - 60;
				double hx = rng.NextDouble() * 6, hy = rng.NextDouble() * 6, hz = rng.NextDouble() * 6;
				bool any = false;
				for (int i = 0; i <= 4 && !any; i++) for (int j = 0; j <= 4 && !any; j++) for (int k = 0; k <= 4 && !any; k++)
					any = OnScreen(e, b, scale, w, h, cx + hx * (i / 2.0 - 1), cy + hy * (j / 2.0 - 1), cz + hz * (k / 2.0 - 1));
				bool boxIn = v.BoxInView(cx, cy, cz, hx, hy, hz);
				if (any) Assert.True(boxIn);
				double r = Math.Sqrt(hx * hx + hy * hy + hz * hz);
				if (any) Assert.True(v.SphereInView(cx, cy, cz, r));
				if (!boxIn) culled++;
			}
			Assert.True(culled > 1000);   // most random boxes around a camera are off screen
		}

		[Fact]
		public void ViewCullingDropsBoxesBehindOrBesideTheView() {
			var b = Basis(0, 0);   // looking down +Z, right +X, up +Y
			double scale = 360 / Math.Tan(35 * Math.PI / 180);
			var v = ViewFrustum.Of(0, 0, 0, b.r[0], b.r[1], b.r[2], b.u[0], b.u[1], b.u[2], b.f[0], b.f[1], b.f[2], scale, 1280, 720);
			Assert.True(v.BoxInView(0, 0, 20, 1, 1, 1));
			Assert.False(v.BoxInView(0, 0, -20, 1, 1, 1));    // behind
			Assert.False(v.BoxInView(200, 0, 20, 1, 1, 1));   // far to the right
			Assert.False(v.BoxInView(0, -200, 20, 1, 1, 1));  // far below
			Assert.True(v.BoxInView(0, 0, -20, 1, 1, 30));    // reaches in front of the eye
			Assert.Equal(25, v.FarDepth(0, 0, 20, 1, 1, 5), 9);
			Assert.Equal(15, v.NearDepth(0, 0, 20, 1, 1, 5), 9);
			Assert.Equal(39, v.NearDepth(50, 0, 40, 1, 1, 1), 9);   // beside the view axis: nearer in depth than in distance
			Assert.Equal(16, v.BoxDistSq(5, 0, 0, 1, 1, 1), 9);
		}

		private static SceneObject Quads() {
			// two quads: texture 5 then texture 7, uMax 2, vMax 3, shade 0.5
			var o = new SceneObject { Kind = 0, N = 2, D = new double[32] };
			double[] q = { 0, 0, 0, 1, 0, 0, 1, 1, 0, 0, 1, 0 };
			for (int i = 0; i < 2; i++) {
				for (int k = 0; k < 12; k++) o.D[i * 16 + k] = q[k] + (k % 3 == 2 ? i : 0);
				o.D[i * 16 + 12] = i == 0 ? 5 : 7; o.D[i * 16 + 13] = 2; o.D[i * 16 + 14] = 3; o.D[i * 16 + 15] = 0.5;
			}
			return o;
		}

		[Fact]
		public void MergedQuadsShareTheirCornersAndKeepTheTriangles() {
			var o = Quads();
			var sc = new GpuRasterizer.MergeScratch();
			GpuRasterizer.EmitMerged(o, new Dictionary<int, GpuRasterizer.MergeScratch> { [7] = sc });   // only texture 7
			Assert.Equal(4 * 9, sc.V.Count);
			Assert.Equal(new uint[] { 0, 1, 2, 0, 2, 3 }, sc.I.ToArray());
			// corner order and UVs of the triangle list: (0,vMax) (uMax,vMax) (uMax,0) | (0,vMax) (uMax,0) (0,0)
			double[][] want = { new double[] { 0, 0, 1, 0, 3 }, new double[] { 1, 0, 1, 2, 3 }, new double[] { 1, 1, 1, 2, 0 },
				new double[] { 0, 0, 1, 0, 3 }, new double[] { 1, 1, 1, 2, 0 }, new double[] { 0, 1, 1, 0, 0 } };
			for (int t = 0; t < 6; t++) {
				int vi = (int)sc.I[t] * 9;
				for (int c = 0; c < 5; c++) Assert.Equal(want[t][c], sc.V[vi + c], 6);
				Assert.Equal(0.5, sc.V[vi + 5], 6);
				Assert.Equal(1.0, Math.Abs(sc.V[vi + 8]), 6);   // the quad's geometric normal is +-Z
			}
			var ids = new List<int>();
			GpuRasterizer.TexIds(o, ids);
			Assert.Equal(new[] { 5, 7 }, ids.ToArray());
		}

		[Fact]
		public void MergedTrianglesKeepTheirUVs() {
			var o = new SceneObject { Kind = 2, N = 1, D = new double[] { 0, 0, 0, 0.1, 0.2, 1, 0, 0, 0.3, 0.4, 0, 1, 0, 0.5, 0.6, 9, 0.8 } };
			var sc = new GpuRasterizer.MergeScratch();
			GpuRasterizer.EmitMerged(o, new Dictionary<int, GpuRasterizer.MergeScratch> { [9] = sc });
			Assert.Equal(new uint[] { 0, 1, 2 }, sc.I.ToArray());
			Assert.Equal(0.3f, sc.V[9 + 3], 6); Assert.Equal(0.4f, sc.V[9 + 4], 6); Assert.Equal(0.8f, sc.V[9 + 5], 6);
		}

		[Fact]
		public void ObjectBufferHoldsTheMergedVerticesBatchedByTexture() {
			var o = Quads();
			var dst = new List<float>(); var batches = new List<GpuRasterizer.OBatch>();
			var b = new GpuRasterizer.Batcher { Batches = batches, Dst = dst, Mode = -1, Tex = int.MinValue };
			GpuRasterizer.Walk(o, ref b); b.Flush();
			Assert.Equal(2, batches.Count);
			Assert.Equal((1, 5, 0, 6), (batches[0].Mode, batches[0].TexId, batches[0].Start, batches[0].Count));
			Assert.Equal((1, 7, 6, 6), (batches[1].Mode, batches[1].TexId, batches[1].Start, batches[1].Count));
			var merged = new Dictionary<int, GpuRasterizer.MergeScratch> { [5] = new(), [7] = new() };
			GpuRasterizer.EmitMerged(o, merged);
			foreach (var bt in batches) {   // a batch's triangles are its texture's merged vertices taken through the indices
				var sc = merged[bt.TexId];
				Assert.Equal(bt.Count, sc.I.Count);
				for (int t = 0; t < bt.Count; t++)
					for (int c = 0; c < 9; c++) Assert.Equal(sc.V[(int)sc.I[t] * 9 + c], dst[(bt.Start + t) * 9 + c]);
			}
		}

		[Fact]
		public void FlatQuadsDrawFlatAndNeverMerge() {
			var o = new SceneObject { Kind = 1, N = 1, D = new double[] { 0, 0, 0, 1, 0, 0, 1, 1, 0, 0, 1, 0 } };
			var dst = new List<float>(); var batches = new List<GpuRasterizer.OBatch>();
			var b = new GpuRasterizer.Batcher { Batches = batches, Dst = dst, Mode = -1, Tex = int.MinValue };
			GpuRasterizer.Walk(o, ref b); b.Flush();
			Assert.Single(batches);
			Assert.Equal((0, 0, 0, 6), (batches[0].Mode, batches[0].TexId, batches[0].Start, batches[0].Count));
			for (int v = 0; v < 6; v++) {   // no uv, full shade
				Assert.Equal(0f, dst[v * 9 + 3]); Assert.Equal(0f, dst[v * 9 + 4]); Assert.Equal(1f, dst[v * 9 + 5]);
			}
			var sc = new GpuRasterizer.MergeScratch();
			GpuRasterizer.EmitMerged(o, new Dictionary<int, GpuRasterizer.MergeScratch> { [0] = sc });
			Assert.Empty(sc.V);
		}

		[Fact]
		public void LightPickKeepsTheNearestInListOrder() {
			var lights = new List<SceneLight>();
			for (int i = 0; i < 70; i++) lights.Add(new SceneLight { X = (i * 37) % 70, Range = 0 });   // distances 0..69 along X, shuffled
			lights[3] = new SceneLight { X = 200, Range = 190 };   // far away, but its reach comes within 10
			var pick = new int[64]; double[] keys = Array.Empty<double>(); int[] idx = Array.Empty<int>();
			int n = GpuRasterizer.PickLights(lights, 0, 0, 0, 64, pick, ref keys, ref idx);
			Assert.Equal(64, n);
			for (int i = 1; i < n; i++) Assert.True(pick[i] > pick[i - 1]);
			var chosen = new HashSet<int>(pick);
			Assert.Contains(3, chosen);
			for (int i = 0; i < 70; i++) if (i != 3 && lights[i].X >= 64) Assert.DoesNotContain(i, chosen);

			var few = lights.GetRange(0, 10);
			Assert.Equal(10, GpuRasterizer.PickLights(few, 0, 0, 0, 64, pick, ref keys, ref idx));
			for (int i = 0; i < 10; i++) Assert.Equal(i, pick[i]);
		}

		// column-major mat3 times a vector
		private static double[] Mul3(float[] m, double x, double y, double z) =>
			new[] { m[0] * x + m[3] * y + m[6] * z, m[1] * x + m[4] * y + m[7] * z, m[2] * x + m[5] * y + m[8] * z };

		[Fact]
		public void NormalMatrixKeepsNormalsPerpendicularUnderAStretch() {
			// rotation about Y by 30 degrees after a non-uniform scale (3, 1, 0.5), row-major
			double c = Math.Cos(Math.PI / 6), s = Math.Sin(Math.PI / 6);
			double[] t = { c * 3, 0, s * 0.5, 4, 0, 1, 0, 5, -s * 3, 0, c * 0.5, 6, 0, 0, 0, 1 };
			var m = GpuRasterizer.NormalMatrix(t, new float[9]);
			// a slanted surface: tangent (1, 1, 0), normal (1, -1, 0) in model space
			double[] wt = { t[0] + t[1], t[4] + t[5], t[8] + t[9] };
			var wn = Mul3(m, 1, -1, 0);
			Assert.Equal(0, (wt[0] * wn[0] + wt[1] * wn[1] + wt[2] * wn[2]) / Math.Sqrt(wn[0] * wn[0] + wn[1] * wn[1] + wn[2] * wn[2]), 5);

			// a mirror keeps the normal on the side the transform puts it, like the plain linear part
			double[] mirror = { -1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1 };
			var mm = GpuRasterizer.NormalMatrix(mirror, new float[9]);
			Assert.Equal(new[] { -1.0, 0, 0 }, Mul3(mm, 1, 0, 0));
			Assert.Equal(new[] { 0.0, 1, 0 }, Mul3(mm, 0, 1, 0));
		}

		[Fact]
		public void MergeCandidatesAreThePlainLitOpaqueTexturedObjects() {
			var o = new SceneObject { Kind = 0, N = 1 };
			Assert.True(o.MergeCandidate);
			o.Transform = new double[16]; Assert.False(o.MergeCandidate); o.Transform = null;
			o.TintR = 0.5; Assert.False(o.MergeCandidate); o.TintR = 1;
			o.Pass = 1; Assert.False(o.MergeCandidate); o.Pass = 0;
			o.Kind = 1; Assert.False(o.MergeCandidate);
		}

		// a textured triangle object from corner triples (x, y, z) and a texture per triangle
		private static SceneObject Tris(double[][] tris, int[] tex) {
			var o = new SceneObject { Kind = 2, N = tris.Length, D = new double[tris.Length * 17] };
			for (int i = 0; i < tris.Length; i++) {
				for (int c = 0; c < 3; c++) for (int a = 0; a < 3; a++) o.D[i * 17 + c * 5 + a] = tris[i][c * 3 + a];
				o.D[i * 17 + 15] = tex[i]; o.D[i * 17 + 16] = 1;
			}
			return o;
		}

		[Fact]
		public void TransparentPiecesAreRunsOfOnePlaneAndTexture() {
			var o = Tris(new[] {
				new double[] { 0, 0, 0, 7, 0, 0, 7, 3, 0 }, new double[] { 0, 0, 0, 7, 3, 0, 0, 3, 0 },        // a pane in z = 0
				new double[] { 7, 0, 0, 14, 0, 0, 14, 3, 0 }, new double[] { 7, 0, 0, 14, 3, 0, 7, 3, 0 },     // the same plane: same piece
				new double[] { 20, 0, 0, 20, 0, 5, 20, 3, 5 }, new double[] { 20, 0, 0, 20, 3, 5, 20, 3, 0 }, // a pane in x = 20
				new double[] { 0, 0, 0, 7, 0, 0, 7, 3, 0 },                                                   // z = 0 again, other texture
			}, new[] { 5, 5, 5, 5, 5, 5, 6 });
			var pieces = new List<GpuRasterizer.Piece>();
			GpuRasterizer.BuildPieces(o, pieces);
			Assert.Equal(3, pieces.Count);
			Assert.Equal((0, 12), (pieces[0].Start, pieces[0].Count));
			Assert.Equal((12, 6), (pieces[1].Start, pieces[1].Count));
			Assert.Equal((18, 3), (pieces[2].Start, pieces[2].Count));
			Assert.True(pieces[0].Planar); Assert.Equal(1, Math.Abs(pieces[0].Nz), 9); Assert.Equal(0, pieces[0].D, 9);
			Assert.Equal(14, pieces[0].MaxX, 9); Assert.Equal(3, pieces[0].MaxY, 9);
			Assert.True(pieces[1].Planar); Assert.Equal(1, Math.Abs(pieces[1].Nx), 9); Assert.Equal(20, pieces[1].D * pieces[1].Nx, 9);
		}

		// 40 unit quads along X whose corners alternate between height 0 and `h`, so each quad tilts the other way
		private static SceneObject Zigzag(double h) {
			var o = new SceneObject { Kind = 1, N = 40, D = new double[40 * 12] };
			for (int i = 0; i < 40; i++) {
				double y0 = i % 2 == 0 ? 0 : h, y1 = i % 2 == 0 ? h : 0;
				double[] q = { i, y0, 0, i + 1, y1, 0, i + 1, y1, 1, i, y0, 1 };
				Array.Copy(q, 0, o.D, i * 12, 12);
			}
			return o;
		}

		[Fact]
		public void TooManyRunsMakeOnePiecePlanarOnlyWhenFlat() {
			var pieces = new List<GpuRasterizer.Piece>();
			GpuRasterizer.BuildPieces(Zigzag(0.1), pieces);   // rippled water: every quad its own run
			Assert.Single(pieces);
			Assert.Equal((0, 240), (pieces[0].Start, pieces[0].Count));
			Assert.True(pieces[0].Planar);
			Assert.Equal(1, Math.Abs(pieces[0].Ny), 6);
			Assert.Equal(0.05, pieces[0].D * pieces[0].Ny, 6);
			GpuRasterizer.BuildPieces(Zigzag(3), pieces);     // a folded sheet
			Assert.Single(pieces);
			Assert.False(pieces[0].Planar);
		}

		[Fact]
		public void ParticlesTakeTheirPlaceBesideAPane() {
			// two queue items 20 and 10 away (keys: squared distances), seen from the origin looking down +Z
			double[] keys = { 400, 100 };
			var none = ReadOnlySpan<GpuRasterizer.TPlane>.Empty;
			Assert.Equal(0, GpuRasterizer.GapOf(keys, 2, none, 625, 0, 0, 25, 0, 0, -1));
			Assert.Equal(1, GpuRasterizer.GapOf(keys, 2, none, 225, 0, 0, 15, 0, 0, -1));
			Assert.Equal(2, GpuRasterizer.GapOf(keys, 2, none, 25, 0, 0, 5, 0, 0, -1));

			// one pane, seen at an angle: the plane x + z = 10 for x in -5..5 (depth 15 down to 5), centre (0, 0, 10)
			double k = Math.Sqrt(0.5);
			var pane = new[] { new GpuRasterizer.TPlane { Index = 0, Nx = k, Nz = k, D = 10 * k, Side = -10 * k, X0 = -1.0 / 3, X1 = 1, Y0 = -0.3, Y1 = 0.3 } };
			double[] one = { 100 };
			// behind the pane at its near end: nearer than the pane's centre, yet drawn before the pane
			Assert.Equal(0, GpuRasterizer.GapOf(one, 1, pane, 5 * 5 + 6.5 * 6.5, 5, 0, 6.5, 5 / 6.5, 0, 0.1));
			// in front of the pane at its far end: farther than the pane's centre, yet drawn after it
			Assert.Equal(1, GpuRasterizer.GapOf(one, 1, pane, 4 * 4 + 12 * 12, -4, 0, 12, -4 / 12.0, 0, 0.1));
			// beyond its plane but beside the pane on screen: by distance alone
			Assert.Equal(1, GpuRasterizer.GapOf(one, 1, pane, 8 * 8 + 3 * 3, 8, 0, 3, 8 / 3.0, 0, 0.1));

			// two panes that disagree: beyond the far one's plane z = 10, on the eye's side of the near one's plane x = 3
			var two = new[] {
				new GpuRasterizer.TPlane { Index = 0, Nz = 1, D = 10, Side = -10, X0 = double.NegativeInfinity, Y0 = double.NegativeInfinity, X1 = double.PositiveInfinity, Y1 = double.PositiveInfinity },
				new GpuRasterizer.TPlane { Index = 1, Nx = 1, D = 3, Side = -3, X0 = double.NegativeInfinity, Y0 = double.NegativeInfinity, X1 = double.PositiveInfinity, Y1 = double.PositiveInfinity },
			};
			Assert.Equal(0, GpuRasterizer.GapOf(new double[] { 200, 50 }, 2, two, 144, 0, 0, 12, 0, 0, 0.1));   // drawn before both
		}

		// the 8 corners of a camera-space box (x, y, depth), corner k at the far end of each axis whose bit (1, 2, 4) it has
		private static double[] Box(double x0, double x1, double y0, double y1, double d0, double d1) {
			var c = new double[24];
			for (int k = 0; k < 8; k++) { c[k * 3] = (k & 1) == 0 ? x0 : x1; c[k * 3 + 1] = (k & 2) == 0 ? y0 : y1; c[k * 3 + 2] = (k & 4) == 0 ? d0 : d1; }
			return c;
		}

		[Fact]
		public void PaneRectanglesKeepToTheirPartBeyondTheNearPlane() {
			// wholly beyond the near plane: the rectangle of its corners
			Assert.True(GpuRasterizer.CameraRect(Box(1, 2, -1, 1, 2, 4), 0.1, out double x0, out double y0, out double x1, out double y1));
			Assert.Equal((0.25, -0.5, 1.0, 0.5), (x0, y0, x1, y1));
			// crossing it: its far corners and the points where its edges cross the near plane
			Assert.True(GpuRasterizer.CameraRect(Box(-1, 1, 0, 1, -5, 5), 0.1, out x0, out y0, out x1, out y1));
			Assert.Equal(-10, x0, 9); Assert.Equal(0, y0, 9); Assert.Equal(10, x1, 9); Assert.Equal(10, y1, 9);
			// behind the eye up to the near plane: nothing on screen
			Assert.False(GpuRasterizer.CameraRect(Box(-1, 1, -1, 1, -6, 0.1), 0.1, out _, out _, out _, out _));
		}

		[Fact]
		public void NoiseTexelsHoldTheirCellsFourCorners() {
			var px = GpuRasterizer.NoiseTexels();
			Assert.Equal(256 * 256 * 4, px.Length);
			byte At(int x, int y) => px[(((y & 255) * 256) + (x & 255)) * 4];
			for (int y = 0; y < 256; y++)
				for (int x = 0; x < 256; x++) {
					int o = (y * 256 + x) * 4;
					Assert.Equal(At(x + 1, y), px[o + 1]);
					Assert.Equal(At(x, y + 1), px[o + 2]);
					Assert.Equal(At(x + 1, y + 1), px[o + 3]);
				}
		}

		[Fact]
		public void NoiseLatticeAlternatesAlongZOnly() {
			// a step in z moves (37, 17) on the texture: the value mostly jumps by about half the range there, while x and
			// y neighbours are unrelated (a quarter of their differences fall in the same band by chance)
			var px = GpuRasterizer.NoiseTexels();
			byte At(int x, int y) => px[(((y & 255) * 256) + (x & 255)) * 4];
			double HalfJumps(int dx, int dy) {
				int n = 0;
				for (int y = 0; y < 256; y++)
					for (int x = 0; x < 256; x++) {
						int d = (At(x + dx, y + dy) - At(x, y)) & 255;
						if (d >= 96 && d <= 160) n++;
					}
				return n / 65536.0;
			}
			Assert.InRange(HalfJumps(37, 17), 0.8, 1.0);
			Assert.InRange(HalfJumps(1, 0), 0.2, 0.3);
			Assert.InRange(HalfJumps(0, 1), 0.2, 0.3);
		}

		[Fact]
		public void SolidTexturesHoldOneOpaqueColour() {
			Assert.Equal(0x336699, GpuRasterizer.ColorPalette.SolidColour(new[] { 0x336699, 0x336699, 0x336699, 0x336699 }, 4));
			Assert.Equal(-1, GpuRasterizer.ColorPalette.SolidColour(new[] { 0x336699, 0x336698, 0x336699, 0x336699 }, 4));
			Assert.Equal(-1, GpuRasterizer.ColorPalette.SolidColour(new[] { unchecked((int)0x80336699), unchecked((int)0x80336699) }, 2));   // cut out
		}

		[Fact]
		public void PaletteGivesEachColourTheCentreOfItsOwnTexel() {
			var p = new GpuRasterizer.ColorPalette();
			int n = GpuRasterizer.ColorPalette.Side;
			Assert.True(p.UvOf(0x112233, out float u0, out float v0));
			Assert.True(p.UvOf(0x445566, out float u1, out float v1));
			Assert.True(p.UvOf(0x112233, out float u2, out float v2));
			Assert.Equal((0.5f / n, 0.5f / n), (u0, v0));
			Assert.Equal((1.5f / n, 0.5f / n), (u1, v1));
			Assert.Equal((u0, v0), (u2, v2));
			Assert.Equal(2, p.Count);
			Assert.Equal(new byte[] { 0x44, 0x55, 0x66, 255 }, p.Rgba[4..8]);
			for (int i = 2; i < n * n; i++) Assert.True(p.UvOf(0x10000 + i, out _, out _));
			Assert.True(p.UvOf(0x112233, out _, out _));    // a colour it has
			Assert.False(p.UvOf(0x0F0F0F, out _, out _));   // full
		}

		[Fact]
		public void FoldedTexturesShareOnePaletteBatchAndTakeTheirPaletteUv() {
			// three quads: textures 5 and 7 fold into the palette, texture 9 does not
			var o = new SceneObject { Kind = 0, N = 3, D = new double[48] };
			double[] q = { 0, 0, 0, 1, 0, 0, 1, 1, 0, 0, 1, 0 };
			for (int i = 0; i < 3; i++) {
				for (int k = 0; k < 12; k++) o.D[i * 16 + k] = q[k] + (k % 3 == 2 ? i : 0);
				o.D[i * 16 + 12] = 5 + 2 * i; o.D[i * 16 + 13] = 2; o.D[i * 16 + 14] = 3; o.D[i * 16 + 15] = 0.5;
			}
			var fold = new Dictionary<int, (float u, float v)> { [5] = (0.1f, 0.2f), [7] = (0.3f, 0.4f) };
			var dst = new List<float>(); var batches = new List<GpuRasterizer.OBatch>();
			var b = new GpuRasterizer.Batcher { Batches = batches, Dst = dst, Mode = -1, Tex = int.MinValue, Fold = fold };
			GpuRasterizer.Walk(o, ref b); b.Flush();
			Assert.Equal(2, batches.Count);
			Assert.Equal((1, GpuRasterizer.PAL_TEX, 0, 12, true), (batches[0].Mode, batches[0].TexId, batches[0].Start, batches[0].Count, batches[0].Pal));
			Assert.Equal((1, 9, 12, 6, false), (batches[1].Mode, batches[1].TexId, batches[1].Start, batches[1].Count, batches[1].Pal));
			var plain = new List<float>(); var pb = new List<GpuRasterizer.OBatch>();
			var b2 = new GpuRasterizer.Batcher { Batches = pb, Dst = plain, Mode = -1, Tex = int.MinValue };
			GpuRasterizer.Walk(o, ref b2); b2.Flush();
			Assert.Equal(plain.Count, dst.Count);
			for (int v = 0; v < 18; v++)
				for (int c = 0; c < 9; c++) {
					float want = plain[v * 9 + c];
					if (c == 3 && v < 12) want = v < 6 ? 0.1f : 0.3f;   // every corner of a folded quad takes its colour's uv
					if (c == 4 && v < 12) want = v < 6 ? 0.2f : 0.4f;
					Assert.Equal(want, dst[v * 9 + c]);
				}
		}

		// a light matrix shaped like BuildLightVP's: an orthonormal frame looking down the sun, +-half across, a depth range
		private static float[] LightMatrix(Random rng) {
			double lx = rng.NextDouble() * 2 - 1, ly = rng.NextDouble() * 0.9 + 0.1, lz = rng.NextDouble() * 2 - 1;
			double ll = Math.Sqrt(lx * lx + ly * ly + lz * lz); lx /= ll; ly /= ll; lz /= ll;
			double fX = -lx, fY = -ly, fZ = -lz;
			double upy = 1, upz = 0; if (Math.Abs(fY) > 0.95) { upy = 0; upz = 1; }
			double rX = fY * upz - fZ * upy, rY = -fX * upz, rZ = fX * upy;
			double rl = Math.Sqrt(rX * rX + rY * rY + rZ * rZ); rX /= rl; rY /= rl; rZ /= rl;
			double uX = rY * fZ - rZ * fY, uY = rZ * fX - rX * fZ, uZ = rX * fY - rY * fX;
			double sH = 1.0 / (5 + rng.NextDouble() * 40), sD = 2.0 / 260;
			double er = rng.NextDouble() * 20 - 10, eu = rng.NextDouble() * 20 - 10, ef = rng.NextDouble() * 20 - 10;
			return new[] {
				(float)(rX * sH), (float)(uX * sH), (float)(fX * sD), 0,
				(float)(rY * sH), (float)(uY * sH), (float)(fY * sD), 0,
				(float)(rZ * sH), (float)(uZ * sH), (float)(fZ * sD), 0,
				(float)(-er * sH), (float)(-eu * sH), (float)(-ef * sD - 1.0), 1,
			};
		}

		[Fact]
		public void ShadowMendRectHoldsEveryCornerOfACaster() {
			var rng = new Random(5);
			GpuRasterizer.TexelRect(-1, -1, 1, 1, out var full);
			int n = full.X1, corners = 0;
			for (int t = 0; t < 400; t++) {
				var m = LightMatrix(rng);
				int tris = 1 + rng.Next(6);
				var o = new SceneObject { Kind = 2, N = tris, D = new double[tris * 17] };
				for (int i = 0; i < tris; i++)
					for (int c = 0; c < 3; c++)
						for (int a = 0; a < 3; a++) o.D[i * 17 + c * 5 + a] = rng.NextDouble() * 4 - 2;
				if (rng.Next(2) == 0) for (int i = 0; i < tris; i++) o.GrowBounds(i * 17, 3, 5);   // the automatic bounds
				else { o.BoundsSet = true; o.HasBounds = true; }   // bounds the stage set (empty here): the primitives decide
				if (rng.Next(3) > 0) {   // turned, scaled and moved
					double yaw = rng.NextDouble() * 6.3, sc = 0.2 + rng.NextDouble() * 3, cy = Math.Cos(yaw) * sc, sy = Math.Sin(yaw) * sc;
					o.Transform = new[] { cy, 0, sy, rng.NextDouble() * 20 - 10, 0, sc * (0.5 + rng.NextDouble()), 0, rng.NextDouble() * 4, -sy, 0, cy, rng.NextDouble() * 20 - 10, 0, 0, 0, 1 };
				}
				bool on = GpuRasterizer.ObjTexels(m, o, out var r);
				for (int k = 0; k < tris * 17; k += 17)
					for (int c = 0; c < 15; c += 5) {
						double x = o.D[k + c], y = o.D[k + c + 1], z = o.D[k + c + 2];
						var T = o.Transform;
						if (T != null) (x, y, z) = (T[0] * x + T[1] * y + T[2] * z + T[3], T[4] * x + T[5] * y + T[6] * z + T[7], T[8] * x + T[9] * y + T[10] * z + T[11]);
						double tx = ((m[0] * x + m[4] * y + m[8] * z + m[12]) * 0.5 + 0.5) * n, ty = ((m[1] * x + m[5] * y + m[9] * z + m[13]) * 0.5 + 0.5) * n;
						if (tx < 0 || tx >= n || ty < 0 || ty >= n) continue;   // off the map
						Assert.True(on);
						int px = (int)Math.Floor(tx), py = (int)Math.Floor(ty);
						// the corner's texel and its neighbours, which an edge through the corner may still cover
						Assert.True(Math.Max(0, px - 1) >= r.X0 && Math.Min(n - 1, px + 1) < r.X1);
						Assert.True(Math.Max(0, py - 1) >= r.Y0 && Math.Min(n - 1, py + 1) < r.Y1);
						corners++;
					}
			}
			Assert.True(corners > 500);
		}

		[Fact]
		public void JoinedMendRectsCoverTheirInputsAndStayApart() {
			var rng = new Random(9);
			for (int t = 0; t < 300; t++) {
				var input = new List<GpuRasterizer.SRect>(); var joined = new List<GpuRasterizer.SRect>();
				int count = 1 + rng.Next(12);
				for (int i = 0; i < count; i++) {
					int x = rng.Next(200), y = rng.Next(200);
					var r = new GpuRasterizer.SRect { X0 = x, Y0 = y, X1 = x + 1 + rng.Next(40), Y1 = y + 1 + rng.Next(40) };
					input.Add(r); GpuRasterizer.JoinRect(joined, r);
				}
				foreach (var r in input) Assert.Contains(joined, j => j.X0 <= r.X0 && j.Y0 <= r.Y0 && j.X1 >= r.X1 && j.Y1 >= r.Y1);
				for (int a = 0; a < joined.Count; a++)
					for (int b = a + 1; b < joined.Count; b++) {
						var p = joined[a]; var q = joined[b];
						Assert.True(p.X0 > q.X1 || q.X0 > p.X1 || p.Y0 > q.Y1 || q.Y0 > p.Y1);   // neither overlapping nor touching
					}
			}
		}
	}
}
