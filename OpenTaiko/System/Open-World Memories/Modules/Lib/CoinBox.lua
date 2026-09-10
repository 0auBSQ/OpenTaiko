---@diagnostic disable: undefined-global, undefined-field, need-check-nil
-- CoinBox.lua — a coin purse widget to lay over a dialogue box: a pill with the coin icon and the
-- balance, an optional price above it, and the animations for paying (the price turns red, drops
-- into the purse while coins fly in, the balance rolls down) and for gaining coins (a green "+n"
-- rises out of the purse while coins pour in, the balance rolls up). Draws with its own glyph
-- fonts and baked canvases; the caller only updates and draws it each frame.
--
--   local CoinBox = require("CoinBox")
--   local purse = CoinBox.new{ x = 1560, y = 60, w = 300, h = 66,        -- default: top-right corner
--                              sfx = <sound handle>,                    -- coins landing: the modal's Coin.ogg
--                              paySfx = <sound handle> }                -- a payment: the coin shop's Buy.ogg
--   purse:show(coins)   purse:hide()   purse:visible()   purse:setPrice(n | nil)
--   purse:pay(n)        purse:gain(n)  purse:update(dt)  purse:draw()   purse:dispose()
--
-- Every coin figure passed in is the true balance change; the number on the pill catches up on
-- its own once the coins have landed.

local CoinBox = {}
CoinBox.__index = CoinBox

local SCREEN_W = 1920
local ICON = 44
local INK = { 52, 58, 92 }
local RED, GREEN = { 214, 64, 56 }, { 62, 168, 92 }
local COIN_SFX = "../../ROActivities/modal/Sounds/Coin.ogg"   -- from any Modules/<kind>/<name>/ folder
local PAY_SFX = "../coin_shop/Sounds/Buy.ogg"                 -- from any Modules/Stages/<name>/ folder

local floor, min, max, sin, sqrt = math.floor, math.min, math.max, math.sin, math.sqrt

local function ease(t) return 1 - (1 - t) * (1 - t) end
local function easeIn(t) return t * t end

-- ── baked art ─────────────────────────────────────────────────────────────────────────────────
local function pill(w, h, r, g, b)
    local cv = CANVAS:CreateCanvas(w, h)
    local rad = floor(h / 2)
    cv:FillRect(rad, 0, w - 2 * rad, h, r, g, b, 255)
    cv:FillCircle(rad, rad, rad, r, g, b, 255)
    cv:FillCircle(w - rad - 1, rad, rad, r, g, b, 255)
    cv:Upload()
    return cv
end

-- the game's coin: a dark rim, red top half, blue bottom half, a glint
local function coinIcon(s)
    local cv = CANVAS:CreateCanvas(s, s)
    local c, r = floor(s / 2), floor(s / 2) - 1
    cv:FillCircle(c, c, r, 24, 24, 30, 255)
    local ri = r - 3
    cv:FillCircle(c, c, ri, 232, 52, 44, 255)
    for dy = 0, ri do
        local half = floor(sqrt(ri * ri - dy * dy))
        cv:FillRect(c - half, c + dy, 2 * half + 1, 1, 52, 120, 236, 255)
    end
    cv:FillRect(c - ri, c, 2 * ri + 1, 1, 24, 24, 30, 255)
    cv:FillCircle(c - floor(ri * 0.4), c - floor(ri * 0.45), max(2, floor(ri * 0.18)), 255, 200, 200, 255)
    cv:Upload()
    return cv
end

-- ── construction ──────────────────────────────────────────────────────────────────────────────
function CoinBox.new(o)
    o = o or {}
    local self = setmetatable({}, CoinBox)
    self.w, self.h = o.w or 300, o.h or 66
    self.x = o.x or (SCREEN_W - 60 - self.w)
    self.y = o.y or 60
    self.face = o.face or { 255, 250, 226 }
    self.textColor = o.text or INK
    self.sfx, self.paySfx = o.sfx, o.paySfx
    self.ownSfx, self.ownPaySfx = false, false
    self.shown, self.alpha = false, 0
    self.balance, self.rolling, self.rollDelay = 0, 0, 0
    self.price = nil
    self.drops, self.rises, self.coins = {}, {}, {}
    self._colors = {}
    return self
