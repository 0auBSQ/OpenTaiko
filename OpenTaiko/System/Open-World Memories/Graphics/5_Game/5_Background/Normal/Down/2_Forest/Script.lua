---@diagnostic disable: undefined-global  -- TEXTURE/fps injected by CLuaScript at runtime
-- Normal down background 2 (Forest): day sky + clear overlay (BG/FG), four twinkling star layers
-- (sin/cos opacity fades) and a parallax cloud layer, faded in globally on clear.
-- Ported from the old ScriptBG func: API to the ROActivity LuaTexture API (pixel-identical).

local loopWidth = 1920
local loopHeight = 474
local downY = 540

local tx = {}            -- name -> LuaTexture

local bgClearFade = 0

local bgScrollX = 0

local starsFadeTime = 0
local starsFade1 = 0
local starsFade2 = 0
local starsFade3 = 0
local starsFade4 = 0

-- onStart runs BEFORE the script receives `state`, so the original init()'s simplemode-gated
-- star-fade seeding is deferred to the first update() via this one-shot flag.
local simpleSeeded = false

function clearIn(player)
end

function clearOut(player)
end

function onStart()
    tx["Day.png"] = TEXTURE:CreateTextureSync("Day.png")
    tx["Clear_BG.png"] = TEXTURE:CreateTextureSync("Clear_BG.png")
    tx["Clear_FG.png"] = TEXTURE:CreateTextureSync("Clear_FG.png")
    tx["Stars_1.png"] = TEXTURE:CreateTextureSync("Stars_1.png")
    tx["Stars_2.png"] = TEXTURE:CreateTextureSync("Stars_2.png")
    tx["Stars_3.png"] = TEXTURE:CreateTextureSync("Stars_3.png")
    tx["Stars_4.png"] = TEXTURE:CreateTextureSync("Stars_4.png")
    tx["Clouds.png"] = TEXTURE:CreateTextureSync("Clouds.png")
end

function update(timestamp, state)
    -- original init() seeded these when simplemode; deferred here (state unavailable in onStart)
    if not simpleSeeded then
        if state.simplemode then
            starsFade1 = 0.83
            starsFade2 = 0.75
            starsFade3 = 0.25
            starsFade4 = 0.49
        end
        simpleSeeded = true
    end

    -- Clear fade
    if state.isClear[0] then
        bgClearFade = bgClearFade + (2000 * fps.deltaTime);
    else
        bgClearFade = bgClearFade - (2000 * fps.deltaTime);
    end

    if bgClearFade > 255 then
        bgClearFade = 255;
    end
    if bgClearFade < 0 then
        bgClearFade = 0;
    end

    if not state.simplemode then
        starsFadeTime = starsFadeTime + fps.deltaTime

        starsFade1 = 0.38 * math.sin(((5 * starsFadeTime) / 1.37)) + 0.83
        starsFade2 = 0.38 * math.cos(((5 * starsFadeTime) / 1.56)) + 0.75
        starsFade3 = 0.38 * math.sin(((5 * starsFadeTime) / 1.71)) + 0.25
        starsFade4 = 0.38 * math.cos(((5 * starsFadeTime) / 2.3)) + 0.49
    end

    -- Cloud scroll
    if not state.simplemode then
        bgScrollX = bgScrollX + (50 * fps.deltaTime);
    end
end

function draw(state)
    tx["Clear_BG.png"]:SetOpacity(bgClearFade / 255);
    tx["Clear_FG.png"]:SetOpacity(bgClearFade / 255);
    tx["Clouds.png"]:SetOpacity(bgClearFade / 255);
    tx["Stars_1.png"]:SetOpacity((bgClearFade * starsFade1) / 255);
    tx["Stars_2.png"]:SetOpacity((bgClearFade * starsFade2) / 255);
    tx["Stars_3.png"]:SetOpacity((bgClearFade * starsFade3) / 255);
    tx["Stars_4.png"]:SetOpacity((bgClearFade * starsFade4) / 255);

    tx["Day.png"]:Draw(0, downY)

    tx["Clear_BG.png"]:Draw(0, downY)
    tx["Stars_1.png"]:DrawRect(0, downY, bgScrollX * 0.35, 0, loopWidth, loopHeight)
    tx["Stars_2.png"]:DrawRect(0, downY, bgScrollX * 0.3, 0, loopWidth, loopHeight)
    tx["Stars_3.png"]:DrawRect(0, downY, bgScrollX * 0.25, 0, loopWidth, loopHeight)
    tx["Stars_4.png"]:DrawRect(0, downY, bgScrollX * 0.2, 0, loopWidth, loopHeight)
    tx["Clouds.png"]:DrawRect(0, downY, bgScrollX, 0, loopWidth, 474)
    tx["Clear_FG.png"]:Draw(0, downY)
end

function onDestroy()
    for _, t in pairs(tx) do
        if t ~= nil then t:Dispose() end
    end
    tx = {}
end