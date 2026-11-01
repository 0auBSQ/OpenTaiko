using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenTaiko;
using Xunit;

namespace OpenTaikoTests {
	// AI battle: the AI plays with player 1's modes and seed for the play, and player 2's own modes come back after it
	public class AIBattleModMirrorTests : IDisposable {
		// the mirror keeps player 2's modes in a static field: a test that failed before its Restore must not leave them
		// to the next one
		public void Dispose() => AIBattleModMirror.Restore(new CConfigIni());

		private static void SetModes(CConfigIni cfg, int player, int scroll, EGameType gameType, int zones, int just,
			EStealthMode stealth, ERandomMode random, EFunMods funMod) {
			cfg.nScrollSpeed[player] = scroll;
			cfg.nGameType[player] = gameType;
			cfg.nTimingZones[player] = zones;
			cfg.bJust[player] = just;
			cfg.eSTEALTH[player] = stealth;
			cfg.eRandom[player] = random;
			cfg.nFunMods[player] = funMod;
		}

		private static object ModesOf(CConfigIni cfg, int player) => (cfg.nScrollSpeed[player], cfg.nGameType[player],
			cfg.nTimingZones[player], cfg.bJust[player], cfg.eSTEALTH[player], cfg.eRandom[player], cfg.nFunMods[player]);

		private static CConfigIni MakeConfig(bool aiBattle) {
			var cfg = new CConfigIni();
			cfg.bAIBattleMode = aiBattle;
			SetModes(cfg, 0, 15, EGameType.Konga, 4, 1, EStealthMode.Stealth, ERandomMode.SuperRandom, EFunMods.Minesweeper);
			SetModes(cfg, 1, 7, EGameType.Taiko, 1, 2, EStealthMode.Off, ERandomMode.Mirror, EFunMods.Avalanche);
			return cfg;
		}

		[Fact]
		public void AIBattle_MirrorsEveryModeAndTheSeed_RestoresPlayer2_TwiceSafely() {
			var cfg = MakeConfig(aiBattle: true);
			var player1 = ModesOf(cfg, 0);
			var player2 = ModesOf(cfg, 1);
			var seeds = new[] { 123, 456, 0, 0, 0 };

			AIBattleModMirror.Apply(cfg, seeds);
			AIBattleModMirror.Apply(cfg, seeds); // a second activation keeps player 2's own modes
			Assert.Equal(player1, ModesOf(cfg, 1));
			Assert.Equal(player1, ModesOf(cfg, 0));
			Assert.Equal(new[] { 123, 123, 0, 0, 0 }, seeds);
			Assert.Equal(0, AIBattleModMirror.ModSourceOf(cfg, 1));
			Assert.Equal(0, AIBattleModMirror.ModSourceOf(cfg, 0));

			AIBattleModMirror.Restore(cfg);
			AIBattleModMirror.Restore(cfg);
			Assert.Equal(player2, ModesOf(cfg, 1));
			Assert.Equal(player1, ModesOf(cfg, 0));
		}

		// player 1 changed modes between two plays: the next play mirrors the new ones, player 2's stay its own
		[Fact]
		public void AIBattle_EachPlayMirrorsPlayer1sCurrentModes() {
			var cfg = MakeConfig(aiBattle: true);
			var player2 = ModesOf(cfg, 1);
			var seeds = new[] { 1, 2, 0, 0, 0 };
			AIBattleModMirror.Apply(cfg, seeds);
			AIBattleModMirror.Restore(cfg);

			SetModes(cfg, 0, 9, EGameType.Taiko, 3, 0, EStealthMode.Hidden, ERandomMode.Random, EFunMods.DynamicBeat);
			seeds[0] = 77;
			AIBattleModMirror.Apply(cfg, seeds);
			Assert.Equal(ModesOf(cfg, 0), ModesOf(cfg, 1));
			Assert.Equal(77, seeds[1]);
			AIBattleModMirror.Restore(cfg);
			Assert.Equal(player2, ModesOf(cfg, 1));
		}

