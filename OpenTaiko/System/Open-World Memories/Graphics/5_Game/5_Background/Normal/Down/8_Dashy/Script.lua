---@diagnostic disable: undefined-global  -- TEXTURE/fps injected by CLuaScript at runtime
-- Down background 8 "Dashy": static BG with two counter-rotating gears in the bottom corners.
-- Ported from the old ScriptBG func: API to the ROActivity LuaTexture API.

local rotation = 0.0

local tx = {}

function clearIn(player)
end

function clearOut(player)
end

function onStart()
    tx["BG.png"] = TEXTURE:CreateTextureSync("BG.png")
    tx["Gears.png"] = TEXTURE:CreateTextureSync("Gears.png")
end

function update(timestamp, state)
    rotation = rotation + (fps.deltaTime * 8)
end

function draw(state)
    tx["BG.png"]:Draw(0, 540)

    tx["Gears.png"]:SetRotation(-rotation)
    tx["Gears.png"]:DrawRectAtAnchor(40, 969, 0, 0, 333, 310, "center")
    tx["Gears.png"]:SetRotation(rotation)
    tx["Gears.png"]:DrawRectAtAnchor(1934, 556, 334, 0, 491, 457, "center")
end

function onDestroy()
    for _, t in pairs(tx) do
        if t ~= nil then t:Dispose() end
    end
    tx = {}
end
