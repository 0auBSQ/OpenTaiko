using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenTaiko;
using Xunit;

namespace OpenTaikoTests {
	// the per-frame shortcuts of the play screen answer as the code they stand for
	[Collection("tja")]
	public class PlayFramePathTests : IClassFixture<TjaFixture> {
		public PlayFramePathTests(TjaFixture _) { }

		private static string CasesDir => Path.Combine(AppContext.BaseDirectory, "TjaCases");

		private const string Scrolls = "#START\n1111,\n#SCROLL 2\n1111,\n#BPMCHANGE 180\n#SCROLL 0.5\n11,\n#SCROLL -1\n1111,\n#BPMCHANGE 90\n1,\n#SCROLL 1\n1111,\n#END\n";
		private const string Delays = "#START\n1111,\n#DELAY 0.5\n1111,\n#DELAY -0.25\n11,\n#BPMCHANGE 240\n#DELAY 1\n#DELAY 0.25\n1111,\n#MEASURE 3/4\n111,\n#BPMCHANGE -120\n1111,\n#BPMCHANGE 120\n#MEASURE 4/4\n1111,\n#END\n";
		private const string Branches = "#START\n1111,\n#BRANCHSTART p,50,80\n#N\n#SCROLL 1.5\n1111,\n#BPMCHANGE 150\n1111,\n#E\n#SCROLL 2\n2222,\n2222,\n#M\n#BPMCHANGE 200\n#SCROLL 3\n3333,\n#DELAY 0.2\n3333,\n#BRANCHEND\n#SCROLL 1\n1111,\n#END\n";
		private const string HbScroll = "#HBSCROLL\n#START\n1111,\n#HISPEED 2\n1111,\n#SCROLL 1+1i\n#BPMCHANGE 160\n1111,\n#DELAY 0.3\n#HISPEED -i\n1111,\n#BMSCROLL\n1111,\n#END\n";

		public static IEnumerable<object[]> Charts() {
			string[] compats = { "", "COMPAT:TJAP3\n", "COMPAT:OOS\n", "COMPAT:JIRO1\n", "COMPAT:JIRO2\n", "COMPAT:TMG\n" };
			var bodies = new (string name, string body)[] { ("scrolls", Scrolls), ("delays", Delays), ("branches", Branches), ("hbscroll", HbScroll) };
			foreach (string compat in compats) {
				foreach (var (name, body) in bodies)
					yield return new object[] { $"{name} {compat.Trim()}", $"TITLE:t\nBPM:120\n{compat}COURSE:Oni\n{body}" };
			}
			foreach (string path in Directory.GetFiles(CasesDir, "*.tja").OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
				yield return new object[] { Path.GetFileName(path), File.ReadAllText(path) };
		}

		// every point time, the doubles next to it and between two of them, and a sweep over the chart
		private static List<double> SampleTimes(CTja tja) {
			var times = tja.listBPM.Select(b => b.bpm_change_time).Where(t => !double.IsNaN(t)).Distinct().OrderBy(t => t).ToList();
			var samples = new List<double> { double.NegativeInfinity, double.PositiveInfinity, -1e12, 1e12, 0, -0.0 };
			for (int i = 0; i < times.Count; i++) {
				double t = times[i];
				samples.AddRange(new[] { t, Math.BitDecrement(t), Math.BitIncrement(t), t - 0.5, t + 0.5, t - 1, t + 1 });
				if (i + 1 < times.Count)
					samples.Add(t + (times[i + 1] - t) / 2);
			}
			double lo = times.Where(double.IsFinite).DefaultIfEmpty(0).Min() - 2000, hi = times.Where(double.IsFinite).DefaultIfEmpty(0).Max() + 2000;
			for (double t = lo; t <= hi; t += 7.3)
				samples.Add(t);
			foreach (CChip chip in tja.listChip)
				samples.Add(chip.dbSoundTimems);
			return samples;
		}

		[Theory]
		[MemberData(nameof(Charts))]
		public void BpmPointLookup_AnswersAsGetNowPBPMPoint(string name, string tjaText) {
			var tja = TjaFixture.ParseTjaText("ot_tja_", "chart.tja", tjaText);
			if (tja.listBPM == null || tja.listBPM.Count < 3)
				return; // a chart without its tempo list (the malformed case) is not played
			var lookup = new CStagePlayScreenCommon.BpmPointLookup(tja);
			Assert.True(lookup.IsFor(tja), name);
			var samples = SampleTimes(tja);
			var points = new CTja.CBPM[3];
			// forward, then backward and shuffled, so that answers kept from earlier queries are checked again
			var orders = new List<IEnumerable<double>> { samples, Enumerable.Reverse(samples), samples.OrderBy(t => (t * 7919.0) % 1013.0) };
			foreach (var order in orders) {
				foreach (double t in order) {
					lookup.GetNowPBPMPoints(t, points);
					for (int b = 0; b < 3; b++) {
						var expected = CStagePlayScreenCommon.GetNowPBPMPoint(tja, t, (CTja.ECourse)b);
						Assert.True(ReferenceEquals(expected, points[b]), $"{name}: branch {b} at {t} ms");
					}
				}
			}
		}

		[Fact]
		public void ReusedSearchChip_FindsAsANewOne() {
			var chips = new List<CChip>();
			int idx = 0;
			foreach (double ms in new[] { -5.5, -1, 0, 0, 0.25, 0.75, 1, 1, 2, 10, 10.5, 1e9 }) {
				foreach (int ch in new[] { 0x11, 0x50, 0xDE, 0xDD })
					chips.Add(new CChip { nChannelNo = ch, dbSoundTimems = ms, idxDefine = idx++ });
			}
			chips.Sort();
			var reused = new CChip();
			foreach (long t in new long[] { long.MinValue, int.MinValue, -10, -6, -5, -1, 0, 1, 2, 9, 10, 11, 1_000_000_000, int.MaxValue, long.MaxValue }) {
				foreach (bool allowAt in new[] { true, false }) {
					reused.SetAsSearchChip(t, allowAt);
					int expected = chips.BinarySearch(0, chips.Count, CChip.GetSearchChipAtOrAfter(t, allowAt), Comparer<CChip>.Default);
					int actual = chips.BinarySearch(0, chips.Count, reused, Comparer<CChip>.Default);
					Assert.True(expected == actual, $"at {t} ms, allowAt {allowAt}: {expected} vs {actual}");
				}
			}
		}
	}
}
