using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using FDK;
using Silk.NET.OpenGLES;

namespace OpenTaiko {
	/// <summary>
	/// GPU rasterizer for <see cref="Lua3DScene"/> on the hardware graphics pipeline (GLES 3.1
	/// vertex/fragment shaders + HW triangle raster, depth, blending) — replacing the CPU
	/// <see cref="Rasterizer"/>. The per-pixel fill runs on the GPU; the CPU only assembles vertices,
	/// and that work is RETAINED so a static scene re-uploads nothing per frame.
	///
	/// Draw-call reduction is the key to matching/beating the CPU: static opaque geometry (textured, no
	/// transform, white tint, full alpha, lit) is MERGED by texture into a few big buffers, so e.g. a 500+
	/// object voxel world draws in ~one call per block texture. Everything else (transparent, transformed,
	/// tinted, flat, unlit) draws per object from its own retained VBO; billboard sprites rebuild per frame.
	///
	/// Forward lighting matches the CPU: per-vertex normal + per-fragment sqrt(shade)·(ambient +
	/// max(0,N·sun)·sun) + Σ point-lights (range or inverse-square). Cutout transparency: a negative texel
	/// (packed&lt;0) uploads as alpha 0 and is alpha-tested away. Output goes into the scene's canvas GL
	/// texture via an FBO over the Lua-drawn background; projection matches the CPU (vertically flipped to
	/// match the 2D draw). NOT yet ported: particles, screen-texture mirrors, FXAA, the overlay pass.
	/// </summary>
	internal sealed class GpuRasterizer : IRenderer {
		private GL Gl => Game.Gl;
		private const int STRIDE = 9;        // pos(3) uv(2) shade(1) normal(3)
		private const double FAR = 10000.0;
		private const int MAXL = 64;         // max point lights sent to the shader

		private bool _init;
		private uint _prog, _vao, _spriteVbo;
		private int _uCam, _uR, _uU, _uF, _uSx, _uSy, _uA, _uB, _uModel;
		private int _uMode, _uTex, _uCutout, _uTint, _uAlpha, _uFlat, _uFog, _uFogCol, _uFogStart, _uFogInv, _uRsh;
		private int _uLit, _uSunDir, _uSunCol, _uAmb, _uNumLights, _uLightPosR, _uLightCol;
		private readonly Dictionary<int, int> _glTexRev = new();   // uploaded revision per texId (for live mirror RTTs)
		private readonly List<SceneObject> _screenTex = new(8);    // ScreenTex mirror/portal surfaces
		private readonly float[] _lpos = new float[MAXL * 4], _lcol = new float[MAXL * 3];

		// 2D overlay (DrawLine/FillRect issued AFTER the 3D, e.g. crosshair/selection): composited onto
		// the canvas texture via the FBO, since the CPU buffer no longer holds the 3D in GPU mode.
		private uint _prog2d, _vbo2d, _vao2d; private int _uRes2d, _uColor2d; private bool _2dActive; private uint _prevFbo2; private readonly int[] _vp2 = new int[4];

		// particles: camera-facing billboards with per-particle RGBA, additive or alpha blend; own shader
		private uint _progP, _vaoP, _partVbo;
		private int _pCam, _pR, _pU, _pF, _pSx, _pSy, _pA, _pB, _pTextured, _pTex;
		private readonly Dictionary<(int add, int tex), List<float>> _partBatches = new();

		// FXAA-lite post pass (optional, SetAntialias): copy scene→temp, then edge-aware blend temp→canvas
		private uint _progPost, _vaoPost, _fxaaTex, _fxaaFbo; private int _postTex, _postMode, _postRes; private int _fxaaW, _fxaaH;

		private uint _fbo, _depthRb;
		private int _fboW, _fboH; private uint _fboColorTex;

		private readonly Dictionary<int, uint> _glTex = new();
		private readonly Dictionary<int, uint> _glSprite = new();

		private struct OBatch { public int Mode; public uint Tex; public int Start, Count; }
		private sealed class ObjCache { public uint Vbo; public int Version = int.MinValue; public readonly List<OBatch> Batches = new(); }
		private readonly Dictionary<SceneObject, ObjCache> _cache = new();
		private readonly List<float> _tmp = new(4096);
		private readonly List<SceneObject> _opaque = new(256), _trans = new(64), _overlay = new(16);
		private int _frame, _pruneAt;

		// Static opaque geometry is merged per (spatial bucket × texture) so a block edit only re-uploads
		// the small affected bucket(s), synchronously (no whole-world spike, and no stale-frame "x-ray").
		// Cheap separate-format binding keeps the extra draw calls (one per non-empty bucket·texture) free.
		private const double BS = 32.0;   // bucket size in world units (2×2 voxel chunks)
		private struct MergeBuf { public uint Vbo; public int Count; }
		private readonly Dictionary<(int bx, int bz, uint gt), MergeBuf> _merge = new();
		private readonly List<float> _scratch = new(8192);   // single reused concat buffer (no persistent per-key copy)
		private long _staticSig = long.MinValue;
		// per-object cached emit (verts grouped by texture) + which bucket the object is in
		private sealed class MergeEmit { public int Version = int.MinValue; public int Bx, Bz; public readonly Dictionary<uint, List<float>> ByTex = new(); }
		private readonly Dictionary<SceneObject, MergeEmit> _mergeCache = new();
		private readonly HashSet<(int bx, int bz, uint gt)> _dirtyKeys = new();
		private readonly HashSet<SceneObject> _live = new();   // reused each rebuild/prune (no per-edit alloc)

		private readonly List<float> _sprVerts = new(2048);
		private struct SprBatch { public SceneObject Obj; public int Cutout, Start, Count, Pass; public uint Tex; }
		private readonly List<SprBatch> _sprBatches = new(64);

		private static readonly float[] _ident = { 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1 };
		private readonly float[] _mtx = new float[16];

