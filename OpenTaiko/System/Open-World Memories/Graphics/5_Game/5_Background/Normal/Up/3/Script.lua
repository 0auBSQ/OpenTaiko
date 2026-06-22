local bgLoopWidth = 1800
local cloudLoopWidth = 1086
local noteLoopWidth = 1086

local bgScrollX = 0
local cloudScrollX = 0
local noteScrollX = 0

local clearOpacity = {0,0}

function clearIn(player)
end

function clearOut(player)
end

function init()
    if (playerCount == 1 and p1IsBlue == false) or playerCount == 2 then
        func:AddGraph("BG_Red.png")
        func:AddGraph("Cloud_Red.png")
        func:AddGraph("Note_Red.png")

        -- if isClear[0] then clearOpacity[1] = 255 end -- If using danger gauge
    end
    if (playerCount == 1 and p1IsBlue == true) or playerCount == 2 then
        func:AddGraph("BG_Blue.png")
        func:AddGraph("Cloud_Blue.png")
        func:AddGraph("Note_Blue.png")

        -- if isClear[1] then clearOpacity[2] = 255 end -- If using danger gauge
    end
    if playerCount < 5 then
        func:AddGraph("BG_Clear.png")
        if playerCount < 4 then
            func:AddGraph("Cloud_Clear.png")
            func:AddGraph("Note_Clear.png")
        end
    end

    -- random values to create initial depth
    bgScrollX = 500
    cloudScrollX = 250
    noteScrollX = 314
end

function update()
    bgScrollX = (bgScrollX + (deltaTime * 20)) % bgLoopWidth
    cloudScrollX = (cloudScrollX + (deltaTime * 27)) % cloudLoopWidth
    noteScrollX = (noteScrollX + (deltaTime * 59)) % noteLoopWidth

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
                func:DrawRectGraph(0, 0, cloudScrollX, 0, 1920, 288, "Cloud_Blue.png")
                func:DrawRectGraph(0, 0, noteScrollX, 0, 1920, 288, "Note_Blue.png")
            else
                func:DrawRectGraph(0, 0, bgScrollX, 0, 1920, 288, "BG_Red.png")
                func:DrawRectGraph(0, 0, cloudScrollX, 0, 1920, 288, "Cloud_Red.png")
                func:DrawRectGraph(0, 0, noteScrollX, 0, 1920, 288, "Note_Red.png")
            end
        end
        if clearOpacity[1] > 0 then
            func:SetOpacity(clearOpacity[1], "BG_Clear.png")
            func:SetOpacity(clearOpacity[1], "Cloud_Clear.png")
            func:SetOpacity(clearOpacity[1], "Note_Clear.png")
            func:DrawRectGraph(0, 0, bgScrollX, 0, 1920, 288, "BG_Clear.png")
            func:DrawRectGraph(0, 0, noteScrollX, 0, 1920, 288, "Note_Clear.png")
            func:DrawRectGraph(0, 0, cloudScrollX, 0, 1920, 288, "Cloud_Clear.png")
        end
    elseif playerCount == 2 then
        if clearOpacity[1] < 255 then
            func:DrawRectGraph(0, 0, bgScrollX, 0, 1920, 288, "BG_Red.png")
            func:DrawRectGraph(0, 0, cloudScrollX, 0, 1920, 288, "Cloud_Red.png")
            func:DrawRectGraph(0, 0, noteScrollX, 0, 1920, 288, "Note_Red.png")
        end
        if clearOpacity[1] > 0 then
            func:SetOpacity(clearOpacity[1], "BG_Clear.png")
            func:SetOpacity(clearOpacity[1], "Cloud_Clear.png")
            func:SetOpacity(clearOpacity[1], "Note_Clear.png")
            func:DrawRectGraph(0, 0, bgScrollX, 0, 1920, 288, "BG_Clear.png")
            func:DrawRectGraph(0, 0, noteScrollX, 0, 1920, 288, "Note_Clear.png")
            func:DrawRectGraph(0, 0, cloudScrollX, 0, 1920, 288, "Cloud_Clear.png")
        end

        if clearOpacity[2] < 255 then
            func:DrawRectGraph(0, 804, bgScrollX, 0, 1920, 288, "BG_Blue.png")
            func:DrawRectGraph(0, 804, cloudScrollX, 0, 1920, 288, "Cloud_Blue.png")
            func:DrawRectGraph(0, 804, noteScrollX, 0, 1920, 288, "Note_Blue.png")
        end
        if clearOpacity[2] > 0 then
            func:SetOpacity(clearOpacity[2], "BG_Clear.png")
            func:SetOpacity(clearOpacity[2], "Cloud_Clear.png")
            func:SetOpacity(clearOpacity[2], "Note_Clear.png")
            func:DrawRectGraph(0, 804, bgScrollX, 0, 1920, 288, "BG_Clear.png")
            func:DrawRectGraph(0, 804, noteScrollX, 0, 1920, 288, "Note_Clear.png")
            func:DrawRectGraph(0, 804, cloudScrollX, 0, 1920, 288, "Cloud_Clear.png")
        end
    elseif playerCount == 3 then
        func:DrawRectGraph(0, 804, bgScrollX, 0, 1920, 288, "BG_Clear.png")
        func:DrawRectGraph(0, 804, noteScrollX, 0, 1920, 288, "Note_Clear.png")
        func:DrawRectGraph(0, 804, cloudScrollX, 0, 1920, 288, "Cloud_Clear.png")
    elseif playerCount == 4 then
        func:DrawRectGraph(0, 0, bgScrollX, 0, 1920, 1080, "BG_Clear.png")
    end
end
