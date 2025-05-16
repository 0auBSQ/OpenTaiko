namespace OpenTaiko.Animations;

/// <summary>
/// Animation interface.
/// </summary>
interface IAnimatable {
	/// <summary>
	/// Starts the animation.
	/// </summary>
	void Start();
	/// <summary>
	/// Stops the animation.
	/// </summary>
	void Stop();
	/// <summary>
	/// Resets the animation.
	/// </summary>
	void Reset();
	/// <summary>
	/// Advances the animation.
	/// </summary>
	void Tick();
	/// <summary>
	/// Returns the animation parameters.
	/// </summary>
	/// <returns>Animation parameters.</returns>
	object GetAnimation();
}
