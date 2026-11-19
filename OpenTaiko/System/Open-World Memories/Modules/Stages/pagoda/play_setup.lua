-- pagoda play changes outside the stage, and their undo

local save        = require("save")
local PlayStrings = require("PlayStrings")

local M = {}

local ROOF_LEVEL      = 21
local PLATE_STYLE_KEY = "danplate_style"
local BACKGROUND      = "play_bg"
local LEVEL_KEY       = "pagoda_play_level"
local MONSTER_KEY     = "pagoda_play_monster"     -- run's rolled monster on floors 1-4
local HIGHEST_KEY     = "pagoda_play_highest"
local PRACTICE_KEY    = "pagoda_play_practice"    -- "1": practice play, no monster
local LOADING_BG_KEY  = "song_loading_dan_bg"
local LOADING_BG      = "Stages/pagoda/Textures/loading/dan.jpg"

local _saved_mods = nil   -- P1 mod flags before the play

-- right before DANBUILDER:Mount(); monster: floor monster or nil
-- tower: highest cleared floor shown; practice: practice play
function M.begin(level, song_speed, monster, tower, practice)
    if _saved_mods == nil then
        _saved_mods = CONFIG:GetModFlags(0)
        save.hold_mods(_saved_mods)
    end
    CONFIG:SetRandomMod(0, CONFIG.RANDOM.Off)
    CONFIG:SetStealthMod(0, CONFIG.STEALTH.Off)
    CONFIG:SetFunMod(0, CONFIG.FUN.None)
    CONFIG:SetJusticeMod(0, CONFIG.JUSTICE.None)
    CONFIG:SetTimingZone(0, 2)                                  -- 2: Normal
    CONFIG:SetScrollSpeed(0, CONFIG.SCROLLSPEED:FromActual(1))
    CONFIG.SongSpeed = song_speed
    CONFIG:SetAutoStatus(0, false)
    SHARED:SetSharedString(PLATE_STYLE_KEY, level >= ROOF_LEVEL and "rainbow" or "")
    SHARED:SetSharedString(LEVEL_KEY, tostring(level))
    SHARED:SetSharedString(MONSTER_KEY, monster or "")
    SHARED:SetSharedString(HIGHEST_KEY, tostring(tower or save.highest_level()))
    SHARED:SetSharedString(PRACTICE_KEY, practice and "1" or "")
    SHARED:SetSharedString(LOADING_BG_KEY, LOADING_BG)
    SONGMOUNT:SetBackgrounds(BACKGROUND)
end

-- right after a successful DANBUILDER:Mount(): stamp strings with the chart
function M.mounted()
    PlayStrings.stamp()
end

-- after the play, a failed mount or a fresh entry: restore P1 mods and song speed
-- (save copy if the play never returned); drops plate style and background strings
function M.finish()
    local flags = _saved_mods or save.held_mods()
    if flags ~= nil then CONFIG:SetModFlags(0, flags) end
    _saved_mods = nil
    save.drop_held_mods()
    SHARED:SetSharedString(PLATE_STYLE_KEY, "")
    SHARED:SetSharedString(LEVEL_KEY, "")
    SHARED:SetSharedString(MONSTER_KEY, "")
    SHARED:SetSharedString(HIGHEST_KEY, "")
    SHARED:SetSharedString(PRACTICE_KEY, "")
    SHARED:SetSharedString(LOADING_BG_KEY, "")
    PlayStrings.clear()
    SONGMOUNT:SetBackgrounds()
end

return M
