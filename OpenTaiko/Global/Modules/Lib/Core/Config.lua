---@diagnostic disable: undefined-global, undefined-field
-- Core.Config: a module's JSON config files, read once into plain Lua tables
-- name the tables Layout / Animation (all-caps names are engine globals)

local Config = {}

local cache = {}

local function warn(msg)
    if debugLog then debugLog("[Config] " .. msg) end
end

-- "Config/layout.json: bar.fill" from the file name and the keys so far
local function child(path, k)
    return path:sub(-1) == ":" and (path .. " " .. tostring(k)) or (path .. "." .. tostring(k))
end

-- an object warns about the keys it lacks; numeric keys read as nil so ipairs-style probing stays safe
local function strict(path)
    local warned = {}
    return {
        __index = function(_, k)
            if type(k) ~= "string" then return nil end
            if not warned[k] then
                warned[k] = true
                warn(child(path, k) .. " is missing")
            end
            return 0
        end,
    }
end

local function isDictionary(v)
    if type(v) ~= "userdata" then return false end
    local ok, has = pcall(function() return v.GetEnumerator ~= nil end)
    return ok and has
end

local function convert(v, path)
    if type(v) == "number" then return math.tointeger(v) or v end
    if not isDictionary(v) then return v end
    local out, array = {}, nil
    local e = v:GetEnumerator()
    while e:MoveNext() do
        local kvp = e.Current
        local k = kvp.Key
        if array == nil then array = type(k) == "number" end
        if array then k = math.tointeger(k) or k end
        out[k] = convert(kvp.Value, child(path, k))
    end
    -- an empty value has no keys to tell an array from an object: it stays a plain table
    if array == false then setmetatable(out, strict(path)) end
    return out
end

-- the file's values (cached per module); name is relative to the module's folder
-- a missing key warns once (file and key path) and reads 0; a missing file warns and reads empty
-- arrays are plain sequences; whole numbers come back as integers
function Config.load(name)
    if cache[name] then return cache[name] end
    local ok, doc = pcall(function() return JSONLOADER:JsonParseFileAny(name) end)
    local data
    if not ok then
        warn(name .. " could not be read: " .. tostring(doc))
    elseif doc == nil then
        warn(name .. " was not found")
    else
        data = convert(doc, name .. ":")
    end
    if type(data) ~= "table" then data = setmetatable({}, strict(name .. ":")) end
    cache[name] = data
    return data
end

-- a key read without the missing-key warning, for the few values a module treats as optional
function Config.optional(t, key)
    return rawget(t, key)
end

-- plain deep copy without the missing-key warning, for a library testing `opts.x or default`
local function plain(t)
    if type(t) ~= "table" then return t end
    local out = {}
    for k, v in pairs(t) do out[k] = plain(v) end
    return out
end
Config.plain = plain

-- [r, g, b] or [r, g, b, a], each 0-255
function Config.color(c)
    return COLOR:CreateColorFromRGBA(c[1] or 0, c[2] or 0, c[3] or 0, c[4] or 255)
end

return Config
