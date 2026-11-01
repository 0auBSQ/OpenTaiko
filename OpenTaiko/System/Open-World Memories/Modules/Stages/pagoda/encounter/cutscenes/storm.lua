-- Level-20 storm effect.

local Easing = require("Easing")
local view   = require("encounter/bands/view")
local Config = require("Core.Config")
local Layout    = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")

local Storm = {}
Storm.__index = Storm

local Look = Layout.encounter.cutscenes.storm
local Anim = Animation.encounter.cutscenes.storm

local REF_W, REF_H = 1920, 1080
local CYCLE_SEC = Anim.cycle_s
-- per strike: time, strength, bolt (a near strike with its clap; the others are far)
local STRIKES = {}
for i, s in ipairs(Anim.strikes) do STRIKES[i] = { s.at_s, s.flash_opacity, s.bolt } end
local THUNDER_VOL, FAR_VOL = 100, 85
local FLASH_SEC = Anim.flash_s
local BOLT_SEC  = Anim.bolt_s
local BOLT_X    = Look.bolt_x
local BOLT_SCALE = Look.bolt_scale
local FLASH_ENV = Anim.flash_env

local LAYERS = {}
for i, l in ipairs(Look.rain_layers) do  -- far, near
    LAYERS[i] = { n = l.count, speed = Anim.rain_px_per_s[i], scale = l.scale, alpha = l.opacity }
end
local SPAWN_X = { Look.spawn_left_px, REF_W + Look.spawn_right_over_px }  -- wide to the right: the rain drifts left
local SPAWN_ABOVE, SPAWN_ABOVE_RANGE = Look.spawn_above_min_px, Look.spawn_above_range_px
local FILL_EXTRA, FILL_TOP = Look.fill_extra_px, Look.fill_top_px
local SLANT, GUST_SLANT = Anim.slant, Anim.gust_slant  -- rain dx per dy
local GUST_RISE, GUST_FALL = Anim.gust_rise_s, Anim.gust_fall_s

local CLOUD_SPEED = Anim.cloud_px_per_s
local CLOUD_GUST  = Anim.cloud_gust_speedup    -- speed factor at a gust's peak
local CLOUD_DARK  = Look.cloud_brightness      -- brightness of the strip while stormy
local CLOUD_ALPHA = Look.cloud_opacity

local function rnd(r) return r[1] + math.random() * (r[2] - r[1]) end

local function reset(tx)
    tx:SetScale(1, 1) ; tx:SetRotation(0) ; tx:SetOpacity(1)
    tx:SetColor(1, 1, 1) ; tx:SetBlendMode("Normal")
end

-- h: the height the rain falls through, from the top of the screen
local function spawn(d, layer, top, h)
    d.x = rnd(SPAWN_X)
    d.y = top and -(SPAWN_ABOVE + math.random() * SPAWN_ABOVE_RANGE) or (math.random() * (h + FILL_EXTRA) - FILL_TOP)
    d.vy = rnd(layer.speed)
    d.s = rnd(layer.scale)
    d.a = rnd(layer.alpha)
    d.live = true
end

-- tex = { rain, bolt, white, cloud }; cloud = { w, y, scale } (strip width, draw y in 1920 space, scale);
-- snd = { rain, thunder, far }, each may be nil; height: the rain only falls through the top this tall
-- (1080 space, whole screen when absent); boltX: x ranges a bolt picks from (1920 space, BOLT_X when absent)
function Storm.new(tex, cloud, snd, height, boltX)
    snd = snd or {}
    local self = setmetatable({ tex = tex, cloud = cloud, loop = snd.rain, thunder = snd.thunder, far = snd.far,
                                height = height or REF_H, boltX = boltX or { BOLT_X } }, Storm)
    local share = (self.height + FILL_EXTRA) / (REF_H + FILL_EXTRA)
    if self.thunder ~= nil then self.thunder:SetVolumePercent(THUNDER_VOL) end
    if self.far ~= nil then self.far:SetVolumePercent(FAR_VOL) end
    self.drops = {}
    for i, layer in ipairs(LAYERS) do
        local list = {}
        for j = 1, math.ceil(layer.n * share) do list[j] = {} ; spawn(list[j], layer, false, self.height) end
        self.drops[i] = list
    end
    self.clock, self.vol = 0, 0
    self.cloudX = 0
    self.flashT, self.flashA, self.boltAt, self.boltS, self.bolt = FLASH_SEC, 0, 0, 1, false
    self.calmT, self.calmSec = nil, 0
    self.gustT, self.gustSec = nil, 0
    self.slant, self.cloudK = SLANT, 0
    return self
end

-- the rain loop starts at vol percent
function Storm:start(vol)
    self.vol = vol or 100
    self.clock = 0
    if self.loop ~= nil then
        self.loop:SetLoop(true)
        self.loop:SetVolumePercent(self.vol)
        self.loop:Play()
    end
end

-- the storm dies down over sec seconds
function Storm:calm(sec)
    self.calmT, self.calmSec = 0, math.max(0.01, sec or Anim.default_calm_s)
end

-- a gust of wind over sec seconds
function Storm:gust(sec)
    self.gustT, self.gustSec = 0, math.max(GUST_RISE + GUST_FALL, sec or Anim.default_gust_s)
end

-- 0 while stormy, 1 once calm
function Storm:calmness()
    if self.calmT == nil then return 0 end
    return math.min(1, self.calmT / self.calmSec)
end

local function strike(self, s)
    if self:calmness() > 0 then return end
    self.flashT, self.flashA = 0, s[2]
    self.bolt = s[3]
    local n = #self.boltX
    self.boltAt, self.boltS = rnd(self.boltX[(n > 1) and math.random(n) or 1]), rnd(BOLT_SCALE)
    local thunder = s[3] and self.thunder or self.far
    if thunder ~= nil then thunder:Play() end
