-- The level's estimated difficulty line on the selector, with encounter/song_bar emblems.

local data   = require("encounter/songbars_data")
local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local metrics = Layout.selectui.metrics

local D = {}

local Look = Layout.selectui.difficulty
local PLUS_DX, PLUS_DY = Layout.encounter.song_bar.plus_dx, Layout.encounter.song_bar.plus_dy
local TEXT_PAD = 25                       -- the text texture's clear margin left and right of the line

local EASY, NORMAL, HARD, EXTREME, EXTRA = 0, 1, 2, 3, 4

local function e(diff, level, plus) return { diff = diff, level = level, plus = plus == true } end

-- per level: the lowest and highest estimate (one entry when they are the same)
local RANGES = {
    [1]  = { e(EASY, 1), e(EASY, 2) },          [2]  = { e(EASY, 2), e(EASY, 3) },
    [3]  = { e(EASY, 3), e(EASY, 5) },          [4]  = { e(EASY, 5), e(EASY, 5, true) },
    [5]  = { e(NORMAL, 4), e(NORMAL, 6) },      [6]  = { e(NORMAL, 6) },
    [7]  = { e(NORMAL, 7) },                    [8]  = { e(HARD, 4), e(HARD, 5) },
    [9]  = { e(HARD, 5), e(HARD, 6) },          [10] = { e(HARD, 6), e(HARD, 7) },
    [11] = { e(EXTREME, 5), e(EXTREME, 6) },    [12] = { e(EXTREME, 7) },
    [13] = { e(EXTREME, 8) },                   [14] = { e(EXTREME, 8, true) },
    [15] = { e(EXTREME, 9) },                   [16] = { e(EXTREME, 9, true) },
    [17] = { e(EXTREME, 10) },                  [18] = { e(EXTREME, 10) },
    [19] = { e(EXTREME, 10, true) },            [20] = { e(EXTREME, 10, true) },
    [21] = { e(EXTREME, 11) },                  [22] = { e(EXTREME, 11) },
    [23] = { e(EXTREME, 11) },                  [24] = { e(EXTREME, 11, true) },
    [25] = { e(EXTREME, 11, true) },            [26] = { e(EXTREME, 11, true) },
    [27] = { e(EXTREME, 12) },
}
local BEYOND = { e(EXTREME, 12), e(EXTREME, 12, true) }   -- level 28 and above

local _tex = nil
local _font = nil
local _white, _edge = nil, nil

local function sk(key, fallback)
    local s = THEME:GetSkinString(key)
    if s == nil or s == "" then return fallback end
    return s
end

local function ready(tx) return tx ~= nil and tx.Ready end

-- font: the selector's label font (GetText)
function D.setup(font)
    _font = font
    if _white == nil then
        _white = Config.color(Look.text_color)
        _edge  = Config.color(Look.text_edge_color)
    end
    if _tex ~= nil then return end
    _tex = {}
    for d = 0, 4 do _tex["sb_emblem" .. d] = TEXTURE:CreateTexture(data.tex["sb_emblem" .. d].path) end
    for d = 0, 9 do _tex["sb_level" .. d] = TEXTURE:CreateTexture(data.tex["sb_level" .. d].path) end
    _tex.sb_level_plus = TEXTURE:CreateTexture(data.tex.sb_level_plus.path)
end

function D.dispose()
    if _tex == nil then return end
    for _, tx in pairs(_tex) do tx:Dispose() end
    _tex = nil
end

function D.range(level)
    return RANGES[level] or BEYOND
end

local function at(tx, x, y, s)
    if not ready(tx) then return end
    tx:SetScale(s, s)
    tx:DrawAtAnchor(x, y, "center")
    tx:SetScale(1, 1)
end

-- the chart level over the emblem centred on (x, y), set as on a song bar
local function digits(est, x, y, k)
    local s = tostring(est.level)
    local total, prev, last = 0, 0, 0
    for i = 1, #s do
        local adv = data.digits[tonumber(s:sub(i, i))] or 0
        if i > 1 then total = total + prev * 0.5 end
        prev, last = adv, adv
    end
    total = total + last
    local cursor, step = -total * 0.5, 0
    for i = 1, #s do
        local d = tonumber(s:sub(i, i))
        local adv = data.digits[d] or 0
        if i > 1 then cursor = cursor + step * 0.5 end
        at(_tex["sb_level" .. d], x + (cursor + adv * 0.5) * k, y, k)
        step = adv
    end
    if est.plus then at(_tex.sb_level_plus, x + (total * 0.5 + PLUS_DX) * k, y + PLUS_DY * k, k) end
end

local function emblemW() return data.tex.sb_emblem0.w * Look.emblem_scale end

-- Extreme draws the Extreme emblem over the Extra Extreme one, down and left of it
local function groupW(est)
    if est.diff == EXTREME then return emblemW() + Look.stack_dx * Look.emblem_scale end
    return emblemW()
end

-- the estimate's emblem(s) from x, centred on y; the level goes on the front one
local function drawGroup(est, x, y)
    local k, w = Look.emblem_scale, emblemW()
    local cx = x + w * 0.5
    if est.diff == EXTREME then
        local dx, dy = Look.stack_dx * k, Look.stack_dy * k
        at(_tex["sb_emblem" .. EXTRA], cx + dx, y - dy * 0.5, k)
        y = y + dy * 0.5
    end
    at(_tex["sb_emblem" .. est.diff], cx, y, k)
    digits(est, cx, y, k)
end

local function label(s)
    if _font == nil then return nil end
    local tx = _font:GetText(s, false, 99999, _white, _edge)
    if not ready(tx) then return nil end
    return tx, math.max(0, tx.Width - 2 * TEXT_PAD)
end

-- the line centred on (cx, cy)
function D.draw(level, cx, cy)
    if _tex == nil or level == nil then return end
    local est = D.range(level)
    local head, headW = label(sk("PAGODA_ESTIMATED_DIFFICULTY", "Estimated difficulty:"))
    local dash, dashW = nil, 0
    if #est > 1 then dash, dashW = label("-") end
    local w = headW + Look.label_gap_px + groupW(est[1])
    if #est > 1 then w = w + 2 * Look.dash_gap_px + dashW + groupW(est[2]) end

    local x = cx - w * 0.5
    local ty = cy + metrics.label.nudge
    if head ~= nil then head:DrawAtAnchor(x - TEXT_PAD, ty, "left") end
    x = x + headW + Look.label_gap_px
    drawGroup(est[1], x, cy)
    if #est > 1 then
        x = x + groupW(est[1]) + Look.dash_gap_px
        if dash ~= nil then dash:DrawAtAnchor(x - TEXT_PAD, ty, "left") end
        x = x + dashW + Look.dash_gap_px
        drawGroup(est[2], x, cy)
    end
end

return D
