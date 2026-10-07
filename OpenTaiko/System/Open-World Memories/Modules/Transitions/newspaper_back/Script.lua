---@diagnostic disable: undefined-global, undefined-field, lowercase-global, need-check-nil
-- newspaper_back: out of the coin shop. Newspapers fly in from the right at different speeds and angles, turning a
-- little as they come, until they cover the screen and drift there while the next stage loads, then fly off to the
-- left. Every trip lays the pile out anew.
-- The shared string "newspaper_cover" holds how long (s) into the fade-in the papers hide the next stage (the coin
-- shop reads it when this is its way in).

local TA = require("TransitionArt")

local SCATTER_AT, SCATTER = 0.1, 1.35    -- the papers fly off to the left, in seconds into the fade-in
local REVEAL_AT = SCATTER_AT + 0.7       -- the next stage starts to show between the leaving papers

FADE_OUT_SECONDS = 1.3                   -- the papers fly in
FADE_IN_SECONDS  = SCATTER_AT + SCATTER

local SCREEN_W = 1920
local PAGES = 6                          -- Textures/Page1..6.png
local OFF = 820                          -- a page (and its shadow) this far past a screen edge is out of sight
local SHADOW_OPACITY = 0.34
local SHADOW_X, SHADOW_Y = 10, 14        -- a resting page's shadow offset; a lifted page's shadow falls further

-- where each page comes to rest, in the order they arrive (a later page lies on top): bx, by the centre and
-- s the scale; every trip moves each a little, turns it to a random angle and picks its page picture
local SHEETS = {
	{ bx = 1685, by = 830, s = 0.90 },
	{ bx = 1180, by = 260, s = 0.92 },
	{ bx = 700,  by = 830, s = 0.88 },
	{ bx = 1685, by = 245, s = 0.90 },
	{ bx = 240,  by = 290, s = 0.92 },
	{ bx = 1200, by = 800, s = 0.90 },
	{ bx = 700,  by = 280, s = 0.88 },
	{ bx = 230,  by = 820, s = 0.90 },
	{ bx = 1420, by = 560, s = 0.84 },
	{ bx = 460,  by = 520, s = 0.84 },
	{ bx = 960,  by = 600, s = 0.86 },
	{ bx = 1830, by = 500, s = 0.82 },
	{ bx = 90,   by = 600, s = 0.82 },
	{ bx = 1000, by = 150, s = 0.80 },
	{ bx = 760,  by = 980, s = 0.84 },
}

local tx_pages, tx_shadow = {}, nil
local snd_rush, snd_scatter = nil, nil

local lastPhase = nil
local played = {}
local clock = 0                          -- seconds since the trip started, for the drift
local wait = TA.new()

local function clamp01(x) return x < 0 and 0 or (x > 1 and 1 or x) end

-- each page's place and flight for this trip: arriving in fractions of the fade-out, leaving in seconds from
-- SCATTER_AT (top first); a few land upside down
local function plan()
	local r = math.random
	local n = #SHEETS
	for i, S in ipairs(SHEETS) do
		local k = (i - 1) / (n - 1)
		S.x = S.bx + (r() - 0.5) * 60
		S.y = S.by + (r() - 0.5) * 50
		S.r = (r() - 0.5) * 30 + ((r() < 0.12) and 180 or 0)
		S.p = math.random(PAGES)
		S.inAt = math.max(0, 0.46 * k + 0.05 * (r() - 0.5))
		S.inDur = math.min(1 - S.inAt, 0.36 + 0.22 * r())
		S.fromY = S.y + (r() - 0.5) * 440
		S.arc = 20 + 60 * r()
		S.spinIn = (r() < 0.5 and -1 or 1) * (25 + 50 * r())
		S.flapIn = 1.2 + 1.4 * r()
		S.ph1, S.ph2, S.ph3 = r() * 6.283, r() * 6.283, r() * 6.283
		S.outAt = 0.5 * (1 - k) + 0.06 * r()
		S.outDur = math.min(SCATTER - 0.02 - S.outAt, 0.55 + 0.3 * r())
		S.toY = S.y + (r() - 0.5) * 320
		S.spinOut = (r() < 0.5 and -1 or 1) * (15 + 30 * r())
		S.flapOut = 1.0 + 1.2 * r()
	end
end

-- the textures live only during a trip (Lib/TransitionArt)
local function loadTextures()
	if tx_shadow ~= nil then return end
	for i = 1, PAGES do tx_pages[i] = TEXTURE:CreateTexture("Textures/Page" .. i .. ".png") end
	tx_shadow = TEXTURE:CreateTexture("Textures/Shadow.png")
