-- Pose curves of the cast's moves as pure functions of time; encounter/cast.lua applies them.
-- Positions are the feet (y down); angles are degrees counter-clockwise, as SetRotation.

local Easing = require("Easing")
local Config = require("Core.Config")
local Animation = Config.load("Config/animation.json")

local M = {}

local sin, cos, exp, abs, min, max, rad = math.sin, math.cos, math.exp, math.abs, math.min, math.max, math.rad
local PI = math.pi
local TAU = 2 * PI

local Anim = Animation.encounter.cast_moves
local WIDEN, THIN = Anim.squash_widen, Anim.stretch_thin   -- share of a squash that widens, of a stretch that thins

-- run cycle
local Wd = Anim.waddle
local WADDLE_DEG, WADDLE_HZ, WADDLE_SHARP = Wd.deg, Wd.hz, Wd.sharpness
local HOP_PX = Wd.hop_px

-- cheer
local Ch = Anim.cheer
local CHEER = {
    antic = Ch.antic_s, anticSquash = Ch.antic_squash,
    hops = {},
    contact = Ch.contact_s, contactSquash = Ch.contact_squash, stretch = Ch.stretch,
    spring = Ch.spring, springHz = Ch.spring_hz, springDecay = Ch.spring_decay,
}
for i, h in ipairs(Ch.hops) do CHEER.hops[i] = { px = h.px, sec = h.s } end

-- run off-screen
local Rn = Anim.run
local RUN_SPEED, RUN_ACCEL, RUN_WADDLE_IN = Rn.speed_px_per_s, Rn.accel_s, Rn.waddle_in_s
local DIAG_DEG = Rn.diag_deg
local UP_SHRINK, UP_SHRINK_PX = Rn.up_shrink, Rn.up_shrink_px
local UP_SHADOW_PX = Rn.up_shadow_px
local DIRS = {
    right = { 1, 0 },
    diag  = { cos(rad(DIAG_DEG)), -sin(rad(DIAG_DEG)) },
    up    = { 0, -1 },
}

-- climb
local Cl = Anim.climb
local BRAKE_SEC, BRAKE_LEAN = Cl.brake_s, Cl.brake_lean_deg
local STOP_HOLD, STOP_SQUASH = Cl.stop_hold_s, Cl.stop_squash
local TURN_SEC, TURN_HOP, TURN_BEAT = Cl.turn_s, Cl.turn_hop_px, Cl.turn_beat_s
local CLIMB_SEC, CLIMB_STEPS = Cl.climb_s, Cl.steps
local CLIMB_DX, CLIMB_DY, CLIMB_SHRINK = Cl.dx, Cl.dy, Cl.shrink
local STEP_HOP, STEP_TILT = Cl.step_hop_px, Cl.step_tilt_deg
local CLIMB_FADE_AT = Cl.fade_at        -- share of the climb before the fade

-- knockback
local Kn = Anim.knock
local KNOCK_HOLD, KNOCK_SQUASH_X, KNOCK_SQUASH_Y, KNOCK_UNSQUASH = Kn.hold_s, Kn.squash_x, Kn.squash_y, Kn.unsquash_per_s
local KNOCK_VY, KNOCK_GRAVITY = Kn.vy_px_per_s, Kn.gravity_px_per_s2
local KNOCK_SHADOW_SEC = Kn.shadow_s

local function tanh(x)
    local e = exp(2 * x)
    return (e - 1) / (e + 1)
end
local TANH_SHARP = tanh(WADDLE_SHARP)

-- run cycle at time t -> tilt (deg), hop (px, up)
function M.waddle(t)
    local s = sin(TAU * WADDLE_HZ * t)
    return WADDLE_DEG * tanh(WADDLE_SHARP * s) / TANH_SHARP, HOP_PX * abs(s)
end

-- cheer at time t -> dy (px), sx, sy, idle-bounce override (1 -> 0)
function M.cheer(t)
    local C = CHEER
    if t < 0 then return 0, 1, 1, 0 end
    if t < C.antic then
        local d = Easing.hump(t / C.antic)
        return 0, 1 + WIDEN * C.anticSquash * d, 1 - C.anticSquash * d, 1
    end
    t = t - C.antic
    for i, h in ipairs(C.hops) do
        if t < h.sec then
            local u = t / h.sec
            local s = C.stretch * (h.px / C.hops[1].px) * abs(1 - 2 * u)
            return -h.px * 4 * u * (1 - u), 1 - THIN * s, 1 + s, 1
        end
        t = t - h.sec
        if i < #C.hops then
            if t < C.contact then
                local d = Easing.hump(t / C.contact)
                return 0, 1 + WIDEN * C.contactSquash * d, 1 - C.contactSquash * d, 1
            end
            t = t - C.contact
        end
    end
    local e = exp(-C.springDecay * t)
    local s = C.spring * e * cos(TAU * C.springHz * t)
    return 0, 1 + WIDEN * s, 1 - s, e
end

