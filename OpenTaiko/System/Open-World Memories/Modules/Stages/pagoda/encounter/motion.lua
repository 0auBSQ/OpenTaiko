-- Idle animations for the encounter actors, one clock per instance.
-- step() returns the pose for this frame:
--   dx, dy    offset from the actor's foot point (px, y down)
--   sx, sy    scale, always positive
--   rot       degrees, counter-clockwise like SetRotation
--   pivot     "foot" or "center" (encounter/sprite.lua)
--   mirrored  true while the actor faces right (the art faces left)
--   jump      true while a hop has it in the air (wander)
--   lift      height of the feet above the ground (px), for the contact shadow
--
-- Kinds and their params:
--   star     (none)
--   wander   { range }
--   counter  { P = period, A = amplitude, phase }
--   float    { hover, tilt, mul }
--   erratic  { float = true: on top of the float idle }

local Easing = require("Easing")
local Config = require("Core.Config")
local Animation = Config.load("Config/animation.json")

local M = {}

local sin, cos, abs, floor, min, max = math.sin, math.cos, math.abs, math.floor, math.min, math.max
local PI, TAU = math.pi, 2 * math.pi

local Anim = Animation.encounter.motion

local SCALE_MIN, SCALE_MAX = Anim.scale_min, Anim.scale_max
local NOTICE_SEC, NOTICE_POP = Anim.notice_s, Anim.notice_pop

local St = Anim.star
local STAR = {
    period = St.period_s, height = St.height_px, contact = St.contact, squashY = St.squash_y, squashX = St.squash_x,
    tilt = St.tilt_deg, spinMin = St.spin_min_s, spinMax = St.spin_max_s,
}

local Wa = Anim.wander
local WANDER = {
    restMin = Wa.rest_min_s, restMax = Wa.rest_max_s,
    range = Wa.range_px, minStep = Wa.min_step_px, maxStep = Wa.max_step_px, speed = Wa.speed_px_per_s,
    bobPx = Wa.bob_px, bobHz = Wa.bob_hz, rock = Wa.rock_deg, ease = Wa.ease_s,
    hopChance = Wa.hop_chance, anticSec = Wa.antic_s, airSec = Wa.air_s, landSec = Wa.land_s, hopPx = Wa.hop_px, hopDx = Wa.hop_dx,
    flipFrom = Wa.flip_from, flipSec = Wa.flip_s,
    turnChance = Wa.turn_chance, turnDelay = Wa.turn_delay_s,
    anticSquashY = Wa.antic_squash_y, anticSquashX = Wa.antic_squash_x,
    airStretchY = Wa.air_stretch_y, airStretchX = Wa.air_stretch_x,
    landSquashY = Wa.land_squash_y, landSquashX = Wa.land_squash_x,
}

local Co = Anim.counter
local COUNTER = { period = Co.period_s, amp = Co.amp, accent = Co.accent, widen = Co.widen }

local Fl = Anim.float
local FLOAT = { hover = Fl.hover_px, bob = Fl.bob_px, bobSec = Fl.bob_s, drift = Fl.drift_px, driftSec = Fl.drift_s,
                tilt = Fl.tilt_deg, tiltSec = Fl.tilt_s, tiltPhase = Fl.tilt_phase_rad }

-- deterministic number in [0, 1) for the n-th random timing of an instance
local function hash(seed, n)
    local v = sin(seed * 127.1 + n * 311.7) * 43758.5453
    return v - floor(v)
end

local function nextRandom(m)
    m.n = m.n + 1
    return hash(m.seed, m.n)
end

-- star
local function starInit(m)
    m.bounce = -1
    m.spinning = false
    m.spinSign = -1
    m.nextSpin = STAR.spinMin + (STAR.spinMax - STAR.spinMin) * nextRandom(m)
end

