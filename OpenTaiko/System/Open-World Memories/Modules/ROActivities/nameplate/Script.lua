-- NamePlate ROActivity
-- Ported from Modules/NamePlate/Script.lua to the new Lua API.
-- Textures and config are loaded from the original Modules/NamePlate/ location.

local TEXTURES_DIR  = "Textures/"

local dan_types = { "Clear", "FC", "AP" }

local config = nil

local config_font_name_normal_size = nil
local config_font_name_normal_maxsize = nil

local config_font_name_withtitle_size = nil
local config_font_name_withtitle_maxsize = nil

local config_font_name_full_size = nil
local config_font_name_full_maxsize = nil

local config_font_title_size = nil
local config_font_title_maxsize = nil

local config_font_dan_size = nil
local config_font_dan_maxsize = nil

local config_text_name_normal_offset_x = nil
local config_text_name_normal_offset_y = nil

local config_text_name_withtitle_offset_x = nil
local config_text_name_withtitle_offset_y = nil

local config_text_name_full_offset_x = nil
local config_text_name_full_offset_y = nil

local config_text_title_offset_x = nil
local config_text_title_offset_y = nil

local config_text_dan_offset_x = nil
local config_text_dan_offset_y = nil

local config_title_plate_offset_x = nil
local config_title_plate_offset_y = nil

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
-- the rarity stars are the difficulty bars' level star sprites (31x34, recoloured by
-- tools/gen_nameplate_stars.py) and glitter the same way: each star pulses additively over itself and
-- two glints (Stars/glint.png) blink in turn beside it. STAR_SPOTS = each overlay's star centres, sprite
-- widths and the clockwise rotation the generator gave that star (the stars fan: left as drawn, middle
-- upright, right leaning the other way); the glint offsets are the bars' own, turned with each star.
local star_glint = nil
local star_time = 0
local STAR_SPOTS = {
	{ { 186, 23, 31, 16 } },
	{ { 160, 23, 31, 0 }, { 202, 23, 31, 32 } },
	{ { 153, 28, 31, 0 }, { 186, 23, 31, 16 }, { 219, 28, 31, 32 } },
}
STAR_SPOTS[4] = STAR_SPOTS[3]
local STAR_SHINE_MS, STAR_SHINE_OP = 900, 0.45
local STAR_GLINT_MS, STAR_GLINT_OP = 1500, 0.95
local STAR_GLINT_OFFSETS = { { 9, -11 }, { -9, 6 } }
local STAR_TIME_WRAP = 45000                       -- a multiple of both periods

