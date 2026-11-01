using System;
using System.Collections.Generic;
using System.Linq;
using OpenTaiko;
using Xunit;
using ENoteType = OpenTaiko.NotesManager.ENoteType;

namespace OpenTaikoTests {
	// the timing-zone rule shared by the gameplay judge and the AI hit planner
	public class JudgeWindowsTests {
		private static readonly CConfigIni Config = new CConfigIni();

		private static readonly ENoteType[] NoteTypes = {
			ENoteType.Don, ENoteType.Ka, ENoteType.DonBig, ENoteType.KaBig, ENoteType.DonHand, ENoteType.KaHand,
			ENoteType.Kadon, ENoteType.Adlib, ENoteType.Bomb,
		};

		// the zone rule as evaluateNodeJudge wrote it before it moved to JudgeWindows.Classify
		private static ENoteJudge FormerRule(int msDelta, CConfigIni.CTimingZones tz, int just, ENoteType nt) {
			if (msDelta > tz.nBadZone)
				return ENoteJudge.Miss;
			if (msDelta <= tz.nGoodZone)
				return ENoteJudge.Perfect;
			if (msDelta <= tz.nOkZone) {
				if (just == 1 && NotesManager.IsMissableNote(nt))
					return ENoteJudge.Poor;
				return ENoteJudge.Good;
			}
			if (just == 2 || NotesManager.IsJudgedFromNearest(nt))
				return ENoteJudge.Good;
			return ENoteJudge.Poor;
		}

		internal static IEnumerable<CConfigIni.CTimingZones> AllZones() {
			foreach (var tz in Config.tzLevels) {
				yield return tz;
				// 1.5x song speed, truncated as the play does
				yield return new CConfigIni.CTimingZones((int)(tz.nGoodZone * 1.5), (int)(tz.nOkZone * 1.5), (int)(tz.nBadZone * 1.5));
			}
		}

		[Fact]
		public void Classify_MatchesTheFormerRule() {
			foreach (var tz in AllZones()) {
				for (int just = 0; just <= 2; ++just) {
					foreach (var nt in NoteTypes) {
						var rule = JudgeWindows.NoteRule.Of(nt, tz, just);
						for (int d = 1; d <= 200; ++d)
							Assert.Equal(FormerRule(d, tz, just, nt), JudgeWindows.Classify(d, rule));
					}
				}
			}
		}

		// the judge as eGetChipJudgeAtTimeImpl and evaluateNodeJudge gave it before JudgeWindows, for a missable note
		private static ENoteJudge FormerJudgeAt(long msHit, double msNote, CConfigIni.CTimingZones tz, int just, ENoteType nt) {
			var msDelta = msHit - msNote;
			int msAbsDelta = (int)Math.Abs(msDelta);
			if (msAbsDelta < 0)
				return ENoteJudge.Miss;
			if (msAbsDelta == 0)
				return ENoteJudge.Perfect;
			return FormerRule(msAbsDelta, tz, just, nt);
		}

		[Fact]
		public void JudgeAt_MatchesTheFormerJudge() {
			foreach (var tz in AllZones()) {
				for (int just = 0; just <= 2; ++just) {
					foreach (var nt in NoteTypes) {
						var rule = JudgeWindows.NoteRule.Of(nt, tz, just);
						foreach (double msNote in new[] { 1000.0, 1000.3, 1000.5, 1000.99, -1000.7 }) {
							for (long msHit = (long)msNote - 200; msHit <= (long)msNote + 200; ++msHit)
								Assert.Equal(FormerJudgeAt(msHit, msNote, tz, just, nt), JudgeWindows.JudgeAt(msHit, msNote, rule));
						}
						foreach (long msHit in new[] { long.MaxValue, long.MinValue, (long)int.MaxValue * 4 })
							Assert.Equal(FormerJudgeAt(msHit, 1000.0, tz, just, nt), JudgeWindows.JudgeAt(msHit, 1000.0, rule));
					}
				}
			}
		}

		[Theory]
		[InlineData(1000.0, 1000, ENoteJudge.Perfect)]
		[InlineData(1000.0, 1025, ENoteJudge.Perfect)]
		[InlineData(1000.0, 1026, ENoteJudge.Good)]
		[InlineData(1000.0, 1075, ENoteJudge.Good)]
		[InlineData(1000.0, 1076, ENoteJudge.Poor)]
		[InlineData(1000.0, 1108, ENoteJudge.Poor)]
		[InlineData(1000.0, 1109, ENoteJudge.Miss)]
		[InlineData(1000.0, 975, ENoteJudge.Perfect)]
		[InlineData(1000.0, 974, ENoteJudge.Good)]
		[InlineData(1000.0, 925, ENoteJudge.Good)]
		[InlineData(1000.0, 924, ENoteJudge.Poor)]
		[InlineData(1000.0, 892, ENoteJudge.Poor)]
		[InlineData(1000.0, 891, ENoteJudge.Miss)]
		[InlineData(1000.3, 1000, ENoteJudge.Perfect)]
		[InlineData(1000.7, 1026, ENoteJudge.Perfect)]
		[InlineData(1000.7, 1027, ENoteJudge.Good)]
		[InlineData(1000.7, 975, ENoteJudge.Perfect)]
		[InlineData(1000.7, 974, ENoteJudge.Good)]
		[InlineData(1000.7, 1109, ENoteJudge.Poor)]
		[InlineData(1000.7, 892, ENoteJudge.Poor)]
		[InlineData(1000.7, 891, ENoteJudge.Miss)]
		public void JudgeAt_Level4_TruncatesTheDistance(double msNote, long msHit, ENoteJudge expected) {
			var rule = JudgeWindows.NoteRule.Of(ENoteType.Don, Config.tzLevels[4], 0);
			Assert.Equal(expected, JudgeWindows.JudgeAt(msHit, msNote, rule));
		}

