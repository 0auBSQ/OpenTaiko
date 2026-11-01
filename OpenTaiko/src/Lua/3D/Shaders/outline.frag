#version 300 es
precision highp float;
uniform sampler2D uTex; uniform vec2 uRes; uniform vec3 uColor; uniform float uThick;
out vec4 frag;
void main(){
    ivec2 p = ivec2(gl_FragCoord.xy);
    vec4 c = texelFetch(uTex, p, 0);
    if (c.a > 0.001) { frag = c; return; }
    int T = int(ceil(uThick)) + 1;
    if (T > 12) T = 12;
    float best = 1e9;
    for (int dy = -T; dy <= T; dy++) {
        for (int dx = -T; dx <= T; dx++) {
            ivec2 q = p + ivec2(dx, dy);
            if (q.x < 0 || q.y < 0 || q.x >= int(uRes.x) || q.y >= int(uRes.y)) continue;
            if (texelFetch(uTex, q, 0).a > 0.5) {
                float d = length(vec2(float(dx), float(dy)));
                if (d < best) best = d;
            }
        }
    }
    float a = clamp(uThick + 0.5 - best, 0.0, 1.0);
    frag = vec4(uColor, a);
}