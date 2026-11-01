---@diagnostic disable: undefined-global, undefined-field, lowercase-global, need-check-nil
-- nokon_curtain: from the title into Intro Nokon, like a TV show opening. The curtain closes, then on it the
-- lights dim, two spotlights circle in and merge (drum roll), the show's name falls in letter by letter, each
-- letter hops in turn while confetti rains (applause), and the curtain opens. The way back is nokon_curtain_back.

local TA = require("TransitionArt")

-- positions, sizes and colours: Config/layout.json; timings: Config/animation.json
local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")
local TITLE, LIGHTS, CONF = Layout.title, Layout.lights, Layout.confetti
local CURTAIN, LIGHTS_ANIM, LETTERS, CONF_ANIM = Animation.curtain, Animation.lights, Animation.letters, Animation.confetti

-- the show, in seconds from the start of the fade-in (fixed: the fade-in length is read once)
local DIM_IN = LIGHTS_ANIM.dim_s
local SPOT_AT, SPOT_END = LIGHTS_ANIM.spot_at_s, LIGHTS_ANIM.spot_end_s
local FLASH = LIGHTS_ANIM.flash_s
local FALL_AT, FALL_LAST = LETTERS.fall_at_s, LETTERS.fall_last_s
local FALL = LETTERS.fall_s
local HOP_AT, HOP_LAST = LETTERS.hop_at_s, LETTERS.hop_last_s
local HOP = LETTERS.hop_s
local SHOW_SECONDS = CURTAIN.show_s
local OPEN_SECONDS = CURTAIN.open_s

FADE_OUT_SECONDS = CURTAIN.close_s
FADE_IN_SECONDS  = SHOW_SECONDS + OPEN_SECONDS

