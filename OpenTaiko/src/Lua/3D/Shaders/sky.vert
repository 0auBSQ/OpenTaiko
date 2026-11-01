#version 300 es
// fullscreen.vert on the far plane: drawn after the opaque world with the depth test on, it only reaches the pixels nothing covers
const vec2 P[3] = vec2[3](vec2(-1.0,-1.0), vec2(3.0,-1.0), vec2(-1.0,3.0));
void main(){ gl_Position = vec4(P[gl_VertexID], 1.0, 1.0); }