---@diagnostic disable: undefined-global, undefined-field, lowercase-global, need-check-nil
local TA = require("TransitionArt")
local VaultWater = require("VaultWater")

-- positions, sizes and colours: Config/layout.json; timings and motion: Config/animation.json
local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")
local NAME, BUBBLES = Layout.name, Layout.bubbles
local WATER, BUBBLES_ANIM, NAME_ANIM = Animation.water, Animation.bubbles, Animation.name

local RISE = WATER.rise_s                -- fade-out: the water rises over the screen
local NAME_IN = WATER.name_in_s          -- the name fades in once the water covers the screen
local LIFT_AT, LIFT = WATER.lift_at_s, WATER.lift_s   -- the water lifts away, in seconds into the fade-in
local COVER = LIFT_AT + LIFT

FADE_OUT_SECONDS = RISE
FADE_IN_SECONDS  = LIFT_AT + LIFT

local SCREEN_W, SCREEN_H = Layout.screen.width, Layout.screen.height
local CX, CY = SCREEN_W / 2, SCREEN_H / 2
local SURFACE_FROM, SURFACE_TO = SCREEN_H + Layout.surface.below_screen, Layout.surface.to_y
local EDGE_FROM, EDGE_TO = SCREEN_H + Layout.edge.below_screen, Layout.edge.to_y
local CALM, SWELL = WATER.wave_calm_px, WATER.wave_swell_px
local FROTH_CALM, FROTH_SWELL = WATER.froth_calm, WATER.froth_swell
local CHURN_CALM, CHURN_SWELL = WATER.churn_calm, WATER.churn_swell
local CARRY = WATER.carry
local FLAT, FLAT_LINE = Layout.flat.water_color, Layout.flat.line_color
local LINE_W = Layout.flat.line_width

local TITLE_KEY, TITLE_DEFAULT = "TITLE_VAULT", "Secret Vault"
local FONT_SIZE = NAME.font_size
local MAX_W = NAME.max_width
local GRAD = "<g." .. NAME.gradient_top_color .. "." .. NAME.gradient_bottom_color .. ">%s</g>"
local SHADOW_DY = NAME.shadow_y

local water = nil
local fill = nil                         -- the stand-in's paint: a white 2x2 canvas, tinted and stretched
local tx_bubble = nil
local snd_splash, snd_surface, snd_bubbles = nil, nil, nil
local font, col_white, col_outline, col_shadow, col_clear = nil, nil, nil, nil, nil
local nudge = 0

local letters = {}
local bubbles = {}
local lastPhase = nil
local played = {}
local clock, coveredAt = 0, nil
local lastMs = nil                       -- the real clock at the last frame (ms)
local carried = 0                        -- px the water has moved up since the trip started
local wait = TA.new()

local function clamp01(x) return x < 0 and 0 or (x > 1 and 1 or x) end
local function rnd(a, b) return a + (b - a) * math.random() end
local function smooth(u) u = clamp01(u); return u * u * (3 - 2 * u) end
-- smootherstep: from rest to rest, no jolt at either end
local function glide(u) u = clamp01(u); return u * u * u * (u * (6 * u - 15) + 10) end
-- its speed against its fastest: 0 at both ends, 1 halfway
local function pace(u) u = clamp01(u); return 16 * u * u * (1 - u) * (1 - u) end

-- the water has moved up to `moved` px: the pattern inside and the bubbles go with it; the waves, the froth and
-- the pattern's own motion follow its speed (p, 0..1)
local function carry(moved, p)
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

local function title()
	local ok, s = pcall(function() return THEME:GetSkinString(TITLE_KEY) end)
	if ok and type(s) == "string" and s ~= "" and s:sub(1, 1) ~= "[" then return s end
	return TITLE_DEFAULT
end

