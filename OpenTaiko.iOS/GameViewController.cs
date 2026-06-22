using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using AVFoundation;
using CoreAnimation;
using CoreGraphics;
using Foundation;
using FDK;
using ObjCRuntime;
using OpenGLES;
using UIKit;

namespace OpenTaiko.iOS;

/// <summary>
/// UIView subclass backed by CAMetalLayer: GL renders off-screen into a shared IOSurface and
/// Metal composites + presents it. See MetalPresenter.
/// </summary>
[Register("MetalView")]
public class MetalView : UIView {
	[Export("layerClass")]
	public static new Class GetLayerClass() {
		return new Class(typeof(CAMetalLayer));
	}

	public MetalView(CGRect frame) : base(frame) { }
}

/// <summary>
/// UIViewController that hosts the OpenTaiko game on iOS.
/// Renders the game with OpenGL ES 2.0 into an off-screen IOSurface-backed FBO, presents it
/// through CAMetalLayer (see MetalPresenter), drives the game loop with CADisplayLink, and
/// routes touch events as keyboard key pulses.
/// </summary>
public partial class GameViewController : UIViewController {
	private EAGLContext? _glContext;
	private int _backingWidth;
	private int _backingHeight;
	private CADisplayLink? _displayLink;

	private global::OpenTaiko.OpenTaiko? _game;
	private iOSGLContext? _fdkContext;

	// Presentation (project_ios_render_metal_plan): GL renders off-screen into a shared
	// IOSurface and Metal composites + presents it via CAMetalLayer.
	private MetalPresenter? _metalPresenter;
	private CInputKeyboard_iOS? _keyboardInput;
	private int _autoAdvanceFrame;   // iOS auto-advance Don-pulse counter (see RenderFrame)
	private bool _initialized;
	private NSObject? _resignActiveObserver;
	private NSObject? _becomeActiveObserver;
	private UILabel? _debugHud;
	private int _debugHudFrameCount;

	public override void LoadView() {
		// GL renders off-screen into a shared IOSurface and the CAMetalLayer-backed view
		// composites + presents it. See MetalPresenter.
		View = new MetalView(UIScreen.MainScreen.Bounds);
	}

	public override void ViewDidLoad() {
		base.ViewDidLoad();

		// Enable multi-touch. The Metal-backed view has no EAGL layer to configure.
		View!.MultipleTouchEnabled = true;
		View.ContentScaleFactor = UIScreen.MainScreen.Scale;

		// Create OpenGL ES 2.0 context (used for off-screen rendering)
		_glContext = new EAGLContext(EAGLRenderingAPI.OpenGLES2);
		if (_glContext == null) {
			throw new Exception("Failed to create EAGLContext");
		}
		EAGLContext.SetCurrentContext(_glContext);

		// Register for app lifecycle notifications
		_resignActiveObserver = NSNotificationCenter.DefaultCenter.AddObserver(
			UIApplication.WillResignActiveNotification, _ => OnResignActive());
		_becomeActiveObserver = NSNotificationCenter.DefaultCenter.AddObserver(
			UIApplication.DidBecomeActiveNotification, _ => OnBecomeActive());
	}

	public override void ViewDidLayoutSubviews() {
		base.ViewDidLayoutSubviews();

		Console.WriteLine($"[OpenTaiko] ViewDidLayoutSubviews: bounds={View!.Bounds.Width}x{View.Bounds.Height} scale={View.ContentScaleFactor}");

		// No on-screen GL framebuffer: GL renders off-screen into the presenter's shared FBO
		// (game resolution); the CAMetalLayer drawable is the native-res screen target.
		EAGLContext.SetCurrentContext(_glContext);
		nfloat scale = View.ContentScaleFactor;
		_backingWidth = (int)(View.Bounds.Width * scale);
		_backingHeight = (int)(View.Bounds.Height * scale);
		_metalPresenter ??= new MetalPresenter((CAMetalLayer)View.Layer, _glContext!);
		_metalPresenter.UpdateDrawableSize(_backingWidth, _backingHeight);
		Console.WriteLine($"[OpenTaiko] Metal drawable: {_backingWidth}x{_backingHeight}");
		if (!_initialized) {
			InitializeGame();
			_initialized = true;
		} else {
			_game?.ResizeViewport(_backingWidth, _backingHeight);
		}
	}