		[Fact]
		public void Zones_AreTheJudgesWithoutMods_JustAndSafeChangeTheirJudge() {
			var tz = Config.tzLevels[4]; // 25/75/108
			Assert.Equal(ENoteJudge.Perfect, JudgeWindows.ZoneOf(25, tz));
			Assert.Equal(ENoteJudge.Good, JudgeWindows.ZoneOf(26, tz));
			Assert.Equal(ENoteJudge.Good, JudgeWindows.ZoneOf(75, tz));
			Assert.Equal(ENoteJudge.Poor, JudgeWindows.ZoneOf(76, tz));
			Assert.Equal(ENoteJudge.Poor, JudgeWindows.ZoneOf(108, tz));
			Assert.Equal(ENoteJudge.Miss, JudgeWindows.ZoneOf(109, tz));

			var just = JudgeWindows.NoteRule.Of(ENoteType.Don, tz, 1);
			var safe = JudgeWindows.NoteRule.Of(ENoteType.Don, tz, 2);
			var mine = JudgeWindows.NoteRule.Of(ENoteType.Bomb, tz, 1);
			Assert.Equal(ENoteJudge.Poor, just.JudgeOfZone(ENoteJudge.Good));
			Assert.Equal(ENoteJudge.Poor, just.JudgeOfZone(ENoteJudge.Poor));
			Assert.Equal(ENoteJudge.Good, safe.JudgeOfZone(ENoteJudge.Poor));
			Assert.Equal(ENoteJudge.Good, mine.JudgeOfZone(ENoteJudge.Good)); // Just only changes missable notes
			Assert.Equal(ENoteJudge.Good, mine.JudgeOfZone(ENoteJudge.Poor));
			Assert.Equal(ENoteJudge.Miss, safe.JudgeOfZone(ENoteJudge.Miss));
		}
	}

	// the AI's zone roll and the hit time picked for it
	public class AIHitTimingTests {
		private static readonly CConfigIni Config = new CConfigIni();
		private static readonly AIHitTiming.HitWindow Open = new(long.MinValue, long.MaxValue, AllowEarly: true);
		private static readonly AIHitTiming.Neighbour[] None = Array.Empty<AIHitTiming.Neighbour>();

		private static AIHitTiming.Neighbour Note(double ms, CConfigIni.CTimingZones tz, ENoteType nt = ENoteType.Don, int just = 0)
			=> new((int)Math.Floor(ms), ms, JudgeWindows.NoteRule.Of(nt, tz, just));

		// offsets from the note of many picked hit times, each checked to land on the note
		private static SortedSet<int> PickedOffsets(AIHitTiming.Neighbour note, ENoteJudge zone, AIHitTiming.HitWindow window,
			AIHitTiming.Neighbour[] before = null, AIHitTiming.Neighbour[] after = null, int count = 20000) {
			var rng = new Random(1);
			var offsets = new SortedSet<int>();
			for (int i = 0; i < count; ++i) {
				long? msHit = AIHitTiming.PickHitTime(note, zone, window, before ?? None, after ?? None, rng);
				Assert.NotNull(msHit);
				Assert.True(AIHitTiming.Lands(msHit.Value, note, window, before ?? None, after ?? None));
				offsets.Add((int)(msHit.Value - note.MsNoteInt));
			}
			return offsets;
		}

		private static SortedSet<int> Range(int from, int to) => new(Enumerable.Range(from, to - from + 1));
		private static SortedSet<int> BothSides(int from, int to) => new(Range(-to, -from).Concat(Range(from, to)));
		private static SortedSet<int> Set(params int[] values) => new(values);

		[Fact]
		public void RollZone_FollowsEachLevelsOdds() {
			foreach (var odds in Config.apAIPerformances) {
				var rolls = Enumerable.Range(0, 1000).Select(dice => AIHitTiming.RollZone(dice, odds.nBadOdds, odds.nGoodOdds)).ToList();
				Assert.Equal(odds.nBadOdds, rolls.Count(z => z == ENoteJudge.Poor));
				Assert.Equal(odds.nGoodOdds, rolls.Count(z => z == ENoteJudge.Good));
				Assert.Equal(1000 - odds.nBadOdds - odds.nGoodOdds, rolls.Count(z => z == ENoteJudge.Perfect));
			}
		}

		// at judge zones level 4 an Ok lands anywhere from 26 to 75 ms off the note, either side
		[Fact]
		public void Level4_Ok_LandsFrom26To75msEarlyOrLate() {
			var tz = Config.tzLevels[4];
			Assert.Equal((25, 75, 108), (tz.nGoodZone, tz.nOkZone, tz.nBadZone));
			Assert.Equal(BothSides(26, 75), PickedOffsets(Note(1000.0, tz), ENoteJudge.Good, Open));
			Assert.Equal(BothSides(76, 108), PickedOffsets(Note(1000.0, tz), ENoteJudge.Poor, Open));
		}

