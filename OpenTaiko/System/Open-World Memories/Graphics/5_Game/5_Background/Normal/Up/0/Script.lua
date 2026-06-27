---@diagnostic disable: undefined-global  -- TEXTURE/fps injected by CLuaScript at runtime
-- Up background 0: scrolling dots + bouncing dango, with a per-player clear fade.
-- Ported from the old ScriptBG func: API to the ROActivity LuaTexture API.
--   func:AddGraph        -> TEXTURE:CreateTextureSync into the local `tx` registry (onStart)
--   func:DrawRectGraph   -> tx[name]:DrawRect              (Repeat wrap → identical tiling)
--   func:SetScale/Opacity-> tx[name]:SetScale / SetOpacity (0-255 → 0-1)
--   prelude globals      -> the `state` table passed to update/draw; deltaTime -> fps.deltaTime

local loopWidth = 1920
local loopHeight = 288
local secondPlayerY = 804

local tx = {}            -- name -> LuaTexture
local bgScrollX = 0
local bgScrollY = 0
local dangoCounter = 0
local bgClearFade = { 0, 0 }

local function updateClearFade(state)
    for player = 0, state.playerCount - 1 do
        if state.isClear[player] then
            bgClearFade[player + 1] = bgClearFade[player + 1] + (2000 * fps.deltaTime)
        else
            bgClearFade[player + 1] = bgClearFade[player + 1] - (2000 * fps.deltaTime)
        end

        if bgClearFade[player + 1] > 255 then
            bgClearFade[player + 1] = 255
        end
        if bgClearFade[player + 1] < 0 then
            bgClearFade[player + 1] = 0
        end
    end
end

function clearIn(player)
end

function clearOut(player)
end

function onStart()
    tx["BG_Red.png"] = TEXTURE:CreateTextureSync("BG_Red.png")
    tx["BG_Blue.png"] = TEXTURE:CreateTextureSync("BG_Blue.png")
    tx["BG_Clear.png"] = TEXTURE:CreateTextureSync("BG_Clear.png")

    tx["Dot_Red.png"] = TEXTURE:CreateTextureSync("Dot_Red.png")
    tx["Dot_Blue.png"] = TEXTURE:CreateTextureSync("Dot_Blue.png")
    tx["Dot_Clear.png"] = TEXTURE:CreateTextureSync("Dot_Clear.png")

    tx["Dango_Red.png"] = TEXTURE:CreateTextureSync("Dango_Red.png")
    tx["Dango_Blue.png"] = TEXTURE:CreateTextureSync("Dango_Blue.png")
    tx["Dango_Clear.png"] = TEXTURE:CreateTextureSync("Dango_Clear.png")
end

function update(timestamp, state)
    bgScrollX = bgScrollX + (59 * fps.deltaTime)
    bgScrollY = bgScrollY + (14 * fps.deltaTime)
    dangoCounter = dangoCounter + fps.deltaTime

    if dangoCounter > 3 then
        dangoCounter = 0
    end

    -- don't bother if player count is 3 or higher, use clear BG for 3P instead
    if state.playerCount < 3 then
        updateClearFade(state)
    end
end

