-- Encounter background for levels 21+.

local assets   = require("encounter/assets")
local view     = require("encounter/bands/view")
local skybox   = require("skybox")
local Emitter  = require("fx/emitter")
local presets  = require("fx/presets")
local Light    = require("fx/light")
local Config   = require("Core.Config")
local Layout    = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")

local M = {}

local Look = Layout.encounter.bands.sky
local Anim = Animation.encounter.bands.sky

local PITCH = Look.sky_pitch_deg

-- based on the stair mist preset
local WISPS = { max = Look.wisps.max_count, alpha = Look.wisps.opacity,
                vx = Config.plain(Anim.wisps_vx_px_per_s), area = Config.plain(Look.wisps.area) }

-- cloud rows, far first; speed in px/s
local CLOUDS = {}
for i, c in ipairs(Look.clouds) do
    CLOUDS[i] = { scale = c.scale, speed = Anim.cloud_px_per_s[i], offset = c.offset_px, alpha = c.opacity }
end
local CLOUD_FOOT = Look.cloud_foot_y

local SUN = {}
for _, name in ipairs({ "day", "sunset" }) do
    local s = Look.sun[name]
    SUN[name] = { last = s.last_hour, x = s.x, y = s.y, scale = s.scale,
                  r = s.color[1], g = s.color[2], b = s.color[3], a = s.opacity }
end
local SPORE_HOUR = Look.spore_from_hour

local VIGNETTE_DAY, VIGNETTE_NIGHT = Look.vignette_day_opacity, Look.vignette_night_opacity
local VIGNETTE_FROM, VIGNETTE_SPAN = Look.vignette_from_hour, Look.vignette_span_hours
local DUST_COL = Look.ground_dust_color

local _hour = 12
local _f = { 1, 1, 1 }
local _t = 0               -- clouds' clock, seconds
local _behind = {}
local _spores = {}         -- not graded by the time-of-day filter

local function add(list, name, tex, over)
    local src = presets[name]
    if src == nil or tex == nil then return end
    local p = src
    if over ~= nil then
        p = {}
        for k, v in pairs(src) do p[k] = v end
        for k, v in pairs(over) do p[k] = v end
    end
    list[#list + 1] = Emitter.new(p, tex)
end

local function each(list, fn)
    for _, e in ipairs(list) do fn(e) end
end

local function draw_list(list, ox, oy)
    for _, e in ipairs(list) do e:draw(ox, oy, 1, _f[1], _f[2], _f[3]) end
end

local function sun_for(hour)
    if hour <= SUN.day.last then return SUN.day end
    if hour <= SUN.sunset.last then return SUN.sunset end
    return nil
end

local function draw_clouds(ox, oy)
    local tx = assets.get("fx_cloud")
    if tx == nil or not tx.Ready then return end
    local right = THEME:GetResolution().X
    tx:SetColor(_f[1], _f[2], _f[3])
    for _, row in ipairs(CLOUDS) do
        local s = row.scale
        local w = tx.Width * s
        local x = (row.offset + row.speed * _t) % w - w + ox
        if x > 0 then x = x - w end
        local y = CLOUD_FOOT - tx.Height * s + oy
        tx:SetScale(s, s)
        tx:SetOpacity(row.alpha)
        while x < right do
            tx:Draw(x, y)
            x = x + w
        end
    end
    tx:SetScale(1, 1)
    tx:SetOpacity(1)
    tx:SetColor(1, 1, 1)
end

function M.keys(level, ctx, variant)
    local hour = skybox.hourFor(level)
    local keys = { "bg_sky_clear", "fx_mist", "fx_cloud", "fx_vignette" }
    if sun_for(hour) ~= nil then keys[#keys + 1] = "fx_glow" end
    if hour >= SPORE_HOUR then keys[#keys + 1] = "fx_mote" end
    return keys
end

function M.open(level, ctx, variant)
    _hour = skybox.hourFor(level)
    skybox.setView(_hour, PITCH)
    _f = { skybox.filterFor(_hour) }
    _t = 0
    assets.open(M.keys(level, ctx, variant))

    _behind, _spores = {}, {}
    add(_behind, "mist", assets.get("fx_mist"), WISPS)
    if _hour >= SPORE_HOUR then add(_spores, "spores", (assets.get("fx_mote"))) end
end

-- 21+ needs door 10 beaten, so never grey
function M.sky() return 0, true end

function M.castAlpha() return 1 end
function M.castFilter() return _f[1], _f[2], _f[3] end
function M.groundDust() return DUST_COL[1] * _f[1], DUST_COL[2] * _f[2], DUST_COL[3] * _f[3] end

function M.update(dt)
    _t = _t + dt
    local function step(e) e:update(dt) end
    each(_behind, step) ; each(_spores, step)
end

function M.drawBehind(ox, oy)
    ox, oy = view.shake(ox, oy)
    draw_clouds(ox, oy)
    draw_list(_behind, ox, oy)
end

function M.drawBack(ox, oy)
    view.shake(ox, oy)
    local bg, meta = assets.get("bg_sky_clear")
    view.drawPlaced(bg, meta, _f[1], _f[2], _f[3], 1)
    local sun = sun_for(_hour)
    if sun ~= nil then
        local x, y = view.xf(sun.x, sun.y)
        Light.glow(assets.get("fx_glow"), x, y, sun.scale * view.scale(), sun.r, sun.g, sun.b, sun.a)
    end
end

function M.drawMid(ox, oy)
    ox, oy = view.shake(ox, oy)
    for _, e in ipairs(_spores) do e:draw(ox, oy, 1, 1, 1, 1) end
end

function M.drawFront(ox, oy)
    view.shake(ox, oy)
    local k = math.max(0, math.min(1, (_hour - VIGNETTE_FROM) / VIGNETTE_SPAN))
    Light.vignette(assets.get("fx_vignette"), VIGNETTE_DAY + (VIGNETTE_NIGHT - VIGNETTE_DAY) * k)
end

function M.close()
    local function clear(e) e:clear() end
    each(_behind, clear) ; each(_spores, clear)
    _behind, _spores = {}, {}
end

return M
