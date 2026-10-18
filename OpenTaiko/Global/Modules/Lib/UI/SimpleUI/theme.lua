---@diagnostic disable: lowercase-global
-- SimpleUI/theme.lua — the look of the UI in one table. Colors are {r,g,b,a} 0-255 (the form the canvas
-- pixel ops want directly). Everything is tweakable; per-widget `theme/style` tables override per-field.

local Util = require("Core.Util")

local Theme = {}

-- ── default palette: "Simple" — flat fills, small corners, thin borders, no sheen and no shadow ─────
Theme.DEFAULT = {
    name = "Simple",
    colors = {
        bg           = { 242, 243, 245, 255 },  -- screen background (if the manager draws one)
        surface      = { 255, 255, 255, 255 },  -- panel/button face (top of gradient)
        surface2     = { 255, 255, 255, 255 },  -- panel/button face (bottom of gradient)
        primary      = { 64,  70,  82,  255 },  -- accent face (top)
        primary2     = { 64,  70,  82,  255 },  -- accent face (bottom)
        accent       = { 92,  132, 196, 255 },  -- secondary accent (top)
        accent2      = { 92,  132, 196, 255 },  -- secondary accent (bottom)
        outline      = { 200, 204, 211, 255 },  -- the border
        text         = { 38,  41,  48,  255 },
        textOnAccent = { 255, 255, 255, 255 },
        textDisabled = { 156, 160, 168, 255 },
        shadow       = { 0,   0,   0,   0   },
        gloss        = { 255, 255, 255, 0   },
        focusRing    = { 92,  132, 196, 255 },  -- shared hover + keyboard-select highlight
        track        = { 228, 230, 234, 255 },  -- slider/toggle empty track
        gain         = { 46,  140, 84,  255 },  -- coins: an amount coming in
        loss         = { 196, 64,  56,  255 },  -- coins: an amount going out
    },
    radius       = 8,     -- corner radius for big elements (button/panel)
    radiusSmall  = 6,     -- corner radius for small elements (chip/checkbox/track)
    outlineWidth = 2,     -- border thickness
    gloss        = false, -- draw the top sheen
    shadow       = { dx = 0, dy = 0, layers = 0, grow = 0 },  -- no drop shadow
    textOutline  = false, -- labels drawn without the outline behind their letters
    font = { small = 18, label = 22, button = 27, title = 34 },
    anim = {
        hoverScale = 1.02, pressScale = 0.97,
        hoverTime  = 0.10, pressTime  = 0.06, releaseTime = 0.12,
        hoverEase  = { "OUT", "QUAD" },
        pressEase  = { "OUT", "QUAD" },
        releaseEase = { "OUT", "QUAD" },
        popInEase  = { "OUT", "QUAD" },     -- entrance
        highlightTime = 0.10,
        typewriterCps = 42,
    },
}

Theme.clone = Util.deepcopy
Theme.merge = Util.merge

-- the style of every widget made without one (the manager resolves it once for all of them)
Theme.NONE = {}

-- resolve an effective theme = base (DEFAULT when nil) < manager theme < per-widget theme/style.
-- NOTICE: colors defined in {r,g,b} have the alpha inherited from base styles
function Theme.resolve(userTheme, style, base)
    local t = Util.merge(base or Theme.DEFAULT, userTheme or {})
    if style then t = Util.merge(t, style) end
    return t
end

return Theme
