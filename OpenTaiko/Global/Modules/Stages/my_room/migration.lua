---@diagnostic disable: undefined-global, undefined-field, lowercase-global
-- My Room save-table migrations, run by Room:loadTable on every raw room table
-- (local save or network payload)

local floor = math.floor

local M = {}

-- removed ids with a successor: placements switch id, inventory counts merge
M.REMAP = { poster = "poster_nokon", painting = "poster_bol",
            poster_meadow = "poster_nokon", poster_dunes = "poster_bol",
            painting_summit = "poster_bol" }
-- retired ids with no successor: removed everywhere
M.DROPPED = { shelf = true }

-- in place on the raw table, before anything reads item ids
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

-- fills room.furniture / wallItems / inventory from the raw table, then structural migrations
-- falls back to the room's current values; catalog decides wall items, starterStock seeds missing ids
function M.applyItems(room, t, catalog, starterStock)
    if t.v and t.v >= 2 then
        room.inventory = t.inventory or room.inventory
        -- v2: wallItems are authoritative; furniture may carry v1 mirrors of wall items, keep ground ones
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
        -- v1: wall-place furniture becomes wallItems (mount high)
        -- starter stock granted once; v1 counts untouched
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
    -- computer split (v1 and older v2): a computer on the first desk, one spare in stock
    -- once per save (gift ledger), or every load would give back a removed computer
    if not (type(t.gifts) == "table" and t.gifts[M.COMPUTER_SPLIT] == true) then
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
        t.gifts = type(t.gifts) == "table" and t.gifts or {}
        t.gifts[M.COMPUTER_SPLIT] = true
    end
    -- seed starter ids the save predates, without touching existing counts
    room.inventory = room.inventory or {}
    for id, n in pairs(starterStock) do if room.inventory[id] == nil then room.inventory[id] = n end end
end

-- placed in new rooms (starter_room.json), given once to saved rooms (applyGifts)
M.GIFTS = { mirror = 1, backpack = 1 }
M.COMPUTER_SPLIT = "computer_split"     -- ledger mark of the computer split (applyItems)

-- ledger of a new room: every gift given, computer split done
function M.allGifts()
    local given = {}
    for id in pairs(M.GIFTS) do given[id] = true end
    given[M.COMPUTER_SPLIT] = true
    return given
end

-- gives each unrecorded gift unless one is placed or in stock
-- true when the inventory grew (caller persists it)
function M.applyGifts(room, t, catalog)
    local given = {}
    if type(t.gifts) == "table" then
        for id, v in pairs(t.gifts) do if v == true then given[id] = true end end
    end
    local granted = false
    for id, n in pairs(M.GIFTS) do
        if not given[id] and catalog[id] then
            local held = (room.inventory[id] or 0) > 0
            for _, it in ipairs(room.furniture) do if it.id == id then held = true end end
            if not held then
                room.inventory[id] = (room.inventory[id] or 0) + n
                granted = true
            end
            given[id] = true
        end
    end
    room.gifts = given
    return granted
end

return M
