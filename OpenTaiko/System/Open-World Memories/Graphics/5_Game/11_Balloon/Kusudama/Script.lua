local kusuState = 0

local animeValue = 0
local effectValue = 0
local fadeValue = 0

local centerPos = { 960, 540 }

local kusudamaPos = { 663, -166 }

local starCenterPos = { 964, 391.5 }
local flashCenterPos = { 975, 436.5 }
local flash2CenterPos = { 967, 370.5 }
local flareCenterPos = { 1014, 413 }

local petalCount = 21

local petals_center_pos_x = { 754, 1187.5, 1194.5, 1172, 758, 1171, 748.5, 753.5, 776.5, 1175, 1008, 913.5, 923, 1017.5, 780, 1152, 856, 1073, 848.5, 1085.5, 965.5}
local petals_center_pos_y = { 518.5, 466, 369.5, 260.5, 260.5, 465.5, 469, 352, 429.5, 526, 626, 623, 158, 161, 370, 370, 231, 228, 551.5, 547.5, 397 }

local petals_x = { 1, 108, 242, 350, 463, 576, 1, 135, 267, 333, 434, 509, 601, 676, 1, 176, 351, 550, 1, 199, 397 }
local petals_y = { 1, 1, 1, 1, 1, 1, 111, 111, 111, 111, 111, 111, 111, 111, 237, 237, 237, 237, 440, 440, 440 }
local petals_rect_x = { 106, 133, 107, 112, 112, 108, 133, 131, 65, 100, 74, 91, 74, 91, 174, 174, 198, 198, 197, 197, 267}
local petals_rect_y = { 109, 88, 65, 77, 77, 77, 88, 90, 101, 72, 114, 116, 114, 116, 196, 196, 202, 202, 199, 199, 288 }
local petals_direction = { -0.8, 1.1, 0.7, 0.6, -0.7, 0.5, -0.6, -0.8, -0.9, 1.1, 0.2, -0.15, -0.2, 0.15, -1.3, 1.2, -0.8, 0.7, -0.6, 0.55, 0.05 }
local petals_rotation = { -0.9, 0.8, 0.7, 0.9, -0.8, 0.6, -0.5, -0.7, -1.1, 1.2, 1.2, -1.1, -1.2, 1.1, -0.8, 0.7, -0.6, 0.55, -0.45, 0.5, 0.2 }
local petals_fall = { 1.1, 1.2, 0.9, 0.7, 0.7, 0.5, 0.6, 0.9, 0.5, 1.1, 1.5, 1.4, 1.5, 1.4, 0.7, 0.8, 0.5, 0.6, 1.2, 1.3, 0.6 }


function kusuIn()
    kusuState = 1
    animeValue = 0
end

function kusuBroke()
    kusuState = 3
    animeValue = 0
end

function kusuMiss()
    kusuState = 4
    animeValue = 0
end

function init()
    func:AddGraph("Kusudama.png")
    func:AddGraph("Kusudama_Fail.png")
    func:AddGraph("Kusudama_Back.png")
    func:AddGraph("Petals.png")
    func:AddGraph("Star.png")
    func:AddGraph("Star_Fail.png")
    func:AddGraph("Flash.png")
    func:AddGraph("Flash2.png")
    func:AddGraph("Flare.png")
    func:AddGraph("Effect_0.png")
    func:SetBlendMode("Add", "Effect_0.png")
    func:AddGraph("Effect_1.png")
    func:SetBlendMode("Add", "Effect_1.png")
    func:AddGraph("Effect_2.png")
    func:SetBlendMode("Add", "Effect_2.png")
    func:AddGraph("Text.png")

    kusuState = 0
    fadeValue = 0
end

function update()

    if kusuState == 1 then
      animeValue = animeValue + (deltaTime * 2)
    elseif kusuState == 2 then
      animeValue = animeValue + deltaTime
    elseif kusuState == 3 then
      animeValue = animeValue + (deltaTime / 1.5)
    elseif kusuState == 4 then
      animeValue = animeValue + (deltaTime * 2)
    end

    if animeValue > 1 then
      if kusuState == 1 then
        kusuState = 2
        animeValue = 0
      elseif kusuState == 3 or kusuState == 4 then
        kusuState = 0
        animeValue = 0
      end
    end

    if kusuState == 0 or kusuState > 2 then
      fadeValue = fadeValue - (deltaTime * 2)
    else
      fadeValue = fadeValue + (deltaTime * 2)
    end

    if fadeValue > 1 then
      fadeValue = 1
    elseif fadeValue < 0 then
      fadeValue = 0
    end

    effectValue = effectValue + (deltaTime * 6)