		private const string VS = @"#version 300 es
layout(location=0) in vec3 aPos;
layout(location=1) in vec2 aUV;
layout(location=2) in float aShade;
layout(location=3) in vec3 aNormal;
uniform mat4 uModel;
uniform vec3 uCam, uR, uU, uF;
uniform float uSx, uSy, uA, uB;
out vec2 vUV; out float vShade; out float vCamZ; out vec3 vWorld; out vec3 vNormal;
void main(){
    vUV = aUV; vShade = aShade;
    vec3 wp = (uModel * vec4(aPos, 1.0)).xyz;
    vWorld = wp; vNormal = mat3(uModel) * aNormal;
    vec3 r = wp - uCam;
    float cx = dot(r, uR), cy = dot(r, uU), cz = dot(r, uF);
    vCamZ = cz;
    gl_Position = vec4(cx * uSx, cy * uSy, uA * cz + uB, cz);
}";
		private const string FS = @"#version 300 es
precision highp float;
in vec2 vUV; in float vShade; in float vCamZ; in vec3 vWorld; in vec3 vNormal;
uniform int uMode;            // 0 flat, 1 textured, 2 sprite, 3 screen-tex mirror
uniform sampler2D uTex;
uniform vec3 uTint; uniform float uAlpha; uniform vec3 uFlat; uniform int uCutout;
uniform int uRsh;             // mirror/portal RTT is rendered at 1/2^uRsh res; sample by gl_FragCoord>>uRsh
uniform int uFog; uniform vec3 uFogCol; uniform float uFogStart; uniform float uFogInv;
uniform int uLit; uniform vec3 uSunDir, uSunCol, uAmb;
uniform int uNumLights; uniform vec4 uLightPosR[64]; uniform vec3 uLightCol[64];
out vec4 frag;
vec3 lightAt(vec3 N, vec3 wp){
    float e = sqrt(max(vShade, 0.0));
    vec3 l = e * (uAmb + max(dot(N, uSunDir), 0.0) * uSunCol);
    for (int i = 0; i < uNumLights; i++){
        vec3 dp = uLightPosR[i].xyz - wp; float range = uLightPosR[i].w;
        float d2 = dot(dp, dp);
        if (range > 0.0){
            if (d2 >= range*range) continue;
            float dl = sqrt(d2); float ndl = max(dot(N, dp/dl), 0.0); if (ndl <= 0.0) continue;
            float f = 1.0 - dl/range; l += uLightCol[i] * (f*f*ndl);
        } else {
            float inv = inversesqrt(max(d2, 1e-6)); float ndl = max(dot(N, dp*inv), 0.0); if (ndl <= 0.0) continue;
            l += uLightCol[i] * (ndl / (d2 + 1.0));
        }
    }
    return l;
}
void main(){
    if (uMode == 3){   // screen-texture mirror: sample the RTT by screen pixel, blend over the base colour
        ivec2 ts = textureSize(uTex, 0);
        ivec2 mp = clamp(ivec2(int(gl_FragCoord.x) >> uRsh, int(gl_FragCoord.y) >> uRsh), ivec2(0), ts - 1);
        vec3 m = texelFetch(uTex, mp, 0).rgb;
        vec3 col = m * uTint + uFlat * (1.0 - uTint);
        if (uFog == 1){ float f = clamp((vCamZ - uFogStart) * uFogInv, 0.0, 1.0); col = mix(col, uFogCol, f); }
        frag = vec4(col, uAlpha); return;
    }
    if (uMode == 2){ vec4 t = texture(uTex, vUV); vec4 c = vec4(t.rgb * uTint, t.a * uAlpha); if (uCutout == 1 && c.a < 0.5) discard;
        if (uFog == 1){ float f = clamp((vCamZ - uFogStart) * uFogInv, 0.0, 1.0); c.rgb = mix(c.rgb, uFogCol, f); } frag = c; return; }
    vec3 base;
    if (uMode == 0){ base = uFlat; }
    else { vec4 t = texture(uTex, vUV); if (t.a < 0.5) discard; base = t.rgb * uTint; }
    vec3 col = (uLit == 1) ? base * lightAt(normalize(vNormal), vWorld) : base * vShade;
    if (uFog == 1){ float f = clamp((vCamZ - uFogStart) * uFogInv, 0.0, 1.0); col = mix(col, uFogCol, f); }
    frag = vec4(col, uAlpha);
}";

		private const string VSP = @"#version 300 es
layout(location=0) in vec3 aPos;
layout(location=1) in vec2 aUV;
layout(location=2) in vec4 aColor;
uniform vec3 uCam, uR, uU, uF;
uniform float uSx, uSy, uA, uB;
out vec2 vUV; out vec4 vColor;
void main(){
    vUV = aUV; vColor = aColor;
    vec3 r = aPos - uCam;
    float cx = dot(r, uR), cy = dot(r, uU), cz = dot(r, uF);
    gl_Position = vec4(cx * uSx, cy * uSy, uA * cz + uB, cz);
}";
		private const string FSP = @"#version 300 es
precision highp float;
in vec2 vUV; in vec4 vColor;
uniform int uTextured; uniform sampler2D uTex;
out vec4 frag;
void main(){
    vec4 c = vColor;
    if (uTextured == 1) { vec4 t = texture(uTex, vUV); c = vec4(t.rgb * vColor.rgb, t.a * vColor.a); }
    frag = c;
}";
		private const string VSPOST = @"#version 300 es
const vec2 P[3] = vec2[3](vec2(-1.0,-1.0), vec2(3.0,-1.0), vec2(-1.0,3.0));
void main(){ gl_Position = vec4(P[gl_VertexID], 0.0, 1.0); }";
		private const string FSPOST = @"#version 300 es
precision highp float;
uniform sampler2D uTex; uniform int uMode; uniform vec2 uRes;
out vec4 frag;
float luma(vec3 c){ return (c.r + 2.0*c.g + c.b) * 255.0; }   // 0..1020, matches the CPU's R+2G+B bytes
void main(){
    ivec2 p = ivec2(gl_FragCoord.xy);
    vec3 c = texelFetch(uTex, p, 0).rgb;
    if (uMode == 0){ frag = vec4(c, 1.0); return; }            // plain copy
    if (p.x < 1 || p.y < 1 || p.x >= int(uRes.x)-1 || p.y >= int(uRes.y)-1){ frag = vec4(c,1.0); return; }
    vec3 cn = texelFetch(uTex, p+ivec2(0,-1),0).rgb, cs = texelFetch(uTex, p+ivec2(0,1),0).rgb;
    vec3 ce = texelFetch(uTex, p+ivec2(1,0),0).rgb,  cw = texelFetch(uTex, p+ivec2(-1,0),0).rgb;
    float lC=luma(c), lN=luma(cn), lS=luma(cs), lE=luma(ce), lW=luma(cw);
    float mn=min(lC,min(min(lN,lS),min(lE,lW))), mx=max(lC,max(max(lN,lS),max(lE,lW)));
    float range = mx-mn;
    if (range < 64.0 || range*8.0 < mx){ frag = vec4(c,1.0); return; }
    vec3 a, b;
    if (abs(lN+lS-2.0*lC) >= abs(lE+lW-2.0*lC)){ a=cn; b=cs; } else { a=ce; b=cw; }
    frag = vec4((c*2.0 + a + b) * 0.25, 1.0);
}";
		private const string VS2D = @"#version 300 es
layout(location=0) in vec2 aPx;
uniform vec2 uRes;
void main(){ gl_Position = vec4(aPx.x / uRes.x * 2.0 - 1.0, aPx.y / uRes.y * 2.0 - 1.0, 0.0, 1.0); }";
		private const string FS2D = @"#version 300 es
precision highp float;
uniform vec4 uColor;
out vec4 frag;
void main(){ frag = uColor; }";

