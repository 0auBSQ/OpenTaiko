---@diagnostic disable: lowercase-global
-- PopUI/theme.lua — the look of the UI in one table. Colors are {r,g,b,a} 0-255 (the form the canvas
-- pixel ops want directly). Everything is tweakable; per-widget `style` tables override per-field.

local Theme = {}

-- ── default palette: "Bubblegum" ──────────────────────────────────────────────────────────────────
Theme.DEFAULT = {
    name = "Bubblegum",
    colors = {
        bg           = { 255, 240, 248, 255 },  -- screen background (if the manager draws one)
        surface      = { 255, 255, 255, 255 },  -- panel/button face (top of gradient)
        surface2     = { 255, 226, 242, 255 },  -- panel/button face (bottom of gradient)
        primary      = { 255, 120, 176, 255 },  -- accent face (top)
        primary2     = { 244, 78,  150, 255 },  -- accent face (bottom)
        accent       = { 122, 212, 255, 255 },  -- secondary accent (top)
        accent2      = { 70,  178, 244, 255 },  -- secondary accent (bottom)
        outline      = { 74,  38,  72,  255 },  -- the bold playful border + default text
        text         = { 74,  38,  72,  255 },
        textOnAccent = { 255, 255, 255, 255 },
        textDisabled = { 176, 158, 172, 255 },
        shadow       = { 60,  30,  60,  90  },  -- soft drop shadow
        gloss        = { 255, 255, 255, 150 },  -- top highlight sheen
        focusRing    = { 255, 206, 84,  255 },  -- shared hover + keyboard-select highlight
        track        = { 236, 222, 234, 255 },  -- slider/toggle empty track
    },
    radius       = 26,    -- corner radius for big elements (button/panel)
    radiusSmall  = 13,    -- corner radius for small elements (chip/checkbox/track)
    outlineWidth = 5,     -- bold border thickness
    gloss        = true,  -- draw the top sheen
    shadow       = { dx = 0, dy = 7, layers = 4, grow = 3 },  -- fake soft shadow (layered silhouettes)
    font = { small = 18, label = 22, button = 27, title = 34 },
    anim = {
        hoverScale = 1.06, pressScale = 0.93,
        hoverTime  = 0.16, pressTime  = 0.07, releaseTime = 0.26,
        hoverEase  = { "OUT", "BACK" },     -- bouncy pop
        pressEase  = { "OUT", "QUAD" },
        popInEase  = { "OUT", "ELASTIC" },  -- entrance
        highlightTime = 0.14,
        typewriterCps = 42,
    },
}

-- deep-ish clone (one level into the known nested tables, plus arbitrary nesting via recursion)
local function deepcopy(t)
    if type(t) ~= "table" then return t end
    local o = {}
    for k, v in pairs(t) do o[k] = deepcopy(v) end
    return o
end
Theme.clone = deepcopy

-- merge `over` onto a clone of `base` (recursive for sub-tables; color arrays / scalars replaced whole).
-- A color is an array {r,g,b,a}; we replace it wholesale rather than merging indices.
local function isColor(v) return type(v) == "table" and type(v[1]) == "number" end

local function merge(base, over)
    if type(over) ~= "table" then return over end
    local out = deepcopy(base)
    for k, v in pairs(over) do
        if isColor(v) or type(v) ~= "table" or type(out[k]) ~= "table" or isColor(out[k]) then
            out[k] = deepcopy(v)
        else
            out[k] = merge(out[k], v)
        end
    end
    return out
end
Theme.merge = merge

-- resolve an effective theme = DEFAULT < user theme < per-widget style
function Theme.resolve(userTheme, style)
    local t = merge(Theme.DEFAULT, userTheme or {})
    if style then t = merge(t, style) end
    return t
end

return Theme
