---@diagnostic disable: undefined-global  -- TEXTURE/fps injected by CLuaScript at runtime
-- Up background 1: scrolling layered sky (1st/3rd) with a bobbing 3rd layer + a rotated, looping "Clear_Up_3rd"
-- diagonal sweep, plus a per-player clear fade. Ported from the old ScriptBG func: API to the ROActivity LuaTexture API.

local loopWidth = 492
local loopHeight = 288

local bgScrollX = 0
local bgScrollX2 = 0
local bgClearFade = { 0, 0, 0, 0, 0 }

local animeCounter1 = 0
local animeCounter2 = 0

local tx = {}            -- name -> LuaTexture

function drawUp3rd_c(x, y, player)

    local move = (animeCounter2 - 0.5) * 800

    if animeCounter2 < 1 then
        tx["Clear_Up_3rd.png"]:Draw(x - move, y + move)
    end
    tx["Clear_Up_3rd.png"]:SetRotation(45)
end

function clearIn(player)
end

function clearOut(player)
end

function onStart()
    tx["1P_Up_1st.png"] = TEXTURE:CreateTextureSync("1P_Up_1st.png")
    tx["2P_Up_1st.png"] = TEXTURE:CreateTextureSync("2P_Up_1st.png")
    tx["1P_Up_3rd.png"] = TEXTURE:CreateTextureSync("1P_Up_3rd.png")
    tx["2P_Up_3rd.png"] = TEXTURE:CreateTextureSync("2P_Up_3rd.png")
    tx["Clear_Up_1st.png"] = TEXTURE:CreateTextureSync("Clear_Up_1st.png")
    tx["Clear_Up_2nd.png"] = TEXTURE:CreateTextureSync("Clear_Up_2nd.png")
    tx["Clear_Up_3rd.png"] = TEXTURE:CreateTextureSync("Clear_Up_3rd.png")
end

function update(timestamp, state)
    bgScrollX = bgScrollX - (59 * fps.deltaTime)
    bgScrollX2 = bgScrollX2 - (100 * fps.deltaTime)
    animeCounter1 = animeCounter1 + (0.5 * fps.deltaTime)
    animeCounter2 = animeCounter2 + (0.5 * fps.deltaTime)


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

    if bgScrollX2 < -loopWidth * 2 then
        bgScrollX2 = 0
    end

    if animeCounter2 > 2 then
        animeCounter2 = 0
    end
end


function draw(state)
    if state.playerCount == 1 and state.p1IsBlue == false then

        tx["Clear_Up_1st.png"]:SetOpacity(bgClearFade[1] / 255)
        tx["Clear_Up_2nd.png"]:SetOpacity(bgClearFade[1] / 255)
        tx["Clear_Up_3rd.png"]:SetOpacity(bgClearFade[1] / 255)

        tx["1P_Up_1st.png"]:DrawRect(0, 0, -bgScrollX, 0, 1920, loopHeight)
        tx["1P_Up_3rd.png"]:DrawRect(0, math.sin(animeCounter1 * math.pi) * 10, -bgScrollX, 0, 1920, 349)
        tx["Clear_Up_1st.png"]:DrawRect(0, 0, -bgScrollX, 0, 1920, loopHeight)
        tx["Clear_Up_2nd.png"]:DrawRect(0, math.sin(animeCounter1 * math.pi) * 10, -bgScrollX, 0, 1920, 349)
        for i = 0, 5 do
            drawUp3rd_c(bgScrollX2 + (loopWidth * 2 * i), 0, 0)
        end

    elseif state.playerCount == 1 and state.p1IsBlue == true then

        tx["Clear_Up_1st.png"]:SetOpacity(bgClearFade[1] / 255)
        tx["Clear_Up_2nd.png"]:SetOpacity(bgClearFade[1] / 255)
        tx["Clear_Up_3rd.png"]:SetOpacity(bgClearFade[1] / 255)

        tx["2P_Up_1st.png"]:DrawRect(0, 0, -bgScrollX, 0, 1920, loopHeight)
        tx["2P_Up_3rd.png"]:DrawRect(0, math.sin(animeCounter1 * math.pi) * 10, -bgScrollX, 0, 1920, 349)
        tx["Clear_Up_1st.png"]:DrawRect(0, 0, -bgScrollX, 0, 1920, loopHeight)
        tx["Clear_Up_2nd.png"]:DrawRect(0, math.sin(animeCounter1 * math.pi) * 10, -bgScrollX, 0, 1920, 349)
        for i = 0, 5 do
            drawUp3rd_c(bgScrollX2 + (loopWidth * 2 * i), 0, 1)
        end

    elseif state.playerCount == 2 then

        for player = 0, state.playerCount - 1 do
            tx["Clear_Up_1st.png"]:SetOpacity(bgClearFade[player + 1] / 255)
            tx["Clear_Up_2nd.png"]:SetOpacity(bgClearFade[player + 1] / 255)
            tx["Clear_Up_3rd.png"]:SetOpacity(bgClearFade[player + 1] / 255)

            if player == 0 then
                tx["1P_Up_1st.png"]:DrawRect(0, 0, -bgScrollX, 0, 1920, loopHeight)
                tx["1P_Up_3rd.png"]:DrawRect(0, math.sin(animeCounter1 * math.pi) * 10, -bgScrollX, 0, 1920, 349)
                tx["Clear_Up_1st.png"]:DrawRect(0, 0, -bgScrollX, 0, 1920, loopHeight)
                tx["Clear_Up_2nd.png"]:DrawRect(0, math.sin(animeCounter1 * math.pi) * 10, -bgScrollX, 0, 1920, 349)
                for i = 0, 5 do
                    drawUp3rd_c(bgScrollX2 + (loopWidth * 2 * i), 0, 0)
                end
            else
                tx["2P_Up_1st.png"]:DrawRect(0, 804, -bgScrollX, 0, 1920, loopHeight)
                tx["2P_Up_3rd.png"]:DrawRect(0, 804 + math.sin(animeCounter1 * math.pi) * 10, -bgScrollX, 0, 1920, 349)
                tx["Clear_Up_1st.png"]:DrawRect(0, 804, -bgScrollX, 0, 1920, loopHeight)
                tx["Clear_Up_2nd.png"]:DrawRect(0, 804 + math.sin(animeCounter1 * math.pi) * 10, -bgScrollX, 0, 1920, 349)
                for i = 0, 5 do
                    drawUp3rd_c(bgScrollX2 + (loopWidth * 2 * i), 804, 1)
                end
            end
        end

    elseif state.playerCount == 3 then

        tx["Clear_Up_1st.png"]:DrawRect(0, 0, -bgScrollX, 0, 1920, 288)
        tx["Clear_Up_1st.png"]:DrawRect(0, 804, -bgScrollX, 0, 1920, 288)
        tx["Clear_Up_2nd.png"]:DrawRect(0, 804 + math.sin(animeCounter1 * math.pi) * 10, -bgScrollX, 0, 1920, 349)
        for i = 0, 5 do
            drawUp3rd_c(bgScrollX2 + (loopWidth * 2 * i), 804, 0)
        end

    elseif state.playerCount == 4 then

        tx["Clear_Up_1st.png"]:DrawRect(0, 0, -bgScrollX, 0, 1920, 1080)

    end
end

function onDestroy()
    for _, t in pairs(tx) do
        if t ~= nil then t:Dispose() end
    end
    tx = {}
end