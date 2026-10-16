#version 300 es
layout(location=0) in vec3 aBase;
layout(location=1) in vec2 aHP;      // height, phase
layout(location=2) in float aShade;
uniform vec3 uCam, uR, uU, uF;
uniform float uSx, uSy, uA, uB, uTime;
uniform vec2 uFade;
out vec2 vUV; out float vShade; out float vCamZ;
const vec2 CORNER[6] = vec2[6](vec2(-1.0, 0.0), vec2(1.0, 0.0), vec2(1.0, 1.0), vec2(-1.0, 0.0), vec2(1.0, 1.0), vec2(-1.0, 1.0));
void main(){
    vec4 aBlade = vec4(CORNER[gl_VertexID], aHP);   // corner.x (-1..1), corner.y (0..1), height, phase
    float dist = length(aBase - uCam);
    float k = clamp(1.0 - (dist - uFade.x) / (uFade.y - uFade.x), 0.0, 1.0);
    float h = aBlade.z * k;
    // per-blade character derived from the (hashed) sway phase: width varies +-30%, each blade
    // leans its own way, and the two sway sines run de-correlated - kills the synchronized-lawn look
    float wf = 0.70 + 0.60 * fract(aBlade.w * 1.6180339);
    float lean = (fract(aBlade.w * 2.3999632) - 0.5) * 0.36;
    float sway = sin(uTime * 1.7 + aBlade.w) * 0.14 + sin(uTime * 3.1 + aBlade.w * 1.7) * 0.05;
    vec3 wp = aBase
        + uR * (aBlade.x * 0.055 * aBlade.z * wf)
        + vec3(0.0, aBlade.y * h, 0.0)
        + uR * ((sway + lean) * aBlade.y * aBlade.y * h);
    vUV = vec2(aBlade.x * 0.5 + 0.5, 1.0 - aBlade.y);
    vShade = aShade;
    vec3 r = wp - uCam;
    float cx = dot(r, uR), cy = dot(r, uU), cz = dot(r, uF);
    vCamZ = cz;
    gl_Position = vec4(cx * uSx, cy * uSy, uA * cz + uB, cz);
}