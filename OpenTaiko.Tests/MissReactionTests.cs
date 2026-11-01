using OpenTaiko;
using Xunit;

namespace OpenTaikoTests {
	// the character's reaction to a miss is chosen once, from the number of misses in a row including that miss
	public class MissReactionTests {
		[Fact]
		public void FirstMiss_PlaysMissIn() {
			Assert.Equal(CCharacter.ANIM_GAME_MISS_IN, CStagePlayScreenCommon.MissReactionAnimation(false, false, 1));
		}

		[Fact]
		public void SixthMiss_PlaysMissDownIn() {
			Assert.Equal(CCharacter.ANIM_GAME_MISS_DOWN_IN, CStagePlayScreenCommon.MissReactionAnimation(false, false, 6));
		}

		[Theory]
		[InlineData(2)]
		[InlineData(5)]
		[InlineData(7)]
		public void OtherMisses_PlayNothing(int missCount) {
			Assert.Null(CStagePlayScreenCommon.MissReactionAnimation(false, false, missCount));
		}

		[Theory]
		[InlineData(1)]
		[InlineData(6)]
		public void LeavingMaxGauge_KeepsTheMaxOutReaction(int missCount) {
			Assert.Null(CStagePlayScreenCommon.MissReactionAnimation(true, false, missCount));
		}

		[Theory]
		[InlineData(1)]
		[InlineData(6)]
		public void GoGoTime_PlaysNothing(int missCount) {
			Assert.Null(CStagePlayScreenCommon.MissReactionAnimation(false, true, missCount));
		}
	}
}
