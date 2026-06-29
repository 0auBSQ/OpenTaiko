---@diagnostic disable: undefined-global, lowercase-global, need-check-nil
-- PopUI speech bubble: a rounded bubble with a pointer tail and an optional typewriter reveal.
--   ui:bubble{ x=, y=, w=, h=, text="Hi!", tail="bottom"|"top"|"left"|"right", tailPos=0.5, typewriter=true, onDone=fn }

local Widget = require("PopUI.widget")
local U      = require("PopUI.util")
local Shape  = require("PopUI.shape")

local Bubble = setmetatable({}, { __index = Widget })
Bubble.__index = Bubble

function Bubble.new(o)
    o = o or {}
    o.focusable = false; o._focusableWant = false
    o.w = o.w or 440; o.h = o.h or 170
    local self = Widget.new(o)
    setmetatable(self, Bubble)
    self.text = o.text or ""
    self.shape = o.shape or "round"     -- "round" | "oval" | "cloud"
    self.tail = o.tail or "bottom"
    self.tailPos = o.tailPos or 0.5
    self.tailSize = math.min(o.tailSize or 28, 100)
    self.typewriter = o.typewriter or false
    self.cps = o.cps or nil
    self.pad = o.pad or 28
    self._shown = 0
    self._done = false
    self._draft = false
    -- fixed canvas reserve (current size + headroom). The body canvas is sized for this once, so tail-drag AND
    -- resize stay within it → reuseCanvas always reuses the same GL texture (no per-frame dispose/create churn).
    self.minW, self.minH = o.minW or 160, o.minH or 90
    self._reserveW = math.max(self.w + 360, 900)
    self._reserveH = math.max(self.h + 260, 520)
    return self
end

function Bubble:setShape(s) self.shape = s; self:restyle(); self:_popIn() end

-- draft = true → low-poly, 1-shadow-layer bake (fast, for live dragging); false → full quality.
function Bubble:setDraft(b) self._draft = b and true or false; self:restyle() end

-- retarget the tail. Pass draft=true while dragging for a responsive bake; release with draft=false.
function Bubble:setTail(side, pos, size, draft)
    self.tail = side or self.tail
    self.tailPos = pos or self.tailPos
    if size then self.tailSize = math.min(size, 100) end
    self._draft = draft and true or false
    self:restyle()
end

-- resize the bubble (drag handle). Clamped to [min, reserve] so the fixed canvas always fits → no churn.
function Bubble:setSize(w, h, draft)
    self.w = U.clamp(math.floor(w or self.w), self.minW, self._reserveW)
    self.h = U.clamp(math.floor(h or self.h), self.minH, self._reserveH)
    self._draft = draft and true or false
    self:_rewrap()
    self:restyle()
end

function Bubble:setText(t, restartTypewriter)
    self.text = tostring(t)
    self._shown = restartTypewriter and 0 or 1e9
    self._done = not restartTypewriter
    self:_rewrap()
    if restartTypewriter then self:_popIn() end
end

function Bubble:_rewrap()
    local c = self.eff.colors
    local size = self.eff.font.label
    local maxW = self.w - 2 * self.pad
    self._size = size
    self._fg = c.text
    if self.typewriter then
        self._lines = self.mgr:wrapLines(size, self.text, maxW)   -- measureText-based wrap (no GetText churn)
        self._fullLen = 0
        for _, l in ipairs(self._lines) do self._fullLen = self._fullLen + #l end
    else
        self._textTex = self.mgr:renderText(size, self.text, c.text, U.withAlpha(c.surface, 0), false, maxW)
    end
end

function Bubble:_popIn()
    self._scaleFrom, self._scaleTo = 0.6, 1.0
    local cc = self._scaleC
    cc.Begin, cc.End, cc.Interval = 0, 1, self.eff.anim.popInEase and 0.5 or 0.3
    cc:SetEasing(self.eff.anim.popInEase[1], self.eff.anim.popInEase[2]); cc:Start()
end

