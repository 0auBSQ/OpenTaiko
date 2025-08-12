local bgLoopWidth = 570
local flowerLoopWidth = 1086
local cloud1LoopWidth = 842
local cloud2LoopWidth = 842

local bgScrollX = 0
local flowerScrollX = 0
local cloud1ScrollX = 0
local cloud2ScrollX = 0

local clearOpacity = {0,0}

function clearIn(player)
end

function clearOut(player)
end

function init()
    if (playerCount == 1 and p1IsBlue == false) or playerCount == 2 then
        func:AddGraph("BG_Red.png")
        func:AddGraph("Flower_Red.png")
        func:AddGraph("Cloud1_Red.png")
        func:AddGraph("Cloud2_Red.png")
        bgLoopWidth = func:GetTextureWidth("BG_Red.png")
        flowerLoopWidth = func:GetTextureWidth("Flower_Red.png")
        cloud1LoopWidth = func:GetTextureWidth("Cloud1_Red.png")
        cloud2LoopWidth = func:GetTextureWidth("Cloud2_Red.png")

        -- if isClear[0] then clearOpacity[1] = 255 end -- If using danger gauge
    end
    if (playerCount == 1 and p1IsBlue == true) or playerCount == 2 then
        func:AddGraph("BG_Blue.png")
        func:AddGraph("Flower_Blue.png")
        func:AddGraph("Cloud1_Blue.png")
        func:AddGraph("Cloud2_Blue.png")
        bgLoopWidth = func:GetTextureWidth("BG_Blue.png")
        flowerLoopWidth = func:GetTextureWidth("Flower_Blue.png")
        cloud1LoopWidth = func:GetTextureWidth("Cloud1_Blue.png")
        cloud2LoopWidth = func:GetTextureWidth("Cloud2_Blue.png")

        -- if isClear[1] then clearOpacity[2] = 255 end -- If using danger gauge
    end
    if playerCount < 5 then
        func:AddGraph("BG_Clear.png")
        bgLoopWidth = func:GetTextureWidth("BG_Clear.png")
        if playerCount < 4 then
            func:AddGraph("Flower_Clear.png")
            func:AddGraph("Cloud1_Clear.png")
            func:AddGraph("Cloud2_Clear.png")
            flowerLoopWidth = func:GetTextureWidth("Flower_Clear.png")
            cloud1LoopWidth = func:GetTextureWidth("Cloud1_Clear.png")
            cloud2LoopWidth = func:GetTextureWidth("Cloud2_Clear.png")
        end
    end

    -- random values to create initial depth
    bgScrollX = 500
    cloud1ScrollX = 250
    cloud2ScrollX = 314
end

function update()
    bgScrollX = (bgScrollX + (deltaTime * 20)) % bgLoopWidth
    flowerScrollX = (flowerScrollX + (deltaTime * 27)) % flowerLoopWidth
    cloud1ScrollX = (cloud1ScrollX + (deltaTime * 40)) % cloud1LoopWidth
    cloud2ScrollX = (cloud2ScrollX + (deltaTime * 59)) % cloud2LoopWidth

    if isClear[0] then
        clearOpacity[1] = math.min(clearOpacity[1] + (2000 * deltaTime), 255)
    else
        clearOpacity[1] = math.max(clearOpacity[1] - (2000 * deltaTime), 0)
    end

    if playerCount == 2 then
        if isClear[1] then
            clearOpacity[2] = math.min(clearOpacity[2] + (2000 * deltaTime), 255)
        else
            clearOpacity[2] = math.max(clearOpacity[2] - (2000 * deltaTime), 0)
        end        
    end

end


