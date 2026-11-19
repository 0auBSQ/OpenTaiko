---@diagnostic disable: undefined-global, undefined-field, lowercase-global
-- Locale.i18n: per-stage localization, one instance per stage Lua VM
-- texts in lang/<code>/<name>.json by stable ids, lang/en as the fallback

local M = {}

M.code = "en"        -- game language code (Lang/ folder name)
M.lang = "default"   -- the loaded dictionary's language, "default" when none is loaded
local dict = {}      -- Lua dictionary of the loaded language

-- language id in use; "en" without an engine (headless harness)
local function currentCode()
    local ok, res = pcall(function() return LANG:GetLanguageId() end)
    return (ok and type(res) == "string" and res ~= "") and res or "en"
end

M.currentCode = currentCode

-- art with baked text: "<name>_<code>.<ext>" when that variant exists next to `path`, else `path`
-- (the engine's rule for its own textures); relative to the stage folder
function M.localizedPath(path, code)
    code = code or currentCode()
    local base, ext = path:match("^(.*)(%.[^%./\\]+)$")
    if base == nil then return path end
    local lp = base .. "_" .. code .. ext
    local ok, found = pcall(function() return TEXTURE:Exists(lp) end)
    if ok and found then return lp end
    return path
end

-- selects a language (also used by headless harnesses); JSON texts follow M.code per lookup
-- loads the Lua dictionary when the stage ships one
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

-- call in activate (a language change applies on the next entry)
-- a module on screen during a language change also needs a reloadLanguage() global
function M.detect()
    M.load(currentCode())
end

-- ==== JSON texts ====
-- lang/<code>/<name>.json mirrors lang/en/<name>.json; lookups try the current language, then English
-- documents parsed once per language
local docs = {}   -- folder .. name -> code -> document | false
local function jsonDoc(name, code, dir)
    local key = (dir or "") .. name
    local per = docs[key]
    if per == nil then per = {}; docs[key] = per end
    local d = per[code]
    if d == nil then
        local ok, doc = pcall(function() return JSONLOADER:JsonParseFileAny((dir or "") .. "lang/" .. code .. "/" .. name .. ".json") end)
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
    local n = walk(jsonDoc(self.name, M.code, self.dir), path)
    if n == nil and M.code ~= "en" then n = walk(jsonDoc(self.name, "en", self.dir), path) end
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
    local cur = walk(jsonDoc(self.name, M.code, self.dir), path)
    local en = (M.code ~= "en") and walk(jsonDoc(self.name, "en", self.dir), path) or nil
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

-- handle resolved at call time (GetString(fallback), like CLocalizationData), so cached names follow language changes
function Text:loc(...)
    local path = { ... }
    local text = self
    return { GetString = function(_, fallback) return text:get(table.unpack(path)) or fallback end }
end

-- text of one id (a missing id shows the id, making the gap visible)
function Text:tr(id)
    if id == nil then return nil end
    return self:get(id) or id
end

-- the text of one id as a format pattern, formatted with the (already translated) args
function Text:trf(id, ...)
    return string.format(self:tr(id), ...)
end

-- dir: folder holding lang/ (Core.Here for a Lib module); "" or nil for the caller's folder
function M.texts(name, dir)
    return setmetatable({ name = name, dir = dir }, Text)
end

-- optional miss recording (harness coverage audits set trackMisses)
M.trackMisses = false
M._missed = {}

-- Lua dictionary (lang/<code>.lua): exact English string -> translation; unknown stays unchanged
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