local SCREEN_W, SCREEN_H = Layout.screen.width, Layout.screen.height
local CX, CY = SCREEN_W / 2, SCREEN_H / 2
local CURTAIN_W = Layout.curtain_half_width
local TITLE_KEY, TITLE_DEFAULT = "TRANSITION_NOKON_TITLE", "INTRO NOKON"
local FONT_SIZE = TITLE.font_size
local LINE_GAP = TITLE.line_gap
local TRACKING = TITLE.tracking_em
local SPACE = TITLE.space_em
local MAX_W = TITLE.max_width
local LETTER_Y = TITLE.y
local INK = "<g." .. TITLE.ink_top_color .. "." .. TITLE.ink_bottom_color .. ">%s</g>"
local SPOT_SIZE = LIGHTS.spot_texture_size
local ORBIT_X, ORBIT_Y = LIGHTS.orbit_x, LIGHTS.orbit_y
local CONFETTI_N = CONF.count
local CONFETTI_COLORS = CONF.colors

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
	local c = LIGHTS.spot_color
	tx_spot:SetColor(c[1], c[2], c[3]); tx_spot:SetOpacity(a); tx_spot:SetScale(s, s)
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
				local land = clamp01((f - 1) * FALL / LETTERS.land_s)        -- a short squash as it lands
				local sq = math.sin(math.pi * land) * (1 - land)
				sx, sy = 1 + LETTERS.squash * sq, 1 - LETTERS.squash * sq
			end
			local h = (now - startOf(i, HOP_AT, HOP_LAST)) / HOP
			if h >= 0 and h <= 1 then
				bump = math.sin(math.pi * h)
				sx, sy = sx * (1 + LETTERS.hop_grow * bump), sy * (1 + LETTERS.hop_grow * bump)
				rot = (i % 2 == 0 and -LETTERS.hop_tilt_deg or LETTERS.hop_tilt_deg) * bump
			end
			shown[#shown + 1] = { L = L, y = y, sx = sx * L.k, sy = sy * L.k, rot = rot, bump = bump, i = i }
		end
	end
	-- all shadows first, then the letters, the biggest hop last so it stands in front of its neighbours
	for _, d in ipairs(shown) do
		font:Draw(d.L.ch, d.L.x + TITLE.shadow_offset.x, d.y + nudge * d.sy + TITLE.shadow_offset.y, col_shadow, col_shadow,
			TITLE.shadow_opacity * alpha, d.sx, 0, "center", d.sy, d.rot)
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
			x = r() * SCREEN_W, y = CONF.start_y - r() * CONF.spread_y, v = CONF_ANIM.speed_min + CONF_ANIM.speed_extra * r(),
			sway = CONF_ANIM.sway_min + CONF_ANIM.sway_extra * r(),
			swayF = CONF_ANIM.sway_rate_min + CONF_ANIM.sway_rate_extra * r(), phase = r() * 6.283,
			spin = (r() < 0.5 and -1 or 1) * (CONF_ANIM.spin_deg_per_s_min + CONF_ANIM.spin_deg_per_s_extra * r()),
			flipF = CONF_ANIM.flip_rate_min + CONF_ANIM.flip_rate_extra * r(),
			scale = CONF.scale_min + CONF.scale_extra * r(), c = CONFETTI_COLORS[1 + math.floor(r() * #CONFETTI_COLORS)],
		}
	end
end

local function drawConfetti(t, alpha)
	if tx_confetti == nil or t <= 0 or alpha <= 0 then return end
	tx_confetti:SetOpacity(alpha)
	for _, p in ipairs(confetti) do
		local y = p.y + p.v * t
		if y > -CONF.cull_margin and y < SCREEN_H + CONF.cull_margin then
			local x = p.x + p.sway * math.sin(p.swayF * t + p.phase)
			local flip = math.max(CONF.min_flip, math.abs(math.cos(p.flipF * t + p.phase)))
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
	local dim, flash = LIGHTS.dim_color, LIGHTS.flash_color
	rect(0, 0, SCREEN_W, SCREEN_H, dim[1], dim[2], dim[3], LIGHTS.dim_opacity * clamp01(now / DIM_IN) * alpha)
	local p = clamp01((now - SPOT_AT) / (SPOT_END - SPOT_AT))
	if now >= SPOT_AT and p < 1 then
		local e = easeInOut(p)
		local ang = 6.283 * LIGHTS_ANIM.turns * p ^ LIGHTS_ANIM.turn_ease
		local shrink = (1 - e) ^ LIGHTS_ANIM.shrink_ease
		for k = 0, 1 do
			local a = ang + k * math.pi
			spot(CX + math.cos(a) * ORBIT_X * shrink, CY + math.sin(a) * ORBIT_Y * shrink, LIGHTS.spot_size, LIGHTS.spot_opacity * alpha)
		end
	elseif p >= 1 then
		spot(CX, LETTER_Y, LIGHTS.merged_size, LIGHTS.merged_opacity * alpha)   -- the merged spot the name stands in
		local fl = (now - SPOT_END) / FLASH
		if fl < 1 then
			spot(CX, CY, LIGHTS_ANIM.flash_start_size + LIGHTS_ANIM.flash_grow_px * easeOut(fl), (1 - fl) * alpha)
			rect(0, 0, SCREEN_W, SCREEN_H, flash[1], flash[2], flash[3], LIGHTS.flash_opacity * (1 - fl) ^ 2 * alpha)
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
	local fade = 1 - clamp01(open / CURTAIN.lights_fade)   -- the lights and the name go as the curtain parts
	draw_curtain(easeInOut(open))
	drawLights(now, fade)
	drawLetters(now, fade)
	local cf = CURTAIN.confetti_fade_s
	drawConfetti(now - HOP_AT, 1 - clamp01((now - (FADE_IN_SECONDS - cf)) / cf))   -- gone before the stage takes over
	if t >= 1 then freeTextures() end   -- the trip is over
end

function onStart()
	snd_curtain = SOUND:CreateSFX("Sounds/CurtainOpen.ogg")
	snd_drum = SOUND:CreateSFX("Sounds/DrumRoll.ogg")
	snd_applause = SOUND:CreateSFX("Sounds/Applause.ogg")
	font = TEXT:CreateGlyphCached(FONT_SIZE)
	nudge = math.floor((font.BoxHeight - math.ceil(font.LineHeight)) / 2) + TITLE.text_nudge
	col_ink = Config.color(TITLE.ink_color)
	col_edge = Config.color(TITLE.edge_color)
	col_shadow = Config.color(TITLE.shadow_color)
end

function onDestroy()
	freeTextures()
	for _, s in ipairs({ snd_curtain, snd_drum, snd_applause }) do s:Dispose() end
	snd_curtain, snd_drum, snd_applause = nil, nil, nil
	if font then font:Dispose(); font = nil end
end
