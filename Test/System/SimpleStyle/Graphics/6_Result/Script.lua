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

local bg_width = 1280
local bg_height = 720

local cloud_width = 1200
local cloud_height = 360

local cloud_count = 11

local cloud_x = { 642, 612, 652, 1148, 1180, 112, 8, 1088, 1100, 32, 412 }
local cloud_y = { 202, 424, 636, 530, 636, 636, 102, 52, 108, 326, 644 }
local cloud_move = { 150, 120, 180, 60, 90, 150, 120, 50, 45, 120, 180 }

local shine_count = 6

local shine_x = { 
    { 885, 1255, 725, 890, 1158, 1140 }, 
    { 395, 25, 555, 390, 122, 140 }
}

local shine_y = { 
    { 650, 405, 645, 420, 202, 585 },
    { 650, 405, 645, 420, 202, 585 }
}

local shine_size = { 0.44, 0.6, 0.4, 0.15, 0.35, 0.6 }

local work_count = 3

local work_x = {
    { 800, 900, 1160 },
    { 480, 380, 120 }
}
local work_y = {
    { 435, 185, 260 },
    { 435, 185, 260 }
}

local worksTimeStamp = { 1000, 2000, 3000 }

local commonCounter = 0

local gaugeFactor = 0
local mountainAppearValue = 0
local mountainClearIncounter = 0

local shineCounter = 0
local workCounter = 0

function skipAnime()
    commonCounter = mountainAppearValue
end

function clearIn(player)
end

function clearOut(player)
end

function init()
    func:AddGraph("Background.png")

    func:AddGraph("Background_0.png")
    func:AddGraph("Background_1.png")
    func:AddGraph("Background_2.png")
    func:AddGraph("Background_3.png")
    func:AddGraph("Background_4.png")
    func:AddGraph("Background_5.png")
    
    func:AddGraph("Background_Mountain_0.png")
    func:AddGraph("Background_Mountain_1.png")
    func:AddGraph("Background_Mountain_2.png")
    func:AddGraph("Background_Mountain_3.png")
    
    func:AddGraph("Cloud.png")
    func:AddGraph("Shine.png")
    
    for i = 0, work_count - 1 do
        func:AddGraph("Work/"..tostring(i)..".png")
    end

    commonCounter = 0
    gaugeFactor = math.max(gauge[0], math.max(gauge[1], math.max(gauge[2], math.max(gauge[3], gauge[4])))) / 2
    mountainAppearValue = 10275 + (66 * gaugeFactor)
    mountainClearIncounter = 0

    shineCounter = 0
end

function update()
    commonCounter = commonCounter + (deltaTime * 1000)

    if commonCounter >= mountainAppearValue then
        mountainClearIncounter = mountainClearIncounter + (deltaTime * 333)
        mountainClearIncounter = math.min(mountainClearIncounter, 515)
    end
    
    shineCounter = shineCounter + (deltaTime * 1000)
    if shineCounter > 1000 then 
        shineCounter = 0
    end

    workCounter = workCounter + (deltaTime * 1000)
    if workCounter > 4000 then 
        workCounter = 0
    end
end