		[Fact]
		public void OutsideAIBattle_NothingIsMirroredOrKept() {
			var cfg = MakeConfig(aiBattle: false);
			var player2 = ModesOf(cfg, 1);
			var seeds = new[] { 123, 456, 0, 0, 0 };
			AIBattleModMirror.Apply(cfg, seeds);
			Assert.Equal(player2, ModesOf(cfg, 1));
			Assert.Equal(456, seeds[1]);
			Assert.Equal(1, AIBattleModMirror.ModSourceOf(cfg, 1));

			cfg.nFunMods[1] = EFunMods.None; // a change of player 2's own is not undone by a restore with nothing kept
			AIBattleModMirror.Restore(cfg);
			Assert.Equal(EFunMods.None, cfg.nFunMods[1]);
		}
	}

	// the chart mods' draws: a player's own stream, and the AI's draws over player 1's timeline
	[Collection("tja")]
	public class ChartModDrawsTests : IClassFixture<TjaFixture> {
		public ChartModDrawsTests(TjaFixture _) { }

		private static CChip Chip(double ms, int channel, int idxDefine)
			=> new CChip { nChannelNo = channel, dbSoundTimems = ms, idxDefine = idxDefine };

		// one chip per definition index, at increasing times
		private static List<CChip> Timeline(int count)
			=> Enumerable.Range(0, count).Select(i => Chip(100.0 * i, 0x11, i)).ToList();

		[Fact]
		public void OwnTimeline_IsTheSeedsStream() {
			var chart = Timeline(500);
			var rng = new Random(4242);
			Assert.Equal(Enumerable.Range(0, 500).Select(_ => rng.Next(100)), ChartModDraws.Over(chart, chart, 4242));
		}

		[Fact]
		public void OverAnotherTimeline_EachChipTakesItsTwinsDraw() {
			var player1 = Timeline(200);
			// events only player 1's timeline holds, at their times
			player1.Add(Chip(450.0, 0xDA, 200));
			player1.Add(Chip(5050.0, 0xDB, 201));
			player1.Add(Chip(-50.0, 0xDA, 202));
			player1.Sort();
			var ai = Timeline(200);
			(ai[10], ai[11]) = (ai[11], ai[10]); // another order of the same chips

			var own = ChartModDraws.Over(player1, player1, 99);
			var drawOfIdx = player1.Select((c, i) => (c.idxDefine, own[i])).ToDictionary(x => x.idxDefine, x => x.Item2);
			var draws = ChartModDraws.Over(ai, player1, 99);
			for (int i = 0; i < ai.Count; ++i)
				Assert.Equal(drawOfIdx[ai[i].idxDefine], draws[i]);
			// the same seed over the AI's own timeline is out of step with player 1's from the first extra event
			Assert.NotEqual(draws, ChartModDraws.Over(ai, ai, 99));
		}

		[Fact]
		public void ChipsWithoutATwin_TakeTheDrawsAfterTheTimeline() {
			var player1 = Timeline(50);
			var ai = Timeline(50);
			ai[7].nChannelNo = 0x12; // another channel than its player 1 chip
			ai.Add(Chip(6000.0, 0x11, 60)); // nothing in player 1's timeline

			var rng = new Random(5);
			var timelineDraws = Enumerable.Range(0, player1.Count).Select(_ => rng.Next(100)).ToList();
			var draws = ChartModDraws.Over(ai, player1, 5);
			Assert.Equal(rng.Next(100), draws[7]);
			Assert.Equal(rng.Next(100), draws[50]);
			Assert.Equal(timelineDraws[8], draws[8]);
		}

		// chips that define no index, as a TCI chart's, pair by times and channel, the n-th such chip with the n-th, so the
		// order of chips of one time does not matter
		[Fact]
		public void ChipsDefiningNoIndex_PairByTimesAndChannel() {
			List<CChip> Undefined() => new List<CChip> {
				Chip(0.0, 0x01, -1), Chip(0.0, 0x50, -1), Chip(0.0, 0x11, -1), Chip(100.0, 0x12, -1), Chip(100.0, 0x12, -1),
			};
			var player1 = Undefined();
			player1.Add(Chip(50.0, 0xDA, 5)); // a mixer event
			var ai = Undefined();
			(ai[0], ai[2]) = (ai[2], ai[0]);

			var own = ChartModDraws.Over(player1, player1, 3);
			var draws = ChartModDraws.Over(ai, player1, 3);
			Assert.Equal(new[] { own[2], own[1], own[0], own[3], own[4] }, draws);
		}

