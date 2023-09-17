--func:DrawText(x, y, text);
--func:DrawNum(x, y, num);
--func:AddGraph("filename");
--func:DrawGraph(x, y, filename);
--func:DrawRectGraph(x, y, rect_x, rect_y, rect_width, rect_height, filename);
--func:SetOpacity(opacity, "filename");
--func:SetScale(xscale, yscale, "filename");
--func:SetColor(r, g, b, "filename");


local y = { 315, 579, 0, 0, 0 }

local sideTextureCount = 4

local animeCounter = { 0, 0, 0, 0, 0 }
local nowFrame = { 0, 0, 0, 0, 0 }



local textScales = { 1.04, 1.11, 1.15, 1.19, 1.23, 1.26, 1.30, 1.31, 1.32, 1.32, 1.32, 1.30, 1.30, 1.26, 1.25, 1.19, 1.15, 1.11, 1.05, 1.0 }
local textOpacitys = { 43, 85, 128, 170, 213, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255 }

local side_ret = { 1.0, 0.99, 0.98, 0.97, 0.96, 0.95, 0.96, 0.97, 0.98, 0.99, 1.0 }

function clearIn(player)
end

function clearOut(player)
end

function playEndAnime(player)
    animeCounter = { 0, 0, 0, 0, 0 }
end

function init()

    if playerCount <= 2 then
        y = { 315, 579, 0, 0, 0 }
    elseif playerCount == 5 then
        y = { 54, 255, 471, 687, 903 }
    else
        y = { 96, 360, 624, 888, 0 }
    end
    
    func:AddGraph("Clear_Text.png")
    func:AddGraph("Clear_Text_Effect.png")

    for i = 0 , sideTextureCount do
        func:AddGraph("Clear_L_"..tostring(i)..".png")
        func:AddGraph("Clear_R_"..tostring(i)..".png")
    end
    
end

function update(player)
    
    animeCounter[player + 1] = animeCounter[player + 1] + (45.4 * deltaTime)
    nowFrame[player + 1] = math.floor(animeCounter[player + 1] + 0.5)
    
end

