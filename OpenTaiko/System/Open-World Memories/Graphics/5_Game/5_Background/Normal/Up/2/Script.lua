---@diagnostic disable: undefined-global  -- TEXTURE/fps injected by CLuaScript at runtime
-- Up background 2: parallax (bg + flower + two cloud layers) with infinite horizontal scroll + per-player clear fade.
-- Loop widths come from the texture sizes, read in onStart via LuaTexture.Width — the load is SYNC
-- (CreateTextureSync), so sizes are valid immediately (the old func:AddGraph forced the same sync load).

local bgLoopWidth = 570
local flowerLoopWidth = 1086
local cloud1LoopWidth = 842
local cloud2LoopWidth = 842

local bgScrollX = 0
local flowerScrollX = 0
local cloud1ScrollX = 0
local cloud2ScrollX = 0

local clearOpacity = { 0, 0 }

local tx = {}

function clearIn(player)
end

function clearOut(player)
end

function onStart()
    -- Load every variant up front; draw() selects by state. Sizes are valid here (sync load).
    tx["BG_Red.png"] = TEXTURE:CreateTextureSync("BG_Red.png")
    tx["Flower_Red.png"] = TEXTURE:CreateTextureSync("Flower_Red.png")
    tx["Cloud1_Red.png"] = TEXTURE:CreateTextureSync("Cloud1_Red.png")
    tx["Cloud2_Red.png"] = TEXTURE:CreateTextureSync("Cloud2_Red.png")

    tx["BG_Blue.png"] = TEXTURE:CreateTextureSync("BG_Blue.png")
    tx["Flower_Blue.png"] = TEXTURE:CreateTextureSync("Flower_Blue.png")
    tx["Cloud1_Blue.png"] = TEXTURE:CreateTextureSync("Cloud1_Blue.png")
    tx["Cloud2_Blue.png"] = TEXTURE:CreateTextureSync("Cloud2_Blue.png")

    tx["BG_Clear.png"] = TEXTURE:CreateTextureSync("BG_Clear.png")
    tx["Flower_Clear.png"] = TEXTURE:CreateTextureSync("Flower_Clear.png")
    tx["Cloud1_Clear.png"] = TEXTURE:CreateTextureSync("Cloud1_Clear.png")
    tx["Cloud2_Clear.png"] = TEXTURE:CreateTextureSync("Cloud2_Clear.png")

    bgLoopWidth = tx["BG_Red.png"].Width
    flowerLoopWidth = tx["Flower_Red.png"].Width
    cloud1LoopWidth = tx["Cloud1_Red.png"].Width
    cloud2LoopWidth = tx["Cloud2_Red.png"].Width

    -- random values to create initial depth
    bgScrollX = 500
    cloud1ScrollX = 250
    cloud2ScrollX = 314
end

function update(timestamp, state)
    bgScrollX = (bgScrollX + (fps.deltaTime * 20)) % bgLoopWidth
    flowerScrollX = (flowerScrollX + (fps.deltaTime * 27)) % flowerLoopWidth
    cloud1ScrollX = (cloud1ScrollX + (fps.deltaTime * 40)) % cloud1LoopWidth
    cloud2ScrollX = (cloud2ScrollX + (fps.deltaTime * 59)) % cloud2LoopWidth

    if state.isClear[0] then
        clearOpacity[1] = math.min(clearOpacity[1] + (2000 * fps.deltaTime), 255)
    else
        clearOpacity[1] = math.max(clearOpacity[1] - (2000 * fps.deltaTime), 0)
    end

    if state.playerCount == 2 then
        if state.isClear[1] then
            clearOpacity[2] = math.min(clearOpacity[2] + (2000 * fps.deltaTime), 255)
        else
            clearOpacity[2] = math.max(clearOpacity[2] - (2000 * fps.deltaTime), 0)
        end
    end
end

