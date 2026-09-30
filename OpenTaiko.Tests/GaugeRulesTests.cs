using System;
using System.IO;
using OpenTaiko;
using Xunit;

namespace OpenTaikoTests {
	// Extreme gauge kill zone: perfects over the notes of the route played, times the norma; never NaN, at most the norma
	public class GaugeKillZoneTests {
		private static double Ratio(int level, int perfects, int notes)
			=> HGaugeMethods.tHardGaugeGetKillscreenRatio(Difficulty.Oni, level, HGaugeMethods.EGaugeType.EXTREME, perfects, notes);

		[Fact]
		public void ScalesWithPerfectsOverRouteNotes() {
			Assert.Equal(32.0, Ratio(10, 400, 1000), 3);   // norma 80
			Assert.Equal(46.0, Ratio(12, 500, 1000), 3);   // norma 92
		}

		[Fact]
		public void NoNotes_GivesNoKillZone() {
			Assert.Equal(0.0, Ratio(10, 0, 0));   // was NaN
			Assert.Equal(0.0, Ratio(10, 5, 0));   // was the full norma
		}

		[Fact]
		public void MorePerfectsThanRouteNotes_StopsAtTheNorma() {
			// dropping to a shorter branch can leave more perfects than the route has notes
			Assert.Equal(80.0, Ratio(10, 350, 300), 3);
		}

		[Fact]
		public void OtherGauges_HaveNoKillZone() {
			Assert.Equal(0f, HGaugeMethods.tHardGaugeGetKillscreenRatio(Difficulty.Oni, 10, HGaugeMethods.EGaugeType.HARD, 400, 1000));
			Assert.Equal(0f, HGaugeMethods.tHardGaugeGetKillscreenRatio(Difficulty.Oni, 10, HGaugeMethods.EGaugeType.NORMAL, 400, 1000));
		}

		[Fact]
		public void DangerCheck_WorksWithoutNotes() {
			// no kill zone: danger starts below 30%, as on the Hard gauge
			Assert.False(HGaugeMethods.tIsDangerHardGauge(Difficulty.Oni, 10, HGaugeMethods.EGaugeType.EXTREME, 50f, 0, 0));
			Assert.True(HGaugeMethods.tIsDangerHardGauge(Difficulty.Oni, 10, HGaugeMethods.EGaugeType.EXTREME, 20f, 0, 0));
		}
	}

	// the level of a .tci course reaches the player side, which the gauge rate, damage, norma and kill zone read;
	// the route note counts include the common notes
	[Collection("tja")]
	public class GaugeChartFieldsTests : IClassFixture<TjaFixture> {
		public GaugeChartFieldsTests(TjaFixture _) { }

		[Fact]
		public void Tci_BuildCtjaFillsThePlayerSideLevel() {
			string dir = Path.Combine(Path.GetTempPath(), "ot_tcilv_" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(dir);
			try {
				File.WriteAllText(Path.Combine(dir, "o.osu"), string.Join("\n",
					"osu file format v14",
					"[General]",
					"Mode: 1",
					"[Metadata]",
					"Creator:tester",
					"[TimingPoints]",
					"0,500,4,2,0,100,1,0",
					"[HitObjects]",
					"256,192,1000,1,0",
					"256,192,1500,1,2"));
				string p = Path.Combine(dir, "chart.tci");
				File.WriteAllText(p, @"{ ""title"": ""tcilevel"", ""courses"": [ { ""difficulty"": ""oni"", ""level"": 12.888, ""single"": ""o.osu"" } ] }");

				var tja = new CTci(p).BuildCtja((int)Difficulty.Oni);

				var side = tja.PlayerSideMetadata;
				Assert.Equal(12, side.LEVELtaiko);
				Assert.Equal(12.888, side.LEVELtaikoDecimal, 3);
				Assert.Equal(CTja.ELevelIcon.ePlus, side.LEVELtaikoIcon);
				Assert.Equal(tja.SongListCourseMetadata[(int)Difficulty.Oni].LEVELtaiko, side.LEVELtaiko);

				// no branches in a .tci: every route holds all the notes, so the kill zone denominator does not move
				Assert.Equal(2, tja.nNotesCount_Common);
				Assert.Equal(new[] { 2, 2, 2 }, tja.nNotesCount_Branch);
			} finally { try { Directory.Delete(dir, true); } catch { } }
		}

		[Fact]
		public void Tja_RouteCountsIncludeTheCommonNotes() {
			string dir = Path.Combine(Path.GetTempPath(), "ot_route_" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(dir);
			try {
				string p = Path.Combine(dir, "route.tja");
				File.WriteAllText(p, string.Join("\n",
					"TITLE:route",
					"BPM:120",
					"WAVE:none.ogg",
					"COURSE:Oni",
					"LEVEL:10",
					"#START",
					"1010,",
					"#BRANCHSTART p,50,90",
					"#N",
					"1000,",
					"#E",
					"1100,",
					"#M",
					"1110,",
					"#BRANCHEND",
					"1000,",
					"#END"));
				OpenTaiko.OpenTaiko.ConfigIni.nPlayerCount = 1;
				var tja = new CTja();
				tja.Activate();
				tja.tInput(p, Difficulty.Oni, 0, true, 0);

				Assert.Equal(3, tja.nNotesCount_Common);                  // 2 before the branches, 1 after
				Assert.Equal(new[] { 4, 5, 6 }, tja.nNotesCount_Branch);  // the common notes plus each route's own
			} finally { try { Directory.Delete(dir, true); } catch { } }
		}
	}
}
