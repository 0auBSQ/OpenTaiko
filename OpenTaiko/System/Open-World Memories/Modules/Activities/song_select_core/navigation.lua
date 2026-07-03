---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local
-- navigation.lua  —  Page management, media loading, hold-scroll and song-select
--                    input handling for song_select_core.

local M = {}
local G   -- shared state injected by Script.lua

local PREVIEW_THROTTLE_MS  = 200
local BOX_SCROLL_SECONDS   = 0.06
local HOLD_DELAY_SECONDS   = 0.16
local HOLD_REPEAT_SECONDS  = 0.06

-- ── Media loaders ─────────────────────────────────────────────────────────────

local function reloadPreimage(songNode)
    SHARED:ClearSharedTexture("preimage")
    if songNode.IsSong == true then
        G.startCounter("throttle_preimage", 0, PREVIEW_THROTTLE_MS, 0.2/PREVIEW_THROTTLE_MS, "none", nil, function()
            if songNode.HasPreimage then
                SHARED:SetSharedTextureUsingAbsolutePath("preimage", songNode.PreimagePath)
            else
                SHARED:SetSharedTexture("preimage", "Textures/preimage.png")
            end
        end)
    else
        G.ctx["throttle_preimage"] = COUNTER:EmptyCounter()
    end
    SHARED:GetSharedTexture("preimage"):SetWrapMode("Border")
end

local function playPreview(songNode)
    G.previewLoaded         = false
    G.previewDurationMs     = 0
    G.previewDemoStartRaw   = 0
    G.previewDemoStart      = 0
    G.previewLoopCooldown   = false
    SHARED:SetSharedPreview("presound", "Sounds/empty.ogg")
    if songNode.IsSong == true then
        -- Suppress audio preview for GRAYED/BLURED locked songs
        if G.unlocks ~= nil and G.unlocks.suppressPreview(songNode) then
            G.ctx["throttle_presound"] = COUNTER:EmptyCounter()
            return
        end
        G.startCounter("throttle_presound", 0, PREVIEW_THROTTLE_MS, 0.2/PREVIEW_THROTTLE_MS, "none", nil, function()
            local demoStart = songNode.DemoStart
            SHARED:SetSharedPreviewUsingAbsolutePath("presound", songNode.AudioPath, function(snd)
                local speed = CONFIG.SongSpeed / 20
                snd:SetSpeed(speed)
                snd:Play()
                snd:SetTimestamp(math.floor(demoStart / speed))
                G.previewDurationMs   = snd:GetDurationMs()
                G.previewDemoStartRaw = demoStart
                G.previewDemoStart    = math.floor(demoStart / speed)
                G.previewLoaded       = true
            end)
        end)
    else
        G.ctx["throttle_presound"] = COUNTER:EmptyCounter()
    end
end

function M.init(g)
    G = g
    -- Expose media reload for any module that needs it via G
    G.reloadMediaForNode = function(node)
        reloadPreimage(node)
        playPreview(node)
    end
    G.navRefreshPage = M.refreshPage
end

-- ── Page refresh ──────────────────────────────────────────────────────────────
-- All node-derived draw data is extracted HERE, once per selection change: every NLua property read
-- (node.IsSong, chart:GetPlayerBestScore, …) allocates on each call, so per-frame reads in the draw loop
-- stack garbage and stutter the GC. The draw code consumes only these plain-Lua caches.

local function focusChart(songNode)
    if songNode.IsSong ~= true then return nil end
    local default = math.min(4, CONFIG:GetDefaultCourse(0))
    local chart   = songNode:GetChart(default)
    local i = 4
    while chart == nil and i >= 0 do chart = songNode:GetChart(i); i = i - 1 end
    return chart
end

-- vault lock icon key from the highest available difficulty's level
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

