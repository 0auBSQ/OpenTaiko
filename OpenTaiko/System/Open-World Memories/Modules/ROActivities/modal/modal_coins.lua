---@diagnostic disable: undefined-global, undefined-field, need-check-nil
-- modal_coins.lua — the coin reward and the coin payment, both around the piggy bank.
--
-- Gaining: the piggy bank slides in from the left with the coin purse (Lib/CoinBox) riding above it
-- showing the balance before the reward and a green "+n" of what is still to come; coins then drop
-- from the top into the slot one by one, each one squashing the piggy and rolling the purse up, and
-- the last one earns a jingle and a happy hop.
-- Spending (a negative amount): the same entrance, the purse showing the balance before and a red
-- "-n" of what is still to go; coins then spring out of the slot one by one and fly off to the
-- right, each one shaking the piggy and rolling the purse down, and the last one rings the till.
-- The first decide press during the coins skips to the end, the next one closes.

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
local COIN_FLY = 0.8                     -- a spent coin's flight out of the slot until it is gone
local FLY_VX, FLY_VY, FLY_G = 420, 640, 1500
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

-- amount > 0: coins gained, total = the balance after; amount < 0: coins spent, total = the balance after
function M:start(amount, total)
    amount = floor(tonumber(amount) or 0)
    self.spend = amount < 0
    amount = math.abs(amount)
    total = floor(tonumber(total) or (self.spend and 0 or amount))
    self.amount, self.total = amount, total
    self.header = LANG:GetString(self.spend and "MODAL_TITLE_SPEND" or "MODAL_TITLE_COIN")
    self.t, self.done, self.closing, self.closeT = 0, false, false, 0
    self.pigX, self.squashT, self.hopT, self.shakeT = PIG_FROM_X, 10, 10, 10
    self.coins = {}
    self.landed, self.remaining = 0, amount
    self.sparks:clear()
    self.purse:show(self.spend and (total + amount) or (total - amount))
    self.purse:setBonus(self.spend and -amount or amount)
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
            if self.spend then self:leave(self.coins[#self.coins]) end
        end)
    end
    local lastStep = POUR_START + (#values - 1) * COIN_GAP
    self.lastLand = lastStep + (self.spend and COIN_FLY or COIN_FALL)
    tl:at((self.spend and lastStep + 0.35 or self.lastLand + 0.2), function() self:finish(true) end)
end

function M:slotX() return self.pigX - PIG_W / 2 + SLOT_X end
function M:slotY() return PIG_CY - PIG_H / 2 + SLOT_Y end

-- a gained coin reaches the slot
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

-- a spent coin springs out of the slot: it counts at once, the piggy shakes
function M:leave(c)
    if c.landed then return end
    c.landed = true
    self.landed = self.landed + 1
    self.remaining = max(0, self.remaining - c.value)
    self.purse:add(-c.value)
    self.purse:setBonus(-self.remaining)
    self.squashT, self.shakeT = 0, 0
    self.A:play(c.i % 2 == 0 and "coin_alt" or "coin")
    self.sparks:burst(self:slotX(), self:slotY(), 4, { speed = 160, size = 0.16, life = 0.35, color = { 255, 232, 150 }, gravity = 500 })
end

-- everything in (or out of) the piggy: the jingle and a happy hop, or the till and a last shake
function M:finish(withFanfare)
    if self.done then return end
    self.done = true
    self.remaining = 0
    self.purse.balance = self.total
    self.purse:setBonus(nil)
    if not withFanfare then return end
    if self.spend then
        self.A:play("pay")
        self.shakeT = 0
    else
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
    self.squashT, self.hopT, self.shakeT = self.squashT + dt, self.hopT + dt, self.shakeT + dt
    if not self.spend then
        for _, c in ipairs(self.coins) do
            if not c.landed and self.tl.t >= c.t0 + COIN_FALL then self:land(c) end
        end
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
    local shakeX = Fx.shake(9, self.shakeT, 0.32)
    local py = PIG_CY - hop * 46
    Fx.draw(A.tex.piggy, self.pigX + shakeX, py + PIG_H / 2 * 0.08 * squash, { sx = 1 + 0.07 * squash, sy = 1 - 0.08 * squash, opacity = fade })

    self.purse:draw()

    local slotY = self:slotY()
    for _, c in ipairs(self.coins) do
        if self.spend then
            -- out of the slot and away to the right, spinning, gone once it leaves the screen
            local ft = self.tl.t - c.t0
            if ft >= 0 and ft < COIN_FLY and not self.done then
                local x = c.x + FLY_VX * ft + (c.i % 3 - 1) * 60 * ft
                local y = slotY - FLY_VY * ft + 0.5 * FLY_G * ft * ft
                local spin = 0.35 + 0.65 * math.abs(cos(ft * 11 + c.i))
                local gone = Fx.span(ft, COIN_FLY - 0.15, 0.15)
                if y < 1140 then self.purse:drawCoin(x, y, COIN_SIZE * spin, COIN_SIZE, fade * (1 - gone)) end
            end
        elseif not c.landed then
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
