-- Level-20 cutscene (lines: story/cutscenes/level20.lua; events land in event()).

local Corruption = require("encounter/cutscenes/corruption")
local Easing     = require("Easing")
local assets     = require("encounter/assets")
local scene      = require("encounter/scene")
local view       = require("encounter/bands/view")
local skybox     = require("skybox")
local Light      = require("fx/light")
local Storm      = require("encounter/cutscenes/storm")
local Glitch     = require("encounter/cutscenes/glitch")
local Star       = require("encounter/cutscenes/star")
local Config     = require("Core.Config")
local Layout    = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")

local M = {}

local Look = Layout.encounter.cutscenes.level20
local Anim = Animation.encounter.cutscenes.level20

local REF_W, REF_H = 1920, 1080
local TAU = math.pi * 2
local OVERSCAN = Look.overscan_px      -- px past each screen edge, for the shakes
local SINK = Look.sink_px              -- px bottom-resting layers reach below the screen

local STORM_KEYS = { "cut20_bg", "cut20_cloud", "cut20_bolt", "cut20_rain", "cut20_white", "cut20_glitch",
                     "cut20_tobiraph", "cut20_ume" }
local CLEAR_KEYS = { "cut20_corrupt", "cut20_ume_clear",
                     "cut20_star", "cut20_bean", "cut20_final", "fx_glow", "fx_shaft", "fx_sparkle" }

-- per screen: sound name -> Sounds/result/<file>.ogg
local SOUNDS = {
    enter = { rain = "rain", thunder = "thunder", thunder_far = "thunder_far", glitch = "glitch" },
    fail  = { rain = "rain", thunder = "thunder", thunder_far = "thunder_far", glitch = "glitch", wind = "wind" },
    clear = { rain = "rain", thunder = "thunder", thunder_far = "thunder_far", glitch = "glitch", slam = "slam",
              buildup = "buildup", ping = "star_ping",
              fly = "star_fly", grow = "grow", grow_rumble = "grow_rumble" },
}
local RAIN_VOL = { enter = 50, clear = 60, fail = 60 }     -- lower on the entrance, under the floor loop
local GLITCH_VOL = 40
local BUILDUP_VOL = 80
local GROW_RUMBLE_VOL = 70

local SKY_HOUR, SKY_PITCH = Look.sky_hour, Look.sky_pitch_deg   -- same as the roof floors
local SCRIM_ALPHA = Look.scrim_opacity

local BOB_PX, BOB_SEC = Anim.bob_px, Anim.bob_s
local SWAY_PX, SWAY_SEC = Anim.sway_px, Anim.sway_s
local GLITCH_SIZE = Look.glitch_size   -- scales the glitch to the figure's size
local BOLTS = Look.bolt_x
local UME_IN_DELAY, UME_IN_SEC = Anim.ume_in_delay_s, Anim.ume_in_s
local UME_OFF = Look.ume_off_px        -- px beyond Ume's width for the slide

-- clear
local SLAM_AMP, SLAM_SEC = Anim.slam_shake_px, Anim.slam_shake_s
local RUMBLE_AMP = Anim.rumble_px
local CALM_SEC = Anim.calm_s
local FADE_FROM, FADE_SEC = Anim.fade_from_s, Anim.fade_s
local CORRUPT_SEC, CORRUPT_TAIL = Anim.corrupt_s, Anim.corrupt_tail_s
local UME_CLEAR_SEC = Anim.ume_clear_s
local BUILDUP_FADE = Anim.buildup_fade_s
local AFTER_RATE = Anim.after_rate     -- glitch rate after the corruption, unvoiced

local TOBI_FADE_TO, TOBI_FADE_SEC = Anim.tobi_fade_to, Anim.tobi_fade_s
-- ascent
local AWAY_SEC = Anim.away_s
local AWAY_SCALE = Look.away_scale
local AWAY_TO = { Look.away_to.x, Look.away_to.y }
local AWAY_FADE_TO = Anim.away_fade_to

