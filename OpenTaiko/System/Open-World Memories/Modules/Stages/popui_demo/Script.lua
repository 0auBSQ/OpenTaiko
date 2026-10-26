---@diagnostic disable: undefined-global, lowercase-global, need-check-nil, undefined-field
-- popui_demo — a showcase for the PopUI library (Lib/PopUI). Two pages of widgets + a live theme
-- switcher that re-skins everything, driven by mouse OR keyboard/gamepad. Esc returns to the title.

local PopUI = require("PopUI")
-- positions, sizes and colours: Config/layout.json
local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local TOP, P1L, P2L, LAB = Layout.top_bar, Layout.page1, Layout.page2, Layout.bubble_lab

local ui
local page = 1
local page1, page2 = {}, {}
local clickCount = 0
local lastTs = 0

-- the library copies a theme with its metatable, so it gets plain tables, not the config's
local function plain(t)
    local o = {}
    for k, v in pairs(t) do o[k] = v end
    return o
end

-- a few original colour palettes to prove customization (no real product is referenced)
local THEMES = {
    { name = "Bubblegum", theme = {} },
    { name = "Aqua", theme = { colors = plain(Layout.themes.aqua) } },
    { name = "Sunset", theme = { colors = plain(Layout.themes.sunset) } },
    { name = "Mint", theme = { colors = plain(Layout.themes.mint) } },
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
    return side, clampn(pos, LAB.tail_pos_min, LAB.tail_pos_max), clampn(size, LAB.tail_size_min, LAB.tail_size_max)
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
    local B, CHIP = TOP.widgets_button, TOP.theme_chips
    ui:label{ text = "PopUI Showcase", x = TOP.title.x, y = TOP.title.y, size = TOP.title.size }
    ui:button{ text = "Widgets", x = B.x, y = B.y, w = B.w, h = B.h, accent = true, onClick = function() setPage(1) end }
    B = TOP.chat_button
    ui:button{ text = "Chat & Menu", x = B.x, y = B.y, w = B.w, h = B.h, onClick = function() setPage(2) end }
    local cx = CHIP.x
    for i, t in ipairs(THEMES) do
        ui:button{ text = t.name, x = cx, y = CHIP.y, w = CHIP.w, h = CHIP.h,
            style = (i == 1) and nil or t.theme,            -- chip previews its own palette
            onClick = function() applyTheme(i) end }
        cx = cx + CHIP.step
    end

    -- ── page 1: widgets ─────────────────────────────────────────────────────────
    local function P1(w) page1[#page1 + 1] = w; return w end
    local W = P1L.panel
    P1(ui:panel{ x = W.x, y = W.y, w = W.w, h = W.h, title = "Widgets" })
    W = P1L.greeting
    local greet = P1(ui:label{ text = "Hi there!", x = W.x, y = W.y, size = W.size })
    W = P1L.clicks
    local clicks = P1(ui:label{ text = "Clicked 0 times", x = W.x, y = W.y, size = W.size })
    W = P1L.press_button
    P1(ui:button{ text = "Press me!", x = W.x, y = W.y, w = W.w, h = W.h, accent = true, onClick = function(b)
        clickCount = clickCount + 1
        clicks:setText("Clicked " .. clickCount .. " times")
    end })
    W = P1L.disabled_button
    P1(ui:button{ text = "Disabled", x = W.x, y = W.y, w = W.w, h = W.h, onClick = function() end }).enabled = false
    P1(ui:toggle{ text = "Fullscreen", x = P1L.toggle.x, y = P1L.toggle.y, value = true, onChange = function(v) end })
    P1(ui:checkbox{ text = "Mute audio", x = P1L.checkbox.x, y = P1L.checkbox.y, value = false, onChange = function(v) end })
    W = P1L.volume_label
    P1(ui:label{ text = "Volume", x = W.x, y = W.y, size = W.size })
    W = P1L.slider
    P1(ui:slider{ x = W.x, y = W.y, w = W.w, min = 0, max = 100, step = 1, value = 70, onChange = function(v) end })
    W = P1L.name_label
    P1(ui:label{ text = "Your name", x = W.x, y = W.y, size = W.size })
    W = P1L.textbox
    P1(ui:textbox{ x = W.x, y = W.y, w = W.w, placeholder = "type here + Enter...", maxLen = 18,
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
    W = P2L.bubble
    local bubble = P2(ui:bubble{ x = W.x, y = W.y, w = W.w, h = W.h, tail = W.tail, tailPos = W.tail_pos,
        typewriter = true, text = COMMENTS[1] })
    labBubble = bubble
    local items = { "Sunrise Stroll", "Triplet Trouble", "Hyper Drive", "Marshmallow Hop", "Final Fury", "Secret Track" }
    W = P2L.menu
    P2(ui:menu{ x = W.x, y = W.y, w = W.w, h = W.h, items = items, selected = 1,
        onChange = function(i) bubble:setText(COMMENTS[i] or "...", true) end,
        onSelect = function(i, it) bubble:setText("Now playing: " .. it.text .. "!", true) end })
    W = P2L.help_panel
    P2(ui:panel{ x = W.x, y = W.y, w = W.w, h = W.h, title = "How to use" })
    W = P2L.help_text
    P2(ui:label{ text = "Mouse: hover + click. Keyboard/Pad: arrows move,\nDecide selects, Cancel/Esc backs out.",
        x = W.x, y = W.y, size = W.size, maxWidth = W.max_width })
    W = P2L.drag_text
    P2(ui:label{ text = "Drag the bubble, its gold tail pin, or the blue corner grip (resize).",
        x = W.x, y = W.y, size = W.size, maxWidth = W.max_width })
    -- show off the different bubble shapes
    local SHAPES = { "round", "oval", "cloud" }
    local shapeIdx = 1
    W = P2L.shape_button
    P2(ui:button{ text = "Bubble shape: round", x = W.x, y = W.y, w = W.w, h = W.h, accent = true, onClick = function(b)
        shapeIdx = shapeIdx % #SHAPES + 1
        bubble:setShape(SHAPES[shapeIdx])
        b:setText("Bubble shape: " .. SHAPES[shapeIdx])
    end })

    -- tail-drag handle (a gold pin drawn at the tail apex)
    local pin = LAB.tail_pin
    handleCv = CANVAS:CreateCanvas(pin.canvas_size, pin.canvas_size)
    local mid = pin.canvas_size / 2
    for _, ring in ipairs(pin.rings) do
        local c = ring.color
        handleCv:FillCircle(mid, mid, ring.radius, c[1], c[2], c[3], c[4])
    end
    handleCv:Upload()
    -- resize grip (a blue rounded square drawn at the bottom-right corner)
    local grip = LAB.size_grip
    sizeCv = CANVAS:CreateCanvas(grip.canvas_size, grip.canvas_size)
    for _, sq in ipairs(grip.squares) do
        local c = sq.color
        sizeCv:FillRect(sq.inset, sq.inset, sq.size, sq.size, c[1], c[2], c[3], c[4])
    end
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
            local gr, tr = LAB.size_grip.grab_radius, LAB.tail_pin.grab_radius
            if (mx - sizeHandle.x) ^ 2 + (my - sizeHandle.y) ^ 2 < gr * gr then dragMode = "resize"
            elseif (mx - tailHandle.x) ^ 2 + (my - tailHandle.y) ^ 2 < tr * tr then dragMode = "tail"
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
            if side ~= labBubble.tail or math.abs(pos - labBubble.tailPos) > LAB.rebake_pos_step or math.abs(size - labBubble.tailSize) > LAB.rebake_px_step then
                labBubble:setTail(side, pos, size, true)   -- draft (responsive)
            end
            updateHandle()
        elseif dragMode == "resize" then
            local nw, nh = mx - labBubble.x, my - labBubble.y
            if math.abs(nw - labBubble.w) > LAB.rebake_px_step or math.abs(nh - labBubble.h) > LAB.rebake_px_step then
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
    local F = Layout.footer
    ui:drawText(F.size, "Esc: back    Q/E: pages    T: cycle theme    (page 2: drag bubble / tail pin / corner grip)",
        F.x, F.y, ui.theme.colors.text)
end
