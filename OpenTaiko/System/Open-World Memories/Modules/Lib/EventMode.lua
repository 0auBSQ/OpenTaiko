---@diagnostic disable: undefined-global, undefined-field, need-check-nil
-- Event Mode (kiosk) settings and session
-- countdown hud: event_timer ROActivity

local EM = {}

EM.DIFF_SECONDS = 30

-- theme settings (ThemeSettings.json, Event Mode section); event_mode resets at every launch
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

-- plays per session (0 = no limit; always 1 in the dojo)
function EM.playCount() return math.max(0, math.min(9, math.floor(tonumber(setting("event_play_count", "3")) or 3))) end
function EM.unlimited() return EM.playCount() == 0 end
function EM.canLeave() return not EM.on() or EM.unlimited() end

-- song select countdown (0 = none)
function EM.selectSeconds()
    local s = math.floor(tonumber(setting("event_select_seconds", "100")) or 100)
    if s < 20 then return 0 end
    return math.min(300, s)
end

-- event_plays_done: hidden session setting, shared across stage VMs
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

-- true when the session is over (thank-you screen next)
function EM.registerPlay(kind)
    local done = EM.playsDone() + 1
    setSetting("event_plays_done", done)
    local limit = limitFor(kind)
    return limit ~= nil and done >= limit
end

-- all players' mods and song speed back to defaults
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
            CONFIG:SetSpecialMod(p, 0)
        end
        CONFIG.SongSpeed = 20
    end)
end

-- ROActivity result: the string, or the first slot of the engine array
function EM.signal(res)
    if type(res) == "string" then return res end
    if res == nil then return nil end
    local ok, v = pcall(function() return res[0] end)
    return ok and v or nil
end

return EM
