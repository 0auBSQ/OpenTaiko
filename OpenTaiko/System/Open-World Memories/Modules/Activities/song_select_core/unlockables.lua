---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local
-- unlockables.lua — Locked-song visuals and unlock interaction for song_select_core.
--
-- HiddenIndex values:
--   0 = DISPLAYED : Normal bar + lock icon at left edge (Textures/Unlockables/0/lock.png)
--   1 = GRAYED    : 1/bar.png + 1/lock.png, no audio preview, no level tag
--   2 = BLURED    : Same bar/lock as GRAYED + static.png noise overlay, no title, no level tag
--   3 = HIDDEN    : Already filtered out of the song list — no code needed here

local M = {}
local G

local HI_DISPLAYED = 0
local HI_GRAYED    = 1
local HI_BLURED    = 2

-- Flash animation: nil = white; non-nil = animated color (red → white)
local flashColor = nil

-- ── Init ──────────────────────────────────────────────────────────────────────

function M.init(g)
    G = g
    flashColor = nil
end

-- Call from Script.lua onStart() to load all unlock-related textures into G.bars.
function M.loadTextures()
    G.bars["lock_0"]   = TEXTURE:CreateTexture("Textures/Unlockables/0/lock.png")
    G.bars["bar_1"]    = TEXTURE:CreateTexture("Textures/Unlockables/1/bar.png")
    G.bars["lock_1"]   = TEXTURE:CreateTexture("Textures/Unlockables/1/lock.png")
    G.bars["static_2"] = TEXTURE:CreateTexture("Textures/Unlockables/2/static.png")
    G.bars["condsbox"] = TEXTURE:CreateTexture("Textures/Unlockables/condsbox.png")
end

-- ── Helpers ───────────────────────────────────────────────────────────────────

local function nodeHI(node)
    if node == nil or not node.IsSong or not node.IsLocked then return HI_DISPLAYED end
    return node.HiddenIndex
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

-- ── Conditions panel ──────────────────────────────────────────────────────────

-- Draw condsbox.png and the unlock condition text for the currently selected locked
-- song. Also applies the flash-red animation when the player just failed to unlock.
function M.drawCondsPanel()
    local ssn = G.songList:GetSelectedSongNode()
    if ssn == nil or not ssn.IsSong or not ssn.IsLocked then return end
    local cond = ssn.UnlockCondition
    if not cond.HasCondition then return end

    G.bars["condsbox"]:DrawAtAnchor(317, 572, "topleft")

    -- Get condition text; fall back to GetConditionMessage() if UnlockText is empty
    local condText = ssn.UnlockText
    if type(condText) ~= "string" or condText == "" then
        condText = cond:GetConditionMessage()
    end
    if condText == nil or condText == "" then return end

    local tx = G.text:GetText(condText, false, 806)
    if flashColor ~= nil then
        tx:SetColor(flashColor)
    end
    tx:Draw(350, 605)
    tx:SetColor(COLOR:CreateColorFromHex("ffffffff"))
end

-- ── Tick ──────────────────────────────────────────────────────────────────────

function M.tick()
end

-- ── Decide handler ────────────────────────────────────────────────────────────

-- Called when the player presses Decide on a locked song.
-- Opens confirm_dialog if the condition is met; otherwise flashes the condition text.
function M.onDecideLocked(player, node)
    local cond = node.UnlockCondition
    if cond:IsUnlockable(player) then
        local cd = G.act_inner["confirm_dialog"]
        if cd ~= nil and not cd.IsActive then
            cd:Activate(player, node)
        end
        return "confirmed"
    else
        -- Flash: condition text animates red → white over 0.5 s
        -- For range 0→255 in T seconds: interval = T / 255
        G.startCounter("unlock_flash", 0, 255, 1/510, "none", function(val)
            flashColor = COLOR:CreateColorFromARGB(255, 255, math.floor(val), math.floor(val))
        end, function()
            flashColor = nil
        end)
        return "flashed"
    end
end

-- ── Preview suppression ───────────────────────────────────────────────────────

-- Returns true when the currently selected node should suppress the audio preview
-- (GRAYED or BLURED locked songs — play empty.ogg instead).
function M.suppressPreview(node)
    if node == nil or not node.IsSong or not node.IsLocked then return false end
    local hi = node.HiddenIndex
    return hi == HI_GRAYED or hi == HI_BLURED
end

return M
