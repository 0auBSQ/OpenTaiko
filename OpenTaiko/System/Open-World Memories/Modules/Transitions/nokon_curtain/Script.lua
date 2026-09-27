---@diagnostic disable: undefined-global, undefined-field, lowercase-global, need-check-nil
-- nokon_curtain: from the title into Intro Nokon, like a TV show opening. The curtain closes, then on it the
-- lights dim, two spotlights circle in and merge (drum roll), the show's name falls in letter by letter, each
-- letter hops in turn while confetti rains (applause), and the curtain opens. The way back is nokon_curtain_back.

local TA = require("TransitionArt")

-- the show, in seconds from the start of the fade-in (fixed: the fade-in length is read once)
local DIM_IN = 0.3                       -- the lights go down
local SPOT_AT, SPOT_END = 0.10, 1.70     -- the spotlights circle in; they merge at SPOT_END (the drum roll's crash)
local FLASH = 0.35
local FALL_AT, FALL_LAST = 1.85, 2.55    -- first and last letter start falling
local FALL = 0.42                        -- one letter's fall
local HOP_AT, HOP_LAST = 3.05, 3.85      -- first and last letter start their hop; confetti and applause start
local HOP = 0.40
local SHOW_SECONDS = 4.45
local OPEN_SECONDS = 0.9

FADE_OUT_SECONDS = 0.9                   -- curtain closing
FADE_IN_SECONDS  = SHOW_SECONDS + OPEN_SECONDS

local SCREEN_W, SCREEN_H = 1920, 1080
local CX, CY = SCREEN_W / 2, SCREEN_H / 2
local CURTAIN_W = 960
local TITLE_KEY, TITLE_DEFAULT = "TRANSITION_NOKON_TITLE", "INTRO NOKON"
local FONT_SIZE = 150
local LINE_GAP = 180
local TRACKING = 0.06                    -- em between letters
local SPACE = 0.35                       -- em for a space
local MAX_W = 1700
local LETTER_Y = 520
local INK = "<g.#FFF3A8.#F2A024>%s</g>"
local SPOT_SIZE = 256                    -- Spot.png
local ORBIT_X, ORBIT_Y = 620, 330
local CONFETTI_N = 150
local CONFETTI_COLORS = {
	{ 0.95, 0.26, 0.26 }, { 1.00, 0.78, 0.18 }, { 0.26, 0.62, 0.95 },
	{ 0.30, 0.82, 0.42 }, { 0.95, 0.45, 0.80 }, { 1.00, 0.55, 0.15 },
}

local tx_closed, tx_open, tx_spot, tx_confetti, tx_px = nil, nil, nil, nil, nil
local snd_curtain, snd_drum, snd_applause = nil, nil, nil
local font, col_ink, col_edge, col_shadow = nil, nil, nil, nil
local nudge = 0

local lastPhase = nil
local wait = TA.new()
local played = {}
local letters = {}                       -- { ch, x, y }
local confetti = {}

local function clamp01(x) return x < 0 and 0 or (x > 1 and 1 or x) end
local function easeOut(x) return 1 - (1 - x) ^ 3 end
local function easeInOut(x) return x < 0.5 and 4 * x * x * x or 1 - (-2 * x + 2) ^ 3 / 2 end

-- the textures live only during a trip (Lib/TransitionArt)
local function loadTextures()
	if tx_closed ~= nil then return end
	tx_closed = TEXTURE:CreateTexture("Textures/Curtain.jpg")
	tx_open = TEXTURE:CreateTexture("Textures/Curtain_Open.png")
	tx_spot = TEXTURE:CreateTexture("Textures/Spot.png")
	tx_confetti = TEXTURE:CreateTexture("Textures/Confetti.png")
	tx_px = TEXTURE:CreateTexture("Textures/Pixel.png")
end

local function freeTextures()
	for _, tx in pairs({ tx_closed, tx_open, tx_spot, tx_confetti, tx_px }) do tx:Dispose() end
	tx_closed, tx_open, tx_spot, tx_confetti, tx_px = nil, nil, nil, nil, nil
end

local function once(key, snd)
	if played[key] then return end
	played[key] = true
	if snd then snd:Play() end
end

-- openness: 0 = fully shut, 1 = fully open (halves off-screen)
local function draw_curtain(openness)
	if openness <= 0 then
		if tx_closed then tx_closed:Draw(0, 0) end   -- seamless closed image
		return
	end
	if tx_open == nil or not tx_open.Loaded then return end
	local off = math.floor(openness * CURTAIN_W)
	tx_open:DrawRect(-off,            0,         0, 0, CURTAIN_W, SCREEN_H)   -- left half slides left
	tx_open:DrawRect(CURTAIN_W + off, 0, CURTAIN_W, 0, CURTAIN_W, SCREEN_H)   -- right half slides right
end

local function rect(x, y, w, h, r, g, b, a)
	if tx_px == nil or a <= 0 then return end
	tx_px:SetColor(r, g, b); tx_px:SetOpacity(a)
	tx_px:SetScale(w / tx_px.Width, h / tx_px.Height); tx_px:Draw(x, y)
	tx_px:SetScale(1, 1); tx_px:SetOpacity(1); tx_px:SetColor(1, 1, 1)
end

local function spot(x, y, size, a)
	if tx_spot == nil or a <= 0 then return end
	local s = size / SPOT_SIZE
	tx_spot:SetColor(1, 0.95, 0.78); tx_spot:SetOpacity(a); tx_spot:SetScale(s, s)
	tx_spot:SetBlendMode("add")
	tx_spot:DrawAtAnchor(x, y, "center")
	tx_spot:SetBlendMode("normal")
	tx_spot:SetScale(1, 1); tx_spot:SetOpacity(1); tx_spot:SetColor(1, 1, 1)
end

local function title()
	local ok, s = pcall(function() return THEME:GetSkinString(TITLE_KEY) end)
	if ok and type(s) == "string" and s ~= "" and s:sub(1, 1) ~= "[" then return s end
	return TITLE_DEFAULT
end

-- one entry per letter (spaces only move the pen), centred line by line
local function layout()
	letters = {}
	if font == nil then return end
	local lines = {}
	for line in (title() .. "\n"):gmatch("(.-)\n") do
		if line ~= "" then lines[#lines + 1] = line end
	end
	for li, line in ipairs(lines) do
		local row, pen = {}, 0
		for ch in line:gmatch("[%z\1-\127\194-\244][\128-\191]*") do
			if ch == " " then
				pen = pen + SPACE * FONT_SIZE
			else
				local w = font:Measure(ch)
				row[#row + 1] = { ch = ch, x = pen + w / 2 }
				pen = pen + w + TRACKING * FONT_SIZE
			end
		end
		local width = pen - TRACKING * FONT_SIZE
		local k = width > MAX_W and MAX_W / width or 1
		local y = LETTER_Y + (li - (#lines + 1) / 2) * LINE_GAP
		for _, L in ipairs(row) do
			L.x = CX + (L.x - width / 2) * k
			L.y = y
			L.k = k
			letters[#letters + 1] = L
		end
	end
end

-- letter i of n starts at from + (i - 1) / (n - 1) of the window up to last
local function startOf(i, from, last)
	if #letters <= 1 then return from end
	return from + (last - from) * (i - 1) / (#letters - 1)
end

local function drawLetters(now, alpha)
	if font == nil or alpha <= 0 then return end
	local shown = {}
	for i, L in ipairs(letters) do
		local f = (now - startOf(i, FALL_AT, FALL_LAST)) / FALL
		if f >= 0 then
			local y, sx, sy, rot, bump = L.y, 1, 1, 0, 0
			if f < 1 then
				y = -FONT_SIZE + (L.y + FONT_SIZE) * f * f                    -- falls faster and faster
			else
				local land = clamp01((f - 1) * FALL / 0.18)                  -- a short squash as it lands
				local sq = math.sin(math.pi * land) * (1 - land)
				sx, sy = 1 + 0.18 * sq, 1 - 0.18 * sq
			end
			local h = (now - startOf(i, HOP_AT, HOP_LAST)) / HOP
			if h >= 0 and h <= 1 then
				bump = math.sin(math.pi * h)
				sx, sy = sx * (1 + 0.45 * bump), sy * (1 + 0.45 * bump)
				rot = (i % 2 == 0 and -9 or 9) * bump
			end
			shown[#shown + 1] = { L = L, y = y, sx = sx * L.k, sy = sy * L.k, rot = rot, bump = bump, i = i }
		end
	end
	-- all shadows first, then the letters, the biggest hop last so it stands in front of its neighbours
	for _, d in ipairs(shown) do
		font:Draw(d.L.ch, d.L.x + 6, d.y + nudge * d.sy + 8, col_shadow, col_shadow, 0.45 * alpha, d.sx, 0, "center", d.sy, d.rot)
	end
	table.sort(shown, function(a, b)
		if a.bump ~= b.bump then return a.bump < b.bump end
		return a.i < b.i
	end)
	for _, d in ipairs(shown) do
		font:Draw(string.format(INK, d.L.ch), d.L.x, d.y + nudge * d.sy, col_ink, col_edge, alpha, d.sx, 0, "center", d.sy, d.rot)
	end
end

-- a fixed pseudo-random sequence, so every trip looks the same
local function rng(seed)
	local s = seed
	return function()
		s = (s * 1103515245 + 12345) % 2147483648
		return s / 2147483648
	end
end

local function makeConfetti()
	confetti = {}
	local r = rng(20260927)
	for i = 1, CONFETTI_N do
		confetti[i] = {
			x = r() * SCREEN_W, y = -20 - r() * 700, v = 260 + 220 * r(),
			sway = 20 + 45 * r(), swayF = 1.5 + 2.0 * r(), phase = r() * 6.283,
			spin = (r() < 0.5 and -1 or 1) * (90 + 270 * r()), flipF = 2 + 4 * r(),
			scale = 0.8 + 0.6 * r(), c = CONFETTI_COLORS[1 + math.floor(r() * #CONFETTI_COLORS)],
		}
	end
end

local function drawConfetti(t, alpha)
	if tx_confetti == nil or t <= 0 or alpha <= 0 then return end
	tx_confetti:SetOpacity(alpha)
	for _, p in ipairs(confetti) do
		local y = p.y + p.v * t
		if y > -30 and y < SCREEN_H + 30 then
			local x = p.x + p.sway * math.sin(p.swayF * t + p.phase)
			local flip = math.max(0.15, math.abs(math.cos(p.flipF * t + p.phase)))
			tx_confetti:SetColor(p.c[1], p.c[2], p.c[3])
			tx_confetti:SetScale(p.scale * flip, p.scale)
			tx_confetti:SetRotation(p.spin * t)
			tx_confetti:DrawAtAnchor(x, y, "center")
		end
	end
	tx_confetti:SetRotation(0); tx_confetti:SetScale(1, 1); tx_confetti:SetColor(1, 1, 1); tx_confetti:SetOpacity(1)
end

-- the stage lights: dim, two spots circling in, then one spot where they met
local function drawLights(now, alpha)
	rect(0, 0, SCREEN_W, SCREEN_H, 0, 0, 0, 0.62 * clamp01(now / DIM_IN) * alpha)
	local p = clamp01((now - SPOT_AT) / (SPOT_END - SPOT_AT))
	if now >= SPOT_AT and p < 1 then
		local e = easeInOut(p)
		local ang = 6.283 * 2.2 * p ^ 1.15
		local shrink = (1 - e) ^ 1.1
		for k = 0, 1 do
			local a = ang + k * math.pi
			spot(CX + math.cos(a) * ORBIT_X * shrink, CY + math.sin(a) * ORBIT_Y * shrink, 380, 0.85 * alpha)
		end
	elseif p >= 1 then
		spot(CX, LETTER_Y, 900, 0.75 * alpha)                             -- the merged spot the name stands in
		local fl = (now - SPOT_END) / FLASH
		if fl < 1 then
			spot(CX, CY, 380 + 1400 * easeOut(fl), (1 - fl) * alpha)
			rect(0, 0, SCREEN_W, SCREEN_H, 1, 0.97, 0.88, 0.45 * (1 - fl) ^ 2 * alpha)
		end
	end
end

-- Close the curtain over the title: t 0→1 = open → shut.
function fadeOut(t)
	if lastPhase ~= "out" then               -- a new trip
		lastPhase = "out"
		played = {}
		layout()
		makeConfetti()
		loadTextures(); wait:reset()
	end
	t = wait:progress(t, tx_closed, tx_open)
	once("close", snd_curtain)
	draw_curtain(1.0 - t)
end

-- Hold shut while Intro Nokon loads behind it.
function loading(progress, elapsed)
	lastPhase = "load"
	draw_curtain(0)
end

-- The show on the shut curtain, then the curtain opens.
function fadeIn(t)
	lastPhase = "in"
	local now = t * FADE_IN_SECONDS
	if now >= SPOT_AT then once("drum", snd_drum) end
	if now >= HOP_AT then once("applause", snd_applause) end
	local open = clamp01((now - SHOW_SECONDS) / OPEN_SECONDS)
	if open > 0 then once("open", snd_curtain) end
	local fade = 1 - clamp01(open / 0.3)     -- the lights and the name go as the curtain parts
	draw_curtain(easeInOut(open))
	drawLights(now, fade)
	drawLetters(now, fade)
	drawConfetti(now - HOP_AT, 1 - clamp01((now - (FADE_IN_SECONDS - 0.4)) / 0.4))   -- gone before the stage takes over
	if t >= 1 then freeTextures() end   -- the trip is over
end

function onStart()
	snd_curtain = SOUND:CreateSFX("Sounds/CurtainOpen.ogg")
	snd_drum = SOUND:CreateSFX("Sounds/DrumRoll.ogg")
	snd_applause = SOUND:CreateSFX("Sounds/Applause.ogg")
	font = TEXT:CreateGlyphCached(FONT_SIZE)
	nudge = math.floor((font.BoxHeight - math.ceil(font.LineHeight)) / 2) - 1
	col_ink = COLOR:CreateColorFromRGBA(255, 255, 255, 255)
	col_edge = COLOR:CreateColorFromRGBA(138, 26, 16, 255)
	col_shadow = COLOR:CreateColorFromRGBA(30, 8, 4, 255)
end

function onDestroy()
	freeTextures()
	for _, s in ipairs({ snd_curtain, snd_drum, snd_applause }) do s:Dispose() end
	snd_curtain, snd_drum, snd_applause = nil, nil, nil
	if font then font:Dispose(); font = nil end
end
