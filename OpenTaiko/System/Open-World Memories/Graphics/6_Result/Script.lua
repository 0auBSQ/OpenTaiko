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

-- Resolution Stuff
local bg_width = 1920
local bg_height = 1080
local bg_width_half = bg_width / 2
local nokonX = 1212
local nokonY = 582

-- Init Checks
local bg_style = 0
local nokon_style = 0

-- Textures
local skyFail = "Background/Sky_Fail.png"
local rain1 = "Background/Fail/Rain_1.png"
local rain2 = "Background/Fail/Rain_2.png"
local storm1 = "Background/Fail/Storm_1.png"
local storm2 = "Background/Fail/Storm_2.png"
local storm3 = "Background/Fail/Storm_3.png"
local storm4 = "Background/Fail/Storm_4.png"

local confetti1 = "Background/Clear/Confetti_1.png"
local confetti2 = "Background/Clear/Confetti_2.png"

local particle1 = "Background/Particle_1.png"
local particle2 = "Background/Particle_2.png"
local drop = "Background/Drop.png"

local sky1P = "Background/Sky_1P.png"
local sky2P = "Background/Sky_2P.png"
local sky4P = "Background/Sky_4P.png"
local clouds1P = "Background/Clouds_1P.png"
local clouds2P = "Background/Clouds_2P.png"
local clouds4P = "Background/Clouds_4P.png"
local cloudsSmall1P = "Background/Clouds_Small_1P.png"
local cloudsSmall2P = "Background/Clouds_Small_2P.png"
local cloudsSmall4P = "Background/Clouds_Small_4P.png"
local frame1P = "Background/Frame_1P.png"
local frame2P = "Background/Frame_2P.png"

local nokonTail = "Background/Nokon/Tail.png"
local nokonFrames = {"Background/Nokon/Idle/en/","Background/Nokon/Result_Bad/en/","Background/Nokon/Result_Good/en/","Background/Nokon/Result_Great/en/"}
local nokonFps = 14

-- Particle Arrays
local red = {255,122,162}
local yellow = {255,224,122}
local green = {179,255,202}
local blue = {122,220,255}

local confetti1XOffset = {190,380,570,760,950,1140,1330,1520}
local confetti1YOffset = {720,165,989,455,699,1,345,960}
local confetti1Scale = {1,0.9,0.6,0.9,0.5,0.8,0.4,0.7}
local confetti1RotOffset = {67,300,40,90,150,0,88,888}
local confetti1SpeedOffset = {1,1.23,-0.876,1.25,-1.06,1.43,1.123,-0.888}
local confetti1Color = {red,yellow,blue,green,yellow,green,red,blue}

local confetti2XOffset = {0,320,640,960,1280,1600,1920}
local confetti2YOffset = {280,500,1,120,600,360,888}
local confetti2Scale = {0.5,0.67,0.4,0.6,0.5,0.66,0.888}
local confetti2RotOffset = {256,0,150,80,300,100,88}
local confetti2SpeedOffset = {1.57,-1.2,0.75,0.88,1.1,0.81,-0.888}
local confetti2Color = {blue,green,yellow,red,yellow,green,blue}

local particle1XOffset = {150,573,888,1000,1400,1700}
local particle1YOffset = {600,240,980,500,100,888}
local particle1Scale = {0.8,0.88,0.6,0.9,0.7,0.888}
local particle1RotOffset = {200,500,69,400,800,420}
local particle1SpeedOffset = {1.1,-1.6,1.2,0.8,1.0,-0.888}

local particle2XOffset = {400,700,1200,1600,1850}
local particle2YOffset = {888,420,700,246,573}
local particle2Scale = {0.75,0.81,0.8,0.6,0.7}
local particle2RotOffset = {40,120,200,88,270}
local particle2SpeedOffset = {-1.1,0.9,-1.3,-0.81,1.2}

local dropX = {200,1700,1000}
local dropY = {300,400,900}
local dropAppear = {3000,6000,9000}

-- Counters
local commonCounter = 0
local confettiCounter = 0
local clearStatusInCounter = 0
local cloudMoveCounter = 0
local cloudSmallMoveCounter = 0
local rain1XMoveCounter = 0
local rain1YMoveCounter = 0
local rain2XMoveCounter = 0
local rain2YMoveCounter = 0
local storm1MoveCounter = 0
local storm2MoveCounter = 0
local storm3MoveCounter = 0
local storm4MoveCounter = 0
local nokonCounter = 0
local nokonResultCounter = 0

