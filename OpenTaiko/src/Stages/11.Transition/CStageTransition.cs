using System.Diagnostics;
using FDK;

namespace OpenTaiko;

// Options that distinguish the few transition flavours (set by the caller; sensible defaults = a plain
// stage-entry load). Everything else is uniform: FadeOut → Load → FadeIn driven by a single CLoadSession.
internal struct TransitionOptions {
	public bool NoAssetPhase;     // skip the CAsyncLoad asset phase (song load streams its own game-screen textures)
	public bool LoaderDrivesBar;  // song load: the load source owns CLoadingProgress + the screen shows immediately;
	                              //   otherwise the transition blends source/asset progress + anti-blink-holds the cover
	public bool RevealsGameplay;  // draw the note chips through the load + fade-in (song load → gameplay)
	public CStage? CancelTarget;  // ESC during the load → go here (song load → song select)
}

// Activate a Lua stage incrementally as an IStepLoad: BeginActivate, then StepActivate each frame (self-limiting
// via the time-based coroutine yield), then finish + create its managed/unmanaged resources.
internal sealed class StageActivateStep : IStepLoad {
	private readonly LuaStageWrapper _target;
	private bool _begun;
	public StageActivateStep(LuaStageWrapper target) { _target = target; }
	public LoadStatus Step(out float progress) {
		if (!_begun) { _target.BeginActivate(); _begun = true; }   // assets created while stepping defer to CAsyncLoad
		if (_target.StepActivate(out progress)) return LoadStatus.More;
		_target.FinishActivate();
		if (!OpenTaiko.ConfigIni.PreAssetsLoading) {
			_target.CreateManagedResource();
			_target.CreateUnmanagedResource();
		}
		progress = 1f;
		return LoadStatus.Done;
	}
}

// Activate a non-Lua stage in one shot (synchronous behind the cover); its assets still defer to the phase.
internal sealed class MountStep : IStepLoad {
	private readonly CStage _target;
	public MountStep(CStage target) { _target = target; }
	public LoadStatus Step(out float progress) {
		OpenTaiko.app.MountActivity(_target);
		progress = 1f;
		return LoadStatus.Done;
	}
}

// Drive CStageSongLoading (chart / WAV / game-screen streaming — its own visual is gated off, it just loads +
// reports CLoadingProgress) and map its return value. The game screen survives as the transition's Target.
internal sealed class SongLoadStep : IStepLoad {
	private readonly CStageSongLoading _loader;
	private bool _mounted;
	public SongLoadStep(CStageSongLoading loader) { _loader = loader; }
	public LoadStatus Step(out float progress) {
		if (!_mounted) {
			_loader.TransitionDriven = true;
			OpenTaiko.app.MountActivity(_loader);
			_mounted = true;
		}
		int rv = _loader.Draw();   // steps the load; reports CLoadingProgress; draws nothing (TransitionDriven)
		progress = CLoadingProgress.Progress;
		if (rv == (int)ESongLoadingScreenReturnValue.LoadCanceled) {
			OpenTaiko.app.UnmountActivity(_loader);   // tears down the half-loaded game screen
			_loader.TransitionDriven = false;
			return LoadStatus.Canceled;
		}
		if (rv == (int)ESongLoadingScreenReturnValue.LoadComplete) {
			OpenTaiko.app.UnmountActivity(_loader);   // game screen survives (streaming done)
			_loader.TransitionDriven = false;
			return LoadStatus.Done;
		}
		return LoadStatus.More;
	}
}

// Orchestrates a stage→stage switch driven by a Lua transition component. Set as the current stage by
// OpenTaiko.UnmountAndChangeStage / EnterSongLoad when a Lua Exit(...) (or the play path) requested a transition,
// then runs in Draw() through three phases:
//   FadeOut — render the outgoing stage under fadeOut(t)               (t 0→1)
//   Load    — unmount the outgoing, drive the CLoadSession to completion behind loading(progress, elapsed)
//             (skipped when there's nothing to load — revealing an already-mounted target)
//   FadeIn  — render the loaded target under fadeIn(t)                 (t 0→1)
// On finish Draw() returns non-zero and the main loop swaps to the (already-mounted) Target — or, if the load was
// cancelled (ESC), to CancelTarget.
internal class CStageTransition : CStage {
	private enum Phase { Idle, FadeOut, Load, FadeIn }
	private Phase _phase = Phase.Idle;

