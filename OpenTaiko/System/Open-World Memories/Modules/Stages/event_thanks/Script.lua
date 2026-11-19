---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local
-- event_thanks: closes an Event Mode session after its last play (exit_screen picture, own fanfare)
-- leaves after HOLD_SEC or Decide, back to the title; Event Mode stays on

local Easing = require("Easing")
local NavInput = require("NavInput")
local EM = require("EventMode")
-- the fade colour: Config/layout.json; timings: Config/animation.json
local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")

local FAREWELL = "Sounds/Thanks.ogg" -- fanfare, 3.8 s
local HOLD_SEC = Animation.hold_s              -- on screen before it leaves by itself
local SKIP_AFTER_SEC = Animation.skip_after_s  -- Decide leaves after this
local FADE_SEC = Animation.fade_s

local fill, snd, screen
local t, fading, fadeT = 0, false, 0

function onStart()
    fill = CANVAS:CreateCanvas(16, 16)
    local c = Layout.fade_color
    fill:Clear(c[1], c[2], c[3], c[4])
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
