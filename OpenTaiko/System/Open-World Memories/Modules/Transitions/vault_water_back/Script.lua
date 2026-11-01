---@diagnostic disable: undefined-global, undefined-field, lowercase-global, need-check-nil
local TA = require("TransitionArt")
local VaultWater = require("VaultWater")

-- positions, sizes and colours: Config/layout.json; timings and motion: Config/animation.json
local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")
local BUBBLES, WATER, BUBBLES_ANIM = Layout.bubbles, Animation.water, Animation.bubbles

local RISE = WATER.rise_s                -- fade-out: the water rises over the screen
local DRAIN = WATER.drain_s              -- fade-in: the water drains down (its first moments out of sight)
local SURFACE_SND = WATER.surface_sound_at_s   -- the drain's sound, in seconds into the fade-in
local COVER = DRAIN

FADE_OUT_SECONDS = RISE
FADE_IN_SECONDS  = DRAIN

local SCREEN_W, SCREEN_H = Layout.screen.width, Layout.screen.height
local SURFACE_LOW, SURFACE_HIGH = SCREEN_H + Layout.surface.below_screen, Layout.surface.high_y
local FLAT, FLAT_LINE = Layout.flat.water_color, Layout.flat.line_color
local LINE_W = Layout.flat.line_width
local CALM, SWELL = WATER.wave_calm_px, WATER.wave_swell_px
local FROTH_CALM, FROTH_SWELL = WATER.froth_calm, WATER.froth_swell
local CHURN_CALM, CHURN_SWELL = WATER.churn_calm, WATER.churn_swell
local CARRY = WATER.carry

local water = nil
local fill = nil                         -- the stand-in's paint: a white 2x2 canvas, tinted and stretched
local tx_bubble = nil
local snd_splash, snd_surface = nil, nil

local bubbles = {}
local lastPhase = nil
local played = {}
local clock = 0
local lastMs = nil                       -- the real clock at the last frame (ms)
local carried = 0                        -- px the water has moved up since the trip started
local wait = TA.new()

local function clamp01(x) return x < 0 and 0 or (x > 1 and 1 or x) end
local function rnd(a, b) return a + (b - a) * math.random() end
-- smootherstep: from rest to rest, no jolt at either end
local function glide(u) u = clamp01(u); return u * u * u * (u * (6 * u - 15) + 10) end
-- its speed against its fastest: 0 at both ends, 1 halfway
local function pace(u) u = clamp01(u); return 16 * u * u * (1 - u) * (1 - u) end

-- the surface at top: the pattern inside and the bubbles move with it; the waves, the froth and the pattern's own
-- motion follow its speed (p, 0..1)
local function carry(top, p)
	local moved = SURFACE_LOW - top
	local d = moved - carried
	carried = moved
	for _, b in ipairs(bubbles) do b.y = b.y - d end
	if water then
		water:setDrift(0, moved * CARRY)
		water:setSwell(CALM + (SWELL - CALM) * p, FROTH_CALM + (FROTH_SWELL - FROTH_CALM) * p,
			CHURN_CALM + (CHURN_SWELL - CHURN_CALM) * p)
	end
end

local function sfx(path)
	if STORAGE:FileExists(path) then return SOUND:CreateSFX(path) end
	return nil
end

-- the water and the textures live only during a trip (Lib/TransitionArt)
local function startTrip()
	if water == nil then
		water = VaultWater.new{ w = SCREEN_W, h = SCREEN_H }
		if not water.ok then water:dispose(); water = nil end
	end
	if tx_bubble == nil then tx_bubble = TEXTURE:CreateTexture("Textures/Bubble.png") end
	if water == nil and fill == nil then
		fill = CANVAS:CreateCanvas(2, 2); fill:Clear(255, 255, 255, 255); fill:Upload()
	end
end

local function endTrip()
	if water then water:dispose(); water = nil end
	if fill then fill:Dispose(); fill = nil end
	if tx_bubble then tx_bubble:Dispose(); tx_bubble = nil end
end

local function once(key, snd)
	if played[key] then return end
	played[key] = true
	if snd then snd:Play() end
end

