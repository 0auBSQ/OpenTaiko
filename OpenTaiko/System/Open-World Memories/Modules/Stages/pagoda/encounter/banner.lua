-- CLEAR! / FAILED... banner of the encounter screen.

local assets  = require("encounter/assets")
local sprite  = require("encounter/sprite")
local Easing  = require("Easing")
local Emitter = require("fx/emitter")
local presets = require("fx/presets")
local Config  = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")
local Look = Layout.encounter.banner
local Clear = Animation.encounter.banner.clear
local Fail = Animation.encounter.banner.fail
local Fade = Animation.encounter.banner.fade

local M = {}

local REF_W, REF_H = 1920, 1080
local CENTER_Y = Look.center_y  -- word centre y, 1920x1080 px

-- CLEAR!
local POP_EVERY  = Clear.pop_every_s
local POP_SEC    = Clear.pop_s
local POP_FROM   = Clear.pop_from_scale
local POP_BACK   = Clear.pop_overshoot   -- Easing.outBack overshoot
local POP_DROP   = Clear.pop_drop_px
local POP_TILT   = Clear.pop_tilt_deg    -- deg, sign alternates per letter
local POP_LAND   = Clear.pop_land_share  -- share of the pop (0..1) when the glitter burst fires
local POP_FADE   = Clear.fade_in_share
local WAVE_AT    = Clear.wave_at_s       -- s after the last pop
local WAVE_EVERY = Clear.wave_every_s
local WAVE_SEC   = Clear.wave_s
local WAVE_HOP   = Clear.wave_hop_px
local WAVE_SPIN  = Clear.wave_spin_deg
local WAVE_SHRINK = Clear.wave_shrink    -- scale loss of the spinning letter, so it clears its neighbours
local CLEAR_REST = Clear.rest_s          -- s after the wave before shown()
local BOB_AMP, BOB_HZ, BOB_PHASE = Clear.bob_px, Clear.bob_hz, Clear.bob_phase_step
local GLITTER_BURST = Clear.glitter_burst
local GLITTER_MARGIN = Look.glitter_margin_px

-- FAILED...
local FALL_EVERY = Fail.fall_every_s
local FALL_SEC   = Fail.fall_s
local FALL_FROM  = Fail.fall_from_px     -- px above the rest position
local FALL_FADE  = Fail.fade_in_share
local BOUNCE_SEC = Fail.bounce_s
local BOUNCE_H   = Fail.bounce_px
local HOP_DECAY  = Fail.hop_decay
local SQUASH     = Fail.squash
local SPRING_FREQ, SPRING_DECAY = Fail.spring_freq, Fail.spring_decay
local LAND_WOB, LAND_WOB_FREQ, LAND_WOB_DECAY = Fail.land_wobble_deg, Fail.land_wobble_freq, Fail.land_wobble_decay
-- per letter: tilt at rest (deg), drop at rest (px), extra turn while falling
local FAIL_TILT  = Fail.tilt_deg
local FAIL_DROP  = Fail.drop_px
local FAIL_SPIN  = Fail.spin_deg
local WOBBLE_AMP, WOBBLE_HZ, WOBBLE_PHASE = Fail.wobble_deg, Fail.wobble_hz, Fail.wobble_phase_step

local FADE_SEC  = Fade.fade_s
local FADE_GROW = Fade.grow              -- CLEAR! scale gain over the fade
local FADE_SINK = Fade.sink_px           -- FAILED... px drop over the fade

local _kind = nil
local _letters = {}             -- { tex, meta, x, y }; x, y: sprite centre relative to the anchor
local _t = 0
local _fadeT = nil
local _shownAt = 0
local _box = { w = 0, h = 0 }
local _glitter = nil
local _burst = 0                -- count of letters whose glitter burst fired
local _o = {}

local function word(kind)
    local d = assets.data(kind == "fail" and "banner_fail" or "banner_clear")
    if type(d) ~= "table" or type(d.letters) ~= "table" then return nil end
    return d
end

