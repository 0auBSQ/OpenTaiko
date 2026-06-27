---@diagnostic disable: undefined-global  -- TEXTURE/fps injected by CLuaScript at runtime
-- Dan Gold Pass end animation: a scroll rises into view then slides open (cubic/quad eases).
-- Group C clear/end script: the host passes the player via the state table; playEndAnime(player)
-- is called directly by the host with the index and (re)starts the per-play counters.

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

local tx = {}            -- name -> LuaTexture

function clearIn(player)
end

function clearOut(player)
end

function playEndAnime(player)
    -- (re)start the per-play counters (the old init reset these too)
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
