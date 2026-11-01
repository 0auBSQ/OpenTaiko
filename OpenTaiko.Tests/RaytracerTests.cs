using System;
using System.Collections.Generic;
using OpenTaiko;
using Xunit;

namespace OpenTaikoTests {
	// Path tracer helpers that run without GL: the repeat-wrapped texel lookup, the shared scene snapshot's two-level
	// BVH against brute force, the GPU texture atlas packer and the SDF bounding spheres.
	public class RaytracerTests {
		[Theory]
		[InlineData(0.0, 16, 0)]
		[InlineData(0.5, 16, 8)]
		[InlineData(0.999, 16, 15)]
		[InlineData(1.0, 16, 0)]
		[InlineData(-0.01, 16, 15)]   // just below 0 lands on the last texel, not on texel 0 again
		[InlineData(-1.5, 4, 2)]
		[InlineData(12.25, 4, 1)]
		public void WrapTexelRepeatsWithFloor(double u, int n, int texel) {
			Assert.Equal(texel, Raytracer.WrapTexel(u, n));
		}

		[Fact]
		public void WrapTexelStaysInRangeForNonFiniteCoordinates() {
			foreach (double u in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity, -1e-300, 1e300 }) {
				int x = Raytracer.WrapTexel(u, 7);
				Assert.InRange(x, 0, 6);
			}
		}

		// ── two-level BVH of the shared raytrace snapshot ──
		private sealed class Soup {
			public readonly Dictionary<int, SceneObject> Objects = new();
			private readonly Random _r = new(1234);
			public double R(double a, double b) => a + (b - a) * _r.NextDouble();

			// a triangle object (Kind 2) of n random triangles around (cx, cy, cz)
			public SceneObject Tris(int id, int n, double cx, double cy, double cz, double[] xf = null) {
				var o = new SceneObject { Kind = 2, N = n, D = new double[n * 17], Transform = xf, GeomVersion = 1 };
				Fill(o);
				for (int i = 0; i < n; i++) {
					int k = i * 17;
					o.D[k] += cx; o.D[k + 1] += cy; o.D[k + 2] += cz; o.D[k + 5] += cx; o.D[k + 6] += cy; o.D[k + 7] += cz; o.D[k + 10] += cx; o.D[k + 11] += cy; o.D[k + 12] += cz;
				}
				Objects[id] = o;
				return o;
			}
			// fresh random corners (same count), as a posed model writes them
			public void Fill(SceneObject o) {
				for (int i = 0; i < o.N; i++) {
					int k = i * 17;
					double x = R(-3, 3), y = R(-3, 3), z = R(-3, 3);
					for (int c = 0; c < 3; c++) { o.D[k + c * 5] = x + R(-0.8, 0.8); o.D[k + c * 5 + 1] = y + R(-0.8, 0.8); o.D[k + c * 5 + 2] = z + R(-0.8, 0.8); }
					o.D[k + 15] = -1; o.D[k + 16] = 1;
				}
			}
			public RtScene Update(RtScene rt) {
				rt.Update(Objects, new List<ScenePrimitive>(), new List<SceneMaterial>(), new List<SceneLight>(), new Dictionary<int, TexEntry>());
				return rt;
			}
		}

		private static double[] Xf(double a, double b, double c, double tx, double d, double e, double f, double ty, double g, double h, double k, double tz) =>
			new[] { a, b, c, tx, d, e, f, ty, g, h, k, tz, 0, 0, 0, 1 };

