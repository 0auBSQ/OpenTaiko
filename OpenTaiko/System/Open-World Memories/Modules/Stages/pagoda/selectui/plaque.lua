-- Floor and Exit plaques, placed from the selected slot's reference top-left (rx, ry).

local Easing  = require("Easing")
local Rainbow = require("Rainbow")
local numbers = require("selectui/numbers")
local Config  = require("Core.Config")
local Layout  = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")
local metrics = Layout.selectui.metrics

local P = {}

local DIR = "Textures/selector/"

local Look = Layout.selectui.plaque
local Move = Animation.selectui.plaque

local OFF_PLAQUE = Look.plaque_offset    -- plaque.png top-left
local SEAL_DY    = Look.seal_dy          -- seal top from the reference
local NUM_DY     = Look.num_dy           -- baseline of a full-size number from the reference
local NUM_GAP    = Look.num_gap          -- least gap from the number ink to the seal and the margin
local NUM_RIGHT  = Look.num_right        -- right margin, from the plaque right
local HEADER_DX, HEADER_DY = Look.header.dx, Look.header.dy   -- from the plaque top-left
local STRIP_DX, STRIP_DY   = Look.strip.dx, Look.strip.dy     -- "Level N" band centre, from the plaque top-left
local EXIT_DX, EXIT_DY     = Look.exit.dx, Look.exit.dy       -- "Exit" centre, from the plaque top-left
local HEADER_MAX_W, LEVEL_MAX_W, EXIT_MAX_W = Look.header_max_w, Look.level_max_w, Look.exit_max_w

local POP_SEC, POP_FROM, POP_BACK = Move.pop_s, Move.pop_from_scale, Move.pop_overshoot

local TIER_SIZE = Look.tier_font_size
local TEXT_PAD  = 25                      -- the text texture's clear margin left and right of the line

local ROOF_FROM = 21
local RAINBOW = { cycles = Look.rainbow.cycles, speed = Move.rainbow_speed, sat = Look.rainbow.sat,
                  val = Look.rainbow.val }
local GLITTER = { rate = Move.glitter_rate_per_s, size = Look.glitter.size, opacity = Look.glitter.opacity,
                  max = Look.glitter.max }
local GLITTER_INSET = Look.glitter_inset  -- share of the number's ink box kept clear at its edges

local _fonts  = nil
local _plaque = nil
local _tierFont = nil
local _sparkle = nil
local _ink, _clear, _grey = nil, nil, nil
local _popT = math.huge
local _t, _glitDt = 0, 0
local _glit = {}

local function sk(key, fallback)
    local s = THEME:GetSkinString(key)
    if s == nil or s == "" then return fallback end
    return s
end

function P.setup(fonts)
    _fonts = fonts
    if _ink == nil then
        _ink   = Config.color(Look.level_text_color)
        _clear = COLOR:CreateColorFromRGBA(0, 0, 0, 0)
        _grey  = Config.color(Look.tier_color)
    end
    if _plaque ~= nil then return end
    _plaque = TEXTURE:CreateTexture(DIR .. "plaque.png")
    _tierFont = TEXT:Create(TIER_SIZE, "regular")
    _sparkle = TEXTURE:CreateTexture(DIR .. "sel_sparkle.png")
end

function P.dispose()
    if _plaque ~= nil then _plaque:Dispose() ; _plaque = nil end
    if _tierFont ~= nil then _tierFont:Dispose() ; _tierFont = nil end
    if _sparkle ~= nil then _sparkle:Dispose() ; _sparkle = nil end
end

-- restarts the number's pop and glitter
function P.pop()
    _popT = 0
    _glit.list = nil
end

function P.update(dt)
    _popT = _popT + dt
    _t = _t + dt
    _glitDt = _glitDt + dt
end

-- tiers: base 1-10 (道), gates 11-20 (扉), roofs 21+ (穹天)
local function tier_name(level)
    if level <= 10 then return sk("PAGODA_BAND_TEST", "Trail")
    elseif level < ROOF_FROM then return sk("PAGODA_BAND_GATE", "Gate")
    else return sk("PAGODA_BAND_SKY", "Sky") end
end

-- the number shown on the plaque counts within its tier
local function tier_number(level)
    if level <= 10 then return level
    elseif level < ROOF_FROM then return level - 10
    else return level - 20 end
