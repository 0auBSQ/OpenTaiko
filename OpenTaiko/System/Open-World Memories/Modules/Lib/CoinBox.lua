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
--   purse:show(coins)   purse:hide()   purse:visible()   purse:setPrice(n | nil)   purse:setBonus(n | nil)
--   purse:pay(n)        purse:gain(n)  purse:add(n)     purse:update(dt)          purse:draw()   purse:dispose()
--
-- Every coin figure passed in is the true balance change; the number on the pill catches up on
-- its own once the coins have landed. setBonus shows a green "+n" above the pill (coins still to
-- come, e.g. while the unlock modal pours them into its piggy bank) or a red "-n" for a negative
-- value (coins still to go) and add rolls the balance up, or down for a negative value, without any
-- coin flying in, for callers animating the coins themselves; drawCoin draws the coin
-- for them. The coin is the "Coin" shared texture (_boot registers Textures/Coin.png, so an artist
-- can redraw it); a drawn fallback covers a skin without one.

-- positions, sizes and colours: Config/CoinBox/layout.json; timings: Config/CoinBox/animation.json
local HERE = require("Core.Here")(...)
local Config = require("Core.Config")
local Layout = Config.load(HERE .. "Config/CoinBox/layout.json")
local Animation = Config.load(HERE .. "Config/CoinBox/animation.json")

local CoinBox = {}
CoinBox.__index = CoinBox

local Pill = Layout.pill
local Icon = Layout.icon
local Fallback = Layout.fallback_coin
local Text = Layout.text
local Flying = Layout.flying_coin
local Roll = Animation.roll
local Pay = Animation.pay
local Gain = Animation.gain
local Flight = Animation.coin_flight

local SCREEN_W = 1920
local ICON = Icon.size
local ICON_LEFT = Icon.left
local INK = Text.ink_color
local RED, GREEN = Text.red_color, Text.green_color
local OUTLINE, OUTLINE_ALPHA = Text.outline_color, Text.outline_alpha
local RIGHT_INSET, ABOVE_DY = Text.right_inset, Text.above_dy
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

-- the fallback coin: a dark rim, red top half, blue bottom half, a glint
local function coinIcon(s)
    local cv = CANVAS:CreateCanvas(s, s)
    local c, r = floor(s / 2), floor(s / 2) - 1
    local rim, top, bottom, glint = Fallback.rim_color, Fallback.top_color, Fallback.bottom_color, Fallback.glint_color
    cv:FillCircle(c, c, r, rim[1], rim[2], rim[3], 255)
    local ri = r - Fallback.rim
    cv:FillCircle(c, c, ri, top[1], top[2], top[3], 255)
    for dy = 0, ri do
        local half = floor(sqrt(ri * ri - dy * dy))
        cv:FillRect(c - half, c + dy, 2 * half + 1, 1, bottom[1], bottom[2], bottom[3], 255)
    end
    cv:FillRect(c - ri, c, 2 * ri + 1, 1, rim[1], rim[2], rim[3], 255)
    cv:FillCircle(c - floor(ri * Fallback.glint_dx), c - floor(ri * Fallback.glint_dy),
        max(Fallback.glint_min, floor(ri * Fallback.glint_size)), glint[1], glint[2], glint[3], 255)
    cv:Upload()
    return cv
end

-- ── construction ──────────────────────────────────────────────────────────────────────────────
function CoinBox.new(o)
    o = o or {}
    local self = setmetatable({}, CoinBox)
    self.w, self.h = o.w or Pill.w, o.h or Pill.h
    self.x = o.x or (SCREEN_W - Pill.right_margin - self.w)
    self.y = o.y or Pill.y
    self.face = o.face or Pill.face_color
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
    local shadow = Pill.shadow_color
    self.shadowCv = pill(self.w, self.h, shadow[1], shadow[2], shadow[3])
    if self.coinTex == nil then
        pcall(function() self.coinTex = SHARED:GetSharedTexture("Coin") end)
    end
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
local function nudge(size) return floor(size * Text.nudge) end

