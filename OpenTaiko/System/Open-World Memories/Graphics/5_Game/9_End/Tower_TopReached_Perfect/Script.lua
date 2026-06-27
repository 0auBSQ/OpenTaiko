---@diagnostic disable: undefined-global  -- TEXTURE/fps injected by CLuaScript at runtime
-- Tower top-reached (Perfect) clear animation. Group C end-anime (per-player via state.player).

local x = { 499, 499, 499, 499, 499 }
local y = { 204, 468, 0, 0, 0 }

local imageHeight = 324

local animeCounter = { 0, 0, 0, 0, 0 }
local nowFrame = { 0, 0, 0, 0, 0 }

local textureCount = 61

local tx = {}

function clearIn(player)
end

function clearOut(player)
end

function playEndAnime(player)
    animeCounter[player + 1] = 0
    nowFrame[player + 1] = 0
end

local function refreshLayout(state)
    if state.playerCount <= 2 then
        y = { 204, 468, 0, 0, 0 }
        imageHeight = 324
    elseif state.playerCount == 5 then
        y = { -71, 145, 361, 577, 793 }
        imageHeight = 288
    else
        y = { -60, 206, 469, 733, 0 }
        imageHeight = 324
    end
end

function onStart()
    tx["bg.png"] = TEXTURE:CreateTextureSync("bg.png")
    for i = 0, textureCount do
        tx[tostring(i) .. ".png"] = TEXTURE:CreateTextureSync(tostring(i) .. ".png")
    end
end

function update(timestamp, state)
    local player = state.player
    refreshLayout(state)
    animeCounter[player + 1] = animeCounter[player + 1] + (30.3 * fps.deltaTime)
    nowFrame[player + 1] = math.floor(animeCounter[player + 1] + 0.5)
end

function draw(state)
    local player = state.player
    -- `useExtraAnime` is never set by the host (nil), so not(useExtraAnime) is always true — preserved verbatim.
    if nowFrame[player + 1] <= 20 or not (useExtraAnime) then
        tx[tostring(math.min(nowFrame[player + 1], textureCount)) .. ".png"]:DrawRect(x[player + 1] - 3, y[player + 1], 0, 0, 1426, imageHeight)
    end
end

function onDestroy()
    for _, t in pairs(tx) do
        if t ~= nil then t:Dispose() end
    end
    tx = {}
end