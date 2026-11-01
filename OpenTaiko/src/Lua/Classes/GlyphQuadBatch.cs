using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using FDK;
using Silk.NET.OpenGLES;

namespace OpenTaiko {
	// Draws upright textured quads collected one by one with one GL draw per run of quads, in the order they were
	// added. A run binds up to Slots textures at once (a glyph font keeps its glyphs on a few atlas pages, and the
	// letters of a line hop between them) and holds up to MaxRunQuads quads, sent as a uniform array. Each quad gives
	// the pixels CTexture.t2DDraw gives for it in the normal blend mode with no gradient map: its corners come out
	// of t2DDraw's matrix with the same float steps, the fragment works out the same texture coordinate from the
	// same texture rect, and the texel is multiplied by the same tint. Glyph text sends its atlas pieces here.
	// Render thread only.
	internal static class GlyphQuadBatch {
		private const string VS = @"#version 300 es
precision highp float;
layout(location = 0) in vec3 aQuadCorner;
uniform mat4 camera;
uniform vec4 quads[192];
out vec2 texcoord;
flat out vec4 textureRect;
flat out vec4 color;
flat out vec4 pick;
void main() {
	int q = int(aQuadCorner.x) * 4;
	vec4 box = quads[q];
	texcoord = aQuadCorner.yz;
	textureRect = quads[q + 1];
	color = quads[q + 2];
	pick = vec4(equal(vec4(quads[q + 3].x), vec4(0.0, 1.0, 2.0, 3.0)));
	gl_Position = camera * vec4(aQuadCorner.y == 0.0 ? box.x : box.z, aQuadCorner.z == 0.0 ? box.y : box.w, 0.0, 1.0);
}";
		// every bound texture is sampled and the quad's own one kept by its 0/1 weight, so no sample sits in
		// flow that differs between quads
		private const string FS = @"#version 300 es
precision highp float;
uniform sampler2D tex0;
uniform sampler2D tex1;
uniform sampler2D tex2;
uniform sampler2D tex3;
in vec2 texcoord;
flat in vec4 textureRect;
flat in vec4 color;
flat in vec4 pick;
out vec4 frag;
void main() {
	vec2 rect = vec2(textureRect.xy + (texcoord * textureRect.zw));
	vec4 rawColor = texture(tex0, rect) * pick.x + texture(tex1, rect) * pick.y
		+ texture(tex2, rect) * pick.z + texture(tex3, rect) * pick.w;
	frag = rawColor * color;
	frag.rgb *= frag.a;
}";

		internal const int FloatsPerQuad = 16;       // NDC box (left, top, right, bottom), texture rect, tint, slot
		internal const int Slots = 4;                // textures one run binds (units 0..3)
		internal const int MaxRunQuads = 48;         // the quads uniform array: 4 vectors each

		private static GL Gl => Game.Gl;
		private static readonly List<float[]> _runs = [];       // each run's quad data, MaxRunQuads quads long
		private static readonly List<int> _runQuads = [];
		private static readonly List<CTexture?> _runTex = [];   // Slots entries per run, null = slot unused
		private static int _runUsed;                             // slots the last run uses
		private static readonly Stack<float[]> _spare = new();
		private static uint _prog, _vao, _vbo, _ebo;
		private static int _camLoc, _quadsLoc;
		private static bool _init, _broken;

		// true when quads can go through the batch now: its program built, and no global gradient map is set
		// (that one tints every 2D draw, which only CTexture's own draw applies)
		public static bool Usable => CTexture.ActiveGradientMapId == 0 && Ensure();

