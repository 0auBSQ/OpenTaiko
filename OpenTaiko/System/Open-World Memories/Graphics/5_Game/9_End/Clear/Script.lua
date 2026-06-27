---@diagnostic disable: undefined-global  -- TEXTURE/fps injected by CLuaScript at runtime
-- Clear end animation: a per-player sprite-sheet "CLEAR!" anime played on song clear.
-- Group C clear/end script: update/draw receive the player via state.player; playEndAnime(player)
-- is called directly by the host with the player index and (re)starts that player's counters.

local y = { 216, 480, 0, 0, 0 }

local textureCount = 23
local w = 653
local h = 327

local animeCounter = { 0, 0, 0, 0, 0 }
local nowFrame = { 0, 0, 0, 0, 0 }

local tx = {}

-- The old init() chose the per-player Y layout from playerCount; onStart can't see state,
-- so the layout is (re)selected from state.playerCount each update instead.
local function updateLayout(playerCount)
    if playerCount <= 2 then
        y = { 216, 480, 0, 0, 0 }
    elseif playerCount == 5 then
        y = { -5, 196, 412, 628, 844 }
    else
        y = { -24, 240, 504, 768, 0 }
    end
end

function playEndAnime(player)
    animeCounter[player + 1] = 0
    nowFrame[player + 1] = 0
end

function onStart()
    tx["Assets.png"] = TEXTURE:CreateTextureSync("Assets.png")
end

function update(timestamp, state)
    local player = state.player

    updateLayout(state.playerCount)

    animeCounter[player + 1] = animeCounter[player + 1] + (45.4 * fps.deltaTime)
    nowFrame[player + 1] = math.min(math.floor(animeCounter[player + 1] + 0.5), textureCount)
end

function draw(state)
    local player = state.player

    -- originally x = 500
    tx["Assets.png"]:DrawRect(883.5, y[player + 1] + 4.5, w * (math.floor(nowFrame[player + 1] / 8)), h * (math.floor(nowFrame[player + 1] % 8)), w, h)
end

function onDestroy()
    for _, t in pairs(tx) do
        if t ~= nil then t:Dispose() end
    end
    tx = {}
end
