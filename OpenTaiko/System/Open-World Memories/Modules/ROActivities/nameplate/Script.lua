-- NamePlate ROActivity
-- Ported from Modules/NamePlate/Script.lua to the new Lua API.
-- Textures are loaded from the module's Textures/ folder.
-- positions, sizes and colours: Config/layout.json; timings: Config/animation.json

local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")

local TEXTURES_DIR  = "Textures/"

local dan_types = { "Clear", "FC", "AP" }

local config_font_name_normal_size = Layout.font_name_normal.size
local config_font_name_normal_maxsize = Layout.font_name_normal.max_size

local config_font_name_withtitle_size = Layout.font_name_withtitle.size
local config_font_name_withtitle_maxsize = Layout.font_name_withtitle.max_size

local config_font_name_full_size = Layout.font_name_full.size
local config_font_name_full_maxsize = Layout.font_name_full.max_size

local config_font_title_size = Layout.font_title.size
local config_font_title_maxsize = Layout.font_title.max_size

local config_font_dan_size = Layout.font_dan.size
local config_font_dan_maxsize = Layout.font_dan.max_size

local config_text_name_normal_offset_x = Layout.text_name_normal.offset_x
local config_text_name_normal_offset_y = Layout.text_name_normal.offset_y

local config_text_name_withtitle_offset_x = Layout.text_name_withtitle.offset_x
local config_text_name_withtitle_offset_y = Layout.text_name_withtitle.offset_y

local config_text_name_full_offset_x = Layout.text_name_full.offset_x
local config_text_name_full_offset_y = Layout.text_name_full.offset_y

local config_text_title_offset_x = Layout.text_title.offset_x
local config_text_title_offset_y = Layout.text_title.offset_y

local config_text_dan_offset_x = Layout.text_dan.offset_x
local config_text_dan_offset_y = Layout.text_dan.offset_y

local config_title_plate_offset_x = Layout.title_plate.offset_x
local config_title_plate_offset_y = Layout.title_plate.offset_y

local PLATE_ONLY = Layout.plate_only
local FLASH = Animation.flash
local FLASH_STARS = FLASH.stars
local FLASH_SLASH = FLASH.slash
local FLASH_BIG = FLASH.big_star

local config_titletypes = { "0", "1" }
local config_titleplate_effects = { }

local nameplate_count = 46

local base = nil
local dan_gradation = { }

local dan_plate = nil
local dan_plategradation = { }

local players = { }
local players_blue = nil
local players_ai   = nil

local title_plates = { { } }
local title_plate_star_big = { }
local title_plate_star_small = { }
local slash = nil

local title_stars_folders = { "1", "2", "3", "4" }
local title_stars = { { } }
-- the rarity stars are the difficulty bars' level star sprites and glitter the same way: each star pulses
-- additively over itself and two glints (Stars/glint.png) blink in turn beside it. STAR_SPOTS = each
-- overlay's star centres, sprite widths and clockwise rotations (the stars fan: left as drawn, middle
-- upright, right leaning the other way); the glint offsets are the bars' own, turned with each star.
local star_glint = nil
local star_time = 0
local RARITY = Layout.rarity_stars
local GLITTER = Animation.rarity_stars
local STAR_SPOTS = { RARITY.spots[1], RARITY.spots[2], RARITY.spots[3] }
STAR_SPOTS[4] = STAR_SPOTS[3]
local STAR_SHINE_MS, STAR_SHINE_OP = GLITTER.shine_period_ms, GLITTER.shine_opacity
local STAR_GLINT_MS, STAR_GLINT_OP = GLITTER.glint_period_ms, GLITTER.glint_opacity
local STAR_GLINT_OFFSETS = RARITY.glint_offsets
local STAR_TIME_WRAP = GLITTER.time_wrap_ms        -- a multiple of both periods

local title_badge_of_achievement = nil
local nameplates_achievement = {"134","135","136","78","66","71","44","11","215","218","220","225","229","234","239","246","251","256","260","292"}
local title_badge_of_team_member = nil
local nameplates_team_member = {"291"}

