---@diagnostic disable: undefined-global, undefined-field
-- Shop.Daily: one save's shop of the day. Each JST day the shop rolls a big (featured) slot from the
-- "major" rows and 4 normal slots from the "regular" rows (6 when the big roll gives nothing), weighted
-- by PoolSize; Shop.Seasonal can pin an item into the big slot. Rerolls cost 10 coins doubling with each
-- reroll of the day. The day's stock, sold-out slots and rerolls are kept per save file (its SaveUID)
-- in a DATABASE local store of the calling stage, "shop_state" by default.
--   local shop = Daily.open(saveIndex)   -- today's shop for that save (rolled or loaded, then saved)
--   shop.big, shop.normals[1..6]         -- item tables (Catalog rows + LocalizedName, Stock, SoldOut)
--   shop:item(slot)                      -- slot 1..6, or Daily.SLOT_BIG
--   shop:buy(slot, qty)                  -- true, or false + "none" | "soldout" | "poor"
--   shop:reroll()                        -- true, or false + "poor"
--   shop:close()

local Util     = require("Core.Util")
local Almanac  = require("Calendar.almanac")
local Catalog  = require("Shop.Catalog")
local Seasonal = require("Shop.Seasonal")

local M = {}

M.SLOT_BIG = 7   -- bit 6 of the sold-out mask; bits 0..5 are the normal slots
M.STORE = "shop_state"

local function log(s) pcall(debugLog, s) end

-- the day number (e.g. 20261018) in JST, the day the stock changes
function M.freezeKey()
    local jst = os.date("!*t", os.time() + 9 * 3600)
    return jst.year * 10000 + jst.month * 100 + jst.day
end

function M.isSoldOut(mask, slot) return ((mask >> (slot - 1)) & 1) == 1 end
function M.markSoldOut(mask, slot) return mask | (1 << (slot - 1)) end

function M.rerollPrice(rerolls) return math.floor(10 * (2 ^ rerolls)) end

-- a shop item from a catalogue row
local function setupItem(entry)
    if entry == nil then return nil end
    local item = Util.deepcopy(entry)
    item.LocalizedName = Catalog.name(item)
    item.SoldOut = false
    return item
end

-- ranged items (StockMax > 0, e.g. paints and floorings) get a random quantity bought one unit at a
-- time; everything else is a single purchase
local function rollStock(item)
    if item == nil then return end
    local smin = tonumber(item.StockMin) or 0
    local smax = tonumber(item.StockMax) or 0
    if smax > 0 then
        smin = math.max(1, smin)
        if smax < smin then smax = smin end
        item.Stock = math.random(smin, smax)
    else
        item.Stock = 1
    end
end

local Shop = {}
Shop.__index = Shop

