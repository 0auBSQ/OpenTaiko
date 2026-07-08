---@diagnostic disable: undefined-global, undefined-field, lowercase-global, need-check-nil
-- OWM3d/sky.lua — the procedural GPU sky v2: a full-screen fragment shader (SetSkyShader) drawn
-- behind the 3D pass and mirrored into reflections. The day palette now lives HERE in Lua as
-- Catmull-Rom keyframes over 11 hour stops (smooth dawn→sunrise→day→sunset→dusk→night hue travel,
-- no binary steps), and the shader gets: gradient DITHERING (kills banding), TWO fbm cloud layers
-- drifting against each other with weather-driven coverage, a TWO-SCALE star field with per-star
-- twinkle plus a milky-way band, a sun disc + halo + lens ghosts, and a PHASED moon.
--
-- uUser packing (8 slots):
--   [0] camera forward xyz, tan(fov/2)        [4] zenith rgb, star intensity
--   [1] camera right xyz, aspect              [5] horizon rgb, cloud cover (0..1)
--   [2] camera up xyz, dayF                   [6] sun tint rgb, moon phase (0..1)
--   [3] sun dir xyz (TO the sun), nightF      [7] milky-way axis xyz, band width

local sin, cos, pi, max, min, floor = math.sin, math.cos, math.pi, math.max, math.min, math.floor

local Sky = {}

