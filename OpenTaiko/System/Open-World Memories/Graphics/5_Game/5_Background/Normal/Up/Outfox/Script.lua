-- Please note that the Project OutFox branding or graphics included are not permitted
-- for use outside of OpenTaiko without permission from the Project OutFox Developers.

-- The graphics are included as part of the OpenTaiko Team Collaboration to promote
-- the use of Taiko and other games to a wider audience.

local loopWidth = 1920
local loopHeight = 288
local simpleHeight = 270
local pos2PY = 804

local bgScrollX = 0
local bpmTime = 0
local totalTime = -2
local bgClearFade = { 0, 0 }

local foxPosBlueStart = { 82.4, 136.6, 338, 56, 564, 111, 820.7, 139.6, 1014, 54, 1238, 54, 1445, 52, 1688, 72, 1915, 66}
local foxPosBlueEnd = { 180, 39, 338, -13, 564, 42, 723.1, 42, 1014, -15, 1238, -15, 1445, -17, 1688, 3, 1915, -3 }
local foxPosRedStart = { 138.7, 136.6, 338, 56, 564, 111, 767.4, 139.6, 1014, 54, 1238, 54, 1479, 52, 1688, 72, 1915, 66 }
local foxPosRedEnd = { 41.1, 39, 338, -13, 564, 42, 865, 42, 1014, -15, 1238, -15, 1479, -17, 1688, 3, 1915, -3 }
local foxRotBlue = { -45, 0, 0, 45, 0, 0, 0, 0, 0 }
local foxRotRed = { 45, 0, 0, -45, 0, 0, 0, 0, 0 }

local noteSize = 225

function updateClearFade()
    for player = 0, playerCount - 1 do
        if isClear[player] then
            bgClearFade[player + 1] = bgClearFade[player + 1] + (2000 * deltaTime)
        else
            bgClearFade[player + 1] = bgClearFade[player + 1] - (2000 * deltaTime)
        end

        if bgClearFade[player + 1] > 255 then
            bgClearFade[player + 1] = 255
        end
        if bgClearFade[player + 1] < 0 then
            bgClearFade[player + 1] = 0
        end
    end
end

function drawTinyFox(x_start, x_end, y_start, y_end, rot, progress, useAlt, lazyArrowFix)

    local foxRect = { 0, 138, 128, 138, 0, 0 }
    local arrowFix = 0
    if useAlt then
        foxRect = { 256, 138, 384, 138, 128, 0 }
    end
    -- Due to a lapse in my judgement, a portion of the fox's body shows where it shouldn't on a couple of notes
    -- Rather than do a bunch of math to account for this, lemme just do this instead lol
    if lazyArrowFix then
        arrowFix = 0.7
    end
    func:SetRotation(rot, "tinyfox.png")

    if progress >= 1.66 then
        func:DrawGraphRectCenter(x_start - ((x_start - x_end)), y_start - ((y_start - y_end)), 512, 0, 128, 138, "tinyfox.png") --Tail
        func:DrawGraphRectCenter(x_start - ((x_start - x_end)), y_start - ((y_start - y_end)), foxRect[5], foxRect[6], 128, 138, "tinyfox.png") --Body
    elseif progress >= 1.33 then
        func:DrawGraphRectCenter(x_start - ((x_start - x_end)), y_start - ((y_start - y_end)), 384, 0, 128, 138, "tinyfox.png") --Tail
        func:DrawGraphRectCenter(x_start - ((x_start - x_end)), y_start - ((y_start - y_end)), foxRect[5], foxRect[6], 128, 138, "tinyfox.png") --Body
    elseif progress >= 1 then
        func:DrawGraphRectCenter(x_start - ((x_start - x_end)), y_start - ((y_start - y_end)), 256, 0, 128, 138, "tinyfox.png") --Tail
        func:DrawGraphRectCenter(x_start - ((x_start - x_end)), y_start - ((y_start - y_end)), foxRect[5], foxRect[6], 128, 138, "tinyfox.png") --Body
    elseif progress >= 0.85 then
        func:DrawGraphRectCenter(x_start - ((x_start - x_end) * progress), y_start - ((y_start - y_end) * progress), foxRect[3], foxRect[4], 128, 138 * progress, "tinyfox.png")
    elseif progress >= arrowFix then
        func:DrawGraphRectCenter(x_start - ((x_start - x_end) * progress), y_start - ((y_start - y_end) * progress), foxRect[1], foxRect[2], 128, 138 * progress, "tinyfox.png")
    end
end

