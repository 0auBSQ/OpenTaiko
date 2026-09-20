---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local
-- confirm_dialog — Song-unlock confirmation dialog.
--
-- activate(player, node)
--   player  : 0-based index of the player who pressed Decide
--   node    : the locked LuaSongNode
--
-- Modes:
--   "notP1"    — player != 0; shows "Only unlockable by Player 1"
--   "unlock"   — free unlock; shows "Unlock [title]?" + Yes/No
--   "purchase" — coin cost; shows "Purchase [title]?" + cost + Yes/No
--
-- On Yes: deduct coins (if any), unlock the song, then play the modal ROActivity (the coin payment
--         first when there was one, then the new song) and wait for it to finish before DEACTIVATE.

local NavInput = require("NavInput")

local _player    = 0
local _node      = nil
local _mode      = "unlock"
local _coinPrice = 0
local _coins     = 0

-- Set to true after unlock while waiting for the modal animation to complete
local _waitingForModal = false
local _modalQueue = {}       -- the modals still to show, each { player, rarity, type, info, secondary }

local text      = nil
local textLarge = nil
local bgtile    = nil

local SCREEN_W     = 1920
local SCREEN_H     = 1080
local TILE_OPACITY = 0.65

-- Rarity string → modal integer (matches HRarity.RarityToModalInt in character_shop)
local rarityToInt = { Poor=0, Common=0, Uncommon=1, Rare=2, Epic=3, Legendary=4, Mythical=4 }

-- ── Lifecycle ─────────────────────────────────────────────────────────────────

function onStart()
    text      = TEXT:CreateGlyphCached(24)
    textLarge = TEXT:CreateGlyphCached(36)
    bgtile    = TEXTURE:CreateTexture("Textures/BgTile.png")
end

function onDestroy()
    if text      then text:Dispose()      end
    if textLarge then textLarge:Dispose() end
    if bgtile    then bgtile:Dispose()    end
end

function activate(player, node)
    _player    = player
    _node      = node
    _coinPrice = 0
    _coins     = 0
    _waitingForModal = false

    if node ~= nil then
        local cond = node.UnlockCondition
        _coinPrice = cond:GetCoinPrice()
    end
    _coins = GetSaveFile(0).Coins

    if player ~= 0 then
        _mode = "notP1"
    elseif _coinPrice > 0 then
        _mode = "purchase"
    else
        _mode = "unlock"
    end
end

function deactivate()
    -- nothing to clean up
end

-- ── Draw ──────────────────────────────────────────────────────────────────────

local function drawOverlay()
    if bgtile == nil or bgtile.Width == 0 or bgtile.Height == 0 then return end
    local cols = math.ceil(SCREEN_W / bgtile.Width) + 1
    local rows = math.ceil(SCREEN_H / bgtile.Height) + 1
    bgtile:SetOpacity(TILE_OPACITY)
    for col = 0, cols do
        for row = 0, rows do
            bgtile:Draw(col * bgtile.Width, row * bgtile.Height)
        end
    end
    bgtile:SetOpacity(1)
end

local function drawLine(str, y, large, col)
    col = col or COLOR:CreateColorFromHex("ffffffff")
    local f = large and textLarge or text
    f:Draw(str, SCREEN_W / 2, y, col, nil, 1, 1, 1200, "center")
end

function draw()
    -- While waiting for the modal, delegate drawing entirely to it
    if _waitingForModal then
        local modal = ROACTIVITY:GetROActivity("modal")
        if modal ~= nil then modal:Draw() end
        return
    end

    drawOverlay()

    local white  = COLOR:CreateColorFromHex("ffffffff")
    local yellow = COLOR:CreateColorFromHex("fffff000")
    local gray   = COLOR:CreateColorFromHex("ffaaaaaa")
    local title  = _node and _node.Title or "???"

    if _mode == "notP1" then
        drawLine("Only unlockable by Player 1", SCREEN_H / 2 - 50, true,  white)
        drawLine("[Decide] Ok",                 SCREEN_H / 2 + 50, false, gray)

    elseif _mode == "unlock" then
        drawLine("Unlock \"" .. title .. "\"?", SCREEN_H / 2 - 70, true,  white)
        drawLine("[Decide] Yes    [Cancel] No",  SCREEN_H / 2 + 50, false, gray)

    elseif _mode == "purchase" then
        local afterCoins = _coins - _coinPrice
        drawLine("Purchase \"" .. title .. "\"?",                   SCREEN_H / 2 - 100, true,  white)
        drawLine("Cost: " .. _coinPrice .. " coins",                SCREEN_H / 2 - 10,  false, yellow)
        drawLine("Your coins: " .. _coins .. "  →  " .. afterCoins, SCREEN_H / 2 + 40,  false, white)
        if afterCoins < 0 then
            drawLine("Not enough coins!",                           SCREEN_H / 2 + 80,  false, COLOR:CreateColorFromHex("ffff4444"))
        else
            drawLine("[Decide] Yes    [Cancel] No",                 SCREEN_H / 2 + 80,  false, gray)
        end
    end
end

-- ── Update ────────────────────────────────────────────────────────────────────

local function pressedDecide()
    return NavInput.p[_player + 1].decide()
end

local function pressedCancel()
    return NavInput.p[_player + 1].cancel()
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

local function doUnlock(paid)
    local sf = GetSaveFile(0)
    sf:UnlockSong(_node.UniqueId)
    _modalQueue = {}
    -- the coins leaving the piggy bank first, then the song
    if paid and paid > 0 then _modalQueue[#_modalQueue + 1] = { 0, 0, 0, -paid, sf.Coins } end
    _modalQueue[#_modalQueue + 1] = { 0, rarityToInt[_node.Rarity] or 0, 4, _node }
    nextModal()
end

function update()
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

    if _mode == "notP1" then
        if pressedDecide() or pressedCancel() then
            DEACTIVATE()
        end

    elseif _mode == "unlock" then
        if pressedCancel() then
            DEACTIVATE()
        elseif pressedDecide() then
            doUnlock()
        end

    elseif _mode == "purchase" then
        if pressedCancel() then
            DEACTIVATE()
        elseif pressedDecide() then
            local sf = GetSaveFile(0)
            if sf.Coins < _coinPrice then
                DEACTIVATE()
                return
            end
            sf:SpendCoins(_coinPrice)
            doUnlock(_coinPrice)
        end
    end
end
