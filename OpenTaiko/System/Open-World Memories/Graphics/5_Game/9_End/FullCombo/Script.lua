---@diagnostic disable: undefined-global  -- TEXTURE/fps injected by CLuaScript at runtime
-- FullCombo end animation (Group C): plays a 40-frame (0..39) sprite sheet per player on full combo.
-- Ported from the old ScriptBG func: API to the ROActivity LuaTexture API.
--   func:AddGraph    -> TEXTURE:CreateTextureSync into the local `tx` registry (onStart, all frames)
--   func:DrawGraph   -> tx[name]:Draw
--   update/draw/playEndAnime took a PLAYER index; now the host passes it via state.player
--   (playEndAnime is still called directly with the index). deltaTime -> fps.deltaTime.

local y = { 202, 466, 0, 0, 0 }

local animeCounter = { 0, 0, 0, 0, 0 }
local nowFrame = { 0, 0, 0, 0, 0 }

local textureCount = 39

local tx = {}

function playEndAnime(player)
    animeCounter[player + 1] = 0
    nowFrame[player + 1] = 0
end

function onStart()
    -- Load every frame up front; onStart has no `state`, so the player-count-dependent
    -- `y` layout is (re)computed in update() where state.playerCount is available.
    for i = 0, textureCount do
        tx[tostring(i) .. ".png"] = TEXTURE:CreateTextureSync(tostring(i) .. ".png")
    end
end

function update(timestamp, state)
    local player = state.player

    -- old init() seeded `y` from playerCount; playerCount only arrives via state, so do it here
    if state.playerCount <= 2 then
        y = { 202, 466, 0, 0, 0 }
    elseif state.playerCount == 5 then
        y = { -50, 166, 382, 598, 814 }
    else
        y = { -17, 247, 511, 775, 0 }
    end

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