	/// <summary>
	/// Copy writable/user-facing assets from the app bundle to the Documents directory.
	/// Only copies if the target doesn't exist yet (first launch or new directory).
	/// Read-only assets (Global/, Lang/, Encyclopedia/, BGScriptAPI.lua) stay in the bundle
	/// and are resolved at runtime via OpenTaiko.ResolveAssetPath().
	/// </summary>
	private static void CopyBundleAssetsToDocuments() {
		string bundlePath = NSBundle.MainBundle.BundlePath;
		string docsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

		// Copy writable directories to Documents. System/ is read directly from
		// the bundle via ResolveAssetPath()/GetMergedDirectories().
		// Songs/ is copied because the game writes uniqueID.json into song folders.
		string[] copyDirs = { "Songs", "Databases", ".init" };

		foreach (string dir in copyDirs) {
			string src = Path.Combine(bundlePath, dir);
			string dst = Path.Combine(docsPath, dir);
			if (!Directory.Exists(src)) continue;
			if (Directory.Exists(dst)) continue;
			CopyDirectory(src, dst);
		}

		// Create empty System/ in Documents so users can add custom skins via file sharing.
		// The bundled skin is resolved at runtime via GetMergedDirectories().
		string systemDir = Path.Combine(docsPath, "System");
		if (!Directory.Exists(systemDir))
			Directory.CreateDirectory(systemDir);
	}

	private static void CopyDirectory(string src, string dst) {
		Directory.CreateDirectory(dst);
		foreach (string file in Directory.GetFiles(src))
			File.Copy(file, Path.Combine(dst, Path.GetFileName(file)));
		foreach (string dir in Directory.GetDirectories(src))
			CopyDirectory(dir, Path.Combine(dst, Path.GetFileName(dir)));
	}

	/// <summary>
	/// Register a DllImport resolver so ManagedBass P/Invoke calls find the iOS xcframeworks.
	/// [DllImport("bass")] → @rpath/bass.framework/bass, etc.
	/// </summary>
	private static void RegisterBassResolver() {
		System.Runtime.InteropServices.DllImportResolver resolver =
			(libraryName, assembly, searchPath) => {
				// Map P/Invoke names to framework paths
				string frameworkName = libraryName switch {
					"bass" => "@rpath/bass.framework/bass",
					"bassmix" => "@rpath/bassmix.framework/bassmix",
					"bass_fx" => "@rpath/bass_fx.framework/bass_fx",
					_ => libraryName
				};
				if (System.Runtime.InteropServices.NativeLibrary.TryLoad(frameworkName, out var handle))
					return handle;
				return IntPtr.Zero;
			};
		// Register for all BASS assemblies (each is a separate DLL)
		System.Runtime.InteropServices.NativeLibrary.SetDllImportResolver(
			typeof(ManagedBass.Bass).Assembly, resolver);
		System.Runtime.InteropServices.NativeLibrary.SetDllImportResolver(
			typeof(ManagedBass.Mix.BassMix).Assembly, resolver);
		System.Runtime.InteropServices.NativeLibrary.SetDllImportResolver(
			typeof(ManagedBass.Fx.BassFx).Assembly, resolver);
	}

