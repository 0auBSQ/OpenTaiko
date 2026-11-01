#version 300 es
precision highp float;
uniform highp sampler2D uAccum;
out vec4 frag;
void main(){
    ivec2 p = ivec2(gl_FragCoord.xy);
    vec4 a = texelFetch(uAccum, p, 0);                // rgb = radiance sum, a = samples in it
    if (a.a < 0.5) a = texelFetch(uAccum, ivec2(p.x & ~1, p.y & ~1), 0);   // skipped by a moving pass: show the pixel it traced
    vec3 c = a.rgb / max(a.a, 1.0);
    c = c / (1.0 + c);                       // Reinhard tonemap (matches CPU Out)
    c = pow(max(c, vec3(0.0)), vec3(1.0/2.2));
    frag = vec4(c, 1.0);
}