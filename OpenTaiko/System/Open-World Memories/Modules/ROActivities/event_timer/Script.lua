---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local
-- event_timer ROActivity
-- ------------------------------------------------------------------------------------------------------
-- Event Mode's countdown and its hud: a floating stopwatch at the top-left, its hand on the seconds left and
-- the number in the box below (black, red from ten down, a tick on each of those last seconds), and the plays
-- still allowed in a cream pill beside it. The countdown is an engine counter running from the seconds given
-- to zero. Driven by the song select and the dojo:
--   activate(seconds, kind)  seconds = the countdown, 0 or nil for none (only the plays pill shows, in the
--                            stopwatch's corner); kind = "song" or "dan", what the plays-left line counts
--   update()                 every frame; returns "expired" once, on the frame the countdown reaches zero
--   draw()                   every frame, over everything
--   restart(seconds)         a new countdown (the difficulty choice), reached through Call
--   stop()                   no countdown, the plays line stays
--   deactivate()

local EM = require("EventMode")

-- positions, sizes and colours: Config/layout.json; timings: Config/animation.json
local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")
local WATCH, COUNTDOWN, PLAYS = Layout.stopwatch, Layout.countdown, Layout.plays
local BOB = Animation.stopwatch

local RED_FROM = COUNTDOWN.red_from_s   -- seconds: red digits and a tick each second from here down

local tex = {}
local pill = nil             -- the plays pill canvas
local gfontBig, gfontSmall
local colBlack, colRed, colNone
local counter = nil          -- the countdown: Value runs from the seconds to 0
local kind = "song"
local expiredSent = false
local lastSeconds = nil      -- for the tick on each new second of the last ten
local bobT = 0

local function tr(key, fallback)
    local ok, s = pcall(function() return THEME:GetSkinString(key) end)
    if ok and type(s) == "string" and s ~= "" and s:sub(1, 1) ~= "[" then return s end
    return fallback
end

local function pillCanvas()
    if pill ~= nil then return pill end
    local w, h = PLAYS.width, PLAYS.height
    local cv = CANVAS:CreateCanvas(w, h)
    local r = math.floor(h / 2)
    local c = PLAYS.color
    local cr, cg, cb, ca = c[1], c[2], c[3], c[4]
    cv:FillRect(r, 0, w - 2 * r, h, cr, cg, cb, ca)
    cv:FillCircle(r, r, r, cr, cg, cb, ca)
    cv:FillCircle(w - r - 1, r, r, cr, cg, cb, ca)
    cv:Upload()
    pill = cv
    return cv
end

-- a glyph line anchored at its middle sits its ink high by half the box's bottom padding
local function nudge(gf) return math.floor((gf.BoxHeight - math.ceil(gf.LineHeight)) / 2) + Layout.text_nudge end

-- the seconds left, whole, or nil without a countdown
local function seconds()
    if counter == nil then return nil end
    return math.max(0, math.ceil(counter.Value - 0.0001))
end

function onStart()
    gfontBig = TEXT:CreateGlyphCached(COUNTDOWN.font_size)
    gfontSmall = TEXT:CreateGlyphCached(PLAYS.font_size)
    colBlack = Config.color(COUNTDOWN.color)
    colRed = Config.color(COUNTDOWN.red_color)
    colNone = Config.color(Layout.edge_color)
    tex.clock = TEXTURE:CreateTexture("Textures/Clock.png")
    tex.hand = TEXTURE:CreateTexture("Textures/Hand.png")
    tex.box = TEXTURE:CreateTexture("Textures/Plate.png")
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
    bobT = bobT + fps.deltaTime
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

-- the stopwatch floats; its hand ticks clockwise from 12 to the seconds left, the number in the box below
local function drawStopwatch(secs)
    local cx = WATCH.x
    local cy = WATCH.y + BOB.bob_px * math.sin(2 * math.pi * bobT / BOB.bob_s)
    tex.clock:DrawAtAnchor(cx, cy, "center")
    tex.hand:SetRotation(-secs * 6)   -- SetRotation turns anticlockwise
    tex.hand:DrawAtAnchor(cx, cy, "center")
    tex.hand:SetRotation(0)
    local by = cy + WATCH.box_dy
    tex.box:DrawAtAnchor(cx, by, "center")
    gfontBig:Draw(tostring(secs), cx, by + nudge(gfontBig), secs <= RED_FROM and colRed or colBlack, colNone, 1, 1, WATCH.text_width, "center")
end

function draw()
    local secs = seconds()
    local plays = EM.playsLeft(kind)
    if secs ~= nil then drawStopwatch(secs) end
    if plays ~= nil then
        local x, y = PLAYS.x, PLAYS.y
        if secs == nil then x, y = PLAYS.solo_x, PLAYS.solo_y end
        pillCanvas():DrawAtAnchor(x, y, "topleft")
        local text = string.format(tr("EVENT_PLAYS_LEFT", "Plays left: %d"), plays)
        gfontSmall:Draw(text, x + PLAYS.width / 2, y + PLAYS.height / 2 + nudge(gfontSmall), colBlack, colNone, 1, 1, PLAYS.width - PLAYS.text_margin, "center")
    end
end

function afterSongEnum() end

function onDestroy()
    if pill ~= nil then pcall(function() pill:Dispose() end) ; pill = nil end
    for k, t in pairs(tex) do pcall(function() t:Dispose() end) ; tex[k] = nil end
    for _, f in ipairs({ gfontBig, gfontSmall }) do if f ~= nil then pcall(function() f:Dispose() end) end end
    gfontBig, gfontSmall, counter = nil, nil, nil
end
