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
local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")

local M = {}
M.__index = M

local L = Layout.chest
local T = Animation.chest
local CHEST_X = L.x
local FRAME = L.frame
local BASE_ROW, MOUTH_ROW = L.base_row, L.mouth_row   -- the chest's base and where its light pours out, inside a frame
local CUSHION_BOTTOM = L.cushion_bottom        -- where the cushion's bottom row lands
local CUSHION_SEAT = L.cushion_seat            -- the chest's base sits this far down the cushion (its top face)
local GROUND_SEAT = L.ground_seat              -- where the chest's base rests without a cushion
local DROP_FROM = T.drop_from_y
local SMOKE_N = L.smoke.count
local WHEEL_SPEED = T.wheel_speed_deg_per_s    -- degrees per second

-- timeline (seconds)
local T_CUSHION, CUSHION_DROP = T.cushion_at_s, T.cushion_drop_s
local T_CHEST, CHEST_DROP = T.chest_at_s, T.chest_drop_s
local T_LATCH = T.latch_at_s
local T_OPEN = T.open_at_s
-- per rarity: how long the rattle builds before the lid pops, and how long the light is admired
local PACE = {}
for _, r in ipairs(Fx.RARITIES) do PACE[r] = { windup = T.pace[r].windup_s, admire = T.pace[r].admire_s } end
local FLASH_IN, FLASH_HOLD, FLASH_TO_BLACK, BLACK_HOLD = T.flash_in_s, T.flash_hold_s, T.flash_to_black_s, T.black_hold_s
local ATTIC_DUST = L.attic_dust_color
local SMOKE = Layout.chest.smoke
local SMOKE_T = Animation.chest.smoke
local SHADOW = Layout.chest.shadow
local WHEEL = Layout.chest.wheel
local DRIFT = Layout.chest.drift_motes
local DRIFT_T = Animation.chest.drift_motes
local RING_T = Animation.chest.dust_ring
local POP_DUST = Animation.chest.pop_dust
local POP_MOTES = Animation.chest.pop_motes
local POP_SPARKS = Animation.chest.pop_sparks

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
    local farthest = SMOKE.min_dist + SMOKE.dist_range
    for i = 1, SMOKE_N do
        local a = (i / SMOKE_N) * 2 * pi + random() * SMOKE_T.angle_jitter
        local d = SMOKE.min_dist + random() * SMOKE.dist_range
        local x, y = SMOKE.cx + cos(a) * d, SMOKE.cy + sin(a) * d * SMOKE.squash_y
        local pool = (i % 2 == 0) and self.smoke or self.smoke2
        local col = self.rainbow and Fx.rainbow(i / SMOKE_N, SMOKE.rainbow_pastel) or (self.attic and ATTIC_DUST or Fx.rarityColor(key))
        pool:emit{ x = x, y = y, vx = -cos(a) * (SMOKE_T.speed_min + random() * SMOKE_T.speed_range), vy = -sin(a) * SMOKE_T.vy_out - (SMOKE_T.rise_vy_min + random() * SMOKE_T.rise_vy_range),
                   life = SMOKE_T.life_s, s0 = SMOKE_T.size_min + random() * SMOKE_T.size_range, s1 = SMOKE_T.size1_min + random() * SMOKE_T.size1_range, rot = random() * 360,
                   spin = (random() - 0.5) * SMOKE_T.spin_range, color = col, fade = SMOKE_T.fade, rise = SMOKE_T.rise_min + (1 - d / farthest) * SMOKE_T.rise_by_dist + random() * SMOKE_T.rise_range,
                   alpha = (self.attic and SMOKE_T.attic_alpha_min or SMOKE_T.alpha_min) + random() * (self.attic and SMOKE_T.attic_alpha_range or SMOKE_T.alpha_range) }
    end

    local tl = self.tl
    tl.cues = {}
    tl:reset()
    tl:at(0, function() self.A:play("puff") end)
    if self.withCushion then
        tl:at(T_CUSHION + CUSHION_DROP, function()
            self.cushionLandT = 0
            self.A:play("cushion")
            self:dustRing(CHEST_X, CUSHION_BOTTOM - L.cushion_dust_above, T.cushion_dust.count, T.cushion_dust.speed)
        end)
    end
    tl:at(T_CHEST + CHEST_DROP, function()
        self.landT, self.shakeT, self.cushionLandT = 0, 0, 0
        self.A:play(self.cue.land)
        self:dustRing(CHEST_X, self:seatY(), T.land_dust.count, T.land_dust.speed)
    end)
    tl:at(T_LATCH, function() self.A:play(self.cue.latch) end)
    tl:at(T_OPEN, function() self.A:play(self.cue.open) end)
    tl:at(self.tPop, function()
        self.popT, self.shakeT, self.cushionLandT = 0, 0, 0
        local mouth = self:seatY() - (BASE_ROW - MOUTH_ROW)
        if self.attic then
            -- a cloud of dust and drifting motes instead of light
            for _ = 1, POP_DUST.count do
                self.dust:emit{ x = CHEST_X + (random() - 0.5) * L.pop_dust_spread_x, y = mouth + random() * L.pop_dust_spread_y, vx = (random() - 0.5) * POP_DUST.vx_range, vy = -(POP_DUST.vy_min + random() * POP_DUST.vy_range), drag = POP_DUST.drag,
                                life = POP_DUST.life_min + random() * POP_DUST.life_range, s0 = POP_DUST.size, s1 = POP_DUST.size1, rot = random() * 360, spin = (random() - 0.5) * POP_DUST.spin_range, color = ATTIC_DUST, alpha = POP_DUST.alpha }
            end
            self.motes:burst(CHEST_X, mouth, POP_MOTES.count, { angle = -pi / 2, spread = POP_MOTES.spread, speed = POP_MOTES.speed, size = POP_MOTES.size, size1 = POP_MOTES.size1, life = POP_MOTES.life_s, color = L.motes_color, gravity = POP_MOTES.gravity, fade = POP_MOTES.fade })
        else
            self.A:play("sparkle")
            local ps = POP_SPARKS
            if self.rainbow then
                for i = 0, ps.rainbow_hues - 1 do
                    self.sparks:burst(CHEST_X, mouth, ps.rainbow_count, { angle = -pi / 2, spread = ps.spread, speed = ps.speed, size = ps.size, life = ps.life_s, color = Fx.rainbow(i / ps.rainbow_hues, L.sparks_rainbow_pastel), gravity = ps.gravity })
                end
            else
                self.sparks:burst(CHEST_X, mouth, ps.count, { angle = -pi / 2, spread = ps.spread, speed = ps.speed, size = ps.size, life = ps.life_s, color = Fx.rarityColor(key), gravity = ps.gravity })
            end
        end
        self:dustRing(CHEST_X, self:seatY(), T.pop_ring_dust.count, T.pop_ring_dust.speed)
    end)
    tl:at(self.tFlash, function() self:flash() end)
