---@diagnostic disable: undefined-global, undefined-field, lowercase-global, need-check-nil
-- dan_doors: the dojo's shoji doors from the title into the Fox Dojo, with the dojo's name crossing them.
-- The shared string "dan_doors_title" holds the name's duration, so dan_select's boards wait for it.

local PASS = 2.4             -- one line's crossing, in seconds
local STAGGER = 0.15         -- the lower line follows the upper one by this much
local TITLE_SECONDS = PASS + STAGGER + 0.1
local OPEN_SECONDS = 0.7

FADE_OUT_SECONDS = 0.7       -- doors closing
FADE_IN_SECONDS  = TITLE_SECONDS + OPEN_SECONDS

local SCREEN_W, SCREEN_H = 1920, 1080
local TITLE_KEY, TITLE_DEFAULT = "TRANSITION_DOJO_TITLE", "THE FOX\nDOJO"
local FONT_SIZE = 118
local LINE_GAP = 170         -- between the two lines' centres
local DRIFT = 70             -- how far a line creeps either side of the middle
local OUT_AT = 0.78          -- part of a crossing when the line speeds off
local INK = "<g.#DC4630.#8A1A10>%s</g>"

local tx_door = nil
local snd_close, snd_open = nil, nil
local snd_swish = {}
local font, col_ink, col_edge, col_shadow = nil, nil, nil, nil
local nudge = 0

local lines = {}             -- { text, w, y, dir, t0, swished }
local closed, opened, swishes = false, false, 0
local lastPhase = nil

local function clamp01(x) return x < 0 and 0 or (x > 1 and 1 or x) end
local function easeInOut(x) return x < 0.5 and 4 * x * x * x or 1 - (-2 * x + 2) ^ 3 / 2 end

-- Draw the doors at the given "openness" (0 = shut / fully covering, 1 = wide open / off-screen). The two
-- halves slide out to the sides as openness → 1, revealing the stage the engine drew behind them.
local function draw_doors(openness)
	if tx_door == nil then return end
	if openness <= 0 then tx_door:Draw(0, 0); return end                 -- fully shut: one seamless image
	local w = tx_door.Width  > 0 and tx_door.Width  or SCREEN_W
	local h = tx_door.Height > 0 and tx_door.Height or SCREEN_H
	local half = math.floor(w / 2)
	local off  = math.floor(openness * half)
	tx_door:DrawRectAtAnchor(-off,       0,    0, 0, half, h, "topleft")  -- left half slides left
	tx_door:DrawRectAtAnchor(half + off, 0, half, 0, half, h, "topleft")  -- right half slides right
end

local function title()
	local ok, s = pcall(function() return THEME:GetSkinString(TITLE_KEY) end)
	if ok and type(s) == "string" and s ~= "" and s:sub(1, 1) ~= "[" then return s end
	return TITLE_DEFAULT
end

-- the upper line crosses left to right, the lower right to left
local function layout()
	lines = {}
	if font == nil then return end
	local texts = {}
	for line in (title() .. "\n"):gmatch("(.-)\n") do
		if line ~= "" then texts[#texts + 1] = line end
	end
	for i = 1, math.min(2, #texts) do
		local y = SCREEN_H / 2 + (#texts > 1 and (i == 1 and -LINE_GAP / 2 or LINE_GAP / 2) or 0)
		lines[i] = { text = texts[i], w = font:Measure(texts[i]), y = y, dir = i == 1 and 1 or -1,
			t0 = (i - 1) * STAGGER, swished = 0 }
	end
end

-- p goes 0..1: fast in, slow in the middle, fast out, starting and ending off screen
local function offset(L, p)
	local u = 2 * p - 1
	local far = SCREEN_W / 2 + L.w / 2 + 80 - DRIFT
	return L.dir * (far * (0.35 * u ^ 3 + 0.65 * u ^ 5) + DRIFT * u)
end

local function swish()
	swishes = swishes + 1
	local s = snd_swish[(swishes - 1) % #snd_swish + 1]
	if s then s:Play() end
end

local function drawLine(L, p)
	local x = SCREEN_W / 2 + offset(L, p)
	local speed = math.abs(offset(L, math.min(1, p + 0.01)) - offset(L, p)) / (0.01 * PASS)   -- px/s
	local trail = clamp01((speed - 900) / 3000)
	for k = 3, 1, -1 do                                          -- a short streak behind a fast line
		if trail > 0 then
			font:Draw(string.format(INK, L.text), x - L.dir * speed * 0.012 * k, L.y + nudge, col_ink, col_edge,
				trail * 0.22 / k, 1, 0, "center")
		end
	end
	font:Draw(L.text, x + 5, L.y + nudge + 7, col_shadow, col_shadow, 0.35, 1, 0, "center")
	font:Draw(string.format(INK, L.text), x, L.y + nudge, col_ink, col_edge, 1, 1, 0, "center")
end

-- now: seconds into the fade-in
local function drawTitle(now)
	draw_doors(0)
	for _, L in ipairs(lines) do
		local p = (now - L.t0) / PASS
		if p >= 0 and p <= 1 then
			if L.swished == 0 then L.swished = 1; swish() end
			if L.swished == 1 and p >= OUT_AT then L.swished = 2; swish() end
			drawLine(L, p)
		end
	end
end

-- Close the doors over the outgoing title: t 0→1 = open → shut.
function fadeOut(t)
	if lastPhase ~= "out" then                -- a new trip
		lastPhase = "out"
		closed, opened, swishes = false, false, 0
		layout()
		SHARED:SetSharedString("dan_doors_title", tostring(TITLE_SECONDS))
	end
	if not closed then
		closed = true
		if snd_close then snd_close:Play() end
	end
	draw_doors(1.0 - t)
end

-- Hold the doors shut while dan_select loads behind them.
function loading(progress, elapsed)
	lastPhase = "load"
	draw_doors(0)
end

-- The name crosses the shut doors, then they open.
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
	draw_doors(easeInOut(clamp01((now - TITLE_SECONDS) / OPEN_SECONDS)))
end

function onStart()
	-- Sync: the door is drawn split (its Width is read), so it must be fully uploaded, not a blank async stub.
	tx_door = TEXTURE:CreateTextureSync("Textures/Door.png")
	snd_close = SOUND:CreateSFX("Sounds/DoorsClose.ogg")
	snd_open = SOUND:CreateSFX("Sounds/DoorsOpen.ogg")
	for i = 1, 2 do snd_swish[i] = SOUND:CreateSFX("Sounds/Swish.ogg") end
	font = TEXT:CreateGlyphCached(FONT_SIZE)
	nudge = math.floor((font.BoxHeight - math.ceil(font.LineHeight)) / 2) - 1
	col_ink = COLOR:CreateColorFromRGBA(255, 255, 255, 255)
	col_edge = COLOR:CreateColorFromRGBA(250, 238, 206, 255)
	col_shadow = COLOR:CreateColorFromRGBA(40, 16, 8, 255)
end

function onDestroy()
	if tx_door ~= nil then tx_door:Dispose() ; tx_door = nil end
	if snd_close then snd_close:Dispose(); snd_close = nil end
	if snd_open then snd_open:Dispose(); snd_open = nil end
	for _, s in ipairs(snd_swish) do s:Dispose() end
	snd_swish = {}
	if font then font:Dispose(); font = nil end
end
