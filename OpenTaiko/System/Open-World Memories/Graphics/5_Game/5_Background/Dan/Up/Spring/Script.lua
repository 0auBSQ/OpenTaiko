---@diagnostic disable: undefined-global  -- TEXTURE/fps injected by CLuaScript at runtime
-- Dan up background (Spring): dojo scene with a scrolling cloud layer + three layers of
-- scrolling/swaying/rotating falling flowers. No per-player / clear-fade logic.
-- Ported from the old ScriptBG func: API to the ROActivity LuaTexture API.

local bgWidth_1 = 1984
local bgWidth_4 = 962
local bgScrollX_1 = 0
local bgScrollX_4 = 0

local flowerScrollX_1 = 0
local flowerScrollX_2 = 150
local flowerScrollX_3 = 300
local flowerScrollY_1 = 0
local flowerScrollY_2 = 200
local flowerScrollY_3 = 350
local flowerRot1 = 0
local flowerRot2 = 0
local flowerRot3 = 0
local flowerSway_1 = 0
local flowerSwayFinal_1 = 0
local flowerSway_2 = 0
local flowerSwayFinal_2 = 0
local flowerSway_3 = 0
local flowerSwayFinal_3 = 0

local tx = {}

function clearIn(player)
end

function clearOut(player)
end

function onStart()
    tx["Skybox.png"] = TEXTURE:CreateTextureSync("Skybox.png")
    tx["Dojo.png"] = TEXTURE:CreateTextureSync("Dojo.png")
    tx["Bush.png"] = TEXTURE:CreateTextureSync("Bush.png")
    tx["Clouds.png"] = TEXTURE:CreateTextureSync("Clouds.png")
    tx["Flower1.png"] = TEXTURE:CreateTextureSync("Flower1.png")
    tx["Flower2.png"] = TEXTURE:CreateTextureSync("Flower2.png")
    tx["Flower3.png"] = TEXTURE:CreateTextureSync("Flower3.png")
end

function update(timestamp, state)
    bgScrollX_1 = bgScrollX_1 + (59.1 * fps.deltaTime)
    if bgScrollX_1 > bgWidth_1 then
        bgScrollX_1 = 0
    end

    flowerScrollX_1 = flowerScrollX_1 + (75 * fps.deltaTime)
    flowerScrollX_2 = flowerScrollX_2 + (75 * fps.deltaTime)
    flowerScrollX_3 = flowerScrollX_3 + (75 * fps.deltaTime)

    bgScrollX_4 = bgScrollX_4 + (100 * fps.deltaTime)
    if bgScrollX_4 > bgWidth_4 then
        bgScrollX_4 = 0
    end

    flowerScrollY_1 = flowerScrollY_1 + (100 * fps.deltaTime)
    if flowerScrollY_1 > 500 then
        flowerScrollY_1 = 0
        flowerScrollX_1 = 0
    end
    flowerScrollY_2 = flowerScrollY_2 + (100 * fps.deltaTime)
    if flowerScrollY_2 > 500 then
        flowerScrollY_2 = 0
        flowerScrollX_2 = 200
    end
    flowerScrollY_3 = flowerScrollY_3 + (100 * fps.deltaTime)
    if flowerScrollY_3 > 500 then
        flowerScrollY_3 = 0
        flowerScrollX_3 = -150
    end

    flowerSway_1 = flowerSway_1 + (50 * fps.deltaTime)
    flowerSwayFinal_1 = 70 * math.cos((5 * flowerSway_1) / 150) * math.sin((2 * flowerSway_1) / 150)
    flowerSway_2 = flowerSway_2 + (70 * fps.deltaTime)
    flowerSwayFinal_2 = 60 * math.cos((5 * flowerSway_2) / 120) * math.sin((2 * flowerSway_2) / 120)
    flowerSway_3 = flowerSway_3 + (36 * fps.deltaTime)
    flowerSwayFinal_3 = 40 * math.cos((5 * flowerSway_2) / 130) * math.sin((2 * flowerSway_2) / 130)   -- original uses flowerSway_2 (not _3) here — preserved

    flowerRot1 = flowerRot1 + (28 * fps.deltaTime)
    flowerRot2 = flowerRot2 + (40 * fps.deltaTime)
    flowerRot3 = flowerRot3 + (50 * fps.deltaTime)
end

function draw(state)
    tx["Skybox.png"]:Draw(0, 0)
    for i = 0, 4 do
        tx["Clouds.png"]:Draw((i * bgWidth_1) - bgScrollX_1, 0)
    end
    tx["Dojo.png"]:Draw(0, 0)
    tx["Bush.png"]:Draw(0, 0)
    for i = 0, 4 do
        tx["Flower1.png"]:SetRotation(flowerRot1 + (i * 60))
        tx["Flower2.png"]:SetRotation(flowerRot2 + (i * 60))
        tx["Flower3.png"]:SetRotation(flowerRot3 + (i * 60))
        tx["Flower1.png"]:Draw((i * 600) - flowerScrollX_1 + flowerSwayFinal_1, -70 + flowerScrollY_1 + (-75 * (i % 2)))
        tx["Flower2.png"]:Draw((i * 500) - flowerScrollX_2 + flowerSwayFinal_2, -70 + flowerScrollY_2 + (-60 * (i % 2)))
        tx["Flower3.png"]:Draw((i * 700) - flowerScrollX_3 + flowerSwayFinal_3, -70 + flowerScrollY_3 + (-50 * (i % 2)))
    end
end

function onDestroy()
    for _, t in pairs(tx) do
        if t ~= nil then t:Dispose() end
    end
    tx = {}
end
