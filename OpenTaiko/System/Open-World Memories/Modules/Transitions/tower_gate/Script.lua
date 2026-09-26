---@diagnostic disable: undefined-global, undefined-field, lowercase-global, need-check-nil
-- tower_gate: the stone gate from the title into Survival Mode.
-- The title drops onto the shut gate letter by letter, then the gate opens.

local SHUT_AT = 0.68             -- part of the fade-out when the halves meet
local ARRIVE = 2.0               -- the letters start falling within this time
local FALL = 0.26                -- one letter's fall, in seconds
local SHOW_AFTER, TITLE_FADE = 0.6, 0.45
local TITLE_SECONDS = ARRIVE + FALL + SHOW_AFTER + TITLE_FADE   -- the first part of the fade-in
local OPEN_SECONDS = 1.2

FADE_OUT_SECONDS = 1.1
FADE_IN_SECONDS  = TITLE_SECONDS + OPEN_SECONDS

local SCREEN_W, SCREEN_H = 1920, 1080
local TOP = -60                  -- 60 px of extra art above and below, for the shake
local TRAVEL_PAD = 40            -- how far past the screen edge an open half rests

local TITLE_KEY, TITLE_DEFAULT = "TRANSITION_TOWERS_TITLE", "SURVIVAL\nMODE"
local FONT_SIZE = 116
local TRACK = 0.2                -- space between two letters, in ems
local MAX_W = 1760               -- a longer line shrinks to this width
local STEP_MAX = 0.17            -- seconds between letters, less for long titles
local BIG = 2.4                  -- a letter's starting scale
local STONE = "<g.#F4ECD9.#8C7A5D>%s</g>"

local tx_left, tx_right, tx_dust = nil, nil, nil
local snd_slide, snd_slam, snd_open = nil, nil, nil
local snd_letters = {}
local font, fore, outline, shadow = nil, nil, nil, nil
local nudge = 0

local letters = {}               -- { ch, x, y, k, t0 } in arrival order
local slid, slammed, opened, fired = false, false, false, 0
local lastPhase = nil

local function clamp01(x) return x < 0 and 0 or (x > 1 and 1 or x) end
local function easeInOut(x) return x < 0.5 and 4 * x * x * x or 1 - (-2 * x + 2) ^ 3 / 2 end

-- openness: 0 = shut, 1 = off screen
local function drawGate(openness, dx, dy)
	if tx_left == nil or tx_right == nil then return end
	local w = tx_left.Width > 0 and tx_left.Width or 1010
	local travel = openness * (SCREEN_W / 2 + TRAVEL_PAD)
	tx_left:Draw(SCREEN_W / 2 - w - travel + dx, TOP + dy)
	tx_right:Draw(SCREEN_W / 2 + travel + dx, TOP + dy)
end

-- dust from the seam, k goes 0..1 over its life
local function drawDust(k, strength)
	if tx_dust == nil or k >= 1 then return end
	tx_dust:SetOpacity(0.85 * (1 - k) * strength)
	for i = 1, 7 do
		local side = (i % 2 == 0) and 1 or -1
		local y = 100 + (i - 1) * 150
		local s = (0.6 + 1.2 * k) * (0.8 + 0.25 * ((i * 37) % 5) / 4) * strength
		tx_dust:SetScale(s, s)
		tx_dust:SetRotation(i * 47 + k * 40 * side)
		tx_dust:DrawAtAnchor(SCREEN_W / 2 + side * (20 + 110 * k), y, "center")
	end
	tx_dust:SetScale(1, 1); tx_dust:SetRotation(0); tx_dust:SetOpacity(1)
end

-- each landed letter adds a short jolt
local function shakeAt(now)
	local dx, dy = 0, 0
	for i, L in ipairs(letters) do
		local d = now - (L.t0 + FALL)
		if d >= 0 and d < 0.4 then
			local a = 13 * math.exp(-d / 0.08)
			dx = dx + a * math.sin(d * 97 + i)
			dy = dy + a * math.cos(d * 83 + i * 1.7)
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
	local lineH = font.LineHeight * 0.92
	local track = FONT_SIZE * 1.3 * TRACK
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
	font:Draw(L.ch, x + 7 * scale, y + 9 * scale, shadow, shadow, alpha * 0.45, scale, 0, "center")
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
			local e = 1 - (1 - p) ^ 3
			drawLetter(L, dx, dy, BIG + (1 - BIG) * e, e * titleAlpha)
		end
	end
end

function fadeOut(t)
	if lastPhase ~= "out" then                -- a new trip
		lastPhase = "out"
		slid, slammed, opened, fired = false, false, false, 0
		layout()
	end
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
	local amp = 20 * (1 - k) ^ 2
	drawGate(0, amp * math.sin(k * 70), amp * math.cos(k * 53))
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
	drawDust(clamp01(k / 0.45), 0.6)
end

function onStart()
	-- sync, since Width places the halves
	tx_left = TEXTURE:CreateTextureSync("Textures/GateLeft.png")
	tx_right = TEXTURE:CreateTextureSync("Textures/GateRight.png")
	tx_dust = TEXTURE:CreateTexture("Textures/Dust.png")
	snd_slide = SOUND:CreateSFX("Sounds/GateSlide.ogg")
	snd_slam = SOUND:CreateSFX("Sounds/GateSlam.ogg")
	snd_open = SOUND:CreateSFX("Sounds/GateOpen.ogg")
	for i = 1, 3 do snd_letters[i] = SOUND:CreateSFX("Sounds/LetterSlam.ogg") end
	font = TEXT:CreateGlyphCached(FONT_SIZE)
	nudge = math.floor((font.BoxHeight - math.ceil(font.LineHeight)) / 2) - 1
	fore = COLOR:CreateColorFromRGBA(244, 236, 217, 255)
	outline = COLOR:CreateColorFromRGBA(40, 32, 24, 255)
	shadow = COLOR:CreateColorFromRGBA(0, 0, 0, 255)
end

function onDestroy()
	if tx_left then tx_left:Dispose(); tx_left = nil end
	if tx_right then tx_right:Dispose(); tx_right = nil end
	if tx_dust then tx_dust:Dispose(); tx_dust = nil end
	if snd_slide then snd_slide:Dispose(); snd_slide = nil end
	if snd_slam then snd_slam:Dispose(); snd_slam = nil end
	if snd_open then snd_open:Dispose(); snd_open = nil end
	for _, s in ipairs(snd_letters) do s:Dispose() end
	snd_letters = {}
	if font then font:Dispose(); font = nil end
end
