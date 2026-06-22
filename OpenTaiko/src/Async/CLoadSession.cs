using System.Diagnostics;
using FDK;

namespace OpenTaiko;

// The single "run an incremental load to completion behind the loading bar" driver — replaces the per-call
// hand-rolled phase machines in CStageStartup (boot), CStageChangeSkin (skin reload) and CStageTransition
// (stage entry / song load).
//
// The engine has two async regimes; keep them distinct:
//   • LOAD-PHASE  — one-shot work covered by a loading screen (boot, skin reload, stage entry, song load).
//                   THIS class. Drives an IStepLoad each frame within a wall-clock budget; when it manages an
//                   asset phase, the onStart/activate textures/sounds/shared stream via CAsyncLoad and the bar
//                   waits on them.
//   • STEADY-STATE STREAMING — ongoing in-gameplay work (e.g. voxel chunk gen/mesh). NOT handled here; that
//                   budgets its own per-frame work. (A future shared "budgeted worker" could unify both.)
//
// The caller maps SourceProgress / AssetFraction onto CLoadingProgress (each stage owns its bar layout) and
// loops Step() until it returns false.
public sealed class CLoadSession {
	// Per-frame budget for the SOURCE step (coroutine / module loop). Kept modest so this + the AsyncActions
	// finalize drain (Game.AsyncBudgetMs, raised below) + render all fit one frame ⇒ a smooth loading screen.
	private const double SourceBudgetMs = 6.0;
	private const double FinalizeBudgetMs = 6.0;   // AsyncActions budget while this load is up

	private readonly IStepLoad _source;
	private readonly bool _assets;     // manage a CAsyncLoad asset-streaming phase around the source
	private bool _sourceDone;

	public float SourceProgress { get; private set; }                  // 0..1 reported by the source
	public bool SourceDone => _sourceDone;                             // source finished; only asset upload remains
	public float AssetFraction => _assets ? CAsyncLoad.Fraction : 1f;  // 0..1 asset-upload progress
	public bool Canceled { get; private set; }

	public CLoadSession(IStepLoad source, bool manageAssetPhase = true) {
		_source = source;
		_assets = manageAssetPhase;
	}

	public void Begin() {
		Game.AsyncBudgetMs = FinalizeBudgetMs;   // bigger finalize budget behind the loading screen
		if (_assets) CAsyncLoad.BeginPhase();
	}

	/// <summary>One frame of work: step the source within a wall-clock budget (fast slices batch; a heavy slice
	/// self-limits via the time-based coroutine yield). The textures/sounds the source queues finalize on
	/// Game.AsyncActions (drained in Window_Render before this Draw). Returns true while work remains; false when
	/// finished — check <see cref="Canceled"/> for an aborted load.</summary>
	public bool Step(double budgetMs = SourceBudgetMs) {
		if (!_sourceDone) {
			long t0 = Stopwatch.GetTimestamp();
			do {
				var st = _source.Step(out float p);
				SourceProgress = p;
				if (st == LoadStatus.Canceled) { Canceled = true; return false; }
				if (st == LoadStatus.Done) { _sourceDone = true; break; }
			} while (Stopwatch.GetElapsedTime(t0).TotalMilliseconds < budgetMs);
		}
		return !(_sourceDone && (!_assets || CAsyncLoad.Complete));
	}

	public void End() {
		if (_assets) CAsyncLoad.EndPhase();
		Game.AsyncBudgetMs = Game.DefaultAsyncBudgetMs;
	}
	public void Cancel() {
		if (_assets) CAsyncLoad.CancelPhase();
		Game.AsyncBudgetMs = Game.DefaultAsyncBudgetMs;
	}
}
