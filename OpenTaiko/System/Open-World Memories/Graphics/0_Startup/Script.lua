---@diagnostic disable: undefined-global  -- TEXTURE/fps injected by CLuaScript at runtime
-- Startup/boot loading background: static background + a small per-character loading animation.

local loadingAnimeType = 0

local currentTime = 0
local frames = { 9, 6 }
local selectedChara = 1

local tx = {}

function clearIn(player)
end

function clearOut(player)
end

function onStart()
    tx["Background.png"] = TEXTURE:CreateTextureSync("Background.png")

    local seed = os.time()
    math.randomseed(seed)
    selectedChara = math.random(2)

    if loadingAnimeType == 0 then
        for i = 0, frames[selectedChara] - 1 do
            tx["Loading/" .. tostring(selectedChara) .. "/" .. tostring(i) .. ".png"] = TEXTURE:CreateTextureSync("Loading/" .. tostring(selectedChara) .. "/" .. tostring(i) .. ".png")
        end
    elseif loadingAnimeType == 1 then
    end
end

function update(timestamp, state)
    if loadingAnimeType == 0 then
        currentTime = (currentTime + fps.deltaTime)
    elseif loadingAnimeType == 1 then
    end
end

function draw(state)
    tx["Background.png"]:Draw(0, 0)
    if loadingAnimeType == 0 then
        tx["Loading/" .. tostring(selectedChara) .. "/" .. tostring(math.floor(currentTime * 10) % frames[selectedChara]) .. ".png"]:Draw(1657, 821)
    elseif loadingAnimeType == 1 then
    end
end

function onDestroy()
    for _, t in pairs(tx) do
        if t ~= nil then t:Dispose() end
    end
    tx = {}
end