---@diagnostic disable: undefined-global, undefined-field, lowercase-global
-- matrix.lua — the Placeholder Matrix furniture (My Room).
--
-- The spot: a floor emitter whose cubes float and turn (the GLB's own animation, started once the
-- room is built). Its glow part and a green point light breathe together.
--
-- The window: Enter on it lists the debug scenes of data/placeholder_matrix.json. A scene with no
-- price is open from the start; a priced one shows its price and is unlocked once per save with
-- coins (a global trigger), asked over the list with the purse on the window's bottom edge.
-- Choosing an open scene hands its stage back to Script, which leaves the room for it.

local PopUI = require("PopUI")
local CoinBox = require("CoinBox")
local I18N = require("i18n")
local T = I18N.texts("matrix")     -- lang/<code>/matrix.json

local M = {}

M.ID = "placeholder_matrix"

-- ── layout ─────────────────────────────────────────────────────────────────────────────────────
local PANEL = { w = 1240, h = 760 }
local LIST_W, ROW = 620, 64
local DESC_SIZE, DESC_LINE, DESC_CHARS = 26, 40, 34
local ASK = { w = 820, h = 390, row = 76 }
local PRICE_SIZE = 30
local COIN_GAP = 30         -- from the coin's centre to its number
local TEXT_PAD = 25         -- a text box's side padding (the ink starts this far right of a left label's x)
local ROW_PRICE = { size = 26, coin = 0.7, gap = 26, w = 140 }   -- a locked row's price, and the room it keeps
local INK, OUTLINE = { 52, 58, 92 }, { 255, 255, 255, 220 }
-- the purse sits on the list window's bottom edge, at its right end
local PURSE_W, PURSE_H = 300, 66
local PURSE_X = (1920 + PANEL.w) / 2 - 40 - PURSE_W
local PURSE_Y = (1080 + PANEL.h) / 2 - 30

-- ── the glow ───────────────────────────────────────────────────────────────────────────────────
local GLOW = { 0.3, 1.0, 0.5 }
local GLOW_SCALE = 0.9
local LIGHT_INTENSITY, LIGHT_RANGE = 0.6, 3.5

local ctx = nil             -- Script.lua closures (see M.init)
local clock = 0
local lightDefs = {}        -- furniture entry → the live light def handed to daynight

local function tr(id) return T:tr(id) end

