-- Last end-credits background.

local assets = require("encounter/assets")
local view   = require("encounter/bands/view")
local Light  = require("fx/light")
local Config = require("Core.Config")
local Layout    = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")

local M = {}

M.holes = true

local REF_W = 1920
local TAU = math.pi * 2

local Look = Layout.encounter.credits.beanstalk
local Anim = Animation.encounter.credits.beanstalk
-- same as encounter/cutscenes/level20.lua and storm.lua
local SINK = Look.cloud_sink
local CLOUD_SPEED = Anim.cloud_speed_px_per_s
local GLEAM_X = Look.gleam_x
local GLEAM_SCALE, GLEAM_REST = Look.gleam_scale, Look.gleam_opacity
local SHAFTS = Look.shafts
local SHAFT_A = Look.shaft_opacity
local WARM = Look.warm_color
local FINIAL_SCALE, FINIAL_A, FINIAL_REST = Look.finial_scale, Look.finial_opacity, Look.finial_rest
local FINIAL_COL = Look.finial_color
local FINIAL_BREATH_HZ = Anim.finial_breath_hz

local _t = 0
local _cloudX = 0
local _bean, _beanMeta, _cloud, _cloudMeta = nil, nil, nil, nil
local _glow, _shaft = nil, nil

function M.keys()
    return { "cut20_bean", "cut20_cloud", "fx_glow", "fx_shaft" }
end

function M.open()
    _t, _cloudX = 0, 0
    _bean, _beanMeta = assets.get("cut20_bean")
    _cloud, _cloudMeta = assets.get("cut20_cloud")
    _glow, _shaft = assets.get("fx_glow"), assets.get("fx_shaft")
end

function M.update(dt)
    _t = _t + dt
    _cloudX = _cloudX + CLOUD_SPEED * dt
end

local function placed(tx, x, y, a, s)
    if tx == nil or not tx.Ready or a <= 0 then return end
    local px, py = view.xf(x, y)
    local k = view.scale() * (s or 1)
    tx:SetScale(k, k)
    tx:SetOpacity(math.min(1, a))
    tx:DrawAtAnchor(px, py, "topleft")
    tx:SetScale(1, 1)
    tx:SetOpacity(1)
end

local function drawCloud(a)
    local m = _cloudMeta
    if m == nil or _cloud == nil or not _cloud.Ready then return end
    local data = assets.data("cut20") or {}
    local scale = data.cloud_scale or 1
    local w = m.w * scale
    local x = (_cloudX % w) - w
    while x < REF_W do
        placed(_cloud, x, (m.y or 0) + SINK, a, scale)
        x = x + w
    end
end

-- drawn under the scene it fades from, so a = 1; c = 1 (the sky is dimmed over it)
function M.draw(a)
    if _beanMeta ~= nil then placed(_bean, _beanMeta.x or 0, _beanMeta.y or 0, a) end
    drawCloud(a)
    local k = view.scale()
    for i, sh in ipairs(SHAFTS) do
        local x, y = view.xf(GLEAM_X + sh[1], sh[2])
        local b = Anim.shaft_flicker_base + Anim.shaft_flicker_amp
                  * math.sin(_t * Anim.shaft_flicker_freq + i * Anim.shaft_flicker_phase_step)
        Light.shaft(_shaft, x, y, sh[3], sh[4] * k, sh[5] * k, WARM[1], WARM[2], WARM[3], SHAFT_A * b * a)
    end
    local data = assets.data("cut20") or {}
    local fx, fy = view.xf(data.finial_x or REF_W / 2, data.finial_y or 1080)
    local breath = Anim.finial_breath_base + Anim.finial_breath_amp * math.sin(_t * FINIAL_BREATH_HZ * TAU)
    Light.glow(_glow, fx, fy, FINIAL_SCALE * k, FINIAL_COL[1], FINIAL_COL[2], FINIAL_COL[3],
               FINIAL_REST * FINIAL_A * breath * a)
    local gx, gy = view.xf(GLEAM_X, 0)
    Light.glow(_glow, gx, gy, GLEAM_SCALE * k, WARM[1], WARM[2], WARM[3], GLEAM_REST * a)
end

function M.close()
    _bean, _beanMeta, _cloud, _cloudMeta, _glow, _shaft = nil, nil, nil, nil, nil, nil
end

return M
