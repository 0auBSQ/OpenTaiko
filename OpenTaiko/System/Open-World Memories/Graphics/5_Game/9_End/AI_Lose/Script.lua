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
    func:AddGraph("BaseLeft.png")
    func:AddGraph("BaseRight.png")
    func:AddGraph("BaseGlow.png")
    func:AddGraph("Text.png")
    func:AddGraph("Overlay.png")
end

function update(player)
    animeCounter = animeCounter + (1.0 * deltaTime)
end

function draw(player)

    --ordered by timeline of appearance
    glowIn = math.min(math.max(animeCounter - 0, 0), 0.25) * 4
    baseIn = math.min(math.max(animeCounter - 0.25, 0), 0.1) * 10
    glowOut = math.min(math.max(animeCounter - 0.35, 0), 0.25) * 4
    baseSplit = (math.min(math.max(animeCounter - 1.0, 0), 0.1) * 10)^0.25 * 30
    overlayIn = math.min(math.max(animeCounter - 1.35, 0), 0.2) * 5

    func:SetOpacity(baseIn * 255, "BaseLeft.png")
    func:SetOpacity(baseIn * 255, "BaseRight.png")
    func:SetOpacity((glowIn - glowOut) * 255, "BaseGlow.png")
    func:SetOpacity(baseIn * 255, "Text.png")
    func:SetOpacity(overlayIn * 255, "Overlay.png")

    func:DrawGraphCenter(x - baseSplit, y, "BaseLeft.png")
    func:DrawGraphCenter(x + baseSplit, y, "BaseRight.png")
    func:DrawGraphCenter(x, y + ((overlayIn - 1) * 20), "Overlay.png")
    func:DrawGraphCenter(x, y, "Text.png")
    func:DrawGraphCenter(x, y, "BaseGlow.png")
end
