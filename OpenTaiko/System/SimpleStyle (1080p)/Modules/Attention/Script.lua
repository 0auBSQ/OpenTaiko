import ('System.Drawing')

local config = -1


local background = nil


function reloadLanguage(lang)
end

function loadAssets()
    config = loadConfig("Config.json")

    background = loadTexture("Background.png")
end

function init()
end

function final()
end

function update()
end

function draw()
    background:t2D_DisplayImage(0, 0)
end
