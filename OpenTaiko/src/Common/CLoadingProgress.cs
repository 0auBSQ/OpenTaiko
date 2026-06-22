namespace OpenTaiko;

/// <summary>
/// A tiny, thread-safe progress source shared by the loading screens (boot and song-loading) and the
/// Lua scripts that draw their loading bars. Only ONE load is ever in flight at a time (boot, then later a
/// song load), so a single static holder is sufficient.
///
/// The heavy work runs on background threads (system-sound preload, texture load, chart parse, WAV load);
/// those threads only ever WRITE primitives here, and the render thread / Lua only READ — no locks needed.
/// The fields are <c>volatile</c> so writes from a worker thread are visible to the render thread promptly.
/// </summary>
public static class CLoadingProgress {
	private static volatile bool _active;

	// float is read/written atomically on every platform we target; volatile gives cross-thread visibility.
	private static volatile float _progress;

	// Display-side smoothing (only ever touched on the render thread, in Tick).
	private static float _displayProgress;
	private static float _animPhase;

	/// <summary>True while a load is in progress (between <see cref="Begin"/> and <see cref="End"/>).</summary>
	public static bool Active => _active;

	/// <summary>Raw target progress, 0..1 (what the loaders report).</summary>
	public static float Progress => _progress;

	/// <summary>Raw target progress as a rounded integer percentage, 0..100.</summary>
	public static int Percent => (int)MathF.Round(_progress * 100f);

	/// <summary>Smoothed, eased progress the bar should actually draw, 0..1 (see <see cref="Tick"/>).</summary>
	public static float DisplayProgress => _displayProgress;

	/// <summary>Smoothed progress as a rounded integer percentage, 0..100.</summary>
	public static int DisplayPercent => (int)MathF.Round(_displayProgress * 100f);

	/// <summary>Looping 0..1 animation phase for the indeterminate "alive" shimmer.</summary>
	public static float AnimPhase => _animPhase;

	/// <summary>True while the loading overlay should still draw (active, or still easing up to 100%).</summary>
	public static bool ShouldDraw => _active || _displayProgress < 0.999f;

	/// <summary>
	/// Advance the display smoothing one frame. Call once per rendered frame from the render thread.
	/// Eases <see cref="DisplayProgress"/> toward the raw target so the bar never snaps, plus a tiny
	/// constant crawl so it always looks alive, and advances the shimmer phase. <paramref name="deltaMs"/>
	/// is clamped so a long stall (e.g. the game-screen activation freeze) eases over several frames
	/// afterward instead of jumping.
	/// </summary>
	public static void Tick(double deltaMs) {
		float dt = (float)Math.Clamp(deltaMs, 0.0, 100.0) / 1000f;
		float target = _progress;

		_displayProgress += (target - _displayProgress) * (1f - MathF.Exp(-8f * dt));
		float minCrawl = 0.03f * dt;   // ≈3%/s floor so it keeps inching toward the target
		if (_displayProgress < target)
			_displayProgress = Math.Min(target, _displayProgress + minCrawl);
		if (_displayProgress > 1f) _displayProgress = 1f;
		else if (_displayProgress < 0f) _displayProgress = 0f;

		_animPhase += dt / 1.2f;        // one shimmer sweep ≈ every 1.2s
		if (_animPhase >= 1f) _animPhase -= (int)_animPhase;
	}

	/// <summary>Start a new load: reset to 0 and mark active.</summary>
	public static void Begin() {
		_progress = 0f;
		_displayProgress = 0f;
		_animPhase = 0f;
		_active = true;
	}

	/// <summary>
	/// Report progress. Monotonic: it only ever moves the bar FORWARD, so the bar never visibly jumps
	/// backwards when one phase hands off to the next (e.g. system sounds → textures).
	/// </summary>
	public static void Report(float p) {
		if (p < 0f) p = 0f;
		else if (p > 1f) p = 1f;
		if (p > _progress)
			_progress = p;
	}

	/// <summary>
	/// Report progress for one phase that occupies the sub-range [lo, hi] of the overall bar, given the
	/// number of items <paramref name="done"/> out of <paramref name="total"/> completed in that phase.
	/// </summary>
	public static void ReportSegment(float lo, float hi, int done, int total) {
		Report(total <= 0 ? hi : lo + (hi - lo) * (done / (float)total));
	}

	/// <summary>Finish the current load: snap to 100% and mark inactive.</summary>
	public static void End() {
		_progress = 1f;
		_active = false;
	}
}
