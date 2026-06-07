---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local
-- favorites.lua — Per-save favorites storage and virtual folder for song_select_core.
--
-- Storage: LightningDB at Databases/favorites
--   Key   "fav_<saveId>"  →  pipe-separated UniqueId list  e.g. "uid1|uid2|uid3"
--
-- Public API:
--   M.init(g)                            — inject shared state G
--   M.loadDB()                           — open database (call from onStart)
--   M.isFavorite(saveId, uniqueId)       — true if song is favorited
--   M.toggleFavorite(saveId, uniqueId)   — add or remove from favorites
--   M.openFavoritesFolder(player)        — snapshot virtual folder for player and open it

local M = {}
local G

local favDB = nil
-- In-memory mirror (saveId -> set). isFavorite() is called for every song bar every frame; LuaDataStorage:Read
-- opens+closes the whole LightningDB environment per call, so reading the DB per draw tanks the song select to
-- single-digit FPS inside folders. We read each save's set ONCE and keep it in sync on writes (favorites only
-- change in-process via toggleFavorite).
local favCache = {}

function M.init(g)
    G = g
end

function M.loadDB()
    favDB = DATABASE:OpenLocalDatabase("Databases/favorites")
end

-- ── Internal helpers ──────────────────────────────────────────────────────────

-- Returns a set (uid → true) for the given saveId. Cached in memory after the first DB read so the per-frame
-- isFavorite() calls never touch the (expensive, open-per-op) LightningDB again.
local function readFavSet(saveId)
    if favDB == nil then return {} end
    local cached = favCache[saveId]
    if cached ~= nil then return cached end
    local raw = favDB:Read("fav_" .. tostring(saveId)) or ""
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

-- ── Public API ────────────────────────────────────────────────────────────────

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

-- Opens a virtual folder containing a snapshot of the highlighted player's
-- favorites at call time.  Contents are NOT updated in real time.
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
