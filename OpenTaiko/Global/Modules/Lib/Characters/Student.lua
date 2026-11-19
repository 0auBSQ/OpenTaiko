---@diagnostic disable: undefined-global, undefined-field, lowercase-global
-- Characters.Student: the story student (A or B) and their palettes

local S = {}

S.IDS = { "a", "b" }
S.COUNTER = "story_student"

local INFO = {
    a = { folder = "02v2 - Student (A)", value = 0 },
    b = { folder = "03v2 - Student (B)", value = 1 },
}

-- Student/<id>/ palette copies beside this file; Global/Characters/ three folders up
local HERE = require("Core.Here")(...)
local OWN_DIR = HERE and (HERE .. "Student/") or nil
local CHARA_DIR = HERE and HERE:match("^(.*/)[^/]+/[^/]+/[^/]+/$")
CHARA_DIR = CHARA_DIR and (CHARA_DIR .. "Characters/") or nil

local floor = math.floor

local function num(v)
    if type(v) == "number" then return v end
    if v == nil then return nil end
    return tonumber(tostring(v))
end

local function jget(node, key)
    local ok, v = pcall(function() return JSONLOADER:JsonGet(node, key) end)
    return ok and v or nil
end

local function jcount(node)
    local ok, n = pcall(function() return JSONLOADER:JsonCount(node) end)
    return (ok and type(n) == "number") and n or 0
end

-- "a" or "b"; anything else is A
function S.valid(id)
    if id == "b" or id == "B" then return "b" end
    return "a"
end

function S.folder(id) return INFO[S.valid(id)].folder end

-- counter story_student: 1 = B, anything else A
function S.current(save)
    if save == nil then return "a" end
    local ok, v = pcall(function() return save:GetGlobalCounter(S.COUNTER) end)
    v = ok and num(v) or nil
    if v == INFO.b.value then return "b" end
    return "a"
end

-- writes Student A into the counter when it holds no student; returns the current student
function S.ensure(save)
    if save == nil then return "a" end
    local ok, v = pcall(function() return save:GetGlobalCounter(S.COUNTER) end)
    v = ok and num(v) or nil
    if v ~= INFO.a.value and v ~= INFO.b.value then
        pcall(function() save:SetGlobalCounter(S.COUNTER, INFO.a.value) end)
        return "a"
    end
    return (v == INFO.b.value) and "b" or "a"
end

function S.set(save, id)
    id = S.valid(id)
    if save ~= nil then pcall(function() save:SetGlobalCounter(S.COUNTER, INFO[id].value) end) end
    return id
end

-- ==== Palettes ====
local lists = {}           -- "id:own" | "id:visitor" → { {name, plays, blend, stops|nil}, ... } (entry i = index i-1)
local sources = {}         -- the same keys → "character" | "merged" | "copy" | "none"
local gradients = {}       -- "source:id:idx" → LuaGradientMap | false