	private const double DefaultFadeSeconds = 0.5;          // a transition may override via FADE_OUT/IN_SECONDS (Lua)
	private const double LoadingScreenDelaySeconds = 0.5;   // shorter loads show no loading screen (anti-blink)
	private const float ActivateBarSpan = 0.8f;             // source fills 0..0.8 of the bar, asset drain 0.8..1

	// Effective fade durations: the transition script's declared override, else the default.
	private double FadeOutSeconds => _script?.FadeOutSeconds ?? DefaultFadeSeconds;
	private double FadeInSeconds => _script?.FadeInSeconds ?? DefaultFadeSeconds;

	private CStage? _outgoing;
	private LuaTransitionWrapper? _script;
	private CLoadSession? _session;     // the unified load driver (null = reveal an already-mounted target)
	private bool _loaderDrivesBar;      // song load: the loader owns CLoadingProgress + the screen shows at once
	private bool _revealsGameplay;      // song load → gameplay: draw note chips through the load + fade-in
	private CStage? _cancelTarget;      // ESC during the load → here
	private long _phaseStart;
	private long _loadStart;
	private long _lastTickTs;           // for advancing the loading-bar easing each Load frame

	// The stage being transitioned to (mounted by this transition / the load; the main loop reads it once finished).
	public CStage? Target { get; private set; }
	public int? TargetDrawLoopReturnValue { get; private set; }

	// Set when a load was cancelled (ESC): the main loop sends the player to CancelTarget instead of Target.
	public bool Canceled { get; private set; }
	public CStage? CancelTarget { get; private set; }

	// True while fading the song-loading screen out to reveal gameplay: the main loop draws the note chips during
	// this phase (the loading screen fades over them) so notes stay visible as the screen clears.
	public bool RevealingGameplay => _revealsGameplay && _phase == Phase.FadeIn;

	// Pending-script handoff: set by the requesting stage's Exit, consumed by UnmountAndChangeStage.
	private static (CStage stage, LuaTransitionWrapper script)? _pendingScript;
	public static void SetPendingScript(CStage stage, LuaTransitionWrapper script) => _pendingScript = (stage, script);
	public static (CStage stage, LuaTransitionWrapper script)? ConsumePendingScript() { var s = _pendingScript; _pendingScript = null; return s; }
	public static void ClearPendingScript() => _pendingScript = null;

	// The right load step for a target stage: a Lua stage activates incrementally; anything else mounts in one shot.
	public static IStepLoad ActivateStep(CStage target)
		=> target is LuaStageWrapper lw ? new StageActivateStep(lw) : new MountStep(target);

	public CStageTransition() {
		base.eStageID = CStage.EStage.Transition;
		base.IsDeActivated = true;
	}

	// The single entry point. `load` == null reveals an already-mounted `target` (no load — e.g. the legacy
	// song-loading → gameplay handoff). `outgoing` is still mounted (rendered during fade-out, then unmounted).
	public void Begin(CStage? outgoing, CStage target, IStepLoad? load, in TransitionOptions opts,
	                  LuaTransitionWrapper script, string? traceMessage) {
		if (traceMessage != null) {
			Trace.TraceInformation("----------------------");
			Trace.TraceInformation($"■ {traceMessage} (transition)");
		}
		_outgoing = outgoing;
		Target = target;
		TargetDrawLoopReturnValue = null;
		_session = load != null ? new CLoadSession(load, manageAssetPhase: !opts.NoAssetPhase) : null;
		_loaderDrivesBar = opts.LoaderDrivesBar;
		_revealsGameplay = opts.RevealsGameplay;
		_cancelTarget = opts.CancelTarget;
		_script = script;
		Canceled = false;
		CancelTarget = null;
		_phase = Phase.FadeOut;
		_phaseStart = Stopwatch.GetTimestamp();
		base.IsDeActivated = false;
	}

