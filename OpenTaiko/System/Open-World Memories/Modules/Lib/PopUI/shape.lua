---@diagnostic disable: undefined-global, lowercase-global
-- PopUI/shape.lua — bakes the look into a LuaCanvas using the engine's software pixel ops.

local U = require("PopUI.util")
local Shape = {}

local floor, ceil, min, max, sqrt = math.floor, math.ceil, math.min, math.max, math.sqrt

-- ── rounded rect (hard) — for inset face / interior fills where the edge is hidden ──────────────────
local function fillRound(cv, x, y, w, h, r, c)
    x, y, w, h = floor(x), floor(y), floor(w), floor(h)
    r = floor(min(r, w * 0.5, h * 0.5))
    local cr, cg, cb, ca = c[1], c[2], c[3], c[4] or 255
    if r <= 0 then cv:FillRect(x, y, w, h, cr, cg, cb, ca); return end
    cv:FillRect(x + r, y, w - 2 * r, h, cr, cg, cb, ca)
    cv:FillRect(x, y + r, w, h - 2 * r, cr, cg, cb, ca)
    cv:FillCircle(x + r, y + r, r, cr, cg, cb, ca)
    cv:FillCircle(x + w - 1 - r, y + r, r, cr, cg, cb, ca)
    cv:FillCircle(x + r, y + h - 1 - r, r, cr, cg, cb, ca)
    cv:FillCircle(x + w - 1 - r, y + h - 1 - r, r, cr, cg, cb, ca)
end
Shape.fillRound = fillRound

-- one anti-aliased corner-band row: solid span [x0..x1] (fractional) with the two end pixels alpha-weighted
-- by sub-pixel coverage. e = distance of this row from the flat top/bottom edge (0..r).
local function aaRow(cv, x, w, py, r, e, cr, cg, cb, ca)
    local k = r - e
    local inset = r - sqrt(max(0, r * r - k * k))
    local x0, x1 = x + inset, x + w - inset
    local ix0, ix1 = floor(x0), floor(x1)
    if ix1 - ix0 > 1 then cv:FillRect(ix0 + 1, py, ix1 - ix0 - 1, 1, cr, cg, cb, ca) end
    cv:SetPixel(ix0, py, cr, cg, cb, floor(ca * (1 - (x0 - ix0)) + 0.5))
    cv:SetPixel(ix1, py, cr, cg, cb, floor(ca * (x1 - ix1) + 0.5))
end

-- rounded rect with anti-aliased outer corners, for the silhouette / border layer. Per-row: one
-- big fill for the straight middle + AA spans only across the top/bottom corner bands (cheap to bake).
local function fillRoundAA(cv, x, y, w, h, r, c)
    x, y, w, h = floor(x), floor(y), floor(w), floor(h)
    r = floor(min(r, w * 0.5, h * 0.5))
    local cr, cg, cb, ca = c[1], c[2], c[3], c[4] or 255
    if r <= 0 then cv:FillRect(x, y, w, h, cr, cg, cb, ca); return end
    cv:FillRect(x, y + r, w, h - 2 * r, cr, cg, cb, ca)            -- straight middle block (full width)
    for iy = 0, r - 1 do
        aaRow(cv, x, w, y + iy, r, iy, cr, cg, cb, ca)            -- top corner band
        aaRow(cv, x, w, y + h - 1 - iy, r, iy, cr, cg, cb, ca)    -- bottom corner band
    end
end
Shape.fillRoundAA = fillRoundAA

-- vertical-gradient rounded rect (hard interior; the face under the AA outline)
local function fillRoundGradient(cv, x, y, w, h, r, topC, botC)
    x, y, w, h = floor(x), floor(y), floor(w), floor(h)
    r = floor(min(r, w * 0.5, h * 0.5))
    for iy = 0, h - 1 do
        local t = (h > 1) and (iy / (h - 1)) or 0
        local cr = U.lerp(topC[1], botC[1], t); local cg = U.lerp(topC[2], botC[2], t)
        local cb = U.lerp(topC[3], botC[3], t); local ca = U.lerp(topC[4] or 255, botC[4] or 255, t)
        local edge = min(iy, h - 1 - iy); local inset = 0
        if edge < r then local k = r - edge; inset = r - sqrt(max(0, r * r - k * k)) end
        local ix = floor(inset)
        cv:FillRect(x + ix, y + iy, w - 2 * ix, 1, cr, cg, cb, ca)
    end