end

function CoinBox:_color(c, a)
    a = a or 255
    local key = c[1] .. "," .. c[2] .. "," .. c[3] .. "," .. a
    local col = self._colors[key]
    if not col then col = COLOR:CreateColorFromRGBA(c[1], c[2], c[3], a) ; self._colors[key] = col end
    return col
end

function CoinBox:_font(size)
    self._fonts = self._fonts or {}
    local f = self._fonts[size]
    if not f then f = TEXT:CreateGlyphCached(size) ; self._fonts[size] = f end
    return f
end

function CoinBox:_ensureArt()
    if self.pillCv then return end
    self.pillCv = pill(self.w, self.h, self.face[1], self.face[2], self.face[3])
    self.shadowCv = pill(self.w, self.h, 40, 34, 60)
    self.coinCv = coinIcon(ICON)
    if self.sfx == nil then
        pcall(function() self.sfx = SOUND:CreateSFX(COIN_SFX) end)
        self.ownSfx = self.sfx ~= nil
    end
    if self.paySfx == nil then
        pcall(function() self.paySfx = SOUND:CreateSFX(PAY_SFX) end)
        self.ownPaySfx = self.paySfx ~= nil
    end
end

-- every label here is anchored by its middle, where the glyph box draws its ink high: move it down
local function nudge(size) return floor(size * 0.36) end

function CoinBox:_text(size, str, x, y, color, opacity, anchor)
    local gf = self:_font(size)
    gf:Draw(tostring(str), x, y + nudge(size), self:_color(color), self:_color({ 255, 255, 255 }, 220), opacity, 1, 0, anchor)
end

function CoinBox:_chime()
    if self.sfx then pcall(function() self.sfx:Play() end) end
end

-- ── state ─────────────────────────────────────────────────────────────────────────────────────
function CoinBox:show(n)
    self:_ensureArt()
    self.balance, self.rolling = n or 0, n or 0
    self.shown = true
end

function CoinBox:hide() self.shown = false ; self.price = nil end
function CoinBox:visible() return self.shown or self.alpha > 0.01 end
function CoinBox:setPrice(n) self.price = n end
function CoinBox:balanceValue() return self.balance end