function M.keys(kind)
    local keys, seen = {}, {}
    local function add(k)
        if k ~= nil and not seen[k] then seen[k] = true ; keys[#keys + 1] = k end
    end
    local d = word(kind)
    if d ~= nil then
        for _, l in ipairs(d.letters) do add(l.key) end
    end
    if kind ~= "fail" then
        for _, k in ipairs(Emitter.texKeys(presets.glitter)) do add(k) end
    end
    return keys
end

function M.open(kind)
    M.close()
    _kind = (kind == "fail") and "fail" or "clear"
    _t, _fadeT, _burst = 0, nil, 0
    _letters = {}
    local d = word(_kind)
    if d ~= nil then
        _box.w, _box.h = d.w or 0, d.h or 0
        for i, l in ipairs(d.letters) do
            local tx, meta = assets.get(l.key)
            _letters[i] = { tex = tx, meta = meta, x = l.x or 0, y = l.y or 0 }
        end
    end
    local n = #_letters
    if _kind == "clear" then
        local waveEnd = (n - 1) * POP_EVERY + POP_SEC + WAVE_AT + (n - 1) * WAVE_EVERY + WAVE_SEC
        _shownAt = waveEnd + CLEAR_REST
        local p = presets.glitter
        if p ~= nil then
            local map = {}
            for _, k in ipairs(Emitter.texKeys(p)) do map[k] = assets.get(k) end
            _glitter = Emitter.new(p, map)
            _glitter:setRate(0)
        end
    else
        _shownAt = (n - 1) * FALL_EVERY + FALL_SEC + BOUNCE_SEC
    end
end

-- returns the anchor (word centre, screen px) and the art scale
local function anchor()
    local res = THEME:GetResolution()
    return res.X * 0.5, CENTER_Y * res.Y / REF_H, res.X / REF_W
end

function M.update(dt)
    if _kind == nil then return end
    _t = _t + dt
    if _fadeT ~= nil then _fadeT = _fadeT + dt end
    if _glitter == nil then return end
    local ax, ay, k = anchor()
    while _burst < #_letters and _t >= _burst * POP_EVERY + POP_SEC * POP_LAND do
        _burst = _burst + 1
        local l = _letters[_burst]
        _glitter:burst(GLITTER_BURST, ax + l.x * k, ay + l.y * k)
    end
    local w, h = _box.w * k + 2 * GLITTER_MARGIN, _box.h * k + 2 * GLITTER_MARGIN
    _glitter:setArea(ax - w * 0.5, ay - h * 0.5, w, h)
    _glitter:setRate((_burst > 0 and _fadeT == nil) and 1 or 0)
    _glitter:update(dt)
end

-- CLEAR! letter i (1-based) at time t -> dx, dy (1920 px), scale, rotation, opacity
local function clearPose(i, t)
    local s0 = (i - 1) * POP_EVERY
    local u = (t - s0) / POP_SEC
    if u <= 0 then return nil end
    local e = Easing.outCubic(u)
    local sc = POP_FROM + (1 - POP_FROM) * Easing.outBack(u, POP_BACK)
    local dy = -POP_DROP * (1 - e)
    local rot = POP_TILT * (1 - e) * ((i % 2 == 0) and 1 or -1)
    local a = math.min(1, u / POP_FADE)
    local n = #_letters
    local w0 = (n - 1) * POP_EVERY + POP_SEC + WAVE_AT + (i - 1) * WAVE_EVERY
    local w = Easing.span(t, w0, WAVE_SEC)
    if w > 0 and w < 1 then
        local h = Easing.hump(w)
        rot = rot - WAVE_SPIN * Easing.inOutCubic(w)
        dy = dy - WAVE_HOP * h
        sc = sc * (1 - WAVE_SHRINK * h)
    end
    dy = dy + BOB_AMP * math.sin(2 * math.pi * BOB_HZ * t + i * BOB_PHASE) * Easing.span(u, 1, 1)
    return 0, dy, sc, sc, rot, a
end

-- FAILED... letter i at time t
local function failPose(i, t)
    local s0 = (i - 1) * FALL_EVERY
    local u = (t - s0) / FALL_SEC
    if u <= 0 then return nil end
    local tilt = FAIL_TILT[(i - 1) % #FAIL_TILT + 1]
    local rest = FAIL_DROP[(i - 1) % #FAIL_DROP + 1]
    local spin = FAIL_SPIN[(i - 1) % #FAIL_SPIN + 1]
    if u < 1 then
        return 0, rest - FALL_FROM * (1 - u * u), 1, 1, tilt + spin * (1 - u), math.min(1, u / FALL_FADE)
    end
    local b = (t - s0 - FALL_SEC) / BOUNCE_SEC
    local sign = (i % 2 == 0) and 1 or -1
    if b < 1 then
        local spring = math.exp(-SPRING_DECAY * b) * math.cos(SPRING_FREQ * math.pi * b)
        local hop = BOUNCE_H * math.max(0, math.sin(2 * math.pi * b)) * math.exp(-HOP_DECAY * b)
        local wob = LAND_WOB * math.exp(-LAND_WOB_DECAY * b) * math.sin(LAND_WOB_FREQ * math.pi * b) * sign
        return 0, rest - hop, 1 + SQUASH * spring, 1 - SQUASH * spring, tilt + wob, 1
    end
    local wob = WOBBLE_AMP * math.sin(2 * math.pi * WOBBLE_HZ * t + i * WOBBLE_PHASE)
    return 0, rest, 1, 1, tilt + wob, 1
end

function M.draw(ox, oy)
    if _kind == nil or #_letters == 0 then return end
    local ax, ay, k = anchor()
    ax, ay = ax + (ox or 0), ay + (oy or 0)
    local fade, f = 1, 0
    if _fadeT ~= nil then
        f = Easing.clamp01(_fadeT / FADE_SEC)
        fade = 1 - Easing.outQuad(f)
        if fade <= 0 then return end
    end
    local pose = (_kind == "clear") and clearPose or failPose
    for i, l in ipairs(_letters) do
        local dx, dy, sx, sy, rot, a = pose(i, _t)
        if dx ~= nil and l.tex ~= nil and l.meta ~= nil then
            if _kind == "clear" then
                sx, sy = sx * (1 + FADE_GROW * f), sy * (1 + FADE_GROW * f)
            else
                dy = dy + FADE_SINK * f
            end
            -- squash about the letter's foot
            local cy = l.y + dy + l.meta.h * 0.5 * (1 - sy)
            _o.sx, _o.sy, _o.rot = sx * k, sy * k, rot
            _o.alpha = a * fade
            _o.r, _o.g, _o.b, _o.blend = 1, 1, 1, "Normal"
            sprite.drawAt(l.tex, ax + (l.x + dx) * k, ay + cy * k, _o)
        end
    end
    if _glitter ~= nil then _glitter:draw(ox or 0, oy or 0, fade, 1, 1, 1) end
end

function M.fadeOut()
    if _kind ~= nil and _fadeT == nil then _fadeT = 0 end
end

-- true once every letter has landed (CLEAR!: and the spin wave has passed)
function M.shown()
    return _kind ~= nil and _t >= _shownAt
end

function M.gone()
    return _kind == nil or (_fadeT ~= nil and _fadeT >= FADE_SEC)
end

function M.close()
    if _glitter ~= nil then _glitter:clear() end
    _glitter = nil
    _kind = nil
    _letters = {}
    _t, _fadeT, _burst = 0, nil, 0
end

return M
