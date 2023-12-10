--func:DrawText(x, y, text)
--func:DrawNum(x, y, num)
--func:AddGraph("filename")
--func:DrawGraph(x, y, filename)
--func:DrawRectGraph(x, y, rect_x, rect_y, rect_width, rect_height, filename)
--func:DrawGraphCenter(x, y, filename)
--func:DrawGraphRectCenter(x, y, rect_x, rect_y, rect_width, rect_height, filename)
--func:SetOpacity(opacity, "filename")
--func:SetRotation(angle, "fileName")
--func:SetScale(xscale, yscale, "filename")
--func:SetColor(r, g, b, "filename")

local loadingAnimeType = 0

local optkAngle = 0

function clearIn(player)
end

function clearOut(player)
end

function init()
    func:AddGraph("Background.png")

    if loadingAnimeType == 0 then
        func:AddGraph("OpTKIcon.png")
    elseif loadingAnimeType == 1 then
    end
end

function update()
    if loadingAnimeType == 0 then
        optkAngle = optkAngle + (360 * deltaTime)
    elseif loadingAnimeType == 1 then
    end
end

function draw()
    func:DrawGraph(0, 0, "Background.png")
    if loadingAnimeType == 0 then
        func:SetRotation(optkAngle, "OpTKIcon.png")
        func:DrawGraph(1720, 880, "OpTKIcon.png")
    elseif loadingAnimeType == 1 then
    end
end
