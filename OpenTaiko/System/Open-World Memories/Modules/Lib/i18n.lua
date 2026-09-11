---@diagnostic disable: undefined-global, undefined-field, lowercase-global
-- i18n.lua — per-stage localization (shared; each stage's Lua VM gets its own instance).
--
-- Texts live in lang/<code>/<name>.json, one file per feature (computer.json, phone.json, dialogs.json...),
-- keyed by stable ids; lang/en holds the English and is the fallback for every other language. A module
-- takes its file once, `local T = I18N.texts("phone")`, then T:tr("menu_join"), T:trf("no_answer", n),
-- T:get(path...), T:list(path...) (arrays, line by line) and T:loc(path...) (a GetString handle resolved at
-- call time). The current game language is read through the engine's CLocalizationData; call detect() in
-- activate (it runs on every entry, so a language change applies on the next visit). A reloadLanguage()
-- global is only needed by a module that stays on screen while the language changes.
--
-- Older stages keep the exact-English-key dictionary: lang/<code>.lua returns a table and M.tr(s) maps the
-- English text in the code to its translation.

local M = {}

M.code = "en"        -- the game's language code (the Lang/ folder name: en, ja, fr, es, de, ko, nl, ru, zh)
M.lang = "default"   -- the loaded dictionary's language, "default" when none is loaded
local dict = {}      -- the legacy Lua dictionary of the loaded language

-- the engine's language codes, keyed on themselves so CLocalizationData:GetString returns the active one
local CODES = { "de", "en", "es", "fr", "ja", "ko", "nl", "ru", "zh" }

local function currentCode()
    local parts = {}
    for _, c in ipairs(CODES) do parts[#parts + 1] = '"' .. c .. '":"' .. c .. '"' end
    local ok, res = pcall(function()
        return LANG:FromString("{" .. table.concat(parts, ",") .. ',"default":"en"}'):GetString("en")
    end)
    return (ok and type(res) == "string" and res ~= "") and res or "en"
end

-- select a language (also used directly by the headless harnesses): the JSON texts follow M.code on
-- every lookup; a legacy Lua dictionary is loaded here when the stage ships one
function M.load(lang)
    M.code = (lang == "default" or lang == nil) and "en" or lang
    dict = {}
    M.lang = "default"
    if M.code == "en" then return end
    local ok, d = pcall(require, "lang/" .. M.code)
    if ok and type(d) == "table" then
        M.lang = M.code
        dict = d
    end
end

function M.detect()
    M.load(currentCode())
end

-- ── per-language JSON texts ─────────────────────────────────────────────────────────────────────
-- lang/<code>/<name>.json mirrors the structure of lang/en/<name>.json; every lookup tries the current
-- language first, then English. Documents are parsed once per language.
local docs = {}   -- name -> code -> document | false
local function jsonDoc(name, code)
    local per = docs[name]
    if per == nil then per = {}; docs[name] = per end
    local d = per[code]
    if d == nil then
        local ok, doc = pcall(function() return JSONLOADER:JsonParseFileAny("lang/" .. code .. "/" .. name .. ".json") end)
        d = (ok and doc ~= nil) and doc or false
        per[code] = d
    end
    return d or nil
end

local function walk(doc, path)
    local node = doc
    for i = 1, #path do
        if node == nil then return nil end
        local ok, nxt = pcall(function() return JSONLOADER:JsonGet(node, path[i]) end)
        if not ok then return nil end
        node = nxt
    end
    return node
end

local Text = {}
Text.__index = Text

-- the node at the key path in the current language, else in English, else nil
function Text:node(...)
    local path = { ... }
    local n = walk(jsonDoc(self.name, M.code), path)
    if n == nil and M.code ~= "en" then n = walk(jsonDoc(self.name, "en"), path) end
    return n
end

-- the string at the key path, nil when no language has it
function Text:get(...)
    local n = self:node(...)
    if type(n) == "string" and n ~= "" then return n end
    return nil
end

-- the array of strings at the key path; each index falls back to English (an empty entry counts as missing)
function Text:list(...)
    local path = { ... }
    local cur = walk(jsonDoc(self.name, M.code), path)
    local en = (M.code ~= "en") and walk(jsonDoc(self.name, "en"), path) or nil
    local out, n = {}, 0
    pcall(function()
        n = math.max(cur and JSONLOADER:JsonCount(cur) or 0, en and JSONLOADER:JsonCount(en) or 0)
    end)
    for i = 1, n do
        local s = nil
        if cur then pcall(function() s = JSONLOADER:JsonGet(cur, i) end) end
        if (type(s) ~= "string" or s == "") and en then pcall(function() s = JSONLOADER:JsonGet(en, i) end) end
        if type(s) == "string" and s ~= "" then out[#out + 1] = s end
    end
    return out
end

-- a handle resolved at call time (GetString(fallback), like CLocalizationData), so a name cached at load
-- follows a later language change
function Text:loc(...)
    local path = { ... }
    local text = self
    return { GetString = function(_, fallback) return text:get(table.unpack(path)) or fallback end }
end

-- the text of one id (a missing id shows the id itself, which makes the gap visible)
function Text:tr(id)
    if id == nil then return nil end
    return self:get(id) or id
end

-- the text of one id as a format pattern, formatted with the (already translated) args
function Text:trf(id, ...)
    return string.format(self:tr(id), ...)
end

function M.texts(name)
    return setmetatable({ name = name }, Text)
end

-- optional miss recording (harness coverage audits set trackMisses)
M.trackMisses = false
M._missed = {}

-- legacy dictionary: translate one English string (exact match; nil-safe; unknown → unchanged)
function M.tr(s)
    if s == nil then return nil end
    local v = dict[s]
    if v == nil then
        if M.trackMisses and M.lang ~= "default" then M._missed[s] = true end
        return s
    end
    return v
end

-- translate a format pattern, then format with (already-translated) args
function M.trf(fmt, ...)
    return string.format(M.tr(fmt) or fmt, ...)
end

return M
