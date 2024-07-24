import ('System.Drawing')

local dan_types = { "Clear", "FC", "AP" }

local config = nil

local config_font_name_normal_size = nil
local config_font_name_normal_maxsize = nil

local config_font_name_withtitle_size = nil
local config_font_name_withtitle_maxsize = nil

local config_text_name_full_offset_x = nil
local config_text_name_full_offset_y = nil

local config_font_title_size = nil
local config_font_title_maxsize = nil

local config_font_dan_size = nil
local config_font_dan_maxsize = nil

local config_text_title_offset_x = nil
local config_text_title_offset_y = nil

local config_text_dan_offset_x = nil
local config_text_dan_offset_y = nil

local config_title_plate_offset_x = nil
local config_title_plate_offset_y = nil

local config_titletypes = { "0", "1" }
local config_titleplate_effects = { }

local nameplate_count = 32

local base = nil
local dan_gradation = { }

local players = { }
local players_blue = nil

local titles = { }

local title_plates = { { } }
local title_plate_star_big = { }
local title_plate_star_small = { }
local slash = nil

local title_stars_folders = { "1", "2", "3" }
local title_stars = { { } }

local title_badge_of_achievement = nil
local nameplates_achievement = {134,135,136,78,66,71,44,14,17}
local title_badge_of_team_member = nil
local nameplates_team_member = {}

local font_name_normal_size = nil
local font_name_withtitle = nil
local font_title = nil
local font_dan = nil
local player_data = { nil, nil, nil, nil, nil }

local name_titlekey = { nil, nil, nil, nil, nil }
local title_titlekey = { nil, nil, nil, nil, nil }
local dan_titlekey = { nil, nil, nil, nil, nil }
local notitle = { false, false, false, false, false }
local nodan = { false, false, false, false, false }

local titleplate_counter = 0
local namePlateEffect_counter = 0

function tableContains(testTable, value)
	for i = 1,#testTable do
		if (testTable[i] == value) then
			return true
		end
	end
	return false
end

function implDrawStar(scale, x, y, star_small)
    star_small:tSetScale(scale, scale)
    star_small:t2D_DisplayImage_AnchorCenter(x, y)
end

