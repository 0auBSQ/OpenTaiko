---@diagnostic disable: undefined-global, undefined-field, lowercase-global, need-check-nil
-- space_voyage: from the title into the Online Lobby, over the lobby's own 3D sky (Lib/SpaceSky).
-- The lobby's name appears letter by letter, then its menu fades in.

local Sky = require("SpaceSky")

local ARRIVE = 1.2                   -- the letters appear within this time
local POP = 0.2                      -- one letter's pop-in, in seconds
local SHOW_AFTER, TITLE_FADE = 0.7, 0.5
local TITLE_SECONDS = ARRIVE + POP + SHOW_AFTER + TITLE_FADE   -- the first part of the fade-in
local REVEAL_SECONDS = 1.2

FADE_OUT_SECONDS = 1.4
FADE_IN_SECONDS  = TITLE_SECONDS + REVEAL_SECONDS

local SCREEN_W, SCREEN_H = 1920, 1080
local BLEND = 140                -- atmosphere rows that overlap the sky edge
local FULL = SCREEN_H + BLEND    -- sky edge when it covers the whole screen
local LIFT_Y, LIFT_PITCH = 6, 22 -- camera height and tilt at the low point
local SKY_SPRITES = { dot = "Textures/StarDot.png", sparkle = "Textures/Sparkle.png" }
local NIGHT = { 0.03, 0.03, 0.10 }  -- fallback colour when there is no sky

-- same look as the lobby's wordmark (onlinelobby drawWords)
local TITLE_KEY, TITLE_DEFAULT = "TRANSITION_ONLINE_TITLE", "ONLINE LOBBY"
local SIZE, INITIAL = 104, 126       -- the lobby uses 66 / 80
local TRACK, WORD_GAP = 0.24, 0.30   -- in ems
local ASCENT = 0.7706
local MAX_W = 1700
local GILD = "<g.#FFFFFF.#BE7007>%s</g>"
local SHADOW_DY = 5
local GOLD, CREAM = { 1, 214 / 255, 122 / 255 }, { 250 / 255, 238 / 255, 206 / 255 }

local STEP_MAX = 0.11                -- seconds between letters, less for long names
local GLITTER_LIFE = 0.9
local SPARKS = 8                     -- glitter sparks per letter

local tx_air, tx_sparkle, tx_glow = nil, nil, nil
local air_h = 600
local fill = nil
local snd_up = nil
local snd_sparkles = {}
local fonts = {}
local col_white, col_shadow, col_clear = nil, nil, nil

local letters = {}                   -- { ch, x, y, k, font, t0 } in appearance order
local fired, whooshed = 0, false
local lastPhase = nil

local function clamp01(x) return x < 0 and 0 or (x > 1 and 1 or x) end
local function easeInOut(x) return x < 0.5 and 4 * x * x * x or 1 - (-2 * x + 2) ^ 3 / 2 end
local function rnd(i, k) local x = math.sin(i * 12.9898 + k * 78.233) * 43758.5453; return x - math.floor(x) end

local function frameDt()
	local ok, d = pcall(function() return fps.deltaTime end)
	d = ok and tonumber(d) or 0
	return d < 0 and 0 or (d > 0.1 and 0.1 or d)
end

