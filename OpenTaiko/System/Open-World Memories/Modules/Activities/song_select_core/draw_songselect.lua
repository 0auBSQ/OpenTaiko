---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local
-- draw_songselect.lua  —  Song-select panel drawing for song_select_core.

local M = {}
local G   -- shared state injected by Script.lua

-- ── Layout constants ──────────────────────────────────────────────────────────

local SONGLIST_ORIGIN_X          = 660
local SONGLIST_ORIGIN_Y          = 500
local SONGLIST_OFFSET_X          = 45
local SONGLIST_OFFSET_Y          = 120
local SONGLIST_TEXT_OFFSET_X     = -65
local SONGLIST_TEXT_OFFSET_Y     = 15
local SONGLIST_SELECTED_X_DIFF   = 50
local SONGLIST_SELECTED_ARROW_GAP = 925
local SONGBAR_LABEL_X_OFFSET     = 288

local SONGINFO_DIFFICULTIES_ORIGIN_X = 1790
local SONGINFO_DIFFICULTIES_ORIGIN_Y = 154
local SONGINFO_DIFFICULTIES_GAP_Y    = 130
local SONGINFO_HASVIDEO_ORIGIN_X     = 1064
local SONGINFO_HASVIDEO_ORIGIN_Y     = 257
local SONGINFO_EXPLICIT_ORIGIN_X     = 1266
local SONGINFO_EXPLICIT_ORIGIN_Y     = 151
local SONGINFO_SUBTITLE_ORIGIN_X     = 1536
local SONGINFO_SUBTITLE_ORIGIN_Y     = 689
local SONGINFO_SUBTITLE_MWIDTH       = 530
local SONGINFO_BPM_ORIGIN_X          = 1780
local SONGINFO_BPM_ORIGIN_Y          = 877
local SONGINFO_BPM_MWIDTH            = 240
local SONGINFO_CHARTER_ORIGIN_X      = 1216
local SONGINFO_CHARTER_ORIGIN_Y      = 750
local SONGINFO_CHARTER_MWIDTH        = 512
local SONGINFO_LENGTH_ORIGIN_X       = 1216
local SONGINFO_LENGTH_ORIGIN_Y       = 806
local SONGINFO_LENGTH_MWIDTH         = 420

local PREIMAGE_ORIGIN_X = 1276
local PREIMAGE_ORIGIN_Y = 146
local PREIMAGE_SIZE_X   = 500
local PREIMAGE_SIZE_Y   = 500

local HEADER_OFFSET_X         = 1780
local HEADER_BOX_TEXT_OFFSET_X = 250
local HEADER_BOX_TEXT_OFFSET_Y = 12

local BARLEFT_X_OFFSET             = -67   -- pixels left of bar.png topleft

local NAMEPLATE_BOX_FOLDED_SIZE_Y  = 182
local NAMEPLATE_SECONDARY_OFFSET_Y = 81
local NAMEPLATE_BOX_START_X        = 0
local NAMEPLATE_BOX_SPACING_X      = 384
local NAMEPLATE_OFFSET_X           = 27
local NAMEPLATE_OFFSET_Y           = 37
local NAMEPLATE_HEIGHT             = 81
local PUCHI_OFFSET_X               = 60

-- ── Init ──────────────────────────────────────────────────────────────────────

function M.init(g)
    G = g
end

-- ── Local utilities ───────────────────────────────────────────────────────────

local function formatDuration(ms)
    if ms <= 0 then return "?:??" end
    local totalSec = math.floor(ms / 1000)
    return string.format("%d:%02d", math.floor(totalSec / 60), totalSec % 60)
end

local function formatNumber(n, decimals)
    local s = string.format("%." .. decimals .. "f", n)
    s = s:gsub("0+$", ""):gsub("%.$", "")
    return s
end

local function getSongNodeFocusChart(songNode)
    if songNode.IsSong ~= true then return nil end
    local default = math.min(4, CONFIG:GetDefaultCourse(0))
    local chart   = songNode:GetChart(default)
    local i = 4
    while chart == nil and i >= 0 do chart = songNode:GetChart(i); i = i - 1 end
    return chart
end

