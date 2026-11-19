---@diagnostic disable: undefined-global, undefined-field, lowercase-global, need-check-nil
-- recycle_bin furniture: sells stock items for coins

local UI = require("uilib")
local I18N = require("Locale.i18n")
local Room = require("room")
local Purse = require("purse")
local Grid = require("itemgrid")
local Catalog = require("Shop.Catalog")
local T = I18N.texts("recycle")       -- lang/<code>/recycle.json

local M = {}

M.ID = "recycle_bin"
M.SHOP_SHARE = 0.4                    -- share of the shop price

-- ==== layout ====
local PANEL = { w = 1560, h = 880 }  -- h refit to the grid in build
local GRID_COLS, GRID_ROWS = 5, 3
local COL_GAP = 48
local VALUE_W = 90                  -- right of the slider, for its number
local BTN_H = 76
local PRICE_SIZE = 34
local COIN_GAP = 30                 -- from the coin's centre to its number
local TEXT_PAD = 25                 -- label side padding: ink starts this far right of x
local GREY = { 120, 126, 150, 255 }
-- purse: on the panel's bottom edge, right end
local PURSE_W, PURSE_H = 300, 66

local ctx = nil                     -- Script.lua closures (see M.init)
local ui, W, grid = nil, {}, nil
local purse = nil                   -- purse.lua, made on the first open
local open, justOpened = false, false
local stacks = {}                   -- { key, id, n, price } for every sellable stack in stock
local sel = 1                       -- picked stack index
local amount = 1
local side = nil                    -- side column box { x, y, w }
local box = nil                     -- panel box { x, y, w, h }, for the purse
local soldDue = false               -- stock changed: refresh after ui:update

local function tr(id) return T:tr(id) end

-- coins per unit of a stock key, nil when not sold here
-- shop row (furn_<id>): SHOP_SHARE of its price, OneTime rows unsold; else the catalogue `sell`
-- default floor and paint never sold; "<id>:<rrggbb>" sells as <id>
function M.priceOf(key)
    local id = Room.splitColor(key)
    if id == Room.DEFAULT_FLOOR or id == Room.DEFAULT_PAINT then return nil end
    local ok, row = pcall(Catalog.find, "furn_" .. id)
    local price = nil
    if ok and row ~= nil then
        if Catalog.isOneTime(row) then return nil end
        price = math.floor((tonumber(row.Price) or 0) * M.SHOP_SHARE)
    else
        local c = Room.CATALOG[id] or Room.FLOORDECO[id] or Room.WALLPAINT[id]
        price = c and c.sell and math.floor(c.sell) or nil
    end
    if price == nil or price < 1 then return nil end
    return price
end

