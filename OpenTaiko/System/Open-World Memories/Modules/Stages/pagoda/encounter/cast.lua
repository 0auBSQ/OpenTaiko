-- Ume and the Student on the encounter screen; positions are their feet.

local Easing  = require("Easing")
local assets  = require("encounter/assets")
local sprite  = require("encounter/sprite")
local motion  = require("encounter/motion")
local moves   = require("encounter/cast_moves")
local student = require("encounter/student")
local Emitter = require("fx/emitter")
local presets = require("fx/presets")
local Config  = require("Core.Config")
local Layout    = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")

local M = {}

local Look = Layout.encounter.cast
local Anim = Animation.encounter.cast

-- authored line sprites -> manifest keys
local SPRITE_MAP = {
    normal = "ume_normal", scared = "ume_normal",
    brave = "ume_brave", clear = "ume_clear", run = "ume_run",
    lost = "ume_fail", fail = "ume_fail",
}
local DEFAULT_SPRITE = "ume_brave"
local UME_KEYS = { "ume_normal", "ume_brave", "ume_clear", "ume_run", "ume_run_m", "ume_fail" }
-- frame -> scale that matches it to the rest of the cast
local ART_SCALE = Config.plain(Look.art_scale)
local ROOF_LEVEL = 21           -- first level without Ume

local START_X, EXIT_X = Look.start_x, Look.exit_x
local RUN_SEC, SKID_SEC = Anim.run_s, Anim.skid_s
local RUN_SETTLE = Anim.run_settle      -- share of the run's end over which the waddle settles
local SKID_LEAN = Anim.skid_lean_deg    -- degrees, back (left) while braking
local Land = Anim.land
local LAND_Y, LAND_X, LAND_DECAY, LAND_HZ = Land.squash_y, Land.squash_x, Land.decay, Land.hz
local LAND_HOP_PX, LAND_HOP_SEC, LAND_SEC = Land.hop_px, Land.hop_s, Land.s
local ARRIVE_HOLD = Anim.arrive_hold_s  -- seconds after the last landing, before the dialog
local IDLE_P, IDLE_A, IDLE_RAMP = Anim.idle.period_s, Anim.idle.amp, Anim.idle.ramp_s
local EXIT_SEC = Anim.exit_s
local DUST_COUNT = Anim.dust_count
local DUST_R, DUST_G, DUST_B = Look.dust_color[1], Look.dust_color[2], Look.dust_color[3]
local Shadow = Look.shadow
local SHADOW_ALPHA, SHADOW_W, LIFT_FULL = Shadow.opacity, Shadow.width_share, Shadow.lift_full_px
local SCREEN_W, SCREEN_H = 1920, 1080

-- marks = feet, the overshoot and start offsets; shadow = the key sizing the contact shadow (shadowPx: its width);
-- knock = the knockback sprite, knockVX px/s, knockSpin deg/s. The Student's keys are filled in by open.
local ACTORS = {
    { ume = true, run = "ume_run", out = "ume_run_m", knock = "ume_fail", shadow = "ume_brave", seed = 2 },
    { ume = false, run = "run", out = "run_m", knock = "fail", shadow = "normal", seed = 3,
      shadowPx = Look.student.shadow_px },
}
for _, def in ipairs(ACTORS) do
    local p, m = def.ume and Look.ume or Look.student, def.ume and Anim.ume or Anim.student
    def.x, def.over, def.y = p.x, p.over_x, p.y
    def.delay, def.exitDelay, def.lag = m.delay_s, m.exit_delay_s, m.lag_s
    def.runDelay, def.knockDelay, def.knockVX, def.knockSpin = m.run_delay_s, m.knock_delay_s, m.knock_px_per_s, m.knock_spin_deg_per_s
end

local _open = false
local _t = 0
local _entryT, _exitT = nil, nil
local _move, _moveT, _runDir = nil, nil, nil   -- "run" | "climb" | "knock" once started
local _cheerT = nil
local _umeKey = DEFAULT_SPRITE
local _umeLanded = false
local _hasUme = true
local _st = student.frames("a")
local _actors = {}
local _shadow, _shadowMeta = nil, nil
local _dust = nil
local _o = {}

local function umeOn(level)
    return (level or 0) < ROOF_LEVEL
end

-- false from ROOF_LEVEL on
M.umeOn = umeOn

local function dustKey()
    local k = presets.dust_puff and presets.dust_puff.tex or nil
    if type(k) == "table" then k = k[1] end
    return k or "fx_dust"
end