function implDrawStarFlash(x, y, titleTexIndex)
    star_small = title_plate_star_small[titleTexIndex]

    resolutionScaleX = 1920.0 / 1280.0;
    resolutionScaleY = 1080.0 / 720.0;

    if namePlateEffect_counter <= 10 then
        implDrawStar(1.0 - (namePlateEffect_counter / 10 * 1.0), x + (63 * resolutionScaleX), y + (25 * resolutionScaleY), star_small)
    end
    if namePlateEffect_counter >= 3 and namePlateEffect_counter <= 10 then
        implDrawStar(1.0 - ((namePlateEffect_counter - 3) / 7 * 1.0), x + (38 * resolutionScaleX), y + (7 * resolutionScaleY), star_small)
    end
    if namePlateEffect_counter >= 6 and namePlateEffect_counter <= 10 then
        implDrawStar(1.0 - ((namePlateEffect_counter - 6) / 4 * 1.0), x + (51 * resolutionScaleX), y + (5 * resolutionScaleY), star_small)
    end
    if namePlateEffect_counter >= 8 and namePlateEffect_counter <= 10 then
        implDrawStar(0.3 - ((namePlateEffect_counter - 8) / 2 * 0.3), x + (110 * resolutionScaleX), y + (25 * resolutionScaleY), star_small)
    end
    if namePlateEffect_counter >= 11 and namePlateEffect_counter <= 13 then
        implDrawStar(1.0 - ((namePlateEffect_counter - 11) / 2 * 1.0), x + (38 * resolutionScaleX), y + (7 * resolutionScaleY), star_small)
    end
    if namePlateEffect_counter >= 11 and namePlateEffect_counter <= 15 then
        implDrawStar(1.0, x + (51 * resolutionScaleX), y + 5, star_small)
    end
    if namePlateEffect_counter >= 11 and namePlateEffect_counter <= 17 then
        implDrawStar(1.0 - ((namePlateEffect_counter - 11) / 7 * 1.0), x + (110 * resolutionScaleX), y + (25 * resolutionScaleY), star_small)
    end
    if namePlateEffect_counter >= 16 and namePlateEffect_counter <= 20 then
        implDrawStar(0.2 - ((namePlateEffect_counter - 16) / 4 * 0.2), x + (63 * resolutionScaleX), y + (25 * resolutionScaleY), star_small)
    end
    if namePlateEffect_counter >= 17 and namePlateEffect_counter <= 20 then
        implDrawStar(1.0 - ((namePlateEffect_counter - 17) / 3 * 1.0), x + (99 * resolutionScaleX), y + (1 * resolutionScaleY), star_small)
    end
    if namePlateEffect_counter >= 20 and namePlateEffect_counter <= 24 then
        implDrawStar(0.4, x + (63 * resolutionScaleX), y + 25, star_small)
    end
    if namePlateEffect_counter >= 20 and namePlateEffect_counter <= 25 then
        implDrawStar(1.0, x + (99 * resolutionScaleX), y + 1, star_small)
    end
    if namePlateEffect_counter >= 20 and namePlateEffect_counter <= 30 then
        implDrawStar(0.5 - ((namePlateEffect_counter - 20) / 10 * 0.5), x + (152 * resolutionScaleX), y + (7 * resolutionScaleY), star_small)
    end
    if namePlateEffect_counter >= 31 and namePlateEffect_counter <= 37 then
        implDrawStar(0.5 - ((namePlateEffect_counter - 31) / 6 * 0.5), x + (176 * resolutionScaleX), y + (8 * resolutionScaleY), star_small)
        implDrawStar(1.0 - ((namePlateEffect_counter - 31) / 6 * 1.0), x + (175 * resolutionScaleX), y + (25 * resolutionScaleY), star_small)
    end
    if namePlateEffect_counter >= 31 and namePlateEffect_counter <= 40 then
        implDrawStar(0.9 - ((namePlateEffect_counter - 31) / 9 * 0.9), x + (136 * resolutionScaleX), y + (24 * resolutionScaleY), star_small)
    end
    if namePlateEffect_counter >= 34 and namePlateEffect_counter <= 40 then
        implDrawStar(0.7 - ((namePlateEffect_counter - 34) / 6 * 0.7), x + (159 * resolutionScaleX), y + (25 * resolutionScaleY), star_small)
    end
    if namePlateEffect_counter >= 41 and namePlateEffect_counter <= 42 then
        implDrawStar(0.7, x + (159 * resolutionScaleX), y + (25 * resolutionScaleY), star_small)
    end
    if namePlateEffect_counter >= 43 and namePlateEffect_counter <= 50 then
        implDrawStar(0.8 - ((namePlateEffect_counter - 43) / 7 * 0.8), x + (196 * resolutionScaleX), y + (23 * resolutionScaleY), star_small)
    end
    if namePlateEffect_counter >= 51 and namePlateEffect_counter <= 57 then
        implDrawStar(0.8 - ((namePlateEffect_counter - 51) / 6 * 0.8), x + (51 * resolutionScaleX), y + (5 * resolutionScaleY), star_small)
    end
    if namePlateEffect_counter >= 51 and namePlateEffect_counter <= 52 then
        implDrawStar(0.2, x + (166 * resolutionScaleX), y + (22 * resolutionScaleY), star_small)
    end
    if namePlateEffect_counter >= 51 and namePlateEffect_counter <= 53 then
        implDrawStar(0.8, x + (136 * resolutionScaleX), y + (24 * resolutionScaleY), star_small)
    end
    if namePlateEffect_counter >= 51 and namePlateEffect_counter <= 55 then
        implDrawStar(1.0, x + (176 * resolutionScaleX), y + (8 * resolutionScaleY), star_small)
    end
    if namePlateEffect_counter >= 51 and namePlateEffect_counter <= 55 then
        implDrawStar(1.0, x + (176 * resolutionScaleX), y + (8 * resolutionScaleY), star_small)
    end
    if namePlateEffect_counter >= 61 and namePlateEffect_counter <= 70 then
        implDrawStar(1.0 - ((namePlateEffect_counter - 61) / 9 * 1.0), x + (196 * resolutionScaleX), y + (23 * resolutionScaleY), star_small)
    end
    if namePlateEffect_counter >= 61 and namePlateEffect_counter <= 67 then
        implDrawStar(0.7 - ((namePlateEffect_counter - 61) / 6 * 0.7), x + (214 * resolutionScaleX), y + (14 * resolutionScaleY), star_small)
    end
    if namePlateEffect_counter >= 63 and namePlateEffect_counter <= 70 then
        implDrawStar(0.5 - ((namePlateEffect_counter - 63) / 7 * 0.5), x + (129 * resolutionScaleX), y + (24 * resolutionScaleY), star_small)
    end
    if namePlateEffect_counter >= 63 and namePlateEffect_counter <= 70 then
        implDrawStar(0.5 - ((namePlateEffect_counter - 63) / 7 * 0.5), x + (129 * resolutionScaleX), y + (24 * resolutionScaleY), star_small)
    end
    if namePlateEffect_counter >= 65 and namePlateEffect_counter <= 70 then
        implDrawStar(0.8 - ((namePlateEffect_counter - 65) / 5 * 0.8), x + (117 * resolutionScaleX), y + (7 * resolutionScaleY), star_small)
    end
    if namePlateEffect_counter >= 71 and namePlateEffect_counter <= 72 then
        implDrawStar(0.8, x + (151 * resolutionScaleX), y + (25 * resolutionScaleY), star_small)
    end
    if namePlateEffect_counter >= 71 and namePlateEffect_counter <= 74 then
        implDrawStar(0.8, x + (117 * resolutionScaleX), y + (7 * resolutionScaleY), star_small)
    end
    if namePlateEffect_counter >= 85 and namePlateEffect_counter <= 112 then
        slash.Opacity = 1400 - (namePlateEffect_counter - 85) * 50

        slash:t2D_DisplayImage(x + ((((namePlateEffect_counter - 85) * (150 / 27))) * resolutionScaleX), y + (7 * resolutionScaleY));
    end
    if namePlateEffect_counter >= 105 and namePlateEffect_counter <= 120 then
        big_scale = 1.0
        if namePlateEffect_counter < 112 then
            big_scale = (namePlateEffect_counter - 105) / 8
            title_plate_star_big[titleTexIndex].Opacity = 255
        else
            title_plate_star_big[titleTexIndex].Opacity = (255 - (namePlateEffect_counter - 112) * 31.875)
        end
        title_plate_star_big[titleTexIndex]:tSetScale(big_scale, big_scale)
        title_plate_star_big[titleTexIndex]:t2D_DisplayImage_AnchorCenter(x + (193 * resolutionScaleX), y + (6 * resolutionScaleY))
    end
