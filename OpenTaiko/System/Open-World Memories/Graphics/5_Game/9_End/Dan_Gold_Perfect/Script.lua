---@diagnostic disable: undefined-global  -- TEXTURE/fps injected by CLuaScript at runtime
-- Dan Gold Perfect end animation: a scroll that rises (eased back-out) then slides open horizontally.
-- Ported from the old ScriptBG func: API to the ROActivity LuaTexture API.
-- Group C clear/end script: the host passes the player index via state.player (update/draw) and
-- directly to playEndAnime(player); per-play counters are (re)started in playEndAnime.

local width = 1920
local height = 1080

local animeTimer = 0
local scrollRiseTransition = 0
local scrollSlideTransition = 0

local scrollRisePos = 0
local scrollSlidePos = 0

local scrollPosX = 1693
local scrollPosY = 254
local scrollBackXOffset = 77

local scrollWidth = 160
local scrollHeight = 573
local scrollBackWidth = 1776

local scrollRiseDuration = 0.5
local scrollSlideDuration = 0.2

local easeA = 1.1
local easeB = easeA + 1

local tx = {}

function clearIn(player)
end

function clearOut(player)
end

function playEndAnime(player)
    -- per-play resets (was the old init() body, minus the texture loads which moved to onStart)
    animeTimer = 0
    scrollRiseTransition = 0
    scrollSlideTransition = 0
    scrollRisePos = 0
    scrollSlidePos = 0
end

function onStart()
    tx["Scroll.png"] = TEXTURE:CreateTextureSync("Scroll.png")
    tx["Scroll_Back.png"] = TEXTURE:CreateTextureSync("Scroll_Back.png")
    tx["Scroll_Back_Overlay.png"] = TEXTURE:CreateTextureSync("Scroll_Back_Overlay.png")
end

function update(timestamp, state)
    local player = state.player

    animeTimer = animeTimer + fps.deltaTime
    scrollRiseTransition = math.min(animeTimer, scrollRiseDuration) * (1 / scrollRiseDuration)
    scrollSlideTransition = math.min(math.max(animeTimer - scrollRiseDuration, 0), scrollSlideDuration) * (1 / scrollSlideDuration)

    scrollRisePos = height - ((height - scrollPosY) * (1 + easeB * (scrollRiseTransition - 1)^3 + easeA * (scrollRiseTransition - 1)^2))
    if scrollSlideTransition < 0.5 then
        scrollSlidePos = scrollPosX - ((scrollBackWidth - scrollWidth) * (2 * scrollSlideTransition * scrollSlideTransition))
    else
        scrollSlidePos = scrollPosX - ((scrollBackWidth - scrollWidth) * (1 - (-2 * scrollSlideTransition + 2)^2 / 2))
    end
end

function draw(state)
    local player = state.player

    if scrollSlideTransition > 0 then
        -- Draw scroll background
        tx["Scroll_Back.png"]:DrawRect(scrollSlidePos, scrollRisePos, scrollSlidePos - scrollBackWidth - scrollBackXOffset, 0, scrollBackWidth - scrollSlidePos + scrollBackXOffset, scrollHeight)
        tx["Scroll_Back_Overlay.png"]:DrawRect(scrollSlidePos, scrollRisePos, scrollSlidePos - scrollBackWidth - scrollBackXOffset, 0, scrollBackWidth - scrollSlidePos + scrollBackXOffset, scrollHeight)
    end
    -- Draw scroll
    tx["Scroll.png"]:Draw(scrollSlidePos, scrollRisePos)
end

function onDestroy()
    for _, t in pairs(tx) do
        if t ~= nil then t:Dispose() end
    end
    tx = {}
end