	private void InitializeGame() {
		CrashLog.FlushPreviousCrashLogs();

		Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
		Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

		// Activate iOS audio session and measure hardware output latency
		try {
			var audioSession = AVAudioSession.SharedInstance();
			audioSession.SetCategory(AVAudioSessionCategory.Playback);
			audioSession.SetActive(true);
			double hwLatency = audioSession.OutputLatency; // seconds
			double bufferDuration = audioSession.IOBufferDuration; // seconds
			int totalMs = (int)((hwLatency + bufferDuration) * 1000);
			FDK.CSoundDeviceBASS.iOSHardwareLatencyMs = totalMs;
		} catch (Exception ex) {
			System.Diagnostics.Trace.TraceWarning($"AVAudioSession activation failed: {ex.Message}");
		}

		// Register BASS native library resolver before any ManagedBass calls
		RegisterBassResolver();

		// Copy writable/user-facing assets from bundle to Documents directory.
		// Read-only assets (Global/, Lang/, etc.) are resolved from the bundle at runtime.
		CopyBundleAssetsToDocuments();
		global::OpenTaiko.OpenTaiko.strBundleFolder = NSBundle.MainBundle.BundlePath + Path.DirectorySeparatorChar;

		// Create input devices
		_keyboardInput = new CInputKeyboard_iOS();

		// Create the FDK GL context wrapper. The hosted frame draws into the shared FBO and the
		// host presents it via Metal (see OnFrame), so FDK never asks the context to swap buffers.
		_fdkContext = new iOSGLContext(
			swapBuffers: () => { },
			makeCurrent: () => {
				EAGLContext.SetCurrentContext(_glContext);
			}
		);

		// Register input devices for the game's input manager
		global::OpenTaiko.OpenTaiko.ExternalInputDevices = new List<FDK.IInputDevice> { _keyboardInput };

		// Create and initialize the game
		_game = new global::OpenTaiko.OpenTaiko();
		_game.InitWithExternalContext(_fdkContext, _backingWidth, _backingHeight);

		// FDK renders the scene into the presenter's shared FBO; the host presents it. The
		// render-target FBO is (re)sized to GameWindowSize each frame in OnFrame.

		// Add touch overlay controls
		CreateTouchOverlay();
		CreateDebugHud();

		// Register iOS native text input handler for CTextInput (replaces ImGui)
		global::OpenTaiko.CTextInput.iOSTextInputHandler = (currentText, maxLength, callback) => {
			InvokeOnMainThread(() => {
				var alert = UIAlertController.Create(
					"Search",
					null,
					UIAlertControllerStyle.Alert);

				alert.AddTextField(tf => {
					tf.Text = currentText;
					tf.Placeholder = "Enter search text";
					tf.AutocorrectionType = UITextAutocorrectionType.No;
					tf.ReturnKeyType = UIReturnKeyType.Search;
				});

				alert.AddAction(UIAlertAction.Create("OK", UIAlertActionStyle.Default, _ => {
					var text = alert.TextFields?[0].Text ?? "";
					if (text.Length > maxLength)
						text = text[..(int)maxLength];
					callback(text);
				}));

				alert.AddAction(UIAlertAction.Create("Cancel", UIAlertActionStyle.Cancel, _ => {
					callback(null);
				}));

				PresentViewController(alert, true, null);
			});
		};

		// Start the display link
		_displayLink = CADisplayLink.Create(OnFrame);
		// Request the display's full refresh rate (120 Hz on ProMotion) unless the "Frame Rate"
		// setting caps it at 60. Requires CADisableMinimumFrameDurationOnPhone=true in Info.plist
		// to take effect on iPhone. (ConfigIni is loaded by now — InitWithExternalContext ran above.)
		bool unlimitedFps = global::OpenTaiko.OpenTaiko.ConfigIni?.biOSUnlimitedFrameRate ?? false;
		int deviceMax = (int)UIScreen.MainScreen.MaximumFramesPerSecond;
		if (UIDevice.CurrentDevice.CheckSystemVersion(15, 0)) {
			float maxFps = unlimitedFps ? deviceMax : Math.Min(60, deviceMax);
			float minFps = Math.Min(60f, maxFps);
			_displayLink.PreferredFrameRateRange = CAFrameRateRange.Create(minFps, maxFps, maxFps);
		} else {
			_displayLink.PreferredFramesPerSecond = unlimitedFps ? deviceMax : Math.Min(60, deviceMax);
		}
		_displayLink.AddToRunLoop(NSRunLoop.Current, NSRunLoopMode.Default);
	}

