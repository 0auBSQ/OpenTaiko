---@diagnostic disable: undefined-global, undefined-field, need-check-nil
-- modal_chest.lua — the reveal before an item card: the screen darkens under rolling smoke in the
-- rarity's colour with a slow wheel of light turning behind it; from rare up a cushion drops in
-- first; the rarity's chest thuds down (onto the cushion or the ground), its latches give, it
-- rattles harder and harder, squats, and pops open with a stretch, a hop and a burst of light;
-- then the screen whites out and settles to black, which is where the card takes over. Each chest
-- opens with its own sound and its own pace: the legendary one strains longest before it gives.
-- Poor is a cardboard box in a dusty attic: dust instead of smoke, motes instead of sparkles, no
-- wheel of light. Mythical's smoke and light run through the rainbow. A decide press skips
-- straight to the white-out.
--
-- chest_<rarity>.png is a horizontal strip of 512x512 frames: closed on the left, open on the
-- right. Two frames are all the opening needs, the motion carries it (the rattle, the squash, the
-- pop); any extra frames in between are shown during the wind-up.

local Fx = require("modal_fx")

local M = {}
M.__index = M

local CHEST_X = 960
local FRAME = 512
local BASE_ROW, MOUTH_ROW = 482, 270           -- the chest's base and where its light pours out, inside a frame
local CUSHION_BOTTOM = 900                     -- where the cushion's bottom row lands
local CUSHION_SEAT = 0.5                       -- the chest's base sits this far down the cushion (its top face)
local GROUND_SEAT = 838                        -- where the chest's base rests without a cushion
local DROP_FROM = -600
local SMOKE_N = 30
local WHEEL_SPEED = 24                         -- degrees per second

-- timeline (seconds)
local T_CUSHION, CUSHION_DROP = 0.8, 0.45
local T_CHEST, CHEST_DROP = 1.45, 0.5
local T_LATCH = 2.45
local T_OPEN = 2.85
-- per rarity: how long the rattle builds before the lid pops, and how long the light is admired
local PACE = {
    poor = { windup = 0.25, admire = 0.70 }, common = { windup = 0.32, admire = 0.95 }, uncommon = { windup = 0.36, admire = 0.95 },
    rare = { windup = 0.40, admire = 1.00 }, epic = { windup = 0.45, admire = 1.10 }, legendary = { windup = 1.00, admire = 1.40 },
    mythical = { windup = 0.60, admire = 1.50 },
}
local FLASH_IN, FLASH_HOLD, FLASH_TO_BLACK, BLACK_HOLD = 0.22, 0.12, 0.45, 0.25
local ATTIC_DUST = { 168, 154, 132 }

local sin, cos, pi, random, min, max, floor = math.sin, math.cos, math.pi, math.random, math.min, math.max, math.floor

function M.new(A)
    local self = setmetatable({}, M)
    self.A = A
    self.smoke = Fx.pool(A.tex.smoke, nil)
    self.smoke2 = Fx.pool(A.tex.smoke, nil)
    self.dust = Fx.pool(A.tex.dust, nil)
    self.sparks = Fx.pool(A.tex.star, "add")
    self.motes = Fx.pool(A.tex.spark, nil)
    self.tl = Fx.timeline()
    return self
end

-- the sound cues a tier uses: the cardboard box has its own, every chest opens with its own
local function cues(key)
    if key == "poor" then return { land = "box_land", latch = "box_rustle", open = "box_open" } end
    return { land = "chest", latch = "latch", open = "open_" .. key }
end

