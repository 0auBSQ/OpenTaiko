-- DanPlate ROActivity
-- Individual per-tick textures are pre-split in Textures/:
--   Textures/back_{i}.png    – background frame i  (drawn white)
--   Textures/plates_{i}.png  – foreground frame i  (tinted by DANTICKCOLOR)
--   Textures/plates_{i}_ramp.png – foreground frame i as a diagonal grey ramp, coloured through a
--                              rainbow gradient map on a rainbow plate
--   Textures/sparkle.png     – glitter over a rainbow plate
-- danTick values beyond frame_count wrap via modulo.
--
-- While the SHARED string "danplate_style" is "rainbow" (the pagoda sets it for its roof floors),
-- the foreground runs a diagonal rainbow instead of DANTICKCOLOR, with glitter over its frame.
--
-- draw(x, y, opacity, danTick, r, g, b, titleText)
--   x, y       – centre position (pixels)
--   opacity    – 0–255
--   danTick    – 0-based frame index
--   r, g, b    – 0–255 foreground tint colour (DANTICKCOLOR)
--   titleText  – dan title drawn vertically on the plate

local Rainbow = require("Rainbow")

local TEXTURES_DIR = "Textures/"

local STYLE_KEY = "danplate_style"
local RAINBOW_CYCLES = 1.5       -- hue cycles along the plate's diagonal
local RAINBOW_SPEED  = 0.35      -- cycles per second
local RAINBOW_STEPS  = 48        -- gradient maps per cycle (the animation's phase steps)
local RAINBOW_STOPS  = 18        -- colour stops per gradient map
local RAINBOW_SAT    = 0.7       -- a little pastel
local GLITTER   = { rate = 4, size = 0.8, max = 7, blend = "Add" }
local GLITTER_OPACITY = 1
-- the plate's frame (shared by frames 0-4), x, y, w, h from its top-left: top roller, bottom roller,
-- left post, right post
local GLITTER_BOXES = { { 50, 46, 190, 42 }, { 50, 482, 190, 44 }, { 63, 92, 20, 382 }, { 212, 92, 20, 382 } }

-- Config values (loaded in onStart)
local cfg_frame_count     = 6
local cfg_title_font_size = 48
local cfg_title_max_h     = 160
local cfg_title_offset_x  = 0
local cfg_title_offset_y  = 0

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

-- ─────────────────────────────────────────────────────────────────────────────
-- Helpers
-- ─────────────────────────────────────────────────────────────────────────────

--- Draw the dan title vertically, centred on (cx, cy).
--- Uses GetVerticalText which renders white text with a black outline baked in,
--- capped to cfg_title_max_h pixels tall.
local function drawTateTitle(cx, cy, text, op)
    if text == nil or text == "" then return end
    if font_title == nil then return end

    local t = font_title:GetVerticalText(text, true, cfg_title_max_h)
    if t == nil or not t.Loaded then return end

    t:SetOpacity(op)
    t:SetColor(1.0, 1.0, 1.0)
    t:DrawAtAnchor(cx, cy, "center")
end

--- Seconds since the previous rainbow draw (0 for a second draw in the same frame), capped so a
--- plate shown again after a pause carries on where it was.
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

--- The gradient maps are made the first time a rainbow plate shows, during play rather than at boot,
--- the way the stages make theirs.
local function ensureRainbowMaps()
    if #rainbow_maps > 0 then return end
    for k = 0, RAINBOW_STEPS - 1 do
        rainbow_maps[k + 1] = GRADIENT:Create(rainbowStops(k / RAINBOW_STEPS), 1.0)
    end
end

--- The foreground drawn black (its border stays), then its fill ramp in a running diagonal rainbow over
--- it, centred on (x, y), with glitter over its frame.
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

-- ─────────────────────────────────────────────────────────────────────────────
-- ROActivity lifecycle
-- ─────────────────────────────────────────────────────────────────────────────

function onStart()
    local config = JSONLOADER:LoadJson("Config.json")
    cfg_frame_count     = JSONLOADER:ExtractNumber(config["frame_count"])      or 6
    cfg_title_font_size = JSONLOADER:ExtractNumber(config["title_font_size"])  or 48
    cfg_title_max_h     = JSONLOADER:ExtractNumber(config["title_max_height"]) or 160
    cfg_title_offset_x  = JSONLOADER:ExtractNumber(config["title_offset_x"])   or 0
    cfg_title_offset_y  = JSONLOADER:ExtractNumber(config["title_offset_y"])   or 0

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

-- ─────────────────────────────────────────────────────────────────────────────
-- draw(x, y, opacity, danTick, r, g, b, titleText)
-- ─────────────────────────────────────────────────────────────────────────────

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

    -- Foreground (tinted by DANTICKCOLOR, or the rainbow while the style asks for it)
    if tx_plate ~= nil and tx_plate.Loaded then
        local tx_ramp = ramp_frames[tick + 1]
        if tx_ramp ~= nil and tx_ramp.Loaded and SHARED:GetSharedString(STYLE_KEY) == "rainbow" then
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
