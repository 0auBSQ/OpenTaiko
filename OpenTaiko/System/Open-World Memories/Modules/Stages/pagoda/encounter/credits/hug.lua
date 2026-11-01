-- First end-credits background.

local Easing = require("Easing")
local assets = require("encounter/assets")
local scene  = require("encounter/scene")
local Config = require("Core.Config")
local Animation = Config.load("Config/animation.json")

local M = {}

local KEY = "cut20_final"
local Anim = Animation.encounter.credits.hug
local PUSH, PUSH_SEC = Anim.push, Anim.push_s   -- scale gained over the roll

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
    if a <= 0 then return end
    if _tx == nil or not _tx.Ready then
        scene.drawBlack(a)
        return
    end
    local res = THEME:GetResolution()
    local push = 1 + PUSH * Easing.inOutSine(_u / PUSH_SEC)
    _tx:SetScale(push * res.X / math.max(1, _tx.Width), push * res.Y / math.max(1, _tx.Height))
    _tx:SetColor(c, c, c)
    _tx:SetOpacity(math.min(1, a))
    _tx:DrawAtAnchor(res.X * 0.5, res.Y * 0.5, "center")
    _tx:SetOpacity(1)
    _tx:SetColor(1, 1, 1)
    _tx:SetScale(1, 1)
end

function M.close()
    _tx = nil
end

return M
