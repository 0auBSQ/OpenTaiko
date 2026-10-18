-- Pagoda.Run (optional): the rules of a challenge run and of practice, without any screen. A challenge run
-- starts on a checkpoint, climbs one floor per clear and ends on its first loss; its tower shows every
-- floor below the start cleared, then each floor it clears. Practice plays any floor up to the highest a
-- run reached (at least 11), and saves nothing.
--   local run = Run.new(start)          -- start: one of Run.startOptions()
--   run:rollFloor()                     -- the floor's songs (run.roll), shown before the play
--   run:enter()                         -- the run reached the floor: practice opens it
--   run:buildDan(opts)                  -- DANBUILDER set up for the floor; mount it, play
--   run:clear() / run:fail()            -- after the play

local Progress = require("Pagoda.Progress")
local Floor    = require("Pagoda.Floor")
local Rewards  = require("Pagoda.Rewards")

local M = {}

-- the starting floors: 道一(1) / 道六(6) / 扉一(11) always; 扉六(16) after clearing 扉五(15); 穹天1(21) after 扉十(20)
M.STARTS = { { level = 1 }, { level = 6 }, { level = 11 }, { level = 16, after = 15 }, { level = 21, after = 20 } }
M.PRACTICE_MIN_TOP = 11

-- the challenge's starting floors open on this save
function M.startOptions()
    local highest = Progress.highest_level()
    local opts = {}
    for _, s in ipairs(M.STARTS) do
        if s.after == nil or highest >= s.after then opts[#opts + 1] = s.level end
    end
    return opts
end

-- the floors practice offers: 1 up to the highest one a challenge run reached (at least 11)
function M.practiceLevels()
    local top = math.max(Progress.highest_reached(), M.PRACTICE_MIN_TOP)
    local out = {}
    for lv = 1, top do out[#out + 1] = lv end
    return out
end

-- sets DANBUILDER up for a practice play of level (a fresh roll); false when no song could be added
function M.practiceDan(level, opts)
    return Floor.buildDan(level, Floor.rollFloor(level), opts)
end

local Run = {}
Run.__index = Run

-- a challenge run from the floor start; songList (optional) is the list its songs come from
function M.new(start, songList)
    return setmetatable({ start = start, level = start, tower = start - 1, roll = nil, songList = songList,
                          over = false }, Run)
end

-- rolls the current floor's songs (Pagoda.Floor.rollFloor); returns the roll
function Run:rollFloor(avoid)
    self.roll = Floor.rollFloor(self.level, avoid, self.songList)
    return self.roll
end

-- the current floor's songs rolled again: other songs where the pools allow; keepMystery keeps its
-- purple song
function Run:reroll(keepMystery)
    self.roll = Floor.rerollFloor(self.level, self.roll, keepMystery, self.songList)
    return self.roll
end

-- the run reached its current floor: practice opens it
function Run:enter()
    Progress.set_highest_reached(self.level)
end

-- sets DANBUILDER up for the current floor with its roll (rolled now when there is none); false when no
-- song could be added. The roll is used up either way.
function Run:buildDan(opts)
    local roll = self.roll or Floor.rollFloor(self.level, nil, self.songList)
    self.roll = nil
    return Floor.buildDan(self.level, roll, opts)
end

-- the current floor was cleared: its rewards are granted and saved, the highest level and the tower move
-- up, the run climbs to the next floor. Returns { level, rewards (Rewards.grant), shrouded (the floor sat
-- above the tower) }
function Run:clear()
    local level = self.level
    local got = Rewards.grant(level)
    local prev = self.tower
    Progress.set_highest_level(level)
    self.tower = math.max(self.tower, level)
    self.level = level + 1
    self.roll = nil
    return { level = level, rewards = got, shrouded = level > prev }
end

-- the run was lost on its current floor: remembered per floor, and the play counted
function Run:fail()
    Progress.mark_lost(self.level)
    Progress.count_play()
    self.over = true
end

return M
