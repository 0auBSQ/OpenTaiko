local bgClearFade = 0
local clearMultiplier = -1

function clearIn(player)
    clearMultiplier = 1
end

function clearOut(player)
    clearMultiplier = -1
end

function init()
    func:AddGraph("Down.png");
    func:AddGraph("Down_Clear.png");

    clearMultiplier = -1
end

function update()

    bgClearFade = bgClearFade + (clearMultiplier * 2000 * deltaTime);

    if bgClearFade > 255 then
        bgClearFade = 255;
    end
    if bgClearFade < 0 then
        bgClearFade = 0;
    end

end

function draw()
    func:SetOpacity(bgClearFade, "Down_Clear.png");

    func:DrawGraph(0, 540, "Down.png");

    if bgClearFade > 0 then
        func:DrawGraph(0, 540, "Down_Clear.png");
    end
end
