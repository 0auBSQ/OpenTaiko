---@diagnostic disable: undefined-global, undefined-field, need-check-nil
local A = {}

A.SW, A.SH = 1920, 1080

function A.clamp01(x) return x < 0 and 0 or (x > 1 and 1 or x) end
function A.lerp(a, b, k) return a + (b - a) * k end

-- moves v toward target by at most step
function A.approach(v, target, step)
    if v < target then return math.min(target, v + step) end
    return math.max(target, v - step)
end

-- seconds since the last frame, kept between 0 and 0.1
function A.dt()
    local ok, d = pcall(function() return fps.deltaTime end)
    d = (ok and type(d) == "number") and d or (1 / 60)
    if d < 0 then d = 0 elseif d > 0.1 then d = 0.1 end
    return d
end

-- (dx, dy) turned by deg on screen (y down)
function A.turn(dx, dy, deg)
    local r = math.rad(deg)
    local c, s = math.cos(r), math.sin(r)
    return dx * c + dy * s, -dx * s + dy * c
end

function A.frame(x, y, rot, k)
    local r = math.rad(rot or 0)
    return { x = x, y = y, rot = rot or 0, c = math.cos(r), s = math.sin(r), k = k or 1 }
end

function A.child(f, ox, oy, rot, k)
    return A.frame(f.x + (ox * f.c + oy * f.s) * f.k, f.y + (-ox * f.s + oy * f.c) * f.k, f.rot + (rot or 0), f.k * (k or 1))
end

-- a texture is drawable once it is on disk; a missing one draws nothing
function A.ok(tex) return tex ~= nil and tex.Loaded end

-- centred on the frame, scaled and turned with it; shade tints it grey (1 = as drawn)
function A.sprite(tex, f, sx, sy, op, shade)
    if not A.ok(tex) then return end
    sx = sx or 1
    tex:SetScale(f.k * sx, f.k * (sy or sx))
    tex:SetRotation(f.rot)
    tex:SetOpacity(op or 1)
    if shade ~= nil then tex:SetColor(shade, shade, shade) end
    tex:DrawAtAnchor(f.x, f.y, "center")
    tex:SetScale(1, 1)
    tex:SetRotation(0)
    tex:SetOpacity(1)
    if shade ~= nil then tex:SetColor(1, 1, 1) end
end

-- the source rect (rx, ry, rw, rh) of tex, its centre on the frame's point, turned and scaled with the frame;
-- sx, sy scale it further and (r, g, b) tint it
function A.piece(tex, f, rx, ry, rw, rh, sx, sy, op, r, g, b)
    if not A.ok(tex) or rw <= 0 or rh <= 0 then return end
    tex:SetScale(f.k * (sx or 1), f.k * (sy or 1))
    tex:SetRotation(f.rot)
    tex:SetOpacity(op or 1)
    if r ~= nil then tex:SetColor(r, g, b) end
    tex:DrawRectAtAnchor(f.x, f.y, rx, ry, rw, rh, "center")
    tex:SetScale(1, 1)
    tex:SetRotation(0)
    tex:SetOpacity(1)
    if r ~= nil then tex:SetColor(1, 1, 1) end
end

-- a pill w px wide on the frame's point, from a three-slice texture: caps cap px wide at both ends, the middle
-- stretched between them
function A.pill(tex, f, w, cap, op, shade)
    if not A.ok(tex) then return end
    local tw, th = tex.Width, tex.Height
    local mid = tw - 2 * cap
    w = math.max(w, 2 * cap)
    local s = shade or 1
    A.piece(tex, A.child(f, -w / 2 + cap / 2, 0), 0, 0, cap, th, 1, 1, op, s, s, s)
    A.piece(tex, A.child(f, w / 2 - cap / 2, 0), tw - cap, 0, cap, th, 1, 1, op, s, s, s)
    local span = w - 2 * cap
    if span > 0 then
        A.piece(tex, f, cap, 0, mid, th, (span + 2) / mid, 1, op, s, s, s)
    end
end

-- unturned, at an anchor point
function A.place(tex, x, y, anchor, sx, sy, op)
    if not A.ok(tex) then return end
    sx = sx or 1
    tex:SetScale(sx, sy or sx)
    tex:SetOpacity(op or 1)
    tex:DrawAtAnchor(x, y, anchor)
    tex:SetScale(1, 1)
    tex:SetOpacity(1)
end

local colors = {}
function A.col(r, g, b, a)
    local key = r * 16777216 + g * 65536 + b * 256 + (a or 255)
    local c = colors[key]
    if c == nil then
        c = COLOR:CreateColorFromRGBA(r, g, b, a or 255)
        colors[key] = c
    end
    return c
end
A.CLEAR = nil
function A.clear()
    if A.CLEAR == nil then A.CLEAR = A.col(0, 0, 0, 0) end
    return A.CLEAR
end

