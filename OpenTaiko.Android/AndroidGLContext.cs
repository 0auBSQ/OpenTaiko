using System.Runtime.InteropServices;
using Android.Opengl;
using Android.Views;
using Silk.NET.Core.Contexts;

namespace OpenTaiko.Android;

/// <summary>
/// IGLContext implementation for Android over EGL14: owns the EGL display/config/context, creates a
/// window surface from the SurfaceView's Surface (recreated on surface loss), and parks the context
/// on a 1x1 pbuffer while the app is backgrounded so GL resources survive. GL entry points resolve
/// through eglGetProcAddress, exactly what Silk.NET's GL.GetApi needs.
/// The context is thread-affine: only the render thread calls MakeCurrent/SwapBuffers.
/// </summary>
public class AndroidGLContext : IGLContext {
	private readonly EGLDisplay _display;
	private readonly EGLConfig _config;
	private readonly EGLContext _context;
	private EGLSurface? _surface;         // current window surface (null while backgrounded)
	private EGLSurface? _pbuffer;         // 1x1 park target so the context stays current-able

	public AndroidGLContext() {
		_display = EGL14.EglGetDisplay(EGL14.EglDefaultDisplay)!;
		if (_display == EGL14.EglNoDisplay) throw new Exception("eglGetDisplay failed");
		int[] version = new int[2];
		if (!EGL14.EglInitialize(_display, version, 0, version, 1))
			throw new Exception($"eglInitialize failed: 0x{EGL14.EglGetError():X}");

		int EGL_OPENGL_ES3_BIT = 0x40;
		int[] configAttribs = {
			EGL14.EglRenderableType, EGL_OPENGL_ES3_BIT,
			EGL14.EglSurfaceType, EGL14.EglWindowBit | EGL14.EglPbufferBit,
			EGL14.EglRedSize, 8, EGL14.EglGreenSize, 8, EGL14.EglBlueSize, 8, EGL14.EglAlphaSize, 8,
			EGL14.EglDepthSize, 24, EGL14.EglStencilSize, 8,
			EGL14.EglNone
		};
		var configs = new EGLConfig[1];
		int[] numConfigs = new int[1];
		if (!EGL14.EglChooseConfig(_display, configAttribs, 0, configs, 0, 1, numConfigs, 0) || numConfigs[0] == 0) {
			// Some GPUs refuse depth 24 + stencil 8 together; retry with depth 16, no stencil.
			configAttribs[13] = 16; configAttribs[15] = 0;
			if (!EGL14.EglChooseConfig(_display, configAttribs, 0, configs, 0, 1, numConfigs, 0) || numConfigs[0] == 0)
				throw new Exception("eglChooseConfig found no ES3 RGBA8888 config");
		}
		_config = configs[0]!;

		int[] contextAttribs = { EGL14.EglContextClientVersion, 3, EGL14.EglNone };
		_context = EGL14.EglCreateContext(_display, _config, EGL14.EglNoContext, contextAttribs, 0)!;
		if (_context == EGL14.EglNoContext)
			throw new Exception($"eglCreateContext(ES3) failed: 0x{EGL14.EglGetError():X}");
	}

	/// <summary>Create (or replace) the window surface for the current SurfaceView surface.</summary>
	public void CreateWindowSurface(Surface surface) {
		DestroyWindowSurface();
		int[] attribs = { EGL14.EglNone };
		_surface = EGL14.EglCreateWindowSurface(_display, _config, surface, attribs, 0);
		if (_surface == EGL14.EglNoSurface)
			throw new Exception($"eglCreateWindowSurface failed: 0x{EGL14.EglGetError():X}");
	}

	/// <summary>Drop the window surface (surfaceDestroyed) and park the context on a pbuffer so
	/// textures/buffers stay alive while backgrounded.</summary>
	public void ParkContext() {
		if (_pbuffer == null) {
			int[] pbAttribs = { EGL14.EglWidth, 1, EGL14.EglHeight, 1, EGL14.EglNone };
			_pbuffer = EGL14.EglCreatePbufferSurface(_display, _config, pbAttribs, 0);
		}
		EGL14.EglMakeCurrent(_display, _pbuffer, _pbuffer, _context);
		DestroyWindowSurface();
	}

	private void DestroyWindowSurface() {
		if (_surface != null && _surface != EGL14.EglNoSurface) {
			EGL14.EglDestroySurface(_display, _surface);
			_surface = null;
		}
	}

	public bool HasWindowSurface => _surface != null;

	// ── IGLContext ─────────────────────────────────────────────────────────────────────────────
	public nint Handle => (nint)_context.NativeHandle;
	public IGLContextSource? Source => null;
	public bool IsCurrent { get; private set; }

	public nint GetProcAddress(string proc, int? slot = null) => eglGetProcAddress(proc);

	public bool TryGetProcAddress(string proc, out nint addr, int? slot = null) {
		addr = eglGetProcAddress(proc);
		return addr != IntPtr.Zero;
	}

	public void SwapInterval(int interval) {
		EGL14.EglSwapInterval(_display, interval);
	}

	public void SwapBuffers() {
		if (_surface == null) return;
		if (!EGL14.EglSwapBuffers(_display, _surface)) {
			int err = EGL14.EglGetError();
			// EGL_CONTEXT_LOST (0x300E) / bad surface: the host recreates the surface on the next
			// surfaceCreated; nothing useful to do mid-frame.
			global::Android.Util.Log.Warn("OpenTaiko", $"eglSwapBuffers failed: 0x{err:X}");
		}
	}

	public void MakeCurrent() {
		var target = _surface ?? _pbuffer;
		if (target == null) return;
		if (!EGL14.EglMakeCurrent(_display, target, target, _context))
			throw new Exception($"eglMakeCurrent failed: 0x{EGL14.EglGetError():X}");
		IsCurrent = true;
	}

	public void Clear() {
		EGL14.EglMakeCurrent(_display, EGL14.EglNoSurface, EGL14.EglNoSurface, EGL14.EglNoContext);
		IsCurrent = false;
	}

	public void Dispose() {
		Clear();
		DestroyWindowSurface();
		if (_pbuffer != null) EGL14.EglDestroySurface(_display, _pbuffer);
		EGL14.EglDestroyContext(_display, _context);
		EGL14.EglTerminate(_display);
	}

	[DllImport("libEGL.so", EntryPoint = "eglGetProcAddress")]
	private static extern nint eglGetProcAddress(string procname);
}