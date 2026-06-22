---@diagnostic disable: undefined-global  -- TEXTURE/fps injected by CLuaScript at runtime
-- End "ClearFailed" anime (Group C): per-player slide-in + fall sprite sequence (0..32.png), plus a
-- TemplateMoyai drop when every player has failed. Ported from the old ScriptBG func: API to the
-- ROActivity LuaTexture API. update/draw/playEndAnime take the player via the state table / index.

local y = { 218, 482, 0, 0, 0 }

local animeCounter = { 0, 0, 0, 0, 0 }
local nowFrame = { 0, 0, 0, 0, 0 }

local slideInFrame = { 0, 0, 0, 0, 0 }
local fallFrame = { 0, 0, 0, 0, 0 }
local slideToFallFrameCount = 5
local textureCount = 32

local frameRate = 24

local allfailed = false
local failed = { false, false, false, false, false }

local templateMoyai_y = -1080

local tx = {}            -- name -> LuaTexture

function playEndAnime(player)
    animeCounter[player + 1] = 0
    nowFrame[player + 1] = 0
    templateMoyai_y = -1080

    -- must failed if reach here
    failed[player + 1] = true
    allfailed = true
    for i = 1, 5 do
        if not failed[i] then
            allfailed = false
            break
        end
    end
end

function onStart()
    -- Load every frame sprite (0.png .. 32.png inclusive) + the all-failed Moyai up front.
    for i = 0, textureCount do
        tx[tostring(i) .. ".png"] = TEXTURE:CreateTextureSync(tostring(i) .. ".png")
    end

    tx["TemplateMoyai.png"] = TEXTURE:CreateTextureSync("TemplateMoyai.png")
end

function update(timestamp, state)
    local player = state.player

    -- y layout depends on player count (was seeded in the old init()); state has it here.
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

    -- only descend if all-failed
    if player == 0 then
        for i = 1, 5 do
            if not (animeCounter[i] > 1.1) then
                return
            end
        end
        templateMoyai_y = math.min(templateMoyai_y + (fps.deltaTime * 5120), 0)
    end
end

function draw(state)
    local player = state.player

    tx[tostring(nowFrame[player + 1]) .. ".png"]:Draw(500, y[player + 1] - 10)

    if allfailed and state.playerCount > 2 then
        tx["TemplateMoyai.png"]:Draw(0, templateMoyai_y)
    end
end

function onDestroy()
    for _, t in pairs(tx) do
        if t ~= nil then t:Dispose() end
    end
    tx = {}
end