function draw()
    func:DrawGraph(0, 0, "Background.png")

    gaugeAnimFactors = (commonCounter - mountainAppearValue) * 3

    if playerCount == 1 then 

        index = 0
        if p1IsBlue then 
            index = 2
        end

        mountainScale = 1.0
        
        if commonCounter >= mountainAppearValue and isClear[0] then 
            func:SetOpacity(gaugeAnimFactors, "Background_1.png")
            func:SetOpacity(255 - gaugeAnimFactors, "Background_Mountain_"..tostring(index)..".png")
            func:SetOpacity(gaugeAnimFactors, "Background_Mountain_"..tostring(index + 1)..".png")

            if mountainClearIncounter <= 90 then 
                mountainScale = 1.0 - math.sin(mountainClearIncounter * (math.pi / 180)) * 0.18
            elseif mountainClearIncounter <= 225 then 
                mountainScale = 0.82 + math.sin((mountainClearIncounter - 90) / 1.5 * (math.pi / 180)) * 0.58
            elseif mountainClearIncounter <= 245 then 
                mountainScale = 1.4
            elseif mountainClearIncounter <= 335 then 
                mountainScale = 0.9 + math.sin((mountainClearIncounter - 155) * (math.pi / 180)) * 0.5
            elseif mountainClearIncounter <= 515 then 
                mountainScale = 0.9 + math.sin((mountainClearIncounter - 335) * (math.pi / 180)) * 0.4
            else    
                mountainScale = 0.9
            end
        else 
            func:SetOpacity(0, "Background_1.png")
            func:SetOpacity(255, "Background_Mountain_"..tostring(index)..".png")
            func:SetOpacity(0, "Background_Mountain_"..tostring(index + 1)..".png")
        end

        func:SetScale(1.0, mountainScale, "Background_Mountain_"..tostring(index + 1)..".png")

        if p1IsBlue then 
            func:DrawGraph(0, 0, "Background_2.png")
        else 
            func:DrawGraph(0, 0, "Background_0.png")
        end
        func:DrawGraph(0, 0, "Background_1.png")
        
        func:DrawGraph(0, 0 - ((mountainScale - 1.0) * bg_height), "Background_Mountain_"..tostring(index)..".png")
        func:DrawGraph(0, 0 - ((mountainScale - 1.0) * bg_height), "Background_Mountain_"..tostring(index + 1)..".png")

        
        func:SetScale(0.65, 0.65, "Cloud.png")

        cloudOpacity = 0
        if commonCounter >= mountainAppearValue and isClear[0] then 
            cloudOpacity = math.min(255, math.max(0, commonCounter - mountainAppearValue))
        end

        for i = 0, cloud_count - 1 do
            move = (cloud_move[i + 1] * (commonCounter / 10000))

            clearValue = ((commonCounter - mountainAppearValue) % 10000) / 10000.0

            clearMove = (cloud_move[i + 1] * clearValue)

            func:SetOpacity(255 - cloudOpacity, "Cloud.png")
            func:DrawGraphRectCenter(cloud_x[i + 1] - move, cloud_y[i + 1], cloud_width * i, cloud_height * index, cloud_width, cloud_height, "Cloud.png")
            
            func:SetOpacity(cloudOpacity + (math.min(math.sin(clearValue * math.pi) * 1000, 255) - 255), "Cloud.png")
            func:DrawGraphRectCenter(cloud_x[i + 1] - clearMove, cloud_y[i + 1], cloud_width * i, cloud_height, cloud_width, cloud_height, "Cloud.png")
        end

        if commonCounter >= mountainAppearValue and isClear[0] then
            quadrant500 = shineCounter % 500

            for i = 0, shine_count - 1 do

                --shineCounter
                if (i < 2 and shineCounter >= 500) or (i >= 2 and shineCounter < 500) then
                    func:SetOpacity(0, "Shine.png")
                elseif quadrant500 >= 100 and quadrant500 <= 500 - 100 then
                    func:SetOpacity(255, "Shine.png")
                else
                    func:SetOpacity((255 * math.min(quadrant500, 500 - quadrant500)) / 100, "Shine.png")
                end
                func:SetScale(shine_size[i + 1], shine_size[i + 1], "Shine.png")
                
                if p1IsBlue then
                    func:DrawGraphCenter(shine_x[2][i + 1], shine_y[2][i + 1], "Shine.png")
                else 
                    func:DrawGraphCenter(shine_x[1][i + 1], shine_y[1][i + 1], "Shine.png")
                end
    
            end

            if commonCounter <= mountainAppearValue + 1000 then 
                for i = 0, work_count - 1 do
                    if commonCounter <= mountainAppearValue + 255 then 
                        tmpTimer = commonCounter - mountainAppearValue
                        func:SetOpacity(tmpTimer, "Work/"..tostring(i)..".png")
                        func:SetScale(0.6 * (tmpTimer / 225), 0.6 * (tmpTimer / 225), "Work/"..tostring(i)..".png")
                    else
                        tmpTimer = math.max(0, (2 * 255) - (commonCounter - mountainAppearValue - 255))
                        func:SetOpacity(tmpTimer, "Work/"..tostring(i)..".png")
                        func:SetScale(0.6, 0.6, "Work/"..tostring(i)..".png")
                    end
    
                    if p1IsBlue then
                        func:DrawGraphCenter(work_x[2][i + 1], work_y[2][i + 1], "Work/"..tostring(i)..".png")
                    else 
                        func:DrawGraphCenter(work_x[1][i + 1], work_y[1][i + 1], "Work/"..tostring(i)..".png")
                    end
                end
            else 
                for i = 0, work_count - 1 do
                    tmpStamp = worksTimeStamp[i + 1]
                    if workCounter <= tmpStamp + 255 then 
                        tmpTimer = workCounter - tmpStamp
                        func:SetOpacity(tmpTimer, "Work/"..tostring(i)..".png")
                        func:SetScale(0.6 * (tmpTimer / 225), 0.6 * (tmpTimer / 225), "Work/"..tostring(i)..".png")
                    else
                        tmpTimer = math.max(0, (2 * 255) - (workCounter - tmpStamp - 255))
                        func:SetOpacity(tmpTimer / 2, "Work/"..tostring(i)..".png")
                        func:SetScale(0.6 , 0.6, "Work/"..tostring(i)..".png")
                    end
    
                    if p1IsBlue then
                        func:DrawGraphCenter(work_x[2][i + 1], work_y[2][i + 1], "Work/"..tostring(i)..".png")
                    else 
                        func:DrawGraphCenter(work_x[1][i + 1], work_y[1][i + 1], "Work/"..tostring(i)..".png")
                    end
                end
            end
        end
    elseif playerCount == 2 then 

        for i = 0, 1 do

            if isClear[i] then 
                func:SetOpacity(gaugeAnimFactors, "Background_1.png")
            else 
                func:SetOpacity(0, "Background_1.png")
            end
            
            func:DrawRectGraph((bg_width / 2) * i, 0, (bg_width / 2) * i, 0, (bg_width / 2), bg_height, "Background_"..tostring(2 * i)..".png")
            func:DrawRectGraph((bg_width / 2) * i, 0, (bg_width / 2) * i, 0, (bg_width / 2), bg_height, "Background_1.png")
        end
    else

        drawCount = math.max(playerCount, 4)
        for i = 0, drawCount - 1 do
            index = i + 1
            if i == 0 then
                index = 0
            end

            if isClear[i] then 
                func:SetOpacity(gaugeAnimFactors, "Background_1.png")
            else 
                func:SetOpacity(0, "Background_1.png")
            end

            func:DrawRectGraph((bg_width / drawCount) * i, 0, (bg_width / drawCount) * i, 0, (bg_width / drawCount), bg_height, "Background_"..tostring(index)..".png")
            func:DrawRectGraph((bg_width / drawCount) * i, 0, (bg_width / drawCount) * i, 0, (bg_width / drawCount), bg_height, "Background_1.png")
        end
    end
end
