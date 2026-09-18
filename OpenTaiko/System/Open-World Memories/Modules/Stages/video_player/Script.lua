---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local
-- video_player (debug stage)
-- ------------------------------------------------------------------------------------------------------
-- The boot intro in a small player, to try the audio-track handling: play/pause, stop, restart, a timeline
-- that can be clicked and dragged, a speed dropdown and a volume slider. The video plays its own audio
-- track and is clocked by it, so every control below acts on picture and sound together.

local PopUI = require("PopUI")

local VIDEO_PATH = "../_boot/Videos/intro.mp4"
local SPEEDS = { 0.5, 0.75, 1, 1.25, 1.5, 2 }
local SPEED_LABELS = { "0.50x", "0.75x", "1.00x", "1.25x", "1.50x", "2.00x" }

-- layout (skin 1920x1080): the picture keeps its 16:9 shape above a control strip. Every width below is
-- measured from its text, so nothing overlaps whatever the font renders.
local VIEW_X, VIEW_Y, VIEW_W, VIEW_H = 192, 40, 1536, 864
local TIMELINE_Y, TIMELINE_H = 930, 40
local ROW_Y, ROW_H = 990, 56
local ROW_CY = ROW_Y + ROW_H / 2
local GAP = 16               -- between controls
local BUTTON_PAD = 40        -- text to button edge, both sides together
local MENU_ROW_H = 56
local READOUT_SIZE = 26
local HINT_Y = 1052

local ui, video
local durationMs = 0
local playing, stopped = false, false   -- the transport as this stage last set it
local timeline, playBtn, speedBtn, speedMenu, volume
local readoutX, readoutRight, volumeLabelCX = 0, 0, 0
local scrubTarget, scrubSeekAt = nil, 0
local speedIndex = 3

local function fmt(ms)
    local s = math.max(0, math.floor((ms or 0) / 1000))
    return string.format("%d:%02d", math.floor(s / 60), s % 60)
end

local function setPlaying(p)
    playing = p
    playBtn:setText(p and "Pause" or "Play")
end

local function play()
    if stopped or video:IsFinished() then
        stopped = false
        video:Start()   -- from the top once stopped or ended
    else
        video:Resume()
    end
    setPlaying(true)
end

local function pause()
    video:Pause()
    setPlaying(false)
end

local function stop()
    video:Stop()
    stopped = true
    setPlaying(false)
end

local function restart()
    if stopped then return play() end
    video:Reset()   -- back to 0; paused stays paused and shows the first frame
end

-- a seek never changes whether the video plays: paused stays paused, showing the frame at the target
local function seekTo(ms)
    if stopped then
        stopped = false
        video:Start()
        video:Pause()
        setPlaying(false)
    end
    video:SetTimestampMs(ms)
end

local function setSpeedIndex(i)
    speedIndex = i
    video:SetSpeed(SPEEDS[i])
    speedBtn:setText("Speed " .. SPEED_LABELS[i])
    speedMenu.visible = false
    speedMenu:setEnabled(false)
end

local function toggleSpeedMenu()
    local open = not speedMenu.visible
    speedMenu.visible = open
    speedMenu:setEnabled(open)
end

-- a button as wide as the widest text it will ever show
local function buttonWidth(...)
    local w = 0
    for _, t in ipairs({ ... }) do w = math.max(w, ui:measureText(ui.theme.font.button, t)) end
    return math.ceil(w) + BUTTON_PAD
end

