namespace OpenTaiko.Animations;

/// <summary>
/// A class that performs linear animation.
/// </summary>
class Linear : Animator {
	/// <summary>
	/// Initialize linear movement.
	/// </summary>
	/// <param name="startPoint">Starting point.</param>
	/// <param name="endPoint">End point.</param>
	/// <param name="timeMs">Time taken for linear, in milliseconds.</param>
	public Linear(int startPoint, int endPoint, int timeMs) : base(0, timeMs, 1, false) {
		StartPoint = startPoint;
		EndPoint = endPoint;
		Sa = EndPoint - StartPoint;
		TimeMs = timeMs;
	}

	public override object GetAnimation() {
		var percent = Counter.CurrentValue / (double)TimeMs;
		return (Sa * percent) + StartPoint;
	}

	private readonly int StartPoint;
	private readonly int EndPoint;
	private readonly int Sa;
	private readonly int TimeMs;
}