		private static Soup MakeSoup() {
			var s = new Soup();
			s.Tris(1, 300, 0, 0, 0);
			double c = Math.Cos(0.7), sn = Math.Sin(0.7);
			s.Tris(2, 200, 1, 0, 0, Xf(c * 1.5, 0, sn, 2, 0, 0.5, 0, -1, -sn * 1.5, 0, c, 1));   // turned, stretched, moved
			s.Tris(3, 120, 0, 1, 0, Xf(-1, 0, 0, -2, 0, 1, 0, 0, 0, 0, 1, 0.5));                   // mirrored
			s.Tris(4, 60, 0, 0, 1, Xf(1, 0, 0, 0, 0, 0, 0, 0.3, 0, 0, 1, 0));                      // flattened: no inverse
			var q = new SceneObject { Kind = 0, N = 40, D = new double[40 * 16], GeomVersion = 1 };   // quads
			for (int i = 0; i < q.N; i++) {
				int k = i * 16; double x = s.R(-4, 4), y = s.R(-4, 4), z = s.R(-4, 4), w = s.R(0.2, 1.2);
				double[] p = { x, y, z, x + w, y, z, x + w, y + w, z + s.R(-0.3, 0.3), x, y + w, z };
				Array.Copy(p, 0, q.D, k, 12);
				q.D[k + 12] = -1; q.D[k + 13] = 1; q.D[k + 14] = 1; q.D[k + 15] = 1;
			}
			s.Objects[5] = q;
			return s;
		}

		// nearest and any hit by testing every triangle of every instance with the same routines the BVH walk uses
		private static (double t, bool any) Brute(RtScene rt, double ox, double oy, double oz, double dx, double dy, double dz, double tmin, double tmax) {
			var bases = new List<(int tb, int ib)>();
			for (int i = 0; i < rt.NumInst; i++) { int ib = rt.InstOff + i * RtScene.INST; bases.Add(((int)rt.Top[ib + 22], ib)); }
			bases.Sort();
			double best = tmax; bool any = false;
			for (int i = 0; i < bases.Count; i++) {
				int ib = bases[i].ib, end = i + 1 < bases.Count ? bases[i + 1].tb : rt.NumTri;
				RtScene.InstRay(rt.Top, ib, ox, oy, oz, dx, dy, dz, out double lx, out double ly, out double lz, out double vx, out double vy, out double vz);
				double dl = Math.Sqrt(vx * vx + vy * vy + vz * vz);
				for (int k = bases[i].tb; k < end; k++) {
					if (rt.TriHit(k, lx, ly, lz, vx, vy, vz, dl, tmin, tmax, out _, out _, out _)) any = true;
					if (rt.TriHit(k, lx, ly, lz, vx, vy, vz, dl, tmin, best, out double t, out _, out _)) best = t;
				}
			}
			return (best, any);
		}

		private static int CompareWithBruteForce(Soup s, RtScene rt, int rays) {
			int hits = 0;
			for (int i = 0; i < rays; i++) {
				double ox = s.R(-7, 7), oy = s.R(-7, 7), oz = s.R(-7, 7);
				double dx = s.R(-1, 1), dy = s.R(-1, 1), dz = s.R(-1, 1), l = Math.Sqrt(dx * dx + dy * dy + dz * dz);
				dx /= l; dy /= l; dz /= l;
				if (i % 7 == 0) { dx = 0; dy = 0; dz = 1; }   // axis-aligned rays: zero direction components
				double tmax = i % 3 == 0 ? s.R(0.5, 8) : RtScene.NoHit;
				var (bt, bany) = Brute(rt, ox, oy, oz, dx, dy, dz, 1e-4, tmax);
				double t = tmax;
				bool hit = rt.ClosestTri(ox, oy, oz, dx, dy, dz, 1e-4, ref t, out _, out _, out _, out _);
				Assert.Equal(bt < tmax, hit);
				Assert.Equal(bt, t);
				Assert.Equal(bany, rt.AnyTri(ox, oy, oz, dx, dy, dz, 1e-4, tmax, false));
				if (hit) hits++;
			}
			return hits;
		}

		[Fact]
		public void BvhClosestAndAnyHitMatchBruteForce() {
			var s = MakeSoup();
			var rt = s.Update(new RtScene());
			Assert.Equal(5, rt.NumInst);
			Assert.Equal(300 + 200 + 120 + 60 + 80, rt.NumTri);
			Assert.True(CompareWithBruteForce(s, rt, 3000) > 300);
		}

