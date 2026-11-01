-- Sixth end-credits background (Textures/loading/dan.jpg).

local Easing = require("Easing")
local assets = require("encounter/assets")
local Config = require("Core.Config")
local Animation = Config.load("Config/animation.json")

local M = {}

M.holes = false

local KEY = "cr_loading_dan"
local PAN_SEC = Animation.encounter.credits.loading.pan_s

local _tx = nil
local _u = 0

function M.keys()
    return { KEY }
end

function M.open()
    _tx = assets.get(KEY)
    _u = 0
end

function M.update(_, u)
    _u = u
end

-- a = opacity, c = brightness
function M.draw(a, c)
    if _tx == nil or not _tx.Ready or a <= 0 then return end
    local res = THEME:GetResolution()
    local s = math.max(res.X / math.max(1, _tx.Width), res.Y / math.max(1, _tx.Height))
    local over = math.max(0, _tx.Height * s - res.Y)
    _tx:SetScale(s, s)
    _tx:SetColor(c, c, c)
    _tx:SetOpacity(math.min(1, a))
    _tx:DrawAtAnchor(res.X * 0.5, -over * Easing.inOutSine(_u / PAN_SEC), "top")
    _tx:SetOpacity(1)
    _tx:SetColor(1, 1, 1)
    _tx:SetScale(1, 1)
end

function M.close()
    _tx = nil
end

return M
