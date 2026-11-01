-- Three-slice box: left cap, stretched middle, right cap.

local M = {}

local function ready(tx) return tx ~= nil and tx.Ready end

local function reset(tx)
    tx:SetScale(1, 1) ; tx:SetOpacity(1) ; tx:SetColor(1, 1, 1) ; tx:SetBlendMode("Normal")
end

local function prep(tx, s, r, g, b, a)
    tx:SetColor(r, g, b) ; tx:SetOpacity(a) ; tx:SetScale(s, s)
end

-- total width for an inner span of `inner` px: the caps' insets added, at least both caps
function M.width(p, inner)
    return math.max(2 * p.cap, math.floor(inner + 2 * p.inset + 0.5))
end

-- pieces = { l, m, r } textures; (x, y) = box centre; w = width at scale 1; s = scale;
-- r, g, b = tint; a = opacity. At scale 1 the box lands on whole pixels.
function M.draw(pieces, x, y, w, s, r, g, b, a)
    local l, m, rt = pieces.l, pieces.m, pieces.r
    if not (ready(l) and ready(m) and ready(rt)) then return end
    s, r, g, b, a = s or 1, r or 1, g or 1, b or 1, a or 1
    local lw, rw, h = l.Width, rt.Width, l.Height
    local mid = math.max(0, w - lw - rw)
    local left, top = x - (lw + mid + rw) * s / 2, y - h * s / 2
    if s == 1 then left, top = math.floor(left + 0.5), math.floor(top + 0.5) end
    prep(l, s, r, g, b, a) ; prep(m, s, r, g, b, a) ; prep(rt, s, r, g, b, a)
    -- each piece snaps to whole pixels and is stretched to span them, so a scaled box has no seam gaps
    local xl = math.floor(left + 0.5)
    local xm = math.floor(left + lw * s + 0.5)
    local xr = math.floor(left + (lw + mid) * s + 0.5)
    local right = math.floor(left + (lw + mid + rw) * s + 0.5)
    l:SetScale((xm - xl) / lw, s)
    l:Draw(xl, top)
    if xr > xm then
        m:SetScale((xr - xm) / m.Width, s)
        m:Draw(xm, top)
    end
    rt:SetScale((right - xr) / rw, s)
    rt:Draw(xr, top)
    reset(l) ; reset(m) ; reset(rt)
end

return M
