
// grass cards around the player (#version and SHADOWS added by the engine)
precision highp float;
layout(location=0) in vec4 aI;        // x, z, r1, r2
uniform vec3 uCam, uR, uU, uF;
uniform float uSx, uSy, uA, uB, uTime;
uniform vec4 uTile;       // focus x, z, tile size, 1 / map size
uniform vec4 uField;      // last column, last row, 1 / cell size, ground uv per unit
uniform vec4 uSplatP;     // 1 / splat x, z, min weight, max slope
uniform vec4 uP[10];      // 0 density, 1 range, 2 size, 3 wind, 4 wind2, 5 kind, 6 atlas, 7 tip, 8 dry, 9 ground
uniform vec4 uCell[16];   // card width, height (world units)
uniform vec3 uAvgGround;
uniform vec4 uTerr[6];
uniform vec3 uSunDir, uSunCol, uAmb;
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
uniform highp sampler2D uHeight;
uniform sampler2D uSplat, uGround, uTintMap, uMask;
#ifdef SHADOWS
uniform mat4 uLightVP; uniform highp sampler2D uShadow; uniform vec2 uShadowF;
#endif
out vec2 vUV; out vec3 vCol; out vec3 vLight; out vec4 vMisc; out float vCamZ;   // vMisc: card height, gust, flower, unused
out vec3 vFog;
uniform vec3 uFogCol;
uniform vec4 uFogSky[6];      // 0 horizon, 1 zenith, 2 below, 3 glow, 4 glow dir, 5 high ground
vec3 fogColor(vec3 wp){
    if (uFogSky[0].w <= 0.0) return uFogCol;
    vec3 d = normalize(wp - uCam);
    vec3 c = mix(uFogSky[0].rgb, uFogSky[1].rgb, 1.0 - exp(-max(d.y, 0.0) * uFogSky[1].w));
    c = mix(uFogSky[2].rgb, c, smoothstep(-uFogSky[2].w, uFogSky[2].w, d.y));
    float g = max(dot(d.xz / max(length(d.xz), 1e-4), uFogSky[4].xz), 0.0);
    return c + uFogSky[3].rgb * (pow(g, uFogSky[3].w) * exp(-abs(d.y) * uFogSky[4].w));
}
const mat2 ROT = mat2(0.80, 0.60, -0.60, 0.80);
float hash(vec2 p){ return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453); }
float vnoise(vec2 p){
    vec2 i = floor(p), f = fract(p); f = f * f * (3.0 - 2.0 * f);
    return mix(mix(hash(i), hash(i + vec2(1.0, 0.0)), f.x), mix(hash(i + vec2(0.0, 1.0)), hash(i + vec2(1.0, 1.0)), f.x), f.y);
}
// on the terrain mesh's two triangles per cell
float groundY(vec2 xz, out float slope){
    vec2 f = clamp(xz * uField.z, vec2(0.0), uField.xy - 0.001);
    vec2 i = floor(f), t = f - i;
    ivec2 c = ivec2(i);
    float h00 = texelFetch(uHeight, c, 0).r, h11 = texelFetch(uHeight, c + ivec2(1, 1), 0).r;
    float h10 = texelFetch(uHeight, c + ivec2(1, 0), 0).r, h01 = texelFetch(uHeight, c + ivec2(0, 1), 0).r;
    slope = max(abs(h10 - h00), max(abs(h01 - h00), abs(h11 - h00))) * uField.z;
    return t.y >= t.x ? h00 + (h11 - h01) * t.x + (h01 - h00) * t.y : h00 + (h10 - h00) * t.x + (h11 - h10) * t.y;
}
void main(){
    float S = uTile.z;
    vec2 xz = uTile.xy + mod(aI.xy - uTile.xy + 0.5 * S, S) - 0.5 * S;
    float r1 = aI.z, r2 = aI.w;
    float r3 = fract(r2 * 61.37), r4 = fract(r2 * 157.31), r5 = fract(r1 * 97.13 + r2 * 31.7);
    float dist = length(xz - uTile.xy);
    float thr = 1.0 - smoothstep(uP[1].x, uP[1].y, dist);
    float slope; float gy = groundY(xz, slope);
    vec4 sw = textureLod(uSplat, xz * uSplatP.xy, 0.0);
    float gw = sw.r / max(sw.r + sw.g + sw.b + sw.a, 1e-4);
    vec3 mask = textureLod(uMask, xz * uTile.w, 0.0).rgb * 2.0;
    float present = smoothstep(uSplatP.z, min(uSplatP.z + 0.3, 1.0), gw) * (1.0 - smoothstep(uSplatP.w * 0.7, uSplatP.w, slope));
    float ps = max(uP[0].w, 1.0);
    float pn = vnoise(xz / ps) * 0.6 + vnoise(xz / (ps * 0.34) + 17.0) * 0.4;
    float dens = present * min(mask.r, 1.0) * mix(uP[0].z, 1.0, smoothstep(0.30, 0.62, pn));
    float k = clamp((thr * dens - r1) * 5.0, 0.0, 1.0);
    if (k <= 0.0){ gl_Position = vec4(2.0, 2.0, 2.0, 1.0); vUV = vec2(0.0); vCol = vec3(0.0); vLight = vec3(0.0); vMisc = vec4(0.0); vCamZ = 0.0; vFog = vec3(0.0); return; }
    float fl = uP[5].x * mask.g * (0.25 + 2.2 * smoothstep(0.50, 0.80, vnoise(xz / max(uP[5].y, 1.0) + 31.0)));
    bool flower = r3 < fl && uP[6].w > 0.5;
    float cell = flower ? uP[6].z + floor(r4 * uP[6].w) : floor(r4 * uP[6].z);
    vec2 cs = uCell[int(cell)].xy;
    float edgeK = mix(0.55, 1.0, smoothstep(uSplatP.z, min(uSplatP.z + 0.45, 1.0), gw));
    float grow = edgeK * (0.45 + 0.55 * k);   // rim tufts smaller, not flatter
    float w = cs.x * mix(uP[2].x, uP[2].y, r5) * grow;
    float h = cs.y * mix(uP[2].z, uP[2].w, fract(r5 * 7.77)) * mask.b * grow;
    float yaw = (r3 - 0.5) * 1.2;
    vec2 rh = normalize(uR.xz + vec2(1e-5, 0.0));
    float cy = cos(yaw), sy = sin(yaw);
    vec3 right = vec3(rh.x * cy - rh.y * sy, 0.0, rh.x * sy + rh.y * cy);
    vec3 up = normalize(mix(vec3(0.0, 1.0, 0.0), uU, uP[1].z));
    vec2 wd = uP[3].xy;
    vec2 wq = xz - wd * (uTime * uP[4].x);
    float gust = vnoise(wq / max(uP[3].w, 1.0)) * 0.7 + vnoise(wq / max(uP[3].w * 0.37, 1.0) + 5.0) * 0.3;
    float sway = (gust - 0.35) * uP[3].z + sin(uTime * (2.2 + r3 * 1.6) + r2 * 40.0) * uP[4].y;
    float cx = float(gl_VertexID & 1), ct = float(gl_VertexID >> 1);
    vec3 wp = vec3(xz.x, gy - 0.03, xz.y) + right * ((cx - 0.5) * w) + up * (ct * h) + vec3(wd.x, 0.0, wd.y) * (sway * h * ct);
    float cols = uP[6].x, rows = uP[6].y;
    float col = mod(cell, cols), row = floor(cell / cols);
    float u = fract(r5 * 3.3) > 0.5 ? 1.0 - cx : cx;
    vUV = vec2((col + u) / cols, (row + 1.0 - ct) / rows);
    vec2 luv = xz * uField.w;
    vec3 g1 = textureLod(uGround, luv, 3.0).rgb;
    vec3 g2 = textureLod(uGround, ROT * luv * uP[9].z, 1.0).rgb;
    vec3 ground = g1 * mix(vec3(1.0), clamp(g2 / max(uAvgGround, vec3(0.04)), 0.65, 1.4), uP[9].w);
    ground *= mix(vec3(1.0), textureLod(uTintMap, xz * uTile.w, 0.0).rgb * 2.0, uTerr[1].y);
    float dry = smoothstep(0.55, 0.85, vnoise(xz / max(uP[5].z, 1.0) + 71.0)) * uP[5].w;
    vCol = ground * mix(vec3(1.0), uP[8].rgb, dry) * (0.90 + 0.20 * r5);
    float sh = 1.0;
#ifdef SHADOWS
    vec4 lp = uLightVP * vec4(wp.x, wp.y + 0.12, wp.z, 1.0);   // per corner: edges can cross the card
    vec3 sp = lp.xyz / lp.w * 0.5 + 0.5;
    vec2 e = abs(sp.xy * 2.0 - 1.0);
    float edge = max(e.x, e.y);
    if (sp.z <= 1.0 && edge < 1.0){
        float s1 = step(sp.z - 0.0008, textureLod(uShadow, sp.xy, 0.0).r);
        float fade = uShadowF.y > 0.0 ? clamp((edge - (1.0 - uShadowF.y)) / uShadowF.y, 0.0, 1.0) : 0.0;
        sh = mix(s1, 1.0, fade);
    }
#endif
    if (uTerr[5].x > 0.0){
        vec2 cp = (xz + uTerr[5].zw * uTime) / max(uTerr[5].y, 1.0);
        float cn = vnoise(cp) * 0.65 + vnoise(cp * 2.3 + 7.1) * 0.35;
        sh *= 1.0 - uTerr[5].x * smoothstep(0.42, 0.72, cn);
    }
    vec3 l = uAmb * uTerr[3].rgb + max(uSunDir.y, 0.0) * uSunCol * sh;
    vec4 ga, gb; int ns = lightSlots(wp, ga, gb);
    for (int k = 0; k < ns; k++){
        int i = lightOf(k, ga, gb); if (i < 0) break;
        vec3 dp = uLightPosR[i].xyz - wp; float range = uLightPosR[i].w;
        float d2 = dot(dp, dp);
        if (range > 0.0){
            if (d2 >= range * range) continue;
            float dl = max(sqrt(d2), 1e-4); float f = 1.0 - dl / range;
            l += uLightCol[i] * (f * f * (0.35 + 0.65 * max(dp.y / dl, 0.0)));
        } else l += uLightCol[i] * (0.6 / (d2 + 1.0));
    }
    vLight = l;
    vMisc = vec4(ct, gust, flower ? 1.0 : 0.0, 0.0);
    vec3 r = wp - uCam;
    float px = dot(r, uR), py = dot(r, uU), pz = dot(r, uF);
    vCamZ = pz;
    vFog = fogColor(wp);
    gl_Position = vec4(px * uSx, py * uSy, uA * pz + uB, pz);
}