		[Fact]
		public void PosedGeometryRefitsAndMovesOnlyTheTopLevel() {
			var s = MakeSoup();
			var rt = s.Update(new RtScene());
			rt.ClearChanges();
			int builds = rt.Builds;

			// new corners, same triangle count: the tree is refitted (or rebuilt if that made it much worse) in place
			var o = s.Objects[1];
			for (int i = 0; i < o.N * 17; i++) if (i % 17 < 15 && i % 17 % 5 < 3) o.D[i] += s.R(-0.05, 0.05);
			o.GeomVersion++;
			s.Update(rt);
			Assert.Equal(1, rt.Refits + rt.Builds - builds);
			Assert.False(rt.GeoFull);
			Assert.Equal(2, rt.GeoRanges.Count);   // that object's nodes and triangles
			CompareWithBruteForce(s, rt, 1500);
			rt.ClearChanges();

			// a move changes no object's triangles: only the instances and the top level
			int refits = rt.Refits; builds = rt.Builds;
			s.Objects[2].Transform[3] += 1.5;
			s.Update(rt);
			Assert.Equal((builds, refits), (rt.Builds, rt.Refits));
			Assert.False(rt.GeoFull);
			Assert.Empty(rt.GeoRanges);
			Assert.True(rt.TopChanged);
			CompareWithBruteForce(s, rt, 1500);
			rt.ClearChanges();

			// nothing changed: nothing to upload
			s.Update(rt);
			Assert.False(rt.Changed);
		}

		[Fact]
		public void ShowingAnObjectAgainRebuildsTheTopLevel() {
			// two objects of the same size in different places, shown in turn: [1, 2], then [1, 3], then [1, 2] again
			var s = new Soup();
			s.Tris(1, 80, 0, 0, 0);
			s.Tris(2, 40, 0, 0, 0, Xf(1, 0, 0, 3, 0, 1, 0, 0, 0, 0, 1, 0));
			s.Tris(3, 40, 0, 0, 0, Xf(1, 0, 0, -3, 0, 1, 0, 2, 0, 0, 1, 0));
			s.Objects[3].Visible = false;
			var rt = s.Update(new RtScene());
			s.Objects[2].Visible = false; s.Objects[3].Visible = true;
			s.Update(rt);
			s.Objects[2].Visible = true; s.Objects[3].Visible = false;
			s.Update(rt);
			var fresh = s.Update(new RtScene());
			Assert.Equal(fresh.Geo[..fresh.GeoLen], rt.Geo[..rt.GeoLen]);
			Assert.Equal(fresh.Top[..fresh.TopLen], rt.Top[..rt.TopLen]);
			CompareWithBruteForce(s, rt, 500);
		}

		[Fact]
		public void AtlasPackerPlacesEveryRectInsideWithoutOverlap() {
			var r = new Random(7);
			int m = 60;
			var pw = new int[m]; var ph = new int[m]; var px = new int[m]; var py = new int[m];
			for (int i = 0; i < m; i++) { pw[i] = 1 << r.Next(2, 8); ph[i] = r.Next(4, 200); }
			Assert.True(GpuRaytracer.Pack(pw, ph, m, 2048, px, py, out int aw, out int ah));
			Assert.InRange(aw, 1, 2048); Assert.InRange(ah, 1, 2048);
			for (int i = 0; i < m; i++) {
				Assert.True(px[i] >= 0 && py[i] >= 0 && px[i] + pw[i] <= aw && py[i] + ph[i] <= ah);
				for (int j = 0; j < i; j++)
					Assert.False(px[i] < px[j] + pw[j] && px[j] < px[i] + pw[i] && py[i] < py[j] + ph[j] && py[j] < py[i] + ph[i]);
			}
			Assert.False(GpuRaytracer.Pack(new[] { 4096 }, new[] { 16 }, 1, 2048, px, py, out _, out _));   // wider than the limit
		}

