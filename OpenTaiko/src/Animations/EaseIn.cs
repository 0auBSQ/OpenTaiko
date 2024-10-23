namespace OpenTaiko.Animations;

/// <summary>
/// A class that performs ease-in animation.
/// </summary>
class EaseIn : Animator {
	/// <summary>
	/// Initialize Ease-in.
	/// </summary>
	/// <param name="startPoint">Starting point</param>
	/// <param name="endPoint">End point</param>
	/// <param name="timeMs">Time taken for easing, in milliseconds</param>
	public EaseIn(int startPoint, int endPoint, int timeMs) : base(0, timeMs, 1, false) {
		StartPoint = startPoint;
		EndPoint = endPoint;
		Sa = EndPoint - StartPoint;
		TimeMs = timeMs;
	}

	public override object GetAnimation() {
		var percent = Counter.CurrentValue / (double)TimeMs;
		return ((double)Sa * percent * percent * percent) + StartPoint;
	}

	private readonly int StartPoint;
	private readonly int EndPoint;
	private readonly int Sa;
	private readonly int TimeMs;
}
