---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local
-- confirm_dialog — the song select's song unlocks, a PopUI panel over the list.
--
-- activate(player, node, mode)
--   player  : 0-based index of the player who pressed Decide
--   node    : the locked LuaSongNode
--   mode    : "direct" unlocks a met condition right away; otherwise a coin price opens the purchase panel
--
-- Panels:
--   "notP1"    — player != 0: only Player 1 unlocks songs; OK
--   "purchase" — the song's title, the coin purse with the price over it, Buy / Cancel side by side
--
-- Buy pays from the purse (the price drops in, the balance rolls down), then the song is unlocked and the
-- modal ROActivity shows it; the dialog closes when the modal is done.

local PopUI = require("PopUI")

local SW, SH = 1920, 1080
local PANEL_W = 760
local PANEL_H = { notP1 = 220, purchase = 440 }
local TITLE_Y = 96            -- the song title's centre, from the panel's top
local PURSE_Y = 210           -- the purse's top, from the panel's top (its price sits 26 px above it)
local PURSE_W, PURSE_H = 300, 66
local BUTTON_W, BUTTON_H, BUTTON_GAP = 280, 60, 40
local BUTTON_BOTTOM = 96      -- the buttons' top, from the panel's bottom
local PAY_WAIT = 1.2          -- seconds the purse's payment plays before the unlock modal
local INK = { 44, 56, 74 }
local INK_EDGE = { 255, 255, 255, 160 }

local UI_THEME = {
    colors = {
        surface  = { 250, 252, 255, 255 }, surface2 = { 230, 236, 244, 255 },
        primary  = { 96, 132, 180, 255 },  primary2 = { 72, 108, 156, 255 },
        accent   = { 126, 196, 200, 255 }, accent2  = { 96, 168, 176, 255 },
        outline  = { 70, 84, 104, 255 },   text = { 44, 56, 74, 255 }, textOnAccent = { 255, 255, 255, 255 },
    },
    font = { small = 18, label = 22, button = 22, title = 30 },
}

