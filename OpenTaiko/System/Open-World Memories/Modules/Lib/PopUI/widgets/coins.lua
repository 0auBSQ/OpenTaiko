---@diagnostic disable: undefined-global, undefined-field, lowercase-global, need-check-nil
-- PopUI coins: SimpleUI coins widget as the CoinBox purse (Lib/CoinBox.lua)

local Widget  = require("UI.SimpleUI.widget")
local CoinBox = require("CoinBox")

-- sounds resolved from this file's folder, so global callers find them
local HERE = require("Core.Here")(...)
local MODULES = HERE and HERE:match("^(.*/)[^/]+/[^/]+/[^/]+/$") or nil
local COIN_SFX = "ROActivities/modal/Sounds/Coin.ogg"
local PAY_SFX = "Stages/coin_shop/Sounds/Buy.ogg"

local floor = math.floor
local CLEAR = { 255, 255, 255, 0 }

local Coins = setmetatable({}, { __index = Widget })
Coins.__index = Coins

local function sound(rel)
    if MODULES == nil then return nil end
    local ok, s = pcall(function() return SOUND:CreateSFXFromAbsolutePath(MODULES .. rel) end)
    return ok and s or nil
end

-- ui:coins{ x, y, w = 300, h = 66, amount = 0, plain = false, size, color = {r,g,b}, edge = {r,g,b,a},
--   coinSfx, paySfx }; plain: coin and number only; sfx default: modal Coin.ogg, coin shop Buy.ogg
-- methods: setAmount, amount, add, gain, pay, setPrice, setBonus, drawCoin(x, y, sx, sy, opacity)
function Coins.new(o)
    o = o or {}
    o.focusable = false; o._focusableWant = false
    o.w = o.w or 300; o.h = o.h or 66
    local self = Widget.new(o)
    setmetatable(self, Coins)
    self.noPop = true
    self.plain = o.plain == true
    self._start = o.amount or 0
    self.amount = nil                 -- the option, copied onto the widget, would hide the amount() method
    return self
end

function Coins:restyle()
    self:resolveStyle()
    if self._box ~= nil then return end
    local coinSfx, paySfx = self.coinSfx, self.paySfx
    if coinSfx == nil then coinSfx = sound(COIN_SFX); self._ownCoin = coinSfx end
    if paySfx == nil then paySfx = sound(PAY_SFX); self._ownPay = paySfx end
    self._box = CoinBox.new{ x = self.x, y = self.y, w = self.w, h = self.h, face = self.face, text = self.color,
                             sfx = coinSfx or false, paySfx = paySfx or false }
    self._box:show(self._start)
end

function Coins:setAmount(n) self._box:show(n or 0) end
function Coins:amount() return self._box:balanceValue() end
function Coins:add(n) self._box:add(n or 0) end
function Coins:gain(n) self._box:gain(n) end
function Coins:pay(n) self._box:pay(n) end
function Coins:setPrice(n) self._box:setPrice(n) end
function Coins:setBonus(n) self._box:setBonus(n) end
function Coins:drawCoin(x, y, sx, sy, opacity) self._box:drawCoin(x, y, sx, sy, opacity) end

function Coins:update(ctx)
    local box = self._box
    box.x, box.y = self.x, self.y
    box:update(ctx.dt or 0)
end

function Coins:draw()
    if not self.visible then return end
    local box = self._box
    if not self.plain then box:draw(); return end
    -- number right-aligned to the widget edge, coin left of it
    local mgr = self.mgr
    local size = self.size or 28
    local right = self.x + self.w
    local cy = floor(self.y + self.h * 0.5)
    local count = string.format("%d", floor(box.rolling + 0.5))
    mgr:drawTextEx(size, count, right, cy + mgr:textNudge(size), self.color or { 70, 72, 96 }, self.edge or CLEAR, 1, 1, 0, "right")
    -- text box pads 25 px around its ink
    local first = right - 25 - mgr:measureText(size, count)
    box:drawCoin(first - 10 - 22, cy, 1, 1, 1)
end

function Coins:dispose()
    if self._box then self._box:dispose(); self._box = nil end
    if self._ownCoin then pcall(function() self._ownCoin:Dispose() end); self._ownCoin = nil end
    if self._ownPay then pcall(function() self._ownPay:Dispose() end); self._ownPay = nil end
    Widget.dispose(self)
end

return Coins