local font_name_normal_size = nil
local font_name_withtitle = nil
local font_name_full = nil
local font_title = nil
local font_dan = nil

local player_data = { nil, nil, nil, nil, nil }
-- Stores the raw text and color for each player slot so the text can be drawn at draw time
local name_text    = { nil, nil, nil, nil, nil }
local title_text   = { nil, nil, nil, nil, nil }
local dan_text     = { nil, nil, nil, nil, nil }  -- coloured dan string (gold or normal)
local dan_short_text = { nil, nil, nil, nil, nil }  -- plain dan string for the "only-dan" layout
local title_fg     = nil
local title_bg     = nil

local titleplate_counter = 0
local namePlateEffect_counter = 0

-- ────────────────────────────────────────────────────────────────────────────
-- Helpers
-- ────────────────────────────────────────────────────────────────────────────

local function tableContains(t, value)
	for i = 1, #t do
		if t[i] == value then return true end
	end
	return false
end

local function toOpacity(int255)
	return math.max(0.0, math.min(1.0, int255 / 255.0))
end

local function implDrawStar(scale, x, y, star_small)
	star_small:SetScale(scale, scale)
	star_small:DrawAtAnchor(x, y, "center")
end

local function implDrawStarFlash(x, y, titleTexIndex)
	local star_small = title_plate_star_small[titleTexIndex]
	if star_small == nil then return end

	local resX = 1920.0 / 1280.0
	local resY = 1080.0 / 720.0

	local c = namePlateEffect_counter
	for _, s in ipairs(FLASH_STARS) do
		if c >= s.from and c <= s.to then
			local scale = s.scale
			local fade = Config.optional(s, "fade_frames")
			if fade ~= nil then scale = s.scale - ((c - s.from) / fade * s.scale) end
			-- y_px: a y already in 1920x1080 pixels
			local y_px = Config.optional(s, "y_px")
			implDrawStar(scale, x + (s.x * resX), (y_px ~= nil) and (y + y_px) or (y + (s.y * resY)), star_small)
		end
	end
	local sl = FLASH_SLASH
	if c >= sl.from and c <= sl.to then
		slash:SetOpacity(math.min(1.0, (sl.start_opacity - (c - sl.from) * sl.opacity_drop_per_frame) / 255.0))
		slash:DrawAtAnchor(x + math.floor(((c - sl.from) * (sl.travel_x / sl.travel_frames)) * resX), y + math.floor(sl.y * resY), "topleft")
	end
	local bs = FLASH_BIG
	if c >= bs.from and c <= bs.to then
		local big = title_plate_star_big[titleTexIndex]
		if big ~= nil then
			local big_scale = 1.0
			if c < bs.grow_until then
				big_scale = (c - bs.from) / bs.grow_frames
				big:SetOpacity(1.0)
			else
				big:SetOpacity((255 - (c - bs.grow_until) * bs.fade_per_frame) / 255.0)
			end
			big:SetScale(big_scale, big_scale)
			big:DrawAtAnchor(x + math.floor(bs.x * resX), y + math.floor(bs.y * resY), "center")
		end
	end
end

local function implDrawTitleEffect(x, y, titleTexIndex)
	if titleTexIndex >= 1 and titleTexIndex <= #title_plates then
		if config_titleplate_effects[titleTexIndex] == "flash" then
			implDrawStarFlash(x, y, titleTexIndex)
		end
	end
end

