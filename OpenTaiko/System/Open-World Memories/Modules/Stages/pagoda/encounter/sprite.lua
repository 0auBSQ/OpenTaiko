-- Draws a cropped sprite from its manifest entry (w, h and foot point fx, fy in its own px).
-- Pivots, (x, y) = the screen point the sprite is placed at:
--   "foot"   (x, y) is the foot point and the sprite turns about it
--   "center" (x, y) is the foot point while unrotated; the sprite turns about its own middle
--   "top"    (x, y) is the top-centre and the sprite turns about it
--
-- Angles are degrees, counter-clockwise like SetRotation. Mirrored: meta.mirror's baked texture, or a
-- negative x scale about the foot point.

local M = {}

-- 1 turns pivot offsets the same way SetRotation turns the image; -1 the other way
M.ROT_SIGN = 1

local RAD = math.pi / 180
local cos, sin = math.cos, math.sin

-- (vx, vy) turned deg degrees counter-clockwise on a y-down screen
local function turn(vx, vy, deg)
    if deg == 0 then return vx, vy end
    local a = M.ROT_SIGN * deg * RAD
    local c, s = cos(a), sin(a)
    return vx * c + vy * s, -vx * s + vy * c
end

local function centre(w, h, fx, fy, x, y, sx, sy, rot, pivot)
    local px, py
    if pivot == "top" then px, py = w * 0.5, 0
    else px, py = fx or w * 0.5, fy or h end
    local vx, vy = (w * 0.5 - px) * sx, (h * 0.5 - py) * sy
    if pivot ~= "center" then vx, vy = turn(vx, vy, rot) end
    return x + vx, y + vy
end

-- the screen point the image centre lands on
function M.center(meta, x, y, o)
    local sx, sy, rot, pivot = 1, 1, 0, "foot"
    if o ~= nil then
        sx, sy, rot, pivot = o.sx or 1, o.sy or 1, o.rot or 0, o.pivot or "foot"
    end
    return centre(meta.w, meta.h, meta.fx, meta.fy, x, y, sx, sy, rot, pivot)
end

-- the screen point of the sprite's own pixel (lx, ly), drawn with the same placement
function M.point(meta, x, y, o, lx, ly)
    local cx, cy = M.center(meta, x, y, o)
    local sx, sy, rot = 1, 1, 0
    if o ~= nil then sx, sy, rot = o.sx or 1, o.sy or 1, o.rot or 0 end
    local vx, vy = turn((lx - meta.w * 0.5) * sx, (ly - meta.h * 0.5) * sy, rot)
    return cx + vx, cy + vy
end

-- a texture centred on (cx, cy); o = { sx, sy, rot, alpha, r, g, b, blend, grad }, all optional
-- (grad: a GRADIENT map for this draw alone)
function M.drawAt(tx, cx, cy, o)
    if tx == nil or not tx.Ready then return end
    local sx, sy, rot, alpha, r, g, b, blend, grad = 1, 1, 0, 1, 1, 1, 1, "Normal", nil
    if o ~= nil then
        sx, sy, rot = o.sx or 1, o.sy or 1, o.rot or 0
        alpha, r, g, b = o.alpha or 1, o.r or 1, o.g or 1, o.b or 1
        blend, grad = o.blend or "Normal", o.grad
    end
    if alpha <= 0 then return end
    tx:SetScale(sx, sy)
    tx:SetRotation(rot)
    tx:SetOpacity(alpha)
    tx:SetColor(r, g, b)
    tx:SetBlendMode(blend)
    if grad ~= nil then tx:SetGradientMap(grad) end
    tx:DrawAtAnchor(cx, cy, "center")
    tx:SetScale(1, 1)
    tx:SetRotation(0)
    tx:SetOpacity(1)
    tx:SetColor(1, 1, 1)
    tx:SetBlendMode("Normal")
    if grad ~= nil then tx:ClearGradientMap() end
end

-- a sprite placed at (x, y) through its pivot (see the top of the file)
function M.draw(tx, meta, x, y, o)
    if tx == nil or not tx.Ready then return end
    local cx, cy
    if meta ~= nil then
        cx, cy = M.center(meta, x, y, o)
    else
        local sx, sy, rot, pivot = 1, 1, 0, "foot"
        if o ~= nil then sx, sy, rot, pivot = o.sx or 1, o.sy or 1, o.rot or 0, o.pivot or "foot" end
        cx, cy = centre(tx.Width, tx.Height, nil, nil, x, y, sx, sy, rot, pivot)
    end
    M.drawAt(tx, cx, cy, o)
end

return M
