-- The pagoda's save data (Global/Modules/Lib/Pagoda/Progress.lua) plus OWM's own parts.

local I18N = require("i18n")
local Progress = require("Pagoda.Progress")

local M = setmetatable({}, { __index = Progress })

-- items (encounter/items/state.lua): presented, and spent until the next replenish
function M.item_presented(id)
    return GetSaveFile(0):GetGlobalTrigger("pagoda_item_" .. id .. "_presented") == true
end

function M.mark_item_presented(id)
    GetSaveFile(0):SetGlobalTrigger("pagoda_item_" .. id .. "_presented", true)
end

function M.item_spent(id)
    return GetSaveFile(0):GetGlobalTrigger("pagoda_item_" .. id .. "_spent") == true
end

function M.set_item_spent(id, spent)
    local sf = GetSaveFile(0)
    local key = "pagoda_item_" .. id .. "_spent"
    if (sf:GetGlobalTrigger(key) == true) ~= (spent == true) then sf:SetGlobalTrigger(key, spent == true) end
end

-- the bestiary (bestiary.lua): how often the player beat each monster and how often it beat the player;
-- key = its art stem (encounter/monsters.lua)
function M.monster_beaten(key)
    return math.max(0, math.floor(GetSaveFile(0):GetGlobalCounter("pagoda_beaten_" .. key)))
end

function M.monster_wins(key)
    return math.max(0, math.floor(GetSaveFile(0):GetGlobalCounter("pagoda_monster_wins_" .. key)))
end

-- a challenge floor's result for its monsters: beaten (the player cleared it) or a win of theirs
function M.count_monster_result(key, beaten)
    local name = (beaten and "pagoda_beaten_" or "pagoda_monster_wins_") .. key
    local sf = GetSaveFile(0)
    sf:SetGlobalCounter(name, math.max(0, math.floor(sf:GetGlobalCounter(name))) + 1)
end

-- true once the player beat or lost to any of the keys
function M.any_monster_fought(keys)
    for _, k in ipairs(keys) do if M.monster_beaten(k) + M.monster_wins(k) > 0 then return true end end
    return false
end

function M.player_name()
    local sf = GetSaveFile(0)
    local ok, n = pcall(function() return sf.Name end)
    if ok and n ~= nil and n ~= "" then return n end
    return I18N.tr("Player")
end

return M
