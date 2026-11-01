-- One figure of the end-credits scenes; x, y are its feet (1920 space).

local Easing   = require("Easing")
local assets   = require("encounter/assets")
local sprite   = require("encounter/sprite")
local motion   = require("encounter/motion")
local monsters = require("encounter/monsters")
local Config   = require("Core.Config")
local Layout    = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")

local M = {}

local Actor = {}
Actor.__index = Actor

local Look = Layout.encounter.credits.actor
local Anim = Animation.encounter.credits.actor
-- same as encounter/cast.lua
local SHADOW_ALPHA, SHADOW_W, LIFT_FULL = Look.shadow_opacity, Look.shadow_width, Look.lift_full
local ART_SCALE = Config.plain(Look.art_scale)
-- turn, as in encounter/motion.lua
local FLIP_FROM, FLIP_SEC = Anim.flip_from, Anim.flip_s

local _o = {}

local function opts(sx, sy, rot, pivot, alpha, r, g, b, grad)
    _o.sx, _o.sy, _o.rot, _o.pivot = sx, sy, rot, pivot
    _o.alpha, _o.r, _o.g, _o.b, _o.blend, _o.grad = alpha, r, g, b, "Normal", grad
    return _o
end

-- { kind, params } of a monster's run-floor idle, or the in-place squash
function M.monsterIdle(key)
    local m = monsters.forLevel(1, key)[1]
    if m ~= nil and m.key == key then return { kind = m.kind, params = m.params } end
    return { kind = "counter", params = {} }
end

-- key = manifest key; o = { idle = { kind, params }, seed, grad, shadowKey, shadowPx }, all optional
function M.new(key, x, y, o)
    o = o or {}
    local self = setmetatable({
        key = key, x = x, y = y, gy = nil, dy = 0, sx = 1, sy = 1, rot = 0, pivot = "foot",
        alpha = 1, shadowA = 1, visible = true, flipT = FLIP_SEC, idleK = 1,
        grad = o.grad, shadowKey = o.shadowKey or key, shadowPx = o.shadowPx,
    }, Actor)
    if o.idle ~= nil then
        self.idle = motion.new(o.idle.kind, o.idle.params, o.seed or 0)
        self.pose = motion.step(self.idle, 0)
    end
    return self
end

-- turn = true plays the turn to the new frame
function Actor:face(key, turn)
    if key == self.key then return end
    self.key = key
    if turn then self.flipT = 0 end
end

function Actor:notice()
    if self.idle ~= nil then motion.notice(self.idle) end
end

function Actor:update(dt)
    self.flipT = self.flipT + dt
    if self.idle ~= nil then self.pose = motion.step(self.idle, dt) end
end

-- the scene's fields plus the idle (weighted by idleK) and the turn
local function final(self)
    local sx, sy, dy, rot, pivot = self.sx, self.sy, self.dy, self.rot, self.pivot
    local p, k = self.pose, self.idleK
    if p ~= nil and k > 0 then
        sx = sx * (1 + (p.sx - 1) * k)
        sy = sy * (1 + (p.sy - 1) * k)
        dy = dy + p.dy * k
        rot = rot + p.rot * k
        if p.pivot ~= "foot" and k >= 0.5 then pivot = p.pivot end
    end
    if self.flipT < FLIP_SEC then sx = sx * (FLIP_FROM + (1 - FLIP_FROM) * Easing.outBack(self.flipT / FLIP_SEC)) end
    local s = ART_SCALE[self.key] or 1
    return sx * s, sy * s, dy, rot, pivot
end

-- a = the scene's opacity
function Actor:drawShadow(shadow, shadowMeta, a)
    if not self.visible or shadow == nil or shadowMeta == nil then return end
    a = a * self.alpha * self.shadowA
    if a <= 0 then return end
    local w = self.shadowPx
    if w == nil then
        local meta = assets.meta(self.shadowKey)
        w = SHADOW_W * ((meta ~= nil and meta.w) or Look.shadow_fallback_w)
    end
    local _, _, dy = final(self)
    local l = math.min(1, math.max(0, -dy) / LIFT_FULL)
    local s = w / math.max(1, shadowMeta.w) * (1 - Look.lift_shrink * l)
    sprite.drawAt(shadow, self.x, self.gy or self.y, opts(s, s, 0, nil, SHADOW_ALPHA * (1 - Look.lift_fade * l) * a, 1, 1, 1))
end

-- a = the scene's opacity, c = its brightness
function Actor:draw(a, c)
    if not self.visible then return end
    a = a * self.alpha
    if a <= 0 then return end
    local tx, meta = assets.get(self.key)
    if meta == nil then return end
    local sx, sy, dy, rot, pivot = final(self)
    sprite.draw(tx, meta, self.x, self.y + dy, opts(sx, sy, rot, pivot, math.min(1, a), c, c, c, self.grad))
end

-- back to front by feet y, shadows first
function M.drawAll(list, shadow, shadowMeta, a, c)
    table.sort(list, function(p, q) return p.y < q.y end)
    for _, act in ipairs(list) do act:drawShadow(shadow, shadowMeta, a) end
    for _, act in ipairs(list) do act:draw(a, c) end
end

return M
