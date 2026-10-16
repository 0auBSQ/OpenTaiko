---@diagnostic disable: undefined-global, undefined-field, lowercase-global, need-check-nil
-- workbench.lua — the Workbench furniture: recolours the floorings and wall paints in the room's stock.
--
-- A flooring or paint with a palette (`colors` in data/floordeco.json / data/wallpaint.json) is kept
-- per colour, as "<id>:<rrggbb>" (Room.colorKey); the plain id is its default colour, which is always
-- one of the choices. The window lists every such stack in stock (a click picks one; hovering moves
-- nothing), shows its colours and how many to recolour. After a recolour the same stack, colour and
-- amount stay picked, so the next one is a single press. Items without a palette never show here.

local PopUI = require("PopUI")
local I18N = require("i18n")
local Room = require("room")
local A = require("assets")
local U = require("PopUI.util")
local T = I18N.texts("workbench")     -- lang/<code>/workbench.json

local M = {}

M.ID = "workbench"

-- ── layout ─────────────────────────────────────────────────────────────────────────────────────
local PANEL = { w = 1280, h = 760 }
local LIST_W, ROW = 600, 72
local COL_GAP = 48
local CHIP, CHIP_GAP, CHIPS_PER_ROW = 92, 16, 5    -- the colour buttons
local COUNT_W = 170                 -- the count column at the right end of every stock row
local DOT = 34                      -- the colour square in that column
local VALUE_W = 90                  -- room right of the amount slider for its number
local BTN_H = 76
local GREY = { 120, 126, 150, 255 }

local ctx = nil                     -- Script.lua closures (see M.init)
local ui, W = nil, {}
local open, justOpened = false, false
local stacks = {}                   -- { key, id, hex, n } for every recolourable stack in stock
local sel = 1                       -- the stack picked in the list
local target = nil                  -- the colour picked: a hex, or "" for the default colour
local amount = 1
local pending = nil                 -- "list" (a stack was picked) / "done" (recoloured): laid out after the UI update

local function tr(id) return T:tr(id) end
local function playSfx(name) pcall(function() SHARED:GetSharedSound(name):Play() end) end

