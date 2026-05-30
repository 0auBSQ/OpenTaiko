---@diagnostic disable: undefined-global, undefined-field, need-check-nil, lowercase-global
-- canvas_test/Script.lua — A tiny paint program demonstrating LuaCanvas + mouse input.
--
-- Two tools (T switches):
--   Brush — hold LEFT to paint in the current colour, RIGHT to erase (white).
--           The mouse WHEEL changes the brush size.
--   Stamp — a character's Render.png pasted onto the sheet. The mouse WHEEL resizes it,
--           LEFT/RIGHT arrows rotate it (hold to spin), [ ] pick the character, and a
--           single LEFT click stamps it (not held).
--
-- Undo/redo (Ctrl+Z / Ctrl+Y) is handled entirely in Lua as a list of *actions*
-- (brush strokes, stamps, clears). Undo drops the last action and replays the rest onto
-- a blank sheet — so there are no big pixel snapshots to allocate.

local SURF_W, SURF_H = 1920, 1080

local paper       = nil   -- the drawing sheet (full-surface LuaCanvas)
local cursor      = nil   -- small overlay canvas for the brush outline
local renderPaths = {}    -- 1-based list of every character's Render.png path
local renderCount = 0
local stampCache  = {}    -- [idx] = lazily-loaded LuaTexture
local stampIdx    = 1
local fontMid, fontSmall = nil, nil

local CURSOR_BOX  = 160
local CURSOR_HALF = CURSOR_BOX / 2

local floor, min, max = math.floor, math.min, math.max
local cos, sin, pi = math.cos, math.sin, math.pi

-- ── State ───────────────────────────────────────────────────────────────────────
local mode       = "brush"     -- "brush" | "stamp"
local brush      = 7           -- radius in pixels
local paperDirty = false
local lastTs     = 0           -- previous update timestamp (frame-rate-independent dt)

local stampScale = 0.30
local stampRot   = 0.0         -- degrees
local STAMP_SPIN = 120.0       -- deg/sec while an arrow is held

-- undo/redo: a chronological list of applied actions, replayed onto a blank sheet.
-- action kinds: {kind="clear"}, {kind="stamp", x,y,scale,rot,idx},
--               {kind="stroke", size,r,g,b, pts={x0,y0,x1,y1,...}}
local actions      = {}
local redoActions  = {}
local currentStroke = nil      -- the stroke being drawn while a brush button is held

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

-- ── Cursor / colour ───────────────────────────────────────────────────────────────

-- redraw the brush-outline cursor overlay (black ring + centre swatch in the draw colour)
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

-- ── Stamps ──────────────────────────────────────────────────────────────────────