	private double _lastTimestamp;
	private int _lastDrumVisual = -1;
	private void OnFrame() {
		try {
			// Rebuild touch overlay if drum size settings changed
			int currentVisual = global::OpenTaiko.OpenTaiko.ConfigIni?.nTouchDrumVisual ?? 30;
			if (currentVisual != _lastDrumVisual) {
				_lastDrumVisual = currentVisual;
				_touchOverlay?.RemoveFromSuperview();
				CreateTouchOverlay();
			}

			if (_game == null) {
				_displayLink?.Invalidate();
				return;
			}

			double now = _displayLink!.Timestamp;
			double delta = _lastTimestamp > 0 ? now - _lastTimestamp : 1.0 / 60.0;
			_lastTimestamp = now;

			EAGLContext.SetCurrentContext(_glContext);
			// Size the shared render target to the current game resolution (skin-dependent,
			// set after init), recreating it on change so it never under/over-sizes the render.
			_game.HostRenderTargetFbo = _metalPresenter!.EnsureRenderTarget(FDK.GameWindowSize.Width, FDK.GameWindowSize.Height);

			// iOS dev auto-advance: drive the Lua stages hands-free for testing. Confirm pulse (Return for the
			// raw KeyboardPressed checks + Don for the Decide action) every 40 frames; between them, navigate
			// forward (DownArrow + Ka) so multi-item menus (e.g. _boot's volume page, where "Continue" is last)
			// reach the bottom before each confirm. Released by ReleaseTouchKeys() below = clean single-frame pulses.
			if (global::OpenTaiko.OpenTaiko.iOSAutoAdvanceUI) {
				_autoAdvanceFrame++;
				// Drive to the song-select softlock. _title's first entry (Performance Mode) already routes to
				// regular_song_select, so DON'T navigate there (navigating scrolls down onto Intro Nokon etc.) —
				// just Decide twice (menu → player-count prompt → song-select). Everywhere else (_boot's volume
				// page needs the last "Continue"; song-select needs to move the cursor + enter a folder) navigate
				// then confirm. Confirm = Return (raw KeyboardPressed) + Don (Decide action); nav = DownArrow + Ka.
				string sn = global::OpenTaiko.OpenTaiko.rCurrentStage?.customStageName ?? "";
				if (_autoAdvanceFrame % 40 == 0) {
					_keyboardInput?.TouchKeyDown(0x28);   // Return
					_keyboardInput?.TouchKeyDown(HID_F);  // Don
				} else if ((sn == "_boot" || sn.Contains("song_select")) && _autoAdvanceFrame % 6 == 0) {
					_keyboardInput?.TouchKeyDown(0x51);   // DownArrow
					_keyboardInput?.TouchKeyDown(HID_K);  // Ka-right
				}
			}

			_game.RenderHostedFrame(delta);

			// Composite the just-rendered shared surface and present it.
			_metalPresenter?.Present();

			// Auto-release all touch-originated keys after the frame processes them.
			// Each touch is a single-frame pulse — the key resets so the next
			// touch-down always registers as a fresh press.
			_keyboardInput?.ReleaseTouchKeys();

			if (++_debugHudFrameCount % 60 == 0)
				UpdateDebugHud();

			if (_debugHudFrameCount % 600 == 0) { // periodic snapshot to OpenTaiko.log (~every 10s)
				LogMemory("periodic");
			}
		} catch (Exception ex) {
			CrashLog.Write(ex, "OnFrame");
			_displayLink?.Invalidate();
			throw;
		}
	}

	public override void DidReceiveMemoryWarning() {
		base.DidReceiveMemoryWarning();
		// An iOS memory warning means the OS thinks we're approaching the jetsam limit — log the
		// footprint so the log shows how close we got, then evict prior-scene textures if over tolerance.
		LogMemory("memory-warning");
		EvictTexturesUnderPressure();
	}

