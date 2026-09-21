---@diagnostic disable: undefined-global, undefined-field, need-check-nil
-- EventMode.lua — the event (kiosk) mode's settings and session, shared by the title, the song selects and
-- the dojo. The countdown and its hud are the event_timer ROActivity.
--
-- The switch and its numbers are theme settings (ThemeSettings.json, section Event Mode): event_mode is a
-- session setting (off again at every launch), event_play_count the plays a session allows (0 = no limit,
-- always 1 in the dojo), event_select_seconds the time a player has to pick a song (0 = no limit),
-- event_pagoda whether the dojo offers the Pagoda. The plays already done live in the hidden session
-- setting event_plays_done, so every stage (each in its own Lua VM) reads the same count through THEME.
--
--   EM.on()                     the switch
--   EM.unlimited()              no play limit
--   EM.canLeave()               a menu may go back to the title (only without a play limit)
--   EM.pagodaEnabled()          the dojo offers the Pagoda
--   EM.selectSeconds()          the song select countdown (0 = none); EM.DIFF_SECONDS the difficulty one
--   EM.playsLeft(kind)          plays still allowed for "song" or "dan"; nil without a limit
--   EM.registerPlay(kind)       one play done; true when the session is over (the thank-you screen follows)
--   EM.resetSession()           a new session: no play done yet
--   EM.resetMods()              every player's mods and the song speed back to their defaults
--   EM.signal(res)              the string an ROActivity's Update or Call handed back

local EM = {}

EM.DIFF_SECONDS = 30

local function setting(id, fallback)
    local ok, v = pcall(function() return THEME:GetThemeSetting(id) end)
    if ok and type(v) == "string" and v ~= "" then return v end
    return fallback
end

local function setSetting(id, value)
    pcall(function() THEME:SetThemeSetting(id, tostring(value)) end)
end

local function flag(id, default)
    local v = setting(id, default)
    return v == "1" or v == "true"
end

function EM.on() return flag("event_mode", "0") end
function EM.pagodaEnabled() return flag("event_pagoda", "1") end

function EM.playCount() return math.max(0, math.min(9, math.floor(tonumber(setting("event_play_count", "3")) or 3))) end
function EM.unlimited() return EM.playCount() == 0 end
function EM.canLeave() return not EM.on() or EM.unlimited() end

function EM.selectSeconds()
    local s = math.floor(tonumber(setting("event_select_seconds", "100")) or 100)
    if s < 20 then return 0 end
    return math.min(300, s)
end

function EM.playsDone() return math.max(0, math.floor(tonumber(setting("event_plays_done", "0")) or 0)) end
function EM.resetSession() setSetting("event_plays_done", 0) end

local function limitFor(kind)
    if not EM.on() or EM.unlimited() then return nil end
    if kind == "dan" then return 1 end
    return EM.playCount()
end

function EM.playsLeft(kind)
    local limit = limitFor(kind)
    if limit == nil then return nil end
    return math.max(0, limit - EM.playsDone())
end

function EM.registerPlay(kind)
    local done = EM.playsDone() + 1
    setSetting("event_plays_done", done)
    local limit = limitFor(kind)
    return limit ~= nil and done >= limit
end

function EM.resetMods()
    pcall(function()
        for p = 0, 4 do
            CONFIG:SetScrollSpeed(p, 9)
            CONFIG:SetTimingZone(p, 2)
            CONFIG:SetJusticeMod(p, 0)
            CONFIG:SetStealthMod(p, 0)
            CONFIG:SetRandomMod(p, 0)
            CONFIG:SetFunMod(p, 0)
            CONFIG:SetGameType(p, 0)
            CONFIG:SetAutoStatus(p, false)
        end
        CONFIG.SongSpeed = 20
    end)
end

-- the value an ROActivity call handed back: the string itself, or the first slot of the engine's array
function EM.signal(res)
    if type(res) == "string" then return res end
    if res == nil then return nil end
    local ok, v = pcall(function() return res[0] end)
    return ok and v or nil
end

return EM
