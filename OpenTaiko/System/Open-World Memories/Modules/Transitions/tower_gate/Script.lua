---@diagnostic disable: undefined-global, undefined-field, lowercase-global, need-check-nil
-- tower_gate: the stone gate from the title into Survival Mode.
-- The title drops onto the shut gate letter by letter, then the gate opens.

local TA = require("TransitionArt")

-- positions, sizes and colours: Config/layout.json; timings: Config/animation.json
local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")
local TITLE, DUST = Layout.title, Layout.dust
local CLOSE, TITLE_ANIM, JOLT, DUST_ANIM = Animation.close, Animation.title, Animation.jolt, Animation.dust

local SHUT_AT = CLOSE.shut_at
local ARRIVE = TITLE_ANIM.arrive_s
local FALL = TITLE_ANIM.fall_s
local SHOW_AFTER, TITLE_FADE = TITLE_ANIM.show_after_s, TITLE_ANIM.fade_s
local TITLE_SECONDS = ARRIVE + FALL + SHOW_AFTER + TITLE_FADE   -- the first part of the fade-in
local OPEN_SECONDS = TITLE_ANIM.open_s

FADE_OUT_SECONDS = CLOSE.fade_out_s
FADE_IN_SECONDS  = TITLE_SECONDS + OPEN_SECONDS

local SCREEN_W, SCREEN_H = Layout.screen.width, Layout.screen.height
local TOP = Layout.gate.top
local TRAVEL_PAD = Layout.gate.travel_pad

local TITLE_KEY, TITLE_DEFAULT = "TRANSITION_TOWERS_TITLE", "SURVIVAL\nMODE"
local FONT_SIZE = TITLE.font_size
local TRACK = TITLE.tracking_em
local MAX_W = TITLE.max_width
local STEP_MAX = TITLE_ANIM.step_max_s
local BIG = TITLE_ANIM.start_scale
local STONE = "<g." .. TITLE.stone_top_color .. "." .. TITLE.stone_bottom_color .. ">%s</g>"

local tx_left, tx_right, tx_dust = nil, nil, nil
local snd_slide, snd_slam, snd_open = nil, nil, nil
local snd_letters = {}
local font, fore, outline, shadow = nil, nil, nil, nil
local nudge = 0

local letters = {}               -- { ch, x, y, k, t0 } in arrival order
local slid, slammed, opened, fired = false, false, false, 0
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

-- each landed letter adds a short jolt
local function shakeAt(now)
	local dx, dy = 0, 0
	for i, L in ipairs(letters) do
		local d = now - (L.t0 + FALL)
		if d >= 0 and d < JOLT.length_s then
			local a = JOLT.amp_px * math.exp(-d / JOLT.decay_s)
			dx = dx + a * math.sin(d * JOLT.freq_x + i)
			dy = dy + a * math.cos(d * JOLT.freq_y + i * JOLT.phase_y)
		end
	end
	return dx, dy
end

local function title()
	local ok, s = pcall(function() return THEME:GetSkinString(TITLE_KEY) end)
	if ok and type(s) == "string" and s ~= "" and s:sub(1, 1) ~= "[" then return s end
	return TITLE_DEFAULT
end

