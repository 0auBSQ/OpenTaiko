-- Non-particle light draws; every handle is reset after use.

local sprite = require("encounter/sprite")
local Config = require("Core.Config")
local Animation = Config.load("Config/animation.json")

local Light = {}

local TAU = math.pi * 2
local motion = Animation.fx.light
local GRAIN_HZ = motion.grain_steps_per_s
local FLICKER_BASE = motion.flicker.base
local amps = motion.flicker.wave_amps
local rates = motion.flicker.wave_rad_per_s
local FL_A1, FL_A2, FL_A3 = amps[1], amps[2], amps[3]
local FL_W1, FL_W2, FL_W3 = rates[1], rates[2], rates[3]
local rays = motion.rays
local RAY_PERIOD, RAY_PHASE = rays.default_period_s, rays.default_phase_step
local RAY_A_MIN, RAY_A_MAX = rays.default_min_opacity, rays.default_max_opacity
local RAY_SWAY_PERIOD, RAY_SWAY_PHASE = rays.sway_period_ratio, rays.sway_phase_ratio

local _metas = setmetatable({}, { __mode = "k" })
local _o = { sx = 1, sy = 1, rot = 0, pivot = "top" }

local function reset(tx)
    tx:SetScale(1, 1) ; tx:SetRotation(0) ; tx:SetOpacity(1)
    tx:SetColor(1, 1, 1) ; tx:SetBlendMode("Normal")
end

local function hash(n)
    local v = math.sin(n * 127.1 + 311.7) * 43758.5453
    return v - math.floor(v)
end

-- the shaft pivots at its top centre
local function top_meta(tx)
    local m = _metas[tx]
    if m == nil then
        local w, h = tx.Width, tx.Height
        m = { w = w, h = h, fx = w * 0.5, fy = 0 }
        _metas[tx] = m
    end
    return m
end

function Light.flicker(t, seed)
    local s = seed or 0
    return FLICKER_BASE + FL_A1 * math.sin(FL_W1 * t + s) + FL_A2 * math.sin(FL_W2 * t + 2 * s)
                + FL_A3 * math.sin(FL_W3 * t + 3 * s)
end

function Light.shaft(tx, x, y, rot, sx, sy, r, g, b, a)
    if tx == nil or not tx.Ready or a <= 0 then return end
    _o.sx, _o.sy, _o.rot = sx or 1, sy or 1, rot or 0
    local cx, cy = sprite.center(top_meta(tx), x, y, _o)
    tx:SetBlendMode("Add")
    tx:SetScale(_o.sx, _o.sy)
    tx:SetRotation(_o.rot)
    tx:SetColor(r or 1, g or 1, b or 1)
    tx:SetOpacity(math.min(1, a))
    tx:DrawAtAnchor(cx, cy, "center")
    reset(tx)
end

function Light.glow(tx, x, y, scale, r, g, b, a)
    if tx == nil or not tx.Ready or a <= 0 then return end
    tx:SetBlendMode("Add")
    tx:SetScale(scale, scale)
    tx:SetColor(r or 1, g or 1, b or 1)
    tx:SetOpacity(math.min(1, a))
    tx:DrawAtAnchor(x, y, "center")
    reset(tx)
end

-- w, h: the area from the top-left corner, the whole screen when absent
function Light.vignette(tx, a, w, h)
    if tx == nil or not tx.Ready or a <= 0 then return end
    local res = THEME:GetResolution()
    tx:SetBlendMode("Normal")
    tx:SetScale((w or res.X) / tx.Width, (h or res.Y) / tx.Height)
    tx:SetOpacity(math.min(1, a))
    tx:DrawAtAnchor(0, 0, "topleft")
    reset(tx)
end

-- tiled through the Repeat wrap over w x h from the top-left corner (the whole screen when absent)
function Light.grain(tx, a, t, w, h)
    if tx == nil or not tx.Ready or a <= 0 then return end
    local res = THEME:GetResolution()
    local step = math.floor((t or 0) * GRAIN_HZ)
    local ox = math.floor(hash(step) * tx.Width)
    local oy = math.floor(hash(step + 57) * tx.Height)
    local wrap = tx:GetWrapMode()
    if wrap ~= "Repeat" then tx:SetWrapMode("Repeat") end
    tx:SetBlendMode("Normal")
    tx:SetOpacity(math.min(1, a))
    tx:DrawRect(0, 0, ox, oy, math.floor(w or res.X), math.floor(h or res.Y))
    if wrap ~= "Repeat" then tx:SetWrapMode(wrap) end
    reset(tx)
end

-- list entries: {x, y, rot, sway, sx, sy, r, g, b, aMin, aMax, period[, phase]}
-- opacity between aMin and aMax and rot within +-sway degrees, each on its own sine
local Rays = {}
Rays.__index = Rays

function Light.rays(tx, list)
    return setmetatable({ tx = tx, list = list or {}, t = 0 }, Rays)
end

function Rays:update(dt)
    self.t = self.t + dt
end

-- xf, scale: optional placement transform (x, y) -> sx, sy and its scale; ox, oy apply without one
function Rays:draw(ox, oy, mulR, mulG, mulB, mulA, xf, scale)
    ox, oy, scale = ox or 0, oy or 0, scale or 1
    mulR, mulG, mulB, mulA = mulR or 1, mulG or 1, mulB or 1, mulA or 1
    local t = self.t
    for i, ry in ipairs(self.list) do
        local period = ry.period or RAY_PERIOD
        local ph = ry.phase or i * RAY_PHASE
        local k = 0.5 + 0.5 * math.sin(TAU * t / period + ph)
        local a = ((ry.aMin or RAY_A_MIN) + ((ry.aMax or RAY_A_MAX) - (ry.aMin or RAY_A_MIN)) * k) * mulA
        local rot = (ry.rot or 0) + (ry.sway or 0) * math.sin(TAU * t / (period * RAY_SWAY_PERIOD) + ph * RAY_SWAY_PHASE)
        local x, y = ry.x + ox, ry.y + oy
        if xf ~= nil then x, y = xf(ry.x, ry.y) end
        Light.shaft(self.tx, x, y, rot, (ry.sx or 1) * scale, (ry.sy or 1) * scale,
                    (ry.r or 1) * mulR, (ry.g or 1) * mulG, (ry.b or 1) * mulB, a)
    end
end

return Light