local gaugeFactor = 0
local clearFinishValue = 0

function drawConfetti1(xOffset, yOffset, scale, rotOffset, speedOffset, color)
    func:SetScale(scale * math.abs(math.sin((confettiCounter * speedOffset / 108))), scale, confetti1)
    func:SetRotation((confettiCounter * speedOffset) + rotOffset, confetti1)
    func:SetColor(color[1] / 255, color[2] / 255, color[3] / 255, confetti1)

    local y = ((confettiCounter + yOffset) % 1400) - 200

    func:DrawGraphCenter(xOffset, y, confetti1)
end

function drawConfetti2(xOffset, yOffset, scale, rotOffset, speedOffset, color)
    func:SetScale(scale * math.abs(math.sin((confettiCounter * speedOffset / 108))), scale, confetti2)
    func:SetRotation((confettiCounter * speedOffset) + rotOffset, confetti2)
    func:SetColor(color[1] / 255, color[2] / 255, color[3] / 255, confetti2)

    local y = (((confettiCounter * 0.7) + yOffset) % 1400) - 200

    func:DrawGraphCenter(xOffset, y, confetti2)
end

function drawParticle1(xOffset, yOffset, scale, rotOffset, speedOffset)
    --func:SetScale(scale * math.abs(math.sin((confettiCounter * speedOffset / 108))), scale, particle1)
    func:SetScale(scale, scale, particle1)
    func:SetRotation((confettiCounter * speedOffset) + rotOffset, particle1)

    local y = (((-confettiCounter * 0.7) + yOffset) % -1400) + 1200

    func:DrawGraphCenter(xOffset, y, particle1)
end

function drawParticle2(xOffset, yOffset, scale, rotOffset, speedOffset)
    --func:SetScale(scale * math.abs(math.sin((confettiCounter * speedOffset / 108))), scale, particle2)
    func:SetScale(scale, scale, particle2)
    func:SetRotation((confettiCounter * speedOffset) + rotOffset, particle2)

    local y = (((-confettiCounter * 0.6) + yOffset) % -1400) + 1200

    func:DrawGraphCenter(xOffset, y, particle2)
end

function drawFail(x, y, rect_x, rect_y, rect_width, rect_height)
    func:DrawRectGraph(x, y, rect_x, rect_y, rect_width, rect_height, skyFail)
    func:DrawRectGraph(x, y, rect_x + rain1XMoveCounter, rect_y - rain1YMoveCounter, rect_width, rect_height, rain1)
    func:DrawRectGraph(x, y, rect_x + rain2XMoveCounter, rect_y - rain2YMoveCounter, rect_width, rect_height, rain2)
    func:DrawRectGraph(x, y, rect_x + storm4MoveCounter, rect_y, rect_width, rect_height, storm4)
    func:DrawRectGraph(x, y, rect_x + storm3MoveCounter, rect_y, rect_width, rect_height, storm3)
    func:DrawRectGraph(x, y, rect_x + storm2MoveCounter, rect_y, rect_width, rect_height, storm2)
    func:DrawRectGraph(x, y, rect_x + storm1MoveCounter, rect_y, rect_width, rect_height, storm1)
end

function drawClear()
    for i = 1, 7 do
      drawConfetti2(confetti2XOffset[i], confetti2YOffset[i], confetti2Scale[i], confetti2RotOffset[i], confetti2SpeedOffset[i], confetti2Color[i])
    end
    for i = 1, 8 do
      drawConfetti1(confetti1XOffset[i], confetti1YOffset[i], confetti1Scale[i], confetti1RotOffset[i], confetti1SpeedOffset[i], confetti1Color[i])
    end
end

function drawBG(x, y, rect_x, rect_y, rect_width, rect_height, playerId)
    local idAssets = {sky1P, clouds1P, cloudsSmall1P, frame1P}

    if playerId == 2 then
      idAssets = {sky2P, clouds2P, cloudsSmall2P, frame2P}
    elseif playerId >= 3 then
      idAssets = {sky4P, clouds4P, cloudsSmall4P}
    end

    func:DrawRectGraph(x, y, rect_x, rect_y, rect_width, rect_height, idAssets[1])
    func:DrawRectGraph(x, y, rect_x + cloudMoveCounter, rect_y, rect_width, rect_height, idAssets[2])
    func:DrawRectGraph(x, y, rect_x + cloudSmallMoveCounter, rect_y, rect_width, rect_height, idAssets[3])
    if playerId == 1 or playerId == 2 then
      func:DrawRectGraph(x, y, rect_x, rect_y, rect_width, rect_height, idAssets[4])
    end
