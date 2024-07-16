import ('System.Drawing')

local config = nil

local font_title = nil
local font_subtitle = nil

local bg = nil
local bg_wait = nil
local bg_fade = nil
local bg = nil
local plate = nil
local chara = nil

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

local title_key = nil
local subtitle_key = nil

local counter_value = 0

function drawBack(tex, time, max, endvalue, isexit)
    if time - max >= endvalue then
        time = endvalue + max
    end

    sizeXHarf = tex.szTextureSize.Width / 2
    sizeY = tex.szTextureSize.Height
    startScaleX = 0.5

    scaleX = 0
    if isexit then
        scaleX = 1 - startScaleX
    end

    if time >= max then
        scaleX = scaleX - (time - max) * ((1 - startScaleX) / endvalue)
    end

    if not isexit then
        scaleX = -scaleX 
    end

    value = 0 
    if isexit then
        value = 1
    end
    
    if time >= max then
        value = value - (time - max) * (1 / endvalue)
    end
    
    if not isexit then
        value = -value 
    end

    tex:tSetScale(startScaleX + scaleX, tex.vcScaleRatio.Y)
    tex:t2D_DisplayImage(-(sizeXHarf * startScaleX) + (value * (sizeXHarf * startScaleX)), 0, RectangleF(0, 0, sizeXHarf, sizeY))
    tex:t2D_DisplayImage((sizeXHarf + (sizeXHarf * startScaleX)) - (value * (sizeXHarf * startScaleX)) + ((1 - tex.vcScaleRatio.X) * sizeXHarf), 0, RectangleF(sizeXHarf, 0, sizeXHarf, sizeY))
end

function drawStar(opacity)
    bg_wait.Opacity = opacity
    bg_wait:t2D_DisplayImage(0, 0)
end

function drawPlate(opacity, scaleX, scaleY)
    sizeX_Harf = plate.szTextureSize.Width / 2
    sizeY_Harf = plate.szTextureSize.Height / 2

    plate.Opacity = opacity
    plate:tSetScale(scaleX, scaleY)
    plate:t2D_DisplayImage(config_plate_x + sizeX_Harf - (sizeX_Harf * scaleX) - sizeX_Harf, config_plate_y - sizeY_Harf + ((1 - scaleY) * sizeY_Harf))
end

function drawChara(time, opacity, x, y)
    if x == -1 and y == -1 and time <= 680 then
        return
    end
    sizeXHarf = chara.szTextureSize.Width / 2
    sizeY = chara.szTextureSize.Height
    if x == -1 and y == -1 then
        y = (math.sin((time - 680) * (math.pi / 320.0)) * config_chara_moving_y)
        x = ((time - 680) / 320.0) * config_chara_moving_x
    end

    chara.Opacity = opacity
    chara:t2D_DisplayImage(-config_chara_moving_x + x, y, RectangleF(0, 0, sizeXHarf, sizeY))
    chara:t2D_DisplayImage(sizeXHarf + config_chara_moving_x - x, y, RectangleF(sizeXHarf, 0, sizeXHarf, sizeY))
end

function reloadLanguage(lang)
end

function loadAssets()
    config = loadConfig("Config.json")

    config_plate_x = getNum(config["plate"]["x"])
    config_plate_y = getNum(config["plate"]["y"])

    config_chara_moving_x = getNum(config["chara"]["moving_x"])
    config_chara_moving_y = getNum(config["chara"]["moving_y"])

    config_font_title_size = getNum(config["font_title"]["size"])
    config_font_title_maxsize = getNum(config["font_title"]["maxsize"])

    config_font_subtitle_size = getNum(config["font_subtitle"]["size"])
    config_font_subtitle_maxsize = getNum(config["font_subtitle"]["maxsize"])

    config_title_x = getNum(config["title"]["x"])
    config_title_y = getNum(config["title"]["y"])

    config_subtitle_x = getNum(config["subtitle"]["x"])
    config_subtitle_y = getNum(config["subtitle"]["y"])

    bg = loadTexture("Bg.png")
    bg_wait = loadTexture("Bg_Wait.png")
    fade = loadTexture("Fade.png")
    chara = loadTexture("Chara.png")
    plate = loadTexture("Plate.png")

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
    counter_value = fadeinfo.dbValue * 3580
end

function draw()

    if fadeinfo.strState == "in" then
        time = counter_value
        fadeValue = time / 140
        if fadeValue >= 1.0 then
            fadeValue = 1.0
        elseif fadeValue <= 0.0 then
            fadeValue = 0.0
        end

        if time < 300.0 then
            drawBack(bg, time, 300.0, 500.0, true)
        else
            drawBack(fade, time, 300.0, 500.0, true)
        end
        drawStar(255 - (fadeValue * 255))
        drawPlate(255 - (fadeValue * 255), 1 + (fadeValue * 0.5), 1 - fadeValue)

        chara_opacity = 0
        if time <= 80.0 then
            chara_opacity = 255
        else
            chara_opacity = 255 - ((((time - 80) ^ 1.5) / (220 ^ 1.5)) * 255)
        end
        chara_x = config_chara_moving_x

        chara_y = 0
        if time <= 80.0 then
            chara_y = ((time / 80) * 30)
        else
            chara_y = 30 - ((((time - 80) ^ 1.5) / (220 ^ 1.5)) * 320)
        end

        drawChara(time, chara_opacity, chara_x, chara_y)
    elseif fadeinfo.strState ==  "out" then
        time = 0
        if counter_value >= 2580 then
            time = counter_value - 2580
        end
        
        fadeValue = (time - 670) / 330.0
        if fadeValue >= 1.0 then
            fadeValue = 1.0
        elseif fadeValue <= 0.0 then
            fadeValue = 0.0
        end

        if time < 500.0 then
            drawBack(fade, time, 0, 500.0, false)
        else
            drawBack(bg, time, 0, 500.0, false)
        end
        drawStar(fadeValue * 255)
        drawPlate(fadeValue * 255, fadeValue, 1)
        drawChara(time, (time - 730) * (255 / 270), -1, -1)
    elseif fadeinfo.strState ==  "idle" then
        drawBack(bg, 0, 0, 500.0, true)
        drawStar(255)
        drawPlate(255, 1, 1)
        drawChara(1000, 255, -1, -1)

        getTextTex(title_key, false, false):t2D_DisplayImage_AnchorCenter(config_title_x, config_title_y)
        getTextTex(subtitle_key, false, false):t2D_DisplayImage_AnchorCenter(config_subtitle_x, config_subtitle_y)
    else 
    end
end
