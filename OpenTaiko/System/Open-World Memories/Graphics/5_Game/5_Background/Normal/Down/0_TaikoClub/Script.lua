---@diagnostic disable: undefined-global  -- TEXTURE/fps injected by CLuaScript at runtime
-- Down background 0 (TaikoClub): a static base with a horizontally scrolling foreground band
-- that rises/bounces into view on clear (bgClearAnime), plus a clear-state fade overlay.
-- Ported from the old ScriptBG func: API to the ROActivity LuaTexture API.

local loopWidth = 1920

local bgClearFade = 0

local bgScrollX = 0
local bgClearAnime = 0

local tx = {}

function clearIn(player)
    bgClearAnime = 0
end

function clearOut(player)
end

function onStart()
    tx["Down.png"] = TEXTURE:CreateTextureSync("Down.png")
    tx["Down_Clear.png"] = TEXTURE:CreateTextureSync("Down_Clear.png")
    tx["Down_Scroll.png"] = TEXTURE:CreateTextureSync("Down_Scroll.png")
end

function update(timestamp, state)
    if state.isClear[0] then
        bgClearFade = bgClearFade + (2000 * fps.deltaTime)
    else
        bgClearFade = bgClearFade - (2000 * fps.deltaTime)
    end

    bgScrollX = bgScrollX + (250 * fps.deltaTime)
    bgClearAnime = bgClearAnime + (2 * fps.deltaTime)

    if bgClearFade > 255 then
        bgClearFade = 255
    end
    if bgClearFade < 0 then
        bgClearFade = 0
    end

    if bgScrollX > loopWidth then
        bgScrollX = 0
    end

    if bgClearAnime > 1 then
        bgClearAnime = 1
    end
end

function draw(state)
    tx["Down_Clear.png"]:SetOpacity(bgClearFade / 255)
    tx["Down_Scroll.png"]:SetOpacity(bgClearFade / 255)

    tx["Down.png"]:Draw(0, 540)
    tx["Down_Clear.png"]:Draw(0, 540)

    local moveY = 474 - bgClearAnime * 474
    moveY = moveY - (math.sin(bgClearAnime * math.pi) * 250)

    for i = 0, 2 do
        tx["Down_Scroll.png"]:Draw(0 + (loopWidth * i) - bgScrollX, 540 + moveY)
    end
end

function onDestroy()
    for _, t in pairs(tx) do
        if t ~= nil then t:Dispose() end
    end
    tx = {}
end