		[Theory]
		[InlineData(0)]
		[InlineData(1)]
		[InlineData(2)]
		[InlineData(3)]
		[InlineData(4)]
		[InlineData(5)]
		[InlineData(6)]
		public void EachZone_CoversItsWholeZone_AndGivesItsJudge(int level) {
			var tz = Config.tzLevels[level];
			var note = Note(1000.0, tz);
			Assert.Equal(Range(-tz.nGoodZone, tz.nGoodZone), PickedOffsets(note, ENoteJudge.Perfect, Open));
			Assert.Equal(BothSides(tz.nGoodZone + 1, tz.nOkZone), PickedOffsets(note, ENoteJudge.Good, Open));
			Assert.Equal(BothSides(tz.nOkZone + 1, tz.nBadZone), PickedOffsets(note, ENoteJudge.Poor, Open));
			foreach (var zone in new[] { ENoteJudge.Perfect, ENoteJudge.Good, ENoteJudge.Poor }) {
				foreach (int d in PickedOffsets(note, zone, Open, count: 2000))
					Assert.Equal(zone, JudgeWindows.JudgeAt(1000 + d, 1000.0, note.Rule));
			}
		}

		[Fact]
		public void SongSpeed_ScaledZones_AreFollowed() {
			var lv2 = Config.tzLevels[2];
			var tz = new CConfigIni.CTimingZones((int)(lv2.nGoodZone * 1.5), (int)(lv2.nOkZone * 1.5), (int)(lv2.nBadZone * 1.5));
			Assert.Equal(BothSides(tz.nGoodZone + 1, tz.nOkZone), PickedOffsets(Note(1000.0, tz), ENoteJudge.Good, Open));
		}

		[Fact]
		public void FractionalNoteTime_EveryPickIsInTheZone() {
			var note = Note(1000.6, Config.tzLevels[4]);
			foreach (var zone in new[] { ENoteJudge.Perfect, ENoteJudge.Good, ENoteJudge.Poor }) {
				var rng = new Random(3);
				for (int i = 0; i < 2000; ++i) {
					long msHit = AIHitTiming.PickHitTime(note, zone, Open, None, None, rng).Value;
					Assert.Equal(zone, JudgeWindows.ZoneOf(JudgeWindows.AbsDeltaMs(msHit, note.MsNote), note.Rule.Zones));
				}
			}
		}

		// with player 1's Just or Safe mirrored, the AI aims at the zone it rolled and the mod judges the hit
		[Fact]
		public void Just_OkZoneHits_AreJudgedBad_Safe_BadZoneHits_AreJudgedOk() {
			var just = Note(1000.0, Config.tzLevels[4], just: 1);
			var okZone = PickedOffsets(just, ENoteJudge.Good, Open);
			Assert.Equal(BothSides(26, 75), okZone);
			Assert.All(okZone, d => Assert.Equal(ENoteJudge.Poor, JudgeWindows.JudgeAt(1000 + d, 1000.0, just.Rule)));

			var safe = Note(1000.0, Config.tzLevels[4], just: 2);
			var badZone = PickedOffsets(safe, ENoteJudge.Poor, Open);
			Assert.Equal(BothSides(76, 108), badZone);
			Assert.All(badZone, d => Assert.Equal(ENoteJudge.Good, JudgeWindows.JudgeAt(1000 + d, 1000.0, safe.Rule)));
		}

		[Fact]
		public void Window_BoundsAreExclusive() {
			var window = new AIHitTiming.HitWindow(970, 1040, AllowEarly: true);
			Assert.Equal(new SortedSet<int>(Range(-29, -26).Concat(Range(26, 39))),
				PickedOffsets(Note(1000.0, Config.tzLevels[4]), ENoteJudge.Good, window));
		}

		[Fact]
		public void Window_WithoutEarly_HitsAtOrAfterTheNote() {
			var window = new AIHitTiming.HitWindow(long.MinValue, long.MaxValue, AllowEarly: false);
			Assert.Equal(Range(0, 25), PickedOffsets(Note(1000.0, Config.tzLevels[4]), ENoteJudge.Perfect, window));
			Assert.Equal(Range(0, 26), PickedOffsets(Note(1000.4, Config.tzLevels[4]), ENoteJudge.Perfect, window));
		}

		[Fact]
		public void UnhitMineBefore_TakesAnEarlyBad() {
			var tz = Config.tzLevels[4];
			var mine = new[] { Note(940.0, tz, ENoteType.Bomb) };
			Assert.Equal(Range(76, 108), PickedOffsets(Note(1000.0, tz), ENoteJudge.Poor, Open, before: mine));
		}

		[Fact]
		public void UnhitMineBefore_TakesAnEarlyHitNearerToIt() {
			var tz = Config.tzLevels[4];
			var mine = new[] { Note(960.0, tz, ENoteType.Bomb) };
			Assert.Equal(Range(26, 75), PickedOffsets(Note(1000.0, tz), ENoteJudge.Good, Open, before: mine));
			Assert.Equal(Range(-19, 25), PickedOffsets(Note(1000.0, tz), ENoteJudge.Perfect, Open, before: mine));
		}

		[Fact]
		public void NextNoteInItsWindow_TakesALateBad() {
			var tz = Config.tzLevels[4];
			var near = new AIHitTiming.HitWindow(long.MinValue, 1100, AllowEarly: true);
			Assert.Equal(Range(-108, -76), PickedOffsets(Note(1000.0, tz), ENoteJudge.Poor, near, after: new[] { Note(1100.0, tz) }));

			var far = new AIHitTiming.HitWindow(long.MinValue, 1190, AllowEarly: true);
			Assert.Equal(BothSides(76, 108), PickedOffsets(Note(1000.0, tz), ENoteJudge.Poor, far, after: new[] { Note(1190.0, tz) }));
		}