-- real seconds since the last frame, so the water keeps its pace through slow frames behind the cover (a frame
-- slower than 0.5 s counts 0.5)
local function tick()
	local d
	local ok, ms = pcall(function() return fps.ms end)
	ms = ok and tonumber(ms) or nil
	if ms ~= nil then
		d = lastMs ~= nil and (ms - lastMs) / 1000 or 0
		lastMs = ms
	else
		local okd, dt = pcall(function() return fps.deltaTime end)
		d = okd and tonumber(dt) or 0
	end
	d = d < 0 and 0 or (d > 0.5 and 0.5 or d)
	clock = clock + d
	if water then water:update(d) end
	return d
end

-- a strip of the stand-in across the screen, from y down h px
local function strip(y, h, c)
	fill:SetColor(c[1], c[2], c[3])
	fill:SetScale(SCREEN_W / 2, h / 2)
	fill:DrawAtAnchor(0, y, "topleft")
end

-- the water below the surface line at top
local function drawWater(top)
	if water then
		water:drawSurface(top, 1)
		return
	end
	if fill == nil then return end
	local y0 = math.max(top, 0)
	if SCREEN_H > y0 then strip(y0, SCREEN_H - y0, FLAT) end
	strip(top, LINE_W, FLAT_LINE)
	fill:SetScale(1, 1); fill:SetColor(1, 1, 1)
end

local function drawBubbles(dt, top, rate)
	if math.random() < rate * dt then
		local B, A = BUBBLES, BUBBLES_ANIM
		bubbles[#bubbles + 1] = { x = rnd(B.margin_x, SCREEN_W - B.margin_x), y = SCREEN_H + B.start_below,
			vy = rnd(A.speed_min, A.speed_max), k = rnd(B.scale_min, B.scale_max), wob = rnd(A.wobble_min, A.wobble_max),
			wobF = rnd(A.wobble_hz_min, A.wobble_hz_max), ph = rnd(0, 6.283) }
	end
	local i = 1
	while i <= #bubbles do
		local b = bubbles[i]
		b.y = b.y - b.vy * dt
		if b.y < BUBBLES.remove_y or b.y > SCREEN_H + BUBBLES.remove_below then
			table.remove(bubbles, i)
		else
			if b.y > top + BUBBLES.surface_margin and tx_bubble ~= nil and tx_bubble.Loaded then
				tx_bubble:SetScale(b.k, b.k)
				tx_bubble:SetOpacity(BUBBLES.opacity)
				tx_bubble:DrawAtAnchor(b.x + b.wob * math.sin(clock * b.wobF * 6.283 + b.ph), b.y, "center")
			end
			i = i + 1
		end
	end
	if tx_bubble ~= nil then tx_bubble:SetScale(1, 1); tx_bubble:SetOpacity(1) end
end

function fadeOut(t)
	if lastPhase ~= "out" then                -- a new trip
		lastPhase = "out"
		played, clock, bubbles, carried, lastMs = {}, 0, {}, 0, nil
		SHARED:SetSharedString("vault_cover", string.format("%.2f", COVER))
		startTrip(); wait:reset()
	end
	t = wait:progress(t, tx_bubble)
	local dt = tick()
	once("splash", snd_splash)
	local top = SURFACE_LOW + (SURFACE_HIGH - SURFACE_LOW) * glide(t)
	carry(top, pace(t))
	drawWater(top)
	drawBubbles(dt, top, BUBBLES_ANIM.spawn_rising)
end

function loading(progress, elapsed)
	lastPhase = "load"
	local dt = tick()
	carry(SURFACE_HIGH, 0)
	drawWater(SURFACE_HIGH)
	drawBubbles(dt, SURFACE_HIGH, BUBBLES_ANIM.spawn_covered)
end

function fadeIn(t)
	lastPhase = "in"
	local dt = tick()
	local now = t * FADE_IN_SECONDS
	if now >= SURFACE_SND then once("surface", snd_surface) end
	local top = SURFACE_HIGH + (SURFACE_LOW - SURFACE_HIGH) * glide(t)
	carry(top, pace(t))
	drawWater(top)
	drawBubbles(dt, top, BUBBLES_ANIM.spawn_draining)
	if t >= 1 then endTrip() end   -- the trip is over
end

function onStart()
	snd_splash = sfx("Sounds/Splash.ogg")
	snd_surface = sfx("Sounds/Surface.ogg")
end

function onDestroy()
	endTrip()
	if snd_splash then snd_splash:Dispose(); snd_splash = nil end
	if snd_surface then snd_surface:Dispose(); snd_surface = nil end
end