-- every recolourable stack in stock, by item then colour (the default colour first)
local function collect()
    local room = ctx.room()
    local out = {}
    for key, n in pairs(room.inventory or {}) do
        local id, hex = Room.splitColor(key)
        if (n or 0) > 0 and #Room.decoColors(id) > 0 then
            out[#out + 1] = { key = key, id = id, hex = hex or "", n = n }
        end
    end
    table.sort(out, function(a, b) return a.key < b.key end)
    return out
end

-- the colours an item can take: "" (the default) then its palette
local function colorsOf(id)
    local out = { "" }
    for _, hex in ipairs(Room.decoColors(id)) do out[#out + 1] = hex end
    return out
end

local function rgbOf(id, hex)
    return A.swatchFor(Room.colorKey(id, hex)) or { 200, 200, 200 }
end

-- the amount line and whether the button can recolour, after any change of colour or amount
local function refreshAmount()
    local s = stacks[sel]
    if s == nil then return end
    amount = math.max(0, math.min(amount, s.n))
    if W.go then W.go:setEnabled(amount > 0 and target ~= nil and target ~= s.hex) end
end

local function build()
    if ui then pcall(function() ui:disposeWidgets() end) end
    W = {}
    stacks = collect()
    ui = PopUI.new{ theme = ctx.theme, navPlayer = ctx.playerIndex() + 1, nav = "spatial" }
    local x, y = (1920 - PANEL.w) / 2, (1080 - PANEL.h) / 2
    local panel = ui:panel{ x = x, y = y, w = PANEL.w, h = PANEL.h, pad = 30, title = Room.displayName(M.ID) }
    local cx, cy, cw, ch = panel:content()
    if #stacks == 0 then
        W.empty = ui:label{ x = cx + cw / 2, y = cy + ch / 2 - 20, text = tr("empty"), size = 26, align = "center",
                            maxWidth = cw - 80, color = GREY }
        return
    end
    sel = math.max(1, math.min(sel, #stacks))
    local s = stacks[sel]
    ui:label{ x = cx, y = cy + 4, text = tr("stock"), size = 22, color = GREY }
    local items = {}
    for i, st in ipairs(stacks) do items[i] = { text = Room.displayName(st.id), value = i } end
    W.list = ui:menu{ x = cx, y = cy + 44, w = LIST_W, h = ch - 44, rowHeight = ROW, items = items, selected = sel,
        hoverSelects = false, textReserve = COUNT_W,
        onChange = function(i) sel = i; target = nil; amount = stacks[i] and stacks[i].n or 1; pending = "list" end }
    W.list:_ensureVisible()
    W.list._scrollCur = W.list._scrollTarget

    -- the colours of the picked stack (its own colour can't be picked)
    local rx = cx + LIST_W + COL_GAP
    local rw = cw - LIST_W - COL_GAP
    ui:label{ x = rx, y = cy + 4, text = tr("color"), size = 22, color = GREY }
    W.chips = {}
    for i, hex in ipairs(colorsOf(s.id)) do
        local col = rgbOf(s.id, hex)
        local dark = { math.floor(col[1] * 0.75), math.floor(col[2] * 0.75), math.floor(col[3] * 0.75) }
        local bx = rx + ((i - 1) % CHIPS_PER_ROW) * (CHIP + CHIP_GAP)
        local by = cy + 44 + math.floor((i - 1) / CHIPS_PER_ROW) * (CHIP + CHIP_GAP)
        local b = ui:button{ text = "", x = bx, y = by, w = CHIP, h = CHIP, accent = true, disabledLook = "fade",
            style = { colors = { primary = col, primary2 = dark } },
            onClick = function() target = hex; refreshAmount() end }
        if hex == s.hex then b:setEnabled(false) end
        W.chips[#W.chips + 1] = { b = b, hex = hex }
    end

    -- how many (the slider's number sits right of its track, inside the column), then the button
    local rows = math.ceil(#W.chips / CHIPS_PER_ROW)
    local ay = cy + 44 + rows * (CHIP + CHIP_GAP) + 24
    ui:label{ x = rx, y = ay, text = tr("amount"), size = 22, color = GREY }
    amount = math.max(0, math.min(amount, s.n))
    W.slider = ui:slider{ x = rx + 10, y = ay + 44, w = rw - VALUE_W - 10, min = 0, max = s.n, step = 1,
        value = amount, onChange = function(v) amount = v; refreshAmount() end }
    W.go = ui:button{ text = tr("recolor"), x = rx, y = ay + 124, w = rw, h = BTN_H, accent = true,
        onClick = function() M.apply() end }
    refreshAmount()
end

-- recolour `amount` of the picked stack into the picked colour
function M.apply()
    local s = stacks[sel]
    if s == nil or target == nil or target == s.hex or amount < 1 then return end
    local room = ctx.room()
    local have = room.inventory[s.key] or 0
    local n = math.min(amount, have)
    if n < 1 then return end
    local to = Room.colorKey(s.id, target)
    room.inventory[s.key] = have - n
    if room.inventory[s.key] <= 0 then room.inventory[s.key] = nil end
    room.inventory[to] = (room.inventory[to] or 0) + n
    playSfx("Decide")
    if ctx.onChanged then ctx.onChanged() end
    -- the same stack stays picked while some is left (else the one now in its place), with the same colour
    local again = collect()
    local keep = nil
    for i, st in ipairs(again) do if st.key == s.key then keep = i end end
    sel = keep or math.min(sel, #again)
    if keep == nil then target = nil end
    pending = "done"
end

-- ── public API ─────────────────────────────────────────────────────────────────────────────────
-- ctx: { theme, playerIndex()->n, room()->Room, onChanged() (the stock changed) }
function M.init(c) ctx = c end

function M.isOpen() return open end

function M.open()
    if open or ctx == nil then return end
    open, justOpened, pending = true, true, nil
    sel, target, amount = 1, nil, 1
    local st = collect()[1]
    if st then amount = st.n end
    build()
    if W.list then ui:focus(W.list) end
end

function M.close(silent)
    if ui then pcall(function() ui:disposeWidgets() end) end
    ui, W, open = nil, {}, false
    if not silent then playSfx("Cancel") end
end

-- returns "closed" the frame the window shuts
function M.update(dt, ts)
    if not open then return nil end
    if justOpened then justOpened = false; return nil end
    if ui == nil then M.close(true); return "closed" end
    local r = ui:update(ts)
    if pending then
        local why = pending
        pending = nil
        build()
        -- the focus stays where the player was: the list, or the button they just pressed
        if not (why == "done" and W.go and ui:focus(W.go)) and W.list then ui:focus(W.list) end
    end
    if r == "cancel" then M.close(); return "closed" end
    return nil
end

function M.draw()
    if not open or ui == nil then return end
    ui:draw()
    -- the count and a colour square at the right end of each visible stock row
    local list = W.list
    if list then
        local vy0, vy1 = list.y, list.y + list.h
        local c = list.eff.colors
        local size = list.eff.font.label
        -- a rectangle cut to the list's viewport, like the rows it sits on
        local function clipped(x, y, w, h, r, g, b)
            local y0, y1 = math.max(y, vy0), math.min(y + h, vy1)
            if y1 > y0 then ui:rect(x, y0, w, y1 - y0, r, g, b, 255) end
        end
        for i, st in ipairs(stacks) do
            local ry = list.y + (i - 1) * list.rowHeight - (list._scrollCur or 0)
            if ry + list.rowHeight > vy0 and ry < vy1 then
                local cy = math.floor(ry + list.rowHeight / 2)
                local col = rgbOf(st.id, st.hex)
                local dx = list.x + list.w - DOT - 24
                clipped(dx - 2, cy - DOT / 2 - 2, DOT + 4, DOT + 4, 40, 40, 52)
                clipped(dx, cy - DOT / 2, DOT, DOT, col[1], col[2], col[3])
                local isSel = (i == list.selected)
                local fg = isSel and c.textOnAccent or c.text
                local bg = isSel and U.shade(c.primary2, 0.5) or { 255, 255, 255, 150 }
                ui:drawTextEx(size, "x" .. st.n, dx - 14, cy + ui:textNudge(size), fg, bg, 1, 1, 0, "right", vy0, vy1)
            end
        end
    end
    -- a white ring around the picked colour
    for _, c in ipairs(W.chips or {}) do
        if c.hex == target then
            local b = c.b
            ui:rect(b.x - 7, b.y - 7, b.w + 14, 5, 255, 255, 255, 255)
            ui:rect(b.x - 7, b.y + b.h + 2, b.w + 14, 5, 255, 255, 255, 255)
            ui:rect(b.x - 7, b.y - 7, 5, b.h + 14, 255, 255, 255, 255)
            ui:rect(b.x + b.w + 2, b.y - 7, 5, b.h + 14, 255, 255, 255, 255)
        end
    end
end

function M.dispose() M.close(true) end

return M