		[Fact]
		public void NextAdlibOrMine_RefusesALateBadWithinItsBadZone() {
			var tz = Config.tzLevels[4];
			var window = new AIHitTiming.HitWindow(long.MinValue, 1190, AllowEarly: true);
			var adlib = new[] { Note(1190.0, tz, ENoteType.Adlib) };
			Assert.Equal(new SortedSet<int>(Range(-108, -76).Concat(Range(76, 81))),
				PickedOffsets(Note(1000.0, tz), ENoteJudge.Poor, window, after: adlib));

			// which pad the note takes is not known, so the mine after it is still checked
			var noteThenMine = new[] { Note(1190.0, tz), Note(1200.0, tz, ENoteType.Bomb) };
			Assert.Equal(new SortedSet<int>(Range(-108, -76).Concat(Range(76, 91))),
				PickedOffsets(Note(1000.0, tz), ENoteJudge.Poor, window, after: noteThenMine));
		}

		// a zone without room on its note: the landing times nearest to it
		[Fact]
		public void NoRoomForTheZone_PicksTheLandingTimesNearestToIt() {
			var note = Note(1000.0, Config.tzLevels[4]);
			var tight = new AIHitTiming.HitWindow(990, 1010, AllowEarly: true); // lands from 991 to 1009
			Assert.Equal(Set(-9, 9), PickedOffsets(note, ENoteJudge.Poor, tight));
			Assert.Equal(Set(-9, 9), PickedOffsets(note, ENoteJudge.Good, tight));
			Assert.Equal(Range(-9, 9), PickedOffsets(note, ENoteJudge.Perfect, tight));

			var earlyRoom = new AIHitTiming.HitWindow(960, 1010, AllowEarly: true); // 39 ms early, 9 ms late
			Assert.Equal(Set(-39), PickedOffsets(note, ENoteJudge.Poor, earlyRoom));
			Assert.Equal(Range(-39, -26), PickedOffsets(note, ENoteJudge.Good, earlyRoom));

			var lateOnly = new AIHitTiming.HitWindow(999, 1040, AllowEarly: false);
			Assert.Equal(Set(39), PickedOffsets(note, ENoteJudge.Poor, lateOnly));
		}

		[Fact]
		public void NothingLands_GivesNoTime() {
			var note = Note(1000.0, Config.tzLevels[4]);
			var empty = new AIHitTiming.HitWindow(1000, 1001, AllowEarly: true);
			foreach (var zone in new[] { ENoteJudge.Perfect, ENoteJudge.Good, ENoteJudge.Poor })
				Assert.Null(AIHitTiming.PickHitTime(note, zone, empty, None, None, new Random(1)));
		}

		[Fact]
		public void MsOutsideZone_IsTheDistanceToTheZoneBounds() {
			var tz = Config.tzLevels[4];
			Assert.Equal(0, AIHitTiming.MsOutsideZone(0, ENoteJudge.Perfect, tz));
			Assert.Equal(1, AIHitTiming.MsOutsideZone(26, ENoteJudge.Perfect, tz));
			Assert.Equal(17, AIHitTiming.MsOutsideZone(9, ENoteJudge.Good, tz));
			Assert.Equal(0, AIHitTiming.MsOutsideZone(75, ENoteJudge.Good, tz));
			Assert.Equal(67, AIHitTiming.MsOutsideZone(9, ENoteJudge.Poor, tz));
			Assert.Equal(1, AIHitTiming.MsOutsideZone(109, ENoteJudge.Poor, tz));
		}
	}

	// the per-play scheduling of the AI's hits, against a stand-in play
	public class AIHitPlannerTests {
		private static readonly CConfigIni Config = new CConfigIni();
		private const double MsFrame = 16.7;
		private static readonly CConfigIni.CAIPerformances GoodOnly = new CConfigIni.CAIPerformances(1000, 0, 0, 10);
		private static readonly CConfigIni.CAIPerformances OkOnly = new CConfigIni.CAIPerformances(0, 1000, 0, 10);
		private static readonly CConfigIni.CAIPerformances BadOnly = new CConfigIni.CAIPerformances(0, 0, 1000, 10);

		private sealed class FakeHost : AIHitPlanner.IHost {
			public List<CChip> Chips { get; } = new List<CChip>();
			public int Level { get; set; } = 1;
			public CConfigIni.CTimingZones Zones { get; set; } = Config.tzLevels[4];
			public int JustMode { get; set; }
			public CConfigIni.CAIPerformances Odds { get; set; } = OkOnly;
			public int MsBalloonHeadWindow => 17;
			public double MsPlanLead { get; set; } = AIHitPlanner.PlanLeadGameMs;
			public readonly HashSet<string> MetTriggers = new HashSet<string>();
			public Func<double, bool> Refuses = _ => false; // by hit time
			public double MsNow, MsPrevNow = double.NegativeInfinity;
			public readonly List<double> Attempts = new();
			public readonly List<(CChip chip, long msHit, double msNow, double msPrevNow)> Hits = new();

			public bool IsNoteIfMet(CChip chip) => chip.NoteIfTrigger == null || this.MetTriggers.Contains(chip.NoteIfTrigger);
			public bool TryHit(CChip chip, double msHit) {
				this.Attempts.Add(msHit);
				if (this.Refuses(msHit))
					return false;
				chip.bHit = true;
				this.Hits.Add((chip, (long)msHit, this.MsNow, this.MsPrevNow));
				return true;
			}
		}

