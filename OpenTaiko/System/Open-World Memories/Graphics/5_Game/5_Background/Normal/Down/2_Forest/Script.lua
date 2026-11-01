---@diagnostic disable: undefined-global  -- TEXTURE/fps injected by CLuaScript at runtime
-- Ported from the old ScriptBG func: API to the ROActivity LuaTexture API.

local loopWidth = 1640

local tx = {}            -- name -> LuaTexture

local bgClearFade = 0

local bgScrollX = 0

local clearInAnime_Common = 0
local clearInAnime_Scroll = 0
local clearInAnime_Deco = 0
local clearInAnime_Left = 0
local clearInAnime_Taiko = 0
local lightCounter = 0

local taiko_rotate = 0

-- onStart runs BEFORE the script receives `state`, so the original init()'s simplemode-gated
-- star-fade seeding is deferred to the first update() via this one-shot flag.
local simpleSeeded = false

function clearIn(player)
    clearInAnime_Common = 0
    clearInAnime_Scroll = 0
    clearInAnime_Deco = -0.4
    clearInAnime_Left = -0.6
    clearInAnime_Taiko = -0.8

    taiko_rotate = 0.0
end

function clearOut(player)
end

function onStart()
    tx["Sky.png"] = TEXTURE:CreateTextureSync("Sky.png")
    tx["Tatemono.png"] = TEXTURE:CreateTextureSync("Tatemono.png")
    tx["Tyoutin.png"] = TEXTURE:CreateTextureSync("Tyoutin.png")
    tx["Tyoutin_Light.png"] = TEXTURE:CreateTextureSync("Tyoutin_Light.png")

    tx["Down_Scroll.png"] = TEXTURE:CreateTextureSync("Down_Scroll.png")
    tx["Down_Clear_Deco.png"] = TEXTURE:CreateTextureSync("Down_Clear_Deco.png")
    tx["Down_Clear_Left.png"] = TEXTURE:CreateTextureSync("Down_Clear_Left.png")
    tx["Down_Clear_Taiko.png"] = TEXTURE:CreateTextureSync("Down_Clear_Taiko.png")
end

function update(timestamp, state)
    if state.isClear[0] then
        bgClearFade = bgClearFade + (2000 * fps.deltaTime)
    else
        bgClearFade = bgClearFade - (2000 * fps.deltaTime)
    end

    lightCounter = lightCounter + (6 * fps.deltaTime)

    clearInAnime_Common = clearInAnime_Common + (1 * fps.deltaTime)
    clearInAnime_Scroll = clearInAnime_Scroll + (2 * fps.deltaTime)
    clearInAnime_Deco = clearInAnime_Deco + (2 * fps.deltaTime)
    clearInAnime_Left = clearInAnime_Left + (2 * fps.deltaTime)
    clearInAnime_Taiko = clearInAnime_Taiko + (2 * fps.deltaTime)

    if clearInAnime_Common > 1.0 then
        taiko_rotate = taiko_rotate + (45 * fps.deltaTime)
    end

    bgScrollX = bgScrollX + (100 * fps.deltaTime)
    
    if bgClearFade > 255 then
        bgClearFade = 255
    end
    if bgClearFade < 0 then
        bgClearFade = 0
    end
    
    if bgScrollX > loopWidth then
        bgScrollX = 0
    end

    if clearInAnime_Scroll > 1 then
        clearInAnime_Scroll = 1
    end

    if clearInAnime_Deco > 1 then
        clearInAnime_Deco = 1
    end
    if clearInAnime_Left > 1 then
        clearInAnime_Left = 1
    end
    if clearInAnime_Taiko > 1 then
        clearInAnime_Taiko = 1
    end
end

function draw(state)
    tx["Down_Scroll.png"]:SetOpacity(bgClearFade / 255)
    tx["Down_Clear_Deco.png"]:SetOpacity(bgClearFade / 255)
    tx["Down_Clear_Left.png"]:SetOpacity(bgClearFade / 255)
    tx["Down_Clear_Taiko.png"]:SetOpacity(bgClearFade / 255)

    tx["Sky.png"]:Draw(0, 540);
    tx["Tatemono.png"]:Draw(0, 540);
    tx["Tyoutin.png"]:Draw(0, 540);
    tx["Tyoutin_Light.png"]:SetOpacity((155 - (math.sin(lightCounter * math.pi) * 100)) / 255)
    tx["Tyoutin_Light.png"]:Draw(0, 540)
    
    for i = 0, 3 do
        tx["Down_Scroll.png"]:Draw(0 + (loopWidth * i) - bgScrollX, 540 + ((1.0 - (clearInAnime_Scroll + (math.sin(clearInAnime_Scroll * math.pi) / 2.0))) * 474))
    end

    tx["Down_Clear_Deco.png"]:Draw(0, 540 + ((1.0 - (clearInAnime_Deco + (math.sin(clearInAnime_Deco * math.pi) / 2.0))) * 474))
    tx["Down_Clear_Left.png"]:Draw(0, 540 + ((1.0 - (clearInAnime_Left + (math.sin(clearInAnime_Left * math.pi) / 2.0))) * 474))
    
    tx["Down_Clear_Taiko.png"]:SetRotation(taiko_rotate)
    tx["Down_Clear_Taiko.png"]:Draw(400 - ((1.0 - (clearInAnime_Taiko + (math.sin(clearInAnime_Taiko * math.pi) / 2.0))) * 700), 540)
    
end

function onDestroy()
    for _, t in pairs(tx) do
        if t ~= nil then t:Dispose() end
    end
    tx = {}
end