end

local function plaque_topleft(rx, ry)
    return math.floor(rx + OFF_PLAQUE[1] + 0.5), math.floor(ry + OFF_PLAQUE[2] + 0.5)
end

local function draw_panel(px, py)
    if _plaque ~= nil and _plaque.Ready then _plaque:Draw(px, py) end
end

-- seal png left, and the number's ink centre x and fit scale, for the plaque at px. Every number
-- is centred midway between the seal and the right margin, scaled down when it cannot keep NUM_GAP
local function layout(px, text)
    local sx = px + HEADER_DX - metrics.seal.ink_l
    local left, right = sx + metrics.seal.ink_r, px + metrics.plaque.w - NUM_RIGHT
    local fit = math.min(1, (right - left - 2 * NUM_GAP) / numbers.width(text))
    return sx, (left + right) * 0.5, fit
end

-- roof number glitter over the middle of its ink box
local function draw_glitter(bx, by, bw, bh)
    local ix, iy = bw * GLITTER_INSET, bh * GLITTER_INSET
    Rainbow.glitter(_glit, _glitDt, bx + ix, by + iy, bw - 2 * ix, bh - 2 * iy, _sparkle, GLITTER)
end

-- tintFn(level) -> r, g, b for the number face (roofs take the rainbow instead)
function P.draw(level, header, rx, ry, tintFn)
    if _plaque == nil then return end
    local px, py = plaque_topleft(rx, ry)
    draw_panel(px, py)

    local text = tostring(tier_number(level))
    local sx, ncx, fit = layout(px, text)
    -- tier name centred in the seal's ink box (the text texture has a pad under the line);
    -- scaled down evenly when wider than the box
    local tier = _tierFont:GetText(tier_name(level), false, 99999, _grey, _clear)
    if tier ~= nil then
        local inkW = math.max(1, tier.Width - 2 * TEXT_PAD)
        local k = math.min(1, (metrics.seal.ink_r - metrics.seal.ink_l) / inkW)
        tier:SetScale(k, k)
        tier:DrawAtAnchor(math.floor(sx + (metrics.seal.ink_l + metrics.seal.ink_r) * 0.5 + 0.5),
                          math.floor(ry + SEAL_DY + metrics.seal.h * 0.5 + metrics.label.nudge * k + 0.5), "center")
        tier:SetScale(1, 1)
    end

    -- the number keeps the centre a full-size one has on the NUM_DY baseline
    local ncy = ry + NUM_DY - numbers.rise(text)
    local pop = POP_FROM + (1 - POP_FROM) * Easing.outBack(_popT / POP_SEC, POP_BACK)
    if level >= ROOF_FROM then
        RAINBOW.t = _t
        local bx, by, bw, bh = numbers.draw(text, ncx, ncy, fit * pop, 1, 1, 1, 1, RAINBOW)
        draw_glitter(bx, by, bw, bh)
    else
        local r, g, b = 1, 1, 1
        if tintFn ~= nil then r, g, b = tintFn(level) end
        numbers.draw(text, ncx, ncy, fit * pop, r, g, b, 1)
    end
    _glitDt = 0

    if header ~= nil and header ~= "" then
        local h = _fonts.header:GetText(header, false, HEADER_MAX_W)
        if h ~= nil then h:DrawAtAnchor(px + HEADER_DX, py + HEADER_DY, "topleft") end
    end

    -- centred on its capitals: the text texture carries a pad under the line
    local lvl = _fonts.name:GetText(sk("PAGODA_LEVEL", "Level") .. " " .. tostring(level),
                                    false, LEVEL_MAX_W, _ink, _clear)
    if lvl ~= nil then lvl:DrawAtAnchor(px + STRIP_DX, py + STRIP_DY + metrics.label.nudge, "center") end
end

function P.drawExit(rx, ry)
    if _plaque == nil then return end
    local px, py = plaque_topleft(rx, ry)
    draw_panel(px, py)
    local t = _fonts.header:GetText(sk("PAGODA_EXIT", "Exit"), false, EXIT_MAX_W)
    if t ~= nil then t:DrawAtAnchor(px + EXIT_DX, py + EXIT_DY, "center") end
end

return P
