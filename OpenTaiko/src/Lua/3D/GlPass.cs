using System;
using FDK;
using Silk.NET.OpenGLES;

namespace OpenTaiko {
	/// <summary>
	/// The GL state of the GPU rasterizer's passes. <see cref="Begin"/> saves what the caller had bound (framebuffer,
	/// viewport, scissor test) and turns the scissor test off: a UI clip is in screen space and must not cut an
	/// off-screen pass. <see cref="End"/> puts the caller's target back with the state the engine's 2D drawing expects.
	/// The static members give each pass its target, clears, depth and blend state explicitly.
	/// </summary>
	internal struct GlPass {
		private uint _fbo; private int _x, _y, _w, _h; private bool _scissor;

		/// <summary>Save the caller's framebuffer, viewport and scissor test, and turn the scissor test off.</summary>
		public static GlPass Begin(GL gl) {
			Span<int> vp = stackalloc int[4];
			gl.GetInteger(GLEnum.Viewport, vp);
			var p = new GlPass { _x = vp[0], _y = vp[1], _w = vp[2], _h = vp[3] };
			p._fbo = (uint)gl.GetInteger(GLEnum.FramebufferBinding);
			p._scissor = gl.IsEnabled(EnableCap.ScissorTest);
			gl.Disable(EnableCap.ScissorTest);
			return p;
		}

		/// <summary>Back to the caller's target, with the GL state the engine's 2D drawing expects.</summary>
		public readonly void End(GL gl) {
			gl.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
			gl.Viewport(_x, _y, (uint)_w, (uint)_h);
			gl.UseProgram(0); gl.BindVertexArray(0); gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
			gl.ActiveTexture(TextureUnit.Texture0); gl.BindTexture(TextureTarget.Texture2D, 0);
			gl.Disable(EnableCap.DepthTest); gl.DepthMask(true); gl.Disable(EnableCap.CullFace); gl.Disable(EnableCap.PolygonOffsetFill);
			gl.Enable(EnableCap.Blend); BlendHelper.SetBlend(BlendType.Normal);
			gl.BindSampler(0, 0);   // the post chain's sampler must not outlive it
			if (_scissor) gl.Enable(EnableCap.ScissorTest);
		}

		/// <summary>Draw into <paramref name="fbo"/>, over its whole <paramref name="w"/> x <paramref name="h"/> area.</summary>
		public static void Target(GL gl, uint fbo, int w, int h) {
			gl.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);
			gl.Viewport(0, 0, (uint)w, (uint)h);
		}

		/// <summary>Clear the bound target's depth. glClear honours the depth write mask, so the mask is turned on first:
		/// with it off the buffer would keep the last frame's depth and the world would z-fail invisible.</summary>
		public static void ClearDepth(GL gl) {
			gl.DepthMask(true);
			gl.Clear((uint)ClearBufferMask.DepthBufferBit);
		}

		/// <summary>Clear the bound target to an opaque colour (channels 0-255) and clear its depth.</summary>
		public static void ClearColorDepth(GL gl, int r, int g, int b) {
			gl.ClearColor(r / 255f, g / 255f, b / 255f, 1f);
			gl.DepthMask(true);   // glClear honours the depth write mask
			gl.Clear((uint)(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));
		}

		/// <summary>Depth-tested drawing: a fragment shows where it lies no farther than what the buffer holds.</summary>
		public static void DepthTested(GL gl) { gl.Enable(EnableCap.DepthTest); gl.DepthFunc(DepthFunction.Lequal); }

		/// <summary>A full-screen pass: no depth test or depth write, no blending.</summary>
		public static void FullScreen(GL gl) { gl.Disable(EnableCap.DepthTest); gl.DepthMask(false); gl.Disable(EnableCap.Blend); }

		/// <summary>A full-screen pass on the far plane, behind what is drawn: depth-tested, no depth write, no blending.</summary>
		public static void Backdrop(GL gl) { DepthTested(gl); gl.DepthMask(false); gl.Disable(EnableCap.Blend); }

		/// <summary>Opaque drawing: depth written, no blending.</summary>
		public static void Opaque(GL gl) { gl.DepthMask(true); gl.Disable(EnableCap.Blend); }

		/// <summary>Opaque drawing whose texels carry alpha (sprites, viewmodels): depth written, blended with
		/// <see cref="BlendCover"/>.</summary>
		public static void Covered(GL gl) { gl.DepthMask(true); gl.Enable(EnableCap.Blend); BlendCover(gl); }

		/// <summary>Translucent drawing: blended with <see cref="BlendOver"/>, no depth write.</summary>
		public static void Transparent(GL gl) { gl.Enable(EnableCap.Blend); BlendOver(gl); gl.DepthMask(false); }

		// Blending onto the canvas: the colour blends as usual while the canvas alpha only gains coverage
		// (a + dst.a * (1 - a)), so an opaque canvas stays opaque and a transparent one takes the draw's alpha.
		public static void BlendOver(GL gl) => gl.BlendFuncSeparate(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha, BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha);
		public static void BlendAdd(GL gl) => gl.BlendFuncSeparate(BlendingFactor.SrcAlpha, BlendingFactor.One, BlendingFactor.One, BlendingFactor.One);
		// opaque draws whose texels carry alpha (sprites, viewmodels): the colour overwrites, the alpha gains coverage
		public static void BlendCover(GL gl) => gl.BlendFuncSeparate(BlendingFactor.One, BlendingFactor.Zero, BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha);
	}
}
