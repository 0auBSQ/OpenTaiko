---@diagnostic disable: undefined-global, undefined-field, need-check-nil, lowercase-global
-- canvas_test/Script.lua — A tiny paint program demonstrating LuaCanvas + mouse input.
--
-- Starts as a blank white sheet. Hold the LEFT mouse button to paint in red, RIGHT to
-- erase (paint white). The paper is a single editable GPU texture (LuaCanvas); a second
-- small canvas is used as the live brush cursor.
--
-- Mouse coordinates come from INPUT:GetMouseX()/GetMouseY(), which the engine maps from
-- raw window pixels into game-surface coordinates — correct even when the window and game
-- resolutions differ (letterbox borders).
--
-- Controls:
--   Left mouse : paint red      Right mouse : erase (white)
--   C : clear sheet             [ / ] : brush size            Escape : back to menu

-- ── Surface / canvas sizing (robust to any skin resolution) ─────────────────────
local SURF_W, SURF_H = 1920, 1080   -- resolved from the engine in onStart()

local paper       = nil   -- the drawing sheet (full-surface LuaCanvas)
local cursor      = nil   -- small overlay canvas for the brush outline
local fontMid, fontSmall = nil, nil

local CURSOR_BOX  = 160
local CURSOR_HALF = CURSOR_BOX / 2

local floor, sqrt, min, max = math.floor, math.sqrt, math.min, math.max
local cos, sin, pi = math.cos, math.sin, math.pi

-- ── State ───────────────────────────────────────────────────────────────────────
local brush      = 7          -- radius in pixels
local lastX, lastY = nil, nil  -- previous paint point (for continuous strokes)
local paperDirty = false

-- cached mouse state for draw()
local mMouseX, mMouseY, mInside = 0, 0, false

-- ── Brush stamping ──────────────────────────────────────────────────────────────

-- filled disc of radius r at (cx,cy), one FillRect per scanline
local function stamp(cv, cx, cy, r, R, G, B)
    cx = floor(cx); cy = floor(cy)
    for dy = -r, r do
        local dx = floor(sqrt(max(0, r * r - dy * dy)) + 0.5)
        cv:FillRect(cx - dx, cy + dy, 2 * dx + 1, 1, R, G, B, 255)
    end
end

-- stamp discs along the segment so fast strokes stay continuous
local function paintSegment(x0, y0, x1, y1, r, R, G, B)
    local dx, dy = x1 - x0, y1 - y0
    local dist = sqrt(dx * dx + dy * dy)
    local steps = max(1, floor(dist / max(1, r * 0.5)))
    for i = 0, steps do
        local t = i / steps
        stamp(paper, x0 + dx * t, y0 + dy * t, r, R, G, B)
    end
    paperDirty = true
end

local function clearSheet()
    paper:Clear(255, 255, 255, 255)
    paper:Upload()
end

-- redraw the brush-outline cursor overlay for the current radius
local function refreshCursor()
    cursor:ClearTransparent()
    local n = max(24, floor(brush * 5))
    for i = 0, n - 1 do
        local a = i / n * 2 * pi
        local px = floor(CURSOR_HALF + cos(a) * brush + 0.5)
        local py = floor(CURSOR_HALF + sin(a) * brush + 0.5)
        cursor:FillRect(px, py, 2, 2, 0, 0, 0, 220)
    end
    cursor:FillRect(CURSOR_HALF - 1, CURSOR_HALF - 1, 2, 2, 0, 0, 0, 255)  -- centre dot
    cursor:Upload()
end

-- ── Lifecycle ─────────────────────────────────────────────────────────────────────

function onStart()
    SURF_W = INPUT:GetSurfaceWidth()
    SURF_H = INPUT:GetSurfaceHeight()
    if SURF_W <= 0 then SURF_W = 1920 end
    if SURF_H <= 0 then SURF_H = 1080 end

    paper  = CANVAS:CreateCanvas(SURF_W, SURF_H)
    cursor = CANVAS:CreateCanvas(CURSOR_BOX, CURSOR_BOX)

    fontMid   = TEXT:Create(24)
    fontSmall = TEXT:Create(18)
end

function activate()
    clearSheet()
    refreshCursor()
    lastX, lastY = nil, nil
end

function deactivate() end
function afterSongEnum() end
function onDestroy() end

-- ── Draw ────────────────────────────────────────────────────────────────────────

local function txt(font, str, r, g, b)
    return font:GetText(str, false, 1800,
        COLOR:CreateColorFromRGBA(r or 255, g or 255, b or 255, 255),
        COLOR:CreateColorFromRGBA(0, 0, 0, 255))
end

function draw()
    -- the sheet: one draw call, scaled to fill the surface (scale is 1 when canvas
    -- matches the surface, but stays correct for any resolution)
    paper:SetColor(1, 1, 1)
    paper:SetOpacity(1.0)
    paper:SetScale(SURF_W / paper.Width, SURF_H / paper.Height)
    paper:Draw(0, 0)

    -- brush cursor overlay (drawn in surface coords, scale 1)
    if mInside then
        cursor:SetColor(1, 1, 1)
        cursor:SetOpacity(1.0)
        cursor:SetScale(1, 1)
        cursor:Draw(floor(mMouseX - CURSOR_HALF), floor(mMouseY - CURSOR_HALF))
    end

    -- HUD
    txt(fontMid, "Canvas Test", 30, 30, 30):Draw(24, 16)
    txt(fontSmall,
        "Left: paint red   Right: erase   C: clear   [ / ]: brush (" .. brush .. ")   Esc: back",
        40, 40, 40):Draw(24, 52)
    if mInside then
        txt(fontSmall, string.format("mouse: %d, %d", floor(mMouseX), floor(mMouseY)), 90, 90, 90)
            :Draw(24, SURF_H - 34)
    end
end

-- ── Update ──────────────────────────────────────────────────────────────────────

local function kp(k) return INPUT:KeyboardPressed(k) end

function update(ts)
    if kp("Escape") or INPUT:Pressed("Cancel") then
        return Exit("stage", "_title")
    end

    if kp("C") then clearSheet() end
    if kp("LeftBracket")  then brush = max(1, brush - 1);  refreshCursor() end
    if kp("RightBracket") then brush = min(CURSOR_HALF - 4, brush + 1); refreshCursor() end

    mMouseX = INPUT:GetMouseX()
    mMouseY = INPUT:GetMouseY()
    mInside = INPUT:IsMouseInside()

    local left  = INPUT:MousePressing("Left")
    local right = INPUT:MousePressing("Right")

    if mInside and (left or right) then
        local R, G, B
        if left then R, G, B = 255, 0, 0 else R, G, B = 255, 255, 255 end
        if lastX == nil then lastX, lastY = mMouseX, mMouseY end
        paintSegment(lastX, lastY, mMouseX, mMouseY, brush, R, G, B)
        lastX, lastY = mMouseX, mMouseY
    else
        lastX, lastY = nil, nil
    end

    if paperDirty then
        paper:Upload()
        paperDirty = false
    end

    return nil
end