function Bubble:restyle()
    self.eff = self.mgr:resolveTheme(self.style)
    self._scaleC = self._scaleC or COUNTER:EmptyCounter()
    local c = self.eff.colors
    local sh = self.eff.shadow
    -- margin must cover the tail AND its shadow (tail + grow + offset can stack on the same side). Reserve a
    -- fixed max-tail margin so dragging the tail size never resizes the canvas → reuseCanvas always reuses it
    -- (same GL texture, just cleared+redrawn) instead of dispose+recreate every frame, which churned memory.
    local sExt = (sh.layers or 4) * (sh.grow or 3) + math.max(math.abs(sh.dx or 0), math.abs(sh.dy or 6))
    local m = math.ceil(100 + sExt + self.eff.outlineWidth + 6)   -- fixed margin (max tail 100 + shadow)
    self._m = m
    -- fixed canvas sized for reserve+margin → reuseCanvas always reuses it (tail-drag + resize cause no
    -- per-frame dispose/create churn). The body is baked centered in the canvas so the centred draw lands right.
    local cw = math.floor(self._reserveW + 2 * m)
    local ch = math.floor(self._reserveH + 2 * m)
    local cv = self.mgr:reuseCanvas(self._body and self._body.canvas, cw, ch)
    local ox = math.floor((cw - self.w) * 0.5)
    local oy = math.floor((ch - self.h) * 0.5)
    -- draft = fast, low-poly, 1 shadow layer (live dragging); full = crisp (on release / normal)
    Shape.bubble(cv, ox, oy, self.w, self.h, {
        shape = self.shape, radius = self:radius(),
        outline = { col = c.outline, width = self.eff.outlineWidth },
        top = c.surface, bottom = c.surface2, gloss = self.eff.gloss and c.gloss or nil,
        tail = { side = self.tail, pos = self.tailPos, size = self.tailSize },
        shadow = { col = c.shadow, dx = sh.dx, dy = sh.dy, layers = sh.layers, grow = sh.grow },
        maxPoints = self._draft and 48 or 200,
        shadowLayers = self._draft and 1 or nil,
    })
    cv:Upload()
    self._body = { canvas = cv, m = m }
    self:_rewrap()
    -- keep typewriter progress across re-bakes (tail/resize drag) — only setText restarts it
    if self.typewriter and self._done then self._shown = self._fullLen or 0 end
end

function Bubble:update(ctx)
    self._scaleC:Tick()
    self._scaleCur = U.lerp(self._scaleFrom or 1, self._scaleTo or 1, self._scaleC.Value)
    if self.typewriter and not self._done then
        self._shown = self._shown + (self.cps or self.eff.anim.typewriterCps) * ctx.dt
        if self._shown >= (self._fullLen or 0) then
            self._shown = self._fullLen or 0; self._done = true
            if self.onDone then self.onDone(self) end
        end
    end
end

function Bubble:draw()
    if not self.visible then return end
    local cx, cy = self:centerX(), self:centerY()
    local s = self._scaleCur or 1
    self._body.canvas:SetColor(1, 1, 1); self._body.canvas:SetOpacity(1); self._body.canvas:SetScale(s, s)
    self._body.canvas:DrawAtAnchor(math.floor(cx), math.floor(cy), "center")
    local x = math.floor(self.x + self.pad)
    local y = math.floor(self.y + self.pad)
    local fg = self._fg or self.eff.colors.text
    if self.typewriter then
        -- glyph-atlas draw: the revealed substring changes every frame; the atlas is a bounded per-char
        -- cache (correct advances), so no per-frame texture allocation and correct letter spacing.
        local lineH = math.floor(self._size * 1.32)
        local rem = math.floor(self._shown)
        for i, line in ipairs(self._lines or {}) do
            if rem <= 0 then break end
            local sub = (rem >= #line) and line or line:sub(1, rem)
            self.mgr:drawText(self._size, sub, x, y + (i - 1) * lineH, fg)
            rem = rem - #line
        end
    elseif self._textTex then
        self._textTex:Draw(x, y)
    end
end

return Bubble
