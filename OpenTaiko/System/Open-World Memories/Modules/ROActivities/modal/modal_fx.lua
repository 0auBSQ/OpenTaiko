---@diagnostic disable: undefined-global, undefined-field, need-check-nil
-- modal_fx.lua — what the unlock sequences share: the once-only cue timeline, a draw helper that
-- sets a texture's scale/opacity/rotation/tint for one draw, full-screen fills, the rarity palette
-- and the particle pools (sparkles, smoke puffs, dust). The curves come from Lib/Easing and the
-- colour maths from Lib/Color; Fx re-exports the curves the flows use.

local Easing = require("Easing")
local Color = require("Color")

local Fx = {}

local sin, cos, pi, min, max, floor, random = math.sin, math.cos, math.pi, math.min, math.max, math.floor, math.random

Fx.clamp01, Fx.span, Fx.lerp, Fx.hump = Easing.clamp01, Easing.span, Easing.lerp, Easing.hump
Fx.inQuad, Fx.outQuad, Fx.inCubic, Fx.outCubic = Easing.inQuad, Easing.outQuad, Easing.inCubic, Easing.outCubic
Fx.outBack, Fx.outBounce = Easing.outBack, Easing.outBounce

-- The engine hands modals a collapsed "modal int" (0 common, 1 uncommon, 2 rare, 3 epic,
-- 4 legendary); an item's own Rarity string is preferred whenever it is known, so poor and mythical
-- keep their own chest and colour.
Fx.RARITIES = { "poor", "common", "uncommon", "rare", "epic", "legendary", "mythical" }
Fx.RARITY_COLOR = {
    poor = { 150, 150, 162 },
    common = { 236, 236, 244 },
    uncommon = { 96, 226, 118 },
    rare = { 72, 156, 255 },
    epic = { 200, 84, 255 },
    legendary = { 255, 172, 64 },
    mythical = { 255, 132, 204 },
}
Fx.RARITY_LANG_INT = { poor = 0, common = 1, uncommon = 2, rare = 3, epic = 4, legendary = 5, mythical = 6 }
Fx.RARITY_RANK = Fx.RARITY_LANG_INT
local MODAL_INT_KEY = { [0] = "common", "uncommon", "rare", "epic", "legendary" }

-- h in 0..1 (wraps), s and v in 0..1 -> { r, g, b } 0..255
function Fx.hsv(h, s, v)
    local r, g, b = Color.hsvToRgb(h % 1, s, v)
    return { floor(r * 255 + 0.5), floor(g * 255 + 0.5), floor(b * 255 + 0.5) }
end

-- the mythical rainbow: a hue at `phase` (0..1, wraps), pulled toward white by `pastel`
function Fx.rainbow(phase, pastel)
    return Fx.hsv(phase, 1 - (pastel or 0), 1)
end

-- a colour pulled toward white by k (0 = as is, 1 = white): the panel tints
function Fx.pastel(col, k)
    return { floor(col[1] + (255 - col[1]) * k + 0.5), floor(col[2] + (255 - col[2]) * k + 0.5), floor(col[3] + (255 - col[3]) * k + 0.5) }
end

-- the colour a rarity draws in; the mythical one cycles with time t
function Fx.rarityColor(key, t)
    if key == "mythical" then return Fx.rainbow((t or 0) * 0.22, 0.12) end
    return Fx.RARITY_COLOR[key] or Fx.RARITY_COLOR.common
end

-- the rarity key for a modal: the item's own string when it names a rarity, else the modal int
function Fx.rarityKey(modalInt, rarityString)
    if type(rarityString) == "string" then
        local k = rarityString:lower()
        if Fx.RARITY_COLOR[k] then return k end
    end
    return MODAL_INT_KEY[max(0, min(4, floor(tonumber(modalInt) or 0)))]
end

-- ── cue timeline: fires each cue once as t passes it; jump() skips silently ──────────────────
local Timeline = {}
Timeline.__index = Timeline

function Fx.timeline()
    return setmetatable({ t = 0, cues = {} }, Timeline)
