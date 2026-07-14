/*
* Copyright (c) 2007-2009 SlimDX Group
*
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
*
* The above copyright notice and this permission notice shall be included in
* all copies or substantial portions of the Software.
*
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
* THE SOFTWARE.
*/
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using ImGuiNET;
using Silk.NET.Core;
using Silk.NET.GLFW;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGLES;
using Silk.NET.OpenGLES.Extensions.ImGui;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Sdl;
using SkiaSharp;

namespace FDK;

/// <summary>
/// Presents an easy to use wrapper for making games and samples.
/// </summary>
public abstract partial class Game : IDisposable {
	public static GL Gl { get; private set; }
	public static Silk.NET.Core.Contexts.IGLContext Context { get; private set; }

	/// <summary>True when the GL context is GLES 3.1+, i.e. compute shaders (the GPU renderer path)
	/// are usable. False on the GLES 2.0 fallback, where the CPU renderers are used instead.</summary>
	public static bool ComputeShadersAvailable { get; private set; }
	
	private static string[] parameters;
	private static string GetParameterValue(string parameter = "", string parameter_full = "") {
		if (parameters.Length == 0) return "";
		int index = parameters.Contains(parameter) || parameters.Contains(parameter_full)
		? Array.FindIndex(parameters, x => x.Equals(parameter) || x.Equals(parameter_full))
		: -1;
		return index > -1 && parameters.Length > index ? parameters[index + 1] : "";
	}

	public static ImGuiController ImGuiController { get; private set; }
	public static ImGuiIOPtr ImGuiIO { get; private set; }
	private static CTexture ImGuiFontAtlas;