-- key: one of Fx.RARITIES
function M:start(key)
    self.key = key
    self.rainbow = key == "mythical"
    self.attic = key == "poor"
    self.withCushion = (Fx.RARITY_RANK[key] or 0) >= Fx.RARITY_RANK.rare
    self.chestTex = self.A.tex["chest_" .. key] or self.A.tex.chest_common
    self.cue = cues(key)
    local pace = PACE[key] or PACE.common
    self.windup = pace.windup
    self.tPop = T_OPEN + pace.windup
    self.tFlash = self.tPop + pace.admire
    self.t, self.wheelRot, self.moteAcc = 0, 0, 0
    self.cushionLandT, self.landT, self.popT, self.shakeT = 10, 10, 10, 10
    self.flashT = nil
    self.smoke:clear() ; self.smoke2:clear() ; self.dust:clear() ; self.sparks:clear() ; self.motes:clear()

    -- a screen of slow puffs, the outer ones rolling in first and the centre filling last; the
    -- attic gets a thinner, dustier haze
    for i = 1, SMOKE_N do
        local a = (i / SMOKE_N) * 2 * pi + random() * 0.4
        local d = 180 + random() * 720
        local x, y = 960 + cos(a) * d, 540 + sin(a) * d * 0.62
        local pool = (i % 2 == 0) and self.smoke or self.smoke2
        local col = self.rainbow and Fx.rainbow(i / SMOKE_N, 0.2) or (self.attic and ATTIC_DUST or Fx.rarityColor(key))
        pool:emit{ x = x, y = y, vx = -cos(a) * (18 + random() * 22), vy = -sin(a) * 10 - (8 + random() * 14),
                   life = 60, s0 = 2.4 + random() * 2.2, s1 = 3.6 + random() * 2.6, rot = random() * 360,
                   spin = (random() - 0.5) * 14, color = col, fade = "rise", rise = 0.35 + (1 - d / 900) * 0.9 + random() * 0.3,
                   alpha = (self.attic and 0.22 or 0.5) + random() * (self.attic and 0.14 or 0.3) }
    end

    local tl = self.tl
    tl.cues = {}
    tl:reset()
    tl:at(0, function() self.A:play("puff") end)
    if self.withCushion then
        tl:at(T_CUSHION + CUSHION_DROP, function()
            self.cushionLandT = 0
            self.A:play("cushion")
            self:dustRing(CHEST_X, CUSHION_BOTTOM - 30, 12, 260)
        end)
    end
    tl:at(T_CHEST + CHEST_DROP, function()
        self.landT, self.shakeT, self.cushionLandT = 0, 0, 0
        self.A:play(self.cue.land)
        self:dustRing(CHEST_X, self:seatY(), 14, 320)
    end)
    tl:at(T_LATCH, function() self.A:play(self.cue.latch) end)
    tl:at(T_OPEN, function() self.A:play(self.cue.open) end)
    tl:at(self.tPop, function()
        self.popT, self.shakeT, self.cushionLandT = 0, 0, 0
        local mouth = self:seatY() - (BASE_ROW - MOUTH_ROW)
        if self.attic then
            -- a cloud of dust and drifting motes instead of light
            for _ = 1, 18 do
                self.dust:emit{ x = CHEST_X + (random() - 0.5) * 200, y = mouth + random() * 40, vx = (random() - 0.5) * 220, vy = -(60 + random() * 160), drag = 1.8,
                                life = 1.2 + random() * 0.8, s0 = 0.4, s1 = 1.6, rot = random() * 360, spin = (random() - 0.5) * 90, color = ATTIC_DUST, alpha = 0.55 }
            end
            self.motes:burst(CHEST_X, mouth, 30, { angle = -pi / 2, spread = 2.4, speed = 160, size = 0.12, size1 = 0.05, life = 2.4, color = { 230, 214, 180 }, gravity = 20, fade = "hump" })
        else
            self.A:play("sparkle")
            if self.rainbow then
                for i = 0, 5 do
                    self.sparks:burst(CHEST_X, mouth, 8, { angle = -pi / 2, spread = 1.8, speed = 560, size = 0.4, life = 1.1, color = Fx.rainbow(i / 6, 0.1), gravity = 380 })
                end
            else
                self.sparks:burst(CHEST_X, mouth, 44, { angle = -pi / 2, spread = 1.8, speed = 560, size = 0.4, life = 1.1, color = Fx.rarityColor(key), gravity = 380 })
            end
        end
        self:dustRing(CHEST_X, self:seatY(), 10, 240)
    end)
    tl:at(self.tFlash, function() self:flash() end)
end

local function cushionHeight(A)
    local h = A.tex.cushion and A.tex.cushion.Height or 0
    if h <= 0 then h = 300 end          -- the shipped cushion, until an async load reports its size
    return h
end

-- the y the chest's base rests on: the cushion's top face, or the ground
function M:seatY()
    if not self.withCushion then return GROUND_SEAT end
    local h = cushionHeight(self.A)
    return CUSHION_BOTTOM - h + h * CUSHION_SEAT
end

function M:color()
    if self.attic then return ATTIC_DUST end
    return Fx.rarityColor(self.key, self.t)
end

