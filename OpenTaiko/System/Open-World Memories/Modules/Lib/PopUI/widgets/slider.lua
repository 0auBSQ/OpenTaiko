---@diagnostic disable: undefined-global, lowercase-global, need-check-nil
-- PopUI slider: rounded track + accent fill + springy knob. Click/drag, or Left/Right when focused.
--   ui:slider{ x=, y=, w=, min=0, max=100, step=1, value=50, showValue=true, onChange=function(v) end }

local Widget = require("PopUI.widget")
local U      = require("PopUI.util")
local Shape  = require("PopUI.shape")

local Slider = setmetatable({}, { __index = Widget })
Slider.__index = Slider

function Slider.new(o)
    o = o or {}
    o.w = o.w or 420; o.h = o.h or 46
    local self = Widget.new(o)
    setmetatable(self, Slider)
    self.min = o.min or 0
    self.max = o.max or 100
    self.step = o.step or 1
    self.value = o.value or self.min
    if o.showValue == nil or o.showValue == true then
        self.showValue = function (self) return tostring(math.floor(self.value)) end
    else
        self.showValue = o.showValue
    end
    self._knobBoost = 0
    return self
end

function Slider:_snap(v)
    v = U.clamp(v, self.min, self.max)
    if self.step and self.step > 0 then v = self.min + math.floor((v - self.min) / self.step + 0.5) * self.step end
    return U.clamp(v, self.min, self.max)
end

function Slider:_frac() return (self.max > self.min) and (self.value - self.min) / (self.max - self.min) or 0 end

function Slider:setValue(v, silent)
    local nv = self:_snap(v)
    if nv == self.value then return false end
    self.value = nv
    if not silent then
        if self.onChange and self.onChange(self.value, self) then return end
        self:playSfx("move")
    end
    return true
end

-- silence click sound
function Slider:onActivate() end  -- Decide handled in onDecide(); onActivate() does nothing

function Slider:restyle()
    self:resolveStyle()
    local c = self.eff.colors
    local th = math.floor(self.h * 0.5)          -- track height
    local ow = math.max(3, self.eff.outlineWidth - 1)
    local m = 4  -- margin
    self._trackH = th
    self._trackR = math.floor(th * 0.5)          -- the knob travels (and the fill spans) within [x+R, x+w-R]
    self._ow = ow
    self._m = m
    -- track (rounded pill) — reuse the canvas across restyles (no leak)
    local w = self.w
    self:bakeShared("_track", "slider.track", w + 2 * m, th + 2 * m, function(track)
        Shape.panel(track, m, m, w, th, { radius = th * 0.5, outline = { col = c.outline, width = ow }, top = c.track, bottom = U.shade(c.track, 0.92) })
    end, { m = m }, th, ow)
    -- fill = two static pieces (baked once):
    --   _capL = rounded-left cap (left semicircle; radius == track interior radius → never pokes the border)
    --   _mid  = a flat vertical-gradient strip, scaled horizontally to reach the knob (flat → no distortion)
    self._fillH = math.max(2, th - 2 * ow)
    self._capW = math.ceil(self._fillH * 0.5)
    local capW, fillH = self._capW, self._fillH
    self:bakeShared("_capL", "slider.cap", capW, fillH, function(cap)
        Shape.fillFillBar(cap, 0, 0, capW, fillH, fillH * 0.5, c.primary, c.primary2)
    end)
    local STRIP = 8
    self:bakeShared("_mid", "slider.mid", STRIP, fillH, function(mid)
        Shape.fillRoundGradient(mid, 0, 0, STRIP, fillH, 0, c.primary, c.primary2)
    end, { w = STRIP })
    -- knob: an anti-aliased circle (smooth edge) with a face + tintable pip
    local kd = self.h  -- knob diameter
    self:bakeShared("_knob", "slider.knob", kd + 2 * m, kd + 2 * m, function(knob)
        Shape.fillRoundAA(knob, m, m, kd, kd, kd * 0.5, c.outline)                                  -- smooth outer circle
        Shape.fillRound(knob, m + ow, m + ow, kd - 2 * ow, kd - 2 * ow, (kd - 2 * ow) * 0.5, c.surface)
    end, { m = m, d = kd }, ow)
    local pipd = math.max(2, math.floor(kd * 0.30))
    self:bakeShared("_pip", "slider.pip", pipd + 2 * m, pipd + 2 * m, function(pip)
        Shape.fillRound(pip, m, m, pipd, pipd, pipd * 0.5, { 255, 255, 255, 255 })
    end, { m = m, d = pipd })
    self:bakeRing()
