using System.Runtime.InteropServices;

namespace OpenTaiko.iOS;

/// <summary>
/// Raw OpenGL ES 2.0 P/Invoke bindings for framebuffer/texture management.
/// The Microsoft.iOS OpenGLES binding only surfaces the EAGL types (EAGLContext, etc.),
/// not the gl* C entrypoints, so the FBO/texture calls have to be declared by hand.
/// </summary>
internal static class GLES {
	private const string Lib = "/System/Library/Frameworks/OpenGLES.framework/OpenGLES";

	public const uint GL_FRAMEBUFFER = 0x8D40;
	public const uint GL_RENDERBUFFER = 0x8D41;
	public const uint GL_TEXTURE_2D = 0x0DE1;
	public const uint GL_TEXTURE_MIN_FILTER = 0x2801;
	public const uint GL_TEXTURE_MAG_FILTER = 0x2800;
	public const uint GL_TEXTURE_WRAP_S = 0x2802;
	public const uint GL_TEXTURE_WRAP_T = 0x2803;
	public const uint GL_LINEAR = 0x2601;
	public const uint GL_CLAMP_TO_EDGE = 0x812F;
	public const uint GL_COLOR_ATTACHMENT0 = 0x8CE0;
	public const uint GL_DEPTH_ATTACHMENT = 0x8D00;
	public const uint GL_DEPTH_COMPONENT16 = 0x81A5;
	public const uint GL_RENDERBUFFER_WIDTH = 0x8D42;
	public const uint GL_RENDERBUFFER_HEIGHT = 0x8D43;
	public const uint GL_FRAMEBUFFER_COMPLETE = 0x8CD5;

	[DllImport(Lib, EntryPoint = "glGenFramebuffers")]
	public static extern void GenFramebuffers(int n, out uint framebuffers);

	[DllImport(Lib, EntryPoint = "glBindFramebuffer")]
	public static extern void BindFramebuffer(uint target, uint framebuffer);

	[DllImport(Lib, EntryPoint = "glGenRenderbuffers")]
	public static extern void GenRenderbuffers(int n, out uint renderbuffers);

	[DllImport(Lib, EntryPoint = "glBindRenderbuffer")]
	public static extern void BindRenderbuffer(uint target, uint renderbuffer);

	[DllImport(Lib, EntryPoint = "glFramebufferRenderbuffer")]
	public static extern void FramebufferRenderbuffer(uint target, uint attachment, uint renderbuffertarget, uint renderbuffer);

	[DllImport(Lib, EntryPoint = "glFramebufferTexture2D")]
	public static extern void FramebufferTexture2D(uint target, uint attachment, uint textarget, uint texture, int level);

	[DllImport(Lib, EntryPoint = "glGenTextures")]
	public static extern void GenTextures(int n, out uint textures);

	[DllImport(Lib, EntryPoint = "glBindTexture")]
	public static extern void BindTexture(uint target, uint texture);

	[DllImport(Lib, EntryPoint = "glDeleteTextures")]
	public static extern void DeleteTextures(int n, ref uint textures);

	[DllImport(Lib, EntryPoint = "glTexParameteri")]
	public static extern void TexParameteri(uint target, uint pname, int param);

	[DllImport(Lib, EntryPoint = "glRenderbufferStorage")]
	public static extern void RenderbufferStorage(uint target, uint internalformat, int width, int height);

	[DllImport(Lib, EntryPoint = "glGetRenderbufferParameteriv")]
	public static extern void GetRenderbufferParameteriv(uint target, uint pname, out int param);

	[DllImport(Lib, EntryPoint = "glCheckFramebufferStatus")]
	public static extern uint CheckFramebufferStatus(uint target);

	[DllImport(Lib, EntryPoint = "glDeleteFramebuffers")]
	public static extern void DeleteFramebuffers(int n, ref uint framebuffers);

	[DllImport(Lib, EntryPoint = "glDeleteRenderbuffers")]
	public static extern void DeleteRenderbuffers(int n, ref uint renderbuffers);

	[DllImport(Lib, EntryPoint = "glClearColor")]
	public static extern void ClearColor(float red, float green, float blue, float alpha);

	[DllImport(Lib, EntryPoint = "glClear")]
	public static extern void Clear(uint mask);
}
