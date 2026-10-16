---@diagnostic disable: undefined-global, lowercase-global, need-check-nil
-- PopUI button
--   ui:button{ text="Play", x=, y=, w=, h=, accent=true, onClick=function(b) ... end }
--   ui:button{ icon="play", ... }   -- language-agnostic transport glyph instead of text (Shape.icon)
-- w/h auto-size to the label when omitted.

local Widget = require("PopUI.widget")
local U      = require("PopUI.util")
local Shape  = require("PopUI.shape")

local Button = setmetatable({}, { __index = Widget })
Button.__index = Button

local CLEAR = { 255, 255, 255, 0 }

function Button.new(o)
    o = o or {}
    o._autoW = (o.w == nil)
    o._autoH = (o.h == nil)
    o.w = o.w or 240; o.h = o.h or 88
    local self = Widget.new(o)
    setmetatable(self, Button)
    self.text = o.text or "Button"
    self.icon = o.icon                 -- transport glyph drawn instead of the text (Shape.icon kinds)
    self.accent = o.accent or false
    self.padX = o.padX or 44
    self.padY = o.padY or 26
    return self
end

-- a new text re-sizes (and re-bakes) the button only when it sizes itself to its label
function Button:setText(t)
    t = tostring(t)
    if t == self.text then return end
    self.text = t
    if self._autoW and self.mgr then self:restyle() end
end
function Button:setIcon(k)
    if self.icon == k then return end
    self.icon = k; self:restyle()
end

function Button:restyle()
    self:resolveStyle()
    local c = self.eff.colors
    local size = self.eff.font.button
    -- text colours: dark outlined text on light face, white outlined text on accent face
    self._fg = self.accent and c.textOnAccent or c.text
    self._bg = self.accent and U.shade(c.primary2, 0.55) or U.withAlpha({ 255, 255, 255 }, 200)
    -- glyph-composed label: measure for auto-sizing (+50 = the padding the glyph box keeps)
    local ink = self.mgr:measureText(size, self.icon and "" or self.text)
    if self._autoW then self.w = math.max(140, ink + 50 + self.padX * 2) end
    if self._autoH then self.h = math.max(64, self.mgr:textHeight(size) + self.padY * 2) end
    if self.accent then self:bakeBody(c.primary, c.primary2) else self:bakeBody(c.surface, c.surface2) end
    self:bakeRing()
    if self.icon then
        -- bake the glyph in the face's text colour; rebaked with the body on accent flips
        local isz = math.floor(math.min(self.w, self.h) * 0.46)
        local pad = 8
        local icon, fg = self.icon, self._fg
        self:bakeShared("_icon", "button.icon", isz + 2 * pad, isz + 2 * pad, function(cv)
            Shape.icon(cv, icon, (isz + 2 * pad) * 0.5, (isz + 2 * pad) * 0.5, isz, fg)
        end, nil, icon, fg)
    else
        self:dropPiece("_icon")
    end
end

function Button:drawContent(cx, cy, s)
    -- on the muted face of a disabled button the label is the plain text colour, half see-through
    local muted = not self.enabled and self._bodyOff ~= nil
    if self.icon and self._icon then
        self:drawPiece(self._icon, cx, cy, s, self.enabled and 1.0 or 0.5)
        return
    end
    if not self.text or self.text == "" then return end
    local size = self.eff.font.button
    self.mgr:drawTextEx(size, self.text, cx, cy + self.mgr:textNudge(size),
        muted and self.eff.colors.text or self._fg, muted and CLEAR or self._bg,
        self.enabled and 1.0 or 0.5, s, 700, "center")
end

return Button
