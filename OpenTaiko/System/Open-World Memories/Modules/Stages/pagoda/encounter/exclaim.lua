-- "!" bubble over Ume's head on surprised lines.

local Easing = require("Easing")
local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")

local M = {}

local Look = Layout.encounter.exclaim
local Anim = Animation.encounter.exclaim
local POP_SEC, OUT_SEC = Anim.pop_s, Anim.out_s
local BOB_PX, BOB_HZ = Anim.bob_px, Anim.bob_hz
local DX, DY = Look.tail_dx, Look.tail_dy      -- tail tip offset from the top of Ume's head

local _tex = nil               -- Script.lua's tex.exclaim
local _t, _outT = nil, nil

function M.setup(tex)
    if tex ~= nil and tex.exclaim ~= nil then _tex = tex.exclaim end
end

function M.start() _t, _outT = 0, nil end

function M.stop()
    if _t ~= nil and _outT == nil then _outT = 0 end
end

function M.reset() _t, _outT = nil, nil end

function M.update(dt)
    if _t == nil then return end
    _t = _t + dt
    if _outT ~= nil then
        _outT = _outT + dt
        if _outT >= OUT_SEC then _t, _outT = nil, nil end
    end
end

-- x, y: top of Ume's head on screen (nil: not drawn); ox, oy: screen shake
function M.draw(x, y, ox, oy)
    if _t == nil or x == nil or _tex == nil or not _tex.Ready then return end
    local s = Easing.outBack(_t / POP_SEC)
    if _outT ~= nil then s = s * (1 - Easing.inQuad(_outT / OUT_SEC)) end
    if s <= 0.01 then return end
    local bob = math.sin(_t * BOB_HZ * 2 * math.pi) * BOB_PX
    _tex:SetScale(s, s)
    _tex:DrawAtAnchor(x + DX + (ox or 0), y + DY + bob + (oy or 0), "bottomleft")
    _tex:SetScale(1, 1)
end

return M
