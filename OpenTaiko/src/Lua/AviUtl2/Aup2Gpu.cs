using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using FDK;
using Silk.NET.OpenGLES;

namespace OpenTaiko.AviUtl2 {
	// An offscreen picture: premultiplied RGBA, row 0 = the picture's top (texture v = 0 is the top).
	internal sealed class Aup2Surface {
		public uint Fbo, Tex, Depth;
		public int W, H;
		public bool HasDepth => Depth != 0;
		public long Bytes => (long)W * H * 4;
		internal bool Rented;
		internal long LastUse;
	}

	// Values a pass hands to its shader, by uniform name.
	internal sealed class Aup2Uniforms {
		internal readonly List<(string name, int kind, Vector4 v, uint tex)> Items = new();
		public Aup2Uniforms F(string name, double x) { Items.Add((name, 1, new Vector4((float)x, 0, 0, 0), 0)); return this; }
		public Aup2Uniforms V2(string name, double x, double y) { Items.Add((name, 2, new Vector4((float)x, (float)y, 0, 0), 0)); return this; }
		public Aup2Uniforms V3(string name, double x, double y, double z) { Items.Add((name, 3, new Vector4((float)x, (float)y, (float)z, 0), 0)); return this; }
		public Aup2Uniforms V3(string name, Vector3 v) => V3(name, v.X, v.Y, v.Z);
		public Aup2Uniforms V4(string name, double x, double y, double z, double w) { Items.Add((name, 4, new Vector4((float)x, (float)y, (float)z, (float)w), 0)); return this; }
		public Aup2Uniforms I(string name, int x) { Items.Add((name, 5, new Vector4(x, 0, 0, 0), 0)); return this; }
		// a second picture for the shader (declare "uniform sampler2D name;")
		public Aup2Uniforms Tex(string name, Aup2Surface s) { Items.Add((name, 6, default, s.Tex)); return this; }
		public Aup2Uniforms Tex(string name, uint glTexture) { Items.Add((name, 6, default, glTexture)); return this; }
	}

	// The GL side of the player: pooled offscreen surfaces, cached shader programs, full-surface passes
	// and the draw of a picture into another with a matrix and a blend mode. Render thread only.
	internal sealed class Aup2Gpu : IDisposable {
		private static GL Gl => Game.Gl;

		// what every pass's fragment shader starts with; a pass writes "void main(){ ... o = ...; }"
		public const string Prelude = @"#version 300 es
precision highp float;
in vec2 v_uv;
out vec4 o;
uniform sampler2D u_src;
uniform vec2 u_srcSize;
uniform vec2 u_size;
";
		private const string VsPass = @"#version 300 es
layout(location = 0) in vec2 a;
out vec2 v_uv;
void main(){ v_uv = a; gl_Position = vec4(a * 2.0 - 1.0, 0.0, 1.0); }";
		private const string VsQuad = @"#version 300 es
layout(location = 0) in vec2 a;
uniform mat4 u_m;
uniform vec4 u_uv;
out vec2 v_uv;
out vec2 v_q;
void main(){ v_q = a; v_uv = u_uv.xy + a * u_uv.zw; gl_Position = u_m * vec4(a, 0.0, 1.0); }";
		// u_mode: 0 normal .. the AviUtl blend modes; the simple ones blend in fixed function, the rest read the backdrop
		private const string FsQuad = @"#version 300 es
precision highp float;
in vec2 v_uv;
in vec2 v_q;
out vec4 o;
uniform sampler2D u_src;
uniform sampler2D u_back;
uniform vec2 u_backSize;
uniform float u_opacity;
uniform int u_straight;
uniform int u_mode;
uniform int u_cut;
uniform vec3 u_tint;
vec3 lum3(vec3 c){ return vec3(dot(c, vec3(0.299, 0.587, 0.114))); }
// a texture whose colours are not premultiplied: the four texels around are weighed by their own
// opacity, so a see-through texel's colour does not leak into the edge
vec4 straightTexel(ivec2 at, ivec2 last){
    vec4 t = texelFetch(u_src, clamp(at, ivec2(0), last), 0);
    return vec4(t.rgb * t.a, t.a);
}
void main(){
    vec4 s;
    if (u_straight == 1) {
        ivec2 size = textureSize(u_src, 0);
        vec2 p = v_uv * vec2(size) - 0.5;
        vec2 i = floor(p), f = p - i;
        ivec2 at = ivec2(i), last = size - 1;
        s = mix(mix(straightTexel(at, last), straightTexel(at + ivec2(1, 0), last), f.x),
                mix(straightTexel(at + ivec2(0, 1), last), straightTexel(at + ivec2(1, 1), last), f.x), f.y);
    } else {
        s = texture(u_src, v_uv);
    }
    s.rgb *= u_tint;
    s *= u_opacity;
    // under a depth test a see-through texel must not hide what is behind it
    if (u_cut == 1 && s.a < 0.004) discard;
    if (u_mode < 10) { o = s; return; }
    vec4 d = texture(u_back, gl_FragCoord.xy / u_backSize);
    vec3 sc = s.a > 0.0 ? s.rgb / s.a : vec3(0.0);
    vec3 dc = d.a > 0.0 ? d.rgb / d.a : vec3(0.0);
    vec3 r = sc;
    if (u_mode == 10) r = mix(2.0 * sc * dc, 1.0 - 2.0 * (1.0 - sc) * (1.0 - dc), step(0.5, dc));      // overlay
    else if (u_mode == 11) r = max(sc, dc);                                                             // lighten
    else if (u_mode == 12) r = min(sc, dc);                                                             // darken
    else if (u_mode == 13) r = dc + (lum3(sc) - lum3(dc));                                              // luminance
    else if (u_mode == 14) r = sc + (lum3(dc) - lum3(sc));                                              // colour difference
    else if (u_mode == 15) r = dc * (0.5 + sc);                                                         // shade
    else if (u_mode == 16) r = mix(2.0 * sc * dc, 1.0 - 2.0 * (1.0 - sc) * (1.0 - dc), step(0.5, sc));  // light and dark
    else if (u_mode == 17) r = abs(dc - sc);                                                            // difference
    r = clamp(r, 0.0, 1.0);
    // over the backdrop, the blended colour where both are present
    vec3 mixed = mix(sc, r, d.a);
    float a = s.a + d.a * (1.0 - s.a);
    o = vec4(mixed * s.a + d.rgb * (1.0 - s.a), a);
}";