local function star(m, pose)
    local P = STAR.period
    local n = floor(m.t / P)
    local u = m.t / P - n
    if n ~= m.bounce then
        m.bounce = n
        m.spinning = false
        if m.t >= m.nextSpin and not m.calm then
            m.spinning = true
            m.spinSign = -m.spinSign
            m.nextSpin = m.t + STAR.spinMin + (STAR.spinMax - STAR.spinMin) * nextRandom(m)
        end
    end
    local h = STAR.height * 4 * u * (1 - u)
    local c = 0
    if u < STAR.contact then c = 1 - u / STAR.contact
    elseif u > 1 - STAR.contact then c = (u - (1 - STAR.contact)) / STAR.contact end
    pose.dy, pose.lift = -h, h
    pose.sy = 1 - STAR.squashY * c
    pose.sx = 1 + STAR.squashX * c
    if m.spinning then
        pose.rot = m.spinSign * 360 * Easing.inOutCubic(u)
        pose.pivot = "center"
    else
        pose.rot = STAR.tilt * sin(PI * m.t / P)
    end
end

-- wander
local function face(m, right)
    if m.mirrored ~= right then
        m.mirrored = right
        m.flipT = 0
    end
end

local function startRest(m, afterMove)
    m.state, m.st = "rest", 0
    m.restSec = WANDER.restMin + (WANDER.restMax - WANDER.restMin) * nextRandom(m)
    m.turnAt = nil
    if afterMove and nextRandom(m) < WANDER.turnChance then m.turnAt = WANDER.turnDelay end
end

local function startMove(m)
    local W = WANDER
    local range = m.params.range or W.range
    local dir = (nextRandom(m) < 0.5) and -1 or 1
    if nextRandom(m) < W.hopChance then
        if abs(m.x + dir * W.hopDx) > range then dir = -dir end
        m.state, m.from, m.to = "antic", m.x, m.x + dir * W.hopDx
    else
        local dist = W.minStep + (W.maxStep - W.minStep) * nextRandom(m)
        local to = max(-range, min(range, m.x + dir * dist))
        if abs(to - m.x) < W.minStep then
            dir = -dir
            to = max(-range, min(range, m.x + dir * dist))
        end
        m.state, m.from, m.to = "walk", m.x, to
        m.walkSec = abs(to - m.x) / W.speed
    end
    m.st = 0
    face(m, dir > 0)
end

local function wanderInit(m)
    m.x = 0
    m.mirrored = false
    m.flipT = WANDER.flipSec
    startRest(m, false)
end

local function wander(m, pose, dt)
    local W = WANDER
    m.st = m.st + dt
    m.flipT = m.flipT + dt
    if m.state == "rest" then
        if m.turnAt ~= nil and m.st >= m.turnAt then
            face(m, false)
            m.turnAt = nil
        end
        if m.st >= m.restSec then startMove(m) end
    elseif m.state == "walk" then
        if m.st >= m.walkSec then
            m.x = m.to
            startRest(m, true)
        else
            m.x = m.from + (m.to - m.from) * (m.st / m.walkSec)
            local env = min(1, m.st / W.ease, (m.walkSec - m.st) / W.ease)
            local step = sin(PI * W.bobHz * m.st)
            pose.dy = -W.bobPx * abs(step) * env
            pose.rot = W.rock * step * env
        end
    elseif m.state == "antic" then
        if m.st >= W.anticSec then
            m.state, m.st = "air", m.st - W.anticSec
        else
            local k = Easing.inOutSine(m.st / W.anticSec)
            pose.sy, pose.sx = 1 - W.anticSquashY * k, 1 + W.anticSquashX * k
        end
    end
    if m.state == "air" then
        if m.st >= W.airSec then
            m.x = m.to
            m.state, m.st = "land", m.st - W.airSec
        else
            local q = m.st / W.airSec
            m.x = m.from + (m.to - m.from) * q
            pose.dy = -W.hopPx * 4 * q * (1 - q)
            local s = (1 - q) * (1 - q)
            pose.sy, pose.sx = 1 + W.airStretchY * s, 1 - W.airStretchX * s
        end
    end
    if m.state == "land" then
        if m.st >= W.landSec then
            startRest(m, true)
        else
            local c = 1 - m.st / W.landSec
            c = c * c
            pose.sy, pose.sx = 1 - W.landSquashY * c, 1 + W.landSquashX * c
        end
    end
    if m.flipT < W.flipSec then
        pose.sx = pose.sx * (W.flipFrom + (1 - W.flipFrom) * Easing.outBack(m.flipT / W.flipSec))
    end
    pose.dx = m.x
    pose.lift = -pose.dy
    pose.mirrored = m.mirrored
    pose.jump = m.state == "air"
