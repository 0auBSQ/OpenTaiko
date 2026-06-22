---@diagnostic disable: undefined-global  -- TEXTURE/fps injected by CLuaScript at runtime
-- Tower up background 0: scrolling day/night sky (3 parallax layers) that darkens as the climb progresses.
-- state.towerNightNum (0..1) drives the night fade + a brightness tint on the parallax layers.

local bgWidth = 1920
local bgHeight = 276
local bgScrollX_1 = 0
local bgScrollX_2 = 0
local bgScrollX_3 = 0

local tx = {}

function clearIn(player)
end

function clearOut(player)
end

function onStart()
    tx["day.png"] = TEXTURE:CreateTextureSync("day.png")
    tx["night.png"] = TEXTURE:CreateTextureSync("night.png")
    tx["1.png"] = TEXTURE:CreateTextureSync("1.png")
    tx["2.png"] = TEXTURE:CreateTextureSync("2.png")
    tx["3.png"] = TEXTURE:CreateTextureSync("3.png")
end

function update(timestamp, state)
    bgScrollX_1 = bgScrollX_1 + (88.65 * fps.deltaTime)
    bgScrollX_2 = bgScrollX_2 + (68.85 * fps.deltaTime)
    bgScrollX_3 = bgScrollX_3 + (65.7 * fps.deltaTime)
end

function draw(state)
    tx["day.png"]:Draw(0, 0)
    tx["night.png"]:SetOpacity(state.towerNightNum)   -- old: SetOpacity(towerNightNum * 255) on the 0-255 API
    tx["night.png"]:Draw(0, 0)

    local colorTmp = 0.5 + (1 - state.towerNightNum) * 0.5

    tx["1.png"]:SetColor(colorTmp, colorTmp, colorTmp)   -- 0-1 float overload (brightness tint)
    tx["2.png"]:SetColor(colorTmp, colorTmp, colorTmp)
    tx["3.png"]:SetColor(colorTmp, colorTmp, colorTmp)

    tx["1.png"]:SetOpacity(colorTmp)
    tx["2.png"]:SetOpacity(colorTmp)
    tx["3.png"]:SetOpacity(colorTmp)

    tx["1.png"]:DrawRect(0, 0, bgScrollX_1, 0, bgWidth, bgHeight)
    tx["2.png"]:DrawRect(0, 0, bgScrollX_2, 0, bgWidth, bgHeight)
    tx["2.png"]:DrawRect(0, 0, bgScrollX_3, 0, bgWidth, bgHeight)   -- original draws 2.png (not 3.png) here — preserved
end

function onDestroy()
    for _, t in pairs(tx) do
        if t ~= nil then t:Dispose() end
    end
    tx = {}
end