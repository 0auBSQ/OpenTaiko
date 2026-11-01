-- Flight after a failed floor; the landing stays on screen for the stage's doors to close over.

local Easing = require("Easing")
local skybox = require("skybox")
local assets = require("encounter/assets")
local sprite = require("encounter/sprite")
local script = require("encounter/script")
local scene  = require("encounter/scene")
local shake  = require("encounter/shake")
local student = require("encounter/student")
local Config  = require("Core.Config")
local Layout  = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")

local M = {}

local Look = Layout.stage.fail_flight
local Time = Animation.stage.fail_flight

local ROOF_LEVEL   = 21            -- Ume is not on the roofs
local LOAD_TIMEOUT = Time.load_timeout_s
local FADE_IN      = Time.fade_in_s
local TALK_AT      = Time.talk_at_s           -- before the lines start
local HOLD_SEC     = Time.hold_s              -- tumble time before the drop when there are no lines
local DROP_MAX     = Time.drop_max_s
local LAND_BEAT    = Time.land_beat_s         -- after they are out of sight, before the stomp
local SCRIM_FADE   = Time.scrim_fade_s
local STOMP_AMP, STOMP_SEC = Time.stomp.amp, Time.stomp.duration_s
local LAND_HOLD    = Time.land_hold_s         -- after the stomp, before the landing is held; outlasts the shake
local RING_FADE    = Time.ring_fade_s         -- fade-out of the stomp's ring over the held landing

-- scroll speeds at full flight speed (px/s, rightwards); all ease to 0 after the drop
local MOUNTAINS = {}
for i, key in ipairs({ "bg_far1", "bg_far2", "bg_far3" }) do
    MOUNTAINS[i] = { key = key, speed = Time.mountain_speeds_px_per_s[i] }
end
local MOUNTAIN_OFFSET = Time.mountain_start_offset_max_px
local YAW_RATE    = Time.yaw_rate_deg_per_s
local CLOUD_DRIFT = Time.cloud_drift
local EASE_SEC    = Time.ease_s

-- clouds: count, centre y range, scale range, opacity, speed range, x stretch and the gap
-- before a cloud comes back (px off the left edge)
local function clouds(look, time)
    return { n = look.n, y = look.y, s = look.scale, a = look.opacity,
             v = time.speed_px_per_s, stretch = look.stretch, gap = look.gap }
end
local CLOUDS_BACK  = clouds(Look.clouds_back, Time.clouds_back)
local CLOUDS_FRONT = clouds(Look.clouds_front, Time.clouds_front)

-- tumble: home offset from screen centre, scale, spin (deg/s, counter-clockwise), phase (keeps
-- the two out of step); the student uses its fail frame (encounter/student.lua)
local function actor(def, look, time)
    def.dx, def.dy, def.scale = look.dx, look.dy, look.scale
    def.spin, def.phase, def.lag = time.spin_deg_per_s, time.phase, time.lag_s
    return def
end
local UME     = actor({ key = "ume_fail" }, Look.ume, Time.ume)
local STUDENT = actor({ student = true }, Look.student, Time.student)
local SOLO    = actor({ student = true }, Look.solo, Time.solo)
local ENTER_DX, ENTER_DY, ENTER_SEC = Time.enter.dx, Time.enter.dy, Time.enter.duration_s
local SWAY_PX, SWAY_HZ = Time.sway.px, Time.sway.hz
local BOB_PX, BOB_HZ = Time.bob.px, Time.bob.hz
local WOBBLE_DEG, WOBBLE_HZ = Time.wobble.deg, Time.wobble.hz
local SQUASH, SQUASH_HZ = Time.squash.amount, Time.squash.hz
local DROP_VX, DROP_AX = Time.drop.vx, Time.drop.ax  -- leftward speed and acceleration after the drop
local DROP_VY, DROP_G = Time.drop.vy, Time.drop.g

