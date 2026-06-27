---@diagnostic disable: undefined-global  -- TEXTURE/fps injected by CLuaScript at runtime
-- AI battle result background: win/lose backdrop + gradation overlay.

local tx = {}

function clearIn(player)
end

function clearOut(player)
end

function onStart()
    tx["Background_Win.png"] = TEXTURE:CreateTextureSync("Background_Win.png")
    tx["Background_Lose.png"] = TEXTURE:CreateTextureSync("Background_Lose.png")
    tx["Background_Gradation.png"] = TEXTURE:CreateTextureSync("Background_Gradation.png")
end

function update(timestamp, state)
end

function draw(state)
    if state.battleWin then
        tx["Background_Win.png"]:Draw(0, 0)
    else
        tx["Background_Lose.png"]:Draw(0, 0)
    end
    tx["Background_Gradation.png"]:Draw(0, 0)
end

function onDestroy()
    for _, t in pairs(tx) do
        if t ~= nil then t:Dispose() end
    end
    tx = {}
end