-- the attic's dust motes: a slow, steady drift up through the haze
function M:driftMotes(dt)
    self.moteAcc = self.moteAcc + dt * 14
    while self.moteAcc >= 1 do
        self.moteAcc = self.moteAcc - 1
        self.motes:emit{ x = random() * 1920, y = 200 + random() * 900, vx = (random() - 0.5) * 24, vy = -(8 + random() * 22),
                         life = 3 + random() * 3, s0 = 0.05 + random() * 0.09, s1 = 0.04, rot = random() * 360, spin = 40,
                         color = { 232, 218, 186 }, fade = "hump", alpha = 0.6 }
    end
end

function M:dustRing(x, y, n, speed)
    for i = 1, n do
        local a = (i / n) * 2 * pi
        self.dust:emit{ x = x, y = y, vx = cos(a) * speed, vy = sin(a) * speed * 0.25 - 40, drag = 2.5,
                        life = 0.6 + random() * 0.3, s0 = 0.5, s1 = 1.4, rot = random() * 360, spin = (random() - 0.5) * 120,
                        color = { 235, 225, 215 }, alpha = 0.7 }
    end
end

function M:flash()
    if self.flashT ~= nil then return end
    self.flashT = 0
    self.A:play("flash")
end

-- a decide press skips whatever is left of the reveal
function M:skip()
    if self.flashT ~= nil then return end
    self.tl:jump(100)
    self:flash()
end

-- returns true once the screen has settled to black
function M:update(dt, decide)
    self.t = self.t + dt
    self.tl:advance(dt)
    self.wheelRot = (self.wheelRot + WHEEL_SPEED * dt) % 360
    self.cushionLandT, self.landT = self.cushionLandT + dt, self.landT + dt
    self.popT, self.shakeT = self.popT + dt, self.shakeT + dt
    self.smoke:update(dt) ; self.smoke2:update(dt) ; self.dust:update(dt) ; self.sparks:update(dt) ; self.motes:update(dt)
    if self.attic then self:driftMotes(dt) end
    if decide then self:skip() end
    if self.flashT ~= nil then
        self.flashT = self.flashT + dt
        return self.flashT >= FLASH_IN + FLASH_HOLD + FLASH_TO_BLACK + BLACK_HOLD
    end
    return false
end

-- the shadow on the ground, gathering under whatever is coming down
function M:drawShadow(fade, sx, sy, groundY, height, width)
    local k = min(1, height / 500)
    Fx.draw(self.A.tex.glow, CHEST_X + sx, groundY + sy, { sx = width - 0.4 * k, sy = 0.36, opacity = fade * (0.6 - 0.35 * k), color = { 0, 0, 0 } })
end

-- the cushion: its drop and the squash whenever something lands on it
function M:drawCushion(fade, sx, sy)
    local A, t = self.A, self.tl.t
    local ck = Fx.span(t, T_CUSHION, CUSHION_DROP)
    if ck <= 0 then return 0 end
    local cush = A.tex.cushion
    local h = cushionHeight(A)
    local bottom = Fx.lerp(DROP_FROM, CUSHION_BOTTOM, Fx.inQuad(ck))
    local sq = Fx.hump(Fx.span(self.cushionLandT, 0, 0.3)) * 0.16
    self:drawShadow(fade, sx, sy, CUSHION_BOTTOM - 40, CUSHION_BOTTOM - bottom, 1.7)
    Fx.draw(cush, CHEST_X + sx, bottom + sy, { anchor = "bottom", sx = 1 + sq * 0.7, sy = 1 - sq, opacity = fade })
    return h * sq * (1 - CUSHION_SEAT)      -- how much lower the seat sits while the cushion is squashed
end

