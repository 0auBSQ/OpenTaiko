-- Encounter background for levels 11-20 except 15.

local assets  = require("encounter/assets")
local view    = require("encounter/bands/view")
local levels  = require("levels")
local Easing  = require("Easing")
local Emitter = require("fx/emitter")
local presets = require("fx/presets")
local Light   = require("fx/light")
local Fire    = require("fx/fire")
local Config  = require("Core.Config")
local Layout    = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")

local M = {}

local Look = Layout.encounter.bands.pagoda
local Anim = Animation.encounter.bands.pagoda

local TINT_LIFT   = Look.tint_lift        -- lift of the plate colour towards white
local TINT_BREATH = Anim.tint_breath      -- share of the tint that follows the firelight

local LOST_DELAY, LOST_FADE = Anim.lost_delay_s, Anim.lost_fade_s
local DOOR_FADE = Anim.door_fade_s

local SHAFT_COL = Look.shaft_color
local SHAFTS = {}
for i, sh in ipairs(Look.shafts) do
    SHAFTS[i] = { x = sh.x, y = sh.y, rot = sh.rot_deg, sx = sh.sx, sy = sh.sy, period = Anim.shaft_period_s[i] }
end
local SHAFT_A = Look.shaft_opacity
local SHAFT_MIN, SHAFT_AMP = Anim.shaft_breath_min, Anim.shaft_breath_amp

local GRAIN, VIGNETTE = Look.grain_opacity, Look.vignette_opacity
local DUST_COL = Look.ground_dust_color

local _t = 0
local _tint = { 1, 1, 1 }
local _lost = false
local _doorT = nil      -- seconds since onClear, nil while the door is shut
local _fires = {}
local _dust, _specks = nil, nil

-- plate colour lifted by TINT_LIFT, 0..1
local function plate_tint(level)
    local c = levels.level_tick_color(level) or { 255, 255, 255 }
    local t = {}
    for i = 1, 3 do
        local v = (c[i] or 255) / 255
        t[i] = v + (1 - v) * TINT_LIFT
    end
    return t
end

-- the room's colour per level, and its share following the firelight
M.plateTint, M.TINT_BREATH = plate_tint, TINT_BREATH

local function fire_flicker()
    if #_fires == 0 then return 1 end
    local s = 0
    for _, f in ipairs(_fires) do s = s + f:flicker() end
    return s / #_fires
end

local function emitter(name, tex)
    local p = presets[name]
    if p == nil or tex == nil then return nil end
    return Emitter.new(p, tex)
end

function M.keys(level, ctx, variant)
    local keys = { "bg_pagoda", "fx_flame_core", "fx_puff", "fx_glow", "fx_ember",
                   "fx_shaft", "fx_mote", "fx_grain", "fx_vignette" }
    if ctx ~= nil and ctx.lostBefore then keys[#keys + 1] = "ov_lost" end
    return keys
end

-- the result screen also loads the open door
function M.resultKeys(level, ctx, variant)
    return { "ov_door_open" }
end

function M.open(level, ctx, variant)
    _t = 0
    _doorT = nil
    _lost = ctx ~= nil and ctx.lostBefore == true
    assets.open(M.keys(level, ctx, variant))
    _tint = plate_tint(level)

    _fires = {}
    local core, coreMeta = assets.get("fx_flame_core")
    local fireTex = {
        core = core, coreMeta = coreMeta, puff = assets.get("fx_puff"),
        glow = assets.get("fx_glow"), ember = assets.get("fx_ember"),
    }
    for i, p in ipairs(assets.data("flames") or {}) do
        local x, y, h = p.x or p[1], p.y or p[2], p.h or p[3]
        if x ~= nil and y ~= nil then
            _fires[#_fires + 1] = Fire.new(fireTex, x, y, h, level * 10 + i * 3.7)
        end
    end

    local mote = assets.get("fx_mote")
    _dust = emitter("interior_dust", mote)
    _specks = emitter("ceiling_specks", mote)
end

function M.sky() return 0, false end

function M.castAlpha() return 1 end
function M.castFilter() return 1, 1, 1 end
function M.groundDust() return DUST_COL[1], DUST_COL[2], DUST_COL[3] end

-- call when the floor is beaten; fades the open door in over bg_pagoda
function M.onClear()
    if _doorT == nil then _doorT = 0 end
end

function M.clearBusy()
    return _doorT ~= nil and _doorT < DOOR_FADE
end

function M.update(dt)
    _t = _t + dt
    if _doorT ~= nil then _doorT = _doorT + dt end
    for _, f in ipairs(_fires) do f:update(dt) end
    if _dust ~= nil then _dust:update(dt) end
    if _specks ~= nil then _specks:update(dt) end
end

function M.drawBehind(ox, oy)
    view.shake(ox, oy)
end

function M.drawBack(ox, oy)
    view.shake(ox, oy)
    local k = (1 - TINT_BREATH) + TINT_BREATH * fire_flicker()
    local r, g, b = _tint[1] * k, _tint[2] * k, _tint[3] * k
    local bg, meta = assets.get("bg_pagoda")
    view.drawPlaced(bg, meta, r, g, b, 1)
    if _doorT ~= nil then
        local door, dm = assets.get("ov_door_open")
        view.drawPlaced(door, dm, r, g, b, Easing.inOutQuad(_doorT / DOOR_FADE))
    end
    if _lost then
        local ov, om = assets.get("ov_lost")
        view.drawPlaced(ov, om, r, g, b, Easing.outQuad((view.time() - LOST_DELAY) / LOST_FADE))
    end
    for _, f in ipairs(_fires) do f:drawLight(view.xf) end
end

function M.drawMid(ox, oy)
    ox, oy = view.shake(ox, oy)
    local shaft = assets.get("fx_shaft")
    local s = view.scale()
    for _, sh in ipairs(SHAFTS) do
        local x, y = view.xf(sh.x, sh.y)
        local a = SHAFT_A * (SHAFT_MIN + SHAFT_AMP * math.sin(_t * 2 * math.pi / sh.period))
        Light.shaft(shaft, x, y, sh.rot, sh.sx * s, sh.sy * s,
                    SHAFT_COL[1] * _tint[1], SHAFT_COL[2] * _tint[2], SHAFT_COL[3] * _tint[3], a)
    end
    if _dust ~= nil then _dust:draw(ox, oy, 1, 1, 1, 1) end
    for _, f in ipairs(_fires) do f:drawBody(view.xf) end
end

function M.drawFront(ox, oy)
    ox, oy = view.shake(ox, oy)
    if _specks ~= nil then _specks:draw(ox, oy, 1, 1, 1, 1) end
    Light.grain(assets.get("fx_grain"), GRAIN, _t)
    Light.vignette(assets.get("fx_vignette"), VIGNETTE)
end

function M.close()
    for _, f in ipairs(_fires) do f:clear() end
    if _dust ~= nil then _dust:clear() end
    if _specks ~= nil then _specks:clear() end
    _fires, _dust, _specks = {}, nil, nil
    _lost = false
    _doorT = nil
end

return M
