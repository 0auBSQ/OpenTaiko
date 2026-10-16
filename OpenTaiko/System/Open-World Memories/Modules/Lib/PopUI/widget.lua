---@diagnostic disable: undefined-global, lowercase-global, need-check-nil
-- PopUI/widget.lua — the base class every widget extends. Owns the shared interaction state machine
-- (idle/hover/press/focus/disabled), the bouncy scale + highlight tweens, accurate rounded hit-testing,
-- and the draw scaffold (baked body + focus ring + content).
-- Mouse-hover and keyboard/gamepad-focus drive one shared highlight, so both look identical.
-- Common options: x, y, w, h, enabled, visible, focusable, mouseOnly, theme/style, sfx,
-- disabledLook = "fade" (a disabled widget fades instead of turning grey).

local U      = require("PopUI.util")
local Shape  = require("PopUI.shape")
local Sfx    = require("PopUI.sfx")
local Theme  = require("PopUI.theme")
local Easing = require("Easing")

local floor, max = math.floor, math.max

local POP_MAX = 16          -- px a highlighted widget grows by at most (big widgets pop less than the ratio)
local PRESS_MAX = 24        -- px a pressed widget shrinks by at most
local SLICE_MIN = 480       -- canvases wider than this are baked narrow and stretched in the middle
local SLICE_MID = 4         -- the plain columns kept in the middle of a narrow bake

local Widget = {}
Widget.__index = Widget