		private const string VsPoly = @"#version 300 es
layout(location = 0) in vec4 a;
layout(location = 1) in vec2 t;
out vec2 v_uv;
out vec2 v_q;
void main(){ v_q = t; v_uv = t; gl_Position = a; }";

		private const string VsMesh = @"#version 300 es
layout(location = 0) in vec3 a;
layout(location = 1) in vec3 n;
layout(location = 2) in vec3 k;
uniform mat4 u_model;
uniform mat4 u_vp;
out vec3 v_at;
out vec3 v_n;
out vec3 v_k;
void main(){ vec4 w = u_model * vec4(a, 1.0); v_at = w.xyz; v_n = mat3(u_model) * n; v_k = k; gl_Position = u_vp * w; }";
		// u_la: place or direction, kind; u_lb: colour, falloff; u_lc: where a spot points, half its angle; u_ls: its soft edge
		private const string FsMesh = @"#version 300 es
precision highp float;
in vec3 v_at;
in vec3 v_n;
in vec3 v_k;
out vec4 o;
uniform float u_opacity;
uniform vec3 u_eye;
uniform int u_lights;
uniform vec4 u_la[10];
uniform vec4 u_lb[10];
uniform vec4 u_lc[10];
uniform float u_ls[10];
void main(){
    vec3 n = normalize(v_n);
    vec3 lit = vec3(0.0);
    // with no light the colour shows as it is
    if (u_lights == 0) lit = vec3(1.0);
    for (int i = 0; i < 10; i++) {
        if (i >= u_lights) break;
        int kind = int(u_la[i].w + 0.5);
        float s = 1.0;
        if (kind != 0) {
            vec3 from = u_la[i].xyz;
            if (kind != 3) {
                vec3 d = u_la[i].xyz - v_at;
                float dist = length(d);
                from = d / max(dist, 0.0001);
                float r = dist / 1024.0;
                s /= 1.0 + u_lb[i].w * r * r;
                if (kind == 2) {
                    float ang = degrees(acos(clamp(dot(normalize(u_lc[i].xyz), -from), -1.0, 1.0)));
                    s *= clamp((u_lc[i].w - ang) / u_ls[i], 0.0, 1.0);
                }
            }
            s *= abs(dot(n, from));
        }
        lit += u_lb[i].rgb * s;
    }
    o = vec4(v_k * min(lit, vec3(4.0)), 1.0) * u_opacity;
}";

		private uint _vao, _vbo, _progQuad, _progPoly, _polyVao, _polyVbo, _progMesh;
		private readonly List<Aup2Mesh> _meshes = new();
		private readonly Dictionary<string, uint> _programs = new();
		private readonly List<Aup2Surface> _pool = new();
		private long _pooledBytes, _tick;
		private bool _init, _broken;
		public string? Error { get; private set; }

		// free surfaces kept for reuse: beyond this, the least recently used are deleted
		public long PoolBudgetBytes = 192L * 1024 * 1024;
		public const int MaxSide = 4096;

		public bool Ready => _init && !_broken;

