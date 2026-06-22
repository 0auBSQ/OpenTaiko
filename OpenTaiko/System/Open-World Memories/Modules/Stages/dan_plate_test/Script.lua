-- dan_plate_test/Script.lua
-- Visual test stage for the "danplate" ROActivity.
-- Displays a grid of dan plates covering all frame indices (0–8), three tint
-- colours, and title strings of varying lengths.
-- Escape / Cancel → exit back to title.

local danplate = nil
local font     = nil

-- ─────────────────────────────────────────────────────────────────────────────
-- Test cases
-- Each entry: { tick, r, g, b, title }
-- ─────────────────────────────────────────────────────────────────────────────
local CASES = {
    -- Standard frames 0–5 (white tint)
    { tick = 0, r = 255, g = 255, b = 255, title = "新人" },
    { tick = 1, r = 255, g = 255, b = 255, title = "五級" },
    { tick = 2, r = 255, g = 255, b = 255, title = "四段" },
    { tick = 3, r = 255, g = 255, b = 255, title = "七段" },
    { tick = 4, r = 255, g = 255, b = 255, title = "玄人" },
    { tick = 5, r = 255, g = 255, b = 255, title = "達人" },
    -- Wrap: danTick 6–8 wraps modulo 6 → frames 0–2
    { tick = 6, r = 255, g = 128, b =   0, title = "初段" },
    { tick = 7, r =   0, g = 200, b = 255, title = "十段" },
    { tick = 8, r = 220, g =   0, b = 220, title = "外伝" },
    -- Colour variations on frame 5
    { tick = 5, r = 255, g =  80, b =  80, title = "達人" },  -- red tint
    { tick = 5, r =  80, g = 200, b =  80, title = "達人" },  -- green tint
    { tick = 5, r =  80, g = 120, b = 255, title = "達人" },  -- blue tint
    -- Long titles: 3, 4, 5, 6 characters (height-capped)
    { tick = 2, r = 255, g = 255, b = 255, title = "全王者" },
    { tick = 3, r = 255, g = 255, b = 255, title = "超人達人" },
    { tick = 4, r = 255, g = 200, b =  50, title = "天下無双" },
    { tick = 0, r = 200, g = 200, b = 200, title = "真の達人" },
}

-- Layout: 2 rows × 8 columns
local COLS     = 8
local CELL_W   = 240
local CELL_H   = 560
local ORIGIN_X = 100
local ORIGIN_Y = 220

-- ─────────────────────────────────────────────────────────────────────────────
-- Lifecycle
-- ─────────────────────────────────────────────────────────────────────────────

function onStart()
    font = TEXT:Create(20, "regular")
end

function onDestroy()
    if font ~= nil then font:Dispose() ; font = nil end
end

function activate()
    danplate = ROACTIVITY:GetROActivity("danplate")
end

function deactivate()
    danplate = nil
end

-- ─────────────────────────────────────────────────────────────────────────────
-- update / draw
-- ─────────────────────────────────────────────────────────────────────────────

function update(_ts)
    if INPUT:Pressed("Cancel") or INPUT:KeyboardPressed("Escape") then
        return Exit("title", nil)
    end
end

function draw()
    -- Title bar
    local titleTex = font:GetText("danplate ROActivity test  |  Esc = exit", false, 1600)
    titleTex:DrawAtAnchor(40, 20, "topleft")

    if danplate == nil then
        local errTex = font:GetText("ERROR: danplate ROActivity not loaded", false, 1200)
        errTex:DrawAtAnchor(40, 80, "topleft")
        return
    end

    -- Draw grid
    for i, c in ipairs(CASES) do
        local col = (i - 1) % COLS
        local row = math.floor((i - 1) / COLS)
        local cx  = ORIGIN_X + col * CELL_W
        local cy  = ORIGIN_Y + row * CELL_H

        danplate:Draw(cx, cy, 255, c.tick, c.r, c.g, c.b, c.title)
    end
end
