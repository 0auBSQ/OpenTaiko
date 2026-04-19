-- DanPlate ROActivity
-- Individual per-tick textures are pre-split in Textures/:
--   Textures/back_{i}.png    – background frame i  (drawn white)
--   Textures/plates_{i}.png  – foreground frame i  (tinted by DANTICKCOLOR)
-- danTick values beyond frame_count wrap via modulo.
--
-- draw(x, y, opacity, danTick, r, g, b, titleText)
--   x, y       – centre position (pixels)
--   opacity    – 0–255
--   danTick    – 0-based frame index
--   r, g, b    – 0–255 foreground tint colour (DANTICKCOLOR)
--   titleText  – dan title drawn vertically on the plate

local TEXTURES_DIR = "Textures/"

-- Config values (loaded in onStart)
local cfg_frame_count     = 6
local cfg_title_font_size = 48
local cfg_title_max_h     = 160
local cfg_title_offset_x  = 0
local cfg_title_offset_y  = 0

-- Per-tick texture arrays (1-indexed)
local back_frames  = {}   -- back_frames[i]  → LuaTexture for background tick i-1
local plate_frames = {}   -- plate_frames[i] → LuaTexture for foreground tick i-1

local font_title = nil

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
    end

    font_title = TEXT:Create(cfg_title_font_size, "regular")
end

function onDestroy()
    for i = 1, #back_frames do
        if back_frames[i] ~= nil then back_frames[i]:Dispose() end
    end
    for i = 1, #plate_frames do
        if plate_frames[i] ~= nil then plate_frames[i]:Dispose() end
    end
    back_frames  = {}
    plate_frames = {}
    if font_title ~= nil then font_title:Dispose() ; font_title = nil end
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

    -- Foreground (tinted by DANTICKCOLOR)
    if tx_plate ~= nil and tx_plate.Loaded then
        tx_plate:SetOpacity(op)
        tx_plate:SetColor(
            math.max(0.0, math.min(1.0, (r or 255) / 255.0)),
            math.max(0.0, math.min(1.0, (g or 255) / 255.0)),
            math.max(0.0, math.min(1.0, (b or 255) / 255.0))
        )
        tx_plate:SetScale(1.0, 1.0)
        tx_plate:DrawAtAnchor(x, y, "center")
    end

    -- Vertical title text
    if titleText ~= nil and titleText ~= "" then
        drawTateTitle(x + cfg_title_offset_x, y + cfg_title_offset_y, titleText, op)
    end
end
