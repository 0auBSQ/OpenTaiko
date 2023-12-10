local loopWidth = 1920
local loopHeight = 474
local downY = 540

local bgClearFade = 0

local bgScrollX = 0

local starsFadeTime = 0
local starsFade1 = 0
local starsFade2 = 0
local starsFade3 = 0
local starsFade4 = 0

function clearIn(player)
end

function clearOut(player)
end

function init()
    func:AddGraph("Day.png");
    func:AddGraph("Clear_BG.png");
    func:AddGraph("Clear_FG.png");
    func:AddGraph("Stars_1.png");
    func:AddGraph("Stars_2.png");
    func:AddGraph("Stars_3.png");
    func:AddGraph("Stars_4.png");
    func:AddGraph("Clouds.png");
end

function update()
    -- Clear fade
    if isClear[0] then
        bgClearFade = bgClearFade + (2000 * deltaTime);
    else
        bgClearFade = bgClearFade - (2000 * deltaTime);
    end

    if bgClearFade > 255 then
        bgClearFade = 255;
    end
    if bgClearFade < 0 then
        bgClearFade = 0;
    end

    starsFadeTime = starsFadeTime + deltaTime

    starsFade1 = 0.38 * math.sin(((5 * starsFadeTime) / 1.37)) + 0.83
    starsFade2 = 0.38 * math.cos(((5 * starsFadeTime) / 1.56)) + 0.75
    starsFade3 = 0.38 * math.sin(((5 * starsFadeTime) / 1.71)) + 0.25
    starsFade4 = 0.38 * math.cos(((5 * starsFadeTime) / 2.3)) + 0.49

    -- Cloud scroll
    bgScrollX = bgScrollX + (50 * deltaTime);
end

function draw()
    func:SetOpacity(bgClearFade, "Clear_BG.png");
    func:SetOpacity(bgClearFade, "Clear_FG.png");
    func:SetOpacity(bgClearFade, "Clouds.png");
    func:SetOpacity(bgClearFade * starsFade1, "Stars_1.png");
    func:SetOpacity(bgClearFade * starsFade2, "Stars_2.png");
    func:SetOpacity(bgClearFade * starsFade3, "Stars_3.png");
    func:SetOpacity(bgClearFade * starsFade4, "Stars_4.png");

    func:DrawGraph(0, downY, "Day.png")

    func:DrawGraph(0, downY, "Clear_BG.png")
    func:DrawRectGraph(0, downY, bgScrollX * 0.35, 0, loopWidth, loopHeight, "Stars_1.png")
    func:DrawRectGraph(0, downY, bgScrollX * 0.3, 0, loopWidth, loopHeight, "Stars_2.png")
    func:DrawRectGraph(0, downY, bgScrollX * 0.25, 0, loopWidth, loopHeight, "Stars_3.png")
    func:DrawRectGraph(0, downY, bgScrollX * 0.2, 0, loopWidth, loopHeight, "Stars_4.png")
    func:DrawRectGraph(0, downY, bgScrollX, 0, loopWidth, 474, "Clouds.png")
    func:DrawGraph(0, downY, "Clear_FG.png")
end
