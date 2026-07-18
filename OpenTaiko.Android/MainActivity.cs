using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Media;
using Android.Opengl;
using Android.OS;
using Android.Views;
using Android.Widget;
using ManagedBass;
using Activity = Android.App.Activity;             // disambiguate from System.Diagnostics.Activity
using Stopwatch = System.Diagnostics.Stopwatch;
using Encoding = System.Text.Encoding;             // disambiguate from Android.Media.Encoding

namespace OpenTaiko.Android;

/// <summary>
/// The Android host for OpenTaiko — the counterpart of the iOS port's GameViewController.
/// A SurfaceView provides the EGL window; a dedicated render thread owns the GL context, the game
/// (constructed ON that thread so NLua stays thread-affine), and the frame loop: render the hosted
/// frame off-screen at the skin's logical resolution, then blit aspect-fit to the surface and
/// eglSwapBuffers (vsync-paced). Touches become single-frame HID key pulses exactly like iOS.
/// Init order matters and mirrors the iOS host: audio props → storage roots/extraction →
/// input devices → GL context → game ctor → InitWithExternalContext.
/// </summary>
[Activity(
	Label = "OpenTaiko",
	MainLauncher = true,
	ScreenOrientation = ScreenOrientation.SensorLandscape,
	Theme = "@android:style/Theme.NoTitleBar.Fullscreen",
	LaunchMode = LaunchMode.SingleTask,
	ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.KeyboardHidden | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.UiMode)]
public class MainActivity : Activity, ISurfaceHolderCallback {
	private SurfaceView? _surfaceView;
	private TouchOverlayView? _overlay;
	private TextView? _loadingLabel;

	private AndroidGLContext? _glContext;
	private global::OpenTaiko.OpenTaiko? _game;
	private CInputKeyboard_Android? _input;

	private Thread? _renderThread;
	private volatile bool _running;          // render loop lifetime
	private volatile bool _exitRequested;    // the game's exit stage ran → close the app
	private volatile bool _paused;           // activity paused → loop idles
	private volatile bool _surfaceReady;
	private volatile int _surfaceW, _surfaceH;
	private readonly ConcurrentQueue<Action> _renderActions = new();   // host actions on the GL thread
	private readonly AutoResetEvent _wake = new(false);

	// Off-screen render target at the skin's logical resolution (recreated when the skin changes it)
	private int _fbo, _fboTex, _fboDepth, _fboW, _fboH;
	private int _lastDrumVisual = -1;

	protected override void OnCreate(Bundle? savedInstanceState) {
		base.OnCreate(savedInstanceState);
		// Invariant culture must hold on EVERY thread (the game runs on the render thread and
		// spawns loaders), not just this one — a comma-decimal device locale otherwise breaks
		// numeric parsing. Desktop gets this via Program.Main on its single main thread.
		Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
		CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
		Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		CrashLog.Install(GetExternalFilesDir(null)!.AbsolutePath);
		CrashLog.FlushPreviousCrashLogs();
		global::Android.Util.Log.Info("OpenTaiko",
			$"MONO_ENV_OPTIONS='{System.Environment.GetEnvironmentVariable("MONO_ENV_OPTIONS")}'");

		var root = new FrameLayout(this);
		_surfaceView = new SurfaceView(this);
		_surfaceView.Holder!.AddCallback(this);
		root.AddView(_surfaceView);
		_overlay = new TouchOverlayView(this);
		root.AddView(_overlay);
		_loadingLabel = new TextView(this) {
			Text = "Preparing game data…",
			TextSize = 18,
			Gravity = GravityFlags.Center,
		};
		_loadingLabel.SetTextColor(global::Android.Graphics.Color.White);
		_loadingLabel.SetBackgroundColor(global::Android.Graphics.Color.Black);
		root.AddView(_loadingLabel);
		SetContentView(root);
		ApplyImmersiveMode();
	}

	public override void OnWindowFocusChanged(bool hasFocus) {
		base.OnWindowFocusChanged(hasFocus);
		if (hasFocus) ApplyImmersiveMode();
	}

