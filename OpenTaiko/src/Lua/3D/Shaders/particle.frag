#version 300 es
precision highp float;
in vec2 vUV; in vec4 vColor; in float vCamZ;
uniform int uTextured; uniform sampler2D uTex;
uniform int uAdd;
uniform int uFog; uniform vec3 uFogCol; uniform float uFogStart; uniform float uFogInv;
out vec4 frag;
void main(){
    vec4 c = vColor;
    if (uTextured == 1) { vec4 t = texture(uTex, vUV); c = vec4(t.rgb * vColor.rgb, t.a * vColor.a); }
    if (uFog == 1){
        float f = clamp((vCamZ - uFogStart) * uFogInv, 0.0, 1.0);
        c.rgb = (uAdd == 1) ? c.rgb * (1.0 - f) : mix(c.rgb, uFogCol, f);
    }
    frag = c;
}