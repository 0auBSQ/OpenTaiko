using System.Diagnostics;
using OpenTK.Graphics.Egl; //OpenTKさん ありがとう!
using Silk.NET.Core.Contexts;
using Silk.NET.GLFW;
using Silk.NET.Windowing;

namespace FDK;

public class AngleContext : IGLContext {
	private nint Display;
	private nint Context;
	private nint Surface;

	public AngleContext(AnglePlatformType anglePlatformType, IWindow window, string flag_override = "") {
		nint windowHandle;
		nint display;
		NativeWindowFlags selectedflag; // For logging

		bool flag_has_override = !string.IsNullOrWhiteSpace(flag_override);
		if ((window.Native.Kind.HasFlag(NativeWindowFlags.Win32) && !flag_has_override) || flag_override == "win32") {
			selectedflag = NativeWindowFlags.Win32;
			windowHandle = window.Native.Win32.Value.Hwnd;
			display = window.Native.Win32.Value.HDC;
			Console.WriteLine("Handle set to Win32");
		} else if ((window.Native.Kind.HasFlag(NativeWindowFlags.X11) && !flag_has_override) || flag_override == "x11") {
			selectedflag = NativeWindowFlags.X11;
			windowHandle = (nint)window.Native.X11.Value.Window;
			// Temporary fix for the segfaults
			// Note than X11 Display number is NOT always 0, it can be 1, 2 and so on for example in cases of user switching
			display = 0;// Egl.GetDisplay(window.Native.X11.Value.Display);
			Console.WriteLine("Handle set to X11");
		} else if ((window.Native.Kind.HasFlag(NativeWindowFlags.Cocoa) && !flag_has_override) || flag_override == "cocoa") {
			selectedflag = NativeWindowFlags.Cocoa;
			windowHandle = window.Native.Cocoa.Value;
			display = 0;
			Console.WriteLine("Handle set to Cocoa");
		} else if ((window.Native.Kind.HasFlag(NativeWindowFlags.Wayland) && !flag_has_override) || flag_override == "wayland") {
			selectedflag = NativeWindowFlags.Wayland;
			windowHandle = window.Native.Wayland.Value.Surface;
			display = window.Native.Wayland.Value.Display;
			Console.WriteLine("Handle set to Wayland");
		} else {
			if (flag_has_override) throw new Exception("Override flag provided is invalid, please check for spelling errors or remove your argument.");
			throw new Exception("Window not found");
		}

		Source = window;
		
		int platform = 0;
		switch (anglePlatformType) {
			case AnglePlatformType.OpenGL:
				platform = Egl.PLATFORM_ANGLE_TYPE_OPENGL_ANGLE;
				break;
			case AnglePlatformType.OpenGLES:
				platform = Egl.PLATFORM_ANGLE_TYPE_OPENGLES_ANGLE;
				break;
			case AnglePlatformType.D3D9:
				platform = Egl.PLATFORM_ANGLE_TYPE_D3D9_ANGLE;
				break;
			case AnglePlatformType.D3D11:
				platform = Egl.PLATFORM_ANGLE_TYPE_D3D11_ANGLE;
				break;
			case AnglePlatformType.Vulkan:
				platform = Egl.PLATFORM_ANGLE_TYPE_VULKAN_ANGLE;
				break;
			case AnglePlatformType.Metal:
				platform = Egl.PLATFORM_ANGLE_TYPE_METAL_ANGLE;
				break;
		}
		int[] platformAttributes = new int[]
		{
			Egl.PLATFORM_ANGLE_TYPE_ANGLE, platform,
			Egl.NONE
		};

		//getEGLNativeDisplay
		Display = Egl.GetPlatformDisplayEXT(Egl.PLATFORM_ANGLE_ANGLE, display, platformAttributes);

		Egl.Initialize(Display, out int major, out int minor);
		Egl.BindAPI(RenderApi.ES);

		IntPtr[] configs = new IntPtr[1];
		int[] configAttributes = new int[]
		{
			Egl.RENDERABLE_TYPE, Egl.OPENGL_ES2_BIT,
			Egl.BUFFER_SIZE, 0,
			Egl.NONE
		};
		unsafe {
			Egl.ChooseConfig(Display, configAttributes, configs, configs.Length, out int num_config);
		}

		int[] contextAttributes = new int[]
		{
            //Egl.CONTEXT_CLIENT_VERSION, 2,
            Egl.CONTEXT_MAJOR_VERSION, 2,
			Egl.CONTEXT_MINOR_VERSION, 0,
			Egl.NONE
		};
		Context = Egl.CreateContext(Display, configs[0], 0, contextAttributes);

		int[] surfaceAttributes = new int[]
		{
			Egl.NONE
		};

		//Surface = Egl.CreatePlatformWindowSurfaceEXT(Display, configs[0], windowHandle, null);
		Surface = Egl.CreateWindowSurface(Display, configs[0], windowHandle, 0);

		var error = Egl.GetError();
		if (error != OpenTK.Graphics.Egl.ErrorCode.SUCCESS) {
			Trace.TraceError("Ran into an error while setting up the window surface. OpenTaiko might be broken... :^(" +
				$"\nEgl Error: {error}" +
				$"\nWindow Flags: {window.Native.Kind} (Selected {selectedflag})" +
				$"\nAnglePlatformType: {anglePlatformType}" +
				$"\nDisplay: {Display}" +
				$"\nContext: {Context}" +
				$"\nSurface: {Surface}");
		}
	}

	public nint Handle { get; set; }

	public IGLContextSource? Source { get; set; }

	public bool IsCurrent { get; set; } = true;

	public nint GetProcAddress(string proc, int? slot = null) {
		nint addr = Egl.GetProcAddress(proc);
		return addr;
	}

	public bool TryGetProcAddress(string proc, out nint addr, int? slot = null) {
		addr = Egl.GetProcAddress(proc);
		return addr != 0;
	}

	public void SwapInterval(int interval) {
		Egl.SwapInterval(Display, interval);
	}

	public void SwapBuffers() {
		Egl.SwapBuffers(Display, Surface);
	}

	public void MakeCurrent() {
		Egl.MakeCurrent(Display, Surface, Surface, Context);
	}

	public void Clear() {
	}

	public void Dispose() {
		Egl.DestroyContext(Display, Context);
		Egl.DestroySurface(Display, Surface);
		Egl.Terminate(Display);
	}
}