function draw(player)
    
    if nowFrame[player + 1] >= 17 then
        if nowFrame[player + 1] <= 36 then
            func:SetScale(1.0, textScales[nowFrame[player + 1] - 17 + 1], "Clear_Text.png")
            func:SetOpacity(textOpacitys[nowFrame[player + 1] - 17 + 1], "Clear_Text.png")
            func:DrawRectGraph(951, y[player + 1] - ((135 * textScales[nowFrame[player + 1] - 17 + 1]) - 135), 0, 0, 135, 135, "Clear_Text.png")
        else
            func:SetScale(1.0, 1.0, "Clear_Text.png")
            func:DrawRectGraph(951, y[player + 1], 0, 0, 135, 135, "Clear_Text.png")
        end
    end
    if nowFrame[player + 1] >= 19 then
        if nowFrame[player + 1] <= 38 then
            func:SetScale(1.0, textScales[nowFrame[player + 1] - 19 + 1], "Clear_Text.png")
            func:SetOpacity(textOpacitys[nowFrame[player + 1] - 19 + 1], "Clear_Text.png")
            func:DrawRectGraph(1038, y[player + 1] - ((135 * textScales[nowFrame[player + 1] - 19 + 1]) - 135), 135, 0, 135, 135, "Clear_Text.png")
        else
            func:SetScale(1.0, 1.0, "Clear_Text.png")
            func:DrawRectGraph(1038, y[player + 1], 135, 0, 135, 135, "Clear_Text.png")
        end
    end
    if nowFrame[player + 1] >= 21 then
        if nowFrame[player + 1] <= 40 then
            func:SetScale(1.0, textScales[nowFrame[player + 1] - 21 + 1], "Clear_Text.png")
            func:SetOpacity(textOpacitys[nowFrame[player + 1] - 21 + 1], "Clear_Text.png")
            func:DrawRectGraph(1125, y[player + 1] - ((135 * textScales[nowFrame[player + 1] - 21 + 1]) - 135), 270, 0, 135, 135, "Clear_Text.png")
        else
            func:SetScale(1.0, 1.0, "Clear_Text.png")
            func:DrawRectGraph(1125, y[player + 1], 270, 0, 135, 135, "Clear_Text.png")
        end
    end
    if nowFrame[player + 1] >= 23 then
        if nowFrame[player + 1] <= 42 then
            func:SetScale(1.0, textScales[nowFrame[player + 1] - 23 + 1], "Clear_Text.png")
            func:SetOpacity(textOpacitys[nowFrame[player + 1] - 23 + 1], "Clear_Text.png")
            func:DrawRectGraph(1229, y[player + 1] - ((135 * textScales[nowFrame[player + 1] - 23 + 1]) - 135), 405, 0, 135, 135, "Clear_Text.png")
        else
            func:SetScale(1.0, 1.0, "Clear_Text.png")
            func:DrawRectGraph(1229, y[player + 1], 405, 0, 135, 135, "Clear_Text.png")
        end
    end
    if nowFrame[player + 1] >= 25 then
        if nowFrame[player + 1] <= 44 then
            func:SetScale(1.0, textScales[nowFrame[player + 1] - 25 + 1], "Clear_Text.png")
            func:SetOpacity(textOpacitys[nowFrame[player + 1] - 25 + 1], "Clear_Text.png")
            func:DrawRectGraph(1335, y[player + 1] + 2 - ((135 * textScales[nowFrame[player + 1] - 25 + 1]) - 135), 540, 0, 135, 135, "Clear_Text.png")
        else
            func:SetScale(1.0, 1.0, "Clear_Text.png")
            func:DrawRectGraph(1335, y[player + 1] + 2, 540, 0, 135, 135, "Clear_Text.png")
        end
    end

    if nowFrame[player + 1] >= 50 and nowFrame[player + 1] < 90 then
        if nowFrame[player + 1] < 70 then
            func:SetOpacity((nowFrame[player + 1] - 50) * (255 / 20), "Clear_Text_Effect.png")
            func:DrawGraph(951, y[player + 1] - 2, "Clear_Text_Effect.png")
        else
            func:SetOpacity(255 - ((nowFrame[player + 1] - 70) * (255 / 20)), "Clear_Text_Effect.png")
            func:DrawGraph(951, y[player + 1] - 2, "Clear_Text_Effect.png")
        end
    end
    
    if nowFrame[player + 1] <= 11 then
        func:DrawGraph(1046, y[player + 1] - 30, "Clear_L_1.png")
        func:SetOpacity((11.0 / nowFrame[player + 1]) * 255, "Clear_L_1.png")
        
        func:DrawGraph(1107, y[player + 1] - 30, "Clear_R_1.png")
        func:SetOpacity((11.0 / nowFrame[player + 1]) * 255, "Clear_R_1.png")
    elseif nowFrame[player + 1] <= 35 then
        func:DrawGraph(1046 - ((nowFrame[player + 1] - 12) * 10 * 1.5), y[player + 1] - 30, "Clear_L_0.png")
        func:DrawGraph(1107 + ((nowFrame[player + 1] - 12) * 10 * 1.5), y[player + 1] - 30, "Clear_R_0.png")
    elseif nowFrame[player + 1] <= 46 then
        
        func:DrawGraph(699, y[player + 1] - 45, "Clear_L_0.png")
        func:SetScale(side_ret[nowFrame[player + 1] - 36 + 1], 1.0, "Clear_L_0.png")
        
        func:DrawGraph(1704 - 270 * side_ret[nowFrame[player + 1] - 36 + 1], y[player + 1] - 45, "Clear_R_0.png")
        func:SetScale(side_ret[nowFrame[player + 1] - 36 + 1], 1.0, "Clear_R_0.png")
    elseif nowFrame[player + 1] <= 49 then
        func:DrawGraph(699, y[player + 1] - 45, "Clear_L_1.png")
        func:DrawGraph(1434, y[player + 1] - 45, "Clear_R_1.png")
    elseif nowFrame[player + 1] <= 54 then
        func:DrawGraph(699, y[player + 1] - 45, "Clear_L_2.png")
        func:DrawGraph(1434, y[player + 1] - 45, "Clear_R_2.png")
    elseif nowFrame[player + 1] <= 58 then
        func:DrawGraph(699, y[player + 1] - 45, "Clear_L_3.png")
        func:DrawGraph(1434, y[player + 1] - 45, "Clear_R_3.png")
    else
        func:DrawGraph(699, y[player + 1] - 45, "Clear_L_4.png")
        func:DrawGraph(1434, y[player + 1] - 45, "Clear_R_4.png")
    end

    func:DrawNum(0, 0, nowFrame[player + 1])
end