	private void ApplyImmersiveMode() {
		if (Build.VERSION.SdkInt >= BuildVersionCodes.R) {
			Window?.SetDecorFitsSystemWindows(false);
			var controller = Window?.InsetsController;
			if (controller != null) {
				controller.Hide(WindowInsets.Type.SystemBars());
				controller.SystemBarsBehavior = (int)WindowInsetsControllerBehavior.ShowTransientBarsBySwipe;
			}
		} else {
#pragma warning disable CA1422
			Window!.DecorView.SystemUiFlags = SystemUiFlags.ImmersiveSticky
				| SystemUiFlags.Fullscreen | SystemUiFlags.HideNavigation
				| SystemUiFlags.LayoutFullscreen | SystemUiFlags.LayoutHideNavigation;
#pragma warning restore CA1422
		}
	}

	// ── surface lifecycle ────────────────────────────────────────────────────────────────────────
	public void SurfaceCreated(ISurfaceHolder holder) {
		_surfaceReady = true;
		if (_renderThread == null) {
			_running = true;
			_renderThread = new Thread(RenderMain) { Name = "OpenTaiko GL", IsBackground = false };
			_renderThread.Start();
		} else {
			// Surface came back after backgrounding: rebuild the EGL window surface on the GL thread.
			_renderActions.Enqueue(() => {
				_glContext?.CreateWindowSurface(_surfaceView!.Holder!.Surface!);
				_glContext?.MakeCurrent();
			});
		}
		_wake.Set();
	}

	public void SurfaceChanged(ISurfaceHolder holder, global::Android.Graphics.Format format, int width, int height) {
		_surfaceW = width; _surfaceH = height;
		_renderActions.Enqueue(() => _game?.ResizeViewport(width, height));
		_wake.Set();
	}

	public void SurfaceDestroyed(ISurfaceHolder holder) {
		_surfaceReady = false;
		// Park the context on a pbuffer so GL resources survive; block until done so Android can
		// safely release the Surface after this callback returns.
		using var done = new ManualResetEventSlim(false);
		_renderActions.Enqueue(() => { _glContext?.ParkContext(); done.Set(); });
		_wake.Set();
		done.Wait(TimeSpan.FromSeconds(3));
	}

	// ── render thread ────────────────────────────────────────────────────────────────────────────
	private void RenderMain() {
		try {
			InitializeGameOnRenderThread();
		} catch (Exception ex) {
			CrashLog.Write(ex, "GameInit");
			global::Android.Util.Log.Error("OpenTaiko", $"Game init failed: {ex}");
			RunOnUiThread(() => { if (_loadingLabel != null) _loadingLabel.Text = $"Startup failed:\n{ex.Message}"; });
			return;
		}
		RunOnUiThread(() => _loadingLabel!.Visibility = ViewStates.Gone);

		var sw = Stopwatch.StartNew();
		double last = 0;
		while (_running) {
			while (_renderActions.TryDequeue(out var act)) {
				try { act(); } catch (Exception ex) { global::Android.Util.Log.Warn("OpenTaiko", $"render action: {ex}"); }
			}
			if (_paused || !_surfaceReady || _glContext == null || !_glContext.HasWindowSurface) {
				_wake.WaitOne(100);
				continue;
			}

			double now = sw.Elapsed.TotalSeconds;
			double delta = last > 0 ? now - last : 1.0 / 60.0;
			last = now;

			try {
				RenderOneFrame(delta);
			} catch (Exception ex) {
				// Keep the crash readable on screen instead of a silent process death.
				CrashLog.Write(ex, "RenderFrame");
				RunOnUiThread(() => {
					if (_loadingLabel != null) {
						_loadingLabel.Text = $"Crashed:\n{ex.Message}\n(see files/CrashLogs)";
						_loadingLabel.Visibility = ViewStates.Visible;
					}
				});
				break;
			}
		}

		_game?.ShutdownHosted();
		_game?.Dispose();
		_glContext?.Dispose();

		// The game asked to exit (exit stage): close the activity and end the process — mono's
		// background threads would otherwise keep the "closed" app alive in a broken half-state.
		// The short delay lets the finish/task-removal animation land before the process dies.
		if (_exitRequested) {
			RunOnUiThread(() => {
				FinishAndRemoveTask();
				new global::Android.OS.Handler(global::Android.OS.Looper.MainLooper!).PostDelayed(
					() => global::Android.OS.Process.KillProcess(global::Android.OS.Process.MyPid()), 400);
			});
		}
	}