Sky.SOURCE = [[
vec2 h22(vec2 p){
    p = vec2(dot(p, vec2(127.1, 311.7)), dot(p, vec2(269.5, 183.3)));
    return fract(sin(p) * 43758.5453);
}
float vnoise(vec2 p){
    vec2 i = floor(p); vec2 f = fract(p);
    vec2 u = f * f * (3.0 - 2.0 * f);
    float a = h22(i).x;
    float b = h22(i + vec2(1.0, 0.0)).x;
    float c = h22(i + vec2(0.0, 1.0)).x;
    float d = h22(i + vec2(1.0, 1.0)).x;
    return mix(mix(a, b, u.x), mix(c, d, u.x), u.y);
}
float fbm(vec2 p){
    float v = 0.0; float amp = 0.55;
    for (int i = 0; i < 4; i++){
        v += vnoise(p) * amp;
        p = p * 2.13 + vec2(17.3, 9.1);
        amp *= 0.5;
    }
    return v;
}
// one star lattice at the given cell scale; thr gates star density (lower = more stars)
float starLayer(vec3 dir, float domeY, float scale, float thr, float tw0){
    vec2 sp = dir.xz / (domeY + 0.25) * scale;
    vec2 cell = floor(sp);
    vec2 rnd = h22(cell);
    vec2 pos = cell + rnd;
    float d = length(sp - pos);
    if (rnd.y <= thr) return 0.0;
    float tw = 0.72 + 0.28 * sin(uTime * (tw0 + rnd.x * 4.0) + rnd.y * 40.0);
    return (1.0 - smoothstep(0.0, 0.055 + rnd.x * 0.05, d)) * tw * (0.55 + 0.45 * rnd.x);
}
void main(){
    vec2 uv = gl_FragCoord.xy / uRes;
    vec3 F = uUser[0].xyz; float tanF = uUser[0].w;
    vec3 R = uUser[1].xyz; float aspect = uUser[1].w;
    vec3 U = uUser[2].xyz; float dayF = uUser[2].w;
    vec3 sunD = uUser[3].xyz; float nightF = uUser[3].w;
    vec3 zen = uUser[4].xyz; float starI = uUser[4].w;
    vec3 hor = uUser[5].xyz; float cover = uUser[5].w;
    vec3 sunCol = uUser[6].xyz; float moonPh = uUser[6].w;
    vec3 mwAxis = uUser[7].xyz; float mwWidth = max(uUser[7].w, 0.05);
    float nx = uv.x * 2.0 - 1.0;
    float ny = 1.0 - uv.y * 2.0;
    vec3 dir = normalize(F + R * (nx * tanF * aspect) + U * (ny * tanF));

    float up = clamp(dir.y, -1.0, 1.0);
    float sunUp = clamp(sunD.y, -0.2, 1.0);
    float duskW = clamp(1.0 - abs(sunUp) * 4.0, 0.0, 1.0) * dayF;   // warm band near sunrise/sunset

    // FULL-DOME backdrop: the whole screen is sky (the iso camera looks down — a horizon cutoff
    // would leave most of the frame empty). Gradient spans the view vertically; clouds/stars use
    // |dir.y| so the dome mirrors below the horizon instead of dying there.
    float t = pow(clamp(up * 0.5 + 0.5, 0.0, 1.0), 0.7);
    vec3 col = mix(hor, zen, t);
    float domeY = abs(up);

    // warm scatter around the sun's horizon point at dawn/dusk
    float sunSide = max(dot(normalize(vec3(dir.x, 0.0, dir.z) + 1e-5), normalize(vec3(sunD.x, 0.0, sunD.z) + 1e-5)), 0.0);
    col += vec3(0.9, 0.4, 0.15) * duskW * pow(sunSide, 3.0) * (1.0 - t) * 0.6;

    // clouds: TWO fbm layers drifting against each other (a high slow deck + low fast wisps),
    // coverage driven from Lua (weather overcasts the sky before the rain lands)
    float den = 0.0;
    {
        vec2 cp = dir.xz / (domeY + 0.22);
        vec2 drift1 = vec2(uTime * 0.008, uTime * 0.0032);
        vec2 drift2 = vec2(-uTime * 0.013, uTime * 0.006);
        float lo = 0.62 - cover * 0.34;
        float d1 = smoothstep(lo, lo + 0.24, fbm(cp * 0.55 + drift1));
        float d2 = smoothstep(lo + 0.10, lo + 0.30, fbm(cp * 1.7 + drift2)) * 0.6;
        den = clamp(d1 + d2 * (1.0 - d1), 0.0, 1.0);
        float horFade = smoothstep(0.015, 0.09, domeY);
        vec3 cloudDay = vec3(1.04);
        vec3 cloudLit = mix(mix(cloudDay, vec3(0.09, 0.10, 0.16), nightF), vec3(1.05, 0.60, 0.42), duskW);
        float shade = fbm(cp * 1.15 - drift1 * 1.7);
        cloudLit *= 0.60 + 0.55 * shade;                       // self-shading = puffier clouds
        den = den * horFade;
        col = mix(col, cloudLit, den * 0.92);
    }

    // stars: two lattice scales + per-star twinkle, boosted inside the milky-way band; a faint
    // band glow sells the galaxy even between stars. Whole dome, masked by clouds.
    if (nightF > 0.2){
        float mw = exp(-pow(dot(dir, mwAxis) / mwWidth, 2.0));
        float sf = (nightF - 0.2) * 1.25 * (1.0 - den) * starI;
        float s1 = starLayer(dir, domeY, 34.0, 0.80 - mw * 0.25, 2.0);
        float s2 = starLayer(dir, domeY, 90.0, 0.86 - mw * 0.30, 3.1);
        col += vec3(0.90, 0.93, 1.00) * s1 * 1.55 * sf;
        col += vec3(0.80, 0.86, 1.00) * s2 * 0.9 * sf;
        col += vec3(0.16, 0.17, 0.26) * mw * sf * 0.8;         // the band's nebular glow
    }

    // sun disc + glow (day) — tint travels with the palette keyframes
    float cosSun = dot(dir, sunD);
    if (dayF > 0.02){
        float disc = smoothstep(0.9994, 0.99978, cosSun);
        float glow = pow(max(cosSun, 0.0), 160.0) * 0.8 + pow(max(cosSun, 0.0), 18.0) * 0.16;
        col += sunCol * (disc * 2.2 + glow) * dayF;

        // lens-flare ghosts: two faint discs mirrored through the screen centre (sun on-screen only)
        vec3 rel = sunD - F * dot(sunD, F);
        float sx = dot(rel, R) / (tanF * aspect * max(dot(sunD, F), 1e-3));
        float sy = dot(rel, U) / (tanF * max(dot(sunD, F), 1e-3));
        if (dot(sunD, F) > 0.2 && abs(sx) < 1.2 && abs(sy) < 1.2){
            vec2 spos = vec2(sx, sy);
            vec2 px = vec2(nx, ny);
            for (int g = 1; g <= 2; g++){
                vec2 gp = -spos * (0.35 * float(g));
                float gd = length((px - gp) * vec2(aspect, 1.0));
                float ring = 1.0 - smoothstep(0.03 * float(g), 0.075 * float(g), gd);
                col += sunCol * ring * 0.045 * dayF;
            }
        }
    }
    // moon with PHASE: brightness masked by a second disc offset along the camera right —
    // moonPh 0 = new (thin crescent), 0.5 = full, 1 wraps back
    if (nightF > 0.3){
        vec3 moonD = normalize(vec3(-sunD.x, max(sunD.y, 0.25), -sunD.z));
        float cosMoon = dot(dir, moonD);
        float mdisc = smoothstep(0.99955, 0.99978, cosMoon);
        if (mdisc > 0.0){
            float full = 1.0 - abs(moonPh * 2.0 - 1.0);        // 0 new .. 1 full
            vec3 shadowD = normalize(moonD + R * (0.028 * (1.0 - full) * (moonPh < 0.5 ? 1.0 : -1.0)));
            float sdisc = smoothstep(0.99950, 0.99978, dot(dir, shadowD));
            mdisc *= clamp(1.0 - sdisc * (1.0 - full * full), 0.06, 1.0);
        }
        float mglow = pow(max(cosMoon, 0.0), 260.0) * 0.35;
        vec2 mp = dir.xz * 300.0;
        float crater = 0.85 + 0.15 * vnoise(mp);
        col += vec3(0.86, 0.89, 0.95) * (mdisc * crater + mglow) * (nightF - 0.3) * 1.5;
    }

    // DITHER: +-0.75/255 of hash noise breaks the 8-bit banding the smooth gradient otherwise shows
    col += vec3((h22(gl_FragCoord.xy).x - 0.5) * (1.5 / 255.0));

    frag = vec4(col, 1.0);
}
]]

