using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using OpenTaiko;
using Xunit;

namespace OpenTaikoTests {
	// ── data-driven TJA chart-parser tests ────────────────────────────────────────────────────────────
	// Each case in TjaCases/ is a .json expectation file pointing at a .tja chart (default: the same
	// basename). The runner parses the chart through the real t入力 path and checks only the keys the
	// case declares. Add a new case = drop in a tja + json pair, no code change.
	//
	// JSON schema (everything optional):
	//   { "tja": "file.tja", "difficulty": 0-4 (default 3 = Oni), "loadChart": true,
	//     "expect": { "title", "bpm",
	//                 "dons", "kas", "bigDons", "bigKas", "rolls", "bigRolls", "balloons", "rollEnds",
	//                 "gogoStarts", "gogoEnds",
	//                 "monotonicTimes": true, "noteGapsMs": [..], "gapToleranceMs": 10 } }
	//
	// CTja is coupled to OpenTaiko statics, so the fixture bootstraps a default config via reflection
	// (the property's setter is private) and a dummy current stage (CTja.Activate dereferences it).
	public class TjaFixture {
		public TjaFixture() {
			var prop = typeof(OpenTaiko.OpenTaiko).GetProperty("ConfigIni",
				BindingFlags.Public | BindingFlags.Static);
			if (prop.GetValue(null) == null)
				prop.SetValue(null, new CConfigIni());
			// any stage whose id != SongLoading keeps Activate() off the Skin-dependent path
			if (OpenTaiko.OpenTaiko.rCurrentStage == null)
				OpenTaiko.OpenTaiko.rCurrentStage = new CStage();
		}
	}

	[Collection("tja")]
	public class TjaParsingTests : IClassFixture<TjaFixture> {
		public TjaParsingTests(TjaFixture _) { }

		private static string CasesDir => Path.Combine(AppContext.BaseDirectory, "TjaCases");

		// chip channels (see NotesManager.cs)
		private static readonly Dictionary<string, int> CountKeys = new() {
			["dons"] = 0x11, ["kas"] = 0x12, ["bigDons"] = 0x13, ["bigKas"] = 0x14,
			["rolls"] = 0x15, ["bigRolls"] = 0x16, ["balloons"] = 0x17, ["rollEnds"] = 0x18,
			["gogoStarts"] = 0x9E, ["gogoEnds"] = 0x9F,
		};

		public static IEnumerable<object[]> Cases()
			=> Directory.GetFiles(CasesDir, "*.json")
				.OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
				.Select(p => new object[] { Path.GetFileNameWithoutExtension(p) });

		[Theory]
		[MemberData(nameof(Cases))]
		public void Case(string name) {
			using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(CasesDir, name + ".json")));
			var root = doc.RootElement;
			string tjaName = root.TryGetProperty("tja", out var t) ? t.GetString() : name + ".tja";
			int difficulty = root.TryGetProperty("difficulty", out var d) ? d.GetInt32() : 3;
			bool loadChart = !root.TryGetProperty("loadChart", out var lc) || lc.GetBoolean();

			var tja = Parse(Path.Combine(CasesDir, tjaName), difficulty, loadChart);
			Assert.NotNull(tja.listChip);   // the parse must at least have survived

			if (!root.TryGetProperty("expect", out var exp)) return;

			if (exp.TryGetProperty("title", out var v))
				Assert.Equal(v.GetString(), tja.TITLE.GetString(""));
			if (exp.TryGetProperty("bpm", out v))
				Assert.Equal(v.GetDouble(), tja.BPM, 3);

			foreach (var (key, channel) in CountKeys) {
				if (!exp.TryGetProperty(key, out v)) continue;
				int actual = tja.listChip.Count(c => c.nChannelNo == channel);
				Assert.True(v.GetInt32() == actual, $"{name}: {key} expected {v.GetInt32()}, got {actual}");
			}

			if (exp.TryGetProperty("monotonicTimes", out v) && v.GetBoolean()) {
				var times = NoteTimes(tja);
				Assert.True(times.Count > 0, $"{name}: no playable notes to check monotonicity on");
				Assert.Equal(times.OrderBy(x => x).ToList(), times);
			}

			if (exp.TryGetProperty("noteGapsMs", out v)) {
				int tol = exp.TryGetProperty("gapToleranceMs", out var tv) ? tv.GetInt32() : 10;
				var times = NoteTimes(tja);
				var gaps = times.Zip(times.Skip(1), (a, b) => b - a).ToList();
				var want = v.EnumerateArray().Select(x => x.GetInt32()).ToList();
				Assert.True(want.Count == gaps.Count,
					$"{name}: expected {want.Count} note gaps, got {gaps.Count} [{string.Join(", ", gaps)}]");
				for (int i = 0; i < want.Count; i++)
					Assert.True(Math.Abs(want[i] - gaps[i]) <= tol,
						$"{name}: gap {i} expected {want[i]}±{tol}ms, got {gaps[i]}ms");
			}
		}

		/// <summary>Parse in a temp copy: t入力 writes a uniqueID.json next to the chart.</summary>
		private static CTja Parse(string tjaPath, int difficulty, bool loadChart) {
			string dir = Path.Combine(Path.GetTempPath(), "ot_tja_" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(dir);
			try {
				string p = Path.Combine(dir, Path.GetFileName(tjaPath));
				File.Copy(tjaPath, p);
				var tja = new CTja();
				tja.Activate();   // allocates listChip & friends (CActivity lifecycle)
				tja.t入力(p, difficulty, 0, loadChart, 0);
				return tja;
			} finally { try { Directory.Delete(dir, true); } catch { } }
		}

		// listChip order (the engine's play order) — monotonicity is checked against it, not re-sorted
		private static List<int> NoteTimes(CTja tja)
			=> tja.listChip.Where(c => c.nChannelNo >= 0x11 && c.nChannelNo <= 0x14)
				.Select(c => c.n発声時刻ms).ToList();
	}

	[CollectionDefinition("tja", DisableParallelization = true)]
	public class TjaCollection { }
}