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
--   Left mouse : paint      Right mouse : erase (white)      1-8 : pick colour
--   C : clear sheet         [ / ] : brush size               Escape : back to menu

-- ── Surface / canvas sizing (robust to any skin resolution) ─────────────────────
local SURF_W, SURF_H = 1920, 1080   -- resolved from the engine in onStart()

local paper       = nil   -- the drawing sheet (full-surface LuaCanvas)
local cursor      = nil   -- small overlay canvas for the brush outline
local fontMid, fontSmall = nil, nil

local CURSOR_BOX  = 160
local CURSOR_HALF = CURSOR_BOX / 2

local floor, min, max = math.floor, math.min, math.max
local cos, sin, pi = math.cos, math.sin, math.pi

-- ── State ───────────────────────────────────────────────────────────────────────
local brush      = 7          -- radius in pixels
local lastX, lastY = nil, nil  -- previous paint point (for continuous strokes)
local paperDirty = false

-- draw-colour palette (number keys 1-8)
local PALETTE = {
    { "Red",    255,  40,  40 },
    { "Orange", 255, 140,   0 },
    { "Yellow", 245, 215,  40 },
    { "Green",   40, 175,  60 },
    { "Blue",    45, 110, 230 },
    { "Purple", 150,  60, 205 },
    { "Black",   20,  20,  20 },
    { "White",  255, 255, 255 },
}
local colIdx = 1
local curR, curG, curB = 255, 40, 40

-- cached mouse state for draw()
local mMouseX, mMouseY, mInside = 0, 0, false

-- ── Brush stamping ──────────────────────────────────────────────────────────────

-- Paint a whole brush stroke in a single C# call (StrokeLine stamps the discs).
local function paintSegment(x0, y0, x1, y1, r, R, G, B)
    paper:StrokeLine(floor(x0), floor(y0), floor(x1), floor(y1), r, R, G, B, 255)
    paperDirty = true
end

local function clearSheet()
    paper:Clear(255, 255, 255, 255)
    paper:Upload()
end

-- redraw the brush-outline cursor overlay (black ring for visibility on any colour,
-- with a centre swatch in the current draw colour)
local function refreshCursor()
    cursor:ClearTransparent()
    local n = max(24, floor(brush * 5))
    for i = 0, n - 1 do
        local a = i / n * 2 * pi
        local px = floor(CURSOR_HALF + cos(a) * brush + 0.5)
        local py = floor(CURSOR_HALF + sin(a) * brush + 0.5)
        cursor:FillRect(px, py, 2, 2, 0, 0, 0, 230)
    end
    cursor:FillCircle(CURSOR_HALF, CURSOR_HALF, max(2, min(brush - 1, 5)), curR, curG, curB, 255)
    cursor:Upload()
end

local function setColor(i)
    if PALETTE[i] == nil then return end
    colIdx = i
    curR, curG, curB = PALETTE[i][2], PALETTE[i][3], PALETTE[i][4]
    refreshCursor()
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

    -- HUD (white text; the txt() helper gives it a black outline so it stays readable
    -- on a white sheet). All strings here are static or change only on key press, so
    -- GetText caches them — no per-frame font rasterisation.
    txt(fontMid, "Canvas Test", 255, 255, 255):Draw(24, 16)
    txt(fontSmall,
        "Left: paint   Right: erase   1-8: colour   [ / ]: brush   C: clear   Esc: back",
        255, 255, 255):Draw(24, 52)
    txt(fontSmall, "Colour: " .. PALETTE[colIdx][1] .. "    Brush: " .. brush,
        255, 255, 255):Draw(24, 82)
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

    -- colour selection (number keys 1-8)
    local colKeys = { "D1", "D2", "D3", "D4", "D5", "D6", "D7", "D8" }
    for i = 1, #PALETTE do
        if kp(colKeys[i]) then setColor(i) end
    end

    mMouseX = INPUT:GetMouseX()
    mMouseY = INPUT:GetMouseY()
    mInside = INPUT:IsMouseInside()

    local left  = INPUT:MousePressing("Left")
    local right = INPUT:MousePressing("Right")

    if mInside and (left or right) then
        local R, G, B
        if left then R, G, B = curR, curG, curB else R, G, B = 255, 255, 255 end
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
