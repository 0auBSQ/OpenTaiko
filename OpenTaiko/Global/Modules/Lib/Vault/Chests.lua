-- Vault.Chests: the secret vault's three chests, 1 wooden, 2 golden, 3 platinum, each opened with its own
-- key (Vault.Keys). A chest gives one reward per key from a weighted pool: nothing (the key snaps), coins,
-- the next chest's key, a nameplate (361-380), My Room furniture, or a song of the "Secret Vault" folder.
--   Chests.enter()        -- once per visit: the first visit's free key, the song pools
--   Chests.roll(i)        -- spends a key of chest i, grants and saves the reward, returns it
--   Chests.showReward(r)  -- its pop-up through the "modal" ROActivity (false for a snap or a key)
-- A reward: { type = "snap" | "coins" (amount) | "key" (target) | "nameplate" (id, info) |
-- "furniture" (id, rarity) | "song" (node), openedChest }.

local Keys  = require("Vault.Keys")
local Texts = require("Vault.Texts")

local M = {}

M.FIRST_KEY = ".vault_first_key_given"
M.SONG_FOLDER = "Secret Vault"
M.SONG_UNLOCKED = ".vault_song_unlocked_"      -- .. the song's unique id

M.NAMEPLATE_POOLS = {
    {361, 362, 363, 364, 365, 366, 367, 368},   -- Simple
    {369, 370, 371, 372, 373, 374, 375},          -- Gold
    {376, 377, 378, 379, 380},                    -- OpTk
}

-- My Room furniture found in each chest, with its rarity. My Room hands over what a grant counter adds
-- (.myroom_<id>) and writes how many the save owns (.myroom_held_<id>); one in stock or placed counts
-- as obtained. Furniture can come again: an owned piece keeps a fifth of its chance.
M.FURNITURE_POOLS = {
    { { "wooden_chest", "Common" }, { "barrel", "Common" }, { "coin_piles", "Uncommon" },
      { "pirate_key", "Uncommon" }, { "pirate_swords", "Uncommon" } },                       -- Simple
    { { "gold_bag", "Uncommon" }, { "skull", "Uncommon" }, { "anchor", "Uncommon" },
      { "gold_key", "Rare" }, { "cannon", "Rare" } },                                        -- Gold
    { { "caspian_mug", "Rare" }, { "chalice", "Rare" }, { "crystal", "Epic" },
      { "fishtank", "Legendary" } },                                                        -- OpTk
}
M.NAMEPLATE_W = 15    -- the chance of a chest's nameplates (one draw among those not unlocked)
M.OWNED_SHARE = 1 / 5 -- what is left of an owned piece's chance

local CHEST_FALLBACK = { "Wooden Chest", "Golden Chest", "Platinum Chest" }

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

local function iterateCsharpList(list)
    local arr = {}
    if list == nil then return arr end
    local e = list:GetEnumerator()
    while e:MoveNext() do table.insert(arr, e.Current) end
    return arr
end

-- the song's highest chart level over its difficulties
function M.maxChartLevel(node)
    local maxLv = 0
    for diff = 0, 4 do
        local ch = node:GetChart(diff)
        if ch ~= nil and ch.Level > 0 then
            maxLv = math.max(maxLv, ch.Level)
        end
    end
    return maxLv
end

-- the songs of the vault's folder, by their highest chart level: up to 1 in chest 1, 2 in chest 2, 3+ in chest 3
function M.buildSongPools()
    songPools = {{}, {}, {}}

    local lsls = GenerateSongListSettings()
    lsls.ModuloPagination     = false
    lsls.AppendMainRandomBox  = false
    lsls.AppendSubRandomBoxes = false
    lsls.SubBackBoxFrequency  = 0
    lsls.RootGenreFolder      = M.SONG_FOLDER
    lsls.IgnoreUnlockables    = true

    local vaultList = RequestSongList(lsls)
    if vaultList == nil then return end

    local allNodes = iterateCsharpList(vaultList:SearchSongsByPredicate(function(n) return true end))
    for _, node in ipairs(allNodes) do
        local lv = M.maxChartLevel(node)
        if lv <= 1 then
            table.insert(songPools[1], node)
        elseif lv == 2 then
            table.insert(songPools[2], node)
        else
            table.insert(songPools[3], node)
        end
    end
end

-- the chest's songs (after buildSongPools / enter)
function M.songPool(idx)
    return songPools[idx]
end

local function getUnobtainedSongs(poolIdx)
    local save = sf()
    local out = {}
    for _, node in ipairs(songPools[poolIdx]) do
        if not save:GetGlobalTrigger(M.SONG_UNLOCKED .. node.UniqueId) then
            table.insert(out, node)
        end
    end
    return out
end

