---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local
-- vault.lua — Vault interior phase for secret_vault_rw.
--
-- Vault.init(tx, snd, txts)
-- Vault.reset()
-- Vault.draw()
-- Vault.update()   — returns "back" to exit, nil otherwise

local M = {}

local tx   = {}
local snd  = {}
local txts = {}

function M.init(t, s, txtsRef)
    tx   = t
    snd  = s
    txts = txtsRef
end

function M.reset()
end

function M.draw()
    if tx["Vault/Bg"] then
        tx["Vault/Bg"]:Draw(0, 0)
    end
end

function M.update()
    local decide = INPUT:Pressed("Decide") or INPUT:KeyboardPressed("Return")
    local cancel = INPUT:Pressed("Cancel") or INPUT:KeyboardPressed("Escape")
    if decide or cancel then
        return "back"
    end
    return nil
end

return M
