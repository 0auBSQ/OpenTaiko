-- Encounter background for level 15.

local assets   = require("encounter/assets")
local view     = require("encounter/bands/view")
local skybox   = require("skybox")
local Easing   = require("Easing")
local Emitter  = require("fx/emitter")
local presets  = require("fx/presets")
local Light    = require("fx/light")
local Darkness = require("fx/darkness")
local Config   = require("Core.Config")
local Layout    = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")

local M = {}

local Look = Layout.encounter.bands.checkpoint
local Anim = Animation.encounter.bands.checkpoint

local SHADOW_CAST_ALPHA = Look.shadow_cast_alpha_255 / 255

local SCATTER_SEC = Anim.scatter_s      -- matches the floor select's scatter
local XFADE_AT, XFADE_SEC = Anim.crossfade_at_s, Anim.crossfade_s
local CLEAR_TAIL = Anim.clear_tail_s    -- seconds clearBusy() holds after both finish

local SHAFT_COL = Look.shaft_color
local SHAFTS = {}
for i, sh in ipairs(Look.shafts) do
    SHAFTS[i] = { x = sh.x, y = sh.y, rot = sh.rot_deg, sx = sh.sx, sy = sh.sy, period = Anim.shaft_period_s[i] }
end
local SHAFT_A = Look.shaft_opacity
local SHAFT_MIN, SHAFT_AMP = Anim.shaft_breath_min, Anim.shaft_breath_amp

local DUST_MAX  = Look.dust_max_count
local DUST_AREA = Look.dust_area

local LANTERN_SCALE, LANTERN_A = Look.lantern_scale, Look.lantern_opacity
local LANTERN_COL = Look.lantern_color
local LANTERN_PHASE = Anim.lantern_flicker_phase_step

local VIGNETTE = Look.vignette_opacity
local DUST_COL = Look.ground_dust_color

local MAX_FRAME_DT = 0.1