end
Shape.fillRoundGradient = fillRoundGradient

-- gradient bar
function Shape.fillFillBar(cv, x, y, w, h, r, topC, botC)
    x, y, w, h = floor(x), floor(y), floor(w), floor(h)
    r = floor(min(r, h * 0.5))
    for iy = 0, h - 1 do
        local t = (h > 1) and (iy / (h - 1)) or 0
        local cr = U.lerp(topC[1], botC[1], t); local cg = U.lerp(topC[2], botC[2], t)
        local cb = U.lerp(topC[3], botC[3], t); local ca = U.lerp(topC[4] or 255, botC[4] or 255, t)
        local edge = min(iy, h - 1 - iy); local inset = 0
        if edge < r then local k = r - edge; inset = r - sqrt(max(0, r * r - k * k)) end
        local ix = floor(inset)
        if w - ix > 0 then cv:FillRect(x + ix, y + iy, w - ix, 1, cr, cg, cb, ca) end   -- inset LEFT only
    end
end

-- ── ellipse (per-row, AA end pixels) ────────────────────────────────────────────────────────────────
local function ellipseRow(cv, cx, cy, rx, ry, iyTop, h, colFn)
    for iy = 0, floor(h) - 1 do
        local py = iyTop + iy
        local dy = (py + 0.5 - cy) / ry
        if dy >= -1 and dy <= 1 then
            local hw = rx * sqrt(max(0, 1 - dy * dy))
            local x0, x1 = cx - hw, cx + hw
            local ix0, ix1 = floor(x0), floor(x1)
            local cr, cg, cb, ca = colFn(iy)
            if ix1 - ix0 > 1 then cv:FillRect(ix0 + 1, py, ix1 - ix0 - 1, 1, cr, cg, cb, ca) end
            cv:SetPixel(ix0, py, cr, cg, cb, floor(ca * (1 - (x0 - ix0)) + 0.5))
            cv:SetPixel(ix1, py, cr, cg, cb, floor(ca * (x1 - ix1) + 0.5))
        end
    end
end

local function fillEllipse(cv, x, y, w, h, c)
    local cr, cg, cb, ca = c[1], c[2], c[3], c[4] or 255
    ellipseRow(cv, x + w * 0.5, y + h * 0.5, w * 0.5, h * 0.5, floor(y), h, function() return cr, cg, cb, ca end)
end
Shape.fillEllipse = fillEllipse

local function fillEllipseGradient(cv, x, y, w, h, topC, botC)
    ellipseRow(cv, x + w * 0.5, y + h * 0.5, w * 0.5, h * 0.5, floor(y), h, function(iy)
        local t = (h > 1) and (iy / (h - 1)) or 0
        return U.lerp(topC[1], botC[1], t), U.lerp(topC[2], botC[2], t), U.lerp(topC[3], botC[3], t), U.lerp(topC[4] or 255, botC[4] or 255, t)
    end)
end

local function drawCloud(cv, x, y, w, h, c, grow)
    x, y, w, h = floor(x), floor(y), floor(w), floor(h)
    grow = grow or 0
    local cr, cg, cb, ca = c[1], c[2], c[3], c[4] or 255
    local bump = max(6, floor(min(w, h) * 0.20))
    -- solid core (guarantees the whole shape is connected — no split), grown by `grow`
    fillRound(cv, x + bump - grow, y + bump - grow, w - 2 * bump + 2 * grow, h - 2 * bump + 2 * grow, bump + grow, c)
    -- bumps around the perimeter (centres independent of `grow`), spaced so neighbours always overlap
    local nx = max(1, floor((w - 2 * bump) / (bump * 1.3)))
    local ny = max(1, floor((h - 2 * bump) / (bump * 1.3)))
    for i = 0, nx do
        local bx = x + bump + (w - 2 * bump) * (i / nx)
        cv:FillCircle(floor(bx), y + bump, bump + grow, cr, cg, cb, ca)
        cv:FillCircle(floor(bx), y + h - bump, bump + grow, cr, cg, cb, ca)
    end
    for j = 0, ny do
        local by = y + bump + (h - 2 * bump) * (j / ny)
        cv:FillCircle(x + bump, floor(by), bump + grow, cr, cg, cb, ca)
        cv:FillCircle(x + w - bump, floor(by), bump + grow, cr, cg, cb, ca)
    end