-- Rarity string → modal integer (matches HRarity.RarityToModalInt in C#)
local rarityToInt = { Poor=0, Common=0, Uncommon=1, Rare=2, Epic=3, Legendary=4, Mythical=4 }

local function tr(key, fallback)
    local ok, s = pcall(function() return THEME:GetSkinString(key) end)
    if ok and type(s) == "string" and s ~= "" and s:sub(1, 1) ~= "[" then return s end
    return fallback
end

local ui = nil
local sounds = {}
local panel = { x = 0, y = 0, w = PANEL_W, h = 0 }
local buttons = {}
local purse = nil

local _player    = 0
local _node      = nil
local _title     = ""
local _mode      = "purchase"
local _coinPrice = 0
local wantClose, justOpened = false, false
local paying = nil            -- seconds left of the payment, nil when not paying
local lastTs = nil

-- Set to true after unlock while waiting for the modal animation to complete
local _waitingForModal = false
local _modalQueue = {}       -- the modals still to show, each { player, rarity, type, info, secondary }

-- ── Lifecycle ─────────────────────────────────────────────────────────────────

function onStart()
    sounds.Decide = SHARED:GetSharedSound("Decide")
    sounds.Cancel = SHARED:GetSharedSound("Cancel")
    sounds.Skip   = SHARED:GetSharedSound("Skip")
end

local function disposeUI()
    if ui ~= nil then ui:disposeWidgets(); ui = nil end
    buttons, purse = {}, nil
end

function onDestroy()
    disposeUI()
end

-- shows the next queued modal; without one (or without the modal activity) the dialog closes
local function nextModal()
    local modal = ROACTIVITY:GetROActivity("modal")
    local m = table.remove(_modalQueue, 1)
    if modal == nil or m == nil then
        _modalQueue = {}
        _waitingForModal = false
        DEACTIVATE()
        return
    end
    modal:Activate(m[1], m[2], m[3], m[4], m[5])
    _waitingForModal = true
end

local function doUnlock()
    GetSaveFile(0):UnlockSong(_node.UniqueId)
    disposeUI()
    _modalQueue = { { 0, rarityToInt[_node.Rarity] or 0, 4, _node } }
    nextModal()
end

local function buy()
    if paying ~= nil then return end
    local sf = GetSaveFile(0)
    if sf.Coins < _coinPrice then
        SHARED:GetSharedSound("Error"):Play()
        wantClose = true
        return
    end
    sf:SpendCoins(_coinPrice)
    for _, b in ipairs(buttons) do b.visible = false end
    if purse ~= nil then purse:setPrice(nil); purse:pay(_coinPrice) end
    paying = PAY_WAIT
end

-- the buttons sit in one row: Left / Right move between them, Up / Down do nothing
local function rowNav()
    for i, b in ipairs(buttons) do
        local function go(d)
            local n = buttons[i + d]
            if n ~= nil and ui:focus(n) then sounds.Skip:Play() end
            return true
        end
        b.onNavLeft = function() return go(-1) end
        b.onNavRight = function() return go(1) end
        b.onNavUp = function() return true end
        b.onNavDown = function() return true end
    end
end

local function build()
    panel.h = PANEL_H[_mode]
    panel.x, panel.y = math.floor((SW - panel.w) / 2), math.floor((SH - panel.h) / 2)
    ui = PopUI.new{
        theme = UI_THEME, bg = false, navPlayer = _player + 1,
        sfx = { click = function() sounds.Decide:Play() end, hover = function() sounds.Skip:Play() end },
    }
    local title = (_mode == "notP1") and tr("SONGSELECT_UNLOCK_P1_ONLY", "Only Player 1 can unlock songs")
        or tr("SONGSELECT_BUY_TITLE", "Buy this song?")
    ui:panel{ x = panel.x, y = panel.y, w = panel.w, h = panel.h, title = title }
    local cx, by = panel.x + panel.w / 2, panel.y + panel.h - BUTTON_BOTTOM
    -- the main button first, so it holds the focus
    if _mode == "notP1" then
        buttons[1] = ui:button{ text = tr("SONGSELECT_CONFIRM_OK", "OK"), x = cx - BUTTON_W / 2, y = by, w = BUTTON_W, h = BUTTON_H,
                                accent = true, onClick = function() wantClose = true end }
    else
        buttons[1] = ui:button{ text = tr("SONGSELECT_BUY", "Buy"), x = cx - BUTTON_GAP / 2 - BUTTON_W, y = by, w = BUTTON_W, h = BUTTON_H,
                                accent = true, onClick = buy, sfx = { click = "" } }
        buttons[2] = ui:button{ text = tr("SONGSELECT_CONFIRM_CANCEL", "Cancel"), x = cx + BUTTON_GAP / 2, y = by, w = BUTTON_W, h = BUTTON_H,
                                onClick = function() sounds.Cancel:Play(); wantClose = true end, sfx = { click = "" } }
        purse = ui:coins{ x = cx - PURSE_W / 2, y = panel.y + PURSE_Y, w = PURSE_W, h = PURSE_H, amount = GetSaveFile(0).Coins }
        purse:setPrice(_coinPrice)
    end
    rowNav()
end

function activate(player, node, mode)
    _player    = player or 0
    _node      = node
    _title     = (node ~= nil and node.Title) or "???"
    _coinPrice = (node ~= nil) and node.UnlockCondition:GetCoinPrice() or 0
    _waitingForModal = false
    _modalQueue = {}
    wantClose, justOpened, paying, lastTs = false, true, nil, nil
    disposeUI()

    if _player ~= 0 then
        _mode = "notP1"
    elseif mode == "direct" then
        -- a met condition: no panel, the song unlocks and its modal plays
        doUnlock()
        return
    else
        _mode = "purchase"
    end
    build()
end

function deactivate()
    disposeUI()
end

-- ── Draw ──────────────────────────────────────────────────────────────────────

function draw()
    -- While waiting for the modal, delegate drawing entirely to it
    if _waitingForModal then
        local modal = ROACTIVITY:GetROActivity("modal")
        if modal ~= nil then modal:Draw() end
        return
    end
    if ui == nil then return end
    ui:rect(0, 0, SW, SH, 8, 10, 18, 150)
    ui:draw()
    if _mode == "purchase" then
        ui:drawTextEx(30, _title, panel.x + panel.w / 2, panel.y + TITLE_Y + ui:textNudge(30), INK, INK_EDGE, 1, 1,
            panel.w - 80, "center")
    end
end

-- ── Update ────────────────────────────────────────────────────────────────────

function update(ts)
    -- While waiting for a modal, update it and move on (or close) when it finishes
    if _waitingForModal then
        local modal = ROACTIVITY:GetROActivity("modal")
        if modal == nil or not modal.IsActive then
            nextModal()
        else
            modal:Update()
        end
        return
    end
    if ui == nil then return end
    local dt = (lastTs ~= nil and ts ~= nil) and math.max(0, math.min(0.1, (ts - lastTs) / 1000)) or 0
    lastTs = ts
    if justOpened then justOpened = false; return end   -- the press that opened the dialog never reaches it
    -- while paying, only the purse moves: no input reaches the buttons
    if paying ~= nil then
        if purse ~= nil then purse:update({ dt = dt }) end
        paying = paying - dt
        if paying <= 0 then paying = nil; doUnlock() end
        return
    end
    local r = ui:update(ts)
    if r == "cancel" then sounds.Cancel:Play(); wantClose = true end
    if wantClose then DEACTIVATE() end
end