-- the price label turns red and drops into the purse; coins follow it; the balance rolls down
function CoinBox:pay(n)
    self:_ensureArt()
    local x, y, w, h = self.x, self.y, self.w, self.h
    self.balance = self.balance - n
    self.price = nil
    self.drops[#self.drops + 1] = { text = "-" .. tostring(n), x = x + w - 24, y = y - 26, ty = y + h / 2, t = 0, dur = 0.55, color = RED }
    for i = 1, 8 do
        self.coins[#self.coins + 1] = { x = x + w - 60 - (i - 1) * 26, y = y - 26 - (i % 3) * 10, tx = x + ICON / 2 + 11, ty = y + h / 2,
                                        t = 0, dur = 0.45 + (i % 4) * 0.05, delay = 0.05 * i, spin = (i % 2 == 0) and 1 or -1 }
    end
    self.rollDelay = 0.45
end

-- coins pour into the purse from above and a green "+n" rises out of it; the balance rolls up
function CoinBox:gain(n)
    self:_ensureArt()
    local x, y, w, h = self.x, self.y, self.w, self.h
    self.balance = self.balance + n
    self.rises[#self.rises + 1] = { text = "+" .. tostring(n), x = x + w / 2, y = y + h / 2, t = 0, dur = 0.9, color = GREEN }
    for i = 1, 8 do
        self.coins[#self.coins + 1] = { x = x + 40 + (i - 1) * 30, y = y - 90 - (i % 3) * 14, tx = x + ICON / 2 + 11, ty = y + h / 2,
                                        t = 0, dur = 0.4 + (i % 3) * 0.06, delay = 0.04 * i, spin = (i % 2 == 0) and 1 or -1 }
    end
    self.rollDelay = 0.3
    self:_chime()
end

function CoinBox:update(dt)
    self.alpha = self.alpha + ((self.shown and 1 or 0) - self.alpha) * min(1, dt * 10)
    if not self.shown and self.alpha < 0.01 then self.alpha = 0 end
    for i = #self.drops, 1, -1 do
        local d = self.drops[i]
        d.t = d.t + dt
        if d.t >= d.dur then
            table.remove(self.drops, i)
            -- the payment lands: the cash-in sound, or the coin chime when there is none
            if self.paySfx then pcall(function() self.paySfx:Play() end) else self:_chime() end
        end
    end
    for i = #self.rises, 1, -1 do
        local r = self.rises[i]
        r.t = r.t + dt
        if r.t >= r.dur then table.remove(self.rises, i) end
    end
    for i = #self.coins, 1, -1 do
        local c = self.coins[i]
        c.t = c.t + dt
        if c.t >= c.delay + c.dur then table.remove(self.coins, i) end
    end
    if self.rollDelay > 0 then
        self.rollDelay = self.rollDelay - dt
    elseif self.rolling ~= self.balance then
        self.rolling = self.rolling + (self.balance - self.rolling) * min(1, dt * 7)
        if math.abs(self.balance - self.rolling) < 0.6 then self.rolling = self.balance end
    end
end

function CoinBox:draw()
    local a = self.alpha
    if a <= 0 or self.pillCv == nil then return end
    local x, y, w, h = self.x, self.y, self.w, self.h
    self.shadowCv:SetOpacity(0.35 * a) ; self.shadowCv:Draw(x + 4, y + 6) ; self.shadowCv:SetOpacity(1)
    self.pillCv:SetOpacity(a) ; self.pillCv:Draw(x, y) ; self.pillCv:SetOpacity(1)
    self.coinCv:SetOpacity(a) ; self.coinCv:Draw(x + 11, y + (h - ICON) / 2) ; self.coinCv:SetOpacity(1)
    self:_text(30, floor(self.rolling + 0.5), x + w - 24, y + h / 2, self.textColor, a, "right")
    if self.price ~= nil then
        self:_text(24, self.price, x + w - 24, y - 26, self.textColor, a, "right")
    end
    for _, d in ipairs(self.drops) do
        local k = easeIn(min(1, d.t / d.dur))
        self:_text(24, d.text, d.x, d.y + (d.ty - d.y) * k, d.color, a * (1 - k * 0.4), "right")
    end
    for _, r in ipairs(self.rises) do
        local k = min(1, r.t / r.dur)
        self:_text(26, r.text, r.x, r.y - 70 * ease(k), r.color, a * (1 - k), "center")
    end
    for _, c in ipairs(self.coins) do
        local k = (c.t - c.delay) / c.dur
        if k >= 0 and k < 1 then
            local e = easeIn(k)
            local cx = c.x + (c.tx - c.x) * e
            local cy = c.y + (c.ty - c.y) * e - sin(k * 3.14159) * 34
            local s = 0.5 + 0.2 * sin(k * 12 * c.spin)             -- a little wobble reads as a spin
            self.coinCv:SetScale(s, 0.5) ; self.coinCv:SetOpacity(a)
            self.coinCv:DrawAtAnchor(cx, cy, "center")
            self.coinCv:SetScale(1, 1) ; self.coinCv:SetOpacity(1)
        end
    end
end

function CoinBox:dispose()
    for _, cv in ipairs({ self.pillCv, self.shadowCv, self.coinCv }) do if cv then pcall(function() cv:Dispose() end) end end
    self.pillCv, self.shadowCv, self.coinCv = nil, nil, nil
    if self.ownSfx and self.sfx then pcall(function() self.sfx:Dispose() end) end
    if self.ownPaySfx and self.paySfx then pcall(function() self.paySfx:Dispose() end) end
    self.sfx, self.ownSfx, self.paySfx, self.ownPaySfx = nil, false, nil, false
    for _, f in pairs(self._fonts or {}) do pcall(function() f:Dispose() end) end
    self._fonts, self._colors = {}, {}
    self.drops, self.rises, self.coins = {}, {}, {}
    self.shown, self.alpha, self.price = false, 0, nil
end

return CoinBox