end

function draw()

    func:SetOpacity(fadeValue * 255, "Kusudama_Back.png")
    func:DrawGraph(0, 0, "Kusudama_Back.png")

    -- Into Kusudama
    if kusuState == 1 then

      -- EaseOutBack
      local c1 = 1.70158
      local c3 = c1 + 1
      local updateValue = 1 + c3 * (animeValue - 1)^3 + c1 * (animeValue - 1)^2

      func:DrawGraph(kusudamaPos[1], kusudamaPos[2] - (500 * (1 - updateValue)), "Kusudama.png")
      func:DrawGraphCenter(starCenterPos[1], starCenterPos[2] - (500 * (1 - updateValue)), "Star.png")

    -- Kusudama
    elseif kusuState == 2 then

      func:DrawGraph(kusudamaPos[1], kusudamaPos[2], "Kusudama.png")
      func:DrawGraphCenter(starCenterPos[1], starCenterPos[2], "Star.png")

      func:SetOpacity(128 - (128 * ((math.cos(animeValue * 8) + 1)/2)), "Flash.png")
      func:SetScale(0.5, 0.5, "Flash.png")

      func:DrawGraphCenter(flashCenterPos[1], flashCenterPos[2], "Flash.png")

    -- Kusudama Broken
    elseif kusuState == 3 then

      func:SetScale(0.9 + (0.45 * animeValue), 0.9 + (0.45 * animeValue), "Flare.png")
      func:SetOpacity(math.max(0, 255 - (384 * animeValue)), "Flare.png")

      func:DrawGraphCenter(flareCenterPos[1], flareCenterPos[2], "Flare.png")

      for i=1,petalCount do
        -- EaseOutCubic
        local xCurve = (1 - (1 - animeValue)^3) * petals_direction[i]
        -- EaseInSine Edited
        local yCurve = (1 - math.cos((animeValue * math.pi) / 2) + (animeValue * -0.6)) * petals_fall[i]

        func:SetRotation(270 * petals_rotation[i] * animeValue, "Petals.png")
        func:SetOpacity(math.min(512 - (512 * animeValue), 255), "Petals.png")

        func:DrawGraphRectCenter(petals_center_pos_x[i] + (500 * xCurve), petals_center_pos_y[i] + (600 * yCurve), petals_x[i], petals_y[i], petals_rect_x[i], petals_rect_y[i], "Petals.png")
      end

      local fscale = math.min(1, 0.5 + (animeValue * 2))
      local fopacity = math.max(0, 255 - (512 * animeValue))

      func:SetRotation(480 * animeValue, "Flash2.png")
      func:SetOpacity(fopacity, "Flash2.png")
      func:SetScale(fscale, fscale, "Flash2.png")

      func:SetOpacity(fopacity, "Flash.png")
      func:SetScale(fscale, fscale, "Flash.png")

      func:DrawGraphCenter(flashCenterPos[1], flashCenterPos[2], "Flash.png")
      func:DrawGraphCenter(flash2CenterPos[1], flash2CenterPos[2], "Flash2.png")

    -- Kusudama Failed
    elseif kusuState == 4 then

      -- EaseInBack
      local c1 = 1.70158
      local c3 = c1 + 1
      local updateValue = c3 * animeValue * animeValue * animeValue - c1 * animeValue * animeValue

      func:SetOpacity(math.min(512 * animeValue, 255), "Kusudama_Fail.png")
      func:SetOpacity(math.min(512 * animeValue, 255), "Star_Fail.png")

      func:DrawGraph(kusudamaPos[1], kusudamaPos[2] - (500 * updateValue), "Kusudama.png")
      func:DrawGraph(kusudamaPos[1], kusudamaPos[2] - (500 * updateValue), "Kusudama_Fail.png")
      func:DrawGraphCenter(starCenterPos[1], starCenterPos[2] - (500 * updateValue), "Star.png")
      func:DrawGraphCenter(starCenterPos[1], starCenterPos[2] - (500 * updateValue), "Star_Fail.png")

    end

    if kusuState == 1 or kusuState == 2 then
      func:DrawGraphCenter(960, 600, "Text.png")
    end

    if kusuState ~= 0 then
      func:SetOpacity(fadeValue * 255, "Effect_" .. tostring(math.floor(effectValue) % 3) .. ".png")
      func:DrawGraph(0, 0, "Effect_" .. tostring(math.floor(effectValue) % 3) .. ".png" )
    end

end
