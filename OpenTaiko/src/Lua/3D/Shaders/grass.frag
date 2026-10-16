#version 300 es
precision highp float;
in vec2 vUV; in float vShade; in float vCamZ;
uniform sampler2D uTex;
uniform vec3 uSunCol, uAmb;
uniform int uFog; uniform vec3 uFogCol; uniform float uFogStart; uniform float uFogInv;
out vec4 frag;
void main(){
    vec4 t = texture(uTex, vUV);
    if (t.a < 0.5) discard;
    vec3 li = uAmb + uSunCol * (0.35 + 0.45 * (1.0 - vUV.y));   // tips catch more sun
    vec3 col = t.rgb * vShade * li;
    if (uFog == 1){ float f = clamp((vCamZ - uFogStart) * uFogInv, 0.0, 1.0); col = mix(col, uFogCol, f); }
    frag = vec4(col, 1.0);
}