-- the chest: the drop, the landing squash, the latch jiggle, the rattling wind-up, the pop open
function M:drawChest(fade, sx, sy, sink)
    local A, t = self.A, self.tl.t
    local dk = Fx.span(t, T_CHEST, CHEST_DROP)
    if dk <= 0 then return end
    local seat = self:seatY() + (sink or 0)
    local bottom = Fx.lerp(DROP_FROM, seat + (FRAME - BASE_ROW), Fx.inQuad(dk))
    local scx, scy, rot, dx, dy = 1, 1, 0, 0, 0
    if not self.withCushion then
        self:drawShadow(fade, sx, sy, seat, seat + (FRAME - BASE_ROW) - bottom, 1.35)
    end

    local land = Fx.hump(Fx.span(self.landT, 0, 0.28)) * 0.10
    scx, scy = scx * (1 + land * 0.5), scy * (1 - land)

    if t >= T_LATCH and t < T_OPEN then
        local k = 1 - Fx.span(t, T_LATCH, T_OPEN - T_LATCH)
        dx = sin((t - T_LATCH) * 60) * 2.5 * k
    end

    local frames = max(1, floor((self.chestTex.Width or 0) / FRAME))
    local frame = 0
    if t >= T_OPEN and t < self.tPop then
        -- the wind-up: the rattle builds, the chest squats down ready to spring
        local k = Fx.span(t, T_OPEN, self.windup)
        dx = dx + sin(t * 95) * 8 * k
        rot = sin(t * 71) * 3.5 * k
        scx, scy = scx * (1 + 0.10 * k * k), scy * (1 - 0.12 * k * k)
        if frames > 2 then frame = min(frames - 2, floor(k * (frames - 1))) end
    elseif t >= self.tPop then
        -- the pop: open frame, a stretch that overshoots and settles, a hop and a bounce
        frame = frames - 1
        local p = Fx.span(self.popT, 0, 0.45)
        local stretch = 1 + 0.48 * (1 - Fx.outBack(p, 2.4)) * (p < 1 and 1 or 0)
        local settle = Fx.outBack(p, 2.0)
        scy = scy * (0.88 + 0.12 * settle) * stretch
        scx = scx * (1.12 - 0.12 * settle) / (stretch > 1 and (0.5 + stretch * 0.5) or 1)
        dy = -Fx.hump(Fx.span(self.popT, 0, 0.34)) * 58
        rot = sin(self.popT * 30) * 2 * (1 - Fx.span(self.popT, 0, 0.5))
    end

    -- the light inside, before the chest so it spills out of the mouth as the lid lifts (the
    -- attic only stirs up a dim haze)
    if t >= self.tPop then
        local g = Fx.outQuad(Fx.span(self.popT, 0, 0.6))
        local flare = 1 + 1.2 * Fx.hump(Fx.span(self.popT, 0, 0.25))
        local strength = self.attic and 0.35 or 1
        Fx.draw(A.tex.glow, CHEST_X + sx, seat - (BASE_ROW - MOUTH_ROW) + sy + dy - 110 * g, { s = (0.9 + 3.0 * g) * flare, opacity = fade * min(1, g * 1.5) * strength, color = self:color(), blend = "add" })
    end
    Fx.draw(self.chestTex, CHEST_X + sx + dx, bottom + sy + dy, { rx = frame * FRAME, ry = 0, rw = FRAME, rh = FRAME, anchor = "bottom",
                                                                 sx = scx, sy = scy, rot = rot, opacity = fade })
end

function M:draw()
    local A = self.A
    local fade = Fx.span(self.t, 0, 0.45)
    Fx.fill(A.fillCv, { 0, 0, 0 }, 0.82 * fade)
    self.smoke:draw(fade) ; self.smoke2:draw(fade)
    if self.attic then
        self.motes:draw(fade)
    else
        -- the wheel of light turns through the smoke
        local wheelCol = self.rainbow and self:color() or nil
        Fx.draw(A.tex.wheel, 960, 540, { s = 1.7, rot = self.wheelRot, opacity = 0.18 * fade, blend = "add", color = wheelCol })
        Fx.draw(A.tex.wheel, 960, 540, { s = 1.2, rot = -self.wheelRot * 0.6, opacity = 0.10 * fade, blend = "add", color = wheelCol })
    end
    local sx, sy = Fx.shake(9, self.shakeT, 0.35)
    local sink = self.withCushion and self:drawCushion(fade, sx, sy) or 0
    self:drawChest(fade, sx, sy, sink)
    self.dust:draw(fade)
    self.sparks:draw(fade)

    if self.flashT ~= nil then
        local ft = self.flashT
        local white = Fx.outQuad(Fx.span(ft, 0, FLASH_IN))
        Fx.fill(A.fillCv, { 255, 255, 255 }, white)
        local dark = Fx.inQuad(Fx.span(ft, FLASH_IN + FLASH_HOLD, FLASH_TO_BLACK))
        Fx.fill(A.fillCv, { 0, 0, 0 }, dark)
    end
end

function M:stop()
    self.smoke:clear() ; self.smoke2:clear() ; self.dust:clear() ; self.sparks:clear() ; self.motes:clear()
end

return M