local function parseStops(node)
    local out = {}
    for j = 1, jcount(node) do
        local s = jget(node, j)
        local p, r, g, b = num(jget(s, 1)), num(jget(s, 2)), num(jget(s, 3)), num(jget(s, 4))
        if p and r and g and b then
            local a = num(jget(s, 5))
            out[#out + 1] = a and { p, r, g, b, a } or { p, r, g, b }
        end
    end
    if #out < 2 then return nil end
    return out
end

local function parseFile(path)
    local ok, doc = pcall(function() return JSONLOADER:JsonParseFileAny(path) end)
    if not ok or doc == nil then return nil end
    local list = {}
    for i = 1, jcount(doc) do
        local e = jget(doc, i)
        if e == nil then return nil end
        -- "names" holds the name per language id; "name" is the one shown when a language has none
        local name = jget(e, "name")
        local okLang, lang = pcall(function() return LANG:GetLanguageId() end)
        local own = okLang and jget(jget(e, "names"), lang) or nil
        if own ~= nil and tostring(own) ~= "" then name = own end
        list[#list + 1] = {
            key = tostring(jget(e, "name") or ""),
            name = (name ~= nil) and tostring(name) or ("Palette " .. i),
            plays = math.max(0, floor(num(jget(e, "plays")) or 0)),
            blend = math.max(0, math.min(1, num(jget(e, "blend")) or 0)),
            stops = parseStops(jget(e, "stops")),
        }
    end
    if #list == 0 then return nil end
    return list
end

-- the character's palettes, then the copy's it lacks (by name); returns list, source
local function merged(own, copy)
    if own == nil then return copy, copy and "copy" or "none" end
    if copy == nil then return own, "character" end
    local have, src = {}, "character"
    for _, e in ipairs(own) do have[e.key] = true end
    for _, e in ipairs(copy) do
        if not have[e.key] then
            own[#own + 1] = e
            src = "merged"
        end
    end
    return own, src
end

local function listKey(id, visitor) return id .. (visitor and ":visitor" or ":own") end

-- visitor: the list for another player's student
function S.palettes(id, visitor)
    id = S.valid(id)
    local key = listKey(id, visitor)
    if lists[key] == nil then
        local list, src = merged(CHARA_DIR and parseFile(CHARA_DIR .. INFO[id].folder .. "/Palettes.json") or nil,
                                 OWN_DIR and parseFile(OWN_DIR .. id .. "/Palettes.json") or nil)
        if list == nil then
            list, src = { { name = "Default", plays = 0, blend = 0, stops = nil } }, "none"
        end
        lists[key], sources[key] = list, src
    end
    return lists[key]
end

-- where the palette list came from: "character", "merged", "copy" or "none"
function S.paletteSource(id, visitor)
    S.palettes(id, visitor)
    return sources[listKey(S.valid(id), visitor)]
end

function S.count(id, visitor) return #S.palettes(id, visitor) end

-- an index that exists in the student's list, else 0 (the default palette)
function S.clamp(id, idx, visitor)
    idx = num(idx)
    if idx == nil or idx ~= idx then return 0 end
    idx = floor(idx)
    if idx < 0 or idx >= S.count(id, visitor) then return 0 end
    return idx
end

function S.entry(id, idx, visitor)
    local list = S.palettes(id, visitor)
    return list[S.clamp(id, idx, visitor) + 1]
end

-- palette index counter, shared with the customize tab
local function paletteKey(id) return ".character_palette_" .. S.folder(id) end

function S.index(save, id)
    if save == nil then return 0 end
    local ok, v = pcall(function() return save:GetGlobalCounter(paletteKey(id)) end)
    return S.clamp(id, ok and v or 0)
end

function S.playCount(save, id)
    if save == nil then return 0 end
    local ok, v = pcall(function() return save:GetGlobalCounter(".character_playcount_" .. S.folder(id)) end)
    return floor((ok and num(v)) or 0)
end

-- the same rule as the customize tab: no play requirement, or enough plays with that character
function S.unlocked(save, id, idx)
    local e = S.palettes(id)[(num(idx) or -1) + 1]
    if e == nil then return false end
    return e.plays <= 0 or S.playCount(save, id) >= e.plays
end

-- saves the palette index; also sets the live palette when that character is equipped
function S.setIndex(save, id, idx)
    id = S.valid(id)
    idx = S.clamp(id, idx)
    if save == nil then return idx end
    pcall(function() save:SetGlobalCounter(paletteKey(id), idx) end)
    pcall(function()
        if save.CharacterName ~= S.folder(id) then return end
        local ch = save:GetCharacter()
        if ch == nil then return end
        local e = S.entry(id, idx)
        if e.stops then ch:SetPaletteGradient(e.stops, e.blend) else ch:ClearPaletteGradient() end
    end)
    return idx
end

-- the palette as a GRADIENT map, or nil for a palette without colours (the default one)
function S.gradient(id, idx, visitor)
    id = S.valid(id)
    idx = S.clamp(id, idx, visitor)
    local key = S.paletteSource(id, visitor) .. ":" .. id .. ":" .. idx
    if gradients[key] == nil then
        local e = S.entry(id, idx, visitor)
        local gm = false
        if e.stops and e.blend > 0 then
            local ok, made = pcall(function() return GRADIENT:Create(e.stops, e.blend) end)
            if ok and made then gm = made end
        end
        gradients[key] = gm
    end
    return gradients[key] or nil
end

-- runs fn (2D draws) with the palette active; returns pcall's results
function S.withPalette(id, idx, fn, ...)
    local gm = S.gradient(id, idx)
    if gm then pcall(function() GRADIENT:SetActive(gm) end) end
    local res = table.pack(pcall(fn, ...))
    if gm then pcall(function() GRADIENT:ClearActive() end) end
    return table.unpack(res, 1, res.n)
end

-- registers tex as a billboard sprite of a Lua3DScene, recoloured in the palette
function S.registerSprite(scene, spriteId, tex, id, idx, visitor)
    if scene == nil or tex == nil then return false end
    local gm = S.gradient(id, idx, visitor)
    if gm and pcall(function() scene:RegisterSpriteFromTextureGradient(spriteId, tex, gm) end) then return true end
    return (pcall(function() scene:RegisterSpriteFromTexture(spriteId, tex) end))
end

-- forget the parsed palette lists (the next query reads the files again)
function S.reload()
    lists, sources, gradients = {}, {}, {}
end

return S
