---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local
-- diffselect.lua  —  Difficulty-select draw panel and update handler
--                    for song_select_core.

local M = {}
local G   -- shared state injected by Script.lua

-- ── Layout constants ──────────────────────────────────────────────────────────

local DIFFSELECT_CHARA_ORIG_X_35P = 1250
local DIFFSELECT_CHARA_ORIG_Y_35P = 470
local DIFFSELECT_CHARA_GAP_X_35P  = 332
local DIFFSELECT_CHARA_GAP_Y_35P  = 457
local DIFFSELECT_CHARA_SCALE_35P  = 0.5
local DIFFSELECT_CHARA_ORIG_X_12P = 1300
local DIFFSELECT_CHARA_ORIG_Y_12P = 760
local DIFFSELECT_CHARA_GAP_X_12P  = 468
local DIFFSELECT_CHARA_SCALE_12P  = 0.8

local DIFFSELECT_SMALL_BAR_X = {678, 718, 758}
local DIFFSELECT_SMALL_BAR_Y = {721, 900, 1079}
local DIFFSELECT_SMALL_BAR_SELECT_Y_OFFSET            = 156
local DIFFSELECT_SMALL_BAR_SELECT_Y_OFFSET_CORRECTION = -6
local DIFFSELECT_BIG_BAR_ORIG_X      = 676
local DIFFSELECT_BIG_BAR_ORIG_Y      = 309
local DIFFSELECT_BIG_BAR_GAP_X       = 3
local DIFFSELECT_BIG_BAR_GAP_Y       = 180
local DIFFSELECT_NOTESDESIGNER_OFFSET_Y = -10
local DIFFSELECT_LEVEL_BAR_X         = 480
local DIFFSELECT_LEVEL_BAR_Y         = 187
local DIFFSELECT_LEVEL_NB_OFF_X      = -226
local DIFFSELECT_LEVEL_NB_OFF_Y      = -115
local DIFFSELECT_LEVEL_COLORS = {
    COLOR:CreateColorFromHex("FF65A9F7"),
    COLOR:CreateColorFromHex("FF8FF5A9"),
    COLOR:CreateColorFromHex("FFE6DA76"),
    COLOR:CreateColorFromHex("FFF39898"),
    COLOR:CreateColorFromHex("FFCD7EE6"),
}

local NAMEPLATE_HEIGHT   = 81
local NAMEPLATE_OFFSET_X = 27
local NAMEPLATE_OFFSET_Y = 37
local PUCHI_OFFSET_X     = 60

-- ── Init ──────────────────────────────────────────────────────────────────────

function M.init(g)
    G = g
end

-- ── Helpers ───────────────────────────────────────────────────────────────────

function M.loadDiffBars(ssn)
    G.diffBars = {}
    local startDiff = 0
    local isVault   = ssn.Genre == "Secret Vault"
    if isVault then startDiff = 3 end
    for i = startDiff, 4 do
        local chart = ssn:GetChart(i)
        if chart ~= nil then
            table.insert(G.diffBars, {
                vault      = isVault,
                level      = chart.Level,
                isplus     = chart.IsPlus,
                charter    = chart.NotesDesigner,
                difficulty = i,
            })
        end
    end
end

function M.updateTransitionVisuals(val)
    G.songSelectShift = val
    local opacity    = 255 - (val * (255 / 960))
    G.songSelectElemOpacity      = math.max(0, math.min(255, opacity))
    local diffOpacity = (val - 960) * (255 / 960)
    G.difficultySelectElemOpacity = math.max(0, math.min(255, diffOpacity))
end

function M.resetToSongSelect()
    G.songSelectElemOpacity      = 255
    G.difficultySelectElemOpacity = 0
    G.songSelectShift = 0
    G.activeScreen    = "songselect"
end

-- ── Private draw helpers ──────────────────────────────────────────────────────