end

function drawNokon()
    if nokonResultCounter == 0 then
      func:DrawGraph(nokonX, nokonY, nokonFrames[1]..tostring(math.floor(nokonCounter) % 6)..".png")
    -- All Players Clear
    elseif nokon_style == 3 then
      func:SetRotation(math.sin(nokonResultCounter / (nokonFps / 4)) * 10, nokonTail)
      func:DrawGraphCenter(1776, 1027, nokonTail)
      if math.floor(nokonResultCounter) % 28 == 0 then
        func:DrawGraph(nokonX, nokonY, nokonFrames[4].."0.png")
      else
        func:DrawGraph(nokonX, nokonY, nokonFrames[4].."1.png")
      end
    -- Two or More Players Clear
    elseif nokon_style == 2 then
      func:SetRotation(math.sin(nokonResultCounter / (nokonFps / 4)) * 10, nokonTail)
      func:DrawGraphCenter(1776, 1027, nokonTail)
      if math.floor(nokonResultCounter) % 28 == 0 then
        func:DrawGraph(nokonX, nokonY, nokonFrames[3].."0.png")
      else
        func:DrawGraph(nokonX, nokonY, nokonFrames[3].."1.png")
      end
    -- One or Zero Players Clear
    elseif nokon_style == 1 then
      func:DrawGraph(nokonX, nokonY, nokonFrames[2]..tostring(math.min(math.floor(nokonResultCounter),4))..".png")
    end
end

function skipAnime()
    commonCounter = clearFinishValue
end

function clearIn(player)
end

function clearOut(player)
end

function init()

    -- Load assets based on Player Count
    if playerCount == 5 then
      func:AddGraph(sky4P)
      func:AddGraph(clouds4P)
      func:AddGraph(cloudsSmall4P)
      bg_style = 5
    elseif playerCount == 4 then
      func:AddGraph(sky4P)
      func:AddGraph(clouds4P)
      func:AddGraph(cloudsSmall4P)
      bg_style = 4
    elseif playerCount == 3 then
      func:AddGraph(sky4P)
      func:AddGraph(clouds4P)
      func:AddGraph(cloudsSmall4P)
      bg_style = 3
    elseif playerCount == 2 then
      func:AddGraph(sky1P)
      func:AddGraph(clouds1P)
      func:AddGraph(cloudsSmall1P)
      func:AddGraph(frame1P)
      func:AddGraph(sky2P)
      func:AddGraph(clouds2P)
      func:AddGraph(cloudsSmall2P)
      func:AddGraph(frame2P)
      bg_style = 2
    elseif playerCount == 1 and p1IsBlue then
      func:AddGraph(sky2P)
      func:AddGraph(clouds2P)
      func:AddGraph(cloudsSmall2P)
      func:AddGraph(frame2P)
      bg_style = 1
    else
      func:AddGraph(sky1P)
      func:AddGraph(clouds1P)
      func:AddGraph(cloudsSmall1P)
      func:AddGraph(frame1P)
      bg_style = 0
    end

    -- Clear Assets
    func:AddGraph(confetti1)
    func:AddGraph(confetti2)

    -- Fail Assets
    func:AddGraph(skyFail)
    func:AddGraph(rain1)
    func:AddGraph(rain2)
    func:AddGraph(storm1)
    func:AddGraph(storm2)
    func:AddGraph(storm3)
    func:AddGraph(storm4)

    -- Nokon Assets (Only 3P or 5P)
    if bg_style == 3 or bg_style == 5 then

      if lang == "ja" then
        nokonFrames = {"Background/Nokon/Idle/ja/","Background/Nokon/Result_Bad/ja/","Background/Nokon/Result_Good/ja/","Background/Nokon/Result_Great/ja/"}
      elseif lang == "zh" then
        nokonFrames = {"Background/Nokon/Idle/zh/","Background/Nokon/Result_Bad/zh/","Background/Nokon/Result_Good/zh/","Background/Nokon/Result_Great/zh/"}
      elseif lang == "fr" then
        nokonFrames = {"Background/Nokon/Idle/fr/","Background/Nokon/Result_Bad/fr/","Background/Nokon/Result_Good/fr/","Background/Nokon/Result_Great/fr/"}
      end

      local clearCheck = 0
      for i = 0, playerCount - 1 do
        if isClear[i] then
          clearCheck = clearCheck + 1
        end
      end
      -- All Players Clear
      if clearCheck == playerCount then
        for i = 0, 1 do
          func:AddGraph(nokonFrames[4]..tostring(i)..".png")
        end
        nokon_style = 3
      -- At Least Two Players Clear
      elseif clearCheck >= 2 then
        for i = 0, 4 do
          func:AddGraph(nokonFrames[3]..tostring(i)..".png")
        end
        nokon_style = 2
      -- Only One or Zero Players Clear
      else
        for i = 0, 4 do
          func:AddGraph(nokonFrames[2]..tostring(i)..".png")
        end
        nokon_style = 1
      end

      for i = 0, 5 do
        func:AddGraph(nokonFrames[1]..tostring(i)..".png")
      end

      func:AddGraph(nokonTail)
    end

    -- Other Assets
    func:AddGraph(particle1)
    func:AddGraph(particle2)
    func:AddGraph(drop)

    -- Fallback Assets
    func:AddGraph("Background.png")

    --func:AddGraph("Background_0.png")
    --func:AddGraph("Background_1.png")
    --func:AddGraph("Background_2.png")
    --func:AddGraph("Background_3.png")
    --func:AddGraph("Background_4.png")
    --func:AddGraph("Background_5.png")

    commonCounter = 0
    clearStatusInCounter = 0
    cloudMoveCounter = 0
    cloudSmallMoveCounter = 0
    rain1XMoveCounter = 0
    rain1YMoveCounter = 0
    rain2XMoveCounter = 0
    rain2YMoveCounter = 0
    storm1MoveCounter = 0
    storm2MoveCounter = 0
    storm3MoveCounter = 0
    storm4MoveCounter = 0

    gaugeFactor = math.max(gauge[0], math.max(gauge[1], math.max(gauge[2], math.max(gauge[3], gauge[4])))) / 2
    clearFinishValue = 10275 + (66 * gaugeFactor)

