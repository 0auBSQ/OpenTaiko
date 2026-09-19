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

local function drop(self, field)
    local e = self[field]
    if e == nil then return end
    if e.key then self.mgr.releaseBaked(e.key) else self.mgr.releaseCanvas(e.canvas) end
    self[field] = nil
end

function Panel:restyle()
    self:resolveStyle()
    local c = self.eff.colors
    self:bakeBody(c.surface, c.surface2)
    if self.title and self.title ~= "" then
        -- glyph-composed title; +50 = the box padding a GetText texture carried
        local ink = math.min(self.mgr:measureText(self.eff.font.title, self.title) + 50, self.w - 40)
        local tw = ink + 56
        local th = self.mgr:textHeight(self.eff.font.title) + 22
        local m = 6
        local rs, ow = self.eff.radiusSmall, self.eff.outlineWidth
        self:bakeShared("_titlePill", "panel.title", tw + 2 * m, th + 2 * m, function(cv)
            Shape.panel(cv, m, m, tw, th, { radius = rs, outline = { col = c.outline, width = ow }, top = c.primary, bottom = c.primary2, gloss = c.gloss })
        end, { m = m, w = tw, h = th })
    else
        drop(self, "_titlePill")
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
        self._titlePill.canvas:SetColor(1, 1, 1); self._titlePill.canvas:SetOpacity(1); self._titlePill.canvas:SetScale(1, 1)
        self._titlePill.canvas:DrawAtAnchor(tx, ty, "center")
        local c = self.eff.colors
        -- the text box is anchored below the pill's centre by the nudge so its glyph line sits on it
        self.mgr:drawTextEx(self.eff.font.title, self.title, tx, ty + self.mgr:textNudge(self.eff.font.title),
            c.textOnAccent, U.shade(c.primary2, 0.5), 1, 1, self.w - 40, "center")
    end
end

return Panel
