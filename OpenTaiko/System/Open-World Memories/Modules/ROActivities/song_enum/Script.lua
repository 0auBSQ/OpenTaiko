---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local
-- song_enum ROActivity: song enumeration status panel (PopUI), sized to the message
-- C# (CActEnumSongs) calls draw(bCommand, done, total) each frame

local PopUI = require("PopUI")

-- layout: Config/layout.json; pulse and marquee: Config/animation.json
local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")
local PANEL, MESSAGE, BAR, COUNT = Layout.panel, Layout.message, Layout.bar, Layout.count

-- settings menu palette (config_ui "ConfigCalm"), opaque faces
local THEME = {
    name = "ConfigCalm",
    colors = Config.plain(Layout.theme.colors),
    radius = Layout.theme.radius,
    outlineWidth = Layout.theme.outline_width,
    gloss = Layout.theme.gloss,
    shadow = Config.plain(Layout.theme.shadow),
}

local ui, panel
local phase = 0
local builtRaw          -- LANG string the panel was sized for (rebuild on change)
local msgLines = {}     -- trimmed, non-empty message lines
local msgBlockH = 0     -- total height of the message block
local msgMaxW = 0       -- glyph maxWidth = content width + glyph box pad

-- layout (skin 1920x1080 space)
local POP_X, POP_Y = PANEL.x, PANEL.y
local PAD = PANEL.padding         -- inner margin
local MSG_SIZE, CNT_SIZE = MESSAGE.font_size, COUNT.font_size
local LINE_H = MSG_SIZE + MESSAGE.line_extra   -- message line height
local BAR_H = BAR.height
local ROW_GAP = BAR.row_gap      -- message to bar row gap
local COL_GAP = BAR.column_gap   -- bar to count gap
local BAR_ROW_H = math.max(BAR_H, CNT_SIZE + COUNT.row_extra)
local MIN_CONTENT_W, MAX_CONTENT_W = PANEL.min_content_width, PANEL.max_content_width
local GLYPH_BOX_PAD = 50   -- glyph box pad, 25px per side

-- trimmed non-empty lines; drops the newline + indent layout of the C# translations
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

    local contentW = math.min(MAX_CONTENT_W, math.max(MIN_CONTENT_W, math.ceil(widest) + PANEL.text_margin))
    msgMaxW = contentW + GLYPH_BOX_PAD           -- squish only past contentW
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
    phase = phase + Animation.phase_step
    local pulse = Animation.pulse_min + Animation.pulse_range * math.abs(math.sin(phase))

    ui:draw()

    local cx, cy, cw = panel:content()
    local C = THEME.colors

    -- message, one centred line per row
    for i, line in ipairs(msgLines) do
        ui:drawTextEx(MSG_SIZE, line, cx + cw / 2, cy + (i - 0.5) * LINE_H + MESSAGE.text_y,
            C.text, nil, pulse, 1, msgMaxW, "center")
    end

    -- bar and count row
    local barRowCy = cy + msgBlockH + ROW_GAP + BAR_ROW_H / 2
    local barY = barRowCy - BAR_H / 2

    local countStr = (total > 0) and (done .. " / " .. total) or tostring(done)
    local reserveStr = (total > 0) and (total .. " / " .. total) or "0000000"
    local reserveW = ui:measureText(CNT_SIZE, reserveStr)
    local liveW = ui:measureText(CNT_SIZE, countStr)
    local barW = math.max(BAR.min_width, cw - reserveW - COL_GAP)
    local inset = BAR.inset

    ui:rect(cx, barY, barW, BAR_H, C.track[1], C.track[2], C.track[3], 255)
    if total > 0 then
        ui:rect(cx + inset, barY + inset, (barW - 2 * inset) * math.max(0, math.min(1, done / total)), BAR_H - 2 * inset,
            C.primary[1], C.primary[2], C.primary[3], 255)
    else
        -- total unknown (pre-count running): marquee
        local seg, fw = BAR.marquee_width, barW - 2 * inset
        local mx = (phase * Animation.marquee_speed) % (fw + seg) - seg
        local x0 = (cx + inset) + math.max(0, mx)
        local x1 = (cx + inset) + math.min(fw, mx + seg)
        ui:rect(x0, barY + inset, x1 - x0, BAR_H - 2 * inset, C.primary[1], C.primary[2], C.primary[3], 255)
    end

    -- count, right-aligned, centred on the bar
    ui:drawTextEx(CNT_SIZE, countStr, cx + cw - liveW / 2, barRowCy + COUNT.text_y, C.text, nil, 1, 1, 0, "center")
end
