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
    func:AddGraph("BG_Space.png")
    func:AddGraph("BG_Frame.png")
end

function update()
end

function draw()
    func:DrawGraph(0, 0, "BG_Space.png")
    func:DrawGraph(0, 0, "BG_Frame.png")
end
