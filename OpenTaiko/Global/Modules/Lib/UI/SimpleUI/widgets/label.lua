---@diagnostic disable: undefined-global, lowercase-global, need-check-nil
-- SimpleUI label: non-interactive text. Optional rounded "chip" background.
--   ui:label{ text="Hello", x=, y=, size="title"|"label"|"small"|number, color={r,g,b,a}, align="center", chip=true }

local Widget = require("UI.SimpleUI.widget")
local U      = require("UI.SimpleUI.util")
local Shape  = require("UI.SimpleUI.shape")

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

-- a new text re-measures the label (and its chip); the theme is not resolved again
function Label:setText(t)
    t = tostring(t)
    if t == self.text then return end
    self.text = t
    if self.eff then self:_layout() end
end

function Label:_fontSize()
    if type(self.size) == "number" then return self.size end
    return self.eff.font[self.size] or self.eff.font.label
end

function Label:restyle()
    self:resolveStyle()
    local c = self.eff.colors
    self._fg = self.color or c.text
    -- chip labels keep a black outline; plain labels draw with none
    self._bg = (self.chip and self.eff.textOutline ~= false) and { 0, 0, 0, 255 } or U.withAlpha(c.surface, 0)
    self:_layout()
end

function Label:_layout()
    local c = self.eff.colors
    local sz = self:_fontSize()
    -- the glyph box: the ink + 50 px of padding, squeezed down to maxWidth
    self.w = math.min(self.mgr:measureText(sz, self.text) + 50, self.maxWidth)
    self.h = self.mgr:textHeight(sz)
    if self.chip then
        self._chipW = self.w + self.padX * 2
        self._chipH = self.h + self.padY * 2
        local m = 6
        local cw, ch, rs, ow = self._chipW, self._chipH, self.eff.radiusSmall, math.max(2, self.eff.outlineWidth - 2)
        self:bakeShared("_chip", "label.chip", cw + 2 * m, ch + 2 * m, function(cv)
            Shape.panel(cv, m, m, cw, ch, {
                radius = rs, outline = { col = c.outline, width = ow },
                top = c.surface, bottom = c.surface2,
            })
        end, { m = m })
    else
        self:dropPiece("_chip")
    end
end

function Label:update(ctx) end  -- static; no anim

function Label:draw()
    if not self.visible then return end
    local x, y = math.floor(self.x), math.floor(self.y)
    if self.chip then
        -- the chip is centred on the glyph line, not on the text box (which carries the bottom padding)
        local lineH = self.mgr:gfont(self:_fontSize()).LineHeight
        self:drawPiece(self._chip, self.x + self.w * 0.5, self.y + lineH * 0.5, 1, 1)
    end
    local anchor = (self.align == "center") and "top" or (self.align == "right") and "topright" or "topleft"
    self.mgr:drawTextEx(self:_fontSize(), self.text, x, y, self._fg, self._bg, 1, 1, self.maxWidth, anchor)
end

return Label
