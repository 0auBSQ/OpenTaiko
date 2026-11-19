---@diagnostic disable: undefined-global, undefined-field
-- Shop.Catalog: coin shop items (itempool table of Items.db3 beside this file)

local Util  = require("Core.Util")
local Texts = require("Shop.Texts")
local HERE  = require("Core.Here")(...)

local M = {}

M.SLOTS = { normal = "regular", big = "major" }

-- SQL paths are relative to the calling module's folder
local function dbPath()
    local ok, from = pcall(function() return STORAGE:GetFullPath(".") end)
    if not ok or type(from) ~= "string" or HERE == nil then return "Databases/Items.db3" end
    local a, b = {}, {}
    for p in from:gsub("\\", "/"):gmatch("[^/]+") do a[#a + 1] = p end
    for p in HERE:gsub("\\", "/"):gmatch("[^/]+") do b[#b + 1] = p end
    local n = 0
    while n < #a and n < #b and a[n + 1]:lower() == b[n + 1]:lower() do n = n + 1 end
    local parts = {}
    for _ = n + 1, #a do parts[#parts + 1] = ".." end
    for i = n + 1, #b do parts[#parts + 1] = b[i] end
    parts[#parts + 1] = "Items.db3"
    return table.concat(parts, "/")
end
M.dbPath = dbPath

local db = nil

-- row: Code, Type (see M.GRANTS; "empty" = big slot empty that day), RefInt/RefText, Price,
-- Name (LANG JSON), Slot ("regular" | "major"), PoolSize (weight), Condition (trigger),
-- StockMin/StockMax (ranged stock when StockMax > 0), OneTime (1 = once ever)
-- the rows of one Slot, in the table's order
function M.rows(slot)
    if db == nil then db = SQL:OpenSQLDatabase(dbPath()) end
    return Util.cloneTable(db:Query("SELECT * FROM itempool WHERE Slot = '" .. slot .. "'"))
end

local pools = nil

-- { normal = rows, big = rows }, read once per Lua state
function M.pools()
    if pools == nil then
        pools = { normal = M.rows(M.SLOTS.normal), big = M.rows(M.SLOTS.big) }
    end
    return pools
end

function M.find(code)
    if code == nil or code == "" then return nil end
    for _, pool in pairs(M.pools()) do
        for i = 1, #pool do
            if pool[i].Code == code then return pool[i] end
        end
    end
    return nil
end

-- OneTime = 1: bought once ever; sets the trigger "<RefText>_owned"
-- (My Room drains the .myroom_<id> grant counter)
function M.isOneTime(entry) return tonumber(entry.OneTime or 0) == 1 end
function M.ownedTrigger(entry) return entry.RefText .. "_owned" end

-- unique: taken out of the pool once picked in a roll, and not offered once owned
function M.isUnique(entry)
    if M.isOneTime(entry) then return true end
    if entry.Type == "counterable" then return false end
    return true
end

function M.isOwned(entry, save)
    if M.isOneTime(entry) then return save:GetGlobalTrigger(M.ownedTrigger(entry)) end
    if entry.Type == "triggerable" then return save:GetGlobalTrigger(entry.RefText) end
    if entry.Type == "nameplate" then return save:IsNameplateUnlocked(entry.RefInt) end
    return false
end

-- whether the row can be rolled for this save
function M.isIncluded(entry, save)
    if entry.Condition ~= nil then return save:GetGlobalTrigger(entry.Condition) end
    if M.isUnique(entry) then return not M.isOwned(entry, save) end
    return true
end

-- the My Room furniture id of a "furn_<id>" code, else nil
function M.furnitureId(code)
    if code and code:sub(1, 5) == "furn_" then return code:sub(6) end
    return nil
end

-- the shown name: the module's own string for the code, else the row's Name
function M.name(item)
    local own = LANG:FromString(item.Name):GetString("")
    return Texts.item(item.Code, own)
end

-- what a purchase of qty units gives, by Type; a skin may add its own types here
M.GRANTS = {
    nameplate = function(save, item, qty) save:UnlockNameplate(item.RefInt) end,
    triggerable = function(save, item, qty) save:SetGlobalTrigger(item.RefText, true) end,
    -- My Room claims the counter deltas into its inventory
    counterable = function(save, item, qty)
        save:SetGlobalCounter(item.RefText, save:GetGlobalCounter(item.RefText) + qty)
    end,
}

function M.grant(save, item, qty)
    local g = M.GRANTS[item.Type]
    if g ~= nil then g(save, item, qty) end
    if M.isOneTime(item) then save:SetGlobalTrigger(M.ownedTrigger(item), true) end
end

return M
