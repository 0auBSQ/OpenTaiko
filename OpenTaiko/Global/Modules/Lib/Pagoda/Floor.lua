-- Pagoda.Floor: a floor's songs and its dan. rollFloor picks the songs (a roll), buildDan hands a roll to
-- DANBUILDER with the floor's plate and exams; the caller then mounts it (DANBUILDER:Mount()).
-- A roll is { songs = { { color, node, entry }, ... }, purple = { node, entry } or nil, speed }: entry is
-- the pool's { id, diff }, node the song, speed the floor's song speed (a CONFIG.SONGSPEED value).

local Levels = require("Pagoda.Levels")
local Songs  = require("Pagoda.Songs")
local Texts  = require("Pagoda.Texts")

local M = {}

M.GENRE_COLOR = { 100, 150, 240 }   -- a lighter blue than the dan folders' #3e51c9
M.PURPLE_BAD_SCALE = 1.25           -- the bad-count exam is more lenient with a purple song

-- the floor's song speed (a CONFIG.SONGSPEED value); it cannot be chosen. It starts ramping at 穹天18
-- (level 38); below that it stays x1.
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

-- One blue, one green and one red song in a shuffled order, then a purple 4th song at the level's chance
-- (Levels.purple_prob), never a song already picked. avoid (optional): a set of song ids not to pick while
-- the pools have others. songList (optional): the list to take the songs from (Songs.get_song_list()).
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

-- the floor's songs rolled again by the same rules, other songs than roll's where the pools allow;
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

-- the floor's three global exams, as { type, red, gold, lessThan }: the gauge, the accuracy (道/扉) or Ok
-- count (穹天), and the bad count (more lenient with a purple song)
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

-- Sets DANBUILDER up for the floor: the plate (name, tick, colour), the genre, roll's songs and the exams.
-- roll nil rolls the floor now. opts (optional): genre (the genre title, the module's "title" string by
-- default) and genreColor ({ r, g, b }). Returns false when no song could be added; the caller mounts
-- the dan otherwise.
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
