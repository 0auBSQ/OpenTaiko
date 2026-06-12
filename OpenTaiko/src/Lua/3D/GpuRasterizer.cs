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
	/// match the 2D draw).
	/// </summary>
	internal sealed class GpuRasterizer : IRenderer {
		private GL Gl => Game.Gl;
		private const int STRIDE = 9;        // pos(3) uv(2) shade(1) normal(3)
		// Projection far plane. Kept as tight as every stage allows (the iso faked-ortho camera sits ~46 back of a
		// ~400-wide world → ~600 max; the FPS/kart stages are smaller) because a huge FAR with a tiny near wastes almost all
		// the 24-bit depth range up close — that hyperbolic crush is the #1 cause of z-fighting on near-coplanar
		// surfaces. 3000 keeps generous headroom while giving ~3x the depth resolution the old 10000 did.
		private const double FAR = 3000.0;
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
		// diorama post pass (SetDiorama, "diorama"): a cheap SCREEN-SPACE tilt-shift (blur grows toward
		// the top/bottom, a sharp middle band) + saturation/contrast/vignette grade. No depth sample (the old
		// depth blur was too costly + the depth-texture FBO slowed the main pass), so the main FBO is back to a
		// plain depth renderbuffer.
		private uint _progDiorama; private int _hTex, _hRes, _hTilt, _hSat, _hVig, _hBloom;
		// toon OUTLINE post pass (3D characters): true-distance silhouette ink with an AA rim
		private uint _progOutline; private int _oTex, _oRes, _oColor, _oThick;

		private uint _fbo, _depthRb;
		private int _fboW, _fboH; private uint _fboColorTex;

		// ── sun SHADOW MAP. The lit fragment shader is compiled in TWO variants: the DEFAULT _prog has NO shadow
		// code at all (so stages that don't use shadows are byte-identical to before — no predicated texture
		// taps), and _progShadow adds the depth-map sampling. We SWITCH PROGRAMS per frame (BindWorldProgram)
		// rather than branch on a uniform inside one shared shader: a uniform branch is PREDICATED on GLES/ANGLE,
		// so the 4 shadow taps + the light-space transform would run for EVERY fragment of EVERY stage — that was
		// the 10× regression. Variants confine the cost to scenes that actually want shadows.
		private uint _progDepth, _progDepthSpr, _progShadow, _shadowFbo, _shadowTex;
		private int _uDLightVP, _uDModel, _uDSLightVP, _uDSTex;
		private int _uLightVP, _uShadow, _uShadowTexel;
		private bool _shadowOk = true;
		private bool _worldShadow;       // which world variant is currently bound (so a mid-pass rebind matches)
		private const int SHADOW_SIZE = 1920;   // full-HD shadow map over a tight player-centred box (HALF 60) = ~16 texels/unit
		// shadow half-extent now lives on the scene (Lua3DScene._shadowHalf, SetShadowArea)
		private readonly float[] _lightVP = new float[16];
		private double _lightFx, _lightFz;       // shadow-map focus XZ (for culling far merge buckets out of the depth pass)
		// sun-facing silhouette casters for billboard characters, rebuilt per shadow frame: a camera-facing quad
		// presents the wrong outline to the sun (and thins/warps as you orbit), so the depth pass instead draws a
		// quad turned to face the sun's azimuth → a stable, correctly-shaped cast shadow on the ground.
		private uint _casterVbo;
		private struct CBatch { public uint Tex; public int Start, Count; }
		private readonly List<float> _casterVerts = new(512);
		private readonly List<CBatch> _casterBatches = new(16);
		// cached world-program uniform locations per variant; BindWorldProgram copies the active set into _u*
		private struct WLoc { public int Cam, R, U, F, Sx, Sy, A, B, Model, Mode, Tex, Cutout, Tint, Alpha, Flat, Fog, FogCol, FogStart, FogInv, Rsh, Lit, SunDir, SunCol, Amb, NumLights, LightPosR, LightCol, LightVP, Shadow, ShadowTexel; }
		private WLoc _locDef, _locSh;

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
		// Lit/textured world FS. Compiled TWICE: as-is (default — no shadow code) and with `#define SHADOWS`
		// prepended (adds the sun depth-map sampling). The `#version` is prepended at compile time so the define
		// can sit right after it. A separate VARIANT — not a `uShadowOn` uniform branch — is what keeps the shadow
		// taps from running (predicated) on every stage's fragments. In the default variant shf folds to 1.0.
		private const string FS = @"
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
#ifdef SHADOWS
uniform mat4 uLightVP; uniform sampler2D uShadow; uniform float uShadowTexel;
float sunShadow(vec3 wp, vec3 N){
    vec4 lp = uLightVP * vec4(wp, 1.0);
    vec3 p = lp.xyz / lp.w * 0.5 + 0.5;
    if (p.z > 1.0 || p.x < 0.0 || p.x > 1.0 || p.y < 0.0 || p.y > 1.0) return 1.0;   // outside the map -> lit (ASCII only! ANGLE rejects bytes>127 even in comments)
    // slope-scaled bias: tiny on surfaces facing the sun (shadow hugs the feet, no peter-panning), larger on
    // grazing slopes (no acne). The old flat 0.0024 floated the shadow off the character's feet.
    float ndl = max(dot(N, uSunDir), 0.0);
    float bias = max(0.0008, 0.0026 * (1.0 - ndl)); float t = p.z - bias; float ts = uShadowTexel; float sh = 0.0;
    sh += step(t, texture(uShadow, p.xy + vec2(-0.5, -0.5) * ts).r);
    sh += step(t, texture(uShadow, p.xy + vec2( 0.5, -0.5) * ts).r);
    sh += step(t, texture(uShadow, p.xy + vec2(-0.5,  0.5) * ts).r);
    sh += step(t, texture(uShadow, p.xy + vec2( 0.5,  0.5) * ts).r);
    return sh * 0.25;
}
#endif
vec3 lightAt(vec3 N, vec3 wp){
    float e = sqrt(max(vShade, 0.0));
#ifdef SHADOWS
    float shf = sunShadow(wp, N);
#else
    float shf = 1.0;
#endif
    vec3 l = e * (uAmb + max(dot(N, uSunDir), 0.0) * uSunCol * shf);
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
        // reflection amount = tint * surface shade (matches the CPU's fmR = shade*tint). The mirror leaves
        // its tint at 1 and carries the reflectivity (e.g. 0.85) in the per-vertex shade, so it stays
        // semi-opaque (reflection blended over the base colour) rather than a full mirror.
        vec3 fm = uTint * vShade;
        vec3 col = m * fm + uFlat * (1.0 - fm);
        if (uFog == 1){ float f = clamp((vCamZ - uFogStart) * uFogInv, 0.0, 1.0); col = mix(col, uFogCol, f); }
        frag = vec4(col, uAlpha); return;
    }
    if (uMode == 2){ vec4 t = texture(uTex, vUV); vec4 c = vec4(t.rgb * uTint, t.a * uAlpha); if (uCutout == 1 && c.a < 0.5) discard;
#ifdef SHADOWS
        c.rgb *= (0.55 + 0.45 * sunShadow(vWorld, vNormal));   // characters RECEIVE the sun shadow (a wall's shade dims them)
#endif
        if (uFog == 1){ float f = clamp((vCamZ - uFogStart) * uFogInv, 0.0, 1.0); c.rgb = mix(c.rgb, uFogCol, f); } frag = c; return; }
    vec3 base;
    if (uMode == 0){ base = uFlat; }
    else { vec4 t = texture(uTex, vUV); if (t.a < 0.5) discard; base = t.rgb * uTint; }
    vec3 col = (uLit == 1) ? base * lightAt(normalize(vNormal), vWorld) : base * vShade;
    if (uFog == 1){ float f = clamp((vCamZ - uFogStart) * uFogInv, 0.0, 1.0); col = mix(col, uFogCol, f); }
    frag = vec4(col, uAlpha);
}";

		// shadow depth pass — opaque casters (terrain/objects): write depth from the light's POV
		private const string VSD = @"#version 300 es
