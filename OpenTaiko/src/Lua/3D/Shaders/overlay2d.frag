#version 300 es
precision highp float;
uniform vec4 uColor;
out vec4 frag;
void main(){ frag = uColor; }