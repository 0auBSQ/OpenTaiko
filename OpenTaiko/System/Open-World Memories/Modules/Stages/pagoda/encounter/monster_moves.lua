-- Pose curves of the monster's result-screen moves.
-- angles: degrees counter-clockwise (SetRotation); a positive lean tips forward (left)

local Easing = require("Easing")
local Config = require("Core.Config")
local Animation = Config.load("Config/animation.json")

local M = {}

local sin, cos, exp, abs, min, max = math.sin, math.cos, math.exp, math.abs, math.min, math.max
local PI, TAU = math.pi, 2 * math.pi

-- style: "hop" (walkers and the star), "swoop" (floaters) or "dash"
local Charge = Animation.encounter.monster_moves.charge

local Squish = Animation.encounter.monster_moves.squish
M.SQUISH_SEC = Squish.s

-- fromX: feet x it leaves from; edgeX: where its left edge stops, fx = the feet's distance from it;
-- dy0: start height (floaters)
function M.newCharge(fromX, edgeX, fx, sec, style, dy0)
    return {
        fromX = fromX, edgeX = edgeX, fx = fx, toX = edgeX + fx * (1 + Charge.stretch),
        sec = max(Charge.min_s, sec or Charge.default_s), style = style or "dash", dy0 = dy0 or 0,
        t = 0, hit = false,
        pose = { x = fromX, dy = 0, sx = 1, sy = 1, rot = 0, w = 1, pivot = "foot" },
    }
end

-- the charge pose at c.t: x (feet), dy, sx, sy, rot, pivot, and w = how much of the idle shows
function M.chargePose(c)
    local C, p, t = Charge, c.pose, c.t
    local antic = c.sec * C.antic
    local swoop = c.style == "swoop"
    p.pivot = swoop and "center" or "foot"
    if t < antic then
        local k = Easing.outQuad(t / antic)
        p.x = c.fromX + C.pull_px * k
        p.dy = swoop and (c.dy0 - C.rise_px * k) or 0
        p.rot = -C.back_deg * k
        p.sx, p.sy = 1 + C.coil_x * k, 1 - C.coil_y * k
        p.w = 1 - k
    elseif t < c.sec then
        local u = (t - antic) / (c.sec - antic)
        local x0 = c.fromX + C.pull_px
        p.x = x0 + (c.toX - x0) * Easing.inQuad(u)
        local l = Easing.outQuad(min(1, u / C.lean_in))
        p.rot = -C.back_deg + (C.lean_deg + C.back_deg) * l
        p.sx = 1 + C.coil_x + (C.stretch - C.coil_x) * l
        p.sy = 1 - C.coil_y + (C.coil_y - C.stretch_thin * C.stretch) * l
        if swoop then
            p.dy = c.dy0 - C.rise_px + (C.rise_px + C.dive_px) * Easing.outQuad(u)
        elseif c.style == "hop" then
            p.dy = -C.hop_px * abs(sin(PI * C.hops * u))
        else
            p.dy = 0
        end
        p.w = 0
    else
        local r = t - c.sec
        local e = exp(-C.decay * r)
        p.sx, p.sy = 1 - C.squash * e, 1 + C.squash_widen * C.squash * e
        p.x = c.edgeX + c.fx * p.sx + C.recoil_px * Easing.outQuad(r / C.recoil_s)
        p.dy = swoop and (c.dy0 + C.dive_px) or 0
        p.rot = C.lean_deg * e * cos(TAU * C.hz * r)
        p.w = Easing.inOutSine((r - C.settle_s) / C.blend_s)
    end
    return p
end

-- the squish at time t into out: sx, sy (about the feet), dy, alpha
function M.squishPose(t, out)
    out.dy = 0
    local S = Squish
    if t < S.antic_s then
        local k = Easing.outQuad(t / S.antic_s)
        out.sx, out.sy, out.alpha = 1 + S.wide * k, 1 - S.low * k, 1
        return out
    end
    local k = Easing.clamp01((t - S.antic_s) / (M.SQUISH_SEC - S.antic_s))
    out.sx = 1 + S.wide + (S.thin - 1 - S.wide) * Easing.outCubic(k)
    out.sy = 1 - S.low + (S.tall - 1 + S.low) * Easing.outBack(k, S.tall_overshoot)
    out.dy = -S.zip_px * Easing.inQuad(k)
    out.alpha = 1 - Easing.inQuad(k)
    return out
end

return M