end
function Timeline:at(time, fn) self.cues[#self.cues + 1] = { at = time, fn = fn, done = false } end
function Timeline:reset() self.t = 0 ; for _, c in ipairs(self.cues) do c.done = false end end
function Timeline:advance(dt)
    self.t = self.t + dt
    for _, c in ipairs(self.cues) do
        if not c.done and self.t >= c.at then c.done = true ; c.fn() end
    end
end
-- moves to a later time and marks every earlier cue as spent without running it
function Timeline:jump(time)
    for _, c in ipairs(self.cues) do if c.at <= time then c.done = true end end
    self.t = time
end

-- ── one-shot draw helper: sets the texture state for this draw and restores the defaults ─────
-- o: { sx, sy, opacity, rot, color = {r,g,b} 0..255, blend, anchor = "center", rx, ry, rw, rh }
function Fx.draw(tex, x, y, o)
    if tex == nil then return end
    o = o or {}
    local sx, sy = o.sx or o.s or 1, o.sy or o.s or 1
    tex:SetScale(sx, sy)
    tex:SetOpacity(o.opacity or 1)
    tex:SetRotation(o.rot or 0)
    local c = o.color
    if c then tex:SetColor(c[1] / 255, c[2] / 255, c[3] / 255) end
    if o.blend then tex:SetBlendMode(o.blend) end
    if o.rw then
        tex:DrawRectAtAnchor(x, y, o.rx or 0, o.ry or 0, o.rw, o.rh, o.anchor or "center")
    else
        tex:DrawAtAnchor(x, y, o.anchor or "center")
    end
    tex:SetScale(1, 1) ; tex:SetOpacity(1) ; tex:SetRotation(0)
    if c then tex:SetColor(1, 1, 1) end
    if o.blend then tex:SetBlendMode("normal") end
end

-- canvases have no rotation or blend mode: scale, opacity and tint only
function Fx.drawCanvas(cv, x, y, o)
    if cv == nil then return end
    o = o or {}
    cv:SetScale(o.sx or o.s or 1, o.sy or o.s or 1)
    cv:SetOpacity(o.opacity or 1)
    local c = o.color
    if c then cv:SetColor(c[1] / 255, c[2] / 255, c[3] / 255) end
    cv:DrawAtAnchor(floor(x + 0.5), floor(y + 0.5), o.anchor or "center")
    cv:SetScale(1, 1) ; cv:SetOpacity(1)
    if c then cv:SetColor(1, 1, 1) end
end

-- ── full-screen fills: one small white canvas, scaled and tinted ─────────────────────────────
local FILL = 16
function Fx.makeFill()
    local cv = CANVAS:CreateCanvas(FILL, FILL)
    cv:Clear(255, 255, 255, 255)
    cv:Upload()
    return cv
end
function Fx.fill(cv, color, alpha)
    if cv == nil or alpha <= 0 then return end
    cv:SetScale(1920 / FILL, 1080 / FILL)
    cv:SetOpacity(min(1, alpha))
    cv:SetColor(color[1] / 255, color[2] / 255, color[3] / 255)
    cv:DrawAtAnchor(960, 540, "center")
    cv:SetScale(1, 1) ; cv:SetOpacity(1) ; cv:SetColor(1, 1, 1)
end

-- ── particles ────────────────────────────────────────────────────────────────────────────────
-- every particle: x, y, vx, vy, life, age, s0, s1 (scale over life), rot, spin, color, fade
-- (alpha curve: "out" fades out, "hump" fades in then out, "rise" fades in over `rise` seconds
-- and stays), gravity, drag
local Pool = {}
Pool.__index = Pool

function Fx.pool(tex, blend) return setmetatable({ tex = tex, blend = blend, list = {} }, Pool) end
function Pool:clear() self.list = {} end
function Pool:count() return #self.list end
function Pool:emit(p)
    p.age = 0
    p.life = p.life or 0.8
    p.s0, p.s1 = p.s0 or 1, p.s1 or (p.s0 or 1)
    p.vx, p.vy = p.vx or 0, p.vy or 0
    p.rot, p.spin = p.rot or 0, p.spin or 0
    p.gravity = p.gravity or 0
    p.fade = p.fade or "out"
    p.alpha = p.alpha or 1
    self.list[#self.list + 1] = p
end
-- n sparkles bursting from (x,y): speed range, size, colour
function Pool:burst(x, y, n, o)
    o = o or {}
    for _ = 1, n do
        local a = (o.angle or 0) + (random() - 0.5) * (o.spread or (2 * pi))
        local v = (o.speed or 300) * (0.5 + random())
        self:emit{ x = x + (random() - 0.5) * (o.jitter or 0), y = y + (random() - 0.5) * (o.jitter or 0),
                   vx = cos(a) * v, vy = sin(a) * v, life = (o.life or 0.7) * (0.7 + 0.6 * random()),
                   s0 = (o.size or 0.4) * (0.6 + 0.8 * random()), s1 = o.size1 or 0,
                   rot = random() * 360, spin = (random() - 0.5) * 400, color = o.color, fade = o.fade,
                   gravity = o.gravity or 0, alpha = o.alpha }
    end
end
function Pool:update(dt)
    local l = self.list
    for i = #l, 1, -1 do
        local p = l[i]
        p.age = p.age + dt
        if p.age >= p.life then
            l[i] = l[#l] ; l[#l] = nil
        else
            p.vy = p.vy + p.gravity * dt
            p.x, p.y = p.x + p.vx * dt, p.y + p.vy * dt
            p.rot = p.rot + p.spin * dt
            if p.drag then p.vx, p.vy = p.vx * (1 - p.drag * dt), p.vy * (1 - p.drag * dt) end
        end
    end
end
function Pool:draw(opacity)
    opacity = opacity or 1
    if self.tex == nil then return end
    for _, p in ipairs(self.list) do
        local k = p.age / p.life
        local a
        if p.fade == "hump" then a = Fx.hump(k)
        elseif p.fade == "rise" then a = min(1, p.age / (p.rise or 0.5))
        elseif p.fade == "in" then a = k
        else a = 1 - k end
        local s = Fx.lerp(p.s0, p.s1, k)
        if s > 0.001 and a > 0.005 then
            Fx.draw(self.tex, p.x, p.y, { s = s, opacity = a * p.alpha * opacity, rot = p.rot, color = p.color, blend = self.blend })
        end
    end
end

-- ── screen shake: a decaying jitter the flows add to their draw offsets ──────────────────────
function Fx.shake(amount, t, dur)
    if t < 0 or t >= dur then return 0, 0 end
    local k = 1 - t / dur
    return sin(t * 70) * amount * k, cos(t * 53) * amount * k
end

return Fx