local function drawLevelTag(songNode, x, y)
    local chart = getSongNodeFocusChart(songNode)
    if chart == nil then return end
    local level      = chart.Level
    local difficulty = chart.DifficultyAsInt
    local labelH     = G.bars["levellabels"].Height / 5
    local labelW     = G.bars["levellabels"].Width
    local isVault    = songNode.Genre == "Secret Vault"

    if isVault then
        -- Vault songs: animated strip (same frame counter as storm), always-on gray border
        G.bars["levellabelsvault"]:DrawRectAtAnchor(x, y, 0, labelH * G.levelLabelFrame, labelW, labelH, "center")
        G.drawNumberCentered(level, "levellabelsborder", x, y, COLOR:CreateColorFromHex("ff808080"))
    elseif difficulty < 3 or level <= 10 then
        G.bars["levellabels"]:DrawRectAtAnchor(x, y, 0, labelH * difficulty, labelW, labelH, "center")
    elseif difficulty == 3 then
        G.bars["levellabelsfire"]:DrawRectAtAnchor(x, y, 0, labelH * G.levelLabelFrame, labelW, labelH, "center")
        G.drawNumberCentered(level, "levellabelsborder", x, y, COLOR:CreateColorFromHex("ffac0c0c"))
    else
        G.bars["levellabelsstorm"]:DrawRectAtAnchor(x, y, 0, labelH * G.levelLabelFrame, labelW, labelH, "center")
        G.drawNumberCentered(level, "levellabelsborder", x, y, COLOR:CreateColorFromHex("ff83159e"))
    end
    G.drawNumberCentered(level, "levellabels", x, y)
    if chart.IsPlus == true then
        if isVault then
            G.bars["levellabelsplusvault"]:DrawAtAnchor(x, y, "center")
        else
            G.bars["levellabelsplus"]:DrawRectAtAnchor(x, y, 0, labelH * difficulty, labelW, labelH, "center")
        end
    end
end

local function drawBarleft(node, xpos, ypos)
    local barW = G.bars["bar"].Width
    local barH = G.bars["bar"].Height
    local lx   = xpos - barW / 2 + BARLEFT_X_OFFSET
    local ly   = ypos - barH / 2

    G.bars["barleft"]:Draw(lx, ly)

    local chart = getSongNodeFocusChart(node)
    if chart == nil then
        G.bars["scorerank_none"]:Draw(lx, ly)
        G.bars["clearstatus_none"]:Draw(lx, ly)
        return
    end

    local info   = chart:GetPlayerBestScore(G.highlightedPlayer)
    local played = info.HasBeenPlayed
    local cs     = info.ClearStatus
    local sr     = info.ScoreRank

    -- cs stored: 0=never played/failed, 1=assisted, 2=clear, 3=FC, 4=perfect.
    if not played then
        G.bars["clearstatus_none"]:Draw(lx, ly)
        G.bars["scorerank_none"]:Draw(lx, ly)
    elseif cs == 0 then
        -- Played but failed
        G.bars["clearstatus_m1"]:Draw(lx, ly)
        if sr == 0 then
            G.bars["scorerank_m1"]:Draw(lx, ly)
        else
            G.bars["scorerank_" .. (sr - 1)]:Draw(lx, ly)
        end
    else
        G.bars["clearstatus_" .. (cs - 1)]:Draw(lx, ly)
        if sr == 0 then
            G.bars["scorerank_m1"]:Draw(lx, ly)
        else
            G.bars["scorerank_" .. (sr - 1)]:Draw(lx, ly)
        end
    end
end

local function drawPreimage()
    local node = G.songList:GetSongNodeAtOffset(0)
    if node == nil or node.IsSong ~= true then return end
    if G.unlocks.effectiveHiddenIndex(node) >= 2 then return end
    G.bgtx["preimage_load"]:Draw(PREIMAGE_ORIGIN_X - G.songSelectShift, PREIMAGE_ORIGIN_Y)
    G.bgtx["load"]:DrawAtAnchor(
        PREIMAGE_ORIGIN_X - G.songSelectShift + PREIMAGE_SIZE_X / 2,
        PREIMAGE_ORIGIN_Y + PREIMAGE_SIZE_Y / 2, "center")
    local tex = SHARED:GetSharedTexture("preimage")
    if tex.Height > 0 and tex.Width > 0 then
        tex:SetScale(PREIMAGE_SIZE_X / tex.Height, PREIMAGE_SIZE_Y / tex.Width)
        tex:Draw(PREIMAGE_ORIGIN_X - G.songSelectShift, PREIMAGE_ORIGIN_Y)
    end