-- sellable stacks in stock, sorted by name
local function collect()
    local room = ctx.room()
    local out = {}
    for key, n in pairs(room.inventory or {}) do
        local price = (n or 0) > 0 and M.priceOf(key) or nil
        if price then out[#out + 1] = { key = key, id = Room.splitColor(key), n = n, price = price } end
    end
    table.sort(out, function(a, b)
        local na, nb = Room.displayName(a.key), Room.displayName(b.key)
        if na ~= nb then return na < nb end
        return a.key < b.key
    end)
    return out
end

-- side widgets for the picked stack
local function showStack()
    local s = stacks[sel]
    if s == nil or W.price == nil then return end
    W.price:setText(tostring(s.price))
    W.slider.max = s.n
    amount = math.max(0, math.min(amount, s.n))
    W.slider:setValue(amount, true)
    W.total:setText(tostring(amount * s.price))
    W.go:setEnabled(amount > 0)
end

local function build()
    if ui then pcall(function() ui:disposeWidgets() end) end
    W, grid, side = {}, nil, nil
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
    box = { x = panel.x, y = panel.y, w = panel.w, h = panel.h }
    if #stacks == 0 then
        W.empty = ui:label{ x = cx + cw / 2, y = cy + ch / 2 - 20, text = tr("empty"), size = 26, align = "center",
                            maxWidth = cw - 80, color = GREY }
        return
    end
    sel = math.max(1, math.min(sel, #stacks))
    local gw = Grid.width(GRID_COLS)
    grid = ui:add(Grid.new{ x = cx, y = cy, w = gw, h = need, items = stacks, selected = sel,
        onSelect = function(i) sel = i; amount = 1; showStack() end,
        onChoose = function() if W.go then ui:focus(W.go) end end })
    grid:showTile(sel, true)

    -- side column; prices: coin icon left of the number
    local rx = cx + gw + COL_GAP
    local rw = cx + cw - rx
    side = { x = rx, y = cy, w = rw }
    local py = cy + 196
    ui:label{ x = rx, y = py, text = tr("price"), size = 22, color = GREY }
    W.price = ui:label{ x = rx + 18 + COIN_GAP, y = py + 38, text = "", size = PRICE_SIZE }
    local ay = py + 124
    ui:label{ x = rx, y = ay, text = tr("amount"), size = 22, color = GREY }
    W.slider = ui:slider{ x = rx + 10, y = ay + 44, w = rw - VALUE_W - 10, min = 0, max = 1, step = 1, value = 0,
        onChange = function(v)
            amount = v
            local s = stacks[sel]
            if s then W.total:setText(tostring(amount * s.price)) end
            W.go:setEnabled(amount > 0)
        end }
    local ty = ay + 134
    ui:label{ x = rx, y = ty, text = tr("total"), size = 22, color = GREY }
    W.total = ui:label{ x = rx + 18 + COIN_GAP, y = ty + 38, text = "", size = PRICE_SIZE }
    W.go = ui:button{ text = tr("sell"), x = rx, y = cy + ch - BTN_H - 14, w = rw, h = BTN_H, accent = true,
        onClick = function() M.apply() end }
    showStack()
end

-- sell `amount` of the picked stack
function M.apply()
    local s = stacks[sel]
    if s == nil or amount < 1 then return end
    local room = ctx.room()
    local have = room.inventory[s.key] or 0
    local n = math.min(amount, have)
    local sf = ctx.save()
    if n < 1 or sf == nil then return end
    local coins = n * s.price
    room.inventory[s.key] = have - n        -- kept at 0, as taking from stock does
    sf:EarnCoins(coins)
    if purse then purse:gain(coins) end
    if ctx.onChanged then ctx.onChanged() end
    -- keep the same stack picked while some is left
    local again = collect()
    local keep = nil
    for i, st in ipairs(again) do if st.key == s.key then keep = i end end
    sel = keep or math.min(sel, #again)
    amount = 1
    soldDue = true
end

-- coin icon left of a price label, centred on its line
local function drawPrice(label)
    if label == nil or not label.visible or purse == nil then return end
    local lineH = label.mgr:gfont(PRICE_SIZE).LineHeight
    purse:drawCoin(math.floor(label.x + TEXT_PAD - COIN_GAP), math.floor(label.y + lineH * 0.5), 0.8, 0.8)
end

-- ==== public API ====
-- ctx: { theme, playerIndex() -> n, room() -> Room, save() -> save file|nil, onChanged() on a stock change }
function M.init(c) ctx = c end

function M.isOpen() return open end

function M.open()
    if open or ctx == nil then return end
    open, justOpened, soldDue = true, true, false
    sel, amount = 1, 1
    build()
    if grid then ui:focus(grid) end
    local px, py = box.x + box.w - 40 - PURSE_W, box.y + box.h - 30
    if purse and (purse.x ~= px or purse.y ~= py) then purse:dispose(); purse = nil end
    purse = purse or Purse.new{ x = px, y = py, w = PURSE_W, h = PURSE_H }
    local sf = ctx.save()
    purse:show(sf and sf.Coins or 0)
end

function M.close(silent)
    if ui then pcall(function() ui:disposeWidgets() end) end
    ui, W, grid, side, open = nil, {}, nil, nil, false
    if purse then purse:hide() end
    if not silent then pcall(function() SHARED:GetSharedSound("Cancel"):Play() end) end
end

-- "closed" on the frame the window shuts
function M.update(dt, ts)
    if not open then return nil end
    if purse then purse:update(dt) end
    if justOpened then justOpened = false; return nil end
    if ui == nil then M.close(true); return "closed" end
    local r = ui:update(ts)
    if soldDue then
        soldDue = false
        -- grid updates in place; full rebuild only when the stock runs out
        local again = collect()
        if grid and #again > 0 then
            stacks = again
            grid:setItems(stacks, sel)
            showStack()
        else
            build()
        end
    end
    if r == "cancel" then M.close(); return "closed" end
    return nil
end

function M.draw()
    if not open or ui == nil then return end
    ui:draw()
    local s = stacks[sel]
    if grid and side and s then grid:drawDetail(s.key, s.n, side.x, side.y, side.w, tr("stock")) end
    drawPrice(W.price)
    drawPrice(W.total)
    if purse then purse:draw() end
end

function M.dispose()
    M.close(true)
    if purse then purse:dispose(); purse = nil end
end

return M