function draw(state)
    -- dango effects
    local moveY = math.sin(math.min(dangoCounter, 1) * math.pi) * 30
    local moveY2 = math.sin(math.min(math.max(((dangoCounter - 1) * 4), 0), 1) * math.pi) / 12.0
    local moveY3 = 274 * moveY2
    local dangoOffset = -moveY + moveY3 - 36

    tx["Dango_Red.png"]:SetScale(1, 1.0 - moveY2)
    tx["Dango_Blue.png"]:SetScale(1, 1.0 - moveY2)
    tx["Dango_Clear.png"]:SetScale(1, 1.0 - moveY2)

    -- draw the stuff
    if state.playerCount == 1 and state.p1IsBlue == false then

        tx["BG_Clear.png"]:SetOpacity(bgClearFade[1] / 255)
        tx["Dot_Clear.png"]:SetOpacity(bgClearFade[1] / 255)
        tx["Dango_Clear.png"]:SetOpacity(bgClearFade[1] / 255)

        tx["BG_Red.png"]:DrawRect(0, 0, 0, 0, loopWidth, loopHeight)
        tx["Dot_Red.png"]:DrawRect(0, 0, bgScrollX, bgScrollY, loopWidth, loopHeight)
        tx["Dango_Red.png"]:DrawRect(0, dangoOffset, bgScrollX, 0, loopWidth, loopHeight)

        tx["BG_Clear.png"]:DrawRect(0, 0, 0, 0, loopWidth, loopHeight)
        tx["Dot_Clear.png"]:DrawRect(0, 0, bgScrollX, bgScrollY, loopWidth, loopHeight)
        tx["Dango_Clear.png"]:DrawRect(0, dangoOffset, bgScrollX, 0, loopWidth, loopHeight)

    elseif state.playerCount == 1 and state.p1IsBlue == true then

        tx["BG_Clear.png"]:SetOpacity(bgClearFade[1] / 255)
        tx["Dot_Clear.png"]:SetOpacity(bgClearFade[1] / 255)
        tx["Dango_Clear.png"]:SetOpacity(bgClearFade[1] / 255)

        tx["BG_Blue.png"]:DrawRect(0, 0, 0, 0, loopWidth, loopHeight)
        tx["Dot_Blue.png"]:DrawRect(0, 0, bgScrollX, bgScrollY, loopWidth, loopHeight)
        tx["Dango_Blue.png"]:DrawRect(0, dangoOffset, bgScrollX, 0, loopWidth, loopHeight)

        tx["BG_Clear.png"]:DrawRect(0, 0, 0, 0, loopWidth, loopHeight)
        tx["Dot_Clear.png"]:DrawRect(0, 0, bgScrollX, bgScrollY, loopWidth, loopHeight)
        tx["Dango_Clear.png"]:DrawRect(0, dangoOffset, bgScrollX, 0, loopWidth, loopHeight)

    elseif state.playerCount == 2 then

        tx["BG_Red.png"]:DrawRect(0, 0, 0, 0, loopWidth, loopHeight)
        tx["BG_Blue.png"]:DrawRect(0, secondPlayerY, 0, 0, loopWidth, loopHeight)
        tx["Dot_Red.png"]:DrawRect(0, 0, bgScrollX, bgScrollY, loopWidth, loopHeight)
        tx["Dot_Blue.png"]:DrawRect(0, secondPlayerY, bgScrollX, bgScrollY, loopWidth, loopHeight)
        tx["Dango_Red.png"]:DrawRect(0, dangoOffset, bgScrollX, 0, loopWidth, loopHeight)
        tx["Dango_Blue.png"]:DrawRect(0, secondPlayerY + dangoOffset, bgScrollX, 0, loopWidth, loopHeight)

        for player = 0, 1 do
            tx["BG_Clear.png"]:SetOpacity(bgClearFade[player + 1] / 255)
            tx["Dot_Clear.png"]:SetOpacity(bgClearFade[player + 1] / 255)
            tx["Dango_Clear.png"]:SetOpacity(bgClearFade[player + 1] / 255)

            tx["BG_Clear.png"]:DrawRect(0, player * secondPlayerY, 0, 0, loopWidth, loopHeight)
            tx["Dot_Clear.png"]:DrawRect(0, player * secondPlayerY, bgScrollX, bgScrollY, loopWidth, loopHeight)
            tx["Dango_Clear.png"]:DrawRect(0, (player * secondPlayerY) + dangoOffset, bgScrollX, 0, loopWidth, loopHeight)
        end

    elseif state.playerCount == 3 then

        tx["BG_Clear.png"]:DrawRect(0, 0, 0, 0, loopWidth, 1920)
        tx["Dot_Clear.png"]:DrawRect(0, 0, bgScrollX, bgScrollY, loopWidth, 1920)
        tx["Dango_Clear.png"]:DrawRect(0, secondPlayerY + dangoOffset, bgScrollX, 0, loopWidth, loopHeight)

    elseif state.playerCount == 4 then

        tx["BG_Clear.png"]:DrawRect(0, 0, 0, 0, loopWidth, 1920)
        tx["Dot_Clear.png"]:DrawRect(0, 0, bgScrollX, bgScrollY, loopWidth, 1920)

    end
end

function onDestroy()
    for _, t in pairs(tx) do
        if t ~= nil then t:Dispose() end
    end
    tx = {}
end