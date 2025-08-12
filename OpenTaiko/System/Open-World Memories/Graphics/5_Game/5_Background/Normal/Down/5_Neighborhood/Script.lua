local downY = 540
local bgClearFade = 0
local starsGlowTime = 0
local rotation = 0

function clearIn(player)
end

function clearOut(player)
end

function init()
    func:AddGraph("Down.png");
    func:AddGraph("Night.png");
    func:AddGraph("Stars.png");
    func:SetScale(1.2, 1.2, "Stars.png")
end

function update()
    -- Clear fade
    if isClear[0] then
        bgClearFade = math.min(bgClearFade + (2000 * deltaTime), 255);
    else
        bgClearFade = math.max(bgClearFade - (2000 * deltaTime), 0);
    end

    if not simplemode then
        starsGlowTime = starsGlowTime + deltaTime
    end

    if not simplemode then
        rotation = rotation + deltaTime
    end
end

function draw()
    if bgClearFade < 255 then
        func:DrawGraph(0, downY, "Down.png");
    end
    if bgClearFade > 0 then
        func:SetOpacity(bgClearFade, "Night.png");
        func:SetOpacity((bgClearFade / 5) * (math.sin(starsGlowTime * 5) + 4), "Stars.png");
        func:SetRotation(math.sin(rotation / 1.6) * 4, "Stars.png")

        func:DrawGraph(0, downY, "Night.png");
        func:DrawGraphCenter(960, downY + 270, "Stars.png");
    end
end
