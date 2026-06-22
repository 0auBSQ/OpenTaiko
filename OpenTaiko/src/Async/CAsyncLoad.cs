using System;
using System.Threading;
using FDK;

namespace OpenTaiko;

// Coordinates async asset loading during a load PHASE (a stage onStart/activate, boot, or skin reload — all
// covered by a loading screen). While a phase is active, asset Funcs called on the load thread defer their
// CPU work; the render-thread finalize is budgeted (textures) or runs via Game.AsyncActions (model/sound/
// shared). No Lua/GL ever runs off-thread. Outside a phase, assets load synchronously.
public static class CAsyncLoad {
	private static volatile bool _active;
	private static int _threadId;
	private static int _pending;   // non-texture background items queued (model / sound / shared)
	private static int _done;      // ... finalized on the render thread

	public static bool Active => _active;

	// Defer only when a phase is active AND we're on the load thread (background tasks must not re-defer).
	public static bool ShouldDefer => _active && Thread.CurrentThread.ManagedThreadId == _threadId;

	public static void BeginPhase() {
		_threadId = Thread.CurrentThread.ManagedThreadId;
		Interlocked.Exchange(ref _pending, 0);
		Interlocked.Exchange(ref _done, 0);
		CTexture.BeginStreaming();
		_active = true;
	}

	public static void StartDecode() => CTexture.StartStreamDecode();

	// Create on the render thread but spread across frames (for BASS streams, unsafe to build off-thread):
	// Game.AsyncActions drains within a 6 ms/frame budget so the bar animates between batches.
	public static void TrackRenderThread(Action create) {
		Interlocked.Increment(ref _pending);
		Game.AsyncActions.Enqueue(() => {
			try { create(); }
			catch (Exception e) { System.Diagnostics.Trace.TraceWarning("[CAsyncLoad] render-thread create failed: " + e.Message); }
			finally { Interlocked.Increment(ref _done); }
		});
	}

	// For the already-async SharedTexture/Sound Reload path (owns its own Task + swap). Pair with NoteDone on
	// every completion path including failure.
	public static void NotePending() => Interlocked.Increment(ref _pending);
	public static void NoteDone() => Interlocked.Increment(ref _done);

	public static void Pump(double budgetMs) => CTexture.PumpUploads(budgetMs);

	public static bool Complete => CTexture.StreamComplete && Volatile.Read(ref _done) >= Volatile.Read(ref _pending);

	// Best-effort 0..1 (min of texture + background fractions).
	public static float Fraction {
		get {
			float tex = CTexture.StreamFraction;
			int p = Volatile.Read(ref _pending);
			float other = p <= 0 ? 1f : Math.Min(1f, Volatile.Read(ref _done) / (float)p);
			return Math.Min(tex, other);
		}
	}

	public static void EndPhase() {
		_active = false;
		CTexture.EndStreaming();
	}

	// Interrupted: drop queued texture work; in-flight tasks finalize harmlessly (stubs discarded with stage).
	public static void CancelPhase() {
		_active = false;
		CTexture.CancelStreaming();
	}
}
