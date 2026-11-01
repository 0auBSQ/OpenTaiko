-- Fail-collision burst at the hit point; fire() again restarts it.

local assets  = require("encounter/assets")
local sprite  = require("encounter/sprite")
local Easing  = require("Easing")
local Emitter = require("fx/emitter")
local presets = require("fx/presets")
local Config  = require("Core.Config")
local Animation = Config.load("Config/animation.json")

local M = {}

local motion = Animation.fx.impact

local REF_W = 1920
local POP_SEC, HOLD_SEC, OUT_SEC = motion.pop_s, motion.hold_s, motion.out_s
local POP_FROM  = motion.pop_from_scale     -- starting scale
local POP_BACK  = motion.pop_overshoot      -- Easing.outBack overshoot
local POP_FADE  = motion.pop_fade_in_share
local OUT_GROW  = motion.out_grow_scale     -- extra scale while it fades
local TILT      = motion.tilt_deg           -- random tilt range
local SPARKS    = motion.sparks

local _tex, _meta = nil, nil
local _sparks = nil
local _t = nil                  -- seconds since fire(), nil when idle
local _x, _y, _rot = 0, 0, 0
local _o = { pivot = "foot" }

function M.keys()
    local keys = { "fx_ouch" }
    for _, k in ipairs(Emitter.texKeys(presets.impact_spark)) do keys[#keys + 1] = k end
    return keys
end

function M.open()
    M.close()
    _tex, _meta = assets.get("fx_ouch")
    local p = presets.impact_spark
    if p ~= nil then
        local map = {}
        for _, k in ipairs(Emitter.texKeys(p)) do map[k] = assets.get(k) end
        _sparks = Emitter.new(p, map)
    end
end

-- x, y: screen px of the burst's centre
function M.fire(x, y)
    _x, _y = x, y
    _t = 0
    _rot = (math.random() * 2 - 1) * TILT
    if _sparks ~= nil then _sparks:burst(SPARKS, x, y) end
end

function M.update(dt)
    if _t ~= nil then
        _t = _t + dt
        if _t >= POP_SEC + HOLD_SEC + OUT_SEC then _t = nil end
    end
    if _sparks ~= nil then _sparks:update(dt) end
end

-- scale and opacity of the burst at time t
local function pose(t)
    if t < POP_SEC then
        local u = t / POP_SEC
        return POP_FROM + (1 - POP_FROM) * Easing.outBack(u, POP_BACK), math.min(1, u / POP_FADE)
    end
    local f = Easing.span(t, POP_SEC + HOLD_SEC, OUT_SEC)
    return 1 + OUT_GROW * Easing.outQuad(f), 1 - Easing.inQuad(f)
end

function M.draw(ox, oy)
    ox, oy = ox or 0, oy or 0
    if _t ~= nil and _meta ~= nil then
        local s, a = pose(_t)
        local k = THEME:GetResolution().X / REF_W
        _o.sx, _o.sy, _o.rot, _o.alpha = s * k, s * k, _rot, a
        sprite.draw(_tex, _meta, _x + ox, _y + oy, _o)
    end
    if _sparks ~= nil then _sparks:draw(ox, oy, 1, 1, 1, 1) end
end

function M.close()
    if _sparks ~= nil then _sparks:clear() end
    _sparks = nil
    _tex, _meta = nil, nil
    _t = nil
end

return M