	private void RenderOneFrame(double delta) {
		if (_game == null) return;

		// Apply UI-thread input events on this thread before the game update (see CInputKeyboard_Android).
		_input?.FlushQueuedKeys();

		// Track the touch-drum size setting (mirrors the iOS host's per-frame overlay rebuild)
		int visual = global::OpenTaiko.OpenTaiko.ConfigIni?.nTouchDrumVisual ?? 30;
		bool navMode = global::OpenTaiko.OpenTaiko.rCurrentStage?.eStageID == CStage.EStage.Config;
		if (visual != _lastDrumVisual || navMode != _overlay!.ArrowNavMode) {
			_lastDrumVisual = visual;
			RunOnUiThread(() => {
				_overlay!.DonRadiusPercent = visual;
				_overlay.ArrowNavMode = navMode;
				_overlay.Invalidate();
			});
		}

		EnsureRenderTarget(FDK.GameWindowSize.Width, FDK.GameWindowSize.Height);
		_game.HostRenderTargetFbo = (uint)_fbo;
		_game.RenderHostedFrame(delta);

		// Present: aspect-fit blit of the logical-resolution target onto the surface.
		GLES30.GlBindFramebuffer(GLES30.GlReadFramebuffer, _fbo);
		GLES30.GlBindFramebuffer(GLES30.GlDrawFramebuffer, 0);
		GLES30.GlViewport(0, 0, _surfaceW, _surfaceH);
		GLES30.GlClearColor(0, 0, 0, 1);
		GLES30.GlClear(GLES30.GlColorBufferBit);
		double scale = Math.Min((double)_surfaceW / _fboW, (double)_surfaceH / _fboH);
		int dw = (int)(_fboW * scale), dh = (int)(_fboH * scale);
		int dx = (_surfaceW - dw) / 2, dy = (_surfaceH - dh) / 2;
		GLES30.GlBlitFramebuffer(0, 0, _fboW, _fboH, dx, dy, dx + dw, dy + dh,
			GLES30.GlColorBufferBit, GLES30.GlLinear);
		_glContext!.SwapBuffers();

		// Touch pulses last one frame (identical to the iOS host)
		_input?.ReleaseTouchKeys();
	}

	private void EnsureRenderTarget(int w, int h) {
		if (_fbo != 0 && _fboW == w && _fboH == h) return;
		if (_fbo != 0) {
			GLES30.GlDeleteFramebuffers(1, new[] { _fbo }, 0);
			GLES30.GlDeleteTextures(1, new[] { _fboTex }, 0);
			GLES30.GlDeleteRenderbuffers(1, new[] { _fboDepth }, 0);
		}
		int[] id = new int[1];
		GLES30.GlGenTextures(1, id, 0); _fboTex = id[0];
		GLES30.GlBindTexture(GLES30.GlTexture2d, _fboTex);
		GLES30.GlTexImage2D(GLES30.GlTexture2d, 0, GLES30.GlRgba8, w, h, 0, GLES30.GlRgba, GLES30.GlUnsignedByte, null);
		GLES30.GlTexParameteri(GLES30.GlTexture2d, GLES30.GlTextureMinFilter, GLES30.GlLinear);
		GLES30.GlTexParameteri(GLES30.GlTexture2d, GLES30.GlTextureMagFilter, GLES30.GlLinear);
		GLES30.GlGenRenderbuffers(1, id, 0); _fboDepth = id[0];
		GLES30.GlBindRenderbuffer(GLES30.GlRenderbuffer, _fboDepth);
		GLES30.GlRenderbufferStorage(GLES30.GlRenderbuffer, GLES30.GlDepth24Stencil8, w, h);
		GLES30.GlGenFramebuffers(1, id, 0); _fbo = id[0];
		GLES30.GlBindFramebuffer(GLES30.GlFramebuffer, _fbo);
		GLES30.GlFramebufferTexture2D(GLES30.GlFramebuffer, GLES30.GlColorAttachment0, GLES30.GlTexture2d, _fboTex, 0);
		GLES30.GlFramebufferRenderbuffer(GLES30.GlFramebuffer, GLES30.GlDepthStencilAttachment, GLES30.GlRenderbuffer, _fboDepth);
		_fboW = w; _fboH = h;
	}

