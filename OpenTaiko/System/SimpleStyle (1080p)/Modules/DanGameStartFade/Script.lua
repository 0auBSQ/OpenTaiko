import ('System.Drawing')

local config = nil

local tile_black = nil
local bg_dan = nil

local danPlate = nil

local config_danPlate_x = nil
local config_danPlate_y = nil

local counter_value = 0

function reloadLanguage(lang)
end

function loadAssets()
    config = loadConfig("Config.json")

    config_danPlate_x = getNum(config["danplate"]["x"])
    config_danPlate_y = getNum(config["danplate"]["y"])

    tile_black = loadTexture("Tile_Black.png")
    bg_dan = loadTexture("Bg_Dan.png")
end

function genTitle(title, subtitle)
end

function setDanPlateTexture(plate)
    danPlate = plate
end

function fadeIn()
end

function fadeOut()
end

function update()
    if fadeinfo.strState == "in" then
        counter_value = fadeinfo.dbValue * 255
    elseif fadeinfo.strState ==  "out" then
        counter_value = fadeinfo.dbValue * 1255
    elseif fadeinfo.strState ==  "idle" then
        counter_value = fadeinfo.dbIdleWait * 1000
    end
end

function draw()
    if fadeinfo.strState == "in" then
        tile_black.Opacity = 255 - counter_value
    elseif fadeinfo.strState ==  "out" then
        tile_black.Opacity = -1000 + counter_value
    elseif fadeinfo.strState ==  "idle" then
        tile_black.Opacity = 0

        if counter_value <= 51 then
            tile_black.Opacity = 255 - counter_value / 0.2
        else
            tile_black.Opacity = (counter_value - 949) / 0.2
        end

        bg_dan_y = 60
        if counter_value <= 600 then
            bg_dan_y = counter_value / 10
        end
        
        bg_dan:t2D_DisplayImage(0, 0 - bg_dan_y)

        displayDanPlate(danPlate, nil, config_danPlate_x, config_danPlate_y)
    else 
        tile_black.Opacity = 0
    end
    
    tile_black:t2D_DisplayImage(0, 0, Rectangle(0, 0, skininfo.width, skininfo.height))
end
