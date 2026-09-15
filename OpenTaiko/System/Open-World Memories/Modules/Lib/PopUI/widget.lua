---@diagnostic disable: undefined-global, lowercase-global, need-check-nil
-- PopUI/widget.lua — the base class every widget extends. Owns the shared interaction state machine
-- (idle/hover/press/focus/disabled), the bouncy scale + highlight tweens (reused COUNTERs, no per-frame
-- allocation), accurate rounded hit-testing, and the draw scaffold (baked body + focus ring + content).
-- Mouse-hover and keyboard/gamepad-focus drive ONE shared "highlight", so both look identical.

local U     = require("PopUI.util")
local Shape = require("PopUI.shape")
local Sfx   = require("PopUI.sfx")

local Widget = {}
Widget.__index = Widget

function Widget.new(o)
    local self = setmetatable(o or {}, Widget)
    self.userTheme = o.theme or o.style or {}
    self.userSfx = o.sfx or {}
    self.x = self.x or 0; self.y = self.y or 0
    self.w = self.w or 160; self.h = self.h or 64
    if self.enabled == nil then self.enabled = true end
    if self.visible == nil then self.visible = true end
    if self.focusable == nil then self.focusable = true end
    self.hovered, self.focused, self.pressed, self.capturing = false, false, false, false
    self._scaleCur, self._scaleFrom, self._scaleTo = 1.0, 1.0, 1.0
    self._hiCur, self._hiFrom, self._hiTo = 0.0, 0.0, 0.0
    self._hiCapCur, self._hiCapFrom, self._hiCapTo = 0.0, 0.0, 0.0
    return self
end

-- called by the manager right after creation
function Widget:init(mgr)
    self.mgr = mgr
    self._scaleC = COUNTER:EmptyCounter()
    self._hiC = COUNTER:EmptyCounter()
    self._hiCapC = COUNTER:EmptyCounter()
    self:restyle()
    return self
end

function Widget:playSfx(name) return Sfx.playSfx(self.sfx, name) end

-- ── geometry ────────────────────────────────────────────────────────────────────
function Widget:centerX() return self.x + self.w * 0.5 end
function Widget:centerY() return self.y + self.h * 0.5 end
function Widget:radius() return math.min(self.eff.radius, self.w * 0.5, self.h * 0.5) end

function Widget:hitTest(px, py)
    return U.pointInRoundRect(px, py, self.x, self.y, self.w, self.h, self:radius())
end

function Widget:isHighlighted() return self.enabled and (self.hovered or self.focused) end
function Widget:isCapturingHighlighted() return self.enabled and (self.capturing) end

-- ── baking (only place canvases are built; called on construct + restyle) ──────────
-- the margin a body canvas keeps around the widget for its shadow
function Widget:_bodyMargin()
    local sh = self.eff.shadow
    local m = math.ceil((sh.layers or 4) * (sh.grow or 3) + math.max(math.abs(sh.dx or 0), math.abs(sh.dy or 6)) + 2)
    self._m = m
    return m
end

-- A baked surface stored in self[field] as { canvas, key, ... }, shared through the manager's cache with
-- every widget that bakes the same thing: `site` names the bake, w/h its canvas, the resolved theme and
-- the extra key parts (colour overrides, variant numbers) complete the key. bake(cv) runs only for a key
-- no live or parked canvas has; an unchanged key keeps the surface the widget already holds. The shared
-- canvas must never be drawn into again — every draw sets the scale/colour/opacity it needs first.
function Widget:bakeShared(field, site, w, h, bake, extra, ...)
    w, h = math.floor(w), math.floor(h)
    local key = site .. "|" .. w .. "x" .. h .. "|" .. self.mgr:themeKey(self.eff) .. "|" .. self.mgr.keyOf(...)
    local old = self[field]
    if old ~= nil and old.key == key then return old end
    if old ~= nil then
        if old.key then self.mgr.releaseBaked(old.key) else self.mgr.releaseCanvas(old.canvas) end
    end
    local e = { canvas = self.mgr:bakedCanvas(key, w, h, bake), key = key }
    if extra then for k, v in pairs(extra) do e[k] = v end end
    self[field] = e
    return e
end

-- bake the standard body: soft shadow + bordered gradient panel + gloss. faceTop/faceBottom override the
-- theme surface gradient (e.g. an accent button passes primary/primary2).
function Widget:bakeBody(faceTop, faceBottom)
    local c = self.eff.colors
    local m = self:_bodyMargin()
    local top, bot = faceTop or c.surface, faceBottom or c.surface2
    local w, h, r = self.w, self.h, self:radius()
    local eff = self.eff
    self:bakeShared("_body", "body", w + 2 * m, h + 2 * m, function(cv)
        Shape.dropShadow(cv, m, m, w, h, r, { col = c.shadow,
            dx = eff.shadow.dx, dy = eff.shadow.dy, layers = eff.shadow.layers, grow = eff.shadow.grow })
        Shape.panel(cv, m, m, w, h, {
            radius = r,
            outline = { col = c.outline, width = eff.outlineWidth },
            top = top, bottom = bot,
            gloss = eff.gloss and c.gloss or nil,
        })
    end, { m = m }, top, bot, r)