		private static CChip Chip(double ms, ENoteType nt = ENoteType.Don)
			=> new CChip { nChannelNo = (int)nt, dbSoundTimems = ms };

		private static (CChip head, CChip end) Roll(double msHead, double msEnd, ENoteType nt = ENoteType.Roll) {
			var head = Chip(msHead, nt);
			var end = Chip(msEnd, ENoteType.EndRoll);
			head.end = end;
			end.start = head;
			return (head, end);
		}

		private static (AIHitPlanner planner, FakeHost host) Make(params CChip[] chips) {
			var host = new FakeHost();
			host.Chips.AddRange(chips);
			return (new AIHitPlanner(host, new Random(7)), host);
		}

		private static void Play(AIHitPlanner planner, FakeHost host, double msFrom, double msTo) {
			for (double ms = msFrom; ms <= msTo; ms += MsFrame) {
				host.MsNow = ms;
				planner.Update(ms);
				host.MsPrevNow = ms;
			}
		}

		// chips compare equal by time, so the hits are checked by reference
		private static void AssertSameChips(IEnumerable<CChip> expected, IEnumerable<CChip> actual) {
			var e = expected.ToList();
			var a = actual.ToList();
			Assert.Equal(e.Count, a.Count);
			for (int i = 0; i < e.Count; ++i)
				Assert.Same(e[i], a[i]);
		}

		// the judge the play gives a hit from its time
		private static ENoteJudge TimingJudgeOf(FakeHost host, CChip chip, long msHit)
			=> JudgeWindows.JudgeAt(msHit, chip.dbSoundTimems, JudgeWindows.NoteRule.Of(chip, host.Zones, host.JustMode));

		private static long HitTimeOf(FakeHost host, CChip chip) => host.Hits.Single(h => ReferenceEquals(h.chip, chip)).msHit;

		[Fact]
		public void EachNote_IsHitOnce_AtItsTarget_InTheFirstFrameReachingIt() {
			var notes = Enumerable.Range(1, 10).Select(i => Chip(i * 500.0)).ToArray();
			var (planner, host) = Make(notes);
			Play(planner, host, 0, 6000);

			AssertSameChips(notes, host.Hits.Select(h => h.chip));
			foreach (var (chip, msHit, msNow, msPrevNow) in host.Hits) {
				Assert.Equal(ENoteJudge.Good, TimingJudgeOf(host, chip, msHit));
				Assert.Equal(ENoteJudge.Good, planner.JudgeOf(chip));
				Assert.True(msHit <= msNow && msHit > Math.Floor(msPrevNow));
			}
			Assert.Equal(0, planner.UnplannedCount + planner.RefusedCount);
		}

		[Fact]
		public void IsPending_FromPlanningUntilTheHit() {
			var note = Chip(1000.0);
			var (planner, host) = Make(note);
			Assert.False(planner.IsPending(note));
			planner.Update(700.0); // within the bad zone + PlanLeadGameMs before the note
			Assert.True(planner.IsPending(note));
			planner.Update(1200.0);
			Assert.False(planner.IsPending(note));
			Assert.True(note.bHit);
			Assert.Single(host.Hits);
		}

		// sparse notes always have room, so each note's judge is its own roll and the timing gives it; the mix follows the odds
		[Fact]
		public void SparseNotes_EachKeepsItsOwnRoll_TheMixFollowsTheOdds() {
			var notes = Enumerable.Range(1, 3000).Select(i => Chip(i * 400.0)).ToArray();
			var (planner, host) = Make(notes);
			host.Odds = Config.apAIPerformances[0]; // 100 Bad, 400 Ok, 500 Good per mille
			Play(planner, host, 0, 3001 * 400.0);

			AssertSameChips(notes, host.Hits.Select(h => h.chip));
			var judges = host.Hits.Select(h => planner.JudgeOf(h.chip)).ToList();
			Assert.All(host.Hits, h => Assert.Equal(planner.JudgeOf(h.chip), TimingJudgeOf(host, h.chip, h.msHit)));
			Assert.InRange(judges.Count(j => j == ENoteJudge.Poor), 300 - 60, 300 + 60);
			Assert.InRange(judges.Count(j => j == ENoteJudge.Good), 1200 - 90, 1200 + 90);
			Assert.Equal(0, planner.UnplannedCount);
		}

		[Theory]
		[InlineData(83.0)]
		[InlineData(40.0)]
		public void DenseStream_TargetsStayInOrderAndBetweenTheNotes_EveryNoteKeepsItsBad(double msGap) {
			var notes = Enumerable.Range(0, 40).Select(i => Chip(1000.0 + i * msGap)).ToArray();
			var (planner, host) = Make(notes);
			host.Odds = BadOnly;
			Play(planner, host, 0, 1000.0 + 40 * msGap + 500);

			AssertSameChips(notes, host.Hits.Select(h => h.chip));
			for (int i = 0; i < host.Hits.Count; ++i) {
				var (chip, msHit, _, _) = host.Hits[i];
				Assert.NotEqual(ENoteJudge.Miss, TimingJudgeOf(host, chip, msHit));
				Assert.Equal(ENoteJudge.Poor, planner.JudgeOf(chip));
				if (i > 0)
					Assert.True(msHit > host.Hits[i - 1].msHit);
				if (i + 1 < notes.Length)
					Assert.True(msHit < notes[i + 1].nSoundTimems);
			}
			Assert.Equal(0, planner.UnplannedCount);
		}

