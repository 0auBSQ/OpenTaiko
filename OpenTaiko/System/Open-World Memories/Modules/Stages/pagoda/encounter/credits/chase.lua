-- Fourth end-credits background (the level-11 room).

local Easing  = require("Easing")
local assets  = require("encounter/assets")
local view    = require("encounter/bands/view")
local pagoda  = require("encounter/bands/pagoda")
local moves   = require("encounter/cast_moves")
local actor   = require("encounter/credits/actor")
local Emitter = require("fx/emitter")
local presets = require("fx/presets")
local Light   = require("fx/light")
local Config  = require("Core.Config")
local Layout    = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")

local M = {}

M.holes = false

local Look = Layout.encounter.credits.chase
local Anim = Animation.encounter.credits.chase

local LEVEL = 11
local DOOR_X, FLOOR_Y = Look.door_x, Look.floor_y  -- the door's middle, Ume's mark
local MID_X = Look.mid_x
local GONE_X = Look.gone_x

-- the door climb of encounter/cast_moves.lua run backwards
local DESCENT = Anim.descent

-- seconds into the scene
local UME_DOWN_AT, UME_TURN_AT = Anim.ume_down_at_s, Anim.ume_turn_at_s
local WALK_AT, WALK_SEC, WALK_K = Anim.walk_at_s, Anim.walk_s, Anim.walk_k     -- WALK_K: the run cycle's share in the walk
local WALK_EASE = Anim.walk_ease
local NOTICE_AT, STARTLE_PX, STARTLE_SEC = Anim.notice_at_s, Anim.startle_px, Anim.startle_s
local FLEE_AT = Anim.flee_at_s
local IDLE_RAMP = Anim.idle_ramp_s
local CHASERS = {}
for i, c in ipairs(Look.chasers) do
    CHASERS[i] = { at = Anim.chasers[i].at_s, x = c.x, y = c.y, chase = Anim.chasers[i].chase_at_s }
end
local CHASE_HOP, CHASE_HOP_SEC, CHASE_LEAN = Anim.chase_hop_px, Anim.chase_hop_s, Anim.chase_lean_deg

local SHAFT_COL = Look.shaft_color
local SHAFTS = {}
for i, sh in ipairs(Look.shafts) do
    SHAFTS[i] = { x = sh.x, y = sh.y, rot = sh.rot, sx = sh.sx, sy = sh.sy, period = Anim.shaft_periods_s[i] }
end
local SHAFT_A = Look.shaft_opacity
local GRAIN, VIGNETTE = Look.grain_opacity, Look.vignette_opacity
local SHAFT_BASE, SHAFT_AMP = Anim.shaft_pulse_base, Anim.shaft_pulse_amp

local UME_IDLE = Config.plain(Anim.ume_idle)

local _t, _u = 0, 0
local _tint = { 1, 1, 1 }
local _bg, _bgMeta, _door, _doorMeta = nil, nil, nil, nil
local _shadow, _shadowMeta = nil, nil
local _dust, _specks = nil, nil
local _ume = nil
local _chasers = {}
local _list = {}
local _noticed = false

function M.keys()
    return { "bg_pagoda", "ov_door_open", "fx_shaft", "fx_mote", "fx_grain", "fx_vignette", "fx_shadow",
             "ume_run", "ume_run_m", "ume_normal", "ume_normal_m", "ume_brave", "m_norm3" }
end

