---@diagnostic disable: undefined-global, lowercase-global, need-check-nil
-- PopUI label: non-interactive text. Optional rounded "chip" background.
--   ui:label{ text="Hello", x=, y=, size="title"|"label"|"small"|number, color={r,g,b,a}, align="center", chip=true }

local Widget = require("PopUI.widget")
local U      = require("PopUI.util")
local Shape  = require("PopUI.shape")

local Label = setmetatable({}, { __index = Widget })
Label.__index = Label

function Label.new(o)
    o = o or {}
    o.focusable = false
    o._focusableWant = false
    o.w = o.w or 10; o.h = o.h or 10
    local self = Widget.new(o)
    setmetatable(self, Label)
    self.text = o.text or ""
    self.size = o.size or "label"
    self.align = o.align or "left"
    self.color = o.color
    self.maxWidth = o.maxWidth or 1600
    self.chip = o.chip or false
    self.padX = o.padX or 26
    self.padY = o.padY or 12
    return self
end

function Label:setText(t) self.text = tostring(t); self:restyle() end

function Label:_fontSize()
    if type(self.size) == "number" then return self.size end
    return self.eff.font[self.size] or self.eff.font.label
end

function Label:restyle()
    self.eff = self.mgr:resolveTheme(self.style)
    local c = self.eff.colors
    local sz = self:_fontSize()
    self._fg = self.color or c.text
    -- chip labels keep the (former GetText-default) black outline; plain labels draw with none
    self._bg = self.chip and { 0, 0, 0, 255 } or U.withAlpha(c.surface, 0)
    -- the box a GetText texture would have had (ink + 50 padding, squished down to maxWidth)
    self.w = math.min(self.mgr:measureText(sz, self.text) + 50, self.maxWidth)
    self.h = self.mgr:textHeight(sz)
    if self.chip then
        self._chipW = self.w + self.padX * 2
        self._chipH = self.h + self.padY * 2
        local m = 6
        local cv = self.mgr:reuseCanvas(self._chip and self._chip.canvas, self._chipW + 2 * m, self._chipH + 2 * m)
        Shape.panel(cv, m, m, self._chipW, self._chipH, {
            radius = self.eff.radiusSmall, outline = { col = c.outline, width = math.max(2, self.eff.outlineWidth - 2) },
            top = c.surface, bottom = c.surface2,
        })
        cv:Upload()
        self._chip = { canvas = cv, m = m }
    else
        if self._chip then self._chip.canvas:Dispose(); self._chip = nil end
    end
end

function Label:update(ctx) end  -- static; no anim

function Label:draw()
    if not self.visible then return end
    local x, y = math.floor(self.x), math.floor(self.y)
    if self.chip then
        self._chip.canvas:SetColor(1, 1, 1); self._chip.canvas:SetOpacity(1)
        self._chip.canvas:DrawAtAnchor(math.floor(self.x + self.w * 0.5), math.floor(self.y + self.h * 0.5), "center")
    end
    local anchor = (self.align == "center") and "top" or (self.align == "right") and "topright" or "topleft"
    self.mgr:drawTextEx(self:_fontSize(), self.text, x, y, self._fg, self._bg, 1, 1, self.maxWidth, anchor)
end

return Label
