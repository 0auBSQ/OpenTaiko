---@diagnostic disable: undefined-global, undefined-field, lowercase-global
-- i18n.lua — per-stage localization (shared; each stage's Lua VM gets its own instance). English strings
-- in the code are the canonical keys; the stage's own lang/ja.lua provides exact-match translations
-- (missing entries fall back to English, so partial coverage degrades gracefully). The current game
-- language is read through the engine's CLocalizationData; call detect() at onStart and from a
-- reloadLanguage() global. lang/ja.lua is resolved via package.path, so it is per-stage.

local M = {}

M.lang = "default"
local dict = {}

-- current language via LANG:FromString → CLocalizationData:GetString picks by the active language
local function currentIsJa()
    local ok, res = pcall(function()
        return LANG:FromString('{"ja":"ja","default":"other"}'):GetString("other")
    end)
    return ok and res == "ja"
end

-- load a specific language (also used directly by the headless coverage harness)
function M.load(lang)
    if lang == "ja" then
        M.lang = "ja"
        local ok, d = pcall(require, "lang/ja")
        dict = (ok and type(d) == "table") and d or {}
    else
        M.lang = "default"
        dict = {}
    end
end

function M.detect()
    M.load(currentIsJa() and "ja" or "default")
end

-- optional miss recording (harness coverage audits set trackMisses)
M.trackMisses = false
M._missed = {}

-- translate one string (exact match; nil-safe; unknown → unchanged)
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
