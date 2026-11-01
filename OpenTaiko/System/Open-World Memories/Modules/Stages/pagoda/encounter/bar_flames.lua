-- Song-bar flames, kept in the bar's local px (y down, about its centre).

local sprite = require("encounter/sprite")
local text = require("encounter/bar_text")

local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")
local Look = Layout.encounter.bar_flames
local Anim = Animation.encounter.bar_flames

local F = {}
F.__index = F

local RATE, MAX, BACKLOG = Anim.rate, Anim.max, Anim.spawn_backlog
-- spawn zone: share, then rise (px/s), life (s) and size ranges
local function zone(z)
    return { share = z.share, rise = z.rise, life = z.life_s, sx = z.sx, sy = z.sy }
end
local BOTTOM, TOP, ENDS = Anim.zones.bottom, Anim.zones.top, Anim.zones.ends
local ZONES = { zone(BOTTOM), zone(TOP), zone(ENDS) }
local LEAN = Anim.lean_deg              -- degrees either way
local SWAY, SWAY_HZ = Anim.sway_px, Anim.sway_hz
local GLOW_SHARE = Anim.glow_share
local DARK_ALPHA, GLOW_ALPHA = Look.dark_opacity, Look.glow_opacity
local BURN_OUT = Anim.burn_out          -- age speed factor once stopped
local PREWARM, STEP = Anim.prewarm_s, 1 / Anim.prewarm_fps
local FADE_IN = Anim.fade_in_k
local GROW_X0, GROW_X, GROW_Y0, GROW_Y = Anim.grow_x_min, Anim.grow_x, Anim.grow_y_min, Anim.grow_y
local VARIANTS = 3                      -- tongue shapes

local random = math.random

local function rnd(r) return r[1] + random() * (r[2] - r[1]) end

-- w, h = the bar's size in local px
function F.new(w, h)
    local f = setmetatable({ w = w, h = h, parts = {}, acc = 0, on = true }, F)
    for _ = 1, math.floor(PREWARM / STEP) do f:update(STEP) end
    return f
end

local function spawn(f)
    local hw, hh = f.w * 0.5, f.h * 0.5
    local r = random()
    local z, x, y
    if r < ZONES[1].share then
        z = ZONES[1]
        x, y = (random() * 2 - 1) * (hw - BOTTOM.inset_x), hh - BOTTOM.inset_y - random() * BOTTOM.band_y
    elseif r < ZONES[1].share + ZONES[2].share then
        z = ZONES[2]
        x, y = (random() * 2 - 1) * (hw - TOP.inset_x), -hh + TOP.inset_y + random() * TOP.band_y
    else
        z = ZONES[3]
        x, y = (random() < 0.5 and -1 or 1) * (hw - ENDS.inset_x - random() * ENDS.band_x), (random() * 2 - 1) * hh * ENDS.spread_y
    end
    f.parts[#f.parts + 1] = {
        x = x, y = y, vy = -rnd(z.rise), age = 0, life = rnd(z.life),
        sx = rnd(z.sx), sy = rnd(z.sy), lean = (random() * 2 - 1) * LEAN, ph = random() * 6.283,
        glow = random() < GLOW_SHARE, v = random(VARIANTS),
    }
end

function F:update(dt)
    if self.on then
        self.acc = math.min(BACKLOG, self.acc + RATE * dt)
        while self.acc >= 1 and #self.parts < MAX do
            self.acc = self.acc - 1
            spawn(self)
        end
    end
    local k = self.on and 1 or BURN_OUT
    local parts = self.parts
    local i = 1
    while i <= #parts do
        local p = parts[i]
        p.age = p.age + dt * k
        if p.age >= p.life then
            parts[i] = parts[#parts]
            parts[#parts] = nil
        else
            p.y = p.y + p.vy * dt
            i = i + 1
        end
    end
end

function F:stop() self.on = false end

function F:alive() return self.on or #self.parts > 0 end

local _o = { sx = 1, sy = 1, rot = 0, pivot = "foot" }
local _ready = {}

-- texs, metas: one per shape; c = { r, g, b }; glow = true draws the glow pass; alpha 0..1
function F:draw(texs, metas, xf, c, glow, alpha)
    if #self.parts == 0 or alpha <= 0 then return end
    local any = false
    for i = 1, VARIANTS do
        local tx = texs[i]
        _ready[i] = tx ~= nil and tx.Ready and metas[i] ~= nil
        if _ready[i] then
            tx:SetColor(c[1], c[2], c[3])
            tx:SetBlendMode(glow and "Add" or "Normal")
            any = true
        end
    end
    if not any then return end
    local peak = alpha * (glow and GLOW_ALPHA or DARK_ALPHA)
    for _, p in ipairs(self.parts) do
        if (not glow or p.glow) and _ready[p.v] then
            local u = p.age / p.life
            local arch = math.sin(u * math.pi)
            local a = math.min(1, arch * FADE_IN) * peak
            if a > 0.003 then
                local x, y = text.point(xf, p.x + SWAY * math.sin(SWAY_HZ * 6.283 * p.age + p.ph), p.y)
                _o.sx = xf.s * p.sx * (GROW_X0 + GROW_X * arch)
                _o.sy = xf.s * p.sy * (GROW_Y0 + GROW_Y * arch)
                _o.rot = xf.rot + p.lean * (0.5 + u)
                local cx, cy = sprite.center(metas[p.v], x, y, _o)
                local tx = texs[p.v]
                tx:SetScale(_o.sx, _o.sy)
                tx:SetRotation(_o.rot)
                tx:SetOpacity(a)
                tx:DrawAtAnchor(cx, cy, "center")
            end
        end
    end
    for i = 1, VARIANTS do
        if _ready[i] then
            local tx = texs[i]
            tx:SetScale(1, 1) ; tx:SetRotation(0) ; tx:SetOpacity(1)
            tx:SetColor(1, 1, 1) ; tx:SetBlendMode("Normal")
        end
    end
end

return F
