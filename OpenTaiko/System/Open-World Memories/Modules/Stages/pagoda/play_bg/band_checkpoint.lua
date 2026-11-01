-- Floor 15 band: the checkpoint room, dark until 15 is beaten.

local assets   = require("encounter/assets")
local reveal   = require("encounter/reveal")
local skybox   = require("skybox")
local Light    = require("fx/light")
local Darkness = require("fx/darkness")
local emit     = require("emit")
local Sky      = require("sky")
local Config   = require("Core.Config")
local Layout   = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")

local M = {}

local W, H = Layout.play_bg.view.w, Layout.play_bg.view.h
local CLEAR_LEVEL = 15
local Look = Layout.play_bg.checkpoint
local Move = Animation.play_bg.checkpoint
local SKY_HOUR = Look.sky_hour

local DARK = Look.dark_filter_color
local DARK_SKY = Look.dark_sky_color
local DENSITY = Look.darkness_density
local MAX_FRAME_DT = Move.darkness_max_dt_s

local LANTERN_SCALE, LANTERN_A = Look.lantern.scale, Look.lantern.opacity
local LANTERN_COL = Look.lantern.glow_color
local SHAFT_COL = Look.shaft_color
local SHAFTS = Look.shafts
local SHAFT_PERIODS = Move.shaft_period_s
local SHAFT_A = Look.shaft_opacity
local BREATH_BASE, BREATH_AMP = Move.shaft_breath.base, Move.shaft_breath.amp
local VIGNETTE = Look.vignette_opacity
local DUST_INSET = Look.interior_dust.inset_x

local _clear = false
local _t, _dt = 0, 0
local _sky = nil
local _dark = nil
local _box = {}
local _boxes = { _box }
local _dust = nil

function M.keys(ctx)
    if ctx.highest < CLEAR_LEVEL then
        return { "up_checkpoint_shadow", "cut20_white", "dark_cloud", "dark_shadow", "dark_spiral" }
    end
    local keys = Sky.keys()
    for _, k in ipairs({ "up_checkpoint_clear", "fx_glow", "fx_shaft", "fx_mote", "fx_vignette" }) do
        keys[#keys + 1] = k
    end
    return keys
end

function M.open(ctx)
    _clear = ctx.highest >= CLEAR_LEVEL
    _t, _dt = 0, 0
    _sky, _dark, _dust = nil, nil, nil
    local hole = assets.data("holes") or { x = 0, y = 0, w = W, h = H }
    _box[1], _box[2], _box[3], _box[4] = hole.x, hole.y, hole.w, hole.h
    if _clear then
        _sky = Sky.new(SKY_HOUR, skybox.rule(ctx.highest) >= 1)
        -- interior_dust spread over the arches rather than the window light
        _dust = emit("interior_dust", (assets.get("fx_mote")), {
            area = { hole.x + DUST_INSET, hole.y, hole.w - 2 * DUST_INSET, hole.h }, max = Look.interior_dust.max,
            rate = Move.interior_dust.rate_per_s,
        })
        if _dust ~= nil then _dust.p.zones = nil end
    else
        _dark = Darkness.new({ cloud = (assets.get("dark_cloud")), shadow = (assets.get("dark_shadow")),
                               spiral = (assets.get("dark_spiral")) },
                             { splitX = hole.x + hole.w * 0.5, density = DENSITY })
    end
end

function M.filter()
    if _clear then return 1, 1, 1 end
    return DARK[1], DARK[2], DARK[3]
end

function M.eyes()
    return _clear and 0 or reveal.EYES_REST
end

M.shadow = true

function M.update(dt)
    _t = _t + dt
    _dt = _dt + dt
    if _dust ~= nil then _dust:update(dt) end
end

function M.drawBack()
    local bg = assets.get(_clear and "up_checkpoint_clear" or "up_checkpoint_shadow")
    if bg == nil or not bg.Ready then return end
    if _sky ~= nil then
        _sky:draw(W, H)
    else
        local white = assets.get("cut20_white")
        if white ~= nil and white.Ready then
            white:SetScale(W / white.Width, H / white.Height)
            white:SetColor(DARK_SKY[1], DARK_SKY[2], DARK_SKY[3])
            white:DrawAtAnchor(0, 0, "topleft")
            white:SetScale(1, 1)
            white:SetColor(1, 1, 1)
        end
    end
    if _dark ~= nil then
        _dark:frame(math.min(_dt, MAX_FRAME_DT), _boxes, 0)
        _dt = 0
    end
    bg:Draw(0, 0)
    if not _clear then return end
    local glow = assets.get("fx_glow")
    for i, p in ipairs(assets.data("lanterns") or {}) do
        Light.glow(glow, p.x, p.y, LANTERN_SCALE, LANTERN_COL[1], LANTERN_COL[2], LANTERN_COL[3],
                   LANTERN_A * Light.flicker(_t, i * 1.7))
    end
    local shaft = assets.get("fx_shaft")
    for i, sh in ipairs(SHAFTS) do
        local a = SHAFT_A * (BREATH_BASE + BREATH_AMP * math.sin(_t * 2 * math.pi / SHAFT_PERIODS[i]))
        Light.shaft(shaft, sh.x, sh.y, sh.rot_deg, sh.sx, sh.sy, SHAFT_COL[1], SHAFT_COL[2], SHAFT_COL[3], a)
    end
    if _dust ~= nil then _dust:draw(0, 0, 1, 1, 1, 1) end
    Light.vignette(assets.get("fx_vignette"), VIGNETTE, W, H)
end

return M
