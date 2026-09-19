---@diagnostic disable: undefined-global, lowercase-global, need-check-nil, undefined-field
-- PopUI settingsList: a scrollable list of settings rows with inline editing. Each row is either a
-- non-focusable section header, or an option row showing "Name .... value". Up/Down moves the selection
-- (scroll + cull like Menu); Left/Right edits the selected value; Decide toggles / activates.
--
-- rows = { {header="Audio"}, {opt=<option>}, ... } where each `opt` exposes (C# CLuaConfigOption OR a mock):
--   .Kind ("Toggle"/"Int"/"Choice"/"Action"/"KeyConfig"), .Name, .Desc, .Index, .ChoiceCount
--   :Display() -> string, :Toggle(), :Add(d), :SetIndex(i), :Activate()
-- Callbacks: onFocusRow(opt) when the selection changes; onActivateRow(opt) for Action/KeyConfig rows.

local Widget = require("PopUI.widget")
local U      = require("PopUI.util")
local Shape  = require("PopUI.shape")

local List = setmetatable({}, { __index = Widget })
List.__index = List

function List.new(o)
    o = o or {}
    o.w = o.w or 900; o.h = o.h or 760
    local self = Widget.new(o)
    setmetatable(self, List)
    self.rowHeight = o.rowHeight or 70
    self.rows = {}
    self._scrollCur, self._scrollTarget = 0, 0
    self.selected = 1
    self:setRows(o.rows or {})
    return self
end

function List:setRows(rows)
    self.rows = rows or {}
    self._focusable = {}            -- indices of selectable (non-header) rows
    for i, r in ipairs(self.rows) do if not r.header then self._focusable[#self._focusable + 1] = i end end
    self.selected = math.min(self.selected, math.max(1, #self._focusable))
    if self.mgr then self:restyle() end
    self:_notifyFocus()
end

function List:_selRowIndex() return self._focusable[self.selected] end
function List:_selOpt() local i = self:_selRowIndex(); return i and self.rows[i] and self.rows[i].opt or nil end
function List:_notifyFocus() if self.onFocusRow then self.onFocusRow(self:_selOpt()) end end

function List:restyle()
    self:resolveStyle()
    local c = self.eff.colors
    local rw, rh = self.w, self.rowHeight - 10
    local m = 6
    self._m = m
    local rs, ow = self.eff.radiusSmall, math.max(2, self.eff.outlineWidth - 2)
    local function row(field, top, bot)
        self:bakeShared(field, "list.row", rw + 2 * m, rh + 2 * m, function(cv)
            Shape.panel(cv, m, m, rw, rh, { radius = rs, outline = { col = c.outline, width = ow }, top = top, bottom = bot })
        end, { m = m }, top, bot)
    end
    row("_row", c.surface, c.surface2)
    row("_rowSel", c.primary, c.primary2)
    -- selected-row focus ring
    local rwid = self.eff.outlineWidth + 4
    local ir = self.eff.radiusSmall
    self:bakeShared("_ring", "list.ring", rw + 2 * m, rh + 2 * m, function(ring)
        Shape.fillRoundAA(ring, m - 2, m - 2, rw + 4, rh + 4, ir + 2, c.focusRing)
        Shape.fillRound(ring, m - 2 + rwid, m - 2 + rwid, rw + 4 - 2 * rwid, rh + 4 - 2 * rwid, math.max(1, ir + 2 - rwid), { 0, 0, 0, 0 })
    end, { m = m })
end

function List:_count() return #self.rows end
function List:_setSelected(i)
    local n = #self._focusable; if n == 0 then return end
    i = U.clamp(i, 1, n)
    if i == self.selected then return end
    self.selected = i
    self:_ensureVisible(); self:_notifyFocus(); self:playSfx("move")
end
function List:onNavDownOrPadRight() self:setCapturing(false, true); if self.selected < #self._focusable then self:_setSelected(self.selected + 1); return true end return false end
function List:onNavUpOrPadLeft() self:setCapturing(false, true); if self.selected > 1 then self:_setSelected(self.selected - 1); return true end return false end

function List:_ensureVisible()
    local rowIdx = self:_selRowIndex() or 1
    local top = (rowIdx - 1) * self.rowHeight
    local bot = top + self.rowHeight
    if top < self._scrollTarget then self._scrollTarget = top end
    if bot > self._scrollTarget + self.h then self._scrollTarget = bot - self.h end
    local maxS = math.max(0, #self.rows * self.rowHeight - self.h)
    self._scrollTarget = U.clamp(self._scrollTarget, 0, maxS)
end

-- editing the selected option
local function editOpt(opt, dir)
    if not opt then return end
    local k = opt.Kind
    if k == "Toggle" then opt:Toggle()
    elseif k == "Int" then opt:Add(dir)
    elseif k == "Choice" then
        local n = opt.ChoiceCount or 0
        if n > 0 then opt:SetIndex(((opt.Index - 1 + dir) % n + n) % n + 0) end
    end
end

function List:onActivate()
    local opt = self:_selOpt(); if not opt then return end
    local k = opt.Kind
    if k == "Action" or k == "KeyConfig" then
        if self.onActivateRow then self.onActivateRow(opt) end
        opt:Activate(); self:playSfx("click")
    elseif k == "Toggle" then opt:Toggle(); self:playSfx("click")
    elseif k == "Choice" then editOpt(opt, 1); self:playSfx("click")
    end
end

function List:onCapturing(silence)
    if not silence then self:playSfx("click") end
end
function List:onEndCapturing(silence)
    if not silence then self:playSfx("cancel") end
end

-- consume Left/Right (also for pad Left/Right in capturing mode), so focus stays on the chooser
-- consume Cancel to quit capturing mode
function List:onDecide() self:setCapturing(not self.capturing); return true end
function List:onCancel()
    if self.capturing then self:setCapturing(false); return true end
    return false
end
function List:onNavLeft(forPad)
    if not forPad or self.capturing then self:setCapturing(true, true); editOpt(self:_selOpt(), -1); self:playSfx("move"); return true end
    return false
end
function List:onNavRight(forPad)
    if not forPad or self.capturing then self:setCapturing(true, true); editOpt(self:_selOpt(), 1); self:playSfx("move"); return true end
    return false
end

function List:update(ctx)
    Widget.update(self, ctx)
    if self.hovered and ctx.scrollDy and ctx.scrollDy ~= 0 then
        self._scrollTarget = self._scrollTarget - ctx.scrollDy * self.rowHeight
        local maxS = math.max(0, #self.rows * self.rowHeight - self.h)
        self._scrollTarget = U.clamp(self._scrollTarget, 0, maxS)
    end
    local capturing, silence = self.capturing, false
    -- mouse hover row -> preview-select; click activates
    if not self.focused or ctx.mPressed then capturing = false end  -- reset first
    if self.hovered and ctx.inside and (ctx.moved or not self._wasHovered or self.pressed) then
        local rel = ctx.my - self.y + self._scrollCur
        local rowIdx = math.floor(rel / self.rowHeight) + 1
        if rowIdx >= 1 and rowIdx <= #self.rows and not self.rows[rowIdx].header then
            -- map rowIdx -> focusable index
            for fi, ri in ipairs(self._focusable) do
                if ri == rowIdx then
                    -- preview-select on hover / press-select on click; the manager's release fires onActivate
                    if fi ~= self.selected then
                        self:setCapturing(false, true)
                        if (ctx.mPressed or ctx.mReleased) then self:_setSelected(fi); silence = true end
                    end
                    if ctx.mPressed then capturing = self.pressed end
                    break
                end
            end
        end
    end
    self:setCapturing(capturing, silence)
    self._wasHovered = self.hovered
    self._scrollCur = self._scrollCur + (self._scrollTarget - self._scrollCur) * math.min(1, ctx.dt * 16)
end

function List:draw()
    if not self.visible then return end
    local n = #self.rows
    local firstVis = math.max(1, math.floor(self._scrollCur / self.rowHeight))
    local lastVis = math.min(n, math.ceil((self._scrollCur + self.h) / self.rowHeight) + 1)
    local sz = self.eff.font.label
    local hsz = self.eff.font.small
    local th = self.mgr:textHeight(sz)
    local selRowIdx = self:_selRowIndex()
    for i = firstVis, lastVis do
        local r = self.rows[i]
        local ry = self.y + (i - 1) * self.rowHeight - self._scrollCur
        local rcy = math.floor(ry + (self.rowHeight - 10) * 0.5 + 5)
        local rcx = math.floor(self.x + self.w * 0.5)
        -- top-anchored text: the box top sits half a box above the row centre, plus the nudge so the
        -- glyph line (not the padded box) is what gets centred
        local ty = math.floor(rcy - th * 0.5 + self.mgr:textNudge(sz))
        if r.header then
            -- section header: just the label, no row body
            self.mgr:drawText(hsz, tostring(r.header), math.floor(self.x + 18), math.floor(rcy - self.mgr:textHeight(hsz) * 0.5 + self.mgr:textNudge(hsz)), self.eff.colors.accent2 or self.eff.colors.primary2)
        else
            local isSel = (i == selRowIdx)
            local piece = isSel and self._rowSel or self._row
            piece.canvas:SetColor(1, 1, 1); piece.canvas:SetOpacity(1); piece.canvas:SetScale(1, 1)
            piece.canvas:DrawAtAnchor(rcx, rcy, "center")
            if isSel and self.focused and self._hiCur > 0.01 then
                self._ring.canvas:SetOpacity(self._hiCur); self._ring.canvas:SetScale(1, 1); self._ring.canvas:DrawAtAnchor(rcx, rcy, "center")
            end
            local opt = r.opt
            local nameCol = isSel and self._hiCapCur > 0.01
                and U.lerpColor(self.eff.colors.text, self.eff.colors.primary2, self._hiCapCur)
                or self.eff.colors.text
            self.mgr:drawText(sz, tostring(opt.Name or ""), math.floor(self.x + 28), ty, nameCol)
            local val = opt:Display() or ""
            if val ~= "" then
                local vw = self.mgr:measureText(sz, val)
                local vx = self.x + self.w - 36 - vw
                local prefix = ""
                if isSel and self.focused and (opt.Kind == "Int" or opt.Kind == "Choice") then prefix = "‹ " end
                local suffix = ""
                if isSel and self.focused and (opt.Kind == "Int" or opt.Kind == "Choice") then suffix = " ›" end
                if prefix ~= "" then
                    self.mgr:drawText(sz, prefix, math.floor(vx - self.mgr:measureText(sz, prefix)), ty, nameCol)
                    self.mgr:drawText(sz, val .. suffix, math.floor(vx), ty, nameCol)
                else
                    self.mgr:drawText(sz, val, math.floor(vx), ty, nameCol)
                end
            end
        end
    end
end

return List
