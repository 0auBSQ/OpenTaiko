using System.Diagnostics;
using FDK;

namespace OpenTaiko;

// Orchestrates a stage→stage switch driven by a Lua transition component. Set as the current stage by
// OpenTaiko.UnmountAndChangeStage when a Lua Exit(...) requested a transition, then runs in Draw():
//   FadeOut    — render the outgoing stage under fadeOut(t)      (t 0→1)
//   BeginLoad  — unmount outgoing, open the async-load phase, begin activating the target behind the cover
//   Activating — (Lua targets) step `activate` as a coroutine each frame so a heavy activate doesn't freeze
//   Draining   — pump the deferred asset loads; past ~0.5 s draw loading(progress, elapsed) (else hold cover)
//   FadeIn     — render the loaded target under fadeIn(t)        (t 0→1)
// On finish Draw() returns non-zero and the main loop swaps to the (already-mounted) Target.
internal class CStageTransition : CStage {
	private enum ETransitionPhase { Idle, FadeOut, BeginLoad, Activating, Draining, LoadingSong, FadeIn }
	private ETransitionPhase _phase = ETransitionPhase.Idle;

	private const double FadeOutSeconds = 0.25;
	private const double FadeInSeconds = 0.25;
	private const double LoadingScreenDelaySeconds = 0.5;   // shorter loads show no loading screen (anti-blink)
	private const float ActivateBarSpan = 0.8f;             // activate coroutine fills 0..0.8, asset drain 0.8..1

	private CStage? _outgoing;
	private LuaTransitionWrapper? _script;
	private LuaStageWrapper? _luaTarget;   // non-null when the target activates incrementally (a Lua stage)
	private bool _revealOnly;              // target is already mounted (e.g. song-loading → gameplay): just fade
	private bool _songLoad;                // drive CStageSongLoading (chart/WAV/game-screen) → fade into gameplay
	private CStage? _cancelTarget;         // where to go if the song load is cancelled (ESC) — song select
	private long _phaseStart;
	private long _loadStart;

	// The stage being transitioned to (mounted by this transition; the main loop reads it once finished).
	public CStage? Target { get; private set; }

	// Set when a song load was cancelled (ESC): the main loop sends the player to CancelTarget instead of Target.
	public bool Canceled { get; private set; }
	public CStage? CancelTarget { get; private set; }