local function placed()
    local out = {}
    if ctx == nil or ctx.room() == nil then return out end
    for _, it in ipairs(ctx.room().furniture or {}) do
        if it.id == M.ID then out[#out + 1] = it end
    end
    return out
end

local function pulse() return 0.75 + 0.25 * math.sin(clock * 2.4) end

-- the glow part's current emissive, for the focus highlight to keep as its base
function M.glowNow()
    local p = pulse() * GLOW_SCALE
    return { GLOW[1] * p, GLOW[2] * p, GLOW[3] * p }
end

-- the light of every placed matrix; Script.rebuild appends them to the room's lights. Their
-- animations start here too, since this runs right after the room's models are built.
function M.lights(world)
    local out = {}
    lightDefs = {}
    for _, it in ipairs(placed()) do
        local inst = ctx.propInstFor(it)
        if inst then
            if inst.anim == nil and inst.play then pcall(function() inst:play(0) end) end
            local s = inst.scale or 1
            local def = { x = inst.x, y = inst.y + 0.7 * s, z = inst.z, r = GLOW[1], g = GLOW[2], b = GLOW[3],
                          intensity = LIGHT_INTENSITY, range = LIGHT_RANGE, live = true }
            lightDefs[it] = def
            out[#out + 1] = def
        end
    end
    return out
end

-- every frame: the glow part and the light breathe
function M.tick(dt)
    if ctx == nil then return end
    clock = clock + dt
    local scene = ctx.scene()
    local g = M.glowNow()
    for _, it in ipairs(placed()) do
        local inst = ctx.propInstFor(it)
        if inst and inst.parts and scene and scene.ObjSetEmissive then
            for _, p in ipairs(inst.parts) do pcall(function() scene:ObjSetEmissive(p.obj, g[1], g[2], g[3]) end) end
        end
        local def = lightDefs[it]
        if def then def.intensity = LIGHT_INTENSITY * pulse() end
    end
end

-- ── the scenes ─────────────────────────────────────────────────────────────────────────────────
local scenes = nil

local function loadScenes()
    if scenes then return scenes end
    scenes = {}
    local doc = JSONLOADER:JsonParseFileAny("data/placeholder_matrix.json")
    local list = doc and JSONLOADER:JsonGet(doc, "scenes")
    local n = list and JSONLOADER:JsonCount(list) or 0
    for i = 1, n do
        local node = JSONLOADER:JsonGet(list, i)
        local stage = node and JSONLOADER:JsonGet(node, "stage")
        if stage then
            scenes[#scenes + 1] = {
                stage = stage,
                name = JSONLOADER:JsonGet(node, "name") or stage,
                desc = JSONLOADER:JsonGet(node, "desc") or "",
                price = math.max(0, math.floor(tonumber(JSONLOADER:JsonGet(node, "price")) or 0)),
            }
        end
    end
    return scenes
end

local function unlockTrigger(s) return ".matrix_unlocked_" .. s.stage end

local function isOpen(s)
    if s.price <= 0 then return true end
    local sf = ctx and ctx.save() or nil
    return sf ~= nil and sf:GetGlobalTrigger(unlockTrigger(s)) == true
end

-- words wrapped into lines of at most `n` characters
local function wrap(text, n)
    local lines, cur = {}, ""
    for word in tostring(text):gmatch("%S+") do
        if cur == "" then cur = word
        elseif #cur + 1 + #word <= n then cur = cur .. " " .. word
        else lines[#lines + 1] = cur; cur = word end
    end
    if cur ~= "" then lines[#lines + 1] = cur end
    return lines
end

-- ── window state ───────────────────────────────────────────────────────────────────────────────
local ui, askUi, W = nil, nil, {}
local purse = nil           -- Lib/CoinBox, made on the first open
local mode = nil            -- nil | "list" | "ask"
local selected = 1
local askDone = false       -- the unlock prompt closes after its update
local chosen = nil          -- the stage to open, handed to Script once
local justOpened = false

local function playSfx(name) pcall(function() SHARED:GetSharedSound(name):Play() end) end

local function focusWidget(mgr, w)
    if mgr == nil or w == nil then return end
    for i, f in ipairs(mgr.focusables) do
        if f == w then mgr:_setFocusIndex(i); return end
    end
end

local function disposeUi(mgr) if mgr then pcall(function() mgr:disposeWidgets() end) end end

local function showDesc(s)
    local lines = wrap(s and s.desc or "", DESC_CHARS)
    for i, l in ipairs(W.desc or {}) do l:setText(lines[i] or "") end
    if W.price then
        if s and not isOpen(s) then W.price:setText(tostring(s.price)); W.price:setVisible(true)
        else W.price:setVisible(false) end
    end
end

-- a price: the coin icon left of its number (a label), centred on the number's glyph line
local function drawPrice(label)
    if label == nil or not label.visible or purse == nil then return end
    local inkX = label.x + TEXT_PAD
    if label.align == "center" then inkX = label.x - (label.w - 2 * TEXT_PAD) / 2 end
    local lineH = label.mgr:gfont(PRICE_SIZE).LineHeight
    purse:drawCoin(math.floor(inkX - COIN_GAP), math.floor(label.y + lineH * 0.5), 0.8, 0.8)
end

-- a locked row's price in place of the padlock: the number ending at `right`, the coin before it
local function drawRowPrice(it, right, cy, menu)
    local text = tostring(it.value.price)
    local mgr = menu.mgr
    mgr:drawTextEx(ROW_PRICE.size, text, right + TEXT_PAD, cy + mgr:textNudge(ROW_PRICE.size), INK, OUTLINE, 1, 1, 0, "right")
    if purse then
        local coinX = right - mgr:measureText(ROW_PRICE.size, text) - ROW_PRICE.gap
        purse:drawCoin(math.floor(coinX), cy, ROW_PRICE.coin, ROW_PRICE.coin)
    end
end

local function closeAsk()
    disposeUi(askUi)
    askUi, askDone = nil, false
    W.ask, W.msg, W.askPrice = nil, nil, nil
    focusWidget(ui, W.list)
    mode = "list"
end

-- the unlock prompt, over the list
local function askUnlock(s, item)
    askDone = false
    askUi = PopUI.new{ theme = ctx.theme, navPlayer = ctx.playerIndex() + 1 }
    local x, y = (1920 - ASK.w) / 2, (1080 - ASK.h) / 2
    local panel = askUi:panel{ x = x, y = y, w = ASK.w, h = ASK.h, pad = 30, title = tr("unlock_title") }
    local cx, cy, cw = panel:content()
    askUi:label{ x = cx + cw / 2, y = cy + 26, text = string.format(tr("unlock_q"), s.name), size = 28,
                 align = "center", maxWidth = cw }
    -- shifted right so the coin and its number are centred together
    W.askPrice = askUi:label{ x = cx + cw / 2 + (COIN_GAP + 18) / 2, y = cy + 76, text = tostring(s.price),
                              size = PRICE_SIZE, align = "center" }
    W.msg = askUi:label{ x = cx + cw / 2, y = cy + 134, text = "", size = 24, align = "center", maxWidth = cw,
                         color = { 214, 64, 56, 255 } }
    W.ask = askUi:menu{ x = cx, y = cy + 180, w = cw, h = ASK.row * 2, rowHeight = ASK.row, selected = 1,
        items = { tr("unlock_yes"), tr("unlock_no") },
        onSelect = function(i)
            if i == 2 then playSfx("Cancel"); askDone = true; return true end
            local sf = ctx.save()
            if sf == nil or sf.Coins < s.price then
                playSfx("Error")
                W.msg:setText(tr("broke"))
                return true
            end
            sf:SpendCoins(s.price)
            sf:SetGlobalTrigger(unlockTrigger(s), true)
            purse:pay(s.price)
            item.locked = false
            if W.list and W.list.items[selected] == item then showDesc(s) end
            askDone = true
            return true
        end }
    focusWidget(askUi, W.ask)
    mode = "ask"
end

local function buildList()
    disposeUi(ui)
    W = {}
    ui = PopUI.new{ theme = ctx.theme, navPlayer = ctx.playerIndex() + 1 }
    local x, y = (1920 - PANEL.w) / 2, (1080 - PANEL.h) / 2
    local panel = ui:panel{ x = x, y = y, w = PANEL.w, h = PANEL.h, pad = 30,
                            title = ctx.displayName and ctx.displayName() or M.ID }
    local cx, cy, cw, ch = panel:content()
    local items = {}
    for _, s in ipairs(loadScenes()) do items[#items + 1] = { text = s.name, value = s, locked = not isOpen(s) } end
    selected = math.max(1, math.min(selected, #items))
    W.list = ui:menu{ x = cx, y = cy + 10, w = LIST_W, h = ch - 10, rowHeight = ROW, items = items, selected = selected,
        lockedW = ROW_PRICE.w, drawLocked = drawRowPrice,
        onChange = function(i, it) selected = i; showDesc(it.value) end,
        onSelect = function(i, it) selected = i; chosen = it.value.stage; return true end,
        onLockedSelect = function(i, it) selected = i; askUnlock(it.value, it); return true end }
    local dx = cx + LIST_W + 40
    ui:label{ x = dx, y = cy + 14, text = tr("hint"), size = 22, color = { 120, 126, 150, 255 }, maxWidth = cw - LIST_W - 40 }
    W.desc = {}
    for i = 1, 5 do
        W.desc[i] = ui:label{ x = dx, y = cy + 70 + (i - 1) * DESC_LINE, text = "", size = DESC_SIZE,
                              maxWidth = cw - LIST_W - 40 }
    end
    -- the coin's left edge lines up with the description's first letter
    W.price = ui:label{ x = dx + 18 + COIN_GAP, y = cy + 70 + 5 * DESC_LINE + 20, text = "", size = PRICE_SIZE }
    local cur = W.list.items[selected]
    showDesc(cur and cur.value or nil)
    focusWidget(ui, W.list)
    mode = "list"
end

-- ── public API ─────────────────────────────────────────────────────────────────────────────────
-- ctx: { theme, playerIndex()->n, save()->save file|nil, room()->room, scene()->scene|nil,
--        propInstFor(it)->inst|nil, displayName()->string }
function M.init(c) ctx = c end

function M.isOpen() return mode ~= nil end

function M.open()
    if mode ~= nil or ctx == nil then return end
    chosen = nil
    justOpened = true
    purse = purse or CoinBox.new{ x = PURSE_X, y = PURSE_Y, w = PURSE_W, h = PURSE_H }
    local sf = ctx.save()
    purse:show(sf and sf.Coins or 0)
    buildList()
end

function M.close(silent)
    disposeUi(askUi); disposeUi(ui)
    ui, askUi, W = nil, nil, {}
    mode, askDone = nil, false
    if purse then purse:hide() end
    if not silent then playSfx("Cancel") end
end

-- returns "closed" the frame the window shuts, or "go", stage once a scene is chosen
function M.update(dt, ts)
    if mode == nil then return nil end
    purse:update(dt)
    if justOpened then justOpened = false; return nil end
    if ui == nil then M.close(true); return "closed" end
    if askUi then
        if askUi:update(ts) == "cancel" and not askDone then playSfx("Cancel"); askDone = true end
        if askDone then closeAsk() end
        return nil
    end
    local r = ui:update(ts)
    if chosen then
        local stage = chosen
        chosen = nil
        M.close(true)
        return "go", stage
    end
    if r == "cancel" then M.close(); return "closed" end
    return nil
end

function M.draw()
    if mode == nil then return end
    if ui then ui:draw() end
    drawPrice(W.price)
    if askUi then
        askUi:rect(0, 0, 1920, 1080, 8, 10, 18, 120)
        askUi:draw()
        drawPrice(W.askPrice)
    end
    if purse then purse:draw() end
end

function M.dispose()
    M.close(true)
    if purse then purse:dispose(); purse = nil end
    lightDefs = {}
end

return M
