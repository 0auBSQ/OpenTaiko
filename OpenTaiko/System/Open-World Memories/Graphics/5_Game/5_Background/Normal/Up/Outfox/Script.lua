---@diagnostic disable: undefined-global  -- TEXTURE/fps injected by CLuaScript at runtime
-- Up background "Outfox": Project OutFox crossover BG. Simple mode = static red/blue/clear bg + simple note strip;
-- regular mode = horizontally scrolling bg with animated note rows (notes/pump/bemu) and hopping tiny foxes,
-- driven by bpm/totalTime. Per-player clear fade for 1P/2P. Ported from old ScriptBG func: API to the
-- ROActivity LuaTexture API (texture loads unconditional in onStart; player/mode conditionals kept in draw).

-- Please note that the Project OutFox branding or graphics included are not permitted
-- for use outside of OpenTaiko without permission from the Project OutFox Developers.

-- The graphics are included as part of the OpenTaiko Team Collaboration to promote
-- the use of Taiko and other games to a wider audience.

local loopWidth = 1920
local loopHeight = 288
local simpleHeight = 270
local pos2PY = 804

local bgScrollX = 0
local bpmTime = 0
local totalTime = -2
local bgClearFade = { 0, 0 }

local foxPosBlueStart = { 82.4, 136.6, 338, 56, 564, 111, 820.7, 139.6, 1014, 54, 1238, 54, 1445, 52, 1688, 72, 1915, 66}
local foxPosBlueEnd = { 180, 39, 338, -13, 564, 42, 723.1, 42, 1014, -15, 1238, -15, 1445, -17, 1688, 3, 1915, -3 }
local foxPosRedStart = { 138.7, 136.6, 338, 56, 564, 111, 767.4, 139.6, 1014, 54, 1238, 54, 1479, 52, 1688, 72, 1915, 66 }
local foxPosRedEnd = { 41.1, 39, 338, -13, 564, 42, 865, 42, 1014, -15, 1238, -15, 1479, -17, 1688, 3, 1915, -3 }
local foxRotBlue = { -45, 0, 0, 45, 0, 0, 0, 0, 0 }
local foxRotRed = { 45, 0, 0, -45, 0, 0, 0, 0, 0 }

local noteSize = 225

local tx = {}            -- name -> LuaTexture

function updateClearFade(state)
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

function drawTinyFox(x_start, x_end, y_start, y_end, rot, progress, useAlt, lazyArrowFix)

    local foxRect = { 0, 138, 128, 138, 0, 0 }
    local arrowFix = 0
    if useAlt then
        foxRect = { 256, 138, 384, 138, 128, 0 }
    end
    -- Due to a lapse in my judgement, a portion of the fox's body shows where it shouldn't on a couple of notes
    -- Rather than do a bunch of math to account for this, lemme just do this instead lol
    if lazyArrowFix then
        arrowFix = 0.7
    end
    tx["tinyfox.png"]:SetRotation(rot)

    if progress >= 1.66 then
        tx["tinyfox.png"]:DrawRectAtAnchor(x_start - ((x_start - x_end)), y_start - ((y_start - y_end)), 512, 0, 128, 138, "center") --Tail
        tx["tinyfox.png"]:DrawRectAtAnchor(x_start - ((x_start - x_end)), y_start - ((y_start - y_end)), foxRect[5], foxRect[6], 128, 138, "center") --Body
    elseif progress >= 1.33 then
        tx["tinyfox.png"]:DrawRectAtAnchor(x_start - ((x_start - x_end)), y_start - ((y_start - y_end)), 384, 0, 128, 138, "center") --Tail
        tx["tinyfox.png"]:DrawRectAtAnchor(x_start - ((x_start - x_end)), y_start - ((y_start - y_end)), foxRect[5], foxRect[6], 128, 138, "center") --Body
    elseif progress >= 1 then
        tx["tinyfox.png"]:DrawRectAtAnchor(x_start - ((x_start - x_end)), y_start - ((y_start - y_end)), 256, 0, 128, 138, "center") --Tail
        tx["tinyfox.png"]:DrawRectAtAnchor(x_start - ((x_start - x_end)), y_start - ((y_start - y_end)), foxRect[5], foxRect[6], 128, 138, "center") --Body
    elseif progress >= 0.85 then
        tx["tinyfox.png"]:DrawRectAtAnchor(x_start - ((x_start - x_end) * progress), y_start - ((y_start - y_end) * progress), foxRect[3], foxRect[4], 128, 138 * progress, "center")
    elseif progress >= arrowFix then
        tx["tinyfox.png"]:DrawRectAtAnchor(x_start - ((x_start - x_end) * progress), y_start - ((y_start - y_end) * progress), foxRect[1], foxRect[2], 128, 138 * progress, "center")
    end
end

