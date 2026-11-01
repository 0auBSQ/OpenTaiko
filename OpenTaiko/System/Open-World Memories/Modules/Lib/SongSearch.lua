---@diagnostic disable: undefined-global, undefined-field, need-check-nil
-- SongSearch.lua — the song search shared by the sort/search dialog (its live count) and song_select_core
-- (applying a confirmed search): the field options, the match predicate, the folder walk, and the
-- hand-over of the parameters through the player's save file.
--
-- Levels are whole numbers: a chart's "+" is ignored, so "Level to 10" includes the 10+ charts.
--
--   S.DIFF_OPTIONS / S.LEVEL_MAX            the difficulty chooser entries; the level sliders' top value
--   S.buildPredicate(params) -> fn(node)    params: diff, levelFrom, levelTo, levelToOpenEnd, title, subtitle, charter
--   S.collectSongs(folder, predicate, out)  every matching song under the folder, depth first
--   S.count(folder, params)                 how many songs match
--   S.write(save, params) / S.read(save)    the parameters through the save file (ss_* counters + shared strings)

local M = {}

-- the level sliders: from 0..LEVEL_MAX, to 0..LEVEL_MAX-1 then an open end ("13~") at LEVEL_MAX
M.LEVEL_MAX = 13

-- value -1 = any; 34 = Oni and Edit together; the labels are locale keys the dialog resolves
M.DIFF_OPTIONS = {
    { key = "SONGSELECT_SEARCH_ANY",   value = -1 },
    { key = "SONGSELECT_DIFF_EASY",    value = 0 },
    { key = "SONGSELECT_DIFF_NORMAL",  value = 1 },
    { key = "SONGSELECT_DIFF_HARD",    value = 2 },
    { key = "SONGSELECT_DIFF_ONI_EDIT", value = 34 },
}


local function acceptsDifficulty(diff, i)
    if diff == -1 then return true end
    if diff == 34 then return i == 3 or i == 4 end
    return i == diff
end

function M.buildPredicate(params)
    local diff = params.diff or -1
    local titlePat    = (params.title    or ""):lower()
    local subtitlePat = (params.subtitle or ""):lower()
    local charterPat  = (params.charter  or ""):lower()
    local effFrom = (params.levelFrom == nil or params.levelFrom == -1) and -math.huge or params.levelFrom
    local effTo   = (params.levelToOpenEnd or params.levelTo == nil or params.levelTo == -1) and math.huge or params.levelTo

    return function(node)
        if not node.IsSong or node.IsLocked then return false end
        -- vault songs only once the vault is open and the song itself unlocked
        if node.Genre == "Secret Vault" then
            local sf = GetSaveFile(0)
            if not sf:GetGlobalTrigger(".vault_opened") then return false end
            if not sf:GetGlobalTrigger(".vault_song_unlocked_" .. (node.UniqueId or "")) then return false end
        end
        if titlePat    ~= "" and not (node.Title    or ""):lower():find(titlePat,    1, true) then return false end
        if subtitlePat ~= "" and not (node.Subtitle or ""):lower():find(subtitlePat, 1, true) then return false end

        local charts = {}
        for i = 0, 4 do
            if acceptsDifficulty(diff, i) then
                local chart = node:GetChart(i)
                if chart ~= nil then charts[#charts + 1] = chart end
            end
        end
        if #charts == 0 then return false end

        local levelOk = false
        for _, chart in ipairs(charts) do
            local lv = chart.Level or 0
            if lv >= effFrom and lv <= effTo then levelOk = true; break end
        end
        if not levelOk then return false end

        if charterPat ~= "" then
            local ok = false
            for _, chart in ipairs(charts) do
                if (chart.NotesDesigner or ""):lower():find(charterPat, 1, true) then ok = true; break end
            end
            if not ok then return false end
        end
        return true
    end
end

function M.collectSongs(folderNode, predicate, results)
    for i = 0, folderNode.ChildrenCount - 1 do
        local node = folderNode:Child(i)
        if node.IsSong then
            if predicate(node) then results[#results + 1] = node end
        elseif node.IsFolder then
            M.collectSongs(node, predicate, results)
        end
    end
end

function M.count(folderNode, params)
    if folderNode == nil then return 0 end
    local results = {}
    M.collectSongs(folderNode, M.buildPredicate(params), results)
    return #results
end

-- the parameters travel through the player's save file: the dialog writes them and raises ss_search_ready,
-- song_select_core reads them back and clears the flag
function M.write(sav, params)
    SHARED:SetSharedString("ss_title",    params.title    or "")
    SHARED:SetSharedString("ss_subtitle", params.subtitle or "")
    SHARED:SetSharedString("ss_charter",  params.charter  or "")
    sav:SetGlobalCounter("ss_diff",         params.diff or -1)
    sav:SetGlobalCounter("ss_levelFrom",    params.levelFrom or -1)
    sav:SetGlobalCounter("ss_levelTo",      params.levelTo or -1)
    sav:SetGlobalCounter("ss_levelToOE",    params.levelToOpenEnd and 1 or 0)
    sav:SetGlobalCounter("ss_search_ready", 1)
end

function M.read(sav)
    return {
        diff           = math.floor(sav:GetGlobalCounter("ss_diff")      + 0.5),
        levelFrom      = math.floor(sav:GetGlobalCounter("ss_levelFrom") + 0.5),
        levelTo        = math.floor(sav:GetGlobalCounter("ss_levelTo")   + 0.5),
        levelToOpenEnd = sav:GetGlobalCounter("ss_levelToOE") > 0.5,
        title          = SHARED:GetSharedString("ss_title"),
        subtitle       = SHARED:GetSharedString("ss_subtitle"),
        charter        = SHARED:GetSharedString("ss_charter"),
    }
end

return M
