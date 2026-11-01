---@diagnostic disable: undefined-global  -- TEXTURE/fps injected by CLuaScript at runtime
-- AI up background 0: a fanned arc of "Up" portrait sprites that ease in from the center,
-- over a base/frame, plus a looping bottom animation. state.battleState splits 1P vs 2P sprites.
-- Ported from the old ScriptBG func: API to the ROActivity LuaTexture API.

local inAnimeCounter = -20

local animeCounter = 0

local nowAnimeFrame = 0

local maxAnimeFrame = 5

local tx = {}            -- name -> LuaTexture

function clearIn(player)
end

function clearOut(player)
end

function onStart()
    tx["Base.png"] = TEXTURE:CreateTextureSync("Base.png")
	tx["Frame.png"] = TEXTURE:CreateTextureSync("Frame.png")
    tx["Down_1P/Default.png"] = TEXTURE:CreateTextureSync("Down_1P/Default.png")
    tx["Down_2P/Default.png"] = TEXTURE:CreateTextureSync("Down_2P/Default.png")
    for i = 0 , 18 do
        --tx["Down_1P/"..tostring(i)..".png"] = TEXTURE:CreateTextureSync("Down_1P/"..tostring(i)..".png")
        --tx["Down_2P/"..tostring(i)..".png"] = TEXTURE:CreateTextureSync("Down_2P/"..tostring(i)..".png")
		tx["Up_1P/"..tostring(i)..".png"] = TEXTURE:CreateTextureSync("Up_1P/"..tostring(i)..".png")
        tx["Up_2P/"..tostring(i)..".png"] = TEXTURE:CreateTextureSync("Up_2P/"..tostring(i)..".png")
    end

	for i = 0 , maxAnimeFrame do
        tx["Animation/"..tostring(i)..".png"] = TEXTURE:CreateTextureSync("Animation/"..tostring(i)..".png")
    end
end

function update(timestamp, state)
    inAnimeCounter = inAnimeCounter + (20 * fps.deltaTime)

	animeCounter = animeCounter + (10 * fps.deltaTime)

    nowAnimeFrame = math.floor(animeCounter+0.5)

    if nowAnimeFrame > maxAnimeFrame then
        animeCounter = 0;
        nowAnimeFrame = 0;
    end
end

function draw(state)
    tx["Base.png"]:Draw(0, 0)

    for i = 0 , 18 do
        local pos = i - 9
        if 9 - math.abs(pos) <= inAnimeCounter then

			local offset1 = -(math.sin((pos / 9.0) * math.pi) * 6)

            local offset2 = 0

            if pos > 0 then
                offset2 = (math.cos((pos / 9.0) * math.pi) * 5)
            elseif pos < 0 then
                offset2 = -(math.cos((pos / 9.0) * math.pi) * 5)
            end

            local up_x = 756 + (91 * pos) + offset1 + offset2;
            local up_y = 57


            local down_x = -32 + (106.5 * i)
            local down_y = 804

            if pos <= state.battleState then
                --tx["Down_1P/"..tostring(i)..".png"]:Draw(x, y)
				tx["Up_1P/"..tostring(i)..".png"]:Draw(up_x, up_y)
                tx["Down_1P/Default.png"]:Draw(down_x, down_y)
            else
                --tx["Down_2P/"..tostring(i)..".png"]:Draw(x, y)
				tx["Up_2P/"..tostring(i)..".png"]:Draw(up_x, up_y)
                tx["Down_2P/Default.png"]:Draw(down_x, down_y)
            end
        end
    end

	tx["Frame.png"]:Draw(0, 0)
	tx["Animation/"..tostring(nowAnimeFrame)..".png"]:Draw(0, 0)
end

function onDestroy()
    for _, t in pairs(tx) do
        if t ~= nil then t:Dispose() end
    end
    tx = {}
end