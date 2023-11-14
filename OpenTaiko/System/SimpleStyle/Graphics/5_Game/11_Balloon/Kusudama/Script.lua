
local kusuState = 0

local origin_x = 640
local origin_y = 214

local text_x = 523
local text_y = 45

local text_move = 23
local text_move_counter = 0

local animeValue = 0
local openFrame = 0
local effectFrame = 0
local effectOpacity = 255
local brokeOpacity = 255
local bgInOpacity = 0
local kusuInY = 0
local kusuInOverY = 67
local kusuMissY = 0
local kusuIdleScale = 1

function kusuIn()
    kusuState = 1
    animeValue = 0
    text_move_counter = 0
end

function kusuBroke()
    kusuState = 3
    animeValue = 0
    brokeOpacity = 255
end

function kusuMiss()
    kusuState = 4
    animeValue = 0
end

function init()
    func:AddGraph("Kusudama_Back.png")

    func:AddGraph("Kusudama_Idle.png")

    func:AddGraph("Kusudama_Open_0.png")
    func:AddGraph("Kusudama_Open_1.png")
    func:AddGraph("Kusudama_Open_2.png")
    func:AddGraph("Kusudama_Open_3.png")

    func:AddGraph("Kusudama_Miss.png")

    func:AddGraph("Text.png")

    func:AddGraph("Effect_0.png")
    func:SetBlendMode("Add", "Effect_0.png")
    func:AddGraph("Effect_1.png")
    func:SetBlendMode("Add", "Effect_1.png")
    func:AddGraph("Effect_2.png")
    func:SetBlendMode("Add", "Effect_2.png")

    kusuState = 0
    openFrame = 0
end

function update()
    animeValue = animeValue + (1 * deltaTime)
    --animeValue = animeValue + (0.25 * deltaTime)

    if animeValue > 10 then 
        animeValue = 10
    end 

    effectFrame = effectFrame + (15 * deltaTime)
    if effectFrame >= 2 then
        effectFrame = 0
    end

    if kusuState == 0 then
    elseif kusuState == 1 then

        bgInOpacity = math.sin(math.min(animeValue, 1) * 2 * math.pi) * 255
        effectOpacity = math.min(animeValue * 500, 255 / 2)
        in1 = -467 + (math.sin(math.min(animeValue * 1.8, 0.5) * math.pi) * (467 + kusuInOverY))
        in2 = ((1 - (math.cos(math.max(math.min((animeValue * 5) - 1.2, 0.5), 0.0) * math.pi))) * kusuInOverY)
        kusuInY = in1 - in2

        if animeValue > 0.35 then
            kusuState = 2
            animeValue = 0
        end
    elseif kusuState == 2 then
        effectOpacity = 255 / 2

        kusuIdleScale = 1.0 + (math.sin(animeValue * 4 * math.pi) / 40.0)
        if animeValue > 0.25 then
            animeValue = 0
        end

        text_move_counter = text_move_counter + (0.5 * deltaTime)
        if text_move_counter > 1 then
            text_move_counter = 0
        end


    elseif kusuState == 3 then
        openFrame = math.min(math.ceil((animeValue * 10) - 1), 3)
        brokeOpacity = 255 - (math.max(animeValue - 0.9, 0) * 1000)
        effectOpacity = brokeOpacity / 2

        if brokeOpacity == 0 then 
            kusuState = 0
        end


    elseif kusuState == 4 then
        effectOpacity = 0
        kusuMissY = (1.0 - math.cos(math.max(animeValue - 0.15, 0) * math.pi)) * 4000

        if animeValue > 0.5 then 
            kusuState = 0
        end

    end
end

function draw()
    effectFile = "Effect_"..tostring(math.ceil(effectFrame))..".png"

    if kusuState == 0 then
        func:SetOpacity(0, effectFile)
    elseif kusuState == 1 then
        func:SetOpacity(bgInOpacity, "Kusudama_Back.png")
        func:DrawGraph(0, 0, "Kusudama_Back.png")
        func:DrawGraphCenter(origin_x, origin_y + kusuInY, "Kusudama_Idle.png")
    elseif kusuState == 2 then
        func:SetScale(kusuIdleScale, kusuIdleScale, "Kusudama_Idle.png")
        func:DrawGraphCenter(origin_x, origin_y, "Kusudama_Idle.png")

        if text_move_counter < 0.5 then 
            func:DrawGraph(text_x, text_y - (text_move * text_move_counter * 2), "Text.png")
        else 
            func:DrawGraph(text_x, text_y - (text_move * (1 - text_move_counter) * 2), "Text.png")
        end
    elseif kusuState == 3 then
        func:SetOpacity(brokeOpacity, "Kusudama_Open_3.png")
        func:DrawGraphCenter(origin_x, origin_y, "Kusudama_Open_"..tostring(openFrame)..".png")
    elseif kusuState == 4 then
        func:DrawGraphCenter(origin_x, origin_y + kusuMissY, "Kusudama_Miss.png")
    end

    if not(kusuState == 0) then
        func:SetOpacity(effectOpacity, effectFile)
        func:DrawGraph(0, 0, "Effect_"..tostring(math.ceil(effectFrame))..".png")
    end
end