layout(location=0) in vec3 aPos;
uniform mat4 uLightVP, uModel;
void main(){ gl_Position = uLightVP * uModel * vec4(aPos, 1.0); }";
		private const string FSD = @"#version 300 es
precision highp float;
void main(){}";
		// shadow depth pass — SPRITE casters (the character): alpha-test so the SILHOUETTE is what casts
		private const string VSDS = @"#version 300 es
layout(location=0) in vec3 aPos;
layout(location=1) in vec2 aUV;
uniform mat4 uLightVP;
out vec2 vUV;
void main(){ vUV = aUV; gl_Position = uLightVP * vec4(aPos, 1.0); }";
		private const string FSDS = @"#version 300 es
precision highp float;
in vec2 vUV; uniform sampler2D uTex;
void main(){ if (texture(uTex, vUV).a < 0.5) discard; }";

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
    if (uMode == 2){ frag = texelFetch(uTex, p, 0); return; } // RGBA copy (alpha preserved: outline pass)
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
		// 'Diorama' grade: SCREEN-SPACE tilt-shift (sharp middle band, blur ramping to the
		// top/bottom — only the edge bands pay the 4 taps, the centre is 1 tap) + saturation/contrast/vignette.
		// No depth sample, so it's cheap and needs no special FBO.
		private const string FSOUTLINE = @"#version 300 es
precision highp float;
uniform sampler2D uTex; uniform vec2 uRes; uniform vec3 uColor; uniform float uThick;
out vec4 frag;
void main(){
    ivec2 p = ivec2(gl_FragCoord.xy);
    vec4 c = texelFetch(uTex, p, 0);
    if (c.a > 0.001) { frag = c; return; }
    int T = int(ceil(uThick)) + 1;
    if (T > 12) T = 12;
    float best = 1e9;
    for (int dy = -12; dy <= 12; dy++) {
        if (dy < -T || dy > T) continue;
        for (int dx = -12; dx <= 12; dx++) {
            if (dx < -T || dx > T) continue;
            ivec2 q = p + ivec2(dx, dy);
            if (q.x < 0 || q.y < 0 || q.x >= int(uRes.x) || q.y >= int(uRes.y)) continue;
            if (texelFetch(uTex, q, 0).a > 0.5) {
                float d = length(vec2(float(dx), float(dy)));
                if (d < best) best = d;
            }
        }
    }
    float a = clamp(uThick + 0.5 - best, 0.0, 1.0);
    frag = vec4(uColor, a);
}";
		private const string FSDIORAMA = @"#version 300 es
