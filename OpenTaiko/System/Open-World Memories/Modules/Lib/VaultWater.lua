---@diagnostic disable: undefined-global, undefined-field, need-check-nil
-- VaultWater: the Secret Vault's water, a GPU shader drawn on a 3D scene used as a 2D layer.
--
-- local water = VaultWater.new{ w = 1920, h = 1080 }   -- water.ok is false when the shader cannot run (CPU
--                                                       -- renderer, compile failure): the caller draws its own picture
--                                                       -- (scale = render size against the drawn size, 2/3 if left out)
-- water:update(dt)                     -- dt = real seconds since the last frame (frames slower than 0.5 s count 0.5)
-- water:drawUnderwater(opacity)        -- full screen, seen from inside the water
-- water:drawSurface(y, opacity)        -- water below a wavy surface line at screen y; above it nothing is drawn
-- water:drawBody(top, bottom, opacity) -- water between a surface line (nil: above the screen) and a lower edge
--                                      -- (nil: below the screen)
-- water:setDrift(x, y)                 -- shifts the pattern inside the water (parallax), in screen px
-- water:setSwell(waves, foam, churn)   -- the wave height at the surface and the lower edge (px, nil: the look's),
--                                      -- the froth under them (1 = the look's, nil: 1) and how fast the pattern
--                                      -- inside moves (1 = the look's, nil: 1)
-- water:dispose()
-- The draw calls return false when nothing was drawn. Above a surface line and below a lower edge the frame is
-- transparent, so what was drawn before shows through.

local VaultWater = {}
VaultWater.__index = VaultWater

local SCALE = 2 / 3                   -- render size against the drawn size
local EDGE = 4                        -- the frame reaches this far past the drawn area: its texture wraps, so its
                                      -- outermost rows blend with the opposite side when scaled up
local NONE = 100000                   -- a surface or an edge this far off the screen is not drawn

local LOOK = {
    deep    = { 0.010, 0.060, 0.150 },
    mid     = { 0.030, 0.270, 0.420 },
    shallow = { 0.160, 0.600, 0.700 },
    sky     = { 0.820, 0.960, 1.000 },
    foam    = { 0.930, 0.990, 1.000 },
    caustics = 0.42, shafts = 0.34, specks = 0.85, waves = 15,
}

local SHADER = [==[
// psrdnoise (c) Stefan Gustavson and Ian McEwan, ver. 2021-12-02, published under the MIT license:
// https://github.com/stegu/psrdnoise/
//
// Copyright (c) 2021 Stefan Gustavson and Ian McEwan.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and
// to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions
// of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO
// THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
// IN THE SOFTWARE.
//
// Changed here: the periodic wrap is left out.
//
// The surface's underside shading (Fresnel and total internal reflection) follows WebGL Water:
// Copyright (c) 2011 Evan Wallace, http://madebyevan.com/webgl-water/, released under the MIT license:
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and
// to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions
// of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO
// THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
// IN THE SOFTWARE.
float psrdnoise(vec2 x, float alpha, out vec2 gradient) {
    vec2 uv = vec2(x.x + x.y * 0.5, x.y);
    vec2 i0 = floor(uv);
    vec2 f0 = fract(uv);
    float cmp = step(f0.y, f0.x);
    vec2 o1 = vec2(cmp, 1.0 - cmp);
    vec2 i1 = i0 + o1;
    vec2 i2 = i0 + 1.0;
    vec2 v0 = vec2(i0.x - i0.y * 0.5, i0.y);
    vec2 v1 = vec2(v0.x + o1.x - o1.y * 0.5, v0.y + o1.y);
    vec2 v2 = vec2(v0.x + 0.5, v0.y + 1.0);
    vec2 x0 = x - v0;
    vec2 x1 = x - v1;
    vec2 x2 = x - v2;
    vec3 iu = vec3(i0.x, i1.x, i2.x);
    vec3 iv = vec3(i0.y, i1.y, i2.y);
    vec3 hash = mod(iu, 289.0);
    hash = mod((hash * 51.0 + 2.0) * hash + iv, 289.0);
    hash = mod((hash * 34.0 + 10.0) * hash, 289.0);
    vec3 psi = hash * 0.07482 + alpha;
    vec3 gx = cos(psi);
    vec3 gy = sin(psi);
    vec2 g0 = vec2(gx.x, gy.x);
    vec2 g1 = vec2(gx.y, gy.y);
    vec2 g2 = vec2(gx.z, gy.z);
    vec3 w = 0.8 - vec3(dot(x0, x0), dot(x1, x1), dot(x2, x2));
    w = max(w, 0.0);
    vec3 w2 = w * w;
    vec3 w4 = w2 * w2;
    vec3 gdotx = vec3(dot(g0, x0), dot(g1, x1), dot(g2, x2));
    float n = dot(w4, gdotx);
    vec3 w3 = w2 * w;
    vec3 dw = -8.0 * w3 * gdotx;
    gradient = 10.9 * (w4.x * g0 + dw.x * x0 + w4.y * g1 + dw.y * x1 + w4.z * g2 + dw.z * x2);
    return 10.9 * n;
}

// uUser[0] = (-, surface y, lower edge y, seed)   uUser[1] = (-, drift x, drift y, clock of the pattern inside)
// (screen px, y down; uTime is the surface's clock)
// uUser[2..4] = deep / mid / shallow colour, w = caustics / shafts / specks   uUser[5] = foam colour, w = wave px
// uUser[6] = light colour, w = froth   uUser[7] = (drawn width, drawn height, margin drawn past each side, -)

float h21(vec2 p) { return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453); }
vec2 h22(vec2 p) { return fract(sin(vec2(dot(p, vec2(127.1, 311.7)), dot(p, vec2(269.5, 183.3)))) * 43758.5453); }

// a wavy line around 0: two long swells and a noise chop (negative = up)
float wave(float x, float t, float amp) {
    vec2 g;
    float n = psrdnoise(vec2(x * 0.0045, t * 0.23), t * 0.9, g);
    return amp * (0.55 * sin(x * 0.0061 + t * 1.25) + 0.28 * sin(x * 0.0157 - t * 1.85 + 1.7) + 0.40 * n);
}

// caustic light: rays bent by a wavy surface (displaced along the slope of a flow noise) bunch up into a web of
// bright lines where the displaced grid folds, brightness = 1 / |det(Jacobian)|
vec2 bend(vec2 q, float a) {
    vec2 g;
    vec2 g2;
    psrdnoise(q, a, g);
    psrdnoise(q * 1.93 + vec2(3.1, 7.7), -a * 1.3, g2);
    return 0.10 * (g + 0.965 * g2);
}
float caustics(vec2 q, float t) {
    float e = 0.02;
    float a = t * 0.6;
    vec2 d0 = bend(q, a);
    vec2 jx = vec2(1.0, 0.0) + (bend(q + vec2(e, 0.0), a) - d0) / e;
    vec2 jy = vec2(0.0, 1.0) + (bend(q + vec2(0.0, e), a) - d0) / e;
    float c = 1.0 / max(abs(jx.x * jy.y - jx.y * jy.x), 0.08);
    return smoothstep(0.55, 3.6, c);
}

// light shafts fanning down from a light far above the surface
float shafts(vec2 p, float ref, float t) {
    vec2 d = p - vec2(1260.0, ref - 1500.0);
    float a = atan(d.x, d.y);
    vec2 g;
    float n = psrdnoise(vec2(a * 15.0, t * 0.06), t * 0.33, g);
    n += 0.6 * psrdnoise(vec2(a * 34.0 + 4.0, t * 0.09), -t * 0.47, g);
    float dy = p.y - ref;
    return smoothstep(0.2, 1.15, n) * exp(-dy / 720.0) * smoothstep(-30.0, 180.0, dy);
}

// drifting specks in three depths, rising slowly
float specks(vec2 q, float t, float seed) {
    float s = 0.0;
    for (int i = 0; i < 3; i++) {
        float fi = float(i);
        float cell = 75.0 + 65.0 * fi;
        vec2 c = (q + vec2(-6.0 - 5.0 * fi, 12.0 + 11.0 * fi) * t) / cell + vec2(fi * 7.31, fi * 3.17);
        vec2 id = floor(c);
        vec2 f = fract(c);
        vec2 h = h22(id + seed);
        vec2 at = 0.25 + 0.5 * h + 0.12 * vec2(sin(t * (0.5 + h.x) + h.y * 6.28), cos(t * (0.4 + h.y) + h.x * 6.28));
        float r = (2.3 + 1.7 * fi) / cell;
        float on = step(h21(id + seed + 13.7), 0.62);
        s += on * (1.0 - smoothstep(r * 0.2, r, length(f - at))) * (0.55 + 0.35 * fi);
    }
    return s;
}

void main() {
    vec2 uv = gl_FragCoord.xy / uRes;
    vec2 size = uUser[7].xy + 2.0 * uUser[7].z;
    vec2 p = uv * size - uUser[7].z;
    float px = size.y / uRes.y;                 // screen px per rendered pixel
    float t = uTime;
    float ti = uUser[1].w;
    float top = uUser[0].y;
    float bot = uUser[0].z;
    float seed = uUser[0].w;
    vec2 q = p + uUser[1].yz;
    vec3 deep = uUser[2].rgb;
    vec3 mid = uUser[3].rgb;
    vec3 shallow = uUser[4].rgb;
    vec3 foam = uUser[5].rgb;
    vec3 sky = uUser[6].rgb;
    float amp = uUser[5].w;
    float froth = uUser[6].w;

    float ref = max(top, -160.0);               // the light comes in here
    float dy = p.y - ref;
    float k = clamp(dy / 1150.0, 0.0, 1.0);
    vec3 col = mix(shallow, mid, smoothstep(0.0, 0.5, k));
    col = mix(col, deep, smoothstep(0.38, 1.0, k));

    // everything inside wobbles a little, as seen through moving water
    vec2 gw;
    psrdnoise(q * vec2(0.0055, 0.008) + vec2(0.0, ti * 0.11), ti * 0.6, gw);
    vec2 qr = q + gw * 12.0;

    float near = exp(-max(dy, 0.0) / 380.0);
    float sh = shafts(p, ref, ti) * uUser[3].w;
    vec2 gp;
    float spots = smoothstep(-0.45, 0.55, psrdnoise(q / 720.0 + vec2(ti * 0.012, 0.0), ti * 0.08, gp));
    float ca = caustics(qr / 190.0 + vec2(ti * 0.012, 0.0), ti) * uUser[2].w * spots * (0.12 + 0.88 * near);
    col += sky * sh * (1.0 - 0.55 * k);
    col += mix(shallow, sky, 0.55) * ca;
    col += vec3(0.72, 0.90, 1.0) * specks(q, ti, seed) * uUser[4].w * (0.55 + 1.3 * sh);

    vec2 v = (p / uUser[7].xy - 0.5) * vec2(1.0, 1.25);
    col *= 1.0 - 0.32 * dot(v, v);

    float alpha = 1.0;
    if (top > -150.0) {
        float ys = top + wave(p.x, t, amp);
        float yb = top - 1.8 * amp + 0.7 * wave(p.x + 517.0, t * 0.8 + 3.0, amp);
        float d = p.y - ys;
        // the top of the water seen just above the waterline: a pale strip between the far and the near wave
        float back = clamp((p.y - yb) / px + 0.5, 0.0, 1.0);
        vec2 gs;
        float rip = psrdnoise(vec2(q.x * 0.011, q.y * 0.05 + t * 0.4), t * 1.3, gs);
        vec3 topCol = mix(shallow, sky, 0.45 + 0.12 * rip);
        float front = clamp(d / px + 0.5, 0.0, 1.0);
        // under the waterline: the underside of the surface, lit through where it is steep and a mirror where
        // the view grazes it (Fresnel, and total internal reflection past the critical angle)
        vec2 gn;
        psrdnoise(vec2(q.x * 0.019, d * 0.045 + t * 0.35), t * 1.1, gn);
        vec3 n = normalize(vec3(-0.22 * gn.x, -1.0, -0.22 * gn.y));
        float e = mix(1.30, 0.10, clamp(d / 95.0, 0.0, 1.0));
        vec3 ray = vec3(0.0, sin(e), cos(e));
        float c = dot(n, -ray);
        float through = smoothstep(-0.15, 0.45, 1.0 - 1.777 * (1.0 - c * c));   // 0 past the critical angle
        float fres = mix(0.5, 1.0, pow(1.0 - c, 3.0));
        vec3 under = mix(mix(mid, shallow, 0.7), sky, (1.0 - fres) * through);
        float band = (1.0 - smoothstep(0.0, 120.0, d)) * step(0.0, d);
        col = mix(col, under, 0.62 * band);
        // foam under the crest and the bright crest line
        vec2 gf;
        float fo = psrdnoise(vec2(q.x * 0.035, d * 0.09 - t * 0.6), t * 1.6, gf);
        col = mix(col, foam, clamp(smoothstep(0.25, 0.75, fo) * exp(-max(d, 0.0) / 9.0) * 0.8 * froth, 0.0, 1.0));
        col = mix(col, foam, exp(-(d * d) / (4.5 * px * px)));
        col = mix(topCol, col, front);
        alpha = max(front, 0.82 * back);
        col = mix(col, foam, 0.7 * exp(-pow((p.y - yb) / (1.6 * px), 2.0)) * (1.0 - front));
    }
    if (bot < 5000.0) {
        // a lifted body of water: its lower edge, with a bright rim and froth just above it
        float ye = bot + wave(p.x + 1311.0, t * 1.1 + 7.0, amp);
        float d = ye - p.y;
        vec2 gf;
        float fo = psrdnoise(vec2(q.x * 0.03, d * 0.08 + t * 0.7), t * 1.4, gf);
        col = mix(col, foam, clamp(smoothstep(0.2, 0.8, fo) * exp(-max(d, 0.0) / 14.0) * 0.75 * froth, 0.0, 1.0));
        col = mix(col, mix(shallow, sky, 0.5), (1.0 - smoothstep(0.0, 90.0, d)) * 0.45);
        col = mix(col, foam, exp(-(d * d) / (4.5 * px * px)));
        alpha *= clamp(d / px + 0.5, 0.0, 1.0);
    }

    col += (h21(gl_FragCoord.xy + fract(t * 7.13)) - 0.5) / 255.0;
    frag = vec4(clamp(col, 0.0, 1.0), alpha);
}
]==]

local function clampDt(dt)
    dt = tonumber(dt) or 0
    if dt < 0 then return 0 end
    if dt > 0.5 then return 0.5 end
    return dt
end

local function setLook(s)
    s:SetSkyUniform(2, LOOK.deep[1], LOOK.deep[2], LOOK.deep[3], LOOK.caustics)
    s:SetSkyUniform(3, LOOK.mid[1], LOOK.mid[2], LOOK.mid[3], LOOK.shafts)
    s:SetSkyUniform(4, LOOK.shallow[1], LOOK.shallow[2], LOOK.shallow[3], LOOK.specks)
end

function VaultWater.new(opts)
    opts = opts or {}
    local t0 = 100 * math.random()
    local self = setmetatable({ ok = false, w = opts.w or 1920, h = opts.h or 1080, t = t0, ti = t0,
        seed = math.floor(1000 * math.random()), dx = 0, dy = 0, waves = LOOK.waves, froth = 1, churn = 1,
        last = nil }, VaultWater)
    if SCENE3D == nil then return self end
    local scale = math.max(0.25, math.min(1, tonumber(opts.scale) or SCALE))
    self.rw = math.max(16, math.floor((self.w + 2 * EDGE) * scale + 0.5))
    self.rh = math.max(16, math.floor((self.h + 2 * EDGE) * scale + 0.5))
    local made, scene = pcall(function() return SCENE3D:CreateScene(self.rw, self.rh) end)
    if not made or scene == nil then return self end
    self.scene = scene
    local ok = pcall(function()
        scene:SetMode("raster")
        assert(scene:GetMode() == "raster", "no GPU renderer")
        scene:SetLighting(false)
        scene:SetFog(false, 0, 0, 0, 0, 0)
        setLook(scene)
        scene:SetSkyUniform(7, self.w, self.h, EDGE, 0)
        scene:SetSkyShader(SHADER)
    end)
    -- render once now, so the shader compiles before the first frame that shows it
    ok = ok and self:render(-NONE, NONE)
    ok = ok and scene:SkyShaderActive()
    if not ok then
        self:dispose()
        return self
    end
    self.ok = true
    return self
end

function VaultWater:update(dt)
    dt = clampDt(dt)
    self.t = self.t + dt
    self.ti = self.ti + dt * self.churn
end

function VaultWater:setDrift(x, y)
    self.dx, self.dy = x or 0, y or 0
end

function VaultWater:setSwell(waves, froth, churn)
    self.waves, self.froth = tonumber(waves) or LOOK.waves, tonumber(froth) or 1
    self.churn = math.max(0, tonumber(churn) or 1)
end

-- renders unless this very frame is already on the canvas
function VaultWater:render(top, bottom)
    local s = self.scene
    if s == nil or s.IsDisposed then return false end
    local key = string.format("%.4f|%.4f|%.1f|%.1f|%.1f|%.1f|%.2f|%.3f", self.t, self.ti, top, bottom, self.dx, self.dy,
        self.waves, self.froth)
    if key == self.last then return true end
    local ok = pcall(function()
        s:SetSkyUniform(0, 1, top, bottom, self.seed)
        s:SetSkyUniform(1, 0, self.dx, self.dy, self.ti)
        s:SetSkyUniform(5, LOOK.foam[1], LOOK.foam[2], LOOK.foam[3], self.waves)
        s:SetSkyUniform(6, LOOK.sky[1], LOOK.sky[2], LOOK.sky[3], self.froth)
        s:SetTime(self.t)
        s:Render()
        s:Upload()
    end)
    self.last = ok and key or nil
    return ok
end

function VaultWater:drawBody(top, bottom, opacity)
    if not self.ok then return false end
    opacity = opacity or 1
    if opacity <= 0 then return false end
    top = top or -NONE
    bottom = bottom or NONE
    if bottom <= top or top >= self.h + 60 or bottom <= -60 then return false end
    if not self:render(top, bottom) then return false end
    local s = self.scene
    s:SetColor(1, 1, 1)
    s:SetOpacity(opacity)
    s:SetScale((self.w + 2 * EDGE) / self.rw, (self.h + 2 * EDGE) / self.rh)
    s:Draw(-EDGE, -EDGE)
    s:SetScale(1, 1)
    s:SetOpacity(1)
    return true
end

function VaultWater:drawUnderwater(opacity)
    return self:drawBody(nil, nil, opacity)
end

function VaultWater:drawSurface(y, opacity)
    return self:drawBody(y, nil, opacity)
end

function VaultWater:dispose()
    if self.scene ~= nil then
        pcall(function() self.scene:SetSkyShader("") end)
        pcall(function() self.scene:Dispose() end)
    end
    self.scene = nil
    self.ok = false
    self.last = nil
end

return VaultWater
