---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local
-- event_thanks — the screen that closes an Event Mode session once its last allowed play is done.
-- "Thank you for playing!" pops in over a dark ground under a shower of confetti, a line below it fades
-- in, and after a few seconds (or a press of Decide) everything fades out and the title screen returns,
-- Event Mode still on for the next player. The plays counter starts over as this screen leaves.

local Easing = require("Easing")
local NavInput = require("NavInput")
local EM = require("EventMode")

local FAREWELL = "Sounds/Thanks.ogg" -- a soft "win" fanfare on a grand piano (CC0, tools/build_event_sfx.py), 3.8 s
local HOLD_SEC = 4.5          -- on screen before it leaves by itself
local SKIP_AFTER_SEC = 1.0    -- Decide leaves after this
local FADE_SEC = 0.6
local CONFETTI = 90
local COLORS = { { 255, 92, 120 }, { 255, 200, 70 }, { 90, 200, 255 }, { 120, 230, 140 }, { 200, 130, 255 } }

local fill, piece, snd
local gfontTitle, gfontSub
local colWhite, colGold, colOutline
local t, fading, fadeT = 0, false, 0
local confetti = {}

local function tr(key, fallback)
    local ok, s = pcall(function() return THEME:GetSkinString(key) end)
    if ok and type(s) == "string" and s ~= "" and s:sub(1, 1) ~= "[" then return s end
    return fallback
end

local function nudge(gf) return math.floor((gf.BoxHeight - math.ceil(gf.LineHeight)) / 2) - 1 end

local function spawn(i, fromTop)
    local c = COLORS[(i % #COLORS) + 1]
    confetti[i] = {
        x = math.random() * 1920, y = fromTop and (-40 - math.random() * 400) or (math.random() * 1080),
        vy = 120 + math.random() * 160, sway = 30 + math.random() * 50, phase = math.random() * 6.28,
        spin = (math.random() < 0.5 and -1 or 1) * (2 + math.random() * 4), rot = math.random() * 360,
        size = 0.7 + math.random() * 0.8, r = c[1], g = c[2], b = c[3],
    }
end

function onStart()
    fill = CANVAS:CreateCanvas(16, 16)
    fill:Clear(255, 255, 255, 255)
    fill:Upload()
    piece = CANVAS:CreateCanvas(16, 24)
    piece:Clear(255, 255, 255, 255)
    piece:Upload()
    gfontTitle = TEXT:CreateGlyphCached(84)
    gfontSub = TEXT:CreateGlyphCached(36)
    colWhite = COLOR:CreateColorFromRGBA(255, 255, 255, 255)
    colGold = COLOR:CreateColorFromRGBA(255, 220, 120, 255)
    colOutline = COLOR:CreateColorFromRGBA(40, 24, 60, 255)
    pcall(function() snd = SOUND:CreateSFX(FAREWELL) end)
end

function activate()
    t, fading, fadeT = 0, false, 0
    confetti = {}
    for i = 1, CONFETTI do spawn(i, true) end
    if snd ~= nil then pcall(function() snd:Play() end) end
end

function deactivate()
    EM.resetSession()   -- the next player starts a fresh count
end

function update(ts)
    local dt = math.min(0.1, fps.deltaTime)
    t = t + dt
    for i, c in ipairs(confetti) do
        c.y = c.y + c.vy * dt
        c.x = c.x + math.sin(t * 2 + c.phase) * c.sway * dt
        c.rot = c.rot + c.spin * 60 * dt
        if c.y > 1120 then spawn(i, true) end
    end
    if fading then
        fadeT = fadeT + dt
        if fadeT >= FADE_SEC then return Exit("title", nil) end
        return
    end
    if t >= HOLD_SEC or (t >= SKIP_AFTER_SEC and NavInput.decide()) then fading = true end
end

local function drawFill(r, g, b, a)
    fill:SetScale(1920 / 16, 1080 / 16)
    fill:SetColor(r / 255, g / 255, b / 255)
    fill:SetOpacity(a)
    fill:Draw(0, 0)
    fill:SetScale(1, 1) ; fill:SetOpacity(1) ; fill:SetColor(1, 1, 1)
end

function draw()
    local fade = fading and (1 - Easing.span(fadeT, 0, FADE_SEC)) or 1
    drawFill(28, 18, 44, 1)
    for _, c in ipairs(confetti) do
        piece:SetColor(c.r / 255, c.g / 255, c.b / 255)
        piece:SetOpacity(0.9 * fade)
        piece:SetScale(c.size * math.abs(math.cos(math.rad(c.rot))), c.size)
        piece:SetRotation(c.rot * 0.35)
        piece:DrawAtAnchor(c.x, c.y, "center")
    end
    piece:SetColor(1, 1, 1) ; piece:SetOpacity(1) ; piece:SetScale(1, 1) ; piece:SetRotation(0)

    -- the title pops in with an overshoot, the line under it fades in a beat later
    local pop = Easing.outBack(Easing.span(t, 0.15, 0.6), 1.4)
    if pop > 0 then
        gfontTitle:Draw(tr("EVENT_THANKS", "Thank you for playing!"), 960, 470 + nudge(gfontTitle) * pop, colGold, colOutline,
            fade * math.min(1, pop * 2), pop, 1700 * pop, "center")
    end
    local sub = Easing.outQuad(Easing.span(t, 0.9, 0.5))
    if sub > 0 then
        gfontSub:Draw(tr("EVENT_THANKS_SUB", "See you next time!"), 960, 580 + nudge(gfontSub) + (1 - sub) * 20, colWhite, colOutline,
            fade * sub, 1, 1400, "center")
    end
    if fading then drawFill(0, 0, 0, 1 - fade) end
end

function afterSongEnum() end

function onDestroy()
    for _, cv in ipairs({ fill, piece }) do if cv ~= nil then pcall(function() cv:Dispose() end) end end
    for _, f in ipairs({ gfontTitle, gfontSub }) do if f ~= nil then pcall(function() f:Dispose() end) end end
    if snd ~= nil then pcall(function() snd:Dispose() end) end
    EM.dispose()
end