function M.keys(level)
    local keys = {}
    if umeOn(level) then
        for _, k in ipairs(UME_KEYS) do keys[#keys + 1] = k end
    end
    for _, k in ipairs(student.keys()) do keys[#keys + 1] = k end
    keys[#keys + 1] = "fx_shadow"
    keys[#keys + 1] = dustKey()
    return keys
end

-- seconds from the start of the run-in until everyone is settled
local function settledAt()
    local last = 0
    for _, def in ipairs(ACTORS) do last = math.max(last, def.delay) end
    return last + RUN_SEC + SKID_SEC + math.max(LAND_SEC, IDLE_RAMP)
end

local pose

-- the Student's def with frame names mapped to the chosen student's keys
local function studentDef(def)
    local d = {}
    for k, v in pairs(def) do d[k] = v end
    d.run, d.out, d.knock, d.shadow = _st[def.run], _st[def.out], _st[def.knock], _st[def.shadow]
    return d
end

-- options = { placed = true: start on the marks, idling; sprite = Ume's authored sprite }
function M.open(level, options)
    M.close()
    _open = true
    _t, _entryT, _exitT = 0, nil, nil
    _move, _moveT, _runDir, _cheerT = nil, nil, nil, nil
    _umeKey, _umeLanded = DEFAULT_SPRITE, false
    _hasUme = umeOn(level)
    _st = student.frames()
    student.prepare()
    _shadow, _shadowMeta = assets.get("fx_shadow")
    _dust = Emitter.new(presets.dust_puff, (assets.get(dustKey())))
    _actors = {}
    for _, def in ipairs(ACTORS) do
        if not def.ume then def = studentDef(def) end
        if _hasUme or not def.ume then
            _actors[#_actors + 1] = {
                def = def, visible = false, gone = false, key = def.run,
                x = START_X, y = def.y, gy = def.y, dy = 0, sx = 1, sy = 1, rot = 0, pivot = "foot",
                alpha = 1, shadowA = 1,
                idle = motion.new("counter", { P = IDLE_P, A = IDLE_A, phase = -def.lag }, (level or 0) * 10 + def.seed),
                dusted = false, landed = false, kicked = false,
            }
        end
    end
    if options ~= nil then
        M.setSprite(options.sprite)
        if options.placed then
            _entryT = -settledAt()
            for _, a in ipairs(_actors) do a.dusted, a.landed = true, true end
        end
    end
    for _, a in ipairs(_actors) do pose(a, motion.step(a.idle, 0)) end
end

function M.hasUme()
    return _hasUme
end

function M.startEntrance()
    _entryT, _exitT = _t, nil
    _umeLanded = false
    for _, a in ipairs(_actors) do a.dusted, a.landed = false, false end
end

function M.startExit()
    if _exitT == nil then _exitT = _t end
end

function M.cheer()
    if not _hasUme then return end
    _umeKey = SPRITE_MAP.clear
    _cheerT = _t
end

-- dir = "right" | "diag" (stairs) | "up" (roof)
function M.startRun(dir)
    if _move ~= nil then return end
    _move, _moveT, _runDir = "run", _t, dir or "right"
end

-- doorX in 1920 space
function M.startClimb(doorX)
    if _move ~= nil then return end
    _move, _moveT = "climb", _t
    local mid = 0
    for _, a in ipairs(_actors) do mid = mid + a.def.x / #_actors end
    for _, a in ipairs(_actors) do
        a.stopX, a.braked = doorX + a.def.x - mid, false
    end
end

function M.knockback()
    if _move ~= nil then return end
    _move, _moveT = "knock", _t
end

-- true once both have landed and settled
function M.arrived()
    if _entryT == nil then return false end
    local last = 0
    for _, def in ipairs(ACTORS) do last = math.max(last, def.delay) end
    return _t - _entryT >= last + RUN_SEC + SKID_SEC + ARRIVE_HOLD
end

-- true once, after Ume lands
function M.umeLanded()
    if _umeLanded then
        _umeLanded = false
        return true
    end
    return false
end

local function allGone()
    for _, a in ipairs(_actors) do
        if not a.gone then return false end
    end
    return true
end

-- true once startRun, startClimb or startExit has taken everyone off
function M.exited()
    if _move == "run" or _move == "climb" then return allGone() end
    if _exitT == nil then return false end
    local last = 0
    for _, def in ipairs(ACTORS) do last = math.max(last, def.exitDelay) end
    return _t - _exitT >= last + EXIT_SEC
end

-- true once the knockback has taken everyone past the left edge
function M.flownOut()
    return _move == "knock" and allGone()
end

-- authored names: normal, scared, brave, ...
function M.setSprite(authored)
    if authored == nil then return end
    _umeKey = SPRITE_MAP[authored] or "ume_normal"
end

local function standKey(def)
    if def.ume then return _umeKey end
    return _st.forUme[_umeKey] or _st.normal
end

local function opts(sx, sy, rot, pivot, alpha, r, g, b, grad)
    _o.sx, _o.sy, _o.rot, _o.pivot = sx, sy, rot, pivot
    _o.alpha, _o.r, _o.g, _o.b, _o.blend, _o.grad = alpha, r, g, b, "Normal", grad
    return _o
end

-- screen point where a charging monster meets the front actor (right side, mid-body)
function M.hitPoint()
    local front = nil
    for _, a in ipairs(_actors) do
        if front == nil or a.def.x > front.def.x then front = a end
    end
    local hit = Look.hit_fallback
    if front == nil then return ACTORS[#ACTORS].x + hit.dx, ACTORS[#ACTORS].y + hit.dy end
    local _, meta = assets.get(front.key)
    if meta == nil then return front.x + hit.dx, front.y + front.dy + hit.dy end
    local fx, fy = meta.fx or meta.w * 0.5, meta.fy or meta.h
    return front.x + (meta.w - fx) * front.sx, front.y + front.dy + (meta.h * 0.5 - fy) * front.sy
end

-- top of Ume's sprite on screen (centre x); nil while not drawn
function M.umeTop()
    for _, a in ipairs(_actors) do
        if a.def.ume and a.visible then
            local _, meta = assets.get(a.key)
            if meta == nil then return nil end
            local k = ART_SCALE[a.key] or 1
            local cx, cy = sprite.center(meta, a.x, a.y + a.dy, opts(a.sx * k, a.sy * k, a.rot, a.pivot))
            return cx, cy - meta.h * a.sy * k * 0.5
        end
    end
    return nil
end

local function offscreen(a)
    local _, meta = assets.get(a.key)
    if meta == nil then meta = assets.meta(a.def.shadow) end
    local off = Look.offscreen_fallback
    if meta == nil then return a.x < -off.margin_x or a.x > SCREEN_W + off.margin_x or a.y < -off.margin_top end
    local k = ART_SCALE[a.key] or 1
    local cx, cy = sprite.center(meta, a.x, a.y + a.dy, opts(a.sx * k, a.sy * k, a.rot, a.pivot))
    local r = 0.5 * math.sqrt((meta.w * a.sx * k) ^ 2 + (meta.h * a.sy * k) ^ 2)
    return cx + r < 0 or cx - r > SCREEN_W or cy + r < 0 or cy - r > SCREEN_H
end

-- dust at the feet as a move starts
local function kick(a)
    if a.kicked then return end
    a.kicked = true
    if _dust ~= nil then _dust:burst(DUST_COUNT, a.def.x, a.def.y) end
end

local function climbPose(a, tc)
    local def = a.def
    local mirrored, step
    a.x, a.y, a.dy, a.rot, a.sx, a.sy, a.alpha, mirrored, step = moves.climb(def.x, def.y, a.stopX, tc)
    a.key = mirrored and def.out or def.run
    a.gy, a.shadowA = a.y, a.alpha
    if step == "brake" and not a.braked then
        a.braked = true
        if _dust ~= nil then _dust:burst(DUST_COUNT, a.x, a.y) end
    end
    if step == "done" then a.gone, a.visible = true, false end
end

-- false while this actor has not started its move
local function movePose(a)
    local def = a.def
    if _move == "climb" then
        local tc = _t - _moveT - def.runDelay
        if tc < 0 then return false end
        kick(a)
        climbPose(a, tc)
        return true
    elseif _move == "run" then
        local tr = _t - _moveT - def.runDelay
        if tr < 0 then return false end
        kick(a)
        local s
        a.key = def.run
        a.x, a.y, a.dy, a.rot, s, a.gy, a.shadowA = moves.run(def.x, def.y, tr, _runDir)
        a.sx, a.sy = s, s
    else
        local tk = _t - _moveT - def.knockDelay
        if tk < 0 then return false end
        kick(a)
        a.key = def.knock
        a.x, a.dy, a.rot, a.sx, a.sy, a.shadowA, a.pivot = moves.knock(def.x, tk, def.knockVX, def.knockSpin)
    end
    if offscreen(a) then
        a.gone, a.visible = true, false
    end
    return true
end

-- tl = seconds since landing
local function stand(a, idle, tl)
    local def = a.def
    a.key = standKey(def)
    a.x = def.x
    if tl < LAND_SEC then
        local s = math.exp(-LAND_DECAY * tl) * math.cos(2 * math.pi * LAND_HZ * tl)
        a.sy, a.sx = 1 - LAND_Y * s, 1 + LAND_X * s
    end
    if tl < LAND_HOP_SEC then a.dy = -LAND_HOP_PX * math.sin(math.pi * tl / LAND_HOP_SEC) end
    local k = math.min(1, tl / IDLE_RAMP)
    if def.ume and _cheerT ~= nil then
        local dy, sx, sy, over = moves.cheer(_t - _cheerT)
        a.dy, a.sx, a.sy = a.dy + dy, a.sx * sx, a.sy * sy
        k = k * (1 - over)
    end
    a.sx = a.sx * (1 + (idle.sx - 1) * k)
    a.sy = a.sy * (1 + (idle.sy - 1) * k)
end

pose = function(a, idle)
    local def = a.def
    a.y, a.gy, a.dy, a.sx, a.sy, a.rot, a.pivot, a.alpha, a.shadowA = def.y, def.y, 0, 1, 1, 0, "foot", 1, 1
    if _entryT == nil or a.gone then
        a.visible = false
        return
    end
    a.visible = true
    if _move ~= nil and movePose(a) then return end
    local te = _t - _entryT - def.delay
    local tx = (_exitT ~= nil) and (_t - _exitT - def.exitDelay) or -1
    if tx >= 0 then
        if tx >= EXIT_SEC then
            a.visible = false
            return
        end
        a.key = def.out
        a.x = def.x - (def.x - EXIT_X) * Easing.inQuad(tx / EXIT_SEC)
        local rot, hop = moves.waddle(tx)
        a.rot, a.dy = rot, -hop
    elseif te < 0 then
        a.visible = false
    elseif te < RUN_SEC then
        local k = te / RUN_SEC
        a.key = def.run
        a.x = START_X + (def.over - START_X) * Easing.outQuad(k)
        local e = math.min(1, (1 - k) / RUN_SETTLE)
        e = e * e * (3 - 2 * e)
        local rot, hop = moves.waddle(te)
        a.rot, a.dy = rot * e, -hop * e
    elseif te < RUN_SEC + SKID_SEC then
        local k = (te - RUN_SEC) / SKID_SEC
        if not a.dusted then
            a.dusted = true
            if _dust ~= nil then _dust:burst(DUST_COUNT, def.over, def.y) end
        end
        a.key = def.run
        a.x = def.over + (def.x - def.over) * Easing.inOutSine(k)
        a.rot = SKID_LEAN * math.sin(math.pi * k)
    else
        local tl = te - RUN_SEC - SKID_SEC
        if not a.landed then
            a.landed = true
            if def.ume then _umeLanded = true end
        end
        stand(a, idle, tl)
    end
end

function M.update(dt)
    if not _open then return end
    _t = _t + dt
    for _, a in ipairs(_actors) do pose(a, motion.step(a.idle, dt)) end
    if _dust ~= nil then _dust:update(dt) end
end

-- ox, oy: the screen shake
function M.drawShadows(alpha, ox, oy)
    if not _open or _shadow == nil or _shadowMeta == nil then return end
    alpha, ox, oy = alpha or 1, ox or 0, oy or 0
    if alpha <= 0 then return end
    for _, a in ipairs(_actors) do
        if a.visible and a.shadowA > 0 then
            local w = a.def.shadowPx
            if w == nil then
                local _, meta = assets.get(a.def.shadow)
                w = SHADOW_W * ((meta ~= nil and meta.w) or Shadow.fallback_w)
            end
            local base = w / math.max(1, _shadowMeta.w)
            local l = math.min(1, math.max(0, -a.dy) / LIFT_FULL)
            local s = base * (1 - Shadow.lift_shrink * l)
            sprite.drawAt(_shadow, a.x + ox, a.gy + oy, opts(s, s, 0, nil, SHADOW_ALPHA * (1 - Shadow.lift_fade * l) * a.shadowA * alpha, 1, 1, 1))
        end
    end
end

-- alpha: the band's cast opacity; r, g, b: its filter; ox, oy: the screen shake
function M.draw(alpha, r, g, b, ox, oy)
    if not _open then return end
    alpha, r, g, b, ox, oy = alpha or 1, r or 1, g or 1, b or 1, ox or 0, oy or 0
    if alpha <= 0 then return end
    for _, a in ipairs(_actors) do
        if a.visible and a.alpha > 0 then
            local tx, meta = assets.get(a.key)
            if meta ~= nil then
                local grad = (not a.def.ume) and student.palette() or nil
                local k = ART_SCALE[a.key] or 1
                sprite.draw(tx, meta, a.x + ox, a.y + a.dy + oy, opts(a.sx * k, a.sy * k, a.rot, a.pivot, alpha * a.alpha, r, g, b, grad))
            end
        end
    end
end

-- alpha: the band's cast opacity; ox, oy: the screen shake; r, g, b: the band's ground dust colour
function M.drawDust(alpha, ox, oy, r, g, b)
    if _dust == nil then return end
    _dust:draw(ox or 0, oy or 0, alpha or 1, r or DUST_R, g or DUST_G, b or DUST_B)
end

function M.close()
    if _dust ~= nil then _dust:clear() end
    _open = false
    _entryT, _exitT = nil, nil
    _move, _moveT, _runDir, _cheerT = nil, nil, nil, nil
    _actors = {}
    _shadow, _shadowMeta = nil, nil
    _dust = nil
end

return M