-- ── palette keyframes (hour → zenith rgb, horizon rgb, sun tint rgb, cloud cover, star intensity) ──
-- Catmull-Rom over the ring: hues TRAVEL through dawn purples → sunrise golds → day blues →
-- sunset oranges → dusk violets → night, instead of snapping between three fixed palettes.
local STOPS = {
    --  hour  zenith                horizon               sun tint             cover star
    {  0.0, { 0.012, 0.018, 0.060 }, { 0.050, 0.070, 0.160 }, { 0.85, 0.87, 0.95 }, 0.34, 1.00 },
    {  4.5, { 0.030, 0.030, 0.095 }, { 0.110, 0.080, 0.190 }, { 0.95, 0.75, 0.60 }, 0.36, 0.85 },
    {  5.5, { 0.100, 0.090, 0.260 }, { 0.560, 0.300, 0.320 }, { 1.00, 0.62, 0.34 }, 0.40, 0.45 },
    {  6.5, { 0.180, 0.300, 0.640 }, { 0.980, 0.590, 0.350 }, { 1.00, 0.72, 0.42 }, 0.42, 0.10 },
    {  8.0, { 0.140, 0.370, 0.860 }, { 0.660, 0.800, 0.960 }, { 1.00, 0.94, 0.80 }, 0.40, 0.00 },
    { 12.0, { 0.130, 0.380, 0.900 }, { 0.600, 0.790, 0.980 }, { 1.00, 0.95, 0.80 }, 0.38, 0.00 },
    { 17.0, { 0.150, 0.350, 0.820 }, { 0.700, 0.720, 0.860 }, { 1.00, 0.88, 0.66 }, 0.42, 0.00 },
    { 18.5, { 0.230, 0.200, 0.520 }, { 1.000, 0.520, 0.260 }, { 1.00, 0.55, 0.24 }, 0.46, 0.15 },
    { 19.5, { 0.120, 0.090, 0.300 }, { 0.520, 0.230, 0.290 }, { 1.00, 0.48, 0.28 }, 0.42, 0.55 },
    { 21.0, { 0.030, 0.030, 0.110 }, { 0.090, 0.090, 0.210 }, { 0.90, 0.82, 0.88 }, 0.36, 0.95 },
    { 24.0, { 0.012, 0.018, 0.060 }, { 0.050, 0.070, 0.160 }, { 0.85, 0.87, 0.95 }, 0.34, 1.00 },
}

