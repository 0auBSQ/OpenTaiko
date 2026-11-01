
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
out vec4 frag;
#ifdef SHADOWS
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
#ifdef SHADOWS
        // characters RECEIVE the sun shadow (a wall's shade dims them). Sample 0.6 units TOWARD the
        // sun: the sprite's own sun-facing caster quad passes through its centre, so sampling at the
        // fragment itself made every character shade ITSELF; real occluders sit further than 0.6.
        c.rgb *= (0.55 + 0.45 * sunShadow(vWorld + uSunDir * 0.6, vNormal));
#endif
        if (uFog == 1){ float f = clamp((vCamZ - uFogStart) * uFogInv, 0.0, 1.0); c.rgb = mix(c.rgb, uFogCol, f); } frag = c; return; }
    vec3 base;
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
    else { vec4 t = texture(uTex, vUV); if (t.a < 0.5) discard; base = t.rgb * uTint; }
    if (uFlatCol.a > 0.5) base = uFlatCol.rgb;   // flat albedo override: recolour a routed part (still shades)
    vec3 N = normalize(vNormal);
    float relief = 1.0;
    if (uNormOn == 1 && uMode == 1){
        // the normal map, turned onto the surface: the surface frame comes from the screen derivatives of
        // the position and the uv (the vertices carry no tangents); B runs down the image like the map's green
        vec3 m = texture(uNormTex, vUV).rgb * 2.0 - 1.0;
        vec3 dp1 = dFdx(vWorld), dp2 = dFdy(vWorld); vec2 du1 = dFdx(vUV), du2 = dFdy(vUV);
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
    if (uFog == 1){ float f = clamp((vCamZ - uFogStart) * uFogInv, 0.0, 1.0); col = mix(col, uFogCol, f); }
    frag = vec4(col, uAlpha);
}