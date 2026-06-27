---@diagnostic disable: undefined-global  -- TEXTURE/fps injected by CLuaScript at runtime
-- AllPerfect end animation: a 62-frame (0.png..61.png) per-player clear flourish.
-- Ported from the old ScriptBG func: API to the ROActivity LuaTexture API. Group C (clear/end anim):
-- update/draw take (timestamp,state)/(state) with the player index in state.player; playEndAnime(player)
-- stays a top-level hook (the host calls it with the index) and re-seeds the per-play frame counters.

local x = { 499, 499, 499, 499, 499 }
local y = { 204, 468, 0, 0, 0 }

local imageHeight = 324

local animeCounter = { 0, 0, 0, 0, 0 }
local nowFrame = { 0, 0, 0, 0, 0 }

local textureCount = 61

local tx = {}            -- name -> LuaTexture

-- Layout (y offsets + frame height) depends on playerCount, which the old init() read once. onStart runs
-- before state arrives, so we (re)derive it per-play from state.playerCount instead — identical result.
local function refreshLayout(state)
    if state.playerCount <= 2 then
        y = { 204, 468, 0, 0, 0 }
    elseif state.playerCount == 5 then
        y = { -71, 145, 361, 577, 793 }
        imageHeight = 288
    else
        y = { -60, 206, 469, 733, 0 }
    end
end

function clearIn(player)
end

function clearOut(player)
end

function playEndAnime(player)
    animeCounter[player + 1] = 0
    nowFrame[player + 1] = 0
end

function onStart()
    -- Load every frame up front (unconditional — onStart runs before state). The old init() forced the same sync load.
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

    -- useExtraAnime was never set by the host in the old API (a nil global), so not(useExtraAnime) is always
    -- true here → the frame is always drawn. Preserved verbatim as a quirk.
    if nowFrame[player + 1] <= 20 or not (useExtraAnime) then
        --func:DrawGraph(x[player + 1] - 3, y[player + 1], tostring(math.min(nowFrame[player + 1], textureCount))..".png")
        tx[tostring(math.min(nowFrame[player + 1], textureCount)) .. ".png"]:DrawRect(x[player + 1] - 3, y[player + 1], 0, 0, 1426, imageHeight)
    end
end

function onDestroy()
    for _, t in pairs(tx) do
        if t ~= nil then t:Dispose() end
    end
    tx = {}
end