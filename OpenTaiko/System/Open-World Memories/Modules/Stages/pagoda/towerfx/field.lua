-- Stateless particle fields for the floor-select ambience.

local F = {}

F.TAU = math.pi * 2

-- 0..1, the same for the same three numbers; stands in for math.random, whose stream the corruption effect uses
function F.rand(a, b, c)
    local v = math.sin(a * 12.9898 + b * 78.233 + c * 37.719) * 43758.5453
    return v - math.floor(v)
end

function F.lerp(a, b, t) return a + (b - a) * t end

function F.smooth(e0, e1, x)
    local t = math.max(0, math.min(1, (x - e0) / (e1 - e0)))
    return t * t * (3 - 2 * t)
end

-- particle i of a looping field at time t: cycle number, 0..1 age and life (s); callers hash the
-- cycle number into its spawn values
function F.cycle(i, salt, t, lifeMin, lifeMax)
    local life = lifeMin + (lifeMax - lifeMin) * F.rand(i, salt, 0.5)
    local p = t / life + F.rand(i, salt, 1.5)
    local k = math.floor(p)
    return k, p - k, life
end

-- 0..1 over a life: rises over the first `fin` and falls over the last `fout` (fractions of it)
function F.fade(u, fin, fout)
    local a = 1
    if fin > 0 and u < fin then a = u / fin end
    if fout > 0 and u > 1 - fout then a = math.min(a, (1 - u) / fout) end
    return a
end

-- x wrapped onto [-margin, width + margin), for things that drift across the whole screen
function F.wrap(x, width, margin)
    local span = width + 2 * margin
    return (x % span) - margin
end

-- one centred sprite; the state stays on the texture until F.reset
function F.sprite(tx, x, y, sx, sy, rot, r, g, b, a)
    tx:SetScale(sx, sy)
    tx:SetRotation(rot)
    tx:SetColor(r, g, b)
    tx:SetOpacity(math.max(0, math.min(1, a)))
    tx:DrawAtAnchor(x, y, "center")
end

function F.reset(tx)
    if tx == nil then return end
    tx:SetScale(1, 1) ; tx:SetRotation(0) ; tx:SetOpacity(1)
    tx:SetColor(1, 1, 1) ; tx:SetBlendMode("Normal")
end

return F
