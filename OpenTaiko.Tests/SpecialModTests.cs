using System;
using System.IO;
using OpenTaiko;
using Xunit;

namespace OpenTaikoTests {
	// Timed: the table lookup in both directions, the two evaluations and the timer's clock
	public class TimedModTests {
		private static readonly TimedMod.Tables Normal = TimedMod.TablesFor(hard: false, level: 10);
		private static readonly TimedMod.Tables Hard = TimedMod.TablesFor(hard: true, level: 9);
		private static readonly TimedMod.Tables HardTop = TimedMod.TablesFor(hard: true, level: 10);

		[Theory]
		[InlineData(100, 5)]
		[InlineData(90, 5)]
		[InlineData(89.9, 4.5)]
		[InlineData(30, 0)]
		[InlineData(10, 0)]   // under the last row: nothing
		public void Lookup_HigherIsBetter_TakesTheFirstRowReached(double accuracy, double seconds)
			=> Assert.Equal(seconds, TimedMod.Lookup(Normal.Accuracy, accuracy, higherIsBetter: true));

		[Theory]
		[InlineData(0, 4)]
		[InlineData(5, 4)]
		[InlineData(6, 3.5)]
		[InlineData(80, -1)]
		[InlineData(500, -1)]   // the catch-all row
		public void Lookup_LowerIsBetter_TakesTheFirstRowReached(double gap, double seconds)
			=> Assert.Equal(seconds, TimedMod.Lookup(Normal.MinGap, gap, higherIsBetter: false));

		[Fact]
		public void NormalTables_WorsePlayNeverScoresBetter() {
			Assert.Equal(-1, TimedMod.Lookup(Normal.MaxGap, 101, false));
			Assert.Equal(-1.5, TimedMod.Lookup(Normal.Combo, 10, true));
			Assert.Equal(2, TimedMod.Lookup(Normal.Miss, 0, false));
			Assert.Equal(1, TimedMod.Lookup(Normal.Miss, 0.1, false));
			Assert.Equal(-0.5, TimedMod.Lookup(Normal.Miss, 100, false));
			Assert.Equal(-0.5, TimedMod.Lookup(Normal.TotalAccuracy, 29, true));
			Assert.Equal(-1, TimedMod.Lookup(Normal.TotalMaxGap, 300, false));
			Assert.Equal(0, TimedMod.Lookup(Normal.TotalCombo, 40, true));
			Assert.Equal(2, TimedMod.Lookup(Normal.TotalMiss, 0, false));
			Assert.Equal(-0.5, TimedMod.Lookup(Normal.TotalMiss, 71, false));
		}

		[Fact]
		public void HardTables_AreAsWritten() {
			Assert.Equal(3, TimedMod.Lookup(Hard.Accuracy, 100, true));
			Assert.Equal(2, TimedMod.Lookup(Hard.Accuracy, 99, true));
			Assert.Equal(-10, TimedMod.Lookup(Hard.Accuracy, 0, true));
			Assert.Equal(2, TimedMod.Lookup(Hard.MinGap, 0, false));
			Assert.Equal(1, TimedMod.Lookup(Hard.MinGap, 3, false));
			Assert.Equal(-2, TimedMod.Lookup(Hard.MinGap, 4, false));
			Assert.Equal(-4, TimedMod.Lookup(Hard.MinGap, 108, false));
			Assert.Equal(0, TimedMod.Lookup(Hard.MinGap, 109, false));   // no catch-all row on the hard tables
			Assert.Equal(-2, TimedMod.Lookup(Hard.TotalCombo, 99, true));
			Assert.Equal(-2, TimedMod.Lookup(Hard.TotalMiss, 1, false));
		}

		[Fact]
		public void HardTablesOfLevel10_ReplaceSomeTablesAndAreStrict() {
			Assert.False(Normal.Strict);
			Assert.False(Hard.Strict);
			Assert.True(HardTop.Strict);
			Assert.Equal(-6, TimedMod.Lookup(HardTop.Combo, 99, true));
			Assert.Equal(0.5, TimedMod.Lookup(Hard.Combo, 99, true));
			Assert.Same(Hard.MinGap, HardTop.MinGap);
			Assert.Same(Hard.TotalAccuracy, HardTop.TotalAccuracy);
			Assert.Same(Normal, TimedMod.TablesFor(false, 1));
		}

