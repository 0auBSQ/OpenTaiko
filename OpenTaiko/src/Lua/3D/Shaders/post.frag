#version 300 es
precision highp float;
uniform sampler2D uTex; uniform int uMode; uniform vec2 uRes;
out vec4 frag;
float luma(vec3 c){ return (c.r + 2.0*c.g + c.b) * 255.0; }   // 0..1020, matches the CPU's R+2G+B bytes
vec4 premul(vec4 t){ return vec4(t.rgb * t.a, t.a); }
float lumaF(vec4 t){ return dot(t.rgb, vec3(0.299, 0.587, 0.114)); }
float lumaAt(vec2 uv){ return lumaF(texture(uTex, uv)); }
// neighbour blend for pixels next to transparency: colour weighted by alpha, so no dark rim over a transparent canvas
vec4 lite(vec4 c, vec4 cn, vec4 cs, vec4 ce, vec4 cw){
    float lC=luma(c.rgb), lN=luma(cn.rgb), lS=luma(cs.rgb), lE=luma(ce.rgb), lW=luma(cw.rgb);
    float mn=min(lC,min(min(lN,lS),min(lE,lW))), mx=max(lC,max(max(lN,lS),max(lE,lW)));
    float range = mx-mn;
    if (range < 64.0 || range*8.0 < mx) return c;
    vec4 a, b;
    if (abs(lN+lS-2.0*lC) >= abs(lE+lW-2.0*lC)){ a=cn; b=cs; } else { a=ce; b=cw; }
    vec4 acc = premul(c)*2.0 + premul(a) + premul(b);
    return vec4(acc.rgb / max(acc.a, 1e-4), acc.a * 0.25);
}
// step lengths, in pixels, of the walk along an edge (after the first one-pixel step)
const float STEP[11] = float[11](1.0, 1.0, 1.0, 1.0, 1.5, 2.0, 2.0, 2.0, 2.0, 4.0, 8.0);
void main(){
    ivec2 p = ivec2(gl_FragCoord.xy);
    vec4 c = texelFetch(uTex, p, 0);
    if (uMode != 1){ frag = c; return; }                       // RGBA copy (alpha kept)
    if (p.x < 1 || p.y < 1 || p.x >= int(uRes.x)-1 || p.y >= int(uRes.y)-1){ frag = c; return; }
    vec4 cn = texelFetch(uTex, p+ivec2(0,-1),0), cs = texelFetch(uTex, p+ivec2(0,1),0);
    vec4 ce = texelFetch(uTex, p+ivec2(1,0),0),  cw = texelFetch(uTex, p+ivec2(-1,0),0);
    if (min(min(c.a, cn.a), min(min(cs.a, ce.a), cw.a)) < 0.999){ frag = lite(c, cn, cs, ce, cw); return; }

    // FXAA: find the edge through this pixel, walk along it both ways to its ends, then take one bilinear
    // sample shifted across the edge by how far this pixel sits from the nearer end
    vec2 rcp = 1.0 / uRes;
    vec2 uv = (vec2(p) + 0.5) * rcp;
    float lM = lumaF(c), lN = lumaF(cn), lS = lumaF(cs), lE = lumaF(ce), lW = lumaF(cw);
    float mx = max(lM, max(max(lN, lS), max(lE, lW)));
    float mn = min(lM, min(min(lN, lS), min(lE, lW)));
    float range = mx - mn;
    if (range < max(0.0312, mx * 0.125)){ frag = c; return; }
    float lNW = lumaF(texelFetch(uTex, p+ivec2(-1,-1),0)), lSE = lumaF(texelFetch(uTex, p+ivec2(1,1),0));
    float lNE = lumaF(texelFetch(uTex, p+ivec2(1,-1),0)),  lSW = lumaF(texelFetch(uTex, p+ivec2(-1,1),0));
    float lNS = lN + lS, lWE = lW + lE;
    float lNESE = lNE + lSE, lNWNE = lNW + lNE, lNWSW = lNW + lSW, lSWSE = lSW + lSE;
    float edgeHorz = abs(-2.0*lW + lNWSW) + abs(-2.0*lM + lNS)*2.0 + abs(-2.0*lE + lNESE);
    float edgeVert = abs(-2.0*lS + lSWSE) + abs(-2.0*lM + lWE)*2.0 + abs(-2.0*lN + lNWNE);
    bool horzSpan = edgeHorz >= edgeVert;
    float lengthSign = horzSpan ? rcp.y : rcp.x;
    float subpixB = ((lNS + lWE)*2.0 + lNWSW + lNESE) * (1.0/12.0) - lM;
    float l1 = horzSpan ? lN : lW, l2 = horzSpan ? lS : lE;
    float gradN = l1 - lM, gradS = l2 - lM;
    bool pairN = abs(gradN) >= abs(gradS);
    float gradient = max(abs(gradN), abs(gradS));
    if (pairN) lengthSign = -lengthSign;
    float lPair = pairN ? l1 + lM : l2 + lM;                    // twice the average luma across the edge
    float subpixC = clamp(abs(subpixB) / range, 0.0, 1.0);
    vec2 posB = uv;
    vec2 offNP = horzSpan ? vec2(rcp.x, 0.0) : vec2(0.0, rcp.y);
    if (horzSpan) posB.y += lengthSign * 0.5; else posB.x += lengthSign * 0.5;
    vec2 posN = posB - offNP, posP = posB + offNP;
    float lEndN = lumaAt(posN) - lPair*0.5, lEndP = lumaAt(posP) - lPair*0.5;
    float gradScaled = gradient * 0.25;
    bool lMLTZero = lM - lPair*0.5 < 0.0;
    bool doneN = abs(lEndN) >= gradScaled, doneP = abs(lEndP) >= gradScaled;
    for (int i = 0; i < 11; i++){
        if (doneN && doneP) break;
        if (!doneN){ posN -= offNP * STEP[i]; lEndN = lumaAt(posN) - lPair*0.5; doneN = abs(lEndN) >= gradScaled; }
        if (!doneP){ posP += offNP * STEP[i]; lEndP = lumaAt(posP) - lPair*0.5; doneP = abs(lEndP) >= gradScaled; }
    }
    float dstN = horzSpan ? uv.x - posN.x : uv.y - posN.y;
    float dstP = horzSpan ? posP.x - uv.x : posP.y - uv.y;
    bool directionN = dstN < dstP;
    float dst = min(dstN, dstP);
    bool goodSpan = directionN ? ((lEndN < 0.0) != lMLTZero) : ((lEndP < 0.0) != lMLTZero);
    float pixelOffset = goodSpan ? 0.5 - dst / (dstN + dstP) : 0.0;
    float subpixF = (-2.0*subpixC + 3.0) * subpixC * subpixC;
    float off = max(pixelOffset, subpixF * subpixF * 0.75);
    vec2 q = uv;
    if (horzSpan) q.y += off * lengthSign; else q.x += off * lengthSign;
    frag = vec4(texture(uTex, q).rgb, c.a);
}
