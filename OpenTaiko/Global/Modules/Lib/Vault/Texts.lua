-- Vault.Texts: vault strings from lang/<code>/vault.json next to this file (English fallback)

local I18N = require("Locale.i18n")
local HERE = require("Core.Here")(...)

local T = I18N.texts("vault", HERE)

local M = {}

local cache, cacheCode = {}, nil

-- follows the game language even when the stage never detected it
local function sync()
    if I18N.code ~= I18N.currentCode() then I18N.detect() end
    if cacheCode ~= I18N.code then cache, cacheCode = {}, I18N.code end
end

-- the string of key, else fallback
function M.get(key, fallback)
    sync()
    local s = cache[key]
    if s == nil then
        s = T:get(key) or false
        cache[key] = s
    end
    return s or fallback
end

return M
