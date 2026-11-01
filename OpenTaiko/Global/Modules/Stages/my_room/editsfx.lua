---@diagnostic disable: undefined-global
-- editsfx.lua — the edit mode's own sound set: Sounds/Edit/<name>.ogg
-- Loaded on first use and freed with the stage. One name per thing that
-- happens, so editmode.lua reads as the action rather than the nearest menu sound:
--   hover          the cursor lands on another cell, slot, tile or piece
--   select / back  a piece's buttons open / the selection or a sub-mode closes
--   open / close   entering / leaving edit mode
--   pickup         a piece (or the doorway) lifts into the hand
--   place          furniture set on the floor; place_soft set on a surface; hang hung on the wall
--   rotate         a turn or a wall-mount flip; refuse when the room says no
--   remove         a piece back to the inventory; erase for flooring and paint
--   paint_floor / paint_wall
--   door           the doorway moved

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

-- the set-down sound for a piece that just landed: on the wall, on a surface, or on the floor
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
