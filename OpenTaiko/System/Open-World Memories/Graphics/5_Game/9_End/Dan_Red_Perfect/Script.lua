--func:DrawText(x, y, text);
--func:DrawNum(x, y, num);
--func:AddGraph("filename");
--func:DrawGraph(x, y, filename);
--func:DrawRectGraph(x, y, rect_x, rect_y, rect_width, rect_height, filename);
--func:SetOpacity(opacity, "filename");
--func:SetScale(xscale, yscale, "filename");
--func:SetColor(r, g, b, "filename");

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

function clearIn(player)
end

function clearOut(player)
end

function playEndAnime(player)
end

function init()
    func:AddGraph("Scroll.png")
    func:AddGraph("Scroll_Back.png")
    func:AddGraph("Scroll_Back_Overlay.png")
end

function update(player)
    animeTimer = animeTimer + deltaTime
    scrollRiseTransition = math.min(animeTimer, scrollRiseDuration) * (1 / scrollRiseDuration)
    scrollSlideTransition = math.min(math.max(animeTimer - scrollRiseDuration, 0), scrollSlideDuration) * (1 / scrollSlideDuration)

    scrollRisePos = height - ((height - scrollPosY) * (1 + easeB * (scrollRiseTransition - 1)^3 + easeA * (scrollRiseTransition - 1)^2))
    if scrollSlideTransition < 0.5 then
        scrollSlidePos = scrollPosX - ((scrollBackWidth - scrollWidth) * (2 * scrollSlideTransition * scrollSlideTransition))
    else
        scrollSlidePos = scrollPosX - ((scrollBackWidth - scrollWidth) * (1 - (-2 * scrollSlideTransition + 2)^2 / 2))
    end
end

function draw(player)
    if scrollSlideTransition > 0 then
        -- Draw scroll background
        func:DrawRectGraph(scrollSlidePos, scrollRisePos, scrollSlidePos - scrollBackWidth - scrollBackXOffset, 0, scrollBackWidth - scrollSlidePos + scrollBackXOffset, scrollHeight, "Scroll_Back.png")
        func:DrawRectGraph(scrollSlidePos, scrollRisePos, scrollSlidePos - scrollBackWidth - scrollBackXOffset, 0, scrollBackWidth - scrollSlidePos + scrollBackXOffset, scrollHeight, "Scroll_Back_Overlay.png")
    end
    -- Draw scroll
    func:DrawGraph(scrollSlidePos, scrollRisePos, "Scroll.png")
end