-- Build the stamp list from CHARACTERLIST. The character DB isn't ready during onStart()
-- (it's empty there), so this runs from activate(), the same place character_shop builds
-- its list. Render.png is always at the character folder root.
local function buildStampList()
    renderPaths = {}
    local count = (CHARACTERLIST ~= nil) and CHARACTERLIST.Count or 0
    for i = 0, count - 1 do
        local entry = CHARACTERLIST:GetByIndex(i)
        if entry ~= nil and entry.Character ~= nil then
            local fp = entry.Character.FullPath
            if fp ~= nil and fp ~= "" then
                renderPaths[#renderPaths + 1] = fp .. "/Render.png"
            end
        end
    end
    renderCount = #renderPaths
    if stampIdx > renderCount then stampIdx = 1 end
end

-- Lazily load + cache the stamp texture for a given index, or nil if unavailable.
local function stampTexAt(idx)
    if idx < 1 or idx > renderCount then return nil end
    local t = stampCache[idx]
    if t == nil then
        t = TEXTURE:CreateTextureFromAbsolutePath(renderPaths[idx])
        stampCache[idx] = t
    end
    if t ~= nil and t.Loaded then return t end
    return nil
end

local function currentStamp() return stampTexAt(stampIdx) end

-- ── Undo / redo (action list) ───────────────────────────────────────────────────

-- Draw a single action onto the sheet (does not upload — callers batch the upload).
local function applyAction(a)
    if a.kind == "clear" then
        paper:Clear(255, 255, 255, 255)
    elseif a.kind == "stamp" then
        local st = stampTexAt(a.idx)
        if st ~= nil then
            -- rotation negated so the paste matches the on-screen preview (screen Y flipped)
            paper:PasteTextureTransformed(st, a.x, a.y, a.scale, -a.rot, "center")
        end
    elseif a.kind == "stroke" then
        local p, n = a.pts, #a.pts
        if n >= 4 then
            for i = 1, n - 3, 2 do
                paper:StrokeLine(floor(p[i]), floor(p[i + 1]), floor(p[i + 2]), floor(p[i + 3]),
                    a.size, a.r, a.g, a.b, 255)
            end
        elseif n == 2 then
            paper:StrokeLine(floor(p[1]), floor(p[2]), floor(p[1]), floor(p[2]), a.size, a.r, a.g, a.b, 255)
        end
    end
end

-- Replay every action onto a blank sheet (used by undo).
local function rebuild()
    paper:Clear(255, 255, 255, 255)
    for i = 1, #actions do applyAction(actions[i]) end
    paper:Upload()
end

-- Register a freshly-finished action (clears the redo branch).
local function pushAction(a)
    actions[#actions + 1] = a
    redoActions = {}
end

local function undo()
    if #actions == 0 then return end
    redoActions[#redoActions + 1] = actions[#actions]
    actions[#actions] = nil
    rebuild()
end

local function redo()
    if #redoActions == 0 then return end
    local a = redoActions[#redoActions]
    redoActions[#redoActions] = nil
    actions[#actions + 1] = a
    applyAction(a)   -- additive on top of the current sheet; clear is absolute either way
    paper:Upload()
end

local function clearSheet()
    local a = { kind = "clear" }
    pushAction(a)
    applyAction(a)
    paper:Upload()
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
    paper:Clear(255, 255, 255, 255)
    paper:Upload()
    refreshCursor()
    buildStampList()
    actions, redoActions, currentStroke = {}, {}, nil
    lastTs = 0
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
    paper:SetColor(1, 1, 1)
    paper:SetOpacity(1.0)
    paper:SetScale(SURF_W / paper.Width, SURF_H / paper.Height)
    paper:Draw(0, 0)

    -- tool cursor
    if mInside then
        local st = mode == "stamp" and currentStamp() or nil
        if st ~= nil then
            st:SetColor(1, 1, 1)
            st:SetOpacity(0.5)
            st:SetScale(stampScale, stampScale)
            st:SetRotation(stampRot)
            st:DrawAtAnchor(floor(mMouseX), floor(mMouseY), "center")
            st:SetRotation(0)
        elseif mode == "brush" then
            cursor:SetColor(1, 1, 1)
            cursor:SetOpacity(1.0)
            cursor:SetScale(1, 1)
            cursor:Draw(floor(mMouseX - CURSOR_HALF), floor(mMouseY - CURSOR_HALF))
        end
    end

    -- HUD (static / change-on-input strings, so GetText caches them)
    txt(fontMid, "Canvas Test", 255, 255, 255):Draw(24, 16)
    txt(fontSmall,
        "T: brush/stamp   Left: paint/stamp   Right: erase   1-8: colour   wheel: size   arrows: rotate   [ ]: stamp   C: clear   Ctrl+Z/Y: undo/redo   Esc: back",
        255, 255, 255):Draw(24, 52)
    if mode == "stamp" then
        txt(fontSmall, string.format("Tool: Stamp %d/%d    Scale: %.2f    Rotation: %d",
            renderCount > 0 and stampIdx or 0, renderCount, stampScale, floor(stampRot % 360)),
            255, 255, 255):Draw(24, 82)
    else
        txt(fontSmall, "Tool: Brush    Colour: " .. PALETTE[colIdx][1] .. "    Brush: " .. brush,
            255, 255, 255):Draw(24, 82)
    end
end

-- ── Update ──────────────────────────────────────────────────────────────────────

local function kp(k) return INPUT:KeyboardPressed(k) end
local function kd(k) return INPUT:KeyboardPressing(k) end

-- finalise the in-progress brush stroke (called on release / mode switch)
local function endStroke()
    if currentStroke ~= nil then
        pushAction(currentStroke)
        currentStroke = nil
    end
end

function update(ts)
    -- real elapsed time so stamp rotation speed is frame-rate independent
    local dt = (ts - lastTs) / 1000.0
    lastTs = ts
    if dt < 0 then dt = 0 elseif dt > 0.1 then dt = 0.1 end
    if kp("Escape") or INPUT:Pressed("Cancel") then
        return Exit("stage", "_title")
    end

    -- undo / redo
    local ctrl = kd("LeftControl") or kd("RightControl")
    if ctrl and kp("Z") then endStroke(); undo() end
    if ctrl and kp("Y") then endStroke(); redo() end

    if kp("T") then mode = (mode == "brush") and "stamp" or "brush" end
    if kp("C") then endStroke(); clearSheet() end

    -- colour selection (number keys 1-8)
    local colKeys = { "D1", "D2", "D3", "D4", "D5", "D6", "D7", "D8" }
    for i = 1, #PALETTE do
        if kp(colKeys[i]) then setColor(i) end
    end

    -- mouse wheel: brush size (brush mode) or stamp scale (stamp mode)
    local _, sdy = INPUT:GetScrollDelta()
    if sdy ~= 0 then
        if mode == "stamp" then
            stampScale = max(0.05, min(3.0, stampScale + (sdy > 0 and 0.05 or -0.05)))
        else
            brush = max(1, min(CURSOR_HALF - 4, brush + (sdy > 0 and 1 or -1)))
            refreshCursor()
        end
    end

    -- stamp rotation via held arrows + stamp selection via [ ]
    if mode == "stamp" then
        if kd("LeftArrow")  then stampRot = stampRot - STAMP_SPIN * dt end
        if kd("RightArrow") then stampRot = stampRot + STAMP_SPIN * dt end
        if renderCount > 0 then
            if kp("RightBracket") then stampIdx = stampIdx % renderCount + 1 end
            if kp("LeftBracket")  then stampIdx = (stampIdx - 2) % renderCount + 1 end
        end
    end

    mMouseX = INPUT:GetMouseX()
    mMouseY = INPUT:GetMouseY()
    mInside = INPUT:IsMouseInside()

    if mode == "stamp" then
        endStroke()
        -- single click stamps (not holdable)
        local st = currentStamp()
        if mInside and st ~= nil and INPUT:MousePressed("Left") then
            local a = { kind = "stamp", x = floor(mMouseX), y = floor(mMouseY),
                        scale = stampScale, rot = stampRot, idx = stampIdx }
            pushAction(a)
            applyAction(a)
            paper:Upload()
        end
    else
        local left  = INPUT:MousePressing("Left")
        local right = INPUT:MousePressing("Right")
        if mInside and (left or right) then
            if currentStroke == nil then
                local R, G, B
                if left then R, G, B = curR, curG, curB else R, G, B = 255, 255, 255 end
                currentStroke = { kind = "stroke", size = brush, r = R, g = G, b = B, pts = { mMouseX, mMouseY } }
                paper:StrokeLine(floor(mMouseX), floor(mMouseY), floor(mMouseX), floor(mMouseY), brush, R, G, B, 255)
            else
                local p = currentStroke.pts
                local lx, ly = p[#p - 1], p[#p]
                paper:StrokeLine(floor(lx), floor(ly), floor(mMouseX), floor(mMouseY),
                    currentStroke.size, currentStroke.r, currentStroke.g, currentStroke.b, 255)
                p[#p + 1] = mMouseX; p[#p + 1] = mMouseY
            end
            paperDirty = true
        else
            endStroke()
        end
    end

    if paperDirty then
        paper:Upload()
        paperDirty = false
    end

    return nil
end