	// Texture memory below which we never evict — prefer keeping everything resident for smooth
	// performance over minimizing footprint. Only above this do we release least-recently-drawn textures.
	private const long TextureMemoryTolerance = 1024L * 1024 * 1024; // 1 GB

	// Release least-recently-drawn file-backed textures when texture memory exceeds the tolerance; they
	// re-decode transparently on their next draw (see CTexture.tCacheOnDraw / EvictLeastRecentlyDrawnDownTo).
	private void EvictTexturesUnderPressure() {
		if (FDK.CTexture.TotalTextureBytes <= TextureMemoryTolerance) return;
		// DeleteTexture needs the GL context current; the warning fires on the main thread between
		// CADisplayLink frames, so the context may not be bound right now.
		if (_glContext != null) EAGLContext.SetCurrentContext(_glContext);
		FDK.CTexture.EvictLeastRecentlyDrawnDownTo(TextureMemoryTolerance);
	}

	// --- Memory diagnostics → OpenTaiko.log ---------------------------------------------------
	// Logs the process phys_footprint (the value iOS jetsam actually enforces) alongside our GL
	// texture total, so the log gives a good idea of how close a session came to being jetsammed.
	private const uint TASK_VM_INFO = 22; // task_info flavor; phys_footprint is at byte offset 144
	[DllImport("libc")] private static extern uint task_self_trap();
	[DllImport("libc")] private static extern int task_info(uint task, uint flavor, byte[] info, ref uint count);

	private static long GetPhysFootprintBytes() {
		try {
			uint count = 87; // TASK_VM_INFO_COUNT — large enough to cover phys_footprint
			byte[] buf = new byte[count * 4];
			if (task_info(task_self_trap(), TASK_VM_INFO, buf, ref count) == 0 && count * 4 >= 152)
				return BitConverter.ToInt64(buf, 144);
		} catch { }
		return -1;
	}

	// Footprint (jetsam-enforced) and managed GC heap in MB. footprint is -1 if unavailable. The
	// managed figure lets us split the footprint into managed vs native: a small managed number with
	// a large footprint means the growth is native memory (e.g. SkiaSharp), not C# objects.
	// GC.GetTotalMemory(false) does not force a collection, so it's cheap enough per-frame.
	private static (long footprint, long managed) GetMemoryMB() {
		long fpBytes = GetPhysFootprintBytes();
		return (
			fpBytes >= 0 ? fpBytes / 1048576 : -1,
			GC.GetTotalMemory(false) / 1048576);
	}

	private void LogMemory(string reason) {
		var (fp, managed) = GetMemoryMB();
		string fpStr = fp >= 0 ? $"{fp} MB" : "?";
		string stage = global::OpenTaiko.OpenTaiko.rCurrentStage?.GetType().Name ?? "-";
		long texMB = FDK.CTexture.s_gpuTextureBytes / 1048576;
		Trace.TraceInformation($"[Mem] {reason}: footprint={fpStr}, managed={managed} MB, gpuTex={FDK.CTexture.s_gpuTextureCount} ({texMB} MB), stage={stage}");
	}

	#region Keyboard Input

	public override void PressesBegan(NSSet<UIPress> presses, UIPressesEvent evt) {
		base.PressesBegan(presses, evt);
		foreach (UIPress press in presses.Cast<UIPress>()) {
			if (press.Key != null)
				_keyboardInput?.KeyDown((long)press.Key.KeyCode);
		}
	}

	public override void PressesEnded(NSSet<UIPress> presses, UIPressesEvent evt) {
		base.PressesEnded(presses, evt);
		foreach (UIPress press in presses.Cast<UIPress>()) {
			if (press.Key != null)
				_keyboardInput?.KeyUp((long)press.Key.KeyCode);
		}
	}

	public override void PressesCancelled(NSSet<UIPress> presses, UIPressesEvent evt) {
		base.PressesCancelled(presses, evt);
		foreach (UIPress press in presses.Cast<UIPress>()) {
			if (press.Key != null)
				_keyboardInput?.KeyUp((long)press.Key.KeyCode);
		}
	}

	#endregion

