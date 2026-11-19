-- Rainbow: animated hue spectrum across a texture, plus glitter
-- loads no texture; every texture state set is reset after

-- looks: Config/Rainbow/layout.json; motion: Config/Rainbow/animation.json
local HERE = require("Core.Here")(...)
local Config = require("Core.Config")
local Layout = Config.load(HERE .. "Config/Rainbow/layout.json")
local Animation = Config.load(HERE .. "Config/Rainbow/animation.json")

local Color = require("Color")

local Rainbow = {}

local floor, min, sin, cos, rad, random, pi = math.floor, math.min, math.sin, math.cos, math.rad, math.random, math.pi

local Spectrum = Layout.spectrum
local Sparkles = Layout.glitter
local Glitter = Animation.glitter
local STRIP = Spectrum.strip_px  -- source px per strip
local SPEED = Animation.spectrum.speed_turns_per_s

function Rainbow.hsv(h, s, v)
    return Color.hsvToRgb(h % 1, s, v)
end

-- drawn where tex:Draw ("topleft") or DrawAtAnchor ("center") would; the tint multiplies (light art works best)
-- opts (optional; cycles, speed, sat, val, strip default to Config/Rainbow):
--   t        clock, sec                anchor   "topleft" | "center"
--   sx, sy   scale                     opacity  0..1
--   cycles   hue turns across the span        speed  turns per second
--   sat, val saturation and value              rot    degrees, counter-clockwise like SetRotation
--   x0, span screen x and width the cycles run over, to share one spectrum
--   strip    source px per strip
function Rainbow.draw(tex, x, y, opts)
    if tex == nil or not tex.Ready then return end
    opts = opts or {}
    local w, h = tex.Width, tex.Height
    local sx, sy = opts.sx or 1, opts.sy or 1
    -- whole-texture top-left: "center" backs off half the integer size, as the engine does
    local left, top = x, y
    if opts.anchor == "center" then
        left, top = x - floor(w / 2) * sx, y - floor(h / 2) * sy
    end
    local rot = opts.rot or 0
    local ca, sa = cos(rad(rot)), sin(rad(rot))
    local qcx, qcy = left + w * sx * 0.5, top + h * sy * 0.5
    local x0, span = opts.x0 or left, opts.span or (w * sx)
    if span <= 0 then span = 1 end
    local cycles, sat, val = opts.cycles or Spectrum.cycles, opts.sat or Spectrum.sat, opts.val or Spectrum.val
    local phase = -(opts.t or 0) * (opts.speed or SPEED)
    local step = opts.strip or STRIP

    tex:SetScale(sx, sy)
    tex:SetRotation(rot)
    tex:SetOpacity(opts.opacity or 1)
    local rx = 0
    while rx < w do
        local rw = min(step, w - rx)
        -- strip centre from the quad centre along the texture's own x; a rotated quad turns
        -- about its own centre, so each strip is moved to where the whole quad would put it
        local dx = (rx + rw * 0.5) * sx - w * sx * 0.5
        tex:SetColor(Rainbow.hsv(phase + cycles * (qcx + dx - x0) / span, sat, val))
        tex:DrawRectAtAnchor(qcx + dx * ca - rw * sx * 0.5, qcy - dx * sa - h * sy * 0.5,
                             rx, 0, rw, h, "topleft")
        rx = rx + rw
    end
    tex:SetColor(1, 1, 1)
    tex:SetOpacity(1)
    tex:SetRotation(0)
    tex:SetScale(1, 1)
end

-- sparkles over the box (x, y, w, h); state: caller table, sparkles box-relative
-- tex: caller's sparkle, centred; opts: rate (per sec), size (1), opacity (1), max, blend ("Normal")
-- rate and max default to Config/Rainbow
function Rainbow.glitter(state, dt, x, y, w, h, tex, opts)
    opts = opts or {}
    local list = state.list
    if list == nil then
        list = {}
        state.list, state.acc = list, 0
    end
    dt = dt or 0
    local i = 1
    while i <= #list do
        local p = list[i]
        p.age = p.age + dt
        if p.age >= p.life then
            list[i] = list[#list]
            list[#list] = nil
        else
            i = i + 1
        end
    end
    local cap = opts.max or Sparkles.max
    state.acc = min(Glitter.burst_max, (state.acc or 0) + (opts.rate or Glitter.rate_per_s) * dt)
    while state.acc >= 1 do
        state.acc = state.acc - 1
        if #list < cap then
            list[#list + 1] = { u = random(), v = random(), age = 0, life = Glitter.life_s + Glitter.life_range_s * random(),
                                s = Sparkles.scale_min + Sparkles.scale_range * random(), rot = random() * Sparkles.rot_range_deg,
                                spin = (random() * 2 - 1) * Glitter.spin_deg_per_s }
        end
    end

    if tex == nil or not tex.Ready or #list == 0 then return end
    local size, op = opts.size or 1, opts.opacity or 1
    tex:SetBlendMode(opts.blend or "Normal")
    for _, p in ipairs(list) do
        local k = sin(pi * p.age / p.life)
        local s = size * p.s * k
        tex:SetScale(s, s)
        tex:SetRotation(p.rot + p.spin * p.age)
        tex:SetOpacity(op * k)
        tex:DrawAtAnchor(x + p.u * w, y + p.v * h, "center")
    end
    tex:SetBlendMode("Normal")
    tex:SetOpacity(1)
    tex:SetRotation(0)
    tex:SetScale(1, 1)
end

return Rainbow
