---@diagnostic disable: undefined-global, lowercase-global, need-check-nil
-- PopUI vertical menu / list: keyboard (Up/Down) + mouse hover + wheel scroll, one shared selection
-- highlight. onChange = selection moved; onSelect = chosen (Decide / click). Off-viewport rows are culled.
--   ui:menu{ x=, y=, w=, h=, items={"One",{text="Two",value=2}}, selected=1, onChange=fn, onSelect=fn }

local Widget = require("PopUI.widget")
local U      = require("PopUI.util")
local Shape  = require("PopUI.shape")

local Menu = setmetatable({}, { __index = Widget })
Menu.__index = Menu

function Menu.new(o)
    o = o or {}
    o.w = o.w or 480; o.h = o.h or 600
    local srcItems = o.items or {}   -- capture before Widget.new (self === o, so self.items= would clobber o.items)
    local self = Widget.new(o)
    setmetatable(self, Menu)
    self.rowHeight = o.rowHeight or 72
    self.selected = o.selected or 1
    self.items = {}
    for _, it in ipairs(srcItems) do
        -- mark = highlighted row (e.g. a music player's now-playing entry): gold face + gold text.
        -- It lives on the item table so callers can flip it live without rebuilding the menu.
        if type(it) == "table" then self.items[#self.items + 1] = { text = it.text or "", value = it.value, mark = it.mark }
        else self.items[#self.items + 1] = { text = tostring(it), value = it } end
    end
    self._scrollCur, self._scrollTarget = 0, 0
    return self
end

function Menu:_count() return #self.items end

function Menu:setSelected(i, fireChange)
    local n = self:_count(); if n == 0 then return end
    i = U.clamp(i, 1, n)
    if i == self.selected then return end
    self.selected = i
    self:_ensureVisible()
    if fireChange ~= false then
        if self.onChange then self.onChange(self.selected, self.items[self.selected], self) end
        self.mgr:playSfx("move")
    end
end

function Menu:_ensureVisible()
    local top = (self.selected - 1) * self.rowHeight
    local bot = top + self.rowHeight
    if top < self._scrollTarget then self._scrollTarget = top end
    if bot > self._scrollTarget + self.h then self._scrollTarget = bot - self.h end
    self:_clampScroll()
end

function Menu:_clampScroll()
    local maxScroll = math.max(0, self:_count() * self.rowHeight - self.h)
    self._scrollTarget = U.clamp(self._scrollTarget, 0, maxScroll)
end

function Menu:onNavDown() if self.selected < self:_count() then self:setSelected(self.selected + 1); return true end return false end
function Menu:onNavUp() if self.selected > 1 then self:setSelected(self.selected - 1); return true end return false end

function Menu:onActivate()
    local it = self.items[self.selected]
    if it and self.onSelect then self.onSelect(self.selected, it, self) end
    self.mgr:playSfx("click")
end

function Menu:restyle()
    self.eff = self.mgr:resolveTheme(self.style)
    local c = self.eff.colors
    local rw, rh = self.w, self.rowHeight - 10
    local m = 6
    local function row(old, top, bot, outlineCol)
        local cv = self.mgr:reuseCanvas(old, rw + 2 * m, rh + 2 * m)
        Shape.panel(cv, m, m, rw, rh, { radius = self.eff.radiusSmall, outline = { col = outlineCol, width = math.max(2, self.eff.outlineWidth - 2) }, top = top, bottom = bot })
        cv:Upload(); return { canvas = cv, m = m }
    end
    self._row = row(self._row and self._row.canvas, c.surface, c.surface2, c.outline)
    self._rowSel = row(self._rowSel and self._rowSel.canvas, c.primary, c.primary2, c.outline)
    self:_clampScroll()
    -- ROW-sized focus ring (drawn on the selected row) — not a full-menu ring
    local rwid = self.eff.outlineWidth + 4
    local ir = self.eff.radiusSmall
    local ring = self.mgr:reuseCanvas(self._ring and self._ring.canvas, rw + 2 * m, rh + 2 * m)
    Shape.fillRoundAA(ring, m - 2, m - 2, rw + 4, rh + 4, ir + 2, c.focusRing)
    Shape.fillRound(ring, m - 2 + rwid, m - 2 + rwid, rw + 4 - 2 * rwid, rh + 4 - 2 * rwid, math.max(1, ir + 2 - rwid), { 0, 0, 0, 0 })
    ring:Upload()
    self._ring = { canvas = ring, m = m }
end

function Menu:update(ctx)
    self._scaleC:Tick(); self._hiC:Tick()
    self._hiCur = U.lerp(self._hiFrom, self._hiTo, self._hiC.Value)
    -- wheel scroll while hovered
    if self.hovered and ctx.scrollDy and ctx.scrollDy ~= 0 then
        self._scrollTarget = self._scrollTarget - ctx.scrollDy * self.rowHeight
        self:_clampScroll()
    end
    -- mouse hover row → preview-select; click sets selection
    if self.hovered and ctx.inside then
        local rel = ctx.my - self.y + self._scrollCur
        local row = math.floor(rel / self.rowHeight) + 1
        if row >= 1 and row <= self:_count() then
            if ctx.mPressed then self:setSelected(row) end
            if row ~= self.selected and not ctx.mPressing then self:setSelected(row) end
        end
    end
    -- smooth scroll
    self._scrollCur = self._scrollCur + (self._scrollTarget - self._scrollCur) * math.min(1, ctx.dt * 14)
end

-- draw one baked row piece clipped to the menu viewport: fully-visible rows blit whole, edge rows
-- are SLICED via LuaCanvas:DrawRect (source-rect draw) so nothing bleeds past the list box
local function drawPieceClipped(piece, rcx, rcy, vy0, vy1)
    local cv = piece.canvas
    local cw, ch = cv.Width, cv.Height
    local dx, dy = math.floor(rcx - cw * 0.5), math.floor(rcy - ch * 0.5)
    local sy0 = math.max(0, math.floor(vy0 - dy))
    local sy1 = math.min(ch, math.ceil(vy1 - dy))
    if sy1 <= sy0 then return end
    if sy0 == 0 and sy1 == ch then cv:DrawAtAnchor(rcx, rcy, "center")
    else cv:DrawRect(dx, dy + sy0, 0, sy0, cw, sy1 - sy0) end
end

function Menu:draw()
    if not self.visible then return end
    local n = self:_count()
    local vy0, vy1 = self.y, self.y + self.h              -- the viewport every row + its text clips to
    local firstVis = math.max(1, math.floor(self._scrollCur / self.rowHeight))
    local lastVis = math.min(n, math.ceil((self._scrollCur + self.h) / self.rowHeight) + 1)
    for i = firstVis, lastVis do
        local ry = self.y + (i - 1) * self.rowHeight - self._scrollCur
        local rcy = math.floor(ry + (self.rowHeight - 10) * 0.5 + self.mgr:textNudge(self.eff.font.label))
        local rcx = math.floor(self.x + self.w * 0.5)
        local isSel = (i == self.selected)
        local it = self.items[i]
        local marked = it and it.mark
        local piece = isSel and self._rowSel or self._row
        -- marked (now-playing) rows: warm gold face tint on the plain row; the selected face keeps
        -- its accent colour and signals through the gold text instead
        if marked and not isSel then piece.canvas:SetColor(1.0, 0.88, 0.45)
        else piece.canvas:SetColor(1, 1, 1) end
        piece.canvas:SetOpacity(1)
        piece.canvas:SetScale(1.0, 1.0)
        drawPieceClipped(piece, rcx, rcy, vy0, vy1)
        if isSel and self.focused and self._hiCur > 0.01 then
            self._ring.canvas:SetOpacity(self._hiCur)
            drawPieceClipped(self._ring, rcx, rcy, vy0, vy1)
        end
        if it then
            local c = self.eff.colors
            local fg = marked and { 255, 208, 74 } or (isSel and c.textOnAccent or c.text)
            local bg = isSel and U.shade(c.primary2, 0.5) or { 255, 255, 255, 150 }
            if marked and not isSel then bg = { 90, 62, 8, 200 } end
            self.mgr:drawTextEx(self.eff.font.label, it.text, math.floor(self.x + 28), rcy,
                fg, bg, 1, 1, self.w - 48, "left", vy0, vy1)
        end
    end
end

return Menu
