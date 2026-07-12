---@diagnostic disable: undefined-global, lowercase-global, need-check-nil, undefined-field
-- popui_demo — a showcase for the PopUI library (Lib/PopUI). Two pages of widgets + a live theme
-- switcher that re-skins everything, driven by mouse OR keyboard/gamepad. Esc returns to the title.

local PopUI = require("PopUI")

local ui
local page = 1
local page1, page2 = {}, {}
local clickCount = 0
local lastTs = 0

-- a few original colour palettes to prove customization (no real product is referenced)
local THEMES = {
    { name = "Bubblegum", theme = {} },
    { name = "Aqua", theme = { colors = {
        primary = { 86, 196, 222 }, primary2 = { 44, 160, 196 }, accent = { 255, 200, 120 }, accent2 = { 240, 170, 80 },
        bg = { 232, 248, 252 }, surface = { 255, 255, 255 }, surface2 = { 214, 240, 246 },
        outline = { 32, 70, 86 }, text = { 32, 70, 86 }, focusRing = { 255, 210, 90 } } } },
    { name = "Sunset", theme = { colors = {
        primary = { 255, 150, 90 }, primary2 = { 240, 96, 120 }, accent = { 150, 120, 230 }, accent2 = { 110, 90, 210 },
        bg = { 255, 242, 234 }, surface = { 255, 255, 255 }, surface2 = { 255, 224, 210 },
        outline = { 92, 44, 52 }, text = { 92, 44, 52 }, focusRing = { 120, 220, 200 } } } },
    { name = "Mint", theme = { colors = {
        primary = { 120, 210, 150 }, primary2 = { 80, 182, 130 }, accent = { 255, 160, 190 }, accent2 = { 240, 120, 160 },
        bg = { 238, 250, 240 }, surface = { 255, 255, 255 }, surface2 = { 220, 244, 228 },
        outline = { 40, 84, 60 }, text = { 40, 84, 60 }, focusRing = { 255, 198, 90 } } } },
}
local themeIdx = 1

-- ── Bubble Lab: drag the bubble body, its tail handle, or its resize handle, to test geometry live ──────
local labBubble, handleCv, sizeCv
local tailHandle = { x = 0, y = 0 }
local sizeHandle = { x = 0, y = 0 }   -- bottom-right resize grip
local dragMode, grabDX, grabDY

local function clampn(v, a, b) return (v < a) and a or ((v > b) and b or v) end

local function updateSizeHandle()
    if not labBubble then return end
    sizeHandle.x, sizeHandle.y = labBubble.x + labBubble.w, labBubble.y + labBubble.h
end

local function bubbleAnchor(b)   -- box-edge anchor + outward normal for the current tail (handle control point)
    local pos = b.tailPos
    if b.tail == "left" then return b.x, b.y + b.h * pos, -1, 0
    elseif b.tail == "right" then return b.x + b.w, b.y + b.h * pos, 1, 0
    elseif b.tail == "top" then return b.x + b.w * pos, b.y, 0, -1
    else return b.x + b.w * pos, b.y + b.h, 0, 1 end
end
local function updateHandle()
    if not labBubble then return end
    local ax, ay, nx, ny = bubbleAnchor(labBubble)
    tailHandle.x, tailHandle.y = ax + nx * labBubble.tailSize, ay + ny * labBubble.tailSize
end
local function tailFromHandle(b, mx, my)   -- which side/pos/size the handle position implies
    local l, rr, t, bo = b.x - mx, mx - (b.x + b.w), b.y - my, my - (b.y + b.h)
    local mv = math.max(l, rr, t, bo)
    local side, pos, size
    if mv == l then side, pos, size = "left", (my - b.y) / b.h, l
    elseif mv == rr then side, pos, size = "right", (my - b.y) / b.h, rr
    elseif mv == t then side, pos, size = "top", (mx - b.x) / b.w, t
    else side, pos, size = "bottom", (mx - b.x) / b.w, bo end
    return side, clampn(pos, 0.18, 0.82), clampn(size, 14, 90)
end

local function setPage(p)
    page = p
    for _, w in ipairs(page1) do w.visible = (p == 1) end
    for _, w in ipairs(page2) do w.visible = (p == 2) end
    ui:_rebuildFocus()
end

local function applyTheme(i)
    themeIdx = i
    ui:setTheme(THEMES[i].theme)
end