		public bool Ensure() {
			if (_init || _broken) return !_broken;
			try {
				_vao = Gl.GenVertexArray();
				_vbo = Gl.GenBuffer();
				Gl.BindVertexArray(_vao);
				Gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
				ReadOnlySpan<float> quad = stackalloc float[] { 0, 0, 1, 0, 0, 1, 1, 1 };
				unsafe {
					fixed (float* p = quad) Gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(quad.Length * sizeof(float)), p, BufferUsageARB.StaticDraw);
					Gl.EnableVertexAttribArray(0);
					Gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), (void*)0);
				}
				Gl.BindVertexArray(0);
				Gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
				_progQuad = ShaderHelper.CreateShaderProgramFromSource(VsQuad, FsQuad);
				_progPoly = ShaderHelper.CreateShaderProgramFromSource(VsPoly, FsQuad);
				_polyVao = Gl.GenVertexArray();
				_polyVbo = Gl.GenBuffer();
				Gl.BindVertexArray(_polyVao);
				Gl.BindBuffer(BufferTargetARB.ArrayBuffer, _polyVbo);
				unsafe {
					Gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(4 * 6 * sizeof(float)), (void*)0, BufferUsageARB.DynamicDraw);
					Gl.EnableVertexAttribArray(0);
					Gl.VertexAttribPointer(0, 4, VertexAttribPointerType.Float, false, 6 * sizeof(float), (void*)0);
					Gl.EnableVertexAttribArray(1);
					Gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 6 * sizeof(float), (void*)(4 * sizeof(float)));
				}
				Gl.BindVertexArray(0);
				Gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
				_init = true;
			} catch (Exception e) {
				_broken = true;
				Error = e.Message;
				Trace.TraceError("AviUtl2 player: the renderer could not start.\n" + e);
			}
			return !_broken;
		}

		#region Frame state
		private readonly int[] _vp = new int[4];
		private uint _prevFbo;
		private bool _prevScissor;

		// between Begin and End the player owns the GL state; End puts back what the 2D pipeline expects
		public void Begin() {
			_tick++;
			Gl.GetInteger(GLEnum.Viewport, _vp);
			_prevFbo = (uint)Gl.GetInteger(GLEnum.FramebufferBinding);
			_prevScissor = Gl.IsEnabled(EnableCap.ScissorTest);
			Gl.Disable(EnableCap.ScissorTest);
			Gl.Disable(EnableCap.DepthTest);
			Gl.Disable(EnableCap.CullFace);
			Gl.BindVertexArray(_vao);
		}

		public void End() {
			Gl.BindFramebuffer(FramebufferTarget.Framebuffer, _prevFbo);
			Gl.Viewport(_vp[0], _vp[1], (uint)_vp[2], (uint)_vp[3]);
			if (_prevScissor) Gl.Enable(EnableCap.ScissorTest);
			Gl.UseProgram(0);
			Gl.BindVertexArray(0);
			Gl.ActiveTexture(TextureUnit.Texture1);
			Gl.BindTexture(TextureTarget.Texture2D, 0);
			Gl.ActiveTexture(TextureUnit.Texture0);
			Gl.BindTexture(TextureTarget.Texture2D, 0);
			Gl.Disable(EnableCap.DepthTest);
			Gl.DepthMask(true);
			Gl.Disable(EnableCap.CullFace);
			Gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
			Gl.Enable(EnableCap.Blend);
			BlendHelper.SetBlend(BlendType.Normal);
			Trim();
		}

		// The game's own drawing lands in a new surface w x h while `draw` runs: its coordinates count
		// from the surface's top left, one pixel each. Called outside Begin and End: what the game had
		// bound is put back, also when `draw` fails (the surface is then given back and the error goes on).
		public Aup2Surface CallerDraws(int w, int h, Action draw) {
			Span<int> vp = stackalloc int[4];
			Gl.GetInteger(GLEnum.Viewport, vp);
			uint fbo = (uint)Gl.GetInteger(GLEnum.FramebufferBinding);
			uint tex = (uint)Gl.GetInteger(GLEnum.TextureBinding2D);
			bool scissor = Gl.IsEnabled(EnableCap.ScissorTest);
			var target = Rent(w, h);
			Gl.BindFramebuffer(FramebufferTarget.Framebuffer, target.Fbo);
			Gl.Disable(EnableCap.ScissorTest);
			Gl.Viewport(0, 0, (uint)target.W, (uint)target.H);
			Gl.ClearColor(0, 0, 0, 0);
			Gl.Clear(ClearBufferMask.ColorBufferBit);
			Gl.Viewport(0, target.H - GameWindowSize.Height, (uint)GameWindowSize.Width, (uint)GameWindowSize.Height);
			try {
				draw();
				return target;
			} catch {
				Release(target);
				throw;
			} finally {
				Gl.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);
				Gl.BindTexture(TextureTarget.Texture2D, tex);
				Gl.Viewport(vp[0], vp[1], (uint)vp[2], (uint)vp[3]);
				if (scissor) Gl.Enable(EnableCap.ScissorTest);
			}
		}

		// back to the caller's target in the middle of a frame (to present)
		public void BindCallerTarget() {
			Gl.BindFramebuffer(FramebufferTarget.Framebuffer, _prevFbo);
			Gl.Viewport(_vp[0], _vp[1], (uint)_vp[2], (uint)_vp[3]);
			if (_prevScissor) Gl.Enable(EnableCap.ScissorTest);
		}
		public void LeaveCallerTarget() => Gl.Disable(EnableCap.ScissorTest);
		#endregion

		#region Surfaces
		// depth: the surface also keeps how far each pixel is, for objects that hide one another
		public Aup2Surface Rent(int w, int h, bool depth = false) {
			w = Math.Clamp(w, 1, MaxSide);
			h = Math.Clamp(h, 1, MaxSide);
			for (int i = 0; i < _pool.Count; i++) {
				var p = _pool[i];
				if (p.Rented || p.W != w || p.H != h || p.HasDepth != depth) continue;
				p.Rented = true;
				p.LastUse = _tick;
				_pooledBytes -= p.Bytes;
				return p;
			}
			var s = new Aup2Surface { W = w, H = h, Rented = true, LastUse = _tick };
			s.Tex = Gl.GenTexture();
			Gl.BindTexture(TextureTarget.Texture2D, s.Tex);
			unsafe { Gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba8, (uint)w, (uint)h, 0, PixelFormat.Rgba, PixelType.UnsignedByte, (void*)0); }
			Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
			Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
			Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
			Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);
			s.Fbo = Gl.GenFramebuffer();
			Gl.BindFramebuffer(FramebufferTarget.Framebuffer, s.Fbo);
			Gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, s.Tex, 0);
			if (depth) {
				s.Depth = Gl.GenRenderbuffer();
				Gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, s.Depth);
				Gl.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.DepthComponent24, (uint)w, (uint)h);
				Gl.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, s.Depth);
				Gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
			}
			_pool.Add(s);
			return s;
		}

		public void ClearDepth(Aup2Surface s) {
			if (!s.HasDepth) return;
			Gl.BindFramebuffer(FramebufferTarget.Framebuffer, s.Fbo);
			Gl.DepthMask(true);
			Gl.ClearDepth(1f);
			Gl.Clear(ClearBufferMask.DepthBufferBit);
		}

		// the surface's pixels as RGBA bytes, top row first, colours not premultiplied
		public byte[] Read(Aup2Surface s) {
			var data = new byte[s.W * s.H * 4];
			Gl.BindFramebuffer(FramebufferTarget.Framebuffer, s.Fbo);
			unsafe { fixed (byte* p = data) Gl.ReadPixels(0, 0, (uint)s.W, (uint)s.H, PixelFormat.Rgba, PixelType.UnsignedByte, p); }
			for (int i = 0; i < data.Length; i += 4) {
				int a = data[i + 3];
				if (a == 0 || a == 255) continue;
				data[i] = (byte)Math.Min(255, data[i] * 255 / a);
				data[i + 1] = (byte)Math.Min(255, data[i + 1] * 255 / a);
				data[i + 2] = (byte)Math.Min(255, data[i + 2] * 255 / a);
			}
			return data;
		}

		// straight RGBA bytes, top row first, into the surface (same size)
		public void Write(Aup2Surface s, byte[] rgba) {
			if (rgba.Length < s.W * s.H * 4) return;
			var data = new byte[s.W * s.H * 4];
			for (int i = 0; i < data.Length; i += 4) {
				int a = rgba[i + 3];
				data[i] = (byte)(rgba[i] * a / 255);
				data[i + 1] = (byte)(rgba[i + 1] * a / 255);
				data[i + 2] = (byte)(rgba[i + 2] * a / 255);
				data[i + 3] = (byte)a;
			}
			Gl.BindTexture(TextureTarget.Texture2D, s.Tex);
			unsafe { fixed (byte* p = data) Gl.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, (uint)s.W, (uint)s.H, PixelFormat.Rgba, PixelType.UnsignedByte, p); }
		}

		public void Release(Aup2Surface? s) {
			if (s == null || !s.Rented) return;
			s.Rented = false;
			s.LastUse = _tick;
			_pooledBytes += s.Bytes;
		}

		// deletes free surfaces, oldest first, until the free ones fit the budget
		private void Trim() {
			while (_pooledBytes > PoolBudgetBytes) {
				int oldest = -1;
				for (int i = 0; i < _pool.Count; i++)
					if (!_pool[i].Rented && (oldest < 0 || _pool[i].LastUse < _pool[oldest].LastUse)) oldest = i;
				if (oldest < 0) break;
				Delete(_pool[oldest]);
				_pool.RemoveAt(oldest);
			}
			// and anything not used for a while
			for (int i = _pool.Count - 1; i >= 0; i--) {
				if (_pool[i].Rented || _tick - _pool[i].LastUse < 600) continue;
				Delete(_pool[i]);
				_pool.RemoveAt(i);
			}
		}

		private void Delete(Aup2Surface s) {
			if (!s.Rented) _pooledBytes -= s.Bytes;
			Gl.DeleteFramebuffer(s.Fbo);
			Gl.DeleteTexture(s.Tex);
			if (s.Depth != 0) Gl.DeleteRenderbuffer(s.Depth);
			s.Fbo = s.Tex = s.Depth = 0;
		}

		public long SurfaceBytes {
			get { long n = 0; foreach (var s in _pool) n += s.Bytes; return n; }
		}
		public int SurfaceCount => _pool.Count;

		public void Clear(Aup2Surface s, float r = 0, float g = 0, float b = 0, float a = 0) {
			Gl.BindFramebuffer(FramebufferTarget.Framebuffer, s.Fbo);
			Gl.Viewport(0, 0, (uint)s.W, (uint)s.H);
			Gl.ClearColor(r, g, b, a);
			Gl.Clear(ClearBufferMask.ColorBufferBit);
		}

		// the nearest-neighbour or smooth reading of a surface (smooth is the default)
		public void SetSmooth(Aup2Surface s, bool smooth) {
			Gl.BindTexture(TextureTarget.Texture2D, s.Tex);
			int f = (int)(smooth ? GLEnum.Linear : GLEnum.Nearest);
			Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, f);
			Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, f);
		}

		public void SetRepeat(Aup2Surface s, bool repeat) {
			Gl.BindTexture(TextureTarget.Texture2D, s.Tex);
			int f = (int)(repeat ? GLEnum.Repeat : GLEnum.ClampToEdge);
			Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, f);
			Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, f);
		}
		#endregion

		#region Passes
		private uint Program(string fragment) {
			if (_programs.TryGetValue(fragment, out uint p)) return p;
			try {
				p = ShaderHelper.CreateShaderProgramFromSource(VsPass, Prelude + fragment);
			} catch (Exception e) {
				p = 0;
				Trace.TraceError("AviUtl2 player: a shader did not compile.\n" + e.Message + "\n" + fragment);
			}
			_programs[fragment] = p;
			return p;
		}

		private void Apply(uint program, Aup2Uniforms? u) {
			if (u == null) return;
			int unit = 2;
			foreach (var (name, kind, v, tex) in u.Items) {
				int loc = Gl.GetUniformLocation(program, name);
				if (loc < 0) continue;
				switch (kind) {
					case 1: Gl.Uniform1(loc, v.X); break;
					case 2: Gl.Uniform2(loc, v.X, v.Y); break;
					case 3: Gl.Uniform3(loc, v.X, v.Y, v.Z); break;
					case 4: Gl.Uniform4(loc, v.X, v.Y, v.Z, v.W); break;
					case 5: Gl.Uniform1(loc, (int)v.X); break;
					case 6:
						Gl.ActiveTexture(TextureUnit.Texture0 + unit);
						Gl.BindTexture(TextureTarget.Texture2D, tex);
						Gl.Uniform1(loc, unit);
						unit++;
						break;
				}
			}
			Gl.ActiveTexture(TextureUnit.Texture0);
		}

		// Runs `fragment` over the whole of `target`, reading `srcTex` (srcW x srcH) as u_src. v_uv is 0..1
		// over the target with (0,0) the top left. Returns false when the shader is broken (the target is
		// then a plain copy of the source, so a bad filter never blanks the picture).
		public bool Pass(Aup2Surface target, string fragment, uint srcTex, int srcW, int srcH, Aup2Uniforms? uniforms = null) {
			uint p = Program(fragment);
			bool ok = p != 0;
			if (!ok) p = Program("void main(){ o = texture(u_src, v_uv); }");
			Gl.BindFramebuffer(FramebufferTarget.Framebuffer, target.Fbo);
			Gl.Viewport(0, 0, (uint)target.W, (uint)target.H);
			Gl.Disable(EnableCap.Blend);
			Gl.UseProgram(p);
			Gl.ActiveTexture(TextureUnit.Texture0);
			Gl.BindTexture(TextureTarget.Texture2D, srcTex);
			Gl.Uniform1(Gl.GetUniformLocation(p, "u_src"), 0);
			Gl.Uniform2(Gl.GetUniformLocation(p, "u_srcSize"), (float)srcW, (float)srcH);
			Gl.Uniform2(Gl.GetUniformLocation(p, "u_size"), (float)target.W, (float)target.H);
			if (ok) Apply(p, uniforms);
			Gl.BindVertexArray(_vao);
			Gl.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
			return ok;
		}

		public bool Pass(Aup2Surface target, string fragment, Aup2Surface src, Aup2Uniforms? uniforms = null)
			=> Pass(target, fragment, src.Tex, src.W, src.H, uniforms);
		#endregion

		#region Drawing a picture into another
		// Draws a texture as the unit square carried by `matrix` (to clip space) into `target`.
		// uv = (x, y, w, h) of the part of the texture, in 0..1. straight: the texture is not premultiplied.
		public void Quad(Aup2Surface target, uint tex, Matrix4x4 matrix, Vector4 uv, double opacity, Aup2Blend blend, bool straight,
			Aup2Surface? backdrop = null, Vector3? tint = null, bool depth = false) {
			Gl.BindFramebuffer(FramebufferTarget.Framebuffer, target.Fbo);
			Gl.Viewport(0, 0, (uint)target.W, (uint)target.H);
			Depth(depth && target.HasDepth);
			DrawQuad(_progQuad, tex, opacity, blend, straight, backdrop, target.W, target.H, tint ?? Vector3.One, premultipliedTarget: true, cut: depth && target.HasDepth);
			unsafe { Gl.UniformMatrix4(Gl.GetUniformLocation(_progQuad, "u_m"), 1, false, (float*)&matrix); }
			Gl.Uniform4(Gl.GetUniformLocation(_progQuad, "u_uv"), uv.X, uv.Y, uv.Z, uv.W);
			Gl.BindVertexArray(_vao);
			Gl.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
			Finish();
		}

		// A triangle or a four-cornered face: `clip` are its corners in clip space, in order around it,
		// `uv` the matching places in the texture (0..1). cull: a face seen from behind is not drawn.
		public void Poly(Aup2Surface target, uint tex, ReadOnlySpan<Vector4> clip, ReadOnlySpan<Vector2> uv, double opacity, Aup2Blend blend,
			bool straight, Aup2Surface? backdrop = null, bool depth = false, bool cull = false) {
			int n = Math.Min(clip.Length, 4);
			if (n < 3 || uv.Length < n) return;
			Gl.BindFramebuffer(FramebufferTarget.Framebuffer, target.Fbo);
			Gl.Viewport(0, 0, (uint)target.W, (uint)target.H);
			Depth(depth && target.HasDepth);
			// clockwise on the picture (y down) is counter-clockwise in clip space
			if (cull) { Gl.Enable(EnableCap.CullFace); Gl.CullFace(TriangleFace.Back); Gl.FrontFace(FrontFaceDirection.Ccw); }
			DrawQuad(_progPoly, tex, opacity, blend, straight, backdrop, target.W, target.H, Vector3.One, premultipliedTarget: true, cut: depth && target.HasDepth);
			Span<float> v = stackalloc float[24];
			// as a fan: 0 1 2 3
			for (int i = 0; i < n; i++) {
				v[i * 6] = clip[i].X; v[i * 6 + 1] = clip[i].Y; v[i * 6 + 2] = clip[i].Z; v[i * 6 + 3] = clip[i].W;
				v[i * 6 + 4] = uv[i].X; v[i * 6 + 5] = uv[i].Y;
			}
			Gl.BindVertexArray(_polyVao);
			Gl.BindBuffer(BufferTargetARB.ArrayBuffer, _polyVbo);
			unsafe { fixed (float* p = v) Gl.BufferSubData(BufferTargetARB.ArrayBuffer, 0, (nuint)(n * 6 * sizeof(float)), p); }
			Gl.DrawArrays(PrimitiveType.TriangleFan, 0, (uint)n);
			Gl.Disable(EnableCap.CullFace);
			Finish();
		}

		// A model's triangles through `model` (to the scene's space) and `viewProj` (to clip space),
		// with depth, lit by the camera's lights.
		public void Mesh(Aup2Surface target, Aup2Mesh mesh, Matrix4x4 model, Matrix4x4 viewProj, Vector3 eye, double opacity, IReadOnlyList<Aup2Light> lights) {
			if (mesh.Corners == 0) return;
			if (_progMesh == 0) _progMesh = ShaderHelper.CreateShaderProgramFromSource(VsMesh, FsMesh);
			if (mesh.Vao == 0) {
				mesh.Vao = Gl.GenVertexArray();
				mesh.Vbo = Gl.GenBuffer();
				Gl.BindVertexArray(mesh.Vao);
				Gl.BindBuffer(BufferTargetARB.ArrayBuffer, mesh.Vbo);
				unsafe {
					fixed (float* p = mesh.Data) Gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(mesh.Data.Length * sizeof(float)), p, BufferUsageARB.StaticDraw);
					for (uint i = 0; i < 3; i++) {
						Gl.EnableVertexAttribArray(i);
						Gl.VertexAttribPointer(i, 3, VertexAttribPointerType.Float, false, 9 * sizeof(float), (void*)(i * 3 * sizeof(float)));
					}
				}
				_meshes.Add(mesh);
			}
			Gl.BindFramebuffer(FramebufferTarget.Framebuffer, target.Fbo);
			Gl.Viewport(0, 0, (uint)target.W, (uint)target.H);
			Depth(target.HasDepth);
			Gl.Disable(EnableCap.CullFace);
			Gl.Enable(EnableCap.Blend);
			Gl.BlendEquation(BlendEquationModeEXT.FuncAdd);
			Gl.BlendFunc(GLEnum.One, GLEnum.OneMinusSrcAlpha);
			Gl.UseProgram(_progMesh);
			unsafe {
				Gl.UniformMatrix4(Gl.GetUniformLocation(_progMesh, "u_model"), 1, false, (float*)&model);
				Gl.UniformMatrix4(Gl.GetUniformLocation(_progMesh, "u_vp"), 1, false, (float*)&viewProj);
			}
			Gl.Uniform1(Gl.GetUniformLocation(_progMesh, "u_opacity"), (float)Math.Clamp(opacity, 0, 1));
			Gl.Uniform3(Gl.GetUniformLocation(_progMesh, "u_eye"), eye.X, eye.Y, eye.Z);
			int count = Math.Min(10, lights.Count);
			Gl.Uniform1(Gl.GetUniformLocation(_progMesh, "u_lights"), count);
			if (count > 0) {
				Span<float> la = stackalloc float[40], lb = stackalloc float[40], lc = stackalloc float[40], ls = stackalloc float[10];
				for (int i = 0; i < count; i++) {
					var l = lights[i];
					var at = l.Kind == Aup2LightKind.Parallel ? l.Direction : l.Position;
					la[i * 4] = at.X; la[i * 4 + 1] = at.Y; la[i * 4 + 2] = at.Z; la[i * 4 + 3] = (int)l.Kind;
					lb[i * 4] = l.Color.X * (float)l.Brightness; lb[i * 4 + 1] = l.Color.Y * (float)l.Brightness; lb[i * 4 + 2] = l.Color.Z * (float)l.Brightness;
					lb[i * 4 + 3] = (float)(l.Falloff / 100);
					var axis = l.Target - l.Position;
					if (axis.LengthSquared() < 1e-6f) axis = new Vector3(0, 0, 1);
					double half = Math.Max(0.1, l.Range / 2);
					lc[i * 4] = axis.X; lc[i * 4 + 1] = axis.Y; lc[i * 4 + 2] = axis.Z; lc[i * 4 + 3] = (float)half;
					ls[i] = (float)Math.Max(0.01, half * Math.Min(1, l.Soft / 100));
				}
				unsafe {
					fixed (float* p = la) Gl.Uniform4(Gl.GetUniformLocation(_progMesh, "u_la"), 10, p);
					fixed (float* p = lb) Gl.Uniform4(Gl.GetUniformLocation(_progMesh, "u_lb"), 10, p);
					fixed (float* p = lc) Gl.Uniform4(Gl.GetUniformLocation(_progMesh, "u_lc"), 10, p);
					fixed (float* p = ls) Gl.Uniform1(Gl.GetUniformLocation(_progMesh, "u_ls"), 10, p);
				}
			}
			Gl.BindVertexArray(mesh.Vao);
			if (opacity < 1 && target.HasDepth) {
				// see-through: every face shows through the others, front or back; the depths go down after
				Gl.DepthMask(false);
				Gl.DrawArrays(PrimitiveType.Triangles, 0, (uint)mesh.Corners);
				Gl.DepthMask(true);
				Gl.ColorMask(false, false, false, false);
				Gl.DrawArrays(PrimitiveType.Triangles, 0, (uint)mesh.Corners);
				Gl.ColorMask(true, true, true, true);
			} else {
				Gl.DrawArrays(PrimitiveType.Triangles, 0, (uint)mesh.Corners);
			}
			Finish();
		}

		private void Depth(bool on) {
			if (on) {
				Gl.Enable(EnableCap.DepthTest);
				Gl.DepthFunc(DepthFunction.Lequal);
				Gl.DepthMask(true);
			} else {
				Gl.Disable(EnableCap.DepthTest);
			}
		}

		private void Finish() {
			Gl.Disable(EnableCap.DepthTest);
			Gl.BlendEquation(BlendEquationModeEXT.FuncAdd);
		}

		// the same onto whatever is bound (the caller's target), which holds straight alpha colours
		public void QuadToCaller(uint tex, Matrix4x4 matrix, Vector4 uv, double opacity) {
			Gl.Disable(EnableCap.DepthTest);
			DrawQuad(_progQuad, tex, opacity, Aup2Blend.Normal, false, null, 1, 1, Vector3.One, premultipliedTarget: false, cut: false);
			unsafe { Gl.UniformMatrix4(Gl.GetUniformLocation(_progQuad, "u_m"), 1, false, (float*)&matrix); }
			Gl.Uniform4(Gl.GetUniformLocation(_progQuad, "u_uv"), uv.X, uv.Y, uv.Z, uv.W);
			Gl.BindVertexArray(_vao);
			Gl.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
			Finish();
		}

		// the program, the texture and the blending shared by the square and the free-form face
		private void DrawQuad(uint program, uint tex, double opacity, Aup2Blend blend, bool straight, Aup2Surface? backdrop,
			int tw, int th, Vector3 tint, bool premultipliedTarget, bool cut) {
			Gl.UseProgram(program);
			Gl.ActiveTexture(TextureUnit.Texture0);
			Gl.BindTexture(TextureTarget.Texture2D, tex);
			Gl.Uniform1(Gl.GetUniformLocation(program, "u_src"), 0);
			Gl.Uniform1(Gl.GetUniformLocation(program, "u_opacity"), (float)Math.Clamp(opacity, 0, 1));
			Gl.Uniform1(Gl.GetUniformLocation(program, "u_straight"), straight ? 1 : 0);
			Gl.Uniform1(Gl.GetUniformLocation(program, "u_cut"), cut ? 1 : 0);
			Gl.Uniform3(Gl.GetUniformLocation(program, "u_tint"), tint.X, tint.Y, tint.Z);
			int mode = (int)blend;
			bool reads = mode >= 10 && backdrop != null;
			Gl.Uniform1(Gl.GetUniformLocation(program, "u_mode"), reads ? mode : 0);
			if (reads) {
				Gl.ActiveTexture(TextureUnit.Texture1);
				Gl.BindTexture(TextureTarget.Texture2D, backdrop!.Tex);
				Gl.Uniform1(Gl.GetUniformLocation(program, "u_back"), 1);
				Gl.Uniform2(Gl.GetUniformLocation(program, "u_backSize"), (float)tw, (float)th);
				Gl.ActiveTexture(TextureUnit.Texture0);
				Gl.Disable(EnableCap.Blend);
			} else {
				Gl.Enable(EnableCap.Blend);
				Gl.BlendEquation(BlendEquationModeEXT.FuncAdd);
				// premultiplied source
				switch (blend) {
					case Aup2Blend.Add: Gl.BlendFuncSeparate(GLEnum.One, GLEnum.One, GLEnum.Zero, GLEnum.One); break;
					case Aup2Blend.Subtract:
						Gl.BlendEquationSeparate(BlendEquationModeEXT.FuncReverseSubtract, BlendEquationModeEXT.FuncAdd);
						Gl.BlendFuncSeparate(GLEnum.One, GLEnum.One, GLEnum.Zero, GLEnum.One);
						break;
					case Aup2Blend.Multiply: Gl.BlendFuncSeparate(GLEnum.DstColor, GLEnum.OneMinusSrcAlpha, GLEnum.Zero, GLEnum.One); break;
					case Aup2Blend.Screen: Gl.BlendFuncSeparate(GLEnum.One, GLEnum.OneMinusSrcColor, GLEnum.One, GLEnum.OneMinusSrcAlpha); break;
					default:
						if (premultipliedTarget) Gl.BlendFunc(GLEnum.One, GLEnum.OneMinusSrcAlpha);
						else Gl.BlendFuncSeparate(GLEnum.One, GLEnum.OneMinusSrcAlpha, GLEnum.One, GLEnum.OneMinusSrcAlpha);
						break;
				}
			}
		}

		// a copy of `src` into a new surface of the same size
		public Aup2Surface Copy(Aup2Surface src) {
			var t = Rent(src.W, src.H);
			Pass(t, "void main(){ o = texture(u_src, v_uv); }", src);
			return t;
		}
		#endregion

		public void Dispose() {
			if (!_init) return;
			foreach (var s in _pool) { Gl.DeleteFramebuffer(s.Fbo); Gl.DeleteTexture(s.Tex); if (s.Depth != 0) Gl.DeleteRenderbuffer(s.Depth); }
			_pool.Clear();
			_pooledBytes = 0;
			foreach (var p in _programs.Values) if (p != 0) Gl.DeleteProgram(p);
			_programs.Clear();
			if (_progQuad != 0) Gl.DeleteProgram(_progQuad);
			if (_progPoly != 0) Gl.DeleteProgram(_progPoly);
			if (_progMesh != 0) Gl.DeleteProgram(_progMesh);
			foreach (var m in _meshes) {
				Gl.DeleteBuffer(m.Vbo);
				Gl.DeleteVertexArray(m.Vao);
				m.Vao = m.Vbo = 0;
			}
			_meshes.Clear();
			_progMesh = 0;
			if (_vbo != 0) Gl.DeleteBuffer(_vbo);
			if (_vao != 0) Gl.DeleteVertexArray(_vao);
			if (_polyVbo != 0) Gl.DeleteBuffer(_polyVbo);
			if (_polyVao != 0) Gl.DeleteVertexArray(_polyVao);
			_progQuad = _progPoly = _vbo = _vao = _polyVbo = _polyVao = 0;
			_init = false;
		}
	}
}