-- splits the title into letters, each line centred
local function layout()
	letters = {}
	if font == nil then return end
	local lines = {}
	for line in (title() .. "\n"):gmatch("(.-)\n") do lines[#lines + 1] = line end
	local lineH = font.LineHeight * TITLE.line_pitch
	local track = FONT_SIZE * TITLE.em_size * TRACK
	local rows, widest = {}, 0
	for _, line in ipairs(lines) do
		local chars, w = {}, 0
		for ch in line:gmatch("[%z\1-\127\194-\244][\128-\191]*") do
			local adv = font:Measure(ch)
			chars[#chars + 1] = { ch = ch, adv = adv }
			w = w + adv + track
		end
		rows[#rows + 1] = { chars = chars, w = w - track }
		widest = math.max(widest, w - track)
	end
	local k = widest > MAX_W and MAX_W / widest or 1
	for li, row in ipairs(rows) do
		local x = SCREEN_W / 2 - row.w * k / 2
		local y = SCREEN_H / 2 + (li - (#rows + 1) / 2) * lineH * k
		for _, c in ipairs(row.chars) do
			if c.ch ~= " " then letters[#letters + 1] = { ch = c.ch, x = x + c.adv * k / 2, y = y, k = k } end
			x = x + (c.adv + track) * k
		end
	end
	local step = math.min(STEP_MAX, ARRIVE / math.max(1, #letters - 1))
	for i, L in ipairs(letters) do L.t0 = (i - 1) * step end
end

local function drawLetter(L, dx, dy, scale, alpha)
	if alpha <= 0 then return end
	scale = scale * L.k
	local x, y = L.x + dx, L.y + dy + nudge * scale
	font:Draw(L.ch, x + TITLE.shadow_offset.x * scale, y + TITLE.shadow_offset.y * scale, shadow, shadow,
		alpha * TITLE.shadow_opacity, scale, 0, "center")
	font:Draw(string.format(STONE, L.ch), x, y, fore, outline, alpha, scale, 0, "center")
end

-- now: seconds into the fade-in
local function drawTitle(now)
	while fired < #letters and now >= letters[fired + 1].t0 + FALL do
		fired = fired + 1
		local s = snd_letters[(fired - 1) % #snd_letters + 1]
		if s then s:Play() end
	end
	local dx, dy = shakeAt(now)
	drawGate(0, dx, dy)
	local titleAlpha = 1 - clamp01((now - (TITLE_SECONDS - TITLE_FADE)) / TITLE_FADE)
	for _, L in ipairs(letters) do
		local p = clamp01((now - L.t0) / FALL)
		if p > 0 then
			local e = 1 - (1 - p) ^ TITLE_ANIM.fall_ease
			drawLetter(L, dx, dy, BIG + (1 - BIG) * e, e * titleAlpha)
		end
	end
end

function fadeOut(t)
	if lastPhase ~= "out" then                -- a new trip
		lastPhase = "out"
		slid, slammed, opened, fired = false, false, false, 0
		layout()
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
	local now = t * FADE_IN_SECONDS
	if now < TITLE_SECONDS then
		drawTitle(now)
		return
	end
	if not opened then
		opened = true
		if snd_open then snd_open:Play() end
	end
	local k = clamp01((now - TITLE_SECONDS) / OPEN_SECONDS)
	drawGate(easeInOut(k), 0, 0)
	drawDust(clamp01(k / DUST_ANIM.open_life), DUST_ANIM.open_strength)
	if t >= 1 then freeTextures() end   -- the trip is over
end

function onStart()
	snd_slide = SOUND:CreateSFX("Sounds/GateSlide.ogg")
	snd_slam = SOUND:CreateSFX("Sounds/GateSlam.ogg")
	snd_open = SOUND:CreateSFX("Sounds/GateOpen.ogg")
	for i = 1, 3 do snd_letters[i] = SOUND:CreateSFX("Sounds/LetterSlam.ogg") end
	font = TEXT:CreateGlyphCached(FONT_SIZE)
	nudge = math.floor((font.BoxHeight - math.ceil(font.LineHeight)) / 2) + TITLE.text_nudge
	fore = Config.color(TITLE.fore_color)
	outline = Config.color(TITLE.outline_color)
	shadow = Config.color(TITLE.shadow_color)
end

function onDestroy()
	freeTextures()
	if snd_slide then snd_slide:Dispose(); snd_slide = nil end
	if snd_slam then snd_slam:Dispose(); snd_slam = nil end
	if snd_open then snd_open:Dispose(); snd_open = nil end
	for _, s in ipairs(snd_letters) do s:Dispose() end
	snd_letters = {}
	if font then font:Dispose(); font = nil end
end