end

local function freeTextures()
	for i, tx in pairs(tx_pages) do tx:Dispose(); tx_pages[i] = nil end
	if tx_shadow then tx_shadow:Dispose(); tx_shadow = nil end
end

local function once(key, snd)
	if played[key] then return end
	played[key] = true
	if snd then snd:Play() end
end

local function tick()
	local ok, d = pcall(function() return fps.deltaTime end)
	d = ok and tonumber(d) or 0
	clock = clock + (d < 0 and 0 or (d > 0.1 and 0.1 or d))
end

-- lift 0..1: how far the page is off the pile (bigger, its shadow further and lighter)
local function drawSheet(tx, x, y, sx, sy, rot, lift)
	if tx == nil or tx_shadow == nil then return end
	local grow = 1 + 0.05 * lift
	tx_shadow:SetOpacity(SHADOW_OPACITY * (1 - 0.35 * lift))
	tx_shadow:SetScale(sx * grow, sy * grow)
	tx_shadow:SetRotation(rot)
	tx_shadow:DrawAtAnchor(x + SHADOW_X + 36 * lift, y + SHADOW_Y + 46 * lift, "center")
	tx:SetScale(sx, sy)
	tx:SetRotation(rot)
	tx:DrawAtAnchor(x, y, "center")
end

-- arrive: fade-out progress (1 = all landed); away: seconds since SCATTER_AT
local function drawPile(arrive, away)
	for _, S in ipairs(SHEETS) do
		local a = S.inDur > 0 and clamp01((arrive - S.inAt) / S.inDur) or 1
		local l = clamp01((away - S.outAt) / S.outDur)
		local x, y, rot, sx, lift = S.x, S.y, S.r, 1, 0
		if a < 1 then
			local e = 1 - (1 - a) ^ 2.5
			local f = 1 - e
			x = SCREEN_W + OFF + (S.x - SCREEN_W - OFF) * e
			y = S.fromY + (S.y - S.fromY) * e - S.arc * math.sin(math.pi * e)
			rot = S.r + S.spinIn * f ^ 1.5
			sx = 1 - 0.3 * math.abs(math.sin(math.pi * S.flapIn * a)) * f
			lift = f
		elseif l > 0 then
			local e = l ^ 2.2
			x = S.x + (-OFF - S.x) * e
			y = S.y + (S.toY - S.y) * e
			rot = S.r + S.spinOut * e
			sx = 1 - 0.3 * math.abs(math.sin(math.pi * S.flapOut * l)) * e
			lift = math.min(1, l * 2.5)
		end
		x = x + 6 * math.sin(0.8 * clock + S.ph1)
		y = y + 5 * math.sin(0.6 * clock + S.ph2)
		rot = rot + 0.7 * math.sin(0.5 * clock + S.ph3)
		if x > -OFF and x < SCREEN_W + OFF then
			local k = S.s * (1 + 0.1 * lift)
			drawSheet(tx_pages[S.p], x, y, k * sx, k, rot, lift)
		end
	end
end

function fadeOut(t)
	if lastPhase ~= "out" then                -- a new trip
		lastPhase = "out"
		played, clock = {}, 0
		plan()
		SHARED:SetSharedString("newspaper_cover", string.format("%.2f", REVEAL_AT))
		loadTextures(); wait:reset()
	end
	t = wait:progress(t, tx_shadow, tx_pages[1], tx_pages[2], tx_pages[3], tx_pages[4], tx_pages[5], tx_pages[6])
	tick()
	once("rush", snd_rush)
	drawPile(t, -1)
end

function loading(progress, elapsed)
	lastPhase = "load"
	tick()
	drawPile(1, -1)
end

function fadeIn(t)
	lastPhase = "in"
	tick()
	local now = t * FADE_IN_SECONDS
	if now >= SCATTER_AT then once("scatter", snd_scatter) end
	drawPile(1, now - SCATTER_AT)
	if t >= 1 then freeTextures() end   -- the trip is over
end

function onStart()
	snd_rush = SOUND:CreateSFX("Sounds/Rush.ogg")
	snd_scatter = SOUND:CreateSFX("Sounds/Scatter.ogg")
end

function onDestroy()
	freeTextures()
	if snd_rush then snd_rush:Dispose(); snd_rush = nil end
	if snd_scatter then snd_scatter:Dispose(); snd_scatter = nil end
end
