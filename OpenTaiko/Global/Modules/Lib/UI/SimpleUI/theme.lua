---@diagnostic disable: lowercase-global
-- SimpleUI default theme; colours are {r,g,b,a} 0-255
-- per-widget `theme`/`style` tables override per field

local Util = require("Core.Util")

local Theme = {}

-- ==== default palette ====
Theme.DEFAULT = {
    name = "Simple",
    colors = {
        bg           = { 242, 243, 245, 255 },  -- screen background (if the manager draws one)
        surface      = { 255, 255, 255, 255 },  -- panel/button face, gradient top
        surface2     = { 255, 255, 255, 255 },  -- panel/button face, gradient bottom
        primary      = { 64,  70,  82,  255 },  -- accent face, top
        primary2     = { 64,  70,  82,  255 },  -- accent face, bottom
        accent       = { 92,  132, 196, 255 },  -- secondary accent, top
        accent2      = { 92,  132, 196, 255 },  -- secondary accent, bottom
        outline      = { 200, 204, 211, 255 },  -- border
        text         = { 38,  41,  48,  255 },
        textOnAccent = { 255, 255, 255, 255 },
        textDisabled = { 156, 160, 168, 255 },
        shadow       = { 0,   0,   0,   0   },
        gloss        = { 255, 255, 255, 0   },
        focusRing    = { 92,  132, 196, 255 },  -- hover and key focus
        track        = { 228, 230, 234, 255 },  -- slider/toggle empty track
        gain         = { 46,  140, 84,  255 },  -- coins: incoming amount
        loss         = { 196, 64,  56,  255 },  -- coins: outgoing amount
    },
    radius       = 8,     -- corner radius (button/panel)
    radiusSmall  = 6,     -- corner radius (chip/checkbox/track)
    outlineWidth = 2,
    gloss        = false,
    shadow       = { dx = 0, dy = 0, layers = 0, grow = 0 },
    textOutline  = false,
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

-- style for widgets made without one (resolved once per manager)
Theme.NONE = {}

-- effective theme: base (DEFAULT when nil) < manager theme < per-widget theme/style
-- {r,g,b} colours inherit alpha from the base
function Theme.resolve(userTheme, style, base)
    local t = Util.merge(base or Theme.DEFAULT, userTheme or {})
    if style then t = Util.merge(t, style) end
    return t
end

return Theme
