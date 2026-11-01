-- Level 20 destiny sequence on the mystery bar; the caller places and draws the bar.

local Easing = require("Easing")
local data   = require("encounter/songbars_data")
local text   = require("encounter/bar_text")
local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")

local D = {}
D.__index = D

local Look = Layout.encounter.destiny
local Anim = Animation.encounter.destiny
local Shard = Anim.shard
local CENTRE_X, CENTRE_Y, ZOOM = Look.centre_x, Look.centre_y, Look.zoom
local ZOOM_IN = Anim.zoom_in_s
local CRACK_AT = Anim.crack_at_s
local SHAKE = Anim.shake.px                     -- px per crack, decays over SHAKE_SEC
local SHAKE_SEC = Anim.shake.s
local SHAKE_RATE_X, SHAKE_RATE_Y, SHAKE_SHARE_Y = Anim.shake.x_rad_per_s, Anim.shake.y_rad_per_s, Anim.shake.y_share
local SHATTER_AT = Anim.shatter_at_s
local POP_AT, POP_STEP, POP_SEC = Anim.pop_at_s, Anim.pop_step_s, Anim.pop_s   -- s; letter pop-in after the shatter
local FLAVOR_AT, FLAVOR_SEC = Anim.flavor_at_s, Anim.flavor_s
local PING_AT = Anim.ping_at_s
local ZOOM_OUT_AT, ZOOM_OUT = Anim.zoom_out_at_s, Anim.zoom_out_s
local DIM = Look.dim_opacity

local SHARD_SPEED = Shard.speed_px_per_s        -- px/s at the zoomed scale
local SHARD_SCATTER = Shard.scatter_px_per_s
local SHARD_VY = Shard.vy_share
local SHARD_LIFT = Shard.lift_px_per_s
local SHARD_GRAVITY = Shard.gravity_px_per_s2
local SHARD_SPIN = Shard.spin_deg_per_s
local SHARD_LIFE = Shard.life_s
local SHARD_FADE, SHARD_SHRINK = Shard.fade_from, Shard.shrink
local FLASH_SEC, FLASH_ALPHA = Anim.flash_s, Look.flash_opacity
local FLASH = Look.flash_color
local FLASH_W, FLASH_H = Look.flash_w_share, Look.flash_h_share

local random = math.random

local function rnd(r) return r[1] + random() * (r[2] - r[1]) end

-- bar: the mystery bar (encounter/song_reveal + encounter/song_bar state); play(name) plays a sound
-- ("zoom", "crack1".."crack3", "shatter", "ping"); tint: the shell's colour; flavor: the flavour line
function D.new(bar, play, tint, flavor)
    local d = setmetatable({ bar = bar, play = play, tint = tint, flavor = flavor,
        t = 0, cracks = 0, shattered = false, pinged = false,
        shakes = {}, shards = {}, flashT = nil }, D)
    if play ~= nil then play("zoom") end
    return d
end

-- 0 at the bar's place, 1 at screen centre
function D:zoom()
    local t = self.t
    if t < ZOOM_IN then return Easing.inOutCubic(t / ZOOM_IN) end
    if t < ZOOM_OUT_AT then return 1 end
    return 1 - Easing.inOutCubic((t - ZOOM_OUT_AT) / ZOOM_OUT)
end

function D:dim() return DIM * self:zoom() end

function D:done() return self.t >= ZOOM_OUT_AT + ZOOM_OUT end

local function shatter(self)
    local b = self.bar
    local xf = b.xf
    self.shattered, self.flashT = true, 0
    b.dark, b.revealed, b.named = false, true, true
    b.flavor, b.flavorA = self.flavor, 0
    if b.flames ~= nil then b.flames:stop() end
    local t0 = self.t
    b.pops = function(i)
        local u = (self.t - t0 - POP_AT - (i - 1) * POP_STEP) / POP_SEC
        if u <= 0 then return 0 end
        return Easing.outCubic(u)
    end
    if xf == nil then return end
    local ix, iy = data.impact.x, data.impact.y
    for _, s in ipairs(data.shards) do
        local x, y = text.point(xf, s.x, s.y)
        local dx, dy = s.x - ix, s.y - iy
        local len = math.sqrt(dx * dx + dy * dy)
        if len < 1 then dx, dy, len = random() - 0.5, -1, 1 end
        local v = rnd(SHARD_SPEED) * xf.s / ZOOM
        self.shards[#self.shards + 1] = {
            key = s.key, x = x, y = y,
            vx = dx / len * v + (random() - 0.5) * SHARD_SCATTER, vy = dy / len * v * SHARD_VY - SHARD_LIFT,
            rot = xf.rot, spin = rnd(SHARD_SPIN) * (random() < 0.5 and -1 or 1),
            age = 0, life = rnd(SHARD_LIFE), s = xf.s,
        }
    end
    if self.play ~= nil then self.play("shatter") end