function CoinBox:_text(size, str, x, y, color, opacity, anchor)
    local gf = self:_font(size)
    gf:Draw(tostring(str), x, y + nudge(size), self:_color(color), self:_color(OUTLINE, OUTLINE_ALPHA), opacity, 1, 0, anchor)
end

function CoinBox:_chime()
    if self.sfx then pcall(function() self.sfx:Play() end) end
end

-- the coin, centred at (x, y), sized as the pill's icon times (sx, sy): the shared texture when it
-- has loaded, the drawn fallback otherwise
function CoinBox:drawCoin(x, y, sx, sy, opacity)
    sx, sy, opacity = sx or 1, sy or 1, opacity or 1
    local tex = self.coinTex
    if tex ~= nil and (tex.Width or 0) > 0 then
        local k = ICON / tex.Width
        tex:SetScale(k * sx, k * sy) ; tex:SetOpacity(opacity)
        tex:DrawAtAnchor(x, y, "center")
        tex:SetScale(1, 1) ; tex:SetOpacity(1)
        return
    end
    if self.coinCv == nil then self.coinCv = coinIcon(ICON) end
    self.coinCv:SetScale(sx, sy) ; self.coinCv:SetOpacity(opacity)
    self.coinCv:DrawAtAnchor(floor(x + 0.5), floor(y + 0.5), "center")
    self.coinCv:SetScale(1, 1) ; self.coinCv:SetOpacity(1)
end

-- ── state ─────────────────────────────────────────────────────────────────────────────────────
function CoinBox:show(n)
    self:_ensureArt()
    self.balance, self.rolling = n or 0, n or 0
    self.shown = true
end

function CoinBox:hide() self.shown = false ; self.price = nil ; self.bonus = nil end
function CoinBox:visible() return self.shown or self.alpha > 0.01 end
function CoinBox:setPrice(n) self.price = n end
function CoinBox:setBonus(n) self.bonus = (n ~= nil and n ~= 0) and n or nil end
function CoinBox:balanceValue() return self.balance end

-- the balance rolls up right away; the caller draws the coins
function CoinBox:add(n)
    self:_ensureArt()
    self.balance = self.balance + n
    self.rollDelay = 0
end