	// ── game bring-up (on the render thread; mirrors GameViewController.InitializeGame order) ────
	private void InitializeGameOnRenderThread() {
		// 1) Audio: the device's native rate + burst size make BASS's AAudio output take the
		//    low-latency fast-mixer path (see FDK CSoundDeviceBASS.Android.cs).
		var audioManager = (AudioManager?)GetSystemService(AudioService);
		if (audioManager != null) {
			if (int.TryParse(audioManager.GetProperty(AudioManager.PropertyOutputSampleRate), out int rate))
				FDK.CSoundDeviceBASS.AndroidSampleRate = rate;
			if (int.TryParse(audioManager.GetProperty(AudioManager.PropertyOutputFramesPerBuffer), out int frames))
				FDK.CSoundDeviceBASS.AndroidFramesPerBuffer = frames;
			global::Android.Util.Log.Info("OpenTaiko",
				$"Audio device: rate={FDK.CSoundDeviceBASS.AndroidSampleRate} framesPerBuffer={FDK.CSoundDeviceBASS.AndroidFramesPerBuffer}");
			RequestAudioFocus(audioManager);
		}

		// 2) Storage: extract bundled data once, then root the whole game there (the shared code
		//    resolves everything against the current directory).
		string dataRoot = GetExternalFilesDir(null)!.AbsolutePath;
		AssetExtractor.EnsureExtracted(this, dataRoot, (done, total) =>
			RunOnUiThread(() => _loadingLabel!.Text = $"Preparing game data… {done}/{total}"));
		// Songs are user-provided (not bundled — see the csproj note): make the folder they drop
		// charts into over USB/MTP.
		Directory.CreateDirectory(Path.Combine(dataRoot, "Songs"));
		// Offer the full official soundtrack as an in-app download (resumable; user can decline).
		global::OpenTaiko.SoundtrackDownloader.EnsureSoundtrack(new AndroidSoundtrackDownloadHost(this), dataRoot,
			s => RunOnUiThread(() => { if (_loadingLabel != null) _loadingLabel.Text = s; }));
		Directory.SetCurrentDirectory(dataRoot);

		// 3) Input + GL context, then the game itself.
		_input = new CInputKeyboard_Android();
		global::OpenTaiko.OpenTaiko.ExternalInputDevices = new List<FDK.IInputDevice> { _input };

		_glContext = new AndroidGLContext();
		_glContext.CreateWindowSurface(_surfaceView!.Holder!.Surface!);
		_glContext.MakeCurrent();
		_glContext.SwapInterval(1);

		// The exit stage calls Game.Exit(): end the render loop, which shuts the game down
		// (config/score save via ShutdownHosted) and then closes the activity.
		FDK.Game.HostExitRequested = () => { _exitRequested = true; _running = false; _wake.Set(); };

		_game = new global::OpenTaiko.OpenTaiko();
		_game.InitWithExternalContext(_glContext, _surfaceW, _surfaceH);

		// 4) Native text input: same shared hook the iOS host uses.
		global::OpenTaiko.CTextInput.iOSTextInputHandler = (currentText, maxLength, callback) => {
			RunOnUiThread(() => {
				var edit = new EditText(this) { Text = currentText };
				new AlertDialog.Builder(this)
					.SetTitle("Enter text")!
					.SetView(edit)!
					.SetPositiveButton("OK", (_, _) => {
						string text = edit.Text ?? "";
						if (text.Length > maxLength) text = text[..(int)maxLength];
						callback(text);
					})!
					.SetNegativeButton("Cancel", (_, _) => callback(null))!
					.SetCancelable(false)!
					.Show();
			});
		};
	}

