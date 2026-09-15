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
    self:playSfx("skip")
end

-- silence click sound
function Chooser:onActivate() end

function Chooser:restyle()
    self:resolveStyle()
    local c = self.eff.colors
    self:bakeBody()       -- rounded surface body + shadow
    self:bakeRing()
    -- baked left/right arrow caps (accent pills with a triangle), reused across restyles
    local ah = math.floor(self.h * 0.62)
    local aw = math.floor(ah)
    self._arrowH = ah; self._arrowW = aw
    local m = 4
    local rs = self.eff.radiusSmall
    local function cap(field, dir)
        self:bakeShared(field, "chooser.cap", aw + 2 * m, ah + 2 * m, function(cv)
            Shape.fillRoundAA(cv, m, m, aw, ah, math.min(rs, ah * 0.5), c.primary)
            -- triangle (left or right) in textOnAccent
            local t = c.textOnAccent
            local cx, cy = m + aw * 0.5, m + ah * 0.5
            local s = ah * 0.22
            if dir < 0 then
                Shape.fillTriangle(cv, cx + s, cy - s, cx + s, cy + s, cx - s, cy, t)
            else
                Shape.fillTriangle(cv, cx - s, cy - s, cx - s, cy + s, cx + s, cy, t)
            end
        end, { m = m }, dir)
    end
    cap("_capL", -1)
    cap("_capR", 1)
end

function Chooser:onCapturing(silence)
    if not silence then self:playSfx("click") end
end
function Chooser:onEndCapturing(silence)
    if not silence then self:playSfx("cancel") end
end

-- consume Left/Right (also for pad Left/Right in capturing mode), so focus stays on the chooser
-- consume Cancel to quit capturing mode
function Chooser:onDecide() self:setCapturing(not self.capturing); return true end
function Chooser:onCancel()
    if self.capturing then self:setCapturing(false); return true end
    return false
end
function Chooser:onNavLeft(forPad)
    if not forPad or self.capturing then self:setCapturing(true, true); self:setIndex(self.index - 1); return true end
    return false
end
function Chooser:onNavRight(forPad)
    if not forPad or self.capturing then self:setCapturing(true, true); self:setIndex(self.index + 1); return true end
    return false
end

function Chooser:update(ctx)
    Widget.update(self, ctx)
    local capturing, silence = self.capturing, false
    local hoverThird = self._hoverThird
    if not self.focused then capturing = false; silence = true end   -- losing focus ends capture quietly
    -- which arrow is the mouse over? (left / right third) — drives hover/press visual feedback
    if not (self.hovered and ctx.inside or self.pressed) then
        hoverThird = nil
    elseif ctx.moved or not self._wasHovered or self.pressed then
        local third = self.w / 3
        if ctx.mx < self.x + third then hoverThird = "left";
        elseif ctx.mx > self.x + self.w - third then hoverThird = "right"
        else hoverThird = nil
        end
        if ctx.mPressed then self._pressThird = hoverThird end
    end
    if self._wasHovered and hoverThird and hoverThird ~= self._hoverThird then
        self:playSfx("hover")
    end
    self._wasHovered = self.hovered
    self._hoverThird = hoverThird
    if ctx.mPressed then
        capturing = self.pressed
        if self._hoverThird and self._hoverThird == self._pressThird then
            if self._hoverThird == "left" then capturing = true; self:setIndex(self.index - 1); silence = true
            else capturing = true; self:setIndex(self.index + 1); silence = true
            end
        end
    end
    self:setCapturing(capturing, silence)
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
    -- value text (centred), glyph-composed: no per-string textures, CJK-correct, squished to the avail width
    local txt = tostring(self:value() or "")
    local sz = self.eff.font.button
    local avail = math.max(20, self.w - 2 * self._arrowW - 24)
    local textColor = self._hiCapCur > 0.01
        and U.lerpColor(self.eff.colors.text, self.eff.colors.primary2, self._hiCapCur)
        or self.eff.colors.text
    self.mgr:drawTextEx(sz, txt, math.floor(cx), math.floor(cy + self.mgr:textNudge(sz)),
        textColor, U.withAlpha({ 255, 255, 255 }, 0), self.enabled and 1 or 0.5, 1, avail, "center")
end

return Chooser
