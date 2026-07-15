local loopWidth = 1640

local bgClearFade = 0

local bgScrollX = 0

local clearInAnime_Common = 0
local clearInAnime_Scroll = 0
local clearInAnime_Deco = 0
local clearInAnime_Left = 0
local clearInAnime_Taiko = 0
local lightCounter = 0

local taiko_rotate = 0

function clearIn(player)
    clearInAnime_Common = 0
    clearInAnime_Scroll = 0
    clearInAnime_Deco = -0.4
    clearInAnime_Left = -0.6
    clearInAnime_Taiko = -0.8

    taiko_rotate = 0.0
end

function clearOut(player)
end

function init()
    func:AddGraph("Sky.png")
    func:AddGraph("Tatemono.png")
    func:AddGraph("Tyoutin.png")
    func:AddGraph("Tyoutin_Light.png")

    func:AddGraph("Down_Scroll.png")
    func:AddGraph("Down_Clear_Deco.png")
    func:AddGraph("Down_Clear_Left.png")
    func:AddGraph("Down_Clear_Taiko.png")
end

function update()
    if isClear[0] then
        bgClearFade = bgClearFade + (2000 * deltaTime)
    else
        bgClearFade = bgClearFade - (2000 * deltaTime)
    end

    lightCounter = lightCounter + (6 * deltaTime)

    clearInAnime_Common = clearInAnime_Common + (1 * deltaTime)
    clearInAnime_Scroll = clearInAnime_Scroll + (2 * deltaTime)
    clearInAnime_Deco = clearInAnime_Deco + (2 * deltaTime)
    clearInAnime_Left = clearInAnime_Left + (2 * deltaTime)
    clearInAnime_Taiko = clearInAnime_Taiko + (2 * deltaTime)

    if clearInAnime_Common > 1.0 then
        taiko_rotate = taiko_rotate + (45 * deltaTime)
    end

    bgScrollX = bgScrollX + (100 * deltaTime)
    
    if bgClearFade > 255 then
        bgClearFade = 255
    end
    if bgClearFade < 0 then
        bgClearFade = 0
    end
    
    if bgScrollX > loopWidth then
        bgScrollX = 0
    end

    if clearInAnime_Scroll > 1 then
        clearInAnime_Scroll = 1
    end

    if clearInAnime_Deco > 1 then
        clearInAnime_Deco = 1
    end
    if clearInAnime_Left > 1 then
        clearInAnime_Left = 1
    end
    if clearInAnime_Taiko > 1 then
        clearInAnime_Taiko = 1
    end
end

function draw()
    func:SetOpacity(bgClearFade, "Down_Scroll.png")
    func:SetOpacity(bgClearFade, "Down_Clear_Deco.png")
    func:SetOpacity(bgClearFade, "Down_Clear_Left.png")
    func:SetOpacity(bgClearFade, "Down_Clear_Taiko.png")

    func:DrawGraph(0, 540, "Sky.png");
    func:DrawGraph(0, 540, "Tatemono.png");
    func:DrawGraph(0, 540, "Tyoutin.png");
    func:SetOpacity(155 - (math.sin(lightCounter * math.pi) * 100), "Tyoutin_Light.png")
    func:DrawGraph(0, 540, "Tyoutin_Light.png")
    
    for i = 0, 3 do
        func:DrawGraph(0 + (loopWidth * i) - bgScrollX, 540 + ((1.0 - (clearInAnime_Scroll + (math.sin(clearInAnime_Scroll * math.pi) / 2.0))) * 474), "Down_Scroll.png")
    end

    func:DrawGraph(0, 540 + ((1.0 - (clearInAnime_Deco + (math.sin(clearInAnime_Deco * math.pi) / 2.0))) * 474), "Down_Clear_Deco.png")
    func:DrawGraph(0, 540 + ((1.0 - (clearInAnime_Left + (math.sin(clearInAnime_Left * math.pi) / 2.0))) * 474), "Down_Clear_Left.png")
    
    func:SetRotation(taiko_rotate, "Down_Clear_Taiko.png")
    func:DrawGraph(400 - ((1.0 - (clearInAnime_Taiko + (math.sin(clearInAnime_Taiko * math.pi) / 2.0))) * 700), 540, "Down_Clear_Taiko.png")
    
end
