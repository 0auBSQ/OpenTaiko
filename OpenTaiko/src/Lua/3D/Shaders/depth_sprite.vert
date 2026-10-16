#version 300 es
layout(location=0) in vec3 aPos;
layout(location=1) in vec2 aUV;
uniform mat4 uLightVP, uModel;
out vec2 vUV;
void main(){ vUV = aUV; gl_Position = uLightVP * uModel * vec4(aPos, 1.0); }