#version 300 es
layout(location=0) in vec3 aPos;
layout(location=1) in vec2 aUV;
layout(location=2) in vec4 aColor;
uniform vec3 uCam, uR, uU, uF;
uniform float uSx, uSy, uA, uB;
out vec2 vUV; out vec4 vColor; out float vCamZ;
void main(){
    vUV = aUV; vColor = aColor;
    vec3 r = aPos - uCam;
    float cx = dot(r, uR), cy = dot(r, uU), cz = dot(r, uF);
    vCamZ = cz;
    gl_Position = vec4(cx * uSx, cy * uSy, uA * cz + uB, cz);
}