local function drawDifficultyBar(index, barinfo)
    local xshift = 1920 - G.songSelectShift
    local tex    = G.bars["difficultybar7"]
    if barinfo.vault == false then
        tex = G.bars["difficultybar" .. (barinfo.difficulty + 2)]
    end
    local xpos = DIFFSELECT_BIG_BAR_ORIG_X + (index - 3) * DIFFSELECT_BIG_BAR_GAP_X + xshift
    local ypos = DIFFSELECT_BIG_BAR_ORIG_Y + (index - 3) * DIFFSELECT_BIG_BAR_GAP_Y
    for i = 1, CONFIG.PlayerCount do
        if G.diffIndex[i] == index and not (G.activeConfig.mountAISlotToP2 and i == 2) then
            G.bars["difficultybarselect" .. i]:DrawRectAtAnchor(
                xpos, ypos + DIFFSELECT_SMALL_BAR_SELECT_Y_OFFSET_CORRECTION,
                0, 0,
                G.bars["difficultybarselect" .. i].Width,
                DIFFSELECT_SMALL_BAR_SELECT_Y_OFFSET,
                "bottomright")
        end
    end
    tex:DrawAtAnchor(xpos, ypos, "bottomright")
    local nd = G.textSmall:GetText("Charter - " .. barinfo.charter, false, 1000)
    nd:DrawAtAnchor(xpos, ypos + DIFFSELECT_NOTESDESIGNER_OFFSET_Y, "topright")

    local xbar  = xpos - tex.Width + DIFFSELECT_LEVEL_BAR_X
    local ybar  = ypos - tex.Height + DIFFSELECT_LEVEL_BAR_Y
    local bartx = G.bars["difficultybarlevel5"]
    if barinfo.vault == false then
        bartx = G.bars["difficultybarlevel" .. (math.min(3, barinfo.difficulty) + 1)]
        if barinfo.level > 10 then bartx = G.bars["difficultybarlevel6"] end
    end
    bartx:DrawRect(xbar, ybar, 0, 0, bartx.Width * (math.min(10, barinfo.level) / 10), bartx.Height)
    if barinfo.level > 10 then
        local anim = G.bars["difficultybarlevel7"]
        anim:DrawRect(xbar, ybar, 0, bartx.Height * G.levelLabelFrame,
            bartx.Width * (math.min(3, barinfo.level - 10) / 3), bartx.Height)
    end

    local xlvnb    = xpos + DIFFSELECT_LEVEL_NB_OFF_X
    local ylvnb    = ypos + DIFFSELECT_LEVEL_NB_OFF_Y
    local lvnbcol  = COLOR:CreateColorFromHex("FFB9B9B9")
    if barinfo.vault == false then lvnbcol = DIFFSELECT_LEVEL_COLORS[barinfo.difficulty + 1] end
    G.drawNumberCentered(barinfo.level, "diffsel_level",    xlvnb, ylvnb)
    G.drawNumberCentered(barinfo.level, "diffsel_levelcol", xlvnb, ylvnb, lvnbcol)
end

local function drawDiffSelectBar(index, barinfo)
    local xshift = 1920 - G.songSelectShift
    if index < 3 then
        local xpos = DIFFSELECT_SMALL_BAR_X[index + 1] + xshift
        local ypos = DIFFSELECT_SMALL_BAR_Y[index + 1]
        for i = 1, CONFIG.PlayerCount do
            if G.diffIndex[i] == index and not (G.activeConfig.mountAISlotToP2 and i == 2) then
                G.bars["difficultybarselect" .. i]:DrawRectAtAnchor(
                    xpos, ypos, 0,
                    DIFFSELECT_SMALL_BAR_SELECT_Y_OFFSET,
                    G.bars["difficultybarselect" .. i].Width,
                    G.bars["difficultybarselect" .. i].Height - DIFFSELECT_SMALL_BAR_SELECT_Y_OFFSET,
                    "bottomleft")
            end
        end
        G.bars["smallbar" .. index]:DrawAtAnchor(xpos, ypos, "bottomleft")
    else
        drawDifficultyBar(index, barinfo)
    end
end

-- ── Draw panel ────────────────────────────────────────────────────────────────

