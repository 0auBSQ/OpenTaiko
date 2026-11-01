-- The floor's monsters on the encounter screen, lead first.

local assets   = require("encounter/assets")
local monsters = require("encounter/monsters")
local body     = require("encounter/monster_body")
local Config   = require("Core.Config")
local Animation = Config.load("Config/animation.json")

local M = {}

-- seconds between one member's notice, charge or vanish and the next one's
local Anim = Animation.encounter.monster
local NOTICE_LAG = Anim.notice_lag_s
local CHARGE_LAG = Anim.charge_lag_s
local VANISH_LAG = Anim.vanish_lag_s

local _bodies = {}
local _pending = {}           -- { t, b, method, x, y }: a member's call waiting for its lag
local _charging, _arrived, _charged, _vanishing = false, false, false, false

-- group = monsters.forLevel(level)
function M.keys(group)
    local keys, seen = {}, {}
    for _, m in ipairs(group) do
        for _, k in ipairs(body.keys(m)) do
            if not seen[k] then
                seen[k] = true
                keys[#keys + 1] = k
            end
        end
    end
    return keys
end

function M.open(group, level)
    M.close()
    local metas = {}
    for i, m in ipairs(group) do metas[i] = assets.meta("m_" .. m.key) end
    local x, footY = monsters.place(group, metas)
    for i, m in ipairs(group) do
        _bodies[i] = body.new(m, x + m.dx, footY + m.dy, (level or 0) * 10 + i)
    end
end

-- b:method(x, y) on the lead now, then on each next member lag seconds later
local function stagger(lag, method, x, y)
    for i, b in ipairs(_bodies) do
        if i == 1 then b[method](b, x, y)
        else _pending[#_pending + 1] = { t = (i - 1) * lag, b = b, method = method, x = x, y = y } end
    end
end

local function runPending(dt)
    local keep = 0
    for i = 1, #_pending do
        local p = _pending[i]
        p.t = p.t - dt
        if p.t <= 0 then
            p.b[p.method](p.b, p.x, p.y)
        else
            keep = keep + 1
            _pending[keep] = p
        end
    end
    for i = #_pending, keep + 1, -1 do _pending[i] = nil end
end

function M.update(dt)
    runPending(dt)
    for _, b in ipairs(_bodies) do
        b:update(dt)
        if _charging and not _arrived and b:arrived() then _arrived, _charged = true, true end
    end
end

function M.notice()
    stagger(NOTICE_LAG, "notice")
end

-- the lead's left edge reaches targetX after sec seconds, each next member CHARGE_LAG later;
-- with no monster on screen it counts as arrived at once
function M.charge(targetX, sec)
    if _charging then return end
    _charging = true
    if _bodies[1] == nil or _bodies[1]:vanishing() then
        _charged = true
        return
    end
    stagger(CHARGE_LAG, "charge", targetX, sec)
end

-- true once, on the frame the first of them arrives
function M.charged()
    if _charged then
        _charged = false
        return true
    end
    return false
end

-- style = "squish" | "dissolve"
function M.vanish(style)
    if _vanishing then return end
    _vanishing = true
    stagger(VANISH_LAG, "vanish", style)
end

-- true once every member's vanish is over and nothing of them is drawn any more
function M.vanished()
    for _, b in ipairs(_bodies) do
        if not b.gone then return false end
    end
    return true
end

-- ox, oy: the screen shake
function M.drawShadow(alpha, ox, oy)
    alpha, ox, oy = alpha or 1, ox or 0, oy or 0
    for i = #_bodies, 1, -1 do _bodies[i]:drawShadow(alpha, ox, oy) end
end

-- alpha: opacity of the monsters and auras; r, g, b: the band's filter; eyes: 0..1;
-- ox, oy: the screen shake
function M.draw(alpha, r, g, b, eyes, ox, oy)
    alpha, r, g, b, eyes, ox, oy = alpha or 1, r or 1, g or 1, b or 1, eyes or 0, ox or 0, oy or 0
    for i = #_bodies, 1, -1 do _bodies[i]:draw(alpha, r, g, b, eyes, ox, oy) end
end

function M.close()
    for _, b in ipairs(_bodies) do b:close() end
    _bodies, _pending = {}, {}
    _charging, _arrived, _charged, _vanishing = false, false, false, false
end

return M