		// ── holes of textures and sprites ──
		private static bool Hits(RtScene rt, double ox, double oy, double oz, double dx, double dy, double dz) {
			double t = RtScene.NoHit;
			bool closest = rt.ClosestTri(ox, oy, oz, dx, dy, dz, 1e-4, ref t, out _, out _, out _, out _);
			Assert.Equal(closest, rt.AnyTri(ox, oy, oz, dx, dy, dz, 1e-4, RtScene.NoHit, false));
			return closest;
		}

		[Fact]
		public void NegativeTexelsOfATextureAreHoles() {
			// a 2x2 quad in the z = 0 plane, u running along x: its texture's left texel is cut out
			var q = new SceneObject { Kind = 0, N = 1, D = new double[] { -1, -1, 0, 1, -1, 0, 1, 1, 0, -1, 1, 0, 7, 1, 1, 1 }, GeomVersion = 1 };
			var objects = new Dictionary<int, SceneObject> { [1] = q };
			var tex = new Dictionary<int, TexEntry> { [7] = new TexEntry { Pix = new[] { unchecked((int)0x80FF0000), 0x0000FF00 }, W = 2, H = 1, Rev = 1 } };
			var rt = new RtScene();
			rt.Update(objects, new List<ScenePrimitive>(), new List<SceneMaterial>(), new List<SceneLight>(), tex);
			Assert.True(rt.HasCutout);
			Assert.False(Hits(rt, -0.5, 0, 5, 0, 0, -1));
			Assert.True(Hits(rt, 0.5, 0, 5, 0, 0, -1));
		}

		[Fact]
		public void SpritesAreAlphaTestedAndTurnWithTheCamera() {
			// a 2x2 camera-facing sprite standing at the origin; its top-left texel is transparent, another half so
			var spr = new SceneObject { Kind = 3, N = 1, D = new double[] { 0, 0, 0, 2, 2, 3, 2, 1 }, GeomVersion = 1 };
			var flat = new SceneObject { Kind = 3, N = 1, D = new double[] { 0, 0, 0, 2, 2, 3, 3, 1 }, GeomVersion = 1 };   // ground-flat shadow
			// a blended sprite quad lying at y 3 over x, z -1..1 (a draped shadow or decal)
			var quad = new SceneObject { Kind = 4, N = 1, D = new double[] { -1, 3, -1, 1, 3, -1, 1, 3, 1, -1, 3, 1, 3, 0 }, GeomVersion = 1 };
			var objects = new Dictionary<int, SceneObject> { [1] = spr, [2] = flat, [3] = quad };
			var sprites = new Dictionary<int, TexEntry> {
				[3] = new TexEntry { Pix = new[] { 0x00FFFFFF, unchecked((int)0xFFFFFFFF), unchecked((int)0x80FFFFFF), unchecked((int)0xFFFFFFFF) }, W = 2, H = 2, Rev = 1 },
			};
			var rt = new RtScene();
			void Update(double[] cam) => rt.Update(objects, new List<ScenePrimitive>(), new List<SceneMaterial>(), new List<SceneLight>(),
				new Dictionary<int, TexEntry>(), sprites, cam);

			Update(new double[] { 1, 0, 0, 0, 1, 0 });   // facing +z: the sprite spans x -1..1, y 0..2 (the image's top at y 2)
			Assert.Equal(1, rt.NumInst);                  // the ground-flat sprite and the blended sprite quad are not traced
			Assert.False(Hits(rt, 0.5, 5, 0.5, 0, -1, 0));
			Assert.False(Hits(rt, -0.5, 1.5, 5, 0, 0, -1));   // transparent texel
			Assert.True(Hits(rt, 0.5, 1.5, 5, 0, 0, -1));
			Assert.True(Hits(rt, 0.5, 0.5, 5, 0, 0, -1));
			Assert.True(Hits(rt, -0.5, 0.5, 5, 0, 0, -1));    // alpha 0x80 passes the 0.5 cut...
			spr.A = 200;
			Update(new double[] { 1, 0, 0, 0, 1, 0 });
			Assert.False(Hits(rt, -0.5, 0.5, 5, 0, 0, -1));   // ...but not once the object's alpha scales it down
			Assert.True(Hits(rt, 0.5, 0.5, 5, 0, 0, -1));
			spr.A = 100;
			Update(new double[] { 1, 0, 0, 0, 1, 0 });
			Assert.Equal(0, rt.NumInst);                  // faded past half: never hit
			spr.A = 255;

			Update(new double[] { 1, 0, 0, 0, 1, 0 });
			Assert.False(Hits(rt, 5, 0.5, 0.5, -1, 0, 0));
			Update(new double[] { 0, 0, 1, 0, 1, 0 });   // the camera turned: the sprite now spans z -1..1 at x 0
			Assert.True(Hits(rt, 5, 0.5, 0.5, -1, 0, 0));
			Assert.False(Hits(rt, 0.5, 0.5, 5, 0, 0, -1));

			quad.D[13] = 1; quad.GeomVersion++;          // a cut-out sprite quad is traced, alpha-tested like a sprite
			Update(new double[] { 0, 0, 1, 0, 1, 0 });
			Assert.Equal(2, rt.NumInst);
			Assert.True(Hits(rt, 0.5, 5, 0.5, 0, -1, 0));
			Assert.False(Hits(rt, -0.5, 5, 0.5, 0, -1, 0));    // the transparent texel
		}