precision highp float;
uniform sampler2D uTex; uniform vec2 uRes; uniform float uTilt, uSat, uVig, uBloom;
out vec4 frag;
void main(){
    vec2 uv = (gl_FragCoord.xy) / uRes;
    float dy = abs(uv.y - 0.5) * 2.0;                 // 0 centre .. 1 top/bottom
    float band = smoothstep(0.34, 1.0, dy);           // middle ~34% stays sharp, ramps out
    float rad = band * uTilt;
    vec3 c = texture(uTex, uv).rgb;
    if (rad > 0.5){
        vec2 inv = 1.0 / uRes;
        c = (c + texture(uTex, uv + vec2(0.0, rad) * inv).rgb + texture(uTex, uv + vec2(0.0, -rad) * inv).rgb
               + texture(uTex, uv + vec2(rad, 0.0) * inv).rgb + texture(uTex, uv + vec2(-rad, 0.0) * inv).rgb) * 0.2;
    }
    // BLOOM (the diorama glow): a sparse 12-tap ring gathers thresholded brightness around the pixel and
    // adds it back as a soft halo. One pass, no extra FBO; only diorama scenes pay for it (and only if uBloom>0).
    if (uBloom > 0.001){
        vec2 inv = 1.0 / uRes;
        vec3 acc = vec3(0.0);
        const float R1 = 7.0; const float R2 = 15.0;
        for (int i = 0; i < 6; i++){
            float a = float(i) * 1.0471975;            // 6 taps on the inner ring (60 deg apart)
            vec2 o = vec2(cos(a), sin(a));
            acc += max(texture(uTex, uv + o * R1 * inv).rgb - 0.62, 0.0);
            acc += max(texture(uTex, uv + o * R2 * inv).rgb - 0.62, 0.0) * 0.6;
        }
        c += acc * (uBloom * 0.22);
    }
    float l = dot(c, vec3(0.299, 0.587, 0.114));
    c = mix(vec3(l), c, uSat);                         // saturation
    c = (c - 0.5) * 1.08 + 0.5;                        // gentle contrast
    vec2 d = uv - 0.5;
    c *= 1.0 - uVig * dot(d, d) * 2.2;                 // vignette
    frag = vec4(clamp(c, 0.0, 1.0), 1.0);
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

		// ANGLE's GLSL ES translator rejects ANY byte >127 (even inside // comments) -> the shader fails to
		// compile -> EnsureGL throws -> the WHOLE GPU rasterizer silently falls back to the CPU one (no shadows,
		// CPU-speed lag). That bug has bitten twice (a stray arrow/em-dash in a comment), so strip non-ASCII
		// defensively here — harmless in comments, and code shouldn't contain any.
		private static string Ascii(string s) {
			bool clean = true; foreach (char c in s) if (c > 127) { clean = false; break; }
			if (clean) return s;
			var sb = new System.Text.StringBuilder(s.Length);
			foreach (char c in s) if (c <= 127) sb.Append(c);
			return sb.ToString();
		}

		// look up every world-program uniform for one variant (locations differ between the two compiled programs)
		private WLoc QueryWLoc(uint p) {
			int g(string n) => Gl.GetUniformLocation(p, n);
			return new WLoc {
				Cam = g("uCam"), R = g("uR"), U = g("uU"), F = g("uF"), Sx = g("uSx"), Sy = g("uSy"), A = g("uA"), B = g("uB"), Model = g("uModel"),
				Mode = g("uMode"), Tex = g("uTex"), Cutout = g("uCutout"), Tint = g("uTint"), Alpha = g("uAlpha"), Flat = g("uFlat"),
				Fog = g("uFog"), FogCol = g("uFogCol"), FogStart = g("uFogStart"), FogInv = g("uFogInv"), Rsh = g("uRsh"),
				Lit = g("uLit"), SunDir = g("uSunDir"), SunCol = g("uSunCol"), Amb = g("uAmb"),
				NumLights = g("uNumLights"), LightPosR = g("uLightPosR[0]"), LightCol = g("uLightCol[0]"),
				LightVP = g("uLightVP"), Shadow = g("uShadow"), ShadowTexel = g("uShadowTexel"),
			};
		}
		// bind the chosen world variant AND point the active _u* fields at that program's locations, so every
		// Gl.Uniform* in the draw code targets the bound program (locations are NOT guaranteed equal across variants)
		private void BindWorldProgram(bool shadow) {
			_worldShadow = shadow && _shadowOk;
			Gl.UseProgram(_worldShadow ? _progShadow : _prog);
			var L = _worldShadow ? _locSh : _locDef;
			_uCam = L.Cam; _uR = L.R; _uU = L.U; _uF = L.F; _uSx = L.Sx; _uSy = L.Sy; _uA = L.A; _uB = L.B; _uModel = L.Model;
			_uMode = L.Mode; _uTex = L.Tex; _uCutout = L.Cutout; _uTint = L.Tint; _uAlpha = L.Alpha; _uFlat = L.Flat;
			_uFog = L.Fog; _uFogCol = L.FogCol; _uFogStart = L.FogStart; _uFogInv = L.FogInv; _uRsh = L.Rsh;
			_uLit = L.Lit; _uSunDir = L.SunDir; _uSunCol = L.SunCol; _uAmb = L.Amb;
			_uNumLights = L.NumLights; _uLightPosR = L.LightPosR; _uLightCol = L.LightCol;
			_uLightVP = L.LightVP; _uShadow = L.Shadow; _uShadowTexel = L.ShadowTexel;
		}

		private void EnsureGL() {
			if (_init) return;
			_prog = ShaderHelper.CreateShaderProgramFromSource(Ascii(VS), Ascii("#version 300 es\n" + FS));
			_prog2d = ShaderHelper.CreateShaderProgramFromSource(VS2D, FS2D);
			_uRes2d = Gl.GetUniformLocation(_prog2d, "uRes"); _uColor2d = Gl.GetUniformLocation(_prog2d, "uColor");
			_vbo2d = Gl.GenBuffer();
			_locDef = QueryWLoc(_prog);
			try {   // the lit shader's SHADOWS variant + the two depth-pass programs (degrade to no-shadows on failure)
				_progShadow = ShaderHelper.CreateShaderProgramFromSource(Ascii(VS), Ascii("#version 300 es\n#define SHADOWS\n" + FS));
				_locSh = QueryWLoc(_progShadow);
				_progDepth = ShaderHelper.CreateShaderProgramFromSource(VSD, FSD);
				_uDLightVP = Gl.GetUniformLocation(_progDepth, "uLightVP"); _uDModel = Gl.GetUniformLocation(_progDepth, "uModel");
				_progDepthSpr = ShaderHelper.CreateShaderProgramFromSource(VSDS, FSDS);
				_uDSLightVP = Gl.GetUniformLocation(_progDepthSpr, "uLightVP"); _uDSTex = Gl.GetUniformLocation(_progDepthSpr, "uTex");
			} catch (Exception e) { Trace.TraceError("Shadow programs failed: " + e); _shadowOk = false; }
			BindWorldProgram(false);   // populate the active _u* set from the default variant
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
			_progPost = ShaderHelper.CreateShaderProgramFromSource(Ascii(VSPOST), Ascii(FSPOST));
			_postTex = Gl.GetUniformLocation(_progPost, "uTex"); _postMode = Gl.GetUniformLocation(_progPost, "uMode"); _postRes = Gl.GetUniformLocation(_progPost, "uRes");
			_progDiorama = ShaderHelper.CreateShaderProgramFromSource(Ascii(VSPOST), Ascii(FSDIORAMA));
			_progOutline = ShaderHelper.CreateShaderProgramFromSource(Ascii(VSPOST), Ascii(FSOUTLINE));
			_oTex = Gl.GetUniformLocation(_progOutline, "uTex"); _oRes = Gl.GetUniformLocation(_progOutline, "uRes");
			_oColor = Gl.GetUniformLocation(_progOutline, "uColor"); _oThick = Gl.GetUniformLocation(_progOutline, "uThick");
			_hTex = Gl.GetUniformLocation(_progDiorama, "uTex"); _hRes = Gl.GetUniformLocation(_progDiorama, "uRes");
			_hTilt = Gl.GetUniformLocation(_progDiorama, "uTilt"); _hSat = Gl.GetUniformLocation(_progDiorama, "uSat"); _hVig = Gl.GetUniformLocation(_progDiorama, "uVig");
			_hBloom = Gl.GetUniformLocation(_progDiorama, "uBloom");
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
			// depth RENDERBUFFER (24-bit; avoids the edge z-fighting the old 16-bit buffer had). Plain renderbuffer
			// keeps the main pass fast (a sampleable depth texture lost a driver fast path → the volcano slowdown).
			if (_depthRb == 0) _depthRb = Gl.GenRenderbuffer();
			Gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _depthRb);
			Gl.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.DepthComponent24, (uint)s._w, (uint)s._h);
			Gl.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, _depthRb);
			_fboW = s._w; _fboH = s._h; _fboColorTex = colorTex;
		}

		private unsafe bool EnsureShadowFbo() {
			if (!_shadowOk) return false;
			if (_shadowFbo != 0) return true;
			try {
				_shadowTex = Gl.GenTexture();
				Gl.BindTexture(TextureTarget.Texture2D, _shadowTex);
				Gl.TexImage2D(TextureTarget.Texture2D, 0, (int)InternalFormat.DepthComponent24, SHADOW_SIZE, SHADOW_SIZE, 0, PixelFormat.DepthComponent, PixelType.UnsignedInt, (void*)0);
				Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Nearest);
				Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Nearest);
				Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
				Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);
				_shadowFbo = Gl.GenFramebuffer();
				Gl.BindFramebuffer(FramebufferTarget.Framebuffer, _shadowFbo);
				Gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, _shadowTex, 0);
				var st = Gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
				Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
				if (st != GLEnum.FramebufferComplete) { Trace.TraceError("Shadow FBO incomplete: " + st); _shadowOk = false; return false; }
				return true;
			} catch (Exception e) { Trace.TraceError("Shadow FBO failed: " + e); _shadowOk = false; return false; }
		}

		// orthographic light view-projection (column-major), looking along the sun, covering an area in front of
		// the camera. Maps a world point so clip.xy∈[-1,1] over ±HALF and clip.z (=p.z after *0.5+0.5) over [0,1].
		private void BuildLightVP(Lua3DScene s) {
			const double DEPTH = 260.0; double HALF = s._shadowHalf;
			// centre on the player (SetShadowFocus) when given — the faked-ortho iso camera sits far back, so
			// camera+forward*38 lands behind the action and the map would miss the player entirely.
			double fxC, fyC, fzC;
			if (s._shadowFocusSet) { fxC = s._sfx; fyC = s._sfy; fzC = s._sfz; }
			else { fxC = s._camX + s._Fx * 38; fyC = s._camY + s._Fy * 38; fzC = s._camZ + s._Fz * 38; }
			_lightFx = fxC; _lightFz = fzC;   // remember the focus so the depth pass can skip far-away merge buckets
			double lx = s._sunX, ly = s._sunY, lz = s._sunZ;
			double ll = Math.Sqrt(lx * lx + ly * ly + lz * lz); if (ll < 1e-6) { lx = 0; ly = 1; lz = 0; } else { lx /= ll; ly /= ll; lz /= ll; }
			double fX = -lx, fY = -ly, fZ = -lz;
			double ex = fxC + lx * DEPTH * 0.5, ey = fyC + ly * DEPTH * 0.5, ez = fzC + lz * DEPTH * 0.5;
			double upx = 0, upy = 1, upz = 0; if (Math.Abs(fY) > 0.95) { upx = 0; upy = 0; upz = 1; }
			double rX = fY * upz - fZ * upy, rY = fZ * upx - fX * upz, rZ = fX * upy - fY * upx;
			double rl = Math.Sqrt(rX * rX + rY * rY + rZ * rZ); rX /= rl; rY /= rl; rZ /= rl;
			double uX = rY * fZ - rZ * fY, uY = rZ * fX - rX * fZ, uZ = rX * fY - rY * fX;
			double sH = 1.0 / HALF, sD = 2.0 / DEPTH;
			double er = ex * rX + ey * rY + ez * rZ, eu = ex * uX + ey * uY + ez * uZ, ef = ex * fX + ey * fY + ez * fZ;
			var m = _lightVP;   // column-major (m[col*4+row])
			m[0] = (float)(rX * sH); m[1] = (float)(uX * sH); m[2] = (float)(fX * sD); m[3] = 0;
			m[4] = (float)(rY * sH); m[5] = (float)(uY * sH); m[6] = (float)(fY * sD); m[7] = 0;
			m[8] = (float)(rZ * sH); m[9] = (float)(uZ * sH); m[10] = (float)(fZ * sD); m[11] = 0;
			m[12] = (float)(-er * sH); m[13] = (float)(-eu * sH); m[14] = (float)(-ef * sD - 1.0); m[15] = 1;
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
			if (_rttColor.TryGetValue(id, out var rttTex)) return rttTex;   // live GPU reflection/portal target (RenderToTexture)
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
			return (o.Kind == 0 || o.Kind == 2) && o.N > 0 && o.Visible && o.Pass == 0 && o.Transform == null && o.Lit
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

		private static void XfPt(double[] t, double px, double py, double pz, out double x, out double y, out double z) {
			if (t == null) { x = px; y = py; z = pz; return; }
			x = t[0] * px + t[1] * py + t[2] * pz + t[3];
			y = t[4] * px + t[5] * py + t[6] * pz + t[7];
			z = t[8] * px + t[9] * py + t[10] * pz + t[11];
		}
		private void BuildSprites(Lua3DScene s) {
			_sprVerts.Clear(); _sprBatches.Clear();
			foreach (var o in s._objects.Values) {
				if ((o.Kind != 3 && o.Kind != 4) || o.N <= 0 || !o.Visible) continue;
				var d = o.D; var t = o.Transform;
				int curCut = -1; uint curTex = uint.MaxValue; int start = _sprVerts.Count / STRIDE;
				void Flush() { int cnt = _sprVerts.Count / STRIDE - start; if (cnt > 0) _sprBatches.Add(new SprBatch { Obj = o, Cutout = curCut, Tex = curTex, Start = start, Count = cnt, Pass = o.Pass }); start = _sprVerts.Count / STRIDE; }
				void Use(uint tex, int cut) { if (tex != curTex || cut != curCut) { Flush(); curTex = tex; curCut = cut; } }
				if (o.Kind == 4) {
					// sprite drawn on an EXPLICIT world quad (4 corners) — e.g. a shadow draped over the terrain
					for (int i = 0; i < o.N; i++) {
						int k = i * 14; uint gt = SpriteFor(s, (int)d[k + 12]); if (gt == 0) continue;
						int cut = d[k + 13] != 0 ? 1 : 0; Use(gt, cut);
						XfPt(t, d[k], d[k + 1], d[k + 2], out double q0x, out double q0y, out double q0z);
						XfPt(t, d[k + 3], d[k + 4], d[k + 5], out double q1x, out double q1y, out double q1z);
						XfPt(t, d[k + 6], d[k + 7], d[k + 8], out double q2x, out double q2y, out double q2z);
						XfPt(t, d[k + 9], d[k + 10], d[k + 11], out double q3x, out double q3y, out double q3z);
						Vtx(_sprVerts, q0x, q0y, q0z, 0, 1, 1, 0, 0, 0); Vtx(_sprVerts, q1x, q1y, q1z, 1, 1, 1, 0, 0, 0); Vtx(_sprVerts, q2x, q2y, q2z, 1, 0, 1, 0, 0, 0);
						Vtx(_sprVerts, q0x, q0y, q0z, 0, 1, 1, 0, 0, 0); Vtx(_sprVerts, q2x, q2y, q2z, 1, 0, 1, 0, 0, 0); Vtx(_sprVerts, q3x, q3y, q3z, 0, 0, 1, 0, 0, 0);
					}
					Flush(); continue;
				}
				for (int i = 0; i < o.N; i++) {
					int k = i * 8; uint gt = SpriteFor(s, (int)d[k + 5]); if (gt == 0) continue;
					double w = d[k + 3], hgt = d[k + 4]; int bb = (int)d[k + 6]; int cut = d[k + 7] != 0 ? 1 : 0;
					double cx, cy, cz;
					if (t == null) { cx = d[k]; cy = d[k + 1]; cz = d[k + 2]; }
					else { cx = t[0] * d[k] + t[1] * d[k + 1] + t[2] * d[k + 2] + t[3]; cy = t[4] * d[k] + t[5] * d[k + 1] + t[6] * d[k + 2] + t[7]; cz = t[8] * d[k] + t[9] * d[k + 1] + t[10] * d[k + 2] + t[11]; }
					double rx, ry, rz, ux, uy, uz;
					if (bb == 2) { rx = s._Rx; ry = s._Ry; rz = s._Rz; ux = s._Ux; uy = s._Uy; uz = s._Uz; }
					else if (bb == 1) { rx = s._Rx; ry = 0; rz = s._Rz; double rl = Math.Sqrt(rx * rx + rz * rz); if (rl > 1e-6) { rx /= rl; rz /= rl; } ux = 0; uy = 1; uz = 0; }
					else if (bb == 3) { rx = s._GsLz; ry = 0; rz = -s._GsLx; ux = s._GsLx * s._GsLen; uy = 0; uz = s._GsLz * s._GsLen; }   // ground-flat cast shadow: lies on XZ, stretched along the light
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

		// Build SUN-FACING silhouette quads for the upright billboard sprites (characters/NPCs/props), used only
		// as shadow-map casters. A standing billboard is camera-facing in the main pass, which would present an
		// edge-on or skewed outline to the sun (a thin/warped, orbit-dependent shadow). Here each sprite becomes a
		// vertical quad whose width axis is perpendicular to the sun's azimuth, so the sun sees its full outline
		// and the cast shadow keeps the character's shape and stretches away from the light. Grouped by texture
		// for the alpha-test discard in the depth-sprite program.
		private void BuildCasters(Lua3DScene s) {
			_casterVerts.Clear(); _casterBatches.Clear();
			double sdx = s._sunX, sdz = s._sunZ;
			double shl = Math.Sqrt(sdx * sdx + sdz * sdz);
			double rx, rz; if (shl > 1e-6) { rx = sdz / shl; rz = -sdx / shl; } else { rx = 1; rz = 0; }   // horizontal ⟂ to sun
			foreach (var o in s._objects.Values) {
				if (o.Kind != 3 || o.N <= 0 || !o.Visible || !o.CastShadow) continue;   // only billboard sprites (Kind 3); Kind 4 = flat decals; CastShadow=false skips UI markers/bubbles
				var d = o.D; var t = o.Transform;
				uint curTex = uint.MaxValue; int start = _casterVerts.Count / STRIDE;
				void Flush() { int cnt = _casterVerts.Count / STRIDE - start; if (cnt > 0) _casterBatches.Add(new CBatch { Tex = curTex, Start = start, Count = cnt }); start = _casterVerts.Count / STRIDE; }
				for (int i = 0; i < o.N; i++) {
					int k = i * 8; uint gt = SpriteFor(s, (int)d[k + 5]); if (gt == 0) continue;
					int bb = (int)d[k + 6]; if (bb != 1 && bb != 2) continue;   // only UPRIGHT billboards cast (skip ground-flat bb=3)
					double w = d[k + 3], hgt = d[k + 4];
					double cx, cy, cz;
					if (t == null) { cx = d[k]; cy = d[k + 1]; cz = d[k + 2]; }
					else { cx = t[0] * d[k] + t[1] * d[k + 1] + t[2] * d[k + 2] + t[3]; cy = t[4] * d[k] + t[5] * d[k + 1] + t[6] * d[k + 2] + t[7]; cz = t[8] * d[k] + t[9] * d[k + 1] + t[10] * d[k + 2] + t[11]; }
					if (gt != curTex) { Flush(); curTex = gt; }
					double hw = w * 0.5;
					double b0x = cx - rx * hw, b0z = cz - rz * hw, b1x = cx + rx * hw, b1z = cz + rz * hw;
					Vtx(_casterVerts, b0x, cy, b0z, 0, 1, 1, 0, 0, 0); Vtx(_casterVerts, b1x, cy, b1z, 1, 1, 1, 0, 0, 0); Vtx(_casterVerts, b1x, cy + hgt, b1z, 1, 0, 1, 0, 0, 0);
					Vtx(_casterVerts, b0x, cy, b0z, 0, 1, 1, 0, 0, 0); Vtx(_casterVerts, b1x, cy + hgt, b1z, 1, 0, 1, 0, 0, 0); Vtx(_casterVerts, b0x, cy + hgt, b0z, 0, 0, 1, 0, 0, 0);
				}
				Flush();
			}
			if (_casterVerts.Count > 0) {
				if (_casterVbo == 0) _casterVbo = Gl.GenBuffer();
				Gl.BindBuffer(BufferTargetARB.ArrayBuffer, _casterVbo);
				Gl.BufferData<float>(BufferTargetARB.ArrayBuffer, (ReadOnlySpan<float>)CollectionsMarshal.AsSpan(_casterVerts), BufferUsageARB.StreamDraw);
			}
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

			GatherObjects(s);

			// ── SUN SHADOW MAP: depth from the light's POV. Casters = merged terrain (opaque) + the character
			// sprites (alpha-tested silhouette). Skipped unless the scene opted in + is lit + has a sun.
			bool shadows = s._shadows && s._useLights && (s._sunR + s._sunG + s._sunB) > 0.0 && !s._rttPass && EnsureShadowFbo();
			if (shadows) {
				BuildLightVP(s);
				Gl.BindFramebuffer(FramebufferTarget.Framebuffer, _shadowFbo);
				Gl.Viewport(0, 0, SHADOW_SIZE, SHADOW_SIZE);
				Gl.Clear((uint)ClearBufferMask.DepthBufferBit);
				Gl.Enable(EnableCap.DepthTest); Gl.DepthFunc(DepthFunction.Lequal); Gl.DepthMask(true);
				Gl.Disable(EnableCap.CullFace); Gl.Disable(EnableCap.Blend);
				Gl.UseProgram(_progDepth); Gl.BindVertexArray(_vao);
				Gl.UniformMatrix4(_uDLightVP, 1, false, (ReadOnlySpan<float>)_lightVP);
				Gl.UniformMatrix4(_uDModel, 1, false, (ReadOnlySpan<float>)_ident);
				// only the geometry NEAR the player matters for the shadow box → skip merge buckets outside it.
				// This is the key perf fix for big worlds (the 400x400 volcano was drawn whole into the map each
				// frame). Buckets are 32u; keep any within HALF + a bucket of the focus (a point beyond samples
				// "outside the map" and reads as lit anyway, so culling is visually consistent).
				double cullR = s._shadowHalf + BS, cullR2 = cullR * cullR;
				foreach (var kv in _merge) {
					if (kv.Value.Count == 0) continue;
					double bcx = (kv.Key.bx + 0.5) * BS - _lightFx, bcz = (kv.Key.bz + 0.5) * BS - _lightFz;
					if (bcx * bcx + bcz * bcz > cullR2) continue;
					Bind3D(kv.Value.Vbo); Gl.DrawArrays(PrimitiveType.Triangles, 0, (uint)kv.Value.Count);
				}
				BuildCasters(s);   // sun-facing silhouette quads for the characters (camera-facing sprites cast wrong)
				if (_casterVerts.Count > 0) {
					Gl.UseProgram(_progDepthSpr); Gl.BindVertexArray(_vao); Bind3D(_casterVbo);
					Gl.UniformMatrix4(_uDSLightVP, 1, false, (ReadOnlySpan<float>)_lightVP);
					Gl.Uniform1(_uDSTex, 0); Gl.ActiveTexture(TextureUnit.Texture0);
					foreach (var bt in _casterBatches) { Gl.BindTexture(TextureTarget.Texture2D, bt.Tex); Gl.DrawArrays(PrimitiveType.Triangles, bt.Start, (uint)bt.Count); }
				}
			}

			Gl.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
			Gl.Viewport(0, 0, (uint)s._w, (uint)s._h);
			Gl.Clear((uint)ClearBufferMask.DepthBufferBit);
			Gl.Enable(EnableCap.DepthTest); Gl.DepthFunc(DepthFunction.Lequal); Gl.Disable(EnableCap.CullFace);
			BindWorldProgram(shadows); Gl.BindVertexArray(_vao);
			SetFrameUniforms(s);
			if (_worldShadow) {   // only the shadow variant reads these: the light matrix + the depth map on unit 1
				Gl.UniformMatrix4(_uLightVP, 1, false, (ReadOnlySpan<float>)_lightVP);
				Gl.Uniform1(_uShadowTexel, 1.0f / SHADOW_SIZE);
				Gl.ActiveTexture(TextureUnit.Texture1); Gl.BindTexture(TextureTarget.Texture2D, _shadowTex); Gl.Uniform1(_uShadow, 1);
				Gl.ActiveTexture(TextureUnit.Texture0);
			}

			int litScene = s._useLights ? 1 : 0;
			DrawWorld(s, litScene);

			// overlay pass: viewmodels over a freshly-cleared depth buffer → always on top of the world
			if (_overlay.Count > 0) {
				BindWorldProgram(false); Gl.BindVertexArray(_vao);   // viewmodels: no world shadows
				Gl.Clear((uint)ClearBufferMask.DepthBufferBit);
				Gl.Enable(EnableCap.DepthTest); Gl.DepthMask(true); Gl.Disable(EnableCap.Blend);
				foreach (var o in _overlay) DrawObj(s, o, litScene);
			}

			// diorama post pass ('diorama' grade): tilt-shift + colour grade. Copy canvas->temp, then
			// grade temp->canvas. No depth, cheap. Only diorama scenes opt in, so others pay nothing.
			if (s._diorama && !s._rttPass) {
				EnsureFxaaTarget(s);
				Gl.Disable(EnableCap.DepthTest); Gl.DepthMask(false); Gl.Disable(EnableCap.Blend);
				Gl.UseProgram(_progPost); Gl.BindVertexArray(_vaoPost);     // copy canvas -> temp
				Gl.Uniform1(_postTex, 0); Gl.Uniform2(_postRes, (float)s._w, (float)s._h); Gl.Uniform1(_postMode, 0);
				Gl.ActiveTexture(TextureUnit.Texture0); Gl.BindTexture(TextureTarget.Texture2D, s._canvas.Pointer);
				Gl.BindFramebuffer(FramebufferTarget.Framebuffer, _fxaaFbo);
				Gl.DrawArrays(PrimitiveType.Triangles, 0, 3);
				Gl.UseProgram(_progDiorama);                                    // grade temp -> canvas
				Gl.Uniform1(_hTex, 0); Gl.Uniform2(_hRes, (float)s._w, (float)s._h);
				Gl.Uniform1(_hTilt, (float)s._hdTilt); Gl.Uniform1(_hSat, (float)s._hdSat); Gl.Uniform1(_hVig, (float)s._hdVig);
				Gl.Uniform1(_hBloom, (float)s._hdBloom);
				Gl.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
				Gl.BindTexture(TextureTarget.Texture2D, _fxaaTex);
				Gl.DrawArrays(PrimitiveType.Triangles, 0, 3);
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

			// toon OUTLINE post pass (3D characters): copy canvas→temp with alpha, ink temp→canvas
			if (s._outlineTh > 0 && !s._rttPass) {
				EnsureFxaaTarget(s);
				Gl.Disable(EnableCap.DepthTest); Gl.DepthMask(false); Gl.Disable(EnableCap.Blend);
				Gl.UseProgram(_progPost); Gl.BindVertexArray(_vaoPost);
				Gl.Uniform1(_postTex, 0); Gl.Uniform2(_postRes, (float)s._w, (float)s._h); Gl.Uniform1(_postMode, 2);
				Gl.ActiveTexture(TextureUnit.Texture0); Gl.BindTexture(TextureTarget.Texture2D, s._canvas.Pointer);
				Gl.BindFramebuffer(FramebufferTarget.Framebuffer, _fxaaFbo);
				Gl.DrawArrays(PrimitiveType.Triangles, 0, 3);
				Gl.UseProgram(_progOutline);
				Gl.Uniform1(_oTex, 0); Gl.Uniform2(_oRes, (float)s._w, (float)s._h);
				Gl.Uniform3(_oColor, (float)(s._outlineR / 255.0), (float)(s._outlineG / 255.0), (float)(s._outlineB / 255.0));
				Gl.Uniform1(_oThick, (float)s._outlineTh);
				Gl.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
				Gl.BindTexture(TextureTarget.Texture2D, _fxaaTex);
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

		// Split the per-frame object scan + the world draw out of Render so the off-screen reflection
		// pass (RenderToTexture) can reuse them with a different camera/target.
		private void GatherObjects(Lua3DScene s) {
			_opaque.Clear(); _trans.Clear(); _overlay.Clear(); _screenTex.Clear();
			foreach (var o in s._objects.Values) {
				if (o.Kind < 0 || o.Kind == 3 || o.Kind == 4 || o.N <= 0) continue;   // 3/4 = sprites, drawn in the sprite pass
				if (o.ScreenTex) { if (o.Visible && !s._rttPass) _screenTex.Add(o); continue; }   // mirror/portal surfaces: own draw (never inside an RTT)
				if (StaticMerged(o)) continue;
				if (o.Overlay) { if (o.Visible) _overlay.Add(o); continue; }   // viewmodels: own pass
				if (!Drawable(s, o)) continue;
				if (o.Pass == 0) _opaque.Add(o); else _trans.Add(o);
			}
			_trans.Sort((a, b) => Dist(s, b).CompareTo(Dist(s, a)));
		}

		// Opaque (merged + per-object + screen-tex + sprites) then transparent (glass writes depth, then
		// sprites/particles). Assumes the program/VAO are bound and SetFrameUniforms already ran.
		private void DrawWorld(Lua3DScene s, int litScene) {
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

			// Transparent pass, back-to-front, no depth write. ORDER matters: soft sprites + particles are
			// drawn BEFORE the alpha-blended glass objects, so a glass pane drawn afterward blends OVER the
			// stuff behind it (death fog, bolts) — they read as dimmed THROUGH the glass instead of either
			// popping in front of it (no ordering) or vanishing (if glass wrote depth). Glass panes are
			// back-to-front sorted among themselves so a nearer pane layers over a farther one.
			Gl.Enable(EnableCap.Blend); Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
			Gl.DepthMask(false);
			Bind3D(_spriteVbo);
			foreach (var bt in _sprBatches) if (bt.Pass == 1) DrawSprite(bt);
			DrawParticles(s);   // additive/alpha billboards, depth-tested against opaque (switches to the particle program)
			// glass last, over the particles/sprites behind it — rebind the active world program/VAO + blend
			// state that DrawParticles left on the particle program (_u* are still this frame's variant).
			Gl.UseProgram(_worldShadow ? _progShadow : _prog); Gl.BindVertexArray(_vao);
			Gl.Enable(EnableCap.Blend); Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha); Gl.DepthMask(false);
			foreach (var o in _trans) DrawObj(s, o, litScene);
		}

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
			if (rw < 1) rw = 1; if (rh < 1) rh = 1;
			Span<int> vp = stackalloc int[4]; Gl.GetInteger(GLEnum.Viewport, vp);
			uint prevFbo = (uint)Gl.GetInteger(GLEnum.FramebufferBinding);

			if (_rttW != rw || _rttH != rh) {                       // size changed → drop all targets
				foreach (var t in _rttColor.Values) Gl.DeleteTexture(t); _rttColor.Clear();
				foreach (var f in _rttFbos.Values) Gl.DeleteFramebuffer(f); _rttFbos.Clear();
				foreach (var d in _rttDepth.Values) Gl.DeleteRenderbuffer(d); _rttDepth.Clear();
				_rttW = rw; _rttH = rh;
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
				_rttColor[texId] = color; _rttFbos[texId] = fbo; _rttDepth[texId] = depth;
			}

			BuildSprites(s); BuildParticles(s);
			long sig = ComputeStaticSig(s);
			if (sig != _staticSig) { RebuildMerged(s); _staticSig = sig; }
			GatherObjects(s);

			Gl.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);
			Gl.Viewport(0, 0, (uint)rw, (uint)rh);
			Gl.ClearColor(clr / 255f, clg / 255f, clb / 255f, 1f);
			Gl.Clear((uint)(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));
			Gl.Enable(EnableCap.DepthTest); Gl.DepthFunc(DepthFunction.Lequal); Gl.Disable(EnableCap.CullFace);
			BindWorldProgram(false); Gl.BindVertexArray(_vao);   // mirror/portal RTT never casts/receives shadows
			SetFrameUniforms(s);
			DrawWorld(s, s._useLights ? 1 : 0);
			// the colour texture lives in _rttColor; DrawScreenTex's TexFor returns it directly for texId,
			// so the mirror/portal surface samples this live GPU target with no CPU pixel-upload path.

			Gl.BindFramebuffer(FramebufferTarget.Framebuffer, prevFbo);
			Gl.Viewport(vp[0], vp[1], (uint)vp[2], (uint)vp[3]);
			Gl.UseProgram(0); Gl.BindVertexArray(0);
			Gl.ActiveTexture(TextureUnit.Texture0); Gl.BindTexture(TextureTarget.Texture2D, 0);
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
			// (the shadow map + light matrix are bound by the main pass only when the shadow variant is active;
			// the default variant has no shadow uniforms at all, so there's nothing to reset here.)
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
			Gl.Uniform1(_uAlpha, o.A / 255.0f); Gl.Uniform1(_uCutout, 1);   // alpha-test: free for opaque texels, makes VRM hair/lashes cut out
			Gl.Uniform1(_uLit, (litScene == 1 && o.Lit) ? 1 : 0);
			// per-object depth bias (polygon offset): lets a coplanar decal/marking sit cleanly on its surface
			// instead of z-fighting it (road stripes on the road, ground shadows, etc.).
			if (o.DepthBias != 0f) { Gl.Enable(EnableCap.PolygonOffsetFill); Gl.PolygonOffset(o.DepthBias, o.DepthBias); }
			float fr = (float)(o.R / 255.0), fg = (float)(o.G / 255.0), fb = (float)(o.B / 255.0);
			foreach (var bt in c.Batches) {
				Gl.Uniform1(_uMode, bt.Mode);
				if (bt.Mode == 0) Gl.Uniform3(_uFlat, fr, fg, fb);
				else { Gl.ActiveTexture(TextureUnit.Texture0); Gl.BindTexture(TextureTarget.Texture2D, bt.Tex); }
				Gl.DrawArrays(PrimitiveType.Triangles, bt.Start, (uint)bt.Count);
			}
			if (o.DepthBias != 0f) Gl.Disable(EnableCap.PolygonOffsetFill);
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
			if (_shadowFbo != 0) { Gl.DeleteFramebuffer(_shadowFbo); _shadowFbo = 0; }
			if (_shadowTex != 0) { Gl.DeleteTexture(_shadowTex); _shadowTex = 0; }
			if (_fxaaFbo != 0) { Gl.DeleteFramebuffer(_fxaaFbo); _fxaaFbo = 0; }
			if (_fxaaTex != 0) { Gl.DeleteTexture(_fxaaTex); _fxaaTex = 0; }
			foreach (var f in _rttFbos.Values) Gl.DeleteFramebuffer(f); _rttFbos.Clear();
			foreach (var d in _rttDepth.Values) Gl.DeleteRenderbuffer(d); _rttDepth.Clear();
			foreach (var t in _rttColor.Values) Gl.DeleteTexture(t);
			_rttColor.Clear(); _rttW = _rttH = 0;
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
			if (_casterVbo != 0) Gl.DeleteBuffer(_casterVbo);
			if (_vbo2d != 0) Gl.DeleteBuffer(_vbo2d);
			if (_partVbo != 0) Gl.DeleteBuffer(_partVbo);
			if (_prog != 0) Gl.DeleteProgram(_prog);
			if (_progShadow != 0) Gl.DeleteProgram(_progShadow);
			if (_progDepth != 0) Gl.DeleteProgram(_progDepth);
			if (_progDepthSpr != 0) Gl.DeleteProgram(_progDepthSpr);
			if (_prog2d != 0) Gl.DeleteProgram(_prog2d);
			if (_progP != 0) Gl.DeleteProgram(_progP);
			if (_progPost != 0) Gl.DeleteProgram(_progPost);
			if (_progDiorama != 0) Gl.DeleteProgram(_progDiorama);
			_vao = _vao2d = _vaoP = _vaoPost = _spriteVbo = _casterVbo = _vbo2d = _partVbo = 0;
			_prog = _progShadow = _progDepth = _progDepthSpr = _prog2d = _progP = _progPost = _progDiorama = 0;
			_init = false;
		}
	}
}