function drawOFNotes(y_offset, drawBlue)
    local allNotesOffset = 2025
    local pump_offsetX = math.floor((bpmTime * 6) % 6) % 3
    local pump_offsetY = math.floor(((bpmTime * 6) % 6) / 3)
    local calculated_y_offset = 27.5 + (math.sin(bpmTime) * 15) + y_offset

    local notePath = "notes_red.png"
    local pumpPath = "pump_red.png"
    local bemuPath = "bemu_red.png"
    if drawBlue == true then
      notePath = "notes_blue.png"
      pumpPath = "pump_blue.png"
      bemuPath = "bemu_blue.png"
    end

    local x_offset = (bgScrollX * 1.3)
    for i = -1, 0 do
        local calculated_x_offset = (x_offset % allNotesOffset) + (allNotesOffset * i)
        local progress = math.min(math.max((totalTime % 8) * 1.25, 0), 1) + math.min(math.max(((totalTime % 8) - 3) * 3, 0), 1) - math.min(math.max(((totalTime % 8) - 3.33) * 3, 0), 1) + math.min(math.max(((totalTime % 8) - 4) * 3, 0), 1) - math.min(math.max(((totalTime % 8) - 4.33) * 3, 0), 1) - math.min(math.max(((totalTime % 8) - 5.5) * 2, 0), 1)
        local foxSet = math.floor(totalTime / 8) % 3

        if progress < 1 then
            for j = foxSet + 1, 9, 3 do
                local offset = (j-1)*2
                local alt = (j == 3)
                local lazy = (j == 1 or j == 4)
                if drawBlue then
                    drawTinyFox( foxPosBlueStart[offset+1] + calculated_x_offset, foxPosBlueEnd[offset+1] + calculated_x_offset, foxPosBlueStart[offset+2] + calculated_y_offset, foxPosBlueEnd[offset+2] + calculated_y_offset, foxRotBlue[j], progress, alt, lazy)
                else
                    drawTinyFox( foxPosRedStart[offset+1] + calculated_x_offset, foxPosRedEnd[offset+1] + calculated_x_offset, foxPosRedStart[offset+2] + calculated_y_offset, foxPosRedEnd[offset+2] + calculated_y_offset, foxRotRed[j], progress, alt, lazy)
                end
            end
        end

        func:DrawGraph(calculated_x_offset, calculated_y_offset, notePath)

        func:SetRotation(-180 * bpmTime, bemuPath)
        func:DrawRectGraph(225 + calculated_x_offset, calculated_y_offset, noteSize, 0, noteSize, noteSize, bemuPath) -- Bemu

        func:DrawRectGraph(1350 + calculated_x_offset, calculated_y_offset, noteSize * pump_offsetX, noteSize * pump_offsetY, noteSize, noteSize, pumpPath) -- Pump

        if progress >= 1 then
            for j = foxSet + 1, 9, 3 do
                local offset = (j-1)*2
                local alt = (j == 3)
                local lazy = (j == 1 or j == 4)
                if drawBlue then
                    drawTinyFox( foxPosBlueStart[offset+1] + calculated_x_offset, foxPosBlueEnd[offset+1] + calculated_x_offset, foxPosBlueStart[offset+2] + calculated_y_offset, foxPosBlueEnd[offset+2] + calculated_y_offset, foxRotBlue[j], progress, alt, lazy)
                else
                    drawTinyFox( foxPosRedStart[offset+1] + calculated_x_offset, foxPosRedEnd[offset+1] + calculated_x_offset, foxPosRedStart[offset+2] + calculated_y_offset, foxPosRedEnd[offset+2] + calculated_y_offset, foxRotRed[j], progress, false, lazy)
                end
            end
        end

        -- Debug Stuff
        -- func:DrawNum(0,0,progress)
        -- func:DrawNum(0,16,totalTime % 8)
        -- func:DrawNum(0,32,foxSet + 1)
    end
end

function clearIn(player)
end

function clearOut(player)
end

function init()

    if simplemode then -- SimpleMode Assets

        if (playerCount == 1 and p1IsBlue == false) or playerCount == 2 then
            func:AddGraph("bg_red.png")
            func:AddGraph("notes_red_simple.png")
        end
        if (playerCount == 1 and p1IsBlue == true) or (playerCount > 1 and playerCount < 5) then
            func:AddGraph("bg_blue.png")
            if (playerCount < 4) then
                func:AddGraph("notes_blue_simple.png")
            end
        end
        if playerCount < 3 then
            func:AddGraph("bg_clear.png")
        end
        
    else -- Regular Assets

        if (playerCount == 1 and p1IsBlue == false) or playerCount == 2 then
            func:AddGraph("bg_red.png")
            func:AddGraph("notes_red.png")
            func:AddGraph("pump_red.png")
            func:AddGraph("bemu_red.png")
            end
        if (playerCount == 1 and p1IsBlue == true) or (playerCount > 1 and playerCount < 5) then
            func:AddGraph("bg_blue.png")
            if (playerCount < 4) then
                func:AddGraph("notes_blue.png")
                func:AddGraph("pump_blue.png")
                func:AddGraph("bemu_blue.png")
            end
        end
        if playerCount < 3 then
            func:AddGraph("bg_clear.png")
        end
        if playerCount < 4 then
            func:AddGraph("tinyfox.png")
        end

    end

