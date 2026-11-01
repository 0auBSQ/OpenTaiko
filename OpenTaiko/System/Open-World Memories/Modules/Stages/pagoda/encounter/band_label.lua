-- Floor tier and number label on the encounter screen.

local Easing  = require("Easing")
local Rainbow = require("Rainbow")
local levels  = require("levels")
local text    = require("encounter/bar_text")
local Config  = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")
local Look = Layout.encounter.band_label
local Anim = Animation.encounter.band_label
local Digits = Layout.selectui.metrics.digits   -- 1-based: digit d at d + 1

-- nil for a non-digit
local function digitMetrics(d)
    return d ~= nil and Digits[d + 1] or nil
end

local M = {}

local GAP = Look.digit_gap_px                   -- px between neighbouring digits' ink
local NUM_SCALE = Look.number_scale
local NUM_LEFT = Look.number_left_x             -- left ink edge of the number
local NUM_Y = Look.number_y                     -- y of the number's ink centre
local LABEL_GAP = Look.label_gap_px             -- px from the name's right end to the number
local LABEL_DY = Look.label_dy                  -- the name's right end, relative to the number's centre
local LABEL_ROT, NUM_ROT = Look.label_rot_deg, Look.number_rot_deg
local LABEL_ALPHA = Look.label_opacity
local LABEL_JITTER = Look.label_jitter
local LABEL_WIDTH = Look.label_width_px
local POP_SEC, POP_FROM, POP_BACK = Anim.pop_s, Anim.pop_from_scale, Anim.pop_overshoot
local NUM_DELAY = Anim.number_delay_s
local ROOF_FROM = 21
local RAINBOW = {
    cycles = Look.rainbow.cycles, speed = Anim.rainbow_speed, sat = Look.rainbow.sat, val = Look.rainbow.val,
    anchor = Look.rainbow.anchor, strip = Look.rainbow.strip,
}
local FACE = Look.face_color

local _font = nil
local _style = nil
local _level = 1
local _num = ""
local _digits = {}          -- digit -> encounter/assets handle
local _lay = nil

local function sk(key, fallback)
    local s = THEME:GetSkinString(key)
    if s == nil or s == "" then return fallback end
    return s
end

local function color(c, a)
    return COLOR:CreateColorFromRGBA(math.floor(c[1] * 255 + 0.5), math.floor(c[2] * 255 + 0.5),
        math.floor(c[3] * 255 + 0.5), a or 255)
end

function M.setup(font)
    _font = font
    if _style == nil then
        _style = { face = color(FACE), rim = color(FACE, 0) }
    end
end

local function tierName(level)
    if level <= 10 then return sk("PAGODA_BAND_TEST", "Test")
    elseif level < ROOF_FROM then return sk("PAGODA_BAND_GATE", "Gate") end
    return sk("PAGODA_BAND_SKY", "Sky")
end

local function tierNumber(level)
    if level <= 10 then return level
    elseif level < ROOF_FROM then return level - 10 end
    return level - 20
end

-- encounter/assets keys of the level's digits
function M.keys(level)
    local num, keys = tostring(tierNumber(level)), {}
    for i = 1, #num do keys[#keys + 1] = "sb_floor" .. num:sub(i, i) end
    return keys
end

-- tex: key -> handle of the visit's opened keys
function M.open(level, tex)
    M.close()
    _level = level
    _num = tostring(tierNumber(level))
    for i = 1, #_num do
        local d = tonumber(_num:sub(i, i))
        if d ~= nil then _digits[d] = tex["sb_floor" .. d] end
    end
    _lay = text.layout(_font, tierName(level), 0, LABEL_WIDTH, 1, LABEL_JITTER, level, true)
end

function M.close()
    _digits = {}
    _lay = nil
end

-- returns the digits' ink width and extent above / below the baseline (unscaled)
local function runBox()
    local pen, last, up, down = 0, nil, 0, 0
    for i = 1, #_num do
        local m = digitMetrics(tonumber(_num:sub(i, i)))
        if m ~= nil then
            if last ~= nil then pen = pen + (last.ink_r - last.ink_l + GAP) end
            last = m
            up = math.max(up, m.base_y - m.ink_t)
            down = math.max(down, m.ink_b - m.base_y)
        end
    end
    if last == nil then return 0, 0, 0 end
    return pen + (last.ink_r - last.ink_l), up, down
end

local function pop(t)
    if t <= 0 then return 0 end
    return POP_FROM + (1 - POP_FROM) * Easing.outBack(t / POP_SEC, POP_BACK)
end

local function drawNumber(t, clock)
    local k = pop(t)
    if k <= 0 then return end
    local alpha = math.min(1, t / (POP_SEC * 0.5))
    local w, up, down = runBox()
    local s = NUM_SCALE * k
    local cx, cy = NUM_LEFT + w * NUM_SCALE * 0.5, NUM_Y
    local xf = { x = cx, y = cy, s = s, rot = NUM_ROT }
    local roof = _level >= ROOF_FROM
    local r, g, b = 1, 1, 1
    if not roof then
        local c = levels.level_tick_color(_level)
        r, g, b = c[1] / 255, c[2] / 255, c[3] / 255
    else
        RAINBOW.t, RAINBOW.sx, RAINBOW.sy, RAINBOW.rot = clock, s, s, NUM_ROT
        RAINBOW.x0, RAINBOW.span, RAINBOW.opacity = cx - w * s * 0.5, w * s, alpha
    end
    local pen, last = 0, nil
    local midY = (down - up) * 0.5
    for i = 1, #_num do
        local d = tonumber(_num:sub(i, i))
        local m = digitMetrics(d)
        local tx = _digits[d]
        if m ~= nil then
            if last ~= nil then pen = pen + (last.ink_r - last.ink_l + GAP) end
            last = m
            local lx = pen - m.ink_l + m.w * 0.5 - w * 0.5
            local ly = -m.base_y + m.h * 0.5 - midY
            local x, y = text.point(xf, lx, ly)
            if tx ~= nil and tx.Ready then
                if roof then
                    Rainbow.draw(tx, x, y, RAINBOW)
                else
                    tx:SetScale(s, s)
                    tx:SetRotation(NUM_ROT)
                    tx:SetColor(r, g, b)
                    tx:SetOpacity(alpha)
                    tx:DrawAtAnchor(x, y, "center")
                    tx:SetScale(1, 1) ; tx:SetRotation(0) ; tx:SetColor(1, 1, 1) ; tx:SetOpacity(1)
                end
            end
        end
    end
end

local function drawName(t)
    local k = pop(t)
    if k <= 0 or _lay == nil then return end
    local alpha = math.min(1, t / (POP_SEC * 0.5)) * LABEL_ALPHA
    local a = math.rad(LABEL_ROT)
    local half = _lay.w * 0.5
    local rx, ry = NUM_LEFT - LABEL_GAP, NUM_Y + LABEL_DY
    local xf = { x = rx - half * math.cos(a), y = ry + half * math.sin(a), s = k, rot = LABEL_ROT }
    text.draw(_font, _lay, xf, _style, alpha)
end

-- t: seconds since the name appeared (nil: not shown yet); clock: the screen's time
function M.draw(t, clock)
    if t == nil then return end
    drawName(t)
    drawNumber(t - NUM_DELAY, clock or 0)
end

-- seconds until the number is settled
function M.duration() return NUM_DELAY + POP_SEC end

return M
