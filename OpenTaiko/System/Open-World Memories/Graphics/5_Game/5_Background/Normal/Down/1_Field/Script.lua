---@diagnostic disable: undefined-global  -- TEXTURE/fps injected by CLuaScript at runtime
-- Normal down background 1 (Field): a static bottom field image with a clear-fade overlay
-- driven by clearIn/clearOut (clearMultiplier toggles the fade direction).
-- Ported from the old ScriptBG func: API to the ROActivity LuaTexture API.

local bgClearFade = 0
local clearMultiplier = -1

local tx = {}

function clearIn(player)
    clearMultiplier = 1
end

function clearOut(player)
    clearMultiplier = -1
end

function onStart()
    tx["Down.png"] = TEXTURE:CreateTextureSync("Down.png")
    tx["Down_Clear.png"] = TEXTURE:CreateTextureSync("Down_Clear.png")

    clearMultiplier = -1
end

function update(timestamp, state)
    bgClearFade = bgClearFade + (clearMultiplier * 2000 * fps.deltaTime)

    if bgClearFade > 255 then
        bgClearFade = 255
    end
    if bgClearFade < 0 then
        bgClearFade = 0
    end
end

function draw(state)
    tx["Down_Clear.png"]:SetOpacity(bgClearFade / 255)

    tx["Down.png"]:Draw(0, 540)

    if bgClearFade > 0 then
        tx["Down_Clear.png"]:Draw(0, 540)
    end
end

function onDestroy()
    for _, t in pairs(tx) do
        if t ~= nil then t:Dispose() end
    end
    tx = {}
end
