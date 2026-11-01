-- Full-screen corruption; start(sec, tail): full for sec seconds, gone tail seconds later.

local Easing = require("Easing")
local Config = require("Core.Config")
local Layout    = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")

local Corruption = {}
Corruption.__index = Corruption

local Look = Layout.encounter.cutscenes.corruption
local Anim = Animation.encounter.cutscenes.corruption

local REF_W, REF_H = 1920, 1080
local RISE_SEC = Anim.rise_s
local SHARDS_PER_SEC = Anim.shards_per_s
local SHARD_LIFE = Anim.shard_life_s
local SHARD_SCALE = Look.shard_scale
local SHARD_A = Look.shard_opacity
local SLIVERS_PER_SEC = Anim.slivers_per_s
local SLIVER_LIFE = Anim.sliver_life_s
local SLIVER_W = Look.sliver_w_px
local SLIVER_H = Look.sliver_h_px
local SLIVER_A = Look.sliver_opacity
local SLIVER_X = { Look.sliver_left_px, REF_W - Look.sliver_right_inset_px }
local TINTS = Look.tint_colors

local function rnd(r) return r[1] + math.random() * (r[2] - r[1]) end

local function reset(tx)
    tx:SetScale(1, 1) ; tx:SetRotation(0) ; tx:SetOpacity(1)
    tx:SetColor(1, 1, 1) ; tx:SetBlendMode("Normal")
end

-- tex = { atlas, white }; rects = the atlas tiles
function Corruption.new(tex, rects)
    local self = setmetatable({ tex = tex, rects = rects or {} }, Corruption)
    self.t, self.sec, self.tail = nil, 1, 1
    self.bits = {}
    self.acc = { shard = 0, sliver = 0 }
    return self
end

function Corruption:start(sec, tail)
    self.t, self.sec, self.tail = 0, math.max(0.1, sec or Anim.default_s), math.max(0.1, tail or Anim.default_tail_s)
    self.bits = {}
    self.acc.shard, self.acc.sliver = 0, 0
end

function Corruption:running()
    return self.t ~= nil and self.t < self.sec + self.tail
end

-- current strength, 0..1
function Corruption:level()
    if self.t == nil then return 0 end
    local t = self.t
    if t < RISE_SEC then return Easing.outQuad(t / RISE_SEC) end
    if t < self.sec then return 1 end
    return 1 - Easing.inOutSine((t - self.sec) / self.tail)
end

local function spawnBits(self, kind, rate, dt, make)
    self.acc[kind] = self.acc[kind] + rate * dt
    while self.acc[kind] >= 1 do
        self.acc[kind] = self.acc[kind] - 1
        self.bits[#self.bits + 1] = make()
    end
end

function Corruption:update(dt)
    if self.t == nil then return end
    self.t = self.t + dt
    local k = (self.t < self.sec) and self:level() or 0
    local n = #self.rects
    spawnBits(self, "shard", SHARDS_PER_SEC * k, dt, function()
        return { tile = (n > 0) and math.random(n) or nil, x = rnd({ 0, REF_W }), y = rnd({ 0, REF_H }),
                 s = rnd(SHARD_SCALE), a = rnd(SHARD_A), c = TINTS[math.random(#TINTS)], life = rnd(SHARD_LIFE), age = 0 }
    end)
    spawnBits(self, "sliver", SLIVERS_PER_SEC * k, dt, function()
        return { x = rnd(SLIVER_X), y = rnd({ 0, REF_H }), w = rnd(SLIVER_W), h = rnd(SLIVER_H),
                 a = rnd(SLIVER_A), c = TINTS[math.random(#TINTS)], life = rnd(SLIVER_LIFE), age = 0 }
    end)
    local i = 1
    while i <= #self.bits do
        local b = self.bits[i]
        b.age = b.age + dt
        if b.age >= b.life then
            self.bits[i] = self.bits[#self.bits]
            self.bits[#self.bits] = nil
        else
            i = i + 1
        end
    end
    if not self:running() then self.t, self.bits = nil, {} end
end

local function drawBits(self, ox, oy, k)
    local atlas, white = self.tex.atlas, self.tex.white
    local aReady = atlas ~= nil and atlas.Ready
    local wReady = white ~= nil and white.Ready
    if aReady then atlas:SetBlendMode("Add") end
    if wReady then white:SetBlendMode("Add") end
    for _, b in ipairs(self.bits) do
        local a = math.min(1, b.a * (1 - b.age / b.life))
        if b.w ~= nil then
            if wReady and a > 0.002 then
                white:SetColor(b.c[1], b.c[2], b.c[3])
                white:SetOpacity(a)
                white:SetScale(b.w * k / math.max(1, white.Width), b.h * k / math.max(1, white.Height))
                white:DrawAtAnchor(b.x * k + ox, b.y * k + oy, "topleft")
            end
        elseif aReady and b.tile ~= nil and a > 0.002 then
            local r = self.rects[b.tile]
            atlas:SetColor(b.c[1], b.c[2], b.c[3])
            atlas:SetOpacity(a)
            atlas:SetScale(b.s * k, b.s * k)
            atlas:DrawRectAtAnchor(b.x * k + ox, b.y * k + oy, r.x, r.y, r.w, r.h, "center")
        end
    end
    if aReady then reset(atlas) end
    if wReady then reset(white) end
end

-- ox, oy = shake offset
function Corruption:draw(ox, oy)
    if self.t == nil then return end
    local res = THEME:GetResolution()
    drawBits(self, ox, oy, res.X / REF_W)
end

return Corruption