local function buildSlot(node, isSelected)
    local slot = { text = node.Title, gold = isSelected }
    slot.isSong, slot.isFolder = node.IsSong == true, node.IsFolder == true
    slot.isRandom, slot.isReturn = node.IsRandom == true, node.IsReturn == true
    slot.genre = node.Genre
    if slot.isSong or slot.isFolder then
        slot.boxColor    = node.BoxColor
        slot.vaultLocked = slot.isSong and G.unlocks.isVaultLocked(node) or false
        slot.vaultFolder = slot.isFolder and G.unlocks.isVaultFolder(node) or false
        slot.isLocked    = slot.isSong and node.IsLocked == true or false
        slot.hi          = slot.isSong and G.unlocks.effectiveHiddenIndex(node) or 0
        if slot.vaultLocked then slot.vaultLockKey = vaultLockKey(node) end
        if slot.isLocked then
            slot.lockedBarOverride = slot.hi >= 1                       -- GRAYED/BLURED use bar_1
            slot.lockKey = slot.hi >= 1 and "lock_1" or "lock_0"
        end
        if slot.isSong then
            local saveId = GetSaveFile(G.highlightedPlayer).SaveId
            slot.fav = node.UniqueId ~= nil and G.favs ~= nil and G.favs.isFavorite(saveId, node.UniqueId) or false
            local chart = focusChart(node)
            if chart ~= nil then
                slot.level = { lv = chart.Level, diff = chart.DifficultyAsInt,
                               isPlus = chart.IsPlus == true, isVault = slot.genre == "Secret Vault" }
                local info = chart:GetPlayerBestScore(G.highlightedPlayer)
                slot.barleft = { played = info.HasBeenPlayed, cs = info.ClearStatus, sr = info.ScoreRank }
            end
        end
    end
    return slot
end

