---@diagnostic disable: undefined-global  -- TEXTURE/fps injected by CLuaScript at runtime
-- Dan up background (Winter): skybox + horizontally scrolling clouds, a dojo/bush foreground,
-- and three swaying, downward-scrolling snow layers tiled across the screen.
-- Ported from the old ScriptBG func: API to the ROActivity LuaTexture API.

local bgWidth_1 = 1984
local bgWidth_4 = 962
local bgScrollX_1 = 0
local bgScrollX_4 = 0
local snowHeight = 276
local snowSway = 0
local snowSwayFinal_1 = 0
local snowSwayFinal_2 = 0
local snowSwayFinal_3 = 0
local snowScrollY_1 = 0
local snowScrollY_2 = 0
local snowScrollY_3 = 0

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
    tx["Snow1.png"] = TEXTURE:CreateTextureSync("Snow1.png")
    tx["Snow2.png"] = TEXTURE:CreateTextureSync("Snow2.png")
    tx["Snow3.png"] = TEXTURE:CreateTextureSync("Snow3.png")
end

function update(timestamp, state)
    bgScrollX_1 = bgScrollX_1 + (59.1 * fps.deltaTime)
    if bgScrollX_1 > bgWidth_1 then
        bgScrollX_1 = 0
    end

    bgScrollX_4 = bgScrollX_4 + (100 * fps.deltaTime)
    if bgScrollX_4 > bgWidth_4 then
        bgScrollX_4 = 0
    end

    snowSway = snowSway + (50 * fps.deltaTime)

    snowSwayFinal_1 = 30 * math.sin((2 * snowSway) / 150)
    snowSwayFinal_2 = 25 * math.sin((2 * snowSway) / 150)
    snowSwayFinal_3 = 20 * math.sin((2 * snowSway) / 150)

    snowScrollY_1 = snowScrollY_1 + (60 * fps.deltaTime)
    if snowScrollY_1 > snowHeight then
        snowScrollY_1 = 0
    end
    snowScrollY_2 = snowScrollY_2 + (75 * fps.deltaTime)
    if snowScrollY_2 > snowHeight then
        snowScrollY_2 = 0
    end
    snowScrollY_3 = snowScrollY_3 + (90 * fps.deltaTime)
    if snowScrollY_3 > snowHeight then
        snowScrollY_3 = 0
    end
end

function draw(state)
    tx["Skybox.png"]:Draw(0, 0)
    for i = 0, 4 do
        tx["Clouds.png"]:Draw((i * bgWidth_1) - bgScrollX_1, 0)
    end
    tx["Dojo.png"]:Draw(0, 0)
    tx["Bush.png"]:Draw(0, 0)
    for i = 0, 4 do
        for j = 0, 2 do
            tx["Snow1.png"]:Draw((i * bgWidth_4) - snowSwayFinal_1 + -300, snowScrollY_1 - (snowHeight * j))
            tx["Snow2.png"]:Draw((i * bgWidth_4) - snowSwayFinal_2 + -300, snowScrollY_2 - (snowHeight * j))
            tx["Snow3.png"]:Draw((i * bgWidth_4) - snowSwayFinal_3 + -300, snowScrollY_3 - (snowHeight * j))
        end
    end
end

function onDestroy()
    for _, t in pairs(tx) do
        if t ~= nil then t:Dispose() end
    end
    tx = {}
end