-- Shared encounter background transform; art is placed in 1920x1080 space.

local Easing = require("Easing")
local Config = require("Core.Config")
local Animation = Config.load("Config/animation.json")
local Anim = Animation.encounter.bands.view

local M = {}

local PUSH_FROM = Anim.push_from_scale
local PUSH_SEC  = Anim.push_s
local REF_W, REF_H = 1920, 1080

local _t = 0
local _ox, _oy = 0, 0
local _art = true

function M.reset()
    _t = 0
end

function M.update(dt)
    _t = _t + dt
end

-- seconds since the last reset
function M.time()
    return _t
end

-- push_from_scale at reset, 1 once settled
function M.push()
    return PUSH_FROM + (1 - PUSH_FROM) * Easing.outCubic(_t / PUSH_SEC)
end

-- sets the shake offset added to every transformed point; with no argument returns the current one
function M.shake(ox, oy)
    if ox ~= nil then _ox, _oy = ox, oy or 0 end
    return _ox, _oy
end

-- draw scale for 1920-space art: push-in times resolution
function M.scale()
    local res = THEME:GetResolution()
    return M.push() * res.X / REF_W
end

-- 1920-space point to the screen, pushed in about the centre
function M.xf(x, y)
    local res = THEME:GetResolution()
    local s = M.push()
    local kx, ky = res.X / REF_W, res.Y / REF_H
    return res.X * 0.5 + (x - REF_W * 0.5) * s * kx + _ox,
           res.Y * 0.5 + (y - REF_H * 0.5) * s * ky + _oy
end

-- runs fn(...) without placed art and with the push-in settled; the view is restored afterwards
function M.effectsOnly(fn, ...)
    local t, art = _t, _art
    _t, _art = math.max(_t, PUSH_SEC), false
    local ok, err = pcall(fn, ...)
    _t, _art = t, art
    if not ok then error(err, 0) end
end

-- manifest x/y = top-left in 1920 space (0 when absent)
function M.drawPlaced(tx, meta, r, g, b, a)
    if not _art or tx == nil or not tx.Ready then return end
    a = a or 1
    if a <= 0 then return end
    local x, y = M.xf(meta and meta.x or 0, meta and meta.y or 0)
    local s = M.scale()
    tx:SetScale(s, s)
    tx:SetColor(r or 1, g or 1, b or 1)
    tx:SetOpacity(a)
    tx:DrawAtAnchor(x, y, "topleft")
    tx:SetScale(1, 1)
    tx:SetColor(1, 1, 1)
    tx:SetOpacity(1)
end

return M