		// a late-only (#NOTEIF) stream 40 ms apart has no room for a Bad: each note is hit at the landing time nearest to
		// the Bad zone, 39 ms late, and keeps its Bad; the last note has room
		[Fact]
		public void NoRoom_HitsNearestToTheZone_AndKeepsTheRolledJudge() {
			var stream = Enumerable.Range(0, 20).Select(i => Chip(1000.0 + i * 40.0)).ToArray();
			foreach (var chip in stream)
				chip.NoteIfTrigger = "placeholder";
			var (planner, host) = Make(stream);
			host.MetTriggers.Add("placeholder");
			host.Odds = BadOnly;
			Play(planner, host, 0, 2500.0);

			AssertSameChips(stream, host.Hits.Select(h => h.chip));
			foreach (var chip in stream.SkipLast(1)) {
				Assert.Equal((long)chip.dbSoundTimems + 39, HitTimeOf(host, chip));
				Assert.Equal(ENoteJudge.Good, TimingJudgeOf(host, chip, HitTimeOf(host, chip)));
				Assert.Equal(ENoteJudge.Poor, planner.JudgeOf(chip)); // the rolled Bad is kept
			}
			var last = stream[^1];
			Assert.InRange(HitTimeOf(host, last) - (long)last.dbSoundTimems, 76, 108);
			Assert.Equal(ENoteJudge.Poor, planner.JudgeOf(last));
		}

		// player 1's Just mod in the AI's slot: Ok-zone hits are judged Bad, by the timing and by the kept judge alike
		[Fact]
		public void JustMode_OkZoneHits_AreBad() {
			var notes = Enumerable.Range(1, 10).Select(i => Chip(i * 500.0)).ToArray();
			var (planner, host) = Make(notes);
			host.JustMode = 1;
			Play(planner, host, 0, 6000);
			foreach (var (chip, msHit, _, _) in host.Hits) {
				Assert.InRange(Math.Abs(msHit - chip.dbSoundTimems), 26, 75);
				Assert.Equal(ENoteJudge.Poor, TimingJudgeOf(host, chip, msHit));
				Assert.Equal(ENoteJudge.Poor, planner.JudgeOf(chip));
			}
		}

		[Fact]
		public void HiddenNote_IsNotHit() {
			var note = Chip(1000.0);
			var (planner, host) = Make(note);
			planner.Update(700.0);
			note.bVisible = false; // a branch hid it
			Play(planner, host, 701.0, 1500.0);
			Assert.Empty(host.Hits);
			Assert.False(planner.IsPending(note));
		}

		[Fact]
		public void RefusedHit_LeavesTheNoteToTheAutoplayHits_WithItsJudge() {
			var note = Chip(1000.0);
			var (planner, host) = Make(note);
			host.Refuses = _ => true;
			Play(planner, host, 0, 1500.0);
			Assert.Equal(1, planner.RefusedCount);
			Assert.False(planner.IsPending(note));
			Assert.Equal(ENoteJudge.Good, planner.JudgeOf(note)); // the autoplay hit at its time still shows its Ok
		}

		// a refused early target is left to the autoplay hit at the note's time, which comes after it
		[Fact]
		public void RefusedEarlyHit_IsNotTriedAgain() {
			var first = Chip(1000.0);
			var (planner, host) = Make(first, Chip(1100.0)); // the next note takes a late Bad, so the Bad is early
			host.Odds = BadOnly;
			host.Refuses = ms => ms < 950; // the first note's early Bad, 892 to 924 ms; the next note's is after 991 ms
			Play(planner, host, 0, 1500.0);
			Assert.Equal(1, planner.RefusedCount);
			Assert.Equal(2, host.Attempts.Count); // the refused early hit and the next note's hit
			Assert.InRange(host.Attempts[0], 892, 924);
			Assert.DoesNotContain(host.Hits, h => ReferenceEquals(h.chip, first));
			Assert.Equal(ENoteJudge.Poor, planner.JudgeOf(first));
		}

		// a refused late target comes after the autoplay hit at the note's time, which let the note pass for it, so the
		// note is hit at its time at once, keeping its judge
		[Fact]
		public void RefusedLateHit_IsHitAtTheNotesTime_KeepingItsJudge() {
			var note = Chip(1000.0);
			note.NoteIfTrigger = "placeholder"; // hit only at or after its time
			var (planner, host) = Make(note);
			host.MetTriggers.Add("placeholder");
			host.Odds = BadOnly;
			host.Refuses = ms => ms > 1000;
			Play(planner, host, 0, 1500.0);
			Assert.Equal(1, planner.RefusedCount);
			Assert.Equal(2, host.Attempts.Count);
			Assert.InRange(host.Attempts[0], 1076, 1108);
			Assert.Equal(1000, HitTimeOf(host, note));
			Assert.Equal(ENoteJudge.Poor, planner.JudgeOf(note));
		}

		[Fact]
		public void MinesAdlibAndRolls_AreNotPlanned() {
			var (rollHead, rollEnd) = Roll(3000.0, 3500.0);
			var chips = new[] { Chip(1000.0, ENoteType.Bomb), Chip(2000.0, ENoteType.Adlib), rollHead, rollEnd };
			var (planner, host) = Make(chips);
			Play(planner, host, 0, 4000.0);
			Assert.Empty(host.Hits);
			Assert.All(chips, c => Assert.False(planner.IsPending(c)));
			Assert.Equal(0, planner.UnplannedCount);
		}

