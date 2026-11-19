---@diagnostic disable: undefined-global, lowercase-global, need-check-nil, undefined-field
-- SimpleUI list scrolling, shared by menu and settingsList

local U     = require("UI.SimpleUI.util")
local Shape = require("UI.SimpleUI.shape")

local floor, max = math.floor, math.max

local BAR_W = 8          -- scrollbar width
local BAR_GAP = 12       -- list to scrollbar gap

local ListBase = {}

-- cls keeps _scrollCur/_scrollTarget (px from the first row) and _count()
-- scrollbar = true: bar beside the list while the rows don't fit
function ListBase.install(cls)
    function cls:_contentH() return self:_count() * self.rowHeight end

    function cls:_clampScroll()
        local maxScroll = max(0, self:_contentH() - self.h)
        self._scrollTarget = U.clamp(self._scrollTarget, 0, maxScroll)
    end

    -- least scroll that shows row i
    function cls:_showRow(i)
        local top = (i - 1) * self.rowHeight
        local bot = top + self.rowHeight
        if top < self._scrollTarget then self._scrollTarget = top end
        if bot > self._scrollTarget + self.h then self._scrollTarget = bot - self.h end
        self:_clampScroll()
    end

    --- scrolls row i into view (instant = true: no easing)
    function cls:scrollTo(i, instant)
        self:_showRow(i)
        if instant then self._scrollCur = self._scrollTarget end
    end

    --- on-screen box of row i: x, y, w, h (face only, no gap)
    function cls:rowBounds(i)
        return self.x, self.y + (i - 1) * self.rowHeight - (self._scrollCur or 0), self.w, self.rowHeight - 10
    end

    -- wheel: one row per notch while hovered; shown position eases to the target
    function cls:_updateScroll(ctx, speed)
        if self.hovered and ctx.scrollDy and ctx.scrollDy ~= 0 then
            self._scrollTarget = self._scrollTarget - ctx.scrollDy * self.rowHeight
            self:_clampScroll()
        end
        self._scrollCur = self._scrollCur + (self._scrollTarget - self._scrollCur) * math.min(1, (ctx.dt or 0) * speed)
    end

    -- dot baked once; drawn as two halves with the middle row stretched
    function cls:_bakeBar()
        if not self.scrollbar then return end
        local c = self.eff.colors
        local d = BAR_W
        self:bakeShared("_barDot", "list.bar", d, d, function(cv)
            Shape.fillEllipse(cv, 0, 0, d, d, { 255, 255, 255, 255 })
        end)
        self._barTrackCol = c.track
        self._barThumbCol = c.primary2
    end

    local function pill(dot, x, y, h, col, alpha)
        local cv = dot.canvas
        local d = cv.Width
        local half = floor(d / 2)
        cv:SetColor(col[1] / 255, col[2] / 255, col[3] / 255)
        cv:SetOpacity(alpha)
        cv:SetScale(1, 1)
        cv:DrawRectAtAnchor(x, y, 0, 0, d, half, "topleft")
        cv:DrawRectAtAnchor(x, y + h - half, 0, d - half, d, half, "topleft")
        if h > 2 * half then
            cv:SetScale(1, h - 2 * half)
            cv:DrawRectAtAnchor(x, y + half, 0, half, d, 1, "topleft")
        end
        cv:SetScale(1, 1); cv:SetColor(1, 1, 1); cv:SetOpacity(1)
    end

    function cls:_drawBar()
        if not self.scrollbar or not self._barDot then return end
        local contentH = self:_contentH()
        if contentH <= self.h then return end
        local x = floor(self.x + self.w + BAR_GAP)
        local y = floor(self.y)
        pill(self._barDot, x, y, self.h, self._barTrackCol, 0.7)
        local thumbH = max(BAR_W * 3, floor(self.h * self.h / contentH))
        local range = contentH - self.h
        local k = range > 0 and U.clamp((self._scrollCur or 0) / range, 0, 1) or 0
        pill(self._barDot, x, floor(y + (self.h - thumbH) * k), thumbH, self._barThumbCol, 1)
    end
end

return ListBase
