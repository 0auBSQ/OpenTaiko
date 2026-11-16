
// terrain with baked maps (#version and SHADOWS added by the engine)
precision highp float;
in float vCamZ; in vec3 vWorld; in vec3 vNormal;
uniform sampler2D uTex, uTex2, uTex3, uTex4, uSplat;
uniform sampler2D uNormalMap, uTintMap;                // tint 0.5 = unchanged
uniform vec2 uSplatSize;
uniform float uMapSize;
uniform float uTexScale;      // uv per world unit
uniform vec3 uAvg[4];
uniform vec4 uTerr[6];        // 0 detail, 1 maps, 2 triplanar, 3 sky amb, 4 ground amb, 5 cloud
uniform vec3 uTint; uniform float uAlpha;
uniform int uFog; uniform vec3 uFogCol; uniform float uFogStart; uniform float uFogInv;
uniform vec3 uSunDir, uSunCol, uAmb, uCam;
uniform vec4 uFogSky[6];      // 0 horizon, 1 zenith, 2 below, 3 glow, 4 glow dir, 5 high ground
vec3 fogColor(vec3 wp){
    if (uFogSky[0].w <= 0.0) return uFogCol;
    vec3 d = normalize(wp - uCam);
    vec3 c = mix(uFogSky[0].rgb, uFogSky[1].rgb, 1.0 - exp(-max(d.y, 0.0) * uFogSky[1].w));
    c = mix(uFogSky[2].rgb, c, smoothstep(-uFogSky[2].w, uFogSky[2].w, d.y));
    float g = max(dot(d.xz / max(length(d.xz), 1e-4), uFogSky[4].xz), 0.0);
    return c + uFogSky[3].rgb * (pow(g, uFogSky[3].w) * exp(-abs(d.y) * uFogSky[4].w));
}
uniform int uNumLights; uniform vec4 uLightPosR[64]; uniform vec3 uLightCol[64];
uniform highp sampler2D uLightGrid;   // light index + 1 per slot, 0 ends
uniform vec4 uLightGridXf;            // corner x, z, cells per unit, cells per side (0 = off)
uniform int uLightGlobal;             // leading unranged lights
int lightSlots(vec3 wp, out vec4 ga, out vec4 gb){
    ga = vec4(0.0); gb = vec4(0.0);
    if (uLightGridXf.w <= 0.0) return uNumLights;
    int n = int(uLightGridXf.w);
    ivec2 c = ivec2(floor((wp.xz - uLightGridXf.xy) * uLightGridXf.z));
    if (c.x < 0 || c.y < 0 || c.x >= n || c.y >= n) return uLightGlobal;
    ga = texelFetch(uLightGrid, c, 0) * 255.0;
    if (ga.x < 0.5) return uLightGlobal;
    if (ga.w > 0.5) gb = texelFetch(uLightGrid, ivec2(c.x, c.y + n), 0) * 255.0;   // slots 4..7
    return uLightGlobal + 8;
}
// -1: no more
int lightOf(int k, vec4 ga, vec4 gb){
    if (uLightGridXf.w <= 0.0 || k < uLightGlobal) return k;
    int j = k - uLightGlobal;
    float v = j == 0 ? ga.x : j == 1 ? ga.y : j == 2 ? ga.z : j == 3 ? ga.w : j == 4 ? gb.x : j == 5 ? gb.y : j == 6 ? gb.z : gb.w;
    return int(v + 0.5) - 1;
}
uniform float uWet, uTime;
uniform vec4 uClip;           // w <= -9e29 = off
out vec4 frag;
#ifdef SHADOWS
uniform mat4 uLightVP; uniform highp sampler2DShadow uShadowCmp; uniform float uShadowTexel;
uniform vec2 uShadowF;        // radius in texels, edge fade share
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
#endif
uniform float uCel;
float cel(float x){
    float s = x * 3.0, f = floor(s);
    return mix(x, (f + smoothstep(0.30, 0.70, s - f)) / 3.0, uCel);
}
float lum(vec3 c){ return dot(c, vec3(0.299, 0.587, 0.114)); }
float hash(vec2 p){ return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453); }
float vnoise(vec2 p){
    vec2 i = floor(p), f = fract(p); f = f * f * (3.0 - 2.0 * f);
    return mix(mix(hash(i), hash(i + vec2(1.0, 0.0)), f.x), mix(hash(i + vec2(0.0, 1.0)), hash(i + vec2(1.0, 1.0)), f.x), f.y);
}
const mat2 ROT = mat2(0.80, 0.60, -0.60, 0.80);
vec3 layerCol(sampler2D t, vec2 uv, vec2 dx, vec2 dy, vec3 avg){
    vec3 a = textureGrad(t, uv, dx, dy).rgb;
    float s2 = uTerr[0].x;
    vec3 b = textureGrad(t, ROT * uv * s2, ROT * dx * s2, ROT * dy * s2).rgb;
    vec3 k = clamp(b / max(avg, vec3(0.04)), 0.65, 1.4);
    return a * mix(vec3(1.0), k, uTerr[0].y);
}
vec3 sideCol(sampler2D t, vec3 p, vec3 dpx, vec3 dpy, vec3 an){
    float ax = an.x * an.x, az = an.z * an.z;
    vec3 a = textureGrad(t, p.zy, dpx.zy, dpy.zy).rgb * ax + textureGrad(t, p.xy, dpx.xy, dpy.xy).rgb * az;
    return a / max(ax + az, 1e-4);
}
void main(){
    if (uClip.w > -8.9e29 && dot(vWorld, uClip.xyz) + uClip.w < 0.0) discard;
    vec2 muv = vWorld.xz / uMapSize;
    vec3 N = normalize(mix(normalize(vNormal), texture(uNormalMap, muv).rgb * 2.0 - 1.0, uTerr[1].x));
    vec3 p = vWorld * uTexScale;
    vec3 dpx = dFdx(p), dpy = dFdy(p);
    vec4 w = texture(uSplat, vWorld.xz / uSplatSize);
    w /= max(w.r + w.g + w.b + w.a, 1e-4);
    const float T = 0.004;
    vec3 c1 = vec3(0.0), c2 = vec3(0.0), c3 = vec3(0.0), c4 = vec3(0.0);
    if (w.r > T) c1 = layerCol(uTex, p.xz, dpx.xz, dpy.xz, uAvg[0]);
    if (w.g > T) c2 = layerCol(uTex2, p.xz, dpx.xz, dpy.xz, uAvg[1]);
    if (w.b > T) c3 = layerCol(uTex3, p.xz, dpx.xz, dpy.xz, uAvg[2]);
    if (w.a > T) c4 = layerCol(uTex4, p.xz, dpx.xz, dpy.xz, uAvg[3]);
    vec3 an = abs(N);
    float steep = 1.0 - smoothstep(0.50, 0.78, an.y);
    if (steep > 0.0){
        if (w.r > T && uTerr[2].x > 0.5) c1 = mix(c1, sideCol(uTex, p, dpx, dpy, an), steep);
        if (w.g > T && uTerr[2].y > 0.5) c2 = mix(c2, sideCol(uTex2, p, dpx, dpy, an), steep);
        if (w.b > T && uTerr[2].z > 0.5) c3 = mix(c3, sideCol(uTex3, p, dpx, dpy, an), steep);
        if (w.a > T && uTerr[2].w > 0.5) c4 = mix(c4, sideCol(uTex4, p, dpx, dpy, an), steep);
    }
    // brighter texel wins near layer borders
    float sharp = uTerr[0].w;
    if (sharp > 0.0){
        vec4 on = step(vec4(T), w);
        vec4 b = (w + vec4(lum(c1), lum(c2), lum(c3), lum(c4)) * (0.6 * sharp)) * on - (1.0 - on);
        float m = max(max(b.x, b.y), max(b.z, b.w)) - mix(0.5, 0.06, sharp);
        b = max(b - m, 0.0) * on;
        w = b / max(b.x + b.y + b.z + b.w, 1e-4);
    }
    vec3 base = (c1 * w.r + c2 * w.g + c3 * w.b + c4 * w.a) * uTint;
    base *= mix(vec3(1.0), texture(uTintMap, muv).rgb * 2.0, uTerr[1].y);
    // relief from brightness
    if (uTerr[0].z > 0.0){
        float hgt = lum(base);
        vec3 wx = dFdx(vWorld), wy = dFdy(vWorld);
        vec3 r1 = cross(wy, N), r2 = cross(N, wx);
        float det = dot(wx, r1);
        vec3 g = sign(det) * (dFdx(hgt) * r1 + dFdy(hgt) * r2);
        N = normalize(abs(det) * N - uTerr[0].z * g);
    }
    float ndl = cel(max(dot(N, uSunDir), 0.0));
#ifdef SHADOWS
    float sh = sunShadow(vWorld, N);
#else
    float sh = 1.0;
#endif
    if (uTerr[5].x > 0.0){
        vec2 cp = (vWorld.xz + uTerr[5].zw * uTime) / max(uTerr[5].y, 1.0);
        float cn = vnoise(cp) * 0.65 + vnoise(cp * 2.3 + 7.1) * 0.35;
        sh *= 1.0 - uTerr[5].x * smoothstep(0.42, 0.72, cn);
    }
    vec3 sky = uAmb * uTerr[3].rgb;
    vec3 l = mix(uAmb * uTerr[4].rgb, sky, N.y * 0.5 + 0.5) + ndl * uSunCol * sh;
    vec4 ga, gb; int ns = lightSlots(vWorld, ga, gb);
    for (int k = 0; k < ns; k++){
        int i = lightOf(k, ga, gb); if (i < 0) break;
        vec3 dp = uLightPosR[i].xyz - vWorld; float range = uLightPosR[i].w;
        float d2 = dot(dp, dp);
        if (range > 0.0){
            if (d2 >= range * range) continue;
            float dl = max(sqrt(d2), 1e-4); float nl = max(dot(N, dp / dl), 0.0); if (nl <= 0.0) continue;
            float f = 1.0 - dl / range; l += uLightCol[i] * (f * f * nl);
        } else {
            float inv = inversesqrt(max(d2, 1e-6)); float nl = max(dot(N, dp * inv), 0.0); if (nl <= 0.0) continue;
            l += uLightCol[i] * (nl / (d2 + 1.0));
        }
    }
    vec3 V = normalize(uCam - vWorld);
    vec3 col = base * l;
    col += base * sky * (pow(1.0 - max(dot(N, V), 0.0), 3.0) * uTerr[1].z);
    if (uWet > 0.0){
        col *= 1.0 - 0.30 * uWet;
        vec3 H = normalize(V + uSunDir);
        col += uSunCol * (pow(max(dot(N, H), 0.0), 48.0) * 0.7 * uWet * sh);
    }
    // shoulder, not a clip
    col = min(col, vec3(0.8)) + 0.2 * (1.0 - exp(-max(col - 0.8, 0.0) * 5.0));
    if (uFog == 1){
        float f = clamp((vCamZ - uFogStart) * uFogInv, 0.0, 1.0);
        if (f > 0.0){
            // high ground through the fog
            if (uFogSky[5].z > 0.0) f *= 1.0 - uFogSky[5].z * smoothstep(uFogSky[5].x, uFogSky[5].y, vWorld.y) * exp(-vCamZ * uFogSky[5].w);
            col = mix(col, fogColor(vWorld), f);
        }
    }
    frag = vec4(col, uAlpha);
}
