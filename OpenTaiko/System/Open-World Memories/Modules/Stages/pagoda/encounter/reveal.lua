-- Monster reveal on a line with `revealMonster = sec`; the next line waits for it.

local Easing = require("Easing")
local Config = require("Core.Config")
local Animation = Config.load("Config/animation.json")

local M = {}

local Anim = Animation.encounter.reveal
local EYES_PEAK, EYES_REST = Anim.eyes_peak, Anim.eyes_rest
M.EYES_REST = EYES_REST                       -- eyes value after the reveal
local EYES_RISE, EYES_SETTLE = Anim.eyes_rise_s, Anim.eyes_settle_s       -- seconds into the line
local PULSE_AMP, PULSE_HZ = Anim.pulse_amp, Anim.pulse_hz
local FADE_DELAY = Anim.fade_delay_s
local CATCH_UP = Anim.catch_up

local _target = 1
local _pending = false      -- a reveal line exists and has not started yet
local _running = false
local _dur, _tau = 0, 0
local _hurry = false

local function smoothstep(e0, e1, x)
    local t = (x - e0) / (e1 - e0)
    t = t < 0 and 0 or (t > 1 and 1 or t)
    return t * t * (3 - 2 * t)
end

local function hasReveal(lines)
    if type(lines) ~= "table" then return false end
    for _, l in ipairs(lines) do
        if type(l) == "table" then
            if l.revealMonster ~= nil then return true end
            if type(l.branch) == "table" then
                for _, br in pairs(l.branch) do
                    if hasReveal(br) then return true end
                end
            end
        end
    end
    return false
end

-- fade progress 0..1
local function progress()
    if not _running then return _pending and 0 or 1 end
    local delay = math.min(FADE_DELAY, _dur)
    local span = _dur - delay
    if span <= 0 then return _tau >= _dur and 1 or 0 end
    local k = (_tau - delay) / span
    return k < 0 and 0 or (k > 1 and 1 or k)
end

function M.prepare(lines, target)
    _target = target or 1
    _pending = hasReveal(lines)
    _running, _hurry = false, false
    _dur, _tau = 0, 0
end

function M.onLine(line)
    if not _pending or line == nil or line.revealMonster == nil then return end
    _pending, _running = false, true
    _dur = math.max(0, tonumber(line.revealMonster) or 0)
    _tau, _hurry = 0, false
end

function M.blocking()
    return _running and progress() < 1
end

function M.update(dt, advanceRequested)
    if not _running then return end
    if advanceRequested and M.blocking() then _hurry = true end
    local rate = (_hurry and M.blocking()) and CATCH_UP or 1
    _tau = _tau + dt * rate
end

function M.monsterAlpha()
    return _target * Easing.inOutSine(progress())
end

function M.eyes()
    if not _running then return 0 end
    local t = _tau
    if t < EYES_RISE then return EYES_PEAK * smoothstep(0, EYES_RISE, t) end
    if t < EYES_SETTLE then
        return EYES_PEAK + (EYES_REST - EYES_PEAK) * Easing.inOutSine((t - EYES_RISE) / (EYES_SETTLE - EYES_RISE))
    end
    return EYES_REST + PULSE_AMP * math.sin(2 * math.pi * PULSE_HZ * (t - EYES_SETTLE))
end

return M