-- selected-node info for the right panel / breadcrumb / preimage / selected-bar visuals
local function buildSelInfo(node)
    local sel = {}
    sel.isSong, sel.isRandom = node.IsSong == true, node.IsRandom == true
    sel.crumbs = {}
    local cur = node.Parent
    while cur ~= nil do
        sel.crumbs[#sel.crumbs + 1] = cur.Title ~= nil and cur.Title or "/"
        cur = cur.Parent
    end
    if not sel.isSong then return sel end
    sel.hi = G.unlocks.effectiveHiddenIndex(node)
    sel.hasVideo, sel.explicit = node.HasVideo == true, node.Explicit == true
    sel.isVault  = node.Genre == "Secret Vault"
    sel.subtitle = node.Subtitle
    sel.charter  = "Chart - " .. node.Maker
    sel.diffs = {}
    for i = 0, 4 do
        local c = node:GetChart(i)
        if c ~= nil then sel.diffs[i] = { level = c.Level, isPlus = c.IsPlus == true } end
    end
    local fc = focusChart(node)
    if fc ~= nil then sel.bpmBase, sel.bpmMin, sel.bpmMax = fc.BaseBPM, fc.MinBPM, fc.MaxBPM end
    sel.isUnlockedSong = node.IsLocked ~= true and not G.unlocks.isVaultLocked(node)
    return sel
end

function M.refreshPage(skipMedia)
    G.currentPage = {}
    G.pageTexts   = {}
    G.selInfo     = nil
    G.unlocks.invalidateCondCache()   -- selection changed: recompute the unlock-condition panel cache

    for i = -5, 5 do
        local node = G.songList:GetSongNodeAtOffset(i)
        G.currentPage[i] = node
        if node == nil then
            G.pageTexts[i] = nil
        else
            G.pageTexts[i] = buildSlot(node, i == 0)
            if i == 0 then G.selInfo = buildSelInfo(node) end
            if G.genre_overlays[node.Genre] == nil then
                if TEXTURE:Exists("Textures/Overlay/"..node.Genre..".png") then
                    G.genre_overlays[node.Genre] = TEXTURE:CreateTexture("Textures/Overlay/"..node.Genre..".png")
                else
                    G.genre_overlays[node.Genre] = TEXTURE:CreateTexture()
                end
            end
        end

        if i == 0 and node ~= nil and not skipMedia then
            reloadPreimage(node)
            playPreview(node)
        end
    end

    if G.ctx["extreme_fade"] then G.ctx["extreme_fade"]:Start() end
end

-- ── Folder navigation ─────────────────────────────────────────────────────────

local function handleDecideSongSelect(Sort)
    local ssn = G.songList:GetSelectedSongNode()

    if ssn.IsFolder == true then
        -- Block opening a locked vault folder (same feedback as a locked song)
        if G.unlocks ~= nil and G.unlocks.isVaultFolder(ssn) then
            G.unlocks.onDecideVaultFolder(G.highlightedPlayer, ssn)
            return nil
        end
        local success = G.songList:OpenFolder()
        if success then Sort.applySort(); G.sounds.Decide:Play() end
        M.refreshPage()
    elseif ssn.IsReturn == true then
        local success = G.songList:CloseFolder()
        if success then Sort.applySort(); G.sounds.Cancel:Play() end
        M.refreshPage()
    elseif ssn.IsSong == true then
        if G.unlocks ~= nil and G.unlocks.isVaultLocked(ssn) then
            G.unlocks.onDecideVaultLocked(G.highlightedPlayer, ssn)
            return nil
        end
        if ssn.IsLocked and G.unlocks ~= nil then
            G.unlocks.onDecideLocked(G.highlightedPlayer, ssn)
            return nil
        end
        G.sounds.SongDecide:Play()
        return ssn
    elseif ssn.IsRandom == true then
        local rdNd = G.songList:GetRandomNodeInFolder(ssn, true, function(node)
            return G.unlocks == nil or not G.unlocks.isVaultLocked(node)
        end)
        if rdNd ~= nil then G.sounds.SongDecide:Play(); return rdNd end
    end
    return nil
end

local function handleFolderClose()
    if G.songList == nil then return false end
    return G.songList:CloseFolder()
end

-- ── Hold scroll ───────────────────────────────────────────────────────────────

local startRepeat

local function stopHold()
    G.holdDir = 0
    G.ctx["hold_delay"]  = COUNTER:EmptyCounter()
    G.ctx["hold_repeat"] = COUNTER:EmptyCounter()
    startRepeat = function() end
end

local function doMove(dir)
    G.sounds.Skip:Play()
    G.songList:Move(dir)
    M.refreshPage()
    G.startCounter("scroll_box_anim", dir, 0, dir > 0 and -BOX_SCROLL_SECONDS or BOX_SCROLL_SECONDS, "none", function(val)
        G.selectBoxDist = val
    end)
end

local function startHold(dir)
    G.holdDir = dir
    G.ctx["hold_delay"]  = COUNTER:EmptyCounter()
    G.ctx["hold_repeat"] = COUNTER:EmptyCounter()
    startRepeat = function()
        G.startCounter("hold_repeat", 0, 1, HOLD_REPEAT_SECONDS, "none", nil, function()
            if G.holdDir ~= 0 then doMove(G.holdDir); startRepeat() end
        end)
    end
    G.startCounter("hold_delay", 0, 1, HOLD_DELAY_SECONDS, "none", nil, function()
        if G.holdDir ~= 0 then startRepeat() end
    end)
end

-- ── Song-select input handler ─────────────────────────────────────────────────
-- Returns "cancel", or nil.  Sets G.selectedSongNode and starts screen_transition
-- when a song is chosen.

function M.handleSongSelectInput(Sort, Diff)
    G.selectedSongNode = nil

    -- Debug / dev shortcuts
    if INPUT:KeyboardPressed("S") then
        G.sounds.Skip:Play()
        CONFIG:SetDefaultCourse(0, (CONFIG:GetDefaultCourse(0) + 1) % 5)
        Sort.applySort(); M.refreshPage(true)
    end
    if INPUT:KeyboardPressed("P") and not G.activeConfig.mountAISlotToP2 then
        G.sounds.Skip:Play()
        local prev = G.highlightedPlayer
        G.highlightedPlayer = (G.highlightedPlayer + 1) % CONFIG.PlayerCount
        if G.highlightedPlayer ~= prev then Sort.applySort(); M.refreshPage(true) end
    end
    if not G.activeConfig.songOnly then   -- online lobby (songOnly): Auto cannot be toggled in song select
        if INPUT:KeyboardPressed("F3") then
            G.sounds.Decide:Play(); CONFIG:SetAutoStatus(0, not CONFIG:GetAutoStatus(0))
        end
        if INPUT:KeyboardPressed("F4") and CONFIG.PlayerCount >= 2 then
            G.sounds.Decide:Play(); CONFIG:SetAutoStatus(1, not CONFIG:GetAutoStatus(1))
        end
    end

    local inpset = G.inputSets[G.highlightedPlayer + 1]

    -- Release hold if direction key lifted
    if G.holdDir ==  1 and not INPUT:Pressing(inpset.right) and not INPUT:KeyboardPressing("RightArrow") then stopHold()
    elseif G.holdDir == -1 and not INPUT:Pressing(inpset.left)  and not INPUT:KeyboardPressing("LeftArrow")  then stopHold()
    end

    -- Main navigation
    if INPUT:KeyboardPressed("Space") then
        stopHold()
        local sd = G.act_inner["sort_search_dialog"]
        if sd ~= nil and not sd.IsActive then
            sd:Activate(G.highlightedPlayer)
        end
    elseif INPUT:KeyboardPressed("A") then
        stopHold()
        local sd = G.act_inner["sort_search_dialog"]
        if sd ~= nil and not sd.IsActive and G.songList ~= nil then
            local ssn        = G.songList:GetSelectedSongNode()
            local baseFolder = (ssn ~= nil and not ssn.IsRoot) and ssn.Parent or G.songList:GetRoot()
            sd:Activate(G.highlightedPlayer, "search", baseFolder)
        end
    elseif (INPUT:Pressed(inpset.right) or INPUT:KeyboardPressed("RightArrow")) and G.songList ~= nil then
        doMove(1); startHold(1)
    elseif (INPUT:Pressed(inpset.left) or INPUT:KeyboardPressed("LeftArrow")) and G.songList ~= nil then
        doMove(-1); startHold(-1)
    elseif INPUT:Pressed(inpset.decide1) or INPUT:Pressed(inpset.decide2) or INPUT:KeyboardPressed("Return") then
        stopHold()
        G.selectedSongNode = handleDecideSongSelect(Sort)
    elseif (inpset.cancel ~= nil and INPUT:Pressed(inpset.cancel)) or INPUT:KeyboardPressed("Escape") then
        stopHold()
        if handleFolderClose() then
            Sort.applySort(); M.refreshPage(); G.sounds.Decide:Play()
        else
            G.sounds.Cancel:Play()
            return "cancel"
        end
    end

    -- Favorites: toggle on selected song (song node, not locked, not vault-locked)
    if (INPUT:KeyboardPressed("LeftControl") or INPUT:KeyboardPressed("RightControl")) and G.favs ~= nil then
        local ssn = G.songList ~= nil and G.songList:GetSelectedSongNode() or nil
        if ssn ~= nil and ssn.IsSong and not ssn.IsLocked
                and (G.unlocks == nil or not G.unlocks.isVaultLocked(ssn)) then
            local saveId = GetSaveFile(G.highlightedPlayer).SaveId
            G.favs.toggleFavorite(saveId, ssn.UniqueId)
            G.sounds.Decide:Play()
            M.refreshPage(true)   -- the fav icon is cached in the page slots
        end
    end

    -- Favorites folder: open snapshot virtual folder for the highlighted player
    if INPUT:KeyboardPressed("O") and G.favs ~= nil then
        G.favs.openFavoritesFolder(G.highlightedPlayer)
    end

    -- Song speed
    if INPUT:KeyboardPressed("Q") then
        G.sounds.Skip:Play()
        CONFIG.SongSpeed = CONFIG.SongSpeed - 1
        local spd = CONFIG.SongSpeed / 20
        SHARED:GetSharedSound("presound"):SetSpeed(spd)
        if G.previewDemoStartRaw > 0 then G.previewDemoStart = math.floor(G.previewDemoStartRaw / spd) end
    end
    if INPUT:KeyboardPressed("W") then
        G.sounds.Skip:Play()
        CONFIG.SongSpeed = CONFIG.SongSpeed + 1
        local spd = CONFIG.SongSpeed / 20
        SHARED:GetSharedSound("presound"):SetSpeed(spd)
        if G.previewDemoStartRaw > 0 then G.previewDemoStart = math.floor(G.previewDemoStartRaw / spd) end
    end

    -- Player count
    if G.activeConfig.allowPlayerCount ~= false and INPUT:KeyboardPressed("L") then
        G.sounds.Skip:Play()
        CONFIG.PlayerCount = 1 + (CONFIG.PlayerCount % 5)
    end

    -- Enforce constraints
    if G.activeConfig.lockedPlayerCount ~= nil then
        CONFIG.PlayerCount = G.activeConfig.lockedPlayerCount
    end
    if G.activeConfig.mountAISlotToP2 then G.highlightedPlayer = 0 end

    -- Song chosen → start transition to difficulty select
    if G.selectedSongNode ~= nil then
        if G.activeConfig.songOnly then
            -- Online lobby: pick the SONG only — NO difficulty prompt. Set it as the chosen song globally
            -- (difficulty is chosen per-player back in the lobby) and signal the parent to return.
            stopHold()
            pcall(function() G.selectedSongNode:Mount(0, 0, 0, 0, 0) end)
            G.lastSignal = "play"
            return "play"
        end
        Diff.loadDiffBars(G.selectedSongNode)
        stopHold()
        G.activeScreen   = "transition"
        G.diffIndex      = {0, 0, 0, 0, 0}
        G.diffSelected   = {false, false, false, false, false}
        G.startCounter("screen_transition", 0, 1920, 0.5/1920, "none", Diff.updateTransitionVisuals, function()
            G.activeScreen = "difficultyselect"
        end)
    end

    return nil
end

return M
