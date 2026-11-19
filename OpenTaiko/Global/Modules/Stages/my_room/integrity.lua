---@diagnostic disable: undefined-global, undefined-field
-- placement sweep on every load: pieces the live rules refuse go back to the inventory

local M = {}

local function key(c, r) return c .. "," .. r end
-- same as Room.stockKey: a coloured piece keeps its colour
local function stockKey(it) return it.hex and (it.id .. ":" .. it.hex) or it.id end

-- room -> inventory, with its riders
local function evict(room, catalog, it, out, gone)
    if not room:detachFurniture(it) then return end
    gone[it] = true
    local cat = catalog[it.id]
    if cat and cat.surface then
        for _, rider in ipairs(room:stackedItemsOn(it)) do evict(room, catalog, rider, out, gone) end
    end
    room:invAdd(stockKey(it))
    out[#out + 1] = it.id
end

-- true when a cell is off the floor or taken
local function groundFault(room, cells, taken)
    for _, cell in ipairs(cells) do
        if room:cellType(cell[1], cell[2]) ~= "O" or taken[key(cell[1], cell[2])] then return true end
    end
    return false
end

-- true unless stackOn, with every cell free and on one surface
local function stackedFault(room, cat, cells, taken)
    if not cat.stackOn then return true end
    local surf = nil
    for _, cell in ipairs(cells) do
        if room:cellType(cell[1], cell[2]) ~= "O" or taken[key(cell[1], cell[2])] then return true end
        local g, gcat = room:groundItemAt(cell[1], cell[2])
        if g == nil or not (gcat and gcat.surface) then return true end
        if surf ~= nil and surf ~= g then return true end
        surf = g
    end
    return false
end

-- returns evicted ids, in eviction order
function M.sweep(room)
    local catalog = room.CATALOG
    local out, gone = {}, {}
    -- snapshot: evictions edit room.furniture
    local pieces = {}
    for _, it in ipairs(room.furniture) do pieces[#pieces + 1] = it end

    -- ground pieces in save order: the earlier piece keeps its cells
    local taken = {}
    for _, it in ipairs(pieces) do
        local cat = catalog[it.id]
        if cat and cat.place ~= "wall" and not it.on then
            local cells = room:footprint(it.id, it.c, it.r, it.facing or 0)
            if groundFault(room, cells, taken) then
                evict(room, catalog, it, out, gone)
            else
                for _, cell in ipairs(cells) do taken[key(cell[1], cell[2])] = true end
            end
        end
    end

    taken = {}
    for _, it in ipairs(pieces) do
        local cat = catalog[it.id]
        if cat and cat.place ~= "wall" and it.on and not gone[it] then
            local cells = room:footprint(it.id, it.c, it.r, it.facing or 0)
            if stackedFault(room, cat, cells, taken) then
                evict(room, catalog, it, out, gone)
            else
                for _, cell in ipairs(cells) do taken[key(cell[1], cell[2])] = true end
            end
        end
    end

    -- wall items: phone and windows stay, the pieces in front of them go
    local walls = {}
    for _, it in ipairs(room.wallItems) do walls[#walls + 1] = it end
    taken = {}
    for _, it in ipairs(walls) do
        local tiles = room:wallItemTiles(it.id, it.c, it.r)
        local mounts = room.wallItemMounts(it.id, it.mount)
        local slots, blocked, front = {}, false, {}
        for _, t in ipairs(tiles or {}) do
            for _, m in ipairs(mounts) do
                local slot = key(t[1], t[2]) .. m
                if taken[slot] then blocked = true end
                slots[#slots + 1] = slot
                for _, p in ipairs(room:piecesInFrontOf(t[1], t[2], m, it.id)) do front[#front + 1] = p end
            end
        end
        local cat = catalog[it.id]
        if it.id == "phone" or (cat and cat.window) then
            for _, p in ipairs(front) do
                if not gone[p] then evict(room, catalog, p, out, gone) end
            end
            for _, s in ipairs(slots) do taken[s] = true end
        elseif tiles == nil or blocked or #front > 0 then
            if room:removeWallItem(it) then
                room:invAdd(stockKey(it))
                out[#out + 1] = it.id
            end
        else
            for _, s in ipairs(slots) do taken[s] = true end
        end
    end
    return out
end

return M
