---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local
-- locked-song visuals and unlock input for song_select_core

local Config    = require("Core.Config")
local Layout    = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")
local U         = Layout.unlock
local UAnim     = Animation.unlock

local M = {}
local G

-- HiddenIndex 0: normal bar + lock icon
-- 1: grayed bar and lock, no preview or level tag
-- 2: as 1 + static.png, no title; 3: filtered out of the list
-- locked vault songs: as 2 without static.png (Textures/Unlockables/Vault/)
local HI_DISPLAYED = 0
local HI_GRAYED    = 1
local HI_BLURED    = 2

-- condition panel on the jacket: coin-only shows the price strip
-- else a "How to unlock" strip; Decide opens the details card, Decide again closes or unlocks
-- the card state persists while browsing
local JACKET_CX, JACKET_W   = U.jacket_cx, U.jacket_w
local STRIP_Y, STRIP_H      = U.strip_y, U.strip_h
local CARD_Y, CARD_W, CARD_H = U.card_y, U.card_w, U.card_h
local CARD_PAD_X       = U.card_pad_x       -- text inset from the card sides
local CARD_TITLE_Y     = U.card_title_y     -- title top, from the card top
local CARD_TEXT_Y      = U.card_text_y      -- condition text top, from the card top
local CARD_TEXT_H      = U.card_text_h      -- taller text shrinks vertically to this
local CARD_TITLE_MARGIN = U.card_title_margin   -- title box = strip or card width minus this
local TITLE_SCALE_SHUT = U.title_scale_shut -- title scale on the closed strip
local DETAILS_MS       = UAnim.details_ms       -- card open / close
local TEXT_FADE_FROM   = UAnim.text_fade_from   -- text fades in over the last part of the opening
local PRICE_COIN_X     = U.price_coin_x     -- coin centre, from the strip left edge
local PRICE_COIN_SIZE  = U.price_coin_size
local PRICE_TEXT_X     = U.price_text_x     -- price left edge
local STRIP_MSG_RIGHT  = U.strip_msg_right  -- right-hand text margin
local PRICE_MSG_W      = U.price_msg_w      -- message max width (longer ones squished)
local VAULT_BOX, VAULT_TEXT = U.vault_box, U.vault_text
local COIN_CONDS = { ch = true, cs = true, cm = true }
local COL_PRICE       = Config.color(U.price_color)
local COL_PRICE_SHORT = Config.color(U.price_short_color)
local COL_KEY         = Config.color(U.key_color)

-- missing key returns "[LOCALE NOT FOUND: KEY]": fallback; texts may start with "["
local function tr(key, fallback)
    local ok, s = pcall(function() return THEME:GetSkinString(key) end)
    if ok and type(s) == "string" and s ~= "" and s:sub(1, 18) ~= "[LOCALE NOT FOUND:" then return s end
    return fallback
end

-- nil: white; else the flash colour (standard and vault locks)
local flashColor = nil
local lastFlashVal = nil

-- per-selection condition text; UnlockText / GetConditionMessage allocate on every call
local condCache = nil   -- { mode, text, scale } or { mode = "coin", player, price, short, text }
local cardTitle = nil
local decideLabel = nil

-- details card toggle and animation (0 strip, 1 card)
local detailsOpen = false
local detailsT = 0
local lastMs = nil
local panelRect = nil   -- { x, y, w, h } of the strip or card drawn this frame, for the mouse

-- ==== Init ====

function M.init(g)
    G = g
    flashColor = nil
    condCache = nil
    detailsOpen, detailsT, lastMs, panelRect = false, 0, nil, nil
end

-- call on re-activation: unlock progress may have changed
function M.invalidateCondCache()
    condCache = nil
end

