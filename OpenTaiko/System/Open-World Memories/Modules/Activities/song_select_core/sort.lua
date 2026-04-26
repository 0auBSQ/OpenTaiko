---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local, cast-local-type
-- sort.lua  —  Song sorting sub-module for song_select_core
-- Reads sort state from the highlighted player's save file and reorders
-- the current folder's Children list in-place.

local M = {}
local G   -- shared state injected by Script.lua

local SORT_METHOD_KEY = "ss_sort_method"
local SORT_DIR_KEY    = "ss_sort_dir"

function M.init(g)
    G = g
end

-- ── Internal ─────────────────────────────────────────────────────────────────

local function captureOriginalOrder(folderNode)
    if G.originalOrders[folderNode] ~= nil then return end
    local children = folderNode.Children
    local snap = {}
    for i = 0, children.Count - 1 do
        snap[i + 1] = children[i]
    end
    G.originalOrders[folderNode] = snap
end

-- ── Public ───────────────────────────────────────────────────────────────────

function M.applySort()
    if G.songList == nil then return end
    local ssn = G.songList:GetSelectedSongNode()
    if ssn == nil or ssn.IsRoot then return end

    local folderNode = ssn.Parent
    if folderNode == nil then return end

    captureOriginalOrder(folderNode)
    local orig = G.originalOrders[folderNode]

    local save       = GetSaveFile(G.highlightedPlayer)
    local methodIdx0 = math.floor(save:GetGlobalCounter(SORT_METHOD_KEY) + 0.5)
    local dirIdx     = math.floor(save:GetGlobalCounter(SORT_DIR_KEY)    + 0.5)
    if methodIdx0 < 0 or methodIdx0 > 6 then methodIdx0 = 0 end
    if dirIdx < 0 or dirIdx > 2         then dirIdx     = 1 end
    if dirIdx == 0 then methodIdx0 = 0; dirIdx = 1 end  -- OFF → filepath ASC

    local children = folderNode.Children

    -- Separate special nodes from sortable regular nodes
    local regular, backs, randoms, origPos = {}, {}, {}, {}
    for idx, node in ipairs(orig) do
        origPos[node] = idx
        if     node.IsReturn then backs[#backs + 1]   = node
        elseif node.IsRandom then randoms[#randoms + 1] = node
        else                       regular[#regular + 1]  = node
        end
    end


    -- Deduce back-box spacing from original positions
    local backFreq = 7
    if #backs > 0 then
        local first, second = -1, -1
        for _, node in ipairs(orig) do
            if node.IsReturn then
                if   first  == -1 then first  = origPos[node]
                elseif second == -1 then second = origPos[node]; break end
            end
        end
        if first ~= -1 and second ~= -1 then backFreq = second - first - 1 end
    end

    -- Build sort keys
    local displayedDiff = CONFIG:GetDefaultCourse(0)

    local function getChart(node)
        if not node.IsSong then return nil end
        local c = node:GetChart(displayedDiff)
        -- Oni/Edit: if one is missing prefer the other before falling back further
        if c == nil and displayedDiff == 4 then c = node:GetChart(3) end
        if c == nil and displayedDiff == 3 then c = node:GetChart(4) end
        -- General fallback: prefer harder difficulties
        if c == nil then
            for d = 4, 0, -1 do c = node:GetChart(d); if c ~= nil then break end end
        end
        return c
    end

    local primary, secondary, tertiary = {}, {}, {}

    for _, node in ipairs(regular) do
        local p, s, t = 0, 0, origPos[node] or 0
        if methodIdx0 == 1 then                        -- title (case-insensitive), 2nd=filepath
            p = (node.Title or ""):lower()
            s = origPos[node] or 0
        elseif methodIdx0 == 2 then                    -- subtitle (case-insensitive), 2nd=filepath
            p = (node.Subtitle or ""):lower()
            s = origPos[node] or 0
        elseif methodIdx0 == 3 then                    -- level (+0.5 for IsPlus), 2nd=filepath
            local c    = getChart(node)
            local lvl  = c and c.Level or -1
            local plus = (c ~= nil and c.IsPlus) and 0.5 or 0
            p = lvl + plus
            s = origPos[node] or 0
        elseif methodIdx0 == 4 then                    -- bpm, 2nd=level, 3rd=filepath
            local c = getChart(node)
            p = c and (c.BaseBPM or -1) or -1
            local c2 = getChart(node)
            s = c2 and c2.Level or -1
            t = origPos[node] or 0
        elseif methodIdx0 == 5 then                    -- best score, 2nd=level, 3rd=filepath
            local c = getChart(node)
            if c ~= nil and node.IsSong then
                local info = c:GetPlayerBestScore(G.highlightedPlayer)
                p = info and info.HighScore or 0
            else p = -1 end
            local c2 = getChart(node)
            s = c2 and c2.Level or -1
            t = origPos[node] or 0
        elseif methodIdx0 == 6 then                    -- clear status, 2nd=best score, 3rd=filepath
            local c = getChart(node)
            if c ~= nil and node.IsSong then
                local info = c:GetPlayerBestScore(G.highlightedPlayer)
                p = info and info.ClearStatus or 0
                s = info and info.HighScore   or 0
            else p = -1; s = -1 end
            t = origPos[node] or 0
        end
        primary[node]   = p
        secondary[node] = s
        tertiary[node]  = t
    end

    -- Split regular into unlocked and locked before sorting
    local unlocked, locked_nodes = {}, {}
    for _, node in ipairs(regular) do
        if node.IsSong and node.IsLocked then
            locked_nodes[#locked_nodes + 1] = node
        else
            unlocked[#unlocked + 1] = node
        end
    end

    local desc = (dirIdx == 2)
    table.sort(unlocked, function(a, b)
        local pa, pb = primary[a],   primary[b]
        local sa, sb = secondary[a], secondary[b]
        local ta, tb = tertiary[a],  tertiary[b]
        if pa ~= pb then if desc then return pa > pb else return pa < pb end end
        if sa ~= sb then if desc then return sa > sb else return sa < sb end end
        if ta ~= tb then return ta < tb end   -- tertiary always ASC
        return false
    end)

    -- Locked songs always go at the bottom, sorted by HiddenIndex ascending
    table.sort(locked_nodes, function(a, b)
        local ha, hb = a.HiddenIndex, b.HiddenIndex
        if ha ~= hb then return ha < hb end
        return (origPos[a] or 0) < (origPos[b] or 0)
    end)

    -- Rebuild children list: unlocked first, then locked
    children:Clear()
    for _, node in ipairs(unlocked)      do children:Add(node) end
    for _, node in ipairs(locked_nodes)  do children:Add(node) end

    -- Re-insert back boxes at original spacing
    local step = backFreq + 1
    for bi, back in ipairs(backs) do
        local pos = (bi - 1) * step
        if pos <= children.Count then children:Insert(pos, back) end
    end

    -- Random boxes at the end
    for _, rand in ipairs(randoms) do children:Add(rand) end
end

return M
