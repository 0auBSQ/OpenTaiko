---@diagnostic disable: undefined-global, lowercase-global, need-check-nil
-- PopUI button
--   ui:button{ text="Play", x=, y=, w=, h=, accent=true, onClick=function(b) ... end }
-- w/h auto-size to the label when omitted.

local Widget = require("PopUI.widget")
local U      = require("PopUI.util")

local Button = setmetatable({}, { __index = Widget })
Button.__index = Button

function Button.new(o)
    o = o or {}
    o._autoW = (o.w == nil)
    o._autoH = (o.h == nil)
    o.w = o.w or 240; o.h = o.h or 88
    local self = Widget.new(o)
    setmetatable(self, Button)
    self.text = o.text or "Button"
    self.accent = o.accent or false
    self.padX = o.padX or 44
    self.padY = o.padY or 26
    return self
end

function Button:setText(t) self.text = tostring(t); self:restyle() end

function Button:restyle()
    self.eff = self.mgr:resolveTheme(self.style)
    local c = self.eff.colors
    local size = self.eff.font.button
    -- text colours: dark outlined text on light face, white outlined text on accent face
    self._fg = self.accent and c.textOnAccent or c.text
    self._bg = self.accent and U.shade(c.primary2, 0.55) or U.withAlpha({ 255, 255, 255 }, 200)
    -- centered=false: a centered GetText pads the texture to the full maxWidth (which would balloon the
    -- auto-sized button). We measure the true text size and centre it ourselves via DrawAtAnchor.
    self._label = self.mgr:renderText(size, self.text, self._fg, self._bg, false, 700)
    -- auto-size to fit the label
    if self._autoW then self.w = math.max(140, (self._label.Width or 80) + self.padX * 2) end
    if self._autoH then self.h = math.max(64, (self._label.Height or 30) + self.padY * 2) end
    if self.accent then self:bakeBody(c.primary, c.primary2) else self:bakeBody(c.surface, c.surface2) end
    self:bakeRing()
end

function Button:drawContent(cx, cy, s)
    if not self._label then return end
    self._label:SetScale(s, s)
    self._label:SetOpacity(self.enabled and 1.0 or 0.5)
    self._label:DrawAtAnchor(math.floor(cx), math.floor(cy + self.mgr:textNudge(self.eff.font.button)), "center")
end

return Button
