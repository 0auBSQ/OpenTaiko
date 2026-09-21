---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local
-- event_timer ROActivity
-- ------------------------------------------------------------------------------------------------------
-- Event Mode's countdown and its hud: a cream pill at the top-left with the seconds left (black, red from
-- ten down, a tick on each of those last seconds) and, under it, the plays still allowed. The countdown is
-- an engine counter running from the seconds given to zero. Driven by the song select and the dojo:
--   activate(seconds, kind)  seconds = the countdown, 0 or nil for none (the pill then shows the plays
--                            only); kind = "song" or "dan", what the plays-left line counts
--   update()                 every frame; returns "expired" once, on the frame the countdown reaches zero
--   draw()                   every frame, over everything
--   restart(seconds)         a new countdown (the difficulty choice), reached through Call
--   stop()                   no countdown, the plays line stays
--   deactivate()

local EM = require("EventMode")

local RED_FROM = 10          -- seconds: red digits and a tick each second from here down
local HUD_X, HUD_Y = 24, 16
local PILL_W, PILL_H = 236, 64
local LINE_H = 30

local pills = {}             -- height -> a cream pill canvas of that height (one line, two lines)
local gfontBig, gfontSmall
local colBlack, colRed, colNone
local counter = nil          -- the countdown: Value runs from the seconds to 0
local kind = "song"
local expiredSent = false
local lastSeconds = nil      -- for the tick on each new second of the last ten

local function tr(key, fallback)
    local ok, s = pcall(function() return THEME:GetSkinString(key) end)
    if ok and type(s) == "string" and s ~= "" and s:sub(1, 1) ~= "[" then return s end
    return fallback
end

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

-- a glyph line anchored at its middle sits its ink high by half the box's bottom padding
local function nudge(gf) return math.floor((gf.BoxHeight - math.ceil(gf.LineHeight)) / 2) - 1 end

-- the seconds left, whole, or nil without a countdown
local function seconds()
    if counter == nil then return nil end
    return math.max(0, math.ceil(counter.Value - 0.0001))
end

function onStart()
    gfontBig = TEXT:CreateGlyphCached(40)
    gfontSmall = TEXT:CreateGlyphCached(22)
    colBlack = COLOR:CreateColorFromRGBA(20, 18, 24, 255)
    colRed = COLOR:CreateColorFromRGBA(214, 40, 40, 255)
    colNone = COLOR:CreateColorFromRGBA(0, 0, 0, 0)
end

function restart(secs)
    secs = tonumber(secs) or 0
    expiredSent = false
    lastSeconds = nil
    if secs <= 0 then counter = nil; return end
    counter = COUNTER:CreateCounterDuration(secs, 0, secs)
    counter:Start()
    lastSeconds = seconds()
end

function stop()
    counter = nil
end

function activate(secs, what)
    kind = (what == "dan") and "dan" or "song"
    restart(secs)
end

function deactivate()
    counter = nil
end

function update()
    if counter == nil then return nil end
    counter:Tick()
    local now = seconds()
    if now ~= lastSeconds then
        if now >= 1 and now <= RED_FROM then pcall(function() SHARED:GetSharedSound("EventTick"):Play() end) end
        lastSeconds = now
    end
    if now <= 0 and not expiredSent then
        expiredSent = true
        return "expired"
    end
    return nil
end

function draw()
    local secs = seconds()
    local plays = EM.playsLeft(kind)
    if secs == nil and plays == nil then return end
    local lines = (secs ~= nil and 1 or 0) + (plays ~= nil and 1 or 0)
    local h = PILL_H + (lines - 1) * LINE_H
    pillFor(h):DrawAtAnchor(HUD_X, HUD_Y, "topleft")
    local cx, y = HUD_X + PILL_W / 2, HUD_Y + PILL_H / 2
    if secs ~= nil then
        gfontBig:Draw(tostring(secs), cx, y + nudge(gfontBig), secs <= RED_FROM and colRed or colBlack, colNone, 1, 1, PILL_W - 40, "center")
        y = y + LINE_H + 4
    end
    if plays ~= nil then
        local text = string.format(tr("EVENT_PLAYS_LEFT", "Plays left: %d"), plays)
        gfontSmall:Draw(text, cx, y + nudge(gfontSmall), colBlack, colNone, 1, 1, PILL_W - 30, "center")
    end
end

function afterSongEnum() end

function onDestroy()
    for h, cv in pairs(pills) do pcall(function() cv:Dispose() end) ; pills[h] = nil end
    for _, f in ipairs({ gfontBig, gfontSmall }) do if f ~= nil then pcall(function() f:Dispose() end) end end
    gfontBig, gfontSmall, counter = nil, nil, nil
end
