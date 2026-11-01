-- Selector carets toward the available neighbours (separate up and down art).

local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")
local metrics = Layout.selectui.metrics

local C = {}

local DIR = "Textures/selector/"

local OFF_UP = Layout.selectui.carets.up_offset       -- caret face centres from the reference top-left
local OFF_DN = Layout.selectui.carets.down_offset
local Move = Animation.selectui.carets
local BOB_AMP, BOB_SPD = Move.bob_px, Move.bob_speed
local PULSE = Move.pulse_scale

local _up, _dn = nil, nil

function C.setup()
    if _up ~= nil then return end
    _up = TEXTURE:CreateTexture(DIR .. "chevron_up.png")
    _dn = TEXTURE:CreateTexture(DIR .. "chevron.png")
end

function C.dispose()
    if _up ~= nil then _up:Dispose() ; _dn:Dispose() ; _up, _dn = nil, nil end
end

-- the scale is about the png centre, so the face centre is placed through its offset from it
local function draw_at(tex, m, x, y, s)
    tex:SetScale(s, s)
    tex:DrawAtAnchor(x + (m.w * 0.5 - m.cx) * s, y + (m.h * 0.5 - m.cy) * s, "center")
    tex:SetScale(1, 1)
end

function C.draw(showUp, showDown, t, rx, ry)
    if _up == nil then return end
    local bob = (math.sin(t * BOB_SPD) * 0.5 + 0.5) * BOB_AMP
    local s = 1 + PULSE * math.sin(t * BOB_SPD)
    if showUp and _up.Ready then
        draw_at(_up, metrics.chevron_up, rx + OFF_UP[1], ry + OFF_UP[2] - bob, s)
    end
    if showDown and _dn.Ready then
        draw_at(_dn, metrics.chevron, rx + OFF_DN[1], ry + OFF_DN[2] + bob, s)
    end
end

return C
