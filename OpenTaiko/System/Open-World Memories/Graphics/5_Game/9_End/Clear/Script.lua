--func:DrawText(x, y, text);
--func:DrawNum(x, y, num);
--func:AddGraph("filename");
--func:DrawGraph(x, y, filename);
--func:DrawRectGraph(x, y, rect_x, rect_y, rect_width, rect_height, filename);
--func:SetOpacity(opacity, "filename");
--func:SetScale(xscale, yscale, "filename");
--func:SetColor(r, g, b, "filename");


local y = { 216, 480, 0, 0, 0 }

local textureCount = 23

local animeCounter = { 0, 0, 0, 0, 0 }
local nowFrame = { 0, 0, 0, 0, 0 }

function playEndAnime(player)
    animeCounter = { 0, 0, 0, 0, 0 }
end

function init()

    if playerCount <= 2 then
        y = { 216, 480, 0, 0, 0 }
    elseif playerCount == 5 then
        y = { -5, 196, 412, 628, 844 }
    else
        y = { -24, 240, 504, 768, 0 }
    end

    for i = 0 , textureCount do
        func:AddGraph(tostring(i) .. ".png")
    end

end

function update(player)

    animeCounter[player + 1] = animeCounter[player + 1] + (45.4 * deltaTime)
    nowFrame[player + 1] = math.min(math.floor(animeCounter[player + 1] + 0.5), textureCount)

end

function draw(player)
    func:DrawGraph(500, y[player + 1], tostring(nowFrame[player + 1]) .. ".png")
end