function draw(state)
    if state.playerCount == 1 then
        if clearOpacity[1] < 255 then
            if state.p1IsBlue then
                tx["BG_Blue.png"]:DrawRect(0, 0, bgScrollX, 0, 1920, 288)
                tx["Flower_Blue.png"]:DrawRect(0, 0, flowerScrollX, 0, 1920, 288)
                tx["Cloud1_Blue.png"]:DrawRect(0, 0, cloud1ScrollX, 0, 1920, 288)
                tx["Cloud2_Blue.png"]:DrawRect(0, 0, cloud2ScrollX, 0, 1920, 288)
            else
                tx["BG_Red.png"]:DrawRect(0, 0, bgScrollX, 0, 1920, 288)
                tx["Flower_Red.png"]:DrawRect(0, 0, flowerScrollX, 0, 1920, 288)
                tx["Cloud1_Red.png"]:DrawRect(0, 0, cloud1ScrollX, 0, 1920, 288)
                tx["Cloud2_Red.png"]:DrawRect(0, 0, cloud2ScrollX, 0, 1920, 288)
            end
        end
        if clearOpacity[1] > 0 then
            tx["BG_Clear.png"]:SetOpacity(clearOpacity[1] / 255)
            tx["Flower_Clear.png"]:SetOpacity(clearOpacity[1] / 255)
            tx["Cloud1_Clear.png"]:SetOpacity(clearOpacity[1] / 255)
            tx["Cloud2_Clear.png"]:SetOpacity(clearOpacity[1] / 255)
            tx["BG_Clear.png"]:DrawRect(0, 0, bgScrollX, 0, 1920, 288)
            tx["Cloud1_Clear.png"]:DrawRect(0, 0, cloud1ScrollX, 0, 1920, 288)
            tx["Cloud2_Clear.png"]:DrawRect(0, 0, cloud2ScrollX, 0, 1920, 288)
            tx["Flower_Clear.png"]:DrawRect(0, 0, flowerScrollX, 0, 1920, 288)
        end
    elseif state.playerCount == 2 then
        if clearOpacity[1] < 255 then
            tx["BG_Red.png"]:DrawRect(0, 0, bgScrollX, 0, 1920, 288)
            tx["Flower_Red.png"]:DrawRect(0, 0, flowerScrollX, 0, 1920, 288)
            tx["Cloud1_Red.png"]:DrawRect(0, 0, cloud1ScrollX, 0, 1920, 288)
            tx["Cloud2_Red.png"]:DrawRect(0, 0, cloud2ScrollX, 0, 1920, 288)
        end
        if clearOpacity[1] > 0 then
            tx["BG_Clear.png"]:SetOpacity(clearOpacity[1] / 255)
            tx["Flower_Clear.png"]:SetOpacity(clearOpacity[1] / 255)
            tx["Cloud1_Clear.png"]:SetOpacity(clearOpacity[1] / 255)
            tx["Cloud2_Clear.png"]:SetOpacity(clearOpacity[1] / 255)
            tx["BG_Clear.png"]:DrawRect(0, 0, bgScrollX, 0, 1920, 288)
            tx["Cloud1_Clear.png"]:DrawRect(0, 0, cloud1ScrollX, 0, 1920, 288)
            tx["Cloud2_Clear.png"]:DrawRect(0, 0, cloud2ScrollX, 0, 1920, 288)
            tx["Flower_Clear.png"]:DrawRect(0, 0, flowerScrollX, 0, 1920, 288)
        end

        if clearOpacity[2] < 255 then
            tx["BG_Blue.png"]:DrawRect(0, 804, bgScrollX, 0, 1920, 288)
            tx["Flower_Blue.png"]:DrawRect(0, 804, flowerScrollX, 0, 1920, 288)
            tx["Cloud1_Blue.png"]:DrawRect(0, 804, cloud1ScrollX, 0, 1920, 288)
            tx["Cloud2_Blue.png"]:DrawRect(0, 804, cloud2ScrollX, 0, 1920, 288)
        end
        if clearOpacity[2] > 0 then
            tx["BG_Clear.png"]:SetOpacity(clearOpacity[2] / 255)
            tx["Flower_Clear.png"]:SetOpacity(clearOpacity[2] / 255)
            tx["Cloud1_Clear.png"]:SetOpacity(clearOpacity[2] / 255)
            tx["Cloud2_Clear.png"]:SetOpacity(clearOpacity[2] / 255)
            tx["BG_Clear.png"]:DrawRect(0, 804, bgScrollX, 0, 1920, 288)
            tx["Cloud1_Clear.png"]:DrawRect(0, 804, cloud1ScrollX, 0, 1920, 288)
            tx["Cloud2_Clear.png"]:DrawRect(0, 804, cloud2ScrollX, 0, 1920, 288)
            tx["Flower_Clear.png"]:DrawRect(0, 804, flowerScrollX, 0, 1920, 288)
        end
    elseif state.playerCount == 3 then
        tx["BG_Clear.png"]:DrawRect(0, 804, bgScrollX, 0, 1920, 288)
        tx["Cloud1_Clear.png"]:DrawRect(0, 804, cloud1ScrollX, 0, 1920, 288)
        tx["Cloud2_Clear.png"]:DrawRect(0, 804, cloud2ScrollX, 0, 1920, 288)
        tx["Flower_Clear.png"]:DrawRect(0, 804, flowerScrollX, 0, 1920, 288)
    elseif state.playerCount == 4 then
        tx["BG_Clear.png"]:DrawRect(0, 0, bgScrollX, 0, 1920, 1080)
    end
end

function onDestroy()
    for _, t in pairs(tx) do
        if t ~= nil then t:Dispose() end
    end
    tx = {}
end