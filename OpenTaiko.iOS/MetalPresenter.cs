using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using CoreAnimation;
using CoreGraphics;
using CoreVideo;
using Foundation;
using Metal;
using ObjCRuntime;
using OpenGLES;

namespace OpenTaiko.iOS;

// Metal present-boundary (see project_ios_render_metal_plan). FDK renders the scene through GLES
// into a shared, IOSurface-backed render target; the host samples it with Metal and presents via
// a CAMetalLayer. Breaks the in-frame GL blit/FBO read-back that serialized CPU and GPU (-> 120fps).
//
// Hard-won device constraints:
//  - The render target MUST track GameWindowSize (OpenTaiko sets it to the skin resolution, e.g.
//    1920x1080, after init and on skin change). A fixed-size surface captures only a corner -> zoom.
//  - CVPixelBuffer must set OpenGLESCompatibilityKey but NOT MetalCompatibilityKey: the Metal key
//    makes iOS allocate a Metal-private layout that GL's texImageIOSurface refuses to bind
//    (incomplete FBO -> black). CVMetalTextureCache wraps a plain IOSurface fine.
//  - Bind GL first (texImageIOSurface), then wrap with Metal.
//  - Sample with IDENTITY uv: GL's bottom-left origin means Metal uv.y=0 is GL's bottom row.
//
// Sync is a plain glFlush (latency-neutral; can tear under load -> re-add double-buffering if so).
public sealed class MetalPresenter : IDisposable {
	private readonly IMTLDevice _device;
	private readonly IMTLCommandQueue _queue;
	private readonly CAMetalLayer _layer;
	private readonly EAGLContext _glContext;
	private readonly CVMetalTextureCache _metalCache;
	private readonly IMTLRenderPipelineState _pipeline;

	private CVPixelBuffer? _pixelBuffer;
	private CVMetalTexture? _metalTex;
	private IMTLTexture? _sharedTexture;
	private uint _glFbo;
	private uint _glTexName;
	private int _targetWidth;
	private int _targetHeight;

	private int _drawableWidth;
	private int _drawableHeight;
	private bool _loggedDrawable;

	private const string ShaderSource = @"
#include <metal_stdlib>
using namespace metal;
struct VOut { float4 pos [[position]]; float2 uv; };
vertex VOut v_main(uint vid [[vertex_id]]) {
    float2 p = float2(float((vid << 1) & 2), float(vid & 2));
    VOut o;
    o.pos = float4(p * 2.0 - 1.0, 0.0, 1.0);
    o.uv  = float2(p.x, p.y); // identity: GL bottom-left origin -> Metal uv.y=0 is GL's bottom row
    return o;
}
fragment float4 f_main(VOut v [[stage_in]], texture2d<float> tex [[texture(0)]]) {
    constexpr sampler s(mag_filter::linear, min_filter::linear);
    return tex.sample(s, v.uv);
}";

	// -[EAGLContext texImageIOSurface:...] is present natively but not surfaced in the managed
	// binding, so call it directly. Binds an IOSurface to the currently-bound GL texture.
	private static readonly IntPtr SelTexImageIOSurface =
		Selector.GetHandle("texImageIOSurface:target:internalFormat:width:height:format:type:plane:");

