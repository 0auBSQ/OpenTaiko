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
    self.showValue = (o.showValue ~= false)
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
    if nv == self.value then return end
    self.value = nv
    if not silent and self.onChange then self.onChange(self.value, self) end
end

function Slider:onActivate() end  -- Decide does nothing; arrows adjust

function Slider:restyle()
    self.eff = self.mgr:resolveTheme(self.style)
    local c = self.eff.colors
    local th = math.floor(self.h * 0.5)          -- track height
    local ow = math.max(3, self.eff.outlineWidth - 1)
    local m = 4
    self._trackH = th
    self._trackR = math.floor(th * 0.5)          -- the knob travels (and the fill spans) within [x+R, x+w-R]
    self._ow = ow
    self._m = m
    -- track (rounded pill) — reuse the canvas across restyles (no leak)
    local track = self.mgr:reuseCanvas(self._track and self._track.canvas, self.w + 2 * m, th + 2 * m)
    Shape.panel(track, m, m, self.w, th, { radius = th * 0.5, outline = { col = c.outline, width = ow }, top = c.track, bottom = U.shade(c.track, 0.92) })
    track:Upload(); self._track = { canvas = track, m = m }
    -- fill = two static pieces (baked once):
    --   _capL = rounded-left cap (left semicircle; radius == track interior radius → never pokes the border)
    --   _mid  = a flat vertical-gradient strip, scaled horizontally to reach the knob (flat → no distortion)
    self._fillH = math.max(2, th - 2 * ow)
    self._capW = math.ceil(self._fillH * 0.5)
    local cap = self.mgr:reuseCanvas(self._capL and self._capL.canvas, self._capW, self._fillH)
    Shape.fillFillBar(cap, 0, 0, self._capW, self._fillH, self._fillH * 0.5, c.primary, c.primary2)
    cap:Upload(); self._capL = { canvas = cap }
    local STRIP = 8
    local mid = self.mgr:reuseCanvas(self._mid and self._mid.canvas, STRIP, self._fillH)
    Shape.fillRoundGradient(mid, 0, 0, STRIP, self._fillH, 0, c.primary, c.primary2)
    mid:Upload(); self._mid = { canvas = mid, w = STRIP }
    -- knob: an anti-aliased circle (smooth edge) with a face + accent pip
    local kd = self.h
    local knob = self.mgr:reuseCanvas(self._knob and self._knob.canvas, kd + 2 * m, kd + 2 * m)
    Shape.fillRoundAA(knob, m, m, kd, kd, kd * 0.5, c.outline)                                  -- smooth outer circle
    Shape.fillRound(knob, m + ow, m + ow, kd - 2 * ow, kd - 2 * ow, (kd - 2 * ow) * 0.5, c.surface)
    local pip = math.max(2, math.floor(kd * 0.30))
    Shape.fillRound(knob, m + (kd - pip) * 0.5, m + (kd - pip) * 0.5, pip, pip, pip * 0.5, c.accent)
    knob:Upload(); self._knob = { canvas = knob, m = m, d = kd }
    self:bakeRing()
end

function Slider:update(ctx)
    self._scaleC:Tick(); self._hiC:Tick()
    self._scaleCur = U.lerp(self._scaleFrom, self._scaleTo, self._scaleC.Value)
    self._hiCur    = U.lerp(self._hiFrom, self._hiTo, self._hiC.Value)
    local R = self._trackR
    if self.pressed and ctx.mPressing then
        local frac = (ctx.mx - (self.x + R)) / math.max(1, self.w - 2 * R)
        self:setValue(self.min + U.clamp(frac, 0, 1) * (self.max - self.min))
        self.mgr:playSfx("move")
    end
    if self.focused then
        if ctx.navLeft then self:setValue(self.value - self.step); self.mgr:playSfx("move") end
        if ctx.navRight then self:setValue(self.value + self.step); self.mgr:playSfx("move") end
    end
end

function Slider:draw()
    if not self.visible then return end
    local s = self._scaleCur
    local cy = math.floor(self.y + self.h * 0.5)
    local R = self._trackR
    if self._ring and self._hiCur > 0.01 then
        self._ring.canvas:SetOpacity(self._hiCur)
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
    self._knob.canvas:SetColor(1, 1, 1); self._knob.canvas:SetOpacity(1); self._knob.canvas:SetScale(s, s)
    self._knob.canvas:DrawAtAnchor(math.floor(kx), cy, "center")
    if self.showValue then
        local sz = self.eff.font.small
        local y = cy - math.floor(self.mgr:textHeight(sz) * 0.5) + self.mgr:textNudge(sz)
        -- glyph-atlas draw: value changes every drag-frame; correct advances + bounded glyph cache
        self.mgr:drawText(sz, tostring(math.floor(self.value)), math.floor(self.x + self.w + 18), y, self.eff.colors.text)
    end
end

return Slider
