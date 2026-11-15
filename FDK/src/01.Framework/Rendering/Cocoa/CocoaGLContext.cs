using System.Diagnostics;
using System.Runtime.InteropServices;
using Silk.NET.Core.Contexts;

namespace FDK;

/// <summary>
/// macOS only. An <see cref="IGLContext"/> over an NSOpenGLContext that the game owns, instead of one GLFW
/// created. The window is built by GLFW with GLFW_NO_API, so GLFW never creates an NSGL context and its cocoa
/// callbacks never issue [NSOpenGLContext update] from the window thread.
///
/// The context and pixel format are created on the main thread (together with setView:). The game thread makes
/// it current and performs all rendering, presentation and drawable updates; the main thread only releases it
/// after the game thread has finished.
/// </summary>
internal sealed class CocoaGLContext : IGLContext {
	private const string ObjCRuntime = "/usr/lib/libobjc.A.dylib";
	private const string OpenGLFramework = "/System/Library/Frameworks/OpenGL.framework/OpenGL";

	// NSOpenGLPixelFormatAttribute
	private const uint NSOpenGLPFADoubleBuffer = 5;
	private const uint NSOpenGLPFAColorSize = 8;
	private const uint NSOpenGLPFAAlphaSize = 11;
	private const uint NSOpenGLPFADepthSize = 12;
	private const uint NSOpenGLPFAStencilSize = 13;
	private const uint NSOpenGLPFAAccelerated = 73;
	private const uint NSOpenGLPFAClosestPolicy = 74;
	private const uint NSOpenGLPFAOpenGLProfile = 99;
	private const uint NSOpenGLProfileVersion3_2Core = 0x3200;

	// NSOpenGLContextParameter
	private const int NSOpenGLCPSwapInterval = 222;

	[DllImport(ObjCRuntime)]
	private static extern nint objc_getClass(string name);

	[DllImport(ObjCRuntime)]
	private static extern nint sel_registerName(string name);

	[DllImport(ObjCRuntime, EntryPoint = "objc_msgSend")]
	private static extern nint Send(nint receiver, nint selector);

	[DllImport(ObjCRuntime, EntryPoint = "objc_msgSend")]
	private static extern nint Send(nint receiver, nint selector, nint arg);

	[DllImport(ObjCRuntime, EntryPoint = "objc_msgSend")]
	private static extern nint Send(nint receiver, nint selector, nint arg1, nint arg2);

	[DllImport(ObjCRuntime, EntryPoint = "objc_msgSend")]
	private static extern void SendVoid(nint receiver, nint selector);

	[DllImport(ObjCRuntime, EntryPoint = "objc_msgSend")]
	private static extern void SendVoid(nint receiver, nint selector, nint arg);

	[DllImport(ObjCRuntime, EntryPoint = "objc_msgSend")]
	private static extern void SendVoid(nint receiver, nint selector, nint arg1, nint arg2);

	[DllImport(ObjCRuntime)]
	private static extern void objc_release(nint obj);

	private readonly nint _pixelFormat;
	private nint _context;
	private readonly nint _glFramework;
	private readonly nint _selMakeCurrentContext, _selFlushBuffer, _selUpdate, _selClearDrawable;
	private readonly nint _selSetValues, _selClearCurrentContext;
	private int _swapInterval;
	private bool _disposed;

