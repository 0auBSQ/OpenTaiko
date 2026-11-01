using System.Collections.Generic;

namespace OpenTaiko;

// The result of one incremental load slice (see IStepLoad / CLoadSession).
public enum LoadStatus { More, Done, Canceled }

// One incremental load source driven by CLoadSession: each Step() does a slice of work and reports 0..1
// progress. Implementations are thin adapters over the existing primitives (the module-onStart loop, a stage's
// activate coroutine, the song loader) so every load path shares ONE drive loop instead of a hand-rolled phase
// machine each. Lua + scene mutation + GL are render-thread-only, so a Step never moves work off-thread — heavy
// work yields (time-budgeted; see CLuaScript.YieldHook) and the session renders a frame between slices.
public interface IStepLoad {
	LoadStatus Step(out float progress);
}

// Adapts a C# iterator that yields 0..1 progress (e.g. CSkin.LoadModulesIncrementally) to IStepLoad.
public sealed class EnumeratorStep : IStepLoad {
	private readonly IEnumerator<float> _it;
	public EnumeratorStep(IEnumerator<float> it) { _it = it; }
	public LoadStatus Step(out float progress) {
		if (_it.MoveNext()) { progress = _it.Current; return LoadStatus.More; }
		progress = 1f;
		return LoadStatus.Done;
	}
}
