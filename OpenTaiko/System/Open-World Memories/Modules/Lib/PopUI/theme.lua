---@diagnostic disable: lowercase-global
-- PopUI/theme.lua — the look of the UI in one table. Colors are {r,g,b,a} 0-255 (the form the canvas
-- pixel ops want directly). Everything is tweakable; per-widget `theme/style` tables override per-field.
-- PopUI is UI.SimpleUI with this theme as the base every theme resolves over.

local Util = require "Util"
local Base = require("UI.SimpleUI.theme")

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

Theme.clone = Util.deepcopy
Theme.merge = Util.merge

-- the style of every widget made without one (the manager resolves it once for all of them)
Theme.NONE = Base.NONE

-- resolve an effective theme = DEFAULT < manager theme < per-widget theme/style.
-- NOTICE: colors defined in {r,g,b} have the alpha inherited from base styles
function Theme.resolve(userTheme, style)
    return Base.resolve(userTheme, style, Theme.DEFAULT)
end

return Theme
