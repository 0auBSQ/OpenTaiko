---@diagnostic disable: undefined-global, lowercase-global
-- Easing.lua — pure easing curves on a 0..1 ratio, for animations driven by a script's own clock
-- (timelines, particle ages) where the engine's counters (COUNTER + SetEasing, engine-clocked and
-- stateful) are not the right tool. Every curve clamps its input and maps 0 -> 0, 1 -> 1, except
-- hump which rises and falls (0 -> 0, 0.5 -> 1, 1 -> 0).
--
--   local Easing = require("Easing")
--   local k = Easing.outBack(Easing.span(t, startTime, duration))
--   x = Easing.lerp(x0, x1, k)
--
-- Easing.get("OUT", "BACK") returns a curve by the engine's own type / function names.

local Easing = {}

local sin, pi = math.sin, math.pi

local function clamp01(t) return t < 0 and 0 or (t > 1 and 1 or t) end
Easing.clamp01 = clamp01

function Easing.linear(t) return clamp01(t) end
function Easing.inQuad(t) t = clamp01(t) ; return t * t end
function Easing.outQuad(t) t = clamp01(t) ; return 1 - (1 - t) * (1 - t) end
function Easing.inOutQuad(t) t = clamp01(t) ; return t < 0.5 and 2 * t * t or 1 - (-2 * t + 2) ^ 2 / 2 end
function Easing.inCubic(t) t = clamp01(t) ; return t * t * t end
function Easing.outCubic(t) t = clamp01(t) ; local u = 1 - t ; return 1 - u * u * u end
function Easing.inOutCubic(t) t = clamp01(t) ; return t < 0.5 and 4 * t * t * t or 1 - (-2 * t + 2) ^ 3 / 2 end
function Easing.inSine(t) t = clamp01(t) ; return 1 - math.cos(t * pi / 2) end
function Easing.outSine(t) t = clamp01(t) ; return sin(t * pi / 2) end
function Easing.inOutSine(t) t = clamp01(t) ; return -(math.cos(pi * t) - 1) / 2 end
function Easing.inExpo(t) t = clamp01(t) ; return t == 0 and 0 or 2 ^ (10 * t - 10) end
function Easing.outExpo(t) t = clamp01(t) ; return t == 1 and 1 or 1 - 2 ^ (-10 * t) end

-- s: the overshoot amount (1.7 is the classic feel)
function Easing.outBack(t, s)
    t = clamp01(t) ; s = s or 1.7 ; local u = t - 1
    return 1 + u * u * ((s + 1) * u + s)
end
function Easing.inBack(t, s)
    t = clamp01(t) ; s = s or 1.7
    return t * t * ((s + 1) * t - s)
end

function Easing.outBounce(t)
    t = clamp01(t)
    if t < 1 / 2.75 then return 7.5625 * t * t end
    if t < 2 / 2.75 then t = t - 1.5 / 2.75 ; return 7.5625 * t * t + 0.75 end
    if t < 2.5 / 2.75 then t = t - 2.25 / 2.75 ; return 7.5625 * t * t + 0.9375 end
    t = t - 2.625 / 2.75 ; return 7.5625 * t * t + 0.984375
end
function Easing.inBounce(t) return 1 - Easing.outBounce(1 - clamp01(t)) end

function Easing.outElastic(t)
    t = clamp01(t)
    if t == 0 or t == 1 then return t end
    return 2 ^ (-10 * t) * sin((t * 10 - 0.75) * (2 * pi) / 3) + 1
end

-- 0 -> 1 -> 0, for squashes and flashes
function Easing.hump(t) return sin(clamp01(t) * pi) end

-- the progress of a window [t0, t0 + dur] at time t
function Easing.span(t, t0, dur) return clamp01((t - t0) / dur) end
function Easing.lerp(a, b, k) return a + (b - a) * k end

-- a curve by the engine's names: type IN / OUT / INOUT, function LINEAR / SINE / QUAD / CUBIC /
-- EXPO / BACK / BOUNCE / ELASTIC (the rest fall back to QUAD)
local BY_NAME = {
    IN = { LINEAR = Easing.linear, SINE = Easing.inSine, QUAD = Easing.inQuad, CUBIC = Easing.inCubic, EXPO = Easing.inExpo,
           BACK = Easing.inBack, BOUNCE = Easing.inBounce },
    OUT = { LINEAR = Easing.linear, SINE = Easing.outSine, QUAD = Easing.outQuad, CUBIC = Easing.outCubic, EXPO = Easing.outExpo,
            BACK = Easing.outBack, BOUNCE = Easing.outBounce, ELASTIC = Easing.outElastic },
    INOUT = { LINEAR = Easing.linear, SINE = Easing.inOutSine, QUAD = Easing.inOutQuad, CUBIC = Easing.inOutCubic },
}
function Easing.get(kind, fn)
    local t = BY_NAME[(kind or "OUT"):upper()] or BY_NAME.OUT
    return t[(fn or "QUAD"):upper()] or t.QUAD or Easing.outQuad
end

return Easing