local _resident = nil
local _variant = "shadow"
local _tower = nil          -- highest floor the tower shows cleared (the run's in a challenge)
local _t = 0
local _dust = nil
local _dark = nil
local _hole = nil       -- { x, y, w, h } in 1920 space
local _box = {}         -- _hole on screen, as a darkness box
local _boxes = { _box }
local _dt = 0           -- seconds toward the next darkness frame
local _clearT = nil     -- seconds since onClear, nil before

function M.setup(tex)
    _resident = tex
end

local function bg_key()
    return (_variant == "clear") and "bg_checkpoint_clear" or "bg_checkpoint_shadow"
end

local function read_hole()
    local h = assets.data("holes15")
    if type(h) ~= "table" then return nil end
    local x, y, w, hh = h.x or h[1], h.y or h[2], h.w or h[3], h.h or h[4]
    if x == nil or y == nil or w == nil or hh == nil then return nil end
    return { x, y, w, hh }
end

function M.keys(level, ctx, variant)
    if variant == "clear" then
        local keys = { "bg_checkpoint_clear", "fx_shaft", "fx_mote", "fx_vignette" }
        if assets.data("lanterns15") ~= nil then keys[#keys + 1] = "fx_glow" end
        return keys
    end
    return { "bg_checkpoint_shadow" }
end

-- the shadow room's result screen also loads bg_checkpoint_clear
function M.resultKeys(level, ctx, variant)
    if variant == "clear" then return {} end
    return { "bg_checkpoint_clear" }
end

function M.open(level, ctx, variant)
    _variant = (variant == "clear") and "clear" or "shadow"
    _tower = ctx ~= nil and (ctx.tower or ctx.highest) or nil
    _t, _dt = 0, 0
    _clearT = nil
    _box.scatter = nil
    assets.open(M.keys(level, ctx, _variant))
    skybox.setView(Look.sky_hour, Look.sky_pitch_deg)
    _dust, _dark, _hole = nil, nil, nil

    if _variant == "clear" then
        local mote = assets.get("fx_mote")
        local src = presets.interior_dust
        if src ~= nil and mote ~= nil then
            local p = {}
            for k, v in pairs(src) do p[k] = v end
            p.max = DUST_MAX
            p.rate = (src.rate or 0) * DUST_MAX / math.max(1, src.max or DUST_MAX)
            p.zones = nil
            p.area = DUST_AREA
            _dust = Emitter.new(p, mote)
        end
    else
        _hole = read_hole()
        if _hole ~= nil and _resident ~= nil then
            _dark = Darkness.new(_resident, { splitX = _hole[1] + _hole[3] * 0.5 })
            _dark:reset()
        end
    end
end

function M.sky()
    return skybox.rule(_tower), true
end

-- 0..1 crossfade to the clear room
local function crossfade()
    if _clearT == nil then return 0 end
    return Easing.inOutQuad((_clearT - XFADE_AT) / XFADE_SEC)
end

function M.castAlpha()
    if _variant == "clear" then return 1 end
    return SHADOW_CAST_ALPHA + (1 - SHADOW_CAST_ALPHA) * crossfade()
end

-- 0 in the shadow room, 1 once clear
function M.monsterAlpha()
    if _variant == "clear" then return 1 end
    return crossfade()
end

function M.castFilter() return 1, 1, 1 end
function M.groundDust() return DUST_COL[1], DUST_COL[2], DUST_COL[3] end

-- call on level 15's first clear
function M.onClear()
    if _variant ~= "clear" and _clearT == nil then _clearT = 0 end
end

function M.clearBusy()
    return _clearT ~= nil and _clearT < math.max(SCATTER_SEC, XFADE_AT + XFADE_SEC) + CLEAR_TAIL
end

function M.update(dt)
    _t = _t + dt
    _dt = _dt + dt
    if _clearT ~= nil then _clearT = _clearT + dt end
    if _dust ~= nil then _dust:update(dt) end
end

-- simulates and draws the darkness in one call, once per frame; skipped until the shadow room is ready
function M.drawBehind(ox, oy)
    view.shake(ox, oy)
    if _dark == nil then return end
    local bg = assets.get(bg_key())
    if bg == nil or not bg.Ready then return end
    local x0, y0 = view.xf(_hole[1], _hole[2])
    local x1, y1 = view.xf(_hole[1] + _hole[3], _hole[2] + _hole[4])
    _box[1], _box[2], _box[3], _box[4] = x0, y0, x1 - x0, y1 - y0
    if _clearT ~= nil then
        local u = math.min(1, _clearT / SCATTER_SEC)
        _box.scatter = u * u * (3 - 2 * u)
    end
    _dark:frame(math.min(_dt, MAX_FRAME_DT), _boxes, 0)
    _dt = 0
end

function M.drawBack(ox, oy)
    view.shake(ox, oy)
    local bg, meta = assets.get(bg_key())
    local xf = crossfade()
    local clear, clearMeta = nil, nil
    if xf > 0 then clear, clearMeta = assets.get("bg_checkpoint_clear") end
    if xf < 1 or clear == nil or not clear.Ready then view.drawPlaced(bg, meta) end
    if clear ~= nil then view.drawPlaced(clear, clearMeta, 1, 1, 1, xf) end
    if _variant ~= "clear" then return end
    local glow = assets.get("fx_glow")
    local s = view.scale()
    for i, p in ipairs(assets.data("lanterns15") or {}) do
        local x, y = view.xf(p.x or p[1], p.y or p[2])
        local a = LANTERN_A * Light.flicker(_t, i * LANTERN_PHASE)
        Light.glow(glow, x, y, LANTERN_SCALE * s, LANTERN_COL[1], LANTERN_COL[2], LANTERN_COL[3], a)
    end
end

function M.drawMid(ox, oy)
    ox, oy = view.shake(ox, oy)
    if _variant ~= "clear" then return end
    local shaft = assets.get("fx_shaft")
    local s = view.scale()
    for _, sh in ipairs(SHAFTS) do
        local x, y = view.xf(sh.x, sh.y)
        local a = SHAFT_A * (SHAFT_MIN + SHAFT_AMP * math.sin(_t * 2 * math.pi / sh.period))
        Light.shaft(shaft, x, y, sh.rot, sh.sx * s, sh.sy * s, SHAFT_COL[1], SHAFT_COL[2], SHAFT_COL[3], a)
    end
    if _dust ~= nil then _dust:draw(ox, oy, 1, 1, 1, 1) end
end

function M.drawFront(ox, oy)
    view.shake(ox, oy)
    if _variant ~= "clear" then return end
    Light.vignette(assets.get("fx_vignette"), VIGNETTE)
end

function M.close()
    if _dust ~= nil then _dust:clear() end
    if _dark ~= nil then _dark:reset() end
    _dust, _dark, _hole = nil, nil, nil
    _clearT = nil
    _box.scatter = nil
end

return M
