using OpenTaiko;
using Xunit;

namespace OpenTaikoTests {
	// branch and dan counting rules: a pp branch condition is a real percentage, and a dan judgement counts for the
	// song its note belongs to, the songs being split at each #NEXTSONG's time
	public class BranchDanRulesTests {
		private static CStagePlayScreenCommon.CBRANCHSCORE Judged(int perfects, int goods, int misses)
			=> new CStagePlayScreenCommon.CBRANCHSCORE { nGreat = perfects, nGood = goods, nMiss = misses };

		private static double PercentPerfect(CStagePlayScreenCommon.CBRANCHSCORE score, CTja.EBranchCondBig big = CTja.EBranchCondBig.Both)
			=> score.GetScore(Exam.Type.PercentPerfect, big);

		[Fact]
		public void PercentPerfect_NinePerfectsAndOneGood_Is90() {
			Assert.Equal(90.0, PercentPerfect(Judged(9, 1, 0)), 9);
		}

		[Fact]
		public void PercentPerfect_CountsGoodsAndMissesAlike() {
			Assert.Equal(90.0, PercentPerfect(Judged(9, 0, 1)), 9);
			Assert.Equal(200.0 / 3, PercentPerfect(Judged(2, 1, 0)), 9);
			Assert.Equal(100.0, PercentPerfect(Judged(4, 0, 0)), 9);
			Assert.Equal(0.0, PercentPerfect(Judged(0, 3, 2)), 9);
		}

		[Fact]
		public void PercentPerfect_WithNothingJudged_IsZero() {
			Assert.Equal(0.0, PercentPerfect(Judged(0, 0, 0)));
		}

		[Fact]
		public void PercentPerfect_BigOnly_ReadsTheBigNotes() {
			var score = Judged(9, 1, 0);
			score.bigOnly.nGreat = 1;
			score.bigOnly.nGood = 1;
			Assert.Equal(50.0, PercentPerfect(score, CTja.EBranchCondBig.BigOnly), 9);
		}

		// three songs whose #NEXTSONG chips sit at 0 s, 60 s and 130 s
		private static readonly double[] SongStarts = { 0.0, 60000.0, 130000.0 };

		[Theory]
		[InlineData(10000.0, 0)]
		[InlineData(59999.5, 0)]
		[InlineData(60000.5, 1)]
		[InlineData(129999.0, 1)]
		[InlineData(130001.0, 2)]
		public void DanSongAt_SplitsAtEachNextSong(double msTjaTime, int song) {
			Assert.Equal(song, CStagePlayScreenCommon.DanSongAt(SongStarts, msTjaTime));
		}

		[Fact]
		public void DanSongAt_AChipAtANextSong_BelongsToTheSongBefore() {
			Assert.Equal(0, CStagePlayScreenCommon.DanSongAt(SongStarts, 60000.0));
			Assert.Equal(1, CStagePlayScreenCommon.DanSongAt(SongStarts, 130000.0));
		}

		[Fact]
		public void DanSongAt_BeforeTheFirstSong_IsTheFirstSong() {
			Assert.Equal(0, CStagePlayScreenCommon.DanSongAt(SongStarts, 0.0));
			Assert.Equal(0, CStagePlayScreenCommon.DanSongAt(SongStarts, -500.0));
		}

		[Fact]
		public void DanSongAt_AfterTheLastSong_IsTheLastSong() {
			Assert.Equal(2, CStagePlayScreenCommon.DanSongAt(SongStarts, 1e9));
		}

		[Fact]
		public void DanSongAt_SkipsASongWithoutStart() {
			// a song whose #NEXTSONG chip was never found keeps +infinity and is never chosen
			var starts = new[] { 0.0, double.PositiveInfinity, 130000.0 };
			Assert.Equal(0, CStagePlayScreenCommon.DanSongAt(starts, 100000.0));
			Assert.Equal(2, CStagePlayScreenCommon.DanSongAt(starts, 140000.0));
		}

		[Fact]
		public void DanSongAt_WithOneOrNoSong_IsTheFirstSong() {
			Assert.Equal(0, CStagePlayScreenCommon.DanSongAt(new[] { 0.0 }, 5000.0));
			Assert.Equal(0, CStagePlayScreenCommon.DanSongAt(new double[0], 5000.0));
		}
	}
}
