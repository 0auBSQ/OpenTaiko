
precision highp float;
in vec2 vUV; in float vShade; in float vCamZ; in vec3 vWorld; in vec3 vNormal;
uniform int uMode;            // 0 flat, 1 textured, 2 sprite, 3 screen-tex mirror
uniform sampler2D uTex;
uniform vec3 uTint; uniform float uAlpha; uniform vec3 uFlat; uniform int uCutout;
uniform vec4 uFlatCol;         // per-object flat albedo OVERRIDE (rgb; w>0.5 = active) - recolours a routed GLB part
uniform int uRsh;             // mirror/portal RTT is rendered at 1/2^uRsh res; sample by gl_FragCoord>>uRsh
uniform int uFog; uniform vec3 uFogCol; uniform float uFogStart; uniform float uFogInv;
uniform int uLit; uniform vec3 uSunDir, uSunCol, uAmb;
uniform int uNumLights; uniform vec4 uLightPosR[64]; uniform vec3 uLightCol[64];
uniform vec3 uCam;            // eye (wetness specular); same value the VS binds
uniform vec3 uEmissive;       // per-object emissive add: col += base * uEmissive (>1 feeds bloom)
uniform float uWet;           // global 0..1: darken lit diffuse + sun specular sheen (rain)
uniform float uTime;          // shader clock (ripple / custom post share it)
uniform vec3 uRipple;         // screen-tex distortion: amp(px), world freq, speed (x=0 off)
uniform int uRttFlip;         // 1 = the RTT came from a GPU FBO (bottom-up rows) - flip the sample
uniform vec4 uClip;           // reflection clip plane: discard when dot(wp,xyz)+w < 0 (w <= -9e29 = off)
uniform vec2 uFres;           // screen-tex fresnel: x = F0, y = 1 enable (0 = legacy shade reflectivity)
uniform vec3 uFlat2;          // screen-tex water deep colour (vShade lerps uFlat -> uFlat2 when enabled)
uniform int uDeepOn;          // 1 = uFlat2 depth tint active
uniform sampler2D uTex2, uTex3, uTex4, uSplat;   // terrain splat: 3 extra layers + the weight image
uniform vec2 uSplatSize;      // splat world coverage (world.xz -> splat uv)
uniform int uNormOn;          // 1 = the bound texture has a normal map (SetTextureNormalMap) on uNormTex
uniform sampler2D uNormTex;
uniform float uTexScale;      // world units -> layer uv repeats
uniform vec4 uUVAnim;         // textured surfaces: xy = uv scroll per second of uTime, z = wobble amplitude, w = its world frequency
out vec4 frag;
#ifdef SHADOWS
#ifdef EXT
// EXT: filtered compares, fade toward the map border
uniform mat4 uLightVP; uniform highp sampler2DShadow uShadowCmp; uniform float uShadowTexel;
uniform vec2 uShadowF;        // radius (texels), fade (share of the half extent)
float sunShadow(vec3 wp, vec3 N){
    vec4 lp = uLightVP * vec4(wp + N * 0.10, 1.0);
    vec3 p = lp.xyz / lp.w * 0.5 + 0.5;
    vec2 e = abs(p.xy * 2.0 - 1.0);
    float edge = max(e.x, e.y);
    if (p.z > 1.0 || edge >= 1.0) return 1.0;
    float ndl = max(dot(N, uSunDir), 0.0);
    float t = p.z - max(0.00012, 0.0006 * (1.0 - ndl));
    float r = uShadowF.x * uShadowTexel;
    float sh = texture(uShadowCmp, vec3(p.xy + vec2(-0.75, -0.25) * r, t))
             + texture(uShadowCmp, vec3(p.xy + vec2( 0.25, -0.75) * r, t))
             + texture(uShadowCmp, vec3(p.xy + vec2( 0.75,  0.25) * r, t))
             + texture(uShadowCmp, vec3(p.xy + vec2(-0.25,  0.75) * r, t));
    sh *= 0.25;
    float fade = uShadowF.y > 0.0 ? clamp((edge - (1.0 - uShadowF.y)) / uShadowF.y, 0.0, 1.0) : 0.0;
    return mix(sh, 1.0, fade);
}
#else
uniform mat4 uLightVP; uniform sampler2D uShadow; uniform float uShadowTexel;
float sunShadow(vec3 wp, vec3 N){
    // NORMAL-OFFSET sampling (~1.6 shadow texels along the surface normal) carries most of the
    // acne fight, so the DEPTH bias can be ~5x smaller - the old 0.0008..0.0026 range was 0.2-0.7
    // world units of light-space depth, which pushed every contact shadow visibly away from the
    // object's base (the gap-between-object-and-shadow bug).
    vec3 wpo = wp + N * 0.10;
    vec4 lp = uLightVP * vec4(wpo, 1.0);
    vec3 p = lp.xyz / lp.w * 0.5 + 0.5;
    if (p.z > 1.0 || p.x < 0.0 || p.x > 1.0 || p.y < 0.0 || p.y > 1.0) return 1.0;   // outside the map -> lit (ASCII only! ANGLE rejects bytes>127 even in comments)
    float ndl = max(dot(N, uSunDir), 0.0);
    float bias = max(0.00012, 0.0006 * (1.0 - ndl)); float t = p.z - bias; float ts = uShadowTexel; float sh = 0.0;
    sh += step(t, texture(uShadow, p.xy + vec2(-0.5, -0.5) * ts).r);
    sh += step(t, texture(uShadow, p.xy + vec2( 0.5, -0.5) * ts).r);
    sh += step(t, texture(uShadow, p.xy + vec2(-0.5,  0.5) * ts).r);
    sh += step(t, texture(uShadow, p.xy + vec2( 0.5,  0.5) * ts).r);
    return sh * 0.25;
}
#endif
#endif
#ifdef EXT
uniform vec4 uFogSky[6];      // 0 horizon+switch, 1 zenith+rise, 2 below+band, 3 glow+focus, 4 glow dir+height fall, 5 terrain
vec3 fogColor(vec3 wp){
    if (uFogSky[0].w <= 0.0) return uFogCol;
    vec3 d = normalize(wp - uCam);
    vec3 c = mix(uFogSky[0].rgb, uFogSky[1].rgb, 1.0 - exp(-max(d.y, 0.0) * uFogSky[1].w));
    c = mix(uFogSky[2].rgb, c, smoothstep(-uFogSky[2].w, uFogSky[2].w, d.y));
    float g = max(dot(d.xz / max(length(d.xz), 1e-4), uFogSky[4].xz), 0.0);
    return c + uFogSky[3].rgb * (pow(g, uFogSky[3].w) * exp(-abs(d.y) * uFogSky[4].w));
}
uniform float uSprLit;        // 0..1 scene light share
uniform float uSprNear;       // dissolve distance, 0 = off
uniform float uCel;
uniform highp sampler2D uLightGrid;   // per cell: light index + 1, 0 ends the list
uniform vec4 uLightGridXf;            // corner x, z; cells per unit; cells per side (0 = no grid)
uniform int uLightGlobal;             // leading unranged lights (in no cell)
int lightSlots(vec3 wp, out vec4 ga, out vec4 gb){
    ga = vec4(0.0); gb = vec4(0.0);
    if (uLightGridXf.w <= 0.0) return uNumLights;
    int n = int(uLightGridXf.w);
    ivec2 c = ivec2(floor((wp.xz - uLightGridXf.xy) * uLightGridXf.z));
    if (c.x < 0 || c.y < 0 || c.x >= n || c.y >= n) return uLightGlobal;
    ga = texelFetch(uLightGrid, c, 0) * 255.0;
    if (ga.x < 0.5) return uLightGlobal;
    if (ga.w > 0.5) gb = texelFetch(uLightGrid, ivec2(c.x, c.y + n), 0) * 255.0;   // more than four: second texel
    return uLightGlobal + 8;
}
// -1 = no more in the cell
int lightOf(int k, vec4 ga, vec4 gb){
    if (uLightGridXf.w <= 0.0 || k < uLightGlobal) return k;
    int j = k - uLightGlobal;
    float v = j == 0 ? ga.x : j == 1 ? ga.y : j == 2 ? ga.z : j == 3 ? ga.w : j == 4 ? gb.x : j == 5 ? gb.y : j == 6 ? gb.z : gb.w;
    return int(v + 0.5) - 1;
}
vec3 spriteShade(vec3 wp){
    float sh = 1.0;
    vec3 plain = vec3(1.0);
#ifdef SHADOWS
    sh = sunShadow(wp + uSunDir * 0.6, vNormal);
    plain = vec3(0.55 + 0.45 * sh);
#endif
    vec3 l = uAmb * 1.15 + uSunCol * (0.62 * sh);
    vec4 ga, gb; int ns = lightSlots(wp, ga, gb);
    for (int k = 0; k < ns; k++){
        int i = lightOf(k, ga, gb); if (i < 0) break;
        vec3 dp = uLightPosR[i].xyz - wp; float range = uLightPosR[i].w;
        float d2 = dot(dp, dp);
        if (range > 0.0){
            if (d2 >= range*range) continue;
            float f = 1.0 - sqrt(d2)/range; l += uLightCol[i] * (f*f*0.8);
        } else l += uLightCol[i] * (0.8 / (d2 + 1.0));
    }
    return mix(plain, l, uSprLit);
}
#endif
vec3 lightAt(vec3 N, vec3 wp){
    float e = sqrt(max(vShade, 0.0));
#ifdef SHADOWS
    float shf = sunShadow(wp, N);
#else
    float shf = 1.0;
#endif
#ifdef EXT
    float sd = max(dot(N, uSunDir), 0.0) * 3.0, sf = floor(sd);   // uCel: blend toward three steps
    vec3 l = e * (uAmb + mix(sd / 3.0, (sf + smoothstep(0.30, 0.70, sd - sf)) / 3.0, uCel) * uSunCol * shf);
#else
    vec3 l = e * (uAmb + max(dot(N, uSunDir), 0.0) * uSunCol * shf);
#endif
#ifdef EXT
    vec4 ga, gb; int ns = lightSlots(wp, ga, gb);
    for (int k = 0; k < ns; k++){
        int i = lightOf(k, ga, gb); if (i < 0) break;
#else
    for (int i = 0; i < uNumLights; i++){
#endif
        vec3 dp = uLightPosR[i].xyz - wp; float range = uLightPosR[i].w;
        float d2 = dot(dp, dp);
        if (range > 0.0){
            if (d2 >= range*range) continue;
            float dl = max(sqrt(d2), 1e-4); float ndl = max(dot(N, dp/dl), 0.0); if (ndl <= 0.0) continue;
            float f = 1.0 - dl/range; l += uLightCol[i] * (f*f*ndl);
        } else {
            float inv = inversesqrt(max(d2, 1e-6)); float ndl = max(dot(N, dp*inv), 0.0); if (ndl <= 0.0) continue;
            l += uLightCol[i] * (ndl / (d2 + 1.0));
        }
    }
    return l;
}
void main(){
    if (uClip.w > -8.9e29 && dot(vWorld, uClip.xyz) + uClip.w < 0.0) discard;   // reflection clip plane
    if (uMode == 3){   // screen-texture mirror: sample the RTT by screen pixel, blend over the base colour
        ivec2 ts = textureSize(uTex, 0);
        ivec2 fc = ivec2(int(gl_FragCoord.x) >> uRsh, int(gl_FragCoord.y) >> uRsh);
        if (uRttFlip == 1) fc.y = ts.y - 1 - fc.y;   // GPU-FBO capture: rows are bottom-up vs the CPU upload
        if (uRipple.x > 0.0){   // water: wobble the sample point with two crossed world-space waves
            vec2 ro = vec2(sin(vWorld.x * uRipple.y + uTime * uRipple.z) + sin(vWorld.z * uRipple.y * 1.31 + uTime * uRipple.z * 0.83),
                           cos(vWorld.z * uRipple.y + uTime * uRipple.z * 0.91) + cos(vWorld.x * uRipple.y * 1.73 + uTime * uRipple.z * 1.13));
            fc += ivec2(ro * uRipple.x);
        }
        ivec2 mp = clamp(fc, ivec2(0), ts - 1);
        vec3 m = texelFetch(uTex, mp, 0).rgb;
        // reflection amount: legacy = tint * per-vertex shade; fresnel mode = per-pixel Schlick
        // (grazing views mirror fully, top-down shows the water colour - the physical look)
        vec3 fm = uTint * vShade;
        if (uFres.y > 0.5){
            vec3 Vv = normalize(uCam - vWorld);
            float cosT = max(dot(Vv, normalize(vNormal)), 0.0);
            float F = uFres.x + (1.0 - uFres.x) * pow(1.0 - cosT, 5.0);
            fm = uTint * clamp(F * 1.15 + 0.08, 0.0, 1.0);   // small floor so calm water still mirrors a touch
        }
        // base colour under the mirror: flat, or the depth tint (shallow uFlat -> deep uFlat2 by vShade)
        vec3 baseC = (uDeepOn == 1) ? mix(uFlat, uFlat2, clamp(vShade, 0.0, 1.0)) : uFlat;
        vec3 col = m * fm + baseC * (1.0 - fm);
        if (uFog == 1){ float f = clamp((vCamZ - uFogStart) * uFogInv, 0.0, 1.0); col = mix(col, uFogCol, f); }
        frag = vec4(col, uAlpha); return;
    }
    if (uMode == 2){ vec4 t = texture(uTex, vUV); vec4 c = vec4(t.rgb * uTint, t.a * uAlpha); if (uCutout == 1 && c.a < 0.5) discard;
#ifdef EXT
        if (uSprNear > 0.0 && vCamZ < uSprNear && (vCamZ / uSprNear - 0.33) * 1.5 <= fract(sin(dot(floor(gl_FragCoord.xy), vec2(12.9898, 78.233))) * 43758.5453)) discard;
        c.rgb *= spriteShade(vWorld);
#else
#ifdef SHADOWS
        // characters RECEIVE the sun shadow (a wall's shade dims them). Sample 0.6 units TOWARD the
        // sun: the sprite's own sun-facing caster quad passes through its centre, so sampling at the
        // fragment itself made every character shade ITSELF; real occluders sit further than 0.6.
        c.rgb *= (0.55 + 0.45 * sunShadow(vWorld + uSunDir * 0.6, vNormal));
#endif
#endif
#ifdef EXT
        if (uFog == 1){ float f = clamp((vCamZ - uFogStart) * uFogInv, 0.0, 1.0); if (f > 0.0) c.rgb = mix(c.rgb, fogColor(vWorld), f); } frag = c; return; }
#else
        if (uFog == 1){ float f = clamp((vCamZ - uFogStart) * uFogInv, 0.0, 1.0); c.rgb = mix(c.rgb, uFogCol, f); } frag = c; return; }
#endif
    vec3 base;
    vec2 uv = vUV + uUVAnim.xy * uTime;   // scrolled and wobbled uv (ObjSetUVScroll / ObjSetUVWarp; 0 = vUV)
    if (uUVAnim.z > 0.0) uv += uUVAnim.z * vec2(sin(vWorld.z * uUVAnim.w + uTime * 1.3), cos(vWorld.x * uUVAnim.w + uTime * 1.1));
    if (uMode == 0){ base = uFlat; }
    else if (uMode == 5){
        // TERRAIN SPLAT: blend the four layer textures per pixel by the (linear-filtered) weight
        // image - painted roads/beaches feather smoothly instead of cutting at quad edges
        vec2 luv = vWorld.xz * uTexScale;
        vec4 w = texture(uSplat, vWorld.xz / uSplatSize);
        float ws = max(w.r + w.g + w.b + w.a, 1e-4);
        base = (texture(uTex, luv).rgb * w.r + texture(uTex2, luv).rgb * w.g
              + texture(uTex3, luv).rgb * w.b + texture(uTex4, luv).rgb * w.a) / ws;
        base *= uTint;
    }
    else { vec4 t = texture(uTex, uv); if (t.a < 0.5) discard; base = t.rgb * uTint; }
    if (uFlatCol.a > 0.5) base = uFlatCol.rgb;   // flat albedo override: recolour a routed part (still shades)
    vec3 N = normalize(vNormal);
    float relief = 1.0;
    if (uNormOn == 1 && uMode == 1){
        // the normal map, turned onto the surface: the surface frame comes from the screen derivatives of
        // the position and the uv (the vertices carry no tangents); B runs down the image like the map's green
        vec3 m = texture(uNormTex, uv).rgb * 2.0 - 1.0;
        vec3 dp1 = dFdx(vWorld), dp2 = dFdy(vWorld); vec2 du1 = dFdx(uv), du2 = dFdy(uv);
        vec3 c2 = cross(dp2, N), c1 = cross(N, dp1);
        vec3 T = c2 * du1.x + c1 * du2.x, B = c2 * du1.y + c1 * du2.y;
        float k = inversesqrt(max(max(dot(T, T), dot(B, B)), 1e-20));
        T *= k; B *= k;
        vec3 Nm = normalize(T * m.x + B * m.y + N * m.z);
        // relief shade: a light fixed to the surface, from the image's upper left (the same on every wall)
        vec3 Ls = normalize(-0.35 * T - 0.45 * B + 0.82 * N);
        relief = clamp(dot(Nm, Ls) / max(dot(N, Ls), 0.2), 0.6, 1.4);
        N = Nm;
    }
    vec3 col = (uLit == 1) ? base * lightAt(N, vWorld) * mix(1.0, relief, 0.5) : base * vShade * relief;
    if (uWet > 0.0 && uLit == 1){   // rain-wet surfaces: darker diffuse + a sun specular sheen (ALU only)
        col *= 1.0 - 0.30 * uWet;
        vec3 N = normalize(vNormal);
        vec3 V = normalize(uCam - vWorld);
        vec3 H = normalize(V + uSunDir);
        col += uSunCol * (pow(max(dot(N, H), 0.0), 48.0) * 0.7 * uWet * sqrt(max(vShade, 0.0)));
    }
    col += base * uEmissive;
#ifdef EXT
    if (uFog == 1){ float f = clamp((vCamZ - uFogStart) * uFogInv, 0.0, 1.0); if (f > 0.0) col = mix(col, fogColor(vWorld), f); }
#else
    if (uFog == 1){ float f = clamp((vCamZ - uFogStart) * uFogInv, 0.0, 1.0); col = mix(col, uFogCol, f); }
#endif
    frag = vec4(col, uAlpha);
}