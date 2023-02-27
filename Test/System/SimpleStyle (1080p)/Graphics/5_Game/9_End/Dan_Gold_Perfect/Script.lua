--func:DrawText(x, y, text);
--func:DrawNum(x, y, num);
--func:AddGraph("filename");
--func:DrawGraph(x, y, filename);
--func:SetOpacity(opacity, "filename");
--func:SetScale(xscale, yscale, "filename");
--func:SetColor(r, g, b, "filename");

local x = { 498, 498, 498, 498, 498 }
local y = { 288, 552, 0, 0, 0 }

local animeCounter = { 0, 0, 0, 0, 0 }
local nowFrame = { 0, 0, 0, 0, 0 }

local textureCount = 61

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
        y = { 288, 552, 0, 0, 0 }
    elseif playerCount == 5 then
        y = { 36, 252, 468, 684, 900 }
    else
        y = { 69, 333, 597, 861, 0 }
    end

    func:AddGraph("bg.png")
    for i = 0 , textureCount do
        func:AddGraph(tostring(i)..".png")
    end
end

function update(player)
    animeCounter[player + 1] = animeCounter[player + 1] + (30.3 * deltaTime)
    nowFrame[player + 1] = math.floor(animeCounter[player + 1] + 0.5)
end

function draw(player)
    if nowFrame[player + 1] >= 34 then
        func:DrawGraph(x[player + 1], y[player + 1], "bg.png")
    end
    if nowFrame[player + 1] <= 20 or not(useExtraAnime) then
        func:DrawGraph(x[player + 1] - 3, y[player + 1] - 213, tostring(math.min(nowFrame[player + 1], textureCount))..".png")
    end
end