function M.drawPanel()
    local opacityNorm = G.difficultySelectElemOpacity / 255
    local xshift      = 1920 - G.songSelectShift

    G.bgtx["difficultyselect"]:Draw(xshift, 0)
    G.bgtx["header"]:Draw(xshift, 0)
    G.bgtx["overlay_difficulty"]:SetOpacity(opacityNorm)
    G.bgtx["overlay_difficulty"]:DrawAtAnchor(1920, 0, "TopRight")

    if G.selectedSongNode ~= nil then
        local titleTx    = G.textLarge:GetText(G.selectedSongNode.Title,    false, 1280)
        local subtitleTx = G.text:GetText(G.selectedSongNode.Subtitle, false, 1280)
        titleTx:SetOpacity(opacityNorm);    titleTx:Draw(1926 - G.songSelectShift, 0)
        subtitleTx:SetOpacity(opacityNorm); subtitleTx:Draw(1926 - G.songSelectShift, 67)

        for i = 0, 2 + #G.diffBars do
            local barinfo = nil
            if i >= 3 then barinfo = G.diffBars[i - 2] end
            drawDiffSelectBar(i, barinfo)
        end
    end

    -- Characters and nameplates
    do
        local p     = CONFIG.PlayerCount
        local is35  = p > 2
        local ox    = is35 and DIFFSELECT_CHARA_ORIG_X_35P or DIFFSELECT_CHARA_ORIG_X_12P
        local oy    = is35 and DIFFSELECT_CHARA_ORIG_Y_35P or DIFFSELECT_CHARA_ORIG_Y_12P
        local gx    = is35 and DIFFSELECT_CHARA_GAP_X_35P  or DIFFSELECT_CHARA_GAP_X_12P
        local gy    = is35 and DIFFSELECT_CHARA_GAP_Y_35P  or 0
        local s     = is35 and DIFFSELECT_CHARA_SCALE_35P  or DIFFSELECT_CHARA_SCALE_12P
        local r1Count = (p == 5 and 3) or (p > 2 and 2) or p

        for i = 0, p - 1 do
            local isRow2 = i >= r1Count
            local r      = isRow2 and 1 or 0
            local cols   = isRow2 and (p - r1Count) or r1Count
            local colIdx = isRow2 and (i - r1Count) or i
            local x      = ox + (colIdx - (cols - 1) / 2) * gx
            local y      = oy + r * gy
            G.drawCharaWithNameplate(i, x, y, s, s, opacityNorm, true)
            local charaX = x + G.bgtx["nameplate_info"].Width / 2 - NAMEPLATE_OFFSET_X
            G.drawPlayerPuchi(i, charaX - PUCHI_OFFSET_X * s, y + G.puchiSineY * s, s, s, opacityNorm)
            if G.modicons_ro ~= nil then
                G.modicons_ro:Draw(x, y + NAMEPLATE_HEIGHT + 4, i, nil, G.difficultySelectElemOpacity)
            end
        end
    end

    -- AI level slider (AI battle only)
    if G.activeConfig.mountAISlotToP2 then
        local cx      = 1490 + xshift
        local labelTx = G.textSmall:GetText("Starting AI Level", false, 400)
        labelTx:SetOpacity(opacityNorm)
        labelTx:DrawAtAnchor(cx, 940, "center")
        local levelTx = G.text:GetText(tostring(CONFIG.AILevel), false, 200)
        levelTx:SetOpacity(opacityNorm)
        levelTx:DrawAtAnchor(cx, 980, "center")
        local ax = G.arrowsDistance
        if CONFIG.AILevel > 1 then
            local arrowL = G.textSmall:GetText("◀", false, 60)
            arrowL:SetOpacity(opacityNorm)
            arrowL:DrawAtAnchor(cx - 55 - ax, 980, "right")
        end
        if CONFIG.AILevel < 10 then
            local arrowR = G.textSmall:GetText("▶", false, 60)
            arrowR:SetOpacity(opacityNorm)
            arrowR:DrawAtAnchor(cx + 55 + ax, 980, "left")
        end
    end
end

-- ── Update handler ────────────────────────────────────────────────────────────
-- Returns "play", "cancel" (unused here; cancel goes back to songselect), or nil.

