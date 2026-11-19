---@diagnostic disable: undefined-global  -- TEXTURE/fps injected by CLuaScript at runtime
-- down background: OutFox

-- Please note that the Project OutFox branding or graphics included are not permitted
-- for use outside of OpenTaiko without permission from the Project OutFox Developers.

-- The graphics are included as part of the OpenTaiko Team Collaboration to promote
-- the use of Taiko and other games to a wider audience.

local effectLoopHeight = 544

local bgClearFade = 0

local effectScrollY = 0

local tx = {}

function clearIn(player)
end

function clearOut(player)
end

function onStart()
    -- p1IsBlue unknown before draw: load both
    tx["Down_Red.png"] = TEXTURE:CreateTextureSync("Down_Red.png")
    tx["Down_Blue.png"] = TEXTURE:CreateTextureSync("Down_Blue.png")
    tx["Down_Clear.png"] = TEXTURE:CreateTextureSync("Down_Clear.png")
    tx["Tile.png"] = TEXTURE:CreateTextureSync("Tile.png")
end

function update(timestamp, state)

    if state.isClear[0] then
        bgClearFade = bgClearFade + (2000 * fps.deltaTime)
    else
        bgClearFade = bgClearFade + (-2000 * fps.deltaTime)
    end

    if not state.simplemode then
        effectScrollY = effectScrollY + (30 * fps.deltaTime)
    end

    if bgClearFade > 255 then
        bgClearFade = 255
    end
    if bgClearFade < 0 then
        bgClearFade = 0
    end

end

function draw(state)
    local bgPath = "Down_Red.png"
    if state.p1IsBlue then
        bgPath = "Down_Blue.png"
    end

    tx["Down_Clear.png"]:SetOpacity(bgClearFade / 255)
    tx["Tile.png"]:SetBlendMode("add")

    tx[bgPath]:DrawRect(0, 540, 0, 0, 1920, effectLoopHeight)

    tx["Down_Clear.png"]:DrawRect(0, 540, 0, 0, 1920, effectLoopHeight)

    tx["Tile.png"]:DrawRect(0, 540, 0, effectScrollY, 1920, effectLoopHeight)
end

function onDestroy()
    for _, t in pairs(tx) do
        if t ~= nil then t:Dispose() end
    end
    tx = {}
end
