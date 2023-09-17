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

function clearIn(player)
end

function clearOut(player)
end

function init()
    func:AddGraph("Background_Win.png")
    func:AddGraph("Background_Lose.png")
    func:AddGraph("Background_Gradation.png")
end

function update()
end

function draw()
    if battleWin then
        func:DrawGraph(0, 0, "Background_Win.png")
    else
        func:DrawGraph(0, 0, "Background_Lose.png")
    end
    func:DrawGraph(0, 0, "Background_Gradation.png")
end
