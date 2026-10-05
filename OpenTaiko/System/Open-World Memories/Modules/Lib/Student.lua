---@diagnostic disable: undefined-global, undefined-field, lowercase-global
-- Lib/Student.lua — the story student the player walks as (A or B) and that student's colour palette.
--
-- The chosen student is the global counter "story_student" (the one Lib/PagodaAvatar reads): 1 = B, and
-- any other value (an unset counter included) reads as A. A student's palette index is the counter its
-- character uses, ".character_palette_<folder>", so the customize tab and every story scene stay in step;
-- A and B each keep their own. The player's own student reads the palettes its character reads: the installed
-- character's Palettes.json (just the default palette when it has none), or the copy in Lib/Student/<id>/ when
-- the character is not installed. A visitor's student (an index picked from another player's list) reads the
-- character's file when there is one, else the copy. An index outside the list reads as the default palette (0).
--
--   local Student = require("Student")
--   local id  = Student.current(save)            -- "a" | "b"
--   local idx = Student.index(save, id)           -- 0-based palette index, always valid
--   Student.withPalette(id, idx, function() tex:Draw(x, y) end)        -- a 2D draw in the palette
--   Student.registerSprite(scene, spriteId, tex, id, idx[, visitor])   -- a 3D billboard in the palette

local S = {}

S.IDS = { "a", "b" }
S.COUNTER = "story_student"

local INFO = {
    a = { folder = "02v2 - Student (A)", value = 0 },
    b = { folder = "03v2 - Student (B)", value = 1 },
}

-- from any Modules/<kind>/<name>/ folder: the skin root, this module's own files and the game's characters
local SKIN_ROOT = "../../../"
local OWN_DIR = SKIN_ROOT .. "Modules/Lib/Student/"
local CHARA_DIR = SKIN_ROOT .. "../../Global/Characters/"

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

-- ── palettes ───────────────────────────────────────────────────────────────────────────────────
local lists = {}           -- "id:own" | "id:visitor" → { {name, plays, blend, stops|nil}, ... } (entry i = index i-1)
local sources = {}         -- the same keys → "character" | "copy" | "none"
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
        local name = jget(e, "name")
        list[#list + 1] = {
            name = (name ~= nil) and tostring(name) or ("Palette " .. i),
            plays = math.max(0, floor(num(jget(e, "plays")) or 0)),
            blend = math.max(0, math.min(1, num(jget(e, "blend")) or 0)),
            stops = parseStops(jget(e, "stops")),
        }
    end
    if #list == 0 then return nil end
    return list
end

-- the game has the student's character: its character list knows the folder, or, before that list is
-- filled, the folder holds the Metadata.json every character carries
local function installed(id)
    local folder = INFO[id].folder
    local ok, known = pcall(function()
        if CHARACTERLIST == nil or (CHARACTERLIST.Count or 0) <= 0 then return nil end
        return CHARACTERLIST:GetByName(folder) ~= nil
    end)
    if ok and known ~= nil then return known end
    local okm, doc = pcall(function() return JSONLOADER:JsonParseFileAny(CHARA_DIR .. folder .. "/Metadata.json") end)
    return okm and doc ~= nil
end

local function listKey(id, visitor) return id .. (visitor and ":visitor" or ":own") end

-- visitor: the list for another player's student (see the header)
function S.palettes(id, visitor)
    id = S.valid(id)
    local key = listKey(id, visitor)
    if lists[key] == nil then
        local list, src = parseFile(CHARA_DIR .. INFO[id].folder .. "/Palettes.json"), "character"
        if list == nil and (visitor or not installed(id)) then
            list, src = parseFile(OWN_DIR .. id .. "/Palettes.json"), "copy"
        end
        if list == nil then
            list, src = { { name = "Default", plays = 0, blend = 0, stops = nil } }, "none"
        end
        lists[key], sources[key] = list, src
    end
    return lists[key]
end

-- where the palette list came from: "character", "copy" or "none"
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

-- stores the palette index on the student's character counter; when that character is the one the
-- save has equipped, its live palette changes too (as the customize tab does on OK)
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
