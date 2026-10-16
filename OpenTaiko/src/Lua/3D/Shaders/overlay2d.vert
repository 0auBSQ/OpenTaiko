#version 300 es
layout(location=0) in vec2 aPx;
uniform vec2 uRes;
void main(){ gl_Position = vec4(aPx.x / uRes.x * 2.0 - 1.0, aPx.y / uRes.y * 2.0 - 1.0, 0.0, 1.0); }