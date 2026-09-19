---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local
-- video_player ROActivity
-- ------------------------------------------------------------------------------------------------------
-- A video player: the picture in a 16:9 view above a control strip with icon buttons (restart, 10 s back,
-- play/pause, 10 s ahead, stop), a timeline that can be clicked and dragged, a speed dropdown, a mute
-- button, a volume slider and a close button. The video plays its own audio track and is clocked by it,
-- so every control acts on picture and sound together. Driven by any stage: activate(absolutePath), then
-- update() and draw() every frame while IsActive; it deactivates itself when closed (the X button, or
-- cancel on a keyboard or pad; both play the cancel sound). deactivate() frees the video.
-- Everything works without a mouse: focus starts on play/pause, pad left/right or the arrow keys walk the
-- controls, Decide on a slider takes it and left/right then step it (5 s on the timeline).

local PopUI = require("PopUI")

local SPEEDS = { 0.5, 0.75, 1, 1.25, 1.5, 2 }
local SPEED_LABELS = { "0.50x", "0.75x", "1.00x", "1.25x", "1.50x", "2.00x" }

-- layout (skin 1920x1080). Every width is measured from what it shows, so nothing overlaps.
local VIEW_X, VIEW_Y, VIEW_W, VIEW_H = 192, 40, 1536, 864
local TIMELINE_Y, TIMELINE_H = 930, 40
local ROW_Y, ROW_H = 990, 56
local ROW_CY = ROW_Y + ROW_H / 2
local GAP = 14
local ICON_BTN_W = 92
local CLOSE_BTN_W = 64
local MENU_ROW_H = 56
local READOUT_SIZE = 26
local FILL = 16
local TIMELINE_KEY_STEP = 5000
local CLOSE_STYLE = { colors = { primary = { 236, 76, 76 }, primary2 = { 196, 40, 48 } } }   -- a red face for the X

local ui, video
local fill
local durationMs = 0
local playing, stopped = false, false   -- the transport as this player last set it
local timeline, playBtn, speedBtn, speedMenu, volume, muteBtn
local readoutX, readoutRight = 0, 0
local muted, volumeBeforeMute = false, 100
local scrubTarget, scrubSeekAt = nil, 0
local speedIndex = 3
local closing = false

local function sfx(name)
    pcall(function() SHARED:GetSharedSound(name):Play() end)
end

local function fmt(ms)
    local s = math.max(0, math.floor((ms or 0) / 1000))
    return string.format("%d:%02d", math.floor(s / 60), s % 60)
end

local function focusWidget(w)
    for i, f in ipairs(ui.focusables) do
        if f == w then ui:_setFocusIndex(i); return end
    end
end

local function setPlaying(p)
    playing = p
    playBtn:setIcon(p and "pause" or "play")
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

local function closeSpeedMenu()
    if not speedMenu.visible then return end
    speedMenu.visible = false
    speedMenu:setEnabled(false)
    focusWidget(speedBtn)
end

local function setSpeedIndex(i)
    speedIndex = i
    video:SetSpeed(SPEEDS[i])
    speedBtn:setText(SPEED_LABELS[i])
    closeSpeedMenu()
end

local function toggleSpeedMenu()
    if speedMenu.visible then return closeSpeedMenu() end
    speedMenu.visible = true
    speedMenu:setEnabled(true)
    speedMenu:setSelected(speedIndex, false)
    focusWidget(speedMenu)   -- the keys land in the list at once
end

local function close()
    if closing then return end
    closing = true
    DEACTIVATE()
end

local function textButtonWidth(...)
    local w = 0
    for _, t in ipairs({ ... }) do w = math.max(w, ui:measureText(ui.theme.font.button, t)) end
    return math.ceil(w) + 40
end

local function fillScreen(alpha)
    if fill == nil or alpha <= 0 then return end
    fill:SetScale(1920 / FILL, 1080 / FILL)
    fill:SetOpacity(math.min(1, alpha))
    fill:SetColor(18 / 255, 20 / 255, 26 / 255)
    fill:DrawAtAnchor(960, 540, "center")
    fill:SetScale(1, 1) ; fill:SetOpacity(1) ; fill:SetColor(1, 1, 1)
end