end

-- ── Draw panel ────────────────────────────────────────────────────────────────

function M.drawPanel()
    local opacityNorm = G.songSelectElemOpacity / 255
    local ssn = G.songList:GetSelectedSongNode()

    -- Random / song info panels
    if ssn ~= nil and ssn.IsRandom then
        G.bgtx["randominfo"]:DrawAtAnchor(1920 - G.songSelectShift, 0, "topright")
    end

    if ssn ~= nil and ssn.IsSong then
        local ssnHI = G.unlocks.effectiveHiddenIndex(ssn)
        -- BLURED / vault locked: skip the song info panel entirely
        if ssnHI < 2 then
            G.bgtx["songinfo"]:DrawAtAnchor(1920 - G.songSelectShift, 0, "topright")
            if ssn.HasVideo then
                G.bgtx["sinfo_video"]:Draw(SONGINFO_HASVIDEO_ORIGIN_X - G.songSelectShift, SONGINFO_HASVIDEO_ORIGIN_Y)
            end
            if ssn.Explicit then
                G.bgtx["sinfo_explicit"]:DrawAtAnchor(
                    SONGINFO_EXPLICIT_ORIGIN_X - G.songSelectShift, SONGINFO_EXPLICIT_ORIGIN_Y, "topright")
            end

            -- Difficulty icons: hidden for GRAYED and above
            if ssnHI == 0 then
                local isVaultSong = ssn.Genre == "Secret Vault"
                for i = 0, 4 do
                    local chart = ssn:GetChart(i)
                    local xpos  = SONGINFO_DIFFICULTIES_ORIGIN_X - G.songSelectShift
                    local ypos  = SONGINFO_DIFFICULTIES_ORIGIN_Y + SONGINFO_DIFFICULTIES_GAP_Y * math.min(i, 3)
                    if ssn:GetChart(3) ~= nil and i == 4 then
                        if chart ~= nil then
                            local difftx = isVaultSong and G.bgtx["sinfo_difficulties_vault"] or G.bgtx["sinfo_difficulties_4"]
                            difftx:SetOpacity(G.difficultyFade4 / 255)
                            difftx:Draw(xpos, ypos)
                            difftx:SetOpacity(1)
                            G.drawNumberCentered(chart.Level, "sinfo_level",
                                xpos + difftx.Width / 2,
                                ypos + difftx.Height / 2,
                                nil, G.difficultyFade4 / 255)
                            if chart.IsPlus then
                                local plustx = isVaultSong and G.bgtx["sinfo_difficulties_vault_plus"] or G.bgtx["sinfo_difficulties_" .. i .. "_plus"]
                                plustx:SetOpacity(G.difficultyFade4 / 255)
                                plustx:Draw(xpos, ypos)
                                plustx:SetOpacity(1)
                            end
                        end
                    elseif chart == nil then
                        -- Vault songs: never show the "missing" indicator
                        if not isVaultSong and (ssn:GetChart(4) == nil or i ~= 3) then
                            G.bgtx["sinfo_difficulties_missing"]:Draw(xpos, ypos)
                        end
                    else
                        local difftx = isVaultSong and G.bgtx["sinfo_difficulties_vault"] or G.bgtx["sinfo_difficulties_" .. i]
                        difftx:Draw(xpos, ypos)
                        G.drawNumberCentered(chart.Level, "sinfo_level",
                            xpos + difftx.Width / 2,
                            ypos + difftx.Height / 2)
                        if chart.IsPlus then
                            local plustx = isVaultSong and G.bgtx["sinfo_difficulties_vault_plus"] or G.bgtx["sinfo_difficulties_" .. i .. "_plus"]
                            plustx:Draw(xpos, ypos)
                        end
                    end
                end
            end

            local focusedChart = getSongNodeFocusChart(ssn)
            G.textSmall:GetText(ssn.Subtitle, false, SONGINFO_SUBTITLE_MWIDTH)
                :DrawAtAnchor(SONGINFO_SUBTITLE_ORIGIN_X - G.songSelectShift, SONGINFO_SUBTITLE_ORIGIN_Y, "center")
            G.textSmall:GetText("Chart - " .. ssn.Maker, false, SONGINFO_CHARTER_MWIDTH)
                :Draw(SONGINFO_CHARTER_ORIGIN_X - G.songSelectShift, SONGINFO_CHARTER_ORIGIN_Y)
            G.textSmall:GetText("Length - " .. formatDuration(G.previewDurationMs), false, SONGINFO_LENGTH_MWIDTH)
                :Draw(SONGINFO_LENGTH_ORIGIN_X - G.songSelectShift, SONGINFO_LENGTH_ORIGIN_Y)

            if focusedChart ~= nil then
                local mult    = CONFIG.SongSpeed / 20
                local bpmText = formatNumber(focusedChart.BaseBPM * mult, 3)
                if focusedChart.BaseBPM ~= focusedChart.MinBPM or focusedChart.BaseBPM ~= focusedChart.MaxBPM then
                    bpmText = bpmText .. " (" .. formatNumber(focusedChart.MinBPM * mult, 3)
                           .. "-" .. formatNumber(focusedChart.MaxBPM * mult, 3) .. ")"
                end
                local col = "FFFFFFFF"
                if mult < 1 then col = "ff95ccff" elseif mult > 1 then col = "ffff9ec3" end
                G.text:GetText(bpmText, false, SONGINFO_BPM_MWIDTH, COLOR:CreateColorFromHex(col))
                    :DrawAtAnchor(SONGINFO_BPM_ORIGIN_X - G.songSelectShift, SONGINFO_BPM_ORIGIN_Y, "center")
            end
        end
    end

    drawPreimage()

    -- Song list bars
    if G.pageTexts ~= nil then
        for i, tx in pairs(G.pageTexts) do
            local xpos = SONGLIST_ORIGIN_X + (i + G.selectBoxDist) * SONGLIST_OFFSET_X - G.songSelectShift
            local ypos = SONGLIST_ORIGIN_Y + (i + G.selectBoxDist) * SONGLIST_OFFSET_Y
            if i == 0 then xpos = xpos + SONGLIST_SELECTED_X_DIFF end
            if tx ~= nil then
                local node = G.currentPage[i]
                if node.IsSong or node.IsFolder then
                    if node.IsSong and G.unlocks.isVaultLocked(node) then
                        -- Secret Vault locked song: vault bar + vault lock icon, no title, no level tag
                        G.unlocks.drawVaultBar(node, xpos, ypos)
                        G.unlocks.drawVaultLockIcon(node, xpos, ypos)
                    elseif node.IsFolder and G.unlocks.isVaultFolder(node) then
                        -- Secret Vault folder (locked): normal bar, blurred glitch, lockF overlay, no title
                        G.bars["bar"]:SetColor(node.BoxColor)
                        G.bars["bar"]:DrawAtAnchor(xpos, ypos, "center")
                        G.unlocks.drawBluredStatic(xpos, ypos)
                        G.unlocks.drawVaultFolderLock(node, xpos, ypos)
                    elseif node.IsLocked then
                        -- Draw the HiddenIndex-appropriate bar (GRAYED/BLURED both use bar_1);
                        -- DISPLAYED locked songs fall through to the normal bar below.
                        if not G.unlocks.drawLockedBar(node, xpos, ypos) then
                            G.bars["bar"]:SetColor(node.BoxColor)
                            G.bars["bar"]:DrawAtAnchor(xpos, ypos, "center")
                            G.genre_overlays[node.Genre]:DrawAtAnchor(xpos, ypos, "center")
                        end
                        local hi = node.IsSong and node.HiddenIndex or 0
                        -- Title area content (over bar, under lock icon):
                        -- BLURED → static.png with GL noise; others → normal title text.
                        if hi == 2 then
                            G.unlocks.drawBluredStatic(xpos, ypos)
                        else
                            tx:DrawAtAnchor(xpos + SONGLIST_TEXT_OFFSET_X, ypos + SONGLIST_TEXT_OFFSET_Y, "center")
                        end
                        -- Lock icon on top of title area
                        G.unlocks.drawLockIcon(node, xpos, ypos)
                        -- Level tag: only DISPLAYED locked songs (hi == 0)
                        if node.IsSong and hi == 0 then
                            drawLevelTag(node, xpos + SONGBAR_LABEL_X_OFFSET, ypos)
                        end
                    else
                        G.bars["bar"]:SetColor(node.BoxColor)
                        G.bars["bar"]:DrawAtAnchor(xpos, ypos, "center")
                        G.genre_overlays[node.Genre]:DrawAtAnchor(xpos, ypos, "center")
                        if node.IsSong then drawBarleft(node, xpos, ypos) end
                        drawLevelTag(node, xpos + SONGBAR_LABEL_X_OFFSET, ypos)
                        tx:DrawAtAnchor(xpos + SONGLIST_TEXT_OFFSET_X, ypos + SONGLIST_TEXT_OFFSET_Y, "center")
                    end
                elseif node.IsRandom then
                    G.bars["random"]:DrawAtAnchor(xpos, ypos, "center")
                    tx:DrawAtAnchor(xpos + SONGLIST_TEXT_OFFSET_X, ypos + SONGLIST_TEXT_OFFSET_Y, "center")
                elseif node.IsReturn then
                    G.bars["back"]:DrawAtAnchor(xpos, ypos, "center")
                    tx:DrawAtAnchor(xpos + SONGLIST_TEXT_OFFSET_X, ypos + SONGLIST_TEXT_OFFSET_Y, "center")
                end
            end
        end

        -- Selected bar + animated arrows
        local x0     = SONGLIST_ORIGIN_X + SONGLIST_SELECTED_X_DIFF - G.songSelectShift
        local y0     = SONGLIST_ORIGIN_Y
        local ax     = G.arrowsDistance
        local xlshift = ax * math.cos(7 * math.pi / 12)
        local ylshift = ax * math.sin(7 * math.pi / 12)
        local isSong = ssn ~= nil and ssn.IsSong
        local isUnlockedSong = isSong and (ssn.IsLocked ~= true) and not G.unlocks.isVaultLocked(ssn)
        if isUnlockedSong then
            -- selectedlarge covers bar + barleft; align its right edge with selected's right edge
            local largeCenterX = x0 - (G.bars["selectedlarge"].Width - G.bars["selected"].Width) / 2
            G.bars["selectedlarge"]:DrawAtAnchor(largeCenterX, y0, "center")
        else
            G.bars["selected"]:DrawAtAnchor(x0, y0, "center")
        end
        -- Left arrow shifts 67px further left when an unlocked song is selected to clear barleft
        local arrowLOffset = isUnlockedSong and BARLEFT_X_OFFSET or 0
        G.bars["selected-arrow-l"]:DrawAtAnchor(x0 - SONGLIST_SELECTED_ARROW_GAP/2 + xlshift + arrowLOffset, y0 - ylshift, "left")
        G.bars["selected-arrow-r"]:DrawAtAnchor(x0 + SONGLIST_SELECTED_ARROW_GAP/2 - xlshift, y0 + ylshift, "right")
    end

    -- Header breadcrumb
    G.bgtx["header"]:Draw(-G.songSelectShift, 0)
    if ssn ~= nil then
        local pathStack  = {}
        local currentNode = ssn.Parent
        while currentNode ~= nil do
            local _tx = currentNode.Title ~= nil and currentNode.Title or "/"
            table.insert(pathStack, G.text:GetText(_tx, false, 270))
            currentNode = currentNode.Parent
        end
        local xpos = HEADER_OFFSET_X - G.songSelectShift
        for i, title in ipairs(pathStack) do
            G.bgtx["header-box"]:SetOpacity(opacityNorm)
            G.bgtx["header-box"]:DrawAtAnchor(xpos, 0, "topright")
            title:SetOpacity(opacityNorm)
            title:DrawAtAnchor(
                xpos - G.bgtx["header-box"].Width + HEADER_BOX_TEXT_OFFSET_X,
                HEADER_BOX_TEXT_OFFSET_Y + G.bgtx["header-box"].Height / 2,
                "center")
            G.bgtx["header-arrow"]:SetOpacity(opacityNorm)
            if i ~= #pathStack then
                G.bgtx["header-arrow"]:DrawAtAnchor(xpos - G.bgtx["header-box"].Width, 0, "topright")
            end
            xpos = xpos - G.bgtx["header-box"].Width - G.bgtx["header-arrow"].Width
        end
    end

    -- Overlay
    G.bgtx["overlay"]:SetOpacity(opacityNorm)
    G.bgtx["overlay"]:Draw(0, 0)

    -- Unlock conditions panel (shown when a locked song is highlighted)
    G.unlocks.drawCondsPanel()
    G.unlocks.drawVaultCondsPanel()

    -- Nameplates
    local playerCount = CONFIG.PlayerCount
    G.highlightedPlayer = G.highlightedPlayer % playerCount

    G.bgtx["nameplate_info"]:SetOpacity(opacityNorm)
    do
        local x0       = NAMEPLATE_BOX_START_X
        local y0       = 1080 - NAMEPLATE_BOX_FOLDED_SIZE_Y
        local ssCharaX = x0 + G.bgtx["nameplate_info"].Width / 2
        G.bgtx["nameplate_info"]:Draw(x0, y0)
        G.drawPlayerChara(G.highlightedPlayer, ssCharaX, y0 + NAMEPLATE_OFFSET_Y, 1, 1, opacityNorm, false)
        G.drawPlayerPuchi(G.highlightedPlayer, ssCharaX - PUCHI_OFFSET_X, y0 + NAMEPLATE_OFFSET_Y + G.puchiSineY, 1, 1, opacityNorm)
        NAMEPLATE:DrawPlayerNameplate(x0 + NAMEPLATE_OFFSET_X, y0 + NAMEPLATE_OFFSET_Y, G.songSelectElemOpacity, G.highlightedPlayer)

        -- Perfect / FC / Clear counts for the highlighted player at the displayed difficulty
        if G.textStats ~= nil then
            local diff    = math.min(4, CONFIG:GetDefaultCourse(0))
            local sav     = GetSaveFile(G.highlightedPlayer)
            local white   = COLOR:CreateColorFromHex("ffffffff")
            local nPerfect = sav:GetClearStatusCount(diff, 4)
            local nFC      = sav:GetClearStatusCount(diff, 3)
            local nClear   = sav:GetClearStatusCount(diff, 2)
            local function drawStat(n, x)
                local tx = G.textStats:GetText(tostring(n), false, 99999, white)
                tx:SetOpacity(opacityNorm)
                tx:DrawAtAnchor(x, 1058, "center")
                tx:SetOpacity(1)
            end
            drawStat(nPerfect, 95)
            drawStat(nFC,     212)
            drawStat(nClear,  329)
        end
    end

    for i = 1, playerCount - 1 do
        local j    = i
        if j - 1 >= G.highlightedPlayer then j = j + 1 end
        local xpos    = NAMEPLATE_BOX_START_X + i * NAMEPLATE_BOX_SPACING_X
        local ypos    = 1080 - NAMEPLATE_SECONDARY_OFFSET_Y
        local portCx  = xpos + G.bgtx["nameplate_info"].Width / 2
        G.bgtx["placeholder_portrait"]:SetOpacity(opacityNorm)
        G.bgtx["placeholder_portrait"]:DrawAtAnchor(portCx, ypos, "bottom")
        G.bgtx["placeholder_portrait"]:SetOpacity(1)
        -- Draw Portrait.png over the placeholder if it loaded for this player slot
        local portrait = G.portraits ~= nil and G.portraits[j - 1]
        if portrait ~= nil and portrait.Loaded then
            portrait:SetOpacity(opacityNorm)
            portrait:DrawAtAnchor(portCx, ypos, "bottom")
            portrait:SetOpacity(1)
        end
        NAMEPLATE:DrawPlayerNameplate(xpos + NAMEPLATE_OFFSET_X, ypos, G.songSelectElemOpacity, j - 1)
    end
end

return M
