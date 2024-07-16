import ('System.Drawing')

local config = nil

local tile_black = nil

function reloadLanguage(lang)
end

function loadAssets()
    config = loadConfig("Config.json")

    tile_black = loadTexture("Tile_Black.png")
end

function fadeIn()
end

function fadeOut()
end

function update()
end

function draw()
    if fadeinfo.strState == "in" then
        tile_black.Opacity = 255 - math.ceil(fadeinfo.dbValue * 255)
    elseif fadeinfo.strState ==  "out" then
        tile_black.Opacity = math.ceil(fadeinfo.dbValue * 255)
    elseif fadeinfo.strState ==  "idle" then
        tile_black.Opacity = 255
    else 
        tile_black.Opacity = 0
    end
    
    tile_black:t2D_DisplayImage(0, 0, Rectangle(0, 0, skininfo.width, skininfo.height))
end
