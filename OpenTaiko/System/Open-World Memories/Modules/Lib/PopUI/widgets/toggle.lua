---@diagnostic disable: undefined-global, lowercase-global, need-check-nil
-- PopUI toggle / checkbox: a control + side label. The knob/check springs on change.
--   ui:toggle{ text="Fullscreen", x=, y=, value=true, onChange=function(v) end }
--   ui:checkbox{ text="Mute", x=, y=, value=false, onChange=fn }   (variant="checkbox")

local Widget = require("PopUI.widget")
local U      = require("PopUI.util")
local Shape  = require("PopUI.shape")

local Toggle = setmetatable({}, { __index = Widget })
Toggle.__index = Toggle

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
    local cc = self._valC
    cc.Begin, cc.End, cc.Interval = 0, 1, 0.22
    cc:SetEasing("OUT", "BACK"); cc:Start()
    if not silent then
        if self.onChange then self.onChange(self.value, self) end
        self.mgr:playSfx("toggle")
    end
end

function Toggle:onActivate()
    self:setValue(not self.value)
end

function Toggle:restyle()
    self.eff = self.mgr:resolveTheme(self.style)
    self._valC = self._valC or COUNTER:EmptyCounter()
    local c = self.eff.colors
    local ch = (self.variant == "checkbox") and 48 or 48
    local cw = (self.variant == "checkbox") and 48 or 92
    self._ctrlW, self._ctrlH = cw, ch
    local ow = math.max(3, self.eff.outlineWidth - 1)
    local m = 4
    if self.variant == "checkbox" then
        local box = self.mgr:reuseCanvas(self._box and self._box.canvas, cw + 2 * m, ch + 2 * m)
        Shape.panel(box, m, m, cw, ch, { radius = self.eff.radiusSmall, outline = { col = c.outline, width = ow }, top = c.surface, bottom = c.surface2 })
        box:Upload(); self._box = { canvas = box, m = m }
        local chk = self.mgr:reuseCanvas(self._check and self._check.canvas, cw + 2 * m, ch + 2 * m)
        local a = c.primary
        chk:StrokeLine(m + cw * 0.24, m + ch * 0.52, m + cw * 0.42, m + ch * 0.72, 4, a[1], a[2], a[3], 255)
        chk:StrokeLine(m + cw * 0.42, m + ch * 0.72, m + cw * 0.78, m + ch * 0.26, 4, a[1], a[2], a[3], 255)
        chk:Upload(); self._check = { canvas = chk, m = m }
    else
        local function pill(old, top, bot)
            local cv = self.mgr:reuseCanvas(old, cw + 2 * m, ch + 2 * m)
            Shape.panel(cv, m, m, cw, ch, { radius = ch * 0.5, outline = { col = c.outline, width = ow }, top = top, bottom = bot })
            cv:Upload(); return { canvas = cv, m = m }
        end
        self._trackOff = pill(self._trackOff and self._trackOff.canvas, c.track, U.shade(c.track, 0.92))
        self._trackOn  = pill(self._trackOn and self._trackOn.canvas, c.primary, c.primary2)
        local kd = ch - 12
        local knob = self.mgr:reuseCanvas(self._knob and self._knob.canvas, kd + 2 * m, kd + 2 * m)
        Shape.fillRound(knob, m, m, kd, kd, kd * 0.5, c.outline)
        Shape.fillRound(knob, m + 3, m + 3, kd - 6, kd - 6, (kd - 6) * 0.5, { 255, 255, 255, 255 })
        knob:Upload(); self._knob = { canvas = knob, m = m, d = kd }
    end
    -- side label (glyph-composed; +50 = the box padding a GetText texture carried)
    self._labelInk = self.mgr:measureText(self.eff.font.label, self.text)
    self.h = math.max(ch, self.mgr:textHeight(self.eff.font.label))
    self.w = cw + self.gap + (self._labelInk > 0 and self._labelInk + 50 or 0)
    self:bakeRing()
end

function Toggle:update(ctx)
    self._scaleC:Tick(); self._hiC:Tick(); self._valC:Tick()
    self._scaleCur = U.lerp(self._scaleFrom, self._scaleTo, self._scaleC.Value)
    self._hiCur    = U.lerp(self._hiFrom, self._hiTo, self._hiC.Value)
    self._valCur   = U.lerp(self._valFrom, self._valTo, self._valC.Value)
end

function Toggle:draw()
    if not self.visible then return end
    local cw, ch = self._ctrlW, self._ctrlH
    local ctrlCX = self.x + cw * 0.5
    local ctrlCY = self.y + self.h * 0.5
    local s = self._scaleCur
    -- focus ring around the control
    if self._ring and self._hiCur > 0.01 then
        self._ring.canvas:SetOpacity(self._hiCur); self._ring.canvas:SetScale(s, s)
        self._ring.canvas:DrawAtAnchor(math.floor(self.x + self.w * 0.5), math.floor(self.y + self.h * 0.5), "center")
    end
    if self.variant == "checkbox" then
        self._box.canvas:SetColor(1, 1, 1); self._box.canvas:SetOpacity(1); self._box.canvas:SetScale(s, s)
        self._box.canvas:DrawAtAnchor(math.floor(ctrlCX), math.floor(ctrlCY), "center")
        local v = self._valCur
        if v > 0.01 then
            self._check.canvas:SetOpacity(v); self._check.canvas:SetScale(s * (0.6 + 0.4 * v), s * (0.6 + 0.4 * v))
            self._check.canvas:DrawAtAnchor(math.floor(ctrlCX), math.floor(ctrlCY), "center")
        end
    else
        self._trackOff.canvas:SetOpacity(1); self._trackOff.canvas:SetScale(s, s)
        self._trackOff.canvas:DrawAtAnchor(math.floor(ctrlCX), math.floor(ctrlCY), "center")
        if self._valCur > 0.01 then
            self._trackOn.canvas:SetOpacity(self._valCur); self._trackOn.canvas:SetScale(s, s)
            self._trackOn.canvas:DrawAtAnchor(math.floor(ctrlCX), math.floor(ctrlCY), "center")
        end
        local kx = U.lerp(self.x + ch * 0.5, self.x + cw - ch * 0.5, self._valCur)
        self._knob.canvas:SetColor(1, 1, 1); self._knob.canvas:SetOpacity(1); self._knob.canvas:SetScale(s, s)
        self._knob.canvas:DrawAtAnchor(math.floor(kx), math.floor(ctrlCY), "center")
    end
    if self.text and self.text ~= "" then
        local U160 = { 255, 255, 255, 160 }
        self.mgr:drawTextEx(self.eff.font.label, self.text,
            math.floor(self.x + cw + self.gap), math.floor(ctrlCY + self.mgr:textNudge(self.eff.font.label)),
            self.eff.colors.text, U160, self.enabled and 1 or 0.5, 1, 1200, "left")
    end
end

return Toggle
