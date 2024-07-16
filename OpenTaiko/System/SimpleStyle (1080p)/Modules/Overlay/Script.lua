import ('System.Drawing')

local config = -1


local network_connection = nil


function reloadLanguage(lang)
end

function loadAssets()
    config = loadConfig("Config.json")

    network_connection = loadTexture("Network_Connection.png")
end

function update()
end

function draw()
    offset = nil
    if not info.online then
        offset = 1
    else
        offset = 0
    end
    
    rect = Rectangle((network_connection.szTextureSize.Width / 2) * offset, 0, network_connection.szTextureSize.Width / 2, network_connection.szTextureSize.Height);
    network_connection:t2D_DisplayImage(skininfo.width - rect.Width, skininfo.height - rect.Height, rect)
end