end

-- counter
local function counter(m, pose)
    local p = m.params
    local P, A = p.P or COUNTER.period, p.A or COUNTER.amp
    local u = ((m.t + (p.phase or 0)) % P) / P
    local d, ac = nil, COUNTER.accent
    if u < ac then d = Easing.outQuad(u / ac)
    else d = 1 - Easing.inOutSine((u - ac) / (1 - ac)) end
    pose.sy = 1 - A * d
    pose.sx = 1 + COUNTER.widen * A * d
end

-- float
local function float(m, pose)
    local p, F, t = m.params, FLOAT, m.t
    local k = p.mul or 1
    local h = ((p.hover or F.hover) + F.bob * (0.5 - 0.5 * cos(TAU * t / F.bobSec))) * k
    pose.dy, pose.lift = -h, h
    pose.dx = F.drift * sin(TAU * t / F.driftSec) * k
    pose.rot = (p.tilt or F.tilt) * sin(TAU * t / F.tiltSec + F.tiltPhase) * k
    pose.pivot = "center"
end

-- erratic
local Er = Anim.erratic
local ERRATIC = {
    gap = Er.gap_s,
    tremor = Er.tremor_deg, tremorHold = Er.tremor_hold_s,
    twitch = { rot = Er.twitch.deg, sec = Er.twitch.s },
    jitter = { sec = Er.jitter.s, hz = Er.jitter.hz, px = Er.jitter.px, rot = Er.jitter.deg },
    skip = { sec = Er.skip.s, dx = Er.skip.dx, dy = Er.skip.dy },
    stretch = { sec = Er.stretch.s, amt = Er.stretch.amount },
    freeze = { sec = Er.freeze.s },
    kinds = {},
}
for i, name in ipairs({ "twitch", "jitter", "skip", "stretch", "freeze" }) do ERRATIC.kinds[i] = { name, Er.shares[i] } end

local function between(m, r) return r[1] + (r[2] - r[1]) * nextRandom(m) end
local function sign(m) return (nextRandom(m) < 0.5) and -1 or 1 end

