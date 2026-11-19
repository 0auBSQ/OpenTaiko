---@diagnostic disable: undefined-global, undefined-field, lowercase-global, need-check-nil
-- nokon_curtain_back: Intro Nokon curtain on the way back to the title (and the fast way in)

local TA = require("TransitionArt")

-- sizes: Config/layout.json; timings: Config/animation.json
local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")

FADE_OUT_SECONDS = Animation.curtain.close_s
FADE_IN_SECONDS  = Animation.curtain.open_s

local SCREEN_W, SCREEN_H = Layout.screen.width, Layout.screen.height
local CURTAIN_W = Layout.curtain_half_width   -- each half

local tx_closed, tx_open = nil, nil
local snd_curtain = nil
local lastPhase = nil                    -- "out" | "loading" | "in" (for one-shot curtain sfx per phase)
local wait = TA.new()

-- textures live only during a trip (Lib/TransitionArt)
local function loadTextures()
	if tx_closed ~= nil then return end
	tx_closed = TEXTURE:CreateTexture("Textures/Curtain.jpg")
	tx_open = TEXTURE:CreateTexture("Textures/Curtain_Open.png")
end

local function freeTextures()
	if tx_closed then tx_closed:Dispose(); tx_closed = nil end
	if tx_open then tx_open:Dispose(); tx_open = nil end
end

-- openness: 0 = fully shut, 1 = fully open (halves off-screen)
local function draw_curtain(openness)
	if openness <= 0 then
		if tx_closed then tx_closed:Draw(0, 0) end   -- full image, hides the seam
		return
	end
	if tx_open == nil or tx_open.Width <= 0 then return end
	local off = math.floor(openness * CURTAIN_W)
	tx_open:DrawRect(-off,            0,         0, 0, CURTAIN_W, SCREEN_H)
	tx_open:DrawRect(CURTAIN_W + off, 0, CURTAIN_W, 0, CURTAIN_W, SCREEN_H)
end

local function phaseSfx(phase)
	if lastPhase ~= phase then
		lastPhase = phase
		if snd_curtain then snd_curtain:Play() end
	end
end

-- t 0..1: open -> shut
function fadeOut(t)
	if lastPhase ~= "out" then loadTextures(); wait:reset() end   -- a new trip
	phaseSfx("out")
	t = wait:progress(t, tx_closed, tx_open)
	draw_curtain(1.0 - t)
end

-- held shut while the next stage loads
function loading(progress, elapsed)
	lastPhase = "loading"
	draw_curtain(0)
end

-- t 0..1: shut -> open
function fadeIn(t)
	phaseSfx("in")
	draw_curtain(t)
	if t >= 1 then freeTextures() end   -- the trip is over
end

function onStart()
	snd_curtain = SOUND:CreateSFX("Sounds/CurtainOpen.ogg")
end

function onDestroy()
	freeTextures()
	if snd_curtain then snd_curtain:Dispose(); snd_curtain = nil end
end
