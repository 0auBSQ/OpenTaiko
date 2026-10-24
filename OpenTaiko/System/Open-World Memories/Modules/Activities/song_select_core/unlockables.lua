---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local
-- unlockables.lua — Locked-song visuals and unlock interaction for song_select_core.
--
-- HiddenIndex values:
--   0 = DISPLAYED : Normal bar + lock icon at left edge (Textures/Unlockables/0/lock.png)
--   1 = GRAYED    : 1/bar.png + 1/lock.png, no audio preview, no level tag
--   2 = BLURED    : Same bar/lock as GRAYED + static.png noise overlay, no title, no level tag
--   3 = HIDDEN    : Already filtered out of the song list — no code needed here
--
-- Secret Vault songs are treated like BLURED but without static.png.
-- They use their own bar/lock textures from Textures/Unlockables/Vault/.
-- Detection: genre == "Secret Vault" AND save trigger ".vault_song_unlocked_<id>" is false.

local M = {}
local G

local HI_DISPLAYED = 0
local HI_GRAYED    = 1
local HI_BLURED    = 2

-- A locked song's condition sits on the jacket (centred at x 1526, 500 px wide, its bottom at y 646). A
-- coin-only condition: the price strip along the jacket's bottom. Any other: the same strip reading "How to
-- unlock" with [Decide], which opens into the details card, wider than the jacket and centred on it. Decide
-- on the open card closes it, or unlocks the song when its condition is met. The card stays open or closed
-- while browsing.
local JACKET_CX, JACKET_W   = 1526, 500
local STRIP_Y, STRIP_H      = 554, 92
local CARD_Y, CARD_W, CARD_H = 146, 600, 500
local CARD_PAD_X       = 30     -- the text's inset from the card's sides
local CARD_TITLE_Y     = 22     -- the title's top, from the card's top
local CARD_TEXT_Y      = 94     -- the condition text's top, from the card's top
local CARD_TEXT_H      = 380    -- a taller condition text is shrunk vertically to this height
local TITLE_SCALE_SHUT = 0.75   -- the title's size on the closed strip
local DETAILS_MS       = 220    -- the card opening or closing
local TEXT_FADE_FROM   = 0.6    -- the condition text fades in over the last part of the opening
local PRICE_COIN_X     = 52     -- the coin's centre, from the strip's left edge
local PRICE_COIN_SIZE  = 58
local PRICE_TEXT_X     = 96     -- the price's left edge
local STRIP_MSG_RIGHT  = 22     -- the right-hand text's margin
local PRICE_MSG_W      = 300    -- the message's widest box (longer ones are squished)
local COIN_CONDS = { ch = true, cs = true, cm = true }
local COL_PRICE       = COLOR:CreateColorFromARGB(255, 255, 226, 92)
local COL_PRICE_SHORT = COLOR:CreateColorFromARGB(255, 255, 110, 100)
local COL_KEY         = COLOR:CreateColorFromARGB(255, 190, 198, 216)

-- a missing key comes back as "[LOCALE NOT FOUND: KEY]": the fallback then; texts may start with "["
local function tr(key, fallback)
    local ok, s = pcall(function() return THEME:GetSkinString(key) end)
    if ok and type(s) == "string" and s ~= "" and s:sub(1, 18) ~= "[LOCALE NOT FOUND:" then return s end
    return fallback
end

-- Flash animation: nil = white; non-nil = animated color (red → white)
-- Shared by both standard locked songs and vault locked songs (mutually exclusive).
local flashColor = nil
local lastFlashVal = nil

-- Per-selection cache of the unlock-condition text. UnlockText/GetConditionMessage BUILD a new string on
-- every call (and each cross-boundary string marshal allocates), so querying them per frame steadily stacks
-- garbage while a locked song is selected. Fetch once per selected node instead.
local condCache = nil   -- { mode, text, scale } or { mode = "coin", player, price, short, text }
local cardTitle = nil
local decideLabel = nil

-- the details card: open or closed (the toggle), and its animation (0 = the strip, 1 = the card)
local detailsOpen = false
local detailsT = 0
local lastMs = nil
local panelRect = nil   -- { x, y, w, h } of the strip or card drawn this frame, for the mouse

