---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local
-- song_enum ROActivity
-- ------------------------------------------------------------------------------------------------------
-- The song-enumeration status overlay: a PopUI panel in the top-left showing the status message, a
-- progress bar, and the loaded/total count (all glyph-rendered). The panel AUTO-SIZES to the message so
-- the text always fits, undistorted. Driven by C# (CActEnumSongs): draw(bCommand, done, total) each frame.

local PopUI = require("PopUI")

-- same palette as the settings menu (config_ui "ConfigCalm") so the box matches; opaque faces (alpha 255)
local THEME = {
    name = "ConfigCalm",
    colors = {
        surface  = { 250, 252, 255, 255 }, -- panel face (top)
        surface2 = { 230, 236, 244, 255 }, -- panel face (bottom)
        primary  = { 96, 132, 180, 255 },  -- progress fill
        primary2 = { 72, 108, 156, 255 },
        outline  = { 70, 84, 104, 255 },
        text     = { 44, 56, 74, 255 },
        shadow   = { 40, 52, 72, 70 },
        gloss    = { 255, 255, 255, 90 },
        track    = { 214, 222, 232, 255 }, -- progress track
    },
    radius = 22,
    outlineWidth = 4,
    gloss = true,
    shadow = { dx = 0, dy = 5, layers = 4, grow = 3 },
}

local ui, panel
local phase = 0
local builtRaw          -- the LANG string the current panel was sized for (rebuild on language change)
local msgLines = {}     -- trimmed, non-empty message lines
local msgBlockH = 0     -- total height of the message block
local msgMaxW = 0       -- glyph maxWidth to pass (= content width + glyph box padding; squish only past the cap)

-- layout (skin 1920x1080 space)
local POP_X, POP_Y = 32, 28
local PAD = 26                    -- inner margin between the panel edge and its content
local MSG_SIZE, CNT_SIZE = 24, 18
local LINE_H = MSG_SIZE + 10      -- airy line spacing for the message
local BAR_H = 18
local ROW_GAP = 20               -- space between the message block and the bar row
local COL_GAP = 16               -- space between the bar and the count
local BAR_ROW_H = math.max(BAR_H, CNT_SIZE + 4)
local MIN_CONTENT_W, MAX_CONTENT_W = 320, 700
local GLYPH_BOX_PAD = 50   -- the ink+pad the glyph box carries (25px per side): factored into the squish cap

-- split a raw LANG string into trimmed, non-empty lines (the translations embed \n + leading-space indent
-- meant for the old fixed C# layout; strip that so the popup lays it out cleanly)
local function normalizeLines(raw)
    local out = {}
    for rawLine in (tostring(raw) .. "\n"):gmatch("(.-)\n") do
        local line = rawLine:gsub("^%s+", ""):gsub("%s+$", "")
        if #line > 0 then out[#out + 1] = line end
    end
    if #out == 0 then out[1] = "" end
    return out
end

-- (re)build the auto-sized panel for the given status string
local function rebuild(raw)
    ui:disposeWidgets()
    ui:clear()

    msgLines = normalizeLines(raw)
    local widest = 0
    for _, l in ipairs(msgLines) do widest = math.max(widest, ui:measureText(MSG_SIZE, l)) end

    local contentW = math.min(MAX_CONTENT_W, math.max(MIN_CONTENT_W, math.ceil(widest) + 8))
    msgMaxW = contentW + GLYPH_BOX_PAD           -- lines squish only if their ink exceeds contentW (the cap)
    msgBlockH = #msgLines * LINE_H

    local popW = contentW + 2 * PAD
    local popH = msgBlockH + ROW_GAP + BAR_ROW_H + 2 * PAD
    panel = ui:panel { x = POP_X, y = POP_Y, w = popW, h = popH, pad = PAD }
    builtRaw = raw
end

function onStart()
    ui = PopUI.new { theme = THEME, bg = false }
end

function activate()
    phase = 0
end

function deactivate()
end

function draw(bCommand, done, total)
    if ui == nil then return end
    local raw = LANG:GetString("SETTINGS_SYSTEM_RELOADSONG_STATUS")
    if raw ~= builtRaw then rebuild(raw) end

    done  = math.floor(done or 0)
    total = math.floor(total or 0)
    phase = phase + 0.05
    local pulse = 0.75 + 0.25 * math.abs(math.sin(phase))

    ui:draw()   -- the PopUI panel (rounded/shadowed box)

    local cx, cy, cw = panel:content()
    local C = THEME.colors

    -- status message, one centred line per row (no squish for normal-length text)
    for i, line in ipairs(msgLines) do
        ui:drawTextEx(MSG_SIZE, line, cx + cw / 2, cy + (i - 0.5) * LINE_H + 2,
            C.text, nil, pulse, 1, msgMaxW, "center")
    end

    -- bar + count row, below the message block
    local barRowCy = cy + msgBlockH + ROW_GAP + BAR_ROW_H / 2
    local barY = barRowCy - BAR_H / 2

    local countStr = (total > 0) and (done .. " / " .. total) or tostring(done)
    local reserveStr = (total > 0) and (total .. " / " .. total) or "0000000"
    local reserveW = ui:measureText(CNT_SIZE, reserveStr)
    local liveW = ui:measureText(CNT_SIZE, countStr)
    local barW = math.max(40, cw - reserveW - COL_GAP)

    ui:rect(cx, barY, barW, BAR_H, C.track[1], C.track[2], C.track[3], 255)
    if total > 0 then
        ui:rect(cx + 2, barY + 2, (barW - 4) * math.max(0, math.min(1, done / total)), BAR_H - 4,
            C.primary[1], C.primary[2], C.primary[3], 255)
    else
        -- total not known yet (pre-count still running): indeterminate marquee
        local seg, fw = 90, barW - 4
        local mx = (phase * 90) % (fw + seg) - seg
        local x0 = (cx + 2) + math.max(0, mx)
        local x1 = (cx + 2) + math.min(fw, mx + seg)
        ui:rect(x0, barY + 2, x1 - x0, BAR_H - 4, C.primary[1], C.primary[2], C.primary[3], 255)
    end

    -- count, flush-right of the row, vertically centred on the bar (never overlaps it)
    ui:drawTextEx(CNT_SIZE, countStr, cx + cw - liveW / 2, barRowCy + 2, C.text, nil, 1, 1, 0, "center")
end