		[Fact]
		public void FunModSeed_IsAnotherSeed_UnseededStaysUnseeded() {
			Assert.Equal(-1, ChartModDraws.FunModSeed(-1));
			foreach (int seed in new[] { 0, 1, 123456, int.MaxValue - 1 }) {
				Assert.NotEqual(seed, ChartModDraws.FunModSeed(seed));
				Assert.True(ChartModDraws.FunModSeed(seed) >= 0);
			}
			var chart = Timeline(300);
			Assert.NotEqual(ChartModDraws.Over(chart, chart, 10), ChartModDraws.Over(chart, chart, ChartModDraws.FunModSeed(10)));
		}

		// a placeholder course with notes, big notes, a balloon and branches
		private const string PlaceholderChart =
			"TITLE:Placeholder\nBPM:150\nOFFSET:-1.0\nWAVE:placeholder.ogg\nCOURSE:Oni\nLEVEL:5\nBALLOON:5,5\n\n#START\n"
			+ "1122112211221122,\n3344334411221122,\n7000000800000000,\n1212121212121212,\n"
			+ "#BRANCHSTART p,50,80\n#N\n1111,\n#E\n2222,\n#M\n3344,\n#BRANCHEND\n"
			+ "1111222211112222,\n7000000800000000,\n2121212121212121,\n#END\n";