-- the price label turns red and drops into the purse; coins follow it; the balance rolls down
function CoinBox:pay(n)
    self:_ensureArt()
    local x, y, w, h = self.x, self.y, self.w, self.h
    self.balance = self.balance - n
    self.price = nil
    self.drops[#self.drops + 1] = { text = "-" .. tostring(n), x = x + w - RIGHT_INSET, y = y - ABOVE_DY, ty = y + h / 2, t = 0, dur = Pay.drop_s, color = RED }
    for i = 1, Pay.count do
        self.coins[#self.coins + 1] = { x = x + w - Pay.start_from_right - (i - 1) * Pay.step_x, y = y - Pay.start_dy - (i % Pay.row_cycle) * Pay.row_dy,
                                        tx = x + ICON / 2 + ICON_LEFT, ty = y + h / 2,
                                        t = 0, dur = Pay.dur_s + (i % Pay.dur_cycle) * Pay.dur_step_s, delay = Pay.delay_step_s * i, spin = (i % 2 == 0) and 1 or -1 }
    end
    self.rollDelay = Pay.roll_delay_s
end

-- coins pour into the purse from above and a green "+n" rises out of it; the balance rolls up
function CoinBox:gain(n)
    self:_ensureArt()
    local x, y, w, h = self.x, self.y, self.w, self.h
    self.balance = self.balance + n
    self.rises[#self.rises + 1] = { text = "+" .. tostring(n), x = x + w / 2, y = y + h / 2, t = 0, dur = Gain.rise_s, color = GREEN }
    for i = 1, Gain.count do
        self.coins[#self.coins + 1] = { x = x + Gain.start_from_left + (i - 1) * Gain.step_x, y = y - Gain.start_dy - (i % Gain.row_cycle) * Gain.row_dy,
                                        tx = x + ICON / 2 + ICON_LEFT, ty = y + h / 2,
                                        t = 0, dur = Gain.dur_s + (i % Gain.dur_cycle) * Gain.dur_step_s, delay = Gain.delay_step_s * i, spin = (i % 2 == 0) and 1 or -1 }
    end
    self.rollDelay = Gain.roll_delay_s
    self:_chime()
end

function CoinBox:update(dt)
    self.alpha = self.alpha + ((self.shown and 1 or 0) - self.alpha) * min(1, dt * Animation.fade_rate_per_s)
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
        self.rolling = self.rolling + (self.balance - self.rolling) * min(1, dt * Roll.rate_per_s)
        if math.abs(self.balance - self.rolling) < Roll.snap then self.rolling = self.balance end
    end
end

function CoinBox:draw()
    local a = self.alpha
    if a <= 0 or self.pillCv == nil then return end
    local x, y, w, h = self.x, self.y, self.w, self.h
    self.shadowCv:SetOpacity(Pill.shadow_opacity * a) ; self.shadowCv:Draw(x + Pill.shadow_dx, y + Pill.shadow_dy) ; self.shadowCv:SetOpacity(1)
    self.pillCv:SetOpacity(a) ; self.pillCv:Draw(x, y) ; self.pillCv:SetOpacity(1)
    self:drawCoin(x + ICON_LEFT + ICON / 2, y + h / 2, 1, 1, a)
    self:_text(Text.balance_size, floor(self.rolling + 0.5), x + w - RIGHT_INSET, y + h / 2, self.textColor, a, "right")
    if self.price ~= nil then
        self:_text(Text.price_size, self.price, x + w - RIGHT_INSET, y - ABOVE_DY, self.textColor, a, "right")
    elseif self.bonus ~= nil then
        local up = self.bonus > 0
        self:_text(Text.bonus_size, (up and "+" or "-") .. tostring(math.abs(self.bonus)), x + w - RIGHT_INSET, y - ABOVE_DY, up and GREEN or RED, a, "right")
    end
    for _, d in ipairs(self.drops) do
        local k = easeIn(min(1, d.t / d.dur))
        self:_text(Text.drop_size, d.text, d.x, d.y + (d.ty - d.y) * k, d.color, a * (1 - k * Pay.drop_fade), "right")
    end
    for _, r in ipairs(self.rises) do
        local k = min(1, r.t / r.dur)
        self:_text(Text.rise_size, r.text, r.x, r.y - Gain.rise_px * ease(k), r.color, a * (1 - k), "center")
    end
    for _, c in ipairs(self.coins) do
        local k = (c.t - c.delay) / c.dur
        if k >= 0 and k < 1 then
            local e = easeIn(k)
            local cx = c.x + (c.tx - c.x) * e
            local cy = c.y + (c.ty - c.y) * e - sin(k * 3.14159) * Flight.arc_px
            local s = Flying.scale_x + Flying.wobble * sin(k * Flight.wobble_freq * c.spin)             -- a little wobble reads as a spin
            self:drawCoin(cx, cy, s, Flying.scale_y, a)
        end
    end
end

function CoinBox:dispose()
    for _, cv in ipairs({ self.pillCv, self.shadowCv, self.coinCv }) do if cv then pcall(function() cv:Dispose() end) end end
    self.pillCv, self.shadowCv, self.coinCv, self.coinTex = nil, nil, nil, nil     -- the shared texture is not ours to dispose
    if self.ownSfx and self.sfx then pcall(function() self.sfx:Dispose() end) end
    if self.ownPaySfx and self.paySfx then pcall(function() self.paySfx:Dispose() end) end
    self.sfx, self.ownSfx, self.paySfx, self.ownPaySfx = nil, false, nil, false
    for _, f in pairs(self._fonts or {}) do pcall(function() f:Dispose() end) end
    self._fonts, self._colors = {}, {}
    self.drops, self.rises, self.coins = {}, {}, {}
    self.shown, self.alpha, self.price, self.bonus = false, 0, nil, nil
end

return CoinBox
