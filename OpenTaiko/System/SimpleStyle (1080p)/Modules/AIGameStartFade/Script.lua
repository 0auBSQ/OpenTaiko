import ('System.Drawing')

local config = nil

local font_title = nil
local font_subtitle = nil

local bg_ai = nil
local bg_ai_wait = nil
local fade_ai = nil
local plate_ai = nil
local bg_ai = nil
local bg_ai_wait = nil
local fade_ai = nil
local fade_ai_anime_base = nil
local fade_ai_anime_loadbar = nil
local fade_ai_anime_loadbar_base = nil
local fade_ai_anime_nowloading = nil
local fade_ai_anime_nowloading = nil
local fade_ai_anime_start = nil
local plate_ai = nil

local config_plate_x = nil
local config_plate_y = nil

local config_chara_moving_x = nil
local config_chara_moving_y = nil

local config_font_title_size = nil
local config_font_title_maxsize = nil

local config_font_subtitle_size = nil
local config_font_subtitle_maxsize = nil

local config_title_x = nil
local config_title_y = nil

local config_subtitle_x = nil
local config_subtitle_y = nil

local config_fade_ai_anime_ring_x = nil
local config_fade_ai_anime_ring_y = nil

local config_fade_ai_anime_loadbar_base_x = nil
local config_fade_ai_anime_loadbar_base_y = nil

local config_fade_ai_anime_loadbar_x = nil
local config_fade_ai_anime_loadbar_y = nil

local title_key = nil
local subtitle_key = nil

local counter_value = 0

function reloadLanguage(lang)
end

function loadAssets()
    config = loadConfig("Config.json")

    bg_ai = loadTexture("Bg_AI.png")
    bg_ai_wait = loadTexture("Bg_AI_Wait.png")
    fade_ai = loadTexture("Fade_AI.png")
    fade_ai_anime_base = loadTexture("Fade_AI_Anime_Base.png")
    fade_ai_anime_loadbar = loadTexture("Fade_AI_Anime_LoadBar.png")
    fade_ai_anime_loadbar_base = loadTexture("Fade_AI_Anime_LoadBar_Base.png")
    fade_ai_anime_nowloading = loadTexture("Fade_AI_Anime_NowLoading.png")
    fade_ai_anime_ring = loadTexture("Fade_AI_Anime_Ring.png")
    fade_ai_anime_start = loadTexture("Fade_AI_Anime_Start.png")
    plate_ai = loadTexture("Plate_AI.png")

    config_config_plate_x = getNum(config["plate_ai"]["x"])
    config_config_plate_y = getNum(config["plate_ai"]["y"])

    config_font_title_size = getNum(config["font_title"]["size"])
    config_font_title_maxsize = getNum(config["font_title"]["maxsize"])

    config_font_subtitle_size = getNum(config["font_subtitle"]["size"])
    config_font_subtitle_maxsize = getNum(config["font_subtitle"]["maxsize"])

    config_title_x = getNum(config["title"]["x"])
    config_title_y = getNum(config["title"]["y"])

    config_subtitle_x = getNum(config["subtitle"]["x"])
    config_subtitle_y = getNum(config["subtitle"]["y"])

    config_fade_ai_anime_ring_x = getNum(config["fade_ai_anime_ring"]["x"])
    config_fade_ai_anime_ring_y = getNum(config["fade_ai_anime_ring"]["y"])

    config_fade_ai_anime_loadbar_x = getNum(config["fade_ai_anime_loadbar"]["x"])
    config_fade_ai_anime_loadbar_y = getNum(config["fade_ai_anime_loadbar"]["y"])

    font_title = loadFontRenderer(config_font_title_size, "regular")
    font_subtitle = loadFontRenderer(config_font_subtitle_size, "regular")

    genTitle("", "")
end

function genTitle(title, subtitle)
    title_key = createTitleTextureKey(title, font_title, config_font_title_maxsize)
    subtitle_key = createTitleTextureKey(subtitle, font_subtitle, config_font_subtitle_maxsize)
end

function fadeIn()
end

function fadeOut()
end

function update()
    counter_value = fadeinfo.dbValue * 5500
end

function draw()

    if fadeinfo.strState == "in" then
        bg_ai.Opacity = 255 - counter_value
        bg_ai:t2D_DisplayImage(0, 0);
    elseif fadeinfo.strState ==  "out" then
        preTime = 0
        if counter_value >= 2000 then
            preTime = counter_value - 2000
        end

        fade_ai.Opacity = preTime
        fade_ai:t2D_DisplayImage(0, 0)

        if preTime > 500 then
            fade_ai_anime_base:tSetScale(math.min(((preTime - 500) / 255.0), 1.0), fade_ai_anime_base.vcScaleRatio.Y)
            fade_ai_anime_base:t2D_DisplayImage_AnchorCenter(skininfo.width / 2, skininfo.height / 2)
        end

        if preTime > 1000 then
            fade_ai_anime_ring.Opacity = preTime - 1000
            fade_ai_anime_ring.fZRotation = preTime / 6000.0
            fade_ai_anime_ring:t2D_DisplayImage(config_fade_ai_anime_ring_x, config_fade_ai_anime_ring_y)
            if preTime - 1000 < 1500 then
                fade_ai_anime_nowloading.Opacity = preTime - 1000
                fade_ai_anime_nowloading:t2D_DisplayImage(0, 0)
                fade_ai_anime_loadbar_base:t2D_DisplayImage(config_fade_ai_anime_loadbar_x, config_fade_ai_anime_loadbar_y)

                value = (preTime - 1000) / 1500
                value = 1.0 - math.cos(value * math.pi / 2.0)
                value = 1.0 - math.cos(value * math.pi / 2.0)
                value = 1.0 - math.cos(value * math.pi / 2.0)

                fade_ai_anime_loadbar:t2D_DisplayImage(config_fade_ai_anime_loadbar_x, config_fade_ai_anime_loadbar_y,
								RectangleF(0, 0, fade_ai_anime_loadbar.szTextureSize.Width * value, 
								fade_ai_anime_loadbar.szTextureSize.Height))
            else
                fade_ai_anime_start:t2D_DisplayImage(0, 0)
            end
        end

        time = 0
        if counter_value >= 5000 then
            time = counter_value - 5000
        end
        bg_ai.Opacity = time;
        bg_ai:t2D_DisplayImage(0, 0)

        bg_ai_wait.Opacity = time - 255
        bg_ai_wait:t2D_DisplayImage(0, 0)

        plate_ai.Opacity = time - 255
        plate_ai:t2D_DisplayImage(config_config_plate_x - (plate_ai.szTextureSize.Width / 2), config_config_plate_y - (plate_ai.szTextureSize.Height / 2))
    elseif fadeinfo.strState ==  "idle" then
        bg_ai_wait:t2D_DisplayImage(0, 0)
        plate_ai.Opacity = 255
        plate_ai:t2D_DisplayImage(config_config_plate_x - (plate_ai.szTextureSize.Width / 2), config_config_plate_y - (plate_ai.szTextureSize.Height / 2))
        getTextTex(title_key, false, false):t2D_DisplayImage_AnchorCenter(config_title_x, config_title_y)
        getTextTex(subtitle_key, false, false):t2D_DisplayImage_AnchorCenter(config_subtitle_x, config_subtitle_y)
    else 
    end
end