function drawOFNotes(y_offset, drawBlue)
    local allNotesOffset = 2025
    local pump_offsetX = math.floor((bpmTime * 6) % 6) % 3
    local pump_offsetY = math.floor(((bpmTime * 6) % 6) / 3)
    local calculated_y_offset = 27.5 + (math.sin(bpmTime) * 15) + y_offset

    local notePath = "notes_red.png"
    local pumpPath = "pump_red.png"
    local bemuPath = "bemu_red.png"
    if drawBlue == true then
      notePath = "notes_blue.png"
      pumpPath = "pump_blue.png"
      bemuPath = "bemu_blue.png"
    end

    local x_offset = (bgScrollX * 1.3)
    for i = -1, 0 do
        local calculated_x_offset = (x_offset % allNotesOffset) + (allNotesOffset * i)
        local progress = math.min(math.max((totalTime % 8) * 1.25, 0), 1) + math.min(math.max(((totalTime % 8) - 3) * 3, 0), 1) - math.min(math.max(((totalTime % 8) - 3.33) * 3, 0), 1) + math.min(math.max(((totalTime % 8) - 4) * 3, 0), 1) - math.min(math.max(((totalTime % 8) - 4.33) * 3, 0), 1) - math.min(math.max(((totalTime % 8) - 5.5) * 2, 0), 1)
        local foxSet = math.floor(totalTime / 8) % 3

        if progress < 1 then
            for j = foxSet + 1, 9, 3 do
                local offset = (j-1)*2
                local alt = (j == 3)
                local lazy = (j == 1 or j == 4)
                if drawBlue then
                    drawTinyFox( foxPosBlueStart[offset+1] + calculated_x_offset, foxPosBlueEnd[offset+1] + calculated_x_offset, foxPosBlueStart[offset+2] + calculated_y_offset, foxPosBlueEnd[offset+2] + calculated_y_offset, foxRotBlue[j], progress, alt, lazy)
                else
                    drawTinyFox( foxPosRedStart[offset+1] + calculated_x_offset, foxPosRedEnd[offset+1] + calculated_x_offset, foxPosRedStart[offset+2] + calculated_y_offset, foxPosRedEnd[offset+2] + calculated_y_offset, foxRotRed[j], progress, alt, lazy)
                end
            end
        end

        tx[notePath]:Draw(calculated_x_offset, calculated_y_offset)

        tx[bemuPath]:SetRotation(-180 * bpmTime)
        tx[bemuPath]:DrawRect(225 + calculated_x_offset, calculated_y_offset, noteSize, 0, noteSize, noteSize) -- Bemu

        tx[pumpPath]:DrawRect(1350 + calculated_x_offset, calculated_y_offset, noteSize * pump_offsetX, noteSize * pump_offsetY, noteSize, noteSize) -- Pump

        if progress >= 1 then
            for j = foxSet + 1, 9, 3 do
                local offset = (j-1)*2
                local alt = (j == 3)
                local lazy = (j == 1 or j == 4)
                if drawBlue then
                    drawTinyFox( foxPosBlueStart[offset+1] + calculated_x_offset, foxPosBlueEnd[offset+1] + calculated_x_offset, foxPosBlueStart[offset+2] + calculated_y_offset, foxPosBlueEnd[offset+2] + calculated_y_offset, foxRotBlue[j], progress, alt, lazy)
                else
                    drawTinyFox( foxPosRedStart[offset+1] + calculated_x_offset, foxPosRedEnd[offset+1] + calculated_x_offset, foxPosRedStart[offset+2] + calculated_y_offset, foxPosRedEnd[offset+2] + calculated_y_offset, foxRotRed[j], progress, false, lazy)
                end
            end
        end

        -- Debug Stuff (was func:DrawNum, removed)
    end
end

function clearIn(player)
end

function clearOut(player)
end

function onStart()
    -- Load every variant up front; draw() selects by state/mode. onStart runs before `state` arrives,
    -- so loads are unconditional (the player/mode gating in the old init() is kept in draw() only).

    -- SimpleMode Assets
    tx["notes_red_simple.png"] = TEXTURE:CreateTextureSync("notes_red_simple.png")
    tx["notes_blue_simple.png"] = TEXTURE:CreateTextureSync("notes_blue_simple.png")

    -- Regular Assets
    tx["notes_red.png"] = TEXTURE:CreateTextureSync("notes_red.png")
    tx["pump_red.png"] = TEXTURE:CreateTextureSync("pump_red.png")
    tx["bemu_red.png"] = TEXTURE:CreateTextureSync("bemu_red.png")
    tx["notes_blue.png"] = TEXTURE:CreateTextureSync("notes_blue.png")
    tx["pump_blue.png"] = TEXTURE:CreateTextureSync("pump_blue.png")
    tx["bemu_blue.png"] = TEXTURE:CreateTextureSync("bemu_blue.png")
    tx["tinyfox.png"] = TEXTURE:CreateTextureSync("tinyfox.png")

    -- Shared backgrounds
    tx["bg_red.png"] = TEXTURE:CreateTextureSync("bg_red.png")
    tx["bg_blue.png"] = TEXTURE:CreateTextureSync("bg_blue.png")
    tx["bg_clear.png"] = TEXTURE:CreateTextureSync("bg_clear.png")
