---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local
-- cutscene ROActivity (videos in order, pause popup)
-- driven by CStageCutScene (song intro and outro)

local PopUI = require("PopUI")
local NavInput = require("NavInput")

-- layout: Config/layout.json
local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local POPUP = Layout.popup

local INPUT_GRACE_S = 0.15  -- s, ignore input after the popup opens
local DIM = Layout.dim_opacity
local FILL = 16
local PANEL_W, PANEL_H, ROW_H = POPUP.width, POPUP.height, POPUP.row_height
local SCREEN_W, SCREEN_H = Layout.screen.width, Layout.screen.height

local ui, menu
local paths, index
local fadeOutS              -- s, final fade (0 = none)
local video
local fill                  -- white canvas, scaled for fills
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

-- all videos done: hand back now or after the fade
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
    ui:focusStay()   -- menu takes keys at once
    sfx("Cancel")
end

local function resume()
    paused = false
    if video ~= nil then video:Resume() end
end

local function skip()
    paused = false
    if video ~= nil then video:Stop() end   -- next video follows
end

local function fillScreen(alpha)
    if fill == nil or alpha <= 0 then return end
    local c = Layout.backdrop_color
    fill:SetScale(SCREEN_W / FILL, SCREEN_H / FILL)
    fill:SetOpacity(math.min(1, alpha))
    fill:SetColor(c[1], c[2], c[3])
    fill:DrawAtAnchor(SCREEN_W / 2, SCREEN_H / 2, "center")
    fill:SetScale(1, 1) ; fill:SetOpacity(1) ; fill:SetColor(1, 1, 1)
end

-- rebuilt per activation (language)
local function buildPopup()
    ui:disposeWidgets()
    ui:clear()
    local x, y = (SCREEN_W - PANEL_W) / 2, (SCREEN_H - PANEL_H) / 2
    local panel = ui:panel{ x = x, y = y, w = PANEL_W, h = PANEL_H, pad = POPUP.padding, title = LANG:GetString("PAUSE_TITLE") }
    local cx, cy, cw = panel:content()
    menu = ui:menu{ x = cx, y = cy + POPUP.menu_y, w = cw, h = ROW_H * 2, rowHeight = ROW_H, selected = 1,
        items = { LANG:GetString("PAUSE_RESUME"), LANG:GetString("PAUSE_SKIP") },
        onSelect = function(i) if i == 1 then resume() else skip() end end }
end

function onStart()
    ui = PopUI.new{ bg = false }   -- default sounds: shared menu ones
    fill = CANVAS:CreateCanvas(FILL, FILL)
    fill:Clear(255, 255, 255, 255)
    fill:Upload()
end

-- pathArray: video files in order; fadeSeconds: fade to black after the last (0 = none)
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

-- returns "finished" after the last video and its fade
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
        -- fit, keep aspect
        local tex = video.Texture
        local s = math.min(SCREEN_W / video.Width, SCREEN_H / video.Height)
        local w, h = video.Width * s, video.Height * s
        tex:SetScale(s, s)
        tex:Draw(math.floor((SCREEN_W - w) / 2), math.floor((SCREEN_H - h) / 2))
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