function M.open()
    M.close()
    _t, _u, _noticed = 0, 0, false
    _tint = pagoda.plateTint(LEVEL)
    _bg, _bgMeta = assets.get("bg_pagoda")
    _door, _doorMeta = assets.get("ov_door_open")
    _shadow, _shadowMeta = assets.get("fx_shadow")
    local mote = assets.get("fx_mote")
    if presets.interior_dust ~= nil and mote ~= nil then _dust = Emitter.new(presets.interior_dust, mote) end
    if presets.ceiling_specks ~= nil and mote ~= nil then _specks = Emitter.new(presets.ceiling_specks, mote) end
    _ume = actor.new("ume_run", DOOR_X, FLOOR_Y, { idle = UME_IDLE, seed = 113, shadowKey = "ume_brave" })
    _ume.visible, _ume.idleK = false, 0
    _list = { _ume }
    local idle = actor.monsterIdle("norm3")
    for i, c in ipairs(CHASERS) do
        local a = actor.new("m_norm3", c.x, c.y, { idle = idle, seed = 117 + i })
        a.visible = false
        _chasers[i] = a
        _list[#_list + 1] = a
    end
end

-- actor a coming down to (x1, y1), t seconds in; returns false once in the doorway
local function descend(a, x1, y1, t)
    local D = DESCENT
    local u = 1 - Easing.clamp01(t / D.s)
    local i = math.min(D.steps - 1, math.floor(u * D.steps))
    local f = u * D.steps - i
    local p = (i + f * f * (3 - 2 * f)) / D.steps
    local s = 1 - D.shrink * p
    local side = (i % 2 == 0) and 1 or -1
    local onStep = u > 0 and u < 1
    a.visible = true
    a.x, a.y, a.gy = x1 + D.dx * p, y1 + D.dy * p, y1 + D.dy * p
    a.dy = onStep and -D.hop * math.sin(math.pi * f) or 0
    a.rot = onStep and side * D.tilt * math.sin(math.pi * f) or 0
    a.sx, a.sy = s, s
    a.alpha = 1 - Easing.inOutSine((u - D.fade_at) / (1 - D.fade_at))
    a.shadowA = a.alpha
    return t < D.s
end

local function still(a)
    a.dy, a.rot, a.sx, a.sy, a.alpha, a.shadowA = 0, 0, 1, 1, 1, 1
end

local function updateUme(dt)
    local a = _ume
    if _u < UME_DOWN_AT then
        a.visible = false
    elseif _u < UME_TURN_AT then
        if not descend(a, DOOR_X, FLOOR_Y, _u - UME_DOWN_AT) then still(a) end
    elseif _u < WALK_AT then
        still(a)
        a.x, a.y, a.gy = DOOR_X, FLOOR_Y, FLOOR_Y
        a:face("ume_run_m", true)
    elseif _u < WALK_AT + WALK_SEC then
        local k = (_u - WALK_AT) / WALK_SEC
        local e = math.min(1, k / WALK_EASE, (1 - k) / WALK_EASE)
        local rot, hop = moves.waddle(_u - WALK_AT)
        a.x = DOOR_X + (MID_X - DOOR_X) * Easing.inOutSine(k)
        a.rot, a.dy = -rot * e * WALK_K, -hop * e * WALK_K
    elseif _u < FLEE_AT then
        still(a)
        a.x = MID_X
        if _u < NOTICE_AT then
            a:face("ume_normal_m", false)
        else
            if not _noticed then
                _noticed = true
                a:face("ume_normal", true)
                a:notice()
            end
            a.dy = -STARTLE_PX * Easing.hump((_u - NOTICE_AT) / STARTLE_SEC)
        end
        a.idleK = math.min(1, a.idleK + dt / IDLE_RAMP)
    else
        a.idleK = 0
        a:face("ume_run_m", true)
        local x, _, dy, rot = moves.run(0, FLOOR_Y, _u - FLEE_AT, "right")
        a.x, a.dy, a.rot = MID_X - x, dy, -rot
        a.visible = a.x > GONE_X
    end
end

local function updateChaser(a, c)
    if _u < c.at then
        a.visible = false
    elseif _u < c.chase then
        if not descend(a, c.x, c.y, _u - c.at) then still(a) end
    else
        still(a)
        local x = moves.run(0, c.y, _u - c.chase, "right")
        local q = (_u - c.chase) / CHASE_HOP_SEC
        a.x = c.x - x
        a.dy = -CHASE_HOP * math.abs(math.sin(math.pi * q))
        a.rot = CHASE_LEAN * Easing.outQuad(q)
        a.visible = a.x > GONE_X
    end
end

function M.update(dt, u)
    _t = _t + dt
    _u = u
    if _dust ~= nil then _dust:update(dt) end
    if _specks ~= nil then _specks:update(dt) end
    updateUme(dt)
    for i, c in ipairs(CHASERS) do updateChaser(_chasers[i], c) end
    for _, a in ipairs(_list) do a:update(dt) end
end

-- a = opacity, c = brightness
function M.draw(a, c)
    if a <= 0 then return end
    local r, g, b = _tint[1] * c, _tint[2] * c, _tint[3] * c
    view.drawPlaced(_bg, _bgMeta, r, g, b, a)
    view.drawPlaced(_door, _doorMeta, r, g, b, a)
    local shaft = assets.get("fx_shaft")
    local s = view.scale()
    for _, sh in ipairs(SHAFTS) do
        local x, y = view.xf(sh.x, sh.y)
        local k = SHAFT_A * (SHAFT_BASE + SHAFT_AMP * math.sin(_t * 2 * math.pi / sh.period)) * a
        Light.shaft(shaft, x, y, sh.rot, sh.sx * s, sh.sy * s, SHAFT_COL[1] * r, SHAFT_COL[2] * g, SHAFT_COL[3] * b, k)
    end
    if _dust ~= nil then _dust:draw(0, 0, a, c, c, c) end
    actor.drawAll(_list, _shadow, _shadowMeta, a, c)
    if _specks ~= nil then _specks:draw(0, 0, a, c, c, c) end
    Light.grain(assets.get("fx_grain"), GRAIN * a, _t)
    Light.vignette(assets.get("fx_vignette"), VIGNETTE * a)
end

function M.close()
    if _dust ~= nil then _dust:clear() end
    if _specks ~= nil then _specks:clear() end
    _dust, _specks, _ume = nil, nil, nil
    _chasers, _list = {}, {}
    _bg, _bgMeta, _door, _doorMeta, _shadow, _shadowMeta = nil, nil, nil, nil, nil, nil
end

return M