end
Shape.drawCloud = drawCloud

-- ── triangle (scanline) — for the seamless tail ─────────────────────────────────────────────────────
local function fillTriangle(cv, x1, y1, x2, y2, x3, y3, c)
    local cr, cg, cb, ca = c[1], c[2], c[3], c[4] or 255
    local miny, maxy = floor(min(y1, y2, y3)), ceil(max(y1, y2, y3))
    for py = miny, maxy do
        local yc = py + 0.5
        local lo, hi, n = 1e9, -1e9, 0
        local function edge(xa, ya, xb, yb)
            if (ya <= yc and yb > yc) or (yb <= yc and ya > yc) then
                local xx = xa + (xb - xa) * (yc - ya) / (yb - ya)
                if xx < lo then lo = xx end; if xx > hi then hi = xx end; n = n + 1
            end
        end
        edge(x1, y1, x2, y2); edge(x2, y2, x3, y3); edge(x3, y3, x1, y1)
        if n >= 2 then cv:FillRect(floor(lo), py, max(1, ceil(hi) - floor(lo)), 1, cr, cg, cb, ca) end
    end
end
Shape.fillTriangle = fillTriangle

-- ── soft drop shadow (layered silhouettes; blurred so hard corners don't matter) ──────────────────────
function Shape.dropShadow(cv, x, y, w, h, r, sh)
    local col = sh.col; local layers = sh.layers or 4; local grow = sh.grow or 3
    local dx, dy = sh.dx or 0, sh.dy or 6; local baseA = col[4] or 90
    for i = layers, 1, -1 do
        local g = i * grow
        local a = floor(baseA * (1 - (i - 1) / layers) * 0.6)
        fillRound(cv, x - g + dx, y - g + dy, w + 2 * g, h + 2 * g, r + g, { col[1], col[2], col[3], a })
    end
end

-- Top sheen. Canvas pixel ops OVERWRITE (no blending), so a translucent gloss drawn raw would
-- REPLACE the opaque face pixels and leave the top of the box see-through on screen. When the face
-- colors are known (topC/botC = the face gradient), the sheen is pre-blended row by row and written
-- back opaque instead. Without them, falls back to the raw write (legacy behaviour).
local function gloss(cv, x, y, w, h, r, col, topC, botC)
    local gh = floor(h * 0.46); if gh < 4 then return end
    local pad = floor(r * 0.4)
    local gx, gy, gw, gr = x + pad, y + floor(r * 0.3), w - 2 * pad, max(2, r - pad)
    if topC == nil then
        fillRound(cv, gx, gy, gw, gh, gr, col)
        return
    end
    botC = botC or topC
    local ga = (col[4] or 255) / 255
    gx, gy, gw, gh = floor(gx), floor(gy), floor(gw), floor(gh)
    gr = floor(min(gr, gw * 0.5, gh * 0.5))
    for iy = 0, gh - 1 do
        -- face gradient sampled at this row's position within the FACE rect (y..y+h-1)
        local t = (h > 1) and ((gy + iy - y) / (h - 1)) or 0
        local fr = U.lerp(topC[1], botC[1], t); local fg = U.lerp(topC[2], botC[2], t)
        local fb = U.lerp(topC[3], botC[3], t); local fa = U.lerp(topC[4] or 255, botC[4] or 255, t)
        local cr = floor(fr + (col[1] - fr) * ga + 0.5)
        local cg = floor(fg + (col[2] - fg) * ga + 0.5)
        local cb = floor(fb + (col[3] - fb) * ga + 0.5)
        -- rounded inset of the gloss rect itself
        local edge = min(iy, gh - 1 - iy); local inset = 0
        if edge < gr then local k = gr - edge; inset = gr - sqrt(max(0, gr * gr - k * k)) end
        local ix = floor(inset)
        cv:FillRect(gx + ix, gy + iy, gw - 2 * ix, 1, cr, cg, cb, fa)
    end
end
Shape.gloss = gloss

-- Bake a bordered, gradient, glossy rounded panel into cv at (x,y,w,h). Smooth outer border.
function Shape.panel(cv, x, y, w, h, opts)
    local r = opts.radius or 24
    local ow = (opts.outline and opts.outline.width) or 0
    if ow > 0 then fillRoundAA(cv, x, y, w, h, r, opts.outline.col) end
    local ix, iy, iw, ih, ir = x + ow, y + ow, w - 2 * ow, h - 2 * ow, max(1, r - ow)
    if opts.top and opts.bottom then fillRoundGradient(cv, ix, iy, iw, ih, ir, opts.top, opts.bottom)
    elseif opts.top then fillRound(cv, ix, iy, iw, ih, ir, opts.top) end
    if opts.gloss then gloss(cv, ix, iy, iw, ih, ir, opts.gloss, opts.top, opts.bottom) end
end

local cos, sin, pi, huge, sort = math.cos, math.sin, math.pi, math.huge, table.sort

local function bodyPath(shape, x, y, w, h, r, maxPoints)
    local p = {}
    if shape == "oval" then
        local cx, cy, rx, ry = x + w * 0.5, y + h * 0.5, w * 0.5, h * 0.5
        local n = max(28, floor((rx + ry) * 0.6))
        if maxPoints and n > maxPoints then n = maxPoints end
        for i = 0, n - 1 do local a = (i / n) * 2 * pi; p[#p + 1] = { cx + rx * cos(a), cy + ry * sin(a) } end
    else
        r = min(r, w * 0.5, h * 0.5)
        local seg, step = 7, 16
        local function arc(ccx, ccy, a0, a1)
            for i = 0, seg do local a = a0 + (a1 - a0) * (i / seg); p[#p + 1] = { ccx + r * cos(a), ccy + r * sin(a) } end
        end
        -- sample the straight edges too (excluding endpoints) so a tail can attach mid-edge
        local function line(x0, y0, x1, y1)
            local dx, dy = x1 - x0, y1 - y0; local L = sqrt(dx * dx + dy * dy); local n = max(1, floor(L / step))
            for i = 1, n - 1 do p[#p + 1] = { x0 + dx * (i / n), y0 + dy * (i / n) } end
        end
        arc(x + r, y + r, pi, 1.5 * pi)                 -- TL  → (x+r, y)
        line(x + r, y, x + w - r, y)                    -- top edge
        arc(x + w - r, y + r, 1.5 * pi, 2 * pi)         -- TR  → (x+w, y+r)
        line(x + w, y + r, x + w, y + h - r)            -- right edge
        arc(x + w - r, y + h - r, 0, 0.5 * pi)          -- BR  → (x+w-r, y+h)
        line(x + w - r, y + h, x + r, y + h)            -- bottom edge
        arc(x + r, y + h - r, 0.5 * pi, pi)             -- BL  → (x, y+h-r)
        line(x, y + h - r, x, y + r)                    -- left edge
    end
    if maxPoints and #p > maxPoints then                -- uniform decimation (draft mode)
        local out, stepf = {}, #p / maxPoints
        for i = 1, maxPoints do out[i] = p[floor((i - 1) * stepf) + 1] end
        p = out
    end
    return p
end

-- where the tail attaches on the body edge + the OUTWARD normal (for placing the apex)
local function tailAnchor(shape, x, y, w, h, side, pos)
    local cx, cy, rx, ry = x + w * 0.5, y + h * 0.5, w * 0.5, h * 0.5
    pos = U.clamp(pos, 0.18, 0.82)
    if side == "left" or side == "right" then
        local sgn = (side == "right") and 1 or -1
        local py = y + h * pos
        local px
        if shape == "oval" then px = cx + sgn * rx * sqrt(max(0, 1 - ((py - cy) / ry) ^ 2)) else px = cx + sgn * rx end
        return px, py, sgn, 0
    else
        local sgn = (side == "top") and -1 or 1
        local px = x + w * pos
        local py
        if shape == "oval" then py = cy + sgn * ry * sqrt(max(0, 1 - ((px - cx) / rx) ^ 2)) else py = cy + sgn * ry end
        return px, py, 0, sgn
    end
end

-- splice the tail into the polygon: rotate so the attach is mid-list (no wrap), find the nearest boundary
-- vertex, then walk out ~hw each side. Replace that span with [baseL, apex, baseR] → one continuous path
-- with a consistent base width (~2*hw) regardless of how the boundary happened to be sampled.
local function spliceTail(path, ax, ay, apexx, apexy, hw)
    local n = #path
    local far, fi = -1, 1
    for i = 1, n do local dx, dy = path[i][1] - ax, path[i][2] - ay; local d = dx * dx + dy * dy; if d > far then far, fi = d, i end end
    local rp = {}
    for i = 0, n - 1 do rp[#rp + 1] = path[((fi - 1 + i) % n) + 1] end
    local near, ai = huge, 1
    for i = 1, #rp do local dx, dy = rp[i][1] - ax, rp[i][2] - ay; local d = dx * dx + dy * dy; if d < near then near, ai = d, i end end
    local hw2 = hw * hw
    local lo, hi = ai, ai
    while lo > 1 and ((rp[lo][1] - ax) ^ 2 + (rp[lo][2] - ay) ^ 2) < hw2 do lo = lo - 1 end
    while hi < #rp and ((rp[hi][1] - ax) ^ 2 + (rp[hi][2] - ay) ^ 2) < hw2 do hi = hi + 1 end
    if lo >= hi then return rp end
    local np = {}
    for i = 1, lo do np[#np + 1] = rp[i] end
    np[#np + 1] = { apexx, apexy }
    for i = hi, #rp do np[#np + 1] = rp[i] end
    return np
end

-- move each vertex of a clockwise polygon `d` px along its bisector: d>0 inward (face), d<0 outward
-- (shadow). Movement is capped at exactly d along the bisector → no miter spikes at sharp corners.
local function offsetPoly(pts, d)
    local n = #pts
    local out = {}
    for i = 1, n do
        local pv, cu, nx2 = pts[(i - 2) % n + 1], pts[i], pts[i % n + 1]
        local e1x, e1y = cu[1] - pv[1], cu[2] - pv[2]
        local e2x, e2y = nx2[1] - cu[1], nx2[2] - cu[2]
        local l1 = sqrt(e1x * e1x + e1y * e1y); if l1 > 0 then e1x, e1y = e1x / l1, e1y / l1 end
        local l2 = sqrt(e2x * e2x + e2y * e2y); if l2 > 0 then e2x, e2y = e2x / l2, e2y / l2 end
        local nx, ny = (-e1y - e2y), (e1x + e2x)     -- sum of inward unit normals (clockwise: (-ey, ex))
        local ln = sqrt(nx * nx + ny * ny); if ln > 0 then nx, ny = nx / ln, ny / ln end
        out[i] = { cu[1] + nx * d, cu[2] + ny * d }
    end
    return out
end

-- scanline polygon fill (even-odd); AA the span ends when aa=true (smooth outer silhouette over transparent)
local function fillPoly(cv, pts, topY, botY, topC, botC, aa)
    local miny, maxy = huge, -huge
    for _, p in ipairs(pts) do if p[2] < miny then miny = p[2] end; if p[2] > maxy then maxy = p[2] end end
    local n = #pts
    for py = floor(miny), ceil(maxy) do
        local yc = py + 0.5
        local xs = {}
        for i = 1, n do local a, b = pts[i], pts[i % n + 1]; local ya, yb = a[2], b[2]
            if (ya <= yc and yb > yc) or (yb <= yc and ya > yc) then xs[#xs + 1] = a[1] + (b[1] - a[1]) * (yc - ya) / (yb - ya) end
        end
        local m = #xs
        if m >= 2 then
            sort(xs)
            local cr, cg, cb, ca
            if botC then local t = U.clamp((py - topY) / max(1, botY - topY), 0, 1)
                cr = U.lerp(topC[1], botC[1], t); cg = U.lerp(topC[2], botC[2], t); cb = U.lerp(topC[3], botC[3], t); ca = U.lerp(topC[4] or 255, botC[4] or 255, t)
            else cr, cg, cb, ca = topC[1], topC[2], topC[3], topC[4] or 255 end
            for k = 1, m - 1, 2 do
                local xl, xr = xs[k], xs[k + 1]
                local ixl, ixr = floor(xl), floor(xr)
                if aa then
                    if ixr - ixl > 1 then cv:FillRect(ixl + 1, py, ixr - ixl - 1, 1, cr, cg, cb, ca) end
                    cv:SetPixel(ixl, py, cr, cg, cb, floor(ca * (1 - (xl - ixl)) + 0.5))
                    cv:SetPixel(ixr, py, cr, cg, cb, floor(ca * (xr - ixr) + 0.5))
                else
                    cv:FillRect(ixl, py, max(1, ceil(xr) - ixl), 1, cr, cg, cb, ca)
                end
            end
        end
    end
end

-- ── speech bubble: round | oval (one continuous polygon silhouette + spliced tail) or cloud (concentric
-- discs + trailing dots). Uniform border that hugs the shape THROUGH the tail; shape-matched drop shadow.
-- opts: shape, radius, outline={col,width}, top, bottom, gloss, tail={side,pos,size}, shadow={col,dx,dy,layers,grow}
function Shape.bubble(cv, x, y, w, h, opts)
    local shape = opts.shape or "round"
    local r = opts.radius or 24
    local ow = (opts.outline and opts.outline.width) or 0
    local oc = opts.outline and opts.outline.col or { 0, 0, 0, 255 }
    local tail = opts.tail
    local tside = tail and tail.side or "bottom"
    local tpos = tail and tail.pos or 0.5
    local tsize = tail and tail.size or 28
    local sh = opts.shadow

    if shape == "cloud" then
        -- concentric outline (radius+ow) UNDER face (radius) = uniform border hugging the lumps.
        -- tail = a few shrinking dots toward the apex (a thought-bubble pointer).
        local ax, ay, nx, ny = tailAnchor("cloud", x, y, w, h, tside, tpos)
        local dots = {}
        if tail then for i = 1, 3 do
            local f = i / 3
            dots[i] = { x = ax + nx * tsize * f, y = ay + ny * tsize * f, rr = max(4, (tsize * 0.34) * (1 - f * 0.5)) }
        end end
        if sh then
            local g = (sh.layers or 4) * (sh.grow or 3) * 0.5
            local sc = { sh.col[1], sh.col[2], sh.col[3], floor((sh.col[4] or 90) * 0.5) }
            drawCloud(cv, x + (sh.dx or 0), y + (sh.dy or 0), w, h, sc, g)
            for _, d in ipairs(dots) do cv:FillCircle(floor(d.x + (sh.dx or 0)), floor(d.y + (sh.dy or 0)), floor(d.rr + g), sc[1], sc[2], sc[3], sc[4]) end
        end
        if ow > 0 then
            drawCloud(cv, x, y, w, h, oc, ow)
            for _, d in ipairs(dots) do cv:FillCircle(floor(d.x), floor(d.y), floor(d.rr + ow), oc[1], oc[2], oc[3], oc[4] or 255) end
        end
        local fc = opts.top
        drawCloud(cv, x, y, w, h, fc, 0)
        for _, d in ipairs(dots) do cv:FillCircle(floor(d.x), floor(d.y), floor(d.rr), fc[1], fc[2], fc[3], fc[4] or 255) end
        return
    end

    -- round / oval: one silhouette polygon with the tail spliced into the boundary
    local P = bodyPath(shape, x, y, w, h, r, opts.maxPoints)
    if tail then
        local ax, ay, nx, ny = tailAnchor(shape, x, y, w, h, tside, tpos)
        P = spliceTail(P, ax, ay, ax + nx * tsize, ay + ny * tsize, tsize * 0.62)
    end

    if sh then
        local layers, grow = (opts.shadowLayers or sh.layers or 4), sh.grow or 3
        local baseA, dx, dy = sh.col[4] or 90, sh.dx or 0, sh.dy or 6
        for i = layers, 1, -1 do
            local sp = offsetPoly(P, -(i * grow))
            for _, p in ipairs(sp) do p[1] = p[1] + dx; p[2] = p[2] + dy end
            fillPoly(cv, sp, 0, 1, { sh.col[1], sh.col[2], sh.col[3], floor(baseA * (1 - (i - 1) / layers) * 0.55) }, nil, false)
        end
    end

    if ow > 0 then fillPoly(cv, P, 0, 1, oc, nil, true) end                       -- AA outline silhouette
    local face = (ow > 0) and offsetPoly(P, ow) or P
    fillPoly(cv, face, y + ow, y + h - ow, opts.top, opts.bottom, false)          -- gradient face (interior)

    if opts.gloss and shape == "round" then
        gloss(cv, x + ow, y + ow, w - 2 * ow, h - 2 * ow, max(1, r - ow), opts.gloss, opts.top, opts.bottom)
    end
end

-- ── audio-player transport icons (language-agnostic glyphs baked onto widget canvases) ─────────────
-- kind: "play" | "pause" | "stop" | "repeat" | "back". Drawn centered at (cx,cy), overall size s,
-- color c = {r,g,b[,a]}. Buttons pass their text color so icons match either face.
function Shape.icon(cv, kind, cx, cy, s, c)
    local h = s * 0.5
    if kind == "play" then
        fillTriangle(cv, cx - h * 0.58, cy - h * 0.88, cx - h * 0.58, cy + h * 0.88, cx + h * 0.92, cy, c)
    elseif kind == "pause" then
        local bw, bh = s * 0.24, s * 0.84
        Shape.fillRoundAA(cv, cx - s * 0.27 - bw * 0.5, cy - bh * 0.5, bw, bh, bw * 0.35, c)
        Shape.fillRoundAA(cv, cx + s * 0.27 - bw * 0.5, cy - bh * 0.5, bw, bh, bw * 0.35, c)
    elseif kind == "stop" then
        local d = s * 0.7
        Shape.fillRoundAA(cv, cx - d * 0.5, cy - d * 0.5, d, d, d * 0.18, c)
    elseif kind == "repeat" then
        -- a rounded loop with an arrowhead on the top run
        local W, H, t = s * 0.40, s * 0.30, s * 0.15
        Shape.fillRoundAA(cv, cx - W, cy - H - t * 0.5, W * 1.30, t, t * 0.45, c)          -- top (stops early)
        Shape.fillRoundAA(cv, cx - W, cy + H - t * 0.5, W * 2, t, t * 0.45, c)             -- bottom
        Shape.fillRoundAA(cv, cx - W - t * 0.5, cy - H, t, H * 2, t * 0.45, c)             -- left
        Shape.fillRoundAA(cv, cx + W - t * 0.5, cy - H * 0.2, t, H * 1.2, t * 0.45, c)     -- right (below arrow)
        fillTriangle(cv, cx + W * 0.30, cy - H - s * 0.19,                                  -- arrowhead → right
                         cx + W * 0.30, cy - H + s * 0.19,
                         cx + W * 0.30 + s * 0.28, cy - H, c)
    elseif kind == "back" then
        local t = s * 0.18
        fillTriangle(cv, cx + h * 0.05, cy - h * 0.8, cx + h * 0.05, cy + h * 0.8, cx - h * 0.88, cy, c)
        Shape.fillRoundAA(cv, cx + h * 0.05, cy - t * 0.5, h * 0.8, t, t * 0.45, c)
    end
end

return Shape
