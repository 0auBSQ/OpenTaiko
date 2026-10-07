---@diagnostic disable: undefined-global, undefined-field, lowercase-global, need-check-nil
-- newspaper: from the title into the coin shop. Newspapers fly in from the right at different speeds and angles,
-- turning a little as they come, until they cover the screen and drift there while the shop loads; the shop's
-- name slams in over everything, is flicked away, then the papers fly off to the left. Every trip lays the pile
-- out anew. The way back is newspaper_back.
-- The shared string "newspaper_cover" holds how long (s) into the fade-in the papers hide the shop, so the shop's
-- entrance waits for it.

local TA = require("TransitionArt")

-- the fade-in, in seconds
local SPIN = 0.45                        -- the title slams in
local LAND = 0.22                        -- it lands: a short press, and the pile under it jolts
local LEAVE_AT, LEAVE = 2.3, 0.5         -- the title is flicked off to the left
local SCATTER_AT, SCATTER = 2.45, 1.35   -- the papers fly off to the left
local REVEAL_AT = SCATTER_AT + 0.7       -- the shop starts to show between the leaving papers

FADE_OUT_SECONDS = 1.3                   -- the papers fly in
FADE_IN_SECONDS  = SCATTER_AT + SCATTER

local SCREEN_W, SCREEN_H = 1920, 1080
local CX, CY = SCREEN_W / 2, SCREEN_H / 2
local PAGES = 6                          -- Textures/Page1..6.png
local OFF = 820                          -- a page (and its shadow) this far past a screen edge is out of sight
local TITLE_ROT = -2.5
local TITLE_OFF = 1400
local SHADOW_OPACITY = 0.34
local SHADOW_X, SHADOW_Y = 10, 14        -- a resting page's shadow offset; a lifted page's shadow falls further

local TITLE_KEY, TITLE_DEFAULT = "TITLE_STORE", "OpenTaiko's General Store"
local FONT_SIZE = 96
local HEAD_W, HEAD_H = 1500, 420         -- the title's room on the screen
local BOLD = { -1.8, 0, 1.8 }            -- the title's fills, px apart along the line
local SHADOW_DX, SHADOW_DY, SHADOW_A = 10, 12, 0.45

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
local snd_rush, snd_slap, snd_swish, snd_scatter = nil, nil, nil, nil
local font, col_ink, col_paper, col_shade, col_clear = nil, nil, nil, nil, nil
local nudge = 0

local lines = {}                         -- the title: { text, y, k }
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

-- arrive: fade-out progress (1 = all landed); away: seconds since SCATTER_AT; jolt: px pushed away from the centre
local function drawPile(arrive, away, jolt)
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
		if jolt ~= 0 then
			local dx, dy = S.x - CX, S.y - CY
			local d = math.max(1, math.sqrt(dx * dx + dy * dy))
			x, y = x + dx / d * jolt, y + dy / d * jolt
		end
		if x > -OFF and x < SCREEN_W + OFF then
			local k = S.s * (1 + 0.1 * lift)
			drawSheet(tx_pages[S.p], x, y, k * sx, k, rot, lift)
		end
	end
end

local function title()
	local ok, s = pcall(function() return THEME:GetSkinString(TITLE_KEY) end)
	if ok and type(s) == "string" and s ~= "" and s:sub(1, 1) ~= "[" then return s end
	return TITLE_DEFAULT
end

