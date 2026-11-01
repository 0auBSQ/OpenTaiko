---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local
-- search.lua — Glue between sort_search_dialog and the song list.
--
-- The dialog hands the search parameters over through the player's save file (Lib/SongSearch.write) and
-- raises ss_search_ready. M.checkSearchReady() picks them up, runs the search (Lib/SongSearch, the same
-- predicate the dialog counts with) against the current folder, and opens a virtual folder with the
-- results. Returns true if a search was applied (callers can skip their own post-dialog logic).

local SongSearch = require("SongSearch")

local M = {}
local G   -- shared state injected by Script.lua

function M.init(g)
    G = g
end

-- Call once per frame when sort_search_dialog is inactive.
function M.checkSearchReady()
    if G.songList == nil then return false end
    local sav = GetSaveFile(G.highlightedPlayer)
    if math.floor(sav:GetGlobalCounter("ss_search_ready") + 0.5) ~= 1 then return false end
    sav:SetGlobalCounter("ss_search_ready", 0)

    -- the search scope: the current folder, or the root at the top level
    local ssn        = G.songList:GetSelectedSongNode()
    local baseFolder = (ssn ~= nil and not ssn.IsRoot) and ssn.Parent or G.songList:GetRoot()

    local results = {}
    if baseFolder ~= nil then
        SongSearch.collectSongs(baseFolder, SongSearch.buildPredicate(SongSearch.read(sav)), results)
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