-- one letter per character, centred on the screen and shrunk to MAX_W
local function layout()
	letters = {}
	if font == nil then return end
	local chars, w = {}, 0
	for ch in title():gsub("\n", " "):gmatch("[%z\1-\127\194-\244][\128-\191]*") do
		local adv = font:Measure(ch)
		if ch == " " then adv = FONT_SIZE * NAME.space_em end
		chars[#chars + 1] = { ch = ch, adv = adv }
		w = w + adv
	end
	local k = math.min(1, MAX_W / math.max(1, w))
	local pen = CX - w * k / 2
	for i, c in ipairs(chars) do
		if c.ch ~= " " then
			letters[#letters + 1] = { ch = c.ch, x = pen + c.adv * k / 2, k = k, ph = i * NAME_ANIM.letter_phase }
		end
		pen = pen + c.adv * k
	end
end

-- a strip of the stand-in across the screen, from y down h px
local function strip(y, h, c)
	fill:SetColor(c[1], c[2], c[3])
	fill:SetScale(SCREEN_W / 2, h / 2)
	fill:DrawAtAnchor(0, y, "topleft")
end

-- the water below the surface line top (nil: above the screen) and above the lower edge bottom (nil: below it)
local function drawWater(top, bottom)
	if water then
		water:drawBody(top, bottom, 1)
		return
	end
	if fill == nil then return end
	local y0, y1 = math.max(top or 0, 0), math.min(bottom or SCREEN_H, SCREEN_H)
	if y1 > y0 then strip(y0, y1 - y0, FLAT) end
	if top ~= nil then strip(top, LINE_W, FLAT_LINE) end
	if bottom ~= nil then strip(bottom - LINE_W, LINE_W, FLAT_LINE) end
	fill:SetScale(1, 1); fill:SetColor(1, 1, 1)
end

local function spawnBubble()
	local B, A = BUBBLES, BUBBLES_ANIM
	bubbles[#bubbles + 1] = { x = rnd(B.margin_x, SCREEN_W - B.margin_x), y = SCREEN_H + B.start_below,
		vy = rnd(A.speed_min, A.speed_max), k = rnd(B.scale_min, B.scale_max), wob = rnd(A.wobble_min, A.wobble_max),
		wobF = rnd(A.wobble_hz_min, A.wobble_hz_max), ph = rnd(0, 6.283) }
end

-- bubbles inside the water: below the surface line (top) and above the lower edge (bottom)
local function drawBubbles(dt, top, bottom, rate)
	if math.random() < rate * dt then spawnBubble() end
	local i = 1
	while i <= #bubbles do
		local b = bubbles[i]
		b.y = b.y - b.vy * dt
		if b.y < BUBBLES.remove_y then
			table.remove(bubbles, i)
		else
			local inside = (top == nil or b.y > top + BUBBLES.surface_margin) and (bottom == nil or b.y < bottom - BUBBLES.edge_margin)
			if inside and tx_bubble ~= nil and tx_bubble.Loaded then
				tx_bubble:SetScale(b.k, b.k)
				tx_bubble:SetOpacity(BUBBLES.opacity)
				tx_bubble:DrawAtAnchor(b.x + b.wob * math.sin(clock * b.wobF * 6.283 + b.ph), b.y, "center")
			end
			i = i + 1
		end
	end
	if tx_bubble ~= nil then tx_bubble:SetScale(1, 1); tx_bubble:SetOpacity(1) end
end

-- the name, each letter bobbing on its own; lift = px the water has carried it up, a = opacity
local function drawName(lift, a)
	if font == nil or a <= 0 then return end
	local N = NAME_ANIM
	local y0 = CY + N.name_bob_px * math.sin(clock * N.name_bob_rate) - lift
	for _, L in ipairs(letters) do
		local y = y0 + N.letter_bob_px * math.sin(clock * N.letter_bob_rate + L.ph) + nudge * L.k
		local rot = N.letter_tilt_deg * math.sin(clock * N.letter_tilt_rate + L.ph * N.tilt_phase_scale)
		font:Draw(L.ch, L.x, y + SHADOW_DY * L.k, col_shadow, col_clear, NAME.shadow_opacity * a, L.k, 0, "center", 0, rot)
		font:Draw(string.format(GRAD, L.ch), L.x, y, col_white, col_outline, a, L.k, 0, "center", 0, rot)
	end
end

local function nameOpacity()
	if coveredAt == nil then return 0 end
	return smooth((clock - coveredAt) / NAME_IN)
end

local RISEN = SURFACE_FROM - SURFACE_TO   -- px the water moves up while it rises

function fadeOut(t)
	if lastPhase ~= "out" then                -- a new trip
		lastPhase = "out"
		played, clock, coveredAt, bubbles, carried, lastMs = {}, 0, nil, {}, 0, nil
		layout()
		SHARED:SetSharedString("vault_cover", string.format("%.2f", COVER))
		startTrip(); wait:reset()
	end
	t = wait:progress(t, tx_bubble)
	local dt = tick()
	once("splash", snd_splash)
	local e = glide(t)
	local top = SURFACE_FROM + (SURFACE_TO - SURFACE_FROM) * e
	carry(RISEN * e, pace(t))
	drawWater(top, nil)
	drawBubbles(dt, top, nil, BUBBLES_ANIM.spawn_rising)
end

function loading(progress, elapsed)
	lastPhase = "load"
	local dt = tick()
	if coveredAt == nil then coveredAt = clock end
	once("bubbles", snd_bubbles)
	carry(RISEN, 0)
	drawWater(nil, nil)
	drawBubbles(dt, nil, nil, BUBBLES_ANIM.spawn_covered)
	drawName(0, nameOpacity())
end

function fadeIn(t)
	lastPhase = "in"
	local dt = tick()
	if coveredAt == nil then coveredAt = clock end
	once("bubbles", snd_bubbles)
	local now = t * FADE_IN_SECONDS
	local u = (now - LIFT_AT) / LIFT
	local e = glide(u)
	local bottom = EDGE_FROM + (EDGE_TO - EDGE_FROM) * e
	local lift = EDGE_FROM - bottom           -- the name rides up with the water
	carry(RISEN + lift, pace(u))
	if now >= LIFT_AT then once("surface", snd_surface) end
	drawWater(nil, e > 0 and bottom or nil)
	drawBubbles(dt, nil, e > 0 and bottom or nil, e > 0 and BUBBLES_ANIM.spawn_lifting or BUBBLES_ANIM.spawn_covered)
	drawName(lift, nameOpacity() * (1 - smooth((lift - NAME_ANIM.fade_from_px) / NAME_ANIM.fade_over_px)))
	if t >= 1 then endTrip() end   -- the trip is over
end

function onStart()
	snd_splash = sfx("Sounds/Splash.ogg")
	snd_surface = sfx("Sounds/Surface.ogg")
	snd_bubbles = sfx("Sounds/Bubbles.ogg")
	font = TEXT:CreateGlyphCached(FONT_SIZE)
	nudge = math.floor((font.BoxHeight - math.ceil(font.LineHeight)) / 2) + NAME.text_nudge
	col_white = Config.color(NAME.fore_color)
	col_outline = Config.color(NAME.outline_color)
	col_shadow = Config.color(NAME.shadow_color)
	col_clear = Config.color(NAME.clear_color)
end

function onDestroy()
	endTrip()
	for _, s in ipairs({ snd_splash, snd_surface, snd_bubbles }) do if s then s:Dispose() end end
	snd_splash, snd_surface, snd_bubbles = nil, nil, nil
	if font then font:Dispose(); font = nil end
end
