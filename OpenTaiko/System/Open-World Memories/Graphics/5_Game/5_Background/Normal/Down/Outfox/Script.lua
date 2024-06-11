-- Please note that the Project OutFox branding or graphics included are not permitted
-- for use outside of OpenTaiko without permission from the Project OutFox Developers.

-- The graphics are included as part of the OpenTaiko Team Collaboration to promote
-- the use of Taiko and other games to a wider audience.

local effectLoopHeight = 544

local bgClearFade = 0

local effectScrollY = 0

local bgPath = "Down_Red.png"

function clearIn(player)
end

function clearOut(player)
end

function init()
    if p1IsBlue then
        bgPath = "Down_Blue.png"
    end
    func:AddGraph(bgPath);
    func:AddGraph("Down_Clear.png");
    func:AddGraph("Tile.png");
end

function update()

    if isClear[0] then
        bgClearFade = bgClearFade + (2000 * deltaTime);
    else
        bgClearFade = bgClearFade + (-2000 * deltaTime);
    end

    -- Don't scroll while SimpleMode is active
    if not simplemode then
        effectScrollY = effectScrollY + (30 * deltaTime);
    end

    if bgClearFade > 255 then
        bgClearFade = 255;
    end
    if bgClearFade < 0 then
        bgClearFade = 0;
    end

end

function draw()
    func:SetOpacity(bgClearFade, "Down_Clear.png");
    func:SetBlendMode("Add", "Tile.png");

    func:DrawGraph(0, 540, bgPath);

    func:DrawGraph(0, 540, "Down_Clear.png");

    func:DrawRectGraph(0, 540, 0, effectScrollY, 1920, effectLoopHeight, "Tile.png");
end
