#version 300 es
layout(location=0) in vec3 aPos;
layout(location=1) in vec2 aUV;
layout(location=2) in float aShade;
layout(location=3) in vec3 aNormal;
uniform mat4 uModel;
uniform mat3 uNormalMat;      // the model's inverse transpose (up to scale): normals stay perpendicular under any scale
uniform vec3 uCam, uR, uU, uF;
uniform float uSx, uSy, uA, uB;
out vec2 vUV; out float vShade; out float vCamZ; out vec3 vWorld; out vec3 vNormal;
void main(){
    vUV = aUV; vShade = aShade;
    vec3 wp = (uModel * vec4(aPos, 1.0)).xyz;
    vWorld = wp; vNormal = uNormalMat * aNormal;
    vec3 r = wp - uCam;
    float cx = dot(r, uR), cy = dot(r, uU), cz = dot(r, uF);
    vCamZ = cz;
    gl_Position = vec4(cx * uSx, cy * uSy, uA * cz + uB, cz);
}