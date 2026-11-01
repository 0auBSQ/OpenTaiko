#version 300 es
precision highp float;
uniform sampler2D uTex; uniform vec2 uRes; uniform float uTilt, uSat, uVig, uBloom;
uniform float uExposure; uniform vec3 uGrade;   // SetColorGrade: exposure multiply + day/night tint
out vec4 frag;
vec4 premul(vec4 t){ return vec4(t.rgb * t.a, t.a); }
void main(){
    vec2 uv = (gl_FragCoord.xy) / uRes;
    float dy = abs(uv.y - 0.5) * 2.0;                 // 0 centre .. 1 top/bottom
    float band = smoothstep(0.34, 1.0, dy);           // middle ~34% stays sharp, ramps out
    float rad = band * uTilt;
    vec4 src = texture(uTex, uv);
    if (rad > 0.5){
        vec2 inv = 1.0 / uRes;
        vec4 acc = premul(src) + premul(texture(uTex, uv + vec2(0.0, rad) * inv)) + premul(texture(uTex, uv + vec2(0.0, -rad) * inv))
                 + premul(texture(uTex, uv + vec2(rad, 0.0) * inv)) + premul(texture(uTex, uv + vec2(-rad, 0.0) * inv));
        src = vec4(acc.rgb / max(acc.a, 1e-4), acc.a * 0.2);   // colour weighted by alpha: no dark rim over a transparent canvas
    }
    vec3 c = src.rgb;
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
    c *= uExposure * uGrade;                           // colour grade (before saturation/contrast)
    float l = dot(c, vec3(0.299, 0.587, 0.114));
    c = mix(vec3(l), c, uSat);                         // saturation
    c = (c - 0.5) * 1.08 + 0.5;                        // gentle contrast
    vec2 d = uv - 0.5;
    c *= 1.0 - uVig * dot(d, d) * 2.2;                 // vignette
    frag = vec4(clamp(c, 0.0, 1.0), src.a);
}