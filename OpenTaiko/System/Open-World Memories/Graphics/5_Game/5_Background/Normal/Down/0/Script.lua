local loopWidth = 1920

local bgClearFade = 0

local bgScrollX = 0
local bgClearAnime = 0

function clearIn(player)
    bgClearAnime = 0
end

function clearOut(player)
end

function init()
    func:AddGraph("Down.png");
    func:AddGraph("Down_Clear.png");
    func:AddGraph("Down_Scroll.png");
end

function update()
    if isClear[0] then
        bgClearFade = bgClearFade + (2000 * deltaTime);
    else
        bgClearFade = bgClearFade - (2000 * deltaTime);
    end

    bgScrollX = bgScrollX + (250 * deltaTime);
    bgClearAnime = bgClearAnime + (2 * deltaTime)

    if bgClearFade > 255 then
        bgClearFade = 255;
    end
    if bgClearFade < 0 then
        bgClearFade = 0;
    end

    if bgScrollX > loopWidth then
        bgScrollX = 0;
    end

    if bgClearAnime > 1 then
        bgClearAnime = 1
    end
end

function draw()
    func:SetOpacity(bgClearFade, "Down_Clear.png");
    func:SetOpacity(bgClearFade, "Down_Scroll.png");

    func:DrawGraph(0, 540, "Down.png");
    func:DrawGraph(0, 540, "Down_Clear.png");

    moveY = 474 - bgClearAnime * 474
    moveY = moveY - (math.sin(bgClearAnime * math.pi) * 250)

    for i = 0, 2 do
        func:DrawGraph(0 + (loopWidth * i) - bgScrollX, 540 + moveY, "Down_Scroll.png");
    end
end
