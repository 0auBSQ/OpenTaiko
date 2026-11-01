using System;
using System.Collections.Generic;
using OpenTaiko;
using Xunit;

namespace OpenTaikoTests {
	// CPU rasterizer helpers that run without GL: the row bands the screen-space triangles are sorted into, and the
	// particle life curves both rasterizers share.
	public class CpuRasterizerTests {
		[Theory]
		[InlineData(100, 1)]
		[InlineData(100, 4)]
		[InlineData(97, 7)]
		[InlineData(5, 8)]   // more bands than rows: some bands hold no rows
		public void BinsHoldTheTrianglesOfTheirRowsInDrawOrder(int h, int bands) {
			var r = new Random(h * 31 + bands);
			var b = new Builder { TriN = 300 };
			for (int i = 0; i < b.TriN; i++) {
				double y = r.NextDouble() * (h + 40) - 20;   // some above or below the screen
				b.Tris[i] = new RTri { Y0 = y, Y1 = y + r.NextDouble() * h * 0.4, Y2 = y - r.NextDouble() * 6 };
			}
			b.Bin(Rasterizer.RowBands(Array.Empty<int>(), bands, h), bands, h);
			for (int band = 0; band < bands; band++) {
				int lo = band * h / bands, hi = (band + 1) * h / bands - 1;
				if (lo > hi) continue;   // a band without rows draws nothing, whatever it holds
				var want = new List<int>();
				for (int i = 0; i < b.TriN; i++) {
					ref RTri t = ref b.Tris[i];
					int y0 = Math.Max(0, (int)Math.Floor(Math.Min(t.Y0, Math.Min(t.Y1, t.Y2))));
					int y1 = Math.Min(h - 1, (int)Math.Ceiling(Math.Max(t.Y0, Math.Max(t.Y1, t.Y2))));
					if (y0 <= y1 && y0 <= hi && y1 >= lo) want.Add(i);
				}
				Assert.Equal(want.ToArray(), b.BinIdx[b.BinStart[band]..b.BinStart[band + 1]]);
			}
		}

		[Fact]
		public void ParticleCurvesFollowTheLife() {
			var p = new Particle { Size0 = 2, Size1 = 6, Rot = 0.5, RotVel = 2, Life = 0.25, MaxLife = 1 };
			Assert.Equal(0.25, p.LifeLeft);
			Assert.Equal(0.25, p.FadeAt(0.25));           // linear fade
			Assert.Equal(5.0, p.SizeAt(0.25));            // three quarters of the way from Size0 to Size1
			Assert.Equal(0.5 + 2 * 0.75, p.Angle);        // turned by the spin over the 0.75 s lived
			p.SizeCurve = 2;                              // pop: the smaller size at birth and at death
			Assert.Equal(2.0, p.SizeAt(1.0), 9);
			Assert.Equal(2.0, p.SizeAt(0.0), 9);
			Assert.Equal(6.0, p.SizeAt(1.0 - Math.Pow(0.5, 1 / 0.75)), 9);
		}
	}
}
