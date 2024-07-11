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

--local Tex = { posx = 0, posy = 0, x = 0, y = 0, w = 0, h = 0 }

local animeCounter = 0

local x = 960
local y = 540

local flowerLeftTex = { posx = 757.5, posy = 532.5, x = 1083, y = 1, w = 587, h = 903 }
local flowerRightTex = { posx = 1173, posy = 524, x = 1054, y = 905, w = 616, h = 938 }
local flowerGlowTex = { posx = 972.5, posy = 525.5, x = 1, y = 1, w = 1081, h = 1011 }
local textTex = { posx = 985.5, posy = 415, x = 1, y = 1385, w = 907, h = 458 }
local expressionTex = { posx = 1267, posy = 356, x = 885, y = 1013, w = 168, h = 392 }

function clearIn(player)
end

function clearOut(player)
end

function playEndAnime(player)
    animeCounter = 0
end

function init()
    func:AddGraph("Assets.png")
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

    func:SetOpacity(baseIn * 255, "Assets.png") -- Left Flower
    func:DrawGraphRectCenter(flowerLeftTex.posx - baseSplit, flowerLeftTex.posy, flowerLeftTex.x, flowerLeftTex.y, flowerLeftTex.w, flowerLeftTex.h, "Assets.png")
    
    func:SetOpacity(baseIn * 255, "Assets.png") -- Right Flower
    func:DrawGraphRectCenter(flowerRightTex.posx + baseSplit, flowerRightTex.posy, flowerRightTex.x, flowerRightTex.y, flowerRightTex.w, flowerRightTex.h, "Assets.png")
    
    func:SetOpacity(overlayIn * 255, "Assets.png") -- Expression
    func:DrawGraphRectCenter(expressionTex.posx, expressionTex.posy + ((overlayIn - 1) * 20), expressionTex.x, expressionTex.y, expressionTex.w, expressionTex.h, "Assets.png")

    func:SetOpacity(baseIn * 255, "Assets.png") -- Text
    func:DrawGraphRectCenter(textTex.posx, textTex.posy, textTex.x, textTex.y, textTex.w, textTex.h, "Assets.png")
    
    func:SetOpacity((glowIn - glowOut) * 255, "Assets.png") -- Flower Glow
    func:DrawGraphRectCenter(flowerGlowTex.posx, flowerGlowTex.posy, flowerGlowTex.x, flowerGlowTex.y, flowerGlowTex.w, flowerGlowTex.h, "Assets.png")
end