local title_badge_of_achievement = nil
local nameplates_achievement = {134,135,136,78,66,71,44,11,215,218,220,225,229,234,239,246,251,256,260,292}
local title_badge_of_team_member = nil
local nameplates_team_member = {291}

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
	if c <= 10 then
		implDrawStar(1.0 - (c / 10), x + (63 * resX), y + (25 * resY), star_small)
	end
	if c >= 3 and c <= 10 then
		implDrawStar(1.0 - ((c - 3) / 7), x + (38 * resX), y + (7 * resY), star_small)
	end
	if c >= 6 and c <= 10 then
		implDrawStar(1.0 - ((c - 6) / 4), x + (51 * resX), y + (5 * resY), star_small)
	end
	if c >= 8 and c <= 10 then
		implDrawStar(0.3 - ((c - 8) / 2 * 0.3), x + (110 * resX), y + (25 * resY), star_small)
	end
	if c >= 11 and c <= 13 then
		implDrawStar(1.0 - ((c - 11) / 2), x + (38 * resX), y + (7 * resY), star_small)
	end
	if c >= 11 and c <= 15 then
		implDrawStar(1.0, x + (51 * resX), y + 5, star_small)
	end
	if c >= 11 and c <= 17 then
		implDrawStar(1.0 - ((c - 11) / 7), x + (110 * resX), y + (25 * resY), star_small)
	end
	if c >= 16 and c <= 20 then
		implDrawStar(0.2 - ((c - 16) / 4 * 0.2), x + (63 * resX), y + (25 * resY), star_small)
	end
	if c >= 17 and c <= 20 then
		implDrawStar(1.0 - ((c - 17) / 3), x + (99 * resX), y + (1 * resY), star_small)
	end
	if c >= 20 and c <= 24 then
		implDrawStar(0.4, x + (63 * resX), y + 25, star_small)
	end
	if c >= 20 and c <= 25 then
		implDrawStar(1.0, x + (99 * resX), y + 1, star_small)
	end
	if c >= 20 and c <= 30 then
		implDrawStar(0.5 - ((c - 20) / 10 * 0.5), x + (152 * resX), y + (7 * resY), star_small)
	end
	if c >= 31 and c <= 37 then
		implDrawStar(0.5 - ((c - 31) / 6 * 0.5), x + (176 * resX), y + (8 * resY), star_small)
		implDrawStar(1.0 - ((c - 31) / 6), x + (175 * resX), y + (25 * resY), star_small)
	end
	if c >= 31 and c <= 40 then
		implDrawStar(0.9 - ((c - 31) / 9 * 0.9), x + (136 * resX), y + (24 * resY), star_small)
	end
	if c >= 34 and c <= 40 then
		implDrawStar(0.7 - ((c - 34) / 6 * 0.7), x + (159 * resX), y + (25 * resY), star_small)
	end
	if c >= 41 and c <= 42 then
		implDrawStar(0.7, x + (159 * resX), y + (25 * resY), star_small)
	end
	if c >= 43 and c <= 50 then
		implDrawStar(0.8 - ((c - 43) / 7 * 0.8), x + (196 * resX), y + (23 * resY), star_small)
	end
	if c >= 51 and c <= 57 then
		implDrawStar(0.8 - ((c - 51) / 6 * 0.8), x + (51 * resX), y + (5 * resY), star_small)
	end
	if c >= 51 and c <= 52 then
		implDrawStar(0.2, x + (166 * resX), y + (22 * resY), star_small)
	end
	if c >= 51 and c <= 53 then
		implDrawStar(0.8, x + (136 * resX), y + (24 * resY), star_small)
	end
	if c >= 51 and c <= 55 then
		implDrawStar(1.0, x + (176 * resX), y + (8 * resY), star_small)
	end
	if c >= 61 and c <= 70 then
		implDrawStar(1.0 - ((c - 61) / 9), x + (196 * resX), y + (23 * resY), star_small)
	end
	if c >= 61 and c <= 67 then
		implDrawStar(0.7 - ((c - 61) / 6 * 0.7), x + (214 * resX), y + (14 * resY), star_small)
	end
	if c >= 63 and c <= 70 then
		implDrawStar(0.5 - ((c - 63) / 7 * 0.5), x + (129 * resX), y + (24 * resY), star_small)
	end
	if c >= 65 and c <= 70 then
		implDrawStar(0.8 - ((c - 65) / 5 * 0.8), x + (117 * resX), y + (7 * resY), star_small)
	end
	if c >= 71 and c <= 72 then
		implDrawStar(0.8, x + (151 * resX), y + (25 * resY), star_small)
	end
	if c >= 71 and c <= 74 then
		implDrawStar(0.8, x + (117 * resX), y + (7 * resY), star_small)
	end
	if c >= 85 and c <= 112 then
		slash:SetOpacity(math.min(1.0, (1400 - (c - 85) * 50) / 255.0))
		slash:Draw(math.floor(x + (((c - 85) * (150 / 27)) * (1920.0 / 1280.0))), math.floor(y + (7 * (1080.0 / 720.0))))
	end
	if c >= 105 and c <= 120 then
		local big = title_plate_star_big[titleTexIndex]
		if big ~= nil then
			local big_scale = 1.0
			if c < 112 then
				big_scale = (c - 105) / 8
				big:SetOpacity(1.0)
			else
				big:SetOpacity((255 - (c - 112) * 31.875) / 255.0)
			end
			big:SetScale(big_scale, big_scale)
			big:DrawAtAnchor(math.floor(x + (193 * (1920.0 / 1280.0))), math.floor(y + (6 * (1080.0 / 720.0))), "center")
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
	local y = o_y - 20
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
		tx_titlestar:Draw(ox, oy)
		-- the glitter: each star's patch of the overlay pulses additively with its own phase, then its glints
		local spots = STAR_SPOTS[star_count]
		local t = star_time
		tx_titlestar:SetBlendMode("add")
		for i, spot in ipairs(spots) do
			local shine = 0.5 + 0.5 * math.sin(2 * math.pi * t / STAR_SHINE_MS + i * 1.3)
			local half = math.floor(spot[3] / 2) + 5      -- room for the rotated sprite's box
			local sx, sy = spot[1] - half, math.max(0, spot[2] - half)
			tx_titlestar:SetOpacity(op * STAR_SHINE_OP * shine)
			tx_titlestar:DrawRectAtAnchor(ox + sx, oy + sy, sx, sy, 2 * half, 2 * half, "topleft")
		end
		tx_titlestar:SetBlendMode("normal")
		tx_titlestar:SetOpacity(1)
		if star_glint ~= nil then
			star_glint:SetBlendMode("add")
			for i, spot in ipairs(spots) do
				local k = spot[3] / 31
				local rot = math.rad(spot[4] or 0)
				local cr, sr = math.cos(rot), math.sin(rot)
				for g, off in ipairs(STAR_GLINT_OFFSETS) do
					local gp = (t / STAR_GLINT_MS + i * 0.37 + g * 0.5) % 1
					local blink = math.sin(gp * math.pi)
					blink = blink * blink * blink
					if blink > 0.02 then
						-- the bars' offset turned with this star (clockwise on screen, y down)
						local dx = (off[1] * cr - off[2] * sr) * k
						local dy = (off[1] * sr + off[2] * cr) * k
						star_glint:SetRotation((spot[4] or 0) + gp * 90)
						star_glint:SetScale((0.6 + 0.6 * blink) * k, (0.6 + 0.6 * blink) * k)
						star_glint:SetOpacity(op * STAR_GLINT_OP * blink)
						star_glint:DrawAtAnchor(math.floor(ox + spot[1] + dx), math.floor(oy + spot[2] + dy), "center")
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
		title_badge_of_achievement:Draw(x + config_title_plate_offset_x, y + config_title_plate_offset_y)
	elseif tableContains(nameplates_team_member, nameplateId) then
		title_badge_of_team_member:SetOpacity(toOpacity(opacity))
		title_badge_of_team_member:Draw(x + config_title_plate_offset_x, y + config_title_plate_offset_y)
	end
