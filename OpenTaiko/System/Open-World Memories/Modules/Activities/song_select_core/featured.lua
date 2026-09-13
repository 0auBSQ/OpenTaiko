---@diagnostic disable: undefined-global, undefined-field, need-check-nil
-- featured.lua — the curated boxes at the top of the root list (Config/featured_boxes.json).
--
-- Each box lists songs by unique id; the ones present in the library go into a virtual box inserted before
-- the first real folder, in the file's order, and a box with none of its songs is skipped. The cursor stays
-- where the list put it (the first real folder), and sort.lua keeps these boxes first.
--
--   { "boxes": [ { "id": "beginners", "genre": "Recommended", "title_key": "SONGSELECT_BOX_BEGINNERS",
--                  "title": "Recommended songs for beginners", "uids": [ "...", ... ] } ] }
--   title_key = a skin locale key (Locales/<code>.json); title = the fallback text.

local M = {}
local G

local function tr(key, fallback)
    if key == nil then return fallback end
    local ok, s = pcall(function() return THEME:GetSkinString(key) end)
    if ok and type(s) == "string" and s ~= "" and s:sub(1, 1) ~= "[" then return s end
    return fallback
end

local function jget(node, key) return JSONLOADER:JsonGet(node, key) end
local function jlist(node, key)
    local v = jget(node, key)
    if v == nil then return {} end
    local out = {}
    for i = 1, JSONLOADER:JsonCount(v) do out[i] = jget(v, i) end
    return out
end

function M.init(g) G = g end

-- the boxes read from the config: { id, genre, title, uids }
function M.load()
    local boxes = {}
    local ok, err = pcall(function()
        local doc = JSONLOADER:JsonParseFileAny("Config/featured_boxes.json")
        if doc == nil then return end
        for _, b in ipairs(jlist(doc, "boxes")) do
            local id = jget(b, "id") or ("box" .. (#boxes + 1))
            local title = jget(b, "title")
            local uids = {}
            for _, u in ipairs(jlist(b, "uids")) do if type(u) == "string" then uids[#uids + 1] = u end end
            boxes[#boxes + 1] = { id = id, genre = jget(b, "genre"), title = tr(jget(b, "title_key"), title or id), uids = uids }
        end
    end)
    if not ok then debugLog("featured boxes: " .. tostring(err)) end
    return boxes
end

-- inserts every box that has at least one available song; call once the song list is (re)built
function M.insertAll()
    if G.songList == nil then return 0 end
    local root = G.songList:GetRoot()
    if root == nil then return 0 end
    local at = 0
    for _, box in ipairs(M.load()) do
        local nodes = {}
        for _, uid in ipairs(box.uids) do
            local node = G.songList:GetSongByUniqueId(uid)
            if node ~= nil then nodes[#nodes + 1] = node end
        end
        if #nodes > 0 then
            local inserted = G.songList:InsertVirtualFolder(root, at, nodes, box.title, box.genre)
            if inserted ~= nil then at = at + 1 end
        end
    end
    return at
end

return M
