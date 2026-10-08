---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local, redundant-parameter, inject-field
local M = {}

local KEY_COUNTERS = {
    ".vault_key_count_simple",
    ".vault_key_count_gold",
    ".vault_key_count_optk",
}

local NAMEPLATE_POOLS = {
    {361, 362, 363, 364, 365, 366, 367, 368},   -- Simple
    {369, 370, 371, 372, 373, 374, 375},          -- Gold
    {376, 377, 378, 379, 380},                    -- OpTk
}

-- Rarity string → modal integer (matches HRarity.RarityToModalInt in C#)
-- Poor=0, Common=0, Uncommon=1, Rare=2, Epic=3, Legendary=4, Mythical=4
local RARITY_MAP = {Poor=0, Common=0, Uncommon=1, Rare=2, Epic=3, Legendary=4, Mythical=4}

local function songLevelToRarity(lv)
    if lv <= 1 then return 0 end   -- Common
    if lv == 2 then return 1 end   -- Uncommon
    if lv == 3 then return 2 end   -- Rare
    if lv == 4 then return 3 end   -- Epic
    return 4                       -- Legendary (5+)
end

local songPools = {{}, {}, {}}   -- indexed by chest (1‥3)

-- ── Helpers ───────────────────────────────────────────────────────────────────

local function sf()
    return GetSaveFile(0)
end

-- counters come back as doubles: kept as whole numbers here
local function getKeyCount(idx)
    return math.floor((tonumber(sf():GetGlobalCounter(KEY_COUNTERS[idx])) or 0) + 0.5)
end

local function setKeyCount(idx, n)
    sf():SetGlobalCounter(KEY_COUNTERS[idx], n)
end

local function iterateCsharpList(list)
    local arr = {}
    if list == nil then return arr end
    local e = list:GetEnumerator()
    while e:MoveNext() do table.insert(arr, e.Current) end
    return arr
end

local function getMaxChartLevel(node)
    local maxLv = 0
    for diff = 0, 4 do
        local ch = node:GetChart(diff)
        if ch ~= nil and ch.Level > 0 then
            maxLv = math.max(maxLv, ch.Level)
        end
    end
    return maxLv
end

local function buildSongPools()
    songPools = {{}, {}, {}}

    local lsls = GenerateSongListSettings()
    lsls.ModuloPagination     = false
    lsls.AppendMainRandomBox  = false
    lsls.AppendSubRandomBoxes = false
    lsls.SubBackBoxFrequency  = 0
    lsls.RootGenreFolder      = "Secret Vault"
    lsls.IgnoreUnlockables    = true

    local vaultList = RequestSongList(lsls)
    if vaultList == nil then return end

    local allNodes = iterateCsharpList(vaultList:SearchSongsByPredicate(function(n) return true end))
    for _, node in ipairs(allNodes) do
        local lv = getMaxChartLevel(node)
        if lv <= 1 then
            table.insert(songPools[1], node)
        elseif lv == 2 then
            table.insert(songPools[2], node)
        else
            table.insert(songPools[3], node)
        end
    end
end

local function getUnobtainedSongs(poolIdx)
    local save = sf()
    local out = {}
    for _, node in ipairs(songPools[poolIdx]) do
        if not save:GetGlobalTrigger(".vault_song_unlocked_" .. node.UniqueId) then
            table.insert(out, node)
        end
    end
    return out
end

local function getUnobtainedNameplates(poolIdx)
    local save = sf()
    local out = {}
    for _, id in ipairs(NAMEPLATE_POOLS[poolIdx]) do
        if not save:IsNameplateUnlocked(id) then
            table.insert(out, id)
        end
    end
    return out
end

local function buildChestPool(idx)
    local pool = {}

    -- snap (nothing)
    local snapW = (idx == 3) and 55 or 35
    table.insert(pool, {w = snapW, type = "snap"})

    -- coins
    local coinW    = (idx == 2) and 25 or 20
    local coinMin  = ({20, 100, 200})[idx]
    local coinMax  = ({50, 200, 500})[idx]
    table.insert(pool, {w = coinW, type = "coins", min = coinMin, max = coinMax})

    -- key (chest1 → gold key, chest2 → optk key, chest3 → none)
    if idx == 1 then table.insert(pool, {w = 10, type = "key", target = 2}) end
    if idx == 2 then table.insert(pool, {w =  5, type = "key", target = 3}) end

    -- nameplate
    local uNp = getUnobtainedNameplates(idx)
    if #uNp > 0 then
        table.insert(pool, {w = 15, type = "nameplate", pool = uNp})
    end

    -- song
    local songW = (idx == 3) and 10 or 20
    local uSongs = getUnobtainedSongs(idx)
    if #uSongs > 0 then
        table.insert(pool, {w = songW, type = "song", pool = uSongs})
    end

    return pool
end

local function rollFromPool(pool)
    local total = 0
    for _, e in ipairs(pool) do total = total + e.w end
    if total == 0 then return {type = "snap"} end
    local r = math.random(1, total)
    local cum = 0
    for _, e in ipairs(pool) do
        cum = cum + e.w
        if r <= cum then return e end
    end
    return pool[#pool]
end

local function processRoll(idx)
    local pool   = buildChestPool(idx)
    local entry  = rollFromPool(pool)
    local save   = sf()

    setKeyCount(idx, getKeyCount(idx) - 1)

    if entry.type == "snap" then
        return {type = "snap", openedChest = idx}
    elseif entry.type == "coins" then
        local amount = math.random(entry.min, entry.max)
        save:EarnCoins(amount)
        return {type = "coins", amount = amount, openedChest = idx}
    elseif entry.type == "key" then
        setKeyCount(entry.target, getKeyCount(entry.target) + 1)
        return {type = "key", target = entry.target, openedChest = idx}
    elseif entry.type == "nameplate" then
        local id = entry.pool[math.random(1, #entry.pool)]
        save:UnlockNameplate(id)
        return {type = "nameplate", id = id, info = NAMEPLATESLIST:GetById(id), openedChest = idx}
    elseif entry.type == "song" then
        local node = entry.pool[math.random(1, #entry.pool)]
        save:SetGlobalTrigger(".vault_song_unlocked_" .. node.UniqueId, true)
        return {type = "song", node = node, openedChest = idx}
    end

    return {type = "snap", openedChest = idx}
end

-- ── API ────────────────────────────────────────────────────────────────────────

function M.enter()
    -- First visit: give 1 simple key
    local gifted = false
    local save = sf()
    if save:GetGlobalTrigger(".vault_first_key_given") ~= true then
        setKeyCount(1, getKeyCount(1) + 1)
        save:SetGlobalTrigger(".vault_first_key_given", true)
        gifted = true
    end

    buildSongPools()
    return gifted
end

function M.keyCount(idx)
    return getKeyCount(idx)
end

function M.stock(idx)
    local left  = #getUnobtainedSongs(idx) + #getUnobtainedNameplates(idx)
    local total = #songPools[idx] + #NAMEPLATE_POOLS[idx]
    return left, total
end

function M.roll(idx)
    return processRoll(idx)
end

function M.showReward(reward)
    local modal = ROACTIVITY:GetROActivity("modal")
    if modal == nil or reward == nil then return false end
    local rtype = reward.type
    if rtype == "coins" then
        modal:Activate(0, 1, 0, reward.amount, sf().Coins)
    elseif rtype == "nameplate" then
        local npRarity = RARITY_MAP[reward.info and reward.info.Rarity] or 1
        modal:Activate(0, npRarity, 3, reward.info, nil)
    elseif rtype == "song" then
        local songRarity = songLevelToRarity(getMaxChartLevel(reward.node))
        modal:Activate(0, songRarity, 4, reward.node, nil)
    else
        return false
    end
    return true
end

return M