-- ── tweens (advanced by the frame's dt) ──────────────────────────────────────────
local function newTween() return { t = 1, dur = 1, ease = Easing.linear } end
local function startTween(tw, time, ease)
    tw.t = 0
    tw.dur = max(0.0001, time)
    tw.ease = ease and Easing.get(ease[1], ease[2]) or Easing.linear
end
-- the eased progress 0..1 (it can overshoot along a bouncy curve)
local function tickTween(tw, dt)
    if tw.t < 1 then tw.t = math.min(1, tw.t + dt / tw.dur) end
    return tw.ease(tw.t)
end
Widget.newTween, Widget.startTween, Widget.tickTween = newTween, startTween, tickTween

-- the options become the widget's fields; the caller's table is left as it is
function Widget.new(o)
    o = o or {}
    local self = setmetatable({}, Widget)
    for k, v in pairs(o) do self[k] = v end
    self.userTheme = o.theme or o.style or Theme.NONE
    self.userSfx = o.sfx or Theme.NONE
    self.x = self.x or 0; self.y = self.y or 0
    self.w = self.w or 160; self.h = self.h or 64
    if self.enabled == nil then self.enabled = true end
    if self.visible == nil then self.visible = true end
    if self.focusable == nil then self.focusable = true end
    if self.focusable == false and self._focusableWant == nil then self._focusableWant = false end
    self.hovered, self.focused, self.pressed, self.capturing = false, false, false, false
    self._scaleCur, self._scaleFrom, self._scaleTo = 1.0, 1.0, 1.0
    self._hiCur, self._hiFrom, self._hiTo = 0.0, 0.0, 0.0
    self._hiCapCur, self._hiCapFrom, self._hiCapTo = 0.0, 0.0, 0.0
    return self
end

-- called by the manager right after creation
function Widget:init(mgr)
    self.mgr = mgr
    self._scaleT = newTween()
    self._hiT = newTween()
    self._hiCapT = newTween()
    self:restyle()
    if not self.enabled then self:_bakeBodyOff() end
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

-- the ring shows on the hovered widget, and on the focused one unless the mouse leads (then the pointer's
-- widget alone is lit, and the focus waits for the keys)
function Widget:isHighlighted()
    if not self.enabled then return false end
    if self.hovered then return true end
    return self.focused and not (self.mgr and self.mgr:mouseActive())
end
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
-- no live or parked canvas has; an unchanged key keeps the surface the widget already holds (its extra
-- fields are refreshed). The shared canvas is never drawn into again — every draw sets the scale, colour
-- and opacity it needs first.
function Widget:bakeShared(field, site, w, h, bake, extra, ...)
    w, h = floor(w), floor(h)
    local key = site .. "|" .. w .. "x" .. h .. "|" .. self.mgr:themeKey(self.eff) .. "|" .. self.mgr.keyOf(...)
    local old = self[field]
    if old ~= nil and old.key == key then
        if extra then for k, v in pairs(extra) do old[k] = v end end
        return old
    end
    if old ~= nil then
        if old.key then self.mgr.releaseBaked(old.key) else self.mgr.releaseCanvas(old.canvas) end
    end
    local e = { canvas = self.mgr:bakedCanvas(key, w, h, bake), key = key }
    if extra then for k, v in pairs(extra) do e[k] = v end end
    self[field] = e
    return e
end

-- releases the baked surface in self[field]
function Widget:dropPiece(field)
    local e = self[field]
    if e == nil then return end
    if e.key then self.mgr.releaseBaked(e.key) else self.mgr.releaseCanvas(e.canvas) end
    self[field] = nil
end

-- A wide surface is baked narrow: its corners and a few plain columns, the middle being the same in every
-- column (a vertical gradient, the gloss band, the shadow). drawPiece stretches those columns back to the
-- full width. Returns the narrow shape width and the width of each end piece, or nil to bake it whole.
function Widget:_slice(w, r, m)
    if w + 2 * m <= SLICE_MIN then return nil end
    local cap = m + math.ceil(r) + math.abs(self.eff.shadow.dx or 0) + 3
    local narrow = 2 * (cap - m) + SLICE_MID
    if narrow >= w then return nil end
    return narrow, cap
end

local function bakeBodyInto(self, field, site, top, bot)
    local c = self.eff.colors
    local m = self:_bodyMargin()
    local w, h, r = self.w, self.h, self:radius()
    local narrow, cap = self:_slice(w, r, m)
    local bw = narrow or w
    local eff = self.eff
    self:bakeShared(field, site, bw + 2 * m, h + 2 * m, function(cv)
        Shape.dropShadow(cv, m, m, bw, h, r, { col = c.shadow,
            dx = eff.shadow.dx, dy = eff.shadow.dy, layers = eff.shadow.layers, grow = eff.shadow.grow })
        Shape.panel(cv, m, m, bw, h, {
            radius = r,
            outline = { col = c.outline, width = eff.outlineWidth },
            top = top, bottom = bot,
            gloss = eff.gloss and c.gloss or nil,
        })
    end, { m = m, cap = cap or false, fullW = w + 2 * m }, top, bot, r)
end

-- bake the standard body: soft shadow + bordered gradient panel + gloss. faceTop/faceBottom override the
-- theme surface gradient (e.g. an accent button passes primary/primary2).
function Widget:bakeBody(faceTop, faceBottom)
    local c = self.eff.colors
    self._faceTop, self._faceBot = faceTop or c.surface, faceBottom or c.surface2
    bakeBodyInto(self, "_body", "body", self._faceTop, self._faceBot)
    if self._bodyOff then self:_bakeBodyOff() end
end

-- a disabled widget's face: the same body in muted, lighter colours, drawn opaque
local function muted(col)
    local g = col[1] * 0.3 + col[2] * 0.59 + col[3] * 0.11
    local function ch(v) return U.lerp(U.lerp(v, g, 0.75), 255, 0.25) end
    return { ch(col[1]), ch(col[2]), ch(col[3]), col[4] or 255 }
end
function Widget:_bakeBodyOff()
    if self.disabledLook == "fade" or self._body == nil or self._faceTop == nil then return end
    bakeBodyInto(self, "_bodyOff", "body", muted(self._faceTop), muted(self._faceBot))
end

-- bake the focus/hover ring (gold rounded outline) as its own canvas, drawn over the body when highlighted
function Widget:bakeRing()
    local c = self.eff.colors
    local m = self:_bodyMargin()
    local rw = self.eff.outlineWidth + 4
    local w, h, r = self.w, self.h, self:radius()
    local narrow, cap = self:_slice(w, r, m)
    local bw = narrow or w
    self:bakeShared("_ring", "ring", bw + 2 * m, h + 2 * m, function(cv)
        Shape.fillRoundAA(cv, m - 2, m - 2, bw + 4, h + 4, r + 2, c.focusRing)   -- smooth outer
        Shape.fillRound(cv, m - 2 + rw, m - 2 + rw, bw + 4 - 2 * rw, h + 4 - 2 * rw, r + 2 - rw, { 0, 0, 0, 0 })
    end, { m = m, cap = cap or false, fullW = w + 2 * m }, r)
end

-- draws a baked piece centred at (cx, cy) at scale s; a narrow bake is drawn as its two ends with the
-- middle columns stretched between them
function Widget:drawPiece(e, cx, cy, s, opacity)
    local cv = e.canvas
    s = s or 1
    cv:SetColor(1, 1, 1)
    cv:SetOpacity(opacity or 1)
    cx, cy = floor(cx), floor(cy)
    if not e.cap then
        cv:SetScale(s, s)
        cv:DrawAtAnchor(cx, cy, "center")
        cv:SetScale(1, 1)
        return
    end
    local cw, ch = cv.Width, cv.Height
    local cap, mid, fullW = e.cap, cv.Width - 2 * e.cap, e.fullW
    local x0 = cx - floor(fullW / 2) * s
    local y0 = cy - floor(ch / 2) * s
    cv:SetScale(s, s)
    cv:DrawRectAtAnchor(x0, y0, 0, 0, cap, ch, "topleft")
    cv:DrawRectAtAnchor(x0 + (fullW - cap) * s, y0, cw - cap, 0, cap, ch, "topleft")
    cv:SetScale((fullW - 2 * cap) * s / mid, s)
    cv:DrawRectAtAnchor(x0 + cap * s, y0, cap, 0, mid, ch, "topleft")
    cv:SetScale(1, 1)
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
function Widget:_hoverScale()
    local hs = self.eff.anim.hoverScale
    local size = max(self.w, self.h)
    if size > 0 and (hs - 1) * size > POP_MAX then hs = 1 + POP_MAX / size end
    return hs
end

function Widget:_pressScale()
    local ps = self.eff.anim.pressScale
    local size = max(self.w, self.h)
    if size > 0 and (1 - ps) * size > PRESS_MAX then ps = 1 - PRESS_MAX / size end
    return ps
end

function Widget:_tweenScale(target)
    self._scaleFrom, self._scaleTo = self._scaleCur, target
    local a = self.eff.anim
    startTween(self._scaleT, a.hoverTime, a.hoverEase)
end

function Widget:_tweenScaleEase(target, time, ease)
    self._scaleFrom, self._scaleTo = self._scaleCur, target
    startTween(self._scaleT, time, ease)
end

function Widget:_tweenHighlight(target)
    self._hiFrom, self._hiTo = self._hiCur, target
    startTween(self._hiT, self.eff.anim.highlightTime)
end

function Widget:_tweenCapturingHighlight(target)
    self._hiCapFrom, self._hiCapTo = self._hiCapCur, target
    startTween(self._hiCapT, self.eff.anim.highlightTime)
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
    if self._hiT == nil then return end
    local hi = self:isHighlighted()
    if (hi and 1 or 0) ~= self._hiTo then self:_tweenHighlight(hi and 1 or 0) end
    if self.noPop then return end                  -- widgets whose content doesn't scale don't pop
    local target = hi and self:_hoverScale() or 1.0
    if not self.pressed and target ~= self._scaleTo then self:_tweenScale(target) end
end

function Widget:_refreshCapturingHighlight()
    if self._hiCapT == nil then return end
    local hi = self:isCapturingHighlighted()
    self:_tweenCapturingHighlight(hi and 1 or 0)
end

function Widget:press()
    if not self.enabled then return end
    self.pressed = true
    if not self.noPop then self:_tweenScaleEase(self:_pressScale(), self.eff.anim.pressTime, self.eff.anim.pressEase) end
end

-- release; if `inside` and was pressed, fire the activation
function Widget:release(inside)
    if not self.pressed then return end
    self.pressed = false
    if not self.noPop then
        self:_tweenScaleEase(self:isHighlighted() and self:_hoverScale() or 1.0,
            self.eff.anim.releaseTime, { "OUT", "BACK" })
    end
    if inside and self.enabled then self:onActivate() end
end

-- keyboard/gamepad activation: snap to the squish then boing back in one shot (a same-frame press+release
-- would cancel the animation), then fire. Used for Decide so Enter shows the same feedback as a click.
function Widget:keyActivate()
    if not self.enabled then return end
    if not self.noPop then
        local ps = self:_pressScale()
        self._scaleCur, self._scaleFrom = ps, ps
        self:_tweenScaleEase(self:isHighlighted() and self:_hoverScale() or 1.0,
            self.eff.anim.releaseTime, { "OUT", "BACK" })
    end
    self:onActivate()
end

-- default activation: buttons override or rely on onClick
function Widget:onActivate()
    if self.onClick and self.onClick(self) then return end
    self:playSfx("click")
end

-- enabling, disabling, showing or hiding a widget updates where the keys can stop
function Widget:setEnabled(b)
    b = b and true or false
    if self.enabled == b then return end
    self.enabled = b
    self.focusable = b and (self._focusableWant ~= false)
    if self.mgr then
        if b then self:dropPiece("_bodyOff") else self:_bakeBodyOff() end
        self:_refreshHighlight(); self:_refreshCapturingHighlight()
        self.mgr:_rebuildFocus()
    end
end

function Widget:setVisible(b)
    b = b and true or false
    if self.visible == b then return end
    self.visible = b
    if self.mgr then self.mgr:_rebuildFocus() end
end

-- widgets edited with Left/Right (slider, chooser, settings rows): the keyboard's Left/Right always edit;
-- the pad's Left/Right step the focus unless Decide put the widget in its edit mode, which Cancel leaves.
-- step(self, dir) applies one edit (dir = -1 / 1).
function Widget.editable(cls, step)
    function cls:onCapturing(silence) if not silence then self:playSfx("click") end end
    function cls:onEndCapturing(silence) if not silence then self:playSfx("cancel") end end
    function cls:onDecide() self:setCapturing(not self.capturing); return true end
    function cls:onCancel()
        if self.capturing then self:setCapturing(false); return true end
        return false
    end
    function cls:onNavLeft(forPad)
        if not forPad or self.capturing then self:setCapturing(true, true); step(self, -1); return true end
        return false
    end
    function cls:onNavRight(forPad)
        if not forPad or self.capturing then self:setCapturing(true, true); step(self, 1); return true end
        return false
    end
end

-- release the GPU canvases this widget baked (LuaCanvas has no finalizer) into the manager's pool, where the
-- next widget of the same size picks them up. Every baked surface is stored as a `{ canvas = <LuaCanvas>, ... }`
-- field (_body/_ring/_track/_knob/_capL/…). Call before dropping a UI.
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
    local dt = ctx.dt or 0
    self._scaleCur = U.lerp(self._scaleFrom, self._scaleTo, tickTween(self._scaleT, dt))
    self._hiCur    = U.lerp(self._hiFrom, self._hiTo, tickTween(self._hiT, dt))
    self._hiCapCur = U.lerp(self._hiCapFrom, self._hiCapTo, tickTween(self._hiCapT, dt))
end

-- the body to draw and its opacity: the muted face while disabled (or the faded body with disabledLook = "fade")
function Widget:_bodyNow()
    if self.enabled then return self._body, 1 end
    if self._bodyOff then return self._bodyOff, 1 end
    return self._body, 0.5
end

function Widget:draw()
    if not self.visible then return end
    local cx, cy = self:centerX(), self:centerY()
    local s = self._scaleCur
    if self._ring and self._hiCur > 0.01 then self:drawPiece(self._ring, cx, cy, s, self._hiCur) end
    local body, op = self:_bodyNow()
    if body then self:drawPiece(body, cx, cy, s, op) end
    self:drawContent(cx, cy, s)
end

-- subclasses draw their label/icon/value here
function Widget:drawContent(cx, cy, s) end

return Widget
