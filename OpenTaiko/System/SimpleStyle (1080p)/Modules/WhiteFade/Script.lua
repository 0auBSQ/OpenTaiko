import ('System.Drawing')

local config = nil

local tile_white = nil

function reloadLanguage(lang)
end

function loadAssets()
    config = loadConfig("Config.json")

    tile_white = loadTexture("Tile_White.png")
end

function fadeIn()
end

function fadeOut()
end

function update()
end

function draw()
    if fadeinfo.strState == "in" then
        tile_white.Opacity = 255 - math.ceil(fadeinfo.dbValue * 255)
    elseif fadeinfo.strState ==  "out" then
        tile_white.Opacity = math.ceil(fadeinfo.dbValue * 255)
    elseif fadeinfo.strState ==  "idle" then
        tile_white.Opacity = 255
    else 
        tile_white.Opacity = 0
    end

    tile_white:t2D_DisplayImage(0, 0, Rectangle(0, 0, skininfo.width, skininfo.height))
end
