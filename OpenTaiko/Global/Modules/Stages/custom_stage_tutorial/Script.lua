---@diagnostic disable: undefined-global, undefined-field, need-check-nil
-- custom_stage_tutorial — what to know to make a custom stage and share it: a list of topics on the left,
-- the selected topic's pages (a heading, a picture and a text) on the right. A custom stage itself: it
-- declares the "CStage" namespace below, so the skin's Custom Stages menu lists it, and it leaves for the
-- stage named by custom_stage_return.
-- Keys: Up/Down a topic, Left/Right a page, Decide the next page, Cancel back. The mouse clicks topics
-- and the Previous, Next and Back buttons.

-- drawn in 1920x1080: a skin of another resolution rescales it
STAGES:SetResolution(1920, 1080)

local I18N     = require("Locale.i18n")
local NavInput = require("Input.NavInput")
local CS       = require("Stages.CustomStages")
local Pick     = require("UI.Pick")

local T = I18N.texts("tutorial")   -- lang/<code>/tutorial.json

CS.declare{
    title       = CS.allLanguages("tutorial", "stage_title"),
    description = CS.allLanguages("tutorial", "stage_desc"),
}

-- each topic's texts are <id>_title, <id>_<n>_head and <id>_<n>_text; each page has its picture, or false
-- for a page of text only. A picture with words in it ships one file per language (namespace_ja.png...).
local TOPICS = {
    { id = "place",     pages = { "place.png" } },
    { id = "namespace", pages = { "namespace.png" } },
    { id = "inside",    pages = { "inside.png" } },
    { id = "docs",      pages = { false } },
}

local NAV = NavInput.p[0]   -- the keyboard and the pad, not the drums

local LEFT_X, LEFT_W = 50, 540
local RIGHT_X, RIGHT_W = 620, 1250
local PANEL_Y, PANEL_H = 70, 900
local IMG_W, IMG_H = 1120, 440
local HEAD_SIZE, TEXT_SIZE, LINE = 34, 26, 36
local TEXT_LINES = 5          -- under a picture
local TEXT_ONLY_LINES = 17    -- on a page without one

local ui, menu, rightPanel = nil, nil, nil
local prevB, nextB
local topic, page = 1, 1
local images = {}
local leaving = false
local back, backTransition = "_title", nil

local function image(file)
    if not file then return nil end
    local tex = images[file]
    if tex == nil then
        tex = TEXTURE:CreateTexture(I18N.localizedPath("Textures/" .. file))
        images[file] = tex
    end
    return tex
end

local function freeImages()
    for _, tex in pairs(images) do pcall(function() tex:Dispose() end) end
    images = {}
end

