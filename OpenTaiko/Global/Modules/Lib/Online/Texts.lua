-- Online.Texts: Online strings from lang/<code>/online.json beside this file (English fallback)

local I18N = require("Locale.i18n")
local HERE = require("Core.Here")(...)

local T = I18N.texts("online", HERE)

local M = {}

local cache, cacheCode = {}, nil

-- the game's language, also when the calling stage never picked it up
local function sync()
    if I18N.code ~= I18N.currentCode() then I18N.detect() end
    if cacheCode ~= I18N.code then cache, cacheCode = {}, I18N.code end
end

-- the string of key; the key itself when no language has it
function M.tr(key)
    sync()
    local s = cache[key]
    if s == nil then s = T:tr(key); cache[key] = s end
    return s
end

function M.trf(key, ...) return string.format(M.tr(key), ...) end

return M