end

function Slider:onCapturing(silence)
    if not silence then self:playSfx("click") end
end
function Slider:onEndCapturing(silence)
    if not silence then self:playSfx("cancel") end
end

-- consume Left/Right (also for pad Left/Right in capturing mode), so focus stays on the chooser
-- consume Cancel to quit capturing mode
function Slider:onDecide() self:setCapturing(not self.capturing); return true end
function Slider:onCancel()
    if self.capturing then self:setCapturing(false); return true end
    return false
end
function Slider:onNavLeft(forPad)
    if not forPad or self.capturing then self:setCapturing(true, true); self:setValue(self.value - self.step); return true end
    return false
end
function Slider:onNavRight(forPad)
    if not forPad or self.capturing then self:setCapturing(true, true); self:setValue(self.value + self.step); return true end
    return false
end

function Slider:update(ctx)
    Widget.update(self, ctx)
    local R = self._trackR
    local capturing, silence = self.capturing, false
    if not self.focused then capturing = false end
    if ctx.mPressing then
        capturing = self.pressed
        if self.pressed then
            local frac = (ctx.mx - (self.x + R)) / math.max(1, self.w - 2 * R)
            if self:setValue(self.min + U.clamp(frac, 0, 1) * (self.max - self.min)) then
                silence = true
            end
        end
    end
    self:setCapturing(capturing, silence)
end

function Slider:draw()
    if not self.visible then return end
    local s = self._scaleCur
    local cy = math.floor(self.y + self.h * 0.5)
    local R = self._trackR
    if self._ring and self._hiCur > 0.01 then
        self._ring.canvas:SetOpacity(self._hiCur); self._ring.canvas:SetScale(1, 1)
        self._ring.canvas:DrawAtAnchor(math.floor(self.x + self.w * 0.5), cy, "center")
    end
    self._track.canvas:SetColor(1, 1, 1); self._track.canvas:SetOpacity(1); self._track.canvas:SetScale(1, 1)
    self._track.canvas:DrawAtAnchor(math.floor(self.x + self.w * 0.5), cy, "center")
    -- fill: static cap (fixed) + flat mid (scaled to the knob). No per-frame re-bake, no rounded scaling.
    local frac = self:_frac()
    local kx = self.x + R + frac * (self.w - 2 * R)
    local top = cy - math.floor(self._fillH * 0.5)
    local capX = self.x + self._ow
    self._capL.canvas:SetColor(1, 1, 1); self._capL.canvas:SetOpacity(1); self._capL.canvas:SetScale(1, 1)
    self._capL.canvas:Draw(math.floor(capX), top)
    local midStart = capX + self._capW - 2          -- 2px overlap with the cap → no seam
    local midW = kx - midStart
    if midW > 0 then
        self._mid.canvas:SetColor(1, 1, 1); self._mid.canvas:SetOpacity(1)
        self._mid.canvas:SetScale(midW / self._mid.w, 1)
        self._mid.canvas:Draw(math.floor(midStart), top)
    end
    local pipColor = self._hiCapCur > 0.01
        and U.lerpColor(self.eff.colors.primary, self.eff.colors.accent, self._hiCapCur)
        or self.eff.colors.primary
    for i, v in ipairs{ { self._knob, { 255, 255, 255, 255 } }, { self._pip, pipColor } } do
        local part, color = table.unpack(v)
        local r, g, b, a = table.unpack(color)
        part.canvas:SetColor(r / 255, g / 255, b / 255); part.canvas:SetOpacity(a / 255); part.canvas:SetScale(s, s)
        part.canvas:DrawAtAnchor(math.floor(kx), cy, "center")
    end
    if self.showValue then
        local sz = self.eff.font.small
        local y = cy - math.floor(self.mgr:textHeight(sz) * 0.5) + self.mgr:textNudge(sz)
        local textColor = self._hiCapCur > 0.01
            and U.lerpColor(self.eff.colors.text, self.eff.colors.primary2, self._hiCapCur)
            or self.eff.colors.text
        -- glyph-atlas draw: value changes every drag-frame; correct advances + bounded glyph cache
        self.mgr:drawText(sz, self:showValue(), math.floor(self.x + self.w + 18), y, textColor)
    end
end

return Slider