-- k lowers and tilts the camera (0 = the lobby's view)
local function drive(k)
	if not Sky.driving() then return end
	Sky.setLift(-LIFT_Y * k, -LIFT_PITCH * k)
	Sky.update(frameDt())
	Sky.render()
end

-- edge: screen y where the sky ends, with the atmosphere below it
local function drawSky(edge, alpha)
	if alpha <= 0 then return end
	local bottom = math.min(SCREEN_H, edge)
	if bottom > 0 then
		local clipped = bottom < SCREEN_H
		if clipped then GRAPHICS:SetClip(0, 0, SCREEN_W, bottom) end
		if not Sky.draw(0, alpha) and fill ~= nil then
			fill:SetColor(NIGHT[1], NIGHT[2], NIGHT[3]); fill:SetOpacity(alpha)
			fill:SetScale(SCREEN_W / 2, bottom / 2); fill:Draw(0, 0)
		end
		if clipped then GRAPHICS:ClearClip() end
	end
	local top = edge - BLEND
	if tx_air ~= nil and top < SCREEN_H and top + air_h > 0 then
		tx_air:SetOpacity(alpha)
		tx_air:Draw(0, math.floor(top + 0.5))
		tx_air:SetOpacity(1)
	end
end

local function title()
	local ok, s = pcall(function() return THEME:GetSkinString(TITLE_KEY) end)
	if ok and type(s) == "string" and s ~= "" and s:sub(1, 1) ~= "[" then return s end
	return TITLE_DEFAULT
end

-- ASCII, Latin-1 and Cyrillic capitals
local function isCapital(ch)
	local b1, b2 = ch:byte(1, 2)
	if #ch == 1 then return b1 >= 65 and b1 <= 90 end
	if b1 == 0xC3 then return b2 >= 0x80 and b2 <= 0x9E and b2 ~= 0x97 end
	if b1 == 0xD0 then return (b2 >= 0x90 and b2 <= 0xAF) or b2 == 0x81 end
	return false
end

-- centred, shrunk to MAX_W when too wide
local function layout()
	letters = {}
	if fonts[SIZE] == nil then return end
	local chars, prev = {}, " "
	for ch in title():gsub("\n", " "):gmatch("[%z\1-\127\194-\244][\128-\191]*") do
		local s = ((prev == " " or prev == "-") and isCapital(ch)) and INITIAL or SIZE
		chars[#chars + 1] = { ch = ch, font = fonts[s], adv = ch ~= " " and fonts[s]:Measure(ch) or 0 }
		prev = ch
	end
	local tr, gap = TRACK * SIZE * 1.3, WORD_GAP * SIZE * 1.3
	local w = 0
	for i, c in ipairs(chars) do
		if c.ch == " " then w = w + gap else w = w + c.adv + (i < #chars and tr or 0) end
	end
	local f = w > MAX_W and MAX_W / w or 1
	local x0 = SCREEN_W / 2 - w * f / 2
	local baseline = SCREEN_H / 2 + SIZE * 1.3 * 0.36 * f
	local pen = 0
	for _, c in ipairs(chars) do
		if c.ch == " " then pen = pen + gap
		else
			local top = baseline - c.font.LineHeight * ASCENT * f
			letters[#letters + 1] = { ch = c.ch, font = c.font, k = f, w = c.adv * f,
				x = x0 + (pen + c.adv / 2) * f, y = top + c.font.BoxHeight * f / 2 }
			pen = pen + c.adv + tr
		end
	end
	local step = math.min(STEP_MAX, ARRIVE / math.max(1, #letters - 1))
	for i, L in ipairs(letters) do L.t0 = (i - 1) * step end
end

local function spark(x, y, scale, rot, op, rgb)
	if tx_sparkle == nil or op <= 0 then return end
	tx_sparkle:SetScale(scale, scale)
	tx_sparkle:SetRotation(rot)
	tx_sparkle:SetOpacity(op)
	tx_sparkle:SetColor(rgb[1], rgb[2], rgb[3])
	tx_sparkle:DrawAtAnchor(x, y, "center")
end

-- age: seconds since the letter appeared
local function burst(i, L, age)
	if age < 0 or age > GLITTER_LIFE then return end
	local q = age / GLITTER_LIFE
	if tx_glow and age < 0.5 then
		local g = age / 0.5
		tx_glow:SetScale(0.6 + 1.6 * g, 0.6 + 1.6 * g)
		tx_glow:SetOpacity(0.8 * (1 - g))
		tx_glow:SetColor(GOLD[1], GOLD[2], GOLD[3])
		tx_glow:DrawAtAnchor(L.x, L.y, "center")
	end
	for j = 1, SPARKS do
		local life = 0.7 + 0.3 * rnd(i, j)
		local p = q / life
		if p < 1 then
			local ang = (j / SPARKS) * 2 * math.pi + rnd(i, j + 20) * 0.8
			local dist = (40 + 150 * rnd(i, j + 40)) * (1 - (1 - p) ^ 3)
			spark(L.x + math.cos(ang) * dist, L.y + math.sin(ang) * dist * 0.8, (0.25 + 0.35 * rnd(i, j + 80)) * (1 - p * 0.6),
				p * 220 * (j % 2 == 0 and 1 or -1), (1 - p) ^ 1.5, rnd(i, j + 60) < 0.5 and GOLD or CREAM)
		end
	end
end

local function drawLetters(now, alpha)
	for i, L in ipairs(letters) do
		local p = clamp01((now - L.t0) / POP)
		if p > 0 then
			local e = 1 - (1 - p) ^ 3
			local a = e * alpha
			if a > 0 then
				local k = L.k * (1.35 - 0.35 * e)
				L.font:Draw(L.ch, L.x, L.y + SHADOW_DY * k, col_shadow, col_clear, 0.85 * a, k, 0, "center")
				L.font:Draw(string.format(GILD, L.ch), L.x, L.y, col_white, col_clear, a, k, 0, "center")
				local tw = math.max(0, math.sin(now * 3 + i * 2.1)) ^ 8            -- a small gold twinkle
				spark(L.x + L.w * 0.35, L.y - L.font.LineHeight * 0.3 * L.k, 0.3, now * 90, tw * a, GOLD)
			end
		end
		burst(i, L, now - L.t0)
	end
	if tx_sparkle then
		tx_sparkle:SetScale(1, 1); tx_sparkle:SetRotation(0); tx_sparkle:SetOpacity(1); tx_sparkle:SetColor(1, 1, 1)
	end
	if tx_glow then tx_glow:SetScale(1, 1); tx_glow:SetOpacity(1); tx_glow:SetColor(1, 1, 1) end
end

function fadeOut(t)
	if lastPhase ~= "out" then                     -- a new trip
		lastPhase = "out"
		fired, whooshed = 0, false
		Sky.create(SKY_SPRITES)                    -- the lobby normally built it at boot
		Sky.claim("transition")
		layout()
	end
	if not whooshed then
		whooshed = true
		if snd_up then snd_up:Play() end
	end
	local e = easeInOut(t)
	drive(1 - e)
	drawSky(-(air_h - BLEND) + (SCREEN_H + air_h) * e, 1)
end

function loading(progress, elapsed)
	lastPhase = "load"
	drive(0)
	drawSky(FULL, 1)
end

-- the opaque sky hides the lobby's menu while the name plays, then fades out
function fadeIn(t)
	lastPhase = "in"
	local now = t * FADE_IN_SECONDS
	drive(0)                                       -- until the lobby takes the sky back
	if now < TITLE_SECONDS then
		while fired < #letters and now >= letters[fired + 1].t0 do
			fired = fired + 1
			local s = snd_sparkles[(fired - 1) % #snd_sparkles + 1]
			if s then s:Play() end
		end
		drawSky(FULL, 1)
		drawLetters(now, 1 - clamp01((now - (TITLE_SECONDS - TITLE_FADE)) / TITLE_FADE))
	else
		drawSky(FULL, 1 - easeInOut(clamp01((now - TITLE_SECONDS) / REVEAL_SECONDS)))
	end
	if t >= 1 then Sky.release() end
end

function onStart()
	-- sync, since Height sets the slide
	tx_air = TEXTURE:CreateTextureSync("Textures/Atmosphere.png")
	if tx_air.Height and tx_air.Height > 0 then air_h = tx_air.Height end
	tx_sparkle = TEXTURE:CreateTexture("Textures/Sparkle.png")
	tx_glow = TEXTURE:CreateTexture("Textures/Glow.png")
	tx_sparkle:SetBlendMode("add")
	tx_glow:SetBlendMode("add")
	fill = CANVAS:CreateCanvas(2, 2); fill:Clear(255, 255, 255, 255); fill:Upload()
	snd_up = SOUND:CreateSFX("Sounds/Ascend.ogg")
	for i = 1, 4 do snd_sparkles[i] = SOUND:CreateSFX("Sounds/Sparkle.ogg") end
	fonts[SIZE] = TEXT:CreateGlyphCached(SIZE)
	fonts[INITIAL] = TEXT:CreateGlyphCached(INITIAL)
	col_white = COLOR:CreateColorFromRGBA(255, 255, 255, 255)
	col_shadow = COLOR:CreateColorFromRGBA(18, 16, 46, 255)
	col_clear = COLOR:CreateColorFromRGBA(18, 16, 46, 0)
end

function onDestroy()
	Sky.forget()
	if tx_air then tx_air:Dispose(); tx_air = nil end
	if tx_sparkle then tx_sparkle:Dispose(); tx_sparkle = nil end
	if tx_glow then tx_glow:Dispose(); tx_glow = nil end
	if fill then fill:Dispose(); fill = nil end
	if snd_up then snd_up:Dispose(); snd_up = nil end
	for _, s in ipairs(snd_sparkles) do s:Dispose() end
	snd_sparkles = {}
	for _, f in pairs(fonts) do f:Dispose() end
	fonts = {}
end
