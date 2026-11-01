---@diagnostic disable: undefined-global, undefined-field, lowercase-global, need-check-nil
local TA = require("TransitionArt")

-- sizes and shading: Config/layout.json; timings: Config/animation.json
local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")
local RATTLE = Animation.rattle

local SHUT = Animation.shut_s            -- fade-out: the halves swing shut
local HOLD, OPEN = Animation.hold_s, Animation.open_s   -- fade-in: a last rattle, then the halves swing open
local COVER = HOLD + OPEN * Animation.cover_at   -- the stage is in view (the halves nearly edge-on at the sides)

FADE_OUT_SECONDS = SHUT
FADE_IN_SECONDS  = HOLD + OPEN

local SCREEN_W = Layout.screen_width

local tx_left, tx_right = nil, nil
local snd_creak, snd_thud = nil, nil

local lastPhase = nil
local played = {}
local clock, thudAt = 0, nil
local wait = TA.new()

local function clamp01(x) return x < 0 and 0 or (x > 1 and 1 or x) end

local function sfx(path)
	if STORAGE:FileExists(path) then return SOUND:CreateSFX(path) end
	return nil
end

-- the textures live only during a trip (Lib/TransitionArt)
local function loadTextures()
	if tx_left ~= nil then return end
	tx_left = TEXTURE:CreateTexture("Textures/WoodLeft.png")
	tx_right = TEXTURE:CreateTexture("Textures/WoodRight.png")
end

local function freeTextures()
	if tx_left then tx_left:Dispose() end
	if tx_right then tx_right:Dispose() end
	tx_left, tx_right = nil, nil
end

local function once(key, snd)
	if played[key] then return end
	played[key] = true
	if snd then snd:Play() end
end

local function tick()
	local ok, d = pcall(function() return fps.deltaTime end)
	d = ok and tonumber(d) or 0
	d = d < 0 and 0 or (d > 0.1 and 0.1 or d)
	clock = clock + d
end

-- one half swung by k (0 = edge-on at its hinge, 1 = shut at its own width: the shut halves overlap at the
-- seam), its hinge at x with the anchor on that side
local function half(tx, x, anchor, k)
	if tx == nil or not tx.Loaded or k <= 0.001 then return end
	if tx.Width <= 0 then return end
	local edge = Layout.half.edge_shade
	local shade = edge + (1 - edge) * k
	tx:SetScale(k, 1)
	tx:SetColor(shade, shade, shade)
	tx:DrawAtAnchor(x, 0, anchor)
	tx:SetScale(1, 1)
	tx:SetColor(1, 1, 1)
end

-- k: how shut the gate is; after the thud the halves bounce back a little and the seam rattles sideways (the
-- hinges stay at the screen edges)
local function drawGate(k)
	local rattle, wob = 0, 0
	if thudAt ~= nil then
		local a = clock - thudAt
		if a >= 0 and a < RATTLE.length_s then
			rattle = RATTLE.bounce * math.exp(-RATTLE.bounce_decay * a) * math.abs(math.sin(RATTLE.bounce_freq * a))
			wob = RATTLE.wobble * math.exp(-RATTLE.wobble_decay * a) * math.sin(RATTLE.wobble_freq * a)
		end
	end
	local s = k * (1 - rattle)
	half(tx_left, 0, "topleft", s * (1 + wob))
	half(tx_right, SCREEN_W, "topright", s * (1 - wob))
end

function fadeOut(t)
	if lastPhase ~= "out" then                -- a new trip
		lastPhase = "out"
		played, clock, thudAt = {}, 0, nil
		SHARED:SetSharedString("vault_cover", string.format("%.2f", COVER))
		loadTextures(); wait:reset()
	end
	t = wait:progress(t, tx_left, tx_right)
	tick()
	once("creak", snd_creak)
	if t >= 1 and thudAt == nil then thudAt = clock; once("thud", snd_thud) end
	drawGate(t * t)
end

function loading(progress, elapsed)
	lastPhase = "load"
	tick()
	if thudAt == nil then thudAt = clock; once("thud", snd_thud) end
	drawGate(1)
end

function fadeIn(t)
	lastPhase = "in"
	tick()
	if thudAt == nil then thudAt = clock; once("thud", snd_thud) end
	local now = t * FADE_IN_SECONDS
	local k = 1
	if now >= HOLD then
		if not played.open then
			played.open = true
			if snd_creak then snd_creak:Play() end
		end
		local u = clamp01((now - HOLD) / OPEN)
		k = 1 - u * u * (3 - 2 * u)
	end
	drawGate(k)
	if t >= 1 then freeTextures() end   -- the trip is over
end

function onStart()
	snd_creak = sfx("Sounds/Creak.ogg")
	snd_thud = sfx("Sounds/Thud.ogg")
end

function onDestroy()
	freeTextures()
	if snd_creak then snd_creak:Dispose(); snd_creak = nil end
	if snd_thud then snd_thud:Dispose(); snd_thud = nil end
end