-- gleam
local GLEAM_SEC = Anim.gleam_s
local GLEAM_PEAK, GLEAM_REST, GLEAM_PEAK_AT = Anim.gleam_peak, Anim.gleam_rest, Anim.gleam_peak_at_s
local GLEAM_SCALE = Look.gleam_scale
local GLEAM_DX = Look.gleam_dx_px      -- x offset from the star's start
local SHAFTS = {}                      -- x offsets from the gleam
for i, sh in ipairs(Look.shafts) do SHAFTS[i] = { sh.dx, sh.y, sh.rot_deg, sh.sx, sh.sy } end
local SHAFT_A = Look.shaft_opacity
local WARM = Look.warm_color

local FINIAL_SEC = Anim.finial_s
local FINIAL_SCALE, FINIAL_A = Look.finial_scale, Look.finial_opacity
local FINIAL_COL = Look.finial_color
local FINIAL_BREATH_HZ = Anim.finial_breath_hz
local BEAN_SEC, BEAN_RISE = Anim.bean_s, Anim.bean_rise_px
local BEAN_RUMBLE = Anim.bean_rumble_px
local FINIAL_UNDER_BEAN = Anim.finial_under_bean

local HOP_SEC, HOPS, HOP_STRETCH = Anim.hop_s, Anim.hops, Anim.hop_stretch
local FINAL_SEC = Anim.final_s

-- fail
local GUST_SEC = Anim.gust_s
local UME_OUT_SEC = Anim.ume_out_s
local WIND_HOLD = Anim.wind_hold_s
local WIND_FADE_AT, WIND_FADE_SEC = Anim.wind_fade_at_s, Anim.wind_fade_s

local _phase = nil
local _t, _busyUntil = 0, 0
local _tx, _m = {}, {}
local _snd = {}
local _storm, _glitch, _corrupt, _star = nil, nil, nil, nil
local _s = {}                          -- animated values
local _tweens, _cues = {}, {}
local _sky = false
local _gleamAt, _finialAt, _hopAt = nil, nil, nil
local _starX = nil                     -- star start x (1920 space)
local _buildupOn, _windOn = false, false