	private static double Elapsed(long since) => Stopwatch.GetElapsedTime(since).TotalSeconds;

	// Draw the loading visual, or hold the full cover during the <0.5 s anti-blink window (stage entry only).
	private void DrawLoading(float progress) {
		double el = Elapsed(_loadStart);
		if (el > LoadingScreenDelaySeconds) _script?.Loading(progress, el);
		else _script?.FadeOut(1.0);
	}

	public override int Draw() {
		switch (_phase) {
			case Phase.FadeOut: {
				int ret = _outgoing?.Draw() ?? 0;
				bool endEarly = (EReturnValue)ret != EReturnValue.Continuation;
				double t = endEarly ? 1.0 : Math.Clamp(Elapsed(_phaseStart) / FadeOutSeconds, 0.0, 1.0);
				_script?.FadeOut(t);
				if (t >= 1.0) {
					OpenTaiko.app.UnmountActivity(_outgoing);
					_outgoing = null;
					_loadStart = Stopwatch.GetTimestamp();
					if (_session == null) {                 // nothing to load → reveal the already-mounted target
						_phase = Phase.FadeIn;
						_phaseStart = _loadStart;
					} else {
						CLoadingProgress.Begin();           // reset the bar (Report is monotonic) + start easing
						_lastTickTs = 0;
						_session.Begin();
						_phase = Phase.Load;
					}
				}
				return 0;
			}

			case Phase.Load: {
				// Advance the bar easing (the transition draws the bar via loading() — unlike CLoadingScreen.Draw
				// it must tick the smoothing itself, so a coarse/0→100 source fills smoothly instead of snapping).
				long now = Stopwatch.GetTimestamp();
				if (_lastTickTs != 0) CLoadingProgress.Tick(Stopwatch.GetElapsedTime(_lastTickTs, now).TotalMilliseconds);
				_lastTickTs = now;

				bool more = _session!.Step();
				if (_session.Canceled) {
					_session.Cancel();
					Canceled = true;
					CancelTarget = _cancelTarget;
					_phase = Phase.Idle;
					CLoadingProgress.End();
					return 1;   // main loop sends the player to CancelTarget
				}
				if (_loaderDrivesBar) {
					// The loader (CStageSongLoading) Reports the raw target; draw the EASED value.
					_script?.Loading(CLoadingProgress.DisplayProgress, Elapsed(_loadStart));
				} else {
					float bar = _session.SourceDone ? ActivateBarSpan + (1f - ActivateBarSpan) * _session.AssetFraction
					                                : ActivateBarSpan * _session.SourceProgress;
					CLoadingProgress.Report(bar);   // monotonic raw target → eased by Tick above
					DrawLoading(CLoadingProgress.DisplayProgress);
				}
				if (!more) {
					_session.End();
					_phase = Phase.FadeIn;
					_phaseStart = Stopwatch.GetTimestamp();
					CLoadingProgress.End();
				}
				return 0;
			}

			case Phase.FadeIn: {
				var ret = Target?.Draw() ?? 0;
				bool endEarly = (EReturnValue)ret != EReturnValue.Continuation;
				if (endEarly)
					TargetDrawLoopReturnValue = ret;
				double t = endEarly ? 1.0 : Math.Clamp(Elapsed(_phaseStart) / FadeInSeconds, 0.0, 1.0);
				_script?.FadeIn(t);
				if (t >= 1.0) {
					_phase = Phase.Idle;
					return 1;   // done — main loop swaps rCurrentStage to Target
				}
				return 0;
			}
		}
		return 0;
	}

	// Release the hold once the main loop has taken over the (already-mounted) target; does NOT unmount it.
	public void Finish() {
		_session?.Cancel();
		_script = null;
		_outgoing = null;
		_session = null;
		_loaderDrivesBar = false;
		_revealsGameplay = false;
		Canceled = false;
		Target = null;
		_cancelTarget = null;
		CancelTarget = null;
		_phase = Phase.Idle;
		base.IsDeActivated = true;
	}
}
