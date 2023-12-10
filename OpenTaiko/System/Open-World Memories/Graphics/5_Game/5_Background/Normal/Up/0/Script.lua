--func:DrawRectGraph(x, y, rect_x, rect_y, rect_width, rect_height, filename);
local loopWidth = 1920
local loopHeight = 288

local secondPlayerY = 804

local bgScrollX = 0
local bgScrollY = 0
local dangoCounter = 0
local bgClearFade = { 0, 0 }

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

function clearIn(player)
end

function clearOut(player)
end

function init()
    func:AddGraph("BG_Red.png")
    func:AddGraph("BG_Blue.png")
    func:AddGraph("BG_Clear.png")

    func:AddGraph("Dot_Red.png")
    func:AddGraph("Dot_Blue.png")
    func:AddGraph("Dot_Clear.png")

    func:AddGraph("Dango_Red.png")
    func:AddGraph("Dango_Blue.png")
    func:AddGraph("Dango_Clear.png")
end

function update()
    bgScrollX = bgScrollX + (59 * deltaTime)
    bgScrollY = bgScrollY + (14 * deltaTime)
    dangoCounter = dangoCounter + deltaTime

    if dangoCounter > 3 then
      dangoCounter = 0
    end

    -- don't bother if player count is 3 or higher, use clear BG for 3P instead
    if playerCount < 3 then
      updateClearFade()
    end
end


function draw()

    -- dango effects

    moveY = math.sin(math.min(dangoCounter, 1) * math.pi) * 30
    moveY2 = math.sin(math.min(math.max(((dangoCounter - 1) * 4), 0), 1) * math.pi) / 12.0
    moveY3 = 274 * moveY2
    dangoOffset = -moveY + moveY3 - 36

    func:SetScale(1, 1.0 - moveY2, "Dango_Red.png")
    func:SetScale(1, 1.0 - moveY2, "Dango_Blue.png")
    func:SetScale(1, 1.0 - moveY2, "Dango_Clear.png")

    -- draw the stuff
    if playerCount == 1 and p1IsBlue == false then

      func:SetOpacity(bgClearFade[1], "BG_Clear.png")
      func:SetOpacity(bgClearFade[1], "Dot_Clear.png")
      func:SetOpacity(bgClearFade[1], "Dango_Clear.png")

      func:DrawGraph(0, 0, "BG_Red.png")
      func:DrawRectGraph(0, 0, bgScrollX, bgScrollY, loopWidth, loopHeight, "Dot_Red.png")
      func:DrawRectGraph(0, dangoOffset, bgScrollX, 0, loopWidth, loopHeight, "Dango_Red.png")

      func:DrawGraph(0, 0, "BG_Clear.png")
      func:DrawRectGraph(0, 0, bgScrollX, bgScrollY, loopWidth, loopHeight, "Dot_Clear.png")
      func:DrawRectGraph(0, dangoOffset, bgScrollX, 0, loopWidth, loopHeight, "Dango_Clear.png")

    elseif playerCount == 1 and p1IsBlue == true then

      func:SetOpacity(bgClearFade[1], "BG_Clear.png")
      func:SetOpacity(bgClearFade[1], "Dot_Clear.png")
      func:SetOpacity(bgClearFade[1], "Dango_Clear.png")

      func:DrawGraph(0, 0, "BG_Blue.png")
      func:DrawRectGraph(0, 0, bgScrollX, bgScrollY, loopWidth, loopHeight, "Dot_Blue.png")
      func:DrawRectGraph(0, dangoOffset, bgScrollX, 0, loopWidth, loopHeight, "Dango_Blue.png")

      func:DrawGraph(0, 0, "BG_Clear.png")
      func:DrawRectGraph(0, 0, bgScrollX, bgScrollY, loopWidth, loopHeight, "Dot_Clear.png")
      func:DrawRectGraph(0, dangoOffset, bgScrollX, 0, loopWidth, loopHeight, "Dango_Clear.png")

    elseif playerCount == 2 then

      func:DrawGraph(0, 0, "BG_Red.png")
      func:DrawGraph(0, secondPlayerY, "BG_Blue.png")
      func:DrawRectGraph(0, 0, bgScrollX, bgScrollY, loopWidth, loopHeight, "Dot_Red.png")
      func:DrawRectGraph(0, secondPlayerY, bgScrollX, bgScrollY, loopWidth, loopHeight, "Dot_Blue.png")
      func:DrawRectGraph(0, dangoOffset, bgScrollX, 0, loopWidth, loopHeight, "Dango_Red.png")
      func:DrawRectGraph(0, secondPlayerY + dangoOffset, bgScrollX, 0, loopWidth, loopHeight, "Dango_Blue.png")

      for player = 0, 1 do
        func:SetOpacity(bgClearFade[player + 1], "BG_Clear.png")
        func:SetOpacity(bgClearFade[player + 1], "Dot_Clear.png")
        func:SetOpacity(bgClearFade[player + 1], "Dango_Clear.png")

        func:DrawGraph(0, player * secondPlayerY, "BG_Clear.png")
        func:DrawRectGraph(0, player * secondPlayerY, bgScrollX, bgScrollY, loopWidth, loopHeight, "Dot_Clear.png")
        func:DrawRectGraph(0, (player * secondPlayerY) + dangoOffset, bgScrollX, 0, loopWidth, loopHeight, "Dango_Clear.png")
      end

    elseif playerCount == 3 then

      func:DrawRectGraph(0, 0, 0, 0, loopWidth, 1920, "BG_Clear.png")
      func:DrawRectGraph(0, 0, bgScrollX, bgScrollY, loopWidth, 1920, "Dot_Clear.png")
      func:DrawRectGraph(0, secondPlayerY + dangoOffset, bgScrollX, 0, loopWidth, loopHeight, "Dango_Clear.png")

    elseif playerCount == 4 then

      func:DrawRectGraph(0, 0, 0, 0, loopWidth, 1920, "BG_Clear.png")
      func:DrawRectGraph(0, 0, bgScrollX, bgScrollY, loopWidth, 1920, "Dot_Clear.png")

    end
end
