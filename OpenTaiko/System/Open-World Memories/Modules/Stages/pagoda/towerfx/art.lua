-- Floor-select ambience sprites (Textures/tower/fx), created and freed with the scene.

local M = {}

local DIR = "Textures/tower/fx/"
local NAMES = { "mote", "glow", "shaft", "streak", "sparkle", "ember", "mist", "puff",
                "leaf_a", "leaf_b", "petal", "bird" }

local _tex = nil

function M.setup()
    if _tex ~= nil then return end
    _tex = {}
    for _, n in ipairs(NAMES) do _tex[n] = TEXTURE:CreateTexture(DIR .. n .. ".png") end
end

function M.dispose()
    if _tex == nil then return end
    for _, t in pairs(_tex) do t:Dispose() end
    _tex = nil
end

-- the sprite once it can be drawn, else nil
function M.get(name)
    local t = _tex ~= nil and _tex[name] or nil
    if t ~= nil and t.Ready then return t end
    return nil
end

return M
