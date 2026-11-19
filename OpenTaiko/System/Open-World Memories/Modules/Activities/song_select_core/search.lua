---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local
-- song_select_core search: glue between sort_search_dialog and the song list.

local SongSearch = require("SongSearch")

local M = {}
local G   -- shared state injected by Script.lua

function M.init(g)
    G = g
end

-- picks up the dialog's parameters (Lib/SongSearch, ss_search_ready), searches the current folder
-- and opens the results as a virtual folder
-- once per frame while sort_search_dialog is inactive; true when a search was applied
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