end

-- bake the focus/hover ring (gold rounded outline) as its own canvas, drawn over the body when highlighted
function Widget:bakeRing()
    local c = self.eff.colors
    local m = self:_bodyMargin()
    local rw = self.eff.outlineWidth + 4
    local w, h, r = self.w, self.h, self:radius()
    self:bakeShared("_ring", "ring", w + 2 * m, h + 2 * m, function(cv)
        Shape.fillRoundAA(cv, m - 2, m - 2, w + 4, h + 4, r + 2, c.focusRing)   -- smooth outer
        Shape.fillRound(cv, m - 2 + rw, m - 2 + rw, w + 4 - 2 * rw, h + 4 - 2 * rw, r + 2 - rw, { 0, 0, 0, 0 })
    end, { m = m }, r)
end

function Widget:resolveStyle()
    self.eff = self.mgr:resolveTheme(self.userTheme)
    self.sfx = self.mgr:resolveSfx(self.userSfx)
end

-- subclasses override to (re)build their canvases + cached text. Default = a plain body + ring.
function Widget:restyle()
    self:resolveStyle()
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

function Widget:_tweenCapturingHighlight(target)
    self._hiCapFrom, self._hiCapTo = self._hiCapCur, target
    local c = self._hiCapC
    c.Begin, c.End, c.Interval = 0, 1, math.max(0.0001, self.eff.anim.highlightTime)
    c:ClearEasing(); c:Start()
end

function Widget:setHover(b, silent)
    if self.hovered == b then return end
    self.hovered = b
    self:_refreshHighlight()
    if b then
        if self.onHover then self.onHover(self, silent)
        elseif not silent then self:playSfx("hover")
        end
    elseif self.onUnhover then self.onUnhover(self, silent) end
end

function Widget:setFocus(b, silent)
    if self.focused == b then return end
    self.focused = b
    self:_refreshHighlight()
    if b then
        if self.onFocus then self.onFocus(self, silent)
        elseif not silent then self:playSfx("move")
        end
    elseif self.onBlur then self.onBlur(self, silent)
    end
end

function Widget:setCapturing(b, silence)
    if self.capturing == b then return end
    self.capturing = b
    self:_refreshCapturingHighlight()
    if b then if self.onCapturing then self.onCapturing(self, silence) end
    else if self.onEndCapturing then self.onEndCapturing(self, silence) end end
end

function Widget:_refreshHighlight()
    local hi = self:isHighlighted()
    self:_tweenHighlight(hi and 1 or 0)
    if self.noPop then return end                  -- text widgets don't zoom (DrawDirect text can't scale to match)
    if hi and not self.pressed then self:_tweenScale(self.eff.anim.hoverScale)
    elseif not self.pressed then self:_tweenScale(1.0) end
end

function Widget:_refreshCapturingHighlight()
    local hi = self:isCapturingHighlighted()
    self:_tweenCapturingHighlight(hi and 1 or 0)
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
    if self.onClick and self.onClick(self) then return end
    self:playSfx("click")
end

function Widget:setEnabled(b) self.enabled = b; self.focusable = b and (self._focusableWant ~= false); self:_refreshHighlight(); self:_refreshCapturingHighlight() end
function Widget:setVisible(b) self.visible = b end

-- release the GPU canvases this widget baked (LuaCanvas has no finalizer) into the manager's pool, where the
-- next widget of the same size picks them up. Every baked surface is stored as a `{ canvas = <LuaCanvas>, ... }`
-- field (_body/_ring/_track/_knob/_capL/…); cached GetText textures are stored bare (not wrapped) and are
-- owned by the font cache, so this leaves them alone. Call before dropping a UI.
function Widget:dispose()
    local keys = {}
    for k, v in pairs(self) do
        if type(v) == "table" and v.canvas ~= nil then keys[#keys + 1] = k end
    end
    local mgr = self.mgr                                     -- nil before init: dispose outright
    for _, k in ipairs(keys) do
        local e = self[k]
        if mgr and e.key then pcall(mgr.releaseBaked, e.key)
        elseif mgr then pcall(mgr.releaseCanvas, e.canvas)
        else pcall(function() e.canvas:Dispose() end) end
        self[k] = nil
    end
end

-- ── per-frame ───────────────────────────────────────────────────────────────────
function Widget:update(ctx)
    self._scaleC:Tick(); self._hiC:Tick(); self._hiCapC:Tick()
    self._scaleCur = U.lerp(self._scaleFrom, self._scaleTo, self._scaleC.Value)
    self._hiCur    = U.lerp(self._hiFrom, self._hiTo, self._hiC.Value)
    self._hiCapCur = U.lerp(self._hiCapFrom, self._hiCapTo, self._hiCapC.Value)
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
