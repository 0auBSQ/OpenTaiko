using System.Diagnostics;
using FDK;

namespace OpenTaiko;

// Orchestrates a stage→stage switch driven by a Lua transition component. Set as the current stage by
// OpenTaiko.UnmountAndChangeStage when a Lua Exit(...) requested a transition, then runs in Draw():
//   FadeOut  — render the outgoing stage under fadeOut(t)        (t 0→1)
//   Activate — unmount outgoing, then activate the target behind the cover (its asset loads defer to CAsyncLoad)
//   Loading  — pump deferred loads; past ~0.5 s draw loading(progress, elapsed) (short loads skip it: no blink)
//   FadeIn   — render the loaded target under fadeIn(t)          (t 0→1)
// On finish Draw() returns non-zero and the main loop swaps to the (already-mounted) Target.
internal class CStageTransition : CStage {
	private enum ETransitionPhase { Idle, FadeOut, Activate, Loading, FadeIn }
	private ETransitionPhase _phase = ETransitionPhase.Idle;

	private const double FadeOutSeconds = 0.25;
	private const double FadeInSeconds = 0.25;
	private const double LoadingScreenDelaySeconds = 0.5;   // shorter loads show no loading screen (anti-blink)

	private CStage? _outgoing;
	private LuaTransitionWrapper? _script;
	private long _phaseStart;
	private long _loadStart;

	// The stage being transitioned to (mounted by this transition; the main loop reads it once finished).
	public CStage? Target { get; private set; }

	// Pending-script handoff: set by the requesting stage's Exit, consumed by UnmountAndChangeStage.
	private static LuaTransitionWrapper? _pendingScript;
	public static void SetPendingScript(LuaTransitionWrapper script) => _pendingScript = script;
	public static LuaTransitionWrapper? ConsumePendingScript() { var s = _pendingScript; _pendingScript = null; return s; }
	public static void ClearPendingScript() => _pendingScript = null;

	public CStageTransition() {
		base.eStageID = CStage.EStage.Transition;
		base.IsDeActivated = true;
	}

	// outgoing is still mounted; target is NOT yet activated (this transition activates it behind the cover).
	public void Begin(CStage? outgoing, CStage target, LuaTransitionWrapper script, string? traceMessage) {
		if (traceMessage != null) {
			Trace.TraceInformation("----------------------");
			Trace.TraceInformation($"■ {traceMessage} (transition)");
		}
		_outgoing = outgoing;
		Target = target;
		_script = script;
		_phase = ETransitionPhase.FadeOut;
		_phaseStart = Stopwatch.GetTimestamp();
		base.IsDeActivated = false;
	}

	private static double Elapsed(long since) => Stopwatch.GetElapsedTime(since).TotalSeconds;

	public override int Draw() {
		switch (_phase) {
			case ETransitionPhase.FadeOut: {
				_outgoing?.Draw();
				double t = Math.Clamp(Elapsed(_phaseStart) / FadeOutSeconds, 0.0, 1.0);
				_script?.FadeOut(t);
				if (t >= 1.0) {
					// Outgoing fully covered: unmount it, then (next frame) activate the target.
					OpenTaiko.app.UnmountActivity(_outgoing);
					_outgoing = null;
					_phase = ETransitionPhase.Activate;
				}
				return 0;
			}

			case ETransitionPhase.Activate: {
				// Cover this frame first so the synchronous MountActivity below never presents a half-built frame.
				_script?.FadeOut(1.0);
				CAsyncLoad.BeginPhase();
				OpenTaiko.app.MountActivity(Target);   // Target.Activate(): asset loads defer to CAsyncLoad
				CAsyncLoad.StartDecode();
				_loadStart = Stopwatch.GetTimestamp();
				_phase = ETransitionPhase.Loading;
				return 0;
			}

			case ETransitionPhase.Loading: {
				CAsyncLoad.Pump(8.0);
				double el = Elapsed(_loadStart);
				if (el > LoadingScreenDelaySeconds)
					_script?.Loading(CAsyncLoad.Fraction, el);
				else
					_script?.FadeOut(1.0);   // keep the screen covered until either we're done or 0.5 s passes
				if (CAsyncLoad.Complete) {
					CAsyncLoad.EndPhase();
					_phase = ETransitionPhase.FadeIn;
					_phaseStart = Stopwatch.GetTimestamp();
				}
				return 0;
			}

			case ETransitionPhase.FadeIn: {
				Target?.Draw();
				double t = Math.Clamp(Elapsed(_phaseStart) / FadeInSeconds, 0.0, 1.0);
				_script?.FadeIn(t);
				if (t >= 1.0) {
					_phase = ETransitionPhase.Idle;
					return 1;   // done — main loop swaps rCurrentStage to Target
				}
				return 0;
			}
		}
		return 0;
	}

	// Release the hold once the main loop has taken over the (already-mounted) target; does NOT unmount it.
	public void Finish() {
		if (CAsyncLoad.Active) CAsyncLoad.CancelPhase();
		_script = null;
		_outgoing = null;
		Target = null;
		_phase = ETransitionPhase.Idle;
		base.IsDeActivated = true;
	}
}
