-- theme_settings_test/Script.lua
-- Simple read-only viewer for all skin-specific settings defined in ThemeSettings.json.
-- Shows each setting's id, scope, type, and current value.
-- RightChange/DownArrow → scroll down
-- LeftChange/UpArrow    → scroll up
-- Cancel/Escape         → exit

-- ─────────────────────────────────────────────────────────────────────────────
-- State
-- ─────────────────────────────────────────────────────────────────────────────

local font   = nil
local sounds = {}

-- Current scroll offset (first visible row index, 0-based)
local scrollIdx = 0

-- Rows built in activate(): list of { id, scope, type, value } strings
local rows = {}

-- How many rows fit on screen at once
local VISIBLE_ROWS = 20
local ROW_H        = 44
local LIST_TOP     = 80
local LIST_LEFT    = 60

-- Column x positions
local COL_SCOPE = LIST_LEFT + 480
local COL_TYPE  = LIST_LEFT + 680
local COL_VALUE = LIST_LEFT + 880

-- Max widths for text rendering (prevents overflow)
local MAX_W_ID    = 410
local MAX_W_SCOPE = 190
local MAX_W_TYPE  = 190
local MAX_W_VALUE = 600

-- ─────────────────────────────────────────────────────────────────────────────
-- Helpers
-- ─────────────────────────────────────────────────────────────────────────────

local function clamp(v, lo, hi)
    if v < lo then return lo end
    if v > hi then return hi end
    return v
end

local function buildRows()
    rows = {}
    local count = THEME:GetDefinitionCount()

    if count == 0 then
        rows[#rows + 1] = {
            id    = "(no ThemeSettings.json found)",
            scope = "",
            type  = "",
            value = "",
        }
        return
    end

    for i = 0, count - 1 do
        local id    = THEME:GetDefinitionId(i)
        local scope = THEME:GetDefinitionScope(i)
        local type_ = THEME:GetDefinitionType(i)
        local value
        if scope == "save" then
            value = THEME:GetThemeSettingForPlayer(id, 1)
        else
            value = THEME:GetThemeSetting(id)
        end
        rows[#rows + 1] = {
            id    = id,
            scope = scope,
            type  = type_,
            value = tostring(value),
        }
    end
end

-- Draw a text string anchored at (x, y) top-left, with an optional color.
local function drawText(str, x, y, maxW, color)
    local tx = font:GetText(str, false, maxW, color)
    tx:DrawAtAnchor(x, y, "topleft")
end

-- ─────────────────────────────────────────────────────────────────────────────
-- update / draw
-- ─────────────────────────────────────────────────────────────────────────────

function update(_timestamp)
    -- Cancel / Back → exit back to the previous stage
    if INPUT:Pressed("Cancel") or INPUT:KeyboardPressed("Escape") then
        sounds.Cancel:Play()
        return Exit("title", nil)
    end

    local maxScroll = math.max(0, #rows - VISIBLE_ROWS)

    if INPUT:Pressed("RightChange") or INPUT:KeyboardPressed("DownArrow") then
        local prev = scrollIdx
        scrollIdx = clamp(scrollIdx + 1, 0, maxScroll)
        if scrollIdx ~= prev then sounds.Move:Play() end
    end

    if INPUT:Pressed("LeftChange") or INPUT:KeyboardPressed("UpArrow") then
        local prev = scrollIdx
        scrollIdx = clamp(scrollIdx - 1, 0, maxScroll)
        if scrollIdx ~= prev then sounds.Move:Play() end
    end
end

function draw()
    local white  = COLOR:CreateColorFromRGBA(255, 255, 255, 255)
    local yellow = COLOR:CreateColorFromRGBA(242, 207,   1, 255)
    local cyan   = COLOR:CreateColorFromRGBA(100, 220, 255, 255)
    local purple = COLOR:CreateColorFromRGBA(180, 180, 255, 255)
    local gray   = COLOR:CreateColorFromRGBA(160, 160, 160, 255)

    -- Title
    drawText(THEME:GetSkinString("VIEWER_TITLE"), LIST_LEFT, 20, 800, yellow)

    -- Column headers
    local headerY = LIST_TOP - ROW_H
    drawText(THEME:GetSkinString("VIEWER_COL_ID"),    LIST_LEFT,  headerY, MAX_W_ID,    purple)
    drawText(THEME:GetSkinString("VIEWER_COL_SCOPE"), COL_SCOPE,  headerY, MAX_W_SCOPE, purple)
    drawText(THEME:GetSkinString("VIEWER_COL_TYPE"),  COL_TYPE,   headerY, MAX_W_TYPE,  purple)
    drawText(THEME:GetSkinString("VIEWER_COL_VALUE"), COL_VALUE,  headerY, MAX_W_VALUE, purple)

    -- Separator line (rendered as text dashes)
    drawText(string.rep("-", 110), LIST_LEFT, LIST_TOP - 10, 1800, gray)

    -- Rows
    local endIdx = math.min(scrollIdx + VISIBLE_ROWS, #rows)
    for i = scrollIdx + 1, endIdx do
        local row = rows[i]
        local y   = LIST_TOP + (i - scrollIdx - 1) * ROW_H

        -- Alternate row color for id
        local idColor = (i % 2 == 0) and white or COLOR:CreateColorFromRGBA(220, 220, 220, 255)

        drawText(row.id,    LIST_LEFT, y, MAX_W_ID,    idColor)
        drawText(row.scope, COL_SCOPE, y, MAX_W_SCOPE, cyan)
        drawText(row.type,  COL_TYPE,  y, MAX_W_TYPE,  cyan)
        drawText(row.value, COL_VALUE, y, MAX_W_VALUE, yellow)
    end

    -- Scroll indicator / hint
    if #rows > VISIBLE_ROWS then
        local info = string.format("(%d-%d / %d)   %s",
            scrollIdx + 1, endIdx, #rows, THEME:GetSkinString("VIEWER_HINT_SCROLL"))
        drawText(info, LIST_LEFT, 1040, 1200, gray)
    else
        drawText(THEME:GetSkinString("VIEWER_HINT_EXIT"), LIST_LEFT, 1040, 400, gray)
    end
end

-- ─────────────────────────────────────────────────────────────────────────────
-- Stage events
-- ─────────────────────────────────────────────────────────────────────────────

function activate()
    scrollIdx = 0
    buildRows()
end

function deactivate()
end

function onStart()
    font = TEXT:Create(22)

    sounds.Move   = SOUND:CreateSFX("Sounds/Move.ogg")
    sounds.Cancel = SOUND:CreateSFX("Sounds/Cancel.ogg")
end

function onDestroy()
    if font ~= nil then
        font:Dispose()
        font = nil
    end
    for _, snd in pairs(sounds) do
        snd:Dispose()
    end
    sounds = {}
end
