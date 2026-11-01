using System;
using Silk.NET.OpenGLES;

namespace OpenTaiko {
	// 2D drawing composited onto the canvas after the 3D.
	internal sealed partial class GpuRasterizer {
		// 2D overlay (DrawLine/FillRect issued AFTER the 3D, e.g. crosshair/selection): composited onto
		// the canvas texture via the FBO, since the CPU buffer no longer holds the 3D in GPU mode.
		private uint _prog2d, _vbo2d, _vao2d; private int _uRes2d, _uColor2d; private bool _2dActive; private GlPass _pass2d;

		// ── 2D overlay onto the canvas texture (crosshair/selection/etc. drawn after the 3D) ──────────
		private unsafe void Begin2D(Lua3DScene s) {
			if (_2dActive) return;
			EnsureGL(); EnsureFbo(s);
			_pass2d = GlPass.Begin(Gl);
			GlPass.Target(Gl, _fbo, s._w, s._h);
			Gl.Disable(EnableCap.DepthTest);
			Gl.Enable(EnableCap.Blend); GlPass.BlendOver(Gl);
			Gl.UseProgram(_prog2d); Gl.Uniform2(_uRes2d, (float)s._w, (float)s._h);
			Gl.BindVertexArray(_vao2d);   // separate VAO so we don't disturb the 3D VAO's format
			Gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo2d);
			Gl.EnableVertexAttribArray(0); Gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), (void*)0);
			_2dActive = true;
		}
		public void Line2D(Lua3DScene s, int x0, int y0, int x1, int y1, int r, int g, int b) {
			Begin2D(s);
			Span<float> v = stackalloc float[] { x0, y0, x1, y1 };
			Gl.BufferData<float>(BufferTargetARB.ArrayBuffer, (ReadOnlySpan<float>)v, BufferUsageARB.StreamDraw);
			Gl.Uniform4(_uColor2d, r / 255f, g / 255f, b / 255f, 1f);
			Gl.DrawArrays(PrimitiveType.Lines, 0, 2);
		}
		public void Rect2D(Lua3DScene s, int x, int y, int w, int h, int r, int g, int b, int a) {
			Begin2D(s);
			float x0 = x, y0 = y, x1 = x + w, y1 = y + h;
			Span<float> v = stackalloc float[] { x0, y0, x1, y0, x1, y1, x0, y0, x1, y1, x0, y1 };
			Gl.BufferData<float>(BufferTargetARB.ArrayBuffer, (ReadOnlySpan<float>)v, BufferUsageARB.StreamDraw);
			Gl.Uniform4(_uColor2d, r / 255f, g / 255f, b / 255f, a / 255f);
			Gl.DrawArrays(PrimitiveType.Triangles, 0, 6);
		}
		public void End2D() {   // called by Lua3DScene.Upload() in GPU mode after the stage's 2D overlay
			if (!_2dActive) return;
			_2dActive = false;
			_pass2d.End(Gl);
		}
	}
}
