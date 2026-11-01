-- Floors 1-10 band: the forest, with the encounter forest's effects fitted to the band.

local assets = require("encounter/assets")
local Light  = require("fx/light")
local emit   = require("emit")
local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")

local M = {}

local W, H = Layout.play_bg.view.w, Layout.play_bg.view.h
local STAIR_LEVEL = 6

local Look = Layout.play_bg.forest
local Move = Animation.play_bg.forest

local RAY_COL = Look.ray_color
local RAYS = Look.rays
local RAY_PERIODS = Move.ray_period_s
local RAY_SX, RAY_SY = Look.ray_sx, Look.ray_sy
local RAY_ALPHA = { Move.ray_opacity_min, Move.ray_opacity_max }
local RAY_SWAY = Move.ray_sway_deg

local SUN = { x = Look.sun.x, y = Look.sun.y, scale = Look.sun.scale, a = Look.sun.opacity,
              breath = Move.sun.breath_opacity, period = Move.sun.period_s }
local SUN_COL = Look.sun_color

local LEAF_KEYS = { "fx_leaf_a", "fx_leaf_b", "fx_leaf_c" }
local VIGNETTE = Look.vignette_opacity
local MIST_GLOW = Look.mist_glow_opacity

local _t = 0
local _rays = nil
local _list = {}                -- back to front
local _near = nil
local _mist, _mistPreset = nil, nil

function M.keys(ctx)
    local keys = { "up_forest", "fx_shaft", "fx_glow", "fx_mote", "fx_vignette" }
    for _, k in ipairs(LEAF_KEYS) do keys[#keys + 1] = k end
    if ctx.level >= STAIR_LEVEL then keys[#keys + 1] = "fx_mist" end
    return keys
end

local function leaves()
    local map = {}
    for _, k in ipairs(LEAF_KEYS) do map[k] = assets.get(k) end
    return map
end

local function add(e)
    if e ~= nil then _list[#_list + 1] = e end
end

function M.open(ctx)
    _t = 0
    local list = {}
    for i, r in ipairs(RAYS) do
        list[#list + 1] = {
            x = r.x, y = r.y, rot = r.rot_deg, sway = RAY_SWAY, sx = RAY_SX, sy = RAY_SY,
            r = RAY_COL[1], g = RAY_COL[2], b = RAY_COL[3],
            aMin = RAY_ALPHA[1], aMax = RAY_ALPHA[2], period = RAY_PERIODS[i],
        }
    end
    _rays = Light.rays(assets.get("fx_shaft"), list)

    _list = {}
    local mote = assets.get("fx_mote")
    add(emit("pollen", mote, { area = { 0, 0, W, H }, max = Look.pollen.max, rate = Move.pollen.rate_per_s }))
    add(emit("fireflies", mote, { area = { 0, Look.fireflies.area_y, W, Look.fireflies.area_h },
                                  max = Look.fireflies.max, rate = Move.fireflies.rate_per_s }))
    local leafLook, leafMove = Look.forest_leaves, Move.forest_leaves
    add(emit("forest_leaves", leaves(), { area = Config.plain(leafLook.area), life = Config.plain(leafMove.life_s),
                                          max = leafLook.max, rate = leafMove.rate_per_s,
                                          prewarm = leafMove.prewarm_s }))
    _mist, _mistPreset = nil, nil
    if ctx.level >= STAIR_LEVEL then
        _mist = emit("mist", (assets.get("fx_mist")), { area = Config.plain(Look.mist.area), max = Look.mist.max,
                                                       size = Config.plain(Look.mist.size) })
        if _mist ~= nil then _mistPreset = _mist.p end
    end
    _near = emit("near_leaves", leaves(), { area = Config.plain(Look.near_leaves.area), max = Look.near_leaves.max,
                                            rate = Move.near_leaves.rate_per_s, size = Look.near_leaves.size })
end

function M.filter() return 1, 1, 1 end
function M.eyes() return 0 end
M.shadow = true

function M.update(dt)
    _t = _t + dt
    if _rays ~= nil then _rays:update(dt) end
    for _, e in ipairs(_list) do e:update(dt) end
    if _mist ~= nil then _mist:update(dt) end
    if _near ~= nil then _near:update(dt) end
end

function M.drawBack()
    local bg = assets.get("up_forest")
    if bg == nil or not bg.Ready then return end
    bg:Draw(0, 0)
    if _rays ~= nil then _rays:draw(0, 0) end
    local a = SUN.a + SUN.breath * math.sin(_t * 2 * math.pi / SUN.period)
    Light.glow(assets.get("fx_glow"), SUN.x, SUN.y, SUN.scale, SUN_COL[1], SUN_COL[2], SUN_COL[3], a)
    for _, e in ipairs(_list) do e:draw(0, 0, 1, 1, 1, 1) end
    if _mist ~= nil then
        local blend = _mistPreset.blend
        _mist:draw(0, 0, 1, 1, 1, 1)
        _mistPreset.blend = "Add"
        _mist:draw(0, 0, MIST_GLOW, 1, 1, 1)
        _mistPreset.blend = blend
    end
    if _near ~= nil then _near:draw(0, 0, 1, 1, 1, 1) end
    Light.vignette(assets.get("fx_vignette"), VIGNETTE, W, H)
end

return M
