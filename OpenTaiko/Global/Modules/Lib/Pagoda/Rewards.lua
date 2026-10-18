-- Pagoda.Rewards: what a challenge clear gives. A floor's first clear gives its nameplate (the shipped
-- pack's "Pagoda Placeholder" ones, 381-397), then each entry of the floor's pool rolls on its own chance,
-- so one clear can give several. grant() saves everything at once; show() then plays one pop-up per
-- reward through the "modal" ROActivity. Vault keys become coins while the secret vault is shut, and all
-- of a clear's coins make one pop-up. Furniture goes to My Room (.myroom_<id> to hand over,
-- .myroom_held_<id> owned).

local Keys  = require("Vault.Keys")
local Texts = require("Pagoda.Texts")

local M = {}

M.NAMEPLATE_BASE = 380
-- the floors whose first clear gives a nameplate -> its number among them (id NAMEPLATE_BASE + n)
M.NAMEPLATES = {
    [1] = 1, [5] = 2, [6] = 3, [10] = 4,
    [11] = 5, [12] = 6, [13] = 7, [14] = 8, [15] = 9, [16] = 10, [17] = 11, [18] = 12, [19] = 13, [20] = 14,
    [21] = 15, [24] = 16, [27] = 17,
}

M.COIN_CHANCE = 0.5

-- My Room furniture: the pop-up's rarity and the name shown without a string
M.FURNITURE = {
    tree          = { rarity = "Common",    name = "Tree" },
    small_rock    = { rarity = "Common",    name = "Rock" },
    large_tree    = { rarity = "Uncommon",  name = "Large Tree" },
    large_rock    = { rarity = "Uncommon",  name = "Large Rock" },
    log           = { rarity = "Rare",      name = "Log" },
    stone_lantern = { rarity = "Rare",      name = "Stone Lantern" },
    bonsai        = { rarity = "Common",    name = "Bonsai" },
    fox           = { rarity = "Uncommon",  name = "Fox Model" },
    fox_statue    = { rarity = "Uncommon",  name = "Fox Statue" },
    mini_pagoda   = { rarity = "Legendary", name = "Miniature Pagoda" },
}

-- the secret vault's keys (Vault.Keys): pop-up rarity, and coins per key while the vault is shut
M.KEYS = {
    simple = { rarity = "Uncommon", coins = 50 },
    gold   = { rarity = "Rare", coins = 100 },
    optk   = { rarity = "Epic", coins = 300 },
}
M.KEY_PICTURE = "pagoda_reward_key_"      -- the shared textures the key pop-ups show

-- the modal's rarity numbers
local RARITY_INT = { Poor = 0, Common = 0, Uncommon = 1, Rare = 2, Epic = 3, Legendary = 4, Mythical = 4 }

local function F(id, pct) return { furniture = id, chance = pct / 100 } end
local function K(key, pct, lo, hi) return { key = key, chance = pct / 100, count = { lo or 1, hi or lo or 1 } } end