end

local function cushionHeight(A)
    local h = A.tex.cushion and A.tex.cushion.Height or 0
    if h <= 0 then h = L.cushion_fallback_h end   -- no cushion art: the shipped cushion's height
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
    self.moteAcc = self.moteAcc + dt * DRIFT_T.per_s
    while self.moteAcc >= 1 do
        self.moteAcc = self.moteAcc - 1
        self.motes:emit{ x = random() * DRIFT.w, y = DRIFT.y + random() * DRIFT.h, vx = (random() - 0.5) * DRIFT_T.vx_range, vy = -(DRIFT_T.vy_min + random() * DRIFT_T.vy_range),
                         life = DRIFT_T.life_min + random() * DRIFT_T.life_range, s0 = DRIFT_T.size_min + random() * DRIFT_T.size_range, s1 = DRIFT_T.size1, rot = random() * 360, spin = DRIFT_T.spin,
                         color = DRIFT.color, fade = DRIFT_T.fade, alpha = DRIFT_T.alpha }
    end
end

function M:dustRing(x, y, n, speed)
    for i = 1, n do
        local a = (i / n) * 2 * pi
        self.dust:emit{ x = x, y = y, vx = cos(a) * speed, vy = sin(a) * speed * RING_T.squash_y - RING_T.lift, drag = RING_T.drag,
                        life = RING_T.life_min + random() * RING_T.life_range, s0 = RING_T.size, s1 = RING_T.size1, rot = random() * 360, spin = (random() - 0.5) * RING_T.spin_range,
                        color = L.dust_ring_color, alpha = RING_T.alpha }
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
    local k = min(1, height / SHADOW.fade_height)
    Fx.draw(self.A.tex.glow, CHEST_X + sx, groundY + sy, { sx = width - SHADOW.shrink * k, sy = SHADOW.height_scale, opacity = fade * (SHADOW.opacity - SHADOW.opacity_drop * k), color = SHADOW.color })
end