-- ── Init ──────────────────────────────────────────────────────────────────────

function M.init(g)
    G = g
    flashColor = nil
    condCache = nil
    detailsOpen, detailsT, lastMs, panelRect = false, 0, nil, nil
end

-- drop the cached condition text (call on activity re-activation: unlock progress may have changed)
function M.invalidateCondCache()
    condCache = nil
end

-- Call from Script.lua onStart() to load all unlock-related textures into G.bars.
function M.loadTextures()
    G.bars["lock_0"]      = TEXTURE:CreateTexture("Textures/Unlockables/0/lock.png")
    G.bars["bar_1"]       = TEXTURE:CreateTexture("Textures/Unlockables/1/bar.png")
    G.bars["lock_1"]      = TEXTURE:CreateTexture("Textures/Unlockables/1/lock.png")
    G.bars["static_2"]    = TEXTURE:CreateTexture("Textures/Unlockables/2/static.png")
    G.bars["condsbox"]    = TEXTURE:CreateTexture("Textures/Unlockables/condsbox.png")
    G.bars["unlock_card"] = TEXTURE:CreateTexture("Textures/Unlockables/card.png")
    pcall(function() G.unlockCoin = SHARED:GetSharedTexture("Coin") end)
    -- Secret Vault textures
    G.bars["vault_bar"]   = TEXTURE:CreateTexture("Textures/Unlockables/Vault/bar.png")
    G.bars["vault_lock0"] = TEXTURE:CreateTexture("Textures/Unlockables/Vault/lock0.png")
    G.bars["vault_lock1"] = TEXTURE:CreateTexture("Textures/Unlockables/Vault/lock1.png")
    G.bars["vault_lock2"] = TEXTURE:CreateTexture("Textures/Unlockables/Vault/lock2.png")
    G.bars["vault_lockF"] = TEXTURE:CreateTexture("Textures/Unlockables/Vault/lockF.png")
end

-- ── Helpers ───────────────────────────────────────────────────────────────────

local function nodeHI(node)
    if node == nil or not node.IsSong or not node.IsLocked then return HI_DISPLAYED end
    return node.HiddenIndex
end

-- ── Secret Vault ──────────────────────────────────────────────────────────────

-- Returns true when the node is a Secret Vault song that should be treated as locked:
-- either the vault itself has not been opened, or this song's individual trigger is not set.
function M.isVaultLocked(node)
    if node == nil or not node.IsSong then return false end
    if node.Genre ~= "Secret Vault" then return false end
    local sf = GetSaveFile(0)
    if not sf:GetGlobalTrigger(".vault_opened") then return true end
    return not sf:GetGlobalTrigger(".vault_song_unlocked_" .. (node.UniqueId or ""))
end

-- Returns true when the node is a Secret Vault folder and the vault has not been opened yet.
function M.isVaultFolder(node)
    if node == nil or not node.IsFolder then return false end
    if node.Genre ~= "Secret Vault" then return false end
    return not GetSaveFile(0):GetGlobalTrigger(".vault_opened")
end

-- Draw lockF.png centered over the folder bar.
function M.drawVaultFolderLock(node, xpos, ypos)
    if G.bars["vault_lockF"] then
        G.bars["vault_lockF"]:DrawAtAnchor(xpos, ypos, "center")
    end
end

-- Effective hidden index for vault songs (treated as BLURED for sort/search purposes).
-- Returns the node's real HiddenIndex for standard locked songs, or HI_BLURED for vault locked.
function M.effectiveHiddenIndex(node)
    if M.isVaultLocked(node) then return HI_BLURED end
    if node == nil or not node.IsSong or not node.IsLocked then return HI_DISPLAYED end
    return node.HiddenIndex
end

-- Vault lock icon key based on the highest available difficulty level.
local function vaultLockKey(node)
    local c = nil
    for d = 4, 0, -1 do
        c = node:GetChart(d)
        if c ~= nil then break end
    end
    local lvl = c and c.Level or 0
    if lvl >= 3 then return "vault_lock2"
    elseif lvl >= 2 then return "vault_lock1"
    else return "vault_lock0" end
end

