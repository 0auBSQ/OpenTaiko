---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local
-- song_select_core favorites: per-save storage and the virtual folder.
-- global LightningDB SongFavorites: key "fav_<saveId>" -> pipe-separated UniqueId list

local M = {}
local G

local FAV_DB = "SongFavorites"
local OLD_DB = "Databases/favorites"   -- module-local store, deleted by a skin update

local favDB = nil
local oldDB = nil
-- saveId -> set mirror: isFavorite() runs per bar per frame, LuaDataStorage:Read opens the DB per call
-- read once per save, kept in sync on writes (only toggleFavorite changes it)
local favCache = {}

function M.init(g)
    G = g
end

-- call from onStart
function M.loadDB()
    favDB = DATABASE:OpenGlobalDatabase(FAV_DB)
    if STORAGE:FileExists(OLD_DB .. "/data.mdb") then oldDB = DATABASE:OpenLocalDatabase(OLD_DB) end
end

-- ==== Internal helpers ====

-- set (uid -> true) for saveId, cached after the first DB read
local function readFavSet(saveId)
    if favDB == nil then return {} end
    local cached = favCache[saveId]
    if cached ~= nil then return cached end
    local key = "fav_" .. tostring(saveId)
    local raw = favDB:Read(key)
    -- no global entry yet: the old store's list, copied once
    if raw == nil and oldDB ~= nil then
        raw = oldDB:Read(key)
        if raw ~= nil then favDB:Write(key, raw) end
    end
    raw = raw or ""
    local set = {}
    for uid in raw:gmatch("[^|]+") do
        set[uid] = true
    end
    favCache[saveId] = set
    return set
end

-- Writes a set back to the DB and refreshes the in-memory mirror.
local function writeFavSet(saveId, set)
    if favDB == nil then return end
    favCache[saveId] = set
    local parts = {}
    for uid in pairs(set) do
        parts[#parts + 1] = uid
    end
    favDB:Write("fav_" .. tostring(saveId), table.concat(parts, "|"))
end

-- ==== Public API ====

function M.isFavorite(saveId, uniqueId)
    if favDB == nil or uniqueId == nil then return false end
    local set = readFavSet(saveId)
    return set[uniqueId] == true
end

function M.toggleFavorite(saveId, uniqueId)
    if favDB == nil or uniqueId == nil then return end
    local set = readFavSet(saveId)
    if set[uniqueId] then
        set[uniqueId] = nil
    else
        set[uniqueId] = true
    end
    writeFavSet(saveId, set)
end

-- virtual folder with a snapshot of the player's favorites (not updated live)
function M.openFavoritesFolder(player)
    if G.songList == nil or favDB == nil then
        G.sounds.Cancel:Play()
        return
    end

    local saveId = GetSaveFile(player).SaveId
    local set    = readFavSet(saveId)

    local nodes = {}
    for uid in pairs(set) do
        local node = G.songList:GetSongByUniqueId(uid)
        if node ~= nil then
            nodes[#nodes + 1] = node
        end
    end

    if #nodes == 0 then
        G.sounds.Cancel:Play()
        return
    end

    local ssn        = G.songList:GetSelectedSongNode()
    local baseFolder = (ssn ~= nil and not ssn.IsRoot) and ssn.Parent or G.songList:GetRoot()
    G.songList:OpenVirtualFolder(baseFolder, nodes, "Favorites")
    G.applySort()
    G.navRefreshPage()
    G.sounds.Decide:Play()
end

return M
