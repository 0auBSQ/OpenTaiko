---@diagnostic disable: undefined-global, lowercase-global, need-check-nil, undefined-field
-- PopUI chooser: an enum stepper  [ ◀  value  ▶ ]. Left/Right (or click the arrows) cycles the options.
--   ui:chooser{ x=, y=, w=, h=, options={"A","B"}, index=1, wrap=true, onChange=function(i,opt) end }

local Widget = require("PopUI.widget")
local U      = require("PopUI.util")
local Shape  = require("PopUI.shape")

local Chooser = setmetatable({}, { __index = Widget })
Chooser.__index = Chooser

function Chooser.new(o)
    o = o or {}
    o.w = o.w or 320; o.h = o.h or 64
    local self = Widget.new(o)
    setmetatable(self, Chooser)
    self.options = o.options or {}
    self.index = math.max(1, math.min(o.index or 1, math.max(1, #self.options)))
    self.wrap = (o.wrap ~= false)
    self.noPop = true   -- value is DrawDirect-style atlas text; don't zoom the body
    return self
end

function Chooser:value() return self.options[self.index] end
function Chooser:setOptions(opts, idx)
    self.options = opts or {}
    self.index = math.max(1, math.min(idx or self.index, math.max(1, #self.options)))
    self:restyle()
end

function Chooser:setIndex(i, silent)
    local n = #self.options
    if n == 0 then return end
    if self.wrap then i = ((i - 1) % n) + 1 else i = U.clamp(i, 1, n) end
    if i == self.index then return end
    self.index = i
    if not silent and self.onChange then self.onChange(self.index, self.options[self.index], self) end
    self.mgr:playSfx("move")
end

function Chooser:restyle()
    self.eff = self.mgr:resolveTheme(self.style)
    local c = self.eff.colors
    self:bakeBody()       -- rounded surface body + shadow
    self:bakeRing()
    -- baked left/right arrow caps (accent pills with a triangle), reused across restyles
    local ah = math.floor(self.h * 0.62)
    local aw = math.floor(ah)
    self._arrowH = ah; self._arrowW = aw
    local m = 4
    local function cap(old, dir)
        local cv = self.mgr:reuseCanvas(old, aw + 2 * m, ah + 2 * m)
        Shape.fillRoundAA(cv, m, m, aw, ah, math.min(self.eff.radiusSmall, ah * 0.5), c.primary)
        -- triangle (left or right) in textOnAccent
        local t = c.textOnAccent
        local cx, cy = m + aw * 0.5, m + ah * 0.5
        local s = ah * 0.22
        if dir < 0 then
            Shape.fillTriangle(cv, cx + s, cy - s, cx + s, cy + s, cx - s, cy, t)
        else
            Shape.fillTriangle(cv, cx - s, cy - s, cx - s, cy + s, cx + s, cy, t)
        end
        cv:Upload(); return { canvas = cv, m = m }
    end
    self._capL = cap(self._capL and self._capL.canvas, -1)
    self._capR = cap(self._capR and self._capR.canvas, 1)
end

-- consume Left/Right so focus stays on the chooser
function Chooser:onNavLeft() if self.focused then self:setIndex(self.index - 1); return true end return false end
function Chooser:onNavRight() if self.focused then self:setIndex(self.index + 1); return true end return false end

function Chooser:update(ctx)
    Widget.update(self, ctx)
    if self.focused then
        if ctx.navLeft then self:setIndex(self.index - 1) end
        if ctx.navRight then self:setIndex(self.index + 1) end
    end
    -- which arrow is the mouse over? (left / right third) — drives hover/press visual feedback
    self._hoverThird = nil
    if self.hovered and ctx.inside then
        local third = self.w / 3
        if ctx.mx < self.x + third then self._hoverThird = "left"
        elseif ctx.mx > self.x + self.w - third then self._hoverThird = "right" end
    end
    if self.hovered and ctx.mPressed and self._hoverThird then
        self._pressThird = self._hoverThird
        if self._hoverThird == "left" then self:setIndex(self.index - 1) else self:setIndex(self.index + 1) end
    end
    if not ctx.mPressing then self._pressThird = nil end
end

function Chooser:draw()
    if not self.visible then return end
    local cx, cy = self:centerX(), self:centerY()
    local s = 1
    if self._ring and self._hiCur > 0.01 then
        self._ring.canvas:SetOpacity(self._hiCur); self._ring.canvas:SetScale(1, 1)
        self._ring.canvas:DrawAtAnchor(math.floor(cx), math.floor(cy), "center")
    end
    self._body.canvas:SetColor(1, 1, 1); self._body.canvas:SetOpacity(self.enabled and 1 or 0.5); self._body.canvas:SetScale(1, 1)
    self._body.canvas:DrawAtAnchor(math.floor(cx), math.floor(cy), "center")
    -- arrows: idle slightly dim; brighten + pop on hover; shrink on press (per-arrow tactile feedback)
    local function drawCap(cap, ax, side)
        local hot = (self._hoverThird == side)
        local pressed = (self._pressThird == side)
        local sc = pressed and 0.86 or (hot and 1.12 or 1.0)
        cap.canvas:SetOpacity(self.enabled and (hot and 1.0 or 0.8) or 0.4)
        cap.canvas:SetScale(sc, sc)
        cap.canvas:DrawAtAnchor(math.floor(ax), math.floor(cy), "center")
        cap.canvas:SetScale(1, 1); cap.canvas:SetOpacity(1)
    end
    drawCap(self._capL, self.x + 6 + self._arrowW * 0.5, "left")
    drawCap(self._capR, self.x + self.w - 6 - self._arrowW * 0.5, "right")
    -- value text (centred). renderText caches one texture per string (the option set is finite → bounded,
    -- no leak) and goes through the font → unlike the ASCII glyph atlas it renders CJK option names too.
    local txt = tostring(self:value() or "")
    local sz = self.eff.font.button
    local avail = math.max(20, self.w - 2 * self._arrowW - 24)
    local tex = self.mgr:renderText(sz, txt, self.eff.colors.text, U.withAlpha({ 255, 255, 255 }, 0), false, avail)
    tex:DrawAtAnchor(math.floor(cx), math.floor(cy + self.mgr:textNudge(sz)), "center")
end

return Chooser
