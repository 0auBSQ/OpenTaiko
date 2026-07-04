---@diagnostic disable: undefined-global, undefined-field
-- sscore_config.lua  —  Loads Config/layout.json once so skinners can tweak song-select-core layout
-- (positions, offsets, sizes, animation, colours) WITHOUT editing the Lua. Every accessor takes the
-- in-code default as its last argument and returns it whenever the file/key is missing or malformed, so
-- an absent or partial Config/layout.json is always safe.

local M = {}

-- Parse once at load. JSONLOADER is injected before the modules are required (same as COLOR), and
-- JsonParseFileAny returns nil (never throws) when the file is absent. pcall guards any other surprise.
local data = nil
pcall(function() data = JSONLOADER:JsonParseFileAny("Config/layout.json") end)

-- Walk a dotted path ("difficulty_select.note.base_x") through the parsed object/array tree.
local function get(path)
    if data == nil then return nil end
    local node = data
    for key in string.gmatch(path, "[^.]+") do
        node = JSONLOADER:JsonGet(node, key)
        if node == nil then return nil end
    end
    return node
end

function M.num(path, default)
    local v = get(path)
    if type(v) == "number" then return v end
    return default
end

function M.str(path, default)
    local v = get(path)
    if type(v) == "string" then return v end
    return default
end

-- Hex string ("AARRGGBB") → LuaColor, else the provided default LuaColor.
function M.color(path, default)
    local v = get(path)
    if type(v) == "string" then return COLOR:CreateColorFromHex(v) end
    return default
end

-- JSON array of hex strings → array of LuaColor, else the provided defaults array.
function M.colorList(path, defaults)
    local node = get(path)
    if node == nil then return defaults end
    local out, i = {}, 1
    while true do
        local v = JSONLOADER:JsonGet(node, i)
        if type(v) ~= "string" then break end
        out[i] = COLOR:CreateColorFromHex(v)
        i = i + 1
    end
    if #out == 0 then return defaults end
    return out
end

return M