local function crSample(p0, p1, p2, p3, f)
    -- Catmull-Rom, componentwise
    local f2, f3 = f * f, f * f * f
    return 0.5 * ((2 * p1) + (-p0 + p2) * f + (2 * p0 - 5 * p1 + 4 * p2 - p3) * f2 + (-p0 + 3 * p1 - 3 * p2 + p3) * f3)
end

-- palette at an hour: { zen = {r,g,b}, hor = {..}, sun = {..}, cover =, star = }
function Sky.paletteAt(hour)
    hour = hour % 24
    local n = #STOPS
    local seg = 1
    for i = 1, n - 1 do
        if hour >= STOPS[i][1] and hour <= STOPS[i + 1][1] then seg = i break end
    end
    local a, b = STOPS[seg], STOPS[seg + 1]
    local f = (b[1] - a[1] > 0) and (hour - a[1]) / (b[1] - a[1]) or 0
    local i0 = (seg > 1) and seg - 1 or n - 1               -- ring neighbours (stop n == stop 1)
    local i3 = (seg + 2 <= n) and seg + 2 or 2
    local p0, p1, p2, p3 = STOPS[i0], a, b, STOPS[i3]
    local out = { zen = {}, hor = {}, sun = {} }
    for c = 1, 3 do
        out.zen[c] = max(0, crSample(p0[2][c], p1[2][c], p2[2][c], p3[2][c], f))
        out.hor[c] = max(0, crSample(p0[3][c], p1[3][c], p2[3][c], p3[3][c], f))
        out.sun[c] = max(0, crSample(p0[4][c], p1[4][c], p2[4][c], p3[4][c], f))
    end
    out.cover = max(0, crSample(p0[5], p1[5], p2[5], p3[5], f))
    out.star = max(0, min(1, crSample(p0[6], p1[6], p2[6], p3[6], f)))
    return out
end

-- attach the shader once; call per frame after the camera settles so the rays match the view
function Sky.install(scene)
    if scene.SetSkyShader then scene:SetSkyShader(Sky.SOURCE) end
end
function Sky.detach(scene)
    if scene.SetSkyShader then scene:SetSkyShader("") end
end

-- world.cam must be applied (scene camera current). dayF/nightF from daynight; sun dir TO the sun.
-- hour drives the palette; coverBoost (0..1, optional) overcasts the sky (weather).
function Sky.update(scene, fovDeg, rw, rh, sunX, sunY, sunZ, dayF, nightF, hour, coverBoost)
    if scene.SetSkyUniform == nil then return end
    local fx, fy, fz = scene:GetCameraForward()
    local rx, ry, rz = scene:GetCameraRight()
    local ux, uy, uz = scene:GetCameraUp()
    local tanF = math.tan(math.rad(fovDeg) * 0.5)
    scene:SetSkyUniform(0, fx, fy, fz, tanF)
    scene:SetSkyUniform(1, rx, ry, rz, rw / rh)
    scene:SetSkyUniform(2, ux, uy, uz, dayF)
    scene:SetSkyUniform(3, sunX, sunY, sunZ, nightF)
    local pal = Sky.paletteAt(hour or 12)
    local cover = min(1, pal.cover + (coverBoost or 0))
    scene:SetSkyUniform(4, pal.zen[1], pal.zen[2], pal.zen[3], pal.star)
    scene:SetSkyUniform(5, pal.hor[1], pal.hor[2], pal.hor[3], cover)
    -- moon phase from the day of the year (~29.5-day cycle; 0.5 = full)
    local yday = (os.date("*t").yday or 180)
    local phase = (yday % 29.53) / 29.53
    scene:SetSkyUniform(6, pal.sun[1], pal.sun[2], pal.sun[3], phase)
    -- the milky-way band: a fixed tilted great circle across the dome
    scene:SetSkyUniform(7, 0.42, 0.50, 0.76, 0.30)
end

return Sky
