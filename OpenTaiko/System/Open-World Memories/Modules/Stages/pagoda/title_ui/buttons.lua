-- Title menu buttons.

local slice3  = require("title_ui/slice3")
local Config  = require("Core.Config")
local Layout  = Config.load("Config/layout.json")
local pill    = require("encounter/ui_pill")

local M = {}

local EXIT = 3                                  -- menu index of Exit
local TEXT_PAD = 25                             -- px margin of a GetText texture left of the ink

local function ready(tx) return tx ~= nil and tx.Ready end

local function reset(tx)
    tx:SetScale(1, 1) ; tx:SetOpacity(1) ; tx:SetColor(1, 1, 1) ; tx:SetBlendMode("Normal")
end

-- common width: the longest label (white GetText textures, by menu index) plus the pill's insets
function M.width(labels)
    local ink = 0
    for _, lb in pairs(labels) do
        if ready(lb) then ink = math.max(ink, lb.Width - 2 * TEXT_PAD) end
    end
    return math.max(pill.BASE_W, slice3.width(Layout.title_ui.metrics.button, ink))
end

local function drawLabel(lb, x, y, s, inkMid, selected)
    if not ready(lb) then return end
    local ink = lb.Width - 2 * TEXT_PAD
    lb:SetScale(s, s)
    lb:SetColor(pill.labelTint(selected))
    lb:Draw(x - (TEXT_PAD + ink / 2) * s, y - inkMid * s)
    reset(lb)
end

-- items = the menu indices shown; sel = the selected index; labels / ys = label texture and centre y
-- by index; x = the column's centre; w = M.width(labels); inkMid = the middle of the letters in a
-- label texture, from its top; t = seconds; tex = { button, exit, marker, glow }
function M.draw(items, sel, labels, ys, x, w, inkMid, t, tex)
    for _, i in ipairs(items) do
        local selected = (i == sel)
        local y = ys[i]
        local pieces = (i == EXIT) and tex.exit or tex.button
        local s = pill.draw(tex, x, y, 1, selected, t, 1, w, function(bs, r, g, b, a)
            slice3.draw(pieces, x, y, w, bs, r, g, b, a)
        end)
        drawLabel(labels[i], x, y, s, inkMid, selected)
    end
end

return M
