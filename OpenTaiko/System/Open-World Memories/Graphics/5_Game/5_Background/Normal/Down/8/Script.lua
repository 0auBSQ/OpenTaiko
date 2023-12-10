--func:DrawRectGraph(x, y, rect_x, rect_y, rect_width, rect_height, filename);

local effectLoopHeight = 544

local bgClearFade = 0
local clearMultiplier = -1

local effectBGScrollY = 0
local effectFGScrollY = 0

function clearIn(player)
    clearMultiplier = 1
end

function clearOut(player)
    clearMultiplier = -1
end

function init()
    func:AddGraph("Down.png");
    func:AddGraph("Down_Clear.png");
    func:AddGraph("EffectBG.png");
    func:AddGraph("EffectFG.png");

    clearMultiplier = -1
end

function update()

    bgClearFade = bgClearFade + (clearMultiplier * 2000 * deltaTime);

    effectBGScrollY = effectBGScrollY + (60 * deltaTime);
    effectFGScrollY = effectFGScrollY + (100 * deltaTime);

    if bgClearFade > 255 then
        bgClearFade = 255;
    end
    if bgClearFade < 0 then
        bgClearFade = 0;
    end

    if effectBGScrollY > effectLoopHeight then
        effectBGScrollY = 0;
    end

    if effectFGScrollY > effectLoopHeight then
        effectFGScrollY = 0;
    end

end

function draw()
    func:SetOpacity(bgClearFade, "Down_Clear.png");
    func:SetOpacity(bgClearFade, "EffectBG.png");
    func:SetOpacity(bgClearFade, "EffectFG.png");

    func:DrawGraph(0, 540, "Down.png");

    func:DrawRectGraph(0, 540, 0, -effectBGScrollY, 1920, effectLoopHeight, "EffectBG.png");

    func:DrawGraph(0, 540, "Down_Clear.png");

    func:DrawRectGraph(0, 540, 0, -effectFGScrollY, 1920, effectLoopHeight, "EffectFG.png");
end
