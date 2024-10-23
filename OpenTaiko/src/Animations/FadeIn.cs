namespace OpenTaiko.Animations;

/// <summary>
/// A class that performs fade-in animation.
/// </summary>
internal class FadeIn : Animator {
	/// <summary>
	/// Initialize fade-in.
	/// </summary>
	/// <param name="timems">Time taken for fading, in milliseconds.</param>
	public FadeIn(int timems) : base(0, timems - 1, 1, false) {
		TimeMs = timems;
	}

	/// <summary>
	/// Returns the opacity of the fade-in animation in 255 levels.
	/// </summary>
	/// <returns>Opacity of the fade-in animation.</returns>
	public override object GetAnimation() {
		var opacity = base.Counter.CurrentValue * 255 / TimeMs;
		return opacity;
	}

	private readonly int TimeMs;
}
