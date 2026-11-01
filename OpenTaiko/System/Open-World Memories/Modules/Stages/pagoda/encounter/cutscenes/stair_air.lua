-- Stair band effects over a cutscene background; lit (0..1) dims them.

local assets  = require("encounter/assets")
local view    = require("encounter/bands/view")
local Emitter = require("fx/emitter")
local presets = require("fx/presets")
local Light   = require("fx/light")
local Config  = require("Core.Config")
local Layout    = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")

local M = {}

local Look = Layout.encounter.cutscenes.stair_air
local Anim = Animation.encounter.cutscenes.stair_air

local RAY_COL = Look.ray_color
local RAYS = {}
for i, r in ipairs(Look.rays) do
    RAYS[i] = { x = r.x, y = r.y, rot = r.rot_deg, period = Anim.ray_period_s[i] }
end
local RAY_SCALE = { Look.ray_scale.x, Look.ray_scale.y }
local RAY_ALPHA = { Look.ray_opacity.min, Look.ray_opacity.max }
local RAY_SWAY  = Anim.ray_sway_deg

local SUN_X, SUN_Y = Look.sun.x, Look.sun.y
local SUN_SCALE, SUN_COL, SUN_A = Look.sun_scale, Look.sun_color, Look.sun_opacity
local SUN_BREATH, SUN_PERIOD = Anim.sun_breath_opacity, Anim.sun_period_s

local LEAF_KEYS = { "fx_leaf_a", "fx_leaf_b", "fx_leaf_c" }
local LEAF_RATE = Anim.leaves_aloft / Anim.preset_leaves_aloft  -- the stair's leaves aloft relative to the preset's
local VIGNETTE  = Look.vignette_opacity
local MIST_GLOW = Look.mist_glow_opacity    -- opacity of the mist's additive pass

local _t = 0
local _rays = nil
local _mid, _front = {}, {}
local _mist, _mistPreset = nil, nil

function M.keys()
    return { "fx_shaft", "fx_glow", "fx_mote", "fx_vignette", "fx_mist", "fx_leaf_a", "fx_leaf_b", "fx_leaf_c" }
end

local function add(list, name, tex, rate)
    local p = presets[name]
    if p == nil or tex == nil then return end
    local e = Emitter.new(p, tex)
    if rate ~= nil then e:setRate(rate) end
    list[#list + 1] = e
end

local function leafMap()
    local map = {}
    for _, k in ipairs(LEAF_KEYS) do map[k] = assets.get(k) end
    return map
end

-- the textures of keys() are already opened
function M.open()
    M.close()
    _t = 0
    local list = {}
    for _, r in ipairs(RAYS) do
        list[#list + 1] = {
            x = r.x, y = r.y, rot = r.rot, sway = RAY_SWAY,
            sx = RAY_SCALE[1], sy = RAY_SCALE[2],
            r = RAY_COL[1], g = RAY_COL[2], b = RAY_COL[3],
            aMin = RAY_ALPHA[1], aMax = RAY_ALPHA[2], period = r.period,
        }
    end
    _rays = Light.rays(assets.get("fx_shaft"), list)
    local mote = assets.get("fx_mote")
    add(_mid, "pollen", mote)
    add(_mid, "fireflies", mote)
    add(_mid, "forest_leaves", leafMap(), LEAF_RATE)
    -- the mist runs on its own copy of the preset, so its second pass can switch the blend
    local mist = assets.get("fx_mist")
    if presets.mist ~= nil and mist ~= nil then
        _mistPreset = {}
        for k, v in pairs(presets.mist) do _mistPreset[k] = v end
        _mist = Emitter.new(_mistPreset, mist)
    end
    add(_front, "near_leaves", leafMap())
end

function M.update(dt)
    _t = _t + dt
    if _rays ~= nil then _rays:update(dt) end
    for _, e in ipairs(_mid) do e:update(dt) end
    if _mist ~= nil then _mist:update(dt) end
    for _, e in ipairs(_front) do e:update(dt) end
end

-- over the background
function M.drawBack(ox, oy, lit)
    if _rays ~= nil then _rays:draw(ox, oy, 1, 1, 1, lit, view.xf, view.scale()) end
    local x, y = view.xf(SUN_X, SUN_Y)
    local a = (SUN_A + SUN_BREATH * math.sin(_t * 2 * math.pi / SUN_PERIOD)) * lit
    Light.glow(assets.get("fx_glow"), x, y, SUN_SCALE * view.scale(), SUN_COL[1], SUN_COL[2], SUN_COL[3], a)
end

-- behind the actors
function M.drawMid(ox, oy, lit)
    for _, e in ipairs(_mid) do e:draw(ox, oy, 1, lit, lit, lit) end
    if _mist ~= nil then
        local blend = _mistPreset.blend
        _mist:draw(ox, oy, 1, lit, lit, lit)
        _mistPreset.blend = "Add"
        _mist:draw(ox, oy, MIST_GLOW * lit, 1, 1, 1)
        _mistPreset.blend = blend
    end
end

-- over everything
function M.drawFront(ox, oy, lit)
    for _, e in ipairs(_front) do e:draw(ox, oy, 1, lit, lit, lit) end
    Light.vignette(assets.get("fx_vignette"), VIGNETTE)
end

function M.close()
    for _, e in ipairs(_mid) do e:clear() end
    for _, e in ipairs(_front) do e:clear() end
    if _mist ~= nil then _mist:clear() end
    _mid, _front, _rays = {}, {}, nil
    _mist, _mistPreset = nil, nil
end

return M
