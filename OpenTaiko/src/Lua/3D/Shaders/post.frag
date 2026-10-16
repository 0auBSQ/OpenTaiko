#version 300 es
precision highp float;
uniform sampler2D uTex; uniform int uMode; uniform vec2 uRes;
out vec4 frag;
float luma(vec3 c){ return (c.r + 2.0*c.g + c.b) * 255.0; }   // 0..1020, matches the CPU's R+2G+B bytes
vec4 premul(vec4 t){ return vec4(t.rgb * t.a, t.a); }
void main(){
    ivec2 p = ivec2(gl_FragCoord.xy);
    vec4 c = texelFetch(uTex, p, 0);
    if (uMode != 1){ frag = c; return; }                       // RGBA copy (alpha kept)
    if (p.x < 1 || p.y < 1 || p.x >= int(uRes.x)-1 || p.y >= int(uRes.y)-1){ frag = c; return; }
    vec4 cn = texelFetch(uTex, p+ivec2(0,-1),0), cs = texelFetch(uTex, p+ivec2(0,1),0);
    vec4 ce = texelFetch(uTex, p+ivec2(1,0),0),  cw = texelFetch(uTex, p+ivec2(-1,0),0);
    float lC=luma(c.rgb), lN=luma(cn.rgb), lS=luma(cs.rgb), lE=luma(ce.rgb), lW=luma(cw.rgb);
    float mn=min(lC,min(min(lN,lS),min(lE,lW))), mx=max(lC,max(max(lN,lS),max(lE,lW)));
    float range = mx-mn;
    if (range < 64.0 || range*8.0 < mx){ frag = c; return; }
    vec4 a, b;
    if (abs(lN+lS-2.0*lC) >= abs(lE+lW-2.0*lC)){ a=cn; b=cs; } else { a=ce; b=cw; }
    vec4 acc = premul(c)*2.0 + premul(a) + premul(b);         // colour weighted by alpha: no dark rim over a transparent canvas
    frag = vec4(acc.rgb / max(acc.a, 1e-4), acc.a * 0.25);
}