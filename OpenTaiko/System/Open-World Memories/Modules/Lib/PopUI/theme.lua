---@diagnostic disable: lowercase-global
-- PopUI default theme (colors {r,g,b,a}, 0-255)
-- per-widget theme/style tables override per field

local Util = require "Util"
local Base = require("UI.SimpleUI.theme")

local Theme = {}

-- ==== Default palette: Bubblegum ====
Theme.DEFAULT = {
    name = "Bubblegum",
    colors = {
        bg           = { 255, 240, 248, 255 },  -- screen background (if the manager draws one)
        surface      = { 255, 255, 255, 255 },  -- panel/button face (gradient top)
        surface2     = { 255, 226, 242, 255 },  -- panel/button face (gradient bottom)
        primary      = { 255, 120, 176, 255 },  -- accent face (top)
        primary2     = { 244, 78,  150, 255 },  -- accent face (bottom)
        accent       = { 122, 212, 255, 255 },  -- secondary accent (top)
        accent2      = { 70,  178, 244, 255 },  -- secondary accent (bottom)
        outline      = { 74,  38,  72,  255 },  -- border and default text
        text         = { 74,  38,  72,  255 },
        textOnAccent = { 255, 255, 255, 255 },
        textDisabled = { 176, 158, 172, 255 },
        shadow       = { 60,  30,  60,  90  },  -- drop shadow
        gloss        = { 255, 255, 255, 150 },  -- top highlight
        focusRing    = { 255, 206, 84,  255 },  -- shared hover + keyboard-select highlight
        track        = { 236, 222, 234, 255 },  -- slider/toggle empty track
    },
    radius       = 26,    -- corner radius for big elements (button/panel)
    radiusSmall  = 13,    -- corner radius for small elements (chip/checkbox/track)
    outlineWidth = 5,     -- border thickness
    gloss        = true,  -- draw the top highlight
    shadow       = { dx = 0, dy = 7, layers = 4, grow = 3 },  -- layered silhouette shadow
    font = { small = 18, label = 22, button = 27, title = 34 },
    anim = {
        hoverScale = 1.06, pressScale = 0.93,
        hoverTime  = 0.16, pressTime  = 0.07, releaseTime = 0.26,
        hoverEase  = { "OUT", "BACK" },
        pressEase  = { "OUT", "QUAD" },
        popInEase  = { "OUT", "ELASTIC" },  -- entrance
        highlightTime = 0.14,
        typewriterCps = 42,
    },
}

Theme.clone = Util.deepcopy
Theme.merge = Util.merge

-- style of widgets made without one (resolved once by the manager)
Theme.NONE = Base.NONE

-- effective theme = DEFAULT < manager theme < per-widget theme/style
-- {r,g,b} colors inherit alpha from the base styles
function Theme.resolve(userTheme, style)
    return Base.resolve(userTheme, style, Theme.DEFAULT)
end

return Theme
