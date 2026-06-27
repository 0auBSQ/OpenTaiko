---@diagnostic disable: undefined-global  -- TEXTURE/fps injected by CLuaScript at runtime
-- Dan up background "Fall": dojo scene with scrolling clouds + three layers of swaying,
-- rotating, falling leaves. No per-player / clear branches in the original — kept as-is.

local bgWidth_1 = 1984
local bgWidth_4 = 962
local bgScrollX_1 = 0
local bgScrollX_4 = 0

local leafScrollX_1 = 0
local leafScrollX_2 = 150
local leafScrollX_3 = 300
local leafScrollY_1 = 0
local leafScrollY_2 = 200
local leafScrollY_3 = 350
local leafRot1 = 0
local leafRot2 = 0
local leafRot3 = 0
local leafSway_1 = 0
local leafSwayFinal_1 = 0
local leafSway_2 = 0
local leafSwayFinal_2 = 0
local leafSway_3 = 0
local leafSwayFinal_3 = 0

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
    tx["Leaf1.png"] = TEXTURE:CreateTextureSync("Leaf1.png")
    tx["Leaf2.png"] = TEXTURE:CreateTextureSync("Leaf2.png")
    tx["Leaf3.png"] = TEXTURE:CreateTextureSync("Leaf3.png")
end

function update(timestamp, state)
    bgScrollX_1 = bgScrollX_1 + (59.1 * fps.deltaTime)
    if bgScrollX_1 > bgWidth_1 then
        bgScrollX_1 = 0
    end

    leafScrollX_1 = leafScrollX_1 + (75 * fps.deltaTime)
    leafScrollX_2 = leafScrollX_2 + (75 * fps.deltaTime)
    leafScrollX_3 = leafScrollX_3 + (75 * fps.deltaTime)

    bgScrollX_4 = bgScrollX_4 + (100 * fps.deltaTime)
    if bgScrollX_4 > bgWidth_4 then
        bgScrollX_4 = 0
    end
    leafScrollY_1 = leafScrollY_1 + (100 * fps.deltaTime)
    if leafScrollY_1 > 500 then
        leafScrollY_1 = 0
        leafScrollX_1 = 0
    end
    leafScrollY_2 = leafScrollY_2 + (100 * fps.deltaTime)
    if leafScrollY_2 > 500 then
        leafScrollY_2 = 0
        leafScrollX_2 = 200
    end
    leafScrollY_3 = leafScrollY_3 + (100 * fps.deltaTime)
    if leafScrollY_3 > 500 then
        leafScrollY_3 = 0
        leafScrollX_3 = -150
    end

    leafSway_1 = leafSway_1 + (50 * fps.deltaTime)
    leafSwayFinal_1 = 70 * math.cos((5 * leafSway_1) / 150) * math.sin((2 * leafSway_1) / 150)
    leafSway_2 = leafSway_2 + (70 * fps.deltaTime)
    leafSwayFinal_2 = 60 * math.cos((5 * leafSway_2) / 120) * math.sin((2 * leafSway_2) / 120)
    leafSway_3 = leafSway_3 + (36 * fps.deltaTime)
    leafSwayFinal_3 = 40 * math.cos((5 * leafSway_2) / 130) * math.sin((2 * leafSway_2) / 130)  -- original uses leafSway_2 (not leafSway_3) — preserved
end


function draw(state)
    tx["Skybox.png"]:Draw(0, 0)
    for i = 0, 4 do
        tx["Clouds.png"]:Draw((i * bgWidth_1) - bgScrollX_1, 0)
    end
    tx["Dojo.png"]:Draw(0, 0)
    tx["Bush.png"]:Draw(0, 0)
    for i = 0, 4 do
        tx["Leaf1.png"]:SetRotation(leafRot1 + (i * 60))
        tx["Leaf2.png"]:SetRotation(leafRot2 + (i * 60))
        tx["Leaf3.png"]:SetRotation(leafRot3 + (i * 60))
        tx["Leaf1.png"]:Draw((i * 600) - leafScrollX_1 + leafSwayFinal_1, -70 + leafScrollY_1 + (-75 * (i % 2)))
        tx["Leaf2.png"]:Draw((i * 500) - leafScrollX_2 + leafSwayFinal_2, -70 + leafScrollY_2 + (-60 * (i % 2)))
        tx["Leaf3.png"]:Draw((i * 700) - leafScrollX_3 + leafSwayFinal_3, -70 + leafScrollY_3 + (-50 * (i % 2)))
    end
end

function onDestroy()
    for _, t in pairs(tx) do
        if t ~= nil then t:Dispose() end
    end
    tx = {}
end