function onStart()
    -- optional UI sounds: only used if the files exist (the lib no-ops on a nil/unloaded sound)
    local function sfx(name)
        local ok, s = pcall(function() return SOUND:CreateSFX(name) end)
        return ok and s or nil
    end

    ui = PopUI.new{
        bg = true,
        theme = THEMES[themeIdx].theme,
        sfx = { hover = sfx("Move.ogg"), click = sfx("Decide.ogg"), move = sfx("Move.ogg"),
                toggle = sfx("Decide.ogg"), error = sfx("Cancel.ogg") },
    }

    -- ── top bar (always visible) ─────────────────────────────────────────────────
    ui:label{ text = "PopUI Showcase", x = 60, y = 36, size = "title" }
    ui:button{ text = "Widgets", x = 470, y = 28, w = 220, h = 64, accent = true, onClick = function() setPage(1) end }
    ui:button{ text = "Chat & Menu", x = 700, y = 28, w = 260, h = 64, onClick = function() setPage(2) end }
    local cx = 1060
    for i, t in ipairs(THEMES) do
        ui:button{ text = t.name, x = cx, y = 32, w = 180, h = 56,
            style = (i == 1) and nil or t.theme,            -- chip previews its own palette
            onClick = function() applyTheme(i) end }
        cx = cx + 196
    end

    -- ── page 1: widgets ─────────────────────────────────────────────────────────
    local function P1(w) page1[#page1 + 1] = w; return w end
    P1(ui:panel{ x = 60, y = 130, w = 820, h = 820, title = "Widgets" })
    local greet = P1(ui:label{ text = "Hi there!", x = 110, y = 210, size = "label" })
    local clicks = P1(ui:label{ text = "Clicked 0 times", x = 110, y = 260, size = "small" })
    P1(ui:button{ text = "Press me!", x = 110, y = 320, w = 320, h = 96, accent = true, onClick = function(b)
        clickCount = clickCount + 1
        clicks:setText("Clicked " .. clickCount .. " times")
    end })
    P1(ui:button{ text = "Disabled", x = 470, y = 320, w = 280, h = 96, onClick = function() end }).enabled = false
    P1(ui:toggle{ text = "Fullscreen", x = 110, y = 450, value = true, onChange = function(v) end })
    P1(ui:checkbox{ text = "Mute audio", x = 110, y = 520, value = false, onChange = function(v) end })
    P1(ui:label{ text = "Volume", x = 110, y = 600, size = "small" })
    P1(ui:slider{ x = 110, y = 636, w = 560, min = 0, max = 100, step = 1, value = 70, onChange = function(v) end })
    P1(ui:label{ text = "Your name", x = 110, y = 720, size = "small" })
    P1(ui:textbox{ x = 110, y = 756, w = 560, placeholder = "type here + Enter...", maxLen = 18,
        onSubmit = function(t) greet:setText(t ~= "" and ("Hi, " .. t .. "!") or "Hi there!") end })

    -- ── page 2: chat & menu ─────────────────────────────────────────────────────
    local function P2(w) page2[#page2 + 1] = w; return w end
    local COMMENTS = {
        "A gentle opener — great for warming up your wrists!",
        "Watch the tricky triplets in the second half.",
        "Pure speed. Hold on tight and keep your cool.",
        "A cute, bouncy tune everyone loves.",
        "Boss territory. You've got this!",
        "Hidden gem — give it a try.",
    }
    local bubble = P2(ui:bubble{ x = 700, y = 200, w = 700, h = 220, tail = "left", tailPos = 0.4,
        typewriter = true, text = COMMENTS[1] })
    labBubble = bubble
    local items = { "Sunrise Stroll", "Triplet Trouble", "Hyper Drive", "Marshmallow Hop", "Final Fury", "Secret Track" }
    P2(ui:menu{ x = 120, y = 200, w = 520, h = 700, items = items, selected = 1,
        onChange = function(i) bubble:setText(COMMENTS[i] or "...", true) end,
        onSelect = function(i, it) bubble:setText("Now playing: " .. it.text .. "!", true) end })
    P2(ui:panel{ x = 700, y = 470, w = 700, h = 430, title = "How to use" })
    P2(ui:label{ text = "Mouse: hover + click. Keyboard/Pad: arrows move,\nDecide selects, Cancel/Esc backs out.",
        x = 740, y = 560, size = "label", maxWidth = 640 })
    P2(ui:label{ text = "Drag the bubble, its gold tail pin, or the blue corner grip (resize).",
        x = 740, y = 690, size = "small", maxWidth = 640 })
    -- show off the different bubble shapes
    local SHAPES = { "round", "oval", "cloud" }
    local shapeIdx = 1
    P2(ui:button{ text = "Bubble shape: round", x = 740, y = 790, w = 420, h = 72, accent = true, onClick = function(b)
        shapeIdx = shapeIdx % #SHAPES + 1
        bubble:setShape(SHAPES[shapeIdx])
        b:setText("Bubble shape: " .. SHAPES[shapeIdx])
    end })

    -- tail-drag handle (a gold pin drawn at the tail apex)
    handleCv = CANVAS:CreateCanvas(40, 40)
    handleCv:FillCircle(20, 20, 16, 60, 40, 70, 255)
    handleCv:FillCircle(20, 20, 12, 255, 206, 84, 255)
    handleCv:FillCircle(20, 20, 5, 90, 60, 40, 255)
    handleCv:Upload()
    -- resize grip (a blue rounded square drawn at the bottom-right corner)
    sizeCv = CANVAS:CreateCanvas(36, 36)
    sizeCv:FillRect(2, 2, 32, 32, 60, 40, 70, 255)
    sizeCv:FillRect(5, 5, 26, 26, 90, 200, 230, 255)
    sizeCv:Upload()
    updateHandle(); updateSizeHandle()

    setPage(1)
end

function activate() page = 1; setPage(1); lastTs = 0 end
function deactivate() end
function afterSongEnum() end
function onDestroy() end

function update(ts)
    local r = ui:update(ts)
    -- quick page / theme hotkeys — ignored while typing in a textbox (so letters that double as
    -- hotkeys go into the field instead of triggering a page/theme change → no softlock).
    if not ui:isCapturing() then
        if INPUT:KeyboardPressed("Q") then setPage(1) end
        if INPUT:KeyboardPressed("E") then setPage(2) end
        if INPUT:KeyboardPressed("T") then applyTheme((themeIdx % #THEMES) + 1) end
    end
    -- Bubble Lab drag (page 2): tail pin (retarget tail), blue corner grip (resize), or body (move).
    -- Tail/resize re-bake in DRAFT quality during the drag (fast); a crisp full bake happens on release.
    if page == 2 and labBubble and not ui:isCapturing() then
        local mx, my = INPUT:GetMouseXY()
        if INPUT:MousePressed("Left") then
            if (mx - sizeHandle.x) ^ 2 + (my - sizeHandle.y) ^ 2 < 28 * 28 then dragMode = "resize"
            elseif (mx - tailHandle.x) ^ 2 + (my - tailHandle.y) ^ 2 < 26 * 26 then dragMode = "tail"
            elseif mx >= labBubble.x and mx <= labBubble.x + labBubble.w and my >= labBubble.y and my <= labBubble.y + labBubble.h then
                dragMode, grabDX, grabDY = "body", mx - labBubble.x, my - labBubble.y
            else dragMode = nil end
        end
        if not INPUT:MousePressing("Left") then
            if dragMode == "tail" or dragMode == "resize" then labBubble:setDraft(false) end  -- crisp final bake
            dragMode = nil
        end
        if dragMode == "body" then
            labBubble.x, labBubble.y = mx - grabDX, my - grabDY; updateHandle(); updateSizeHandle()
        elseif dragMode == "tail" then
            local side, pos, size = tailFromHandle(labBubble, mx, my)
            if side ~= labBubble.tail or math.abs(pos - labBubble.tailPos) > 0.02 or math.abs(size - labBubble.tailSize) > 2 then
                labBubble:setTail(side, pos, size, true)   -- draft (responsive)
            end
            updateHandle()
        elseif dragMode == "resize" then
            local nw, nh = mx - labBubble.x, my - labBubble.y
            if math.abs(nw - labBubble.w) > 2 or math.abs(nh - labBubble.h) > 2 then
                labBubble:setSize(nw, nh, true)   -- draft (responsive)
            end
            updateHandle(); updateSizeHandle()
        end
    end
    if r == "cancel" then return Exit("stage", "_title") end
end

function draw()
    ui:draw()
    if page == 2 then
        if sizeCv then sizeCv:DrawAtAnchor(math.floor(sizeHandle.x), math.floor(sizeHandle.y), "center") end
        if handleCv then handleCv:DrawAtAnchor(math.floor(tailHandle.x), math.floor(tailHandle.y), "center") end
    end
    -- footer hint (glyph-atlas draw, theme text colour, correct spacing, bounded glyph cache)
    ui:drawText(22, "Esc: back    Q/E: pages    T: cycle theme    (page 2: drag bubble / tail pin / corner grip)",
        60, 1006, ui.theme.colors.text)
end