end

function D:update(dt)
    self.t = self.t + dt
    local t = self.t
    local b = self.bar
    while self.cracks < #CRACK_AT and t >= CRACK_AT[self.cracks + 1] do
        self.cracks = self.cracks + 1
        b.crack = self.cracks
        self.shakes[#self.shakes + 1] = { t = 0, amp = SHAKE[self.cracks] }
        if self.play ~= nil then self.play("crack" .. self.cracks) end
    end
    if not self.shattered and t >= SHATTER_AT then shatter(self) end
    if self.shattered then
        local since = t - SHATTER_AT
        b.flavorA = math.max(0, math.min(1, (since - FLAVOR_AT) / FLAVOR_SEC))
        if not self.pinged and since >= PING_AT then
            self.pinged = true
            if self.play ~= nil then self.play("ping") end
        end
    end
    if self:done() then b.pops = nil end
    for _, s in ipairs(self.shakes) do s.t = s.t + dt end
    if self.flashT ~= nil then self.flashT = self.flashT + dt end
    local i = 1
    while i <= #self.shards do
        local p = self.shards[i]
        p.age = p.age + dt
        if p.age >= p.life then
            table.remove(self.shards, i)
        else
            p.vy = p.vy + SHARD_GRAVITY * dt
            p.x, p.y = p.x + p.vx * dt, p.y + p.vy * dt
            p.rot = p.rot + p.spin * dt
            i = i + 1
        end
    end
end

-- shake offset this frame
local function shake(self)
    local ox, oy = 0, 0
    for _, s in ipairs(self.shakes) do
        if s.t < SHAKE_SEC then
            local k = s.amp * (1 - s.t / SHAKE_SEC) ^ 2
            ox = ox + math.sin(s.t * SHAKE_RATE_X) * k
            oy = oy + math.cos(s.t * SHAKE_RATE_Y) * k * SHAKE_SHARE_Y
        end
    end
    return ox, oy
end

-- writes into out (x, y, s, rot) the bar's transform from its place `slot` this frame
function D:place(slot, out)
    local k = self:zoom()
    local ox, oy = shake(self)
    out.x = slot.x + (CENTRE_X - slot.x) * k + ox
    out.y = slot.y + (CENTRE_Y - slot.y) * k + oy
    out.s = slot.s + (ZOOM - slot.s) * k
    out.rot = slot.rot * (1 - k)
    return out
end

-- draws the shards and the flash; tex: key -> bar texture handle, glow: fx_glow
function D:draw(tex, glow)
    for _, p in ipairs(self.shards) do
        local tx = tex[p.key]
        if tx ~= nil and tx.Ready then
            local u = p.age / p.life
            local a = (u < SHARD_FADE) and 1 or (1 - (u - SHARD_FADE) / (1 - SHARD_FADE))
            local s = p.s * (1 - SHARD_SHRINK * u)
            tx:SetScale(s, s)
            tx:SetRotation(p.rot)
            tx:SetColor(self.tint[1], self.tint[2], self.tint[3])
            tx:SetOpacity(math.max(0, a))
            tx:DrawAtAnchor(p.x, p.y, "center")
            tx:SetScale(1, 1) ; tx:SetRotation(0) ; tx:SetColor(1, 1, 1) ; tx:SetOpacity(1)
        end
    end
    local f = self.flashT
    if f ~= nil and f < FLASH_SEC and glow ~= nil and glow.Ready and self.bar.xf ~= nil then
        local xf = self.bar.xf
        local u = f / FLASH_SEC
        local w, h = data.bar.w * xf.s * FLASH_W, data.bar.h * xf.s * FLASH_H
        glow:SetBlendMode("Add")
        glow:SetColor(FLASH[1], FLASH[2], FLASH[3])
        glow:SetScale(w / math.max(1, glow.Width), h / math.max(1, glow.Height))
        glow:SetOpacity(FLASH_ALPHA * (1 - u) * (1 - u))
        glow:DrawAtAnchor(xf.x, xf.y, "center")
        glow:SetScale(1, 1) ; glow:SetColor(1, 1, 1) ; glow:SetOpacity(1) ; glow:SetBlendMode("Normal")
    end
end

return D
