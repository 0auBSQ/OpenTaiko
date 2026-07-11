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

function Button:setText(t) self.text = tostring(t); self:restyle() end
function Button:setIcon(k)
    if self.icon == k then return end
    self.icon = k; self:restyle()
end

function Button:restyle()
    self.eff = self.mgr:resolveTheme(self.style)
    local c = self.eff.colors
    local size = self.eff.font.button
    -- text colours: dark outlined text on light face, white outlined text on accent face
    self._fg = self.accent and c.textOnAccent or c.text
    self._bg = self.accent and U.shade(c.primary2, 0.55) or U.withAlpha({ 255, 255, 255 }, 200)
    -- glyph-composed label: measure for auto-sizing (+50 = the box padding a GetText texture carried)
    local ink = self.mgr:measureText(size, self.icon and "" or self.text)
    if self._autoW then self.w = math.max(140, ink + 50 + self.padX * 2) end
    if self._autoH then self.h = math.max(64, self.mgr:textHeight(size) + self.padY * 2) end
    if self.accent then self:bakeBody(c.primary, c.primary2) else self:bakeBody(c.surface, c.surface2) end
    self:bakeRing()
    if self.icon then
        -- bake the glyph in the face's text colour; rebaked with the body on accent flips
        local isz = math.floor(math.min(self.w, self.h) * 0.46)
        local pad = 8
        local cv = self.mgr:reuseCanvas(self._icon and self._icon.canvas, isz + 2 * pad, isz + 2 * pad)
        Shape.icon(cv, self.icon, (isz + 2 * pad) * 0.5, (isz + 2 * pad) * 0.5, isz, self._fg)
        cv:Upload()
        self._icon = { canvas = cv }
    elseif self._icon then
        pcall(function() self._icon.canvas:Dispose() end)
        self._icon = nil
    end
end

function Button:drawContent(cx, cy, s)
    if self.icon and self._icon then
        local cv = self._icon.canvas
        cv:SetOpacity(self.enabled and 1.0 or 0.5)
        cv:SetScale(s, s)
        cv:DrawAtAnchor(cx, cy, "center")
        cv:SetScale(1, 1); cv:SetOpacity(1)
        return
    end
    if not self.text or self.text == "" then return end
    local size = self.eff.font.button
    self.mgr:drawTextEx(size, self.text, cx, cy + self.mgr:textNudge(size), self._fg, self._bg,
        self.enabled and 1.0 or 0.5, s, 700, "center")
end

return Button
