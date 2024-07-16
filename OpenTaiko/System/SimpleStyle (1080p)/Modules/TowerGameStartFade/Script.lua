import ('System.Drawing')

local config = nil

local tile_black = nil
local background = nil

local tower = nil

local counter_value = 0

function reloadLanguage(lang)
end

function loadAssets()
    config = loadConfig("Config.json")

    tile_black = loadTexture("Tile_Black.png")
    background = loadTexture("Background.png")
end

function genTitle(title, subtitle)
end

function setTowerTexture(towerTexture)
    tower = towerTexture
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
        counter_value = fadeinfo.dbIdleWait * 1200
    end
end

function draw()
    if fadeinfo.strState == "in" then
        tile_black.Opacity = 255 - counter_value
    elseif fadeinfo.strState ==  "out" then
        tile_black.Opacity = -1000 + counter_value
    elseif fadeinfo.strState == "idle" then
        tile_black.Opacity = 0

        if counter_value <= 51 then
            tile_black.Opacity = 255 - counter_value / 0.2
        else
            tile_black.Opacity = (counter_value - 949) / 0.2
        end

        pos = (background.szTextureSize.Height - skininfo.height)

        temp = 120
        if counter_value <= 1200 then
            temp = counter_value / 10
        end

        pos = pos - (temp / 120 * (background.szTextureSize.Height - skininfo.height))

        background:t2D_DisplayImage(0, -1 * pos)

        if not(tower == nil) then
            xFactor = 0
            yFactor = 1
            xFactor = (background.szTextureSize.Width - tower.szTextureSize.Width) / 2
            yFactor = tower.szTextureSize.Height / background.szTextureSize.Height
            tower:t2D_DisplayImage(xFactor, -1 * yFactor * pos)
        end
    else 
        tile_black.Opacity = 0
    end
    
    tile_black:t2D_DisplayImage(0, 0, Rectangle(0, 0, skininfo.width, skininfo.height))
end