-- from Script.lua onStart(); textures into G.bars
function M.loadTextures()
    G.bars["lock_0"]      = TEXTURE:CreateTexture("Textures/Unlockables/0/lock.png")
    G.bars["bar_1"]       = TEXTURE:CreateTexture("Textures/Unlockables/1/bar.png")
    G.bars["lock_1"]      = TEXTURE:CreateTexture("Textures/Unlockables/1/lock.png")
    G.bars["static_2"]    = TEXTURE:CreateTexture("Textures/Unlockables/2/static.png")
    G.bars["condsbox"]    = TEXTURE:CreateTexture("Textures/Unlockables/condsbox.png")
    G.bars["unlock_card"] = TEXTURE:CreateTexture("Textures/Unlockables/card.png")
    pcall(function() G.unlockCoin = SHARED:GetSharedTexture("Coin") end)
    -- Secret Vault
    G.bars["vault_bar"]   = TEXTURE:CreateTexture("Textures/Unlockables/Vault/bar.png")
    G.bars["vault_lock0"] = TEXTURE:CreateTexture("Textures/Unlockables/Vault/lock0.png")
    G.bars["vault_lock1"] = TEXTURE:CreateTexture("Textures/Unlockables/Vault/lock1.png")
    G.bars["vault_lock2"] = TEXTURE:CreateTexture("Textures/Unlockables/Vault/lock2.png")
    G.bars["vault_lockF"] = TEXTURE:CreateTexture("Textures/Unlockables/Vault/lockF.png")
end

-- ==== Helpers ====

local function nodeHI(node)
    if node == nil or not node.IsSong or not node.IsLocked then return HI_DISPLAYED end
    return node.HiddenIndex
end

-- ==== Secret Vault ====

-- vault song locked: vault not opened, or no .vault_song_unlocked_<id> trigger
function M.isVaultLocked(node)
    if node == nil or not node.IsSong then return false end
    if node.Genre ~= "Secret Vault" then return false end
    local sf = GetSaveFile(0)
    if not sf:GetGlobalTrigger(".vault_opened") then return true end
    return not sf:GetGlobalTrigger(".vault_song_unlocked_" .. (node.UniqueId or ""))
end

-- vault folder while the vault is unopened
function M.isVaultFolder(node)
    if node == nil or not node.IsFolder then return false end
    if node.Genre ~= "Secret Vault" then return false end
    return not GetSaveFile(0):GetGlobalTrigger(".vault_opened")
end

-- lockF.png centred over the folder bar
function M.drawVaultFolderLock(node, xpos, ypos)
    if G.bars["vault_lockF"] then
        G.bars["vault_lockF"]:DrawAtAnchor(xpos, ypos, "center")
    end
end

-- HI_BLURED for locked vault songs (sort / search), else the real HiddenIndex
function M.effectiveHiddenIndex(node)
    if M.isVaultLocked(node) then return HI_BLURED end
    if node == nil or not node.IsSong or not node.IsLocked then return HI_DISPLAYED end
    return node.HiddenIndex
end

-- lock icon key by the highest difficulty level
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

-- replaces the normal bar
function M.drawVaultBar(node, xpos, ypos)
    G.bars["vault_bar"]:DrawAtAnchor(xpos, ypos, "center")
end

-- at the bar's left edge
function M.drawVaultLockIcon(node, xpos, ypos)
    local key = vaultLockKey(node)
    local lx  = xpos - G.bars["bar"].Width / 2
    if G.bars[key] then
        G.bars[key]:DrawAtAnchor(lx, ypos, "left")
    end
end

-- ==== Bar drawing ====

-- GRAYED / BLURED: bar_1, returns true; DISPLAYED: false, caller draws bar.png
function M.drawLockedBar(node, xpos, ypos)
    if not node.IsLocked then return false end
    local hi = nodeHI(node)
    if hi == HI_GRAYED or hi == HI_BLURED then
        G.bars["bar_1"]:DrawAtAnchor(xpos, ypos, "center")
        return true
    end
    return false
end

-- at the bar's left edge, y = bar centre; DISPLAYED lock_0, else lock_1
function M.drawLockIcon(node, xpos, ypos)
    if not node.IsLocked then return end
    local hi  = nodeHI(node)
    local key = (hi == HI_GRAYED or hi == HI_BLURED) and "lock_1" or "lock_0"
    local lx  = xpos - G.bars["bar"].Width / 2  -- left edge of bar.png
    if G.bars[key] then
        G.bars[key]:DrawAtAnchor(lx, ypos, "left")
    end
end

-- BLURED static noise; xpos, ypos: bar centre
function M.drawBluredStatic(xpos, ypos)
    local st = G.bars["static_2"]
    if st == nil or st.Width == 0 or st.Height == 0 then return end
    st:SetUseNoiseEffect(true)
    st:DrawAtAnchor(xpos, ypos, "center")
    st:SetUseNoiseEffect(false)
end

-- ==== Condition panels ====

-- no wrap: width squished to vault_text.max_width, height scaled by scale
local function drawCondText(condText, scale)
    if condText == nil or condText == "" then return end
    G.text:Draw(condText, VAULT_TEXT.x, VAULT_TEXT.y, flashColor, nil, 1, 1, VAULT_TEXT.max_width, "topleft", scale or 1)
