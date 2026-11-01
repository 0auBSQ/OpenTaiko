-- Fifth end-credits background (the forest stair).

local assets  = require("encounter/assets")
local forest  = require("encounter/bands/forest")
local actor   = require("encounter/credits/actor")
local Emitter = require("fx/emitter")
local presets = require("fx/presets")
local Config  = require("Core.Config")
local Layout    = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")

local M = {}

M.holes = true

local Look = Layout.encounter.credits.fall
local Anim = Animation.encounter.credits.fall

local LEVEL = 6
-- the line down the treads, in 1920 space: y = Y0 - SLOPE * x, from X0 off the right to X1 off the left
local X0, X1, Y0, SLOPE = Look.x0, Look.x1, Look.y0, Look.slope
local SPEED, ACCEL = Anim.speed_px_per_s, Anim.accel    -- px/s along x, and its growth (px/s^2)
local BOUNCE_SEC = Anim.bounce_s
local SQUASH, SQUASH_SEC = Anim.squash, Anim.squash_s
local SQUASH_WIDEN = Anim.squash_widen
local DUST_COUNT = Anim.dust_count

-- start (s into the scene), spin (degrees/s, counter-clockwise), bounce height
local TUMBLERS = {
    { key = "ume_fail" },
    { key = "m_norm3" },
    { key = "m_norm3" },
}
for i, d in ipairs(TUMBLERS) do
    local t = Anim.tumblers[i]
    d.at, d.spin, d.hop = t.at_s, t.spin_deg_per_s, t.hop_px
end

local _u = 0
local _shadow, _shadowMeta = nil, nil
local _dust = nil
local _actors, _state = {}, {}
local _list = {}               -- the actors in draw order

local function dustKey()
    return Emitter.texKeys(presets.dust_puff)[1] or "fx_dust"
end

function M.keys()
    local keys = forest.keys(LEVEL, nil, "stair")
    for _, k in ipairs({ "ume_fail", "m_norm3", "fx_shadow", dustKey() }) do keys[#keys + 1] = k end
    return keys
end

function M.open()
    M.close()
    forest.open(LEVEL, nil, "stair")
    _shadow, _shadowMeta = assets.get("fx_shadow")
    _dust = Emitter.new(presets.dust_puff, (assets.get(dustKey())))
    _u = 0
    for i, d in ipairs(TUMBLERS) do
        local a = actor.new(d.key, X0, Y0 - SLOPE * X0, { shadowKey = (d.key == "ume_fail") and "ume_brave" or d.key })
        a.visible, a.pivot = false, "center"
        _actors[i], _list[i] = a, a
        _state[i] = { bounce = -1 }
    end
end

local function tumble(a, d, st, t)
    if t < 0 then a.visible = false return end
    local x = X0 - (SPEED * t + 0.5 * ACCEL * t * t)
    if x < X1 then a.visible = false return end
    local gy = Y0 - SLOPE * x
    local n = math.floor(t / BOUNCE_SEC)
    local q = t / BOUNCE_SEC - n
    if n ~= st.bounce then
        st.bounce = n
        if n > 0 and _dust ~= nil then _dust:burst(DUST_COUNT, x, gy) end
    end
    local land = (n > 0 and q * BOUNCE_SEC < SQUASH_SEC) and (1 - q * BOUNCE_SEC / SQUASH_SEC) or 0
    a.visible = true
    a.x, a.y, a.gy = x, gy, gy
    a.dy = -d.hop * 4 * q * (1 - q)
    a.rot = d.spin * t
    a.sx, a.sy = 1 + SQUASH_WIDEN * SQUASH * land, 1 - SQUASH * land
end

function M.update(dt, u)
    _u = u
    forest.update(dt)
    for i, d in ipairs(TUMBLERS) do tumble(_actors[i], d, _state[i], _u - d.at) end
    if _dust ~= nil then _dust:update(dt) end
end

-- drawn under the scene it fades into or from, so a = 1; c = 1 (the sky is dimmed over it)
function M.draw(a, c)
    forest.drawBehind(0, 0)
    forest.drawBack(0, 0)
    forest.drawMid(0, 0)
    actor.drawAll(_list, _shadow, _shadowMeta, a, c)
    if _dust ~= nil then
        local r, g, b = forest.groundDust()
        _dust:draw(0, 0, a, r * c, g * c, b * c)
    end
    forest.drawFront(0, 0)
end

function M.close()
    forest.close()
    if _dust ~= nil then _dust:clear() end
    _dust, _shadow, _shadowMeta = nil, nil, nil
    _actors, _state, _list = {}, {}, {}
end

return M
