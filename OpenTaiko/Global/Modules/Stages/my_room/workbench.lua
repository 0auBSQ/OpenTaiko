---@diagnostic disable: undefined-global, undefined-field, lowercase-global, need-check-nil
-- workbench furniture: recolours palette items in stock
-- one stack per colour: "<id>:<rrggbb>" (Room.colorKey); the plain id is the default colour

local UI = require("uilib")
local I18N = require("Locale.i18n")
local Room = require("room")
local A = require("assets")
local Grid = require("itemgrid")
local T = I18N.texts("workbench")     -- lang/<code>/workbench.json

local M = {}

M.ID = "workbench"

-- ==== layout ====
local PANEL = { w = 1560, h = 880 }  -- h refit to the grid in build
local GRID_COLS, GRID_ROWS = 5, 3
local COL_GAP = 48
local CHIP, CHIP_GAP, CHIPS_PER_ROW = 84, 14, 5    -- colour chips
local VALUE_W = 90                  -- right of the slider, for its number
local BTN_H = 76
local GREY = { 120, 126, 150, 255 }

local ctx = nil                     -- Script.lua closures (see M.init)
local ui, W, grid = nil, {}, nil
local open, justOpened = false, false
local stacks = {}                   -- { key, id, hex, n } for every recolourable stack in stock
local sel = 1                       -- picked stack index
local target = nil                  -- picked hex, "" for the default
local amount = 1
local pending = nil                 -- "side" (stack picked) | "done" (recoloured), handled after ui:update
local area = nil                    -- side column box { x, y, w, bottom }
local side = {}                     -- side column widgets

local function tr(id) return T:tr(id) end
local function playSfx(name) pcall(function() SHARED:GetSharedSound(name):Play() end) end

