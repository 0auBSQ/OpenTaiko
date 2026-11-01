-- Level-20 star exit; figure() says how to draw the shrinking figure.

local Easing = require("Easing")
local view   = require("encounter/bands/view")
local Light  = require("fx/light")
local sprite = require("encounter/sprite")
local Config = require("Core.Config")
local Layout    = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")

local Star = {}
Star.__index = Star

local Look = Layout.encounter.cutscenes.star
local Anim = Animation.encounter.cutscenes.star

local SHRINK_SEC = Anim.shrink_s
local POP_AT, POP_SEC = Anim.pop_at_s, Anim.pop_s
local FLY_AT, FLY_SEC = Anim.fly_at_s, Anim.fly_s
local DONE_AT = Anim.done_at_s         -- busy until then
local FLY_DX, FLY_TO_Y = Anim.fly_dx_px, Anim.fly_to_y  -- flight end: dx from the start x, y in screen space
local STAR_SCALE = Look.star_scale
local TWINKLE_HZ, TWINKLE_AMP = Anim.twinkle_hz, Anim.twinkle_amp
local SPIN = Anim.spin_deg_per_s
local GLOW_SCALE, GLOW_A = Look.glow_scale, Look.glow_opacity
local TRAIL_EVERY, TRAIL_LIFE = Anim.trail_every_s, Anim.trail_life_s
local TRAIL_SCALE = Look.trail_scale
local TRAIL_JITTER = Anim.trail_jitter_px
local TRAIL_FALL = Anim.trail_fall_px_per_s
local TINT = Look.tint_color

local function rnd(r) return r[1] + math.random() * (r[2] - r[1]) end

-- tex = { star, glow, sparkle }; sfx = { ping, fly }
function Star.new(tex, sfx)
    local self = setmetatable({ tex = tex, sfx = sfx or {} }, Star)
    self.t, self.left = nil, false
    self.trail, self.trailAcc = {}, 0
    return self
end

-- (x, y) = the figure's eye in 1920 space
function Star:start(x, y)
    self.t, self.x0, self.y0 = 0, x, y
    self.left = false
    self.x, self.y = x, y
    self.pinged, self.flown = false, false
    self.trail, self.trailAcc = {}, 0
end

function Star:busy()
    return self.t ~= nil and self.t < DONE_AT
end

-- returns the scale about the eye, opacity and white burn (0..1)
function Star:figure()
    if self.t == nil then return 1, 1, 0 end
    local u = math.min(1, self.t / SHRINK_SEC)
    return 1 - Anim.figure_shrink * Easing.inCubic(u), 1 - Easing.inQuad(u), Easing.outQuad(u)
end

-- true once the figure has shrunk away
function Star:gone()
    return self.left
end

function Star:update(dt)
    if self.t == nil then return end
    self.t = self.t + dt
    local t = self.t
    if t >= SHRINK_SEC then self.left = true end
    if not self.pinged and t >= POP_AT then
        self.pinged = true
        if self.sfx.ping ~= nil then self.sfx.ping:Play() end
    end
    if not self.flown and t >= FLY_AT then
        self.flown = true
        if self.sfx.fly ~= nil then self.sfx.fly:Play() end
    end
    if t >= FLY_AT then
        local u = math.min(1, (t - FLY_AT) / FLY_SEC)
        self.x = self.x0 + FLY_DX * Easing.outSine(u)
        self.y = self.y0 + (FLY_TO_Y - self.y0) * Easing.inQuad(u)
        if u < 1 then
            self.trailAcc = self.trailAcc + dt
            while self.trailAcc >= TRAIL_EVERY do
                self.trailAcc = self.trailAcc - TRAIL_EVERY
                self.trail[#self.trail + 1] = { x = self.x + rnd({ -TRAIL_JITTER, TRAIL_JITTER }),
                                                y = self.y + rnd({ -TRAIL_JITTER, TRAIL_JITTER }),
                                                s = rnd(TRAIL_SCALE), age = 0, vy = rnd(TRAIL_FALL) }
            end
        end
    end
    local i = 1
    while i <= #self.trail do
        local p = self.trail[i]
        p.age = p.age + dt
        p.y = p.y + p.vy * dt
        if p.age >= TRAIL_LIFE then
            self.trail[i] = self.trail[#self.trail]
            self.trail[#self.trail] = nil
        else
            i = i + 1
        end
    end
    if t >= FLY_AT + FLY_SEC and #self.trail == 0 then self.t = nil end
end

-- the shake is in view.xf
function Star:draw()
    if self.t == nil then return end
    local t = self.t
    local k = view.scale()
    local g = (t < POP_AT) and Easing.inQuad(t / POP_AT) or math.max(0, 1 - (t - POP_AT) / (FLY_AT + FLY_SEC - POP_AT))
    if g > 0.002 then
        local gx, gy = view.xf(self.x, self.y)
        local gs = GLOW_SCALE[1] + (GLOW_SCALE[2] - GLOW_SCALE[1]) * math.min(1, t / POP_AT)
        Light.glow(self.tex.glow, gx, gy, gs * k, TINT[1], TINT[2], TINT[3], GLOW_A * g)
    end
    local sp = self.tex.sparkle
    if sp ~= nil and sp.Ready and #self.trail > 0 then
        for _, p in ipairs(self.trail) do
            local u = p.age / TRAIL_LIFE
            local px, py = view.xf(p.x, p.y)
            local shrink = 1 - Anim.trail_shrink * u
            sprite.drawAt(sp, px, py, { sx = p.s * shrink * k, sy = p.s * shrink * k,
                                        alpha = 1 - u, r = TINT[1], g = TINT[2], b = TINT[3], blend = "Add" })
        end
    end
    if t >= POP_AT and t < FLY_AT + FLY_SEC then
        local pop = Easing.outBack(math.min(1, (t - POP_AT) / POP_SEC))
        local tw = 1 + TWINKLE_AMP * math.sin((t - POP_AT) * TWINKLE_HZ * 2 * math.pi)
        local u = (t >= FLY_AT) and (t - FLY_AT) / FLY_SEC or 0
        local s = STAR_SCALE * pop * tw * (1 - Anim.fly_shrink * u) * k
        local sx, sy = view.xf(self.x, self.y)
        sprite.drawAt(self.tex.star, sx, sy, { sx = s, sy = s, rot = (t - POP_AT) * SPIN,
                                               alpha = 1, r = TINT[1], g = TINT[2], b = TINT[3], blend = "Add" })
        local inner = Look.inner_star_scale
        sprite.drawAt(self.tex.star, sx, sy, { sx = s * inner, sy = s * inner, rot = -(t - POP_AT) * SPIN * Anim.inner_spin_share,
                                               alpha = Look.inner_star_opacity, blend = "Add" })
    end
end

return Star
