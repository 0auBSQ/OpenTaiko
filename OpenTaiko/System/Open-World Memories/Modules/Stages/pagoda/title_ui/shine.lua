-- Shine bars across a line of text, masked to the letters.

local Easing = require("Easing")
local Rainbow = require("Rainbow")
local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")

local M = {}

local Look = Layout.title_ui.shine
local Move = Animation.title_ui.shine

local FIRST, EVERY, SWEEP = Move.first_s, Move.every_s, Move.sweep_s
local SLANT = Look.slant                      -- px right per px up
local STRIP = Look.strip_px                   -- strip width, texture px
local SOFT, CORE = Look.core_width_share, Look.core_opacity_share
local BARS = {}                               -- per bar: offset behind the lead (px), half width, peak opacity
for i, b in ipairs(Look.bars) do BARS[i] = { off = b.off_px, hw = b.half_w, a = b.opacity } end
local GLITTER_W, GLITTER_H = Look.glitter_w, Look.glitter_h_share
local GLITTER_RATE = Move.glitter_rate_per_s
local TEXT_PAD = 25                           -- px margin of a GetText texture left and right of the ink
local _glitterOpts = { rate = 0, size = Look.glitter_size, opacity = Look.glitter_opacity, blend = "Add" }

local floor, max, min = math.floor, math.max, math.min

function M.new() return { t = -FIRST, glitter = {} } end

-- one bar of half width hw at opacity a, centred on column p at the middle row
local function drawBar(mask, x, y, sx, p, hw, a, row0, row1, ymid)
    local w = mask.Width
    local c0 = max(0, floor((p - hw - SLANT * (row1 - ymid)) / STRIP) * STRIP)
    local c1 = min(w, p + hw + SLANT * (ymid - row0))
    mask:SetOpacity(a)
    local c = c0
    while c < c1 do
        local cw = min(STRIP, w - c)
        local d = c + cw * 0.5 - p
        local ya = max(row0, floor(ymid - (d + hw) / SLANT))
        local yb = min(row1, math.ceil(ymid - (d - hw) / SLANT))
        if yb > ya then mask:DrawRect(x + c * sx, y + ya, c, ya, cw, yb - ya) end
        c = c + STRIP
    end
end

-- st = M.new() state; mask = the text's white texture, drawn at (x, y) with x scale sx;
-- row0..row1 = its ink rows; spark = the glitter texture
function M.draw(st, dt, mask, x, y, sx, row0, row1, spark)
    st.t = st.t + (dt or 0)
    if mask == nil or not mask.Ready then return end
    local phase = (st.t >= 0) and (st.t % EVERY) or -1
    local sweeping = phase >= 0 and phase < SWEEP
    local lead = nil
    if sweeping then
        local ymid = (row0 + row1) * 0.5
        local reach = BARS[1].hw + SLANT * (row1 - row0)
        local from, to = -reach, mask.Width + reach - BARS[2].off
        lead = from + (to - from) * Easing.inOutSine(phase / SWEEP)
        mask:SetBlendMode("Add")
        mask:SetColor(1, 1, 1)
        mask:SetScale(sx, 1)
        for _, b in ipairs(BARS) do
            local p = lead + b.off
            drawBar(mask, x, y, sx, p, b.hw, b.a * (1 - CORE), row0, row1, ymid)
            drawBar(mask, x, y, sx, p, b.hw * SOFT, b.a * CORE, row0, row1, ymid)
        end
        mask:SetBlendMode("Normal")
        mask:SetScale(1, 1)
        mask:SetOpacity(1)
    end
    if spark ~= nil and spark.Ready then
        local over = false
        if lead ~= nil then
            local c = lead + BARS[2].off * 0.5
            over = c >= TEXT_PAD and c <= mask.Width - TEXT_PAD
            if over then st.gx = x + c * sx - GLITTER_W * 0.5 end
        end
        _glitterOpts.rate = over and GLITTER_RATE or 0
        local gh = (row1 - row0) * GLITTER_H
        Rainbow.glitter(st.glitter, dt or 0, st.gx or x, y + (row0 + row1 - gh) * 0.5, GLITTER_W, gh, spark,
                        _glitterOpts)
    end
end

return M