	public CocoaGLContext(nint nsWindow) {
		_glFramework = NativeLibrary.Load(OpenGLFramework);

		_selMakeCurrentContext = sel_registerName("makeCurrentContext");
		_selFlushBuffer = sel_registerName("flushBuffer");
		_selUpdate = sel_registerName("update");
		_selClearDrawable = sel_registerName("clearDrawable");
		_selSetValues = sel_registerName("setValues:forParameter:");
		_selClearCurrentContext = sel_registerName("clearCurrentContext");

		nint view = Send(nsWindow, sel_registerName("contentView"));
		if (view == 0)
			throw new Exception("The Cocoa window has no content view to render into.");

		unsafe {
			// The same format GLFW asked for before this change (a 3.3 request becomes 3.2 core on macOS).
			uint* attributes = stackalloc uint[] {
				NSOpenGLPFAAccelerated,
				NSOpenGLPFAClosestPolicy,
				NSOpenGLPFAOpenGLProfile, NSOpenGLProfileVersion3_2Core,
				NSOpenGLPFAColorSize, 24,
				NSOpenGLPFAAlphaSize, 8,
				NSOpenGLPFADepthSize, 24,
				NSOpenGLPFAStencilSize, 8,
				NSOpenGLPFADoubleBuffer,
				0,
			};

			nint pixelFormatClass = objc_getClass("NSOpenGLPixelFormat");
			_pixelFormat = Send(Send(pixelFormatClass, sel_registerName("alloc")),
				sel_registerName("initWithAttributes:"), (nint)attributes);
			if (_pixelFormat == 0)
				throw new Exception("Failed to create the NSOpenGLPixelFormat for the game window.");

			nint contextClass = objc_getClass("NSOpenGLContext");
			_context = Send(Send(contextClass, sel_registerName("alloc")),
				sel_registerName("initWithFormat:shareContext:"), _pixelFormat, 0);
		}
		if (_context == 0) {
			objc_release(_pixelFormat);
			throw new Exception("Failed to create the NSOpenGLContext for the game window.");
		}

		Handle = _context;

		// The drawable stays at the window's point size, matching the game's point-based viewport, mouse and
		// readback math. A best-resolution (Retina) drawable would need those to use FramebufferSize instead.
		SendVoid(view, sel_registerName("setWantsBestResolutionOpenGLSurface:"), 0);
		SendVoid(_context, sel_registerName("setView:"), view);
	}

	public nint Handle { get; set; }

	public IGLContextSource? Source { get; set; }

	public bool IsCurrent { get; set; }

	public nint GetProcAddress(string proc, int? slot = null) {
		NativeLibrary.TryGetExport(_glFramework, proc, out nint address);
		return address;
	}

	public bool TryGetProcAddress(string proc, out nint addr, int? slot = null) {
		addr = GetProcAddress(proc, slot);
		return addr != 0;
	}

	public unsafe void SwapInterval(int interval) {
		if (_context == 0)
			return;
		_swapInterval = interval;
		// Also applied when called again later (the game's vsync setting); the context need not be current.
		SendVoid(_context, _selSetValues, (nint)(&interval), NSOpenGLCPSwapInterval);
	}

	public void SwapBuffers() {
		if (_context != 0)
			SendVoid(_context, _selFlushBuffer);
	}

	public void MakeCurrent() {
		if (_context == 0)
			return;
		SendVoid(_context, _selMakeCurrentContext);
		IsCurrent = true;
	}

	/// <summary>
	/// macOS only: the drawable follows the view. Runs on the game thread, the only thread that uses the
	/// context, so no lock is needed.
	/// </summary>
	public void Update() {
		if (_disposed || _context == 0)
			return;
		SendVoid(_context, _selUpdate);
	}

	/// <summary>Stops the context being current on the calling (game) thread.</summary>
	public void ReleaseCurrent() {
		if (_context == 0 || !IsCurrent)
			return;
		SendVoid(objc_getClass("NSOpenGLContext"), _selClearCurrentContext);
		IsCurrent = false;
	}

	public void Clear() {
	}

	/// <summary>Releases the context. Called on the main thread, after the game thread has finished.</summary>
	public void Dispose() {
		if (_disposed)
			return;
		_disposed = true;

		if (_context != 0) {
			SendVoid(_context, _selClearDrawable);
			objc_release(_context);
			Trace.TraceInformation($"Cocoa GL context released (swap interval was {_swapInterval}).");
			_context = 0;
			Handle = 0;
		}

		if (_pixelFormat != 0)
			objc_release(_pixelFormat);

		if (_glFramework != 0)
			NativeLibrary.Free(_glFramework);
	}
}
