-- Vault.Keys: the vault's three chest keys (1 simple, 2 gold, 3 optk), counted in player 1's save
-- the vault is open once the trigger .vault_opened is set

local Texts = require("Vault.Texts")

local M = {}

M.IDS = { "simple", "gold", "optk" }
M.COUNTERS = { ".vault_key_count_simple", ".vault_key_count_gold", ".vault_key_count_optk" }
M.OPENED = ".vault_opened"

local FALLBACK = { "Vault Key", "Vault Key (Gold)", "Vault Key (OpTk)" }
local INDEX = { simple = 1, gold = 2, optk = 3 }

-- the key's number (1-3) from its number or id
function M.index(key)
    if type(key) == "number" then return key end
    return INDEX[key]
end

-- counters come back as doubles: kept as whole numbers here
function M.count(key)
    local sf = GetSaveFile(0)
    return math.floor((tonumber(sf:GetGlobalCounter(M.COUNTERS[M.index(key)])) or 0) + 0.5)
end

function M.set(key, n)
    GetSaveFile(0):SetGlobalCounter(M.COUNTERS[M.index(key)], n)
end

function M.add(key, n)
    M.set(key, M.count(key) + (n or 1))
end

-- the key's name in the game's language
function M.name(key)
    local i = M.index(key)
    return Texts.get("key_" .. i, FALLBACK[i])
end

function M.vaultOpen()
    return GetSaveFile(0):GetGlobalTrigger(M.OPENED) == true
end

return M
