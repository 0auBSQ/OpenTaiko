---@diagnostic disable: undefined-global  -- TEXTURE/fps injected by CLuaScript at runtime
-- Up background 3: parallax (bg + cloud + note layers) with infinite horizontal scroll + per-player clear fade.
-- Ported from the old ScriptBG func: API to the ROActivity LuaTexture API.

local bgLoopWidth = 1800
local cloudLoopWidth = 1086
local noteLoopWidth = 1086

local bgScrollX = 0
local cloudScrollX = 0
local noteScrollX = 0

local clearOpacity = {0,0}

local tx = {}

function clearIn(player)
end

function clearOut(player)
end

function onStart()
    -- Load every variant up front; draw() selects by state. (onStart runs before `state` exists,
    -- so loads are unconditional — the old init() gated these on player count / p1IsBlue.)
    tx["BG_Red.png"] = TEXTURE:CreateTextureSync("BG_Red.png")
    tx["Cloud_Red.png"] = TEXTURE:CreateTextureSync("Cloud_Red.png")
    tx["Note_Red.png"] = TEXTURE:CreateTextureSync("Note_Red.png")

    -- if isClear[0] then clearOpacity[1] = 255 end -- If using danger gauge

    tx["BG_Blue.png"] = TEXTURE:CreateTextureSync("BG_Blue.png")
    tx["Cloud_Blue.png"] = TEXTURE:CreateTextureSync("Cloud_Blue.png")
    tx["Note_Blue.png"] = TEXTURE:CreateTextureSync("Note_Blue.png")

    -- if isClear[1] then clearOpacity[2] = 255 end -- If using danger gauge

    tx["BG_Clear.png"] = TEXTURE:CreateTextureSync("BG_Clear.png")
    tx["Cloud_Clear.png"] = TEXTURE:CreateTextureSync("Cloud_Clear.png")
    tx["Note_Clear.png"] = TEXTURE:CreateTextureSync("Note_Clear.png")

    -- random values to create initial depth
    bgScrollX = 500
    cloudScrollX = 250
    noteScrollX = 314
end

function update(timestamp, state)
    bgScrollX = (bgScrollX + (fps.deltaTime * 20)) % bgLoopWidth
    cloudScrollX = (cloudScrollX + (fps.deltaTime * 27)) % cloudLoopWidth
    noteScrollX = (noteScrollX + (fps.deltaTime * 59)) % noteLoopWidth

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
                tx["Cloud_Blue.png"]:DrawRect(0, 0, cloudScrollX, 0, 1920, 288)
                tx["Note_Blue.png"]:DrawRect(0, 0, noteScrollX, 0, 1920, 288)
            else
                tx["BG_Red.png"]:DrawRect(0, 0, bgScrollX, 0, 1920, 288)
                tx["Cloud_Red.png"]:DrawRect(0, 0, cloudScrollX, 0, 1920, 288)
                tx["Note_Red.png"]:DrawRect(0, 0, noteScrollX, 0, 1920, 288)
            end
        end
        if clearOpacity[1] > 0 then
            tx["BG_Clear.png"]:SetOpacity(clearOpacity[1] / 255)
            tx["Cloud_Clear.png"]:SetOpacity(clearOpacity[1] / 255)
            tx["Note_Clear.png"]:SetOpacity(clearOpacity[1] / 255)
            tx["BG_Clear.png"]:DrawRect(0, 0, bgScrollX, 0, 1920, 288)
            tx["Note_Clear.png"]:DrawRect(0, 0, noteScrollX, 0, 1920, 288)
            tx["Cloud_Clear.png"]:DrawRect(0, 0, cloudScrollX, 0, 1920, 288)
        end
    elseif state.playerCount == 2 then
        if clearOpacity[1] < 255 then
            tx["BG_Red.png"]:DrawRect(0, 0, bgScrollX, 0, 1920, 288)
            tx["Cloud_Red.png"]:DrawRect(0, 0, cloudScrollX, 0, 1920, 288)
            tx["Note_Red.png"]:DrawRect(0, 0, noteScrollX, 0, 1920, 288)
        end
        if clearOpacity[1] > 0 then
            tx["BG_Clear.png"]:SetOpacity(clearOpacity[1] / 255)
            tx["Cloud_Clear.png"]:SetOpacity(clearOpacity[1] / 255)
            tx["Note_Clear.png"]:SetOpacity(clearOpacity[1] / 255)
            tx["BG_Clear.png"]:DrawRect(0, 0, bgScrollX, 0, 1920, 288)
            tx["Note_Clear.png"]:DrawRect(0, 0, noteScrollX, 0, 1920, 288)
            tx["Cloud_Clear.png"]:DrawRect(0, 0, cloudScrollX, 0, 1920, 288)
        end

        if clearOpacity[2] < 255 then
            tx["BG_Blue.png"]:DrawRect(0, 804, bgScrollX, 0, 1920, 288)
            tx["Cloud_Blue.png"]:DrawRect(0, 804, cloudScrollX, 0, 1920, 288)
            tx["Note_Blue.png"]:DrawRect(0, 804, noteScrollX, 0, 1920, 288)
        end
        if clearOpacity[2] > 0 then
            tx["BG_Clear.png"]:SetOpacity(clearOpacity[2] / 255)
            tx["Cloud_Clear.png"]:SetOpacity(clearOpacity[2] / 255)
            tx["Note_Clear.png"]:SetOpacity(clearOpacity[2] / 255)
            tx["BG_Clear.png"]:DrawRect(0, 804, bgScrollX, 0, 1920, 288)
            tx["Note_Clear.png"]:DrawRect(0, 804, noteScrollX, 0, 1920, 288)
            tx["Cloud_Clear.png"]:DrawRect(0, 804, cloudScrollX, 0, 1920, 288)
        end
    elseif state.playerCount == 3 then
        tx["BG_Clear.png"]:DrawRect(0, 804, bgScrollX, 0, 1920, 288)
        tx["Note_Clear.png"]:DrawRect(0, 804, noteScrollX, 0, 1920, 288)
        tx["Cloud_Clear.png"]:DrawRect(0, 804, cloudScrollX, 0, 1920, 288)
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
