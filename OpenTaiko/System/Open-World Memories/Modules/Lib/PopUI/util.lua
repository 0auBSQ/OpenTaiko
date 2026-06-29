---@diagnostic disable: undefined-global, lowercase-global
-- PopUI/util.lua — tiny math/color/geometry helpers. Engine globals are touched only inside functions
-- (never at load) so the module imports cleanly even under a headless test harness.

local U = {}

function U.clamp(v, lo, hi) if v < lo then return lo elseif v > hi then return hi else return v end end
function U.lerp(a, b, t) return a + (b - a) * t end
function U.round(v) return math.floor(v + 0.5) end

-- lerp two {r,g,b,a} colors
function U.lerpColor(a, b, t)
    return {
        U.lerp(a[1], b[1], t), U.lerp(a[2], b[2], t),
        U.lerp(a[3], b[3], t), U.lerp(a[4] or 255, b[4] or 255, t),
    }
end

-- multiply a color's brightness (keeps alpha); f>1 lightens via blend to white, f<1 darkens
function U.shade(c, f)
    if f <= 1 then
        return { c[1] * f, c[2] * f, c[3] * f, c[4] or 255 }
    else
        local t = f - 1
        return { U.lerp(c[1], 255, t), U.lerp(c[2], 255, t), U.lerp(c[3], 255, t), c[4] or 255 }
    end
end

function U.withAlpha(c, a) return { c[1], c[2], c[3], a } end

-- point inside a rounded rect (x,y,w,h, radius). Used for accurate button/panel hit-testing.
function U.pointInRoundRect(px, py, x, y, w, h, r)
    if px < x or px >= x + w or py < y or py >= y + h then return false end
    r = math.min(r or 0, w * 0.5, h * 0.5)
    if r <= 0 then return true end
    -- inside the non-corner cross?
    if px >= x + r and px < x + w - r then return true end
    if py >= y + r and py < y + h - r then return true end
    -- corner test
    local cx = (px < x + r) and (x + r) or (x + w - r)
    local cy = (py < y + r) and (y + r) or (y + h - r)
    local dx, dy = px - cx, py - cy
    return dx * dx + dy * dy <= r * r
end

function U.pointInRect(px, py, x, y, w, h)
    return px >= x and px < x + w and py >= y and py < y + h
end

-- packed key for caching COLOR objects
function U.packRGBA(r, g, b, a)
    return math.floor(r) * 0x1000000 + math.floor(g) * 0x10000 + math.floor(b) * 0x100 + math.floor(a or 255)
end

return U
