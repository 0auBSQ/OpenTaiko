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
using System.Reflection;
using System.Runtime.InteropServices;
using FDK;
using ImGuiNET;
using Silk.NET.Core;
using Silk.NET.GLFW;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGLES;
using Silk.NET.OpenGLES.Extensions.ImGui;
using Silk.NET.Windowing;
using SkiaSharp;

namespace FDK;

/// <summary>
/// Presents an easy to use wrapper for making games and samples.
/// </summary>
public abstract class Game : IDisposable {
	public static GL Gl { get; private set; }
	public static Silk.NET.Core.Contexts.IGLContext Context { get; private set; }

	public static ImGuiController ImGuiController { get; private set; }
	public static ImGuiIOPtr ImGuiIO { get; private set; }
	private static CTexture ImGuiFontAtlas;

	static string _test = "";
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

	public static List<Action> AsyncActions { get; private set; } = new();

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

	private bool _FullScreen;
	public bool FullScreen {
		get {
			return _FullScreen;
		}
		set {
			_FullScreen = value;
			if (Window_ != null) {
				Window_.WindowState = value ? WindowState.Fullscreen : WindowState.Normal;
			}
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

	private Vector2D<int> ViewPortSize = new Vector2D<int>();
	private Vector2D<int> ViewPortOffset = new Vector2D<int>();

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

	public static long TimeMs;

	public static Matrix4X4<float> Camera;

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

	private RawImage GetIconData(string fileName) {
		SKCodec codec = SKCodec.Create(fileName);
		using SKBitmap bitmap = SKBitmap.Decode(codec, new SKImageInfo(codec.Info.Width, codec.Info.Height, SKColorType.Rgba8888));
		return new RawImage(bitmap.Width, bitmap.Height, bitmap.GetPixelSpan().ToArray());
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Game"/> class.
	/// </summary>
	protected Game(string iconFileName) {
		strIconFileName = iconFileName;

		MainThreadID = Thread.CurrentThread.ManagedThreadId;
		Configuration();

		//GlfwProvider.GLFW.Value.WindowHint(WindowHintContextApi.ContextCreationApi, ContextApi.EglContextApi);

		WindowOptions options = WindowOptions.Default;

		options.Size = WindowSize;
		options.Position = WindowPosition;
		options.UpdatesPerSecond = VSync ? 0 : Framerate;
		options.FramesPerSecond = VSync ? 0 : Framerate;
		options.WindowState = FullScreen ? WindowState.Fullscreen : WindowState.Normal;
		options.VSync = VSync;

		if (!OperatingSystem.IsMacOS()) options.API = GraphicsAPI.None;

		options.WindowBorder = WindowBorder.Resizable;
		options.Title = Text;


		// Use SDL on Linux with Wayland, otherwise use GLFW for everything else
		if (OperatingSystem.IsLinux() && Environment.GetEnvironmentVariable("XDG_SESSION_TYPE") == "wayland") {
		    Silk.NET.Windowing.Sdl.SdlWindowing.Use();
		} else {
		    Silk.NET.Windowing.Glfw.GlfwWindowing.Use();
		}

		Window_ = Window.Create(options);

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
		Window_.Dispose();
	}

	public void Exit() {
		Window_.Close();
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
		Window_.Run();
	}

	protected virtual void Configuration() {

	}

	protected virtual void Initialize() {

	}


	protected virtual void LoadContent() {

	}

	protected virtual void UnloadContent() {

	}

	protected virtual void OnExiting() {

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
		Window_.SetWindowIcon(new ReadOnlySpan<RawImage>(GetIconData(strIconFileName)));

		if (OperatingSystem.IsMacOS()) {
			if (Window_.GLContext == null) {
				throw new Exception("No native OpenGL context available");
			}

			Context = Window_.GLContext;
		} else {
			Context = new AngleContext(GraphicsDeviceType_, Window_);

			Context.MakeCurrent();
		}

		Gl = GL.GetApi(Context);

		Gl.Enable(GLEnum.Blend);
		BlendHelper.SetBlend(BlendType.Normal);
		CTexture.Init();

		Gl.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);

		if (!OperatingSystem.IsMacOS())
			Gl.Viewport(0, 0, (uint)Window_.Size.X, (uint)Window_.Size.Y);
		
		Context.SwapInterval(VSync ? 1 : 0);

		Initialize();
		LoadContent();
	}

	public void Window_Closing() {
		CTexture.Terminate();

		UnloadContent();
		OnExiting();

		Context.Dispose();
	}

	public void Window_Update(double deltaTime) {
		double fps = 1.0f / deltaTime;
		TimeMs = (long)(Window_.Time * 1000);

		Update();

		ImGuiController?.Update((float)deltaTime);
	}

	public void Window_Render(double deltaTime) {
		Camera = Matrix4X4<float>.Identity;

		if (AsyncActions.Count > 0) {
			AsyncActions[0]?.Invoke();
			AsyncActions.Remove(AsyncActions[0]);
		}
		Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

		Draw();

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