end

function update()
    totalTime = totalTime + deltaTime
    bgScrollX = bgScrollX - (59 * deltaTime)
    bpmTime = bpmTime + (deltaTime * (bpm[0] / 120))

    -- Only needed for 1/2 player(s)
    if playerCount < 3 then
      updateClearFade()
    end
end


function draw()

    if simplemode then

        if playerCount == 1 and p1IsBlue == false then

            func:SetOpacity(bgClearFade[1], "bg_clear.png")
            func:DrawRectGraph(0, 0, 0, 0, loopWidth, loopHeight, "bg_red.png")
            func:DrawRectGraph(0, 0, 0, 0, loopWidth, loopHeight, "bg_clear.png")
            func:DrawRectGraph(0, 0, 0, 0, loopWidth, simpleHeight, "notes_red_simple.png")

        elseif playerCount == 1 and p1IsBlue == true then

            func:SetOpacity(bgClearFade[1], "bg_clear.png")
            func:DrawRectGraph(0, 0, 0, 0, loopWidth, loopHeight, "bg_blue.png")
            func:DrawRectGraph(0, 0, 0, 0, loopWidth, loopHeight, "bg_clear.png")
            func:DrawRectGraph(0, 0, 0, 0, loopWidth, simpleHeight, "notes_blue_simple.png")

        elseif playerCount == 2 then

            func:SetOpacity(bgClearFade[1], "bg_clear.png")
            func:DrawRectGraph(0, 0, 0, 0, loopWidth, loopHeight, "bg_red.png")
            func:DrawRectGraph(0, 0, 0, 0, loopWidth, loopHeight, "bg_clear.png")
            func:DrawRectGraph(0, 0, 0, 0, loopWidth, simpleHeight, "notes_red_simple.png")
            
            func:SetOpacity(bgClearFade[2], "bg_clear.png")
            func:DrawRectGraph(0, pos2PY, 0, 0, loopWidth, loopHeight, "bg_blue.png")
            func:DrawRectGraph(0, pos2PY, 0, 0, loopWidth, loopHeight, "bg_clear.png")
            func:DrawRectGraph(0, pos2PY, 0, 0, loopWidth, simpleHeight, "notes_blue_simple.png")

        elseif playerCount == 3 then

            func:DrawRectGraph(0, pos2PY, 0, 0, loopWidth, loopHeight, "bg_blue.png")
            func:DrawRectGraph(0, pos2PY, 0, 0, loopWidth, simpleHeight, "notes_blue_simple.png")

        elseif playerCount == 4 then

            func:DrawRectGraph(0, 0, 0, 0, 1920, 1080, "bg_blue.png")

        end   

    else
        if playerCount == 1 and p1IsBlue == false then

            func:SetOpacity(bgClearFade[1], "bg_clear.png")
            func:DrawRectGraph(0, 0, -bgScrollX, 0, loopWidth, loopHeight, "bg_red.png")
            func:DrawRectGraph(0, 0, -bgScrollX, 0, loopWidth, loopHeight, "bg_clear.png")
            drawOFNotes(28, false)

        elseif playerCount == 1 and p1IsBlue == true then

            func:SetOpacity(bgClearFade[1], "bg_clear.png")
            func:DrawRectGraph(0, 0, -bgScrollX, 0, loopWidth, loopHeight, "bg_blue.png")
            func:DrawRectGraph(0, 0, -bgScrollX, 0, loopWidth, loopHeight, "bg_clear.png")
            drawOFNotes(28, true)

        elseif playerCount == 2 then

            func:SetOpacity(bgClearFade[1], "bg_clear.png")
            func:DrawRectGraph(0, 0, -bgScrollX, 0, loopWidth, loopHeight, "bg_red.png")
            func:DrawRectGraph(0, 0, -bgScrollX, 0, loopWidth, loopHeight, "bg_clear.png")
            drawOFNotes(28, false)
            func:SetOpacity(bgClearFade[2], "bg_clear.png")
            func:DrawRectGraph(0, pos2PY, -bgScrollX, 0, loopWidth, loopHeight, "bg_blue.png")
            func:DrawRectGraph(0, pos2PY, -bgScrollX, 0, loopWidth, loopHeight, "bg_clear.png")
            drawOFNotes(pos2PY + 28, true)

        elseif playerCount == 3 then

            func:DrawRectGraph(0, pos2PY, -bgScrollX, 0, loopWidth, loopHeight, "bg_blue.png")
            drawOFNotes(pos2PY + 28, true)

        elseif playerCount == 4 then

            func:DrawRectGraph(0, 0, -bgScrollX, 0, 1920, 1080, "bg_blue.png")

        end
    end

end
