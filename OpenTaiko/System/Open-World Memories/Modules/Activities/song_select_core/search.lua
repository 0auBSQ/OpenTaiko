---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local
-- search.lua — Glue between sort_search_dialog and the song list.
--
-- sort_search_dialog writes search params to shared storage (SHARED strings +
-- save-file counters) and sets ss_search_ready=1 when the user confirms a search.
-- M.checkSearchReady() reads those params, runs the search against G.songList,
-- and opens a virtual folder with the results.  Returns true if a search was
-- applied (callers can skip their own post-dialog logic in that case).

local M = {}
local G   -- shared state injected by Script.lua

-- ── Predicate builder (mirrors sort_search_dialog's filter logic) ─────────────

local function buildPredicate(params)
    local diff           = params.diff
    local levelFrom      = params.levelFrom
    local levelFromPlus  = params.levelFromPlus
    local levelTo        = params.levelTo
    local levelToPlus    = params.levelToPlus
    local levelToOpenEnd = params.levelToOpenEnd
    local titlePat       = (params.title    or ""):lower()
    local subtitlePat    = (params.subtitle or ""):lower()
    local charterPat     = (params.charter  or ""):lower()

    local effFrom = (levelFrom == -1) and -math.huge
                    or (levelFrom + (levelFromPlus and 0.5 or 0))
    local effTo   = (levelToOpenEnd or levelTo == -1) and math.huge
                    or (levelTo + (levelToPlus and 0.5 or 0))

    local function isAcceptedDiff(i)
        if diff == -1 then return true end
        if diff == 34 then return i == 3 or i == 4 end
        return i == diff
    end

    return function(node)
        if not node.IsSong then return false end
        if titlePat    ~= "" and not (node.Title    or ""):lower():find(titlePat,    1, true) then return false end
        if subtitlePat ~= "" and not (node.Subtitle or ""):lower():find(subtitlePat, 1, true) then return false end

        local accepted = {}
        for i = 0, 4 do
            if isAcceptedDiff(i) then
                local chart = node:GetChart(i)
                if chart ~= nil then accepted[#accepted + 1] = chart end
            end
        end
        if #accepted == 0 then return false end

        local levelOk = false
        for _, chart in ipairs(accepted) do
            local eff = chart.Level + (chart.IsPlus and 0.5 or 0)
            if eff >= effFrom and eff <= effTo then levelOk = true; break end
        end
        if not levelOk then return false end

        if charterPat ~= "" then
            local ok = false
            for _, chart in ipairs(accepted) do
                if (chart.NotesDesigner or ""):lower():find(charterPat, 1, true) then ok = true; break end
            end
            if not ok then return false end
        end

        return true
    end
end

-- ── Song collection ───────────────────────────────────────────────────────────

local function collectSongs(folderNode, predicate, results)
    for i = 0, folderNode.ChildrenCount - 1 do
        local node = folderNode:Child(i)
        if node.IsSong then
            if predicate(node) then results[#results + 1] = node end
        elseif node.IsFolder then
            collectSongs(node, predicate, results)
        end
    end
end

-- ── Init ──────────────────────────────────────────────────────────────────────

function M.init(g)
    G = g
end

-- ── Search application ────────────────────────────────────────────────────────
-- Call once per frame when sort_search_dialog is inactive.
-- Returns true if a search was applied (caller should skip its own sort/refresh).

function M.checkSearchReady()
    if G.songList == nil then return false end
    local sav = GetSaveFile(G.highlightedPlayer)
    if math.floor(sav:GetGlobalCounter("ss_search_ready") + 0.5) ~= 1 then return false end
    sav:SetGlobalCounter("ss_search_ready", 0)

    local params = {
        diff           = math.floor(sav:GetGlobalCounter("ss_diff")      + 0.5),
        levelFrom      = math.floor(sav:GetGlobalCounter("ss_levelFrom") + 0.5),
        levelFromPlus  = sav:GetGlobalCounter("ss_levelFromP")  > 0.5,
        levelTo        = math.floor(sav:GetGlobalCounter("ss_levelTo")   + 0.5),
        levelToPlus    = sav:GetGlobalCounter("ss_levelToP")    > 0.5,
        levelToOpenEnd = sav:GetGlobalCounter("ss_levelToOE")   > 0.5,
        title          = SHARED:GetSharedString("ss_title"),
        subtitle       = SHARED:GetSharedString("ss_subtitle"),
        charter        = SHARED:GetSharedString("ss_charter"),
    }

    -- Determine search scope: current folder or root if at root level
    local ssn        = G.songList:GetSelectedSongNode()
    local baseFolder = (ssn ~= nil and not ssn.IsRoot) and ssn.Parent or G.songList:GetRoot()

    local predicate = buildPredicate(params)
    local results   = {}
    if baseFolder ~= nil then
        collectSongs(baseFolder, predicate, results)
    end

    if #results > 0 then
        G.songList:OpenVirtualFolder(baseFolder, results, "Search Results")
        G.applySort()
        G.navRefreshPage()
        G.sounds.Decide:Play()
    else
        G.sounds.Cancel:Play()
    end

    return true
end

return M
