---@diagnostic disable: undefined-global  -- TEXTURE/fps injected by CLuaScript at runtime
-- Tower top-reached (Full Combo) clear animation. Group C end-anime (per-player via state.player).

local y = { 202, 466, 0, 0, 0 }

local animeCounter = { 0, 0, 0, 0, 0 }
local nowFrame = { 0, 0, 0, 0, 0 }

local textureCount = 39

local tx = {}

function playEndAnime(player)
    animeCounter[player + 1] = 0
    nowFrame[player + 1] = 0
end

local function refreshLayout(state)
    if state.playerCount <= 2 then
        y = { 202, 466, 0, 0, 0 }
    elseif state.playerCount == 5 then
        y = { -50, 166, 382, 598, 814 }
    else
        y = { -17, 247, 511, 775, 0 }
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
    animeCounter[player + 1] = animeCounter[player + 1] + (45.4 * fps.deltaTime)
    nowFrame[player + 1] = math.min(math.floor(animeCounter[player + 1] + 0.5), textureCount)
end

function draw(state)
    local player = state.player
    tx[tostring(nowFrame[player + 1]) .. ".png"]:Draw(500, y[player + 1])
end

function onDestroy()
    for _, t in pairs(tx) do
        if t ~= nil then t:Dispose() end
    end
    tx = {}
end