local _tex = nil
local _open = false
local _phase = "idle"   -- idle|loading|fly|talk|drop|land|held
local _phaseT, _t = 0, 0
local _dropT = nil
local _keys = nil
local _lines = {}
local _ctx = nil
local _talking = false
local _grey = 1
local _f = { 1, 1, 1 }
local _fade = 1
local _scrimA = 0
local _scrimFull = 1    -- scrim opacity behind the lines (the failed level's, encounter/scene)
local _scrim, _nametag = nil, nil
local _stomp = nil
local _stomped = false
local _ringVol = 100
local _off = { 0, 0, 0 }
local _yaw0, _pan = nil, 0
local _back, _front = {}, {}
local _actors = {}
local _seed = 1
local _shake = shake.new()
local _o = {}

local hooks = {
    onShake = function(amp, dur) _shake:start(amp, dur) end,
}

local function rnd(lo, hi)
    _seed = (_seed * 1103515245 + 12345) % 2147483648
    return lo + (hi - lo) * (_seed / 2147483648)
end

local function loadSfx(path)
    local ok, s = pcall(function() return SOUND:CreateSFX(path) end)
    if ok then return s end
    return nil
end

-- tex: the stage's resident textures (bg_far1..3, cloud); the black cover is encounter/scene's,
-- set up with the same table
function M.setup(tex, fonts)
    _tex = tex
    if fonts ~= nil then script.setup(fonts) end
end

function M.keys(level)
    local keys = { student.frames().fail, "scrim", "ui_nametag" }
    if (level or 0) < ROOF_LEVEL then keys[#keys + 1] = "ume_fail" end
    return keys
end

local function cloudTex()
    return _tex ~= nil and _tex.cloud or nil
end

-- half the drawn width of a cloud at scale s (0 until the texture is ready)
local function cloudHalf(def, s)
    local cl = cloudTex()
    if cl == nil or not cl.Ready then return 0 end
    return cl.Width * s * def.stretch * 0.5
end

-- first: placed on screen; otherwise off the left edge
local function spawn(c, def, first)
    local res = THEME:GetResolution()
    c.s = rnd(def.s[1], def.s[2])
    c.y = rnd(def.y[1], def.y[2])
    c.v = rnd(def.v[1], def.v[2])
    if first then
        c.x = rnd(0, res.X)
    else
        c.x = -cloudHalf(def, c.s) - rnd(def.gap[1], def.gap[2])
    end
end

local function newClouds(def, first)
    local list = {}
    for i = 1, def.n do
        list[i] = {}
        spawn(list[i], def, first)
    end
    return list
end

local function newActor(def)
    local key = def.student and student.frames().fail or def.key
    local _, meta = assets.get(key)
    local w, h = Look.fallback_actor.w, Look.fallback_actor.h
    if meta ~= nil then w, h = meta.w, meta.h end
    return {
        def = def, key = key,
        radius = 0.5 * math.sqrt(w * w + h * h) * def.scale * (1 + SQUASH),
        x = 0, y = 0, rot = 0, sx = def.scale, sy = def.scale,
    }
end

local function setPhase(p)
    _phase, _phaseT = p, 0
end

-- lines: the failed level's flight lines (may be empty); ctx (optional): the floor's
-- encounter ctx, for the `when` guards and <player name>
function M.open(level, highest, lines, ctx)
    M.close()
    _open = true
    level = level or 1
    _lines = lines or {}
    _ctx = ctx
    _grey = skybox.rule(highest)
    local hour = skybox.hourFor(level)
    _f = { skybox.filterFor(hour) }
    skybox.reset()
    skybox.setView(hour)
    _yaw0, _pan = skybox.yaw(), 0

    _keys = M.keys(level)
    assets.open(_keys)
    student.prepare()
    _scrim = assets.get("scrim")
    _scrimFull = scene.scrimAlpha(level)
    _nametag = assets.get("ui_nametag")
    _stomp = loadSfx("Sounds/result/stomp.ogg")

    _seed = level * 7919 + 17
    _off = { rnd(0, MOUNTAIN_OFFSET), rnd(0, MOUNTAIN_OFFSET), rnd(0, MOUNTAIN_OFFSET) }
    _back = newClouds(CLOUDS_BACK, true)
    _front = newClouds(CLOUDS_FRONT, false)
    if level < ROOF_LEVEL then
        _actors = { newActor(UME), newActor(STUDENT) }
    else
        _actors = { newActor(SOLO) }
    end

    _t, _dropT = 0, nil
    _talking, _stomped, _ringVol = false, false, 100
    _fade, _scrimA = 1, 0
    _shake:stop()
    setPhase("loading")
end

-- 1 while flying, eases to 0 after the drop
local function scrollK()
    if _dropT == nil then return 1 end
    return 1 - Easing.outCubic(_dropT / EASE_SEC)
end

local function stepScroll(dt, k)
    for i, L in ipairs(MOUNTAINS) do
        local tx = _tex ~= nil and _tex[L.key] or nil
        _off[i] = _off[i] + L.speed * k * dt
        if tx ~= nil and tx.Ready then _off[i] = _off[i] % tx.Width end
    end
    if _yaw0 ~= nil then
        _pan = _pan + YAW_RATE * k * dt
        skybox.setYaw(_yaw0 + _pan)
    end
    skybox.setSpeed(1 + (CLOUD_DRIFT - 1) * k)
end

local function stepClouds(list, def, dt, k)
    local res = THEME:GetResolution()
    for _, c in ipairs(list) do
        c.x = c.x + c.v * k * dt
        if c.x - cloudHalf(def, c.s) > res.X then spawn(c, def, false) end
    end
end

local function wave(hz, phase, t)
    return math.sin(2 * math.pi * hz * t + phase)
end

local function stepActors()
    local res = THEME:GetResolution()
    local cx, cy = res.X * 0.5, res.Y * 0.5
    for _, a in ipairs(_actors) do
        local d, t = a.def, _t
        local e = Easing.outCubic((t - d.lag) / ENTER_SEC)
        a.x = cx + d.dx + ENTER_DX * (1 - e) + SWAY_PX * wave(SWAY_HZ, d.phase, t)
        a.y = cy + d.dy + ENTER_DY * (1 - e) + BOB_PX * wave(BOB_HZ, d.phase, t)
        if _dropT ~= nil then
            local td = math.max(0, _dropT - d.lag)
            a.x = a.x - DROP_VX * td - 0.5 * DROP_AX * td * td
            a.y = a.y + DROP_VY * td + 0.5 * DROP_G * td * td
        end
        a.rot = d.spin * t + WOBBLE_DEG * wave(WOBBLE_HZ, d.phase, t)
        local q = SQUASH * wave(SQUASH_HZ, d.phase, t)
        a.sx, a.sy = d.scale * (1 + q), d.scale * (1 - q)
    end
end

local function allOut()
    local res = THEME:GetResolution()
    for _, a in ipairs(_actors) do
        if a.y - a.radius < res.Y then return false end
    end
    return true
end

local function startTalk()
    _fade = 0
    setPhase("talk")
    if #_lines > 0 then
        script.play(_lines, hooks, _ctx)
        _talking = true
    end
end

local function startDrop()
    _talking = false
    _dropT = 0
    setPhase("drop")
end

-- nil while running, then "done" once the stomp's shake has settled; the landing stays drawn
-- (and "done" keeps coming) until close()
function M.update(dt, nav)
    if _phase == "idle" then return nil end
    if _phase == "held" then return "done" end
    _phaseT = _phaseT + dt

    if _phase == "loading" then
        if assets.ready(_keys) or _phaseT >= LOAD_TIMEOUT then setPhase("fly") end
        return nil
    end

    _t = _t + dt
    if _dropT ~= nil then _dropT = _dropT + dt end
    _shake:update(dt)
    local k = scrollK()
    stepScroll(dt, k)
    stepClouds(_back, CLOUDS_BACK, dt, k)
    stepClouds(_front, CLOUDS_FRONT, dt, k)
    stepActors()
    local want = _talking and _scrimFull or 0
    if _scrimA < want then _scrimA = math.min(want, _scrimA + dt / SCRIM_FADE)
    elseif _scrimA > want then _scrimA = math.max(want, _scrimA - dt / SCRIM_FADE) end

    if _phase == "fly" then
        _fade = 1 - Easing.outQuad(_t / FADE_IN)
        if _t >= TALK_AT then startTalk() end

    elseif _phase == "talk" then
        if _talking then
            if script.update(dt, true) ~= "running" then startDrop() end
        elseif _phaseT >= HOLD_SEC then
            startDrop()
        end

    elseif _phase == "drop" then
        if allOut() or _phaseT >= DROP_MAX then setPhase("land") end

    elseif _phase == "land" then
        if not _stomped and _phaseT >= LAND_BEAT then
            _stomped = true
            if _stomp ~= nil then _stomp:Play() end
            _shake:start(STOMP_AMP, STOMP_SEC)
        end
        if _phaseT >= LAND_BEAT + LAND_HOLD then
            _shake:stop()
            setPhase("held")
            return "done"
        end
    end
    return nil
end

-- sky for this frame: nil when idle, otherwise grey (0..1) and whether the sky shows
function M.sky()
    if _phase == "idle" then return nil end
    if _phase == "loading" then return 0, false end
    return _grey, true
end

-- a mountain layer, bottom-aligned and tiled; a shake only sinks it so no sky shows under it
local function drawMountain(tx, off, ox, oy)
    if tx == nil or not tx.Ready then return end
    local res = THEME:GetResolution()
    local w = tx.Width
    local y = res.Y - tx.Height + math.max(0, oy)
    local x = math.floor((off + ox) % w + 0.5) - w
    tx:SetColor(_f[1], _f[2], _f[3])
    while x < res.X do
        tx:Draw(x, y)
        x = x + w
    end
    tx:SetColor(1, 1, 1)
end

local function drawClouds(list, def, ox, oy)
    local cl = cloudTex()
    if cl == nil or not cl.Ready then return end
    cl:SetOpacity(def.a)
    cl:SetColor(_f[1], _f[2], _f[3])
    for _, c in ipairs(list) do
        cl:SetScale(c.s * def.stretch, c.s)
        cl:DrawAtAnchor(c.x + ox, c.y + oy, "center")
    end
    cl:SetScale(1, 1)
    cl:SetOpacity(1)
    cl:SetColor(1, 1, 1)
end

local function drawActors(ox, oy)
    _o.alpha, _o.r, _o.g, _o.b, _o.blend = 1, _f[1], _f[2], _f[3], "Normal"
    for _, a in ipairs(_actors) do
        _o.sx, _o.sy, _o.rot = a.sx, a.sy, a.rot
        _o.grad = a.def.student and student.palette() or nil
        sprite.drawAt((assets.get(a.key)), a.x + ox, a.y + oy, _o)
    end
    _o.grad = nil
end

-- the held landing gets no update() under the closing doors, so the ring sound fades and stops
-- here (the stage must not leave on a playing sound)
local function fadeRing()
    if _stomp == nil or _ringVol <= 0 then return end
    _ringVol = math.max(0, _ringVol - fps.deltaTime / RING_FADE * 100)
    _stomp:SetVolumePercent(_ringVol)
    if _ringVol <= 0 then _stomp:Stop() end
end

function M.draw()
    if _phase == "idle" then return end
    if _phase == "loading" then scene.drawBlack(1) ; return end
    if _phase == "held" then fadeRing() end

    local ox, oy = _shake:offset()
    for i, L in ipairs(MOUNTAINS) do
        drawMountain(_tex ~= nil and _tex[L.key] or nil, _off[i], ox, oy)
    end
    drawClouds(_back, CLOUDS_BACK, ox, oy)
    scene.drawScrim(_scrim, _scrimA, ox, oy)
    drawActors(ox, oy)
    drawClouds(_front, CLOUDS_FRONT, ox, oy)
    if _talking then script.draw(_nametag) end
    scene.drawBlack(_fade)
end

function M.close()
    if _stomp ~= nil then _stomp:Dispose() ; _stomp = nil end
    if _open then skybox.reset() end
    _open = false
    _phase = "idle"
    _talking = false
    _actors, _back, _front = {}, {}, {}
    _keys, _lines, _ctx = nil, {}, nil
    _scrim, _nametag = nil, nil
    _dropT, _yaw0 = nil, nil
end

return M