	// NOTE: the GL enum/size args are NSUInteger (64-bit on arm64) in the real selector, NOT 32-bit.
	// Declaring them as uint works under the Debug interpreter (high bits zero-extended) but FAILS
	// under Release AOT (garbage high bits -> bogus enums -> texImageIOSurface returns NO -> black).
	// Use nuint so the full 64-bit register/stack slots are written correctly.
	[DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
	private static extern byte MsgSendTexImageIOSurface(IntPtr receiver, IntPtr sel, IntPtr ioSurface,
		nuint target, nuint internalFormat, nuint width, nuint height, nuint format, nuint type, nuint plane);

	public MetalPresenter(CAMetalLayer layer, EAGLContext glContext) {
		_device = MTLDevice.SystemDefault ?? throw new InvalidOperationException("No Metal device");
		_queue = _device.CreateCommandQueue() ?? throw new InvalidOperationException("No Metal command queue");
		_glContext = glContext;

		_layer = layer;
		_layer.Device = _device;
		_layer.PixelFormat = MTLPixelFormat.BGRA8Unorm;
		_layer.FramebufferOnly = true;

		_metalCache = new CVMetalTextureCache(_device);
		_pipeline = BuildPipeline();
		Console.WriteLine("[MetalPresenter] initialized");
	}

	private IMTLRenderPipelineState BuildPipeline() {
		var lib = _device.CreateLibrary(ShaderSource, new MTLCompileOptions(), out NSError? libErr);
		if (lib == null) throw new InvalidOperationException($"Metal library compile failed: {libErr?.LocalizedDescription}");
		var desc = new MTLRenderPipelineDescriptor {
			VertexFunction = lib.CreateFunction("v_main"),
			FragmentFunction = lib.CreateFunction("f_main"),
		};
		desc.ColorAttachments[0].PixelFormat = MTLPixelFormat.BGRA8Unorm;
		var pipeline = _device.CreateRenderPipelineState(desc, out NSError? pErr);
		if (pipeline == null) throw new InvalidOperationException($"Metal pipeline failed: {pErr?.LocalizedDescription}");
		return pipeline;
	}

	/// <summary>Ensure the shared render target matches the current game resolution, recreating it
	/// on change. Returns the GL framebuffer FDK should render the scene into.</summary>
	public uint EnsureRenderTarget(int width, int height) {
		if (_pixelBuffer != null && width == _targetWidth && height == _targetHeight)
			return _glFbo;

		DisposeSurface();
		_targetWidth = width;
		_targetHeight = height;

		// IOSurface-backed pixel buffer at game resolution, shared GL<->Metal with no copy.
		var attrs = new CVPixelBufferAttributes {
			PixelFormatType = CVPixelFormatType.CV32BGRA,
			Width = width,
			Height = height,
		};
		attrs.Dictionary[CVPixelBuffer.IOSurfacePropertiesKey] = new NSDictionary();
		attrs.Dictionary[CVPixelBuffer.OpenGLESCompatibilityKey] = NSNumber.FromBoolean(true);
		_pixelBuffer = new CVPixelBuffer(width, height, CVPixelFormatType.CV32BGRA, attrs);
		var ioSurface = _pixelBuffer.GetIOSurface() ?? throw new InvalidOperationException("CVPixelBuffer has no IOSurface");

		// GL first: bind the IOSurface to a GL texture + FBO (FDK's render target).
		GLES.GenTextures(1, out _glTexName);
		GLES.BindTexture(GLES.GL_TEXTURE_2D, _glTexName);
		GLES.TexParameteri(GLES.GL_TEXTURE_2D, GLES.GL_TEXTURE_MIN_FILTER, (int)GLES.GL_LINEAR);
		GLES.TexParameteri(GLES.GL_TEXTURE_2D, GLES.GL_TEXTURE_MAG_FILTER, (int)GLES.GL_LINEAR);
		GLES.TexParameteri(GLES.GL_TEXTURE_2D, GLES.GL_TEXTURE_WRAP_S, (int)GLES.GL_CLAMP_TO_EDGE);
		GLES.TexParameteri(GLES.GL_TEXTURE_2D, GLES.GL_TEXTURE_WRAP_T, (int)GLES.GL_CLAMP_TO_EDGE);
		byte bound = MsgSendTexImageIOSurface((IntPtr)_glContext.Handle, SelTexImageIOSurface, (IntPtr)ioSurface.Handle,
			(nuint)GLES.GL_TEXTURE_2D, (nuint)0x1908 /* GL_RGBA */, (nuint)width, (nuint)height, (nuint)0x80E1 /* GL_BGRA_EXT */, (nuint)0x1401 /* GL_UNSIGNED_BYTE */, (nuint)0);

		GLES.GenFramebuffers(1, out _glFbo);
		GLES.BindFramebuffer(GLES.GL_FRAMEBUFFER, _glFbo);
		GLES.FramebufferTexture2D(GLES.GL_FRAMEBUFFER, GLES.GL_COLOR_ATTACHMENT0, GLES.GL_TEXTURE_2D, _glTexName, 0);
		uint status = GLES.CheckFramebufferStatus(GLES.GL_FRAMEBUFFER);

		// Metal view of the same surface (after the GL bind).
		_metalTex = _metalCache.TextureFromImage(_pixelBuffer, MTLPixelFormat.BGRA8Unorm, width, height, 0, out CVReturn mret)
			?? throw new InvalidOperationException($"CVMetalTexture failed: {mret}");
		_sharedTexture = _metalTex.Texture;

		Trace.TraceInformation($"[MetalPresenter] render target {width}x{height} fbo={_glFbo} status=0x{status:X} texImage={bound}");
		return _glFbo;
	}

	/// <summary>Match the on-screen drawable to the view's pixel size (native resolution).</summary>
	public void UpdateDrawableSize(int pixelWidth, int pixelHeight) {
		_drawableWidth = pixelWidth;
		_drawableHeight = pixelHeight;
		_layer.DrawableSize = new CGSize(pixelWidth, pixelHeight);
	}

	/// <summary>Sample the shared surface (just rendered by FDK) and present it, letterboxed to
	/// preserve the game's aspect ratio (matching the old GL blit viewport).</summary>
	public void Present() {
		if (_sharedTexture == null) return;
		using var drawable = _layer.NextDrawable();
		if (drawable == null) return;

		var rpd = new MTLRenderPassDescriptor();
		rpd.ColorAttachments[0].Texture = drawable.Texture;
		rpd.ColorAttachments[0].LoadAction = MTLLoadAction.Clear; // black letterbox bars
		rpd.ColorAttachments[0].ClearColor = new MTLClearColor(0, 0, 0, 1);
		rpd.ColorAttachments[0].StoreAction = MTLStoreAction.Store;

		// Aspect-preserving fit of the game render into the actual drawable.
		double gameAspect = _targetWidth / (double)_targetHeight;
		double dw = drawable.Texture.Width, dh = drawable.Texture.Height;
		double vw, vh, vx, vy;
		if (dw / dh > gameAspect) { // drawable wider than game → pillarbox (bars left/right)
			vh = dh; vw = dh * gameAspect; vx = (dw - vw) / 2.0; vy = 0;
		} else {                    // taller → letterbox (bars top/bottom)
			vw = dw; vh = dw / gameAspect; vx = 0; vy = (dh - vh) / 2.0;
		}
		if (!_loggedDrawable) {
			_loggedDrawable = true;
			Trace.TraceInformation($"[MetalPresenter] target={_targetWidth}x{_targetHeight} drawableTex={dw:F0}x{dh:F0} viewport=({vx:F0},{vy:F0},{vw:F0},{vh:F0})");
		}

		var cb = _queue.CommandBuffer();
		var enc = cb.CreateRenderCommandEncoder(rpd);
		enc.SetViewport(new MTLViewport { OriginX = vx, OriginY = vy, Width = vw, Height = vh, ZNear = 0, ZFar = 1 });
		enc.SetRenderPipelineState(_pipeline);
		enc.SetFragmentTexture(_sharedTexture, 0);
		enc.DrawPrimitives(MTLPrimitiveType.Triangle, 0, 3);
		enc.EndEncoding();
		cb.PresentDrawable(drawable);
		cb.Commit();
	}

	private void DisposeSurface() {
		if (_glFbo != 0) { GLES.DeleteFramebuffers(1, ref _glFbo); _glFbo = 0; }
		if (_glTexName != 0) { GLES.DeleteTextures(1, ref _glTexName); _glTexName = 0; }
		_metalTex?.Dispose(); _metalTex = null;
		_sharedTexture = null;
		_pixelBuffer?.Dispose(); _pixelBuffer = null;
	}

	public void Dispose() {
		DisposeSurface();
		_metalCache?.Dispose();
	}
}