-- Draw the Vault bar texture. Always overrides the normal bar.
function M.drawVaultBar(node, xpos, ypos)
    G.bars["vault_bar"]:DrawAtAnchor(xpos, ypos, "center")
end

-- Draw the Vault lock icon (level-dependent) at the left edge of the bar.
function M.drawVaultLockIcon(node, xpos, ypos)
    local key = vaultLockKey(node)
    local lx  = xpos - G.bars["bar"].Width / 2
    if G.bars[key] then
        G.bars[key]:DrawAtAnchor(lx, ypos, "left")
    end
end

-- ── Bar drawing ───────────────────────────────────────────────────────────────

-- Draw the locked-specific bar texture (GRAYED/BLURED both use bar_1). Returns true if overridden.
-- For DISPLAYED locked songs, returns false so the caller draws the normal bar.
function M.drawLockedBar(node, xpos, ypos)
    if not node.IsLocked then return false end
    local hi = nodeHI(node)
    if hi == HI_GRAYED or hi == HI_BLURED then
        G.bars["bar_1"]:DrawAtAnchor(xpos, ypos, "center")
        return true
    end
    return false  -- DISPLAYED: caller draws normal bar.png
end

-- Draw the lock icon at the left edge of the bar (Y = bar center).
-- DISPLAYED uses lock_0; GRAYED and BLURED both use lock_1.
function M.drawLockIcon(node, xpos, ypos)
    if not node.IsLocked then return end
    local hi  = nodeHI(node)
    local key = (hi == HI_GRAYED or hi == HI_BLURED) and "lock_1" or "lock_0"
    local lx  = xpos - G.bars["bar"].Width / 2  -- left edge of bar.png
    if G.bars[key] then
        G.bars[key]:DrawAtAnchor(lx, ypos, "left")
    end
end

-- Draw the static noise GL effect for BLURED songs, at the same position as the bar.
-- xpos, ypos = bar center (same coords passed to bar:DrawAtAnchor with "center").
function M.drawBluredStatic(xpos, ypos)
    local st = G.bars["static_2"]
    if st == nil or st.Width == 0 or st.Height == 0 then return end
    st:SetUseNoiseEffect(true)
    st:DrawAtAnchor(xpos, ypos, "center")
    st:SetUseNoiseEffect(false)
end

-- ── Conditions panels ─────────────────────────────────────────────────────────

-- unlock text keeps the original behavior: no horizontal wrap — the width squishes to the box (806) while
-- the HEIGHT shrinks independently (scaleY) when the newline-stacked block exceeds the 380px text area
local function drawCondText(condText, scale)
    if condText == nil or condText == "" then return end
    G.text:Draw(condText, 350, 605, flashColor, nil, 1, 1, 806, "topleft", scale or 1)
end

-- Rebuilt only when the cache is invalid (selection moved / activity re-activated / unlock attempted):
-- classifies the selected node once and precomputes the text + shrink scale, so the per-frame draw does
-- ZERO cross-boundary accessor calls (each NLua property read allocates; per-frame reads stack garbage).
local function refreshCondCache()
    condCache = { mode = nil }
    local ssn = G.songList:GetSelectedSongNode()
    if ssn == nil then return end
    if ssn.IsSong and ssn.IsLocked then
        local cond = ssn.UnlockCondition
        if not cond.HasCondition then return end
        -- a coin price is shown for the highlighted player: affordable or not
        local player = G.highlightedPlayer or 0
        local price = cond:GetCoinPrice()
        if COIN_CONDS[cond:GetConditionType()] and price > 0 then
            local ok = cond:IsUnlockable(player)
            condCache = { mode = "coin", player = player, price = tostring(price), short = not ok,
                text = ok and tr("SONGSELECT_UNLOCK_BUY", "[Decide] to buy") or cond:GetBlockedMessage(player) }
            return
        end
        local text = ssn.UnlockText
        if type(text) ~= "string" or text == "" then text = cond:GetConditionMessage() end
        if text == nil or text == "" then return end
        local h = G.text:MeasureWrapped(text, 0)
        condCache = { mode = "locked", text = text, scale = (h > CARD_TEXT_H) and (CARD_TEXT_H / h) or 1 }
        return
    end
    if M.isVaultLocked(ssn) then
        condCache = { mode = "vault" }
    elseif M.isVaultFolder(ssn) then
        condCache = { mode = "vaultFolder" }
    end
