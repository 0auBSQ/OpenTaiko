---@diagnostic disable: undefined-global  -- TEXTURE/fps injected by CLuaScript at runtime
-- Kusudama (giant balloon) animation: roll-in, hold, broken (petal burst) or failed. Group D.

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

local petals_center_pos_x = { 754, 1187.5, 1194.5, 1172, 758, 1171, 748.5, 753.5, 776.5, 1175, 1008, 913.5, 923, 1017.5, 780, 1152, 856, 1073, 848.5, 1085.5, 965.5 }
local petals_center_pos_y = { 518.5, 466, 369.5, 260.5, 260.5, 465.5, 469, 352, 429.5, 526, 626, 623, 158, 161, 370, 370, 231, 228, 551.5, 547.5, 397 }

local petals_x = { 1, 108, 242, 350, 463, 576, 1, 135, 267, 333, 434, 509, 601, 676, 1, 176, 351, 550, 1, 199, 397 }
local petals_y = { 1, 1, 1, 1, 1, 1, 111, 111, 111, 111, 111, 111, 111, 111, 237, 237, 237, 237, 440, 440, 440 }
local petals_rect_x = { 106, 133, 107, 112, 112, 108, 133, 131, 65, 100, 74, 91, 74, 91, 174, 174, 198, 198, 197, 197, 267 }
local petals_rect_y = { 109, 88, 65, 77, 77, 77, 88, 90, 101, 72, 114, 116, 114, 116, 196, 196, 202, 202, 199, 199, 288 }
local petals_direction = { -0.8, 1.1, 0.7, 0.6, -0.7, 0.5, -0.6, -0.8, -0.9, 1.1, 0.2, -0.15, -0.2, 0.15, -1.3, 1.2, -0.8, 0.7, -0.6, 0.55, 0.05 }
local petals_rotation = { -0.9, 0.8, 0.7, 0.9, -0.8, 0.6, -0.5, -0.7, -1.1, 1.2, 1.2, -1.1, -1.2, 1.1, -0.8, 0.7, -0.6, 0.55, -0.45, 0.5, 0.2 }
local petals_fall = { 1.1, 1.2, 0.9, 0.7, 0.7, 0.5, 0.6, 0.9, 0.5, 1.1, 1.5, 1.4, 1.5, 1.4, 0.7, 0.8, 0.5, 0.6, 1.2, 1.3, 0.6 }

local tx = {}

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

function onStart()
    tx["Kusudama.png"] = TEXTURE:CreateTextureSync("Kusudama.png")
    tx["Kusudama_Fail.png"] = TEXTURE:CreateTextureSync("Kusudama_Fail.png")
    tx["Kusudama_Back.png"] = TEXTURE:CreateTextureSync("Kusudama_Back.png")
    tx["Petals.png"] = TEXTURE:CreateTextureSync("Petals.png")
    tx["Star.png"] = TEXTURE:CreateTextureSync("Star.png")
    tx["Star_Fail.png"] = TEXTURE:CreateTextureSync("Star_Fail.png")
    tx["Flash.png"] = TEXTURE:CreateTextureSync("Flash.png")
    tx["Flash2.png"] = TEXTURE:CreateTextureSync("Flash2.png")
    tx["Flare.png"] = TEXTURE:CreateTextureSync("Flare.png")
    tx["Effect_0.png"] = TEXTURE:CreateTextureSync("Effect_0.png")
    tx["Effect_0.png"]:SetBlendMode("add")
    tx["Effect_1.png"] = TEXTURE:CreateTextureSync("Effect_1.png")
    tx["Effect_1.png"]:SetBlendMode("add")
    tx["Effect_2.png"] = TEXTURE:CreateTextureSync("Effect_2.png")
    tx["Effect_2.png"]:SetBlendMode("add")
    tx["Text.png"] = TEXTURE:CreateTextureSync("Text.png")

    kusuState = 0
    fadeValue = 0
end

function update(timestamp, state)
    if kusuState == 1 then
        animeValue = animeValue + (fps.deltaTime * 2)
    elseif kusuState == 2 then
        animeValue = animeValue + fps.deltaTime
    elseif kusuState == 3 then
        animeValue = animeValue + (fps.deltaTime / 1.5)
    elseif kusuState == 4 then
        animeValue = animeValue + (fps.deltaTime * 2)
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
        fadeValue = fadeValue - (fps.deltaTime * 2)
    else
        fadeValue = fadeValue + (fps.deltaTime * 2)
    end

    if fadeValue > 1 then
        fadeValue = 1
    elseif fadeValue < 0 then
        fadeValue = 0
    end

    effectValue = effectValue + (fps.deltaTime * 6)
end

