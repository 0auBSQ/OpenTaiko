using OpenTaiko;
using Xunit;

namespace OpenTaikoTests {
	// coins: the share of the bonus a play's note count earns against its difficulty and level
	public class CoinEffortTests {
		private static double Effort(Difficulty d, int stars, int notes) => CStageResult.tCoinEffort((int)d, stars, notes);

		[Theory]
		[InlineData(Difficulty.Easy, 1, 1)]
		[InlineData(Difficulty.Easy, 2, 40)]
		[InlineData(Difficulty.Easy, 8, 100)]
		[InlineData(Difficulty.Normal, 5, 70)]
		[InlineData(Difficulty.Hard, 6, 130)]
		[InlineData(Difficulty.Hard, 10, 300)]
		[InlineData(Difficulty.Oni, 2, 100)]
		[InlineData(Difficulty.Oni, 7, 300)]
		[InlineData(Difficulty.Oni, 13, 1500)]
		[InlineData(Difficulty.Edit, 11, 800)]
		public void TheListedCountEarnsTheWholeBonus(Difficulty d, int stars, int notes) {
			Assert.Equal(1.0, Effort(d, stars, notes));
			Assert.True(Effort(d, stars, notes - 1) < 1.0 || notes == 1);
		}

		[Fact]
		public void ALevelPastItsRowTakesTheNextHarderRow() {
			Assert.Equal(1.0, Effort(Difficulty.Easy, 9, 250));     // Hard 9
			Assert.True(Effort(Difficulty.Easy, 9, 249) < 1.0);
			Assert.Equal(1.0, Effort(Difficulty.Easy, 10, 300));    // Hard 10
			Assert.True(Effort(Difficulty.Easy, 10, 299) < 1.0);
			Assert.Equal(1.0, Effort(Difficulty.Normal, 11, 800));  // Oni 11
			Assert.True(Effort(Difficulty.Hard, 12, 999) < 1.0);    // Oni 12
			Assert.Equal(1.0, Effort(Difficulty.Oni, 15, 1500));    // the last Oni value covers every level above
		}

		[Theory]
		[InlineData(Difficulty.Easy, 200)]
		[InlineData(Difficulty.Normal, 200)]
		[InlineData(Difficulty.Hard, 200)]
		[InlineData(Difficulty.Oni, 200)]
		[InlineData(Difficulty.Edit, int.MaxValue)]
		public void AnAbsurdLevelTakesTheOni13Value(Difficulty d, int stars) {
			Assert.Equal(1.0, Effort(d, stars, 1500));
			Assert.Equal(1499 / 1500.0, Effort(d, stars, 1499), 9);
		}

		[Theory]
		[InlineData(Difficulty.Easy, 0)]
		[InlineData(Difficulty.Normal, -1)]
		[InlineData(Difficulty.Hard, -50)]
		[InlineData(Difficulty.Edit, int.MinValue)]
		public void ALevelUnder1TakesThe1Value(Difficulty d, int stars) {
			Assert.Equal(1.0, Effort(d, stars, 1));
			Assert.Equal(0.0, Effort(d, stars, 0));
		}

		[Fact]
		public void FewNotesEarnAShare() {
			Assert.Equal(1 / 1500.0, Effort(Difficulty.Edit, 13, 1), 9);
			Assert.Equal(0.5, Effort(Difficulty.Normal, 2, 20), 9);
			Assert.Equal(0.0, Effort(Difficulty.Oni, 10, 0));
		}

		[Fact]
		public void OneNoteOni13PaysTheBaseWhateverTheMods() {
			// the most an Oni 13 full combo can add up to (5 + 3 x 17 stars + clear 9 + rank 9), every coin mod at its
			// highest (2x speed, Just, widest zones, Stealth: about x4.2) and a generous x10 for gauges and characters
			int amount = 5 + 51 + 9 + 9;
			double multiplier = 1.63 * 1.3 * 1.4 * 1.4 * 10;
			Assert.Equal(5, CStageResult.tFinalCoins(amount, multiplier, Effort(Difficulty.Oni, 13, 1)));
			Assert.Equal(5, CStageResult.tFinalCoins(amount, multiplier, Effort(Difficulty.Edit, 13, 1)));
		}

		[Fact]
		public void ADanWeighsEachSongsEffortByWhatTheSongIsWorth() {
			// three full songs worth 10 each and a 1-note 13 star song worth 51: the cheap song drags the whole dan
			var values = new[] { 10, 10, 10, 51 };
			var efforts = new[] { 1.0, 1.0, 1.0, Effort(Difficulty.Oni, 13, 1) };
			double dan = CStageResult.tDanCoinEffort(values, efforts);
			Assert.Equal((30 + 51 / 1500.0) / 81, dan, 9);
			Assert.Equal(1.0, CStageResult.tDanCoinEffort(new[] { 10, 20 }, new[] { 1.0, 1.0 }));
			Assert.Equal(0.5, CStageResult.tDanCoinEffort(new[] { 0, 0 }, new[] { 1.0, 0.0 }), 9);   // worth nothing: the average
			Assert.Equal(1.0, CStageResult.tDanCoinEffort(new int[0], new double[0]));
		}

		[Fact]
		public void AOneNoteDanPaysItsBaseWhateverTheMods() {
			var values = new[] { 51, 51, 51 };
			var efforts = new[] { Effort(Difficulty.Oni, 13, 1), Effort(Difficulty.Oni, 13, 1), Effort(Difficulty.Edit, 13, 1) };
			// 10 + gold pass 20 + full combo 6 + the partial scores, every mod at its highest and x10 on top
			int amount = 10 + 20 + 6 + 153;
			Assert.Equal(10, CStageResult.tFinalCoins(amount, 1.63 * 1.3 * 1.4 * 1.4 * 10, CStageResult.tDanCoinEffort(values, efforts), 10));
		}

		[Fact]
		public void AFullEffortPlayKeepsItsCoins() {
			Assert.Equal((int)(74 * 4.2), CStageResult.tFinalCoins(74, 4.2, Effort(Difficulty.Oni, 13, 1500)));
			Assert.Equal(5, CStageResult.tFinalCoins(3, 1.0, 1.0));      // never under the base
		}
	}
}
