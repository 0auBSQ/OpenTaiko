---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local, inject-field
local Tex = require("tex")
local Easing = require("Easing")
local Cabin = require("cabin")
local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")
local Chest, Banner = Layout.chest, Layout.chest.banner
local Run = Animation.chest_open
local Time, Fall, Bounce, KeyMotion = Run.timeline, Run.fall, Run.bounces, Run.key
local Shards, Landing, Punch, Kick, Won = Run.shards, Run.landing, Run.punch, Run.kick, Run.won_key

local O = {}

local DESK_Y = Cabin.DESK_Y
local CHEST_X = Chest.x
local CHEST_SCALE = Chest.scale
local CHEST_H, HOLE_Y = Chest.h, Chest.hole_y

-- the key: its scale once home and when its tip touches the keyhole; where it starts (px off the keyhole at the
-- keyhole's depth), how close (depth, 1 = the keyhole), turned and seen from the side (1 = end-on)
local KEY_K = Chest.key_scale
local TOUCH_K = KeyMotion.touch_scale
local START_X, START_Y, Z0, START_ROT, YAW = KeyMotion.start_x, KeyMotion.start_y, KeyMotion.start_depth, KeyMotion.start_rot_deg, KeyMotion.start_yaw
local TURNED, WIND_DEG, OVER = KeyMotion.turned_deg, KeyMotion.wind_deg, KeyMotion.over_deg
local SHARDS_X, SHARDS_Y = Shards.cols, Shards.rows   -- a broken key falls apart in this many pieces

-- seconds from the start
local DROP_AT, FALL = Time.drop_at_s, Time.fall_s
local LAND_AT = DROP_AT + FALL
local KEY_AT, FLY, PUSH = Time.key_at_s, Time.fly_s, Time.push_s
local TOUCH = KEY_AT + FLY
local SEATED = TOUCH + PUSH
local TURN_AT, WIND, TURN, SETTLE = Time.turn_at_s, Time.wind_s, Time.turn_s, Time.settle_s
local KEYIN_CLICK = Time.keyin_click_s           -- KeyIn.ogg's click, played to land as the key gets home
local TURN_CLUNK = Time.turn_clunk_s             -- KeyTurn.ogg's bolt, played to land on the turn's furthest point
local AFTER = Time.after_s
local SHATTER_AT = AFTER + Time.shatter_after_s
local OPEN_AT = AFTER + Time.open_after_s
local POP, LEAVE = Time.pop_s, Time.leave_s
local WON_Y, BANNER_Y = Chest.won_y, Banner.y

-- the punch, in seconds from the hit; the flight is in the room's space (cabin.lua): the chest stands on the desk at
-- depth CHEST_Z and lands on the floor at (LAND_X, LAND_Z), ARC px over its straight path at the top; past BEHIND_Z
-- it is drawn behind Mr.Caspian
local HIT_X, HIT_Y = Chest.hit.x, Chest.hit.y
local HITSTOP, FLIGHT, FLASH = Punch.hitstop_s, Punch.flight_s, Punch.flash_s
local CHEST_Z, BEHIND_Z = Punch.chest_depth, Punch.behind_depth
local LAND_X, LAND_Z = Punch.land_x, Punch.land_depth
local ARC, TUMBLE = Punch.arc_px, Punch.tumble_deg
local LANDED = HITSTOP + FLIGHT
local FADE_AT, FADE_OUT = LANDED + Punch.fade_after_s, Punch.fade_s
local HEY_AT = Punch.hey_after_s
local PUNCH_END = FADE_AT + FADE_OUT

-- the kick, in seconds from it: the flash at the bottom centre, the jolt (px, s, degrees of twist), the tumble
local KICK_X, KICK_Y, KICK_FLASH = Chest.kick_flash.x, Chest.kick_flash.y, Kick.flash_s
local JOLT, JOLT_TIME, JOLT_TWIST = Kick.jolt_px, Kick.jolt_s, Kick.jolt_twist_deg
local TUMBLE_AT, TUMBLE_TIME = Kick.tumble_at_s, Kick.tumble_s

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
    run.talkAt = (t == "snap") and (SHATTER_AT + Time.snap_talk_after_s) or (t == "key" and (OPEN_AT + Time.key_talk_after_s)) or nil
    run.modalAt = (t == "coins" or t == "nameplate" or t == "song" or t == "furniture") and (OPEN_AT + Time.modal_after_s) or nil
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
        local y = Tex.lerp(Fall.from_y, DESK_Y, u * u)            -- gravity: from rest above the screen
        return y, 1 - Fall.squash_x * u, 1 + Fall.stretch_y * u, Fall.rot_deg * (1 - u) ^ Fall.rot_ease_pow
    end
    -- the slam, the first bounce, its knock, the second small bounce, its knock (the sound's knocks)
    local B = Bounce
    local slamEnd = B.slam_s
    local bounce1End = slamEnd + B.bounce1_s
    local knock1End = bounce1End + B.knock1_s
    local bounce2End = knock1End + B.bounce2_s
    local knock2End = bounce2End + B.knock2_s
    local l = t - LAND_AT
    if l < slamEnd then                                           -- the slam: flattened, then springing back
        local q = Easing.hump(l / B.slam_s)
        return DESK_Y, 1 + B.slam_x * q, 1 - B.slam_y * q, 0
    end
    if l < bounce1End then                                        -- the first bounce, stretched as it leaves
        local u = (l - slamEnd) / B.bounce1_s
        local st = 1 + B.bounce1_stretch * (1 - u) * (1 - u)
        return DESK_Y - B.bounce1_px * 4 * u * (1 - u), 2 - st, st, B.bounce1_rot_deg * math.sin(u * math.pi)
    end
    if l < knock1End then
        local q = Easing.hump((l - bounce1End) / B.knock1_s)
        return DESK_Y, 1 + B.knock1_x * q, 1 - B.knock1_y * q, 0
    end
    if l < bounce2End then                                        -- the second, small one
        local u = (l - knock1End) / B.bounce2_s
        return DESK_Y - B.bounce2_px * 4 * u * (1 - u), 1, 1, 0
    end
    if l < knock2End then
        local q = Easing.hump((l - bounce2End) / B.knock2_s)
        return DESK_Y, 1 + B.knock2_x * q, 1 - B.knock2_y * q, 0
    end
    return DESK_Y, 1, 1, 0
end

-- a random value in a [min, max] list
local function rndIn(r) return rnd(r[1], r[2]) end

local function landed(now)
    run.dust = {}
    local Dust, Bub = Landing.dust, Landing.bubbles
    for i = 1, Landing.count do
        local side = (i % 2 == 0) and 1 or -1
        run.dust[#run.dust + 1] = {
            tex = (i <= Landing.dust_count) and "Cabin/Dust" or "Cabin/Bubble",
            x = CHEST_X + side * rndIn(Dust.x), y = DESK_Y - rndIn(Dust.y),
            vx = side * rndIn(Dust.vx), vy = -rndIn(Dust.vy), k = rndIn(Dust.scale), grow = rndIn(Dust.grow),
            life = rndIn(Dust.life_s), age = 0,
        }
        if i > Landing.dust_count then
            local d = run.dust[#run.dust]
            d.vx, d.vy, d.k, d.grow, d.life = side * rndIn(Bub.vx), -rndIn(Bub.vy), rndIn(Bub.scale), Bub.grow, rndIn(Bub.life_s)
        end
    end
    if GLOBALCAMERA ~= nil then
        pcall(function() GLOBALCAMERA:Shake(Landing.shake_px, Landing.shake_s) end)
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
        GLOBALCAMERA:SetUniformZoom(need * cover(JOLT_TWIST * j) + Kick.jolt_zoom * j)
        GLOBALCAMERA:Update(dt)
    end)
end

-- the screen camera while it shakes: zoomed in a little more than it shakes, so no edge shows
local function camStep(now, dt)
    if camT == nil or GLOBALCAMERA == nil then return end
    if kickT ~= nil then kickCam(now, dt); return end
    local a = now - camT
    local Cam = Run.camera
    if a >= Cam.shake_zoom_s then camReset(); return end
    pcall(function()
        GLOBALCAMERA:SetUniformZoom(1 + Cam.shake_zoom * (1 - a / Cam.shake_zoom_s))
        GLOBALCAMERA:Update(dt)
    end)
end

---------------------------------------
-- The key, seen from the front
---------------------------------------

-- the keyhole on screen for a chest bottom at y squashed by sy
local function holeAt(y, sy)
    return CHEST_X, y - (CHEST_H - HOLE_Y) * CHEST_SCALE * sy
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
    return TURNED - OVER * math.cos(u * math.pi * KeyMotion.settle_turns) * (1 - u)
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
        local split, back = KeyMotion.push_split, KeyMotion.push_back_scale
        if u < split then
            k = KEY_K * Tex.lerp(TOUCH_K, back, Easing.inOutQuad(u / split))
        else
            k = KEY_K * Tex.lerp(back, 1, Easing.outQuad((u - split) / (1 - split)))
        end
    end
    return 0, 0, k, keyAngle(t), 1
end

local function drawKey(t, hx, hy, op)
    local key = ctx.tex["Chest/KeyFront" .. run.chest]
    if not Tex.ok(key) or run.shards ~= nil then return end
    local dx, dy, k, rot, yaw = keyPose(t)
    if dx == nil then return end
    if not run.opens and t >= AFTER and t < SHATTER_AT then
        rot = rot + KeyMotion.snap_shake_deg * math.sin((t - AFTER) * KeyMotion.snap_shake_rad_per_s)
    end
    local x, y = hx + dx, hy + dy
    local Sh = Chest.key_shadow
    local lift = (k / KEY_K - 1) * Sh.lift
    Tex.sprite(key, Tex.frame(x + Sh.x + lift, y + Sh.y + lift, rot, k), yaw, 1, Sh.opacity * op, 0)
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
                vx = ux / d * rndIn(Shards.out_speed) + rndIn(Shards.side_speed),
                vy = uy / d * rndIn(Shards.rise_speed) - rndIn(Shards.toss_speed),
                spin = rndIn(Shards.spin_deg_per_s),
            }
        end
    end
    run.shardT = t
end

local function drawShards(t, dt)
    local key = ctx.tex["Chest/KeyFront" .. run.chest]
    if run.shards == nil or not Tex.ok(key) then return end
    local a = t - run.shardT
    local op = 1 - Tex.clamp01((a - Shards.fade_after_s) / Shards.fade_s)
    if op <= 0 then return end
    for _, s in ipairs(run.shards) do
        s.vy = s.vy + Shards.gravity * dt
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
        local j = (math.floor(a * Punch.jitter_hz) % 2 == 0) and Punch.jitter_px or -Punch.jitter_px
        return CHEST_X + j, DESK_Y, Punch.hit_squash_x, Punch.hit_squash_y, 0, CHEST_Z, 1
    end
    local op = 1 - Tex.clamp01((a - FADE_AT) / FADE_OUT)
    local X0, Y0 = Cabin.unproject(CHEST_X, DESK_Y, CHEST_Z)
    local FY = Cabin.FLOOR_Y
    if a < LANDED then
        local u = (a - HITSTOP) / FLIGHT
        local s = 1 - (1 - u) ^ Punch.water_pow                   -- slowed by the water as it goes
        local z = Tex.lerp(CHEST_Z, LAND_Z, s)
        local Y = Tex.lerp(Y0, FY, u) - ARC * 4 * u * (1 - u)
        local x, y = Cabin.project(Tex.lerp(X0, LAND_X, s), Y, z)
        return x, y, 1, 1, TUMBLE * Easing.outQuad(u), z, op
    end
    -- on the floor: a squash, one small hop, a smaller squash
    local l = a - LANDED
    local sx, sy, Y = 1, 1, FY
    local squashEnd = Punch.land_squash_s
    local hopEnd = squashEnd + Punch.hop_s
    if l < squashEnd then
        local q = Easing.hump(l / Punch.land_squash_s)
        sx, sy = 1 + Punch.land_squash_x * q, 1 - Punch.land_squash_y * q
    elseif l < hopEnd then
        local u = (l - squashEnd) / Punch.hop_s
        Y = FY - Punch.hop_px * 4 * u * (1 - u)
    elseif l < hopEnd + Punch.knock_s then
        local q = Easing.hump((l - hopEnd) / Punch.knock_s)
        sx, sy = 1 + Punch.knock_x * q, 1 - Punch.knock_y * q
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
    local shade = 1 - Punch.far_shade * Tex.clamp01((z - CHEST_Z) / (LAND_Z - CHEST_Z))
    Tex.sprite(tex, Tex.frame(x, y - tex.Height / 2 * k * sy, rot, k), sx, sy, op, shade)
end

local function drawFlash(now)
    local a = now - run.punchT
    if a >= FLASH then return end
    local op = 1 - Tex.clamp01((a - HITSTOP) / (FLASH - HITSTOP))
    Tex.sprite(ctx.tex["Chest/Impact"], Tex.frame(HIT_X, HIT_Y, run.punchRot, Punch.flash_scale + Punch.flash_grow * Easing.outQuad(a / FLASH)), 1, 1, op)
end

function O.punch(now)
    if run == nil or run.opens or run.punchT ~= nil then return end
    run.punchT = now
    run.punchRot = rnd(-Punch.rot_deg, Punch.rot_deg)
    play("Punch")
    if GLOBALCAMERA ~= nil then
        pcall(function() GLOBALCAMERA:Shake(Punch.shake_px, Punch.shake_s) end)
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
    kickT, kickRot = now, rnd(-Kick.rot_deg, Kick.rot_deg)
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
    Tex.sprite(ctx.tex["Chest/Impact"], Tex.frame(KICK_X, KICK_Y, kickRot, Kick.flash_scale + Kick.flash_grow * Easing.outQuad(u)), 1, 1,
        1 - Easing.inQuad(u))
end

---------------------------------------
-- Main
---------------------------------------

function O.update(now)
    local dt = lastNow and math.max(0, math.min(Animation.frame_step_max_s, now - lastNow)) or 0
    lastNow = now
    camStep(now, dt)
    if kickT ~= nil and not kickWhoosh and now - kickT >= TUMBLE_AT then
        kickWhoosh = true
        play("Whoosh")
    end
    if run == nil then return nil end
    local t = now - run.t0
    local ev = nil
    once("fall", t >= DROP_AT - Time.whoosh_lead_s, function() play("Whoosh") end)
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
            p.vx, p.vy = p.vx * (1 - Landing.drag_x * dt), p.vy * (1 - Landing.drag_y * dt) - Landing.gravity * dt
            local u = p.age / p.life
            local k = p.k * (1 + p.grow * u)
            Tex.place(ctx.tex[p.tex], p.x, p.y, "center", k, k, Landing.opacity * (1 - u))
        end
    end
end

-- the won key rising out of the open chest, up to just over Mr.Caspian's hat
local function wonKey(t)
    local r = run.reward
    if r == nil or r.type ~= "key" then return nil end
    local u = (t - OPEN_AT - Won.delay_s) / POP
    if u < 0 then return nil end
    local e = Easing.outBack(Tex.clamp01(u), Won.overshoot)
    local y = Tex.lerp(DESK_Y - Won.rise_from_dy, WON_Y, e) + Won.bob_px * math.sin(t * Won.bob_rad_per_s)
    local rot = Won.rot_deg + Won.spin_deg * (1 - Easing.outCubic(Tex.clamp01(u)))
    return ctx.tex["Chest/Key" .. (r.target or 2)], CHEST_X, y, Tex.lerp(Won.scale[1], Won.scale[2], e), rot, Tex.clamp01(u * Won.fade_speed)
end

function O.draw(now, dt)
    if run == nil then return end
    local t = now - run.t0
    if t < DROP_AT - Time.show_lead_s then return end
    local leave = run.leaveT and Tex.clamp01((now - run.leaveT) / LEAVE) or 0
    local op = 1 - leave

    local y, sx, sy, rot = chestPose(t)
    y = y - Run.leave.rise_px * Easing.inQuad(leave)
    local open = run.opens and t >= OPEN_AT
    if open then
        local o = t - OPEN_AT
        local Pop = Run.open
        if o < Pop.hump_s then local q = Easing.hump(o / Pop.hump_s); sx, sy = sx * (1 + Pop.x * q), sy * (1 + Pop.y * q) end
    end
    local tex = chestTex(open)
    if not Tex.ok(tex) then tex = chestTex(false) end
    if run.punchT ~= nil then
        drawPunched(now, false)
    elseif Tex.ok(tex) then
        local cy = y - tex.Height / 2 * CHEST_SCALE * sy          -- turned and squashed about its bottom
        Tex.sprite(tex, Tex.frame(CHEST_X, cy, rot, CHEST_SCALE), sx, sy, op)
    end
    drawDust(dt)
    local hx, hy = holeAt(y, sy)
    drawKey(t, hx, hy, op)
    drawShards(t, dt)
    if run.punchT ~= nil then drawFlash(now) end

    local wt, wx, wy, wk, wrot, wop = wonKey(t)
    if Tex.ok(wt) then Tex.sprite(wt, Tex.frame(wx, wy - Run.leave.won_rise_px * Easing.inQuad(leave), wrot, wk), 1, 1, wop * op) end
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
    local BA = Run.banner
    local a = Tex.clamp01((t - OPEN_AT - BA.delay_s - POP * BA.after_pop) / BA.fade_s)
    if run.leaveT then a = a * (1 - Tex.clamp01((now - run.leaveT) / BA.out_s)) end
    if a <= 0 then return end
    local k = 1 + BA.pop_grow * (1 - Easing.outBack(a, BA.overshoot))
    local function col(c) return Tex.col(c[1], c[2], c[3]) end
    local ink = col(Banner.ink_color)
    local title = Tex.tr("VAULT_KEY_GOT", "New key!")
    local big = ctx.fonts.big
    local ks = k * Tex.fitScale(big, title, Banner.title_fit)
    local Sh = Banner.shadow
    local cx = Banner.x
    Tex.text(big, title, Tex.frame(cx + Sh.dx, BANNER_Y + Sh.dy), col(Sh.color), col(Sh.color), ks, Banner.title_max_width, "center", Sh.opacity * a)
    Tex.text(big, string.format(Banner.title_markup, title), Tex.frame(cx, BANNER_Y), col(Banner.title_color), ink, ks, Banner.title_max_width, "center", a)
    local nm = KEY_NAMES[run.reward.target or 2] or KEY_NAMES[2]
    local name = Tex.tr(nm[1], nm[2])
    Tex.text(ctx.fonts.title, name, Tex.frame(cx, BANNER_Y + Banner.name_dy), col(Banner.name_color), ink,
        Tex.fitScale(ctx.fonts.title, name, Banner.name_max_width), Banner.name_max_width, "center", a)
end

return O
