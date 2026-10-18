---@diagnostic disable: undefined-global, lowercase-global, need-check-nil
-- SimpleUI speech bubble: a rounded bubble with a pointer tail and an optional typewriter reveal.
--   ui:bubble{ x=, y=, w=, h=, text="Hi!", tail="bottom"|"top"|"left"|"right", tailPos=0.5, typewriter=true, onDone=fn }

local Widget = require("UI.SimpleUI.widget")
local U      = require("UI.SimpleUI.util")
local Shape  = require("UI.SimpleUI.shape")

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
    -- while dragged (draft), the bubble bakes into a canvas with room to grow (its size + headroom), so a
    -- tail drag or a resize keeps reusing one GL texture; at rest the canvas fits the bubble
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

-- resize the bubble (drag handle), clamped to [min, reserve]
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
    local maxW = self.w - 2 * self.pad - 50   -- the glyph box pads 25px per side; keep both margins equal
    self._size = size
    self._fg = c.text
    self._lines = self.mgr:wrapLines(size, self.text, maxW)   -- glyph-measured wrap (no texture allocation)
    if self.typewriter then
        -- per-line UTF-8 char lists so the reveal never splits a multi-byte character
        self._lineChars = {}
        self._fullLen = 0
        for i, l in ipairs(self._lines) do
            self._lineChars[i] = self.mgr:utf8chars(l)
            self._fullLen = self._fullLen + #self._lineChars[i]
        end
    end
end

function Bubble:_popIn()
    self._scaleFrom, self._scaleTo = 0.6, 1.0
    local a = self.eff.anim
    Widget.startTween(self._scaleT, a.popInEase and 0.5 or 0.3, a.popInEase)
end

function Bubble:restyle()
    self:resolveStyle()
    local c = self.eff.colors
    local sh = self.eff.shadow
    -- the margin covers the longest tail and its shadow (tail + grow + offset can stack on one side), so a
    -- tail-size drag never resizes the canvas
    local sExt = (sh.layers or 4) * (sh.grow or 3) + math.max(math.abs(sh.dx or 0), math.abs(sh.dy or 6))
    local m = math.ceil(100 + sExt + self.eff.outlineWidth + 6)
    self._m = m
    local bw = self._draft and self._reserveW or self.w
    local bh = self._draft and self._reserveH or self.h
    local cw = math.floor(bw + 2 * m)
    local ch = math.floor(bh + 2 * m)
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
    Shape.upload(cv, false)
    self._body = { canvas = cv, m = m, cap = false }
    self:_rewrap()
    -- keep typewriter progress across re-bakes (tail/resize drag) — only setText restarts it
    if self.typewriter and self._done then self._shown = self._fullLen or 0 end
end

function Bubble:update(ctx)
    self._scaleCur = U.lerp(self._scaleFrom or 1, self._scaleTo or 1, Widget.tickTween(self._scaleT, ctx.dt or 0))
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
    self:drawPiece(self._body, self:centerX(), self:centerY(), self._scaleCur or 1, 1)
    local x = math.floor(self.x + self.pad)
    local y = math.floor(self.y + self.pad)
    local fg = self._fg or self.eff.colors.text
    local lineH = math.floor(self._size * 1.32)
    if self.typewriter then
        -- glyph draw: the revealed substring changes every frame; the glyph cache is bounded per character,
        -- so no per-frame texture allocation. Reveal counts UTF-8 characters, never splitting a sequence.
        local rem = math.floor(self._shown)
        for i, chars in ipairs(self._lineChars or {}) do
            if rem <= 0 then break end
            local sub = (rem >= #chars) and self._lines[i] or table.concat(chars, "", 1, rem)
            self.mgr:drawText(self._size, sub, x, y + (i - 1) * lineH, fg)
            rem = rem - #chars
        end
    else
        for i, line in ipairs(self._lines or {}) do
            self.mgr:drawText(self._size, line, x, y + (i - 1) * lineH, fg)
        end
    end
end

return Bubble