function M.handleUpdate()
    local allDiffsSelected = true
    local canceled         = false

    for i = 1, CONFIG.PlayerCount do
        if G.activeConfig.mountAISlotToP2 and i == 2 then
            -- AI mirrors P1
            G.diffIndex[2]   = G.diffIndex[1]
            G.diffSelected[2] = G.diffSelected[1]
        else
            local inpset = G.inputSets[i]

            if G.diffSelected[i] == false then
                if INPUT:Pressed(inpset.right) or (i == 1 and INPUT:KeyboardPressed("RightArrow")) then
                    G.sounds.Skip:Play()
                    G.diffIndex[i] = (G.diffIndex[i] + 1) % (3 + #G.diffBars)
                elseif INPUT:Pressed(inpset.left) or (i == 1 and INPUT:KeyboardPressed("LeftArrow")) then
                    G.sounds.Skip:Play()
                    G.diffIndex[i] = (G.diffIndex[i] - 1) % (3 + #G.diffBars)
                elseif INPUT:Pressed(inpset.decide1) or INPUT:Pressed(inpset.decide2)
                        or (i == 1 and INPUT:KeyboardPressed("Return")) then
                    if G.diffIndex[i] == 0 then
                        G.sounds.Cancel:Play(); canceled = true
                    elseif G.diffIndex[i] == 1 then
                        G.act_inner["customize_dialog"]:Activate(i - 1); return nil
                    elseif G.diffIndex[i] == 2 then
                        G.act_inner["mod_select_dialog"]:Activate(i - 1); return nil
                    else
                        G.sounds.Decide:Play(); G.diffSelected[i] = true
                    end
                elseif (inpset.cancel ~= nil and INPUT:Pressed(inpset.cancel))
                        or (i == 1 and INPUT:KeyboardPressed("Escape")) then
                    G.sounds.Cancel:Play()
                    if G.diffSelected[i] then G.diffSelected[i] = false
                    else canceled = true end
                end
            end
        end

        if G.diffSelected[i] == false then allDiffsSelected = false end
    end

    if INPUT:KeyboardPressed("F3") then
        G.sounds.Decide:Play(); CONFIG:SetAutoStatus(0, not CONFIG:GetAutoStatus(0))
    end
    if INPUT:KeyboardPressed("F4") and CONFIG.PlayerCount >= 2 then
        G.sounds.Decide:Play(); CONFIG:SetAutoStatus(1, not CONFIG:GetAutoStatus(1))
    end

    if canceled or INPUT:KeyboardPressed("Escape") then
        G.sounds.Decide:Play()
        G.activeScreen = "transition"
        G.startCounter("screen_transition", 1920, 0, -0.5/1920, "none", M.updateTransitionVisuals, function()
            G.activeScreen = "songselect"
        end)
    elseif allDiffsSelected then
        local success = G.selectedSongNode:Mount(
            (G.diffIndex[1] >= 3) and G.diffBars[G.diffIndex[1] - 2].difficulty or 0,
            (G.diffIndex[2] >= 3) and G.diffBars[G.diffIndex[2] - 2].difficulty or 0,
            (G.diffIndex[3] >= 3) and G.diffBars[G.diffIndex[3] - 2].difficulty or 0,
            (G.diffIndex[4] >= 3) and G.diffBars[G.diffIndex[4] - 2].difficulty or 0,
            (G.diffIndex[5] >= 3) and G.diffBars[G.diffIndex[5] - 2].difficulty or 0
        )
        if success then
            G.lastSignal = "play"; return "play"
        else
            G.diffSelected = {false, false, false, false, false}
        end
    end

    -- AI level slider (AI battle only)
    if G.activeConfig.mountAISlotToP2 then
        if INPUT:Pressed("LBlue2P") and CONFIG.AILevel > 1 then
            CONFIG.AILevel = CONFIG.AILevel - 1; G.sounds.Skip:Play()
        elseif INPUT:Pressed("RBlue2P") and CONFIG.AILevel < 10 then
            CONFIG.AILevel = CONFIG.AILevel + 1; G.sounds.Skip:Play()
        end
    end

    return nil
end

return M