function draw(state)
    tx["Kusudama_Back.png"]:SetOpacity(fadeValue)   -- old: SetOpacity(fadeValue * 255) on the 0-255 API
    tx["Kusudama_Back.png"]:DrawRect(0, 0, 0, 0, 1920, 1080)

    -- Into Kusudama
    if kusuState == 1 then

        -- EaseOutBack
        local c1 = 1.70158
        local c3 = c1 + 1
        local updateValue = 1 + c3 * (animeValue - 1) ^ 3 + c1 * (animeValue - 1) ^ 2

        tx["Kusudama.png"]:Draw(kusudamaPos[1], kusudamaPos[2] - (500 * (1 - updateValue)))
        tx["Star.png"]:DrawAtAnchor(starCenterPos[1], starCenterPos[2] - (500 * (1 - updateValue)), "center")

    -- Kusudama
    elseif kusuState == 2 then

        tx["Kusudama.png"]:Draw(kusudamaPos[1], kusudamaPos[2])
        tx["Star.png"]:DrawAtAnchor(starCenterPos[1], starCenterPos[2], "center")

        tx["Flash.png"]:SetOpacity((128 - (128 * ((math.cos(animeValue * 8) + 1) / 2))) / 255)
        tx["Flash.png"]:SetScale(0.5, 0.5)

        tx["Flash.png"]:DrawAtAnchor(flashCenterPos[1], flashCenterPos[2], "center")

    -- Kusudama Broken
    elseif kusuState == 3 then

        tx["Flare.png"]:SetScale(0.9 + (0.45 * animeValue), 0.9 + (0.45 * animeValue))
        tx["Flare.png"]:SetOpacity(math.max(0, 255 - (384 * animeValue)) / 255)

        tx["Flare.png"]:DrawAtAnchor(flareCenterPos[1], flareCenterPos[2], "center")

        for i = 1, petalCount do
            -- EaseOutCubic
            local xCurve = (1 - (1 - animeValue) ^ 3) * petals_direction[i]
            -- EaseInSine Edited
            local yCurve = (1 - math.cos((animeValue * math.pi) / 2) + (animeValue * -0.6)) * petals_fall[i]

            tx["Petals.png"]:SetRotation(270 * petals_rotation[i] * animeValue)
            tx["Petals.png"]:SetOpacity(math.min(512 - (512 * animeValue), 255) / 255)

            tx["Petals.png"]:DrawRectAtAnchor(petals_center_pos_x[i] + (500 * xCurve), petals_center_pos_y[i] + (600 * yCurve), petals_x[i], petals_y[i], petals_rect_x[i], petals_rect_y[i], "center")
        end

        local fscale = math.min(1, 0.5 + (animeValue * 2))
        local fopacity = math.max(0, 255 - (512 * animeValue))

        tx["Flash2.png"]:SetRotation(480 * animeValue)
        tx["Flash2.png"]:SetOpacity(fopacity / 255)
        tx["Flash2.png"]:SetScale(fscale, fscale)

        tx["Flash.png"]:SetOpacity(fopacity / 255)
        tx["Flash.png"]:SetScale(fscale, fscale)

        tx["Flash.png"]:DrawAtAnchor(flashCenterPos[1], flashCenterPos[2], "center")
        tx["Flash2.png"]:DrawAtAnchor(flash2CenterPos[1], flash2CenterPos[2], "center")

    -- Kusudama Failed
    elseif kusuState == 4 then

        -- EaseInBack
        local c1 = 1.70158
        local c3 = c1 + 1
        local updateValue = c3 * animeValue * animeValue * animeValue - c1 * animeValue * animeValue

        tx["Kusudama_Fail.png"]:SetOpacity(math.min(512 * animeValue, 255) / 255)
        tx["Star_Fail.png"]:SetOpacity(math.min(512 * animeValue, 255) / 255)

        tx["Kusudama.png"]:Draw(kusudamaPos[1], kusudamaPos[2] - (500 * updateValue))
        tx["Kusudama_Fail.png"]:Draw(kusudamaPos[1], kusudamaPos[2] - (500 * updateValue))
        tx["Star.png"]:DrawAtAnchor(starCenterPos[1], starCenterPos[2] - (500 * updateValue), "center")
        tx["Star_Fail.png"]:DrawAtAnchor(starCenterPos[1], starCenterPos[2] - (500 * updateValue), "center")

    end

    if kusuState == 1 or kusuState == 2 then
        tx["Text.png"]:DrawAtAnchor(960, 600, "center")
    end

    if kusuState ~= 0 then
        tx["Effect_" .. tostring(math.floor(effectValue) % 3) .. ".png"]:SetOpacity(fadeValue)
        tx["Effect_" .. tostring(math.floor(effectValue) % 3) .. ".png"]:Draw(0, 0)
    end
end

function onDestroy()
    for _, t in pairs(tx) do
        if t ~= nil then t:Dispose() end
    end
    tx = {}
end