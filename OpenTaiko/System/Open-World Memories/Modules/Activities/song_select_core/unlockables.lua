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

-- Flash animation: nil = white; non-nil = animated color (red → white)
-- Shared by both standard locked songs and vault locked songs (mutually exclusive).
local flashColor = nil
local lastFlashVal = nil

-- Per-selection cache of the unlock-condition text. UnlockText/GetConditionMessage BUILD a new string on
-- every call (and each cross-boundary string marshal allocates), so querying them per frame steadily stacks
-- garbage while a locked song is selected. Fetch once per selected node instead.
local condCache = nil   -- { node, has, text, scale }

-- ── Init ──────────────────────────────────────────────────────────────────────

function M.init(g)
    G = g
    flashColor = nil
    condCache = nil
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
        local text = ssn.UnlockText
        if type(text) ~= "string" or text == "" then text = cond:GetConditionMessage() end
        if text == nil or text == "" then return end
        local h = G.text:MeasureWrapped(text, 0)
        condCache = { mode = "locked", text = text, scale = (h > 380) and (380 / h) or 1 }
        return
    end
    if M.isVaultLocked(ssn) then
        condCache = { mode = "vault" }
    elseif M.isVaultFolder(ssn) then
        condCache = { mode = "vaultFolder" }
    end
end

-- Draw condsbox.png and the unlock condition text for the currently selected locked
-- song. Also applies the flash-red animation when the player just failed to unlock.
function M.drawCondsPanel()
    if condCache == nil then refreshCondCache() end
    if condCache.mode ~= "locked" then return end
    G.bars["condsbox"]:DrawAtAnchor(317, 572, "topleft")
    drawCondText(condCache.text, condCache.scale)
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
-- Opens confirm_dialog if the condition is met; otherwise flashes the condition text.
function M.onDecideLocked(player, node)
    condCache = nil
    local cond = node.UnlockCondition
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
