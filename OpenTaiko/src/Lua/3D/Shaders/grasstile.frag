
// grass card colour (#version added by the engine)
precision highp float;
in vec2 vUV; in vec3 vCol; in vec3 vLight; in vec4 vMisc; in float vCamZ; in vec3 vFog;
uniform sampler2D uAtlasTex;
uniform vec4 uP[10];          // 1.w contrast, 4.z cut-out, 7 tip, 8 dry
uniform vec3 uAvgAtlas;
uniform int uFog; uniform vec3 uFogCol; uniform float uFogStart; uniform float uFogInv;
out vec4 frag;
void main(){
    vec4 t = texture(uAtlasTex, vUV);
    if (t.a < uP[4].z) discard;
    float y = vMisc.x;
    // atlas detail on the ground colour; flowers keep their own
    vec3 tinted = vCol * mix(vec3(1.0), t.rgb / max(uAvgAtlas, vec3(0.04)), uP[1].w);
    vec3 g = mix(tinted, t.rgb, max(uP[8].w, vMisc.z));
    g *= mix(vec3(uP[7].w), uP[7].rgb, smoothstep(0.0, 0.9, y));
    vec3 col = g * vLight * (1.0 + 0.10 * vMisc.y * y);
    col = min(col, vec3(0.8)) + 0.2 * (1.0 - exp(-max(col - 0.8, 0.0) * 5.0));   // terrain's shoulder
    if (uFog == 1){ float f = clamp((vCamZ - uFogStart) * uFogInv, 0.0, 1.0); col = mix(col, vFog, f); }
    frag = vec4(col, 1.0);
}