function draw()
    if playerCount == 1 then
        if clearOpacity[1] < 255 then
            if p1IsBlue then
                func:DrawRectGraph(0, 0, bgScrollX, 0, 1920, 288, "BG_Blue.png")
                func:DrawRectGraph(0, 0, flowerScrollX, 0, 1920, 288, "Flower_Blue.png")
                func:DrawRectGraph(0, 0, cloud1ScrollX, 0, 1920, 288, "Cloud1_Blue.png")
                func:DrawRectGraph(0, 0, cloud2ScrollX, 0, 1920, 288, "Cloud2_Blue.png")
            else
                func:DrawRectGraph(0, 0, bgScrollX, 0, 1920, 288, "BG_Red.png")
                func:DrawRectGraph(0, 0, flowerScrollX, 0, 1920, 288, "Flower_Red.png")
                func:DrawRectGraph(0, 0, cloud1ScrollX, 0, 1920, 288, "Cloud1_Red.png")
                func:DrawRectGraph(0, 0, cloud2ScrollX, 0, 1920, 288, "Cloud2_Red.png")
            end
        end
        if clearOpacity[1] > 0 then
            func:SetOpacity(clearOpacity[1], "BG_Clear.png")
            func:SetOpacity(clearOpacity[1], "Flower_Clear.png")
            func:SetOpacity(clearOpacity[1], "Cloud1_Clear.png")
            func:SetOpacity(clearOpacity[1], "Cloud2_Clear.png")
            func:DrawRectGraph(0, 0, bgScrollX, 0, 1920, 288, "BG_Clear.png")
            func:DrawRectGraph(0, 0, cloud1ScrollX, 0, 1920, 288, "Cloud1_Clear.png")
            func:DrawRectGraph(0, 0, cloud2ScrollX, 0, 1920, 288, "Cloud2_Clear.png")
            func:DrawRectGraph(0, 0, flowerScrollX, 0, 1920, 288, "Flower_Clear.png")
        end
    elseif playerCount == 2 then
        if clearOpacity[1] < 255 then
            func:DrawRectGraph(0, 0, bgScrollX, 0, 1920, 288, "BG_Red.png")
            func:DrawRectGraph(0, 0, flowerScrollX, 0, 1920, 288, "Flower_Red.png")
            func:DrawRectGraph(0, 0, cloud1ScrollX, 0, 1920, 288, "Cloud1_Red.png")
            func:DrawRectGraph(0, 0, cloud2ScrollX, 0, 1920, 288, "Cloud2_Red.png")
        end
        if clearOpacity[1] > 0 then
            func:SetOpacity(clearOpacity[1], "BG_Clear.png")
            func:SetOpacity(clearOpacity[1], "Flower_Clear.png")
            func:SetOpacity(clearOpacity[1], "Cloud1_Clear.png")
            func:SetOpacity(clearOpacity[1], "Cloud2_Clear.png")
            func:DrawRectGraph(0, 0, bgScrollX, 0, 1920, 288, "BG_Clear.png")
            func:DrawRectGraph(0, 0, cloud1ScrollX, 0, 1920, 288, "Cloud1_Clear.png")
            func:DrawRectGraph(0, 0, cloud2ScrollX, 0, 1920, 288, "Cloud2_Clear.png")
            func:DrawRectGraph(0, 0, flowerScrollX, 0, 1920, 288, "Flower_Clear.png")
        end

        if clearOpacity[2] < 255 then
            func:DrawRectGraph(0, 804, bgScrollX, 0, 1920, 288, "BG_Blue.png")
            func:DrawRectGraph(0, 804, flowerScrollX, 0, 1920, 288, "Flower_Blue.png")
            func:DrawRectGraph(0, 804, cloud1ScrollX, 0, 1920, 288, "Cloud1_Blue.png")
            func:DrawRectGraph(0, 804, cloud2ScrollX, 0, 1920, 288, "Cloud2_Blue.png")
        end
        if clearOpacity[2] > 0 then
            func:SetOpacity(clearOpacity[2], "BG_Clear.png")
            func:SetOpacity(clearOpacity[2], "Flower_Clear.png")
            func:SetOpacity(clearOpacity[2], "Cloud1_Clear.png")
            func:SetOpacity(clearOpacity[2], "Cloud2_Clear.png")
            func:DrawRectGraph(0, 804, bgScrollX, 0, 1920, 288, "BG_Clear.png")
            func:DrawRectGraph(0, 804, cloud1ScrollX, 0, 1920, 288, "Cloud1_Clear.png")
            func:DrawRectGraph(0, 804, cloud2ScrollX, 0, 1920, 288, "Cloud2_Clear.png")
            func:DrawRectGraph(0, 804, flowerScrollX, 0, 1920, 288, "Flower_Clear.png")
        end
    elseif playerCount == 3 then
        func:DrawRectGraph(0, 804, bgScrollX, 0, 1920, 288, "BG_Clear.png")
        func:DrawRectGraph(0, 804, cloud1ScrollX, 0, 1920, 288, "Cloud1_Clear.png")
        func:DrawRectGraph(0, 804, cloud2ScrollX, 0, 1920, 288, "Cloud2_Clear.png")
        func:DrawRectGraph(0, 804, flowerScrollX, 0, 1920, 288, "Flower_Clear.png")
    elseif playerCount == 4 then
        func:DrawRectGraph(0, 0, bgScrollX, 0, 1920, 1080, "BG_Clear.png")
    end
end
