---@diagnostic disable: undefined-global, undefined-field, need-check-nil
-- modal_coins.lua — the coin reward: the piggy bank slides in from the left with the coin purse
-- (Lib/CoinBox) riding above it showing the balance before the reward and a green "+n" of what is
-- still to come; coins then drop from the top into the slot one by one, each one squashing the
-- piggy and rolling the purse up, and the last one earns a jingle and a happy hop. The first
-- decide press during the pour skips to the end, the next one closes.

local Fx = require("modal_fx")
local CoinBox = require("CoinBox")

local M = {}
M.__index = M

local PIG_CX, PIG_CY = 960, 690          -- the piggy's resting centre
local PIG_W, PIG_H = 520, 420
local SLOT_X, SLOT_Y = 260, 94           -- the coin slot's centre inside piggy.png
local PIG_FROM_X = -PIG_W                -- off screen left
local SLIDE_DUR = 0.75
local POUR_START = 0.9
local COIN_GAP, COIN_FALL = 0.16, 0.42
local COIN_SIZE = 1.5                    -- the purse's 44px coin icon scaled up
local PURSE_W = 300
local OVERLAY = 0.62
local HEADER_Y = 180
local FADE_OUT = 0.28

local floor, min, max, cos, ceil = math.floor, math.min, math.max, math.cos, math.ceil

function M.new(A)
    local self = setmetatable({}, M)
    self.A = A
    self.purse = CoinBox.new{ w = PURSE_W, sfx = A.sfx.jingle }
    self.sparks = Fx.pool(A.tex.star, "add")
    self.tl = Fx.timeline()
    return self
end

local function splitCoins(amount)
    if amount <= 0 then return {} end
    local n = max(3, min(12, ceil(amount / 10)))
    if amount < 3 then n = amount end
    local base, rest, values = floor(amount / n), amount % n, {}
    for i = 1, n do values[i] = base + (i <= rest and 1 or 0) end
    return values
end

function M:start(amount, total)
    amount = max(0, floor(tonumber(amount) or 0))
    total = floor(tonumber(total) or amount)
    self.amount, self.total = amount, total
    self.header = LANG:GetString("MODAL_TITLE_COIN")
    self.t, self.done, self.closing, self.closeT = 0, false, false, 0
    self.pigX, self.squashT, self.hopT = PIG_FROM_X, 10, 10
    self.coins = {}
    self.landed, self.remaining = 0, amount
    self.sparks:clear()
    self.purse:show(total - amount)
    self.purse:setBonus(amount)
    self.purse.x, self.purse.y = self.pigX - PURSE_W / 2, PIG_CY - PIG_H / 2 - 110

    local tl = self.tl
    tl.cues = {}
    tl:reset()
    tl:at(0, function() self.A:play("slide") end)
    local values = splitCoins(amount)
    for i, v in ipairs(values) do
        local at = POUR_START + (i - 1) * COIN_GAP
        tl:at(at, function()
            self.coins[#self.coins + 1] = { t0 = at, value = v, x = self:slotX() + (i % 3 - 1) * 26, landed = false, i = i }
        end)
    end
    self.lastLand = POUR_START + (#values - 1) * COIN_GAP + COIN_FALL
    tl:at(self.lastLand + 0.2, function() self:finish(true) end)
end

function M:slotX() return self.pigX - PIG_W / 2 + SLOT_X end
function M:slotY() return PIG_CY - PIG_H / 2 + SLOT_Y end

function M:land(c)
    if c.landed then return end
    c.landed = true
    self.landed = self.landed + 1
    self.remaining = max(0, self.remaining - c.value)
    self.purse:add(c.value)
    self.purse:setBonus(self.remaining)
    self.squashT = 0
    self.A:play(c.i % 2 == 0 and "coin_alt" or "coin")
    self.sparks:burst(self:slotX(), self:slotY(), 6, { speed = 220, size = 0.18, life = 0.4, color = { 255, 240, 160 }, gravity = 500 })
end

-- everything in the piggy: the jingle, the hop and a shower of sparkles
function M:finish(withFanfare)
    if self.done then return end
    self.done = true
    self.remaining = 0
    self.purse.balance = self.total
    self.purse:setBonus(nil)
    if withFanfare then
        self.A:play("jingle")
        self.hopT = 0
        self.sparks:burst(PIG_CX, PIG_CY - 60, 26, { speed = 380, size = 0.28, life = 0.8, color = { 255, 236, 140 }, gravity = 700 })
    end
end

function M:skip()
    for _, c in ipairs(self.coins) do c.landed = true end
    self.tl:jump(self.lastLand + 1)
    self.pigX = PIG_CX
    self.purse.rolling = self.total
    self:finish(false)
end

-- returns true once the closing fade is over
function M:update(dt, decide)
    if self.closing then
        self.closeT = self.closeT + dt
        self.purse:update(dt)
        return self.closeT >= FADE_OUT
    end
    self.t = self.t + dt
    self.tl:advance(dt)
    self.pigX = Fx.lerp(PIG_FROM_X, PIG_CX, Fx.outBack(Fx.span(self.t, 0, SLIDE_DUR), 1.2))
    self.purse.x = self.pigX - PURSE_W / 2
    self.squashT, self.hopT = self.squashT + dt, self.hopT + dt
    for _, c in ipairs(self.coins) do
        if not c.landed and self.tl.t >= c.t0 + COIN_FALL then self:land(c) end
    end
    self.purse:update(dt)
    self.sparks:update(dt)
    if decide then
        if self.done then
            self.closing = true
            self.purse:hide()
            self.A:play("close")
        else
            self:skip()
        end
    end
    return false
end

function M:draw()
    local A = self.A
    local fade = self.closing and (1 - Fx.span(self.closeT, 0, FADE_OUT)) or Fx.span(self.t, 0, 0.3)
    Fx.fill(A.fillCv, { 0, 0, 0 }, OVERLAY * fade)
    A.fontHeader:Draw(self.header, 960, HEADER_Y, nil, nil, fade, 1, 0, "center")

    local squash = Fx.hump(Fx.span(self.squashT, 0, 0.22))
    local hop = Fx.hump(Fx.span(self.hopT, 0, 0.38))
    local py = PIG_CY - hop * 46
    Fx.draw(A.tex.piggy, self.pigX, py + PIG_H / 2 * 0.08 * squash, { sx = 1 + 0.07 * squash, sy = 1 - 0.08 * squash, opacity = fade })

    self.purse:draw()

    local slotY = self:slotY()
    for _, c in ipairs(self.coins) do
        if not c.landed then
            local k = Fx.span(self.tl.t, c.t0, COIN_FALL)
            local y = Fx.lerp(-60, slotY, Fx.inQuad(k))
            local spin = 0.35 + 0.65 * math.abs(cos(k * 9 + c.i))
            local sink = k > 0.85 and (1 - (k - 0.85) / 0.15) or 1
            self.purse:drawCoin(c.x, y, COIN_SIZE * spin, COIN_SIZE * sink, fade)
        end
    end
    self.sparks:draw(fade)
end

function M:stop()
    self.purse:hide()
    self.coins = {}
    self.sparks:clear()
end

function M:dispose()
    self.purse:dispose()
end

return M
