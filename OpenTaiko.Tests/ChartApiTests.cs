using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenTaiko;
using Xunit;

namespace OpenTaikoTests {
	// Unit tests for the new song/chart Lua-facing APIs: decimal difficulty (issue 3) and per-song/per-difficulty
	// playcount (issue 2). The pure helpers need no game state; the parse test uses the shared TjaFixture.
	public class ChartApiTests {

		// ── decimal difficulty: first-decimal digit, truncated (issue 3) ─────────────
		[Theory]
		[InlineData(12.888, 8)]   // the example from the request
		[InlineData(8.0, 0)]      // whole number → 0
		[InlineData(12.5, 5)]
		[InlineData(9.999, 9)]
		[InlineData(10.1, 1)]
		[InlineData(0.0, 0)]
		[InlineData(-1.0, 0)]     // unset/negative guarded to 0
		public void FirstDecimalDigit_TruncatesTenths(double level, int expected) {
			Assert.Equal(expected, LuaSongChart.FirstDecimalDigit(level));
		}

		// ── playcount summed across mod variants, per song + per difficulty (issue 2) ─
		[Fact]
		public void SumPlayCount_SumsAcrossModVariants_PerSongPerDifficulty() {
			var recs = new List<BestPlayRecords.CBestPlayRecord> {
				new() { ChartUniqueId = "A", ChartDifficulty = 3, PlayMods = 0, PlayCount = 5 },
				new() { ChartUniqueId = "A", ChartDifficulty = 3, PlayMods = 1, PlayCount = 2 },  // same chart+diff, other mods
				new() { ChartUniqueId = "A", ChartDifficulty = 2, PlayMods = 0, PlayCount = 9 },  // other difficulty
				new() { ChartUniqueId = "B", ChartDifficulty = 3, PlayMods = 0, PlayCount = 4 },  // other song
			};
			Assert.Equal(7, BestPlayRecords.SumPlayCount(recs, "A", 3));   // 5 + 2, mods ignored
			Assert.Equal(9, BestPlayRecords.SumPlayCount(recs, "A", 2));
			Assert.Equal(4, BestPlayRecords.SumPlayCount(recs, "B", 3));
			Assert.Equal(0, BestPlayRecords.SumPlayCount(recs, "A", 0));   // difficulty never played
			Assert.Equal(0, BestPlayRecords.SumPlayCount(recs, "C", 3));   // song never played
			Assert.Equal(0, BestPlayRecords.SumPlayCount(null, "A", 3));   // null-safe
		}
	}

	// The LEVEL decimal must survive the .tja parse into the course metadata (issue 3, source side).
	[Collection("tja")]
	public class ChartLevelDecimalParseTests : IClassFixture<TjaFixture> {
		public ChartLevelDecimalParseTests(TjaFixture _) { }

		[Fact]
		public void Level_DecimalParsedTruncatedAndIcon() {
			string content = string.Join("\n",
				"TITLE:deci",
				"BPM:120",
				"WAVE:none.ogg",
				"COURSE:Oni",
				"LEVEL:12.888",
				"#START",
				"1000,",
				"#END");
			var tja = Parse(content, Difficulty.Oni);
			var md = tja.SongListCourseMetadata[(int)Difficulty.Oni];

			Assert.Equal(12, md.LEVELtaiko);                         // truncated int kept as before
			Assert.Equal(12.888, md.LEVELtaikoDecimal, 3);           // fraction now preserved
			Assert.Equal(CTja.ELevelIcon.ePlus, md.LEVELtaikoIcon);  // .888 >= .5 → plus (unchanged behaviour)
			Assert.Equal(8, LuaSongChart.FirstDecimalDigit(md.LEVELtaikoDecimal));
		}

		private static CTja Parse(string content, Difficulty difficulty) {
			string dir = Path.Combine(Path.GetTempPath(), "ot_deci_" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(dir);
			try {
				string p = Path.Combine(dir, "deci.tja");
				File.WriteAllText(p, content);
				var tja = new CTja();
				tja.Activate();
				tja.tInput(p, difficulty, 0, false, 0);   // loadChart=false: metadata only
				return tja;
			} finally { try { Directory.Delete(dir, true); } catch { } }
		}
	}

	// .tci carries "level" as a JSON double, so it must keep the fraction and derive + AND − (it only did + before).
	[Collection("tja")]
	public class TciLevelDecimalTests : IClassFixture<TjaFixture> {
		public TciLevelDecimalTests(TjaFixture _) { }

		[Fact]
		public void Tci_KeepsDecimalAndDerivesPlusMinusNone() {
			// dir must outlive BuildSongListNode (it lazily reads the .tci FileInfo.Length)
			string dir = Path.Combine(Path.GetTempPath(), "ot_tci_" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(dir);
			try {
				string json = @"{
  ""title"": ""tcitest"", ""bpm"": 120,
  ""courses"": [
    { ""difficulty"": ""oni"",    ""level"": 12.888, ""single"": ""o.osu"" },
    { ""difficulty"": ""hard"",   ""level"": 10.3,   ""single"": ""h.osu"" },
    { ""difficulty"": ""normal"", ""level"": 8,      ""single"": ""n.osu"" }
  ]
}";
				string p = Path.Combine(dir, "chart.tci");
				File.WriteAllText(p, json);
				var tci = new CTci(p);

				var oni = tci.Courses.First(c => c.DifficultyIndex == (int)Difficulty.Oni);
				Assert.Equal(12, oni.Level);
				Assert.Equal(12.888, oni.LevelDecimal, 3);
				Assert.Equal(CTja.ELevelIcon.ePlus, oni.LevelIcon);    // .888 ≥ .5 → +

				var hard = tci.Courses.First(c => c.DifficultyIndex == (int)Difficulty.Hard);
				Assert.Equal(10.3, hard.LevelDecimal, 3);
				Assert.Equal(CTja.ELevelIcon.eMinus, hard.LevelIcon);  // 0 < .3 < .5 → − (was wrongly none)

				var normal = tci.Courses.First(c => c.DifficultyIndex == (int)Difficulty.Normal);
				Assert.Equal(8.0, normal.LevelDecimal, 3);
				Assert.Equal(CTja.ELevelIcon.eNone, normal.LevelIcon); // whole number → no icon

				var node = tci.BuildSongListNode();
				Assert.Equal(12.888, node.dLevel[(int)Difficulty.Oni], 3);
				Assert.Equal(10.3, node.dLevel[(int)Difficulty.Hard], 3);
			} finally { try { Directory.Delete(dir, true); } catch { } }
		}
	}
}