function onStart()
    ui = PopUI.new{ bg = false }   -- the default sounds are the shared menu ones

    -- the timeline: the slider's range becomes the duration once the decoder reports it
    timeline = ui:slider{ x = VIEW_X, y = TIMELINE_Y, w = VIEW_W, h = TIMELINE_H, min = 0, max = 1000, step = 1, value = 0, showValue = false,
        onChange = function(v) scrubTarget = v ; return true end }   -- true: no step sound while scrubbing

    -- left group, laid out left to right from measured widths
    local x = VIEW_X
    local function place(w)
        local px = x
        x = x + w + GAP
        return px
    end
    local w = buttonWidth("Play", "Pause")
    playBtn = ui:button{ text = "Play", x = place(w), y = ROW_Y, w = w, h = ROW_H, accent = true, onClick = function() if playing then pause() else play() end end }
    w = buttonWidth("Stop");    ui:button{ text = "Stop",    x = place(w), y = ROW_Y, w = w, h = ROW_H, onClick = stop }
    w = buttonWidth("Restart"); ui:button{ text = "Restart", x = place(w), y = ROW_Y, w = w, h = ROW_H, onClick = restart }
    w = buttonWidth("-10s");    ui:button{ text = "-10s",    x = place(w), y = ROW_Y, w = w, h = ROW_H, onClick = function() seekTo(math.max(0, video:GetTimestampMs() - 10000)) end }
    w = buttonWidth("+10s");    ui:button{ text = "+10s",    x = place(w), y = ROW_Y, w = w, h = ROW_H, onClick = function() seekTo(math.min(durationMs, video:GetTimestampMs() + 10000)) end }
    readoutX = x + GAP   -- the time and state text starts here

    -- right group, laid out right to left
    local right = VIEW_X + VIEW_W
    local volumeW = 200
    volume = ui:slider{ x = right - volumeW, y = ROW_CY - 20, w = volumeW, h = 40, min = 0, max = 100, step = 5, value = 100,
        onChange = function(v) video:SetVolumePercent(v) end }
    right = right - volumeW - GAP
    local labelW = math.ceil(ui:measureText(ui.theme.font.label, "Volume"))
    volumeLabelCX = right - labelW / 2
    right = right - labelW - GAP * 2

    local speedW = buttonWidth("Speed 0.00x")
    speedBtn = ui:button{ text = "Speed " .. SPEED_LABELS[speedIndex], x = right - speedW, y = ROW_Y, w = speedW, h = ROW_H, onClick = toggleSpeedMenu }
    -- the speed dropdown: a list that opens above its button
    local items = {}
    for i, l in ipairs(SPEED_LABELS) do items[i] = { text = l, value = i } end
    local menuH = MENU_ROW_H * #SPEEDS
    speedMenu = ui:menu{ x = right - speedW, y = TIMELINE_Y - menuH - 12, w = speedW, h = menuH, rowHeight = MENU_ROW_H, items = items, selected = speedIndex,
        onSelect = function(i, item) setSpeedIndex(item.value or i) end }
    speedMenu.visible = false
    speedMenu:setEnabled(false)
    readoutRight = right - speedW - GAP * 2   -- the readout must end before the speed button
end

function activate()
    durationMs = 0
    stopped = false
    scrubTarget = nil
    speedIndex = 3
    speedBtn:setText("Speed " .. SPEED_LABELS[speedIndex])
    timeline:setValue(0, true)
    volume:setValue(100, true)
    video = VIDEO:CreateVideo(VIDEO_PATH, true)
    video:SetVolumePercent(100)
    video:Start()
    setPlaying(true)
end

function deactivate()
    if video ~= nil then
        video:Dispose()
        video = nil
    end
end

function update(ts)
    if video == nil then return end
    if durationMs <= 0 and video.Width > 0 then
        durationMs = video.DurationMs
        timeline.max = math.max(1, durationMs)
    end

    local r = ui:update(ts)
    if r == "cancel" then return Exit("stage", "_title") end

    -- a drag scrubs at most every 150 ms, and the release lands exactly
    if scrubTarget ~= nil and (not timeline.pressed or ts - scrubSeekAt > 150) then
        seekTo(scrubTarget)
        scrubSeekAt = ts
        scrubTarget = nil
    end
    if not timeline.pressed and scrubTarget == nil then
        timeline:setValue(video:GetTimestampMs(), true)
    end

    -- the end pauses the video itself, so a seek from there stays paused
    if playing and video:IsFinished() then pause() end
end

function draw()
    if video == nil then return end
    ui:rect(0, 0, 1920, 1080, 18, 20, 26, 255)
    ui:rect(VIEW_X - 4, VIEW_Y - 4, VIEW_W + 8, VIEW_H + 8, 40, 44, 54, 255)

    local tex = video.Texture
    if video.Width > 0 then
        tex:SetScale(VIEW_W / video.Width, VIEW_H / video.Height)
        tex:Draw(VIEW_X, VIEW_Y)
        tex:SetScale(1, 1)
    end

    ui:draw()

    -- texts sit on the row's centre line the way the button labels do
    local state = stopped and "stopped" or (playing and "playing" or "paused")
    local readout = fmt(video:GetTimestampMs()) .. " / " .. fmt(durationMs) .. "   " .. state
    ui:drawTextEx(READOUT_SIZE, readout, readoutX, ROW_CY + ui:textNudge(READOUT_SIZE), { 235, 238, 245, 255 }, nil, 1, 1, readoutRight - readoutX, "left")
    ui:drawTextEx(ui.theme.font.label, "Volume", volumeLabelCX, ROW_CY + ui:textNudge(ui.theme.font.label), { 235, 238, 245, 255 }, nil, 1, 1, 0, "center")
    ui:drawText(20, "Click or drag the timeline to seek.  Esc: back", VIEW_X, HINT_Y, { 160, 168, 182, 255 })
end

function afterSongEnum() end
function onDestroy() end
