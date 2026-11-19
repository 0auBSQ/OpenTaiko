-- Pagoda.Floor: a floor's song roll and its dan

local Levels = require("Pagoda.Levels")
local Songs  = require("Pagoda.Songs")
local Texts  = require("Pagoda.Texts")

local M = {}

M.GENRE_COLOR = { 100, 150, 240 }
M.PURPLE_BAD_SCALE = 1.25           -- bad-count exam factor with a purple song

-- floor song speed (CONFIG.SONGSPEED value): x1 below level 38 (穹天18), then ramps
function M.speed(level)
    local SPEED = CONFIG.SONGSPEED
    local actual = (level >= 38) and (1 + (level - 37) / SPEED.ScaleFromActual) or 1
    return SPEED:FromActual(actual)
end

-- one entry of a colour, as { node, entry }, or nil when the colour has none or the song is not installed
local function pick(sl, entries, used_ids)
    if entries == nil or #entries == 0 then return nil end
    local entry = Levels.pick_entry(entries, used_ids)
    if entry == nil then return nil end
    local node = sl:GetSongByUniqueId(entry.id)
    if node ~= nil and node.NotNull then return node, entry end
    return nil
end

-- one song per colour in shuffled order, then purple at Levels.purple_prob; no repeats
-- avoid: optional set of song ids to skip; songList: optional (default Songs.get_song_list())
-- roll = { songs = { { color, node, entry } }, purple = { node, entry } | nil, speed }
-- entry = the pool's { id, diff }; speed = a CONFIG.SONGSPEED value
function M.rollFloor(level, avoid, songList)
    local roll = { songs = {}, purple = nil, speed = M.speed(level) }
    local sl   = songList or Songs.get_song_list()
    local pool = Levels.get_pool(level)
    if pool == nil then return roll end

    local used_ids = {}
    for id in pairs(avoid or {}) do used_ids[id] = true end
    local colors = { "blue", "green", "red" }
    Levels.shuffle(colors)

    for _, color in ipairs(colors) do
        local node, entry = pick(sl, pool[color], used_ids)
        if node ~= nil then
            table.insert(roll.songs, { color = color, node = node, entry = entry })
            used_ids[entry.id] = true
        end
    end

    if math.random() < Levels.purple_prob(level) then
        local node, entry = pick(sl, pool["purple"], used_ids)
        if node ~= nil then roll.purple = { node = node, entry = entry } end
    end
    return roll
end

-- reroll by the same rules, avoiding roll's songs where the pools allow
-- keepMystery keeps roll's purple song
function M.rerollFloor(level, roll, keepMystery, songList)
    local avoid = {}
    for _, item in ipairs((roll and roll.songs) or {}) do avoid[item.entry.id] = true end
    local purple = roll and roll.purple
    if purple ~= nil then avoid[purple.entry.id] = true end
    local out = M.rollFloor(level, avoid, songList)
    if keepMystery then out.purple = purple end
    return out
end

-- the floor's global exams: 3x { type, red, gold, lessThan }
function M.exams(level, hadPurple)
    local g_red, g_gold = Levels.exam1_gauge(level)
    local e2_type, e2_red, e2_gold, e2_less = Levels.exam2(level)
    local bad_red, bad_gold = Levels.exam3_base(level)
    if hadPurple then
        bad_red  = math.floor(bad_red  * M.PURPLE_BAD_SCALE)
        bad_gold = math.floor(bad_gold * M.PURPLE_BAD_SCALE)
    end
    return {
        { "g", g_red, g_gold, false },
        { e2_type, e2_red, e2_gold, e2_less },
        { "jb", bad_red, bad_gold, true },
    }
end

-- sets up DANBUILDER for the floor (roll nil rolls now); the caller then mounts it
-- opts: genre (default the "title" string), genreColor ({ r, g, b })
-- returns false when no song could be added
function M.buildDan(level, roll, opts)
    opts = opts or {}
    roll = roll or M.rollFloor(level)
    local tc = Levels.level_tick_color(level)
    local gc = opts.genreColor or M.GENRE_COLOR

    DANBUILDER:Clear()
    DANBUILDER:SetTitle(Levels.level_name(level))
    DANBUILDER:SetDanTick(Levels.level_tick(level))
    DANBUILDER:SetDanTickColor(tc[1], tc[2], tc[3])
    DANBUILDER:SetGenreTitle(opts.genre or Texts.get("title", "Pagoda of the Unknown"))
    DANBUILDER:SetGenreColor(gc[1], gc[2], gc[3])

    local added_count = 0
    for _, item in ipairs(roll.songs or {}) do
        DANBUILDER:AddSong(item.node, item.entry.diff)
        added_count = added_count + 1
    end
    if roll.purple ~= nil then
        DANBUILDER:AddSong(roll.purple.node, roll.purple.entry.diff)
        added_count = added_count + 1
    end
    if added_count == 0 then return false end

    for i, e in ipairs(M.exams(level, roll.purple ~= nil)) do
        DANBUILDER:SetGlobalExam(i, e[1], e[2], e[3], e[4])
    end
    return true
end

return M
