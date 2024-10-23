namespace OpenTaiko.Animations;

/// <summary>
/// A class that performs fade-out animation.
/// </summary>
internal class FadeOut : Animator {
	/// <summary>
	/// Initialize fade-out.
	/// </summary>
	/// <param name="timems">Time taken for fading, in milliseconds.</param>
	public FadeOut(int timems) : base(0, timems - 1, 1, false) {
		TimeMs = timems;
	}

	/// <summary>
	/// Returns the opacity of the fade-out animation in 255 levels.
	/// </summary>
	/// <returns>Opacity of the fade-out animation.</returns>
	public override object GetAnimation() {
		var opacity = (TimeMs - base.Counter.CurrentValue) * 255 / TimeMs;
		return opacity;
	}

	private readonly int TimeMs;
}
