---@diagnostic disable: undefined-global, undefined-field, need-check-nil
-- itemgrid.lua — My Room's stock grid (the recycle bin and the workbench): one widget of the UI library with
-- a tile per stack, showing its picture, its count and its name in full.
--
-- The arrow keys move the picked tile across the grid (Right past the last column and Left past the first
-- let the focus go to the widgets beside it), the drum's Ka steps through the tiles one by one, a click picks
-- a tile and the wheel or the scrollbar scrolls. The rows glide and are cut at the grid's edges. A name is
-- never cut short: it wraps on two lines, then gets smaller. An item kept in a colour shows that colour as a
-- small box in brackets after its name. Slots left free fill the rows in view, so a short stock still reads
-- as a grid.
--   local Grid = require("itemgrid")
--   local g = ui:add(Grid.new{ x =, y =, w =, h =, items = { { key =, n = }, ... }, selected = 1, scroll = 0,
--                              onSelect = function(i, item) end, onChoose = function(i, item) end })
--   Grid.width(cols)   -- the width a grid of that many columns takes
--   onSelect: a tile was picked (click, arrows); onChoose: Decide on the grid (the keys, not the mouse)
-- The helpers the other stock views share: Grid.nameOf(key), Grid.drawName(mgr, key, x, y, w, h, fg, bg, opts),
-- Grid.drawPicture(mgr, key, x, y, size); g:drawDetail(key, n, x, y, w, stockText) draws a stack's picture,
-- name and count large (the window's side column).

local UI = require("uilib")
local Room = require("room")
local A = require("assets")
local I18N = require("Locale.i18n")
local ITEMS = I18N.texts("items")       -- lang/<code>/items.json

local floor, ceil, max, min = math.floor, math.ceil, math.max, math.min

-- the library is picked on first use (uilib), so its Widget base is looked up at run time
local Grid = setmetatable({}, { __index = function(_, k) return UI.Widget[k] end })
Grid.__index = Grid

local TILE_W, TILE_H, GAP = 168, 236, 16
local INSET = 10                        -- the room kept round the tiles inside the grid, for the ring
local WELL = 10                         -- the picture's box: this far in from the tile's sides and top
local WELL_SIZE = TILE_W - 2 * WELL
local LABEL_Y, LABEL_PAD = WELL + WELL_SIZE + 4, 8     -- the name's band under the picture
local LABEL_H = TILE_H - LABEL_Y - LABEL_PAD
local NAME_SIZES = { 18, 16, 14 }       -- the largest that fits the band on two lines is used
local DETAIL_SIZES = { 28, 24, 20 }     -- the name in the side column
local DIM = { 120, 126, 150 }           -- the side column's labels
local BADGE_SIZE, BADGE_H = 17, 30
local BAR_W, BAR_GAP = 10, 14           -- the scrollbar, right of the tiles
local WHEEL_ROWS = 0.5                  -- rows a wheel notch scrolls
local EASE = 14                         -- how fast the shown position follows the target
local PAD = 25                          -- the side padding of a glyph text box (the ink starts this far in)
local FREE_ALPHA = 0.4                  -- a free slot's face

-- ── names ────────────────────────────────────────────────────────────────────────────────────────
-- a stock key's name and the colour it is kept in ({r,g,b} 0-255, nil for none): the colour of a key in a
-- colour, else the item's own colour (furniture.json `color`). An item in a colour takes its plain name
-- ("<id>_plain", e.g. Plaster for Plaster (Colorable)) when it has one.
function Grid.nameOf(key)
    local id, hex = Room.splitColor(key)
    local rgb = hex and A.swatchFor(key) or Room.defaultColor(id)
    if rgb and hex then
        local ok, plain = pcall(function() return ITEMS:get(id .. "_plain") end)
        if ok and type(plain) == "string" and plain ~= "" then return plain, rgb end
    end
    return Room.displayName(id), rgb
end

local function text(key)
    local ok, v = pcall(function() return ITEMS:get(key) end)
    return (ok and type(v) == "string") and v or nil
end

-- the brackets round the colour box (full-width ones in Japanese and Chinese)
local function brackets() return text("color_open") or " (", text("color_close") or ")" end

local layouts, layoutCount = {}, 0

-- Japanese and Chinese names break between any two characters (the engine's wrap), the others at spaces
-- only, so a word is never cut; tooWide = a word is wider than the box
local CJK = { ja = true, zh = true }
local function wrapName(mgr, size, name, w)
    if CJK[I18N.code] then return mgr:wrapLines(size, name, w), false end
    local lines, cur, tooWide = {}, nil, false
    for word in name:gmatch("%S+") do
        if mgr:measureText(size, word) > w then tooWide = true end
        local try = cur and (cur .. " " .. word) or word
        if cur and mgr:measureText(size, try) > w then lines[#lines + 1] = cur; cur = word else cur = try end
    end
    if cur then lines[#lines + 1] = cur end
    return lines, tooWide
end

-- the name from where line `from` starts (a wrap drops only the spaces at its breaks)
local function restOf(name, lines, from)
    local pos = 1
    for i = 1, from - 1 do
        local s, e = name:find(lines[i], pos, true)
        if s == nil then return table.concat(lines, CJK[I18N.code] and "" or " ", from) end
        pos = e + 1
    end
    return (name:sub(pos):gsub("^%s+", ""))
end

-- the lines a name is drawn on within w x h: the largest of sizes whose lines fit with no word wider than
-- the box, the colour box after the last line (or on a line of its own). At the smallest size a name that
-- still does not fit puts its rest on the last line, which is squeezed
local function nameLayout(mgr, key, w, h, sizes)
    local ck = key .. "|" .. w .. "|" .. h .. "|" .. sizes[1] .. "|" .. I18N.code
    local L = layouts[ck]
    if L then return L end
    local name, rgb = Grid.nameOf(key)
    local open, close = brackets()
    for si, size in ipairs(sizes) do
        local lh = mgr:gfont(size).LineHeight
        local maxLines = max(1, floor(h / lh))
        local lines, tooWide = wrapName(mgr, size, name, w)
        if #lines == 0 then lines = { "" } end
        local box = floor(size * 0.8)
        if rgb then
            local last = lines[#lines]
            if last ~= "" and mgr:measureText(size, last .. open) + box + 6 + mgr:measureText(size, close) > w then
                lines[#lines + 1] = ""
            end
        end
        if (#lines <= maxLines and not tooWide) or si == #sizes then
            if #lines > maxLines then
                local keep = {}
                for i = 1, maxLines - 1 do keep[i] = lines[i] end
                keep[maxLines] = restOf(name, lines, maxLines)
                lines = keep
            end
            L = { size = size, lh = lh, lines = lines, rgb = rgb, box = box, open = open, close = close }
            break
        end
    end
    if layoutCount >= 512 then layouts, layoutCount = {}, 0 end
    layouts[ck] = L
    layoutCount = layoutCount + 1
    return L
end

-- draws a stock key's name in the box (x, y, w, h); fg/bg = the text and outline colours.
-- opts: sizes (largest first, NAME_SIZES by default), align = "center" | "left", valign = "center" | "top"
function Grid.drawName(mgr, key, x, y, w, h, fg, bg, opts)
    opts = opts or {}
    local L = nameLayout(mgr, key, w, h, opts.sizes or NAME_SIZES)
    local left = opts.align == "left"
    local n = #L.lines
    local ly = (opts.valign == "top") and floor(y) or floor(y + (h - n * L.lh) / 2)
    for i, line in ipairs(L.lines) do
        local yy = ly + (i - 1) * L.lh
        if i == n and L.rgb then
            -- the last line, then the colour box in its brackets
            local head = (line == "") and L.open:gsub("^%s+", "") or (line .. L.open)
            local closeW = mgr:measureText(L.size, L.close)
            local room = w - L.box - 6 - closeW                   -- a longer head is squeezed into it
            local headW = min(room, mgr:measureText(L.size, head))
            local x0 = left and floor(x) or floor(x + (w - (headW + 6 + L.box + closeW)) / 2)
            mgr:drawTextEx(L.size, head, x0 - PAD, yy, fg, bg, 1, 1, room + 2 * PAD, "topleft")
            local bx = x0 + headW + 3
            local by = floor(yy + (L.lh - L.box) / 2)
            local c = L.rgb
            mgr:rect(bx - 2, by - 2, L.box + 4, L.box + 4, 52, 58, 92, 255)
            mgr:rect(bx, by, L.box, L.box, c[1], c[2], c[3], 255)
            mgr:drawTextEx(L.size, L.close, bx + L.box + 3 - PAD, yy, fg, bg, 1, 1, 0, "topleft")
        elseif left then
            mgr:drawTextEx(L.size, line, floor(x) - PAD, yy, fg, bg, 1, 1, w + 2 * PAD, "topleft")
        else
            mgr:drawTextEx(L.size, line, floor(x + w / 2), yy, fg, bg, 1, 1, w + 2 * PAD, "top")
        end
    end
end

-- ── pictures ─────────────────────────────────────────────────────────────────────────────────────
-- the thumbnail My Room bakes for every shop, reward and swatch item ("myroom_thumb_<id>")
local function thumbOf(id)
    local ok, tex = pcall(function() return SHARED:GetSharedTexture("myroom_thumb_" .. id) end)
    if ok and tex and tex.Loaded then return tex end
    return nil
end

-- a stock key's picture fitted in the square (x, y, size): a flooring or paint in a colour is tinted; an item
-- without a thumbnail shows its colour, when it has one
function Grid.drawPicture(mgr, key, x, y, size)
    local id, hex = Room.splitColor(key)
    local tex = thumbOf(id)
    local isDeco = Room.FLOORDECO[id] ~= nil or Room.WALLPAINT[id] ~= nil
    local rgb = A.swatchFor(key)
    if tex then
        local s = size / max(tex.Width, tex.Height)
        if hex and isDeco and rgb then tex:SetColor(rgb[1] / 255, rgb[2] / 255, rgb[3] / 255) end
        tex:SetScale(s, s)
        tex:DrawAtAnchor(floor(x + size / 2), floor(y + size / 2), "center")
        tex:SetScale(1, 1)
        tex:SetColor(1, 1, 1)
    elseif rgb then
        mgr:rect(x + 8, y + 8, size - 16, size - 16, rgb[1], rgb[2], rgb[3], 255)
    end
end

-- ── the widget ───────────────────────────────────────────────────────────────────────────────────
function Grid.width(cols) return 2 * INSET + cols * TILE_W + (cols - 1) * GAP + BAR_GAP + BAR_W end
function Grid.height(rows) return 2 * INSET + rows * TILE_H + (rows - 1) * GAP end

function Grid.new(o)
    o = o or {}
    o.noPop = true                      -- the grid itself never grows or squishes
    local self = UI.Widget.new(o)
    setmetatable(self, Grid)
    self.items = o.items or {}
    self.cols = max(1, floor((self.w - 2 * INSET - BAR_GAP - BAR_W + GAP) / (TILE_W + GAP)))
    self.rowStep = TILE_H + GAP
    self.viewW = self.w - BAR_GAP - BAR_W
    self.selected = max(1, min(o.selected or 1, #self.items))
    self._scrollCur = o.scroll or 0
    self._scrollTarget = self._scrollCur
    self._hoverI = nil
    self._drag = nil                    -- the scrollbar's grab: the pointer's offset in the thumb
    return self
end

function Grid:rows() return ceil(#self.items / self.cols) end
-- the whole box takes the pointer (the rounded hit of other widgets would cut off the scrollbar's ends)
function Grid:hitTest(px, py) return UI.Util.pointInRect(px, py, self.x, self.y, self.w, self.h) end
function Grid:_contentH()
    local r = self:rows()
    if r == 0 then return 0 end
    return 2 * INSET + r * TILE_H + (r - 1) * GAP
end
function Grid:_maxScroll() return max(0, self:_contentH() - self.h) end
function Grid:_clamp() self._scrollTarget = UI.Util.clamp(self._scrollTarget, 0, self:_maxScroll()) end
function Grid:scrollPos() return self._scrollTarget end

-- tile i's top-left on screen at the shown scroll position
function Grid:tilePos(i)
    local r, c = floor((i - 1) / self.cols), (i - 1) % self.cols
    return self.x + INSET + c * (TILE_W + GAP), self.y + INSET + r * self.rowStep - self._scrollCur
end

-- scrolls the least needed for tile i's row to be in view (with the ring's room round it)
function Grid:showTile(i, instant)
    local r = floor((i - 1) / self.cols)
    local top = r * self.rowStep
    local bot = top + TILE_H + 2 * INSET
    if top < self._scrollTarget then self._scrollTarget = top end
    if bot > self._scrollTarget + self.h then self._scrollTarget = bot - self.h end
    self:_clamp()
    if instant then self._scrollCur = self._scrollTarget end
end

-- new stacks in place (after a sale or a recolour): the grid keeps its scroll, tile `selected` is picked
function Grid:setItems(items, selected)
    self.items = items or {}
    self.selected = max(1, min(selected or self.selected, #self.items))
    self:_clamp()
    if #self.items > 0 then self:showTile(self.selected) end
end

-- picks tile i; quiet = no move sound
function Grid:pick(i, quiet)
    if self.items[i] == nil or i == self.selected then return false end
    self.selected = i
    self:showTile(i)
    if not quiet then self:playSfx("move") end
    if self.onSelect then self.onSelect(i, self.items[i]) end
    return true
end

-- the arrow keys move in two dimensions and keep the focus at the top and bottom rows; the drum's Ka
-- (the pad's Left/Right) steps through the tiles in order, and lets the focus go at either end
function Grid:onNavDown()
    local i = self.selected + self.cols
    if i > #self.items then
        if floor((self.selected - 1) / self.cols) < self:rows() - 1 then i = #self.items else return true end
    end
    self:pick(i)
    return true
end
function Grid:onNavUp()
    if self.selected - self.cols >= 1 then self:pick(self.selected - self.cols) end
    return true
end
function Grid:onNavRight(forPad)
    if forPad then
        if self.selected < #self.items then self:pick(self.selected + 1); return true end
        return false
    end
    if (self.selected - 1) % self.cols < self.cols - 1 and self.selected < #self.items then
        self:pick(self.selected + 1)
        return true
    end
    return false
end
function Grid:onNavLeft(forPad)
    if forPad then
        if self.selected > 1 then self:pick(self.selected - 1); return true end
        return false
    end
    if (self.selected - 1) % self.cols > 0 then self:pick(self.selected - 1); return true end
    return false
end

-- Decide on the grid hands the picked stack on; a click already picked it on the press
function Grid:onActivate()
    if self.mgr and self.mgr:mouseActive() then return end
    local it = self.items[self.selected]
    if it and self.onChoose then self.onChoose(self.selected, it); return end
    self:playSfx("click")
end

function Grid:restyle()
    self:resolveStyle()
    local c = self.eff.colors
    local Shape = UI.Shape
    local m = 6
    local rs, ow = self.eff.radiusSmall, math.max(2, self.eff.outlineWidth - 2)
    local function tile(field, top, bot)
        self:bakeShared(field, "grid.tile", TILE_W + 2 * m, TILE_H + 2 * m, function(cv)
            Shape.panel(cv, m, m, TILE_W, TILE_H, { radius = rs, outline = { col = c.outline, width = ow }, top = top, bottom = bot })
        end, { m = m }, top, bot)
    end
    tile("_tile", c.surface, c.surface2)
    tile("_tileSel", c.primary, c.primary2)
    self:bakeShared("_well", "grid.well", WELL_SIZE, WELL_SIZE, function(cv)
        Shape.fillRoundAA(cv, 0, 0, WELL_SIZE, WELL_SIZE, max(4, rs - 4), { 255, 255, 255, 190 })
    end)
    self:bakeShared("_frame", "grid.frame", WELL_SIZE + 2 * m, WELL_SIZE + 2 * m, function(cv)
        Shape.panel(cv, m, m, WELL_SIZE, WELL_SIZE, { radius = rs, outline = { col = c.outline, width = ow }, top = { 255, 255, 255, 255 }, bottom = c.surface2 })
    end, { m = m })
    local rw = self.eff.outlineWidth + 4
    self:bakeShared("_ring", "grid.ring", TILE_W + 2 * m, TILE_H + 2 * m, function(cv)
        Shape.fillRoundAA(cv, m - 2, m - 2, TILE_W + 4, TILE_H + 4, rs + 2, c.focusRing)
        Shape.fillRound(cv, m - 2 + rw, m - 2 + rw, TILE_W + 4 - 2 * rw, TILE_H + 4 - 2 * rw, max(1, rs + 2 - rw), { 0, 0, 0, 0 })
    end, { m = m })
    self:bakeShared("_barDot", "grid.bar", BAR_W, BAR_W, function(cv)
        Shape.fillEllipse(cv, 0, 0, BAR_W, BAR_W, { 255, 255, 255, 255 })
    end)
    self:_dropBadges()
    self._badges = {}
    self:_clamp()
end

function Grid:_dropBadges()
    for _, b in pairs(self._badges or {}) do pcall(self.mgr.releaseBaked, b.key) end
    self._badges = nil
end

function Grid:dispose()
    if self.mgr then self:_dropBadges() end
    UI.Widget.dispose(self)
end

-- the count's pill, one bake per width
function Grid:_badge(w)
    local b = self._badges[w]
    if b then return b end
    local key = "grid.badge|" .. w .. "x" .. BADGE_H .. "|" .. self.mgr:themeKey(self.eff)
    local col = self.eff.colors.outline
    local cv = self.mgr:bakedCanvas(key, w, BADGE_H, function(cv)
        UI.Shape.fillRoundAA(cv, 0, 0, w, BADGE_H, BADGE_H / 2, { col[1], col[2], col[3], 225 })
    end)
    b = { canvas = cv, key = key }
    self._badges[w] = b
    return b
end

-- the scrollbar's track and thumb (x, y, h of each); nil while every row fits
function Grid:_bar()
    local contentH = self:_contentH()
    if contentH <= self.h then return nil end
    local x = self.x + self.viewW + BAR_GAP
    local thumbH = max(BAR_W * 4, floor(self.h * self.h / contentH))
    local k = UI.Util.clamp(self._scrollCur / max(1, contentH - self.h), 0, 1)
    return x, self.y, self.h, floor(self.y + (self.h - thumbH) * k), thumbH
end

-- the tile under the pointer, or nil
function Grid:_tileAt(mx, my)
    if mx < self.x or mx >= self.x + self.viewW or my < self.y or my >= self.y + self.h then return nil end
    local lx = mx - self.x - INSET
    local ly = my - self.y - INSET + self._scrollCur
    if lx < 0 or ly < 0 then return nil end
    local c, r = floor(lx / (TILE_W + GAP)), floor(ly / self.rowStep)
    if c >= self.cols or lx - c * (TILE_W + GAP) >= TILE_W or ly - r * self.rowStep >= TILE_H then return nil end
    local i = r * self.cols + c + 1
    return self.items[i] and i or nil
end

function Grid:update(ctx)
    UI.Widget.update(self, ctx)
    local mx, my = ctx.mx or -1e5, ctx.my or -1e5
    -- the scrollbar: drag its thumb, or click its track to move a view's height
    if self._drag then
        if ctx.mPressing then
            local bx, by, bh, ty, th = self:_bar()
            if bx then
                local k = UI.Util.clamp((my - self._drag - by) / max(1, bh - th), 0, 1)
                self._scrollTarget = k * self:_maxScroll()
                self._scrollCur = self._scrollTarget
            end
        else
            self._drag = nil
        end
    elseif self.hovered and ctx.mPressed then
        local bx, by, bh, ty, th = self:_bar()
        if bx and mx >= bx - 8 and mx < bx + BAR_W + 8 then
            if my >= ty and my < ty + th then self._drag = my - ty
            else
                self._scrollTarget = self._scrollTarget + (my < ty and -self.h or self.h)
                self:_clamp()
            end
        else
            local i = self:_tileAt(mx, my)
            if i then self:pick(i, i == self.selected) end
        end
    end
    -- the wheel over the grid
    if self.hovered and ctx.scrollDy and ctx.scrollDy ~= 0 then
        self._scrollTarget = self._scrollTarget - ctx.scrollDy * self.rowStep * WHEEL_ROWS
        self:_clamp()
    end
    self._hoverI = (self.hovered and ctx.mouseLead and not self._drag) and self:_tileAt(mx, my) or nil
    local d = self._scrollTarget - self._scrollCur
    if math.abs(d) < 0.5 then self._scrollCur = self._scrollTarget
    else self._scrollCur = self._scrollCur + d * min(1, (ctx.dt or 0) * EASE) end
end

local function piece(e, x, y, alpha)
    local cv = e.canvas
    cv:SetColor(1, 1, 1); cv:SetOpacity(alpha or 1); cv:SetScale(1, 1)
    cv:Draw(floor(x - (e.m or 0)), floor(y - (e.m or 0)))
    cv:SetOpacity(1)
end

function Grid:_drawTile(i, x, y)
    x, y = floor(x), floor(y)
    local mgr, c = self.mgr, self.eff.colors
    local it = self.items[i]
    local sel = (i == self.selected)
    piece(sel and self._tileSel or self._tile, x, y)
    piece(self._well, x + WELL, y + WELL)
    Grid.drawPicture(mgr, it.key, x + WELL + 6, y + WELL + 6, WELL_SIZE - 12)
    -- the count, on a pill in the picture's top-right corner
    local label = "×" .. tostring(it.n)
    local ink = mgr:measureText(BADGE_SIZE, label)
    local bw = max(BADGE_H + 8, ceil((ink + 20) / 6) * 6)
    local b = self:_badge(bw)
    b.canvas:SetColor(1, 1, 1); b.canvas:SetOpacity(1); b.canvas:SetScale(1, 1)
    local bxr = x + WELL + WELL_SIZE - 4
    b.canvas:Draw(bxr - bw, y + WELL + 4)
    mgr:drawTextEx(BADGE_SIZE, label, floor(bxr - bw / 2), y + WELL + 4 + floor(BADGE_H / 2) + mgr:textNudge(BADGE_SIZE),
        { 255, 255, 255 }, { 255, 255, 255, 0 }, 1, 1, 0, "center")
    -- the name under the picture
    local fg = sel and c.textOnAccent or c.text
    local bg = sel and UI.Util.shade(c.primary2, 0.5) or { 255, 255, 255, 150 }
    if self.eff.textOutline == false then bg = { 255, 255, 255, 0 } end
    Grid.drawName(mgr, it.key, x + LABEL_PAD, y + LABEL_Y, TILE_W - 2 * LABEL_PAD, LABEL_H, fg, bg)
end

function Grid:draw()
    if not self.visible then return end
    local clip = GRAPHICS ~= nil and GRAPHICS.SetClip ~= nil
    if clip then GRAPHICS:SetClip(self.x, self.y, self.viewW, self.h) end
    local rowsUsed = self:rows()
    local rowsInView = max(1, floor((self.h - 2 * INSET + GAP) / self.rowStep))
    local slots = max(rowsUsed, rowsInView) * self.cols
    local r0 = max(0, floor((self._scrollCur - INSET) / self.rowStep))
    local r1 = ceil((self._scrollCur + self.h) / self.rowStep)
    for i = r0 * self.cols + 1, min(slots, (r1 + 1) * self.cols) do
        local x, y = self:tilePos(i)
        if y < self.y + self.h and y + TILE_H > self.y then
            if self.items[i] then self:_drawTile(i, x, y)
            else piece(self._tile, x, y, FREE_ALPHA) end
        end
    end
    -- the ring: on the picked tile while the keys lead, on the tile under the pointer while the mouse does
    local ringI, ringA = nil, 0
    if self.mgr:mouseActive() then
        if self._hoverI then ringI, ringA = self._hoverI, 1 end
    elseif self.focused and self._hiCur > 0.01 then
        ringI, ringA = self.selected, self._hiCur
    end
    if ringI and self.items[ringI] then
        local x, y = self:tilePos(ringI)
        piece(self._ring, x, y, ringA)
    end
    if clip then GRAPHICS:ClearClip() end
    -- the scrollbar
    local bx, by, bh, ty, th = self:_bar()
    if bx then
        local c = self.eff.colors
        self:_pill(bx, by, bh, c.track, 0.8)
        self:_pill(bx, ty, th, c.primary2, 1)
    end
end

-- a stack drawn large in the box (x, y, w): its picture in a frame, its name beside it and, under the name,
-- stockText and its count
function Grid:drawDetail(key, n, x, y, w, stockText)
    local mgr, c = self.mgr, self.eff.colors
    piece(self._frame, x, y)
    Grid.drawPicture(mgr, key, x + 12, y + 12, WELL_SIZE - 24)
    local tx = x + WELL_SIZE + 24
    local tw = x + w - tx
    Grid.drawName(mgr, key, tx, y + 4, tw, 106, c.text, { 255, 255, 255, 150 },
        { sizes = DETAIL_SIZES, align = "left", valign = "top" })
    local sy = y + 114
    mgr:drawTextEx(20, stockText, tx - PAD, sy + 4, DIM, { 255, 255, 255, 150 }, 1, 1, tw + 2 * PAD, "topleft")
    local sw = min(tw, mgr:measureText(20, stockText))
    mgr:drawTextEx(26, "×" .. tostring(n), tx + sw + 14 - PAD, sy, c.text, { 255, 255, 255, 150 }, 1, 1, 0, "topleft")
end

-- a vertical pill of the scrollbar: the dot's two halves with its middle row stretched between them
function Grid:_pill(x, y, h, col, alpha)
    local cv = self._barDot.canvas
    local d = cv.Width
    local half = floor(d / 2)
    x, y = floor(x), floor(y)
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

return Grid