end

local function smooth(t) return t * t * (3 - 2 * t) end
local function lerp(a, b, t) return a + (b - a) * t end

-- the strip or the card: the card texture (a flat fill) cut to the size
local function drawFill(x, y, w, h)
    G.bars["unlock_card"]:DrawRectAtAnchor(x, y, 0, 0, w, h, "topleft")
    panelRect = { x = x, y = y, w = w, h = h }
end

-- The selected locked song's condition: the price strip for a coin-only condition, else the strip that opens
-- into the details card. Its texts take the flash-red animation when the player just failed to unlock.
function M.drawCondsPanel()
    if condCache == nil or (condCache.player ~= nil and condCache.player ~= G.highlightedPlayer) then
        refreshCondCache()
    end
    local now = G.nowMs or 0
    local dt = (lastMs ~= nil) and math.max(0, math.min(100, now - lastMs)) or 0
    lastMs = now
    panelRect = nil
    local shift = G.songSelectShift or 0
    if condCache.mode == "coin" then
        local x, cy = JACKET_CX - JACKET_W / 2 - shift, STRIP_Y + STRIP_H / 2
        drawFill(x, STRIP_Y, JACKET_W, STRIP_H)
        local coin = G.unlockCoin
        if coin ~= nil and coin.Width > 0 then
            local k = PRICE_COIN_SIZE / coin.Width
            coin:SetScale(k, k)
            coin:DrawAtAnchor(x + PRICE_COIN_X, cy, "center")
            coin:SetScale(1, 1)
        end
        local big, small = G.textLarge, G.text
        local bigPad = big.BoxHeight - math.ceil(big.LineHeight)
        local smallPad = small.BoxHeight - math.ceil(small.LineHeight)
        big:Draw(condCache.price, x + PRICE_TEXT_X - bigPad, cy - big.LineHeight / 2,
            flashColor or (condCache.short and COL_PRICE_SHORT or COL_PRICE), nil, 1, 1, 0, "topleft")
        small:Draw(condCache.text, x + JACKET_W - STRIP_MSG_RIGHT + smallPad, cy - small.LineHeight / 2,
            flashColor, nil, 1, 1, PRICE_MSG_W, "topright")
    elseif condCache.mode == "locked" then
        local target = detailsOpen and 1 or 0
        if detailsT < target then detailsT = math.min(target, detailsT + dt / DETAILS_MS)
        elseif detailsT > target then detailsT = math.max(target, detailsT - dt / DETAILS_MS) end
        local e = smooth(detailsT)
        -- the strip on the jacket's bottom grows into the card, its bottom edge rising to the card's
        local w = lerp(JACKET_W, CARD_W, e)
        local y = lerp(STRIP_Y, CARD_Y, e)
        local h = lerp(STRIP_H, CARD_H, e)
        local x = JACKET_CX - w / 2 - shift
        drawFill(math.floor(x + 0.5), math.floor(y + 0.5), math.floor(w + 0.5), math.floor(h + 0.5))
        local big, small = G.textLarge, G.text
        local smallPad = small.BoxHeight - math.ceil(small.LineHeight)
        -- the title: centred on the strip, then at the card's top, growing to its full size
        cardTitle = cardTitle or tr("SONGSELECT_UNLOCK_TITLE", "How to unlock")
        local ts = lerp(TITLE_SCALE_SHUT, 1, e)
        local ty = lerp(STRIP_Y + STRIP_H / 2 - big.LineHeight * ts / 2, CARD_Y + CARD_TITLE_Y, e)
        big:Draw(cardTitle, x + w / 2, ty, nil, nil, 1, ts, w - 160, "top")
        -- [Decide]: the strip's right end, then the card's bottom-right corner
        decideLabel = decideLabel or tr("SONGSELECT_DECIDE_KEY", "[Decide]")
        small:Draw(decideLabel, x + w - STRIP_MSG_RIGHT + smallPad, y + h - STRIP_H / 2 - small.LineHeight / 2,
            COL_KEY, nil, 1, 1, 0, "topright")
        local fade = (e - TEXT_FADE_FROM) / (1 - TEXT_FADE_FROM)
        if fade > 0 then
            small:Draw(condCache.text, x + CARD_PAD_X - smallPad, y + CARD_TEXT_Y, flashColor, nil, math.min(1, fade), 1,
                w - 2 * CARD_PAD_X + 2 * smallPad, "topleft", condCache.scale)
        end
    end
