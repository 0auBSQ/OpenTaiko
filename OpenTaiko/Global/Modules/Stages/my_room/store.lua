---@diagnostic disable: undefined-global, undefined-field
-- My Room save state: one JSON string per save, global string "my_room"
--   { "v": 1, "room": <Room:toTable()>, "jukebox": <JB.persistState(), absent while nothing plays> }

local Json = require("Core.Json")

local Store = {}

Store.KEY = "my_room"
Store.VERSION = 1

local floor = math.floor

-- ==== saved fields ====
-- only these fields are written and read back
local function num(v)
    v = tonumber(v)
    if v == nil or v ~= v or v == math.huge or v == -math.huge then return nil end
    return v
end
local function int(v) v = num(v); return v and floor(v) or nil end
local function str(v) return type(v) == "string" and v or nil end
local function bool(v) if v == true or v == false then return v end return nil end

local function map(t, value)
    if type(t) ~= "table" then return nil end
    local out = {}
    for k, v in pairs(t) do
        if type(k) == "string" then out[k] = value(v) end
    end
    return out
end
local function onlyTrue(v) return v == true or nil end
local function hex(v) return type(v) == "string" and v:match("^%x%x%x%x%x%x$") and v:lower() or nil end

local FURNITURE = { id = str, c = int, r = int, facing = int, on = bool, lit = bool, tvOn = bool, hex = hex }
local WALL_ITEM = { id = str, c = int, r = int, mount = str }
local ROOM = { v = int, tier = int, iw = int, ih = int, exitCol = int, win = int }
local JUKEBOX = { kind = str, md5 = str, uid = str, title = str, genre = str, pos = num, speed = num,
                  rep = bool, c = int, r = int, paused = bool }

local function fields(t, spec)
    local out = {}
    for k, f in pairs(spec) do out[k] = f(t[k]) end
    return out
end

local function list(t, spec)
    local out = {}
    if type(t) ~= "table" then return out end
    for _, e in ipairs(t) do
        if type(e) == "table" and type(e.id) == "string" then out[#out + 1] = fields(e, spec) end
    end
    return out
end

function Store.cleanRoom(t)
    if type(t) ~= "table" then return nil end
    local out = fields(t, ROOM)
    out.furniture = list(t.furniture, FURNITURE)
    out.wallItems = list(t.wallItems, WALL_ITEM)
    out.floorDeco = map(t.floorDeco, str)
    out.wallPaint = map(t.wallPaint, str)
    out.inventory = map(t.inventory, num)
    out.claimed = map(t.claimed, num)
    out.gifts = map(t.gifts, onlyTrue)
    return out
end

function Store.cleanJukebox(t)
    if type(t) ~= "table" or str(t.kind) == nil then return nil end
    return fields(t, JUKEBOX)
end

local function hasSave(sf) return sf ~= nil and type(sf.SaveUID) == "string" and sf.SaveUID ~= "" end

-- -> room|nil, jukebox|nil, the stored string ("" when unset)
function Store.read(sf)
    if sf == nil then return nil, nil, "" end
    local ok, s = pcall(function() return sf:GetGlobalString(Store.KEY) end)
    if not ok or type(s) ~= "string" or s == "" then return nil, nil, "" end
    local doc = Json.decode(s)
    if type(doc) ~= "table" then return nil, nil, s end
    return Store.cleanRoom(doc.room), Store.cleanJukebox(doc.jukebox), s
end

function Store.encode(room, jukebox)
    return Json.encode({ v = Store.VERSION, room = Store.cleanRoom(room), jukebox = Store.cleanJukebox(jukebox) })
end

-- true when written; false for an empty slot
function Store.write(sf, room, jukebox)
    if not hasSave(sf) or type(room) ~= "table" then return false end
    local text = Store.encode(room, jukebox)
    return (pcall(function() sf:SetGlobalString(Store.KEY, text) end))
end

-- ==== legacy OWM store (temporary) ====
-- OWM myroom LMDB store: Lua table source under room_<SaveUID> and jukebox_<SaveUID> (or slot<n>)
-- path relative to this folder; DATABASE remaps it for writing (iOS)
local LEGACY_DIR = "../../../../System/Open-World Memories/Modules/Stages/myroom/myroom"
local LEGACY_MAX = 1048576

-- plain table source only: no parentheses, no names beyond LEGACY_WORDS, so nothing can run
local LEGACY_WORDS = { ["true"] = true, ["false"] = true, ["nil"] = true, inf = true, nan = true }
local function stripStrings(s)
    local out, i, n = {}, 1, #s
    while i <= n do
        local c = s:sub(i, i)
        if c == '"' then
            local j = i + 1
            while j <= n do
                local d = s:sub(j, j)
                if d == "\\" then j = j + 2
                elseif d == '"' then break
                else j = j + 1 end
            end
            if j > n then return nil end
            out[#out + 1] = '""'
            i = j + 1
        else
            out[#out + 1] = c
            i = i + 1
        end
    end
    return table.concat(out)
end

function Store.legacyValue(s)
    if type(s) ~= "string" or s == "" or #s > LEGACY_MAX then return nil end
    local rest = stripStrings(s)
    if rest == nil or rest:find("[^%w%s%.%-%+{}%[%]=,\"_]") then return nil end
    local bare = rest:gsub("%d+%.?%d*[eE][%+%-]?%d+", "0"):gsub("%d+%.?%d*", "0")
    for w in bare:gmatch("[%a_][%w_]*") do
        if not LEGACY_WORDS[w] then return nil end
    end
    local f = load("return " .. s, "=my_room_legacy", "t", {})
    if f == nil then return nil end
    local ok, v = pcall(f)
    if ok and type(v) == "table" then return v end
    return nil
end

-- -> room|nil, jukebox|nil
function Store.legacy(sf, slot)
    if not hasSave(sf) then return nil, nil end
    local okE, exists = pcall(function() return STORAGE:FileExists(LEGACY_DIR .. "/data.mdb") end)
    if not okE or exists ~= true then return nil, nil end
    local db = nil
    pcall(function() db = DATABASE:OpenLocalDatabase(LEGACY_DIR) end)
    if db == nil then return nil, nil end
    local function get(prefix)
        for _, key in ipairs({ prefix .. sf.SaveUID, prefix .. "slot" .. tostring(slot or 0) }) do
            local v = nil
            pcall(function() v = db:Read(key) end)
            if type(v) == "string" and v ~= "" then return v end
        end
        return nil
    end
    local room = Store.cleanRoom(Store.legacyValue(get("room_")))
    local jukebox = Store.cleanJukebox(Store.legacyValue(get("jukebox_")))
    pcall(function() db:Dispose() end)
    return room, jukebox
end

return Store