		private static TimedMod.Section Hits(int perfect, int miss, int minGap, int maxGap, int maxCombo)
			=> new() { Perfect = perfect, Miss = miss, Notes = perfect + miss, MinGap = minGap, MaxGap = maxGap, MaxCombo = maxCombo };

		[Fact]
		public void Regular_ABreakGivesFifteenSeconds()
			=> Assert.Equal(15, TimedMod.RegularSeconds(Normal, new TimedMod.Section(), 40, 20, new(40, 0, 40)));

		[Fact]
		public void Regular_APerfectSectionTakesEveryTopRow() {
			var section = Hits(perfect: 20, miss: 0, minGap: 2, maxGap: 30, maxCombo: 20);
			Assert.Equal(29.2, TimedMod.RegularSeconds(Normal, section, 20, 30, new(20, 0, 20)), 6);
			section = Hits(perfect: 10, miss: 0, minGap: 0, maxGap: 3, maxCombo: 10);
			Assert.Equal(20.5, TimedMod.RegularSeconds(Hard, section, 10, 3, new(10, 0, 10)), 6);
		}

		[Fact]
		public void Regular_ASectionOfMissesGivesNothing() {
			var section = Hits(perfect: 0, miss: 10, minGap: -1, maxGap: -1, maxCombo: 0);
			Assert.Equal(0, TimedMod.RegularSeconds(Normal, section, 10, -1, new(0, 10, 0)));
			Assert.Equal(0, TimedMod.RegularSeconds(Hard, section, 10, -1, new(0, 10, 0)));
		}

		// the total accuracy is taken over the notes of the whole play, not of the section
		[Fact]
		public void Regular_TotalAccuracyUsesThePassedNotes() {
			var section = Hits(perfect: 10, miss: 0, minGap: 200, maxGap: 200, maxCombo: 10);
			double low = TimedMod.RegularSeconds(Normal, section, 100, -1, new(55, 0, 0));
			double high = TimedMod.RegularSeconds(Normal, section, 100, -1, new(95, 0, 0));
			Assert.Equal(3.0, high - low, 6);   // 3.5 at 95% against 0.5 at 55%
		}

		[Fact]
		public void Bonus_SixSecondsPerTermMet_MinusTwoForMisses() {
			Assert.Equal(18, TimedMod.BonusSeconds(Normal, Hits(50, 0, 3, 20, 50)));
			Assert.Equal(12, TimedMod.BonusSeconds(Normal, Hits(50, 0, 6, 20, 50)));   // the smallest gap must be 5 ms or less
			Assert.Equal(10, TimedMod.BonusSeconds(Normal, Hits(95, 5, 3, 20, 50)));   // 95%: under 98, and 5% missed
			Assert.Equal(16, TimedMod.BonusSeconds(HardTop, Hits(95, 5, 3, 20, 50)));  // strict tables ask 95%
			Assert.Equal(0, TimedMod.BonusSeconds(Normal, Hits(0, 10, -1, -1, 0)));
		}

		[Theory]
		[InlineData(-9000, 25)]
		[InlineData(0, 25)]
		[InlineData(999, 25)]
		[InlineData(1000, 25)]
		[InlineData(1001, 24)]
		[InlineData(24000, 2)]
		[InlineData(24999, 1)]
		[InlineData(25000, 0)]   // time up
		public void DisplayedSeconds_CountDownFrom25(int msElapsed, int shown)
			=> Assert.Equal(shown, TimedMod.DisplayedSeconds(msElapsed));

		// a player and the chart clock: a note every 500 ms, judged by the given rule
		private sealed class Play {
			public readonly TimedMod Timer;
			public long Ms;
			private int hits, misses;

			public Play(TimedMod.Tables tables) {
				this.Timer = new TimedMod(tables, () => new TimedMod.Totals(this.hits, this.misses, this.hits));
				this.Timer.Start();
				this.Timer.Advance(0);
				this.Timer.SetNotes(true, true);
			}

			public void RunTo(long msEnd, Func<long, ENoteJudge?> judgeAt) {
				for (; this.Ms < msEnd; this.Ms += 100) {
					if (this.Ms % 500 == 0 && judgeAt(this.Ms) is ENoteJudge judge) {
						if (judge is ENoteJudge.Miss) ++this.misses; else ++this.hits;
						this.Timer.Judge(judge, 0, this.Ms);
					}
					this.Timer.Advance(this.Ms);
					this.Timer.SetNotes(true, true);
				}
			}
		}

