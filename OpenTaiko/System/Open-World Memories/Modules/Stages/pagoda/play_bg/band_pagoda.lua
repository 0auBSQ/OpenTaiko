-- Floors 11-19 (but 15) band: the pagoda room.
-- The band shows the encounter room from row 502 down; effects keep their encounter positions.

local assets = require("encounter/assets")
local room   = require("encounter/bands/pagoda")
local Light  = require("fx/light")
local Fire   = require("fx/fire")
local emit   = require("emit")
local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")

local M = {}

local W, H = Layout.play_bg.view.w, Layout.play_bg.view.h

local Look = Layout.play_bg.pagoda
local Move = Animation.play_bg.pagoda

local SHAFT_COL = Look.shaft_color
local SHAFTS = Look.shafts
local SHAFT_PERIODS = Move.shaft_period_s
local SHAFT_A = Look.shaft_opacity
local BREATH_BASE, BREATH_AMP = Move.shaft_breath.base, Move.shaft_breath.amp
local GRAIN, VIGNETTE = Look.grain_opacity, Look.vignette_opacity

local _t = 0
local _tint = { 1, 1, 1 }
local _fires = {}
local _dust, _specks = nil, nil

function M.keys(ctx)
    return { "up_pagoda", "fx_flame_core", "fx_puff", "fx_glow", "fx_ember", "fx_shaft", "fx_mote",
             "fx_grain", "fx_vignette" }
end

function M.open(ctx)
    _t = 0
    _tint = room.plateTint(ctx.level)
    _fires = {}
    local core, coreMeta = assets.get("fx_flame_core")
    local tex = { core = core, coreMeta = coreMeta, puff = assets.get("fx_puff"), glow = assets.get("fx_glow"),
                  ember = assets.get("fx_ember") }
    for i, p in ipairs(assets.data("torches") or {}) do
        _fires[#_fires + 1] = Fire.new(tex, p.x, p.y, p.h, ctx.level * 10 + i * 3.7)
    end
    local mote = assets.get("fx_mote")
    local zones = {}
    for i, z in ipairs(Look.interior_dust.zones) do
        zones[i] = { z.x, z.y, z.w, Config.optional(z, "h") or H, p = z.share }
    end
    _dust = emit("interior_dust", mote, {
        zones = zones,
        area = { 0, 0, W, H }, max = Look.interior_dust.max, rate = Move.interior_dust.rate_per_s,
    })
    _specks = emit("ceiling_specks", mote, { area = Config.plain(Look.ceiling_specks.area) })
end

-- averaged flicker of the fires
local function flicker()
    if #_fires == 0 then return 1 end
    local s = 0
    for _, f in ipairs(_fires) do s = s + f:flicker() end
    return s / #_fires
end

function M.filter() return 1, 1, 1 end
function M.eyes() return 0 end
M.shadow = true

function M.update(dt)
    _t = _t + dt
    for _, f in ipairs(_fires) do f:update(dt) end
    if _dust ~= nil then _dust:update(dt) end
    if _specks ~= nil then _specks:update(dt) end
end

function M.drawBack()
    local bg = assets.get("up_pagoda")
    if bg == nil or not bg.Ready then return end
    local k = (1 - room.TINT_BREATH) + room.TINT_BREATH * flicker()
    bg:SetColor(_tint[1] * k, _tint[2] * k, _tint[3] * k)
    bg:Draw(0, 0)
    bg:SetColor(1, 1, 1)
    for _, f in ipairs(_fires) do f:drawLight() end
    local shaft = assets.get("fx_shaft")
    for i, sh in ipairs(SHAFTS) do
        local a = SHAFT_A * (BREATH_BASE + BREATH_AMP * math.sin(_t * 2 * math.pi / SHAFT_PERIODS[i]))
        Light.shaft(shaft, sh.x, sh.y, sh.rot_deg, sh.sx, sh.sy,
                    SHAFT_COL[1] * _tint[1], SHAFT_COL[2] * _tint[2], SHAFT_COL[3] * _tint[3], a)
    end
    if _dust ~= nil then _dust:draw(0, 0, 1, 1, 1, 1) end
    for _, f in ipairs(_fires) do f:drawBody() end
    if _specks ~= nil then _specks:draw(0, 0, 1, 1, 1, 1) end
    Light.grain(assets.get("fx_grain"), GRAIN, _t, W, H)
    Light.vignette(assets.get("fx_vignette"), VIGNETTE, W, H)
end

return M