end

-- opens or closes the details card (a locked song with a condition other than a coin price only)
local function toggleDetails()
    detailsOpen = not detailsOpen
    G.sounds.Skip:Play()
end

-- a click on the strip or the card does what Decide does; true when the click was on it
function M.clickDetails(mx, my)
    local r = panelRect
    if r == nil or condCache == nil or condCache.mode ~= "locked" then return false end
    if mx < r.x or mx > r.x + r.w or my < r.y or my > r.y + r.h then return false end
    local node = G.songList:GetSelectedSongNode()
    if node == nil then return false end
    M.onDecideLocked(G.highlightedPlayer or 0, node)
    return true
end

-- Draw condsbox.png and the vault message for the currently selected vault-locked song or vault folder.
function M.drawVaultCondsPanel()
    if condCache == nil then refreshCondCache() end
    if condCache.mode == "vault" then
        G.bars["condsbox"]:DrawAtAnchor(317, 572, "topleft")
        drawCondText("Get this song in the Secret Vault menu", 1)
    elseif condCache.mode == "vaultFolder" then
        G.bars["condsbox"]:DrawAtAnchor(317, 572, "topleft")
        drawCondText("4 keys is what you need...", 1)
    end
end

-- ── Tick ──────────────────────────────────────────────────────────────────────

function M.tick()
end

-- ── Decide handlers ───────────────────────────────────────────────────────────

local function startFlash()
    lastFlashVal = nil
    G.startCounter("unlock_flash", 0, 255, 1/510, "none", function(val)
        val = math.floor(val)
        if val ~= lastFlashVal then
            lastFlashVal = val
            flashColor = COLOR:CreateColorFromARGB(255, 255, val, val)
        end
    end, function()
        flashColor = nil
    end)
end

-- Called when the player presses Decide on a standard locked song.
-- A details card: Decide opens it; on the open card it unlocks a met condition right away (confirm_dialog
-- plays the modal, the card is closed at once), else it closes the card.
-- A coin price: confirm_dialog's purchase panel when the player can pay, else the flash.
function M.onDecideLocked(player, node)
    local cond = node.UnlockCondition
    if condCache == nil then refreshCondCache() end
    if condCache.mode == "locked" then
        if detailsOpen and cond:IsUnlockable(player) then
            local cd = G.act_inner["confirm_dialog"]
            if cd ~= nil and not cd.IsActive then cd:Activate(player, node, "direct") end
            detailsOpen, detailsT = false, 0
            condCache = nil
            return "confirmed"
        end
        toggleDetails()
        return "details"
    end
    condCache = nil
    if cond:IsUnlockable(player) then
        local cd = G.act_inner["confirm_dialog"]
        if cd ~= nil and not cd.IsActive then
            cd:Activate(player, node)
        end
        return "confirmed"
    else
        G.sounds.Cancel:Play()
        startFlash()
        return "flashed"
    end
end

-- Called when the player presses Decide on a vault-locked song.
-- Always flashes the vault message (no purchase possible from here).
function M.onDecideVaultLocked(player, node)
    condCache = nil
    G.sounds.Cancel:Play()
    startFlash()
    return "flashed"
end

-- Called when the player presses Decide on a locked vault folder.
function M.onDecideVaultFolder(player, node)
    condCache = nil
    G.sounds.Cancel:Play()
    startFlash()
    return "flashed"
end

-- ── Preview suppression ───────────────────────────────────────────────────────

-- Returns true when the currently selected node should suppress the audio preview.
function M.suppressPreview(node)
    if node == nil or not node.IsSong then return false end
    if M.isVaultLocked(node) then return true end
    if not node.IsLocked then return false end
    local hi = node.HiddenIndex
    return hi == HI_GRAYED or hi == HI_BLURED
end

return M
