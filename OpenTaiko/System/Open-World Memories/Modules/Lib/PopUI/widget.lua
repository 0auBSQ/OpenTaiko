---@diagnostic disable: undefined-global, lowercase-global, need-check-nil
-- PopUI/widget.lua — the base class every widget extends. Owns the shared interaction state machine
-- (idle/hover/press/focus/disabled), the bouncy scale + highlight tweens (reused COUNTERs, no per-frame
-- allocation), accurate rounded hit-testing, and the draw scaffold (baked body + focus ring + content).
-- Mouse-hover and keyboard/gamepad-focus drive ONE shared "highlight", so both look identical.

local U     = require("PopUI.util")
local Shape = require("PopUI.shape")

local Widget = {}
Widget.__index = Widget

function Widget.new(o)
    local self = setmetatable(o or {}, Widget)
    self.x = self.x or 0; self.y = self.y or 0
    self.w = self.w or 160; self.h = self.h or 64
    if self.enabled == nil then self.enabled = true end
    if self.visible == nil then self.visible = true end
    if self.focusable == nil then self.focusable = true end
    self.hovered, self.focused, self.pressed = false, false, false
    self._scaleCur, self._scaleFrom, self._scaleTo = 1.0, 1.0, 1.0
    self._hiCur, self._hiFrom, self._hiTo = 0.0, 0.0, 0.0
    return self
end

-- called by the manager right after creation
function Widget:init(mgr)
    self.mgr = mgr
    self.eff = mgr:resolveTheme(self.style)
    self._scaleC = COUNTER:EmptyCounter()
    self._hiC = COUNTER:EmptyCounter()
    self:restyle()
    return self
end

-- ── geometry ────────────────────────────────────────────────────────────────────
function Widget:centerX() return self.x + self.w * 0.5 end
function Widget:centerY() return self.y + self.h * 0.5 end
function Widget:radius() return math.min(self.eff.radius, self.w * 0.5, self.h * 0.5) end

function Widget:hitTest(px, py)
    return U.pointInRoundRect(px, py, self.x, self.y, self.w, self.h, self:radius())
end

function Widget:isHighlighted() return self.enabled and (self.hovered or self.focused) end

-- ── baking (only place canvases are built; called on construct + restyle) ──────────
-- a transparent canvas sized to the body + a shadow margin, REUSING `old` when the size matches (no leak)
function Widget:_newBodyCanvas(old)
    local sh = self.eff.shadow
    local m = math.ceil((sh.layers or 4) * (sh.grow or 3) + math.max(math.abs(sh.dx or 0), math.abs(sh.dy or 6)) + 2)
    self._m = m
    return self.mgr:reuseCanvas(old, math.floor(self.w + 2 * m), math.floor(self.h + 2 * m)), m
end

-- bake the standard body: soft shadow + bordered gradient panel + gloss. faceTop/faceBottom override the
-- theme surface gradient (e.g. an accent button passes primary/primary2).
function Widget:bakeBody(faceTop, faceBottom)
    local c = self.eff.colors
    local cv, m = self:_newBodyCanvas(self._body and self._body.canvas)
    Shape.dropShadow(cv, m, m, self.w, self.h, self:radius(), { col = c.shadow,
        dx = self.eff.shadow.dx, dy = self.eff.shadow.dy, layers = self.eff.shadow.layers, grow = self.eff.shadow.grow })
    Shape.panel(cv, m, m, self.w, self.h, {
        radius = self:radius(),
        outline = { col = c.outline, width = self.eff.outlineWidth },
        top = faceTop or c.surface, bottom = faceBottom or c.surface2,
        gloss = self.eff.gloss and c.gloss or nil,
    })
    cv:Upload()
    self._body = { canvas = cv, m = m }
end

-- bake the focus/hover ring (gold rounded outline) as its own canvas, drawn over the body when highlighted
function Widget:bakeRing()
    local c = self.eff.colors
    local cv, m = self:_newBodyCanvas(self._ring and self._ring.canvas)
    local rw = self.eff.outlineWidth + 4
    Shape.fillRoundAA(cv, m - 2, m - 2, self.w + 4, self.h + 4, self:radius() + 2, c.focusRing)   -- smooth outer
    Shape.fillRound(cv, m - 2 + rw, m - 2 + rw, self.w + 4 - 2 * rw, self.h + 4 - 2 * rw, self:radius() + 2 - rw, { 0, 0, 0, 0 })
    cv:Upload()
    self._ring = { canvas = cv, m = m }
end

-- subclasses override to (re)build their canvases + cached text. Default = a plain body + ring.
function Widget:restyle()
    self.eff = self.mgr:resolveTheme(self.style)
    self:bakeBody()
    self:bakeRing()
end

-- ── interaction ───────────────────────────────────────────────────────────────────
function Widget:_tweenScale(target)
    self._scaleFrom, self._scaleTo = self._scaleCur, target
    local a = self.eff.anim
    local c = self._scaleC
    c.Begin, c.End, c.Interval = 0, 1, math.max(0.0001, a.hoverTime)
    c:SetEasing(a.hoverEase[1], a.hoverEase[2]); c:Start()