	public static void InitImGuiController(IView window, IInputContext context) {
		if (ImGuiController != null) return;

		ImGuiController = new ImGuiController(Gl, window, context);
		ImGuiIO = ImGui.GetIO();
		ImGui.StyleColorsDark();
#if DEBUG
		try {
			ImGuiIO.Fonts.Clear();
			unsafe {
				Stream data = Assembly.GetExecutingAssembly().GetManifestResourceStream(@"FDK.mplus-1p-medium.ttf");
				byte[] stream_data = new byte[data.Length];
				data.Read(stream_data);
				fixed (byte* stream = stream_data) {
					ImFontConfigPtr config = new ImFontConfigPtr(ImGuiNative.ImFontConfig_ImFontConfig());
					ImGuiIO.Fonts.AddFontFromMemoryTTF((IntPtr)stream, 64, 16.0f, config, ImGuiIO.Fonts.GetGlyphRangesDefault());
					config.MergeMode = true;
					ImGuiIO.Fonts.AddFontFromMemoryTTF((IntPtr)stream, 64, 16.0f, config, ImGuiIO.Fonts.GetGlyphRangesJapanese());

					ImGuiIO.Fonts.GetTexDataAsRGBA32(out byte* out_pixels, out int width, out int height);

					using (SKImage image = SKImage.FromPixels(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Opaque), (IntPtr)out_pixels)) {
						using (SKBitmap bitmap = SKBitmap.FromImage(image)) {
							ImGuiFontAtlas?.Dispose();
							ImGuiFontAtlas = new CTexture(bitmap);
						}
					}
					Marshal.FreeHGlobal((IntPtr)out_pixels);

					ImGuiIO.Fonts.SetTexID((nint)ImGuiFontAtlas.Pointer);
				}
			}
		} catch (Exception ex) {
			ImGuiIO.Fonts.Clear();
			ImGuiIO.Fonts.AddFontDefault();
		}
#endif
	}

	public static ConcurrentQueue<Action> AsyncActions { get; private set; } = new ConcurrentQueue<Action>();

	// Per-frame time budget for draining AsyncActions (the single render-thread "finalize" queue: deferred GL
	// texture uploads + sound/shared creation). Small in normal gameplay so runtime async loads trickle in
	// without hurting fps; CLoadSession raises it behind a loading screen, where nothing else needs the frame.
	public const double DefaultAsyncBudgetMs = 3.0;
	public static double AsyncBudgetMs = DefaultAsyncBudgetMs;

	public Thread thInput { get; private set; }
	private CancellationTokenSource thInputCancel;

	private string strIconFileName;

	protected string _Text = "";
	protected string Text {
		get {
			return _Text;
		}
		set {
			_Text = value;
			if (Window_ != null) {
				Window_.Title = value;
			}
		}
	}

	public static AnglePlatformType GraphicsDeviceType_ = AnglePlatformType.OpenGL;

	public IWindow Window_;

	private Vector2D<int> _WindowSize;
	public Vector2D<int> WindowSize {
		get {
			return _WindowSize;
		}
		set {
			_WindowSize = value;
			if (Window_ != null) {
				Window_.Size = value;
			}
		}
	}

	private Vector2D<int> _WindowPosition;
	public Vector2D<int> WindowPosition {
		get {
			return _WindowPosition;
		}
		set {
			_WindowPosition = value;
			if (Window_ != null) {
				Window_.Position = value;
			}
		}
	}

	private int _Framerate;

	public int Framerate {
		get {
			return _Framerate;
		}
		set {
			_Framerate = value;
			if (Window_ != null) {
				UpdateWindowFramerate(VSync, value);
			}
		}
	}

	private int _windowMode;   // 0 = Windowed, 1 = Fullscreen (exclusive), 2 = Borderless Fullscreen
	public int WindowMode {
		get { return _windowMode; }
		set {
			_windowMode = value;
			ApplyWindowMode();
		}
	}
	// Compat for callers that only distinguish fullscreen vs windowed: both Fullscreen (1) and Borderless (2)
	// count as "fullscreen".
	public bool FullScreen {
		get { return _windowMode != 0; }
		set { WindowMode = value ? 1 : 0; }
	}

	/// <summary>Apply the current <see cref="WindowMode"/> to the live window (no-op before the window exists; the
	/// initial WindowOptions also seed it at creation). Borderless = an undecorated window filling the monitor.</summary>
	private void ApplyWindowMode() {
		// Skip entirely for a deliberately-hidden window (offline video export, --mode=record): the
		// WindowState/WindowBorder/Size/Position setters below force a hidden GLFW window to show itself,
		// which would pop the export window open. Export only reads the backbuffer, so it needs no mode.
		if (Window_ == null || StartHidden) return;
		switch (_windowMode) {
			case 1:   // exclusive fullscreen
				Window_.WindowBorder = WindowBorder.Resizable;
				Window_.WindowState = WindowState.Fullscreen;
				break;
			case 2:   // borderless fullscreen
				Window_.WindowState = WindowState.Normal;
				Window_.WindowBorder = WindowBorder.Hidden;
				var mon = Window_.Monitor;
				if (mon != null) {
					Window_.Position = mon.Bounds.Origin;
					Window_.Size = mon.Bounds.Size;
				}
				break;
			default:  // windowed
				Window_.WindowState = WindowState.Normal;
				Window_.WindowBorder = WindowBorder.Resizable;
				// Restore the configured windowed size/position — borderless (and exclusive fullscreen) resized the
				// window to the monitor, so without this the window stays monitor-sized and looks still-borderless.
				Window_.Size = WindowSize;
				Window_.Position = WindowPosition;
				break;
		}
	}

	private bool _VSync;
	public bool VSync {
		get {
			return _VSync;
		}
		set {
			_VSync = value;
			if (Window_ != null) {
				UpdateWindowFramerate(value, Framerate);
				Window_.VSync = value;
			}
		}
	}

	public static int MainThreadID { get; private set; }

	// ── virtual clock (offline video export) ────────────────────────────────────────────────────
	// When enabled, TimeMs/dbTimeMs come from VirtualClockMs instead of the window's wall clock.
	// Everything timed off Game.TimeMs (CTimer.MultiMedia, CSoundTimer in OS-timer mode, video
	// decoders) then advances exactly as fast as the exporter steps this value — one fixed step
	// per rendered frame — so render duration no longer affects game time at all.
	public static bool VirtualClockEnabled = false;
	public static double VirtualClockMs = 0;
	/// <summary>Create the window invisible (offline export); set before Run().</summary>
	public bool StartHidden = false;

	// Public so input mapping can convert window-pixel mouse coords into game-surface
	// coords (the rendered surface is aspect-fit inside the window, leaving letterbox
	// borders described by these two values).
	public static Vector2D<int> ViewPortSize = new Vector2D<int>();
	public static Vector2D<int> ViewPortOffset = new Vector2D<int>();

	public unsafe SKBitmap GetScreenShot() {
		int ViewportWidth = ViewPortSize.X;
		int ViewportHeight = ViewPortSize.Y;
		fixed (uint* pixels = new uint[(uint)ViewportWidth * (uint)ViewportHeight]) {
			Gl.ReadBuffer(GLEnum.Front);
			Gl.ReadPixels(ViewPortOffset.X, ViewPortOffset.Y, (uint)ViewportWidth, (uint)ViewportHeight, PixelFormat.Bgra, GLEnum.UnsignedByte, pixels);

			fixed (uint* pixels2 = new uint[(uint)ViewportWidth * (uint)ViewportHeight]) {
				for (int x = 0; x < ViewportWidth; x++) {
					for (int y = 1; y < ViewportHeight; y++) {
						int pos = x + ((y - 1) * ViewportWidth);
						int pos2 = x + ((ViewportHeight - y) * ViewportWidth);
						var p = pixels[pos2];
						pixels2[pos] = p;
					}
				}

				using SKBitmap sKBitmap = new(ViewportWidth, ViewportHeight - 1);
				sKBitmap.SetPixels((IntPtr)pixels2);
				return sKBitmap.Copy();
			}
		}
	}

	public unsafe void GetScreenShotAsync(Action<SKBitmap> action) {
		int ViewportWidth = ViewPortSize.X;
		int ViewportHeight = ViewPortSize.Y;
		byte[] pixels = new byte[(uint)ViewportWidth * (uint)ViewportHeight * 4];
		Gl.ReadBuffer(GLEnum.Front);
		fixed (byte* pix = pixels) {
			Gl.ReadPixels(ViewPortOffset.X, ViewPortOffset.Y, (uint)ViewportWidth, (uint)ViewportHeight, PixelFormat.Bgra, GLEnum.UnsignedByte, pix);
		}

		Task.Run(() => {
			fixed (byte* pixels2 = new byte[(uint)ViewportWidth * (uint)ViewportHeight * 4]) {
				for (int x = 0; x < ViewportWidth; x++) {
					for (int y = 1; y < ViewportHeight; y++) {
						int pos = x + ((y - 1) * ViewportWidth);
						int pos2 = x + ((ViewportHeight - y) * ViewportWidth);
						pixels2[(pos * 4) + 0] = pixels[(pos2 * 4) + 0];
						pixels2[(pos * 4) + 1] = pixels[(pos2 * 4) + 1];
						pixels2[(pos * 4) + 2] = pixels[(pos2 * 4) + 2];
						pixels2[(pos * 4) + 3] = 255;
					}
				}

				using SKBitmap sKBitmap = new(ViewportWidth, ViewportHeight - 1);
				sKBitmap.SetPixels((IntPtr)pixels2);

				using SKBitmap scaledBitmap = new(GameWindowSize.Width, GameWindowSize.Height);
				if (sKBitmap.ScalePixels(scaledBitmap, SKFilterQuality.High)) action(scaledBitmap);
				else action(sKBitmap);
			}
		});
	}

	public static double dbTimeMs;
	public static long TimeMs;

	public static Matrix4X4<float> Camera;

	private static Color4 _borderColor = new Color4(0f, 0f, 0f, 1f);
	public static Color4 BorderColor {
		get => _borderColor;
		set {
			if (value != _borderColor)
				Gl.ClearColor(BorderColor.Red, BorderColor.Green, BorderColor.Blue, BorderColor.Alpha);
			_borderColor = value;
		}
	}

	public static float ScreenAspect {
		get {
			return (float)GameWindowSize.Width / GameWindowSize.Height;
		}
	}

	//[DllImportAttribute("libEGL", EntryPoint = "eglGetError")]
	//public static extern Silk.NET.OpenGLES.ErrorCode GetError();

	/// <summary>
	/// Initializes the <see cref="Game"/> class.
	/// </summary>
	static Game() {
		//GlfwProvider.UninitializedGLFW.Value.InitHint(InitHint.AnglePlatformType, (int)AnglePlatformType.OpenGL);
		//GlfwProvider.UninitializedGLFW.Value.Init();
		//GetError();
	}

	[DllImport("user32.dll")] private static extern nint SendMessage(nint h, uint msg, nuint w, nint l);
	[DllImport("user32.dll")] private static extern nint SetClassLongPtr(nint h, int idx, nint val);

	// glfwSetWindowIcon sets ICON_SMALL (title bar) and ICON_BIG, but never ICON_SMALL2.
	// Windows 10/11 taskbar reads ICON_SMALL2 for the button icon — sync it here.
	private static void SyncTaskbarIcon(nint hwnd) {
		nint hIcon = SendMessage(hwnd, 0x007F /* WM_GETICON */, 1 /* ICON_BIG */, 0);
		if (hIcon == 0) return;
		SendMessage(hwnd, 0x0080 /* WM_SETICON */, 2 /* ICON_SMALL2 */, hIcon);
		SetClassLongPtr(hwnd, -14 /* GCLP_HICON */, hIcon);
	}

	private RawImage? GetIconData(string fileName) {
		try {
			string path = Path.IsPathRooted(fileName) ? fileName : Path.Combine(AppContext.BaseDirectory, fileName);
			using var src = SKBitmap.Decode(path);
			if (src == null) return null;
			using var rgba = src.Copy(SKColorType.Rgba8888);
			if (rgba == null) return null;
			return new RawImage(rgba.Width, rgba.Height, rgba.Bytes);
		} catch (Exception ex) {
			Trace.TraceWarning($"[GetIconData] {ex.Message}");
			return null;
		}
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Game"/> class.
	/// </summary>
	protected Game(string iconFileName, params string[] args) {
		try {
			this.InitializeLog();
		} catch (Exception ex) {
			Console.WriteLine(ex.ToString());
			Console.WriteLine("Error initializing log.");
		}

		strIconFileName = iconFileName;
		parameters = args;

		MainThreadID = Thread.CurrentThread.ManagedThreadId;
		Configuration();

		if (OperatingSystem.IsIOS() || OperatingSystem.IsAndroid()) {
			// Window and GL context are managed externally by the host (iOS GameViewController /
			// Android MainActivity).
			// The host calls InitWithExternalContext() and drives the game loop directly.
			return;
		}

		//GlfwProvider.GLFW.Value.WindowHint(WindowHintContextApi.ContextCreationApi, ContextApi.EglContextApi);

		WindowOptions options = WindowOptions.Default;

		options.Size = WindowSize;
		options.Position = WindowPosition;
		options.UpdatesPerSecond = VSync ? 0 : Framerate;
		options.FramesPerSecond = VSync ? 0 : Framerate;
		options.WindowState = (WindowMode == 1) ? WindowState.Fullscreen : WindowState.Normal;
		options.VSync = VSync;

		if (!OperatingSystem.IsMacOS()) options.API = GraphicsAPI.None;

		options.WindowBorder = (WindowMode == 2) ? WindowBorder.Hidden : WindowBorder.Resizable;
		options.Title = Text;
		options.IsVisible = !StartHidden;

		#region Override Windowing
		string windowing_override = GetParameterValue("-w", "--windowing");
		#endregion

		// Use SDL on Linux with Wayland, otherwise use GLFW for everything else
		if ((OperatingSystem.IsLinux() && Environment.GetEnvironmentVariable("XDG_SESSION_TYPE") == "wayland" && windowing_override != "glfw")
		|| windowing_override == "sdl") {
			Silk.NET.Windowing.Sdl.SdlWindowing.Use();
			Trace.TraceInformation("SDL selected for Windowing");
		} else {
			Silk.NET.Windowing.Glfw.GlfwWindowing.Use();
			Trace.TraceInformation("GLFW selected for Windowing");
		}

		try {
			Window_ = Window.Create(options);
		}
		catch (Exception ex) {
			Trace.TraceError(ex.ToString());
			Trace.TraceError("The window failed to be created.\nYou can attempt to fix this by overriding the default windowing.\nTry launching OpenTaiko with args, using '-w glfw' to force GLFW or '-w sdl' to force SDL.");
			throw;
        }

		ViewPortSize.X = Window_.Size.X;
		ViewPortSize.Y = Window_.Size.Y;
		ViewPortOffset.X = 0;
		ViewPortOffset.Y = 0;

		Window_.Load += Window_Load;
		Window_.Closing += Window_Closing;
		Window_.Update += Window_Update;
		Window_.Render += Window_Render;
		Window_.Resize += Window_Resize;
		Window_.Move += Window_Move;
		Window_.FramebufferResize += Window_FramebufferResize;
	}

	private void UpdateWindowFramerate(bool vsync, int value) {
		if (vsync) {
			Window_.UpdatesPerSecond = 0;
			Window_.FramesPerSecond = 0;
			Context.SwapInterval(1);
		} else {
			Window_.UpdatesPerSecond = value;
			Window_.FramesPerSecond = value;
			Context.SwapInterval(0);
		}
	}

	/// <summary>
	/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
	/// </summary>
	public void Dispose() {
		if (OperatingSystem.IsIOS() || OperatingSystem.IsAndroid())
			return;
		Window_.Dispose();
	}

	// Hosted (mobile) builds have no window to close: the host installs a callback here and
	// tears its render loop/activity down when the game asks to exit (e.g. the exit stage).
	public static Action? HostExitRequested;

	public void Exit() {
		if (OperatingSystem.IsIOS() || OperatingSystem.IsAndroid()) {
			HostExitRequested?.Invoke();
			return;
		}
		Window_.Close();
	}

	/// <summary>
	/// Initializes the GL context and game systems using an externally-provided GL context.
	/// Used when the window and GL surface are managed by an external host.
	/// </summary>
	public void InitWithExternalContext(Silk.NET.Core.Contexts.IGLContext externalContext, int viewportWidth, int viewportHeight) {
		Context = externalContext;
		Context.MakeCurrent();

		Gl = GL.GetApi(Context);

		Gl.Enable(GLEnum.Blend);
		BlendHelper.SetBlend(BlendType.Normal);
		CTexture.Init();

		// Detect compute-shader capability (GLES 3.1+) so renderers can pick the GPU or CPU path;
		// mirrors the desktop probe in Window_Load, which hosted contexts never run.
		ComputeShadersAvailable = false;
		try {
			int glMaj = Gl.GetInteger(GLEnum.MajorVersion);
			int glMin = Gl.GetInteger(GLEnum.MinorVersion);
			ComputeShadersAvailable = glMaj > 3 || (glMaj == 3 && glMin >= 1);
			if (ComputeShadersAvailable && !Context.TryGetProcAddress("glDispatchCompute", out _))
				ComputeShadersAvailable = false;
		} catch { ComputeShadersAvailable = false; }
		Trace.TraceInformation($"Compute shaders available (hosted): {ComputeShadersAvailable}");

		Gl.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
		Window_Resize(new Vector2D<int>(viewportWidth, viewportHeight));

		Context.SwapInterval(VSync ? 1 : 0);

		Initialize();
		LoadContent();
	}

	private long _hostStartTimeMs;
	private bool _hostTimeInitialized;

	// Render target supplied by the host; the hosted frame draws the scene into it at game resolution, and the host presents it.
	public uint HostRenderTargetFbo;

	/// <summary>
	/// Called by the host once per frame; drives Update() and the render.
	/// </summary>
	public void RenderHostedFrame(double deltaTime) {
		if (!_hostTimeInitialized) {
			_hostStartTimeMs = Stopwatch.GetTimestamp();
			_hostTimeInitialized = true;
		}
		// Set BOTH clocks: dbTimeMs feeds CTimer.NowTimeMs_Double -> CFPS.DeltaTime, which drives the Lua
		// counters that advance screen transitions.
		double hostElapsedMs = (Stopwatch.GetTimestamp() - _hostStartTimeMs) * 1000.0 / Stopwatch.Frequency;
		dbTimeMs = hostElapsedMs;
		TimeMs = (long)hostElapsedMs;

		Update();

		Camera = Matrix4X4<float>.Identity;

		// Drain queued main-thread actions (deferred GL uploads from background loaders and texture
		// streaming) within a time budget. Same idiom as Window_Render, see the note there.
		if (!AsyncActions.IsEmpty) {
			long asyncStart = System.Diagnostics.Stopwatch.GetTimestamp();
			while (AsyncActions.TryDequeue(out var action)) {
				try {
					action();
				} catch (Exception ex) {
					Console.Error.WriteLine($"Error in async action: {ex}");
				}
				if (System.Diagnostics.Stopwatch.GetElapsedTime(asyncStart).TotalMilliseconds >= AsyncBudgetMs)
					break;
			}
		}

		// Draw the scene into the host's render target; the host presents it (no GL swap).
		Gl.BindFramebuffer(FramebufferTarget.Framebuffer, HostRenderTargetFbo);
		Gl.Viewport(0, 0, (uint)GameWindowSize.Width, (uint)GameWindowSize.Height);
		// Border color fills any margin the camera transform leaves around the scene.
		Gl.ClearColor(BorderColor.Red, BorderColor.Green, BorderColor.Blue, BorderColor.Alpha);
		Gl.Clear(ClearBufferMask.ColorBufferBit);
		Draw();
		Gl.Flush(); // submit GL commands so the host can sample the rendered surface
	}

	/// <summary>
	/// Called by the host when shutting down.
	/// </summary>
	public void ShutdownHosted() {
		CTexture.Terminate();
		UnloadContent();
		OnExiting();
		Context.Dispose();
	}

	/// <summary>
	/// Called by the host when the viewport size changes.
	/// </summary>
	public void ResizeViewport(int width, int height) {
		Window_Resize(new Vector2D<int>(width, height));
	}

	protected void ToggleWindowMode() {
		/*
		DeviceSettings settings = base.GraphicsDeviceManager.CurrentSettings.Clone();
		if ( ( ConfigIni != null ) && ( ConfigIni.bウィンドウモード != settings.Windowed ) )
		{
			settings.Windowed = ConfigIni.bウィンドウモード;
			if ( ConfigIni.bウィンドウモード == false )	// #23510 2010.10.27 yyagi: backup current window size before going fullscreen mode
			{
				currentClientSize = this.Window.ClientSize;
				ConfigIni.nウインドウwidth = this.Window.ClientSize.Width;
				ConfigIni.nウインドウheight = this.Window.ClientSize.Height;
//					FDK.CTaskBar.ShowTaskBar( false );
			}
			base.GraphicsDeviceManager.ChangeDevice( settings );
			if ( ConfigIni.bウィンドウモード == true )	// #23510 2010.10.27 yyagi: to resume window size from backuped value
			{
				base.Window.ClientSize =
					new Size( currentClientSize.Width, currentClientSize.Height );
                base.Window.Icon = Properties.Resources.tjap3;
//					FDK.CTaskBar.ShowTaskBar( true );
			}
		}
		*/

		FullScreen = !FullScreen;
	}

	/// <summary>
	/// Runs the game.
	/// </summary>
	public void Run() {
		if (OperatingSystem.IsIOS() || OperatingSystem.IsAndroid()) {
			// The game loop is driven externally by the host.
			// Run() is a no-op; the host calls InitWithExternalContext() then RenderHostedFrame() each frame.
			return;
		}
		Window_.Run();
	}

	protected virtual void Configuration() {

	}

	protected virtual void InitializeLog() {

	}

	protected virtual void Initialize() {

	}


	protected virtual void LoadContent() {

	}

	protected virtual void UnloadContent() {

	}

	protected virtual void OnExiting() {

	}

	protected virtual void Events() {
		// Silk's GLFW event pump can intermittently throw (seen as "Nullable object must have a
		// value") under very rapid input, especially with the cursor locked. It's a transient in
		// the windowing backend, not our state — swallow it and re-pump next frame instead of
		// letting it crash the whole game.
		try {
			Window_.DoEvents();
		} catch (Exception e) {
			System.Diagnostics.Trace.TraceWarning($"Window event pump threw (ignored): {e}");
		}
	}

	protected virtual void Update() {

	}

	protected virtual void Draw() {

	}

	/// <summary>
	/// Releases unmanaged and - optionally - managed resources
	/// </summary>
	/// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
	protected internal virtual void Dispose(bool disposing) {

	}


	public void Window_Load() {
		var icon = GetIconData(strIconFileName);
		if (icon.HasValue) {
			Window_.SetWindowIcon(new ReadOnlySpan<RawImage>(icon.Value));
			if (OperatingSystem.IsWindows() && Window_.Native?.Win32 is { } w && w.Hwnd != 0)
				SyncTaskbarIcon(w.Hwnd);
		}

		if (OperatingSystem.IsMacOS()) {
			if (Window_.GLContext == null) {
				throw new Exception("No native OpenGL context available");
			}

			Context = Window_.GLContext;
		} else {
			#region Override Platform
			GraphicsDeviceType_ = GetParameterValue("-p", "--platform") switch {
				"opengl" => AnglePlatformType.OpenGL,
				"opengles" => AnglePlatformType.OpenGLES,
				"d3d9" => AnglePlatformType.D3D9,
				"d3d11" => AnglePlatformType.D3D11,
				"vulkan" => AnglePlatformType.Vulkan,
				"metal" => AnglePlatformType.Metal,
				_ => GraphicsDeviceType_
			};
			Console.WriteLine("Platform set to " + GraphicsDeviceType_);
			#endregion

			Context = new AngleContext(GraphicsDeviceType_, Window_, GetParameterValue("-f", "--flag"));

			Context.MakeCurrent();
		}

		Gl = GL.GetApi(Context);

		// Detect compute-shader capability (GLES 3.1+) so renderers can pick the GPU or CPU path.
		ComputeShadersAvailable = false;
		try {
			if (Context is AngleContext ac) {
				ComputeShadersAvailable = ac.ContextMajor > 3 || (ac.ContextMajor == 3 && ac.ContextMinor >= 1);
			} else {
				int glMaj = Gl.GetInteger(GLEnum.MajorVersion);
				int glMin = Gl.GetInteger(GLEnum.MinorVersion);
				ComputeShadersAvailable = glMaj > 3 || (glMaj == 3 && glMin >= 1);
			}
			if (ComputeShadersAvailable && !Context.TryGetProcAddress("glDispatchCompute", out _))
				ComputeShadersAvailable = false;
		} catch { ComputeShadersAvailable = false; }
		Trace.TraceInformation($"Compute shaders available: {ComputeShadersAvailable}");

		Gl.Enable(GLEnum.Blend);
		BlendHelper.SetBlend(BlendType.Normal);
		CTexture.Init();

		Gl.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);

		if (!OperatingSystem.IsMacOS())
			Gl.Viewport(0, 0, (uint)Window_.Size.X, (uint)Window_.Size.Y);

		Context.SwapInterval(VSync ? 1 : 0);

		// Finalise the window mode now that the window + monitor exist (esp. Borderless, which needs the monitor
		// bounds to size the undecorated window — WindowOptions can't query the monitor before creation).
		ApplyWindowMode();

		Initialize();
		LoadContent();

		try {
			this.thInputCancel = new CancellationTokenSource();
			ThreadStart thInputFunc = new (async () => {
				using PeriodicTimer timer = new(TimeSpan.FromMilliseconds(1));
				while (!this.thInputCancel.IsCancellationRequested) { // ensure alive
					try {
						while (await timer.WaitForNextTickAsync(this.thInputCancel.Token))
							this.Events();
					} catch (OperationCanceledException) {
						// ignore
					} finally {
						if (this.thInputCancel.IsCancellationRequested)
							Trace.TraceInformation("Input thread terminated.");
					}
				}
			});
			this.thInput = new Thread(thInputFunc) {
				Name = "InputThread",
				IsBackground = true,
			};
			this.thInput.Start();
			Trace.TraceInformation("Input thread started.");
		} catch (Exception ex) {
			Trace.TraceWarning(ex.ToString());
			Trace.TraceWarning("Input thread failed to start. Fell back to poll input per draw frame.");
		}
	}

	public void Window_Closing() {
		CTexture.Terminate();
		DeleteRenderScaleFbo();

		UnloadContent();
		OnExiting();

		this.thInputCancel.Cancel();
		this.thInputCancel.Dispose();
		Context.Dispose();
	}

	public void Window_Update(double deltaTime) {
		double fps = 1.0f / deltaTime;
		if (VirtualClockEnabled) {
			dbTimeMs = VirtualClockMs;
			TimeMs = (long)VirtualClockMs;
		} else {
			dbTimeMs = Window_.Time * 1000;
			TimeMs = (long)(Window_.Time * 1000);
		}
		unsafe {
			if (SdlWindowing.IsViewSdl(Window_)) {
				Silk.NET.SDL.Event sdlEvent;
				while (Silk.NET.SDL.SdlProvider.SDL.Value.PollEvent(&sdlEvent) != 0) {

				}
			}
		}
		Update();

		ImGuiController?.Update((float)deltaTime);
	}

	// ── Render-scale (global resolution downscale) ────────────────────────────────────────────────
	// When RenderScale < 1, the whole frame (all 2D + composited 3D) is rendered into an offscreen FBO sized
	// (GameWindowSize × RenderScale) and then upscale-blitted to the window's letterboxed region on present. Content
	// keeps drawing in the unchanged GameWindowSize logical coordinates, so nothing else needs to know about the scale —
	// it just renders into fewer physical pixels. Set by the skin's chosen resolution multiplier.
	public static float RenderScale = 1.0f;
	private uint _rsFbo, _rsColorTex, _rsDepthRb;
	private int _rsW, _rsH;

	private unsafe bool EnsureRenderScaleFbo() {
		int tw = Math.Max(1, (int)Math.Round(GameWindowSize.Width * RenderScale));
		int th = Math.Max(1, (int)Math.Round(GameWindowSize.Height * RenderScale));
		if (_rsFbo != 0 && _rsW == tw && _rsH == th) return true;
		DeleteRenderScaleFbo();
		_rsW = tw; _rsH = th;
		_rsFbo = Gl.GenFramebuffer();
		Gl.BindFramebuffer(FramebufferTarget.Framebuffer, _rsFbo);
		_rsColorTex = Gl.GenTexture();
		Gl.BindTexture(TextureTarget.Texture2D, _rsColorTex);
		Gl.TexImage2D(TextureTarget.Texture2D, 0, (int)InternalFormat.Rgba8, (uint)tw, (uint)th, 0, PixelFormat.Rgba, PixelType.UnsignedByte, (void*)0);
		Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
		Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
		Gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, _rsColorTex, 0);
		_rsDepthRb = Gl.GenRenderbuffer();
		Gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _rsDepthRb);
		Gl.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.DepthComponent24, (uint)tw, (uint)th);
		Gl.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, _rsDepthRb);
		var status = Gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
		Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
		Gl.BindTexture(TextureTarget.Texture2D, 0);
		if (status != GLEnum.FramebufferComplete) {
			Trace.TraceWarning($"Render-scale FBO incomplete ({status}); rendering at full resolution.");
			DeleteRenderScaleFbo();
			return false;
		}
		return true;
	}

	private void DeleteRenderScaleFbo() {
		if (_rsColorTex != 0) { Gl.DeleteTexture(_rsColorTex); _rsColorTex = 0; }
		if (_rsDepthRb != 0) { Gl.DeleteRenderbuffer(_rsDepthRb); _rsDepthRb = 0; }
		if (_rsFbo != 0) { Gl.DeleteFramebuffer(_rsFbo); _rsFbo = 0; }
		_rsW = _rsH = 0;
	}

	public void Window_Render(double deltaTime) {
		if (GraphicsSelfTest) { RunGraphicsSelfTest(); return; }   // --mode=checkgl (see Game.GraphicsSelfTest.cs)
		Gl.Disable(EnableCap.ScissorTest);   // a leaked UI clip (GRAPHICS:SetClip) must never survive the frame
		Camera = Matrix4X4<float>.Identity;
		Gl.ClearColor(BorderColor.Red, BorderColor.Green, BorderColor.Blue, BorderColor.Alpha);

		/*
		if (AsyncActions.Count > 0) {
			AsyncActions[0]?.Invoke();
			AsyncActions.Remove(AsyncActions[0]);
		}
		*/

		// Drain queued main-thread actions (mostly deferred GL texture uploads from background loaders —
		// boot atlas, song-load game-screen activation). Process a batch within a small time budget so a
		// large burst uploads quickly instead of trickling out one-per-frame, while still capping the time
		// spent so the frame rate stays smooth. When the queue is empty (the normal in-game case) the very
		// first TryDequeue fails and the loop exits immediately — no per-frame cost.
		if (!AsyncActions.IsEmpty) {
			long asyncStart = System.Diagnostics.Stopwatch.GetTimestamp();
			while (AsyncActions.TryDequeue(out var action)) {
				try {
					action();
				} catch (Exception ex) {
					Console.Error.WriteLine($"Error in async action: {ex}");
				}
				if (System.Diagnostics.Stopwatch.GetElapsedTime(asyncStart).TotalMilliseconds >= AsyncBudgetMs)
					break;
			}
		}

		bool useScale = RenderScale < 0.999f && EnsureRenderScaleFbo();
		if (useScale) {
			// clear the window first (the letterbox bars stay the border colour), render the whole frame into the
			// scaled offscreen FBO, then upscale-blit it into the window's letterboxed region.
			Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
			Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
			Gl.BindFramebuffer(FramebufferTarget.Framebuffer, _rsFbo);
			Gl.Viewport(0, 0, (uint)_rsW, (uint)_rsH);
			Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
			Draw();
			Gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, _rsFbo);
			Gl.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
			Gl.BlitFramebuffer(0, 0, _rsW, _rsH,
				ViewPortOffset.X, ViewPortOffset.Y, ViewPortOffset.X + ViewPortSize.X, ViewPortOffset.Y + ViewPortSize.Y,
				ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Linear);
			Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
			Gl.Viewport(ViewPortOffset.X, ViewPortOffset.Y, (uint)ViewPortSize.X, (uint)ViewPortSize.Y);   // restore for ImGui/post
		} else {
			Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
			Draw();
		}

		double fps = 1.0f / deltaTime;

#if DEBUG
		ImGuiController?.Render();
#endif

		if (!OperatingSystem.IsMacOS()) Context.SwapBuffers();
	}

	public void Window_Resize(Vector2D<int> size) {
		if (size.X > 0 && size.Y > 0) {
			float resolutionAspect = (float)GameWindowSize.Width / GameWindowSize.Height;
			float windowAspect = (float)size.X / size.Y;
			if (windowAspect > resolutionAspect) {
				ViewPortSize.X = (int)(size.Y * resolutionAspect);
				ViewPortSize.Y = size.Y;
			} else {
				ViewPortSize.X = size.X;
				ViewPortSize.Y = (int)(size.X / resolutionAspect);
			}
		}

		ViewPortOffset.X = (size.X - ViewPortSize.X) / 2;
		ViewPortOffset.Y = (size.Y - ViewPortSize.Y) / 2;


		Gl.Viewport(ViewPortOffset.X, ViewPortOffset.Y, (uint)ViewPortSize.X, (uint)ViewPortSize.Y);
	}

	public void Window_Move(Vector2D<int> size) { }

	public void Window_FramebufferResize(Vector2D<int> size) { }
}
