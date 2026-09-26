---@diagnostic disable: undefined-global, undefined-field, lowercase-global, need-check-nil
-- myroom_door: My Room's door, between the title and My Room both ways.

FADE_OUT_SECONDS = 0.8
FADE_IN_SECONDS  = 0.9

local SCREEN_H = 1080
local OVERSCAN = 30          -- the art is 30 px bigger than the screen on each side, for the shake
local SLAM_AT = 0.72         -- part of the fade-out when the door hits the frame
local OPEN_SPAN = 0.92       -- part of the fade-in the swing takes

local tx_door = nil
local snd_close, snd_open = nil, nil
local slammed, creaked = false, false
local lastPhase = nil

local function easeIn(x) return x * x * x end
local function easeInOut(x) return x < 0.5 and 4 * x * x * x or 1 - (-2 * x + 2) ^ 3 / 2 end

-- angle: 0 = shut, 1 = fully open
local function drawDoor(angle, dx, dy)
	if tx_door == nil or angle >= 1 then return end
	local theta = angle * math.pi / 2
	local shade = 0.42 + 0.58 * math.cos(theta)
	tx_door:SetScale(math.cos(theta), 1 + 0.12 * math.sin(theta))   -- the free edge swings toward the viewer
	tx_door:SetColor(shade, shade, shade)
	tx_door:DrawAtAnchor(-OVERSCAN + dx, SCREEN_H / 2 + dy, "left")
	tx_door:SetScale(1, 1)
	tx_door:SetColor(1, 1, 1)
end

function fadeOut(t)
	if lastPhase ~= "out" then                -- a new trip
		lastPhase = "out"
		slammed, creaked = false, false
	end
	if t < SLAM_AT then
		drawDoor(1 - easeIn(t / SLAM_AT), 0, 0)
		return
	end
	if not slammed then
		slammed = true
		if snd_close then snd_close:Play() end
	end
	local k = (t - SLAM_AT) / (1 - SLAM_AT)                 -- 0..1 after the slam
	local rebound = 0.035 * math.sin(k * math.pi) * (1 - k)  -- it bounces off the frame once
	local amp = 12 * (1 - k) ^ 2
	drawDoor(rebound, amp * math.sin(k * 61), amp * math.cos(k * 47))
end

function loading(progress, elapsed)
	lastPhase = "load"
	drawDoor(0, 0, 0)
end

function fadeIn(t)
	lastPhase = "in"
	if not creaked then
		creaked = true
		if snd_open then snd_open:Play() end
	end
	drawDoor(easeInOut(math.min(1, t / OPEN_SPAN)), 0, 0)
end

function onStart()
	tx_door = TEXTURE:CreateTextureSync("Textures/Door.png")
	snd_close = SOUND:CreateSFX("Sounds/DoorClose.ogg")
	snd_open = SOUND:CreateSFX("Sounds/DoorOpen.ogg")
end

function onDestroy()
	if tx_door then tx_door:Dispose(); tx_door = nil end
	if snd_close then snd_close:Dispose(); snd_close = nil end
	if snd_open then snd_open:Dispose(); snd_open = nil end
end
