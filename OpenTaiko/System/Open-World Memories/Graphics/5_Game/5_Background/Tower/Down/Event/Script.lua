---@diagnostic disable: undefined-global  -- TEXTURE/fps injected by CLuaScript at runtime
-- Tower down event background: a sky-gradient panel scrolled vertically as the climb progresses,
-- with the scroll rate driven by BPM and snapped to towerNightNum step boundaries.

local towerUpProgress = 0
local lastNightNum = 0
local skyHeight = 7434

local tx = {}

function clearIn(player)

end

function clearOut(player)

end

function onStart()
    tx["Sky_Gradient.png"] = TEXTURE:CreateTextureSync("Sky_Gradient.png")
end

function update(timestamp, state)
    towerUpProgress = towerUpProgress + ((fps.deltaTime * (state.bpm[0] / 120)) / 140);
    if towerUpProgress > 1 then
      towerUpProgress = 1
    elseif towerUpProgress > lastNightNum then
      towerUpProgress = lastNightNum
    end

    if state.towerNightNum ~= lastNightNum then
      towerUpProgress = lastNightNum
      lastNightNum = state.towerNightNum
    end

end

function draw(state)
    tx["Sky_Gradient.png"]:DrawRect(0, 540, 0, skyHeight - (towerUpProgress * skyHeight), 1920, 540);
end

function onDestroy()
    for _, t in pairs(tx) do
        if t ~= nil then t:Dispose() end
    end
    tx = {}
end
