-- Shop.Texts: the shop modules' own strings, lang/<code>/shop.json next to this file, in the game's
-- language (English when a language lacks one). "items" holds item names by Items.db3 code.

local I18N = require("Locale.i18n")
local HERE = require("Core.Here")(...)

local T = I18N.texts("shop", HERE)

local M = {}

local cache, cacheCode = {}, nil

-- the game's language, also when the calling stage never picked it up
local function sync()
    if I18N.code ~= I18N.currentCode() then I18N.detect() end
    if cacheCode ~= I18N.code then cache, cacheCode = {}, I18N.code end
end

local function cached(id, ...)
    local s = cache[id]
    if s == nil then
        s = T:get(...) or false
        cache[id] = s
    end
    return s
end

-- the string of key, else fallback
function M.get(key, fallback)
    sync()
    return cached(key, key) or fallback
end

-- the name of the item with this code, else fallback
function M.item(code, fallback)
    sync()
    return cached("items/" .. code, "items", code) or fallback
end

return M
