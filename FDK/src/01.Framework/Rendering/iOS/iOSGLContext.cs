using System.Runtime.InteropServices;
using Silk.NET.Core.Contexts;

namespace FDK;

/// <summary>
/// IGLContext implementation for iOS. The GL context and renderbuffer are owned by the caller,
/// which supplies make-current and present callbacks; this class adapts them to IGLContext so
/// FDK's GL pipeline can drive them.
///
/// GL function pointers are resolved via dlsym from the OpenGLES framework.
/// </summary>
public class iOSGLContext : IGLContext {
	private readonly nint _glFrameworkHandle;
	private readonly Action _swapBuffersAction;
	private readonly Action _makeCurrentAction;

	/// <summary>
	/// Creates a new iOS GL context wrapper.
	/// </summary>
	/// <param name="swapBuffers">Action that presents the renderbuffer.</param>
	/// <param name="makeCurrent">Action that makes the GL context current.</param>
	public iOSGLContext(Action swapBuffers, Action makeCurrent) {
		_swapBuffersAction = swapBuffers;
		_makeCurrentAction = makeCurrent;

		// Load the OpenGLES framework to resolve GL function pointers
		_glFrameworkHandle = dlopen("/System/Library/Frameworks/OpenGLES.framework/OpenGLES", RTLD_LAZY);
		if (_glFrameworkHandle == IntPtr.Zero) {
			throw new Exception("Failed to load OpenGLES.framework");
		}
	}

	public nint Handle { get; set; }

	public IGLContextSource? Source { get; set; }

	public bool IsCurrent { get; set; } = true;

	public nint GetProcAddress(string proc, int? slot = null) {
		return dlsym(_glFrameworkHandle, proc);
	}

	public bool TryGetProcAddress(string proc, out nint addr, int? slot = null) {
		addr = dlsym(_glFrameworkHandle, proc);
		return addr != IntPtr.Zero;
	}

	public void SwapInterval(int interval) {
		// On iOS vsync is driven by the display refresh callback, not a swap interval.
	}

	public void SwapBuffers() {
		_swapBuffersAction();
	}

	public void MakeCurrent() {
		_makeCurrentAction();
		IsCurrent = true;
	}

	public void Clear() {
	}

	public void Dispose() {
		if (_glFrameworkHandle != IntPtr.Zero) {
			dlclose(_glFrameworkHandle);
		}
	}

	// Native interop for dynamic library loading
	private const int RTLD_LAZY = 0x1;

	[DllImport("libdl.dylib")]
	private static extern nint dlopen(string path, int mode);

	[DllImport("libdl.dylib")]
	private static extern nint dlsym(nint handle, string symbol);

	[DllImport("libdl.dylib")]
	private static extern int dlclose(nint handle);
}
