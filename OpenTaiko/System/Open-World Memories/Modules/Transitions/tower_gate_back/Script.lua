---@diagnostic disable: undefined-global, undefined-field, lowercase-global, need-check-nil
-- tower_gate_back: the stone gate out of Survival Mode.

local TA = require("TransitionArt")

-- positions and sizes: Config/layout.json; timings: Config/animation.json
local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")
local DUST, CLOSE, DUST_ANIM = Layout.dust, Animation.close, Animation.dust

FADE_OUT_SECONDS = CLOSE.fade_out_s
FADE_IN_SECONDS  = Animation.open_s

local SCREEN_W = Layout.screen_width
local TOP = Layout.gate.top
local SHUT_AT = CLOSE.shut_at
local TRAVEL_PAD = Layout.gate.travel_pad

local tx_left, tx_right, tx_dust = nil, nil, nil
local snd_slide, snd_slam, snd_open = nil, nil, nil
local slid, slammed, opened = false, false, false
local lastPhase = nil
local wait = TA.new()

-- the textures live only during a trip (Lib/TransitionArt)
local function loadTextures()
	if tx_left == nil then
		tx_left = TEXTURE:CreateTexture("Textures/GateLeft.jpg")
		tx_right = TEXTURE:CreateTexture("Textures/GateRight.jpg")
		tx_dust = TEXTURE:CreateTexture("Textures/Dust.png")
	end
end

local function freeTextures()
	if tx_left then tx_left:Dispose(); tx_left = nil end
	if tx_right then tx_right:Dispose(); tx_right = nil end
	if tx_dust then tx_dust:Dispose(); tx_dust = nil end
end

local function clamp01(x) return x < 0 and 0 or (x > 1 and 1 or x) end
local function easeInOut(x) return x < 0.5 and 4 * x * x * x or 1 - (-2 * x + 2) ^ 3 / 2 end

-- openness: 0 = shut, 1 = off screen
local function drawGate(openness, dx, dy)
	if tx_left == nil or tx_right == nil then return end
	local w = tx_left.Width > 0 and tx_left.Width or Layout.gate.half_width
	local travel = openness * (SCREEN_W / 2 + TRAVEL_PAD)
	tx_left:Draw(SCREEN_W / 2 - w - travel + dx, TOP + dy)
	tx_right:Draw(SCREEN_W / 2 + travel + dx, TOP + dy)
end

-- dust from the seam, k goes 0..1 over its life
local function drawDust(k, strength)
	if tx_dust == nil or k >= 1 then return end
	tx_dust:SetOpacity(DUST.opacity * (1 - k) * strength)
	for i = 1, DUST.count do
		local side = (i % 2 == 0) and 1 or -1
		local y = DUST.first_y + (i - 1) * DUST.spacing_y
		local s = (DUST_ANIM.start_scale + DUST_ANIM.grow * k) * (DUST.base_scale + DUST.scale_spread * ((i * 37) % 5) / 4) * strength
		tx_dust:SetScale(s, s)
		tx_dust:SetRotation(i * DUST.angle_step_deg + k * DUST_ANIM.turn_deg * side)
		tx_dust:DrawAtAnchor(SCREEN_W / 2 + side * (DUST.seam_offset + DUST_ANIM.travel * k), y, "center")
	end
	tx_dust:SetScale(1, 1); tx_dust:SetRotation(0); tx_dust:SetOpacity(1)
end

function fadeOut(t)
	if lastPhase ~= "out" then                -- a new trip
		lastPhase = "out"
		slid, slammed, opened = false, false, false
		loadTextures(); wait:reset()
	end
	t = wait:progress(t, tx_left, tx_right, tx_dust)
	if not slid then
		slid = true
		if snd_slide then snd_slide:Play() end
	end
	if t < SHUT_AT then
		local p = t / SHUT_AT
		drawGate(1 - p * p, 0, 0)                 -- heavy: slow to start, fast at the end
		return
	end
	if not slammed then
		slammed = true
		if snd_slam then snd_slam:Play() end
	end
	local k = (t - SHUT_AT) / (1 - SHUT_AT)
	local amp = CLOSE.shake_px * (1 - k) ^ 2
	drawGate(0, amp * math.sin(k * CLOSE.shake_freq_x), amp * math.cos(k * CLOSE.shake_freq_y))
	drawDust(k, 1)
end

function loading(progress, elapsed)
	lastPhase = "load"
	drawGate(0, 0, 0)
end

function fadeIn(t)
	lastPhase = "in"
	if not opened then
		opened = true
		if snd_open then snd_open:Play() end
	end
	drawGate(easeInOut(t), 0, 0)
	drawDust(clamp01(t / DUST_ANIM.open_life), DUST_ANIM.open_strength)
	if t >= 1 then freeTextures() end   -- the trip is over
end

function onStart()
	snd_slide = SOUND:CreateSFX("Sounds/GateSlide.ogg")
	snd_slam = SOUND:CreateSFX("Sounds/GateSlam.ogg")
	snd_open = SOUND:CreateSFX("Sounds/GateOpen.ogg")
end

function onDestroy()
	freeTextures()
	if snd_slide then snd_slide:Dispose(); snd_slide = nil end
	if snd_slam then snd_slam:Dispose(); snd_slam = nil end
	if snd_open then snd_open:Dispose(); snd_open = nil end
end
