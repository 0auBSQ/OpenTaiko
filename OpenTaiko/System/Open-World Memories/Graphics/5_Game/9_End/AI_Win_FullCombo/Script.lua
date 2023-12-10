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

local animeCounter = 0

local x = 960
local y = 540

function clearIn(player)
end

function clearOut(player)
end

function playEndAnime(player)
    animeCounter = 0
end

function init()
    func:AddGraph("Base.png")
    func:AddGraph("BaseGlow.png")
    func:AddGraph("WaterDrop.png")
    func:AddGraph("Text.png")
    func:AddGraph("Overlay.png")
    func:AddGraph("OverlayGlow.png")
    func:AddGraph("Background.png")
end

function update(player)
    animeCounter = animeCounter + (1.0 * deltaTime)
end

function draw(player)
    value = math.min(animeCounter * 4, 1)
    scale = 2 - value

    --ordered by timeline of appearance
    glowIn = math.min(math.max(animeCounter - 0, 0), 0.25) * 4
    baseIn = math.min(math.max(animeCounter - 0.25, 0), 0.1) * 10
    glowOut = math.min(math.max(animeCounter - 0.35, 0), 0.25) * 4
    if animeCounter >= 0.35 then
      dropIn = 0.5
    else
      dropIn = 0
    end
    dropOut = math.min(math.max(animeCounter - 0.35, 0), 2) / 2

    outPos = glowOut^0.25 * 20

    func:SetOpacity(baseIn * 255, "Base.png")
    func:SetScale(scale, scale, "Base.png")
    func:SetOpacity(baseIn * 255, "Text.png")
    func:SetScale(scale, scale, "Text.png")
    func:SetOpacity(baseIn * 255, "Overlay.png")
    func:SetScale(scale, scale, "Overlay.png")

    func:SetOpacity((glowIn - glowOut) * 255, "BaseGlow.png")
    func:SetScale(scale, scale, "BaseGlow.png")
    func:SetOpacity((dropIn - dropOut)^0.4 * 255, "WaterDrop.png")
    func:SetScale((dropIn + dropOut)^0.4, (dropIn + dropOut)^0.4, "WaterDrop.png")
    func:SetOpacity(baseIn * math.sin(animeCounter)^2 * 255, "OverlayGlow.png")
    func:SetScale(scale, scale, "OverlayGlow.png")

    func:SetOpacity((baseIn - glowOut) * 255, "Background.png")

    func:DrawGraphCenter(x, y, "Background.png")
    func:DrawGraphCenter(x, y, "WaterDrop.png")

    func:DrawRectGraph(20 - outPos, 20 - outPos, 0, 0, x, y, "Overlay.png")
    func:DrawRectGraph(x - 20 + outPos, 20 - outPos, x, 0, x, y, "Overlay.png")
    func:DrawRectGraph(20 - outPos, y - 20 + outPos, 0, y, x, y, "Overlay.png")
    func:DrawRectGraph(x - 20 + outPos, y - 20 + outPos, x, y, x, y, "Overlay.png")


    func:DrawRectGraph(20 - outPos, 20 - outPos, 0, 0, x, y, "OverlayGlow.png")
    func:DrawRectGraph(x - 20 + outPos, 20 - outPos, x, 0, x, y, "OverlayGlow.png")
    func:DrawRectGraph(20 - outPos, y - 20 + outPos, 0, y, x, y, "OverlayGlow.png")
    func:DrawRectGraph(x - 20 + outPos, y - 20 + outPos, x, y, x, y, "OverlayGlow.png")

    func:DrawGraphCenter(x, y, "Base.png")
    func:DrawGraphCenter(x, y, "Text.png")
    func:DrawGraphCenter(x, y, "BaseGlow.png")
end
