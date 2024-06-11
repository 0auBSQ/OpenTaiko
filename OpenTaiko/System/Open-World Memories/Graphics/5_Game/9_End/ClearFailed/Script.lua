--func:DrawText(x, y, text);
--func:DrawNum(x, y, num);
--func:AddGraph("filename");
--func:DrawGraph(x, y, filename);
--func:SetOpacity(opacity, "filename");
--func:SetScale(xscale, yscale, "filename");
--func:SetColor(r, g, b, "filename");

local y = { 218, 482, 0, 0, 0 }

local animeCounter = { 0, 0, 0, 0, 0 }
local nowFrame = { 0, 0, 0, 0, 0 }

local slideInFrame = { 0, 0, 0, 0, 0 }
local fallFrame = { 0, 0, 0, 0, 0 }
local slideToFallFrameCount = 5
local textureCount = 32

local frameRate = 24

local allfailed = false

local templateMoyai_y = -1080

function playEndAnime(player)
    animeCounter = { 0, 0, 0, 0, 0 }
    nowFrame = { 0, 0, 0, 0, 0 }
    
    if player == 0 then
        allfailed = true
    elseif isClear[player] then
        allfailed = false
    end
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

    func:AddGraph("TemplateMoyai.png")
end

function update(player)

    animeCounter[player + 1] = animeCounter[player + 1] + deltaTime
    slideInFrame[player + 1] = math.min(math.floor(animeCounter[player + 1] * frameRate), 23)
    fallFrame[player + 1] = math.max(math.min(math.floor(((animeCounter[player + 1] - 1.133) * frameRate) + slideToFallFrameCount), 9), 0)

    nowFrame[player + 1] = slideInFrame[player + 1] + fallFrame[player + 1]

    if animeCounter[player + 1] > 1.1 and player == 0 then
        templateMoyai_y = math.min(templateMoyai_y + (deltaTime * 5120), 0)
    end

end

function draw(player)
    func:DrawGraph(500, y[player + 1] - 10, tostring(nowFrame[player + 1]) .. ".png")

    if allfailed and playerCount > 2 then
        func:DrawGraph(0, templateMoyai_y, "TemplateMoyai.png")
    end
end