	private void RequestAudioFocus(AudioManager audioManager) {
		if (Build.VERSION.SdkInt < BuildVersionCodes.O) return;   // AudioFocusRequest is API 26+
		try {
			var attrs = new AudioAttributes.Builder()
				.SetUsage(AudioUsageKind.Game)!
				.SetContentType(AudioContentType.Music)!
				.Build()!;
			var request = new AudioFocusRequestClass.Builder(AudioFocus.Gain)
				.SetAudioAttributes(attrs)!
				.Build()!;
			audioManager.RequestAudioFocus(request);
		} catch (Exception ex) {
			global::Android.Util.Log.Warn("OpenTaiko", $"Audio focus request failed: {ex.Message}");
		}
	}

	// ── input events ─────────────────────────────────────────────────────────────────────────────
	public override bool OnTouchEvent(MotionEvent? e) {
		if (e == null || _input == null || _overlay == null) return base.OnTouchEvent(e);
		int action = (int)e.ActionMasked;
		if (action == (int)MotionEventActions.Down || action == (int)MotionEventActions.PointerDown) {
			int idx = e.ActionIndex;
			long hid = _overlay.HitTest(e.GetX(idx), e.GetY(idx));
			if (hid >= 0) _input.TouchKeyDown(hid);
			return true;
		}
		return true;   // consume moves/ups: touches are single-frame pulses
	}

	public override bool OnKeyDown(Keycode keyCode, KeyEvent? e) {
		if (keyCode is Keycode.VolumeUp or Keycode.VolumeDown or Keycode.VolumeMute)
			return base.OnKeyDown(keyCode, e);
		long hid = CInputKeyboard_Android.KeycodeToHid(keyCode);
		if (hid != 0 && _input != null) { _input.KeyDown(hid); return true; }
		return base.OnKeyDown(keyCode, e);
	}

	public override bool OnKeyUp(Keycode keyCode, KeyEvent? e) {
		if (keyCode is Keycode.VolumeUp or Keycode.VolumeDown or Keycode.VolumeMute)
			return base.OnKeyUp(keyCode, e);
		long hid = CInputKeyboard_Android.KeycodeToHid(keyCode);
		if (hid != 0 && _input != null) { _input.KeyUp(hid); return true; }
		return base.OnKeyUp(keyCode, e);
	}

	// ── app lifecycle ────────────────────────────────────────────────────────────────────────────
	protected override void OnPause() {
		base.OnPause();
		// During gameplay, pulse ESC and render one frame so the in-game pause engages (iOS parity),
		// then idle the loop and silence BASS.
		if (_game != null && _input != null &&
			global::OpenTaiko.OpenTaiko.rCurrentStage?.eStageID == CStage.EStage.Game) {
			using var done = new ManualResetEventSlim(false);
			_renderActions.Enqueue(() => {
				_input.KeyDown(TouchOverlayView.HID_ESC);
				RenderOneFrame(1.0 / 60.0);
				_input.KeyUp(TouchOverlayView.HID_ESC);
				done.Set();
			});
			_wake.Set();
			done.Wait(TimeSpan.FromSeconds(1));
		}
		_paused = true;
		try { Bass.Pause(); } catch { }
	}

	protected override void OnResume() {
		base.OnResume();
		ApplyImmersiveMode();
		try { Bass.Start(); } catch { }
		_paused = false;
		_wake.Set();
	}

	public override void OnTrimMemory(TrimMemory level) {
		base.OnTrimMemory(level);
		if (level >= TrimMemory.RunningLow) {
			_renderActions.Enqueue(() =>
				FDK.CTexture.EvictLeastRecentlyDrawnDownTo(512L * 1024 * 1024));
			_wake.Set();
		}
	}

	protected override void OnDestroy() {
		_running = false;
		_wake.Set();
		_renderThread?.Join(TimeSpan.FromSeconds(5));
		base.OnDestroy();
	}
}