		[Fact]
		public void Clock_DoesNotRunBeforeTheFirstNote() {
			var timer = new TimedMod(Normal, () => default);
			timer.Advance(0);
			timer.SetNotes(true, true);
			timer.Advance(5000);
			Assert.False(timer.IsRunning);
			Assert.Equal(0, timer.MsElapsed);
		}

		[Fact]
		public void Clock_StopsInABreak_AndIgnoresEarlierTimes() {
			var timer = new TimedMod(Normal, () => default);
			timer.Start();
			timer.Advance(0);
			timer.SetNotes(true, true);
			timer.Advance(3000);
			Assert.Equal(3000, timer.MsElapsed);

			timer.SetNotes(anyNear: false, anyAhead: false);   // no note within 5 s
			timer.Advance(10000);
			Assert.False(timer.IsRunning);
			Assert.Equal(3000, timer.MsElapsed);

			timer.SetNotes(anyNear: false, anyAhead: true);    // a note within 5 s but not within 2 s: still stopped
			Assert.False(timer.IsRunning);
			timer.SetNotes(true, true);
			timer.Advance(11000);
			Assert.Equal(4000, timer.MsElapsed);

			timer.Judge(ENoteJudge.Perfect, 0, 10500);         // a hit stamped before the clock does not move it back
			timer.Advance(11500);
			Assert.Equal(4500, timer.MsElapsed);
		}

		[Fact]
		public void IdlePlayer_IsNotExtended_AndRunsOutOfTime() {
			var play = new Play(Normal);
			play.RunTo(24900, _ => ENoteJudge.Miss);
			Assert.False(play.Timer.IsTimeUp);
			Assert.False(play.Timer.IsJudging);
			play.RunTo(25200, _ => ENoteJudge.Miss);
			Assert.True(play.Timer.IsTimeUp);
			Assert.Equal(TimedMod.MsLimit, play.Timer.MsElapsed);
		}

		[Fact]
		public void GoodPlayer_IsExtended_ThenShownTheAddedSeconds() {
			var play = new Play(Normal);
			play.RunTo(20100, _ => ENoteJudge.Perfect);
			Assert.True(play.Timer.IsJudging);                  // held for 2 s, the digits hidden
			Assert.Null(play.Timer.AddedSeconds);
			Assert.Equal(20000 - 29200, play.Timer.MsElapsed);
			play.RunTo(22000, _ => ENoteJudge.Perfect);
			Assert.Equal(20000 - 29200, play.Timer.MsElapsed);  // not counting while held
			play.RunTo(22200, _ => ENoteJudge.Perfect);
			Assert.False(play.Timer.IsJudging);
			Assert.Equal(29, play.Timer.AddedSeconds);
			play.RunTo(23500, _ => ENoteJudge.Perfect);
			Assert.Null(play.Timer.AddedSeconds);
			Assert.True(play.Timer.IsRunning);
		}

		[Fact]
		public void Bonus_AddsTheTimeItShows() {
			var play = new Play(Normal);
			play.RunTo(20000, _ => ENoteJudge.Miss);            // misses up to the regular evaluation, which gives nothing
			play.RunTo(24100, _ => ENoteJudge.Perfect);         // 1 s left: the bonus, 6 s per term
			Assert.True(play.Timer.IsJudging);
			Assert.Equal(24000 - 18000, play.Timer.MsElapsed);
			play.RunTo(26200, _ => null);
			Assert.Equal(18, play.Timer.AddedSeconds);
		}

		// a bonus of 5 s or less is not applied, and only one is tried between two regular evaluations
		[Fact]
		public void Bonus_OneTryPerInterval_NothingShownWhenNotApplied() {
			var play = new Play(Normal);
			play.RunTo(24100, _ => ENoteJudge.Miss);
			Assert.False(play.Timer.IsJudging);
			Assert.Null(play.Timer.AddedSeconds);
			play.RunTo(25200, _ => ENoteJudge.Perfect);         // perfect hits after the try change nothing
			Assert.True(play.Timer.IsTimeUp);
			Assert.Null(play.Timer.AddedSeconds);
		}