-- one line, or two split at the space that balances them best, shrunk to the title's room
local function layout()
	lines = {}
	if font == nil then return end
	local s = title():gsub("\n", " ")
	local parts = { s }
	if font:Measure(s) > HEAD_W then
		local bestW = math.huge
		for pos in s:gmatch("() ") do
			local a, b = s:sub(1, pos - 1), s:sub(pos + 1)
			local w = math.max(font:Measure(a), font:Measure(b))
			if a ~= "" and b ~= "" and w < bestW then parts, bestW = { a, b }, w end
		end
	end
	local widest = 0
	for _, p in ipairs(parts) do widest = math.max(widest, font:Measure(p)) end
	local pitch = font.LineHeight * 0.92
	local k = math.min(1, HEAD_W / math.max(1, widest), HEAD_H / (#parts * pitch))
	for i, p in ipairs(parts) do
		lines[#lines + 1] = { text = p, k = k, y = (i - (#parts + 1) / 2) * pitch * k }
	end
end

-- the title centred at (x, y) at its scale, rotation and opacity, in passes over all its lines: a soft drop
-- shadow, an outline in the paper's colour so it reads over any page, then three ink fills a little apart along
-- the line (bold) with no outline of their own, which would cover the fill before them
local function drawTitle(x, y, scale, rot, a)
	if font == nil or a <= 0 then return end
	local c, s = math.cos(math.rad(rot)), math.sin(math.rad(rot))
	for pass = 1, 3 do
		for _, L in ipairs(lines) do
			local oy = (L.y + nudge * L.k) * scale
			local lx, ly = x + oy * s, y + oy * c
			if pass == 1 then
				font:Draw(L.text, lx + SHADOW_DX * scale, ly + SHADOW_DY * scale, col_shade, col_shade, SHADOW_A * a, L.k * scale, 0, "center", 0, rot)
			elseif pass == 2 then
				font:Draw(L.text, lx, ly, col_paper, col_paper, a, L.k * scale, 0, "center", 0, rot)
			else
				for _, dx in ipairs(BOLD) do
					local d = dx * scale
					font:Draw(L.text, lx + d * c, ly - d * s, col_ink, col_clear, a, L.k * scale, 0, "center", 0, rot)
				end
			end
		end
	end
end

-- now: seconds into the fade-in
local function drawTitleAt(now)
	if now > LEAVE_AT + LEAVE then return end
	local x, y, k, rot, a = CX, CY, 1, TITLE_ROT, 1
	if now < SPIN then
		local u = now / SPIN
		local e = 1 - (1 - u) ^ 3
		k = 1.7 - 0.7 * e
		a = math.min(1, u * 2.5)
		rot = TITLE_ROT - 9 * (1 - e)
	elseif now < SPIN + LAND then
		k = 1 - 0.04 * math.sin(math.pi * (now - SPIN) / LAND)
	elseif now > LEAVE_AT then
		local u = (now - LEAVE_AT) / LEAVE
		local e = u * u
		x = CX + (-TITLE_OFF - CX) * e
		y = CY - 50 * e
		rot = TITLE_ROT + 14 * e
		k = 1 + 0.05 * math.min(1, u * 3)
	end
	x = x + 3 * math.sin(0.7 * clock)
	rot = rot + 0.35 * math.sin(0.45 * clock + 1)
	drawTitle(x, y, k, rot, a)
end

function fadeOut(t)
	if lastPhase ~= "out" then                -- a new trip
		lastPhase = "out"
		played, clock = {}, 0
		plan()
		layout()
		SHARED:SetSharedString("newspaper_cover", string.format("%.2f", REVEAL_AT))
		loadTextures(); wait:reset()
	end
	t = wait:progress(t, tx_shadow, tx_pages[1], tx_pages[2], tx_pages[3], tx_pages[4], tx_pages[5], tx_pages[6])
	tick()
	once("rush", snd_rush)
	drawPile(t, -1, 0)
end

function loading(progress, elapsed)
	lastPhase = "load"
	tick()
	drawPile(1, -1, 0)
end

function fadeIn(t)
	lastPhase = "in"
	tick()
	local now = t * FADE_IN_SECONDS
	if now >= SPIN then once("slap", snd_slap) end
	if now >= LEAVE_AT then once("swish", snd_swish) end
	if now >= SCATTER_AT then once("scatter", snd_scatter) end
	local v = (now - SPIN) / LAND
	drawPile(1, now - SCATTER_AT, (v > 0 and v < 1) and 14 * math.sin(math.pi * v) * (1 - v) or 0)
	drawTitleAt(now)
	if t >= 1 then freeTextures() end   -- the trip is over
end

function onStart()
	snd_rush = SOUND:CreateSFX("Sounds/Rush.ogg")
	snd_slap = SOUND:CreateSFX("Sounds/Slap.ogg")
	snd_swish = SOUND:CreateSFX("Sounds/Swish.ogg")
	snd_scatter = SOUND:CreateSFX("Sounds/Scatter.ogg")
	font = TEXT:CreateGlyphCached(FONT_SIZE)
	nudge = math.floor((font.BoxHeight - math.ceil(font.LineHeight)) / 2) - 1
	col_ink = COLOR:CreateColorFromRGBA(30, 25, 21, 255)
	col_paper = COLOR:CreateColorFromRGBA(242, 234, 214, 255)
	col_shade = COLOR:CreateColorFromRGBA(0, 0, 0, 255)
	col_clear = COLOR:CreateColorFromRGBA(0, 0, 0, 0)
end

function onDestroy()
	freeTextures()
	for _, s in ipairs({ snd_rush, snd_slap, snd_swish, snd_scatter }) do s:Dispose() end
	snd_rush, snd_slap, snd_swish, snd_scatter = nil, nil, nil, nil
	if font then font:Dispose(); font = nil end
end
