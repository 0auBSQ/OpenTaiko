-- Roof floors (21+) band: the beanstalk over the sky at the floor select's hour.

local assets = require("encounter/assets")
local skybox = require("skybox")
local Light  = require("fx/light")
local emit   = require("emit")
local Sky    = require("sky")
local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")

local M = {}

local W, H = Layout.play_bg.view.w, Layout.play_bg.view.h

local Look = Layout.play_bg.roof
local Move = Animation.play_bg.roof

local SUN = Look.sun
local SPORE_HOUR = Look.spore_from_hour
local STAR_MIN = Look.star_min_opacity
local MOON_DAY, MOON_NIGHT = Look.moon.day_luminance, Look.moon.night_luminance
local MOON_LIFT, MOON_MAX = Look.moon.lift, Look.moon.max_opacity
local VIGNETTE_DAY, VIGNETTE_NIGHT = Look.vignette.day_opacity, Look.vignette.night_opacity
local VIGNETTE_FROM, VIGNETTE_HOURS = Look.vignette.ramp_from_hour, Look.vignette.ramp_hours

-- cloud rows tiled behind the bean, bottom-aligned, far row first
local CLOUDS = Look.clouds
local CLOUD_SPEEDS = Move.cloud_speed_px_per_s
local CLOUD_FOOT = H

local _hour = 12
local _moon = 0
local _f = { 1, 1, 1 }
local _t = 0                            -- the clouds' clock
local _sky = nil
local _behind = {}                      -- emitters under the bean, graded by the filter
local _stars, _spores = nil, nil        -- not graded by the filter

local function sunFor(hour)
    if hour <= SUN.day.last_hour then return SUN.day end
    if hour <= SUN.sunset.last_hour then return SUN.sunset end
    return nil
end

function M.keys(ctx)
    local hour = skybox.hourFor(ctx.level)
    local keys = Sky.keys()
    for _, k in ipairs({ "up_bean", "fx_mist", "fx_cloud", "fx_vignette" }) do keys[#keys + 1] = k end
    if sunFor(hour) ~= nil then keys[#keys + 1] = "fx_glow" end
    if hour >= SPORE_HOUR then keys[#keys + 1] = "fx_mote" end
    if Sky.starsAt(hour) > STAR_MIN then keys[#keys + 1] = "fx_glint" end
    return keys
end

local function add(list, e)
    if e ~= nil then list[#list + 1] = e end
end

local function drawClouds()
    local tx = assets.get("fx_cloud")
    if tx == nil or not tx.Ready then return end
    tx:SetColor(_f[1], _f[2], _f[3])
    for i, row in ipairs(CLOUDS) do
        local s = row.scale
        local w = tx.Width * s
        local x = (row.offset_px + CLOUD_SPEEDS[i] * _t) % w - w
        tx:SetScale(s, s)
        tx:SetOpacity(row.opacity)
        while x < W do
            tx:Draw(x, CLOUD_FOOT - tx.Height * s)
            x = x + w
        end
    end
    tx:SetScale(1, 1)
    tx:SetOpacity(1)
    tx:SetColor(1, 1, 1)
end

function M.open(ctx)
    _hour = skybox.hourFor(ctx.level)
    _f = { skybox.filterFor(_hour) }
    _t = 0
    _sky = Sky.new(_hour, false)          -- 21+ is only reached after 20 is beaten, so never greyed
    local l = _sky:luminance()
    local k = math.max(0, math.min(1, (MOON_DAY - l) / (MOON_DAY - MOON_NIGHT)))
    _moon = k * k * (3 - 2 * k) * math.min(MOON_MAX, l + MOON_LIFT)
    _behind = {}
    local mist = Look.mist
    add(_behind, emit("mist", (assets.get("fx_mist")), { max = mist.max, alpha = mist.opacity,
                                                       vx = Config.plain(Move.mist.vx_px_per_s),
                                                       area = Config.plain(mist.area), size = Config.plain(mist.size) }))
    _stars = nil
    if _sky:stars() > STAR_MIN then
        _stars = emit("sky_glints", (assets.get("fx_glint")), { area = { 0, 0, W, H }, max = Look.sky_glints.max,
                                                               rate = Move.sky_glints.rate_per_s })
    end
    _spores = nil
    if _hour >= SPORE_HOUR then
        _spores = emit("spores", (assets.get("fx_mote")), { area = { 0, 0, W, H }, max = Look.spores.max,
                                                           rate = Move.spores.rate_per_s })
    end
end

function M.filter() return _f[1], _f[2], _f[3] end

-- opacity of the monster's white copy
function M.glow()
    return _moon
end
function M.eyes() return 0 end
M.shadow = false

local function each(fn)
    for _, e in ipairs(_behind) do fn(e) end
    if _stars ~= nil then fn(_stars) end
    if _spores ~= nil then fn(_spores) end
end

function M.update(dt)
    _t = _t + dt
    each(function(e) e:update(dt) end)
end

function M.drawBack()
    local bean, meta = assets.get("up_bean")
    if bean == nil or not bean.Ready or _sky == nil then return end
    _sky:draw(W, H)
    if _stars ~= nil then _stars:draw(0, 0, _sky:stars(), 1, 1, 1) end
    drawClouds()
    local sun = sunFor(_hour)
    if sun ~= nil then
        local c = sun.glow_color
        Light.glow(assets.get("fx_glow"), sun.x, sun.y, sun.scale, c[1], c[2], c[3], sun.opacity)
    end
    for _, e in ipairs(_behind) do e:draw(0, 0, 1, _f[1], _f[2], _f[3]) end
    bean:SetColor(_f[1], _f[2], _f[3])
    bean:Draw(meta.x or 0, meta.y or 0)
    bean:SetColor(1, 1, 1)
    if _spores ~= nil then _spores:draw(0, 0, 1, 1, 1, 1) end
    local k = math.max(0, math.min(1, (_hour - VIGNETTE_FROM) / VIGNETTE_HOURS))
    Light.vignette(assets.get("fx_vignette"), VIGNETTE_DAY + (VIGNETTE_NIGHT - VIGNETTE_DAY) * k, W, H)
end

return M