function Shop:roll()
    -- the address of a table as extra seed entropy; %p is platform-dependent (bionic prints "0x...",
    -- MSVC bare hex), so the trailing hex digits are read explicitly
    local ptr = tonumber(tostring({}):match("(%x+)%s*$") or "0", 16) or 0
    math.randomseed(os.time() + ptr % 0x7FFFFFFF)

    local itemPools = { normal = {}, big = {} }
    local poolSizes = { normal = 0, big = 0 }

    for pool, entries in pairs(Catalog.pools()) do
        local _count = #entries
        for i = 1, _count do
            local _e = entries[i]
            if Catalog.isIncluded(_e, self.save) then
                local _size = poolSizes[pool]
                itemPools[pool][_size] = _e
                poolSizes[pool] = _size + _e.PoolSize
            end
        end
    end

    log(poolSizes["normal"] .. " - " .. poolSizes["big"])

    -- the entry whose weight range holds roll
    local function pickFromPool(pool, roll)
        local chosen = nil
        local lastKey = -1
        for key, e in pairs(pool) do
            if key <= roll and key >= lastKey then
                chosen = e
                lastKey = key
            end
        end
        return chosen
    end

    if poolSizes.big > 0 then
        local roll = math.random(0, poolSizes.big - 1)
        self.big = pickFromPool(itemPools.big, roll)
        if self.big.Type == "empty" then
            self.big = nil   -- a day with 6 normal slots
        else
            self.big = setupItem(self.big)
            rollStock(self.big)
        end
    else
        self.big = nil
    end

    local count = self.big and 4 or 6
    self.normals = {}

    local pool = {}
    for k, v in pairs(itemPools.normal) do
        pool[k] = v
    end
    local poolSize = poolSizes.normal

    for n = 1, count do
        if poolSize == 0 then
            self.normals[n] = nil
        else
            local roll = math.random(0, poolSize - 1)
            log("Pool " .. poolSize .. " - " .. roll)
            local chosen = pickFromPool(pool, roll)
            self.normals[n] = setupItem(chosen)
            rollStock(self.normals[n])

            if chosen and Catalog.isUnique(chosen) then
                -- the pool again without this entry
                local newPool = {}
                local newSize = 0
                for _, e in pairs(pool) do
                    if e ~= chosen then
                        newPool[newSize] = e
                        newSize = newSize + e.PoolSize
                    end
                end
                pool = newPool
                poolSize = newSize
            end
        end
    end

    log("Big: " .. tostring(self.big and self.big.LocalizedName or "nil"))
    log("Normal count: " .. tostring(#self.normals))
    for _, v in pairs(self.normals) do
        log(v.LocalizedName .. " (" .. v.Code .. ")")
    end
end

-- the best active seasonal entry (highest priority, not owned) takes the big slot; a roll and a reroll
-- can both displace it, so this runs after every roll and load
function Shop:applySeasonal()
    local save = self.save
    local pick = Almanac.pick(self.seasonal, save, function(e)
        local item = Catalog.find(Almanac.plain(e.code))
        return item == nil or Catalog.isOwned(item, save)
    end)
    if pick == nil then return end
    local code = Almanac.plain(pick.code)
    if self.big ~= nil and self.big.Code == code then return end
    self.big = setupItem(Catalog.find(code))
    rollStock(self.big)
    self.soldOut = self.soldOut & ~(1 << (M.SLOT_BIG - 1))
    for i = 5, 6 do self.normals[i] = nil end
end

function Shop:store()
    local db, p = self.db, self.prefix
    db:Write(p .. "day",     tostring(self.day))
    db:Write(p .. "rerolls", tostring(self.rerolls))
    db:Write(p .. "soldout", tostring(self.soldOut))
    db:Write(p .. "big",       self.big and self.big.Code or "")
    db:Write(p .. "big_stock", tostring(self.big and self.big.Stock or 0))
    for i = 1, 6 do
        local it = self.normals[i]
        db:Write(p .. "n" .. i,        it and it.Code or "")
        db:Write(p .. "n" .. i .. "s", tostring(it and it.Stock or 0))
    end
end

function Shop:load()
    local db, p = self.db, self.prefix
    self.soldOut = tonumber(db:Read(p .. "soldout") or "0") or 0

    self.big = setupItem(Catalog.find(db:Read(p .. "big")))
    if self.big then self.big.Stock = tonumber(db:Read(p .. "big_stock") or "1") or 1 end

    self.normals = {}
    for i = 1, 6 do
        self.normals[i] = setupItem(Catalog.find(db:Read(p .. "n" .. i)))
        if self.normals[i] then self.normals[i].Stock = tonumber(db:Read(p .. "n" .. i .. "s") or "1") or 1 end
    end

    if self.big and M.isSoldOut(self.soldOut, M.SLOT_BIG) then self.big.SoldOut = true end
    for i = 1, 6 do
        if self.normals[i] and M.isSoldOut(self.soldOut, i) then self.normals[i].SoldOut = true end
    end

    self.rerolls = tonumber(db:Read(p .. "rerolls") or "0") or 0
end

function Shop:item(slot)
    if slot == M.SLOT_BIG then return self.big end
    return self.normals[slot]
end

function Shop:rerollPrice() return M.rerollPrice(self.rerolls) end

-- why the slot can't be bought now ("none", "soldout", "poor"), or nil
function Shop:refusal(slot, qty)
    local item = self:item(slot)
    if item == nil then return "none" end
    if item.SoldOut then return "soldout" end
    if item.Price * (qty or 1) > self.save.Coins then return "poor" end
    return nil
end

-- buys qty units of a slot (clamped to its stock): grants them (Catalog.grant), spends price × qty,
-- lowers the stock, marks the slot sold out once it is empty, and saves
function Shop:buy(slot, qty)
    local why = self:refusal(slot, qty)
    if why ~= nil then return false, why end
    local item = self:item(slot)
    qty = math.max(1, math.min(math.floor(qty or 1), item.Stock or 1))
    Catalog.grant(self.save, item, qty)
    self.save:SpendCoins(item.Price * qty)
    item.Stock = (item.Stock or 1) - qty
    if item.Stock <= 0 then
        item.SoldOut = true
        self.soldOut = M.markSoldOut(self.soldOut, slot)
    end
    self:store()
    return true
end

-- spends the reroll price, rolls a new stock (every slot back in stock) and saves
function Shop:reroll()
    local price = self:rerollPrice()
    if price > self.save.Coins then return false, "poor" end
    self.save:SpendCoins(price)
    self.rerolls = self.rerolls + 1
    self.soldOut = 0
    self:roll()
    self:applySeasonal()
    self:store()
    return true
end

function Shop:close()
    if self.db then self.db:Dispose() end
    self.db = nil
end

-- opts.store: the local DATABASE name; opts.seasonal: the featured entries (Shop.Seasonal by default)
function M.open(index, opts)
    opts = opts or {}
    local save = GetSaveFile(index)
    local uid = (save.SaveUID and save.SaveUID ~= "") and save.SaveUID or ("slot" .. index)
    local self = setmetatable({
        index = index, save = save, prefix = uid .. "_",
        seasonal = opts.seasonal or Seasonal,
        big = nil, normals = {}, rerolls = 0, soldOut = 0,
    }, Shop)
    self.db = DATABASE:OpenLocalDatabase(opts.store or M.STORE)
    self.day = M.freezeKey()
    local storedDay = tonumber(self.db:Read(self.prefix .. "day") or "0") or 0
    if storedDay ~= self.day then
        self.rerolls = 0; self.soldOut = 0; self:roll(); self:applySeasonal(); self:store()
    else
        self:load()
        self:applySeasonal(); self:store()
    end
    return self
end

return M
