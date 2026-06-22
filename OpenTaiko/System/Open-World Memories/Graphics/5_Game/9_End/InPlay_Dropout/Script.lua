---@diagnostic disable: undefined-global  -- TEXTURE/fps injected by CLuaScript at runtime
-- End animation "InPlay_Dropout": per-player slide-in-then-fall frame animation (numbered .png frames).
-- Ported from the old ScriptBG func: API to the ROActivity LuaTexture API.
--   init -> onStart (texture loads); per-play state arrives via the `state` table.
--   The old update(player)/draw(player) took a player index; now the host passes it as state.player.
--   playEndAnime(player) stays a top-level fn taking the index directly (host calls it to (re)start a play).

local y = { 218, 482, 0, 0, 0 }

local animeCounter = { 0, 0, 0, 0, 0 }
local nowFrame = { 0, 0, 0, 0, 0 }

local slideInFrame = { 0, 0, 0, 0, 0 }
local fallFrame = { 0, 0, 0, 0, 0 }
local slideToFallFrameCount = 5
local textureCount = 1

local frameRate = 24

local tx = {}

function playEndAnime(player)
    animeCounter[player + 1] = 0
    nowFrame[player + 1] = 0
end

function onStart()
    -- Load every frame up front (onStart runs before `state` arrives — don't gate on player count).
    for i = 0, textureCount do
        tx[tostring(i) .. ".png"] = TEXTURE:CreateTextureSync(tostring(i) .. ".png")
    end
end

function update(timestamp, state)
    local player = state.player

    -- old init() set the row layout from playerCount; per-play value lives in state now.
    if state.playerCount <= 2 then
        y = { 216, 480, 0, 0, 0 }
    elseif state.playerCount == 5 then
        y = { -5, 196, 412, 628, 844 }
    else
        y = { -24, 240, 504, 768, 0 }
    end

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