using System.Collections.Generic;
using System.Diagnostics;
using Silk.NET.OpenGLES;

namespace OpenTaiko {
	// Off-screen reflection and portal targets.
	internal sealed partial class GpuRasterizer {
		// ── GPU off-screen reflection/portal render — replaces the CPU RenderView for mirrors/portals ──
		// Renders the scene from the camera CURRENTLY set on `s` (Lua3DScene.RenderView positions it) into a
		// dedicated rgba8 colour texture sized rw×rh, then registers that texture as texId's GL texture so the
		// mirror/portal surface (uMode 3) samples it directly — no CPU rasterise, no pixel readback. ScreenTex
		// objects are skipped (no mirrors-in-mirrors) because s._rttPass is true while this runs. This is the
		// fix for the manor's 25fps: a 233-sector reflection re-render is now a GPU pass, not a CPU one.
		private int _rttW, _rttH;
		private readonly Dictionary<int, uint> _rttColor = new();   // texId → colour texture
		private readonly Dictionary<int, uint> _rttFbos = new();    // texId → its OWN FBO (no attachment swapping)
		private readonly Dictionary<int, uint> _rttDepth = new();   // texId → its OWN depth RB (no shared GL state across FBOs)
		public unsafe void RenderToTexture(Lua3DScene s, int texId, int rw, int rh, int clr, int clg, int clb) {
			EnsureGL();
			End2D();
			if (rw < 1) rw = 1; if (rh < 1) rh = 1;
			var pass = GlPass.Begin(Gl);
			try { DrawToTexture(s, texId, rw, rh, clr, clg, clb); }
			finally { pass.End(Gl); }
		}

		private unsafe void DrawToTexture(Lua3DScene s, int texId, int rw, int rh, int clr, int clg, int clb) {
			if (_rttW != rw || _rttH != rh) {                       // size changed → drop all targets
				foreach (var t in _rttColor.Values) Gl.DeleteTexture(t); _rttColor.Clear();
				foreach (var f in _rttFbos.Values) Gl.DeleteFramebuffer(f); _rttFbos.Clear();
				foreach (var d in _rttDepth.Values) Gl.DeleteRenderbuffer(d); _rttDepth.Clear();
				_rttW = rw; _rttH = rh; _rttRev++;
			}
			// Each mirror/portal texId owns its OWN FBO + colour texture + DEPTH renderbuffer, built once.
			// Sharing a single FBO (attachment swapping) OR a single depth RB across FBOs corrupted the frame
			// once 2+ reflections were live (ANGLE/D3D11 mis-validates shared targets rendered in sequence).
			// Fully independent targets fix the multi-mirror/portal break.
			if (!_rttFbos.TryGetValue(texId, out uint fbo)) {
				uint color = Gl.GenTexture();
				Gl.BindTexture(TextureTarget.Texture2D, color);
				Gl.TexImage2D(TextureTarget.Texture2D, 0, (int)InternalFormat.Rgba8, (uint)rw, (uint)rh, 0, PixelFormat.Rgba, PixelType.UnsignedByte, null);
				Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Nearest);
				Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Nearest);
				Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
				Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);
				uint depth = Gl.GenRenderbuffer();
				Gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, depth);
				Gl.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.DepthComponent24, (uint)rw, (uint)rh);
				fbo = Gl.GenFramebuffer();
				Gl.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);
				Gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, color, 0);
				Gl.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, depth);
				if (Gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != GLEnum.FramebufferComplete)
					Trace.TraceError($"GpuRasterizer RTT FBO incomplete for texId {texId}");
				_rttColor[texId] = color; _rttFbos[texId] = fbo; _rttDepth[texId] = depth; _rttRev++;
			}

			PreparePass(s);

			GlPass.Target(Gl, fbo, rw, rh);
			GlPass.ClearColorDepth(Gl, clr, clg, clb);
			// the reflection needs the sky behind the geometry too (water mirrors mostly sky), seen from this camera
			EnsureSky(s);
			GlPass.DepthTested(Gl); Gl.Disable(EnableCap.CullFace);
			BindWorldProgram(false); Gl.BindVertexArray(_vao);   // mirror/portal RTT never casts/receives shadows
			SetFrameUniforms(s);
			DrawWorld(s, s._useLights ? 1 : 0, rw, rh, true);
			InvalidateDepth();
			// the colour texture lives in _rttColor; DrawScreenTex's TexFor returns it directly for texId,
			// so the mirror/portal surface samples this live GPU target with no CPU pixel-upload path.
		}

		/// <summary>Delete the GPU reflection target bound to this texId, so TexFor serves the id's
		/// registered pixels (a CPU capture) again.</summary>
		public void DropRenderTarget(int id) {
			if (_rttColor.TryGetValue(id, out var c)) { Gl.DeleteTexture(c); _rttColor.Remove(id); _rttRev++; }
			if (_rttFbos.TryGetValue(id, out var f)) { Gl.DeleteFramebuffer(f); _rttFbos.Remove(id); }
			if (_rttDepth.TryGetValue(id, out var d)) { Gl.DeleteRenderbuffer(d); _rttDepth.Remove(id); }
		}
	}
}
