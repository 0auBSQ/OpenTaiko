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

local flowerTex = { posx = 972.5, posy = 524, x = 1, y = 1, w = 1017, h = 938 }
local textTex = { posx = 951, posy = 413, x = 1083, y = 1082, w = 900, h = 642 }
local flowerGlowTex = { posx = 972.5, posy = 525, x = 1, y = 940, w = 1081, h = 1010 }
local waterDropTex = { posx = 960, posy = 540, x = 1019, y = 1, w = 1080, h = 1080 }

local star1Tex = { posx = 649, posy = 226.5, x = 1083, y = 1725, w = 104, h = 137 }
local star2Tex = { posx = 1259.5, posy = 178.5, x = 1188, y = 1725, w = 79, h = 103 }
local star3Tex = { posx = 503, posy = 687.5, x = 1268, y = 1725, w = 104, h = 137 }
local star4Tex = { posx = 1372.5, posy = 753, x = 1373, y = 1725, w = 79, h = 104 }
local star1GlowTex = { posx = 650, posy = 226.5, x = 1453, y = 1725, w = 164, h = 199 }
local star2GlowTex = { posx = 1259, posy = 178, x = 1618, y = 1725, w = 136, h = 162 }
local star3GlowTex = { posx = 504, posy = 687.5, x = 1755, y = 1725, w = 164, h = 199 }
local star4GlowTex = { posx = 1372, posy = 753, x = 1920, y = 1725, w = 136, h = 162 }

local pfcTex = { posx = 591, posy = 717, x = 1, y = 1951, w = 712, h = 174 }
local pfcGlowTex = { posx = 591, posy = 717, x = 1083, y = 1925, w = 712, h = 174 }

function clamp(value, min, max) -- why isn't this part of lua's math library? :(
  return math.min(math.max(value, min), max)
end

function clearIn(player)
end

function clearOut(player)
end

function playEndAnime(player)
    animeCounter = 0
end

function init()
  func:AddGraph("Assets.png")
  func:AddGraph("Background.png")
end

function update(player)
    animeCounter = animeCounter + (1.0 * deltaTime)
end

function draw(player)
    value = math.min(animeCounter * 4, 1)
    scale = 2 - value

    --ordered by timeline of appearance
    glowIn = clamp(animeCounter - 0, 0, 0.25) * 4
    baseIn = clamp(animeCounter - 0.25, 0, 0.1) * 10
    glowOut = clamp(animeCounter - 0.35, 0, 0.25) * 4
    if animeCounter >= 0.35 then
      dropIn = 0.5
    else
      dropIn = 0
    end
    dropOut = clamp(animeCounter - 0.35, 0, 2) / 2

    outPos = glowOut^0.25 * 20

    textProgress = clamp(animeCounter - 1.5, 0, 0.4) * 2.5
    textProgress2 = clamp(animeCounter - 1.51, 0, 0.4) * 2.5
    flowerGlowProgress = (clamp(animeCounter - 1.5, 0, 0.2) * 2) - (clamp(animeCounter - 1.7, 0, 0.2) * 2)

    func:SetOpacity((baseIn - glowOut) * 255, "Background.png") -- Background
    func:DrawRectGraph(0, 0, 0, 0, 1920, 1080, "Background.png")

    func:SetOpacity((dropIn - dropOut)^0.4 * 255, "Assets.png") -- Water Drop
    func:SetScale((dropIn + dropOut)^0.4, (dropIn + dropOut)^0.4, "Assets.png")
    func:DrawGraphRectCenter(waterDropTex.posx, waterDropTex.posy, waterDropTex.x, waterDropTex.y, waterDropTex.w, waterDropTex.h, "Assets.png")

    func:SetOpacity(baseIn * 255, "Assets.png") -- Stars
    func:SetScale(scale, scale, "Assets.png") 
    func:DrawGraphRectCenter(star1Tex.posx + 20 - outPos, star1Tex.posy + 20 - outPos, star1Tex.x, star1Tex.y, star1Tex.w, star1Tex.h, "Assets.png") -- 1
    func:DrawGraphRectCenter(star2Tex.posx - 20 + outPos, star2Tex.posy + 20 - outPos, star2Tex.x, star2Tex.y, star2Tex.w, star2Tex.h, "Assets.png") -- 2
    func:DrawGraphRectCenter(star3Tex.posx + 20 - outPos, star3Tex.posy - 20 + outPos, star3Tex.x, star3Tex.y, star3Tex.w, star3Tex.h, "Assets.png") -- 3
    func:DrawGraphRectCenter(star4Tex.posx - 20 + outPos, star4Tex.posy - 20 + outPos, star4Tex.x, star4Tex.y, star4Tex.w, star4Tex.h, "Assets.png") -- 4

    func:SetOpacity(baseIn * math.sin(animeCounter)^2 * 255, "Assets.png") -- Glowing Stars
    func:SetScale(scale, scale, "Assets.png")
    func:DrawGraphRectCenter(star1GlowTex.posx + 20 - outPos, star1GlowTex.posy + 20 - outPos, star1GlowTex.x, star1GlowTex.y, star1GlowTex.w, star1GlowTex.h, "Assets.png") -- 1
    func:DrawGraphRectCenter(star2GlowTex.posx - 20 + outPos, star2GlowTex.posy + 20 - outPos, star2GlowTex.x, star2GlowTex.y, star2GlowTex.w, star2GlowTex.h, "Assets.png") -- 2
    func:DrawGraphRectCenter(star3GlowTex.posx + 20 - outPos, star3GlowTex.posy - 20 + outPos, star3GlowTex.x, star3GlowTex.y, star3GlowTex.w, star3GlowTex.h, "Assets.png") -- 3
    func:DrawGraphRectCenter(star4GlowTex.posx - 20 + outPos, star4GlowTex.posy - 20 + outPos, star4GlowTex.x, star4GlowTex.y, star4GlowTex.w, star4GlowTex.h, "Assets.png") -- 4

    func:SetOpacity(baseIn * 255, "Assets.png") -- Flower & Text
    func:SetScale(scale, scale, "Assets.png")
    func:DrawGraphRectCenter(flowerTex.posx, flowerTex.posy, flowerTex.x, flowerTex.y, flowerTex.w, flowerTex.h, "Assets.png") -- Flower
    func:DrawGraphRectCenter(textTex.posx, textTex.posy, textTex.x, textTex.y, textTex.w, textTex.h, "Assets.png") -- Text

    func:SetOpacity((glowIn - glowOut + flowerGlowProgress) * 255, "Assets.png") -- Glowing Flower
    func:SetScale(scale, scale, "Assets.png")
    func:DrawGraphRectCenter(flowerGlowTex.posx, flowerGlowTex.posy, flowerGlowTex.x, flowerGlowTex.y, flowerGlowTex.w, flowerGlowTex.h, "Assets.png")

    func:SetOpacity(255, "Assets.png") -- Full Combo Text
    func:SetScale(1, 1, "Assets.png")
    func:DrawRectGraph(pfcTex.posx, pfcTex.posy, pfcTex.x, pfcTex.y, pfcTex.w * textProgress, pfcTex.h, "Assets.png")
    func:DrawRectGraph(pfcGlowTex.posx + (pfcGlowTex.w * textProgress), pfcGlowTex.posy, pfcGlowTex.x + (pfcGlowTex.w * textProgress), pfcGlowTex.y, pfcGlowTex.w * (textProgress - textProgress2), pfcGlowTex.h, "Assets.png")
  end
