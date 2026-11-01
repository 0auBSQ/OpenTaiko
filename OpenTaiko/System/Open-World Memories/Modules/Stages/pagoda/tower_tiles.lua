-- Floor-select tiles for levels 1-10; Edge wrap keeps abutting tiles seamless.

local M = {}

local DIR = "Textures/tower/"

local NAMES = { "ground", "groundtostair", "stair", "summit", "groundfloor" }

M.KEY = {
    [1] = "ground", [2] = "ground", [3] = "ground", [4] = "ground",
    [5] = "groundtostair",
    [6] = "stair", [7] = "stair", [8] = "stair",
    [9] = "summit",
    [10] = "groundfloor",
}

local _tex = nil

function M.setup()
    if _tex ~= nil then return end
    _tex = {}
    for _, name in ipairs(NAMES) do
        local t = TEXTURE:CreateTexture(DIR .. name .. ".png")
        t:SetWrapMode("Edge")
        _tex[name] = t
    end
end

function M.dispose()
    if _tex == nil then return end
    for _, t in pairs(_tex) do t:Dispose() end
    _tex = nil
end

function M.forLevel(L)
    if _tex == nil then return nil end
    local key = M.KEY[L]
    return key and _tex[key] or nil
end

return M
