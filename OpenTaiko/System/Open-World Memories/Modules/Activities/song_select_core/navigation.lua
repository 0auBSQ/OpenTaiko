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

function M.refreshPage(skipMedia)
    G.currentPage = {}
    G.pageTexts   = {}

    for i = -5, 5 do
        local node = G.songList:GetSongNodeAtOffset(i)
        G.currentPage[i] = node
        if node == nil then
            G.pageTexts[i] = nil
        else
            if i == 0 then
                G.pageTexts[i] = G.text:GetText(node.Title, false, 525, COLOR:CreateColorFromARGB(255,242,207,1))
            else
                G.pageTexts[i] = G.text:GetText(node.Title, false, 525)
            end
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
        local rdNd = G.songList:GetRandomNodeInFolder(ssn)
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
    if INPUT:KeyboardPressed("F3") then
        G.sounds.Decide:Play(); CONFIG:SetAutoStatus(0, not CONFIG:GetAutoStatus(0))
    end
    if INPUT:KeyboardPressed("F4") and CONFIG.PlayerCount >= 2 then
        G.sounds.Decide:Play(); CONFIG:SetAutoStatus(1, not CONFIG:GetAutoStatus(1))
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

    -- Background cycle
    if INPUT:KeyboardPressed("O") then
        if G.currentBackground == 0 then
            SHARED:SetSharedTexture("background", "Textures/bg1.png"); G.currentBackground = 1
        else
            SHARED:SetSharedTexture("background", "Textures/bg0.png"); G.currentBackground = 0
        end
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
