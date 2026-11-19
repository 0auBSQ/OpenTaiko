---@diagnostic disable: undefined-global, undefined-field, lowercase-global
-- demo_overall: five debug demos in one stage, keys 1-5 switch
-- each lives in <name>/demo.lua; leaving goes back to My Room

-- drawn in 1920x1080, rescaled for other skin resolutions
STAGES:SetResolution(1920, 1080)

local I18N = require("Locale.i18n")
local T = I18N.texts("demo_overall")   -- lang/<code>/demo_overall.json

local DEMOS = {
    { id = "theme_settings", mod = require("theme_settings.demo") },
    { id = "gradient",       mod = require("gradient.demo") },
    { id = "database",       mod = require("database.demo") },
    { id = "dan_plate",      mod = require("dan_plate.demo") },
    { id = "first_demo",     mod = require("first_demo.demo") },
}

-- legend time on entry and after each switch (s)
local LEGEND_TIME, LEGEND_FADE = 3.5, 0.6
local LEGEND_Y, LEGEND_STEP, LEGEND_W = 990, 360, 340

local current = nil
local legendLeft = 0
local lastTs = nil
local font, white, yellow = nil, nil, nil
local labels = {}

local function switchTo(i)
    if i ~= current then
        if current ~= nil then DEMOS[current].mod.deactivate() end
        current = i
        DEMOS[current].mod.activate()
    end
    legendLeft = LEGEND_TIME
end

local function numberPressed()
    for i = 1, #DEMOS do
        if INPUT:KeyboardPressed("D" .. i) or INPUT:KeyboardPressed("NumberPad" .. i) then return i end
    end
    return nil
end

local function drawLegend()
    if legendLeft <= 0 or font == nil then return end
    local alpha = math.min(1, legendLeft / LEGEND_FADE)
    local x0 = 960 - (#DEMOS - 1) * LEGEND_STEP / 2
    for i = 1, #DEMOS do
        font:Draw(labels[i], x0 + (i - 1) * LEGEND_STEP, LEGEND_Y, i == current and yellow or white, nil, alpha, 1,
                  LEGEND_W, "center")
    end
end

function onStart()
    font = TEXT:CreateGlyphCached(26)
    white = COLOR:CreateColorFromRGBA(255, 255, 255, 255)
    yellow = COLOR:CreateColorFromRGBA(242, 207, 1, 255)
    for _, d in ipairs(DEMOS) do d.mod.onStart() end
end

function activate()
    I18N.detect()
    for i, d in ipairs(DEMOS) do labels[i] = i .. "  " .. T:tr("demo_" .. d.id) end
    lastTs = nil
    current = nil
    switchTo(1)
end

function update(ts)
    local dt = 0
    if lastTs ~= nil and ts ~= nil then dt = math.max(0, (ts - lastTs) / 1000) end
    lastTs = ts
    if legendLeft > 0 then legendLeft = math.max(0, legendLeft - dt) end

    -- number keys may also be drum binds (3P-5P), so a switch frame skips the demo update
    local n = numberPressed()
    if n ~= nil then switchTo(n); return end

    if current ~= nil then return DEMOS[current].mod.update(ts) end
end

function draw()
    if current ~= nil then DEMOS[current].mod.draw() end
    drawLegend()
end

function deactivate()
    if current ~= nil then DEMOS[current].mod.deactivate() end
    current = nil
end

function onDestroy()
    for _, d in ipairs(DEMOS) do d.mod.onDestroy() end
    if font ~= nil then font:Dispose(); font = nil end
end
