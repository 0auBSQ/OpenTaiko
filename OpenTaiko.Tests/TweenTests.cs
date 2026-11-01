using OpenTaiko;
using Xunit;

namespace OpenTaikoTests {
	// A chart tween (#CAM*START, #OBJ*START) is a pure function of the chart time elapsed since its start.
	public class TweenTests {
		private static (bool ended, float value) Eval(double msElapsed, double msDuration, string ease,
			Easing.CalcType calc = Easing.CalcType.Linear, float start = 0f, float end = 10f) {
			bool ended = Easing.EvaluateTween(msElapsed, msDuration, ease, start, end, calc, out float value);
			return (ended, value);
		}

		[Fact]
		public void BeforeItsStart_HoldsTheStartValue() {
			Assert.Equal((false, 0f), Eval(-5, 1000, "IN_OUT"));
			Assert.Equal((false, 2f), Eval(-0.5, 1000, "OUT", Easing.CalcType.Cubic, start: 2f));
		}

		[Fact]
		public void InTheMiddle_FollowsTheEase() {
			Assert.Equal((false, 5f), Eval(500, 1000, "IN_OUT"));
			Assert.Equal((false, 2.5f), Eval(500, 1000, "IN", Easing.CalcType.Quadratic));
			Assert.Equal((false, 7.5f), Eval(500, 1000, "OUT", Easing.CalcType.Quadratic));
		}

		[Fact]
		public void AtAndAfterItsEnd_GivesTheEndValueAndEnds() {
			Assert.Equal((true, 10f), Eval(1000, 1000, "IN_OUT", Easing.CalcType.Cubic));
			Assert.Equal((true, 10f), Eval(5000, 1000, "IN", Easing.CalcType.Exponential));
		}

		[Fact]
		public void FractionalDuration_Ends() {
			var (ended, value) = Eval(16.6, 16.7, "IN_OUT");
			Assert.False(ended);
			Assert.InRange(value, 9.9f, 10f);
			Assert.Equal((true, 10f), Eval(16.7, 16.7, "IN_OUT"));
		}

		[Fact]
		public void SubMillisecondDuration_AnimatesThenEnds() {
			Assert.Equal((false, 5f), Eval(0.2, 0.4, "IN_OUT"));
			Assert.Equal((true, 10f), Eval(0.4, 0.4, "IN_OUT"));
		}

		[Fact]
		public void ZeroOrNegativeDuration_EndsAtOnce() {
			Assert.Equal((true, 10f), Eval(0, 0, "IN_OUT"));
			Assert.Equal((true, 10f), Eval(0, -3, "IN"));
		}

		[Fact]
		public void UnknownEase_IsLinear() {
			Assert.Equal((false, 5f), Eval(500, 1000, "BOUNCE", Easing.CalcType.Cubic));
			Assert.Equal((false, 2.5f), Eval(250, 1000, "", Easing.CalcType.Exponential));
			Assert.Equal((false, 5f), Eval(500, 1000, null, Easing.CalcType.Quintic));
			Assert.Equal((true, 10f), Eval(1000, 1000, "BOUNCE"));
		}
	}
}
