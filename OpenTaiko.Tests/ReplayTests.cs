using System;
using System.IO;
using System.Security.Cryptography;
using OpenTaiko;
using Xunit;

namespace OpenTaikoTests {
	// Guards for the replay watch feature: the chart-md5, the on-disk header round-trip (read order must match
	// the write order, incl. seeking past the compressed input log), the ranked listing/filtering, the
	// "watchable" (reproducible-RNG) rule, and — the bug that shipped first — that the Lua-facing ListReplays
	// returns a C# ARRAY (Lua reads .Length / list[i]; a List<> has .Count and would read as empty).
	public class ReplayTests {
		// a throwaway song folder with a dummy .tja; replays save into {dir}/Replay/
		private static (string dir, string tja) NewSong() {
			string dir = Path.Combine(Path.GetTempPath(), "ot_replay_" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(dir);
			string tja = Path.Combine(dir, "chart.tja");
			File.WriteAllText(tja, "#TITLE:test\n#COURSE:Oni\n#START\n#END\n");
			return (dir, tja);
		}

		private static CSongReplay Save(string tja, string uid, int diff, int score, int mods, int seed, long ts, int version = 601) {
			var r = new CSongReplay(tja, 0) {
				GameMode = 0, GameVersion = version, PlayerName = "Tester",
				GoodCount = 50, OkCount = 10, BadCount = 2, RollCount = 5, MaxCombo = 60, BoomCount = 1, ADLibCount = 3,
				Score = score, ClearStatus = 2, ScoreRank = 5,
				ModFlags = mods, RandomSeed = seed, Timestamp = ts,
				ChartUniqueID = uid, ChartDifficulty = (byte)diff, ChartLevel = 8,
				CompressedInputs = new byte[] { 9, 8, 7, 6, 5, 4, 3, 2, 1, 0 },
			};
			r.CompressedInputsSize = r.CompressedInputs.Length;
			r.tSaveReplayFile();
			return r;
		}

		[Fact]
		public void Ctor_ComputesChartMd5() {
			var (dir, tja) = NewSong();
			try {
				var r = new CSongReplay(tja, 0);
				string expected = Convert.ToHexString(MD5.HashData(File.ReadAllBytes(tja)));
				Assert.Equal(expected, r.ChartChecksum);
				Assert.NotEqual("", r.ChartChecksum);
			} finally { Directory.Delete(dir, true); }
		}

		[Fact]
		public void SaveThenParseHeader_RoundTripsMetadata() {
			var (dir, tja) = NewSong();
			try {
				Save(tja, "uid-A", 3, 777777, (int)CSongReplay.EModFlag.Mirror, 4242, 123456789L);
				string file = Directory.GetFiles(Path.Combine(dir, "Replay"), "Replay_*.optkr")[0];

				var h = CSongReplay.tParseHeader(file);
				Assert.NotNull(h);
				Assert.Equal(0, h.GameMode);
				Assert.Equal("Tester", h.PlayerName);
				Assert.Equal(777777, h.Score);
				Assert.Equal(2, h.ClearStatus);
				Assert.Equal(5, h.ScoreRank);
				Assert.Equal((int)CSongReplay.EModFlag.Mirror, h.ModFlags);
				Assert.Equal("uid-A", h.ChartUniqueID);   // read correctly AFTER seeking past the compressed blob
				Assert.Equal(3, h.ChartDifficulty);
				Assert.Equal(4242, h.RandomSeed);
				Assert.True(h.Watchable);                 // mirror is deterministic
				// chart md5 captured (written by the ctor) + current-version replay carries no warning
				string md5 = Convert.ToHexString(MD5.HashData(File.ReadAllBytes(tja)));
				Assert.Equal(md5, h.ChartChecksum);
				Assert.False(h.OldVersion);
				// judge counts (shown on the best-plays card)
				Assert.Equal(50, h.Good); Assert.Equal(10, h.Ok); Assert.Equal(2, h.Bad);
				Assert.Equal(5, h.Roll); Assert.Equal(60, h.MaxCombo); Assert.Equal(1, h.Boom); Assert.Equal(3, h.ADLib);
			} finally { Directory.Delete(dir, true); }
		}

		[Fact]
		public void ListReplays_FiltersByChartAndDifficulty_AndRanksByScore() {
			var (dir, tja) = NewSong();
			try {
				Save(tja, "uid-A", 3, 100, 0, -1, 1);
				Save(tja, "uid-A", 3, 300, 0, -1, 2);   // highest for A/3
				Save(tja, "uid-A", 3, 200, 0, -1, 3);
				Save(tja, "uid-A", 2, 999, 0, -1, 4);   // different difficulty
				Save(tja, "uid-B", 3, 999, 0, -1, 5);   // different chart

				var list = CSongReplay.tListReplays(dir, "uid-A", 3, 50);
				Assert.Equal(3, list.Count);
				Assert.Equal(300, list[0].Score);       // ranked by score desc
				Assert.Equal(200, list[1].Score);
				Assert.Equal(100, list[2].Score);
			} finally { Directory.Delete(dir, true); }
		}

		[Fact]
		public void ListReplays_IgnoresZeroScore() {
			var (dir, tja) = NewSong();
			try {
				Save(tja, "uid-A", 3, 0, 0, -1, 1);     // empty/incomplete play — must be skipped
				Save(tja, "uid-A", 3, 500, 0, -1, 2);   // real play
				var list = CSongReplay.tListReplays(dir, "uid-A", 3, 50);
				Assert.Single(list);
				Assert.Equal(500, list[0].Score);
			} finally { Directory.Delete(dir, true); }
		}

		[Fact]
		public void ListReplays_CapsToTopN() {
			var (dir, tja) = NewSong();
			try {
				for (int i = 0; i < 5; i++) Save(tja, "uid-A", 3, i * 100, 0, -1, i + 1);
				var list = CSongReplay.tListReplays(dir, "uid-A", 3, 2);
				Assert.Equal(2, list.Count);
				Assert.Equal(400, list[0].Score);
				Assert.Equal(300, list[1].Score);
			} finally { Directory.Delete(dir, true); }
		}

		[Theory]
		[InlineData(0, -1, CSongReplay.STORED_GAME_VERSION, true)]                                    // no mods
		[InlineData((int)CSongReplay.EModFlag.Mirror, -1, CSongReplay.STORED_GAME_VERSION, true)]     // mirror is deterministic
		[InlineData((int)CSongReplay.EModFlag.Random, 123, CSongReplay.STORED_GAME_VERSION, true)]    // random WITH a seed
		[InlineData((int)CSongReplay.EModFlag.Random, -1, CSongReplay.STORED_GAME_VERSION, false)]    // random WITHOUT a seed
		[InlineData((int)CSongReplay.EModFlag.SuperRandom, -1, CSongReplay.STORED_GAME_VERSION, false)]
		[InlineData((int)CSongReplay.EModFlag.Avalanche, 123, CSongReplay.STORED_GAME_VERSION, false)]  // unseeded RNG mod
		[InlineData((int)CSongReplay.EModFlag.DynamicBeat, -1, CSongReplay.STORED_GAME_VERSION, true)]  // deterministic from inputs on the same version
		[InlineData((int)CSongReplay.EModFlag.DynamicBeat, -1, CSongReplay.STORED_GAME_VERSION - 1, false)]  // evaluation logic may differ
		public void IsReplayWatchable_FollowsRngRules(int mods, int seed, int gameVersion, bool expected) {
			Assert.Equal(expected, CSongReplay.tIsReplayWatchable(mods, seed, gameVersion));
		}

		[Fact]
		public void ListReplays_MarksUnreproducibleRngNotWatchable() {
			var (dir, tja) = NewSong();
			try {
				Save(tja, "uid-A", 3, 100, (int)CSongReplay.EModFlag.Random, -1, 1);   // random, no seed
				var list = CSongReplay.tListReplays(dir, "uid-A", 3, 50);
				Assert.Single(list);
				Assert.False(list[0].Watchable);
			} finally { Directory.Delete(dir, true); }
		}

		[Fact]
		public void LuaListReplays_ReturnsArray_SoLuaLengthWorks() {
			var (dir, tja) = NewSong();
			try {
				Save(tja, "uid-A", 3, 100, 0, -1, 1);
				Save(tja, "uid-A", 3, 200, 0, -1, 2);

				var result = new LuaReplayFunc().ListReplays(dir, "uid-A", 3, 50);
				// the Lua side does `list.Length` + `list[i]` — that only works on a C# array, not a List<>
				Assert.True(result.GetType().IsArray);
				Assert.Equal(2, result.Length);
				Assert.Equal(200, result[0].Score);
			} finally { Directory.Delete(dir, true); }
		}

		[Fact]
		public void ListReplays_FlagsChecksumMismatch_WhenChartEdited() {
			var (dir, tja) = NewSong();
			try {
				Save(tja, "uid-A", 3, 100, 0, -1, 1);
				// unchanged chart: no mismatch
				var list = CSongReplay.tListReplays(dir, "uid-A", 3, 50, tja);
				Assert.Single(list);
				Assert.False(list[0].ChecksumMismatch);
				// edit the chart -> md5 differs -> mismatch flagged
				File.AppendAllText(tja, "\n// edited after the play\n");
				list = CSongReplay.tListReplays(dir, "uid-A", 3, 50, tja);
				Assert.True(list[0].ChecksumMismatch);
				// without a chartPath the flag stays off (nothing to compare against)
				list = CSongReplay.tListReplays(dir, "uid-A", 3, 50);
				Assert.False(list[0].ChecksumMismatch);
			} finally { Directory.Delete(dir, true); }
		}

		[Fact]
		public void ListReplays_FlagsOldVersion() {
			var (dir, tja) = NewSong();
			try {
				Save(tja, "uid-A", 3, 100, 0, -1, 1, version: 600);   // pre-601 replay
				var list = CSongReplay.tListReplays(dir, "uid-A", 3, 50);
				Assert.Single(list);
				Assert.True(list[0].OldVersion);
			} finally { Directory.Delete(dir, true); }
		}

		[Fact]
		public void EvaluateWarnings_SetsPlaybackFlags() {
			var (dir, tja) = NewSong();
			try {
				Save(tja, "uid-A", 3, 100, 0, -1, 1);
				string file = Directory.GetFiles(Path.Combine(dir, "Replay"), "Replay_*.optkr")[0];
				var rep = new CSongReplay();
				rep.tLoadReplayFile(file);
				rep.tEvaluateWarnings(tja);
				Assert.False(rep.WarnOldVersion);
				Assert.False(rep.WarnChecksumMismatch);
				File.AppendAllText(tja, "\n// edited\n");
				rep.tEvaluateWarnings(tja);
				Assert.True(rep.WarnChecksumMismatch);
			} finally { Directory.Delete(dir, true); }
		}

		[Fact]
		public void ListReplays_MissingFolder_ReturnsEmpty() {
			var list = CSongReplay.tListReplays(Path.Combine(Path.GetTempPath(), "ot_nope_" + Guid.NewGuid().ToString("N")), "x", 0, 50);
			Assert.Empty(list);
		}
	}
}