end

-- rebuilt when invalidated: per-frame draw makes no NLua property reads (each allocates)
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

-- strip or card: unlock_card cut to size
local function drawFill(x, y, w, h)
    G.bars["unlock_card"]:DrawRectAtAnchor(x, y, 0, 0, w, h, "topleft")
    panelRect = { x = x, y = y, w = w, h = h }
end

-- texts use flashColor after a failed unlock
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
        -- strip grows into the card
        local w = lerp(JACKET_W, CARD_W, e)
        local y = lerp(STRIP_Y, CARD_Y, e)
        local h = lerp(STRIP_H, CARD_H, e)
        local x = JACKET_CX - w / 2 - shift
        drawFill(math.floor(x + 0.5), math.floor(y + 0.5), math.floor(w + 0.5), math.floor(h + 0.5))
        local big, small = G.textLarge, G.text
        local smallPad = small.BoxHeight - math.ceil(small.LineHeight)
        -- title: strip centre to card top, growing to full size
        cardTitle = cardTitle or tr("SONGSELECT_UNLOCK_TITLE", "How to unlock")
        local ts = lerp(TITLE_SCALE_SHUT, 1, e)
        local ty = lerp(STRIP_Y + STRIP_H / 2 - big.LineHeight * ts / 2, CARD_Y + CARD_TITLE_Y, e)
        big:Draw(cardTitle, x + w / 2, ty, nil, nil, 1, ts, w - CARD_TITLE_MARGIN, "top")
        -- [Decide]: strip right end to card bottom-right
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

-- non-coin conditions only
local function toggleDetails()
    detailsOpen = not detailsOpen
    G.sounds.Skip:Play()
end

-- click acts as Decide; true when on the panel
function M.clickDetails(mx, my)
    local r = panelRect
    if r == nil or condCache == nil or condCache.mode ~= "locked" then return false end
    if mx < r.x or mx > r.x + r.w or my < r.y or my > r.y + r.h then return false end
    local node = G.songList:GetSelectedSongNode()
    if node == nil then return false end
    M.onDecideLocked(G.highlightedPlayer or 0, node)
    return true
end

-- condsbox.png and the vault message, for a locked vault song or folder
function M.drawVaultCondsPanel()
    if condCache == nil then refreshCondCache() end
    if condCache.mode == "vault" then
        G.bars["condsbox"]:DrawAtAnchor(VAULT_BOX.x, VAULT_BOX.y, "topleft")
        drawCondText(G.skinText("SONGSELECT_VAULT_SONG", "Get this song in the Secret Vault menu"), 1)
    elseif condCache.mode == "vaultFolder" then
        G.bars["condsbox"]:DrawAtAnchor(VAULT_BOX.x, VAULT_BOX.y, "topleft")
        drawCondText(G.skinText("SONGSELECT_VAULT_FOLDER", "4 keys is what you need..."), 1)
    end
end

-- ==== Tick ====

function M.tick()
end

-- ==== Decide handlers ====

local function startFlash()
    lastFlashVal = nil
    -- G and B 0..255 over flash_s
    G.startCounter("unlock_flash", 0, 255, UAnim.flash_s / 255, "none", function(val)
        val = math.floor(val)
        if val ~= lastFlashVal then
            lastFlashVal = val
            flashColor = COLOR:CreateColorFromARGB(255, 255, val, val)
        end
    end, function()
        flashColor = nil
    end)
end

-- Decide on a standard locked song
-- card: opens, then unlocks a met condition (confirm_dialog modal) or closes
-- coin price: confirm_dialog purchase panel, or the flash when unaffordable
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

-- vault-locked song: flash only, no purchase here
function M.onDecideVaultLocked(player, node)
    condCache = nil
    G.sounds.Cancel:Play()
    startFlash()
    return "flashed"
end

-- Decide on a locked vault folder
function M.onDecideVaultFolder(player, node)
    condCache = nil
    G.sounds.Cancel:Play()
    startFlash()
    return "flashed"
end

-- ==== Preview suppression ====

function M.suppressPreview(node)
    if node == nil or not node.IsSong then return false end
    if M.isVaultLocked(node) then return true end
    if not node.IsLocked then return false end
    local hi = node.HiddenIndex
    return hi == HI_GRAYED or hi == HI_BLURED
end

return M
