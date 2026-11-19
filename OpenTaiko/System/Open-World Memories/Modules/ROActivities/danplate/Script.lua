-- DanPlate ROActivity.

local Rainbow = require("Rainbow")
local PlayStrings = require("PlayStrings")

-- sizes, offsets and colours: Config/layout.json; the rainbow's and the glitter's pace: Config/animation.json
local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")

local TEXTURES_DIR = "Textures/"

local STYLE_KEY = "danplate_style"
local RAINBOW_CYCLES = Layout.rainbow.cycles         -- hue cycles along the plate's diagonal
local RAINBOW_SPEED  = Animation.rainbow.speed       -- cycles per second
local RAINBOW_STEPS  = Animation.rainbow.steps       -- gradient maps per cycle (the animation's phase steps)
local RAINBOW_STOPS  = Layout.rainbow.stops          -- colour stops per gradient map
local RAINBOW_SAT    = Layout.rainbow.saturation
local GLITTER   = { rate = Animation.glitter.rate, size = Layout.glitter.size, max = Layout.glitter.max,
    blend = Layout.glitter.blend }
local GLITTER_OPACITY = Layout.glitter.opacity
-- the plate's frame (shared by frames 0-4), x, y, w, h from its top-left: top roller, bottom roller,
-- left post, right post
local GLITTER_BOXES = Layout.glitter.boxes

local cfg_frame_count     = Layout.frame_count
local cfg_title_font_size = Layout.title_font_size
local cfg_title_max_h     = Layout.title_max_height
local cfg_title_offset_x  = Layout.title_offset_x
local cfg_title_offset_y  = Layout.title_offset_y

-- Per-tick texture arrays (1-indexed)
local back_frames  = {}   -- back_frames[i]  → LuaTexture for background tick i-1
local plate_frames = {}   -- plate_frames[i] → LuaTexture for foreground tick i-1
local ramp_frames  = {}   -- ramp_frames[i]  → the diagonal ramp of foreground tick i-1
local rainbow_maps = {}   -- one fully saturated rainbow gradient map per phase step (made on first use)

local font_title = nil

local sparkle   = nil
local glitter   = {}    -- one sparkle state per glitter box
local rainbow_t = 0
local last_ms   = nil

-- ==== Helpers ====

--- dan title, vertical, centred on (cx, cy); GetVerticalText, capped to cfg_title_max_h px
local function drawTateTitle(cx, cy, text, op)
    if text == nil or text == "" then return end
    if font_title == nil then return end

    local t = font_title:GetVerticalText(text, true, cfg_title_max_h)
    if t == nil or not t.Loaded then return end

    t:SetOpacity(op)
    t:SetColor(1.0, 1.0, 1.0)
    t:DrawAtAnchor(cx, cy, "center")
end

--- seconds since the previous rainbow draw (0 within the same frame), capped (resumes after a pause)
local function rainbowStep()
    local now = fps.ms
    local dt = 0
    if last_ms ~= nil then dt = math.max(0, math.min(0.1, (now - last_ms) / 1000)) end
    last_ms = now
    rainbow_t = rainbow_t + dt
    return dt
end

--- Gradient stops of the rainbow shifted by phase (0..1): hue runs RAINBOW_CYCLES times along the ramp.
local function rainbowStops(phase)
    local stops = {}
    for j = 0, RAINBOW_STOPS do
        local pos = j / RAINBOW_STOPS
        local r, g, b = Rainbow.hsv(pos * RAINBOW_CYCLES + phase, RAINBOW_SAT, 1)
        stops[#stops + 1] = { pos, math.floor(r * 255 + 0.5), math.floor(g * 255 + 0.5), math.floor(b * 255 + 0.5) }
    end
    return stops
end

--- gradient maps made on the first rainbow plate (during play, not at boot)
local function ensureRainbowMaps()
    if #rainbow_maps > 0 then return end
    for k = 0, RAINBOW_STEPS - 1 do
        rainbow_maps[k + 1] = GRADIENT:Create(rainbowStops(k / RAINBOW_STEPS), 1.0)
    end
end

--- foreground drawn black (border kept), then the fill ramp through the rainbow map; centred on (x, y)
--- glitter over the frame
local function drawRainbowPlate(plate, tex, x, y, op)
    local dt = rainbowStep()
    ensureRainbowMaps()
    local step = math.floor((rainbow_t * RAINBOW_SPEED % 1) * RAINBOW_STEPS) % RAINBOW_STEPS
    plate:SetOpacity(op)
    plate:SetColor(0.0, 0.0, 0.0)
    plate:SetScale(1.0, 1.0)
    plate:DrawAtAnchor(x, y, "center")
    plate:SetColor(1.0, 1.0, 1.0)
    tex:SetOpacity(op)
    tex:SetColor(1.0, 1.0, 1.0)
    tex:SetScale(1.0, 1.0)
    GRADIENT:SetActive(rainbow_maps[step + 1])
    tex:DrawAtAnchor(x, y, "center")
    GRADIENT:ClearActive()

    local left = x - math.floor(tex.Width / 2)
    local top  = y - math.floor(tex.Height / 2)
    GLITTER.opacity = GLITTER_OPACITY * op
    for i, b in ipairs(GLITTER_BOXES) do
        Rainbow.glitter(glitter[i], dt, left + b[1], top + b[2], b[3], b[4], sparkle, GLITTER)
    end
end

-- ==== ROActivity lifecycle ====

function onStart()
    -- Textures/back_{i}.png: background frame i (drawn white); plates_{i}.png: foreground frame i (DANTICKCOLOR)
    -- plates_{i}_ramp.png: frame i as a diagonal grey ramp (rainbow plate); danTick wraps by frame_count
    -- Load one texture per tick for both layers
    for i = 0, cfg_frame_count - 1 do
        back_frames[i + 1]  = TEXTURE:CreateTexture(TEXTURES_DIR .. "back_"   .. i .. ".png")
        plate_frames[i + 1] = TEXTURE:CreateTexture(TEXTURES_DIR .. "plates_" .. i .. ".png")
        ramp_frames[i + 1]  = TEXTURE:CreateTexture(TEXTURES_DIR .. "plates_" .. i .. "_ramp.png")
    end

    font_title = TEXT:Create(cfg_title_font_size, "regular")

    sparkle = TEXTURE:CreateTexture(TEXTURES_DIR .. "sparkle.png")
    for i = 1, #GLITTER_BOXES do glitter[i] = {} end
end

function onDestroy()
    for i = 1, #back_frames do
        if back_frames[i] ~= nil then back_frames[i]:Dispose() end
    end
    for i = 1, #plate_frames do
        if plate_frames[i] ~= nil then plate_frames[i]:Dispose() end
    end
    for _, t in pairs(ramp_frames) do t:Dispose() end
    for _, gm in ipairs(rainbow_maps) do gm:Dispose() end
    back_frames  = {}
    plate_frames = {}
    ramp_frames  = {}
    rainbow_maps = {}
    if font_title ~= nil then font_title:Dispose() ; font_title = nil end
    if sparkle ~= nil then sparkle:Dispose() ; sparkle = nil end
    glitter = {}
end

-- ==== Draw ====

-- x, y: centre px; opacity 0-255; danTick: 0-based frame; r, g, b: 0-255 tint (DANTICKCOLOR)
-- titleText: dan title drawn vertically
function draw(x, y, opacity, danTick, r, g, b, titleText)
    if x == nil then return end

    local op   = math.max(0.0, math.min(1.0, (opacity or 255) / 255.0))
    local tick = math.floor(danTick or 0) % cfg_frame_count

    local tx_back  = back_frames[tick + 1]
    local tx_plate = plate_frames[tick + 1]

    -- Background (always white)
    if tx_back ~= nil and tx_back.Loaded then
        tx_back:SetOpacity(op)
        tx_back:SetColor(1.0, 1.0, 1.0)
        tx_back:SetScale(1.0, 1.0)
        tx_back:DrawAtAnchor(x, y, "center")
    end

    -- Foreground: DANTICKCOLOR tint, or the rainbow while SHARED "danplate_style" = "rainbow" (Lib/PlayStrings.lua)
    if tx_plate ~= nil and tx_plate.Loaded then
        local tx_ramp = ramp_frames[tick + 1]
        if tx_ramp ~= nil and tx_ramp.Loaded and PlayStrings.forTitle(STYLE_KEY, titleText) == "rainbow" then
            drawRainbowPlate(tx_plate, tx_ramp, x, y, op)
        else
            tx_plate:SetOpacity(op)
            tx_plate:SetColor(
                math.max(0.0, math.min(1.0, (r or 255) / 255.0)),
                math.max(0.0, math.min(1.0, (g or 255) / 255.0)),
                math.max(0.0, math.min(1.0, (b or 255) / 255.0))
            )
            tx_plate:SetScale(1.0, 1.0)
            tx_plate:DrawAtAnchor(x, y, "center")
        end
    end

    -- Vertical title text
    if titleText ~= nil and titleText ~= "" then
        drawTateTitle(x + cfg_title_offset_x, y + cfg_title_offset_y, titleText, op)
    end
end