end

function update(timestamp, state)
    totalTime = totalTime + fps.deltaTime
    bgScrollX = bgScrollX - (59 * fps.deltaTime)
    bpmTime = bpmTime + (fps.deltaTime * (state.bpm[0] / 120))

    -- Only needed for 1/2 player(s)
    if state.playerCount < 3 then
      updateClearFade(state)
    end
end


function draw(state)

    if state.simplemode then

        if state.playerCount == 1 and state.p1IsBlue == false then

            tx["bg_clear.png"]:SetOpacity(bgClearFade[1] / 255)
            tx["bg_red.png"]:DrawRect(0, 0, 0, 0, loopWidth, loopHeight)
            tx["bg_clear.png"]:DrawRect(0, 0, 0, 0, loopWidth, loopHeight)
            tx["notes_red_simple.png"]:DrawRect(0, 0, 0, 0, loopWidth, simpleHeight)

        elseif state.playerCount == 1 and state.p1IsBlue == true then

            tx["bg_clear.png"]:SetOpacity(bgClearFade[1] / 255)
            tx["bg_blue.png"]:DrawRect(0, 0, 0, 0, loopWidth, loopHeight)
            tx["bg_clear.png"]:DrawRect(0, 0, 0, 0, loopWidth, loopHeight)
            tx["notes_blue_simple.png"]:DrawRect(0, 0, 0, 0, loopWidth, simpleHeight)

        elseif state.playerCount == 2 then

            tx["bg_clear.png"]:SetOpacity(bgClearFade[1] / 255)
            tx["bg_red.png"]:DrawRect(0, 0, 0, 0, loopWidth, loopHeight)
            tx["bg_clear.png"]:DrawRect(0, 0, 0, 0, loopWidth, loopHeight)
            tx["notes_red_simple.png"]:DrawRect(0, 0, 0, 0, loopWidth, simpleHeight)

            tx["bg_clear.png"]:SetOpacity(bgClearFade[2] / 255)
            tx["bg_blue.png"]:DrawRect(0, pos2PY, 0, 0, loopWidth, loopHeight)
            tx["bg_clear.png"]:DrawRect(0, pos2PY, 0, 0, loopWidth, loopHeight)
            tx["notes_blue_simple.png"]:DrawRect(0, pos2PY, 0, 0, loopWidth, simpleHeight)

        elseif state.playerCount == 3 then

            tx["bg_blue.png"]:DrawRect(0, pos2PY, 0, 0, loopWidth, loopHeight)
            tx["notes_blue_simple.png"]:DrawRect(0, pos2PY, 0, 0, loopWidth, simpleHeight)

        elseif state.playerCount == 4 then

            tx["bg_blue.png"]:DrawRect(0, 0, 0, 0, 1920, 1080)

        end

    else
        if state.playerCount == 1 and state.p1IsBlue == false then

            tx["bg_clear.png"]:SetOpacity(bgClearFade[1] / 255)
            tx["bg_red.png"]:DrawRect(0, 0, -bgScrollX, 0, loopWidth, loopHeight)
            tx["bg_clear.png"]:DrawRect(0, 0, -bgScrollX, 0, loopWidth, loopHeight)
            drawOFNotes(28, false)

        elseif state.playerCount == 1 and state.p1IsBlue == true then

            tx["bg_clear.png"]:SetOpacity(bgClearFade[1] / 255)
            tx["bg_blue.png"]:DrawRect(0, 0, -bgScrollX, 0, loopWidth, loopHeight)
            tx["bg_clear.png"]:DrawRect(0, 0, -bgScrollX, 0, loopWidth, loopHeight)
            drawOFNotes(28, true)

        elseif state.playerCount == 2 then

            tx["bg_clear.png"]:SetOpacity(bgClearFade[1] / 255)
            tx["bg_red.png"]:DrawRect(0, 0, -bgScrollX, 0, loopWidth, loopHeight)
            tx["bg_clear.png"]:DrawRect(0, 0, -bgScrollX, 0, loopWidth, loopHeight)
            drawOFNotes(28, false)
            tx["bg_clear.png"]:SetOpacity(bgClearFade[2] / 255)
            tx["bg_blue.png"]:DrawRect(0, pos2PY, -bgScrollX, 0, loopWidth, loopHeight)
            tx["bg_clear.png"]:DrawRect(0, pos2PY, -bgScrollX, 0, loopWidth, loopHeight)
            drawOFNotes(pos2PY + 28, true)

        elseif state.playerCount == 3 then

            tx["bg_blue.png"]:DrawRect(0, pos2PY, -bgScrollX, 0, loopWidth, loopHeight)
            drawOFNotes(pos2PY + 28, true)

        elseif state.playerCount == 4 then

            tx["bg_blue.png"]:DrawRect(0, 0, -bgScrollX, 0, 1920, 1080)

        end
    end

end

function onDestroy()
    for _, t in pairs(tx) do
        if t ~= nil then t:Dispose() end
    end
    tx = {}
end
