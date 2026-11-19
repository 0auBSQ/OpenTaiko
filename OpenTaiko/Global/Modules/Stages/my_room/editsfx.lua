---@diagnostic disable: undefined-global
-- edit mode sounds: Sounds/Edit/<name>.ogg, loaded on first use
-- names: hover select back open close pickup place place_soft hang rotate refuse remove erase paint_floor paint_wall door

local M = {}
local cache = {}

local function load(name)
    if cache[name] == nil then
        local ok, s = pcall(function() return SOUND:CreateSFX("Sounds/Edit/" .. name .. ".ogg") end)
        cache[name] = (ok and s ~= nil) and s or false
    end
    return cache[name] or nil
end

function M.play(name)
    local s = load(name)
    if s ~= nil then s:Play() end
end

-- set-down sound name: wall, surface or floor
function M.landed(it, kind)
    if kind == "wall" then return "hang" end
    if it.on then return "place_soft" end
    return "place"
end

function M.dispose()
    for _, s in pairs(cache) do
        if s then pcall(function() s:Dispose() end) end
    end
    cache = {}
end

return M