local function runDistance(t)
    if t <= 0 then return 0 end
    if t < RUN_ACCEL then return RUN_SPEED * t * t / (2 * RUN_ACCEL) end
    return RUN_SPEED * (t - RUN_ACCEL * 0.5)
end

-- run off-screen from (x0, y0), t s in; dir = "right" | "diag" | "up".
-- Returns x, y (feet), dy (hop), rot, scale, shadow ground y, shadow opacity.
function M.run(x0, y0, t, dir)
    local v = DIRS[dir] or DIRS.right
    local d = runDistance(t)
    local x, y = x0 + v[1] * d, y0 + v[2] * d
    local rot, hop = M.waddle(t)
    local k = min(1, t / RUN_WADDLE_IN)
    if dir == "up" then
        return x, y, -hop * k, rot * k, 1 - UP_SHRINK * min(1, d / UP_SHRINK_PX), y0, 1 - min(1, d / UP_SHADOW_PX)
    end
    return x, y, -hop * k, rot * k, 1, y, 1
end

-- px covered t s into a dist px run that peaks at speed v and brakes to a stop at the end
local function stopDistance(t, dist, v, ends)
    if t <= 0 then return 0 end
    if t >= ends then return dist end
    if t > ends - BRAKE_SEC then
        local u = ends - t
        return dist - v * u * u / (2 * BRAKE_SEC)
    end
    if t < RUN_ACCEL then return v * t * t / (2 * RUN_ACCEL) end
    return v * (t - RUN_ACCEL * 0.5)
end

-- climb from (x0, y0) to the door stop x1 (right of x0), t s in. Returns x, y (feet), dy (hop),
-- rot, sx, sy, opacity, whether the mirrored sprite shows, and the step:
-- "run" | "brake" | "turn" | "climb" | "done"
function M.climb(x0, y0, x1, t)
    local dist = max(0, x1 - x0)
    local v = min(RUN_SPEED, 2 * dist / (RUN_ACCEL + BRAKE_SEC))
    local ends = (v > 0) and (dist / v + (RUN_ACCEL + BRAKE_SEC) * 0.5) or 0
    if t < ends then
        local x = x0 + stopDistance(t, dist, v, ends)
        local rot, hop = M.waddle(t)
        local k = min(1, t / RUN_WADDLE_IN)
        local b = (t - (ends - BRAKE_SEC)) / BRAKE_SEC
        if b < 0 then return x, y0, -hop * k, rot * k, 1, 1, 1, false, "run" end
        local e = 1 - b
        e = e * e * (3 - 2 * e)
        return x, y0, -hop * k * e, rot * k * e + BRAKE_LEAN * sin(PI * b), 1, 1, 1, false, "brake"
    end
    t = t - ends
    if t < STOP_HOLD then
        local q = STOP_SQUASH * sin(PI * t / STOP_HOLD)
        return x1, y0, 0, 0, 1 + WIDEN * q, 1 - q, 1, false, "turn"
    end
    t = t - STOP_HOLD
    if t < TURN_SEC then
        local u = t / TURN_SEC
        return x1, y0, -TURN_HOP * sin(PI * u), 0, max(0.02, abs(cos(PI * u))), 1, 1, u >= 0.5, "turn"
    end
    t = t - TURN_SEC
    if t < TURN_BEAT then return x1, y0, 0, 0, 1, 1, 1, true, "turn" end
    t = t - TURN_BEAT
    if t >= CLIMB_SEC then
        local s = 1 - CLIMB_SHRINK
        return x1 + CLIMB_DX, y0 + CLIMB_DY, 0, 0, s, s, 0, true, "done"
    end
    local u = t / CLIMB_SEC
    local i = math.floor(u * CLIMB_STEPS)
    local f = u * CLIMB_STEPS - i
    local p = (i + f * f * (3 - 2 * f)) / CLIMB_STEPS
    local s = 1 - CLIMB_SHRINK * p
    local side = (i % 2 == 0) and 1 or -1
    local fade = Easing.inOutSine((u - CLIMB_FADE_AT) / (1 - CLIMB_FADE_AT))
    return x1 + CLIMB_DX * p, y0 + CLIMB_DY * p, -STEP_HOP * sin(PI * f), side * STEP_TILT * sin(PI * f),
        s, s, 1 - fade, true, "climb"
end

-- knockback from x0, t s after the hit; vx (px/s, leftwards), spin (deg/s).
-- Returns x, dy (lift, up), rot, sx, sy, shadow opacity, pivot.
function M.knock(x0, t, vx, spin)
    if t < KNOCK_HOLD then
        return x0, 0, 0, 1 - KNOCK_SQUASH_X, 1 + KNOCK_SQUASH_Y, 1, "foot"
    end
    t = t - KNOCK_HOLD
    local lift = KNOCK_VY * t - 0.5 * KNOCK_GRAVITY * t * t
    local e = exp(-KNOCK_UNSQUASH * t)
    return x0 - vx * t, -lift, spin * t, 1 - KNOCK_SQUASH_X * e, 1 + KNOCK_SQUASH_Y * e,
        1 - min(1, t / KNOCK_SHADOW_SEC), "center"
end

return M
