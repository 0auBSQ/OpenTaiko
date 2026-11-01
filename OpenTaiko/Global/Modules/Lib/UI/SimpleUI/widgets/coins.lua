---@diagnostic disable: undefined-global, undefined-field, lowercase-global, need-check-nil
-- SimpleUI coins: an amount of coins with the coin icon, on a flat pill (plain = true: the coin and the
-- number only, right-aligned to the widget's right edge). Decorative (non-focusable).
--   ui:coins{ x=, y=, w=, h=, amount=0, plain=false, size=, iconSize=44, color={r,g,b}, edge={r,g,b,a} }
--   c:setAmount(n)   the shown amount, at once          c:amount()        the amount it is heading to
--   c:add(n)         the number rolls by n              c:gain(n) / c:pay(n)   the same, with a "+n" / "-n"
--   c:setPrice(n)    a price above it (nil: none)       c:setBonus(n)     a "+n" (or "-n") above it (nil: none)
--   c:drawCoin(x, y, sx, sy, opacity)                   the coin icon centred at (x, y), scaled by sx, sy
-- color and edge (the letters' outline colour) may change any frame. The icon is Coin.png in SimpleUI's
-- folder (a copy of OWM's coin picture, its _boot Textures/Coin.png).
-- gain plays the sfx "coin" (none by default; pass sfx = { coin = <sound> }), pay the sfx "pay".

local Widget = require("UI.SimpleUI.widget")
local U      = require("UI.SimpleUI.util")

local HERE = require("Core.Here")(...)
local ICON_FILE = HERE and (HERE:gsub("[^/]+/$", "") .. "Coin.png") or nil

local floor, min = math.floor, math.min
local CLEAR = { 255, 255, 255, 0 }
local GAIN, LOSS = { 46, 140, 84, 255 }, { 196, 64, 56, 255 }

local iconTex = nil       -- the coin texture, loaded once per script (false: it could not load)
local function icon()
    if iconTex == nil then
        iconTex = false
        if ICON_FILE then
            pcall(function() iconTex = TEXTURE:CreateTextureFromAbsolutePath(ICON_FILE) or false end)
        end
    end
    return iconTex or nil
end

local Coins = setmetatable({}, { __index = Widget })
Coins.__index = Coins

function Coins.new(o)
    o = o or {}
    o.focusable = false; o._focusableWant = false
    o.w = o.w or 260; o.h = o.h or 60
    local self = Widget.new(o)
    setmetatable(self, Coins)
    self.noPop = true
    self.plain = o.plain == true
    self.iconSize = o.iconSize or 44
    self.balance = o.amount or 0
    self.amount = nil                 -- the option, copied onto the widget, would hide the amount() method
    self.rolling = self.balance
    self.pops = {}
    return self
end

function Coins:restyle()
    self:resolveStyle()
    local c = self.eff.colors
    if self.plain then self:dropPiece("_body") else self:bakeBody(c.surface, c.surface2) end
end

function Coins:setAmount(n) self.balance = n or 0; self.rolling = self.balance end
function Coins:amount() return self.balance end
function Coins:add(n) self.balance = self.balance + (n or 0) end
function Coins:setPrice(n) self.price = n end
function Coins:setBonus(n) self.bonus = (n ~= nil and n ~= 0) and n or nil end

local function pop(self, n, up)
    self.pops[#self.pops + 1] = { text = (up and "+" or "-") .. tostring(math.abs(n)), up = up, t = 0 }
end
function Coins:gain(n)
    self:add(n); pop(self, n, true); self:playSfx("coin")
end
function Coins:pay(n)
    self:add(-n); self.price = nil; pop(self, n, false); self:playSfx("pay")
end

function Coins:drawCoin(x, y, sx, sy, opacity)
    local tex = icon()
    if tex == nil or (tex.Width or 0) <= 0 then return end
    local k = self.iconSize / tex.Width
    tex:SetScale(k * (sx or 1), k * (sy or 1)); tex:SetOpacity(opacity or 1)
    tex:DrawAtAnchor(floor(x + 0.5), floor(y + 0.5), "center")
    tex:SetScale(1, 1); tex:SetOpacity(1)
end

function Coins:update(ctx)
    local dt = ctx.dt or 0
    if self.rolling ~= self.balance then
        self.rolling = self.rolling + (self.balance - self.rolling) * min(1, dt * 9)
        if math.abs(self.balance - self.rolling) < 0.6 then self.rolling = self.balance end
    end
    for i = #self.pops, 1, -1 do
        local p = self.pops[i]
        p.t = p.t + dt
        if p.t >= 0.8 then table.remove(self.pops, i) end
    end
end

function Coins:draw()
    if not self.visible then return end
    local c = self.eff.colors
    local mgr = self.mgr
    local cy = floor(self.y + self.h * 0.5)
    local fg = self.color or c.text
    local edge = self.edge or CLEAR
    local size = self.size or (self.plain and 28 or self.eff.font.button)
    local count = string.format("%d", floor(self.rolling + 0.5))
    -- right: where the ink of the number ends (a text box keeps 25 px around its ink)
    local right
    if self.plain then
        right = self.x + self.w - 25
        mgr:drawTextEx(size, count, right + 25, cy + mgr:textNudge(size), fg, edge, 1, 1, 0, "right")
        -- the coin stands left of the first digit
        local first = right - mgr:measureText(size, count)
        self:drawCoin(first - 10 - self.iconSize * 0.5, cy, 1, 1, 1)
    else
        local body = self._body
        if body then self:drawPiece(body, self:centerX(), self:centerY(), 1, 1) end
        self:drawCoin(self.x + 11 + self.iconSize * 0.5, cy, 1, 1, 1)
        right = self.x + self.w - 24
        mgr:drawTextEx(size, count, right + 25, cy + mgr:textNudge(size), fg, edge, 1, 1, 0, "right")
    end
    -- above the coins: a price, or the bonus still to come, and the amounts just gained or paid
    local small = self.eff.font.small + 4
    local ay = self.y - 26
    if self.price ~= nil then
        mgr:drawTextEx(small, self.price, right + 25, ay + mgr:textNudge(small), fg, edge, 1, 1, 0, "right")
    elseif self.bonus ~= nil then
        local up = self.bonus > 0
        mgr:drawTextEx(small, (up and "+" or "-") .. tostring(math.abs(self.bonus)), right + 25, ay + mgr:textNudge(small),
            up and (c.gain or GAIN) or (c.loss or LOSS), edge, 1, 1, 0, "right")
    end
    for _, p in ipairs(self.pops) do
        local k = U.clamp(p.t / 0.8, 0, 1)
        local py = cy - 40 * k * (p.up and 1 or -0.5)
        mgr:drawTextEx(small, p.text, right + 25, py + mgr:textNudge(small), p.up and (c.gain or GAIN) or (c.loss or LOSS),
            edge, 1 - k, 1, 0, "right")
    end
end

return Coins
