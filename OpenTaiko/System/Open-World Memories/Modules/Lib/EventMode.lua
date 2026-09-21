---@diagnostic disable: undefined-global, undefined-field, need-check-nil
-- EventMode.lua — the event (kiosk) mode shared by the title, the song selects and the dojo.
--
-- The switch and its numbers are theme settings (ThemeSettings.json, section Event Mode): event_mode is a
-- session setting (off again at every launch), event_play_count the plays a session allows (0 = no limit,
-- always 1 in the dojo), event_select_seconds the time a player has to pick a song (0 = no limit). The plays
-- already done live in the hidden session setting event_plays_done, so every stage (each in its own Lua VM)
-- reads the same count through THEME.
--
--   EM.on()                     the switch
--   EM.unlimited()              no play limit
--   EM.canLeave()               a menu may go back to the title (only without a play limit)
--   EM.playsLeft(kind)          plays still allowed for "song" or "dan"; nil without a limit
--   EM.registerPlay(kind)       one play done; true when the session is over (the thank-you screen follows)
--   EM.resetSession()           a new session: no play done yet
--   EM.resetMods()              every player's mods and the song speed back to their defaults
--   EM.newTimer(seconds)        a countdown: :update(dt) -> true once when it runs out; .remaining; :restart(s)
--   EM.drawHud(seconds, plays)  the clock (black, red at 10 s or under) and the plays left, top-left
--   EM.selectSeconds()          the song select countdown (0 = none), EM.DIFF_SECONDS the difficulty one

local EM = {}

EM.DIFF_SECONDS = 30
local RED_UNDER = 10
local HUD_X, HUD_Y = 24, 16
local PILL_W, PILL_H = 236, 64
local LINE_H = 30

local function setting(id, fallback)
    local ok, v = pcall(function() return THEME:GetThemeSetting(id) end)
    if ok and type(v) == "string" and v ~= "" then return v end
    return fallback
end

local function setSetting(id, value)
    pcall(function() THEME:SetThemeSetting(id, tostring(value)) end)
end

local function tr(key, fallback)
    local ok, s = pcall(function() return THEME:GetSkinString(key) end)
    if ok and type(s) == "string" and s ~= "" and s:sub(1, 1) ~= "[" then return s end
    return fallback
end

function EM.on()
    local v = setting("event_mode", "0")
    return v == "1" or v == "true"
end

function EM.playCount() return math.max(0, math.min(9, math.floor(tonumber(setting("event_play_count", "3")) or 3))) end
function EM.unlimited() return EM.playCount() == 0 end
function EM.canLeave() return not EM.on() or EM.unlimited() end

function EM.selectSeconds()
    local s = math.floor(tonumber(setting("event_select_seconds", "100")) or 100)
    if s < 20 then return 0 end
    return math.min(300, s)
end

function EM.playsDone() return math.max(0, math.floor(tonumber(setting("event_plays_done", "0")) or 0)) end

-- the Pagoda stays available in Event Mode unless the skin's switch says otherwise
function EM.pagodaEnabled()
    local v = setting("event_pagoda", "1")
    return not (v == "0" or v == "false")
end
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

-- ── the countdown ──────────────────────────────────────────────────────────────────────────────
local Timer = {}
Timer.__index = Timer

function EM.newTimer(seconds)
    return setmetatable({ remaining = seconds, active = seconds > 0, fired = false }, Timer)
end

function Timer:restart(seconds)
    self.remaining, self.active, self.fired = seconds, seconds > 0, false
end

function Timer:stop() self.active = false end

-- true on the one frame the countdown reaches zero; each of the last ten seconds ticks (the shared
-- EventTick sound the boot registers)
function Timer:update(dt)
    if not self.active or self.fired then return false end
    local before = self:seconds()
    self.remaining = math.max(0, self.remaining - dt)
    local now = self:seconds()
    if now ~= before and now >= 1 and now <= RED_UNDER then
        pcall(function() SHARED:GetSharedSound("EventTick"):Play() end)
    end
    if self.remaining <= 0 then self.fired = true; return true end
    return false
end

function Timer:seconds()
    if not self.active then return nil end
    return math.ceil(self.remaining - 0.0001)
end

-- ── the hud ────────────────────────────────────────────────────────────────────────────────────
local pills, gfontBig, gfontSmall, colBlack, colRed, colNone = {}, nil, nil, nil, nil, nil

-- a cream pill of the wanted height, baked once per height (one line, two lines)
local function pillFor(h)
    local cv = pills[h]
    if cv ~= nil then return cv end
    cv = CANVAS:CreateCanvas(PILL_W, h)
    local r = math.floor(PILL_H / 2)
    cv:FillRect(r, 0, PILL_W - 2 * r, h, 250, 242, 222, 235)
    cv:FillRect(0, r, PILL_W, h - 2 * r, 250, 242, 222, 235)
    cv:FillCircle(r, r, r, 250, 242, 222, 235)
    cv:FillCircle(PILL_W - r - 1, r, r, 250, 242, 222, 235)
    cv:FillCircle(r, h - r - 1, r, 250, 242, 222, 235)
    cv:FillCircle(PILL_W - r - 1, h - r - 1, r, 250, 242, 222, 235)
    cv:Upload()
    pills[h] = cv
    return cv
end

local function ensureArt()
    if gfontBig ~= nil then return end
    gfontBig = TEXT:CreateGlyphCached(40)
    gfontSmall = TEXT:CreateGlyphCached(22)
    colBlack = COLOR:CreateColorFromRGBA(20, 18, 24, 255)
    colRed = COLOR:CreateColorFromRGBA(214, 40, 40, 255)
    colNone = COLOR:CreateColorFromRGBA(0, 0, 0, 0)
end

-- a glyph line anchored at its middle sits its ink high by half the box's bottom padding
local function nudge(gf) return math.floor((gf.BoxHeight - math.ceil(gf.LineHeight)) / 2) - 1 end

-- seconds: the countdown's seconds, nil when there is none; plays: plays left, nil when unlimited
function EM.drawHud(seconds, plays)
    if seconds == nil and plays == nil then return end
    ensureArt()
    local lines = (seconds ~= nil and 1 or 0) + (plays ~= nil and 1 or 0)
    local h = PILL_H + (lines - 1) * LINE_H
    pillFor(h):DrawAtAnchor(HUD_X, HUD_Y, "topleft")
    local cx, y = HUD_X + PILL_W / 2, HUD_Y + PILL_H / 2
    if seconds ~= nil then
        gfontBig:Draw(tostring(seconds), cx, y + nudge(gfontBig), seconds <= RED_UNDER and colRed or colBlack, colNone, 1, 1, PILL_W - 40, "center")
        y = y + LINE_H + 4
    end
    if plays ~= nil then
        local text = string.format(tr("EVENT_PLAYS_LEFT", "Plays left: %d"), plays)
        gfontSmall:Draw(text, cx, y + nudge(gfontSmall), colBlack, colNone, 1, 1, PILL_W - 30, "center")
    end
end

function EM.dispose()
    for h, cv in pairs(pills) do pcall(function() cv:Dispose() end) ; pills[h] = nil end
    for _, f in ipairs({ gfontBig, gfontSmall }) do if f ~= nil then pcall(function() f:Dispose() end) end end
    gfontBig, gfontSmall = nil, nil
end

return EM
