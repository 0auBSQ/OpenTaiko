---@diagnostic disable: undefined-global  -- TEXTURE/fps injected by CLuaScript at runtime
-- down background: Neighborhood

local downY = 540
local bgClearFade = 0
local starsGlowTime = 0
local rotation = 0

local tx = {}            -- name -> LuaTexture

function clearIn(player)
end

function clearOut(player)
end

function onStart()
    tx["Down.png"] = TEXTURE:CreateTextureSync("Down.png")
    tx["Night.png"] = TEXTURE:CreateTextureSync("Night.png")
    tx["Stars.png"] = TEXTURE:CreateTextureSync("Stars.png")
    tx["Stars.png"]:SetScale(1.2, 1.2)
end

function update(timestamp, state)
    if state.isClear[0] then
        bgClearFade = math.min(bgClearFade + (2000 * fps.deltaTime), 255)
    else
        bgClearFade = math.max(bgClearFade - (2000 * fps.deltaTime), 0)
    end

    if not state.simplemode then
        starsGlowTime = starsGlowTime + fps.deltaTime
    end

    if not state.simplemode then
        rotation = rotation + fps.deltaTime
    end
end

function draw(state)
    if bgClearFade < 255 then
        tx["Down.png"]:Draw(0, downY)
    end
    if bgClearFade > 0 then
        tx["Night.png"]:SetOpacity(bgClearFade / 255)
        tx["Stars.png"]:SetOpacity(((bgClearFade / 5) * (math.sin(starsGlowTime * 5) + 4)) / 255)
        tx["Stars.png"]:SetRotation(math.sin(rotation / 1.6) * 4)

        tx["Night.png"]:Draw(0, downY)
        tx["Stars.png"]:DrawAtAnchor(960, downY + 270, "center")
    end
end

function onDestroy()
    for _, t in pairs(tx) do
        if t ~= nil then t:Dispose() end
    end
    tx = {}
end
