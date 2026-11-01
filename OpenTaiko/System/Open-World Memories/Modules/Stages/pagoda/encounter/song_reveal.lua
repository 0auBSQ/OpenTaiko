-- What the encounter's song bars reveal of the floor's rolled songs.

local levels = require("levels")

local M = {}

M.DESTINY_LEVEL = 20

-- stable number from a string
local function hash(s)
    local h = 5381
    for i = 1, #s do h = (h * 33 + s:byte(i)) % 2147483647 end
    return h
end

local function songInfo(node, diff)
    local info = { diff = diff or 0, name = nil, level = nil, plus = false, id = "" }
    if node == nil then return info end
    pcall(function()
        info.name = node.Title
        info.id = node.UniqueId or info.name or ""
    end)
    pcall(function()
        local chart = node:GetChart(info.diff)
        if chart ~= nil then info.level, info.plus = chart.Level, chart.IsPlus == true end
    end)
    if info.name == nil or info.name == "" then info.name = "?" end
    return info
end

local function newBar(color, info, purple)
    return {
        color = color, purple = purple,
        title = info.name, diff = info.diff, level = info.level, plus = info.plus,
        named = false,          -- title shown; otherwise "???"
        dark = false,           -- darkness over the bar (unrevealed mystery song)
        chance = nil,           -- percent shown on the darkness while the mystery song is unknown
    }
end

-- roll = { songs = { { color, node, entry }, ... }, purple = { node, entry } | nil }; highest = highest beaten floor.
-- Returns the bars top to bottom (mystery last) and true when the floor plays its destiny sequence.
function M.bars(level, highest, roll)
    local list = {}
    if roll == nil then return list, false end
    local gap = (highest or 0) - level
    local mystery = (level == M.DESTINY_LEVEL) and "black" or "purple"
    local named = math.max(0, math.min(3, gap - 1))

    local base = {}
    for _, item in ipairs(roll.songs or {}) do
        local info = songInfo(item.node, item.entry ~= nil and item.entry.diff or 0)
        local bar = newBar(item.color or "blue", info, false)
        bar.rank = hash(info.id .. "|" .. level)
        list[#list + 1] = bar
        base[#base + 1] = bar
    end
    table.sort(base, function(a, b) return a.rank < b.rank end)
    for i = 1, named do
        if base[i] ~= nil then base[i].named = true end
    end

    local chance = math.floor(levels.purple_prob(level) * 100 + 0.5)
    local purple = roll.purple
    local destiny = level == M.DESTINY_LEVEL and purple ~= nil
    local bar = nil
    if purple ~= nil and (destiny or gap >= 1) then
        bar = newBar(mystery, songInfo(purple.node, purple.entry ~= nil and purple.entry.diff or 0), true)
        bar.named = gap >= 2 and not destiny
        bar.dark = not bar.named
    elseif gap < 1 and chance > 0 then
        bar = newBar(mystery, songInfo(nil), true)
        bar.dark, bar.chance = true, chance
    end
    if bar ~= nil then list[#list + 1] = bar end
    return list, destiny
end

-- the bars with everything told (Asagao's Gift); no mystery bar when none came
function M.revealed(level, roll)
    local list = {}
    if roll == nil then return list end
    for _, item in ipairs(roll.songs or {}) do
        local bar = newBar(item.color or "blue", songInfo(item.node, item.entry ~= nil and item.entry.diff or 0), false)
        bar.named = true
        list[#list + 1] = bar
    end
    local purple = roll.purple
    if purple ~= nil then
        local mystery = (level == M.DESTINY_LEVEL) and "black" or "purple"
        local bar = newBar(mystery, songInfo(purple.node, purple.entry ~= nil and purple.entry.diff or 0), true)
        bar.named = true
        list[#list + 1] = bar
    end
    return list
end

return M
