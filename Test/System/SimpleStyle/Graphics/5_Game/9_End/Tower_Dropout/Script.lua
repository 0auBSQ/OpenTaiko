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

local x = { 505, 505, 505, 505, 505 }
local y = { 145, 321, 0, 0, 0 }

local animeCounter = { 0, 0, 0, 0, 0 }
local nowFrame = { 0, 0, 0, 0, 0 }

local textureCount = 25

local useExtraAnime = true

function clearIn(player)
end

function clearOut(player)
end

function playEndAnime(player)
    animeCounter = { 0, 0, 0, 0, 0 }
    nowFrame = { 0, 0, 0, 0, 0 }
end

function init()

    if playerCount <= 2 then
        y = { 145, 321, 0, 0, 0 }
    elseif playerCount == 5 then
        y = { -23, 121, 265, 409, 553 }
    else
        y = { -1, 175, 351, 527, 0 }
    end

    for i = 0 , textureCount do
        func:AddGraph(tostring(i)..".png")
    end

    if useExtraAnime then
        func:AddGraph("Clear_Failed.png")
        func:AddGraph("Clear_Failed1.png")
        func:AddGraph("Clear_Failed2.png")
    end
end

function update(player)
    animeCounter[player + 1] = animeCounter[player + 1] + (33.3 * deltaTime)
    nowFrame[player + 1] = math.floor(animeCounter[player + 1] + 0.5)
end

function draw(player)
    if nowFrame[player + 1] <= 20 or not(useExtraAnime) then
        func:DrawGraph(x[player + 1], y[player + 1], tostring(math.min(nowFrame[player + 1], textureCount))..".png")
    elseif nowFrame[player + 1] >= 20 and nowFrame[player + 1] <= 67 then
        func:DrawGraph(x[player + 1] - 3, y[player + 1] + 47, "Clear_Failed.png")
    elseif nowFrame[player + 1] == 68 then
        func:DrawGraph(x[player + 1] - 3, y[player + 1] + 47, "Clear_Failed1.png")
    elseif nowFrame[player + 1] >= 69 then
        func:DrawGraph(x[player + 1] - 3, y[player + 1] + 47, "Clear_Failed2.png")
    end
end