end

local function stepGust(self, dt)
    if self.gustT == nil then self.slant, self.cloudK = SLANT, 0 ; return end
    self.gustT = self.gustT + dt
    local t, sec = self.gustT, self.gustSec
    local k
    if t < GUST_RISE then k = Easing.outQuad(t / GUST_RISE)
    elseif t < sec - GUST_FALL then k = 1
    else k = 1 - Easing.inOutSine((t - (sec - GUST_FALL)) / GUST_FALL) end
    if t >= sec then self.gustT, k = nil, 0 end
    self.slant = SLANT + (GUST_SLANT - SLANT) * k
    self.cloudK = k
end

function Storm:update(dt)
    local prev = self.clock % CYCLE_SEC
    self.clock = self.clock + dt
    local now = self.clock % CYCLE_SEC
    for _, s in ipairs(STRIKES) do
        local crossed
        if now >= prev then crossed = s[1] > prev and s[1] <= now
        else crossed = s[1] > prev or s[1] <= now end
        if crossed then strike(self, s) end
    end
    self.flashT = self.flashT + dt
    stepGust(self, dt)

    local calm = self:calmness()
    if self.calmT ~= nil then
        self.calmT = self.calmT + dt
        if self.loop ~= nil then
            if calm >= 1 then
                if self.loop.IsPlaying then self.loop:Stop() end
            else
                self.loop:SetVolumePercent(self.vol * (1 - Easing.inQuad(calm)))
            end
        end
    end

    for i, layer in ipairs(LAYERS) do
        for _, d in ipairs(self.drops[i]) do
            if d.live then
                d.y = d.y + d.vy * dt
                d.x = d.x + d.vy * self.slant * dt
                if d.x < SPAWN_X[1] - Look.wrap_margin_px then d.x = d.x + (SPAWN_X[2] - SPAWN_X[1]) end
                if d.y > self.height + Look.fall_past_px then
                    if self.calmT == nil then spawn(d, layer, true, self.height) else d.live = false end
                end
            end
        end
    end
    local speed = CLOUD_SPEED * (1 + (CLOUD_GUST - 1) * self.cloudK) * (1 - Anim.calm_cloud_slow * calm)
    self.cloudX = self.cloudX + speed * dt
end

local function flashEnv(t)
    local e = FLASH_ENV
    if t < e.rise_s then return t / e.rise_s end
    if t < e.dip_at_s then return 1 - (1 - e.dip) * (t - e.rise_s) / (e.dip_at_s - e.rise_s) end
    if t < e.back_at_s then return e.dip + (e.back - e.dip) * (t - e.dip_at_s) / (e.back_at_s - e.dip_at_s) end
    if t < FLASH_SEC then return e.back * (1 - (t - e.back_at_s) / (FLASH_SEC - e.back_at_s)) end
    return 0
end

-- behind the figure; the shake is in view.xf
function Storm:drawBack()
    local bolt = self.tex.bolt
    if self.bolt and self.flashT < BOLT_SEC and bolt ~= nil and bolt.Ready then
        local a = math.min(1, flashEnv(self.flashT) * Look.bolt_opacity_scale)
        if a > 0.002 then
            local x, y = view.xf(self.boltAt, Look.bolt_y)
            local s = view.scale() * self.boltS
            bolt:SetBlendMode("Add")
            bolt:SetScale(s, s)
            bolt:SetOpacity(a)
            bolt:DrawAtAnchor(x, y, "top")
            reset(bolt)
        end
    end
    local tx, m = self.tex.cloud, self.cloud
    if tx == nil or not tx.Ready or m == nil then return end
    local calm = Easing.inOutSine(self:calmness())
    local c = CLOUD_DARK + (1 - CLOUD_DARK) * calm
    local a = CLOUD_ALPHA + (1 - CLOUD_ALPHA) * calm
    local w = m.w * m.scale
    local s = view.scale() * m.scale
    tx:SetScale(s, s)
    tx:SetColor(c, c, c)
    tx:SetOpacity(a)
    local x = (self.cloudX % w) - w
    while x < REF_W do
        local sx, sy = view.xf(x, m.y)
        tx:DrawAtAnchor(sx, sy, "topleft")
        x = x + w
    end
    reset(tx)
end

-- i = 1 far, 2 near; screen space with the shake offset
function Storm:drawRain(i, ox, oy)
    local tx = self.tex.rain
    local list = self.drops[i]
    if tx == nil or not tx.Ready or list == nil then return end
    local fade = 1 - self:calmness()
    if fade <= 0 then return end
    local res = THEME:GetResolution()
    local k = res.X / REF_W
    tx:SetRotation(math.deg(math.atan(self.slant, 1)))
    for _, d in ipairs(list) do
        if d.live then
            local a = d.a * fade
            if a > 0.002 then
                tx:SetScale(k * Look.rain_width_scale, d.s * k)
                tx:SetOpacity(math.min(1, a))
                tx:DrawAtAnchor(d.x * k + ox, d.y * k + oy, "center")
            end
        end
    end
    reset(tx)
end

function Storm:drawFlash()
    local tx = self.tex.white
    if tx == nil or not tx.Ready then return end
    local a = self.flashA * flashEnv(self.flashT)
    if a <= 0.002 then return end
    local res = THEME:GetResolution()
    tx:SetBlendMode("Add")
    tx:SetScale(res.X / math.max(1, tx.Width), res.Y / math.max(1, tx.Height))
    local fc = Look.flash_color
    tx:SetColor(fc[1], fc[2], fc[3])
    tx:SetOpacity(math.min(1, a))
    tx:Draw(0, 0)
    reset(tx)
end

return Storm