end

function implDrawTitleEffect(x, y, titleTexIndex)
    if titleTexIndex >= 1 and titleTexIndex <= #title_plates then
        if config_titleplate_effects[titleTexIndex] == "flash" then
            implDrawStarFlash(x, y, titleTexIndex)
        elseif config_titleplate_effects[titleTexIndex] == "none" then
        else
        end 
    end
end

function implDrawRarityStars(o_x, o_y, opacity, rarity)
	local x = o_x
	local y = o_y - 20
	
	local star_count = 0
	--Rare
	if rarity == 3 then
		star_count = 1
	--Epic
	elseif rarity == 4 then
		star_count = 2
	--Legendary / Mythical
	elseif rarity >= 5 then
		star_count = 3
	end
	
	if star_count > 0 then
		local star_frame = 1 + math.ceil(titleplate_counter * (#title_stars[star_count] - 1))
		tx_titlestar = title_stars[star_count][star_frame]
		tx_titlestar.Opacity = opacity
		tx_titlestar:t2D_DisplayImage(x + config_title_plate_offset_x, y + config_title_plate_offset_y)
	end
end

function implDrawBadges(x, y, opacity, nameplateId)
	if tableContains(nameplates_achievement, nameplateId) then
		title_badge_of_achievement.Opacity = opacity
		title_badge_of_achievement:t2D_DisplayImage(x + config_title_plate_offset_x, y + config_title_plate_offset_y)
	elseif tableContains(nameplates_team_member, nameplateId) then
		title_badge_of_team_member.Opacity = opacity
		title_badge_of_team_member:t2D_DisplayImage(x + config_title_plate_offset_x, y + config_title_plate_offset_y)
	end
end

function implDrawTitlePlate(x, y, opacity, titleTexIndex)
    if titleTexIndex >= 1 and titleTexIndex <= #title_plates then
        titleplate_frame = 1 + math.ceil(titleplate_counter * (#title_plates[titleTexIndex] - 1))
        tx_titleplate = title_plates[titleTexIndex][titleplate_frame]
        tx_titleplate.Opacity = opacity
        tx_titleplate:t2D_DisplayImage(x + config_title_plate_offset_x, y + config_title_plate_offset_y)
    end
end

function implDrawPlayerRing(x, y, opacity, player_lua, side_lua)
	if (player_lua == 1 and side_lua == 2) then
		players_blue.Opacity = opacity
		players_blue:t2D_DisplayImage(x, y)
	else
		players[player_lua].Opacity = opacity
		players[player_lua]:t2D_DisplayImage(x, y)
	end
end

function reloadLanguage(lang)
end

function getCharaOffset()
    return base.szTextureSize.Width / 2
end

function setInfos(player, name, title, dan, data)
    local player_lua = player + 1
    
    player_data[player_lua] = data

    notitle[player_lua] = (title == "")
    nodan[player_lua] = (player_data[player_lua].Dan == nil or player_data[player_lua].Dan == "")

    if notitle[player_lua] then
        name_titlekey[player_lua] = createTitleTextureKey(name, font_name_normal_size, 99999)
    else
        name_titlekey[player_lua] = createTitleTextureKey(name, font_name_withtitle, 99999)
    end
    title_titlekey[player_lua] = createTitleTextureKey(title, font_title, 99999, Color.FromArgb(0,0,0,1), Color.FromArgb(0,0,0,0))
    dan_titlekey[player_lua] = createTitleTextureKey(dan, font_dan, 99999)
end

function loadAssets()
    config = loadConfig("Config.json")
    
    config_font_name_normal_size = getNum(config["font_name_normal"]["size"])
    config_font_name_normal_maxsize = getNum(config["font_name_normal"]["maxsize"])

    config_font_name_withtitle_size = getNum(config["font_name_withtitle"]["size"])
    config_font_name_withtitle_maxsize = getNum(config["font_name_withtitle"]["maxsize"])

    config_font_title_size = getNum(config["font_title"]["size"])
    config_font_title_maxsize = getNum(config["font_title"]["maxsize"])

    config_font_dan_size = getNum(config["font_dan"]["size"])
    config_font_dan_maxsize = getNum(config["font_dan"]["maxsize"])

    config_text_name_normal_offset_x = getNum(config["text_name_normal"]["offset_x"])
    config_text_name_normal_offset_y = getNum(config["text_name_normal"]["offset_y"])

    config_text_name_withtitle_offset_x = getNum(config["text_name_withtitle"]["offset_x"])
    config_text_name_withtitle_offset_y = getNum(config["text_name_withtitle"]["offset_y"])

    config_text_name_full_offset_x = getNum(config["text_name_full"]["offset_x"])
    config_text_name_full_offset_y = getNum(config["text_name_full"]["offset_y"])

    config_text_title_offset_x = getNum(config["text_title"]["offset_x"])
    config_text_title_offset_y = getNum(config["text_title"]["offset_y"])

    config_text_dan_offset_x = getNum(config["text_dan"]["offset_x"])
    config_text_dan_offset_y = getNum(config["text_dan"]["offset_y"])

    config_title_plate_offset_x = getNum(config["title_plate"]["offset_x"])
    config_title_plate_offset_y = getNum(config["title_plate"]["offset_y"])

    for i = 0, nameplate_count - 1 do 
        config_titletypes[i + 1] = tostring(i)
    end

    base = loadTexture("Base.png")
    dan_base = loadTexture("Dan_Base.png")
    slash = loadTexture("Shines/Slash.png")
	
	title_badge_of_achievement = loadTexture("Badges/0.png")
	title_badge_of_team_member = loadTexture("Badges/1.png")

    for i = 1, 3 do 
        dan_gradation[i] = loadTexture("Dan_"..dan_types[i]..".png")
    end

    for i = 1, 5 do 
		players[i] = loadTexture(tostring(i).."P.png")
    end
	players_blue = loadTexture("1P_Blue.png")

	--Load title plates
    for i = 1, #config_titletypes do
        titledir = "Title/"..config_titletypes[i]
		
        titleplate_config = config["titles"][config_titletypes[i]]

		config_titleplate_effects[i] = "none"
		config_titleplate_framecount = 0
		
		if titleplate_config ~= nil then
			config_titleplate_effects[i] = getText(titleplate_config["effect"])
			config_titleplate_framecount = getNum(titleplate_config["framecount"])
		end

        local titleplates = { }
		
        for j = 0, config_titleplate_framecount do 
            titleplates[j + 1] = loadTexture(titledir.."/"..j..".png")
        end
        title_plates[i] = titleplates

        if config_titleplate_effects[i] == "flash" then
            title_plate_star_small[i] = loadTexture(titledir.."/Small.png")
            title_plate_star_big[i] = loadTexture(titledir.."/Big.png")
        end

    end
	
	--Load rarity stars
	for i = 1, #title_stars_folders do
		stardir = "Stars/"..title_stars_folders[i]
		
		title_stars_config = config["stars"][title_stars_folders[i]]
		
		config_stars_framecount = 0
		
		if title_stars_config ~= nil then
			config_stars_framecount = getNum(title_stars_config["framecount"])
		end
		
		local stars = { }
		for j = 0, config_stars_framecount do 
            stars[j + 1] = loadTexture(stardir.."/"..j..".png")
        end
        title_stars[i] = stars
	end
    
    font_name_normal_size = loadFontRenderer(config_font_name_normal_size, "regular")
    font_name_withtitle = loadFontRenderer(config_font_name_withtitle_size, "regular")
    font_title = loadFontRenderer(config_font_title_size, "regular")
    font_dan = loadFontRenderer(config_font_dan_size, "regular")
end

function drawDan(o_x, o_y, opacity, type, titleTex)
	--Shift on side bar
	local x = o_x - 180
	local y = o_y + 5

	--White background
    base.Opacity = opacity
    base:t2D_DisplayImage(x, y)

	--Dan base
    dan_base.Opacity = opacity
    dan_base:t2D_DisplayImage(x, y)
    dan_gradation[type + 1].Opacity = opacity
    dan_gradation[type + 1]:t2D_DisplayImage(x, y)
	
	--Player number
	implDrawPlayerRing(x, y, opacity, 1, 1)
    
    --Dan text
    if not(nodan[player_lua]) then
        titleTex:tSetScale(math.min(config_font_dan_maxsize / titleTex.szTextureSize.Width, 1.0), 1.0)
        titleTex.Opacity = opacity
        titleTex:t2D_DisplayImage_AnchorCenter(x + config_text_dan_offset_x, y + config_text_dan_offset_y)
    end
end

function drawTitlePlate(o_x, o_y, opacity, titletype, titleTex, rarityInt, nameplateId)
	--Shift on side bar
	local x = o_x - 180
	local y = o_y + 5
	
	--White background
    base.Opacity = opacity
    base:t2D_DisplayImage(x, y)

	--Upper (title) plate
    implDrawTitlePlate(x, y, opacity, titletype + 1)
	
	--Rarity stars
	implDrawRarityStars(x, y, opacity, rarityInt)
	
	--Badges
	implDrawBadges(x, y, opacity, nameplateId)
	
	--Glow
    implDrawTitleEffect(x, y, titletype + 1)
	
	--Player number
	implDrawPlayerRing(x, y, opacity, 1, 1)

	--Title/Name text
    titleTex:tSetScale(math.min(config_font_title_maxsize / titleTex.szTextureSize.Width, 1.0), 1.0)
    titleTex.Opacity = opacity
    titleTex:t2D_DisplayImage_AnchorCenter(x + config_text_title_offset_x, y + config_text_title_offset_y)
end

function update()
    titleplate_counter = titleplate_counter + (3.3 * fps.deltaTime)
    if titleplate_counter >= 1 then
        titleplate_counter = 0
    end

    namePlateEffect_counter = namePlateEffect_counter + (60 * fps.deltaTime)
    if namePlateEffect_counter >= 120 then
        namePlateEffect_counter = 0
    end
end

function draw(x, y, opacity, player, side)
    local player_lua = player + 1
    local side_lua = side + 1
	local rarityInt = player_data[player_lua].TitleRarityInt
	local nameplateId = player_data[player_lua].TitleId
    
    --White background
    base.Opacity = opacity
    base:t2D_DisplayImage(x, y)

    --Upper (title) plate
    titleplate_index = player_data[player_lua].TitleType + 1
    if not(notitle[player_lua]) then
        implDrawTitlePlate(x, y, opacity, titleplate_index)
    end
	
	--Rarity stars
	implDrawRarityStars(x, y, opacity, rarityInt)
	
	--Badges
	implDrawBadges(x, y, opacity, nameplateId)

    --Dan plate
    if not(player_data[player_lua].Dan == nil) and not(player_data[player_lua].Dan == "") then
        dan_base.Opacity = opacity
        dan_base:t2D_DisplayImage(x, y)
        dan_gradation[player_data[player_lua].DanType + 1].Opacity = opacity
        dan_gradation[player_data[player_lua].DanType + 1]:t2D_DisplayImage(x, y)
    end

    --Glow
    implDrawTitleEffect(x, y, titleplate_index)

    --Player number
	implDrawPlayerRing(x, y, opacity, player_lua, side_lua)

    --Dan text
    if not(nodan[player_lua]) then
        tx_dan = getTextTex(dan_titlekey[player_lua], false, false)
        tx_dan:tSetScale(math.min(config_font_dan_maxsize / tx_dan.szTextureSize.Width, 1.0), 1.0)
        tx_dan.Opacity = opacity
        tx_dan:t2D_DisplayImage_AnchorCenter(x + config_text_dan_offset_x, y + config_text_dan_offset_y)
    end

    --Title/Name text
    if notitle[player_lua] then
        tx_name = getTextTex(name_titlekey[player_lua], false, false)
        tx_name:tSetScale(math.min(config_font_name_normal_maxsize / tx_name.szTextureSize.Width, 1.0), 1.0)
        tx_name.Opacity = opacity
        tx_name:t2D_DisplayImage_AnchorCenter(x + config_text_name_normal_offset_x, y + config_text_name_normal_offset_y)
    else
        tx_title = getTextTex(title_titlekey[player_lua], false, false)
        tx_title:tSetScale(math.min(config_font_title_maxsize / tx_title.szTextureSize.Width, 1.0), 1.0)
        tx_title.Opacity = opacity
        tx_title:t2D_DisplayImage_AnchorCenter(x + config_text_title_offset_x, y + config_text_title_offset_y)

        tx_name = getTextTex(name_titlekey[player_lua], false, false)
        tx_name:tSetScale(math.min(config_font_name_withtitle_maxsize / tx_name.szTextureSize.Width, 1.0), 1.0)
        tx_name.Opacity = opacity
        if nodan[player_lua] then
            tx_name:t2D_DisplayImage_AnchorCenter(x + config_text_name_withtitle_offset_x, y + config_text_name_withtitle_offset_y)
        else
            tx_name:t2D_DisplayImage_AnchorCenter(x + config_text_name_full_offset_x, y + config_text_name_full_offset_y)
        end
    end

end
