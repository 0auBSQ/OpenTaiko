---@diagnostic disable: undefined-global  -- TEXTURE/fps injected by CLuaScript at runtime
-- AI Win Full Combo end animation: timed glow/star/flower/text reveal over a fading background.
-- Ported from the old ScriptBG func: API to the ROActivity LuaTexture API.
--   init -> onStart (CreateTextureSync into the local `tx` registry); playEndAnime(player) resets the timeline.
--   update(player)/draw(player) -> update(timestamp, state)/draw(state) with `local player = state.player`.
--   func:SetOpacity(v*255) -> tx:SetOpacity(v) (0-255 -> 0-1); deltaTime -> fps.deltaTime.

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

local fcTex = { posx = 536, posy = 689, x = 19, y = 1951, w = 873, h = 202 }
local fcGlowTex = { posx = 518, posy = 689, x = 1083, y = 1925, w = 873, h = 202 }

local tx = {}

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

function onStart()
  tx["Assets.png"] = TEXTURE:CreateTextureSync("Assets.png")
  tx["Background.png"] = TEXTURE:CreateTextureSync("Background.png")
end

function update(timestamp, state)
    local player = state.player
    animeCounter = animeCounter + (1.0 * fps.deltaTime)
end

function draw(state)
    local player = state.player
    local value = math.min(animeCounter * 4, 1)
    local scale = 2 - value

    --ordered by timeline of appearance
    local glowIn = clamp(animeCounter - 0, 0, 0.25) * 4
    local baseIn = clamp(animeCounter - 0.25, 0, 0.1) * 10
    local glowOut = clamp(animeCounter - 0.35, 0, 0.25) * 4
    local dropIn
    if animeCounter >= 0.35 then
      dropIn = 0.5
    else
      dropIn = 0
    end
    local dropOut = clamp(animeCounter - 0.35, 0, 2) / 2

    local outPos = glowOut^0.25 * 20

    local textProgress = clamp(animeCounter - 1.5, 0, 0.4) * 2.5
    local textProgress2 = clamp(animeCounter - 1.51, 0, 0.4) * 2.5
    local flowerGlowProgress = clamp(animeCounter - 1.5, 0, 0.2) - clamp(animeCounter - 1.7, 0, 0.2)

    tx["Background.png"]:SetOpacity(baseIn - glowOut) -- Background
    tx["Background.png"]:DrawRect(0, 0, 0, 0, 1920, 1080)

    tx["Assets.png"]:SetOpacity((dropIn - dropOut)^0.4) -- Water Drop
    tx["Assets.png"]:SetScale((dropIn + dropOut)^0.4, (dropIn + dropOut)^0.4)
    tx["Assets.png"]:DrawRectAtAnchor(waterDropTex.posx, waterDropTex.posy, waterDropTex.x, waterDropTex.y, waterDropTex.w, waterDropTex.h, "center")

    tx["Assets.png"]:SetOpacity(baseIn) -- Stars
    tx["Assets.png"]:SetScale(scale, scale)
    tx["Assets.png"]:DrawRectAtAnchor(star1Tex.posx + 20 - outPos, star1Tex.posy + 20 - outPos, star1Tex.x, star1Tex.y, star1Tex.w, star1Tex.h, "center") -- 1
    tx["Assets.png"]:DrawRectAtAnchor(star2Tex.posx - 20 + outPos, star2Tex.posy + 20 - outPos, star2Tex.x, star2Tex.y, star2Tex.w, star2Tex.h, "center") -- 2
    tx["Assets.png"]:DrawRectAtAnchor(star3Tex.posx + 20 - outPos, star3Tex.posy - 20 + outPos, star3Tex.x, star3Tex.y, star3Tex.w, star3Tex.h, "center") -- 3
    tx["Assets.png"]:DrawRectAtAnchor(star4Tex.posx - 20 + outPos, star4Tex.posy - 20 + outPos, star4Tex.x, star4Tex.y, star4Tex.w, star4Tex.h, "center") -- 4

    tx["Assets.png"]:SetOpacity(baseIn * math.sin(animeCounter)^2) -- Glowing Stars
    tx["Assets.png"]:SetScale(scale, scale)
    tx["Assets.png"]:DrawRectAtAnchor(star1GlowTex.posx + 20 - outPos, star1GlowTex.posy + 20 - outPos, star1GlowTex.x, star1GlowTex.y, star1GlowTex.w, star1GlowTex.h, "center") -- 1
    tx["Assets.png"]:DrawRectAtAnchor(star2GlowTex.posx - 20 + outPos, star2GlowTex.posy + 20 - outPos, star2GlowTex.x, star2GlowTex.y, star2GlowTex.w, star2GlowTex.h, "center") -- 2
    tx["Assets.png"]:DrawRectAtAnchor(star3GlowTex.posx + 20 - outPos, star3GlowTex.posy - 20 + outPos, star3GlowTex.x, star3GlowTex.y, star3GlowTex.w, star3GlowTex.h, "center") -- 3
    tx["Assets.png"]:DrawRectAtAnchor(star4GlowTex.posx - 20 + outPos, star4GlowTex.posy - 20 + outPos, star4GlowTex.x, star4GlowTex.y, star4GlowTex.w, star4GlowTex.h, "center") -- 4

    tx["Assets.png"]:SetOpacity(baseIn) -- Flower & Text
    tx["Assets.png"]:SetScale(scale, scale)
    tx["Assets.png"]:DrawRectAtAnchor(flowerTex.posx, flowerTex.posy, flowerTex.x, flowerTex.y, flowerTex.w, flowerTex.h, "center") -- Flower
    tx["Assets.png"]:DrawRectAtAnchor(textTex.posx, textTex.posy, textTex.x, textTex.y, textTex.w, textTex.h, "center") -- Text

    tx["Assets.png"]:SetOpacity(glowIn - glowOut + flowerGlowProgress) -- Glowing Flower
    tx["Assets.png"]:SetScale(scale, scale)
    tx["Assets.png"]:DrawRectAtAnchor(flowerGlowTex.posx, flowerGlowTex.posy, flowerGlowTex.x, flowerGlowTex.y, flowerGlowTex.w, flowerGlowTex.h, "center")

    tx["Assets.png"]:SetOpacity(1) -- Full Combo Text
    tx["Assets.png"]:SetScale(1, 1)
    tx["Assets.png"]:DrawRect(fcTex.posx, fcTex.posy, fcTex.x, fcTex.y, fcTex.w * textProgress, fcTex.h)
    tx["Assets.png"]:DrawRect(fcGlowTex.posx + (fcGlowTex.w * textProgress), fcGlowTex.posy, fcGlowTex.x + (fcGlowTex.w * textProgress), fcGlowTex.y, fcGlowTex.w * (textProgress - textProgress2), fcGlowTex.h)
  end

function onDestroy()
    for _, t in pairs(tx) do
        if t ~= nil then t:Dispose() end
    end
    tx = {}
end
