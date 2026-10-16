---@diagnostic disable: undefined-global, lowercase-global, need-check-nil
-- PopUI toggle / checkbox: a control + side label. The knob/check springs on change.
--   ui:toggle{ text="Fullscreen", x=, y=, value=true, onChange=function(v) end }
--   ui:checkbox{ text="Mute", x=, y=, value=false, onChange=fn }   (variant="checkbox")

local Widget = require("PopUI.widget")
local U      = require("PopUI.util")
local Shape  = require("PopUI.shape")

local Toggle = setmetatable({}, { __index = Widget })
Toggle.__index = Toggle

local LABEL_BG = { 255, 255, 255, 160 }

function Toggle.new(o)
    o = o or {}
    o.w = o.w or 320; o.h = o.h or 56
    local self = Widget.new(o)
    setmetatable(self, Toggle)
    self.text = o.text or ""
    self.value = o.value == true
    self.variant = o.variant or "switch"
    self.gap = o.gap or 22
    self._valCur = self.value and 1 or 0
    self._valFrom, self._valTo = self._valCur, self._valCur
    return self
end

function Toggle:setValue(v, silent)
    v = v == true
    if v == self.value then return end
    self.value = v
    self._valFrom = self._valCur; self._valTo = v and 1 or 0
    Widget.startTween(self._valT, 0.22, { "OUT", "BACK" })
    if not silent then
        if self.onChange and self.onChange(self.value, self) then return end
        self:playSfx("toggle")
    end
end

function Toggle:onActivate()
    self:setValue(not self.value)
end

-- the tick: two strokes with round ends
local function bakeCheck(cv, m, cw, ch, a)
    local x0, y0 = m + cw * 0.24, m + ch * 0.52
    local x1, y1 = m + cw * 0.42, m + ch * 0.72
    local x2, y2 = m + cw * 0.78, m + ch * 0.26
    if Shape.smooth(cv) then
        cv:FillCapsule(x0, y0, x1, y1, 4.5, a[1], a[2], a[3], 255)
        cv:FillCapsule(x1, y1, x2, y2, 4.5, a[1], a[2], a[3], 255)
    else
        cv:StrokeLine(x0, y0, x1, y1, 4, a[1], a[2], a[3], 255)
        cv:StrokeLine(x1, y1, x2, y2, 4, a[1], a[2], a[3], 255)
    end
end

function Toggle:restyle()
    self:resolveStyle()
    self._valT = self._valT or Widget.newTween()
    local c = self.eff.colors
    local ch = 48
    local cw = (self.variant == "checkbox") and 48 or 92
    self._ctrlW, self._ctrlH = cw, ch
    local ow = math.max(3, self.eff.outlineWidth - 1)
    local m = 4
    local rs = self.eff.radiusSmall
    if self.variant == "checkbox" then
        self:bakeShared("_box", "toggle.box", cw + 2 * m, ch + 2 * m, function(box)
            Shape.panel(box, m, m, cw, ch, { radius = rs, outline = { col = c.outline, width = ow }, top = c.surface, bottom = c.surface2 })
        end, { m = m })
        self:bakeShared("_check", "toggle.check", cw + 2 * m, ch + 2 * m, function(chk)
            bakeCheck(chk, m, cw, ch, c.primary)
        end, { m = m })
    else
        local function pill(field, top, bot)
            self:bakeShared(field, "toggle.pill", cw + 2 * m, ch + 2 * m, function(cv)
                Shape.panel(cv, m, m, cw, ch, { radius = ch * 0.5, outline = { col = c.outline, width = ow }, top = top, bottom = bot })
            end, { m = m }, top, bot)
        end
        pill("_trackOff", c.track, U.shade(c.track, 0.92))
        pill("_trackOn", c.primary, c.primary2)
        local kd = ch - 12
        self:bakeShared("_knob", "toggle.knob", kd + 2 * m, kd + 2 * m, function(knob)
            Shape.fillRound(knob, m, m, kd, kd, kd * 0.5, c.outline)
            Shape.fillRound(knob, m + 3, m + 3, kd - 6, kd - 6, (kd - 6) * 0.5, { 255, 255, 255, 255 })
        end, { m = m, d = kd })
    end
    -- side label (glyph-composed; +50 = the padding the glyph box keeps)
    self._labelInk = self.mgr:measureText(self.eff.font.label, self.text)
    self.h = math.max(ch, self.mgr:textHeight(self.eff.font.label))
    self.w = cw + self.gap + (self._labelInk > 0 and self._labelInk + 50 or 0)
    self:bakeRing()
end

function Toggle:update(ctx)
    Widget.update(self, ctx)
    self._valCur = U.lerp(self._valFrom, self._valTo, Widget.tickTween(self._valT, ctx.dt or 0))
end

function Toggle:draw()
    if not self.visible then return end
    local cw = self._ctrlW
    local ctrlCX = self.x + cw * 0.5
    local ctrlCY = self.y + self.h * 0.5
    local s = self._scaleCur
    local op = self.enabled and 1 or 0.5      -- a disabled toggle fades its label
    -- focus ring around the control
    if self._ring and self._hiCur > 0.01 then
        self:drawPiece(self._ring, self.x + self.w * 0.5, self.y + self.h * 0.5, s, self._hiCur)
    end
    if self.variant == "checkbox" then
        self:drawPiece(self._box, ctrlCX, ctrlCY, s, 1)
        local v = self._valCur
        if v > 0.01 then self:drawPiece(self._check, ctrlCX, ctrlCY, s * (0.6 + 0.4 * v), math.min(1, v)) end
    else
        self:drawPiece(self._trackOff, ctrlCX, ctrlCY, s, 1)
        if self._valCur > 0.01 then self:drawPiece(self._trackOn, ctrlCX, ctrlCY, s, math.min(1, self._valCur)) end
        local kx = U.lerp(self.x + self._ctrlH * 0.5, self.x + cw - self._ctrlH * 0.5, self._valCur)
        self:drawPiece(self._knob, kx, ctrlCY, s, 1)
    end
    if self.text and self.text ~= "" then
        self.mgr:drawTextEx(self.eff.font.label, self.text,
            math.floor(self.x + cw + self.gap), math.floor(ctrlCY + self.mgr:textNudge(self.eff.font.label)),
            self.eff.colors.text, LABEL_BG, op, 1, 1200, "left")
    end
end

return Toggle
