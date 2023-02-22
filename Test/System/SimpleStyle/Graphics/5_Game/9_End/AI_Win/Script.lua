--func:DrawText(x, y, text)
--func:DrawNum(x, y, num)
--func:AddGraph("filename")
--func:DrawGraph(x, y, filename)
--func:DrawRectGraph(x, y, rect_x, rect_y, rect_width, rect_height, filename)
--func:DrawGraphCenter(x, y, filename)
--func:DrawGraphRectCenter(x, y, rect_x, rect_y, rect_width, rect_height, filename)
--func:SetOpacity(opacity, "filename")
--func:SetRotation(angle, "fileName")
--func:SetScale(xscale, yscale, "filename")
--func:SetColor(r, g, b, "filename")

local animeCounter = 0

local x = 640
local y = 360

function clearIn(player)
end

function clearOut(player)
end

function playEndAnime(player)
    animeCounter = 0
end

function init()
    func:AddGraph("Base.png")
end

function update(player)
    animeCounter = animeCounter + (1.0 * deltaTime)
end

function draw(player)
    value = math.min(animeCounter * 3, 1)
    scale = 2 - value

    func:SetOpacity(value * 255, "Base.png")
    func:SetScale(scale, scale, "Base.png")

    func:DrawGraphCenter(x, y, "Base.png")
end