local function implDrawRarityStars(o_x, o_y, opacity, rarity)
	local x = o_x
	local y = o_y + RARITY.offset_y
	local star_count = 0
	if rarity == 3 then star_count = 1
	elseif rarity == 4 then star_count = 2
	elseif rarity == 5 then star_count = 3
	elseif rarity >= 6 then star_count = 4
	end
	if star_count > 0 then
		local star_frame = 1 + math.ceil(titleplate_counter * (#title_stars[star_count] - 1))
		local tx_titlestar = title_stars[star_count][star_frame]
		local op = toOpacity(opacity)
		local ox, oy = x + config_title_plate_offset_x, y + config_title_plate_offset_y
		tx_titlestar:SetOpacity(op)
		tx_titlestar:DrawAtAnchor(ox, oy, "topleft")
		-- the glitter: each star's patch of the overlay pulses additively with its own phase, then its glints
		local spots = STAR_SPOTS[star_count]
		local t = star_time
		tx_titlestar:SetBlendMode("add")
		for i, spot in ipairs(spots) do
			local shine = 0.5 + 0.5 * math.sin(2 * math.pi * t / STAR_SHINE_MS + i * GLITTER.shine_phase_step)
			local half = math.floor(spot[3] / 2) + RARITY.spot_padding      -- room for the rotated sprite's box
			local sx, sy = spot[1] - half, math.max(0, spot[2] - half)
			tx_titlestar:SetOpacity(op * STAR_SHINE_OP * shine)
			tx_titlestar:DrawRectAtAnchor(ox + sx, oy + sy, sx, sy, 2 * half, 2 * half, "topleft")
		end
		tx_titlestar:SetBlendMode("normal")
		tx_titlestar:SetOpacity(1)
		if star_glint ~= nil then
			star_glint:SetBlendMode("add")
			for i, spot in ipairs(spots) do
				local k = spot[3] / RARITY.glint_base_w
				local rot = math.rad(spot[4] or 0)
				local cr, sr = math.cos(rot), math.sin(rot)
				for g, off in ipairs(STAR_GLINT_OFFSETS) do
					local gp = (t / STAR_GLINT_MS + i * GLITTER.glint_phase_step + g * GLITTER.glint_pair_phase) % 1
					local blink = math.sin(gp * math.pi)
					blink = blink * blink * blink
					if blink > GLITTER.glint_min_blink then
						-- the bars' offset turned with this star (clockwise on screen, y down)
						local dx = (off[1] * cr - off[2] * sr) * k
						local dy = (off[1] * sr + off[2] * cr) * k
						local gs = (GLITTER.glint_min_scale + GLITTER.glint_scale_grow * blink) * k
						star_glint:SetRotation((spot[4] or 0) + gp * GLITTER.glint_turn_deg)
						star_glint:SetScale(gs, gs)
						star_glint:SetOpacity(op * STAR_GLINT_OP * blink)
						star_glint:DrawAtAnchor(ox + math.floor(spot[1] + dx), oy + math.floor(spot[2] + dy), "center")
					end
				end
			end
			star_glint:SetBlendMode("normal")
			star_glint:SetRotation(0)
			star_glint:SetScale(1, 1)
			star_glint:SetOpacity(1)
		end
	end
end

local function implDrawBadges(x, y, opacity, nameplateId)
	if tableContains(nameplates_achievement, nameplateId) then
		title_badge_of_achievement:SetOpacity(toOpacity(opacity))
		title_badge_of_achievement:DrawAtAnchor(x + config_title_plate_offset_x, y + config_title_plate_offset_y, "topleft")
	elseif tableContains(nameplates_team_member, nameplateId) then
		title_badge_of_team_member:SetOpacity(toOpacity(opacity))
		title_badge_of_team_member:DrawAtAnchor(x + config_title_plate_offset_x, y + config_title_plate_offset_y, "topleft")
	end
end

local function implDrawTitlePlate(x, y, opacity, titleTexIndex)
	if titleTexIndex >= 1 and titleTexIndex <= #title_plates then
		local titleplate_frame = 1 + math.ceil(titleplate_counter * (#title_plates[titleTexIndex] - 1))
		local tx_titleplate = title_plates[titleTexIndex][titleplate_frame]
		tx_titleplate:SetOpacity(toOpacity(opacity))
		tx_titleplate:DrawAtAnchor(x + config_title_plate_offset_x, y + config_title_plate_offset_y, "topleft")
	end
end

local function implDrawPlayerRing(x, y, opacity, player_lua, side_lua)
	if CONFIG.IsAIBattleMode and player_lua == 2 then
		players_ai:SetOpacity(toOpacity(opacity))
		players_ai:DrawAtAnchor(x, y, "topleft")
	elseif player_lua == 1 and side_lua == 2 then
		players_blue:SetOpacity(toOpacity(opacity))
		players_blue:DrawAtAnchor(x, y, "topleft")
	else
		players[player_lua]:SetOpacity(toOpacity(opacity))
		players[player_lua]:DrawAtAnchor(x, y, "topleft")
	end
end

local function drawDanTitlePlate(x, y, opacity, danType)
	dan_plate:SetOpacity(toOpacity(opacity))
	dan_plategradation[danType]:SetOpacity(toOpacity(opacity))
	dan_plate:DrawAtAnchor(x + config_title_plate_offset_x, y + config_title_plate_offset_y, "topleft")
	dan_plategradation[danType]:DrawAtAnchor(x + config_title_plate_offset_x, y + config_title_plate_offset_y, "topleft")
end

-- ────────────────────────────────────────────────────────────────────────────
-- ROActivity lifecycle
-- ────────────────────────────────────────────────────────────────────────────

function onStart()
	for i = 0, nameplate_count - 1 do
		config_titletypes[i + 1] = tostring(i)
	end

	base     = TEXTURE:CreateTexture(TEXTURES_DIR .. "Base.png")
	dan_base = TEXTURE:CreateTexture(TEXTURES_DIR .. "Dan_Base.png")
	slash    = TEXTURE:CreateTexture(TEXTURES_DIR .. "Shines/Slash.png")

	title_badge_of_achievement  = TEXTURE:CreateTexture(TEXTURES_DIR .. "Badges/0.png")
	title_badge_of_team_member  = TEXTURE:CreateTexture(TEXTURES_DIR .. "Badges/1.png")

	dan_plate = TEXTURE:CreateTexture(TEXTURES_DIR .. "Title_Dan/0.png")
	for i = 1, 3 do
		dan_gradation[i]     = TEXTURE:CreateTexture(TEXTURES_DIR .. "Dan_" .. dan_types[i] .. ".png")
		dan_plategradation[i] = TEXTURE:CreateTexture(TEXTURES_DIR .. "Title_Dan/" .. dan_types[i] .. ".png")
	end

	for i = 1, 5 do
		players[i] = TEXTURE:CreateTexture(TEXTURES_DIR .. tostring(i) .. "P.png")
	end
	players_blue = TEXTURE:CreateTexture(TEXTURES_DIR .. "1P_Blue.png")
	players_ai   = TEXTURE:CreateTexture(TEXTURES_DIR .. "AI.png")

	-- Title plates
	for i = 1, #config_titletypes do
		local titledir = TEXTURES_DIR .. "Title/" .. config_titletypes[i]
		local titleplate_config = Config.optional(Layout.titles, config_titletypes[i])

		config_titleplate_effects[i] = "none"
		local config_titleplate_framecount = 0

		if titleplate_config ~= nil then
			config_titleplate_effects[i] = titleplate_config.effect
			config_titleplate_framecount  = titleplate_config.frame_count
		end

		local titleplates = { }
		for j = 0, config_titleplate_framecount do
			titleplates[j + 1] = TEXTURE:CreateTexture(titledir .. "/" .. j .. ".png")
		end
		title_plates[i] = titleplates

		if config_titleplate_effects[i] == "flash" then
			title_plate_star_small[i] = TEXTURE:CreateTexture(titledir .. "/Small.png")
			title_plate_star_big[i]   = TEXTURE:CreateTexture(titledir .. "/Big.png")
		end
	end

	-- Rarity stars
	for i = 1, #title_stars_folders do
		local stardir = TEXTURES_DIR .. "Stars/" .. title_stars_folders[i]
		local title_stars_config = Config.optional(Layout.stars, title_stars_folders[i])
		local config_stars_framecount = 0
		if title_stars_config ~= nil then
			config_stars_framecount = title_stars_config.frame_count
		end
		local stars = { }
		for j = 0, config_stars_framecount do
			stars[j + 1] = TEXTURE:CreateTexture(stardir .. "/" .. j .. ".png")
		end
		title_stars[i] = stars
	end
	star_glint = TEXTURE:CreateTexture(TEXTURES_DIR .. "Stars/glint.png")

	-- glyph-composed fonts (bounded per-character cache; the maxsize clamp becomes the per-letter squish)
	font_name_normal_size = TEXT:CreateGlyphCached(config_font_name_normal_size, "regular")
	font_name_withtitle   = TEXT:CreateGlyphCached(config_font_name_withtitle_size, "regular")
	font_name_full        = TEXT:CreateGlyphCached(config_font_name_full_size, "regular")
	font_title            = TEXT:CreateGlyphCached(config_font_title_size, "regular")
	font_dan              = TEXT:CreateGlyphCached(config_font_dan_size, "regular")

	-- Cached color objects for title plate text (black on transparent)
	title_fg = Config.color(Layout.title_text_color)
	title_bg = Config.color(Layout.title_text_back_color)
end

function activate() end
-- activate(player, name, title, dan, data) — replaces setInfos; stores player info for draw
function activate(player, name, title, dan, data)
	local player_lua = player + 1
	player_data[player_lua] = data

	name_text[player_lua]      = name
	title_text[player_lua]     = title
	dan_text[player_lua]       = dan
	dan_short_text[player_lua] = dan
end

function deactivate() end

function update()
	titleplate_counter = titleplate_counter + (Animation.title_cycles_per_s * fps.deltaTime)
	if titleplate_counter >= 1 then titleplate_counter = 0 end

	namePlateEffect_counter = namePlateEffect_counter + (60 * fps.deltaTime)
	if namePlateEffect_counter >= FLASH.loop_frames then namePlateEffect_counter = 0 end

	star_time = (star_time + fps.deltaTime * 1000) % STAR_TIME_WRAP
end

-- ── Shared full-nameplate renderer ───────────────────────────────────────────
-- Modes 0, 3, 4, 5 all route through here.  Pass nil for any override to fall
-- back to the value stored in player_data / the activate()-cached text tables.
-- danGradeOvr is 0-based (same convention as DanType); it is clamped to [1,3] internally.
local function implDrawFullNameplate(x, y, opacity, player_lua, side_lua,
		titleTextOvr, titleTypeOvr, rarityIntOvr, nameplateIdOvr,
		danTextOvr, danGradeOvr, playerNameOvr)

	local data = player_data[player_lua]
	if data == nil then return end
	local op = toOpacity(opacity)

	-- Resolve each field: use override when provided, else activate()-cached / player_data value.
	local titleText   = titleTextOvr   ~= nil and titleTextOvr   or title_text[player_lua]
	local titleType   = titleTypeOvr   ~= nil and titleTypeOvr   or data.TitleType
	local rarityInt   = rarityIntOvr   ~= nil and rarityIntOvr   or data.TitleRarityInt
	local nameplateId = nameplateIdOvr ~= nil and nameplateIdOvr or (data.TitleId or "")
	local danText     = danTextOvr     ~= nil and danTextOvr     or dan_text[player_lua]
	local danShortTxt = danTextOvr     ~= nil and danTextOvr     or dan_short_text[player_lua]
	-- In AI battle the dan reflects the current AI level, which may change during song select.
	-- Read it live so the nameplate stays in sync without any cross-state refresh.
	if CONFIG.IsAIBattleMode and player_lua == 2 then
		local stages = {"初", "二", "三", "四", "五", "六", "七", "八", "九", "極"}
		local aiDan = stages[math.max(1, math.min(10, CONFIG.AILevel))] .. "面"
		danText     = aiDan
		danShortTxt = aiDan
	end
	local danGradeIdx = danGradeOvr    ~= nil
	                    and math.max(1, math.min(3, danGradeOvr + 1))
	                    or  (data.DanType + 1)
	local dispName    = (playerNameOvr ~= nil and playerNameOvr ~= "")
	                    and playerNameOvr or name_text[player_lua]

	local _notitle        = (titleText == nil or titleText == "")
	local _nodan          = (danText   == nil or danText   == "")
	local titleplate_index = titleType + 1

	base:SetOpacity(op) ; base:DrawAtAnchor(x, y, "topleft")

	if not _notitle then
		implDrawTitlePlate(x, y, opacity, titleplate_index)
	elseif not _nodan then
		drawDanTitlePlate(x, y, opacity, danGradeIdx)
	end

	implDrawRarityStars(x, y, opacity, rarityInt)
	implDrawBadges(x, y, opacity, nameplateId)

	if not _nodan and not _notitle then
		dan_base:SetOpacity(op) ; dan_base:DrawAtAnchor(x, y, "topleft")
		dan_gradation[danGradeIdx]:SetOpacity(op) ; dan_gradation[danGradeIdx]:DrawAtAnchor(x, y, "topleft")
	end

	implDrawTitleEffect(x, y, titleplate_index)
	implDrawPlayerRing(x, y, opacity, player_lua, side_lua)

	-- Dan text (small, inside the dan ring)
	if not _nodan and not _notitle then
		font_dan:Draw(danText, x + config_text_dan_offset_x, y + config_text_dan_offset_y,
			nil, nil, op, 1, config_font_dan_maxsize, "center")
	end

	-- Name / title text (three layout variants depending on _notitle / _nodan)
	if not _nodan and _notitle then
		-- Dan title replaces the nameplate title slot; name shown in "with-title" size
		font_title:Draw(danShortTxt, x + config_text_title_offset_x, y + config_text_title_offset_y,
			nil, nil, op, 1, config_font_name_normal_maxsize, "center")
		font_name_withtitle:Draw(dispName, x + config_text_name_withtitle_offset_x, y + config_text_name_withtitle_offset_y,
			nil, nil, op, 1, config_font_name_withtitle_maxsize, "center")
	elseif _notitle then
		-- No title plate and no dan: name takes the full nameplate area
		font_name_normal_size:Draw(dispName, x + config_text_name_normal_offset_x, y + config_text_name_normal_offset_y,
			nil, nil, op, 1, config_font_name_normal_maxsize, "center")
	else
		-- Full layout: title plate text + name (smaller when dan is also present)
		font_title:Draw(titleText, x + config_text_title_offset_x, y + config_text_title_offset_y,
			title_fg, title_bg, op, 1, config_font_title_maxsize, "center")
		if _nodan then
			font_name_withtitle:Draw(dispName, x + config_text_name_withtitle_offset_x, y + config_text_name_withtitle_offset_y,
				nil, nil, op, 1, config_font_name_withtitle_maxsize, "center")
		else
			font_name_full:Draw(dispName, x + config_text_name_full_offset_x, y + config_text_name_full_offset_y,
				nil, nil, op, 1, config_font_name_full_maxsize, "center")
		end
	end
end

-- draw(mode, ...) — unified draw entry point.
--   mode 0: full player nameplate                    → draw(0, x, y, opacity, player, side)
--   mode 1: dan plate only                           → draw(1, x, y, opacity, danType, titleTex)
--   mode 2: title plate only                         → draw(2, x, y, opacity, titletype, titleTex, rarityInt, nameplateId)
--   mode 3: full nameplate, title override           → draw(3, x, y, opacity, player, side, titleText, titleType, rarityInt, nameplateId[, playerNameOverride])
--   mode 4: full nameplate, dan override             → draw(4, x, y, opacity, player, side, danText, danGrade[, playerNameOverride])
--   mode 5: full nameplate, player-name override     → draw(5, x, y, opacity, player, side, playerName)
--   mode 6: full nameplate, all overrides            → draw(6, x, y, opacity, player, side, titleText, titleType, rarityInt, nameplateId, danText, danGrade, playerName)
function draw(mode, ...)
	local args = {...}

	if mode == 0 then
		-- All fields from player_data / activate cache — no overrides.
		local x, y, opacity, player, side = args[1], args[2], args[3], args[4], args[5]
		implDrawFullNameplate(x, y, opacity, player + 1, side + 1,
			nil, nil, nil, nil, nil, nil, nil)

	elseif mode == 1 then
		-- ── Dan plate only ──
		local o_x, o_y, opacity, danType, titleTex = args[1], args[2], args[3], args[4], args[5]
		local x  = o_x + PLATE_ONLY.offset_x
		local y  = o_y + PLATE_ONLY.offset_y
		local op = toOpacity(opacity)

		base:SetOpacity(op)
		base:DrawAtAnchor(x, y, "topleft")

		dan_base:SetOpacity(op)
		dan_base:DrawAtAnchor(x, y, "topleft")
		dan_gradation[danType + 1]:SetOpacity(op)
		dan_gradation[danType + 1]:DrawAtAnchor(x, y, "topleft")

		implDrawPlayerRing(x, y, opacity, 1, 1)

		if titleTex ~= nil then
			titleTex:SetScale(math.min(config_font_dan_maxsize / titleTex.Width, 1.0), 1.0)
			titleTex:SetOpacity(op)
			titleTex:DrawAtAnchor(x + config_text_dan_offset_x, y + config_text_dan_offset_y, "center")
		end

	elseif mode == 3 then
		-- Title override; optional player-name override as 10th arg.
		local x, y, opacity, player, side, titleText, titleType, rarityInt, nameplateId, playerName =
			args[1], args[2], args[3], args[4], args[5], args[6], args[7], args[8], args[9], args[10]
		implDrawFullNameplate(x, y, opacity, player + 1, side + 1,
			titleText, titleType, rarityInt, nameplateId, nil, nil, playerName)

	elseif mode == 4 then
		-- Dan override; optional player-name override as 8th arg.
		local x, y, opacity, player, side, danText, danGrade, playerName =
			args[1], args[2], args[3], args[4], args[5], args[6], args[7], args[8]
		implDrawFullNameplate(x, y, opacity, player + 1, side + 1,
			nil, nil, nil, nil, danText, danGrade, playerName)

	elseif mode == 5 then
		-- Player-name override.
		local x, y, opacity, player, side, playerName =
			args[1], args[2], args[3], args[4], args[5], args[6]
		implDrawFullNameplate(x, y, opacity, player + 1, side + 1,
			nil, nil, nil, nil, nil, nil, playerName)

	elseif mode == 6 then
		-- All overrides: title + dan + player name simultaneously.
		local x, y, opacity, player, side, titleText, titleType, rarityInt, nameplateId, danText, danGrade, playerName =
			args[1], args[2], args[3], args[4], args[5], args[6], args[7], args[8], args[9], args[10], args[11], args[12]
		implDrawFullNameplate(x, y, opacity, player + 1, side + 1,
			titleText, titleType, rarityInt, nameplateId, danText, danGrade, playerName)

	elseif mode == 2 then
		-- ── Title plate only ──
		local o_x, o_y, opacity, titletype, titleTex, rarityInt, nameplateId =
			args[1], args[2], args[3], args[4], args[5], args[6], args[7]
		local x  = o_x + PLATE_ONLY.offset_x
		local y  = o_y + PLATE_ONLY.offset_y
		local op = toOpacity(opacity)

		base:SetOpacity(op)
		base:DrawAtAnchor(x, y, "topleft")

		implDrawTitlePlate(x, y, opacity, titletype + 1)
		implDrawRarityStars(x, y, opacity, rarityInt)
		implDrawBadges(x, y, opacity, nameplateId)
		implDrawTitleEffect(x, y, titletype + 1)

		implDrawPlayerRing(x, y, opacity, 1, 1)

		if titleTex ~= nil then
			titleTex:SetScale(math.min(config_font_title_maxsize / titleTex.Width, 1.0), 1.0)
			titleTex:SetOpacity(op)
			titleTex:DrawAtAnchor(x + config_text_title_offset_x, y + config_text_title_offset_y, "center")
		end
	end
end

function reloadLanguage(lang) end
function afterSongEnum() end
function onDestroy() end