function onStart()
    ui = PopUI.new{ bg = false }   -- its default sounds are the shared menu ones
    fill = CANVAS:CreateCanvas(FILL, FILL)
    fill:Clear(255, 255, 255, 255)
    fill:Upload()

    -- widgets are added in the order the keys and the pad walk them: the transport, then the right group,
    -- then the timeline

    -- transport, left to right: restart, 10 s back, play/pause, 10 s ahead, stop
    local x = VIEW_X
    local function place(w)
        local px = x
        x = x + w + GAP
        return px
    end
    ui:button{ icon = "restart", x = place(ICON_BTN_W), y = ROW_Y, w = ICON_BTN_W, h = ROW_H, onClick = restart }
    ui:button{ icon = "rewind",  x = place(ICON_BTN_W), y = ROW_Y, w = ICON_BTN_W, h = ROW_H, onClick = function() seekTo(math.max(0, video:GetTimestampMs() - 10000)) end }
    playBtn = ui:button{ icon = "play", x = place(ICON_BTN_W + 24), y = ROW_Y, w = ICON_BTN_W + 24, h = ROW_H, accent = true, onClick = function() if playing then pause() else play() end end }
    ui:button{ icon = "forward", x = place(ICON_BTN_W), y = ROW_Y, w = ICON_BTN_W, h = ROW_H, onClick = function() seekTo(math.min(durationMs, video:GetTimestampMs() + 10000)) end }
    ui:button{ icon = "stop",    x = place(ICON_BTN_W), y = ROW_Y, w = ICON_BTN_W, h = ROW_H, onClick = stop }
    readoutX = x + GAP   -- the time text starts here

    -- right group, laid out right to left: close, the volume slider, a mute button, the speed dropdown
    local right = VIEW_X + VIEW_W
    local closeX = right - CLOSE_BTN_W
    right = closeX - GAP * 2
    local volumeW = 200
    local volumeX = right - volumeW
    right = volumeX - GAP
    local muteX = right - ICON_BTN_W
    right = muteX - GAP * 2
    -- the dropdown's rows draw their text 28 px in with a width limit of w - 48, and that limit is on the
    -- text box (the ink plus its 50 px of padding): a row narrower than ink + 98 squishes its label
    local speedW = textButtonWidth(table.unpack(SPEED_LABELS))
    for _, l in ipairs(SPEED_LABELS) do speedW = math.max(speedW, math.ceil(ui:measureText(ui.theme.font.label, l)) + 100) end
    local speedX = right - speedW
    readoutRight = speedX - GAP * 2   -- the time text must end before the speed button

    speedBtn = ui:button{ text = SPEED_LABELS[speedIndex], x = speedX, y = ROW_Y, w = speedW, h = ROW_H, onClick = toggleSpeedMenu }
    muteBtn = ui:button{ icon = "volume", x = muteX, y = ROW_Y, w = ICON_BTN_W, h = ROW_H,
        onClick = function()
            muted = not muted
            if muted then volumeBeforeMute = volume.value; volume:setValue(0, true); video:SetVolumePercent(0)
            else volume:setValue(volumeBeforeMute, true); video:SetVolumePercent(volumeBeforeMute) end
            muteBtn:setIcon(muted and "muted" or "volume")
        end }
    volume = ui:slider{ x = volumeX, y = ROW_CY - 20, w = volumeW, h = 40, min = 0, max = 100, step = 5, value = 100,
        sfx = { cancel = false },   -- leaving the slider is not a cancel
        onChange = function(v)
            if v > 0 and muted then muted = false; muteBtn:setIcon("volume") end
            video:SetVolumePercent(v)
        end }
    -- the one control that sounds like a cancel: it is one
    ui:button{ icon = "close", x = closeX, y = ROW_Y, w = CLOSE_BTN_W, h = ROW_H, accent = true, style = CLOSE_STYLE,
               sfx = { click = "cancel" }, onClick = close }

    -- the timeline: the slider's range becomes the duration once the decoder reports it
    timeline = ui:slider{ x = VIEW_X, y = TIMELINE_Y, w = VIEW_W, h = TIMELINE_H, min = 0, max = 1000, step = 1, navStep = TIMELINE_KEY_STEP,
        value = 0, showValue = false, sfx = { cancel = false },
        onChange = function(v) scrubTarget = v ; return true end }   -- true: no step sound while scrubbing

    local items = {}
    for i, l in ipairs(SPEED_LABELS) do items[i] = { text = l, value = i } end
    local menuH = MENU_ROW_H * #SPEEDS
    speedMenu = ui:menu{ x = speedX, y = TIMELINE_Y - menuH - 12, w = speedW, h = menuH, rowHeight = MENU_ROW_H, items = items, selected = speedIndex,
        onSelect = function(i, item) setSpeedIndex(item.value or i) end }
    speedMenu.visible = false
    speedMenu:setEnabled(false)
end

function activate(path)
    durationMs = 0
    stopped, closing = false, false
    scrubTarget = nil
    speedIndex = 3
    speedBtn:setText(SPEED_LABELS[speedIndex])
    speedMenu.visible = false
    speedMenu:setEnabled(false)
    timeline:setValue(0, true)
    volume:setValue(100, true)
    muted, volumeBeforeMute = false, 100
    muteBtn:setIcon("volume")
    focusWidget(playBtn)
    video = VIDEO:CreateVideoFromAbsolutePath(path, true)
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
    -- a click on the X inside that update already deactivated this activity and freed the video
    if closing or video == nil then return end
    if r == "cancel" then
        if speedMenu.visible then
            closeSpeedMenu()
        else
            sfx("Cancel")
            close()
            return
        end
    end

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
    fillScreen(1)
    ui:rect(VIEW_X - 4, VIEW_Y - 4, VIEW_W + 8, VIEW_H + 8, 40, 44, 54, 255)

    -- the picture keeps its shape inside the view: one scale for both axes, centred, black around it
    ui:rect(VIEW_X, VIEW_Y, VIEW_W, VIEW_H, 0, 0, 0, 255)
    local tex = video.Texture
    if video.Width > 0 and video.Height > 0 then
        local s = math.min(VIEW_W / video.Width, VIEW_H / video.Height)
        local w, h = video.Width * s, video.Height * s
        tex:SetScale(s, s)
        tex:Draw(math.floor(VIEW_X + (VIEW_W - w) / 2), math.floor(VIEW_Y + (VIEW_H - h) / 2))
        tex:SetScale(1, 1)
    end

    ui:draw()

    -- the time sits on the row's centre line the way the button glyphs do
    ui:drawTextEx(READOUT_SIZE, fmt(video:GetTimestampMs()) .. " / " .. fmt(durationMs), readoutX, ROW_CY + ui:textNudge(READOUT_SIZE),
        { 235, 238, 245, 255 }, nil, 1, 1, readoutRight - readoutX, "left")
end

function afterSongEnum() end

function onDestroy()
    if video ~= nil then video:Dispose() end
    video = nil
    if fill ~= nil then pcall(function() fill:Dispose() end) end
end
