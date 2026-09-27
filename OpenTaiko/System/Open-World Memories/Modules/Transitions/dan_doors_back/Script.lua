---@diagnostic disable: undefined-global, undefined-field, lowercase-global, need-check-nil
-- dan_doors_back: the dojo's shoji doors on every trip except the title into the Fox Dojo.

local TA = require("TransitionArt")

FADE_OUT_SECONDS = 0.7   -- doors closing
FADE_IN_SECONDS  = 0.7   -- doors opening

local SCREEN_W, SCREEN_H = 1920, 1080
local tx_door = nil
local snd_close, snd_open = nil, nil
local closed, opened = false, false
local lastPhase = nil
local wait = TA.new()

-- the texture lives only during a trip (Lib/TransitionArt)
local function loadTextures()
	if tx_door == nil then tx_door = TEXTURE:CreateTexture("Textures/Door.jpg") end
end

local function freeTextures()
	if tx_door ~= nil then tx_door:Dispose(); tx_door = nil end
end

-- openness: 0 = shut, 1 = off screen
local function draw_doors(openness)
	if tx_door == nil then return end
	if openness <= 0 then tx_door:Draw(0, 0); return end                 -- fully shut: one seamless image
	local w = tx_door.Width  > 0 and tx_door.Width  or SCREEN_W
	local h = tx_door.Height > 0 and tx_door.Height or SCREEN_H
	local half = math.floor(w / 2)
	local off  = math.floor(openness * half)
	tx_door:DrawRectAtAnchor(-off,       0,    0, 0, half, h, "topleft")  -- left half slides left
	tx_door:DrawRectAtAnchor(half + off, 0, half, 0, half, h, "topleft")  -- right half slides right
end

-- Close the doors over the outgoing stage: t 0→1 = open → shut.
function fadeOut(t)
	if lastPhase ~= "out" then                -- a new trip
		lastPhase = "out"
		closed, opened = false, false
		loadTextures(); wait:reset()
	end
	t = wait:progress(t, tx_door)
	if not closed then
		closed = true
		if snd_close then snd_close:Play() end
	end
	draw_doors(1.0 - t)
end

-- Hold the doors shut while the next stage loads behind them.
function loading(progress, elapsed)
	lastPhase = "load"
	draw_doors(0)
end

-- Open the doors to reveal the loaded stage: t 0→1 = shut → open.
function fadeIn(t)
	lastPhase = "in"
	if not opened then
		opened = true
		if snd_open then snd_open:Play() end
	end
	draw_doors(t)
	if t >= 1 then freeTextures() end   -- the trip is over
end

function onStart()
	snd_close = SOUND:CreateSFX("Sounds/DoorsClose.ogg")
	snd_open = SOUND:CreateSFX("Sounds/DoorsOpen.ogg")
end

function onDestroy()
	freeTextures()
	if snd_close then snd_close:Dispose(); snd_close = nil end
	if snd_open then snd_open:Dispose(); snd_open = nil end
end