		[Fact]
		public void NoteInsideARoll_IsUnplanned_AndKeepsItsRoll() {
			var (rollHead, rollEnd) = Roll(1000.0, 3000.0);
			var note = Chip(2000.0);
			var (planner, host) = Make(rollHead, note, rollEnd);
			host.Odds = BadOnly;
			Play(planner, host, 0, 3500.0);
			Assert.Empty(host.Hits);
			Assert.Equal(1, planner.UnplannedCount);
			Assert.Equal(ENoteJudge.Poor, planner.JudgeOf(note));
		}

		[Fact]
		public void NoteAfterARoll_IsHitAfterTheRollEnds() {
			var (rollHead, rollEnd) = Roll(1000.0, 1500.0);
			var note = Chip(1550.0);
			for (int seed = 0; seed < 50; ++seed) {
				var (_, host) = Make(rollHead, rollEnd, note);
				var planner = new AIHitPlanner(host, new Random(seed));
				note.bHit = false;
				Play(planner, host, 0, 2000.0);
				Assert.True(host.Hits.Single().msHit >= 1500);
			}
		}

		[Fact]
		public void NoteBeforeABalloon_IsHitBeforeItsHeadWindow() {
			var (balloon, balloonEnd) = Roll(1060.0, 2000.0, ENoteType.Balloon);
			var note = Chip(1000.0);
			for (int seed = 0; seed < 50; ++seed) {
				var (_, host) = Make(note, balloon, balloonEnd);
				var planner = new AIHitPlanner(host, new Random(seed));
				note.bHit = false;
				Play(planner, host, 0, 1100.0);
				Assert.True(host.Hits.Single().msHit < 1060 - 17);
			}
		}

		[Fact]
		public void UnhitMineBefore_MovesABadLate() {
			var note = Chip(1000.0);
			for (int seed = 0; seed < 50; ++seed) {
				var (_, host) = Make(Chip(960.0, ENoteType.Bomb), note);
				host.Odds = BadOnly;
				var planner = new AIHitPlanner(host, new Random(seed));
				note.bHit = false;
				Play(planner, host, 0, 1500.0);
				Assert.InRange(host.Hits.Single().msHit, 1076, 1108);
			}
		}

		[Fact]
		public void NoteIf_IsHitOnlyAtOrAfterItsTime_AndSkippedWhenUnmet() {
			var notes = Enumerable.Range(1, 10).Select(i => Chip(i * 500.0)).ToArray();
			foreach (var chip in notes)
				chip.NoteIfTrigger = "placeholder";
			var (planner, host) = Make(notes);
			host.MetTriggers.Add("placeholder");
			Play(planner, host, 0, 6000.0);
			AssertSameChips(notes, host.Hits.Select(h => h.chip));
			Assert.All(host.Hits, h => Assert.True(h.msHit >= h.chip.nSoundTimems));

			var unmet = Chip(1000.0);
			unmet.NoteIfTrigger = "placeholder";
			var (planner2, host2) = Make(unmet);
			Play(planner2, host2, 0, 1500.0);
			Assert.Empty(host2.Hits);
		}

		[Fact]
		public void NotesAtTheSameTime_AreBothHitInOrder() {
			var first = Chip(1000.0);
			var second = Chip(1000.0, ENoteType.Ka);
			var (planner, host) = Make(first, second);
			Play(planner, host, 0, 1500.0);
			AssertSameChips(new[] { first, second }, host.Hits.Select(h => h.chip));
			Assert.True(host.Hits[0].msHit < 1000 && host.Hits[1].msHit > host.Hits[0].msHit);
		}

		[Fact]
		public void Reset_DropsThePlans() {
			var note = Chip(1000.0);
			var (planner, host) = Make(note);
			planner.Update(700.0);
			Assert.True(planner.IsPending(note));
			planner.Reset();
			Assert.False(planner.IsPending(note));
			Play(planner, host, 700.0, 1500.0);
			Assert.Single(host.Hits);
		}

		[Fact]
		public void PlanLead_ComesFromTheHost() {
			var note = Chip(1000.0);
			var (planner, host) = Make(note);
			host.MsPlanLead = 0;
			planner.Update(700.0); // 700 + 108 < 1000
			Assert.False(planner.IsPending(note));
			host.MsPlanLead = 500;
			planner.Update(701.0);
			Assert.True(planner.IsPending(note));
		}

		// a note the planner never reached (a long stall) rolls its judge when it is hit, once
		[Fact]
		public void NotePassedWithoutAPlan_RollsItsJudgeWhenHit() {
			var note = Chip(1000.0);
			var (planner, host) = Make(note);
			host.Odds = BadOnly;
			planner.Update(1500.0); // the note is out of reach when first seen
			Assert.Empty(host.Hits);
			Assert.False(planner.IsPending(note));
			Assert.Equal(ENoteJudge.Poor, planner.JudgeOf(note));
			host.Odds = GoodOnly;
			Assert.Equal(ENoteJudge.Poor, planner.JudgeOf(note)); // rolled once
		}

