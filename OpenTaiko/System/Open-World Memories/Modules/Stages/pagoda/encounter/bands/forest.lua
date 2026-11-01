-- Encounter background for levels 1-10.

local assets  = require("encounter/assets")
local view    = require("encounter/bands/view")
local skybox  = require("skybox")
local Emitter = require("fx/emitter")
local presets = require("fx/presets")
local Light   = require("fx/light")
local Config  = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")
local Look = Layout.encounter.bands.forest
local Anim = Animation.encounter.bands.forest

local M = {}

local RAY_COL = Look.ray_color
local RAYS = Look.rays
local RAY_PERIOD = Anim.ray_period_s
local RAY_SCALE = Look.ray_scale
local RAY_ALPHA = Look.ray_opacity
local RAY_SWAY  = Anim.ray_sway_deg

local SUN = Look.sun
local SUN_SCALE, SUN_COL, SUN_A = Look.sun_scale, Look.sun_color, Look.sun_opacity
local SUN_BREATH, SUN_PERIOD = Anim.sun_breath_opacity, Anim.sun_period_s

local LEAF_KEYS = { "fx_leaf_a", "fx_leaf_b", "fx_leaf_c" }
local LEAF_RATE = {
    trail = Anim.leaves_aloft.trail / Anim.preset_leaves_aloft,
    stair = Anim.leaves_aloft.stair / Anim.preset_leaves_aloft,
}
local VIGNETTE  = Look.vignette_opacity
local DUST_COL  = Look.ground_dust_color
local MIST_GLOW = Look.mist_glow_opacity   -- opacity of the additive second mist pass

local _variant = "trail"
local _t = 0
local _rays = nil
local _mid, _front = {}, {}
local _mist, _mistPreset = nil, nil

-- an emitter for a preset; tex is a handle or a key -> handle map
local function add(list, name, tex, rate)
    local p = presets[name]
    if p == nil or tex == nil then return end
    local e = Emitter.new(p, tex)
    if rate ~= nil then e:setRate(rate) end
    list[#list + 1] = e
end

local function leaf_map()
    local map = {}
    for _, k in ipairs(LEAF_KEYS) do map[k] = assets.get(k) end
    return map
end

-- own copy of the preset, for the second additive pass
local function mist_emitter(tex)
    local src = presets.mist
    if src == nil or tex == nil then return nil, nil end
    local p = {}
    for k, v in pairs(src) do p[k] = v end
    return Emitter.new(p, tex), p
end

function M.keys(level, ctx, variant)
    local v = (variant == "stair") and "stair" or "trail"
    local keys = { "bg_" .. v, "fx_shaft", "fx_glow", "fx_mote", "fx_vignette",
                   "fx_leaf_a", "fx_leaf_b", "fx_leaf_c" }
    if v == "stair" then keys[#keys + 1] = "fx_mist" end
    return keys
end

function M.open(level, ctx, variant)
    _variant = (variant == "stair") and "stair" or "trail"
    _t = 0
    assets.open(M.keys(level, ctx, _variant))
    if _variant == "stair" then skybox.setView(Look.stair_sky_hour, Look.stair_sky_pitch_deg) end

    local list = {}
    for i, r in ipairs(RAYS[_variant]) do
        list[#list + 1] = {
            x = r.x, y = r.y, rot = r.rot_deg, sway = RAY_SWAY,
            sx = RAY_SCALE.x, sy = RAY_SCALE.y,
            r = RAY_COL[1], g = RAY_COL[2], b = RAY_COL[3],
            aMin = RAY_ALPHA.min, aMax = RAY_ALPHA.max, period = RAY_PERIOD[_variant][i],
        }
    end
    _rays = Light.rays(assets.get("fx_shaft"), list)

    _mid, _front = {}, {}
    local mote = assets.get("fx_mote")
    add(_mid, "pollen", mote)
    add(_mid, "fireflies", mote)
    add(_mid, "forest_leaves", leaf_map(), LEAF_RATE[_variant])
    _mist, _mistPreset = nil, nil
    if _variant == "stair" then _mist, _mistPreset = mist_emitter((assets.get("fx_mist"))) end
    add(_front, "near_leaves", leaf_map())
end

function M.sky()
    return 0, _variant == "stair"
end

function M.castAlpha() return 1 end
function M.castFilter() return 1, 1, 1 end
function M.groundDust() return DUST_COL[1], DUST_COL[2], DUST_COL[3] end

function M.update(dt)
    _t = _t + dt
    if _rays ~= nil then _rays:update(dt) end
    for _, e in ipairs(_mid) do e:update(dt) end
    if _mist ~= nil then _mist:update(dt) end
    for _, e in ipairs(_front) do e:update(dt) end
end

function M.drawBehind(ox, oy)
    view.shake(ox, oy)
end

function M.drawBack(ox, oy)
    ox, oy = view.shake(ox, oy)
    local bg, meta = assets.get("bg_" .. _variant)
    view.drawPlaced(bg, meta)
    if _rays ~= nil then _rays:draw(ox, oy, 1, 1, 1, 1, view.xf, view.scale()) end
    local sun = SUN[_variant]
    local x, y = view.xf(sun.x, sun.y)
    local a = SUN_A + SUN_BREATH * math.sin(_t * 2 * math.pi / SUN_PERIOD)
    Light.glow(assets.get("fx_glow"), x, y, SUN_SCALE * view.scale(), SUN_COL[1], SUN_COL[2], SUN_COL[3], a)
end

function M.drawMid(ox, oy)
    ox, oy = view.shake(ox, oy)
    for _, e in ipairs(_mid) do e:draw(ox, oy, 1, 1, 1, 1) end
    if _mist ~= nil and _mistPreset ~= nil then
        local blend = _mistPreset.blend
        _mist:draw(ox, oy, 1, 1, 1, 1)
        _mistPreset.blend = "Add"
        _mist:draw(ox, oy, MIST_GLOW, 1, 1, 1)
        _mistPreset.blend = blend
    end
end

function M.drawFront(ox, oy)
    ox, oy = view.shake(ox, oy)
    for _, e in ipairs(_front) do e:draw(ox, oy, 1, 1, 1, 1) end
    Light.vignette(assets.get("fx_vignette"), VIGNETTE[_variant])
end

function M.close()
    for _, e in ipairs(_mid) do e:clear() end
    for _, e in ipairs(_front) do e:clear() end
    if _mist ~= nil then _mist:clear() end
    _mid, _front, _rays = {}, {}, nil
    _mist, _mistPreset = nil, nil
end

return M