		private static bool Ensure() {
			if (_init || _broken) return !_broken;
			try {
				_prog = ShaderHelper.CreateShaderProgramFromSource(VS, FS);
				_camLoc = Gl.GetUniformLocation(_prog, "camera");
				_quadsLoc = Gl.GetUniformLocation(_prog, "quads");
				Gl.UseProgram(_prog);
				for (int k = 0; k < Slots; k++) Gl.Uniform1(Gl.GetUniformLocation(_prog, "tex" + k), k);
				// a fixed mesh of MaxRunQuads quads: per vertex its quad and corner, two triangles per quad split
				// like CTexture's (top-left, top-right, bottom-left / bottom-left, top-right, bottom-right)
				var verts = new float[MaxRunQuads * 4 * 3];
				var idx = new ushort[MaxRunQuads * 6];
				for (int q = 0; q < MaxRunQuads; q++) {
					for (int c = 0; c < 4; c++) {
						verts[(q * 4 + c) * 3] = q;
						verts[(q * 4 + c) * 3 + 1] = c & 1;
						verts[(q * 4 + c) * 3 + 2] = c >> 1;
					}
					int v = q * 4, k = q * 6;
					idx[k] = (ushort)v; idx[k + 1] = (ushort)(v + 1); idx[k + 2] = (ushort)(v + 2);
					idx[k + 3] = (ushort)(v + 2); idx[k + 4] = (ushort)(v + 1); idx[k + 5] = (ushort)(v + 3);
				}
				_vao = Gl.GenVertexArray();
				_vbo = Gl.GenBuffer();
				_ebo = Gl.GenBuffer();
				Gl.BindVertexArray(_vao);
				Gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
				Gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
				unsafe {
					fixed (float* p = verts) Gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(verts.Length * sizeof(float)), p, BufferUsageARB.StaticDraw);
					fixed (ushort* p = idx) Gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(idx.Length * sizeof(ushort)), p, BufferUsageARB.StaticDraw);
					Gl.EnableVertexAttribArray(0);
					Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), (void*)0);
				}
				Gl.BindVertexArray(0);
				Gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
				_init = true;
			} catch (Exception e) {
				_broken = true;
				Trace.TraceError("GlyphQuadBatch: the batched text path could not start, glyphs draw one by one.\n" + e);
			}
			return !_broken;
		}

		// One quad as the shader reads it: the NDC box of the quad t2DDraw(x, y, rect) draws at the scale (sx, sy)
		// on a screen of screenW x screenH (GameWindowSize), t2DDraw's texture rect on a texture of texW x texH
		// (its full-image size), the tint and the texture slot. The box repeats t2DDraw's float steps for an
		// upright quad: its matrix's scale (with the aspect round trip of the rotation) and offsets, then the
		// corners (-1 or 1) through it.
		internal static void PackQuad(Span<float> dst, float x, float y, RectangleF src, float sx, float sy,
			int screenW, int screenH, float texW, float texH, float r, float g, float b, float a, int slot) {
			float aspect = (float)screenW / screenH;
			float kx = aspect * (1.0f / aspect);
			float m11 = src.Width / screenW * sx * kx;
			float m22 = src.Height / screenH * sy;
			float offsetX = src.Width * sx / screenW;
			float offsetY = src.Height * sy / screenH;
			float m41 = (offsetX + -1.0f) + x / screenW * 2;
			float m42 = (-offsetY + 1.0f) + -y / screenH * 2;
			dst[0] = m41 - m11; dst[1] = m42 + m22; dst[2] = m41 + m11; dst[3] = m42 - m22;
			dst[4] = src.X / texW; dst[5] = src.Y / texH; dst[6] = src.Width / texW; dst[7] = src.Height / texH;
			dst[8] = r; dst[9] = g; dst[10] = b; dst[11] = a;
			dst[12] = slot; dst[13] = 0; dst[14] = 0; dst[15] = 0;
		}

		// queues one quad; the tint is straight RGBA in 0..1 (CTexture's colour with its opacity as alpha)
		public static void Add(CTexture tex, float x, float y, RectangleF src, float sx, float sy, float r, float g, float b, float a) {
			tex.RealizeIfDeferred();
			if (tex.Pointer == 0) return;   // a texture with no pixels draws nothing, like t2DDraw
			// the slot of this texture in the last run, else a free one there, else a new run
			int runs = _runs.Count, slot = -1;
			if (runs > 0 && _runQuads[runs - 1] < MaxRunQuads) {
				int at = (runs - 1) * Slots;
				for (int k = 0; k < _runUsed; k++) if (_runTex[at + k] == tex) { slot = k; break; }
				if (slot < 0 && _runUsed < Slots) { slot = _runUsed++; _runTex[at + slot] = tex; }
			}
			if (slot < 0) {
				_runs.Add(_spare.Count > 0 ? _spare.Pop() : new float[MaxRunQuads * FloatsPerQuad]);
				_runQuads.Add(0);
				_runTex.Add(tex);
				for (int k = 1; k < Slots; k++) _runTex.Add(null);
				_runUsed = 1;
				slot = 0;
				runs++;
			}
			int n = _runQuads[runs - 1];
			var size = tex.szImageSize;
			PackQuad(_runs[runs - 1].AsSpan(n * FloatsPerQuad, FloatsPerQuad), x, y, src, sx, sy,
				GameWindowSize.Width, GameWindowSize.Height, size.Width, size.Height, r, g, b, a, slot);
			_runQuads[runs - 1] = n + 1;
		}

		// draws every queued quad, one draw call per run. Like t2DDraw it leaves its program, vertex array and
		// textures bound (units 0 to Slots - 1, unit 0 active): every draw binds what it uses.
		public static void Flush() {
			if (_runs.Count == 0) return;
			try {
				BlendHelper.SetPremultipliedBlend(BlendType.Normal);
				Gl.UseProgram(_prog);
				var camera = Game.Camera;
				unsafe { Gl.UniformMatrix4(_camLoc, 1, false, (float*)&camera); }
				Gl.BindVertexArray(_vao);
				Gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
				for (int i = 0; i < _runs.Count; i++) {
					int n = _runQuads[i];
					unsafe { fixed (float* p = _runs[i]) Gl.Uniform4(_quadsLoc, (uint)(n * 4), p); }
					// an unused slot gets the run's first texture: its weight is 0, any texture will do
					for (int k = Slots - 1; k >= 0; k--) {
						Gl.ActiveTexture(TextureUnit.Texture0 + k);
						Gl.BindTexture(TextureTarget.Texture2D, (_runTex[i * Slots + k] ?? _runTex[i * Slots])!.Pointer);
					}
					unsafe { Gl.DrawElements(PrimitiveType.Triangles, (uint)(n * 6), DrawElementsType.UnsignedShort, (void*)0); }
				}
			} finally {
				BlendHelper.SetBlend(BlendType.Normal);
				foreach (var run in _runs) _spare.Push(run);
				_runs.Clear();
				_runQuads.Clear();
				_runTex.Clear();
				_runUsed = 0;
			}
		}
	}
}
