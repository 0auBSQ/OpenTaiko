-- Which of Ume's items the player has and which are ready; kept in save.lua, so quitting never refills one.

local save = require("save")

local M = {}

M.ORDER = { "charm", "tambourine" }                 -- slot order, left to right

local FROM_PLAYS = { charm = 1, tambourine = 2 }    -- minimum save.plays() for the item
local CHECKPOINTS = { [1] = true, [6] = true, [11] = true, [16] = true, [21] = true }
local ROOF_FIRST, ROOF_EVERY = 21, 3

local function list(pass)
    local out = {}
    for _, id in ipairs(M.ORDER) do
        if pass(id) then out[#out + 1] = id end
    end
    return out
end

-- ids Ume still has to present, in order
function M.toPresent()
    local plays = save.plays()
    return list(function(id) return plays >= FROM_PLAYS[id] and not save.item_presented(id) end)
end

-- the items the player has, in order
function M.owned()
    return list(save.item_presented)
end

function M.ready(id)
    return save.item_presented(id) and not save.item_spent(id)
end

-- marks the item owned and ready
function M.present(id)
    save.mark_item_presented(id)
    save.set_item_spent(id, false)
end

function M.spend(id)
    save.set_item_spent(id, true)
end

function M.replenishes(level)
    if CHECKPOINTS[level] then return true end
    return level > ROOF_FIRST and (level - ROOF_FIRST) % ROOF_EVERY == 0
end

-- on a replenish floor the spent items are ready again; returns the items that were refilled
function M.replenish(level)
    if not M.replenishes(level) then return {} end
    local refilled = list(function(id) return save.item_presented(id) and save.item_spent(id) end)
    for _, id in ipairs(refilled) do save.set_item_spent(id, false) end
    return refilled
end

return M
