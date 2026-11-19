---@diagnostic disable: undefined-global, lowercase-global, need-check-nil
-- SimpleUI menu: vertical scrolling list with one selection
--   ui:menu{ x=, y=, w=, h=, items={"One",{text="Two",value=2}}, selected=1, onChange=fn, onSelect=fn }

local Widget   = require("UI.SimpleUI.widget")
local U        = require("UI.SimpleUI.util")
local Shape    = require("UI.SimpleUI.shape")
local ListBase = require("UI.SimpleUI.widgets.listbase")

local Menu = setmetatable({}, { __index = Widget })
Menu.__index = Menu
ListBase.install(Menu)

local HOVER_TICK = 0.06   -- min s between hover move sounds

-- onChange: selection moved; onSelect: chosen (Decide or click)
-- locked rows: onLockedSelect(i, item, menu) runs on choose (error sound when absent or false)
-- drawLocked(item, right, cy, menu) replaces the padlock; lockedW px kept free for it
-- hoverSelects = false: click to pick only; textReserve: px kept free at each row's right end
-- scrollbar = true: bar beside the menu while the rows don't fit
function Menu.new(o)
    o = o or {}
    o.w = o.w or 480; o.h = o.h or 600
    local self = Widget.new(o)
    setmetatable(self, Menu)
    self.rowHeight = o.rowHeight or 72
    self.selected = o.selected or 1
    self.items = {}
    for _, it in ipairs(o.items or {}) do
        -- mark: highlighted row, read live from the item table
        if type(it) == "table" then self.items[#self.items + 1] = { text = it.text or "", value = it.value, mark = it.mark, locked = it.locked }
        else self.items[#self.items + 1] = { text = tostring(it), value = it } end
    end
    self._scrollCur, self._scrollTarget = 0, 0
    return self
end

function Menu:_count() return #self.items end

-- quiet = true: no move sound
function Menu:setSelected(i, fireChange, quiet)
    local n = self:_count(); if n == 0 then return end
    i = U.clamp(i, 1, n)
    if i == self.selected then return end
    self.selected = i
    self:_ensureVisible()
    if fireChange ~= false then
        if self.onChange and self.onChange(self.selected, self.items[self.selected], self) then return end
        if not quiet then self:playSfx("move") end
    end
end

function Menu:_ensureVisible() self:_showRow(self.selected) end

function Menu:onNavDownOrPadRight() if self.selected < self:_count() then self:setSelected(self.selected + 1); return true end return false end
function Menu:onNavUpOrPadLeft() if self.selected > 1 then self:setSelected(self.selected - 1); return true end return false end

function Menu:onActivate()
    local it = self.items[self.selected]
    -- locked rows can't be chosen unless the caller handles it
    if it and it.locked then
        if self.onLockedSelect and self.onLockedSelect(self.selected, it, self) ~= false then return end
        self:playSfx("error"); return
    end
    if it and self.onSelect and self.onSelect(self.selected, it, self) then return end
    self:playSfx("click")
end

function Menu:restyle()
    self:resolveStyle()
    local c = self.eff.colors
    local rw, rh = self.w, self.rowHeight - 10
    local m = 6
    local rs, ow = self.eff.radiusSmall, math.max(2, self.eff.outlineWidth - 2)
    local function row(field, top, bot, outlineCol)
        self:bakeShared(field, "menu.row", rw + 2 * m, rh + 2 * m, function(cv)
            Shape.panel(cv, m, m, rw, rh, { radius = rs, outline = { col = outlineCol, width = ow }, top = top, bottom = bot })
        end, { m = m }, top, bot, outlineCol)
    end
    row("_row", c.surface, c.surface2, c.outline)
    row("_rowSel", c.primary, c.primary2, c.outline)
    self:_clampScroll()
    -- row-sized focus ring, drawn on the selected row
    local rwid = self.eff.outlineWidth + 4
    local ir = self.eff.radiusSmall
    self:bakeShared("_ring", "menu.ring", rw + 2 * m, rh + 2 * m, function(ring)
        Shape.fillRoundAA(ring, m - 2, m - 2, rw + 4, rh + 4, ir + 2, c.focusRing)
        Shape.fillRound(ring, m - 2 + rwid, m - 2 + rwid, rw + 4 - 2 * rwid, rh + 4 - 2 * rwid, math.max(1, ir + 2 - rwid), { 0, 0, 0, 0 })
    end, { m = m })
    self:_bakeBar()
end

function Menu:update(ctx)
    Widget.update(self, ctx)
    -- pointer previews a row (unless hoverSelects = false); a click picks it
    if self.hovered and ctx.inside and (ctx.moved or ctx.mPressed or not self._wasHovered) then
        local rel = ctx.my - self.y + self._scrollCur
        local row = math.floor(rel / self.rowHeight) + 1
        if row >= 1 and row <= self:_count() then
            if ctx.mPressed then self:setSelected(row) end
            if self.hoverSelects ~= false and row ~= self.selected and not ctx.mPressing then
                local quiet = (ctx.ts or 0) - (self._tickTs or -1e9) < HOVER_TICK * 1000
                if not quiet then self._tickTs = ctx.ts end
                self:setSelected(row, true, quiet)
            end
        end
    end
    self._wasHovered = self.hovered
    self:_updateScroll(ctx, 14)
end

-- draws a row piece clipped to the viewport; edge rows use a source-rect draw
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
    local vy0, vy1 = self.y, self.y + self.h              -- viewport for rows and text
    local firstVis = math.max(1, math.floor(self._scrollCur / self.rowHeight))
    local lastVis = math.min(n, math.ceil((self._scrollCur + self.h) / self.rowHeight) + 1)
    for i = firstVis, lastVis do
        local ry = self.y + (i - 1) * self.rowHeight - self._scrollCur
        -- text box offset by textNudge so the glyph line sits on the row centre
        local rcy = math.floor(ry + self.rowHeight * 0.5)
        local tcy = rcy + self.mgr:textNudge(self.eff.font.label)
        local rcx = math.floor(self.x + self.w * 0.5)
        local isSel = (i == self.selected)
        local it = self.items[i]
        local marked = it and it.mark
        local locked = it and it.locked
        local piece = isSel and self._rowSel or self._row
        -- face tint: marked (unselected) or locked rows
        if marked and not isSel then piece.canvas:SetColor(1.0, 0.88, 0.45)
        elseif locked then piece.canvas:SetColor(0.62, 0.62, 0.66)
        else piece.canvas:SetColor(1, 1, 1) end
        piece.canvas:SetOpacity(1)
        piece.canvas:SetScale(1.0, 1.0)
        drawPieceClipped(piece, rcx, rcy, vy0, vy1)
        if isSel and self.focused and self._hiCur > 0.01 then
            self._ring.canvas:SetColor(1, 1, 1)
            self._ring.canvas:SetOpacity(self._hiCur); self._ring.canvas:SetScale(1, 1)
            drawPieceClipped(self._ring, rcx, rcy, vy0, vy1)
        end
        if it then
            local c = self.eff.colors
            local fg = marked and { 255, 208, 74 } or (isSel and c.textOnAccent or c.text)
            local bg = isSel and U.shade(c.primary2, 0.5) or { 255, 255, 255, 150 }
            if marked and not isSel then bg = { 90, 62, 8, 200 } end
            if self.eff.textOutline == false then bg = { 255, 255, 255, 0 } end
            if locked then fg = { 120, 122, 130 } end
            local textW = locked and (self.w - 48 - (self.lockedW or self.rowHeight)) or (self.w - 48 - (self.textReserve or 0))
            self.mgr:drawTextEx(self.eff.font.label, it.text, math.floor(self.x + 28), tcy,
                fg, bg, 1, 1, textW, "left", vy0, vy1)
            if locked and self.drawLocked then
                if ry + self.rowHeight <= vy1 + 1 and ry >= vy0 - 1 then
                    self.drawLocked(it, math.floor(self.x + self.w - 24), rcy, self)
                end
            elseif locked then
                -- padlock at the row's right end
                local isz = math.floor(self.rowHeight * 0.62)
                self:bakeShared("_lock", "menu.lock", isz + 8, isz + 8, function(cv)
                    Shape.icon(cv, "lock", (isz + 8) * 0.5, (isz + 8) * 0.5, isz, { 236, 238, 244 })
                end, nil, isz)
                if self._lock then
                    self._lock.canvas:SetColor(1, 1, 1); self._lock.canvas:SetOpacity(1); self._lock.canvas:SetScale(1, 1)
                    drawPieceClipped(self._lock, math.floor(self.x + self.w - 24 - isz * 0.5), rcy, vy0, vy1)   -- clipped like its row
                end
            end
        end
    end
    self:_drawBar()
end

return Menu