		[Fact]
		public void EachPlayerHasTheirOwnTimer() {
			var idle = new Play(Normal);
			var good = new Play(Normal);
			idle.RunTo(26000, _ => ENoteJudge.Miss);
			good.RunTo(26000, _ => ENoteJudge.Perfect);
			Assert.True(idle.Timer.IsTimeUp);
			Assert.False(good.Timer.IsTimeUp);
			Assert.True(good.Timer.MsElapsed < 0);
		}
	}

	// who is judged under a special mod, the Flawless state per player, and where the choice is stored
	public class SpecialModRulesTests {
		[Fact]
		public void SetSpecialMod_KeepsTheFiveValuesExclusive() {
			var cfg = new CConfigIni();
			Assert.Equal(ESpecialMod.None, cfg.GetSpecialMod(0));

			cfg.SetSpecialMod(0, ESpecialMod.Timed);
			Assert.False(cfg.bAutoPlay[0]);
			Assert.Equal(ESpecialMod.Timed, cfg.GetSpecialMod(0));
			Assert.Equal(ESpecialMod.None, cfg.GetSpecialMod(1));

			cfg.SetSpecialMod(0, ESpecialMod.Auto);
			Assert.True(cfg.bAutoPlay[0]);
			Assert.Equal(ESpecialMod.None, cfg.eSpecialMod[0]);
			Assert.Equal(ESpecialMod.Auto, cfg.GetSpecialMod(0));

			cfg.SetSpecialMod(0, ESpecialMod.Flawless);
			Assert.False(cfg.bAutoPlay[0]);
			cfg.SetSpecialMod(0, ESpecialMod.None);
			Assert.Equal(ESpecialMod.None, cfg.GetSpecialMod(0));
		}

		// the auto flag set by itself (the old Lua setter, the toggle keys) wins over the stored mod and leaves it there
		[Fact]
		public void AutoFlag_HidesTheStoredMod() {
			var cfg = new CConfigIni();
			cfg.SetSpecialMod(0, ESpecialMod.Flawless);
			cfg.bAutoPlay[0] = true;
			Assert.Equal(ESpecialMod.Auto, cfg.GetSpecialMod(0));
			Assert.Equal(ESpecialMod.None, SpecialMods.Judged(cfg, 0, isRemoteSpot: false));
			cfg.bAutoPlay[0] = false;
			Assert.Equal(ESpecialMod.Flawless, cfg.GetSpecialMod(0));
		}

		[Fact]
		public void Judged_EachPlayerUnderTheirOwnMod() {
			var cfg = new CConfigIni();
			cfg.nPlayerCount = 3;
			cfg.SetSpecialMod(0, ESpecialMod.Timed);
			cfg.SetSpecialMod(1, ESpecialMod.Flawless);
			Assert.Equal(ESpecialMod.Timed, SpecialMods.Judged(cfg, 0, false));
			Assert.Equal(ESpecialMod.Flawless, SpecialMods.Judged(cfg, 1, false));
			Assert.Equal(ESpecialMod.None, SpecialMods.Judged(cfg, 2, false));
		}

		[Fact]
		public void Judged_NotForRemoteSpots_TheAI_OrTraining() {
			var cfg = new CConfigIni();
			cfg.SetSpecialMod(0, ESpecialMod.TimedHard);
			cfg.SetSpecialMod(1, ESpecialMod.Flawless);

			Assert.Equal(ESpecialMod.None, SpecialMods.Judged(cfg, 1, isRemoteSpot: true));
			Assert.Equal(ESpecialMod.Flawless, SpecialMods.Judged(cfg, 1, isRemoteSpot: false));

			cfg.bAIBattleMode = true;   // player 2's slot is the AI
			Assert.Equal(ESpecialMod.None, SpecialMods.Judged(cfg, 1, false));
			Assert.Equal(ESpecialMod.TimedHard, SpecialMods.Judged(cfg, 0, false));
			cfg.bAIBattleMode = false;

			cfg.bTokkunMode = true;
			Assert.Equal(ESpecialMod.None, SpecialMods.Judged(cfg, 0, false));
		}

		[Fact]
		public void Flawless_OneMissFailsThatPlayerOnly() {
			var flawless = new FlawlessState();
			flawless.OnMiss(1, ESpecialMod.None);
			flawless.OnMiss(2, ESpecialMod.Timed);
			Assert.False(flawless.IsFailed(1));
			Assert.False(flawless.IsFailed(2));

			flawless.OnMiss(1, ESpecialMod.Flawless);
			Assert.True(flawless.IsFailed(1));
			Assert.False(flawless.IsFailed(0));
			Assert.False(flawless.IsFailed(2));

			flawless.Reset(1);   // a retry
			Assert.False(flawless.IsFailed(1));
		}

