-- Pagoda.Songs: the song list the pagoda draws from, and the check that the installed soundtrack covers
-- every pool entry (the pools name songs of the OpenTaiko soundtrack by unique id).

local Levels = require("Pagoda.Levels")

local M = {}

local _song_list_cache = nil

function M.clear_cache() _song_list_cache = nil end

-- ── Song list ────────────────────────────────────────────────────────────────

function M.get_song_list()
    if _song_list_cache ~= nil then return _song_list_cache end
    local lsls = GenerateSongListSettings()
    lsls.AppendMainRandomBox  = false
    lsls.AppendSubRandomBoxes = false
    lsls.FlattenOpenedFolders = true
    _song_list_cache = RequestSongList(lsls)
    return _song_list_cache
end

-- ── Pool validation ──────────────────────────────────────────────────────────

-- how many pool entries (song + difficulty) the installed songs lack; each one is logged
function M.count_missing_songs()
    local pools = Levels.load_pools()
    if pools == nil then return 0 end

    local lsls = GenerateSongListSettings()
    lsls.AppendMainRandomBox  = false
    lsls.AppendSubRandomBoxes = false
    lsls.FlattenOpenedFolders = true
    lsls.ExcludeHiddenSongs   = false
    local sl = RequestSongList(lsls)

    local diff_names = { [0] = "Easy", [1] = "Normal", [2] = "Hard", [3] = "Oni", [4] = "Edit" }

    local seen    = {}
    local missing = 0
    for _, pool in pairs(pools) do
        for _, color in ipairs(Levels.COLORS) do
            local entries = pool[color]
            if entries ~= nil then
                for _, entry in ipairs(entries) do
                    local key = (entry.id or "") .. "\0" .. tostring(entry.diff)
                    if not seen[key] then
                        seen[key] = true
                        local node  = sl:GetSongByUniqueId(entry.id)
                        local diff  = diff_names[entry.diff] or tostring(entry.diff)
                        if node == nil then
                            debugLog("[Pagoda] Missing song – id: " .. tostring(entry.id) .. "  diff: " .. diff)
                            missing = missing + 1
                        elseif node:GetChart(entry.diff) == nil then
                            local title = node.Title or entry.id
                            debugLog("[Pagoda] Missing difficulty – \"" .. title .. "\"  id: " .. tostring(entry.id) .. "  diff: " .. diff)
                            missing = missing + 1
                        end
                    end
                end
            end
        end
    end
    return missing
end

return M
