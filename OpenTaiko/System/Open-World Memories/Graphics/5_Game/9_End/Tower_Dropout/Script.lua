---@diagnostic disable: undefined-global  -- TEXTURE/fps injected by CLuaScript at runtime
-- Tower dropout (fail) animation: slide-in then fall. Group C end-anime (per-player via state.player).

local y = { 218, 482, 0, 0, 0 }

local animeCounter = { 0, 0, 0, 0, 0 }
local nowFrame = { 0, 0, 0, 0, 0 }

local slideInFrame = { 0, 0, 0, 0, 0 }
local fallFrame = { 0, 0, 0, 0, 0 }
local slideToFallFrameCount = 5
local textureCount = 32

local frameRate = 24

local tx = {}

function playEndAnime(player)
    animeCounter[player + 1] = 0
    nowFrame[player + 1] = 0
end

local function refreshLayout(state)
    if state.playerCount <= 2 then
        y = { 216, 480, 0, 0, 0 }
    elseif state.playerCount == 5 then
        y = { -5, 196, 412, 628, 844 }
    else
        y = { -24, 240, 504, 768, 0 }
    end
end

function onStart()
    for i = 0, textureCount do
        tx[tostring(i) .. ".png"] = TEXTURE:CreateTextureSync(tostring(i) .. ".png")
    end
end

function update(timestamp, state)
    local player = state.player
    refreshLayout(state)
    animeCounter[player + 1] = animeCounter[player + 1] + fps.deltaTime
    slideInFrame[player + 1] = math.min(math.floor(animeCounter[player + 1] * frameRate), 23)
    fallFrame[player + 1] = math.max(math.min(math.floor(((animeCounter[player + 1] - 1.133) * frameRate) + slideToFallFrameCount), 9), 0)
    nowFrame[player + 1] = slideInFrame[player + 1] + fallFrame[player + 1]
end

function draw(state)
    local player = state.player
    tx[tostring(nowFrame[player + 1]) .. ".png"]:Draw(500, y[player + 1] - 10)
end

function onDestroy()
    for _, t in pairs(tx) do
        if t ~= nil then t:Dispose() end
    end
    tx = {}
end