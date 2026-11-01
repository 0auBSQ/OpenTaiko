using System.Linq;
using OpenTaiko;
using Xunit;

namespace OpenTaikoTests {
	// the fireworks along a big note's flight start once per step of the flight counter
	public class FireworkStepTests {
		private static int[] Steps(int old, int current, int step)
			=> FlyingNotes.MidValues(old, current, step, true).Where(v => v.onStep).Select(v => v.mid).ToArray();

		[Theory]
		[InlineData(0)]
		[InlineData(-8)]
		public void StepOfZeroOrLessGivesNoFireworks(int step) {
			Assert.Equal(new[] { (140, false) }, FlyingNotes.MidValues(0, 140, step, true).ToArray());
		}

		[Fact]
		public void EachStepIsGivenOnceAcrossFrames() {
			// frames that end on a step, between steps, and that do not move
			int[] frames = { 0, 8, 8, 13, 16, 40, 41, 139, 140 };
			var steps = Enumerable.Range(1, frames.Length - 1).SelectMany(i => Steps(frames[i - 1], frames[i], 8)).ToArray();
			Assert.Equal(Enumerable.Range(1, 17).Select(i => i * 8).ToArray(), steps);
		}

		[Fact]
		public void TheCurrentPositionComesLast() {
			Assert.Equal(new[] { (8, true), (16, true), (16, false) }, FlyingNotes.MidValues(5, 16, 8, true).ToArray());
			Assert.Equal(new[] { (16, false) }, FlyingNotes.MidValues(5, 16, 8, false).ToArray());
		}
	}

	public class DancerMotionTests {
		[Fact]
		public void EntriesOutsideTheFramesAreClamped() {
			int[] motion = { -1, 0, 5, 6, 40 };
			CActImplDancer.ClampMotion(motion, 6);
			Assert.Equal(new[] { 0, 0, 5, 5, 5 }, motion);

			int[] none = { 3, 0 };
			CActImplDancer.ClampMotion(none, 0);
			Assert.Equal(new[] { 0, 0 }, none);
		}
	}
}