-- each floor's coins (a range, at COIN_CHANCE) and pool
M.POOLS = {
    [1]  = { coins = { 10, 20 }, F("tree", 4), F("small_rock", 4) },
    [2]  = { coins = { 10, 20 }, F("tree", 4), F("small_rock", 4) },
    [3]  = { coins = { 10, 20 }, F("tree", 4), F("small_rock", 4), F("large_tree", 1), F("large_rock", 1) },
    [4]  = { coins = { 20, 30 }, F("tree", 5), F("small_rock", 5), F("large_tree", 2), F("large_rock", 2) },
    [5]  = { coins = { 20, 30 }, F("tree", 6), F("small_rock", 6), F("large_tree", 3), F("large_rock", 3) },
    [6]  = { coins = { 20, 30 }, F("tree", 6), F("small_rock", 6), F("large_tree", 3), F("large_rock", 3), F("log", 1) },
    [7]  = { coins = { 20, 30 }, F("tree", 6), F("small_rock", 6), F("large_tree", 3), F("large_rock", 3), F("log", 1) },
    [8]  = { coins = { 20, 30 }, F("tree", 6), F("small_rock", 6), F("large_tree", 3), F("large_rock", 3), F("log", 2),
             F("stone_lantern", 1) },
    [9]  = { coins = { 30, 50 }, F("tree", 7), F("small_rock", 7), F("large_tree", 4), F("large_rock", 4), F("log", 2),
             F("stone_lantern", 2) },
    [10] = { coins = { 30, 50 }, F("tree", 8), F("small_rock", 8), F("large_tree", 5), F("large_rock", 5), F("log", 3),
             F("stone_lantern", 3) },
    [11] = { coins = { 30, 50 }, F("bonsai", 4), F("fox", 3), K("simple", 3), F("stone_lantern", 2) },
    [12] = { coins = { 30, 50 }, F("bonsai", 4), F("fox", 3), K("simple", 3), F("stone_lantern", 2) },
    [13] = { coins = { 30, 50 }, F("bonsai", 5), F("fox", 4), K("simple", 4), F("stone_lantern", 3) },
    [14] = { coins = { 50, 70 }, F("bonsai", 5), F("fox", 4), K("simple", 4), F("stone_lantern", 3) },
    [15] = { coins = { 50, 70 }, F("bonsai", 6), F("fox", 5), K("simple", 5), F("stone_lantern", 4) },
    [16] = { coins = { 70, 90 }, F("bonsai", 6), F("fox", 4), F("fox_statue", 3), K("simple", 10),
             F("stone_lantern", 4), K("gold", 2) },
    [17] = { coins = { 90, 110 }, F("bonsai", 7), F("fox", 4), F("fox_statue", 4), K("simple", 15),
             F("stone_lantern", 5), K("gold", 5) },
    [18] = { coins = { 110, 130 }, F("bonsai", 8), F("fox", 5), F("fox_statue", 5), K("simple", 20),
             F("stone_lantern", 6), K("gold", 10), K("optk", 2) },
    [19] = { coins = { 130, 150 }, F("bonsai", 9), F("fox", 6), F("fox_statue", 6), K("simple", 25),
             F("stone_lantern", 7), K("gold", 15), K("optk", 4) },
    [20] = { coins = { 150, 200 }, F("bonsai", 10), F("fox", 7), F("fox_statue", 7), K("simple", 30, 1, 2),
             F("stone_lantern", 8), K("gold", 20), K("optk", 6), F("mini_pagoda", 10) },
}
-- the roofs, by their first floor
M.ROOF_POOLS = {
    { from = 27, coins = { 400, 500 }, K("simple", 50, 1, 5), K("gold", 35), K("optk", 12) },
    { from = 24, coins = { 300, 400 }, K("simple", 50, 1, 4), K("gold", 30), K("optk", 10) },
    { from = 21, coins = { 200, 300 }, K("simple", 40, 1, 3), K("gold", 25), K("optk", 8) },
}

-- the reward pool of a level: { coins = { lo, hi }, entries... } with { furniture, chance } or
-- { key, chance, count = { lo, hi } } entries; nil when the level gives nothing
function M.poolOf(level)
    if M.POOLS[level] ~= nil then return M.POOLS[level] end
    for _, p in ipairs(M.ROOF_POOLS) do
        if level >= p.from then return p end
    end
    return nil
end

-- the nameplate id a level's first clear gives, or nil
function M.nameplateOf(level)
    local n = M.NAMEPLATES[level]
    return n ~= nil and tostring(M.NAMEPLATE_BASE + n) or nil
end

local function counter(sf, name)
    return math.floor((tonumber(sf:GetGlobalCounter(name)) or 0) + 0.5)
end

local _shared = false

-- the key pictures, shared once per session
function M.setup()
    if _shared then return end
    _shared = true
    for id in pairs(M.KEYS) do SHARED:SetSharedTextureUsingAbsolutePath(M.KEY_PICTURE .. id, Keys.picture(id)) end
