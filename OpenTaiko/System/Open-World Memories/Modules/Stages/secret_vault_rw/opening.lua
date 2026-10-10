---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local, inject-field
local Tex = require("tex")
local Easing = require("Easing")
local Cabin = require("cabin")

local O = {}

local DESK_Y = Cabin.DESK_Y
local CHEST_SCALE = 0.75
local CHEST_H, HOLE_Y = 300, 171

-- the key: its scale once home and when its tip touches the keyhole; where it starts (px off the keyhole at the
-- keyhole's depth), how close (depth, 1 = the keyhole), turned and seen from the side (1 = end-on)
local KEY_K = 0.42
local TOUCH_K = 1.12
local START_X, START_Y, Z0, START_ROT, YAW = 70, 120, 0.22, -28, 1.8
local TURNED, WIND_DEG, OVER = -90, 9, 11
local SHARDS_X, SHARDS_Y = 3, 6                  -- a broken key falls apart in this many pieces

-- seconds from the start
local DROP_AT, FALL = 0.30, 0.48
local LAND_AT = DROP_AT + FALL
local KEY_AT, FLY, PUSH = 1.30, 0.55, 0.20
local TOUCH = KEY_AT + FLY
local SEATED = TOUCH + PUSH
local TURN_AT, WIND, TURN, SETTLE = 2.20, 0.12, 0.42, 0.22
local KEYIN_CLICK = 0.43                         -- KeyIn.ogg's click, played to land as the key gets home
local TURN_CLUNK = 0.39                          -- KeyTurn.ogg's bolt, played to land on the turn's furthest point
local AFTER = 3.00
local SHATTER_AT = AFTER + 0.22
local OPEN_AT = AFTER + 0.15
local POP, LEAVE = 0.6, 0.5
local WON_Y, BANNER_Y = 300, 104

-- the punch, in seconds from the hit; the flight is in the room's space (cabin.lua): the chest stands on the desk at
-- depth CHEST_Z and lands on the floor at (LAND_X, LAND_Z), ARC px over its straight path at the top; past BEHIND_Z
-- it is drawn behind Mr.Caspian
local HIT_X, HIT_Y = 960, 640
local HITSTOP, FLIGHT, FLASH = 0.034, 0.78, 0.16
local CHEST_Z, BEHIND_Z = 1.3, 1.9
local LAND_X, LAND_Z = -560, 2.45
local ARC, TUMBLE = 950, 360
local LANDED = HITSTOP + FLIGHT
local FADE_AT, FADE_OUT = LANDED + 0.45, 0.7
local HEY_AT = 0.12
local PUNCH_END = FADE_AT + FADE_OUT

-- the kick, in seconds from it: the flash at the bottom centre, the jolt (px, s, degrees of twist), the tumble
local KICK_X, KICK_Y, KICK_FLASH = 960, 1000, 0.3
local JOLT, JOLT_TIME, JOLT_TWIST = 34, 0.45, 4
local TUMBLE_AT, TUMBLE_TIME = 0.12, 0.85

local ctx = nil
local run = nil
local camT, lastNow = nil, nil
local kickT, kickRot, kickWhoosh = nil, 0, false

local function rnd(a, b) return a + (b - a) * math.random() end

local function play(name, volume)
    local s = ctx.snd[name]
    if s then
        pcall(function() s:SetVolumePercent(volume or 100) end)
        s:Play()
    end
end

local function camReset()
    if camT ~= nil and GLOBALCAMERA ~= nil then pcall(function() GLOBALCAMERA:Reset() end) end
    camT = nil
end

function O.init(c) ctx = c end
function O.reset() run = nil; kickT = nil; camReset() end
function O.active() return run ~= nil or kickT ~= nil end

function O.start(chest, reward, now)
    run = { chest = chest, reward = reward, t0 = now, said = {}, dust = nil, shards = nil, leaveT = nil }
    local t = reward and reward.type or "snap"
    run.opens = t ~= "snap"
    run.talkAt = (t == "snap") and (SHATTER_AT + 0.9) or (t == "key" and (OPEN_AT + 0.8)) or nil
    run.modalAt = (t == "coins" or t == "nameplate" or t == "song") and (OPEN_AT + 0.6) or nil
end

local function once(key, cond, fn)
    if cond and not run.said[key] then
        run.said[key] = true
        fn()
        return true
    end
    return false
end

local function chestTex(open)
    return ctx.tex["Chest/Chest" .. run.chest .. (open and "Open" or "")]
end

---------------------------------------
-- The chest: fall, landing, bounces
---------------------------------------

-- its bottom's y, its squash (sx, sy) and its turn at time t
local function chestPose(t)
    if t < LAND_AT then
        local u = Tex.clamp01((t - DROP_AT) / FALL)
        local y = Tex.lerp(-30, DESK_Y, u * u)                    -- gravity: from rest above the screen
        return y, 1 - 0.06 * u, 1 + 0.12 * u, 16 * (1 - u) ^ 1.2
    end
    -- landing at 0, bouncing off again at 0.08 and 0.31, touching down at 0.26 and 0.40 (the sound's knocks)
    local l = t - LAND_AT
    if l < 0.08 then                                              -- the slam: flattened, then springing back
        local q = Easing.hump(l / 0.08)
        return DESK_Y, 1 + 0.24 * q, 1 - 0.26 * q, 0
    end
    if l < 0.26 then                                              -- the first bounce, stretched as it leaves
        local u = (l - 0.08) / 0.18
        local st = 1 + 0.08 * (1 - u) * (1 - u)
        return DESK_Y - 30 * 4 * u * (1 - u), 2 - st, st, -2.5 * math.sin(u * math.pi)
    end
    if l < 0.31 then
        local q = Easing.hump((l - 0.26) / 0.05)
        return DESK_Y, 1 + 0.1 * q, 1 - 0.11 * q, 0
    end
    if l < 0.40 then                                              -- the second, small one
        local u = (l - 0.31) / 0.09
        return DESK_Y - 8 * 4 * u * (1 - u), 1, 1, 0
    end
    if l < 0.47 then
        local q = Easing.hump((l - 0.40) / 0.07)
        return DESK_Y, 1 + 0.035 * q, 1 - 0.04 * q, 0
    end
    return DESK_Y, 1, 1, 0
end

local function landed(now)
    run.dust = {}
    for i = 1, 12 do
        local side = (i % 2 == 0) and 1 or -1
        run.dust[#run.dust + 1] = {
            tex = (i <= 8) and "Cabin/Dust" or "Cabin/Bubble",
            x = 960 + side * rnd(70, 140), y = DESK_Y - rnd(4, 16),
            vx = side * rnd(90, 260), vy = -rnd(20, 110), k = rnd(0.32, 0.55), grow = rnd(0.9, 1.6),
            life = rnd(0.55, 0.95), age = 0,
        }
        if i > 8 then
            local d = run.dust[#run.dust]
            d.vx, d.vy, d.k, d.grow, d.life = side * rnd(10, 60), -rnd(160, 260), rnd(0.4, 0.8), 0, rnd(0.8, 1.2)
        end
    end
    if GLOBALCAMERA ~= nil then
        pcall(function() GLOBALCAMERA:Shake(8, 0.3) end)
        camT = now
    end
end

-- the zoom that keeps the screen's edge out of sight with the screen turned by deg
local function cover(deg)
    local r = math.rad(deg)
    local c, s = math.abs(math.cos(r)), math.abs(math.sin(r))
    return c + s * Tex.SW / Tex.SH
end
local PEAK = math.deg(math.atan(Tex.SW, Tex.SH))     -- the turn that needs the most zoom (60.6 degrees)

-- the kicked view: the jolt, then one full turn; between the turn's first and last peak the zoom holds at its most,
-- so it does not pump
local function kickCam(now, dt)
    local a = now - kickT
    local deg = 360 * Easing.inOutCubic(Tex.clamp01((a - TUMBLE_AT) / TUMBLE_TIME))
    local need = (deg > PEAK and deg < 360 - PEAK) and cover(PEAK) or cover(deg)
    local j = Tex.clamp01(1 - a / JOLT_TIME)              -- the jolt still left (it fades out evenly)
    pcall(function()
        GLOBALCAMERA:SetRotation(deg < 360 and deg or 0)
        GLOBALCAMERA:SetUniformZoom(need * cover(JOLT_TWIST * j) + 0.06 * j)
        GLOBALCAMERA:Update(dt)
    end)
end

-- the screen camera while it shakes: zoomed in a little more than it shakes, so no edge shows
local function camStep(now, dt)
    if camT == nil or GLOBALCAMERA == nil then return end
    if kickT ~= nil then kickCam(now, dt); return end
    local a = now - camT
    if a >= 0.42 then camReset(); return end
    pcall(function()
        GLOBALCAMERA:SetUniformZoom(1 + 0.035 * (1 - a / 0.42))
        GLOBALCAMERA:Update(dt)
    end)
end

---------------------------------------
-- The key, seen from the front
---------------------------------------

-- the keyhole on screen for a chest bottom at y squashed by sy
local function holeAt(y, sy)
    return 960, y - (CHEST_H - HOLE_Y) * CHEST_SCALE * sy
end

-- the quarter turn on the key's length, in degrees (clockwise is negative): a small wind back, an eased turn past
-- the quarter, a settle
local function keyAngle(t)
    if t < TURN_AT then return 0 end
    local a = t - TURN_AT
    if a < WIND then return WIND_DEG * Easing.outQuad(a / WIND) end
    a = a - WIND
    if a < TURN then return Tex.lerp(WIND_DEG, TURNED - OVER, Easing.inOutCubic(a / TURN)) end
    local u = Tex.clamp01((a - TURN) / SETTLE)
    return TURNED - OVER * math.cos(u * math.pi * 1.5) * (1 - u)
end

-- the key at time t: its offset from the keyhole, its scale, its turn (deg) and how far it is seen from the side
-- (1 = end-on); nil before it comes
local function keyPose(t)
    if t < KEY_AT then return nil end
    if t < TOUCH then
        -- coming in from close to us: the nearer, the bigger and the further off the keyhole's axis
        local u = (t - KEY_AT) / FLY
        local z = Tex.lerp(Z0, 1, Easing.inOutSine(u))
        local o = 1 - Easing.inOutSine(u)
        return START_X * o / z, START_Y * o / z, KEY_K * TOUCH_K / z, START_ROT * (1 - Easing.outCubic(u)), 1 + (YAW - 1) * o
    end
    local k = KEY_K
    if t < SEATED then
        -- the tip goes in: pushed a little past home, then back
        local u = (t - TOUCH) / PUSH
        if u < 0.65 then
            k = KEY_K * Tex.lerp(TOUCH_K, 0.95, Easing.inOutQuad(u / 0.65))
        else
            k = KEY_K * Tex.lerp(0.95, 1, Easing.outQuad((u - 0.65) / 0.35))
        end
    end
    return 0, 0, k, keyAngle(t), 1
end

local function drawKey(t, hx, hy, op)
    local key = ctx.tex["Chest/KeyFront" .. run.chest]
    if not Tex.ok(key) or run.shards ~= nil then return end
    local dx, dy, k, rot, yaw = keyPose(t)
    if dx == nil then return end
    if not run.opens and t >= AFTER and t < SHATTER_AT then rot = rot + 3 * math.sin((t - AFTER) * 80) end
    local x, y = hx + dx, hy + dy
    local lift = (k / KEY_K - 1) * 14
    Tex.sprite(key, Tex.frame(x + 4 + lift, y + 6 + lift, rot, k), yaw, 1, 0.3 * op, 0)
    Tex.sprite(key, Tex.frame(x, y, rot, k), yaw, 1, op)
end

-- the key flies apart where it is: cut in pieces, thrown out and falling
local function breakKey(t, hx, hy)
    run.shards = {}
    local key = ctx.tex["Chest/KeyFront" .. run.chest]
    if not Tex.ok(key) then return end
    local _, _, k, rot = keyPose(t)
    local f = Tex.frame(hx, hy, rot, k)
    local cw, ch = math.floor(key.Width / SHARDS_X), math.floor(key.Height / SHARDS_Y)
    for gy = 0, SHARDS_Y - 1 do
        for gx = 0, SHARDS_X - 1 do
            local rx, ry = gx * cw, gy * ch
            local p = Tex.child(f, rx + cw / 2 - key.Width / 2, ry + ch / 2 - key.Height / 2)
            local ux, uy = p.x - hx, p.y - hy
            local d = math.max(1, math.sqrt(ux * ux + uy * uy))
            run.shards[#run.shards + 1] = {
                rx = rx, ry = ry, w = cw, h = ch, x = p.x, y = p.y, rot = rot, k = k,
                vx = ux / d * rnd(160, 420) + rnd(-60, 60), vy = uy / d * rnd(120, 300) - rnd(260, 480),
                spin = rnd(-540, 540),
            }
        end
    end
    run.shardT = t
end

local function drawShards(t, dt)
    local key = ctx.tex["Chest/KeyFront" .. run.chest]
    if run.shards == nil or not Tex.ok(key) then return end
    local a = t - run.shardT
    local op = 1 - Tex.clamp01((a - 0.55) / 0.5)
    if op <= 0 then return end
    for _, s in ipairs(run.shards) do
        s.vy = s.vy + 1700 * dt
        s.x, s.y = s.x + s.vx * dt, s.y + s.vy * dt
        s.rot = s.rot + s.spin * dt
        Tex.piece(key, Tex.frame(s.x, s.y, s.rot, s.k), s.rx, s.ry, s.w, s.h, 1, 1, op)
    end
end

---------------------------------------
-- The punch
---------------------------------------

-- the punched chest a seconds after the hit: its bottom on screen (x, y), its squash (sx, sy), its turn, its depth
-- and its opacity
local function punchPose(a)
    if a < HITSTOP then
        local j = (math.floor(a * 120) % 2 == 0) and 4 or -4
        return 960 + j, DESK_Y, 1.08, 0.93, 0, CHEST_Z, 1
    end
    local op = 1 - Tex.clamp01((a - FADE_AT) / FADE_OUT)
    local X0, Y0 = Cabin.unproject(960, DESK_Y, CHEST_Z)
    local FY = Cabin.FLOOR_Y
    if a < LANDED then
        local u = (a - HITSTOP) / FLIGHT
        local s = 1 - (1 - u) ^ 1.6                               -- slowed by the water as it goes
        local z = Tex.lerp(CHEST_Z, LAND_Z, s)
        local Y = Tex.lerp(Y0, FY, u) - ARC * 4 * u * (1 - u)
        local x, y = Cabin.project(Tex.lerp(X0, LAND_X, s), Y, z)
        return x, y, 1, 1, TUMBLE * Easing.outQuad(u), z, op
    end
    -- on the floor: a squash, one small hop, a smaller squash
    local l = a - LANDED
    local sx, sy, Y = 1, 1, FY
    if l < 0.06 then
        local q = Easing.hump(l / 0.06)
        sx, sy = 1 + 0.18 * q, 1 - 0.2 * q
    elseif l < 0.24 then
        local u = (l - 0.06) / 0.18
        Y = FY - 70 * 4 * u * (1 - u)
    elseif l < 0.29 then
        local q = Easing.hump((l - 0.24) / 0.05)
        sx, sy = 1 + 0.06 * q, 1 - 0.07 * q
    end
    local x, y = Cabin.project(LAND_X, Y, LAND_Z)
    return x, y, sx, sy, TUMBLE, LAND_Z, op
end

local function drawPunched(now, back)
    local a = now - run.punchT
    local x, y, sx, sy, rot, z, op = punchPose(a)
    if op <= 0 or (z > BEHIND_Z) ~= back then return end
    local tex = chestTex(false)
    if not Tex.ok(tex) then return end
    local k = CHEST_SCALE * CHEST_Z / z
    local shade = 1 - 0.3 * Tex.clamp01((z - CHEST_Z) / (LAND_Z - CHEST_Z))
    Tex.sprite(tex, Tex.frame(x, y - tex.Height / 2 * k * sy, rot, k), sx, sy, op, shade)
end

local function drawFlash(now)
    local a = now - run.punchT
    if a >= FLASH then return end
    local op = 1 - Tex.clamp01((a - HITSTOP) / (FLASH - HITSTOP))
    Tex.sprite(ctx.tex["Chest/Impact"], Tex.frame(HIT_X, HIT_Y, run.punchRot, 0.6 + 0.6 * Easing.outQuad(a / FLASH)), 1, 1, op)
end

function O.punch(now)
    if run == nil or run.opens or run.punchT ~= nil then return end
    run.punchT = now
    run.punchRot = rnd(-25, 25)
    play("Punch")
    if GLOBALCAMERA ~= nil then
        pcall(function() GLOBALCAMERA:Shake(14, 0.3) end)
        camT = now
    end
end

function O.drawBack(now)
    if run ~= nil and run.punchT ~= nil then drawPunched(now, true) end
end

---------------------------------------
-- The kick
---------------------------------------

function O.kick(now)
    kickT, kickRot = now, rnd(-20, 20)
    kickWhoosh = false
    play("Punch")
    if GLOBALCAMERA ~= nil then
        pcall(function() GLOBALCAMERA:Shake(JOLT, JOLT_TIME, JOLT_TWIST) end)
        camT = now
    end
end

-- the boot's flash at the bottom of the screen
function O.drawKick(now)
    if kickT == nil then return end
    local a = now - kickT
    if a >= KICK_FLASH then return end
    local u = a / KICK_FLASH
    Tex.sprite(ctx.tex["Chest/Impact"], Tex.frame(KICK_X, KICK_Y, kickRot, 1.5 + 1.2 * Easing.outQuad(u)), 1, 1,
        1 - Easing.inQuad(u))
end

---------------------------------------
-- Main
---------------------------------------

function O.update(now)
    local dt = lastNow and math.max(0, math.min(0.1, now - lastNow)) or 0
    lastNow = now
    camStep(now, dt)
    if kickT ~= nil and not kickWhoosh and now - kickT >= TUMBLE_AT then
        kickWhoosh = true
        play("Whoosh")
    end
    if run == nil then return nil end
    local t = now - run.t0
    local ev = nil
    once("fall", t >= DROP_AT - 0.04, function() play("Whoosh") end)
    once("land", t >= LAND_AT, function() play("ChestDrop"); landed(now) end)
    once("keyin", t >= SEATED - KEYIN_CLICK, function() play("KeyIn") end)
    once("turn", t >= TURN_AT + WIND + TURN - TURN_CLUNK, function() play("KeyTurn") end)
    if run.opens then
        once("open", t >= OPEN_AT, function() play("ChestOpen") end)
    else
        once("shatter", t >= SHATTER_AT, function()
            play("Shatter")
            play("KeySnap")
            local y, _, sy = chestPose(t)
            breakKey(t, holeAt(y, sy))
        end)
    end
    if run.talkAt and once("talk", t >= run.talkAt, function() end) then ev = "talk" end
    if run.modalAt and once("modal", t >= run.modalAt, function() end) then ev = "modal" end
    if run.punchT ~= nil then
        local a = now - run.punchT
        if once("hey", a >= HEY_AT, function() end) then ev = "hey" end
        once("thud", a >= LANDED, function() play("ChestFar") end)
    end
    if run.leaveT ~= nil and now - run.leaveT >= LEAVE and (run.punchT == nil or now - run.punchT >= PUNCH_END) then
        run = nil
        return "done"
    end
    return ev
end

function O.leave(now)
    if run ~= nil and run.leaveT == nil then
        run.leaveT = now
        if run.punchT == nil then play("Whoosh", 55) end
    end
end

local function drawDust(dt)
    if run.dust == nil then return end
    for _, p in ipairs(run.dust) do
        p.age = p.age + dt
        if p.age < p.life then
            p.x, p.y = p.x + p.vx * dt, p.y + p.vy * dt
            p.vx, p.vy = p.vx * (1 - 3 * dt), p.vy * (1 - 2 * dt) - 20 * dt
            local u = p.age / p.life
            local k = p.k * (1 + p.grow * u)
            Tex.place(ctx.tex[p.tex], p.x, p.y, "center", k, k, 0.8 * (1 - u))
        end
    end
end

-- the won key rising out of the open chest, up to just over Mr.Caspian's hat
local function wonKey(t)
    local r = run.reward
    if r == nil or r.type ~= "key" then return nil end
    local u = (t - OPEN_AT - 0.1) / POP
    if u < 0 then return nil end
    local e = Easing.outBack(Tex.clamp01(u), 1.5)
    local y = Tex.lerp(DESK_Y - 150, WON_Y, e) + 8 * math.sin(t * 2.2)
    local rot = -18 + 360 * (1 - Easing.outCubic(Tex.clamp01(u)))
    return ctx.tex["Chest/Key" .. (r.target or 2)], 960, y, Tex.lerp(0.2, 0.7, e), rot, Tex.clamp01(u * 4)
end

function O.draw(now, dt)
    if run == nil then return end
    local t = now - run.t0
    if t < DROP_AT - 0.05 then return end
    local leave = run.leaveT and Tex.clamp01((now - run.leaveT) / LEAVE) or 0
    local op = 1 - leave

    local y, sx, sy, rot = chestPose(t)
    y = y - 380 * Easing.inQuad(leave)
    local open = run.opens and t >= OPEN_AT
    if open then
        local o = t - OPEN_AT
        if o < 0.3 then local q = Easing.hump(o / 0.3); sx, sy = sx * (1 + 0.06 * q), sy * (1 + 0.09 * q) end
    end
    local tex = chestTex(open)
    if not Tex.ok(tex) then tex = chestTex(false) end
    if run.punchT ~= nil then
        drawPunched(now, false)
    elseif Tex.ok(tex) then
        local cy = y - tex.Height / 2 * CHEST_SCALE * sy          -- turned and squashed about its bottom
        Tex.sprite(tex, Tex.frame(960, cy, rot, CHEST_SCALE), sx, sy, op)
    end
    drawDust(dt)
    local hx, hy = holeAt(y, sy)
    drawKey(t, hx, hy, op)
    drawShards(t, dt)
    if run.punchT ~= nil then drawFlash(now) end

    local wt, wx, wy, wk, wrot, wop = wonKey(t)
    if Tex.ok(wt) then Tex.sprite(wt, Tex.frame(wx, wy - 300 * Easing.inQuad(leave), wrot, wk), 1, 1, wop * op) end
end

local KEY_NAMES = {
    { "VAULT_KEY_1", "Vault Key" },
    { "VAULT_KEY_2", "Vault Key (Gold)" },
    { "VAULT_KEY_3", "Vault Key (OpTk)" },
}

-- the "new key" banner over the won key
function O.drawBanner(now)
    if run == nil or run.reward == nil or run.reward.type ~= "key" then return end
    local t = now - run.t0
    local a = Tex.clamp01((t - OPEN_AT - 0.1 - POP * 0.6) / 0.3)
    if run.leaveT then a = a * (1 - Tex.clamp01((now - run.leaveT) / 0.3)) end
    if a <= 0 then return end
    local k = 1 + 0.12 * (1 - Easing.outBack(a, 2))
    local ink = Tex.col(40, 22, 8)
    local title = Tex.tr("VAULT_KEY_GOT", "New key!")
    local big = ctx.fonts.big
    local ks = k * Tex.fitScale(big, title, 860)
    Tex.text(big, title, Tex.frame(963, BANNER_Y + 4), Tex.col(0, 0, 0), Tex.col(0, 0, 0), ks, 900, "center", 0.4 * a)
    Tex.text(big, "<g.#FFF4C8.#E0A030>" .. title .. "</g>", Tex.frame(960, BANNER_Y), Tex.col(255, 255, 255), ink, ks, 900, "center", a)
    local nm = KEY_NAMES[run.reward.target or 2] or KEY_NAMES[2]
    local name = Tex.tr(nm[1], nm[2])
    Tex.text(ctx.fonts.title, name, Tex.frame(960, BANNER_Y + 58), Tex.col(255, 246, 226), ink,
        Tex.fitScale(ctx.fonts.title, name, 700), 700, "center", a)
end

return O
