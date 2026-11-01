-- Second end-credits background (the forest trail).

local assets   = require("encounter/assets")
local view     = require("encounter/bands/view")
local monsters = require("encounter/monsters")
local body     = require("encounter/monster_body")
local Emitter  = require("fx/emitter")
local presets  = require("fx/presets")
local Light    = require("fx/light")
local Config   = require("Core.Config")
local Layout    = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")

local M = {}

M.holes = false

local REF_W = 1920

local Look = Layout.encounter.credits.forest
local Anim = Animation.encounter.credits.forest

-- order does not matter (drawn by feet); speed (px/s, leftwards) is for non-wanderers;
-- range widens a wanderer's walk around x
local WALKERS = {
    { key = "norm5" },
    { key = "norm2" },
    { key = "norm1" },
    { key = "norm4b" },
    { key = "norm3" },
    { key = "critter" },
}
for i, w in ipairs(WALKERS) do
    local p, m = Look.walkers[i], Anim.walkers[i]
    w.x, w.y, w.speed, w.range = p.x, p.y, m.speed, Config.optional(m, "range")
end
local WRAP_PAD = Look.wrap_pad  -- px past each edge before a walker wraps
local STEP_PX, STEP_HZ = Anim.step_px, Anim.step_hz -- walk bob for idles that stay in place

-- same as encounter/bands/forest.lua
local RAY_COL = Look.ray_color
local RAYS = {}
for i, r in ipairs(Look.rays) do
    RAYS[i] = { x = r.x, y = r.y, rot = r.rot, period = Anim.ray_periods_s[i] }
end
local RAY_SCALE, RAY_ALPHA, RAY_SWAY = Look.ray_scale, Anim.ray_opacity, Anim.ray_sway_deg
local SUN_X, SUN_Y = Look.sun_x, Look.sun_y
local SUN_SCALE, SUN_COL, SUN_A = Look.sun_scale, Look.sun_color, Look.sun_opacity
local SUN_BREATH, SUN_PERIOD = Anim.sun_breath, Anim.sun_period_s
local LEAF_KEYS = { "fx_leaf_a", "fx_leaf_b", "fx_leaf_c" }
local VIGNETTE = Look.vignette_opacity

local _t = 0
local _bg, _bgMeta = nil, nil
local _rays = nil
local _mid, _front = {}, {}
local _walkers = {}

-- copy of a monster's run-floor member entry, so its walk can widen
local function member(key)
    local m = monsters.forLevel(1, key)[1]
    local c = { key = key, kind = "counter", params = {}, glow = 0, flash = 1, eyes = false,
                dissolve = false, dx = 0, dy = 0, tempo = 1 }
    if m ~= nil and m.key == key then
        c.kind, c.tempo = m.kind, m.tempo or 1
        for k, v in pairs(m.params or {}) do c.params[k] = v end
    end
    return c
end

local function members()
    local list = {}
    for i, w in ipairs(WALKERS) do
        local m = member(w.key)
        if m.kind == "wander" and w.range ~= nil then m.params.range = w.range end
        list[i] = m
    end
    return list
end

function M.keys()
    local keys = { "bg_trail", "fx_shaft", "fx_glow", "fx_mote", "fx_vignette" }
    for _, k in ipairs(LEAF_KEYS) do keys[#keys + 1] = k end
    for _, m in ipairs(members()) do
        for _, k in ipairs(body.keys(m)) do keys[#keys + 1] = k end
    end
    return keys
end

local function add(list, name, tex)
    local p = presets[name]
    if p ~= nil and tex ~= nil then list[#list + 1] = Emitter.new(p, tex) end
end

local function leafMap()
    local map = {}
    for _, k in ipairs(LEAF_KEYS) do map[k] = assets.get(k) end
    return map
end

function M.open()
    M.close()
    _t = 0
    _bg, _bgMeta = assets.get("bg_trail")
    local list = {}
    for _, r in ipairs(RAYS) do
        list[#list + 1] = { x = r.x, y = r.y, rot = r.rot, sway = RAY_SWAY, sx = RAY_SCALE[1], sy = RAY_SCALE[2],
                            r = RAY_COL[1], g = RAY_COL[2], b = RAY_COL[3],
                            aMin = RAY_ALPHA[1], aMax = RAY_ALPHA[2], period = r.period }
    end
    _rays = Light.rays(assets.get("fx_shaft"), list)
    local mote = assets.get("fx_mote")
    add(_mid, "pollen", mote)
    add(_mid, "fireflies", mote)
    add(_mid, "forest_leaves", leafMap())
    add(_front, "near_leaves", leafMap())
    for i, m in ipairs(members()) do
        local w = WALKERS[i]
        local walks = m.kind ~= "wander"
        _walkers[i] = { b = body.new(m, w.x, w.y, 600 + i * 7), x = w.x, y = w.y,
                        speed = walks and (w.speed or 0) or 0, bob = walks and m.kind ~= "star" and m.kind ~= "float" }
    end
    table.sort(_walkers, function(p, q) return p.y < q.y end)
end

function M.update(dt)
    _t = _t + dt
    if _rays ~= nil then _rays:update(dt) end
    for _, e in ipairs(_mid) do e:update(dt) end
    for _, e in ipairs(_front) do e:update(dt) end
    for _, w in ipairs(_walkers) do
        if w.speed > 0 then
            w.x = w.x - w.speed * dt
            if w.x < -WRAP_PAD then w.x = w.x + REF_W + 2 * WRAP_PAD end
            w.b.x = w.x
        end
        w.b:update(dt)
    end
end

-- a = opacity, c = brightness
function M.draw(a, c)
    if a <= 0 then return end
    view.drawPlaced(_bg, _bgMeta, c, c, c, a)
    if _rays ~= nil then _rays:draw(0, 0, c, c, c, a, view.xf, view.scale()) end
    local x, y = view.xf(SUN_X, SUN_Y)
    local sun = (SUN_A + SUN_BREATH * math.sin(_t * 2 * math.pi / SUN_PERIOD)) * a
    Light.glow(assets.get("fx_glow"), x, y, SUN_SCALE * view.scale(), SUN_COL[1] * c, SUN_COL[2] * c, SUN_COL[3] * c, sun)
    for _, e in ipairs(_mid) do e:draw(0, 0, a, c, c, c) end
    for _, w in ipairs(_walkers) do w.b:drawShadow(a, 0, 0) end
    for i, w in ipairs(_walkers) do
        local oy = w.bob and -STEP_PX * math.abs(math.sin(math.pi * STEP_HZ * _t + i)) or 0
        w.b:draw(a, c, c, c, 0, 0, oy)
    end
    for _, e in ipairs(_front) do e:draw(0, 0, a, c, c, c) end
    Light.vignette(assets.get("fx_vignette"), VIGNETTE * a)
end

function M.close()
    for _, e in ipairs(_mid) do e:clear() end
    for _, e in ipairs(_front) do e:clear() end
    for _, w in ipairs(_walkers) do w.b:close() end
    _mid, _front, _walkers, _rays = {}, {}, {}, nil
    _bg, _bgMeta = nil, nil
end

return M
