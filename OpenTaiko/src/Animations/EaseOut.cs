namespace OpenTaiko.Animations;

/// <summary>
/// A class that performs ease-out animation.
/// </summary>
class EaseOut : Animator {
	/// <summary>
	/// Initialize Ease-out.
	/// </summary>
	/// <param name="startPoint">Starting point.</param>
	/// <param name="endPoint">End point.</param>
	/// <param name="timeMs">Time taken for easing, in milliseconds.</param>
	public EaseOut(int startPoint, int endPoint, int timeMs) : base(0, timeMs, 1, false) {
		StartPoint = startPoint;
		EndPoint = endPoint;
		Sa = EndPoint - StartPoint;
		TimeMs = timeMs;
	}

	public override object GetAnimation() {
		var percent = Counter.CurrentValue / (double)TimeMs;
		percent -= 1;
		return (double)Sa * (percent * percent * percent + 1) + StartPoint;
	}

	private readonly int StartPoint;
	private readonly int EndPoint;
	private readonly int Sa;
	private readonly int TimeMs;
}