-- recolourable stacks in stock, sorted by name then key (default colour first)
local function collect()
    local room = ctx.room()
    local out = {}
    for key, n in pairs(room.inventory or {}) do
        local id, hex = Room.splitColor(key)
        if (n or 0) > 0 and #Room.decoColors(id) > 0 then
            out[#out + 1] = { key = key, id = id, hex = hex or "", n = n }
        end
    end
    table.sort(out, function(a, b)
        local na, nb = Room.displayName(a.id), Room.displayName(b.id)
        if na ~= nb then return na < nb end
        return a.key < b.key
    end)
    return out
end

-- "" (default) then the palette
local function colorsOf(id)
    local out = { "" }
    for _, hex in ipairs(Room.decoColors(id)) do out[#out + 1] = hex end
    return out
end

local function rgbOf(id, hex)
    return A.swatchFor(Room.colorKey(id, hex)) or Room.defaultColor(id) or { 200, 200, 200 }
end

-- clamp amount and enable the button, after a colour or amount change
local function refreshAmount()
    local s = stacks[sel]
    if s == nil then return end
    amount = math.max(0, math.min(amount, s.n))
    if W.go then W.go:setEnabled(amount > 0 and target ~= nil and target ~= s.hex) end
end

-- side column for the picked stack; its own colour chip is disabled
local function buildSide()
    for _, w in ipairs(side) do ui:remove(w); pcall(function() w:dispose() end) end
    side, W.chips, W.slider, W.go = {}, {}, nil, nil
    local s = stacks[sel]
    if s == nil then return end
    local function add(w) side[#side + 1] = w; return w end
    local rx, rw = area.x, area.w
    local ry = area.y + 196
    add(ui:label{ x = rx, y = ry, text = tr("color"), size = 22, color = GREY })
    for i, hex in ipairs(colorsOf(s.id)) do
        local col = rgbOf(s.id, hex)
        local dark = { math.floor(col[1] * 0.75), math.floor(col[2] * 0.75), math.floor(col[3] * 0.75) }
        local bx = rx + ((i - 1) % CHIPS_PER_ROW) * (CHIP + CHIP_GAP)
        local by = ry + 44 + math.floor((i - 1) / CHIPS_PER_ROW) * (CHIP + CHIP_GAP)
        local b = add(ui:button{ text = "", x = bx, y = by, w = CHIP, h = CHIP, accent = true, disabledLook = "fade",
            style = { colors = { primary = col, primary2 = dark } },
            onClick = function() target = hex; refreshAmount() end })
        if hex == s.hex then b:setEnabled(false) end
        W.chips[#W.chips + 1] = { b = b, hex = hex }
    end
    -- slider (number right of its track), then the button
    local rows = math.ceil(#W.chips / CHIPS_PER_ROW)
    local ay = ry + 44 + rows * (CHIP + CHIP_GAP) + 24
    add(ui:label{ x = rx, y = ay, text = tr("amount"), size = 22, color = GREY })
    amount = math.max(0, math.min(amount, s.n))
    W.slider = add(ui:slider{ x = rx + 10, y = ay + 44, w = rw - VALUE_W - 10, min = 0, max = s.n, step = 1,
        value = amount, onChange = function(v) amount = v; refreshAmount() end })
    W.go = add(ui:button{ text = tr("recolor"), x = rx, y = area.bottom - BTN_H - 14, w = rw, h = BTN_H, accent = true,
        onClick = function() M.apply() end })
    refreshAmount()
end

-- first enabled chip (Decide on the grid focuses it)
local function firstChip()
    for _, c in ipairs(W.chips or {}) do if c.b.enabled then return c.b end end
    return nil
end

local function build()
    if ui then pcall(function() ui:disposeWidgets() end) end
    W, grid, side, area = {}, nil, {}, nil
    stacks = collect()
    ui = UI.new{ theme = ctx.theme, navPlayer = ctx.playerIndex() + 1, nav = "spatial" }
    local x, y = (1920 - PANEL.w) / 2, (1080 - PANEL.h) / 2
    local panel = ui:panel{ x = x, y = y, w = PANEL.w, h = PANEL.h, pad = 30, title = Room.displayName(M.ID) }
    -- panel fits GRID_ROWS rows of tiles under its title (title height comes from the font)
    local cx, cy, cw, ch = panel:content()
    local need = Grid.height(GRID_ROWS)
    if ch ~= need then
        panel.h = math.ceil(panel.h + need - ch)
        panel.y = math.floor((1080 - panel.h) / 2)
        panel:restyle()
        cx, cy, cw, ch = panel:content()
    end
    if #stacks == 0 then
        W.empty = ui:label{ x = cx + cw / 2, y = cy + ch / 2 - 20, text = tr("empty"), size = 26, align = "center",
                            maxWidth = cw - 80, color = GREY }
        return
    end
    sel = math.max(1, math.min(sel, #stacks))
    local gw = Grid.width(GRID_COLS)
    grid = ui:add(Grid.new{ x = cx, y = cy, w = gw, h = need, items = stacks, selected = sel,
        onSelect = function(i)
            sel, target = i, nil
            amount = stacks[i] and stacks[i].n or 1
            pending = "side"
        end,
        onChoose = function() local b = firstChip(); if b then ui:focus(b) end end })
    grid:showTile(sel, true)
    local rx = cx + gw + COL_GAP
    area = { x = rx, y = cy, w = cx + cw - rx, bottom = cy + ch }
    buildSide()
end

-- recolour `amount` of the picked stack to target
function M.apply()
    local s = stacks[sel]
    if s == nil or target == nil or target == s.hex or amount < 1 then return end
    local room = ctx.room()
    local have = room.inventory[s.key] or 0
    local n = math.min(amount, have)
    if n < 1 then return end
    local to = Room.colorKey(s.id, target)
    room.inventory[s.key] = have - n        -- kept at 0, as taking from stock does
    room.inventory[to] = (room.inventory[to] or 0) + n
    playSfx("Decide")
    if ctx.onChanged then ctx.onChanged() end
    -- keep the same stack and colour picked while some is left
    local again = collect()
    local keep = nil
    for i, st in ipairs(again) do if st.key == s.key then keep = i end end
    sel = keep or math.min(sel, #again)
    if keep == nil then target = nil end
    pending = "done"
end

-- ==== public API ====
-- ctx: { theme, playerIndex() -> n, room() -> Room, onChanged() on a stock change }
function M.init(c) ctx = c end

function M.isOpen() return open end

function M.open()
    if open or ctx == nil then return end
    open, justOpened, pending = true, true, nil
    sel, target, amount = 1, nil, 1
    local st = collect()[1]
    if st then amount = st.n end
    build()
    if grid then ui:focus(grid) end
end

function M.close(silent)
    if ui then pcall(function() ui:disposeWidgets() end) end
    ui, W, grid, side, area, open = nil, {}, nil, {}, nil, false
    if not silent then playSfx("Cancel") end
end

-- "closed" on the frame the window shuts
function M.update(dt, ts)
    if not open then return nil end
    if justOpened then justOpened = false; return nil end
    if ui == nil then M.close(true); return "closed" end
    local r = ui:update(ts)
    if pending then
        local why = pending
        pending = nil
        if why == "side" then
            -- side column only; grid and focus stay
            local f = ui:focused()
            buildSide()
            if f then ui:focus(f) end
        else
            -- grid updates in place; focus moves to the new button
            local again = collect()
            if grid and #again > 0 then
                stacks = again
                grid:setItems(stacks, sel)
                buildSide()
                ui:clearPrevFocus()             -- old button is gone
                if W.go then ui:focus(W.go) end
            else
                build()
            end
        end
    end
    if r == "cancel" then M.close(); return "closed" end
    return nil
end

function M.draw()
    if not open or ui == nil then return end
    ui:draw()
    local s = stacks[sel]
    if grid and area and s then grid:drawDetail(s.key, s.n, area.x, area.y, area.w, tr("stock")) end
    -- ring around the target chip
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
