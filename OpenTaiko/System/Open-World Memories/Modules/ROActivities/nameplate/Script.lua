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

local title_plates = { { } }
local title_plate_star_big = { }
local title_plate_star_small = { }
local slash = nil

local title_stars_folders = { "1", "2", "3", "4" }
local title_stars = { { } }

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
-- Stores the raw text and color for each player slot so GetText can be called at draw time
local name_text    = { nil, nil, nil, nil, nil }
local title_text   = { nil, nil, nil, nil, nil }
local dan_text     = { nil, nil, nil, nil, nil }  -- coloured dan string (gold or normal)
local dan_short_text = { nil, nil, nil, nil, nil }  -- plain dan string for the "only-dan" layout
local title_fg     = nil
local title_bg     = nil

local notitle = { false, false, false, false, false }
local nodan   = { false, false, false, false, false }

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
		tx_titlestar:SetOpacity(toOpacity(opacity))
		tx_titlestar:Draw(x + config_title_plate_offset_x, y + config_title_plate_offset_y)
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
	if player_lua == 1 and side_lua == 2 then
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

	font_name_normal_size = TEXT:Create(config_font_name_normal_size, "regular")
	font_name_withtitle   = TEXT:Create(config_font_name_withtitle_size, "regular")
	font_name_full        = TEXT:Create(config_font_name_full_size, "regular")
	font_title            = TEXT:Create(config_font_title_size, "regular")
	font_dan              = TEXT:Create(config_font_dan_size, "regular")

	-- Cached color objects for title plate text (black on transparent)
	title_fg = COLOR:CreateColorFromRGBA(0, 0, 0, 255)
	title_bg = COLOR:CreateColorFromRGBA(0, 0, 0, 0)
end

function activate() end
-- activate(player, name, title, dan, data) — replaces setInfos; stores player info for draw
function activate(player, name, title, dan, data)
	local player_lua = player + 1
	player_data[player_lua] = data

	notitle[player_lua] = (title == "")
	nodan[player_lua]   = (data.Dan == nil or data.Dan == "")

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
end

-- draw(mode, ...) — unified draw entry point.
--   mode 0: full player nameplate  → draw(0, x, y, opacity, player, side)
--   mode 1: dan plate only         → draw(1, x, y, opacity, danType, titleTex)
--   mode 2: title plate only       → draw(2, x, y, opacity, titletype, titleTex, rarityInt, nameplateId)
function draw(mode, ...)
	local args = {...}

	if mode == 0 then
		-- ── Full player nameplate ──
		local x, y, opacity, player, side = args[1], args[2], args[3], args[4], args[5]
		local player_lua = player + 1
		local side_lua   = side + 1
		local data       = player_data[player_lua]
		if data == nil then return end

		local rarityInt   = data.TitleRarityInt
		local nameplateId = data.TitleId
		local op          = toOpacity(opacity)

		base:SetOpacity(op)
		base:Draw(x, y)

		local titleplate_index = data.TitleType + 1
		if not notitle[player_lua] then
			implDrawTitlePlate(x, y, opacity, titleplate_index)
		elseif not nodan[player_lua] then
			drawDanTitlePlate(x, y, opacity, data.DanType + 1)
		end

		implDrawRarityStars(x, y, opacity, rarityInt)
		implDrawBadges(x, y, opacity, nameplateId)

		if not nodan[player_lua] and not notitle[player_lua] then
			dan_base:SetOpacity(op)
			dan_base:Draw(x, y)
			dan_gradation[data.DanType + 1]:SetOpacity(op)
			dan_gradation[data.DanType + 1]:Draw(x, y)
		end

		implDrawTitleEffect(x, y, titleplate_index)
		implDrawPlayerRing(x, y, opacity, player_lua, side_lua)

		if not nodan[player_lua] and not notitle[player_lua] then
			local tx_dan = font_dan:GetText(dan_text[player_lua], false, 99999)
			tx_dan:SetScale(math.min(config_font_dan_maxsize / tx_dan.Width, 1.0), 1.0)
			tx_dan:SetOpacity(op)
			tx_dan:DrawAtAnchor(x + config_text_dan_offset_x, y + config_text_dan_offset_y, "center")
		end

		if not nodan[player_lua] and notitle[player_lua] then
			local tx_title = font_title:GetText(dan_short_text[player_lua], false, 99999)
			tx_title:SetScale(math.min(config_font_name_normal_maxsize / tx_title.Width, 1.0), 1.0)
			tx_title:SetOpacity(op)
			tx_title:DrawAtAnchor(x + config_text_title_offset_x, y + config_text_title_offset_y, "center")

			local tx_name = font_name_withtitle:GetText(name_text[player_lua], false, 99999)
			tx_name:SetScale(math.min(config_font_name_withtitle_maxsize / tx_name.Width, 1.0), 1.0)
			tx_name:SetOpacity(op)
			tx_name:DrawAtAnchor(x + config_text_name_withtitle_offset_x, y + config_text_name_withtitle_offset_y, "center")
		elseif notitle[player_lua] then
			local tx_name = font_name_normal_size:GetText(name_text[player_lua], false, 99999)
			tx_name:SetScale(math.min(config_font_name_normal_maxsize / tx_name.Width, 1.0), 1.0)
			tx_name:SetOpacity(op)
			tx_name:DrawAtAnchor(x + config_text_name_normal_offset_x, y + config_text_name_normal_offset_y, "center")
		else
			local tx_title = font_title:GetText(title_text[player_lua], false, 99999, title_fg, title_bg)
			tx_title:SetScale(math.min(config_font_title_maxsize / tx_title.Width, 1.0), 1.0)
			tx_title:SetOpacity(op)
			tx_title:DrawAtAnchor(x + config_text_title_offset_x, y + config_text_title_offset_y, "center")

			local tx_name
			if nodan[player_lua] then
				tx_name = font_name_withtitle:GetText(name_text[player_lua], false, 99999)
				tx_name:SetScale(math.min(config_font_name_withtitle_maxsize / tx_name.Width, 1.0), 1.0)
				tx_name:SetOpacity(op)
				tx_name:DrawAtAnchor(x + config_text_name_withtitle_offset_x, y + config_text_name_withtitle_offset_y, "center")
			else
				tx_name = font_name_full:GetText(name_text[player_lua], false, 99999)
				tx_name:SetScale(math.min(config_font_name_full_maxsize / tx_name.Width, 1.0), 1.0)
				tx_name:SetOpacity(op)
				tx_name:DrawAtAnchor(x + config_text_name_full_offset_x, y + config_text_name_full_offset_y, "center")
			end
		end

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