		[Theory]
		[InlineData("0", ESpecialMod.None)]
		[InlineData("1", ESpecialMod.None)]   // Auto is the Taiko keys, never this one
		[InlineData("2", ESpecialMod.Flawless)]
		[InlineData("3", ESpecialMod.Timed)]
		[InlineData("4", ESpecialMod.TimedHard)]
		[InlineData("5", ESpecialMod.None)]
		[InlineData("x", ESpecialMod.None)]
		public void ParseSpecialMod_TakesOnlyTheThreeMods(string value, ESpecialMod expected)
			=> Assert.Equal(expected, CConfigIni.ParseSpecialMod(value));

		[Theory]
		[InlineData(ESpecialMod.None)]
		[InlineData(ESpecialMod.Flawless)]
		[InlineData(ESpecialMod.Timed)]
		[InlineData(ESpecialMod.TimedHard)]
		public void ReplayFlags_RoundTripTheMod(ESpecialMod mod) {
			int flags = CSongReplay.SpecialModFlags(mod) | (int)CSongReplay.EModFlag.Mirror | (int)CSongReplay.EModFlag.Flashlight;
			Assert.Equal(mod, CSongReplay.SpecialModOf(flags));
		}

		[Fact]
		public void ReplayFlags_AutoIsNotRecorded() {
			Assert.Equal(0, CSongReplay.SpecialModFlags(ESpecialMod.Auto));
			Assert.Equal(ESpecialMod.None, CSongReplay.SpecialModOf(0x0FFF));   // every flag a 601 file can hold
		}
	}

	[Collection("tja")]   // sequential: temp Config.ini files
	public class SpecialModConfigTests : IClassFixture<TjaFixture> {
		static SpecialModConfigTests() {
			System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
		}
		public SpecialModConfigTests(TjaFixture _) { }

		private static string TempPath() =>
			Path.Combine(Path.GetTempPath(), "ot_cfg_" + Guid.NewGuid().ToString("N") + ".ini");

		[Fact]
		public void SpecialMods_RoundTripThroughConfigIni() {
			var cfg = new CConfigIni();
			cfg.SetSpecialMod(0, ESpecialMod.Flawless);
			cfg.SetSpecialMod(1, ESpecialMod.Auto);
			cfg.SetSpecialMod(3, ESpecialMod.TimedHard);
			string path = TempPath();
			try {
				cfg.tExport(path);
				var loaded = new CConfigIni();
				loaded.LoadFromFile(path);
				Assert.Equal(ESpecialMod.Flawless, loaded.GetSpecialMod(0));
				Assert.Equal(ESpecialMod.Auto, loaded.GetSpecialMod(1));
				Assert.Equal(ESpecialMod.None, loaded.GetSpecialMod(2));
				Assert.Equal(ESpecialMod.TimedHard, loaded.GetSpecialMod(3));
			} finally { File.Delete(path); }
		}

		// a Config.ini written before the special mods: its Risky and GameMode lines are ignored and not written back
		[Fact]
		public void OldRiskyAndGameModeLines_AreIgnoredAndDropped() {
			string path = TempPath();
			try {
				new CConfigIni().tExport(path);
				var encoding = System.Text.Encoding.GetEncoding(OpenTaiko.OpenTaiko.sEncType);
				string text = File.ReadAllText(path, encoding);
				Assert.Contains("DrumsReverse=", text);
				File.WriteAllText(path, text.Replace("DrumsReverse=", "Risky=3\nGameMode=2\nDrumsReverse="), encoding);

				var loaded = new CConfigIni();
				loaded.LoadFromFile(path);
				for (int i = 0; i < 5; ++i)
					Assert.Equal(ESpecialMod.None, loaded.GetSpecialMod(i));

				loaded.tExport(path);
				string written = File.ReadAllText(path, encoding);
				Assert.DoesNotContain("Risky=", written);
				Assert.DoesNotContain("GameMode=", written);
				Assert.Contains("SpecialMod1P=0", written);
			} finally { File.Delete(path); }
		}
	}
}