local function keyList(phase)
    local list = {}
    for _, k in ipairs(STORM_KEYS) do list[#list + 1] = k end
    if phase == "clear" then
        for _, k in ipairs(CLEAR_KEYS) do list[#list + 1] = k end
    end
    return list
end

function M.keys(level, ctx, phase)
    return keyList(phase)
end

-- stops the tween on obj[field], leaving it where it is now
local function untween(obj, field)
    for i = #_tweens, 1, -1 do
        if _tweens[i].obj == obj and _tweens[i].field == field then table.remove(_tweens, i) end
    end
end

-- moves obj[field] to `to` over sec seconds after delay, replacing a tween already on it
local function tween(obj, field, to, sec, ease, delay)
    untween(obj, field)
    _tweens[#_tweens + 1] = { obj = obj, field = field, to = to, sec = math.max(1e-3, sec),
                              ease = ease or Easing.linear, t = -(delay or 0) }
end

local function stepTweens(dt)
    local i = 1
    while i <= #_tweens do
        local w = _tweens[i]
        w.t = w.t + dt
        if w.t >= 0 then
            if w.from == nil then w.from = w.obj[w.field] end
            local k = math.min(1, w.t / w.sec)
            w.obj[w.field] = w.from + (w.to - w.from) * w.ease(k)
            if k >= 1 then table.remove(_tweens, i) else i = i + 1 end
        else
            i = i + 1
        end
    end
end

-- runs fn sec seconds from now
local function cue(sec, fn)
    _cues[#_cues + 1] = { at = _t + sec, fn = fn }
end

local function stepCues()
    local i = 1
    while i <= #_cues do
        if _t >= _cues[i].at then
            local c = table.remove(_cues, i)
            c.fn()
        else
            i = i + 1
        end
    end
end

local function play(name, vol)
    local s = _snd[name]
    if s == nil then return end
    if vol ~= nil then s:SetVolumePercent(vol) end
    s:Play()
end

function M.open(level, ctx, phase)
    _phase = phase
    _t, _busyUntil = 0, 0
    _tweens, _cues = {}, {}
    _sky = false
    _gleamAt, _finialAt, _hopAt = nil, nil, nil
    _starX = nil
    _buildupOn, _windOn = false, false
    _tx, _m = {}, {}
    for _, k in ipairs(keyList(phase)) do _tx[k], _m[k] = assets.get(k) end

    _snd = {}
    for name, file in pairs(SOUNDS[phase] or SOUNDS.enter) do
        local ok, s = pcall(function() return SOUND:CreateSFX("Sounds/result/" .. file .. ".ogg") end)
        if ok and s ~= nil then _snd[name] = s end
    end
    if _snd.glitch ~= nil then _snd.glitch:SetVolumePercent(GLITCH_VOL) end

    local data = assets.data("cut20") or {}
    local cloud = _m.cut20_cloud
    _storm = Storm.new({ rain = _tx.cut20_rain, bolt = _tx.cut20_bolt, white = _tx.cut20_white, cloud = _tx.cut20_cloud },
                       cloud and { w = cloud.w, y = (cloud.y or 0) + SINK, scale = data.cloud_scale or 1 },
                       { rain = _snd.rain, thunder = _snd.thunder, far = _snd.thunder_far }, nil, BOLTS)
    local rects = assets.data("cut20_glitch") or {}
    _glitch = Glitch.new({ atlas = _tx.cut20_glitch, white = _tx.cut20_white }, rects, _snd.glitch, GLITCH_SIZE)
    _corrupt, _star = nil, nil
    if phase == "clear" then
        _corrupt = Corruption.new({ atlas = _tx.cut20_glitch, white = _tx.cut20_white }, rects)
        _star = Star.new({ star = _tx.cut20_star, glow = _tx.fx_glow, sparkle = _tx.fx_sparkle },
                         { ping = _snd.ping, fly = _snd.fly })
        skybox.setView(SKY_HOUR, SKY_PITCH)
    end

    local ume = _m.cut20_ume
    _s = {
        bg = 1, tobiA = 1, tobiK = 0, away = 0,
        umeDx = (phase == "enter" and ume ~= nil) and -(ume.w + UME_OFF) or 0, umeA = 1, umeK = 0,
        bean = 0, beanY = BEAN_RISE, finial = 0, gleam = 0, final = 0, scrim = SCRIM_ALPHA,
        buildupVol = BUILDUP_VOL, windVol = 100,
    }
end

function M.begin()
    if _storm ~= nil then _storm:start(RAIN_VOL[_phase] or 60) end
    if _phase == "enter" then
        tween(_s, "umeDx", 0, UME_IN_SEC, Easing.outCubic, UME_IN_DELAY)
        _busyUntil = _t + UME_IN_DELAY + UME_IN_SEC
    end
end

local function hold(sec)
    _busyUntil = math.max(_busyUntil, _t + sec)
end

local EVENTS = {}

function EVENTS.corrupt()
    if _corrupt == nil then return end
    _sky = true
    play("slam")
    scene.shake(SLAM_AMP, SLAM_SEC)
    scene.rumble(RUMBLE_AMP)
    if _snd.buildup ~= nil then
        _snd.buildup:SetLoop(true)
        play("buildup", BUILDUP_VOL)
        _buildupOn = true
    end
    _storm:calm(CALM_SEC)
    _corrupt:start(CORRUPT_SEC, CORRUPT_TAIL)
    _glitch:force(CORRUPT_SEC)
    _glitch:setRate(AFTER_RATE, false)
    tween(_s, "bg", 0, FADE_SEC, Easing.inOutSine, FADE_FROM)
    tween(_s, "tobiK", 1, FADE_SEC, Easing.inOutSine, FADE_FROM)
    tween(_s, "umeK", 1, UME_CLEAR_SEC, Easing.inOutSine, CORRUPT_SEC)
    tween(_s, "buildupVol", 0, BUILDUP_FADE, Easing.linear, CORRUPT_SEC)
    cue(CORRUPT_SEC, function() scene.rumble(0) end)
    hold(CORRUPT_SEC + CORRUPT_TAIL)
end

function EVENTS.tobi_fade()
    tween(_s, "tobiA", TOBI_FADE_TO, TOBI_FADE_SEC, Easing.inOutSine)
end

-- away runs 0..1 with the distance, so the shrink follows perspective
function EVENTS.ascend()
    tween(_s, "away", 1, AWAY_SEC, Easing.inOutSine)
    tween(_s, "tobiA", AWAY_FADE_TO, AWAY_SEC, Easing.inOutSine)
end

-- returns the eye (1920 space) and the figure's size
local function tobiEye()
    local m = _m.cut20_tobiraph
    if m == nil then return 0, 0, 1 end
    local sc = 1 / (1 + (1 / AWAY_SCALE - 1) * _s.away)
    local ex = m.x + m.fx + math.sin(_t * TAU / SWAY_SEC) * SWAY_PX
    local ey = m.y + m.fy + math.sin(_t * TAU / BOB_SEC) * BOB_PX
    return AWAY_TO[1] + (ex - AWAY_TO[1]) * sc, AWAY_TO[2] + (ey - AWAY_TO[2]) * sc, sc
end

-- the star starts at the figure, which stops shrinking there
function EVENTS.star()
    if _star == nil then return end
    _glitch:setRate(0)
    untween(_s, "away")
    local x, y = tobiEye()
    _starX = x
    _star:start(x, y)
end

function EVENTS.gleam()
    _gleamAt = _t
    tween(_s, "gleam", 1, GLEAM_SEC, Easing.outSine)
end

function EVENTS.finial()
    _finialAt = _t
    tween(_s, "finial", 1, FINIAL_SEC, Easing.outSine)
end

function EVENTS.bean()
    play("grow")
    play("grow_rumble", GROW_RUMBLE_VOL)
    tween(_s, "bean", 1, BEAN_SEC, Easing.outCubic)
    tween(_s, "beanY", 0, BEAN_SEC, Easing.outCubic)
    tween(_s, "finial", FINIAL_UNDER_BEAN, BEAN_SEC, Easing.inOutSine)
    scene.rumble(BEAN_RUMBLE)
    cue(BEAN_SEC, function() scene.rumble(0) end)
    hold(BEAN_SEC)
end

function EVENTS.hop()
    _hopAt = _t
end

function EVENTS.final()
    tween(_s, "final", 1, FINAL_SEC, Easing.inOutSine)
    tween(_s, "scrim", 0, FINAL_SEC, Easing.inOutSine)
    hold(FINAL_SEC)
end

function EVENTS.wind()
    local ume = _m.cut20_ume
    if _snd.wind ~= nil then
        play("wind", 100)
        _windOn = true
        tween(_s, "windVol", 0, WIND_FADE_SEC, Easing.linear, WIND_FADE_AT)
    end
    _storm:gust(GUST_SEC)
    if ume ~= nil then tween(_s, "umeDx", -(ume.w + UME_OFF), UME_OUT_SEC, Easing.inCubic) end
    tween(_s, "umeA", 0, UME_OUT_SEC * Anim.ume_fade_share, Easing.inQuad, UME_OUT_SEC * Anim.ume_fade_delay_share)
    hold(WIND_HOLD)
end

function M.event(name, line)
    local fn = EVENTS[name]
    if fn ~= nil and _phase ~= nil then fn(line) end
end

function M.busy()
    if _t < _busyUntil then return true end
    return _star ~= nil and _star:busy()
end

function M.update(dt)
    if _phase == nil then return end
    _t = _t + dt
    stepTweens(dt)
    stepCues()
    _storm:update(dt)
    _glitch:update(dt)
    if _corrupt ~= nil then _corrupt:update(dt) end
    if _star ~= nil then _star:update(dt) end
    if _buildupOn then
        if _s.buildupVol <= 0 then
            _snd.buildup:Stop()
            _buildupOn = false
        else
            _snd.buildup:SetVolumePercent(_s.buildupVol)
        end
    end
    if _windOn then
        if _s.windVol <= 0 then
            _snd.wind:Stop()
            _windOn = false
        else
            _snd.wind:SetVolumePercent(_s.windVol)
        end
    end
end

function M.scrimAlpha()
    return _s.scrim or SCRIM_ALPHA
end

-- sky visible from the clear's backdrop fade until the final picture covers it
function M.sky()
    if _sky and (_s.bg or 1) < 0.999 and (_s.final or 0) < 0.999 then return 0, true end
    return 0, false
end

-- placed art: top-left (x, y) in 1920 space through the push-in and shake, scaled by sx, sy
local function placed(tx, x, y, a, sx, sy)
    if tx == nil or not tx.Ready or a <= 0.002 then return end
    local px, py = view.xf(x, y)
    local k = view.scale()
    tx:SetScale(k * (sx or 1), k * (sy or sx or 1))
    tx:SetOpacity(math.min(1, a))
    tx:DrawAtAnchor(px, py, "topleft")
    tx:SetScale(1, 1)
    tx:SetOpacity(1)
end

-- a full-screen layer stored small, reaching `over` px past each screen edge
local function fullScreen(key, a, over)
    local tx, m = _tx[key], _m[key]
    if m == nil then return end
    over = over or 0
    local s = (REF_W + 2 * over) / math.max(1, m.w)
    placed(tx, -over, -over * REF_H / REF_W, a, s, s)
end

local function drawBean()
    local m = _m.cut20_bean
    if m == nil then return end
    placed(_tx.cut20_bean, m.x, m.y + _s.beanY, _s.bean)
end

-- top-left on whole pixels, so the glitch's slices meet without gaps
local function drawTobiraph()
    local m = _m.cut20_tobiraph
    if m == nil then return end
    local fs, fa, fw = 1, 1, 0
    if _star ~= nil then
        if _star:gone() then return end
        fs, fa, fw = _star:figure()
    end
    local a = _s.tobiA * fa
    if a <= 0.002 then return end
    local ex, ey, sc = tobiEye()
    local px, py = view.xf(ex, ey)
    local s = view.scale() * fs * sc
    local x, y = math.floor(px - m.fx * s + 0.5), math.floor(py - m.fy * s + 0.5)
    _glitch:drawBack(px, py, s * GLITCH_SIZE, a)
    local c = _s.tobiK
    _glitch:drawFigure(_tx.cut20_tobiraph, x, y, s, a * math.min(1, 2 * (1 - c)))
    _glitch:drawFigure(_tx.cut20_corrupt, x, y, s, a * math.min(1, 2 * c))
    local burn = _tx.cut20_corrupt
    if fw > 0.002 and burn ~= nil and burn.Ready then
        burn:SetBlendMode("Add")
        burn:SetScale(s, s)
        burn:SetOpacity(math.min(1, fw * a))
        burn:DrawAtAnchor(x, y, "topleft")
        burn:SetBlendMode("Normal")
        burn:SetScale(1, 1)
        burn:SetOpacity(1)
    end
end

-- 1920 space
local function gleamX()
    if _starX ~= nil then return _starX + GLEAM_DX end
    local m = _m.cut20_tobiraph
    return ((m ~= nil) and (m.x + m.fx) or REF_W / 2) + GLEAM_DX
end

-- drawn over the scrim
local function drawGleam()
    if _gleamAt == nil then return end
    local u = _t - _gleamAt
    local g = (u < GLEAM_PEAK_AT) and GLEAM_PEAK * Easing.outQuad(u / GLEAM_PEAK_AT)
        or GLEAM_REST + (GLEAM_PEAK - GLEAM_REST) * math.max(0, 1 - (u - GLEAM_PEAK_AT) / GLEAM_SEC)
    local gx, gy = view.xf(gleamX(), 0)
    Light.glow(_tx.fx_glow, gx, gy, GLEAM_SCALE * view.scale(), WARM[1], WARM[2], WARM[3], g * (1 - _s.final))
end

local function drawShafts()
    local a = _s.gleam * SHAFT_A * (1 - _s.final)
    if a <= 0.002 then return end
    local k = view.scale()
    local x0 = gleamX()
    for i, sh in ipairs(SHAFTS) do
        local x, y = view.xf(x0 + sh[1], sh[2])
        local b = Anim.shaft_breath_min + Anim.shaft_breath_amp * math.sin(_t * Anim.shaft_breath_rate + i * Anim.shaft_phase_step)
        Light.shaft(_tx.fx_shaft, x, y, sh[3], sh[4] * k, sh[5] * k, WARM[1], WARM[2], WARM[3], a * b)
    end
end

local function drawFinial()
    if _finialAt == nil or _s.finial <= 0.002 then return end
    local data = assets.data("cut20") or {}
    local x, y = view.xf(data.finial_x or REF_W / 2, data.finial_y or 1080)
    local b = Anim.finial_breath_min + Anim.finial_breath_amp * math.sin((_t - _finialAt) * FINIAL_BREATH_HZ * TAU)
    Light.glow(_tx.fx_glow, x, y, FINIAL_SCALE * view.scale(), FINIAL_COL[1], FINIAL_COL[2], FINIAL_COL[3],
               _s.finial * FINIAL_A * b * (1 - _s.final))
end

local function drawUme()
    local m = _m.cut20_ume
    if m == nil or _s.umeA <= 0.002 then return end
    local sy = 1
    if _hopAt ~= nil then
        local u = (_t - _hopAt) / HOP_SEC
        if u < 1 then sy = 1 + HOP_STRETCH * math.abs(math.sin(u * HOPS * math.pi)) end
    end
    local sx = 1 - 0.5 * (sy - 1)
    local x = m.x + _s.umeDx + m.w * (1 - sx) * 0.5
    local y = m.y + SINK + m.h * (1 - sy)
    local c = _s.umeK
    placed(_tx.cut20_ume, x, y, _s.umeA * math.min(1, 2 * (1 - c)), sx, sy)
    placed(_tx.cut20_ume_clear, x, y, _s.umeA * math.min(1, 2 * c), sx, sy)
end

-- under the scrim, back to front
function M.draw(ox, oy)
    if _phase == nil then return end
    view.shake(ox, oy)
    if _s.final >= 0.999 then
        fullScreen("cut20_final", 1)
        return
    end
    fullScreen("cut20_bg", _s.bg, OVERSCAN)
    drawBean()
    _storm:drawBack()
    drawShafts()
    drawTobiraph()
    _storm:drawRain(1, ox, oy)
    drawFinial()
    drawUme()
    _storm:drawRain(2, ox, oy)
    _storm:drawFlash()
    fullScreen("cut20_final", _s.final)
end

function M.drawOver(ox, oy)
    if _phase == nil or _s.final >= 0.999 then return end
    view.shake(ox, oy)
    if _star ~= nil then _star:draw() end
    drawGleam()
    if _corrupt ~= nil then _corrupt:draw(ox, oy) end
    _glitch:drawOver(ox, oy, 1 - _s.final)
end

function M.close()
    for _, s in pairs(_snd) do
        s:Stop()
        s:Dispose()
    end
    _snd = {}
    _phase = nil
    _tx, _m = {}, {}
    _storm, _glitch, _corrupt, _star = nil, nil, nil, nil
    _tweens, _cues = {}, {}
    _s = {}
    _sky = false
    _starX = nil
    _buildupOn, _windOn = false, false
end

return M
