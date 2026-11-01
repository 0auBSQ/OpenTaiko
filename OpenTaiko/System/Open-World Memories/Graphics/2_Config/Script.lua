---@diagnostic disable: undefined-global  -- TEXTURE/fps injected by CLuaScript at runtime
-- Config screen background: static background + header.

local tx = {}

function clearIn(player)
end

function clearOut(player)
end

function onStart()
    tx["Background.png"] = TEXTURE:CreateTextureSync("Background.png")
    tx["Header.png"] = TEXTURE:CreateTextureSync("Header.png")
end

function update(timestamp, state)
end

function draw(state)
    tx["Background.png"]:Draw(0, 0)
    tx["Header.png"]:Draw(0, 0)
end

function onDestroy()
    for _, t in pairs(tx) do
        if t ~= nil then t:Dispose() end
    end
    tx = {}
end