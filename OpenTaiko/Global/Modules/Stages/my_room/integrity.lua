---@diagnostic disable: undefined-global, undefined-field
-- integrity.lua — the placement sweep My Room runs on every load. A saved room can carry pieces that
-- the live rules would refuse (an older build let rotations overlap): this walks the furniture and
-- sends every offender back to the inventory so the room opens consistent.
--
-- Ground pieces go first, in save order (the earlier piece keeps its cells): a ground piece is evicted
-- when any of its cells is off the floor or already taken by another ground piece. Stacked pieces
-- follow: one is evicted when it cannot stack, when its cells are not all hosted by the same surface,
-- or when another stacked piece already holds one of them. An evicted surface takes its riders with
-- it, as removing it in edit mode would. Wall items come last: one is evicted when its tile is not a
-- wall tile, when an earlier item already takes one of its tiles and bands, or when a piece in front
-- of it stands higher than its mount. The phone (Room:ensurePhone keeps it on the walls) and the
-- windows (part of the room's walls) are never evicted: the pieces they would run into go instead.
--
--   Integrity.sweep(room) -> { id, ... }   the ids returned to stock, in eviction order

local M = {}

local function key(c, r) return c .. "," .. r end

-- take one piece out of the room and into the inventory, riders first when it carried any
local function evict(room, catalog, it, out, gone)
    if not room:detachFurniture(it) then return end
    gone[it] = true
    local cat = catalog[it.id]
    if cat and cat.surface then
        for _, rider in ipairs(room:stackedItemsOn(it)) do evict(room, catalog, rider, out, gone) end
    end
    room:invAdd(it.id)
    out[#out + 1] = it.id
end

-- a ground piece's cells must all be floor and free of other ground pieces
local function groundFault(room, cells, taken)
    for _, cell in ipairs(cells) do
        if room:cellType(cell[1], cell[2]) ~= "O" or taken[key(cell[1], cell[2])] then return true end
    end
    return false
end

-- a stacked piece must be allowed to stack, rest on ONE surface under all its cells, and share no
-- cell with another stacked piece
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

function M.sweep(room)
    local catalog = room.CATALOG
    local out, gone = {}, {}
    -- snapshot: evictions edit room.furniture while we walk
    local pieces = {}
    for _, it in ipairs(room.furniture) do pieces[#pieces + 1] = it end

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
                room:invAdd(it.id)
                out[#out + 1] = it.id
            end
        else
            for _, s in ipairs(slots) do taken[s] = true end
        end
    end
    return out
end

return M