	public override bool PrefersStatusBarHidden() => true;

	public override bool ShouldAutorotate() => true;

	public override UIInterfaceOrientationMask GetSupportedInterfaceOrientations() {
		return UIInterfaceOrientationMask.Landscape;
	}

	public override UIInterfaceOrientation PreferredInterfaceOrientationForPresentation() {
		return UIInterfaceOrientation.LandscapeLeft;
	}

	#region App Lifecycle

	/// <summary>
	/// Called when the app resigns active (home button, Control Center, phone call, etc.).
	/// During gameplay, sends ESC to trigger in-game pause. Always pauses the display link.
	/// </summary>
	public void OnResignActive() {
		if (_displayLink == null || _game == null) return;

		// Only send ESC during gameplay (other stages: ESC navigates back or exits)
		bool isInGameplay = global::OpenTaiko.OpenTaiko.rCurrentStage?.eStageID == CStage.EStage.Game;
		if (isInGameplay) {
			// Press ESC down, run a frame so the game polls and detects the press, then release
			_keyboardInput?.KeyDown(HID_ESC);

			EAGLContext.SetCurrentContext(_glContext);
			_game.HostRenderTargetFbo = _metalPresenter!.EnsureRenderTarget(FDK.GameWindowSize.Width, FDK.GameWindowSize.Height);
			_game.RenderHostedFrame(1.0 / 60.0);
			_metalPresenter?.Present();

			_keyboardInput?.KeyUp(HID_ESC);
		}

		// Pause the display link
		_displayLink.Paused = true;
	}

	/// <summary>
	/// Called when the app becomes active again.
	/// Resumes the display link only — game stays paused until user manually unpauses.
	/// </summary>
	public void OnBecomeActive() {
		if (_displayLink == null) return;
		_displayLink.Paused = false;
	}

	#endregion

	private void CreateDebugHud() {
		var safeInsets = View!.SafeAreaInsets;
		_debugHud = new UILabel(new CGRect(
			View.Bounds.Width - 220 - safeInsets.Right,
			safeInsets.Top + 4,
			220, 76)) {
			BackgroundColor = UIColor.Black.ColorWithAlpha(0.5f),
			TextColor = UIColor.FromRGBA(0x00, 0xFF, 0x00, 0xCC),
			Font = UIFont.FromName("Menlo", 10) ?? UIFont.SystemFontOfSize(10),
			Lines = 0,
			TextAlignment = UITextAlignment.Left,
			UserInteractionEnabled = false,
		};
		_debugHud.Layer.CornerRadius = 6;
		_debugHud.ClipsToBounds = true;
		View.AddSubview(_debugHud);
	}

	private void UpdateDebugHud() {
		if (_debugHud == null) return;
		int fps = global::OpenTaiko.OpenTaiko.FPS?.NowFPS ?? 0;
		string stage = global::OpenTaiko.OpenTaiko.rCurrentStage?.eStageID.ToString() ?? "?";
		var (fp, managed) = GetMemoryMB();
		string fpStr = fp >= 0 ? fp.ToString() : "?";
		_debugHud.Text = $" FPS: {fps}  Stage: {stage}\n GL: {_backingWidth}x{_backingHeight}\n Mem: fp{fpStr} mg{managed} MB";
	}

	protected override void Dispose(bool disposing) {
		if (disposing) {
			if (_resignActiveObserver != null)
				NSNotificationCenter.DefaultCenter.RemoveObserver(_resignActiveObserver);
			if (_becomeActiveObserver != null)
				NSNotificationCenter.DefaultCenter.RemoveObserver(_becomeActiveObserver);
			_displayLink?.Invalidate();
			_game?.ShutdownHosted();
			_game?.Dispose();
			_metalPresenter?.Dispose();
			_fdkContext?.Dispose();
			if (_glContext != null) {
				if (EAGLContext.CurrentContext == _glContext) {
					EAGLContext.SetCurrentContext(null);
				}
				_glContext.Dispose();
			}
		}
		base.Dispose(disposing);
	}
}
