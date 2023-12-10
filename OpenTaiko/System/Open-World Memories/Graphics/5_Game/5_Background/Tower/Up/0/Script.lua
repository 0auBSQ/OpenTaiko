local bgWidth = 1920
local bgHeight = 276
local bgScrollX_1 = 0
local bgScrollX_2 = 0
local bgScrollX_3 = 0

function clearIn(player)
end

function clearOut(player)
end

function init()
    func:AddGraph("day.png");
    func:AddGraph("night.png");
    func:AddGraph("1.png");
    func:AddGraph("2.png");
    func:AddGraph("3.png");
end

function update()
    bgScrollX_1 = bgScrollX_1 + (88.65 * deltaTime);

    bgScrollX_2 = bgScrollX_2 + (68.85 * deltaTime);

    bgScrollX_3 = bgScrollX_3 + (65.7 * deltaTime);
end


function draw()
    func:DrawGraph(0, 0, "day.png");
    func:SetOpacity((towerNightNum * 255.0), "night.png");
    func:DrawGraph(0, 0, "night.png");

    colorTmp = 0.5 + (1 - towerNightNum) * 0.5;

    func:SetColor(colorTmp, colorTmp, colorTmp, "1.png");
    func:SetColor(colorTmp, colorTmp, colorTmp, "2.png");
    func:SetColor(colorTmp, colorTmp, colorTmp, "3.png");

    func:SetOpacity(colorTmp * 255.0, "1.png");
    func:SetOpacity(colorTmp * 255.0, "2.png");
    func:SetOpacity(colorTmp * 255.0, "3.png");

    func:DrawRectGraph(0, 0, bgScrollX_1, 0, bgWidth, bgHeight, "1.png")
    func:DrawRectGraph(0, 0, bgScrollX_2, 0, bgWidth, bgHeight, "2.png")
    func:DrawRectGraph(0, 0, bgScrollX_3, 0, bgWidth, bgHeight, "2.png")
    -- Debugging stuff
    --func:DrawNum(0,100,towerNightNum);
end
