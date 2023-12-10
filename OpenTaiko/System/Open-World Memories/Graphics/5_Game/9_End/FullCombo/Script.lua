--func:DrawText(x, y, text);
--func:DrawNum(x, y, num);
--func:AddGraph("filename");
--func:DrawGraph(x, y, filename);
--func:SetOpacity(opacity, "filename");
--func:SetScale(xscale, yscale, "filename");
--func:SetColor(r, g, b, "filename");

local y = { 202, 466, 0, 0, 0 }

local animeCounter = { 0, 0, 0, 0, 0 }
local nowFrame = { 0, 0, 0, 0, 0 }

local textureCount = 39

function playEndAnime(player)
    animeCounter = { 0, 0, 0, 0, 0 }
    nowFrame = { 0, 0, 0, 0, 0 }
end

function init()

    if playerCount <= 2 then
        y = { 202, 466, 0, 0, 0 }
    elseif playerCount == 5 then
        y = { -50, 166, 382, 598, 814 }
    else
        y = { -17, 247, 511, 775, 0 }
    end

    for i = 0 , textureCount do
        func:AddGraph(tostring(i)..".png")
    end
end

function update(player)
    animeCounter[player + 1] = animeCounter[player + 1] + (45.4 * deltaTime)
    nowFrame[player + 1] = math.min(math.floor(animeCounter[player + 1] + 0.5), textureCount)
end

function draw(player)
      func:DrawGraph(500, y[player + 1], tostring(nowFrame[player + 1]) .. ".png")
end
