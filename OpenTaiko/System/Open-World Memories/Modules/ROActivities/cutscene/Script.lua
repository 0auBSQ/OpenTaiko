---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local
-- cutscene ROActivity
-- ------------------------------------------------------------------------------------------------------
-- Plays cutscene videos one after another, each with its own audio track, and owns the pause popup: the
-- screen dims and a PopUI panel offers Resume and Skip, with the shared menu sounds. It knows nothing about
-- what the cutscene is for; the caller decides which files play and how the end looks. Driven by C#
-- (CStageCutScene for a song's intro and outro):
--   activate(paths, fadeOutSeconds)
--     paths = the video files, in order; fadeOutSeconds = a fade to black after the last one (0 = hand back at once)
--   update() every frame, returning "finished" once every video has played and the fade, if any, is over
--   draw() every frame; deactivate() frees the videos.
-- Esc (or a pad's cancel) and F1 open the popup; Skip ends the current video only.

local PopUI = require("PopUI")
local NavInput = require("NavInput")

local INPUT_GRACE_S = 0.15  -- after the popup opens, so the press that opened it does not act on it
local DIM = 0.5             -- how dark the picture goes behind the popup
local FILL = 16
local PANEL_W, PANEL_H, ROW_H = 560, 300, 72

local ui, menu
local paths, index
local fadeOutS              -- the fade to black after the last video, 0 for none
local video
local fill                  -- a small white canvas, scaled up for the letterbox, the dim and the fade
local finished, fadeT
local paused, popupAge
local lastTs

local function sfx(name)
    pcall(function() SHARED:GetSharedSound(name):Play() end)
end

local function openVideo(i)
    if video ~= nil then video:Dispose() end
    video = nil
    index = i
    if i > #paths then return end
    video = VIDEO:CreateVideoFromAbsolutePath(paths[i], true)
    video:Start()
end

-- every video has played: hand back at once, or after the fade to black the caller asked for
local function done()
    if fadeOutS > 0 then
        if fadeT == nil then fadeT = 0 end
        return nil
    end
    finished = true
    return "finished"
end

local function pause()
    paused, popupAge = true, 0
    if video ~= nil then video:Pause() end
    menu:setSelected(1, false)
    ui:focusStay()   -- the menu takes the keys at once
    sfx("Cancel")
end

local function resume()
    paused = false
    if video ~= nil then video:Resume() end
end

local function skip()
    paused = false
    if video ~= nil then video:Stop() end   -- finished: the next video (if any) follows
end

local function fillScreen(alpha)
    if fill == nil or alpha <= 0 then return end
    fill:SetScale(1920 / FILL, 1080 / FILL)
    fill:SetOpacity(math.min(1, alpha))
    fill:SetColor(0, 0, 0)
    fill:DrawAtAnchor(960, 540, "center")
    fill:SetScale(1, 1) ; fill:SetOpacity(1) ; fill:SetColor(1, 1, 1)
end

-- the popup, rebuilt on each activation so its strings follow the language
local function buildPopup()
    ui:disposeWidgets()
    ui:clear()
    local x, y = (1920 - PANEL_W) / 2, (1080 - PANEL_H) / 2
    local panel = ui:panel{ x = x, y = y, w = PANEL_W, h = PANEL_H, pad = 30, title = LANG:GetString("PAUSE_TITLE") }
    local cx, cy, cw = panel:content()
    menu = ui:menu{ x = cx, y = cy + 10, w = cw, h = ROW_H * 2, rowHeight = ROW_H, selected = 1,
        items = { LANG:GetString("PAUSE_RESUME"), LANG:GetString("PAUSE_SKIP") },
        onSelect = function(i) if i == 1 then resume() else skip() end end }
end

function onStart()
    ui = PopUI.new{ bg = false }   -- its default sounds are the shared menu ones
    fill = CANVAS:CreateCanvas(FILL, FILL)
    fill:Clear(255, 255, 255, 255)
    fill:Upload()
end

function activate(pathArray, fadeSeconds)
    fadeOutS = math.max(0, tonumber(fadeSeconds) or 0)
    paths = {}
    if pathArray ~= nil then
        for i = 0, pathArray.Length - 1 do paths[#paths + 1] = pathArray[i] end
    end
    buildPopup()
    finished, fadeT, paused, lastTs = false, nil, false, nil
    openVideo(1)
end

function deactivate()
    if video ~= nil then video:Dispose() end
    video = nil
    paths, paused, fadeT = {}, false, nil
end

function update(ts)
    local dt = 1 / 60
    if lastTs ~= nil and ts ~= nil then dt = math.max(0, math.min(0.1, (ts - lastTs) / 1000)) end
    lastTs = ts

    if finished then return "finished" end
    if fadeT ~= nil then
        fadeT = fadeT + dt
        if fadeT >= fadeOutS then
            finished = true
            return "finished"
        end
        return nil
    end
    if video == nil then return done() end

    if paused then
        popupAge = popupAge + dt
        if popupAge >= INPUT_GRACE_S then
            if ui:update(ts) == "cancel" then
                sfx("Cancel")
                resume()
            end
        end
        return nil
    end

    if NavInput.cancel() or INPUT:KeyboardPressed("F1") then
        pause()
        return nil
    end
    if video:IsFinished() then
        openVideo(index + 1)
        if video == nil then return done() end
    end
    return nil
end

function draw()
    fillScreen(1)
    if video ~= nil and video.Width > 0 then
        -- the picture fits the screen and keeps its shape
        local tex = video.Texture
        local s = math.min(1920 / video.Width, 1080 / video.Height)
        local w, h = video.Width * s, video.Height * s
        tex:SetScale(s, s)
        tex:Draw(math.floor((1920 - w) / 2), math.floor((1080 - h) / 2))
        tex:SetScale(1, 1)
    end
    if paused then
        fillScreen(DIM)
        ui:draw()
    end
    if fadeT ~= nil then fillScreen(fadeT / fadeOutS) end
end

function afterSongEnum() end

function onDestroy()
    if video ~= nil then video:Dispose() end
    video = nil
    if fill ~= nil then pcall(function() fill:Dispose() end) end
end
