-- Floor song speed badge on the encounter screen; nothing at x1.

local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")
local Look = Layout.encounter.speed_badge
local Anim = Animation.encounter.speed_badge

local M = {}

local X, LABEL_Y, VALUE_Y = Look.x, Look.label_y, Look.value_y   -- centre x, and each line's centre y
local ARROW_GAP = Look.arrow_gap_px              -- px between the two ▶ centres
local BEAT_BPM = Anim.beat_bpm                   -- arrow beat at x1, scaled by the speed
local BEAT_MIN, BEAT_MAX = Anim.beat_min_ms, Anim.beat_max_ms
local ARROW_MIN, ARROW_PULSE = Anim.arrow_min_opacity, Anim.arrow_pulse_opacity
local FAST, SLOW = Look.fast_color, Look.slow_color
local NUDGE = Look.text_nudge_px

local _fonts = nil
local _colors = nil
local _ink = nil
local _shown = false
local _mult, _value, _label = 1, "", ""
local _col, _rgb = nil, nil
local _beatMs = 500

-- fonts = { label, value } glyph fonts
function M.setup(fonts)
    _fonts = fonts
    if _colors == nil then
        _colors = {
            fast = Config.color(FAST),
            slow = Config.color(SLOW),
        }
        _ink = Config.color(Look.outline_color)
    end
end

-- the speed multiplier of a CONFIG.SONGSPEED value, or nil when it is x1 (or no value)
function M.multiplier(speed)
    if speed == nil then return nil end
    local ok, mult = pcall(function() return CONFIG.SONGSPEED:ToActual(speed) end)
    if not ok or type(mult) ~= "number" or math.abs(mult - 1) < 1e-4 then return nil end
    return mult
end

-- speed = the floor's CONFIG.SONGSPEED value, or nil
function M.open(speed)
    _shown = false
    local mult = M.multiplier(speed)
    if mult == nil then return end
    _shown, _mult = true, mult
    _value = string.format("x%.2f", mult)
    local s = THEME:GetSkinString("SONGSELECT_SONG_SPEED")
    _label = (s ~= nil and s ~= "") and s or "Song speed"
    local fast = mult > 1
    _col, _rgb = fast and _colors.fast or _colors.slow, fast and FAST or SLOW
    _beatMs = math.max(BEAT_MIN, math.min(BEAT_MAX, 60000 / (BEAT_BPM * mult)))
end

function M.shown() return _shown end

local function line(font, str, y, alpha)
    if font ~= nil then font:Draw(str, X, y + NUDGE, _col, _ink, alpha, 1, 0, "center") end
end

-- arrow = the ▶ texture; alpha fades it all in; clock = the screen's time in seconds
function M.draw(arrow, alpha, clock)
    if not _shown or alpha <= 0 or _fonts == nil then return end
    if arrow ~= nil and arrow.Ready then
        local phase = ((clock * 1000) % _beatMs) / _beatMs
        if _mult < 1 then phase = 1 - phase end
        arrow:SetColor(_rgb[1] / 255, _rgb[2] / 255, _rgb[3] / 255)
        for i = 0, 1 do
            local p = (phase - i * 0.5) % 1
            arrow:SetOpacity(alpha * (ARROW_MIN + ARROW_PULSE * (0.5 + 0.5 * math.cos(2 * math.pi * p))))
            arrow:DrawAtAnchor(X + (i - 0.5) * ARROW_GAP, VALUE_Y, "center")
        end
        arrow:SetOpacity(1)
        arrow:SetColor(1, 1, 1)
    end
    line(_fonts.label, _label, LABEL_Y, alpha)
    line(_fonts.value, _value, VALUE_Y, alpha)
end

return M
