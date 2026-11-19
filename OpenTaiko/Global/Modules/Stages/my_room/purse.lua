---@diagnostic disable: undefined-global, undefined-field, need-check-nil
-- coin purse over My Room's dialogues and windows: ui:coins in its own manager

local UI = require("uilib")

local Purse = {}
Purse.__index = Purse

function Purse.new(o)
    o = o or {}
    local self = setmetatable({}, Purse)
    self.w, self.h = o.w or 300, o.h or 66
    self.x, self.y = o.x or (1920 - 60 - self.w), o.y or 60
    self.shown = false
    return self
end

-- made on first use (UI library known by then)
function Purse:widget()
    if self.coins == nil then
        self.ui = UI.new{}
        self.coins = self.ui:coins{ x = self.x, y = self.y, w = self.w, h = self.h, amount = 0 }
    end
    return self.coins
end

function Purse:show(n)
    local c = self:widget()
    c:setAmount(n or 0)
    self.shown = true
end

function Purse:hide()
    self.shown = false
    if self.coins then self.coins:setPrice(nil); self.coins:setBonus(nil) end
end

function Purse:visible() return self.shown end
function Purse:setPrice(n) self:widget():setPrice(n) end
function Purse:setBonus(n) self:widget():setBonus(n) end
function Purse:pay(n) self:widget():pay(n) end
function Purse:gain(n) self:widget():gain(n) end
function Purse:add(n) self:widget():add(n) end
function Purse:drawCoin(x, y, sx, sy, opacity) self:widget():drawCoin(x, y, sx, sy, opacity) end

function Purse:update(dt)
    if self.coins then self.coins:update({ dt = dt or 0 }) end
end

function Purse:draw()
    if self.shown and self.coins then self.coins:draw() end
end

function Purse:dispose()
    if self.ui then pcall(function() self.ui:disposeWidgets() end) end
    self.ui, self.coins, self.shown = nil, nil, false
end

return Purse
