-- Per-floor BGM path and monster names; a floor without its own BGM takes the nearest lower one.

local M = {}

local SND = "Sounds/floors/"

-- levels that ship a BGM file, ascending; keep in sync with the folder
local BGM_LEVELS = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 17, 18, 19, 20, 21 }

-- monster display names by the level they first appear on
local MONSTER_NAMES = {
    [11] = "Needle",  [12] = "Thorncushion", [13] = "Counterplate", [14] = "Pincerblaze",
    [15] = "Startle", [16] = "Fray",         [17] = "Dragoon",      [18] = "Frangel",
    [19] = "Zirr",    [20] = "Tobiraph",
}

-- the highest entry <= level (or the lowest entry when the level is below all of them)
local function resolve(list, level)
    local best = list[1]
    for _, n in ipairs(list) do
        if n <= level then best = n else break end
    end
    return best
end

function M.bgmPath(level)     return SND .. resolve(BGM_LEVELS, level) .. ".ogg" end
function M.monsterName(level) return MONSTER_NAMES[level] end

return M