		// a branch shows a note between a note and its planned late hit: the hit is planned again before the shown note,
		// with the same zone
		[Fact]
		public void BranchShowingANote_ReplansThePendingHits_KeepingTheirZones() {
			int lateFirstHits = 0;
			for (int seed = 0; seed < 50; ++seed) {
				var first = Chip(1000.0);
				var shown = Chip(1050.0);
				shown.bVisible = false;
				var last = Chip(1200.0);
				var (_, host) = Make(first, shown, last);
				host.Odds = new CConfigIni.CAIPerformances(0, 500, 500, 10); // Ok or Bad
				var planner = new AIHitPlanner(host, new Random(seed));
				Play(planner, host, 0, 1000.0);
				if (planner.IsPending(first))
					++lateFirstHits; // planned after 1050 while the note was hidden
				var judgeBefore = planner.JudgeOf(first);
				planner.DispatchBefore(1000.0); // the branch judge chip at 1000 ms
				shown.bVisible = true;
				planner.ReplanPending();
				Play(planner, host, 1000.0 + MsFrame, 1600.0);

				AssertSameChips(new[] { first, shown, last }, host.Hits.Select(h => h.chip));
				Assert.True(host.Hits[0].msHit < 1050 && host.Hits[1].msHit < 1200);
				Assert.All(host.Hits, h => Assert.NotEqual(ENoteJudge.Miss, TimingJudgeOf(host, h.chip, h.msHit)));
				Assert.Equal(judgeBefore, planner.JudgeOf(first));
				Assert.Equal(0, planner.RefusedCount);
			}
			Assert.True(lateFirstHits > 0);
		}

		// a branch chip between the play's last frame and a pending late Bad, the frame then past the Bad zone: the target
		// still lands, so it is kept and hit in that frame rather than planned after it (where nothing lands)
		[Fact]
		public void Replan_KeepsATargetThatStillLands_EvenOnceTheFrameIsPastIt() {
			long HitTime(bool replan) {
				var note = Chip(1000.0);
				note.NoteIfTrigger = "placeholder"; // hit only at or after its time
				var (planner, host) = Make(note);
				host.MetTriggers.Add("placeholder");
				host.Odds = BadOnly;
				planner.Update(900.0);
				Assert.True(planner.IsPending(note));
				if (replan) {
					planner.DispatchBefore(1060.0); // the branch judge chip at 1060 ms, before the target
					planner.ReplanPending();
				}
				planner.Update(1115.0);
				Assert.Equal(ENoteJudge.Poor, planner.JudgeOf(note));
				return HitTimeOf(host, note);
			}
			long msTarget = HitTime(replan: false);
			Assert.InRange(msTarget, 1076, 1108);
			Assert.Equal(msTarget, HitTime(replan: true));
		}

		[Fact]
		public void Replan_DropsAPendingNoteTheBranchHid() {
			var note = Chip(1000.0);
			var (planner, host) = Make(note);
			planner.Update(800.0);
			Assert.True(planner.IsPending(note));
			note.bVisible = false;
			planner.ReplanPending();
			Assert.False(planner.IsPending(note));
			Play(planner, host, 801.0, 1500.0);
			Assert.Empty(host.Hits);
		}

		// an AI battle section passed and changed the AI level: the notes from then on roll again with the new odds, the
		// notes before keep their rolls
		[Fact]
		public void LevelChange_RerollsTheZonesAhead_TheNotesBeforeKeepTheirs() {
			var before = Chip(1000.0);
			var after1 = Chip(1400.0);
			var after2 = Chip(1800.0);
			var (planner, host) = Make(before, after1, after2);
			host.MsPlanLead = 1000;
			host.Odds = BadOnly;
			planner.Update(1001.0); // all three planned in their Bad zones
			Assert.True(planner.IsPending(after1) && planner.IsPending(after2));
			Assert.Equal(ENoteJudge.Poor, planner.JudgeOf(after1));
			host.Odds = GoodOnly;
			host.Level = 2;
			Play(planner, host, 1010.0, 2500.0);

			AssertSameChips(new[] { before, after1, after2 }, host.Hits.Select(h => h.chip));
			Assert.Equal(ENoteJudge.Poor, planner.JudgeOf(before));
			Assert.Equal(ENoteJudge.Perfect, planner.JudgeOf(after1));
			Assert.Equal(ENoteJudge.Perfect, planner.JudgeOf(after2));
			Assert.All(host.Hits, h => Assert.Equal(planner.JudgeOf(h.chip), TimingJudgeOf(host, h.chip, h.msHit)));
		}

		// a branch shows a roll that the planner already looked past while nothing was pending: the next note, planned
		// while the roll runs, is not hit before the roll ends
		[Fact]
		public void ReplanWithNothingPending_SeesARollTheBranchShowed() {
			for (int seed = 0; seed < 30; ++seed) {
				var first = Chip(1000.0);
				var (head, end) = Roll(1200.0, 1600.0);
				head.bVisible = false;
				var mid = Chip(1250.0);
				var last = Chip(1650.0); // early Bad zone 1542..1574, inside the roll
				var (_, host) = Make(first, head, mid, end, last);
				host.MsPlanLead = 0;
				host.Odds = BadOnly;
				var planner = new AIHitPlanner(host, new Random(seed));
				Play(planner, host, 0, 1400.0); // first and mid are planned past the hidden roll and hit
				Assert.False(planner.IsPending(first) || planner.IsPending(mid));
				head.bVisible = true;
				planner.ReplanPending();
				host.MsPlanLead = 300; // last is planned in the next frame, while the roll runs
				Play(planner, host, 1400.0 + MsFrame, 2000.0);
				Assert.True(HitTimeOf(host, last) >= 1600);
			}
		}
	}
}
