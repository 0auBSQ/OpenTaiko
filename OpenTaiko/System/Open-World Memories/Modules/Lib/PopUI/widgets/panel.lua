---@diagnostic disable: undefined-global, lowercase-global, need-check-nil
-- PopUI panel: a rounded, shadowed container with an optional title "tab". Decorative (non-focusable);
-- the demo positions child widgets inside panel:content().
--   ui:panel{ x=, y=, w=, h=, title="Settings" }

local Widget = require("PopUI.widget")
local U      = require("PopUI.util")
local Shape  = require("PopUI.shape")

local Panel = setmetatable({}, { __index = Widget })
Panel.__index = Panel

function Panel.new(o)
    o = o or {}
    o.focusable = false; o._focusableWant = false
    o.w = o.w or 600; o.h = o.h or 400
    local self = Widget.new(o)
    setmetatable(self, Panel)
    self.title = o.title
    self.pad = o.pad or 28
    return self
end

function Panel:restyle()
    self.eff = self.mgr:resolveTheme(self.style)
    local c = self.eff.colors
    self:bakeBody(c.surface, c.surface2)
    if self.title and self.title ~= "" then
        local txt = self.mgr:renderText(self.eff.font.title, self.title, c.textOnAccent, U.shade(c.primary2, 0.5), false, self.w - 40)
        self._titleText = txt
        local tw = (txt.Width or 100) + 56
        local th = (txt.Height or 30) + 22
        local m = 6
        local cv = self.mgr:reuseCanvas(self._titlePill and self._titlePill.canvas, tw + 2 * m, th + 2 * m)
        Shape.panel(cv, m, m, tw, th, { radius = self.eff.radiusSmall, outline = { col = c.outline, width = self.eff.outlineWidth }, top = c.primary, bottom = c.primary2, gloss = c.gloss })
        cv:Upload()
        self._titlePill = { canvas = cv, m = m, w = tw, h = th }
    else
        if self._titlePill then self._titlePill.canvas:Dispose(); self._titlePill = nil end
    end
end

function Panel:update(ctx) end  -- static

function Panel:content()
    local topPad = self.pad + (self._titlePill and (self._titlePill.h * 0.5) or 0)
    return self.x + self.pad, self.y + topPad, self.w - 2 * self.pad, self.h - topPad - self.pad
end

function Panel:draw()
    if not self.visible then return end
    local cx, cy = self:centerX(), self:centerY()
    self._body.canvas:SetColor(1, 1, 1); self._body.canvas:SetOpacity(1); self._body.canvas:SetScale(1, 1)
    self._body.canvas:DrawAtAnchor(math.floor(cx), math.floor(cy), "center")
    if self._titlePill then
        local tx = math.floor(self.x + self.w * 0.5)
        local ty = math.floor(self.y)
        self._titlePill.canvas:SetColor(1, 1, 1); self._titlePill.canvas:SetOpacity(1)
        self._titlePill.canvas:DrawAtAnchor(tx, ty, "center")
        self._titleText:DrawAtAnchor(tx, ty, "center")
    end
end

return Panel
