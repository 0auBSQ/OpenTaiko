--func:DrawText(x, y, text)
--func:DrawNum(x, y, num)
--func:AddGraph("filename")
--func:DrawGraph(x, y, filename)
--func:DrawRectGraph(x, y, rect_x, rect_y, rect_width, rect_height, filename)
--func:DrawGraphCenter(x, y, filename)
--func:DrawGraphRectCenter(x, y, rect_x, rect_y, rect_width, rect_height, filename)
--func:SetOpacity(opacity, "filename")
--func:SetRotation(angle, "fileName")
--func:SetScale(xscale, yscale, "filename")
--func:SetColor(r, g, b, "filename")


local y = { 210, 386, 0, 0, 0 }

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
        y = { 210, 386, 0, 0, 0 }
    elseif playerCount == 5 then
        y = { 36, 170, 314, 458, 602 }
    else
        y = { 64, 240, 416, 592, 0 }
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
            func:DrawRectGraph(634, y[player + 1] - ((90 * textScales[nowFrame[player + 1] - 17 + 1]) - 90), 0, 0, 90, 90, "Clear_Text.png")
        else
            func:SetScale(1.0, 1.0, "Clear_Text.png")
            func:DrawRectGraph(634, y[player + 1], 0, 0, 90, 90, "Clear_Text.png")
        end
    end
    if nowFrame[player + 1] >= 19 then
        if nowFrame[player + 1] <= 38 then
            func:SetScale(1.0, textScales[nowFrame[player + 1] - 19 + 1], "Clear_Text.png")
            func:SetOpacity(textOpacitys[nowFrame[player + 1] - 19 + 1], "Clear_Text.png")
            func:DrawRectGraph(692, y[player + 1] - ((90 * textScales[nowFrame[player + 1] - 19 + 1]) - 90), 90, 0, 90, 90, "Clear_Text.png")
        else
            func:SetScale(1.0, 1.0, "Clear_Text.png")
            func:DrawRectGraph(692, y[player + 1], 90, 0, 90, 90, "Clear_Text.png")
        end
    end
    if nowFrame[player + 1] >= 21 then
        if nowFrame[player + 1] <= 40 then
            func:SetScale(1.0, textScales[nowFrame[player + 1] - 21 + 1], "Clear_Text.png")
            func:SetOpacity(textOpacitys[nowFrame[player + 1] - 21 + 1], "Clear_Text.png")
            func:DrawRectGraph(750, y[player + 1] - ((90 * textScales[nowFrame[player + 1] - 21 + 1]) - 90), 180, 0, 90, 90, "Clear_Text.png")
        else
            func:SetScale(1.0, 1.0, "Clear_Text.png")
            func:DrawRectGraph(750, y[player + 1], 180, 0, 90, 90, "Clear_Text.png")
        end
    end
    if nowFrame[player + 1] >= 23 then
        if nowFrame[player + 1] <= 42 then
            func:SetScale(1.0, textScales[nowFrame[player + 1] - 23 + 1], "Clear_Text.png")
            func:SetOpacity(textOpacitys[nowFrame[player + 1] - 23 + 1], "Clear_Text.png")
            func:DrawRectGraph(819, y[player + 1] - ((90 * textScales[nowFrame[player + 1] - 23 + 1]) - 90), 270, 0, 90, 90, "Clear_Text.png")
        else
            func:SetScale(1.0, 1.0, "Clear_Text.png")
            func:DrawRectGraph(819, y[player + 1], 270, 0, 90, 90, "Clear_Text.png")
        end
    end
    if nowFrame[player + 1] >= 25 then
        if nowFrame[player + 1] <= 44 then
            func:SetScale(1.0, textScales[nowFrame[player + 1] - 25 + 1], "Clear_Text.png")
            func:SetOpacity(textOpacitys[nowFrame[player + 1] - 25 + 1], "Clear_Text.png")
            func:DrawRectGraph(890, y[player + 1] + 2 - ((90 * textScales[nowFrame[player + 1] - 25 + 1]) - 90), 360, 0, 90, 90, "Clear_Text.png")
        else
            func:SetScale(1.0, 1.0, "Clear_Text.png")
            func:DrawRectGraph(890, y[player + 1] + 2, 360, 0, 90, 90, "Clear_Text.png")
        end
    end

    if nowFrame[player + 1] >= 50 and nowFrame[player + 1] < 90 then
        if nowFrame[player + 1] < 70 then
            func:SetOpacity((nowFrame[player + 1] - 50) * (255 / 20), "Clear_Text_Effect.png")
            func:DrawGraph(634, y[player + 1] - 2, "Clear_Text_Effect.png")
        else
            func:SetOpacity(255 - ((nowFrame[player + 1] - 70) * (255 / 20)), "Clear_Text_Effect.png")
            func:DrawGraph(634, y[player + 1] - 2, "Clear_Text_Effect.png")
        end
    end
    
    if nowFrame[player + 1] <= 11 then
        func:DrawGraph(697, y[player + 1] - 30, "Clear_L_1.png")
        func:SetOpacity((11.0 / nowFrame[player + 1]) * 255, "Clear_L_1.png")
        
        func:DrawGraph(738, y[player + 1] - 30, "Clear_R_1.png")
        func:SetOpacity((11.0 / nowFrame[player + 1]) * 255, "Clear_R_1.png")
    elseif nowFrame[player + 1] <= 35 then
        func:DrawGraph(697 - ((nowFrame[player + 1] - 12) * 10), y[player + 1] - 30, "Clear_L_0.png")
        func:DrawGraph(738 + ((nowFrame[player + 1] - 12) * 10), y[player + 1] - 30, "Clear_R_0.png")
    elseif nowFrame[player + 1] <= 46 then
        
        func:DrawGraph(466, y[player + 1] - 30, "Clear_L_0.png")
        func:SetScale(side_ret[nowFrame[player + 1] - 36 + 1], 1.0, "Clear_L_0.png")
        
        func:DrawGraph(1136 - 180 * side_ret[nowFrame[player + 1] - 36 + 1], y[player + 1] - 30, "Clear_R_0.png")
        func:SetScale(side_ret[nowFrame[player + 1] - 36 + 1], 1.0, "Clear_R_0.png")
    elseif nowFrame[player + 1] <= 49 then
        func:DrawGraph(466, y[player + 1] - 30, "Clear_L_1.png")
        func:DrawGraph(956, y[player + 1] - 30, "Clear_R_1.png")
    elseif nowFrame[player + 1] <= 54 then
        func:DrawGraph(466, y[player + 1] - 30, "Clear_L_2.png")
        func:DrawGraph(956, y[player + 1] - 30, "Clear_R_2.png")
    elseif nowFrame[player + 1] <= 58 then
        func:DrawGraph(466, y[player + 1] - 30, "Clear_L_3.png")
        func:DrawGraph(956, y[player + 1] - 30, "Clear_R_3.png")
    else
        func:DrawGraph(466, y[player + 1] - 30, "Clear_L_4.png")
        func:DrawGraph(956, y[player + 1] - 30, "Clear_R_4.png")
    end

    func:DrawNum(0, 0, nowFrame[player + 1])
end