		[Fact]
		public void EmissiveInstancesBlockShadowRaysUnlessPassed() {
			// a 2x2 quad in the z = 0 plane with an emissive material
			var q = new SceneObject { Kind = 0, N = 1, D = new double[] { -1, -1, 0, 1, -1, 0, 1, 1, 0, -1, 1, 0, -1, 1, 1, 1 }, GeomVersion = 1, Material = 0 };
			var rt = new RtScene();
			rt.Update(new Dictionary<int, SceneObject> { [1] = q }, new List<ScenePrimitive>(), new List<SceneMaterial> { new SceneMaterial { Type = 3 } },
				new List<SceneLight>(), new Dictionary<int, TexEntry>());
			Assert.True(rt.AnyTri(0, 0, 5, 0, 0, -1, 1e-4, RtScene.NoHit, false));
			Assert.False(rt.AnyTri(0, 0, 5, 0, 0, -1, 1e-4, RtScene.NoHit, true));
		}

		[Fact]
		public void SdfBoundingSphereHoldsTheWholeSurface() {
			var prims = new List<ScenePrimitive> {
				new() { Kind = 3, A = 0.85, B = 0.32, C = 1 },
				new() { Kind = 4, SdfPreset = 0, A = 0.9 },
				new() { Kind = 4, SdfPreset = 1, A = 0.75, B = 0.5, C = 1.1 },
				new() { Kind = 4, SdfPreset = 2, A = 1.2, B = 0.3 },
				new() { Kind = 4, SdfPreset = 3, A = 0.8, B = 0.35 },
				new() { Kind = 4, SdfPreset = 4, A = 0.85 },
				new() { Kind = 4, SdfPreset = 5, A = 1.0 },
				new() { Kind = 4, SdfPreset = 6, A = 1.25 },
			};
			var r = new Random(3);
			foreach (var p in prims) {
				double rad = RtScene.SdfBound(p);
				for (int i = 0; i < 4000; i++) {
					double x = r.NextDouble() * 2 - 1, y = r.NextDouble() * 2 - 1, z = r.NextDouble() * 2 - 1, l = Math.Sqrt(x * x + y * y + z * z);
					double k = rad * (1 + r.NextDouble() * 0.5) / Math.Max(l, 1e-9);
					Assert.True(Raytracer.Sdf(p, x * k, y * k, z * k) > 0, $"kind {p.Kind} preset {p.SdfPreset}: surface outside the bound");
				}
			}
		}
	}
}