-- the glyph box carries padding under the ink: centred text moves down by this to centre its ink
local nudges = {}
function A.nudge(font)
    local n = nudges[font]
    if n == nil then
        n = math.floor((font.BoxHeight - math.ceil(font.LineHeight)) / 2) - 1
        nudges[font] = n
    end
    return n
end

-- text centred (or "left"/"right") on the frame's point, its ink vertically centred there; maxInk = the widest
-- the ink may get at this scale (0 = no limit): a longer text shrinks down to FIT_MIN, then squeezes
local FIT_MIN = 0.72
function A.text(font, str, f, fore, back, scale, maxInk, anchor, op)
    if font == nil or str == nil or str == "" then return end
    local s = f.k * (scale or 1)
    if maxInk and maxInk > 0 then
        local ink = font:Measure(str) * (scale or 1)
        if ink > maxInk then s = s * math.max(FIT_MIN, maxInk / ink) end
    end
    local n = A.nudge(font) * s
    local x, y = f.x + n * f.s, f.y + n * f.c
    local maxW = (maxInk and maxInk > 0) and (maxInk * f.k + 50 * s) or 0
    font:Draw(str, x, y, fore, back or A.clear(), op or 1, s, maxW, anchor or "center", 0, f.rot)
end

-- the lines a text wraps to at width w, kept per font, text and width
local wraps = setmetatable({}, { __mode = "k" })
function A.wrap(font, str, w)
    if font == nil or str == nil then return {} end
    local byFont = wraps[font]
    if byFont == nil then byFont = {}; wraps[font] = byFont end
    local key = str .. "\0" .. w
    local lines = byFont[key]
    if lines == nil then
        lines = {}
        local arr = font:WrapToLines(str, w)
        if arr ~= nil then
            for i = 0, (arr.Length or 0) - 1 do lines[#lines + 1] = arr[i] end
        end
        byFont[key] = lines
    end
    return lines
end

-- a whole number as text (save counters come back as doubles: 6 would print as "6.0")
function A.int(n)
    return string.format("%d", math.floor((tonumber(n) or 0) + 0.5))
end

-- the scale (at most 1) that brings str's ink down to maxInk, kept per font and text
local widths = setmetatable({}, { __mode = "k" })
function A.measure(font, str)
    local byFont = widths[font]
    if byFont == nil then byFont = {}; widths[font] = byFont end
    local w = byFont[str]
    if w == nil then
        w = font:Measure(str)
        byFont[str] = w
    end
    return w
end

function A.fitScale(font, str, maxInk)
    local w = A.measure(font, str)
    if w <= maxInk or w <= 0 then return 1 end
    return maxInk / w
end

-- str wrapped into a w x h box: the biggest scale (kmax down to kmin) whose lines all fit; returns the lines, the
-- scale and the line pitch at that scale. Below kmin the lines are squeezed by A.text's maxInk.
local boxes = setmetatable({}, { __mode = "k" })
function A.fitBox(font, str, w, h, kmax, kmin)
    kmax, kmin = kmax or 1, kmin or 0.6
    local byFont = boxes[font]
    if byFont == nil then byFont = {}; boxes[font] = byFont end
    local key = str .. "\0" .. w .. "\0" .. h .. "\0" .. kmax .. "\0" .. kmin
    local r = byFont[key]
    if r ~= nil then return r.lines, r.k, r.pitch end
    local lh = font.LineHeight
    local function try(k)
        local lines = A.wrap(font, str, math.floor(w / k))
        local widest = 0
        for _, l in ipairs(lines) do widest = math.max(widest, A.measure(font, l)) end
        return { lines = lines, k = k, pitch = lh * k }, widest * k <= w and #lines * lh * k <= h
    end
    local best, fits = nil, false
    local k = kmax
    while k >= kmin - 1e-6 do
        best, fits = try(k)
        if fits then break end
        k = k - 0.04
    end
    -- a last line of a word or two: a little smaller, if that saves the line
    local n = #best.lines
    if fits and n > 1 and A.measure(font, best.lines[n]) * best.k < w * 0.25 then
        local k2 = best.k - 0.04
        while k2 >= math.max(kmin, best.k - 0.16) - 1e-6 do
            local b2, f2 = try(k2)
            if f2 and #b2.lines < n then best = b2; break end
            k2 = k2 - 0.04
        end
    end
    byFont[key] = best
    return best.lines, best.k, best.pitch
end

-- the skin's Locales/<code>.json through THEME, looked up once per visit (A.resetStrings); the English text
-- stays in the code as the fallback
local strings = {}
function A.tr(key, fallback)
    local s = strings[key]
    if s == nil then
        local ok, v = pcall(function() return THEME:GetSkinString(key) end)
        s = (ok and type(v) == "string" and v ~= "" and v:sub(1, 1) ~= "[") and v or false
        strings[key] = s
    end
    return s or fallback
end

function A.resetStrings() strings = {} end

function A.utf8chars(s)
    local out = {}
    for ch in s:gmatch("[%z\1-\127\194-\244][\128-\191]*") do out[#out + 1] = ch end
    return out
end

return A