		private void EnsureGL() {
			if (_init) return;
			_prog = ShaderHelper.CreateShaderProgramFromSource(VS, FS);
			_prog2d = ShaderHelper.CreateShaderProgramFromSource(VS2D, FS2D);
			_uRes2d = Gl.GetUniformLocation(_prog2d, "uRes"); _uColor2d = Gl.GetUniformLocation(_prog2d, "uColor");
			_vbo2d = Gl.GenBuffer();
			int U(string n) => Gl.GetUniformLocation(_prog, n);
			_uCam = U("uCam"); _uR = U("uR"); _uU = U("uU"); _uF = U("uF");
			_uSx = U("uSx"); _uSy = U("uSy"); _uA = U("uA"); _uB = U("uB"); _uModel = U("uModel");
			_uMode = U("uMode"); _uTex = U("uTex"); _uCutout = U("uCutout"); _uTint = U("uTint");
			_uAlpha = U("uAlpha"); _uFlat = U("uFlat"); _uRsh = U("uRsh");
			_uFog = U("uFog"); _uFogCol = U("uFogCol"); _uFogStart = U("uFogStart"); _uFogInv = U("uFogInv");
			_uLit = U("uLit"); _uSunDir = U("uSunDir"); _uSunCol = U("uSunCol"); _uAmb = U("uAmb");
			_uNumLights = U("uNumLights"); _uLightPosR = U("uLightPosR[0]"); _uLightCol = U("uLightCol[0]");
			// 3D VAO uses separate attribute format (GLES 3.1): the vertex layout is fixed once here, so
			// switching geometry buffers per draw is a single cheap glBindVertexBuffer (binding index 0).
			_vao = Gl.GenVertexArray();
			Gl.BindVertexArray(_vao);
			Gl.EnableVertexAttribArray(0); Gl.VertexAttribFormat(0, 3, VertexAttribType.Float, false, 0); Gl.VertexAttribBinding(0, 0);
			Gl.EnableVertexAttribArray(1); Gl.VertexAttribFormat(1, 2, VertexAttribType.Float, false, (uint)(3 * sizeof(float))); Gl.VertexAttribBinding(1, 0);
			Gl.EnableVertexAttribArray(2); Gl.VertexAttribFormat(2, 1, VertexAttribType.Float, false, (uint)(5 * sizeof(float))); Gl.VertexAttribBinding(2, 0);
			Gl.EnableVertexAttribArray(3); Gl.VertexAttribFormat(3, 3, VertexAttribType.Float, false, (uint)(6 * sizeof(float))); Gl.VertexAttribBinding(3, 0);
			Gl.BindVertexArray(0);
			_vao2d = Gl.GenVertexArray();   // 2D overlay (classic attrib pointers, set in Begin2D)
			_spriteVbo = Gl.GenBuffer();
			// particle program + its own VAO (pos3, uv2, rgba4)
			_progP = ShaderHelper.CreateShaderProgramFromSource(VSP, FSP);
			int P(string n) => Gl.GetUniformLocation(_progP, n);
			_pCam = P("uCam"); _pR = P("uR"); _pU = P("uU"); _pF = P("uF"); _pSx = P("uSx"); _pSy = P("uSy"); _pA = P("uA"); _pB = P("uB"); _pTextured = P("uTextured"); _pTex = P("uTex");
			_vaoP = Gl.GenVertexArray(); _partVbo = Gl.GenBuffer();
			Gl.BindVertexArray(_vaoP); Gl.BindBuffer(BufferTargetARB.ArrayBuffer, _partVbo);
			unsafe {
				Gl.EnableVertexAttribArray(0); Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 9 * sizeof(float), (void*)0);
				Gl.EnableVertexAttribArray(1); Gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 9 * sizeof(float), (void*)(3 * sizeof(float)));
				Gl.EnableVertexAttribArray(2); Gl.VertexAttribPointer(2, 4, VertexAttribPointerType.Float, false, 9 * sizeof(float), (void*)(5 * sizeof(float)));
			}
			Gl.BindVertexArray(0);
			// FXAA post program + an empty VAO (fullscreen triangle is generated from gl_VertexID)
			_progPost = ShaderHelper.CreateShaderProgramFromSource(VSPOST, FSPOST);
			_postTex = Gl.GetUniformLocation(_progPost, "uTex"); _postMode = Gl.GetUniformLocation(_progPost, "uMode"); _postRes = Gl.GetUniformLocation(_progPost, "uRes");
			_vaoPost = Gl.GenVertexArray();
			_init = true;
		}

		private unsafe void EnsureFxaaTarget(Lua3DScene s) {
			if (_fxaaTex != 0 && _fxaaW == s._w && _fxaaH == s._h) return;
			if (_fxaaTex == 0) _fxaaTex = Gl.GenTexture();
			Gl.BindTexture(TextureTarget.Texture2D, _fxaaTex);
			Gl.TexImage2D(TextureTarget.Texture2D, 0, (int)InternalFormat.Rgba8, (uint)s._w, (uint)s._h, 0, PixelFormat.Rgba, PixelType.UnsignedByte, (void*)0);
			Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Nearest);
			Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Nearest);
			if (_fxaaFbo == 0) _fxaaFbo = Gl.GenFramebuffer();
			Gl.BindFramebuffer(FramebufferTarget.Framebuffer, _fxaaFbo);
			Gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, _fxaaTex, 0);
			_fxaaW = s._w; _fxaaH = s._h;
		}

		// point the 3D VAO (separate format) at a vertex buffer — one call per draw
		private void Bind3D(uint vbo) => Gl.BindVertexBuffer(0, vbo, 0, (uint)(STRIDE * sizeof(float)));

		private void EnsureFbo(Lua3DScene s) {
			uint colorTex = s._canvas.Pointer;
			if (_fbo != 0 && _fboW == s._w && _fboH == s._h && _fboColorTex == colorTex) return;
			if (_fbo == 0) _fbo = Gl.GenFramebuffer();
			Gl.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
			Gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, colorTex, 0);
			if (_depthRb == 0) _depthRb = Gl.GenRenderbuffer();
			Gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _depthRb);
			Gl.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.DepthComponent24, (uint)s._w, (uint)s._h);   // 24-bit: avoids edge z-fighting the 16-bit buffer caused
			Gl.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, _depthRb);
			_fboW = s._w; _fboH = s._h; _fboColorTex = colorTex;
		}

		private unsafe uint UploadTexture(int[] px, int w, int h, bool sprite) {
			byte[] b = new byte[w * h * 4];
			for (int i = 0; i < w * h; i++) {
				int p = px[i];
				b[i * 4] = (byte)(p >> 16); b[i * 4 + 1] = (byte)(p >> 8); b[i * 4 + 2] = (byte)p;
				b[i * 4 + 3] = sprite ? (byte)(p >> 24) : (p < 0 ? (byte)0 : (byte)255);
			}
			uint tex = Gl.GenTexture();
			Gl.BindTexture(TextureTarget.Texture2D, tex);
			fixed (byte* dp = b)
				Gl.TexImage2D(TextureTarget.Texture2D, 0, (int)InternalFormat.Rgba8, (uint)w, (uint)h, 0, PixelFormat.Rgba, PixelType.UnsignedByte, dp);
			Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.Repeat);
			Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.Repeat);
			Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Nearest);
			Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Nearest);
			return tex;
		}
		private uint TexFor(Lua3DScene s, int id) {
			s._texRev.TryGetValue(id, out int rev);
			if (_glTex.TryGetValue(id, out var t)) {
				if (!_glTexRev.TryGetValue(id, out int got) || got == rev) return t;   // current (or untracked) → reuse
				Gl.DeleteTexture(t);   // pixels changed (e.g. a live mirror RTT) → re-upload
			}
			if (!s._texPix.TryGetValue(id, out var px)) return 0;
			t = UploadTexture(px, s._texW[id], s._texH[id], false); _glTex[id] = t; _glTexRev[id] = rev; return t;
		}
		private uint SpriteFor(Lua3DScene s, int id) {
			if (_glSprite.TryGetValue(id, out var t)) return t;
			if (!s._spritePix.TryGetValue(id, out var px)) return 0;
			t = UploadTexture(px, s._spriteW[id], s._spriteH[id], true); _glSprite[id] = t; return t;
		}

		private float[] ModelMatrix(double[] t) {
			if (t == null) return _ident;
			var m = _mtx;
			m[0] = (float)t[0]; m[1] = (float)t[4]; m[2] = (float)t[8]; m[3] = 0;
			m[4] = (float)t[1]; m[5] = (float)t[5]; m[6] = (float)t[9]; m[7] = 0;
			m[8] = (float)t[2]; m[9] = (float)t[6]; m[10] = (float)t[10]; m[11] = 0;
			m[12] = (float)t[3]; m[13] = (float)t[7]; m[14] = (float)t[11]; m[15] = 1;
			return m;
		}

		private void Vtx(List<float> d, double x, double y, double z, double u, double v, double sh, double nx, double ny, double nz) {
			d.Add((float)x); d.Add((float)y); d.Add((float)z); d.Add((float)u); d.Add((float)v); d.Add((float)sh);
			d.Add((float)nx); d.Add((float)ny); d.Add((float)nz);
		}
		// per-primitive normal: the object's own normal if set, else the triangle's geometric normal
		private static void Nrm(SceneObject o, double ax, double ay, double az, double bx, double by, double bz, double cx, double cy, double cz, out double nx, out double ny, out double nz) {
			if (o.HasNormal) { nx = o.Nx; ny = o.Ny; nz = o.Nz; return; }
			double e1x = bx - ax, e1y = by - ay, e1z = bz - az, e2x = cx - ax, e2y = cy - ay, e2z = cz - az;
			nx = e1y * e2z - e1z * e2y; ny = e1z * e2x - e1x * e2z; nz = e1x * e2y - e1y * e2x;
			double l = Math.Sqrt(nx * nx + ny * ny + nz * nz); if (l > 1e-9) { nx /= l; ny /= l; nz /= l; }
		}

		// emit one object's primitives (model-space) into `dst`, grouping by texture via `use`
		private void Emit(Lua3DScene s, SceneObject o, List<float> dst, Action<int, uint> use) {
			var d = o.D;
			if (o.Kind == 0) {
				for (int i = 0; i < o.N; i++) {
					int k = i * 16; uint gt = TexFor(s, (int)d[k + 12]); if (gt == 0) continue;
					double uM = d[k + 13], vM = d[k + 14], sh = d[k + 15]; use(1, gt);
					Nrm(o, d[k], d[k + 1], d[k + 2], d[k + 3], d[k + 4], d[k + 5], d[k + 9], d[k + 10], d[k + 11], out double nx, out double ny, out double nz);
					Vtx(dst, d[k], d[k + 1], d[k + 2], 0, vM, sh, nx, ny, nz); Vtx(dst, d[k + 3], d[k + 4], d[k + 5], uM, vM, sh, nx, ny, nz); Vtx(dst, d[k + 6], d[k + 7], d[k + 8], uM, 0, sh, nx, ny, nz);
					Vtx(dst, d[k], d[k + 1], d[k + 2], 0, vM, sh, nx, ny, nz); Vtx(dst, d[k + 6], d[k + 7], d[k + 8], uM, 0, sh, nx, ny, nz); Vtx(dst, d[k + 9], d[k + 10], d[k + 11], 0, 0, sh, nx, ny, nz);
				}
			} else if (o.Kind == 1) {
				use(0, 0);
				for (int i = 0; i < o.N; i++) {
					int k = i * 12;
					Nrm(o, d[k], d[k + 1], d[k + 2], d[k + 3], d[k + 4], d[k + 5], d[k + 9], d[k + 10], d[k + 11], out double nx, out double ny, out double nz);
					Vtx(dst, d[k], d[k + 1], d[k + 2], 0, 0, 1, nx, ny, nz); Vtx(dst, d[k + 3], d[k + 4], d[k + 5], 0, 0, 1, nx, ny, nz); Vtx(dst, d[k + 6], d[k + 7], d[k + 8], 0, 0, 1, nx, ny, nz);
					Vtx(dst, d[k], d[k + 1], d[k + 2], 0, 0, 1, nx, ny, nz); Vtx(dst, d[k + 6], d[k + 7], d[k + 8], 0, 0, 1, nx, ny, nz); Vtx(dst, d[k + 9], d[k + 10], d[k + 11], 0, 0, 1, nx, ny, nz);
				}
			} else if (o.Kind == 2) {
				for (int i = 0; i < o.N; i++) {
					int k = i * 17; uint gt = TexFor(s, (int)d[k + 15]); if (gt == 0) continue;
					double sh = d[k + 16]; use(1, gt);
					Nrm(o, d[k], d[k + 1], d[k + 2], d[k + 5], d[k + 6], d[k + 7], d[k + 10], d[k + 11], d[k + 12], out double nx, out double ny, out double nz);
					Vtx(dst, d[k], d[k + 1], d[k + 2], d[k + 3], d[k + 4], sh, nx, ny, nz);
					Vtx(dst, d[k + 5], d[k + 6], d[k + 7], d[k + 8], d[k + 9], sh, nx, ny, nz);
					Vtx(dst, d[k + 10], d[k + 11], d[k + 12], d[k + 13], d[k + 14], sh, nx, ny, nz);
				}
			}
		}

		private ObjCache EnsureObj(Lua3DScene s, SceneObject o) {
			if (!_cache.TryGetValue(o, out var c)) { c = new ObjCache { Vbo = Gl.GenBuffer() }; _cache[o] = c; }
			if (c.Version == o.GeomVersion) return c;
			c.Version = o.GeomVersion; c.Batches.Clear(); _tmp.Clear();
			int curMode = -1, start = 0; uint curTex = uint.MaxValue;
			void Flush() { int cnt = _tmp.Count / STRIDE - start; if (cnt > 0) c.Batches.Add(new OBatch { Mode = curMode, Tex = curTex, Start = start, Count = cnt }); start = _tmp.Count / STRIDE; }
			void Use(int mode, uint tex) { if (mode != curMode || tex != curTex) { Flush(); curMode = mode; curTex = tex; } }
			Emit(s, o, _tmp, Use); Flush();
			Gl.BindBuffer(BufferTargetARB.ArrayBuffer, c.Vbo);
			Gl.BufferData<float>(BufferTargetARB.ArrayBuffer, (ReadOnlySpan<float>)CollectionsMarshal.AsSpan(_tmp), BufferUsageARB.StaticDraw);
			return c;
		}

		// ── merged static opaque geometry ────────────────────────────────────────────────────────
		private static bool StaticMerged(SceneObject o) {
			return (o.Kind == 0 || o.Kind == 2) && o.N > 0 && o.Pass == 0 && o.Transform == null && o.Lit
				&& !o.Overlay && !o.ScreenTex && o.A == 255 && o.TintR == 1.0 && o.TintG == 1.0 && o.TintB == 1.0;
		}
		private long ComputeStaticSig(Lua3DScene s) {
			long sig = 0;
			foreach (var kv in s._objects) if (StaticMerged(kv.Value)) sig += (long)kv.Key * 1000003L + kv.Value.GeomVersion + 1;
			return sig;
		}
		private static List<float> SubList(Dictionary<uint, List<float>> map, uint gt) { if (!map.TryGetValue(gt, out var l)) { l = new List<float>(256); map[gt] = l; } return l; }
		// Re-emit objects whose geometry changed, then SYNCHRONOUSLY rebuild only the affected
		// (bucket × texture) buffers. Each is small (one bucket's worth), so there's no whole-world upload
		// spike and the geometry is always current (no stale-frame x-ray). Called only when geometry changes.
		private void RebuildMerged(Lua3DScene s) {
			_dirtyKeys.Clear();
			foreach (var o in s._objects.Values) {
				if (!StaticMerged(o)) continue;
				if (!_mergeCache.TryGetValue(o, out var em)) { em = new MergeEmit(); _mergeCache[o] = em; }
				int bx = (int)Math.Floor(o.CenX / BS), bz = (int)Math.Floor(o.CenZ / BS);
				if (em.Version != o.GeomVersion || em.Bx != bx || em.Bz != bz) {
					foreach (var kv in em.ByTex) { if (kv.Value.Count > 0) _dirtyKeys.Add((em.Bx, em.Bz, kv.Key)); kv.Value.Clear(); }
					em.Version = o.GeomVersion; em.Bx = bx; em.Bz = bz;
					EmitObjectByTex(s, o, em.ByTex);
					foreach (var key in em.ByTex.Keys) _dirtyKeys.Add((bx, bz, key));
				}
			}
			_live.Clear(); foreach (var v in s._objects.Values) _live.Add(v);
			List<SceneObject> gone = null;
			foreach (var kv in _mergeCache)
				if (!_live.Contains(kv.Key) || !StaticMerged(kv.Key)) { (gone ??= new()).Add(kv.Key); foreach (var t in kv.Value.ByTex) if (t.Value.Count > 0) _dirtyKeys.Add((kv.Value.Bx, kv.Value.Bz, t.Key)); }
			if (gone != null) foreach (var o in gone) _mergeCache.Remove(o);
			foreach (var key in _dirtyKeys) {
				_scratch.Clear();
				foreach (var kv in _mergeCache) { var em = kv.Value; if (em.Bx != key.bx || em.Bz != key.bz) continue; if (em.ByTex.TryGetValue(key.gt, out var tl) && tl.Count > 0) _scratch.AddRange(tl); }
				if (!_merge.TryGetValue(key, out var mb)) mb = new MergeBuf { Vbo = Gl.GenBuffer() };
				mb.Count = _scratch.Count / STRIDE;
				Gl.BindBuffer(BufferTargetARB.ArrayBuffer, mb.Vbo);
				Gl.BufferData<float>(BufferTargetARB.ArrayBuffer, (ReadOnlySpan<float>)CollectionsMarshal.AsSpan(_scratch), BufferUsageARB.DynamicDraw);
				_merge[key] = mb;
			}
		}
		// emit one object's textured primitives into per-texture lists (its own cache)
		private void EmitObjectByTex(Lua3DScene s, SceneObject o, Dictionary<uint, List<float>> byTex) {
			var d = o.D;
			if (o.Kind == 0) {
				for (int i = 0; i < o.N; i++) {
					int k = i * 16; uint gt = TexFor(s, (int)d[k + 12]); if (gt == 0) continue;
					double uM = d[k + 13], vM = d[k + 14], sh = d[k + 15]; var L = SubList(byTex, gt);
					Nrm(o, d[k], d[k + 1], d[k + 2], d[k + 3], d[k + 4], d[k + 5], d[k + 9], d[k + 10], d[k + 11], out double nx, out double ny, out double nz);
					Vtx(L, d[k], d[k + 1], d[k + 2], 0, vM, sh, nx, ny, nz); Vtx(L, d[k + 3], d[k + 4], d[k + 5], uM, vM, sh, nx, ny, nz); Vtx(L, d[k + 6], d[k + 7], d[k + 8], uM, 0, sh, nx, ny, nz);
					Vtx(L, d[k], d[k + 1], d[k + 2], 0, vM, sh, nx, ny, nz); Vtx(L, d[k + 6], d[k + 7], d[k + 8], uM, 0, sh, nx, ny, nz); Vtx(L, d[k + 9], d[k + 10], d[k + 11], 0, 0, sh, nx, ny, nz);
				}
			} else {
				for (int i = 0; i < o.N; i++) {
					int k = i * 17; uint gt = TexFor(s, (int)d[k + 15]); if (gt == 0) continue;
					double sh = d[k + 16]; var L = SubList(byTex, gt);
					Nrm(o, d[k], d[k + 1], d[k + 2], d[k + 5], d[k + 6], d[k + 7], d[k + 10], d[k + 11], d[k + 12], out double nx, out double ny, out double nz);
					Vtx(L, d[k], d[k + 1], d[k + 2], d[k + 3], d[k + 4], sh, nx, ny, nz);
					Vtx(L, d[k + 5], d[k + 6], d[k + 7], d[k + 8], d[k + 9], sh, nx, ny, nz);
					Vtx(L, d[k + 10], d[k + 11], d[k + 12], d[k + 13], d[k + 14], sh, nx, ny, nz);
				}
			}
		}

		private void BuildSprites(Lua3DScene s) {
			_sprVerts.Clear(); _sprBatches.Clear();
			foreach (var o in s._objects.Values) {
				if (o.Kind != 3 || o.N <= 0 || !o.Visible) continue;
				var d = o.D; var t = o.Transform;
				int curCut = -1; uint curTex = uint.MaxValue; int start = _sprVerts.Count / STRIDE;
				void Flush() { int cnt = _sprVerts.Count / STRIDE - start; if (cnt > 0) _sprBatches.Add(new SprBatch { Obj = o, Cutout = curCut, Tex = curTex, Start = start, Count = cnt, Pass = o.Pass }); start = _sprVerts.Count / STRIDE; }
				void Use(uint tex, int cut) { if (tex != curTex || cut != curCut) { Flush(); curTex = tex; curCut = cut; } }
				for (int i = 0; i < o.N; i++) {
					int k = i * 8; uint gt = SpriteFor(s, (int)d[k + 5]); if (gt == 0) continue;
					double w = d[k + 3], hgt = d[k + 4]; int bb = (int)d[k + 6]; int cut = d[k + 7] != 0 ? 1 : 0;
					double cx, cy, cz;
					if (t == null) { cx = d[k]; cy = d[k + 1]; cz = d[k + 2]; }
					else { cx = t[0] * d[k] + t[1] * d[k + 1] + t[2] * d[k + 2] + t[3]; cy = t[4] * d[k] + t[5] * d[k + 1] + t[6] * d[k + 2] + t[7]; cz = t[8] * d[k] + t[9] * d[k + 1] + t[10] * d[k + 2] + t[11]; }
					double rx, ry, rz, ux, uy, uz;
					if (bb == 2) { rx = s._Rx; ry = s._Ry; rz = s._Rz; ux = s._Ux; uy = s._Uy; uz = s._Uz; }
					else if (bb == 1) { rx = s._Rx; ry = 0; rz = s._Rz; double rl = Math.Sqrt(rx * rx + rz * rz); if (rl > 1e-6) { rx /= rl; rz /= rl; } ux = 0; uy = 1; uz = 0; }
					else { rx = 1; ry = 0; rz = 0; ux = 0; uy = 1; uz = 0; }
					double hw = w * 0.5;
					double b0x = cx - rx * hw, b0y = cy - ry * hw, b0z = cz - rz * hw;
					double b1x = cx + rx * hw, b1y = cy + ry * hw, b1z = cz + rz * hw;
					double tx = ux * hgt, ty = uy * hgt, tz = uz * hgt;
					Use(gt, cut);
					Vtx(_sprVerts, b0x, b0y, b0z, 0, 1, 1, 0, 0, 0); Vtx(_sprVerts, b1x, b1y, b1z, 1, 1, 1, 0, 0, 0); Vtx(_sprVerts, b1x + tx, b1y + ty, b1z + tz, 1, 0, 1, 0, 0, 0);
					Vtx(_sprVerts, b0x, b0y, b0z, 0, 1, 1, 0, 0, 0); Vtx(_sprVerts, b1x + tx, b1y + ty, b1z + tz, 1, 0, 1, 0, 0, 0); Vtx(_sprVerts, b0x + tx, b0y + ty, b0z + tz, 0, 0, 1, 0, 0, 0);
				}
				Flush();
			}
			if (_sprVerts.Count > 0) { Gl.BindBuffer(BufferTargetARB.ArrayBuffer, _spriteVbo); Gl.BufferData<float>(BufferTargetARB.ArrayBuffer, (ReadOnlySpan<float>)CollectionsMarshal.AsSpan(_sprVerts), BufferUsageARB.StreamDraw); }
		}

		// ── particles (camera-facing billboards; built per frame, drawn last in the transparent pass) ──
		private List<float> PartList((int, int) key) { if (!_partBatches.TryGetValue(key, out var l)) { l = new List<float>(512); _partBatches[key] = l; } return l; }
		private static void PV(List<float> d, double x, double y, double z, double u, double v, double r, double g, double b, double a) {
			d.Add((float)x); d.Add((float)y); d.Add((float)z); d.Add((float)u); d.Add((float)v); d.Add((float)r); d.Add((float)g); d.Add((float)b); d.Add((float)a);
		}
		private void BuildParticles(Lua3DScene s) {
			foreach (var l in _partBatches.Values) l.Clear();
			var systems = s._particleSystems; if (systems == null) return;
			double Rx = s._Rx, Ry = s._Ry, Rz = s._Rz, Ux = s._Ux, Uy = s._Uy, Uz = s._Uz;
			for (int si = 0; si < systems.Count; si++) {
				var ps = systems[si]; var arr = ps.P; int cnt = ps.Count;
				for (int i = 0; i < cnt; i++) {
					ref var p = ref arr[i];
					double lf = p.MaxLife > 0 ? p.Life / p.MaxLife : 0; if (lf < 0) lf = 0; else if (lf > 1) lf = 1;
					double a = p.A0 * lf; if (a <= 0) continue; if (a > 1) a = 1;
					double hw = (p.Size0 + (p.Size1 - p.Size0) * (1.0 - lf)) * 0.5;
					double cx = p.X, cy = p.Y, cz = p.Z;
					double rX = Rx * hw, rY = Ry * hw, rZ = Rz * hw, uX = Ux * hw, uY = Uy * hw, uZ = Uz * hw;
					double cr = p.R / 255.0, cg = p.G / 255.0, cb = p.B / 255.0;
					var L = PartList((p.Additive ? 1 : 0, p.Sprite));
					PV(L, cx - rX - uX, cy - rY - uY, cz - rZ - uZ, 0, 1, cr, cg, cb, a);
					PV(L, cx + rX - uX, cy + rY - uY, cz + rZ - uZ, 1, 1, cr, cg, cb, a);
					PV(L, cx + rX + uX, cy + rY + uY, cz + rZ + uZ, 1, 0, cr, cg, cb, a);
					PV(L, cx - rX - uX, cy - rY - uY, cz - rZ - uZ, 0, 1, cr, cg, cb, a);
					PV(L, cx + rX + uX, cy + rY + uY, cz + rZ + uZ, 1, 0, cr, cg, cb, a);
					PV(L, cx - rX + uX, cy - rY + uY, cz - rZ + uZ, 0, 0, cr, cg, cb, a);
				}
			}
		}
		private void DrawParticles(Lua3DScene s) {
			bool any = false; foreach (var l in _partBatches.Values) if (l.Count > 0) { any = true; break; }
			if (!any) return;
			Gl.UseProgram(_progP);
			Gl.Uniform3(_pCam, (float)s._camX, (float)s._camY, (float)s._camZ);
			Gl.Uniform3(_pR, (float)s._Rx, (float)s._Ry, (float)s._Rz);
			Gl.Uniform3(_pU, (float)s._Ux, (float)s._Uy, (float)s._Uz);
			Gl.Uniform3(_pF, (float)s._Fx, (float)s._Fy, (float)s._Fz);
			Gl.Uniform1(_pSx, (float)(2.0 * s._scale / s._w)); Gl.Uniform1(_pSy, (float)(-2.0 * s._scale / s._h));
			double n = s._near, A = (FAR + n) / (FAR - n), B = -2.0 * FAR * n / (FAR - n);
			Gl.Uniform1(_pA, (float)A); Gl.Uniform1(_pB, (float)B); Gl.Uniform1(_pTex, 0);
			Gl.BindVertexArray(_vaoP);
			Gl.Enable(EnableCap.Blend); Gl.DepthMask(false);
			foreach (var kv in _partBatches) {
				var L = kv.Value; if (L.Count == 0) continue;
				var (add, tex) = kv.Key;
				if (add == 1) Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.One);
				else Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
				Gl.Uniform1(_pTextured, tex >= 0 ? 1 : 0);
				if (tex >= 0) { uint gt = SpriteFor(s, tex); Gl.ActiveTexture(TextureUnit.Texture0); Gl.BindTexture(TextureTarget.Texture2D, gt); }
				Gl.BindBuffer(BufferTargetARB.ArrayBuffer, _partVbo);
				Gl.BufferData<float>(BufferTargetARB.ArrayBuffer, (ReadOnlySpan<float>)CollectionsMarshal.AsSpan(L), BufferUsageARB.StreamDraw);
				Gl.DrawArrays(PrimitiveType.Triangles, 0, (uint)(L.Count / 9));
			}
		}

		private bool _logged;
		public unsafe void Render(Lua3DScene s) {
			if (!_logged) { _logged = true; Trace.TraceInformation("GpuRasterizer ACTIVE (hardware pipeline, merged + lit)"); }
			EnsureGL();
			_frame++;
			Span<int> vp = stackalloc int[4];
			Gl.GetInteger(GLEnum.Viewport, vp);
			uint prevFbo = (uint)Gl.GetInteger(GLEnum.FramebufferBinding);

			s._canvas.Upload();
			EnsureFbo(s);
			BuildSprites(s);
			BuildParticles(s);
			long sig = ComputeStaticSig(s);
			if (sig != _staticSig) { RebuildMerged(s); _staticSig = sig; }   // synchronous, per-bucket → no spike

			_opaque.Clear(); _trans.Clear(); _overlay.Clear(); _screenTex.Clear();
			foreach (var o in s._objects.Values) {
				if (o.Kind < 0 || o.Kind == 3 || o.N <= 0) continue;
				if (o.ScreenTex) { if (o.Visible && !s._rttPass) _screenTex.Add(o); continue; }   // mirror/portal surfaces: own draw
				if (StaticMerged(o)) continue;
				if (o.Overlay) { if (o.Visible) _overlay.Add(o); continue; }   // viewmodels: own pass
				if (!Drawable(s, o)) continue;
				if (o.Pass == 0) _opaque.Add(o); else _trans.Add(o);
			}
			_trans.Sort((a, b) => Dist(s, b).CompareTo(Dist(s, a)));

			Gl.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
			Gl.Viewport(0, 0, (uint)s._w, (uint)s._h);
			Gl.Clear((uint)ClearBufferMask.DepthBufferBit);
			Gl.Enable(EnableCap.DepthTest); Gl.DepthFunc(DepthFunction.Lequal); Gl.Disable(EnableCap.CullFace);
			Gl.UseProgram(_prog); Gl.BindVertexArray(_vao);
			SetFrameUniforms(s);

			int litScene = s._useLights ? 1 : 0;

			Gl.DepthMask(true); Gl.Disable(EnableCap.Blend);
			// merged static opaque (one draw per texture)
			Gl.Uniform1(_uMode, 1); Gl.Uniform1(_uCutout, 0); Gl.Uniform3(_uTint, 1f, 1f, 1f); Gl.Uniform1(_uAlpha, 1f);
			Gl.Uniform1(_uLit, litScene); Gl.UniformMatrix4(_uModel, 1, false, (ReadOnlySpan<float>)_ident);
			foreach (var kv in _merge) {
				if (kv.Value.Count == 0) continue;
				Bind3D(kv.Value.Vbo);
				Gl.ActiveTexture(TextureUnit.Texture0); Gl.BindTexture(TextureTarget.Texture2D, kv.Key.gt);
				Gl.DrawArrays(PrimitiveType.Triangles, 0, (uint)kv.Value.Count);
			}
			foreach (var o in _opaque) DrawObj(s, o, litScene);
			foreach (var o in _screenTex) DrawScreenTex(s, o);   // mirrors/portals (opaque, depth-written, screen-space sample)
			Bind3D(_spriteVbo);
			foreach (var bt in _sprBatches) if (bt.Pass == 0) DrawSprite(bt);

			Gl.DepthMask(false); Gl.Enable(EnableCap.Blend); Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
			foreach (var o in _trans) DrawObj(s, o, litScene);
			Bind3D(_spriteVbo);
			foreach (var bt in _sprBatches) if (bt.Pass == 1) DrawSprite(bt);
			DrawParticles(s);   // additive/alpha billboards, depth-tested against the scene, no depth write

			// overlay pass: viewmodels over a freshly-cleared depth buffer → always on top of the world
			if (_overlay.Count > 0) {
				Gl.UseProgram(_prog); Gl.BindVertexArray(_vao);
				Gl.Clear((uint)ClearBufferMask.DepthBufferBit);
				Gl.Enable(EnableCap.DepthTest); Gl.DepthMask(true); Gl.Disable(EnableCap.Blend);
				foreach (var o in _overlay) DrawObj(s, o, litScene);
			}

			// FXAA-lite post pass (before the stage's 2D overlay so the crosshair stays sharp):
			// copy canvas→temp, then edge-blend temp→canvas.
			if (s._aa) {
				EnsureFxaaTarget(s);
				Gl.Disable(EnableCap.DepthTest); Gl.DepthMask(false); Gl.Disable(EnableCap.Blend);
				Gl.UseProgram(_progPost); Gl.BindVertexArray(_vaoPost);
				Gl.Uniform1(_postTex, 0); Gl.Uniform2(_postRes, (float)s._w, (float)s._h);
				Gl.ActiveTexture(TextureUnit.Texture0);
				Gl.BindFramebuffer(FramebufferTarget.Framebuffer, _fxaaFbo);     // copy: canvas → temp
				Gl.BindTexture(TextureTarget.Texture2D, s._canvas.Pointer); Gl.Uniform1(_postMode, 0);
				Gl.DrawArrays(PrimitiveType.Triangles, 0, 3);
				Gl.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);          // fxaa: temp → canvas
				Gl.BindTexture(TextureTarget.Texture2D, _fxaaTex); Gl.Uniform1(_postMode, 1);
				Gl.DrawArrays(PrimitiveType.Triangles, 0, 3);
			}

			Gl.BindFramebuffer(FramebufferTarget.Framebuffer, prevFbo);
			Gl.Viewport(vp[0], vp[1], (uint)vp[2], (uint)vp[3]);
			Gl.UseProgram(0); Gl.BindVertexArray(0); Gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
			Gl.ActiveTexture(TextureUnit.Texture0); Gl.BindTexture(TextureTarget.Texture2D, 0);
			Gl.Disable(EnableCap.DepthTest); Gl.DepthMask(true); Gl.Disable(EnableCap.CullFace);
			Gl.Enable(EnableCap.Blend); BlendHelper.SetBlend(BlendType.Normal);
			s._gpuOwnsCanvas = true;

			if (_frame >= _pruneAt) { _pruneAt = _frame + 120; Prune(s); }
		}

		private void SetFrameUniforms(Lua3DScene s) {
			Gl.Uniform3(_uCam, (float)s._camX, (float)s._camY, (float)s._camZ);
			Gl.Uniform3(_uR, (float)s._Rx, (float)s._Ry, (float)s._Rz);
			Gl.Uniform3(_uU, (float)s._Ux, (float)s._Uy, (float)s._Uz);
			Gl.Uniform3(_uF, (float)s._Fx, (float)s._Fy, (float)s._Fz);
			Gl.Uniform1(_uSx, (float)(2.0 * s._scale / s._w));
			Gl.Uniform1(_uSy, (float)(-2.0 * s._scale / s._h));
			double n = s._near, A = (FAR + n) / (FAR - n), B = -2.0 * FAR * n / (FAR - n);
			Gl.Uniform1(_uA, (float)A); Gl.Uniform1(_uB, (float)B); Gl.Uniform1(_uTex, 0);
			Gl.Uniform1(_uRsh, s._rttShift);   // mirror screen-tex sampling shift
			// fog colour is stored in 0-255 (the CPU blends in pixel space); our shader works in 0-1
			if (s._fog) { Gl.Uniform1(_uFog, 1); Gl.Uniform3(_uFogCol, (float)(s._fogR / 255.0), (float)(s._fogG / 255.0), (float)(s._fogB / 255.0)); Gl.Uniform1(_uFogStart, (float)s._fogStart); Gl.Uniform1(_uFogInv, (float)s._fogInv); }
			else Gl.Uniform1(_uFog, 0);
			// lighting: sun + ambient + point lights
			Gl.Uniform3(_uSunDir, (float)s._sunX, (float)s._sunY, (float)s._sunZ);
			Gl.Uniform3(_uSunCol, (float)s._sunR, (float)s._sunG, (float)s._sunB);
			Gl.Uniform3(_uAmb, (float)s._ambR, (float)s._ambG, (float)s._ambB);
			var lights = s._lights; int nl = lights.Count < MAXL ? lights.Count : MAXL;
			for (int i = 0; i < nl; i++) {
				var L = lights[i];
				_lpos[i * 4] = (float)L.X; _lpos[i * 4 + 1] = (float)L.Y; _lpos[i * 4 + 2] = (float)L.Z; _lpos[i * 4 + 3] = (float)L.Range;
				_lcol[i * 3] = (float)L.R; _lcol[i * 3 + 1] = (float)L.G; _lcol[i * 3 + 2] = (float)L.B;
			}
			Gl.Uniform1(_uNumLights, nl);
			if (nl > 0) { Gl.Uniform4(_uLightPosR, (uint)nl, new ReadOnlySpan<float>(_lpos, 0, nl * 4)); Gl.Uniform3(_uLightCol, (uint)nl, new ReadOnlySpan<float>(_lcol, 0, nl * 3)); }
		}

		private bool Drawable(Lua3DScene s, SceneObject o) {
			if (!o.Visible || o.Overlay || o.ScreenTex) return false;
			if (s._renderDist > 0 && o.HasBounds) { double rr = s._renderDist + o.Radius; if (Dist(s, o) > rr * rr) return false; }
			return true;
		}
		private static double Dist(Lua3DScene s, SceneObject o) { double dx = o.CenX - s._camX, dy = o.CenY - s._camY, dz = o.CenZ - s._camZ; return dx * dx + dy * dy + dz * dz; }

		private void DrawObj(Lua3DScene s, SceneObject o, int litScene) {
			var c = EnsureObj(s, o);
			if (c.Batches.Count == 0) return;
			Bind3D(c.Vbo);
			Gl.UniformMatrix4(_uModel, 1, false, (ReadOnlySpan<float>)ModelMatrix(o.Transform));
			Gl.Uniform3(_uTint, (float)o.TintR, (float)o.TintG, (float)o.TintB);
			Gl.Uniform1(_uAlpha, o.A / 255.0f); Gl.Uniform1(_uCutout, 0);
			Gl.Uniform1(_uLit, (litScene == 1 && o.Lit) ? 1 : 0);
			float fr = (float)(o.R / 255.0), fg = (float)(o.G / 255.0), fb = (float)(o.B / 255.0);
			foreach (var bt in c.Batches) {
				Gl.Uniform1(_uMode, bt.Mode);
				if (bt.Mode == 0) Gl.Uniform3(_uFlat, fr, fg, fb);
				else { Gl.ActiveTexture(TextureUnit.Texture0); Gl.BindTexture(TextureTarget.Texture2D, bt.Tex); }
				Gl.DrawArrays(PrimitiveType.Triangles, bt.Start, (uint)bt.Count);
			}
		}
		private void DrawSprite(SprBatch bt) {
			var o = bt.Obj;
			Gl.UniformMatrix4(_uModel, 1, false, (ReadOnlySpan<float>)_ident);
			Gl.Uniform1(_uMode, 2); Gl.Uniform1(_uCutout, bt.Cutout); Gl.Uniform1(_uLit, 0);
			Gl.Uniform3(_uTint, (float)o.TintR, (float)o.TintG, (float)o.TintB); Gl.Uniform1(_uAlpha, o.A / 255.0f);
			Gl.ActiveTexture(TextureUnit.Texture0); Gl.BindTexture(TextureTarget.Texture2D, bt.Tex);
			Gl.DrawArrays(PrimitiveType.Triangles, bt.Start, (uint)bt.Count);
		}
		// A screen-texture mirror/portal surface (uMode 3): the FS samples the live RTT texture by screen
		// pixel and blends it over the base colour by the per-channel mirror factor (the object's tint). The
		// texture is the per-frame RenderView capture, re-uploaded by TexFor when its revision changes.
		private void DrawScreenTex(Lua3DScene s, SceneObject o) {
			var c = EnsureObj(s, o);
			if (c.Batches.Count == 0) return;
			int texId = (int)o.D[12];                       // Kind-0 quad texId = the mirror RTT
			uint gt = TexFor(s, texId); if (gt == 0) return;
			Bind3D(c.Vbo);
			Gl.UniformMatrix4(_uModel, 1, false, (ReadOnlySpan<float>)ModelMatrix(o.Transform));
			Gl.Uniform1(_uMode, 3); Gl.Uniform1(_uCutout, 0); Gl.Uniform1(_uLit, 0);
			Gl.Uniform3(_uTint, (float)o.TintR, (float)o.TintG, (float)o.TintB);          // mirror factor (fm)
			Gl.Uniform3(_uFlat, (float)(o.R / 255.0), (float)(o.G / 255.0), (float)(o.B / 255.0));   // base colour under the mirror
			Gl.Uniform1(_uAlpha, o.A / 255.0f);
			Gl.ActiveTexture(TextureUnit.Texture0); Gl.BindTexture(TextureTarget.Texture2D, gt);
			int total = 0; foreach (var bt in c.Batches) total += bt.Count;
			Gl.DrawArrays(PrimitiveType.Triangles, 0, (uint)total);   // single mirror texture → one contiguous draw
		}

		private void Prune(Lua3DScene s) {
			if (_cache.Count <= s._objects.Count) return;
			_live.Clear(); foreach (var v in s._objects.Values) _live.Add(v);
			List<SceneObject> dead = null;
			foreach (var kv in _cache) if (!_live.Contains(kv.Key)) { (dead ??= new()).Add(kv.Key); Gl.DeleteBuffer(kv.Value.Vbo); }
			if (dead != null) foreach (var o in dead) _cache.Remove(o);
		}

		// ── 2D overlay onto the canvas texture (crosshair/selection/etc. drawn after the 3D) ──────────
		private unsafe void Begin2D(Lua3DScene s) {
			if (_2dActive) return;
			EnsureGL(); EnsureFbo(s);
			Span<int> v = stackalloc int[4]; Gl.GetInteger(GLEnum.Viewport, v); _vp2[0] = v[0]; _vp2[1] = v[1]; _vp2[2] = v[2]; _vp2[3] = v[3];
			_prevFbo2 = (uint)Gl.GetInteger(GLEnum.FramebufferBinding);
			Gl.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
			Gl.Viewport(0, 0, (uint)s._w, (uint)s._h);
			Gl.Disable(EnableCap.DepthTest);
			Gl.Enable(EnableCap.Blend); Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
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
			Gl.BindFramebuffer(FramebufferTarget.Framebuffer, _prevFbo2);
			Gl.Viewport(_vp2[0], _vp2[1], (uint)_vp2[2], (uint)_vp2[3]);
			Gl.UseProgram(0); Gl.BindVertexArray(0); Gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
			Gl.Disable(EnableCap.DepthTest); Gl.DepthMask(true); Gl.Disable(EnableCap.CullFace);
			Gl.Enable(EnableCap.Blend); BlendHelper.SetBlend(BlendType.Normal);
			_2dActive = false;
		}

		public void Invalidate() {
			if (_fbo != 0) { Gl.DeleteFramebuffer(_fbo); _fbo = 0; }
			if (_depthRb != 0) { Gl.DeleteRenderbuffer(_depthRb); _depthRb = 0; }
			if (_fxaaFbo != 0) { Gl.DeleteFramebuffer(_fxaaFbo); _fxaaFbo = 0; }
			if (_fxaaTex != 0) { Gl.DeleteTexture(_fxaaTex); _fxaaTex = 0; }
			_fboW = _fboH = 0; _fboColorTex = 0; _fxaaW = _fxaaH = 0;
			foreach (var c in _cache.Values) Gl.DeleteBuffer(c.Vbo);
			_cache.Clear();
			foreach (var mb in _merge.Values) Gl.DeleteBuffer(mb.Vbo);
			_merge.Clear(); _scratch.Clear(); _scratch.TrimExcess(); _mergeCache.Clear(); _dirtyKeys.Clear(); _staticSig = long.MinValue;
			foreach (var t in _glTex.Values) Gl.DeleteTexture(t);
			foreach (var t in _glSprite.Values) Gl.DeleteTexture(t);
			_glTex.Clear(); _glSprite.Clear(); _glTexRev.Clear();
		}

		// Free EVERY GL resource, including the one-time programs/VAOs/buffers EnsureGL creates. Called
		// from Lua3DScene.Dispose so a stage exit doesn't leak the renderer's GL objects.
		public void Dispose() {
			if (!_init) { Invalidate(); return; }
			Invalidate();
			if (_vao != 0) Gl.DeleteVertexArray(_vao);
			if (_vao2d != 0) Gl.DeleteVertexArray(_vao2d);
			if (_vaoP != 0) Gl.DeleteVertexArray(_vaoP);
			if (_vaoPost != 0) Gl.DeleteVertexArray(_vaoPost);
			if (_spriteVbo != 0) Gl.DeleteBuffer(_spriteVbo);
			if (_vbo2d != 0) Gl.DeleteBuffer(_vbo2d);
			if (_partVbo != 0) Gl.DeleteBuffer(_partVbo);
			if (_prog != 0) Gl.DeleteProgram(_prog);
			if (_prog2d != 0) Gl.DeleteProgram(_prog2d);
			if (_progP != 0) Gl.DeleteProgram(_progP);
			if (_progPost != 0) Gl.DeleteProgram(_progPost);
			_vao = _vao2d = _vaoP = _vaoPost = _spriteVbo = _vbo2d = _partVbo = _prog = _prog2d = _progP = _progPost = 0;
			_init = false;
		}
	}
}