	// True while fading the song-loading screen out to reveal gameplay: the main loop draws the note chips
	// during this phase (the loading screen fades over them) so notes stay visible as the screen clears.
	public bool RevealingGameplay => _songLoad && _phase == ETransitionPhase.FadeIn;

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
		_luaTarget = target as LuaStageWrapper;
		_revealOnly = false;
		_songLoad = false;
		Canceled = false;
		_script = script;
		_phase = ETransitionPhase.FadeOut;
		_phaseStart = Stopwatch.GetTimestamp();
		base.IsDeActivated = false;
	}

	// Song-select → gameplay as ONE transition: fade the outgoing (song select) out, drive CStageSongLoading
	// (chart / WAV / game-screen streaming — its own visual + bar shown via loading()), then fade the loaded
	// game screen in. ESC during the load → Canceled + CancelTarget (back to song select).
	public void BeginSongLoad(CStage? outgoing, CStage gameplayTarget, CStage cancelTarget, LuaTransitionWrapper script, string? traceMessage) {
		if (traceMessage != null) {
			Trace.TraceInformation("----------------------");
			Trace.TraceInformation($"■ {traceMessage} (song-load transition)");
		}
		_outgoing = outgoing;
		Target = gameplayTarget;
		_cancelTarget = cancelTarget;
		_luaTarget = null;
		_revealOnly = false;
		_songLoad = true;
		Canceled = false;
		CancelTarget = null;
		_script = script;
		_phase = ETransitionPhase.FadeOut;
		_phaseStart = Stopwatch.GetTimestamp();
		base.IsDeActivated = false;
	}

	// Fade from an outgoing stage to an ALREADY-mounted target (no activate / load). Used as the legacy
	// song-loading → gameplay handoff when no song_loading transition module exists.
	public void BeginReveal(CStage? outgoing, CStage target, LuaTransitionWrapper script, string? traceMessage) {
		if (traceMessage != null) {
			Trace.TraceInformation("----------------------");
			Trace.TraceInformation($"■ {traceMessage} (reveal)");
		}
		_outgoing = outgoing;
		Target = target;
		_luaTarget = null;
		_revealOnly = true;
		_songLoad = false;
		Canceled = false;
		_script = script;
		_phase = ETransitionPhase.FadeOut;
		_phaseStart = Stopwatch.GetTimestamp();
		base.IsDeActivated = false;
	}

	private static double Elapsed(long since) => Stopwatch.GetElapsedTime(since).TotalSeconds;

	// Draw the loading visual, or hold the full cover during the <0.5 s anti-blink window.
	private void DrawLoading(float progress) {
		double el = Elapsed(_loadStart);
		if (el > LoadingScreenDelaySeconds) _script?.Loading(progress, el);
		else _script?.FadeOut(1.0);
	}

	public override int Draw() {
		switch (_phase) {
			case ETransitionPhase.FadeOut: {
				_outgoing?.Draw();
				double t = Math.Clamp(Elapsed(_phaseStart) / FadeOutSeconds, 0.0, 1.0);
				_script?.FadeOut(t);
				if (t >= 1.0) {
					OpenTaiko.app.UnmountActivity(_outgoing);
					_outgoing = null;
					if (_revealOnly) {                    // target already mounted → straight to fade-in
						_phase = ETransitionPhase.FadeIn;
						_phaseStart = Stopwatch.GetTimestamp();
					} else if (_songLoad) {               // mount the song loader (load only — we draw the screen)
						OpenTaiko.stageSongLoading.TransitionDriven = true;
						OpenTaiko.app.MountActivity(OpenTaiko.stageSongLoading);
						_loadStart = Stopwatch.GetTimestamp();
						_phase = ETransitionPhase.LoadingSong;
					} else {
						_phase = ETransitionPhase.BeginLoad;
					}
				}
				return 0;
			}

			case ETransitionPhase.BeginLoad: {
				_script?.FadeOut(1.0);   // keep covered this frame
				_loadStart = Stopwatch.GetTimestamp();
				CAsyncLoad.BeginPhase();
				if (_luaTarget != null) {
					_luaTarget.BeginActivate();   // assets created while stepping defer to CAsyncLoad
					_phase = ETransitionPhase.Activating;
				} else {
					OpenTaiko.app.MountActivity(Target);   // non-Lua target: synchronous activate behind the cover
					CAsyncLoad.StartDecode();
					_phase = ETransitionPhase.Draining;
				}
				return 0;
			}

			case ETransitionPhase.Activating: {
				bool more = _luaTarget!.StepActivate(out float p);   // sound/shared finalizers auto-drain via the render loop
				DrawLoading(ActivateBarSpan * Math.Clamp(p, 0f, 1f));
				if (!more) {
					_luaTarget.FinishActivate();
					if (!OpenTaiko.ConfigIni.PreAssetsLoading) {
						_luaTarget.CreateManagedResource();
						_luaTarget.CreateUnmanagedResource();
					}
					CAsyncLoad.StartDecode();   // decode the textures queued during activate
					_phase = ETransitionPhase.Draining;
				}
				return 0;
			}

			case ETransitionPhase.Draining: {
				CAsyncLoad.Pump(8.0);
				float bar = _luaTarget != null ? ActivateBarSpan + (1f - ActivateBarSpan) * CAsyncLoad.Fraction
				                                : CAsyncLoad.Fraction;
				DrawLoading(bar);
				if (CAsyncLoad.Complete) {
					CAsyncLoad.EndPhase();
					_phase = ETransitionPhase.FadeIn;
					_phaseStart = Stopwatch.GetTimestamp();
				}
				return 0;
			}

			case ETransitionPhase.LoadingSong: {
				// CStageSongLoading draws its own screen + bar and steps the chart/WAV/game-screen load; the
				// transition's loading() overlays (e.g. fades the song-loading screen in over the cover).
				int rv = OpenTaiko.stageSongLoading.Draw();
				// Raw (not DisplayProgress): the smoothing Tick lives in CLoadingScreen.Draw, which we skip here.
				_script?.Loading(CLoadingProgress.Progress, Elapsed(_loadStart));
				if (rv == (int)ESongLoadingScreenReturnValue.LoadCanceled) {
					OpenTaiko.app.UnmountActivity(OpenTaiko.stageSongLoading);   // tears down the half-loaded game screen
					OpenTaiko.stageSongLoading.TransitionDriven = false;
					Canceled = true;
					CancelTarget = _cancelTarget;
					_phase = ETransitionPhase.Idle;
					return 1;   // main loop sends the player to CancelTarget (song select)
				}
				if (rv == (int)ESongLoadingScreenReturnValue.LoadComplete) {
					OpenTaiko.app.UnmountActivity(OpenTaiko.stageSongLoading);   // game screen survives (streaming done)
					OpenTaiko.stageSongLoading.TransitionDriven = false;
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
		_luaTarget = null;
		_revealOnly = false;
		_songLoad = false;
		Canceled = false;
		Target = null;
		_cancelTarget = null;
		CancelTarget = null;
		_phase = ETransitionPhase.Idle;
		base.IsDeActivated = true;
	}
}
