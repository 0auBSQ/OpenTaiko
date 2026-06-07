namespace OpenTaiko;

internal class CLuaFps {
	public double deltaTime => OpenTaiko.FPS.DeltaTime;
	public int fps => OpenTaiko.FPS.NowFPS;

	/// <summary>High-resolution monotonic clock in milliseconds (for profiling Lua sections via fps.ms deltas).</summary>
	public double ms => (double)System.Diagnostics.Stopwatch.GetTimestamp() * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
}
