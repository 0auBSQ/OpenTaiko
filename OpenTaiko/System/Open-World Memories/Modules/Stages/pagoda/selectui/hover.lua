-- Selected floor's hover border and sheen.

local Easing  = require("Easing")
local Config  = require("Core.Config")
local Layout  = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")
local metrics = Layout.selectui.metrics

local H = {}

local DIR = "Textures/selector/"

local Look = Layout.selectui.hover
local Move = Animation.selectui.hover

-- the border rect from the reference (the floor's slot top); its right edge is res.X minus the inset
local RECT_DX0, RECT_DY0 = Look.rect_dx0, Look.rect_dy0
local RECT_RIGHT_INSET   = Look.rect_right_inset
local RECT_DY1           = Look.rect_dy1

local SLICE, PAD = metrics.border.slice_px, metrics.border.pad_px

local SHEEN_DELAY             = Move.sheen_delay_s
local SHEEN_PERIOD, SHEEN_SEC = Move.sheen_period_s, Move.sheen_s
local SHEEN_ROT, SHEEN_ALPHA  = Look.sheen_rot_deg, Look.sheen_opacity
local SHEEN_LIGHTEN           = Look.sheen_lighten

local _border, _sheen = nil, nil
local _selT = math.huge

function H.setup()
    if _border ~= nil then return end
    _border = TEXTURE:CreateTexture(DIR .. "sel_border.png")
    _sheen  = TEXTURE:CreateTexture(DIR .. "sel_sheen.png")
end

function H.dispose()
    for _, t in ipairs({ _border, _sheen }) do t:Dispose() end
    _border, _sheen = nil, nil
end

function H.select(level) _selT = 0 end

function H.update(dt)
    _selT = _selT + dt
end

local function rect(rx, ry)
    local res = THEME:GetResolution()
    return math.floor(rx + RECT_DX0 + 0.5), math.floor(ry + RECT_DY0 + 0.5),
           res.X - RECT_RIGHT_INSET,        math.floor(ry + RECT_DY1 + 0.5)
end

-- nine-slice border round the rect (empty middle); the bar's outer edge lies on the rect's edge
local function draw_border(x0, y0, x1, y1)
    if not _border.Ready then return end
    local x, y = x0 - PAD, y0 - PAD
    local w, h = x1 - x0 + 2 * PAD, y1 - y0 + 2 * PAD
    local tw, th = _border.Width, _border.Height
    local mw, mh = tw - 2 * SLICE, th - 2 * SLICE
    local iw, ih = math.max(0, w - 2 * SLICE), math.max(0, h - 2 * SLICE)
    local cols = { { 0, SLICE, x, SLICE }, { SLICE, mw, x + SLICE, iw }, { tw - SLICE, SLICE, x + SLICE + iw, SLICE } }
    local rows = { { 0, SLICE, y, SLICE }, { SLICE, mh, y + SLICE, ih }, { th - SLICE, SLICE, y + SLICE + ih, SLICE } }
    for i, col in ipairs(cols) do
        for j, row in ipairs(rows) do
            if not (i == 2 and j == 2) and col[4] > 0 and row[4] > 0 then
                _border:SetScale(col[4] / col[2], row[4] / row[2])
                _border:DrawRect(col[3], row[3], col[1], row[1], col[2], row[2])
            end
        end
    end
    _border:SetScale(1, 1)
end

local function draw_sheen(x0, y0, x1, y1, r, g, b)
    if not _sheen.Ready then return end
    local st = _selT - SHEEN_DELAY
    if st < 0 then return end
    local ph = st % SHEEN_PERIOD
    if ph >= SHEEN_SEC then return end
    local half = _sheen.Width
    local x = (x0 - half) + (x1 - x0 + 2 * half) * Easing.inOutSine(ph / SHEEN_SEC)
    GRAPHICS:SetClip(x0, y0, x1 - x0, y1 - y0)
    _sheen:SetBlendMode("Add")
    _sheen:SetRotation(-SHEEN_ROT)
    _sheen:SetColor(r + (1 - r) * SHEEN_LIGHTEN, g + (1 - g) * SHEEN_LIGHTEN, b + (1 - b) * SHEEN_LIGHTEN)
    _sheen:SetOpacity(SHEEN_ALPHA)
    _sheen:DrawAtAnchor(x, (y0 + y1) * 0.5, "center")
    _sheen:SetOpacity(1)
    _sheen:SetColor(1, 1, 1)
    _sheen:SetRotation(0)
    _sheen:SetBlendMode("Normal")
    GRAPHICS:ClearClip()
end

-- r, g, b: the floor's plate colour (white for roofs), tints the sheen
function H.draw(level, r, g, b, rx, ry)
    if _border == nil then return end
    local x0, y0, x1, y1 = rect(rx, ry)
    draw_sheen(x0, y0, x1, y1, r, g, b)
    draw_border(x0, y0, x1, y1)
end

return H