end

function Widget:_tweenScaleEase(target, time, ease)
    self._scaleFrom, self._scaleTo = self._scaleCur, target
    local c = self._scaleC
    c.Begin, c.End, c.Interval = 0, 1, math.max(0.0001, time)
    if ease then c:SetEasing(ease[1], ease[2]) else c:ClearEasing() end
    c:Start()
end

function Widget:_tweenHighlight(target)
    self._hiFrom, self._hiTo = self._hiCur, target
    local c = self._hiC
    c.Begin, c.End, c.Interval = 0, 1, math.max(0.0001, self.eff.anim.highlightTime)
    c:ClearEasing(); c:Start()
end

function Widget:setHover(b)
    if self.hovered == b then return end
    self.hovered = b
    self:_refreshHighlight()
    if b then if self.onHover then self.onHover(self) end
    else if self.onUnhover then self.onUnhover(self) end end
end

function Widget:setFocus(b)
    if self.focused == b then return end
    self.focused = b
    self:_refreshHighlight()
    if b then if self.onFocus then self.onFocus(self) end
    else if self.onBlur then self.onBlur(self) end end
end

function Widget:_refreshHighlight()
    local hi = self:isHighlighted()
    self:_tweenHighlight(hi and 1 or 0)
    if self.noPop then return end                  -- text widgets don't zoom (DrawDirect text can't scale to match)
    if hi and not self.pressed then self:_tweenScale(self.eff.anim.hoverScale)
    elseif not self.pressed then self:_tweenScale(1.0) end
end

function Widget:press()
    if not self.enabled then return end
    self.pressed = true
    if not self.noPop then self:_tweenScaleEase(self.eff.anim.pressScale, self.eff.anim.pressTime, self.eff.anim.pressEase) end
end

-- release; if `inside` and was pressed, fire the activation
function Widget:release(inside)
    if not self.pressed then return end
    self.pressed = false
    if not self.noPop then
        self:_tweenScaleEase(self:isHighlighted() and self.eff.anim.hoverScale or 1.0,
            self.eff.anim.releaseTime, { "OUT", "BACK" })
    end
    if inside and self.enabled then self:onActivate() end
end

-- keyboard/gamepad activation: snap to the squish then boing back in one shot (a same-frame press+release
-- would cancel the animation), then fire. Used for Decide so Enter shows the same feedback as a click.
function Widget:keyActivate()
    if not self.enabled then return end
    if not self.noPop then
        self._scaleCur, self._scaleFrom = self.eff.anim.pressScale, self.eff.anim.pressScale
        self:_tweenScaleEase(self:isHighlighted() and self.eff.anim.hoverScale or 1.0,
            self.eff.anim.releaseTime, { "OUT", "BACK" })
    end
    self:onActivate()
end

-- default activation: buttons override or rely on onClick
function Widget:onActivate()
    if self.onClick then self.onClick(self) end
    if self.mgr then self.mgr:playSfx("click") end
end

function Widget:setEnabled(b) self.enabled = b; self.focusable = b and (self._focusableWant ~= false); self:_refreshHighlight() end
function Widget:setVisible(b) self.visible = b end

-- free the GPU canvases this widget baked (LuaCanvas has no finalizer). Every baked surface is stored as a
-- `{ canvas = <LuaCanvas>, ... }` field (_body/_ring/_track/_knob/_capL/…); cached GetText textures are stored
-- bare (not wrapped) and are owned by the font cache, so this leaves them alone. Call before dropping a UI.
function Widget:dispose()
    local keys = {}
    for k, v in pairs(self) do
        if type(v) == "table" and v.canvas ~= nil then keys[#keys + 1] = k end
    end
    for _, k in ipairs(keys) do pcall(function() self[k].canvas:Dispose() end); self[k] = nil end
end

-- ── per-frame ───────────────────────────────────────────────────────────────────
function Widget:update(ctx)
    self._scaleC:Tick(); self._hiC:Tick()
    self._scaleCur = U.lerp(self._scaleFrom, self._scaleTo, self._scaleC.Value)
    self._hiCur    = U.lerp(self._hiFrom, self._hiTo, self._hiC.Value)
end

function Widget:draw()
    if not self.visible then return end
    local cx, cy = self:centerX(), self:centerY()
    local s = self._scaleCur
    if self._ring and self._hiCur > 0.01 then
        self._ring.canvas:SetOpacity(self._hiCur)
        self._ring.canvas:SetScale(s, s)
        self._ring.canvas:DrawAtAnchor(math.floor(cx), math.floor(cy), "center")
    end
    if self._body then
        self._body.canvas:SetColor(1, 1, 1)
        self._body.canvas:SetOpacity(self.enabled and 1.0 or 0.5)
        self._body.canvas:SetScale(s, s)
        self._body.canvas:DrawAtAnchor(math.floor(cx), math.floor(cy), "center")
    end
    self:drawContent(cx, cy, s)
end

-- subclasses draw their label/icon/value here
function Widget:drawContent(cx, cy, s) end

return Widget