end

-- the rewards of a challenge clear of level, saved now; returns the pop-ups to show, in order:
-- { kind = "nameplate", id }, { kind = "furniture", id }, { kind = "key", key, count }, { kind = "coins", amount }
function M.grant(level)
    local sf = GetSaveFile(0)
    local out = {}
    if sf == nil then return out end

    local id = M.nameplateOf(level)
    if id ~= nil then
        if not sf:IsNameplateUnlocked(id) then
            sf:UnlockNameplate(id)
            out[#out + 1] = { kind = "nameplate", id = id }
        end
    end

    local pool = M.poolOf(level)
    if pool == nil then return out end
    local coins = 0
    if math.random() < M.COIN_CHANCE then coins = math.random(pool.coins[1], pool.coins[2]) end
    local vaultOpen = Keys.vaultOpen()
    for _, e in ipairs(pool) do
        if math.random() < e.chance then
            if e.furniture ~= nil then
                -- My Room adds it to the room's stock on its next visit; owned from now on
                sf:SetGlobalCounter(".myroom_" .. e.furniture, counter(sf, ".myroom_" .. e.furniture) + 1)
                sf:SetGlobalCounter(".myroom_held_" .. e.furniture, counter(sf, ".myroom_held_" .. e.furniture) + 1)
                out[#out + 1] = { kind = "furniture", id = e.furniture }
            else
                local k = M.KEYS[e.key]
                local count = math.random(e.count[1], e.count[2])
                if vaultOpen then
                    Keys.add(e.key, count)
                    out[#out + 1] = { kind = "key", key = e.key, count = count }
                else
                    coins = coins + count * k.coins
                end
            end
        end
    end
    if coins > 0 then
        sf:EarnCoins(coins)
        out[#out + 1] = { kind = "coins", amount = coins }
    end
    return out
end

-- starts the pop-up of one reward from grant(); false when it can't show. The furniture pop-up shows My
-- Room's shared thumbnail "myroom_thumb_<id>", the key pop-up the picture setup() shared.
function M.show(r)
    local modal = ROACTIVITY:GetROActivity("modal")
    if modal == nil or r == nil then return false end
    if r.kind == "coins" then
        local sf = GetSaveFile(0)
        modal:Activate(0, 1, 0, r.amount, sf ~= nil and sf.Coins or r.amount)
    elseif r.kind == "nameplate" then
        local info = NAMEPLATESLIST:GetById(r.id)
        if info == nil then return false end
        modal:Activate(0, RARITY_INT[info.Rarity] or 0, 3, info, nil)
    elseif r.kind == "furniture" then
        local f = M.FURNITURE[r.id]
        modal:Activate(0, RARITY_INT[f.rarity] or 0, 5, Texts.get("furniture_" .. r.id, f.name), f.rarity,
            "myroom_thumb_" .. r.id, Texts.get("new_furniture", "New Furniture!"))
    elseif r.kind == "key" then
        local k = M.KEYS[r.key]
        local name = Keys.name(r.key)
        if r.count > 1 then name = name .. " ×" .. r.count end
        modal:Activate(0, RARITY_INT[k.rarity] or 0, 5, name, k.rarity, M.KEY_PICTURE .. r.key, Texts.get("new_key", "New Key!"))
    else
        return false
    end
    return true
end

-- true while a reward's pop-up plays; it reads its own input
function M.update()
    local modal = ROACTIVITY:GetROActivity("modal")
    if modal == nil or not modal.IsActive then return false end
    modal:Update()
    return modal.IsActive
end

function M.draw()
    local modal = ROACTIVITY:GetROActivity("modal")
    if modal ~= nil and modal.IsActive then modal:Draw() end
end

-- drops a pop-up still playing (the screen closing under it); the reward stays saved
function M.stop()
    local modal = ROACTIVITY:GetROActivity("modal")
    if modal ~= nil and modal.IsActive then modal:Deactivate() end
end

return M