local function startGlitch(m, g, t)
    local E = ERRATIC
    local r, kind = nextRandom(m), E.kinds[#E.kinds][1]
    for _, k in ipairs(E.kinds) do
        if r < k[2] then kind = k[1] break end
        r = r - k[2]
    end
    g.kind, g.t0, g.held = kind, t, nil
    if kind == "twitch" then
        g.t1, g.a = t + between(m, E.twitch.sec), sign(m) * between(m, E.twitch.rot)
    elseif kind == "jitter" then
        g.t1, g.base = t + between(m, E.jitter.sec), floor(nextRandom(m) * 100000)
    elseif kind == "skip" then
        g.t1, g.a, g.b = t + between(m, E.skip.sec), sign(m) * between(m, E.skip.dx), sign(m) * between(m, E.skip.dy)
    elseif kind == "stretch" then
        g.t1, g.a = t + between(m, E.stretch.sec), sign(m) * between(m, E.stretch.amt)
    else
        g.t1 = t + between(m, E.freeze.sec)
    end
end

local function erratic(m, pose)
    local E, t = ERRATIC, m.t
    if m.params.float then float(m, pose) end
    local g = m.glitch
    if g == nil then
        g = { next = between(m, E.gap), tremorT = 0, tremor = 0 }
        m.glitch = g
    end
    if t >= g.tremorT then
        g.tremor = (nextRandom(m) * 2 - 1) * E.tremor
        g.tremorT = t + between(m, E.tremorHold)
    end
    pose.rot = pose.rot + g.tremor
    if g.kind == nil and t >= g.next then startGlitch(m, g, t) end
    local kind = g.kind
    if kind == "twitch" then
        pose.rot = pose.rot + g.a
    elseif kind == "jitter" then
        local n = g.base + floor((t - g.t0) * E.jitter.hz) * 3
        pose.dx = pose.dx + (hash(m.seed, n) * 2 - 1) * E.jitter.px
        pose.dy = pose.dy + (hash(m.seed, n + 1) * 2 - 1) * E.jitter.px
        pose.rot = pose.rot + (hash(m.seed, n + 2) * 2 - 1) * E.jitter.rot
    elseif kind == "skip" then
        pose.dx, pose.dy = pose.dx + g.a, pose.dy + g.b
    elseif kind == "stretch" then
        pose.sx, pose.sy = pose.sx * (1 + g.a), pose.sy * (1 - g.a)
    elseif kind == "freeze" then
        local h = g.held
        if h == nil then
            h = { dx = pose.dx, dy = pose.dy, rot = pose.rot, sx = pose.sx, sy = pose.sy }
            g.held = h
        end
        pose.dx, pose.dy, pose.rot, pose.sx, pose.sy = h.dx, h.dy, h.rot, h.sx, h.sy
    end
    if kind ~= nil and t >= g.t1 then
        g.kind, g.held = nil, nil
        g.next = t + between(m, E.gap)
    end
    pose.lift = max(0, -pose.dy)
    pose.pivot = "center"
end

-- public
function M.new(kind, params, seed)
    local m = {
        kind = kind, params = params or {}, seed = seed or 0, n = 0, t = 0,
        mirrored = false, noticeT = nil,
        pose = { dx = 0, dy = 0, sx = 1, sy = 1, rot = 0, pivot = "foot", mirrored = false, jump = false, lift = 0 },
    }
    if kind == "star" then starInit(m)
    elseif kind == "wander" then wanderInit(m) end
    return m
end

function M.step(m, dt)
    dt = max(0, dt or 0)
    m.t = m.t + dt
    local pose = m.pose
    pose.dx, pose.dy, pose.sx, pose.sy, pose.rot = 0, 0, 1, 1, 0
    pose.pivot, pose.mirrored, pose.jump, pose.lift = "foot", false, false, 0
    local kind = m.kind
    if kind == "star" then star(m, pose)
    elseif kind == "wander" then wander(m, pose, dt)
    elseif kind == "counter" then counter(m, pose)
    elseif kind == "float" then float(m, pose)
    elseif kind == "erratic" then erratic(m, pose)
    end
    if m.noticeT ~= nil then
        m.noticeT = m.noticeT + dt
        if m.noticeT >= NOTICE_SEC then
            m.noticeT = nil
        else
            local k = 1 + NOTICE_POP * Easing.hump(m.noticeT / NOTICE_SEC)
            pose.sx, pose.sy = pose.sx * k, pose.sy * k
        end
    end
    pose.sx = max(SCALE_MIN, min(SCALE_MAX, pose.sx))
    pose.sy = max(SCALE_MIN, min(SCALE_MAX, pose.sy))
    return pose
end

-- returns the cycle length and the time since its accent (star landing, counter's deepest squash,
-- float's lowest point), in seconds; nil for the kinds without one
function M.beat(m)
    local p, P, at = m.params, nil, 0
    if m.kind == "star" then
        P = STAR.period
    elseif m.kind == "counter" then
        P = p.P or COUNTER.period
        at = COUNTER.accent * P - (p.phase or 0)
    elseif m.kind == "float" or (m.kind == "erratic" and p.float) then
        P = FLOAT.bobSec
    else
        return nil
    end
    return P, (m.t - at) % P
end

function M.notice(m)
    m.noticeT = 0
end

-- no new star spins from now on (one under way finishes)
function M.calm(m)
    m.calm = true
end

return M