local function getUnobtainedNameplates(poolIdx)
    local save = sf()
    local out = {}
    for _, id in ipairs(M.NAMEPLATE_POOLS[poolIdx]) do
        if not save:IsNameplateUnlocked(id) then
            table.insert(out, id)
        end
    end
    return out
end

local function counter(name)
    return math.floor((tonumber(sf():GetGlobalCounter(name)) or 0) + 0.5)
end

local function furnitureOwned(id)
    return counter(".myroom_held_" .. id) > 0
end

local function getUnobtainedFurniture(poolIdx)
    local out = {}
    for _, f in ipairs(M.FURNITURE_POOLS[poolIdx]) do
        if not furnitureOwned(f[1]) then table.insert(out, f) end
    end
    return out
end

-- the chest's weighted pool as it stands on this save: { w, type, ... } entries
function M.chestPool(idx)
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
        table.insert(pool, {w = M.NAMEPLATE_W, type = "nameplate", pool = uNp})
    end

    -- furniture: each piece with the chance one nameplate of the chest has, a fifth of it once owned
    local per = M.NAMEPLATE_W / #M.NAMEPLATE_POOLS[idx]
    for _, f in ipairs(M.FURNITURE_POOLS[idx]) do
        local w = furnitureOwned(f[1]) and per * M.OWNED_SHARE or per
        table.insert(pool, {w = w, type = "furniture", id = f[1], rarity = f[2]})
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
    local r = math.random() * total
    local cum = 0
    for _, e in ipairs(pool) do
        cum = cum + e.w
        if r < cum then return e end
    end
    return pool[#pool]
end

local function processRoll(idx)
    local pool   = M.chestPool(idx)
    local entry  = rollFromPool(pool)
    local save   = sf()

    Keys.set(idx, Keys.count(idx) - 1)

    if entry.type == "snap" then
        return {type = "snap", openedChest = idx}
    elseif entry.type == "coins" then
        local amount = math.random(entry.min, entry.max)
        save:EarnCoins(amount)
        return {type = "coins", amount = amount, openedChest = idx}
    elseif entry.type == "key" then
        Keys.set(entry.target, Keys.count(entry.target) + 1)
        return {type = "key", target = entry.target, openedChest = idx}
    elseif entry.type == "nameplate" then
        local id = entry.pool[math.random(1, #entry.pool)]
        save:UnlockNameplate(id)
        return {type = "nameplate", id = id, info = NAMEPLATESLIST:GetById(id), openedChest = idx}
    elseif entry.type == "furniture" then
        -- My Room adds it to the room's stock on its next visit; owned from now on
        save:SetGlobalCounter(".myroom_" .. entry.id, counter(".myroom_" .. entry.id) + 1)
        save:SetGlobalCounter(".myroom_held_" .. entry.id, counter(".myroom_held_" .. entry.id) + 1)
        return {type = "furniture", id = entry.id, rarity = entry.rarity, openedChest = idx}
    elseif entry.type == "song" then
        local node = entry.pool[math.random(1, #entry.pool)]
        save:SetGlobalTrigger(M.SONG_UNLOCKED .. node.UniqueId, true)
        return {type = "song", node = node, openedChest = idx}
    end

    return {type = "snap", openedChest = idx}
end

-- ── API ────────────────────────────────────────────────────────────────────────

-- once per visit: the first visit gives 1 simple key (returns true then), and the song pools are built
function M.enter()
    local gifted = false
    local save = sf()
    if save:GetGlobalTrigger(M.FIRST_KEY) ~= true then
        Keys.set(1, Keys.count(1) + 1)
        save:SetGlobalTrigger(M.FIRST_KEY, true)
        gifted = true
    end

    M.buildSongPools()
    return gifted
end

function M.keyCount(idx)
    return Keys.count(idx)
end

-- the chest's name in the game's language
function M.name(idx)
    return Texts.get("chest_" .. idx, CHEST_FALLBACK[idx])
end

-- what is left to get from the chest (songs, nameplates and furniture not obtained yet), and its total
function M.stock(idx)
    local left  = #getUnobtainedSongs(idx) + #getUnobtainedNameplates(idx) + #getUnobtainedFurniture(idx)
    local total = #songPools[idx] + #M.NAMEPLATE_POOLS[idx] + #M.FURNITURE_POOLS[idx]
    return left, total
end

-- opens chest idx with one of its keys (the caller checks keyCount first): the reward is saved at once
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
        local songRarity = songLevelToRarity(M.maxChartLevel(reward.node))
        modal:Activate(0, songRarity, 4, reward.node, nil)
    elseif rtype == "furniture" then
        -- the picture is the one My Room shares for each of its reward items
        modal:Activate(0, RARITY_MAP[reward.rarity] or 1, 5, Texts.get("furniture_" .. reward.id, reward.id), reward.rarity,
            "myroom_thumb_" .. reward.id, Texts.get("new_furniture", "New Furniture!"))
    else
        return false
    end
    return true
end

return M