end

local function implDrawTitlePlate(x, y, opacity, titleTexIndex)
	if titleTexIndex >= 1 and titleTexIndex <= #title_plates then
		local titleplate_frame = 1 + math.ceil(titleplate_counter * (#title_plates[titleTexIndex] - 1))
		local tx_titleplate = title_plates[titleTexIndex][titleplate_frame]
		tx_titleplate:SetOpacity(toOpacity(opacity))
		tx_titleplate:Draw(x + config_title_plate_offset_x, y + config_title_plate_offset_y)
	end
end

local function implDrawPlayerRing(x, y, opacity, player_lua, side_lua)
	if CONFIG.IsAIBattleMode and player_lua == 2 then
		players_ai:SetOpacity(toOpacity(opacity))
		players_ai:Draw(x, y)
	elseif player_lua == 1 and side_lua == 2 then
		players_blue:SetOpacity(toOpacity(opacity))
		players_blue:Draw(x, y)
	else
		players[player_lua]:SetOpacity(toOpacity(opacity))
		players[player_lua]:Draw(x, y)
	end
end

local function drawDanTitlePlate(x, y, opacity, danType)
	dan_plate:SetOpacity(toOpacity(opacity))
	dan_plategradation[danType]:SetOpacity(toOpacity(opacity))
	dan_plate:Draw(x + config_title_plate_offset_x, y + config_title_plate_offset_y)
	dan_plategradation[danType]:Draw(x + config_title_plate_offset_x, y + config_title_plate_offset_y)
end

-- ────────────────────────────────────────────────────────────────────────────
-- ROActivity lifecycle
-- ────────────────────────────────────────────────────────────────────────────

function onStart()
	config = JSONLOADER:LoadJson("Config.json")

	config_font_name_normal_size    = JSONLOADER:ExtractNumber(config["font_name_normal"]["size"])
	config_font_name_normal_maxsize = JSONLOADER:ExtractNumber(config["font_name_normal"]["maxsize"])

	config_font_name_withtitle_size    = JSONLOADER:ExtractNumber(config["font_name_withtitle"]["size"])
	config_font_name_withtitle_maxsize = JSONLOADER:ExtractNumber(config["font_name_withtitle"]["maxsize"])

	config_font_name_full_size    = JSONLOADER:ExtractNumber(config["font_name_full"]["size"])
	config_font_name_full_maxsize = JSONLOADER:ExtractNumber(config["font_name_full"]["maxsize"])

	config_font_title_size    = JSONLOADER:ExtractNumber(config["font_title"]["size"])
	config_font_title_maxsize = JSONLOADER:ExtractNumber(config["font_title"]["maxsize"])

	config_font_dan_size    = JSONLOADER:ExtractNumber(config["font_dan"]["size"])
	config_font_dan_maxsize = JSONLOADER:ExtractNumber(config["font_dan"]["maxsize"])

	config_text_name_normal_offset_x = JSONLOADER:ExtractNumber(config["text_name_normal"]["offset_x"])
	config_text_name_normal_offset_y = JSONLOADER:ExtractNumber(config["text_name_normal"]["offset_y"])

	config_text_name_withtitle_offset_x = JSONLOADER:ExtractNumber(config["text_name_withtitle"]["offset_x"])
	config_text_name_withtitle_offset_y = JSONLOADER:ExtractNumber(config["text_name_withtitle"]["offset_y"])

	config_text_name_full_offset_x = JSONLOADER:ExtractNumber(config["text_name_full"]["offset_x"])
	config_text_name_full_offset_y = JSONLOADER:ExtractNumber(config["text_name_full"]["offset_y"])

	config_text_title_offset_x = JSONLOADER:ExtractNumber(config["text_title"]["offset_x"])
	config_text_title_offset_y = JSONLOADER:ExtractNumber(config["text_title"]["offset_y"])

	config_text_dan_offset_x = JSONLOADER:ExtractNumber(config["text_dan"]["offset_x"])
	config_text_dan_offset_y = JSONLOADER:ExtractNumber(config["text_dan"]["offset_y"])

	config_title_plate_offset_x = JSONLOADER:ExtractNumber(config["title_plate"]["offset_x"])
	config_title_plate_offset_y = JSONLOADER:ExtractNumber(config["title_plate"]["offset_y"])

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
		local titleplate_config = config["titles"][config_titletypes[i]]

		config_titleplate_effects[i] = "none"
		local config_titleplate_framecount = 0

		if titleplate_config ~= nil then
			config_titleplate_effects[i] = JSONLOADER:ExtractText(titleplate_config["effect"])
			config_titleplate_framecount  = JSONLOADER:ExtractNumber(titleplate_config["framecount"])
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
		local title_stars_config = config["stars"][title_stars_folders[i]]
		local config_stars_framecount = 0
		if title_stars_config ~= nil then
			config_stars_framecount = JSONLOADER:ExtractNumber(title_stars_config["framecount"])
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
	title_fg = COLOR:CreateColorFromRGBA(0, 0, 0, 255)
	title_bg = COLOR:CreateColorFromRGBA(0, 0, 0, 0)
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
	titleplate_counter = titleplate_counter + (3.3 * fps.deltaTime)
	if titleplate_counter >= 1 then titleplate_counter = 0 end

	namePlateEffect_counter = namePlateEffect_counter + (60 * fps.deltaTime)
	if namePlateEffect_counter >= 120 then namePlateEffect_counter = 0 end

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
	local nameplateId = nameplateIdOvr ~= nil and nameplateIdOvr or (data.TitleId or -1)
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

	base:SetOpacity(op) ; base:Draw(x, y)

	if not _notitle then
		implDrawTitlePlate(x, y, opacity, titleplate_index)
	elseif not _nodan then
		drawDanTitlePlate(x, y, opacity, danGradeIdx)
	end

	implDrawRarityStars(x, y, opacity, rarityInt)
	implDrawBadges(x, y, opacity, nameplateId)

	if not _nodan and not _notitle then
		dan_base:SetOpacity(op) ; dan_base:Draw(x, y)
		dan_gradation[danGradeIdx]:SetOpacity(op) ; dan_gradation[danGradeIdx]:Draw(x, y)
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
		local x  = o_x - 180
		local y  = o_y + 5
		local op = toOpacity(opacity)

		base:SetOpacity(op)
		base:Draw(x, y)

		dan_base:SetOpacity(op)
		dan_base:Draw(x, y)
		dan_gradation[danType + 1]:SetOpacity(op)
		dan_gradation[danType + 1]:Draw(x, y)

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
		local x  = o_x - 180
		local y  = o_y + 5
		local op = toOpacity(opacity)

		base:SetOpacity(op)
		base:Draw(x, y)

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
