---@diagnostic disable: undefined-global, lowercase-global, need-check-nil
-- Color.lua — colour space conversions and adjustments shared between stages.
-- Every channel is a 0..1 float (hue included: 0..1 around the wheel); the engine bridge helpers at the
-- bottom convert from / to COLOR objects (bytes 0..255).

local Color = {}

local function clamp01(v)
    if v < 0 then return 0 elseif v > 1 then return 1 else return v end
end
Color.clamp01 = clamp01

-- ─── HSV ─────────────────────────────────────────────────────────────────────

function Color.rgbToHsv(r, g, b)
    local mx, mn = math.max(r, g, b), math.min(r, g, b)
    local d = mx - mn
    local h = 0
    if d > 0 then
        if mx == r then h = ((g - b) / d) % 6
        elseif mx == g then h = (b - r) / d + 2
        else h = (r - g) / d + 4 end
        h = h / 6
    end
    return h, (mx > 0) and d / mx or 0, mx
end

function Color.hsvToRgb(h, s, v)
    local i = math.floor(h * 6) % 6
    local f = h * 6 - math.floor(h * 6)
    local p, q, t = v * (1 - s), v * (1 - s * f), v * (1 - s * (1 - f))
    if i == 0 then return v, t, p elseif i == 1 then return q, v, p elseif i == 2 then return p, v, t
    elseif i == 3 then return p, q, v elseif i == 4 then return t, p, v else return v, p, q end
end

-- ─── HSL ─────────────────────────────────────────────────────────────────────

function Color.rgbToHsl(r, g, b)
    local mx, mn = math.max(r, g, b), math.min(r, g, b)
    local l = (mx + mn) / 2
    local d = mx - mn
    if d <= 0 then return 0, 0, l end
    local s = (l > 0.5) and d / (2 - mx - mn) or d / (mx + mn)
    local h
    if mx == r then h = ((g - b) / d) % 6
    elseif mx == g then h = (b - r) / d + 2
    else h = (r - g) / d + 4 end
    return h / 6, s, l
end

local function hueToRgb(p, q, t)
    t = t % 1
    if t < 1 / 6 then return p + (q - p) * 6 * t end
    if t < 1 / 2 then return q end
    if t < 2 / 3 then return p + (q - p) * (2 / 3 - t) * 6 end
    return p
end

function Color.hslToRgb(h, s, l)
    if s <= 0 then return l, l, l end
    local q = (l < 0.5) and l * (1 + s) or l + s - l * s
    local p = 2 * l - q
    return hueToRgb(p, q, h + 1 / 3), hueToRgb(p, q, h), hueToRgb(p, q, h - 1 / 3)
end

-- ─── Adjustments ─────────────────────────────────────────────────────────────

-- linear blend from (r,g,b) to (r2,g2,b2); t = 0 keeps the first, 1 gives the second
function Color.mix(r, g, b, r2, g2, b2, t)
    return r + (r2 - r) * t, g + (g2 - g) * t, b + (b2 - b) * t
end

-- HSL lightness moved toward 1 by `amount` of the remaining range (0 = unchanged, 1 = white); hue and
-- saturation kept, so the colour stays coloured while it brightens
function Color.lighten(r, g, b, amount)
    local h, s, l = Color.rgbToHsl(r, g, b)
    return Color.hslToRgb(h, s, clamp01(l + (1 - l) * amount))
end

-- HSL lightness moved toward 0 by `amount` of the remaining range (1 = black)
function Color.darken(r, g, b, amount)
    local h, s, l = Color.rgbToHsl(r, g, b)
    return Color.hslToRgb(h, s, clamp01(l * (1 - amount)))
end

-- HSV saturation multiplied by `factor` (plus an optional flat `add`), clamped to 1; value kept
function Color.saturate(r, g, b, factor, add)
    local h, s, v = Color.rgbToHsv(r, g, b)
    return Color.hsvToRgb(h, clamp01(s * factor + (add or 0)), v)
end

-- HSV value multiplied by `factor`, clamped to 1
function Color.brighten(r, g, b, factor)
    local h, s, v = Color.rgbToHsv(r, g, b)
    return Color.hsvToRgb(h, s, clamp01(v * factor))
end

-- n colours linearly stepped from {r,g,b} `from` to {r,g,b} `to` (n = 1 gives `to`)
function Color.ramp(from, to, n)
    local out = {}
    for i = 1, n do
        local t = (n > 1) and (i - 1) / (n - 1) or 1
        out[i] = { Color.mix(from[1], from[2], from[3], to[1], to[2], to[3], t) }
    end
    return out
end

-- ─── Engine bridge (COLOR objects: byte channels) ────────────────────────────

-- r, g, b, a floats from a COLOR object
function Color.fromEngine(col)
    return col.R / 255, col.G / 255, col.B / 255, (col.A or 255) / 255
end

-- COLOR object from float channels (alpha optional, default opaque)
function Color.toEngine(r, g, b, a)
    return COLOR:CreateColorFromRGBA(
        math.floor(clamp01(r) * 255 + 0.5),
        math.floor(clamp01(g) * 255 + 0.5),
        math.floor(clamp01(b) * 255 + 0.5),
        math.floor(clamp01(a == nil and 1 or a) * 255 + 0.5))
end

-- a ramp of n COLOR objects between two COLOR objects
function Color.rampEngine(fromCol, toCol, n)
    local fr, fg, fb = Color.fromEngine(fromCol)
    local tr, tg, tb = Color.fromEngine(toCol)
    local out = {}
    for i, c in ipairs(Color.ramp({ fr, fg, fb }, { tr, tg, tb }, n)) do
        out[i] = Color.toEngine(c[1], c[2], c[3])
    end
    return out
end

return Color
