---@diagnostic disable: undefined-global  -- TEXTURE/fps injected by CLuaScript at runtime
-- AI_Lose end animation: timeline-driven flower-burst + text + expression overlay on a single Assets.png atlas.
-- Group C clear/end script: playEndAnime(player) restarts the per-play counter; update/draw read player from state.

--local Tex = { posx = 0, posy = 0, x = 0, y = 0, w = 0, h = 0 }

local animeCounter = 0

local x = 960
local y = 540

local flowerLeftTex = { posx = 757.5, posy = 532.5, x = 1083, y = 1, w = 587, h = 903 }
local flowerRightTex = { posx = 1173, posy = 524, x = 1054, y = 905, w = 616, h = 938 }
local flowerGlowTex = { posx = 972.5, posy = 525.5, x = 1, y = 1, w = 1081, h = 1011 }
local textTex = { posx = 985.5, posy = 415, x = 1, y = 1385, w = 907, h = 458 }
local expressionTex = { posx = 1267, posy = 356, x = 885, y = 1013, w = 168, h = 392 }

local tx = {}

function clearIn(player)
end

function clearOut(player)
end

function playEndAnime(player)
    animeCounter = 0
end

function onStart()
    tx["Assets.png"] = TEXTURE:CreateTextureSync("Assets.png")
end

function update(timestamp, state)
    local player = state.player
    animeCounter = animeCounter + (1.0 * fps.deltaTime)
end

function draw(state)
    local player = state.player

    --ordered by timeline of appearance
    local glowIn = math.min(math.max(animeCounter - 0, 0), 0.25) * 4
    local baseIn = math.min(math.max(animeCounter - 0.25, 0), 0.1) * 10
    local glowOut = math.min(math.max(animeCounter - 0.35, 0), 0.25) * 4
    local baseSplit = (math.min(math.max(animeCounter - 1.0, 0), 0.1) * 10)^0.25 * 30
    local overlayIn = math.min(math.max(animeCounter - 1.35, 0), 0.2) * 5

    tx["Assets.png"]:SetOpacity(baseIn) -- Left Flower
    tx["Assets.png"]:DrawRectAtAnchor(flowerLeftTex.posx - baseSplit, flowerLeftTex.posy, flowerLeftTex.x, flowerLeftTex.y, flowerLeftTex.w, flowerLeftTex.h, "center")

    tx["Assets.png"]:SetOpacity(baseIn) -- Right Flower
    tx["Assets.png"]:DrawRectAtAnchor(flowerRightTex.posx + baseSplit, flowerRightTex.posy, flowerRightTex.x, flowerRightTex.y, flowerRightTex.w, flowerRightTex.h, "center")

    tx["Assets.png"]:SetOpacity(overlayIn) -- Expression
    tx["Assets.png"]:DrawRectAtAnchor(expressionTex.posx, expressionTex.posy + ((overlayIn - 1) * 20), expressionTex.x, expressionTex.y, expressionTex.w, expressionTex.h, "center")

    tx["Assets.png"]:SetOpacity(baseIn) -- Text
    tx["Assets.png"]:DrawRectAtAnchor(textTex.posx, textTex.posy, textTex.x, textTex.y, textTex.w, textTex.h, "center")

    tx["Assets.png"]:SetOpacity(glowIn - glowOut) -- Flower Glow
    tx["Assets.png"]:DrawRectAtAnchor(flowerGlowTex.posx, flowerGlowTex.posy, flowerGlowTex.x, flowerGlowTex.y, flowerGlowTex.w, flowerGlowTex.h, "center")
end

function onDestroy()
    for _, t in pairs(tx) do
        if t ~= nil then t:Dispose() end
    end
    tx = {}
end