end

function update()
    commonCounter = commonCounter + (deltaTime * 1000)
    confettiCounter = confettiCounter + (deltaTime * 108)

    cloudMoveCounter = cloudMoveCounter + (deltaTime * 50)
    cloudSmallMoveCounter = cloudMoveCounter * 1.6

    rain1XMoveCounter = rain1XMoveCounter + (deltaTime * 550)
    rain1YMoveCounter = rain1XMoveCounter * 1.5
    rain2XMoveCounter = rain2XMoveCounter + (deltaTime * 405)
    rain2YMoveCounter = rain2XMoveCounter * 1.5

    storm1MoveCounter = storm1MoveCounter + (deltaTime * 50)
    storm2MoveCounter = storm1MoveCounter * 0.9
    storm3MoveCounter = storm1MoveCounter * 0.8
    storm4MoveCounter = storm1MoveCounter * 0.7

    nokonCounter = nokonCounter + (deltaTime * nokonFps)

    if commonCounter >= clearFinishValue then

      clearStatusInCounter = clearStatusInCounter + (deltaTime * 510)
      if clearStatusInCounter > 255 then
        clearStatusInCounter = 255
      end
      nokonResultCounter = nokonResultCounter + (deltaTime * nokonFps)

    end
end

function draw()
    func:DrawGraph(0, 0, "Background.png")

    func:SetOpacity(clearStatusInCounter, skyFail)
    func:SetOpacity(clearStatusInCounter, rain1)
    func:SetOpacity(clearStatusInCounter, rain2)
    func:SetOpacity(clearStatusInCounter, storm1)
    func:SetOpacity(clearStatusInCounter, storm2)
    func:SetOpacity(clearStatusInCounter, storm3)
    func:SetOpacity(clearStatusInCounter, storm4)
    func:SetOpacity(clearStatusInCounter, confetti1)
    func:SetOpacity(clearStatusInCounter, confetti2)

    -- 1 Player Results
    if bg_style == 0 then

      drawBG(0, 0, 0, 0, bg_width, bg_height, 1)

      -- Draw on Clear or Fail
      if isClear[0] then
        drawClear()
      else
        drawFail(0, 0, 0, 0, bg_width, bg_height)
      end

    -- 1 Player Results (Right/Blue Side)
    elseif bg_style == 1 then

      drawBG(0, 0, 0, 0, bg_width, bg_height, 2)
      --func:DrawRectGraph(0, 0, 0, 0, bg_width, bg_height, sky2P)
      --func:DrawRectGraph(0, 0, 0 + cloudMoveCounter, 0, bg_width, bg_height, clouds2P)
      --func:DrawRectGraph(0, 0, 0 + cloudSmallMoveCounter, 0, bg_width, bg_height, cloudsSmall2P)
      --func:DrawRectGraph(0, 0, 0, 0, bg_width, bg_height, frame2P)

      -- Draw on Clear or Fail
      if isClear[0] then
        drawClear()
      else
        drawFail(0, 0, 0, 0, bg_width, bg_height)
      end

    -- 2 Players Results
    elseif bg_style == 2 then
      -- Player 1
      drawBG(0, 0, 0, 0, bg_width_half, bg_height, 1)
      --func:DrawRectGraph(0, 0, 0, 0, bg_width_half, bg_height, sky1P)
      --func:DrawRectGraph(0, 0, 0 + cloudMoveCounter, 0, bg_width_half, bg_height, clouds1P)
      --func:DrawRectGraph(0, 0, 0 + cloudSmallMoveCounter, 0, bg_width_half, bg_height, cloudsSmall1P)
      --func:DrawRectGraph(0, 0, 0, 0, bg_width_half, bg_height, frame1P)
      -- Player 2
      drawBG(bg_width_half, 0, bg_width_half, 0, bg_width_half, bg_height, 2)
      --func:DrawRectGraph(bg_width_half, 0, bg_width_half, 0, bg_width_half, bg_height, sky2P)
      --func:DrawRectGraph(bg_width_half, 0, bg_width_half + cloudMoveCounter, 0, bg_width_half, bg_height, clouds2P)
      --func:DrawRectGraph(bg_width_half, 0, bg_width_half + cloudSmallMoveCounter, 0, bg_width_half, bg_height, cloudsSmall2P)
      --func:DrawRectGraph(bg_width_half, 0, bg_width_half, 0, bg_width_half, bg_height, frame2P)

      -- Draw Clear, Fail will draw on top if the player fails
      drawClear()

      if isClear[0] == false then
        drawFail(0, 0, 0, 0, bg_width_half, bg_height)
      end
      if isClear[1] == false then
        drawFail(bg_width_half, 0, bg_width_half, 0, bg_width_half, bg_height)
      end

    -- 3 Players Results
    elseif bg_style == 3 then
      drawBG(0, 0, 0, 0, bg_width, bg_height, 3)
      if isClear[0] and isClear[1] and isClear[2] then
        drawClear()
      end

    -- 4 Players Results
    elseif bg_style == 4 then
      drawBG(0, 0, 0, 0, bg_width, bg_height, 4)
      if isClear[0] and isClear[1] and isClear[2] and isClear[3] then
        drawClear()
      end

    -- 5 Players Results
    elseif bg_style == 5 then
      drawBG(0, 0, 0, 0, bg_width, bg_height, 5)
      if isClear[0] and isClear[1] and isClear[2] and isClear[3] and isClear[4] then
        drawClear()
      end

    end
    -- Stop player conditions here

    -- Extra Effects
    for i = 1, 3 do
      local work = math.min(1000, math.abs(((commonCounter - dropAppear[i]) % 10000) - 1000))
      local workscale = math.min(2000, math.abs(((commonCounter - dropAppear[i]) % 10000) - 2000))
      func:SetOpacity(255 - (work * 0.255), drop)
      func:SetScale(1 - (workscale / 4000), 1 - (workscale / 4000), drop)
      func:DrawGraphCenter(dropX[i], dropY[i], drop)
    end
    for i = 1, 6 do
      drawParticle1(particle1XOffset[i], particle1YOffset[i], particle1Scale[i], particle1RotOffset[i], particle1SpeedOffset[i])
    end
    for i = 1, 5 do
      drawParticle2(particle2XOffset[i], particle2YOffset[i], particle2Scale[i], particle2RotOffset[i], particle2SpeedOffset[i])
    end

    if bg_style == 3 or bg_style == 5 then
      drawNokon()
    end
end