		private static CTja Parse(int playerSide) {
			string dir = Path.Combine(Path.GetTempPath(), "ot_tja_" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(dir);
			try {
				string path = Path.Combine(dir, "placeholder.tja");
				File.WriteAllText(path, PlaceholderChart);
				var tja = new CTja();
				tja.Activate();
				tja.tInput(path, Difficulty.Oni, playerSide, true, 0);
				return tja;
			} finally { try { Directory.Delete(dir, true); } catch { } }
		}

		// events only player 1's timeline holds, as the mixer plan adds them, with their definition indexes
		private static void AddMixerEvents(CTja player1) {
			int idx = player1.listChip.Count;
			foreach (double ms in new[] { -1000.0, 900.0, 4321.5 })
				player1.listChip.Add(new CChip { nChannelNo = 0xDA, dbSoundTimems = ms, idxDefine = idx++ });
			player1.listChip.Sort();
		}

		// player 1's chart and the AI's are two loads of one course; only player 1's holds the mixer events
		private static (CTja player1, CTja ai) LoadBothSides() {
			OpenTaiko.OpenTaiko.ConfigIni.nPlayerCount = 1;
			var player1 = Parse(0);
			var ai = Parse(1);
			AddMixerEvents(player1);
			return (player1, ai);
		}

		// as the song load does: the draws of both charts first, then the mod on each
		private static void ApplyFunMod(EFunMods funMod, CTja player1, CTja ai, int seed, DBCharacter.CharacterEffect effect) {
			int funSeed = ChartModDraws.FunModSeed(seed);
			var player1Draws = ChartModDraws.Over(player1.listChip, player1.listChip, funSeed);
			var aiDraws = ChartModDraws.Over(ai.listChip, player1.listChip, funSeed);
			player1.tApplyFunMods(funMod, player1Draws, effect);
			ai.tApplyFunMods(funMod, aiDraws, effect);
		}

		private static Dictionary<int, CChip> ByIdx(CTja tja) => tja.listChip.ToDictionary(c => c.idxDefine);

		[Fact]
		public void TwoLoadsOfACourse_DefineTheSameChipsAtTheSameIndexes() {
			var (player1, ai) = LoadBothSides();
			var twins = ByIdx(player1); // throws on a repeated index
			Assert.Equal(ai.listChip.Count, ByIdx(ai).Count);
			foreach (var chip in ai.listChip) {
				var twin = twins[chip.idxDefine];
				Assert.Equal((twin.nChannelNo, twin.dbSoundTimems), (chip.nChannelNo, chip.dbSoundTimems));
			}
		}

		[Fact]
		public void MirroredAI_GetsPlayer1sMines() {
			var (player1, ai) = LoadBothSides();
			var effect = new DBCharacter.CharacterEffect { BombFactor = 40, FuseRollFactor = 50 };
			ApplyFunMod(EFunMods.Minesweeper, player1, ai, 2024, effect);

			var twins = ByIdx(player1);
			Assert.All(ai.listChip, chip => Assert.Equal(twins[chip.idxDefine].nChannelNo, chip.nChannelNo));
			Assert.Contains(ai.listChip, chip => chip.nChannelNo == 0x1C);
			Assert.Contains(ai.listChip, chip => NotesManager.IsMissableNote(chip));
		}

		[Fact]
		public void MirroredAI_GetsPlayer1sAvalanche() {
			var (player1, ai) = LoadBothSides();
			ApplyFunMod(EFunMods.Avalanche, player1, ai, 7, new DBCharacter.CharacterEffect());

			var twins = ByIdx(player1);
			Assert.All(ai.listChip, chip => Assert.Equal(twins[chip.idxDefine].dbSCROLL, chip.dbSCROLL));
			Assert.True(ai.listChip.Select(c => c.dbSCROLL).Distinct().Count() > 1);
		}

		// BD8: the same seed gives the same mines and Avalanche again; another seed gives other ones
		[Fact]
		public void SeededFunMods_AreReproducible() {
			var effect = new DBCharacter.CharacterEffect { BombFactor = 40, FuseRollFactor = 50 };
			List<int> Mines(int seed) {
				var (player1, ai) = LoadBothSides();
				ApplyFunMod(EFunMods.Minesweeper, player1, ai, seed, effect);
				return player1.listChip.Select(c => c.nChannelNo).ToList();
			}
			List<System.Numerics.Complex> Scrolls(int seed) {
				var (player1, ai) = LoadBothSides();
				ApplyFunMod(EFunMods.Avalanche, player1, ai, seed, effect);
				return player1.listChip.Select(c => c.dbSCROLL).ToList();
			}
			Assert.Equal(Mines(31), Mines(31));
			Assert.NotEqual(Mines(31), Mines(32));
			Assert.Equal(Scrolls(31), Scrolls(31));
			Assert.NotEqual(Scrolls(31), Scrolls(32));
		}

		// a placeholder TCI course: notes on the bar lines and between them, and a spinner (a balloon)
		private const string PlaceholderTci = "{ \"title\": \"Placeholder\", \"courses\": [ { \"difficulty\": \"oni\", \"level\": 5, \"single\": \"o.osu\" } ] }";

		private static string PlaceholderOsu()
			=> "osu file format v14\n\n[General]\nMode: 1\n\n[Difficulty]\nSliderMultiplier:1.4\n\n[TimingPoints]\n0,400,4,1,0,100,1,0\n\n[HitObjects]\n"
			+ string.Concat(Enumerable.Range(0, 64).Select(i => $"256,192,{i * 100},1,{2 * (i % 4)}\n"))
			+ "256,192,7000,8,0,8000\n";

		// the multiset of a chart's chips other than the mixer events
		private static List<(int, double, int)> Shape(CTja tja) => tja.listChip.Where(c => c.nChannelNo is not (0xDA or 0xDB))
			.Select(c => (c.nSoundTimems, c.dbSoundTimems, c.nChannelNo)).OrderBy(x => x).ToList();

		// a TCI course defines no chip indexes; two builds of it still get the same mines and fuses
		[Fact]
		public void MirroredAI_GetsPlayer1sMines_OnATciCourse() {
			string dir = Path.Combine(Path.GetTempPath(), "ot_tci_" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(dir);
			try {
				File.WriteAllText(Path.Combine(dir, "chart.tci"), PlaceholderTci);
				File.WriteAllText(Path.Combine(dir, "o.osu"), PlaceholderOsu());
				var tci = new CTci(Path.Combine(dir, "chart.tci"));
				var player1 = tci.BuildCtja((int)Difficulty.Oni);
				var ai = tci.BuildCtja((int)Difficulty.Oni);
				Assert.All(ai.listChip, c => Assert.Equal(-1, c.idxDefine));
				AddMixerEvents(player1);

				var effect = new DBCharacter.CharacterEffect { BombFactor = 40, FuseRollFactor = 100 };
				ApplyFunMod(EFunMods.Minesweeper, player1, ai, 2024, effect);
				Assert.Equal(Shape(player1), Shape(ai));
				Assert.Contains(ai.listChip, c => c.nChannelNo == 0x1C);
				Assert.Contains(ai.listChip, c => c.nChannelNo == 0x1D);
				Assert.Contains(ai.listChip, c => NotesManager.IsMissableNote(c));
			} finally { try { Directory.Delete(dir, true); } catch { } }
		}
	}
}
