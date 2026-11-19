-- read-only viewer for the ThemeSettings.json settings (id, scope, type, value)
-- Down/Up scroll, Cancel/Escape exits

local NavInput = require("Input.NavInput").p[0]   -- navigation keys only, no drums
local I18N = require("Locale.i18n")
local T = I18N.texts("theme_viewer", "theme_settings/")

local D = {}

-- ==== state ====

local font   = nil
local sounds = {}

-- scroll offset: first visible row, 0-based
local scrollIdx = 0

-- rows built in activate(): { id, scope, type, value } strings
local rows = {}

local VISIBLE_ROWS = 20
local ROW_H        = 44
local LIST_TOP     = 80
local LIST_LEFT    = 60

-- column x positions
local COL_SCOPE = LIST_LEFT + 480
local COL_TYPE  = LIST_LEFT + 680
local COL_VALUE = LIST_LEFT + 880

-- max text widths
local MAX_W_ID    = 410
local MAX_W_SCOPE = 190
local MAX_W_TYPE  = 190
local MAX_W_VALUE = 600

-- ==== helpers ====

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
            value = THEME:GetThemeSettingForPlayer(id, 0)
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

-- top-left text at (x, y), optional colour
local function drawText(str, x, y, maxW, color)
    local tx = font:GetText(str, false, maxW, color)
    tx:DrawAtAnchor(x, y, "topleft")
end

-- ==== update / draw ====

function D.update(_timestamp)
    if NavInput.cancel() then
        sounds.Cancel:Play()
        return Exit("stage", "my_room")
    end

    local maxScroll = math.max(0, #rows - VISIBLE_ROWS)

    if NavInput.downOrPadRight() then
        local prev = scrollIdx
        scrollIdx = clamp(scrollIdx + 1, 0, maxScroll)
        if scrollIdx ~= prev then sounds.Move:Play() end
    end

    if NavInput.upOrPadLeft() then
        local prev = scrollIdx
        scrollIdx = clamp(scrollIdx - 1, 0, maxScroll)
        if scrollIdx ~= prev then sounds.Move:Play() end
    end
end

function D.draw()
    local white  = COLOR:CreateColorFromRGBA(255, 255, 255, 255)
    local yellow = COLOR:CreateColorFromRGBA(242, 207,   1, 255)
    local cyan   = COLOR:CreateColorFromRGBA(100, 220, 255, 255)
    local purple = COLOR:CreateColorFromRGBA(180, 180, 255, 255)
    local gray   = COLOR:CreateColorFromRGBA(160, 160, 160, 255)

    drawText(T:tr("title"), LIST_LEFT, 20, 800, yellow)

    -- column headers
    local headerY = LIST_TOP - ROW_H
    drawText(T:tr("col_id"),    LIST_LEFT,  headerY, MAX_W_ID,    purple)
    drawText(T:tr("col_scope"), COL_SCOPE,  headerY, MAX_W_SCOPE, purple)
    drawText(T:tr("col_type"),  COL_TYPE,   headerY, MAX_W_TYPE,  purple)
    drawText(T:tr("col_value"), COL_VALUE,  headerY, MAX_W_VALUE, purple)

    -- separator line (text dashes)
    drawText(string.rep("-", 110), LIST_LEFT, LIST_TOP - 10, 1800, gray)

    -- rows
    local endIdx = math.min(scrollIdx + VISIBLE_ROWS, #rows)
    for i = scrollIdx + 1, endIdx do
        local row = rows[i]
        local y   = LIST_TOP + (i - scrollIdx - 1) * ROW_H

        local idColor = (i % 2 == 0) and white or COLOR:CreateColorFromRGBA(220, 220, 220, 255)

        drawText(row.id,    LIST_LEFT, y, MAX_W_ID,    idColor)
        drawText(row.scope, COL_SCOPE, y, MAX_W_SCOPE, cyan)
        drawText(row.type,  COL_TYPE,  y, MAX_W_TYPE,  cyan)
        drawText(row.value, COL_VALUE, y, MAX_W_VALUE, yellow)
    end

    -- scroll hint
    if #rows > VISIBLE_ROWS then
        local info = string.format("(%d-%d / %d)   %s",
            scrollIdx + 1, endIdx, #rows, T:tr("hint_scroll"))
        drawText(info, LIST_LEFT, 1040, 1200, gray)
    else
        drawText(T:tr("hint_exit"), LIST_LEFT, 1040, 400, gray)
    end
end

-- ==== stage events ====

function D.activate()
    I18N.detect()
    scrollIdx = 0
    buildRows()
end

function D.deactivate()
end

function D.onStart()
    font = TEXT:Create(22)

    sounds.Move   = SOUND:CreateSFX("theme_settings/Sounds/Move.ogg")
    sounds.Cancel = SOUND:CreateSFX("theme_settings/Sounds/Cancel.ogg")
end

function D.onDestroy()
    if font ~= nil then
        font:Dispose()
        font = nil
    end
    for _, snd in pairs(sounds) do
        snd:Dispose()
    end
    sounds = {}
end

return D
