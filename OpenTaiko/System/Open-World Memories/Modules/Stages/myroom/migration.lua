---@diagnostic disable: undefined-global, undefined-field, lowercase-global
-- migration.lua — save-table migrations for My Room. Room:loadTable hands every RAW room table
-- (local save or network payload) through here before applying it, so version upgrades and
-- retired-id cleanups live in one place, away from the room model.
--
-- Registries:
--   M.REMAP   — removed catalog ids with a successor: placements switch id, inventory counts merge
--   M.DROPPED — retired ids with no successor: removed from placements and inventories entirely
-- Structural migrations:
--   v1 → v2   — wall-place furniture entries become wallItems; starter stock granted once
--   computer  — the PC used to be baked into the desk: spawn one on the first desk + seed a spare
--   starter   — any starter-stock id the save predates is seeded without touching player counts

local floor = math.floor

local M = {}

M.REMAP = { poster = "poster_nokon", painting = "poster_bol",
            poster_meadow = "poster_nokon", poster_dunes = "poster_bol",
            painting_summit = "poster_bol" }
M.DROPPED = { shelf = true }

-- retired-id cleanup, in place on the raw table (runs before anything reads item ids)
function M.remapIds(t)
    local function remapList(list)
        if list == nil then return end
        for i = #list, 1, -1 do
            local it = list[i]
            it.id = M.REMAP[it.id] or it.id
            if M.DROPPED[it.id] then table.remove(list, i) end
        end
    end
    remapList(t.furniture); remapList(t.wallItems)
    if t.inventory then
        for old, new in pairs(M.REMAP) do
            if t.inventory[old] then
                t.inventory[new] = (t.inventory[new] or 0) + t.inventory[old]
                t.inventory[old] = nil
            end
        end
        for old in pairs(M.DROPPED) do t.inventory[old] = nil end
    end
end

-- item-table application + structural migrations: fills room.furniture / room.wallItems /
-- room.inventory from the raw table (falling back to the room's current values like loadTable
-- always did). `catalog` decides what counts as a wall item; `starterStock` seeds missing ids.
function M.applyItems(room, t, catalog, starterStock)
    if t.v and t.v >= 2 then
        room.inventory = t.inventory or room.inventory
        -- v2: wallItems are authoritative; the furniture array may carry v1-readable mirrors of
        -- legacy wall items — keep only ground entries from it.
        local furn = {}
        for _, it in ipairs(t.furniture or {}) do
            local cat = catalog[it.id]
            if not (cat and cat.place == "wall") then furn[#furn + 1] = it end
        end
        room.furniture = furn
        local wi = {}
        for _, it in ipairs(t.wallItems or {}) do
            wi[#wi + 1] = { id = it.id, c = floor(it.c or 0), r = floor(it.r or 0),
                            mount = (it.mount == "high") and "high" or "low" }
        end
        room.wallItems = wi
    else
        -- v1 migration: wall-place furniture entries become wallItems (clocks hang high); the fixed
        -- phone becomes a movable wall item at its old spot; the new catalog's starter stock is
        -- granted once (v1 ids and their counts are left untouched).
        local furn, wi = {}, {}
        for _, it in ipairs(t.furniture or room.furniture) do
            local cat = catalog[it.id]
            if cat and cat.place == "wall" then
                wi[#wi + 1] = { id = it.id, c = floor(it.c or 0), r = floor(it.r or 0), mount = "high" }
            else
                furn[#furn + 1] = it
            end
        end
        room.furniture, room.wallItems = furn, wi
        local inv = t.inventory or room.inventory
        for id, n in pairs(starterStock) do if inv[id] == nil then inv[id] = n end end
        room.inventory = inv
    end
    -- computer split-out migration (v1 AND older v2 saves): the PC used to be baked into the
    -- desk — spawn a computer stacked on the first desk so nobody loses their PC, and seed one
    -- spare into inventories that predate the item
    local hasComputer = false
    for _, it in ipairs(room.furniture) do if it.id == "computer" then hasComputer = true end end
    if not hasComputer then
        for _, it in ipairs(room.furniture) do
            if it.id == "desk" and not it.on then
                room.furniture[#room.furniture + 1] =
                    { id = "computer", c = it.c, r = it.r, facing = it.facing or 0, on = true }
                break
            end
        end
    end
    if room.inventory.computer == nil then room.inventory.computer = 1 end
    -- seed any NEW starter item the save predates (new furniture variants, souvenir pictures,
    -- floorings, wall paints) so existing rooms get the new content without resetting counts
    room.inventory = room.inventory or {}
    for id, n in pairs(starterStock) do if room.inventory[id] == nil then room.inventory[id] = n end end
end

return M
