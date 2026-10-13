---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local
-- event_thanks — the screen that closes an Event Mode session once its last allowed play is done.
-- It shows the goodbye picture of the exit_screen ROActivity, the one the game shows when it closes, with
-- its own short fanfare. After a few seconds (or a press of Decide) it fades to black and the title
-- screen returns, Event Mode still on for the next player. The plays counter starts over as this screen
-- leaves.

local Easing = require("Easing")
local NavInput = require("NavInput")
local EM = require("EventMode")

local FAREWELL = "Sounds/Thanks.ogg" -- a soft "win" fanfare on a grand piano (CC0, tools/build_event_sfx.py), 3.8 s
local HOLD_SEC = 4.5          -- on screen before it leaves by itself
local SKIP_AFTER_SEC = 1.0    -- Decide leaves after this
local FADE_SEC = 0.6

local fill, snd, screen
local t, fading, fadeT = 0, false, 0

function onStart()
    fill = CANVAS:CreateCanvas(16, 16)
    fill:Clear(0, 0, 0, 255)
    fill:Upload()
    pcall(function() snd = SOUND:CreateSFX(FAREWELL) end)
end

function activate()
    t, fading, fadeT = 0, false, 0
    screen = ROACTIVITY:GetROActivity("exit_screen")
    if screen ~= nil then screen:Activate() end
    if snd ~= nil then pcall(function() snd:Play() end) end
end

function deactivate()
    if screen ~= nil and screen.IsActive then screen:Deactivate() end
    screen = nil
    EM.resetSession()   -- the next player starts a fresh count
end

function update(ts)
    local dt = math.min(0.1, fps.deltaTime)
    t = t + dt
    if screen ~= nil then screen:Update() end
    if fading then
        fadeT = fadeT + dt
        if fadeT >= FADE_SEC then return Exit("title", nil) end
        return
    end
    if t >= HOLD_SEC or (t >= SKIP_AFTER_SEC and NavInput.decide()) then fading = true end
end

function draw()
    if screen ~= nil then screen:Draw() end
    if fading then
        fill:SetScale(1920 / 16, 1080 / 16)
        fill:SetOpacity(Easing.span(fadeT, 0, FADE_SEC))
        fill:Draw(0, 0)
        fill:SetScale(1, 1) ; fill:SetOpacity(1)
    end
end

function afterSongEnum() end

function onDestroy()
    if fill ~= nil then pcall(function() fill:Dispose() end) end
    if snd ~= nil then pcall(function() snd:Dispose() end) end
end