local function showPage()
    local t = TOPICS[topic]
    rightPanel.title = T:tr(t.id .. "_title")
    rightPanel:restyle()
    prevB:setEnabled(topic > 1 or page > 1)
    nextB:setEnabled(topic < #TOPICS or page < #t.pages)
    image(t.pages[page])
    -- the next picture starts loading now, so it is there when the page turns
    local nt = t.pages[page + 1]
    if nt == nil and TOPICS[topic + 1] then nt = TOPICS[topic + 1].pages[1] end
    image(nt)
end

-- d = 1 or -1: one page on, past the end of a topic into the next one
local function turn(d)
    local t = TOPICS[topic]
    local p, k = page + d, topic
    if p > #t.pages then
        if k >= #TOPICS then return end
        k, p = k + 1, 1
    elseif p < 1 then
        if k <= 1 then return end
        k = k - 1
        p = #TOPICS[k].pages
    end
    topic, page = k, p
    if menu.selected ~= topic then menu:setSelected(topic, false) end
    ui:playSfx("move")
    showPage()
end

local function build()
    local lib = Pick()
    ui = lib.new{ bg = true, navPlayer = 0 }
    local left = ui:panel{ x = LEFT_X, y = PANEL_Y, w = LEFT_W, h = PANEL_H, title = T:tr("topics") }
    local cx, cy, cw = left:content()
    local items = {}
    for i, t in ipairs(TOPICS) do items[i] = { text = T:tr(t.id .. "_title"), value = i } end
    menu = ui:menu{ x = cx, y = cy + 24, w = cw, h = #items * 96, rowHeight = 96, items = items, selected = topic,
                    hoverSelects = false,
                    onChange = function(i) topic, page = i, 1; showPage() end,
                    -- Decide turns the page; the mouse turns pages with the buttons
                    onSelect = function() if not ui:mouseActive() then turn(1) end return true end }
    ui:button{ text = T:tr("back"), x = cx, y = PANEL_Y + PANEL_H - 28 - 76, w = cw, h = 76,
               mouseOnly = true, onClick = function() leaving = true end }
    rightPanel = ui:panel{ x = RIGHT_X, y = PANEL_Y, w = RIGHT_W, h = PANEL_H, title = T:tr(TOPICS[topic].id .. "_title") }
    local by = PANEL_Y + PANEL_H - 28 - 76
    prevB = ui:button{ text = T:tr("prev"), x = RIGHT_X + 40, y = by, w = 260, h = 76, mouseOnly = true,
                       onClick = function() turn(-1) end }
    nextB = ui:button{ text = T:tr("next"), x = RIGHT_X + RIGHT_W - 40 - 260, y = by, w = 260, h = 76, accent = true,
                       mouseOnly = true, onClick = function() turn(1) end }
    ui:focus(menu)
    showPage()
end

function onStart() end

function activate()
    I18N.detect()
    back, backTransition = CS.takeReturn()
    leaving = false
    topic, page = 1, 1
    build()
end

function deactivate()
    if ui then ui:disposeWidgets() end
    ui, menu, rightPanel = nil, nil, nil
    freeImages()
end

function update(ts)
    if ui == nil then return end
    local r = ui:update(ts)
    if r == "cancel" or leaving then return Exit("stage", back, backTransition) end
    if NAV.rightKeyboard() then turn(1) elseif NAV.leftKeyboard() then turn(-1) end
end

-- the text of a page, wrapped to the panel; a line break in the text starts a new paragraph
local function textLines(s, width)
    local out = {}
    for para in (s .. "\n"):gmatch("(.-)\n") do
        for _, l in ipairs(ui:wrapLines(TEXT_SIZE, para, width)) do out[#out + 1] = l end
    end
    return out
end

function draw()
    if ui == nil then return end
    ui:draw()
    local t = TOPICS[topic]
    local c = ui.theme.colors
    local bg = ui.theme.textOutline == false and { 255, 255, 255, 0 } or { 255, 255, 255, 150 }
    local cx = RIGHT_X + RIGHT_W / 2
    local top = PANEL_Y + 64
    local key = t.id .. "_" .. page
    ui:drawTextEx(HEAD_SIZE, T:tr(key .. "_head"), cx, top, c.text, bg, 1, 1, RIGHT_W - 80, "top")
    local tex = image(t.pages[page])
    local iy = top + 64
    local ty, maxLines = iy + 22, TEXT_ONLY_LINES
    if tex ~= nil then
        if tex.Width > 0 then
            tex:SetOpacity(1)
            tex:Draw(math.floor(cx - IMG_W / 2), iy)
        end
        ty, maxLines = iy + IMG_H + 22, TEXT_LINES
    end
    local lines = textLines(T:tr(key .. "_text"), IMG_W)
    for i = 1, math.min(#lines, maxLines) do
        ui:drawTextEx(TEXT_SIZE, lines[i], math.floor(cx - IMG_W / 2), ty + (i - 1) * LINE, c.text, bg, 1, 1, IMG_W, "topleft")
    end
    local by = PANEL_Y + PANEL_H - 28 - 76
    if #t.pages > 1 then
        ui:drawTextEx(TEXT_SIZE, string.format(T:tr("page"), page, #t.pages), cx, by + 38 + ui:textNudge(TEXT_SIZE),
                      c.text, bg, 1, 1, 400, "center")
    end
    ui:drawTextEx(22, T:tr("hint"), 960, PANEL_Y + PANEL_H + 22, c.text, bg, 0.85, 1, 1800, "top")
end

function onDestroy()
    freeImages()
end