-- the cushion: its drop and the squash whenever something lands on it
function M:drawCushion(fade, sx, sy)
    local A, t = self.A, self.tl.t
    local ck = Fx.span(t, T_CUSHION, CUSHION_DROP)
    if ck <= 0 then return 0 end
    local cush = A.tex.cushion
    local h = cushionHeight(A)
    local bottom = Fx.lerp(DROP_FROM, CUSHION_BOTTOM, Fx.inQuad(ck))
    local sq = Fx.hump(Fx.span(self.cushionLandT, 0, T.cushion_squash_s)) * T.cushion_squash
    self:drawShadow(fade, sx, sy, CUSHION_BOTTOM - SHADOW.cushion_above, CUSHION_BOTTOM - bottom, SHADOW.cushion_w)
    Fx.draw(cush, CHEST_X + sx, bottom + sy, { anchor = "bottom", sx = 1 + sq * T.cushion_squash_x, sy = 1 - sq, opacity = fade })
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
        self:drawShadow(fade, sx, sy, seat, seat + (FRAME - BASE_ROW) - bottom, SHADOW.ground_w)
    end

    local land = Fx.hump(Fx.span(self.landT, 0, T.land_squash_s)) * T.land_squash
    scx, scy = scx * (1 + land * T.land_squash_x), scy * (1 - land)

    if t >= T_LATCH and t < T_OPEN then
        local k = 1 - Fx.span(t, T_LATCH, T_OPEN - T_LATCH)
        dx = sin((t - T_LATCH) * T.latch_jiggle_freq) * T.latch_jiggle_px * k
    end

    local frames = max(1, floor((self.chestTex.Width or 0) / FRAME))
    local frame = 0
    if t >= T_OPEN and t < self.tPop then
        -- the wind-up: the rattle builds, the chest squats down ready to spring
        local k = Fx.span(t, T_OPEN, self.windup)
        dx = dx + sin(t * T.rattle_freq) * T.rattle_px * k
        rot = sin(t * T.rattle_rot_freq) * T.rattle_rot_deg * k
        scx, scy = scx * (1 + T.squat_x * k * k), scy * (1 - T.squat_y * k * k)
        if frames > 2 then frame = min(frames - 2, floor(k * (frames - 1))) end
    elseif t >= self.tPop then
        -- the pop: open frame, a stretch that overshoots and settles, a hop and a bounce
        frame = frames - 1
        local p = Fx.span(self.popT, 0, T.pop_s)
        local stretch = 1 + T.pop_stretch * (1 - Fx.outBack(p, T.pop_stretch_overshoot)) * (p < 1 and 1 or 0)
        local settle = Fx.outBack(p, T.pop_settle_overshoot)
        scy = scy * (T.pop_squash_y + T.pop_settle_y * settle) * stretch
        scx = scx * (T.pop_squash_x - T.pop_settle_x * settle) / (stretch > 1 and (0.5 + stretch * 0.5) or 1)
        dy = -Fx.hump(Fx.span(self.popT, 0, T.pop_hop_s)) * T.pop_hop_px
        rot = sin(self.popT * T.pop_wobble_freq) * T.pop_wobble_deg * (1 - Fx.span(self.popT, 0, T.pop_wobble_s))
    end

    -- the light inside, before the chest so it spills out of the mouth as the lid lifts (the
    -- attic only stirs up a dim haze)
    if t >= self.tPop then
        local g = Fx.outQuad(Fx.span(self.popT, 0, T.light_s))
        local flare = 1 + T.light_flare * Fx.hump(Fx.span(self.popT, 0, T.light_flare_s))
        local strength = self.attic and L.light_attic_opacity or 1
        Fx.draw(A.tex.glow, CHEST_X + sx, seat - (BASE_ROW - MOUTH_ROW) + sy + dy - L.light_rise_px * g, { s = (T.light_scale_start + T.light_scale_grow * g) * flare, opacity = fade * min(1, g * T.light_fade_in) * strength, color = self:color(), blend = "add" })
    end
    Fx.draw(self.chestTex, CHEST_X + sx + dx, bottom + sy + dy, { rx = frame * FRAME, ry = 0, rw = FRAME, rh = FRAME, anchor = "bottom",
                                                                 sx = scx, sy = scy, rot = rot, opacity = fade })
end

function M:draw()
    local A = self.A
    local fade = Fx.span(self.t, 0, T.fade_in_s)
    Fx.fill(A.fillCv, L.overlay_color, L.overlay_opacity * fade)
    self.smoke:draw(fade) ; self.smoke2:draw(fade)
    if self.attic then
        self.motes:draw(fade)
    else
        -- the wheel of light turns through the smoke
        local wheelCol = self.rainbow and self:color() or nil
        Fx.draw(A.tex.wheel, WHEEL.cx, WHEEL.cy, { s = WHEEL.scale, rot = self.wheelRot, opacity = WHEEL.opacity * fade, blend = "add", color = wheelCol })
        Fx.draw(A.tex.wheel, WHEEL.cx, WHEEL.cy, { s = WHEEL.inner_scale, rot = -self.wheelRot * T.wheel_inner_speed_ratio, opacity = WHEEL.inner_opacity * fade, blend = "add", color = wheelCol })
    end
    local sx, sy = Fx.shake(T.shake_px, self.shakeT, T.shake_s)
    local sink = self.withCushion and self:drawCushion(fade, sx, sy) or 0
    self:drawChest(fade, sx, sy, sink)
    self.dust:draw(fade)
    self.sparks:draw(fade)

    if self.flashT ~= nil then
        local ft = self.flashT
        local white = Fx.outQuad(Fx.span(ft, 0, FLASH_IN))
        Fx.fill(A.fillCv, L.flash_color, white)
        local dark = Fx.inQuad(Fx.span(ft, FLASH_IN + FLASH_HOLD, FLASH_TO_BLACK))
        Fx.fill(A.fillCv, L.settle_color, dark)
    end
end

function M:stop()
    self.smoke:clear() ; self.smoke2:clear() ; self.dust:clear() ; self.sparks:clear() ; self.motes:clear()
end

return M
