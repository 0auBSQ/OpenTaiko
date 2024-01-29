--func:DrawText(x, y, text);
--func:DrawNum(x, y, num);
--func:AddGraph("filename");
--func:DrawGraph(x, y, filename);
--func:SetOpacity(opacity, "filename");
--func:SetScale(xscale, yscale, "filename");
--func:SetColor(r, g, b, "filename");

local x = { 499, 499, 499, 499, 499 }
local y = { 204, 468, 0, 0, 0 }

local imageHeight = 324

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
        y = { 204, 468, 0, 0, 0 }
    elseif playerCount == 5 then
        y = { -71, 145, 361, 577, 793 }
        imageHeight = 288
    else
        y = { -60, 206, 469, 733, 0 }
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
    if nowFrame[player + 1] <= 20 or not(useExtraAnime) then
        --func:DrawGraph(x[player + 1] - 3, y[player + 1], tostring(math.min(nowFrame[player + 1], textureCount))..".png")
        func:DrawRectGraph(x[player + 1] - 3, y[player + 1], 0, 0, 1426, imageHeight, tostring(math.min(nowFrame[player + 1], textureCount))..".png")
    end
end
