--func:DrawText(x, y, text);
--func:DrawNum(x, y, num);
--func:AddGraph("filename");
--func:DrawGraph(x, y, filename);
--func:DrawRectGraph(x, y, rect_x, rect_y, rect_width, rect_height, filename);
--func:SetOpacity(opacity, "filename");
--func:SetScale(xscale, yscale, "filename");
--func:SetColor(r, g, b, "filename");

local xPos = 1292
local yPos = 405

local rectWidth = 197
local rectHeight = 355

local swipeWidth = 10
local swipeLength = 187

local animeDraw = 0
local swipeDraw = 0

local drawSpeed = 0

function init()
    animeDraw = 0
    swipeDraw = 0
    drawSpeed = 197 / 0.15

    func:AddGraph("Diffs.png")
    func:AddGraph("Swipe.png")
end

function playAnime()
    animeDraw = 0
    swipeDraw = 0
end

function update()
    animeDraw = animeDraw + (drawSpeed * deltaTime)
    swipeDraw = math.min(animeDraw, swipeWidth) - math.min(math.max(animeDraw - swipeLength, 0), swipeWidth)
end

function draw()
    -- Draw Extra
    func:DrawRectGraph(xPos + animeDraw, yPos, rectWidth + animeDraw, 0, math.max(rectWidth - animeDraw, 0), rectHeight, "Diffs.png")
    -- Draw Extreme
    func:DrawRectGraph(xPos, yPos, 0, 0, math.min(animeDraw, rectWidth), rectHeight, "Diffs.png")

    -- Draw Swipe
    func:DrawRectGraph(xPos + animeDraw, yPos, 0, 0, swipeDraw, rectHeight, "Swipe.png")
end
