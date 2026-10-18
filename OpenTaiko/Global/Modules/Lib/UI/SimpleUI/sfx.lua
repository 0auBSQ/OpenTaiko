---@diagnostic disable: lowercase-global
-- SimpleUI/sfx.lua — the sound of the UI in one table; per-widget `sfx` tables override per-field.

local Util = require("Core.Util")

local Sfx = {}

Sfx.DEFAULT = {
    move   = function () pcall(function () SHARED:GetSharedSound("Move"):Play() end) end,
    hover  = function () pcall(function () SHARED:GetSharedSound("Move"):Play() end) end,
    click  = function () pcall(function () SHARED:GetSharedSound("Decide"):Play() end) end,
    toggle = function () pcall(function () SHARED:GetSharedSound("Decide"):Play() end) end,
    decide = function () pcall(function () SHARED:GetSharedSound("Decide"):Play() end) end,
    cancel = function () pcall(function () SHARED:GetSharedSound("Cancel"):Play() end) end,
    skip   = function () pcall(function () SHARED:GetSharedSound("Skip"):Play() end) end,
    error  = function () pcall(function () SHARED:GetSharedSound("Error"):Play() end) end,
}

-- resolve an effective sfx set = DEFAULT < manager sfx < per-widget sfx
function Sfx.resolve(userSfx, sfx)
    local t = Util.merge(Sfx.DEFAULT, userSfx or {})
    if sfx then t = Util.merge(t, sfx) end
    return t
end

local function playSfx(sfx, name, seen)
    seen = seen or {}
    seen[name] = true
    local s = sfx[name]
    if not s or seen[s] then return end
    if type(s) == "string" then playSfx(sfx, s, seen)
    elseif type(s) == "function" then s()
    else pcall(function() s:Play() end) end
end
Sfx.playSfx = playSfx

return Sfx
