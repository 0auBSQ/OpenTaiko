---@diagnostic disable: undefined-global  -- TEXTURE/fps injected by CLuaScript at runtime
-- Up background 8: a single BG tile + two horizontally-scrolling ribbon layers, per player row.
-- Ported from the old ScriptBG func: API to the ROActivity LuaTexture API.
--   func:AddGraph      -> TEXTURE:CreateTextureSync into the local `tx` registry (onStart)
--   func:DrawGraph     -> tx[name]:Draw  ;  func:DrawRectGraph -> tx[name]:DrawRect
--   prelude globals    -> the `state` table passed to update/draw; deltaTime -> fps.deltaTime
-- NOTE: original init() computed maxPlayerLoop from the global playerCount; since onStart runs before
-- state is available, that player-count clamp is moved into draw() (uses state.playerCount) instead.

local ribbon0LoopWidth = 1566
local ribbon1LoopWidth = 1566

local ribbon0ScrollX = 0
local ribbon1ScrollX = 0

local tx = {}

function clearIn(player)
end

function clearOut(player)
end

function onStart()
    tx["BG.png"] = TEXTURE:CreateTextureSync("BG.png")
    tx["Ribbon0.png"] = TEXTURE:CreateTextureSync("Ribbon0.png")
    tx["Ribbon1.png"] = TEXTURE:CreateTextureSync("Ribbon1.png")

    -- random values to create initial depth
    ribbon0ScrollX = 250
    ribbon1ScrollX = 914
end

function update(timestamp, state)
    ribbon0ScrollX = ribbon0ScrollX + (fps.deltaTime * 59)
    if ribbon0ScrollX > ribbon0LoopWidth then
      ribbon0ScrollX = 0
    end
    ribbon1ScrollX = ribbon1ScrollX + (fps.deltaTime * 27)
    if ribbon1ScrollX > ribbon1LoopWidth then
      ribbon1ScrollX = 0
    end
end


function draw(state)
    -- avoid unnecessary draw loops for 3+ players
    local maxPlayerLoop = math.min(state.playerCount - 1, 1)

    for player = 0, maxPlayerLoop do
        local y = 0
        if player == 1 then
            y = 804
        end
        tx["BG.png"]:DrawRect(0, y, 0, 0, 1920, 288)
        for i = 0, 3 do
        tx["Ribbon0.png"]:Draw((ribbon0LoopWidth * i) - ribbon0ScrollX, y)
        tx["Ribbon1.png"]:Draw((ribbon1LoopWidth * i) - ribbon1ScrollX, y)
        end
    end
end

function onDestroy()
    for _, t in pairs(tx) do
        if t ~= nil then t:Dispose() end
    end
    tx = {}
end
